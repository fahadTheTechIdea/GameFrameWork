using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // JSON composition plus ordinary player input/public immutable engine views only.
    internal static class ContinuousPreviewStateTests
    {
        internal const string Case = "Continuous execution preview: controls waits pause and stop reflect authoritative state";
        private static readonly Code Capability = new Code("process");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static TransformationManagementView View(SimGame g) => g.Seat(0).View.Feature<TransformationManagementView>();
        private static EntityId Factory(SimGame g) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == "flow.factory").Id;
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 814, Seats = { Controller.Human, Controller.Human }, Mods = { "proof.transformation-supply" } });
        private static TransformationExecutionPreview Read(Check c, SimGame g)
        {
            ulong hash = g.Hash(); long step = g.Clock.StepIndex;
            c.Equal(Code.None, View(g).ReadExecutionPreview(Factory(g), Capability, "flow.refine", out var facts), "actual named continuous preview admitted");
            c.Equal(hash, g.Hash(), "preview mutates no control progress resource RNG or state");
            c.Equal(step, g.Clock.StepIndex, "preview never drives time");
            if (facts == null) throw new InvalidOperationException("missing actual preview");
            c.That(facts.NominalCycle == null && facts.RemainingProductiveTime == null && facts.ProductiveCompletionStep == null,
                "every continuous status excludes invented batch completion/ETA");
            return facts;
        }
        private static TransformationSnapshot Current(SimGame g)
        { if (!View(g).TryGetTransformation(Factory(g), Capability, out var p)) throw new InvalidOperationException("missing named processor"); return p; }
        private static void Update(SimGame g)
        { g.Update(); if (g.Fault != null) throw new InvalidOperationException("continuous preview clock=" + g.Clock.StepIndex + " " + g.Fault.Message); }
        private static void Submit(Check c, SimGame g, Command command)
        {
            c.Equal(Code.None, g.Seat(0).Preview(command), "ordinary command uses original engine evaluator");
            int sequence = g.Seat(0).Submit(command); Update(g);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "normal player command applies during Update");
        }
        private static void Until(SimGame g, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 512 && !ready(); i++) Update(g);
            if (!ready()) { var p = Current(g); throw new InvalidOperationException(stage + " clock=" + g.Clock.StepIndex + " phase=" + p.Phase + " blocker=" + p.Blocker + " progress=" + p.Progress); }
        }
        internal static void Physical(Check c, bool half)
        {
            var factory = ContinuousTransformationTests.Factory(basisEdit: root =>
            {
                if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            });
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var replay = game.RecordReplay(limits);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "continuous work/control require no business module");
            var initial = Read(c, game); c.Equal(ProductiveEstimateStatus.NotHead, initial.EstimateStatus, "unqueued continuous intent is not active production");
            Submit(c, game, new EnqueueTransformationCommand { Transformer = Factory(game), Capability = Capability, Transformation = "flow.refine" });
            Until(game, () => Current(game).Flow.Produced.Count > 0, "physical cumulative output");
            var running = Read(c, game);
            c.Equal(ProductiveEstimateStatus.Continuous, running.EstimateStatus, "healthy actual working flow is continuous");
            c.That(running.Current.Phase == TransformationPhase.Working && running.CurrentBlocker == Code.None && running.Control.Enabled,
                "status agrees with authoritative work/control/blocker");
            c.That(running.Inputs.IsEmpty && running.Outputs.IsEmpty && running.InputRates.Count == 1 && running.OutputRates.Count == 2 && running.OperatingRates.Count == 1,
                "immutable rates, not fabricated batch yields");
            c.That(running.InputRates[0].Quantity == Fix.FromInt(2) && running.InputRates[0].PeriodNumerator == 1 && running.InputRates[0].PeriodUnit == DurationUnit.Minute,
                "actual JSON input throughput coefficient/period");
            var retainedProgress = running.Current.Progress;
            game.Clock.Pause(); ulong pausedHash = game.Hash(); long pausedAt = game.Clock.StepIndex;
            c.Equal(ProductiveEstimateStatus.Paused, Read(c, game).EstimateStatus, "continuous mode cannot bypass sole paused clock");
            Update(game); c.Equal(pausedHash, game.Hash(), "paused flow consumes/delivers no additional resources"); c.Equal(pausedAt, game.Clock.StepIndex, "one paused game time");
            game.Clock.Resume();
            Submit(c, game, new ConfigureOperationCommand { Entity = Factory(game), Operation = Capability, Enabled = false, Target = Fix.Half });
            var disabled = Read(c, game);
            c.Equal(ProductiveEstimateStatus.Blocked, disabled.EstimateStatus, "continuous mode cannot bypass disabled operation");
            c.Equal((Code?)OperationCodes.Disabled, disabled.CurrentBlocker, "disabled cause remains actual operation gate");
            c.Equal(retainedProgress, disabled.Current.Progress, "disabling advances no work");
            game.Clock.Pause(); c.Equal(ProductiveEstimateStatus.Blocked, Read(c, game).EstimateStatus, "disabled blocker deterministically precedes pause"); game.Clock.Resume();
            var restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "disabled/paused-history state saves exactly");
            c.Equal(ProductiveEstimateStatus.Blocked, Read(c, restored).EstimateStatus, "saved provider status agrees after restore");
            Submit(c, game, new ConfigureOperationCommand { Entity = Factory(game), Operation = Capability, Enabled = true, Target = Fix.FromRatio(3, 2) });
            c.Equal(ProductiveEstimateStatus.Continuous, Read(c, game).EstimateStatus, "real bounded target/restart restores active flow status");
            Until(game, () => !Current(game).Blocker.IsNone, "finite physical feed/operating resource wait");
            var blocked = Read(c, game); c.Equal(ProductiveEstimateStatus.Blocked, blocked.EstimateStatus, "continuous mode cannot hide actual material/output/operating wait");
            c.That(blocked.CurrentBlocker.HasValue && !blocked.CurrentBlocker.Value.IsNone, "actual cause accompanies blocked status");
            var twin = factory.Load(game.Save()); Update(game); Update(twin); c.Equal(game.Hash(), twin.Hash(), "blocked saved physical continuation identical");
            c.Equal(blocked.Current.Progress, Current(game).Progress, "blocked resource wait earns no work");
            c.Equal(ProductiveEstimateStatus.Blocked, Read(c, twin).EstimateStatus, "restored wait reports same engine status");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadExecutionPreview(Factory(game), Capability, "flow.refine", out var hidden), "foreign processor facts refused");
            c.That(hidden == null, "foreign read exposes no status/rate prefix");
            c.Equal(Code.None, View(game).PreviewCancel(Factory(game), Capability, 0), "ordinary stop management preview");
            Submit(c, game, new CancelTransformationCommand { Transformer = Factory(game), Capability = Capability, Index = 0 });
            c.Equal(ProductiveEstimateStatus.NotHead, Read(c, game).EstimateStatus, "stop retires actual continuous intent");
            c.That(Current(game).QueueCount == 0 && Current(game).Tools.Sources.Count == 0, "stop releases original queue/tool commitments");
            c.That(running.EstimateStatus == ProductiveEstimateStatus.Continuous && running.Current.Progress.Equals(retainedProgress) && running.Control.Enabled,
                "retained running projection is not mutated by pause/control/wait/stop");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed stop checkpoint stable");
            c.Equal(idleHash, idle.Hash(), "interleaved unrelated session remains untouched");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "all actual queue/control/clock/stop commands replay without preview events");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual preview engine identity");
            Physical(c, false); Physical(c, true);
            foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true })
                RecipeDisclosureTests.Physical(c, direct, "resource", half);
        }
    }
}
