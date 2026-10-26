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
    // Ordinary player inputs/public facts only; the command-boundary probe never performs gameplay.
    internal static class AutomaticServiceDisclosureTests
    {
        internal const string Case = "Automatic service disclosure: discarded pending carriers protect ready source-selected offers without changing physical dispatch";
        private static readonly Code Pending = new Code("service.pending");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<LogisticsDispatchOffer> Offers(LogisticsView view, EntityId target, ResourceId supply)
        { var rows = new List<LogisticsDispatchOffer>(); view.CopyQueue(rows); return rows.Where(row => row.Destination == target && row.Resource == supply).ToList(); }
        private static CommandResult Result(SimGame game, int sequence)
        { long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows); return rows.Single(row => row.Sequence == sequence); }
        private static void Branch(Check c, GameFactory factory, SimGame parent, EntityId worker, EntityId source, EntityId target, ResourceId supply)
        {
            ulong original = parent.Hash(); var game = factory.Load(parent.Save());
            var courier = TransformationContentContractTests.Entity(game, "reserve.courier");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, EmergencyPreemptionTests.ConfigureCommand(courier));
            PermanentRemovalTests.Submit(c, game, new ConfigureRecurringPlanCommand { Entity = courier, Source = Pending,
                Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(60), Code.None) });
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(courier, Pending, out var pending), "actual unserviced carrier source is inspectable before hiding");
            c.That(pending.Schedule.LastPlan == 0 && pending.RequestBlocker == RecurringPlanCodes.NoTarget,
                "absent authored home blocks future service, not fabricated completion or free repair");
            PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            PermanentRemovalTests.Submit(c, game, new AddRouteCommand { From = source, To = target, Resource = supply,
                Settings = new LogisticsRouteSettings(true, Fix.FromInt(2), 1) });
            PermanentRemovalTests.Submit(c, game, new ConfigureStockPolicyCommand { Store = target, Resource = supply,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(40), 1, LogisticsReserveAccess.Protected)) });
            var known = Offers(game.Seat(0).View.Feature<LogisticsView>(), target, supply);
            c.That(known.Count == 2 && known.Count(row => row.Route != 0) == 1, "actual waiting route and source-selected demand exist before release");
            int route = known.Single(row => row.Route != 0).Route;
            var inactive = factory.Load(game.Save());
            using (var stopped = inactive.RecordReplay(limits))
            {
                PermanentRemovalTests.Submit(c, inactive, new SetInventorySightCommand { Hidden = true });
                PermanentRemovalTests.Submit(c, inactive, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(2), 1) });
                var published = Offers(inactive.Seat(0).View.Feature<LogisticsView>(), target, supply);
                c.That(published.Count == 1 && published[0].Route == route && published[0].Haul.Status == HaulStatus.PolicyBlocked,
                    "disabled route remains visible because it never consults the private pending carrier; active demand stays opaque");
                var replay = new ReplayVerifier().Verify(factory, stopped.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == inactive.Hash(), "actual private disabled-route inputs replay");
            }
            var visible = new List<int>(); var hidden = new List<int>();
            game.Seat(0).Submit(new SetWorkerHeldCommand { Worker = worker, Held = false });
            foreach (bool diagnostics in new[] { false, true }) visible.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                Worker = worker, WaitingWorker = courier, Source = source, Destination = target, Diagnostics = diagnostics }));
            game.Seat(0).Submit(new SetInventorySightCommand { Hidden = true });
            foreach (bool diagnostics in new[] { false, true }) hidden.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                Worker = worker, WaitingWorker = courier, Source = source, Destination = target, Diagnostics = diagnostics }));
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "earned service, pending carrier and ordered decision probes save before assignment");
            PermanentRemovalTests.Step(game, twin);
            foreach (var current in new[] { game, twin })
            {
                foreach (int sequence in visible) c.That(Result(current, sequence).Accepted, "actual ready serviced-carrier route/demand and whole diagnostics are visible");
                foreach (int sequence in hidden) c.That(!Result(current, sequence).Accepted && Result(current, sequence).Refusal == Codes.NotFound,
                    "discarded private service dependency withholds ready offers/whole diagnostics without a result prefix");
                c.Equal(Code.None, current.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(worker, out var actual), "selected serviced carrier remains independently visible");
                c.That(actual != null && actual.Blocker.IsNone && !actual.Unrestricted, "selected carrier retains real earned service deadline");
                c.Equal(Codes.NotFound, current.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(courier, out _), "only the discarded carrier's service workflow is private");
                c.That(PermanentRemovalTests.Jobs(current).Any(row => row.IsHaul && row.Worker == worker && row.Destination == target),
                    "engine privately assigns the actually serviced carrier despite withholding the public aggregate decision");
            }
            Func<Command> disable = () => new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(2), 1) };
            c.Equal(Code.None, game.Seat(0).Preview(disable()), "normal route stop is admitted after dispatch");
            int stoppedRoute = game.Seat(0).Submit(disable()); twin.Seat(0).Submit(disable()); PermanentRemovalTests.Step(game, twin);
            c.That(Result(game, stoppedRoute).Accepted, "route stop prevents new trips without cancelling admitted physical cargo");
            int updates = 0; bool loaded = false; var watch = Stopwatch.StartNew();
            while (updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                var cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply); var delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                loaded |= cargo == Fix.FromInt(2);
                c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply) + cargo + delivered
                    + LoadPolicyDisclosureTests.Stock(game, courier, supply), "actual automatic delivery conserves finite post-service material");
                if (cargo.IsZero && delivered == Fix.FromInt(2) && !PermanentRemovalTests.Jobs(game).Any(row => row.IsHaul)) break;
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "actual automatic two-unit trip physically loads moves and unloads within original bounds");
            c.Equal(Fix.FromInt(15), LoadPolicyDisclosureTests.Stock(game, source, supply), "service consumed three and the one physical delivery removed two");
            c.Equal(Fix.Zero, LoadPolicyDisclosureTests.Stock(game, courier, supply), "discarded unserviced carrier never obtains cargo");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "private automatic selection and settled physical delivery restore");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "actual held release probes hide and automatic physical delivery replay");
            c.Equal(original, parent.Hash(), "automatic selection branches never mutate canonical physical-service session");
            Console.WriteLine("automatic-service/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs="
                + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
        internal static void AuthorPendingService(JsonValue parts)
        {
            parts.Get("worker").Get("roles").Add(JsonValue.Of("inspect"));
            parts.Set("haul_trip_policy", Json.Parse("{\"profile\":\"trip.pending\"}"));
            parts.Set("recurring_activity", Json.Parse("{\"id\":\"service.pending\",\"plan\":\"activity:trip.pending-service\",\"template\":null,\"cadence\":\"successful_completion\",\"trigger\":\"schedule\",\"dispatch_mode\":\"service_first\",\"target\":\"worker_home\",\"arguments\":null,\"facility_policy\":null,\"reserve_use\":\"none\",\"worker_nodes\":[\"service\"],\"enabled\":false,\"priority\":60,\"group\":\"pending.service\",\"operation_policy\":null,\"interval\":{\"numerator\":60,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[]}"));
        }
        private static void Run(Check c)
        {
            var factory = PhysicalTripServiceTests.Factory(new GameModule[] { new InventorySightModule() },
                Json.Parse("[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                authorScenario: scenario => scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"activity\",\"target\":\"trip.pending-service\",\"initial_hidden\":false}")),
                authorPowered: root => {
                    AuthorPendingService(root.Get("entities")[2].Get("components"));
                }, extraDocuments: new Dictionary<string, string> { ["pending-carrier-service.json"] = FrameworkProofContent.Read("pending-carrier-service.json") });
            PhysicalTripServiceTests.Physical(c, factory, onServiced: (actual, game, worker, source, target, supply) => Branch(c, actual, game, worker, source, target, supply));
        }
    }
}
