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
    // Existing funded fixture/JSON and public player inputs only, never consumption or movement logic.
    internal static class OperatingDisclosureTests
    {
        internal const string Case = "Operating disclosure: hidden fuel withholds complete motion work and trip facts with private command parity";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory()
            => HaulTripPolicyTests.Factory(edit: document => {
                var parts = document.Get("entities")[1].Get("components");
                var movement = parts.Get("operating_inputs").Clone(); var idle = movement.Clone();
                idle.Set("id", JsonValue.Of("hotel")); idle.Set("phase", JsonValue.Of("idle"));
                idle.Get("inputs")[0].Get("period").Set("numerator", JsonValue.Number("1000"));
                var inputs = JsonValue.NewArray(); inputs.Add(movement); inputs.Add(idle); parts.Set("operating_inputs", inputs);
            }, editPointScenario: scenario => {
                scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"resource\",\"target\":\"fuel\",\"initial_hidden\":false}"));
            }, pointModules: new GameModule[] { new InventorySightModule() },
                pointDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static OperatingInputEvaluation Inputs(SimGame game, EntityId worker, OperatingPhase phase)
        {
            OperatingInputEvaluation value;
            Code refusal = phase == OperatingPhase.Movement ? game.Observer.Feature<MotionView>().ReadOperatingInputs(worker, 0, out value)
                : game.Observer.Feature<WorkView>().ReadOperatingInputs(worker, phase, out value, 0);
            if (!refusal.IsNone) throw new InvalidOperationException("actual operating facts/" + refusal); return value;
        }
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            var refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual operating stock/" + refusal); return value.Stock;
        }
        private static HaulResourcesCommand Haul(EntityId worker, EntityId source, EntityId target, ResourceId supply)
            => new HaulResourcesCommand { Worker = worker, Source = source, Destination = target, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 };
        private static void Hidden(Check c, SimGame game, EntityId worker, EntityId source, EntityId target, ResourceId supply, bool active)
        {
            ulong hash = game.Hash(); long at = game.Clock.StepIndex;
            c.That(game.Seat(0).View.TryGetEntity(worker, out _) && game.Seat(0).View.TryFindResource("supply", out _)
                && !game.Seat(0).View.TryFindResource("fuel", out _), "owned vehicle and cargo stay visible while actual fuel metadata is hidden");
            var motion = game.Seat(0).View.Feature<MotionView>(); var work = game.Seat(0).View.Feature<WorkView>();
            c.Equal(Codes.NotFound, motion.ReadOperatingInputs(worker, 0, out var horizon), "zero horizon still protects declared fuel ports and stock");
            c.That(horizon == null, "no partially redacted operating horizon");
            c.Equal(Codes.NotFound, motion.ReadExecution(worker, out var execution), "whole movement execution protects nested operating and trip data");
            c.That(execution == null, "no private movement prefix");
            c.Equal(Codes.NotFound, work.ReadOperatingInputs(worker, OperatingPhase.Idle, out var idle, 0), "idle horizon protects the same actual fuel declaration");
            c.That(idle == null, "no private idle exposure/stock prefix");
            c.Equal(Code.None, work.ReadOperatingInputs(worker, OperatingPhase.Standby, out var unrelated, 0), "unselected empty phase remains authorized");
            c.That(unrelated.Providers.Count == 0 && unrelated.RequiredInputs.IsEmpty, "no fabricated input provider for an empty phase");
            c.That(Inputs(game, worker, OperatingPhase.Movement).RequiredInputs.IsEmpty && Inputs(game, worker, OperatingPhase.Movement).Providers.Count == 1,
                "actual zero horizon has no payable bill but still contains resource-bearing provider facts");
            if (active)
            {
                c.Equal(Codes.NotFound, game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripBudget(worker, out var budget), "acquired budget protects fuel floors and nested providers");
                c.That(budget == null, "no private acquired funding prefix");
                c.Equal(Code.None, game.Observer.Feature<LogisticsView>().ReadHaulTripBudget(worker, out var actual), "observer diagnoses original acquired budget");
                c.That(actual.ReserveFloor.Count > 0 && actual.Operating.Providers.Count == 1, "proof reaches original funded haul with real fuel references");
            }
            else
            {
                var preview = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker, priority: 100);
                c.Equal(HaulStatus.NotFound, preview.Status, "fresh player haul cannot expose hidden operating cost through a visible cargo resource");
                c.That(preview.TripBudget == null && !preview.Carrier.IsValid && !preview.Resource.IsValid && preview.Amount == Fix.Zero, "private fresh preview contains no budget/carrier/resource prefix");
                c.Equal(Codes.NotFound, game.Seat(0).Preview(Haul(worker, source, target, supply)), "ordinary command preview shares the same private funding decision");
            }
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadExecution(worker, out _), "foreign movement remains private");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadOperatingInputs(worker, OperatingPhase.Idle, out _, 0), "foreign phase remains private");
            c.Equal(hash, game.Hash(), "all player/observer phase and budget observations pure"); c.Equal(at, game.Clock.StepIndex, "horizons never advance the sole clock");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var request = new NewGameRequest { Scenario = "points", Seed = 11523, Seats = { Controller.Human, Controller.Human } };
            var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical fuel budget requires no business module");
            var worker = TransformationContentContractTests.Entity(game, "reserve.truck"); var source = TransformationContentContractTests.Entity(game, "source");
            var target = TransformationContentContractTests.Entity(game, "destination"); var supply = PermanentRemovalTests.Supply(game);
            c.That(game.Seat(0).View.TryFindResource("fuel", out var fuel), "authored physical fuel initially visible");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            void Observe()
            {
                if (game.Fault != null) throw new InvalidOperationException("operating disclosure fault/" + game.Fault);
                c.Equal(Fix.FromInt(20), Stock(game, source, supply) + Stock(game, target, supply) + Stock(game, worker, supply), "native finite cargo conservation");
                Fix burned = Inputs(game, worker, OperatingPhase.Movement).Providers.Aggregate(Fix.Zero, (sum, row) => sum + row.Demand.Consumed.AmountOf(fuel))
                    + Inputs(game, worker, OperatingPhase.Idle).Providers.Aggregate(Fix.Zero, (sum, row) => sum + row.Demand.Consumed.AmountOf(fuel));
                c.Equal(Fix.FromInt(12), burned + Stock(game, worker, fuel), "actual movement/idle exposures account for all finite fuel");
            }
            void Reach(SimGame twin, Func<bool> done, string stage)
            {
                while (!done() && game.Clock.StepIndex - began < 128 && timer.Elapsed.TotalSeconds < 25)
                { PermanentRemovalTests.Step(game, twin); Observe(); }
                if (!done()) throw new InvalidOperationException(stage + "/128updates25seconds/" + game.Clock.StepIndex + "/"
                    + string.Join(";", PermanentRemovalTests.Jobs(game).Select(job => job.Id + "/" + job.State + "/" + job.Blocker)));
            }
            var retained = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker, priority: 100);
            c.That(retained.Status == HaulStatus.Ready && retained.TripBudget.RequiredStock.AmountOf(fuel).IsPositive && retained.TripBudget.ReserveFloor.AmountOf(fuel) == Fix.FromInt(2), "actual selected preview contains funded fuel plus original outlet floor");
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true }); Hidden(c, game, worker, source, target, supply, false);
            var denied = factory.Load(game.Save()); var deniedTwin = factory.Load(game.Save()); using var denialRecording = denied.RecordReplay(limits);
            int sequence = denied.Seat(0).Submit(Haul(worker, source, target, supply));
            c.Equal(sequence, deniedTwin.Seat(0).Submit(Haul(worker, source, target, supply)), "private command retains the saved pending identity");
            PermanentRemovalTests.Step(denied, deniedTwin);
            long cursor = 0; var results = new List<CommandResult>(); denied.Seat(0).ReadResults(ref cursor, results); var result = results.Single(row => row.Sequence == sequence);
            c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed hidden-cost command shares the private preview refusal");
            c.That(Stock(denied, source, supply) == Fix.FromInt(20) && Stock(denied, target, supply) == Fix.Zero
                && !PermanentRemovalTests.Jobs(denied).Any(job => job.IsHaul && job.Worker == worker), "private command creates no successful source/job/claim prefix");
            c.Equal(denied.Hash(), deniedTwin.Hash(), "refused private command continuation matches restored twin");
            c.That(new ReplayVerifier().Verify(factory, denialRecording.Finish(), limits).Success, "actual private command result prefix replays");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex;
            PermanentRemovalTests.Step(game, twin); Hidden(c, twin, worker, source, target, supply, false);
            c.Equal(stopped, game.Clock.StepIndex, "paused hidden horizons and saved fuel preserve game time"); game.Clock.Resume(); twin.Clock.Resume();
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, Haul(worker, source, target, supply));
            Reach(twin, () => Stock(game, worker, supply) == Fix.FromInt(4) && PermanentRemovalTests.At(game, worker) > Fix.One, "physically loaded funded travel");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = true });
            game.Clock.Pause(); Hidden(c, game, worker, source, target, supply, true); twin = factory.Load(game.Save()); twin.Clock.Pause();
            Hidden(c, twin, worker, source, target, supply, true); c.Equal(game.Hash(), twin.Hash(), "hidden acquired trip/exposure/stock restore exactly");
            game.Clock.Resume(); twin.Clock.Resume();
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            Reach(twin, () => Stock(game, target, supply) == Fix.FromInt(4) && !PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Worker == worker), "native physical delivery");
            Observe(); c.That(Stock(game, worker, supply) == Fix.Zero && Stock(game, source, supply) == Fix.FromInt(16), "four real cargo units delivered once");
            c.That(retained.TripBudget.RequiredStock.AmountOf(fuel).IsPositive && retained.TripBudget.ReserveFloor.AmountOf(fuel) == Fix.FromInt(2), "old authorized budget stays detached after privacy and consumption");
            c.Equal(game.Hash(), twin.Hash(), "saved physical movement funding and delivery converge"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical fuel/stock checkpoint exact");
            c.Equal(isolated, idle.Hash(), "interleaved visibility/fuel session isolated");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "full main replay retains every advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "full physical fuel/visibility/hauling continuation replays");
            Console.WriteLine("operating-disclosure/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount
                + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16") + "/fuel=" + Stock(game, worker, fuel));
        }
    }
}
