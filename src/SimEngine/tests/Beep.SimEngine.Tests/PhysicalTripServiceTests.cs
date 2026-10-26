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
    // Authored JSON, ordinary player inputs and immutable facts only; no driver-owned gameplay rules.
    internal static class PhysicalTripServiceTests
    {
        internal const string Case = "Physical trip service: nonempty source bindings earn timed material service before saved physical delivery";
        private static readonly Code Service = new Code("service.interval");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(IReadOnlyList<GameModule> extraModules = null, JsonValue extraDeclarations = null,
            Action<JsonValue> authorScenario = null, Action<JsonValue> authorPowered = null, IReadOnlyDictionary<string, string> extraDocuments = null)
        {
            var documents = new Dictionary<string, string> { ["trip-physical-service.json"] = FrameworkProofContent.Read("trip-physical-service.json") };
            if (extraDocuments != null) foreach (var document in extraDocuments) documents.Add(document.Key, document.Value);
            return TripRuleObservationTests.Factory(0, new FleetTripProofModule(), out _, authorRules: rules =>
                rules.Get("independent_haul_trip_profiles")[0].Set("service_sources", Json.Parse("[\"service.interval\"]")), authorCarrier: parts =>
                parts.Set("recurring_activity", Json.Parse("{\"id\":\"service.interval\",\"plan\":\"activity:trip.physical-service\",\"template\":null,\"cadence\":\"successful_completion\",\"trigger\":\"schedule\",\"dispatch_mode\":\"service_first\",\"target\":\"worker_home\",\"arguments\":null,\"facility_policy\":null,\"reserve_use\":\"none\",\"worker_nodes\":[\"service\"],\"enabled\":false,\"priority\":60,\"group\":\"trip.service\",\"operation_policy\":null,\"interval\":{\"numerator\":60,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[]}")),
                additionalDocuments: documents,
                extraModules: extraModules, extraDeclarations: extraDeclarations, authorScenario: authorScenario, authorPowered: authorPowered);
        }
        private static RecurringPlanView Recurring(SimGame game, EntityId worker)
        {
            var code = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(worker, Service, out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual service source: " + code); return value;
        }
        private static PlanView Plan(SimGame game, long id)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows.Single(row => row.Id == id); }
        private static ServiceMovementWindow Window(SimGame game, EntityId worker)
        {
            var code = game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(worker, out var value);
            if (!code.IsNone || value == null) throw new InvalidOperationException("actual service window: " + code); return value;
        }
        private static void Both(Check c, SimGame game, SimGame twin, Func<Command> command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command()), "normal saved player admission");
            int sequence = game.Seat(0).Submit(command()); twin.Seat(0).Submit(command()); PermanentRemovalTests.Step(game, twin);
            long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows);
            c.That(rows.Single(row => row.Sequence == sequence).Accepted, "normal saved command executes through engine");
        }
        private static void Run(Check c) => Physical(c);
        internal static void Physical(Check c, GameFactory factory = null, Action<GameFactory, SimGame, EntityId, EntityId, EntityId, ResourceId> onPartial = null,
            Action<GameFactory, SimGame, EntityId, EntityId, EntityId, ResourceId> onServiced = null)
        {
            factory ??= Factory(); var idle = TripAdmissionObservationTests.New(factory); ulong isolated = idle.Hash();
            var game = TripAdmissionObservationTests.New(factory);
            var worker = TransformationContentContractTests.Entity(game, "reserve.truck");
            var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination");
            var supply = PermanentRemovalTests.Supply(game); var view = game.Seat(0).View.Feature<LogisticsView>();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical service needs no business module");
            c.Equal(Code.None, view.ReadHaulTripServiceSources(worker, out var bindings), "actual nonempty service metadata is visible");
            c.That(bindings.Count == 1 && bindings[0] == Service, "actual independent trip profile selects the real source contract");
            var disabled = Window(game, worker); c.That(disabled.Unrestricted, "disabled source has no invented deadline");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, new SetHomeCommand { Worker = worker, Home = source });
            PermanentRemovalTests.Submit(c, game, new ConfigureRecurringPlanCommand { Entity = worker, Source = Service,
                Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(60), Code.None) });
            var pending = Window(game, worker);
            c.That(!pending.Unrestricted && pending.Source == Service && !pending.Blocker.IsNone, "enabled never-serviced machine is genuinely pending");
            c.That(view.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker).Status != HaulStatus.Ready,
                "fresh hauling cannot ignore actual pending service");
            c.Equal(0L, Recurring(game, worker).Schedule.LastCompletedPlan, "posting and requesting never earn success");
            bool sawTools = false, savedDuringWork = false; ActivityNodeView? earned = null; SimGame twin = null;
            var watch = Stopwatch.StartNew(); int updates = 0;
            while (Recurring(game, worker).Schedule.LastCompletedPlan == 0 && updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                var current = Recurring(game, worker);
                var total = LoadPolicyDisclosureTests.Stock(game, source, supply) + LoadPolicyDisclosureTests.Stock(game, target, supply) + LoadPolicyDisclosureTests.Stock(game, worker, supply);
                c.That(total == Fix.FromInt(20) || total == Fix.FromInt(17), "only authored three spares can leave finite supply during service");
                c.Equal(Code.None, view.ReadDiagnostics(source, supply, out var stock), "source reservations are engine-calculated facts");
                sawTools |= stock.OutboundPromised == Fix.FromInt(5) || stock.OutboundPromised == Fix.FromInt(2);
                if (current.Schedule.LastPlan > 0)
                {
                    var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Plan(game, current.Schedule.LastPlan).Order, nodes);
                    foreach (var node in nodes.Where(row => row.Id == "service"))
                    {
                        if (node.State == NodeState.Done) earned = node;
                        if (!savedDuringWork && node.ProductiveSteps > 0 && node.ProductiveSteps < node.RequiredTicks)
                        {
                            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "checkpoint captures actual partial staffed service"); savedDuringWork = true;
                            onPartial?.Invoke(factory, game, worker, source, target, supply);
                        }
                    }
                }
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(updates < 128 && watch.ElapsedMilliseconds < 25000, "physical service and acknowledgement complete within bounded updates");
            c.That(savedDuringWork && sawTools && earned.HasValue, "real staffed work, tool hold and mid-work continuation were observed");
            if (!earned.HasValue || twin == null) throw new InvalidOperationException("missing actual earned service/work checkpoint");
            c.That(earned.Value.CompletedBy == worker && earned.Value.ProductiveSteps == 12 && earned.Value.Progress.Complete(earned.Value.RequiredTicks),
                "the actual bound machine earns all authored productive work");
            var completed = Recurring(game, worker); var plan = Plan(game, completed.Schedule.LastCompletedPlan);
            c.That(plan.State == PlanState.Done && plan.EndedTick - plan.CreatedTick >= 18 && completed.LastCompletedTick == plan.EndedTick,
                "whole staffed and timed stages, not request time, earn service completion");
            var ready = Window(game, worker);
            c.That(ready.Source == Service && ready.Blocker.IsNone && !ready.Unrestricted && ready.DueUpdateStep.Value == completed.LastCompletedTick.Value + 60,
                "actual completed plan anchors the finite game-clock service deadline");
            c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply), "three spares consumed once, held tools retained");
            c.Equal(Code.None, view.ReadDiagnostics(source, supply, out var released), "post-service stock can be inspected");
            c.Equal(Fix.Zero, released.OutboundPromised, "completed service releases tool reservations");
            onServiced?.Invoke(factory, game, worker, source, target, supply);
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long at = game.Clock.StepIndex;
            PermanentRemovalTests.Step(game, twin); c.That(game.Hash() == paused && game.Clock.StepIndex == at, "sole-clock pause freezes earned deadline and all work");
            game.Clock.Resume(); twin.Clock.Resume();
            var fitting = view.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker);
            c.Equal(HaulStatus.Ready, fitting.Status,
                "actual service completion, not a driver override, reopens physical hauling");
            if (!fitting.Travel?.RemainingTicks.HasValue ?? true) throw new InvalidOperationException("actual bounded trip duration required");
            long travel = fitting.Travel.Value.RemainingTicks.Value;
            c.That(travel > 2, "authored geography has a real multi-update trip");
            var shortInterval = GameDuration.FromSteps(game.Clock.StepIndex - completed.LastCompletedTick.Value + travel, game.Clock.Time);
            Both(c, game, twin, () => new ConfigureRecurringPlanCommand { Entity = worker, Source = Service,
                Settings = new RecurringPlanSettings(true, 60, shortInterval, Code.None) });
            var limited = Window(game, worker);
            c.That(limited.Blocker.IsNone && limited.RemainingUpdateSteps == travel - 1
                && limited.CheckMovement(travel) == RecurringPlanCodes.MovementWindow, "engine-calculated trip would cross actual earned service deadline");
            c.Equal(RecurringPlanCodes.MovementWindow, view.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker).Blocker,
                "typed haul preview uses the same finite source window");
            var refused = factory.Load(game.Save());
            using (var refusalRecording = refused.RecordReplay(limits))
            {
                var command = new HaulResourcesCommand { Source = source, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(4) };
                c.Equal(RecurringPlanCodes.MovementWindow, refused.Seat(0).Preview(command), "ordinary command preview matches typed service refusal");
                int sequence = refused.Seat(0).Submit(command); refused.Update();
                long cursor = 0; var results = new List<CommandResult>(); refused.Seat(0).ReadResults(ref cursor, results);
                var result = results.Single(row => row.Sequence == sequence);
                c.That(!result.Accepted && result.Refusal == RecurringPlanCodes.MovementWindow, "executed request cannot bypass earned deadline");
                c.That(LoadPolicyDisclosureTests.Stock(refused, source, supply) == Fix.FromInt(17)
                    && LoadPolicyDisclosureTests.Stock(refused, worker, supply).IsZero, "refused trip creates no cargo or material prefix");
                var refusalReplay = new ReplayVerifier().Verify(factory, refusalRecording.Finish(), limits);
                c.That(refusalReplay.Success && refusalReplay.ActualHash == refused.Hash(), "actual deadline-refused input replays from service checkpoint");
            }
            Both(c, game, twin, () => new ConfigureRecurringPlanCommand { Entity = worker, Source = Service,
                Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(60), Code.None) });
            c.That(Recurring(game, worker).LastCompletedTick == completed.LastCompletedTick
                && Window(game, worker).DueUpdateStep == completed.LastCompletedTick + 60, "policy edit preserves actual earned service history, never fabricates new completion");
            Both(c, game, twin, () => new HaulResourcesCommand { Source = source, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(4) });
            bool loaded = false; watch.Restart(); updates = 0;
            while (updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                Fix cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply), delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                loaded |= cargo == Fix.FromInt(4);
                c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply) + delivered + cargo, "post-service physical delivery conserves remaining stock");
                if (delivered == Fix.FromInt(4) && cargo.IsZero && !PermanentRemovalTests.Jobs(game).Any(row => row.IsHaul && row.Worker == worker)) break;
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "serviced machine really loads moves and unloads four units");
            c.Equal(Fix.FromInt(13), LoadPolicyDisclosureTests.Stock(game, source, supply), "source accounts for service and delivery exactly");
            c.Equal(completed.Schedule.LastCompletedPlan, Recurring(game, worker).Schedule.LastCompletedPlan, "fitting trip does not invent another service completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled actual service/history/cargo checkpoint restores");
            c.Equal(isolated, idle.Hash(), "service and saved physical work leave interleaved idle session unchanged");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "actual commands service work material use deadline and delivery replay");
            Console.WriteLine("physical-trip-service/full/0->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
    }
}
