using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // File-authored scenario and normal player inputs only; original recovery owns receiver selection and delivery.
    internal static class EvacuationServiceDisclosureTests
    {
        internal const string Case = "Evacuation service disclosure: area-gated discarded receivers protect the selected automatic delivery without changing physical execution";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static ConfigureOutletPolicyCommand Export(EntityId source, bool enabled)
            => new ConfigureOutletPolicyCommand { Source = source, Change = OutletPolicyChange.Replace(new OutletPolicySettings(enabled, Bill.Empty, 0,
                new[] { "destination", "service.receiver.far" }, LogisticsReserveAccess.Protected)) };
        private static List<LogisticsDispatchOffer> Evacuations(LogisticsView view, EntityId origin)
        { var rows = new List<LogisticsDispatchOffer>(); view.CopyQueue(rows); return rows.Where(row => row.Kind == LogisticsDispatchKind.Evacuation && row.RecoveryOrigin == origin).ToList(); }
        private static CommandResult Result(SimGame game, int sequence)
        { long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows); return rows.Single(row => row.Sequence == sequence); }
        private static void Branch(Check c, GameFactory factory, SimGame parent, EntityId worker, EntityId source, EntityId target, ResourceId supply)
        {
            ulong untouched = parent.Hash(); var game = factory.Load(parent.Save());
            var origin = TransformationContentContractTests.Entity(game, "service.evacuation-origin");
            var far = TransformationContentContractTests.Entity(game, "service.receiver.far");
            var courier = TransformationContentContractTests.Entity(game, "reserve.courier");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            PermanentRemovalTests.Submit(c, game, new ConfigureWorkPolicyCommand { Worker = courier, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, "service.evacuation-area", true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, game, new ConfigureRecurringPlanCommand { Entity = courier, Source = new Code("service.pending"),
                Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(60), Code.None) });
            PermanentRemovalTests.Submit(c, game, Export(origin, true));
            var view = game.Seat(0).View.Feature<LogisticsView>();
            c.Equal(WorkPolicyCodes.OutsideArea, view.PreviewHaul(origin, target, supply, Fix.FromInt(2), worker: courier).Blocker,
                "chosen receiver excludes pending carrier before its service rules are consulted");
            c.Equal(Code.None, view.ReadHaulTripServiceWindow(courier, out var pendingWindow), "actual never-serviced source window is inspectable");
            c.That(pendingWindow != null && !pendingWindow.Unrestricted && pendingWindow.Source == new Code("service.pending")
                && pendingWindow.Blocker == RecurringPlanCodes.MovementWindow, "no completed service yields the native expired movement allowance, not an outstanding-plan blocker");
            c.Equal(pendingWindow.Blocker, view.PreviewHaul(origin, far, supply, Fix.FromInt(2), worker: courier).Blocker,
                "discarded farther receiver actually consults the pending carrier service");
            var waiting = Evacuations(game.Observer.Feature<LogisticsView>(), origin).Single();
            c.That(waiting.Requested == Fix.FromInt(2) && waiting.SourceEndpoint == ResourceEndpointId.ForEntity(origin), "one actual file-authored evacuation retains endpoint and age");
            var inactive = factory.Load(game.Save());
            using (var closed = inactive.RecordReplay(limits))
            {
                PermanentRemovalTests.Submit(c, inactive, new SetInventorySightCommand { Hidden = true });
                PermanentRemovalTests.Submit(c, inactive, Export(origin, false));
                var published = Evacuations(inactive.Seat(0).View.Feature<LogisticsView>(), origin);
                c.That(published.Count == 1 && published[0].Haul.Status == HaulStatus.PolicyBlocked,
                    "disabled export remains visible because it never consults service candidates");
                var replay = new ReplayVerifier().Verify(factory, closed.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == inactive.Hash(), "actual private closed-origin policy replays");
            }
            var visible = new List<int>(); var hidden = new List<int>();
            game.Seat(0).Submit(new SetWorkerHeldCommand { Worker = worker, Held = false });
            foreach (bool diagnostics in new[] { false, true }) visible.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                Kind = LogisticsDispatchKind.Evacuation, Worker = worker, WaitingWorker = courier, Source = origin, Destination = target, Diagnostics = diagnostics }));
            game.Seat(0).Submit(new SetInventorySightCommand { Hidden = true });
            foreach (bool diagnostics in new[] { false, true }) hidden.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                Kind = LogisticsDispatchKind.Evacuation, Worker = worker, WaitingWorker = courier, Source = origin, Destination = target, Diagnostics = diagnostics }));
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual receiver policies and ordered release/query/visibility inbox checkpoint");
            PermanentRemovalTests.Step(game, twin);
            foreach (var current in new[] { game, twin })
            {
                foreach (int sequence in visible) c.That(Result(current, sequence).Accepted, "actual chosen receiver decision is visible before hiding");
                foreach (int sequence in hidden) c.That(!Result(current, sequence).Accepted && Result(current, sequence).Refusal == Codes.NotFound,
                    "discarded receiver's private service dependency withholds selected aggregate and whole diagnosis");
                var jobs = PermanentRemovalTests.Jobs(current).Where(row => row.IsHaul).ToArray();
                c.That(jobs.Length == 1 && jobs[0].Worker == worker && jobs[0].Source == origin && jobs[0].Destination == target
                    && jobs[0].RecoveryKind == LogisticsRecoveryKind.Evacuation, "native dispatch still chooses the actually feasible nearer receiver and serviced carrier");
                var actual = Evacuations(current.Observer.Feature<LogisticsView>(), origin).Single();
                c.That(actual.CreatedTick == waiting.CreatedTick && actual.SourceEndpoint == waiting.SourceEndpoint, "receiver ranking preserves original evacuation chronology and endpoint");
            }
            bool loaded = false; int updates = 0; var watch = Stopwatch.StartNew();
            while (updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                Fix cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply), delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                loaded |= cargo == Fix.FromInt(2);
                c.Equal(Fix.FromInt(19), LoadPolicyDisclosureTests.Stock(game, source, supply) + LoadPolicyDisclosureTests.Stock(game, origin, supply)
                    + LoadPolicyDisclosureTests.Stock(game, far, supply) + cargo + delivered + LoadPolicyDisclosureTests.Stock(game, courier, supply),
                    "finite original20 plus authored2 minus actually consumed3 service spares remains19");
                if (updates == 32 || updates == 127)
                {
                    if (!game.Seat(0).View.TryFindResource("fuel", out var fuel) || !game.Seat(0).View.TryGetEntity(worker, out var actor))
                        throw new InvalidOperationException("actual fuel and worker diagnostics required");
                    game.Observer.Feature<LogisticsView>().ReadHaulTripBudget(worker, out var budget);
                    Console.WriteLine("evacuation-service/diagnostic/at=" + game.Clock.StepIndex + "/origin=" + LoadPolicyDisclosureTests.Stock(game, origin, supply)
                        + "/cargo=" + cargo + "/delivered=" + delivered + "/far=" + LoadPolicyDisclosureTests.Stock(game, far, supply)
                        + "/position=" + ((PointSnapshot)actor.Spatial).Position + "/fuel=" + LoadPolicyDisclosureTests.Stock(game, worker, fuel)
                        + "/remaining=" + budget?.Travel.RemainingTicks + "/budget=" + budget?.Blocker + "/required="
                        + (budget == null ? "none" : string.Join(";", budget.RequiredStock.Lines.Select(row => row.Resource + ":" + row.Amount)))
                        + "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.ToilIndex
                            + "/" + row.Blocker + "/loaded=" + row.Loaded + "/worker=" + row.Worker + "/from=" + row.Source + "/to=" + row.Destination)));
                }
                if (cargo.IsZero && delivered == Fix.FromInt(2) && !PermanentRemovalTests.Jobs(game).Any(row => row.IsHaul)) break;
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "chosen receiver earns real two-unit loading movement and unload over game time");
            c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply), "evacuation does not pick up unrelated source stock");
            c.That(LoadPolicyDisclosureTests.Stock(game, origin, supply).IsZero && LoadPolicyDisclosureTests.Stock(game, far, supply).IsZero
                && LoadPolicyDisclosureTests.Stock(game, courier, supply).IsZero, "one original origin drains once; discarded receiver/carrier obtain nothing");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled receiver selection, policy and physical inventory restore");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "actual area outlet release hide and physical evacuation replay");
            c.Equal(untouched, parent.Hash(), "receiver-selection branches leave canonical physical-service session untouched");
            Console.WriteLine("evacuation-service/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs="
                + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
        private static void Run(Check c)
        {
            var factory = PhysicalTripServiceTests.Factory(new GameModule[] { new InventorySightModule(), new LogisticsRecoveryPolicyModule() },
                Json.Parse("[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                authorScenario: scenario => {
                    scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"activity\",\"target\":\"trip.pending-service\",\"initial_hidden\":false}"));
                    scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
                }, authorPowered: root => {
                    AutomaticServiceDisclosureTests.AuthorPendingService(root.Get("entities")[2].Get("components"));
                    StartupContentAuthoring.Position(root, "reserve.courier", JsonValue.Number("14"));
                    root.Get("world_setup_profiles")[0].Get("includes").Add(JsonValue.Of("service.evacuation-start"));
                }, extraDocuments: new Dictionary<string, string> {
                    ["pending-carrier-service.json"] = FrameworkProofContent.Read("pending-carrier-service.json"),
                    ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json"),
                    ["evacuation-service-disclosure.json"] = FrameworkProofContent.Read("evacuation-service-disclosure.json") });
            PhysicalTripServiceTests.Physical(c, factory, onServiced: (actual, game, worker, source, target, supply) => Branch(c, actual, game, worker, source, target, supply));
        }
    }
}
