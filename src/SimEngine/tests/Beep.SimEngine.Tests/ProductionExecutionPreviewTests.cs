using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // Only composition authoring and ordinary player commands/views/Update/save. No production execution in driver.
    internal static class ProductionExecutionPreviewTests
    {
        internal const string Case = "Production execution previews: exact productive estimates and detached authored throughput reject faulted reads";
        private static readonly Code Operation = new Code("space.operation");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static TransformationManagementView View(SimGame game, int seat = 0)
            => game.Seat(seat).View.Feature<TransformationManagementView>();
        private static TransformationExecutionPreview Read(Check c, SimGame game, string actor = "smelter", string recipe = "space.smelt.planetx")
        {
            ulong before = game.Hash();
            ITransformationExecutionView view = View(game);
            c.Equal(Code.None, view.ReadExecutionPreview(Actor(game, actor), Operation, recipe, out var facts), "actual named preview contract");
            c.Equal(before, game.Hash(), "preview performs no productive work/supply requests/RNG mutation");
            return facts;
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " phase="
                + Process(game, "smelter").Phase + " blocker=" + Process(game, "smelter").Blocker + " progress=" + Process(game, "smelter").Progress);
        }
        private static void Controls(JsonValue world)
        {
            foreach (var entity in Enumerable.Range(0, world.Get("entities").Count).Select(i => world.Get("entities")[i]))
            {
                var processor = entity.Get("components").Get("transformer");
                if (processor == null) continue;
                processor.Get("operation").Set("minimum", JsonValue.Number("1"));
                processor.Get("operation").Set("maximum", JsonValue.Number("2"));
            }
        }
        private static void Configure(Check c, SimGame game, bool enabled, Fix rate)
        {
            var command = new ConfigureOperationCommand { Entity = Actor(game, "smelter"), Operation = Operation, Enabled = enabled, Target = rate };
            c.Equal(Code.None, game.Seat(0).Preview(command), "actual authored bounded operation preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary operation command applies during Update");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current protocol build");
            c.That(WorkProgress.Zero.TryRemainingUpdates(Fix.FromRatio(3, 2), 6, out var whole) && whole == 4, "exact rate1.5 completes six work steps in four updates");
            c.That(new WorkProgress(1, 1u << 31).TryRemainingUpdates(Fix.FromRatio(3, 2), 6, out var fractional)
                && fractional == 3, "fractional earned progress retained in exact ceiling");
            c.That(!WorkProgress.Zero.TryRemainingUpdates(Fix.FromRaw(1), long.MaxValue, out _), "huge slow work has typed range absence");
            c.That(!WorkProgress.Zero.TryRemainingUpdates(Fix.Zero, 6, out _), "zero work rate never invents ETA");
            c.That(new WorkProgress(6).TryRemainingUpdates(Fix.Zero, 6, out var done) && done == 0, "already earned work has zero remaining work, not output settlement");
            var factory = Factory(worldEdit: Controls); var game = New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var idle = Read(c, game);
            c.That(idle.EstimateStatus == ProductiveEstimateStatus.NotHead && idle.CurrentBlocker == null && !idle.ProductiveCompletionStep.HasValue,
                "unstarted intent has no current blocker or guessed supply ETA");
            c.That(game.Clock.Time.Ticks(idle.NominalCycle.Value) == 6 && game.Clock.Time.Ticks(idle.AuthoredDuration) == 6
                && idle.Inputs.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10)
                && idle.Outputs.AmountOf(Resource(game, "alloy")) == Fix.FromInt(6)
                && idle.Outputs.AmountOf(Resource(game, "slag")) == Fix.FromInt(4)
                && idle.Tools.AmountOf(Resource(game, "crucible")) == Fix.One && idle.ToolConsumption.IsEmpty, "exact batch yields per thirty-second nominal cycle, tools separate");
            c.That(idle.InputRates.Count == 0 && idle.OutputRates.Count == 0 && idle.OperatingRates.Count == 0
                && idle.MonetaryCost.Applicability == MonetaryCostApplicability.NotAuthored, "no substituted rates/budget/account requirement");
            c.Equal(Codes.NotFound, View(game, 1).ReadExecutionPreview(Actor(game, "smelter"), Operation, "space.smelt.planetx", out var foreign), "private preview refuses");
            c.That(foreign == null, "no private prefix");
            c.Equal(Codes.UnknownDef, View(game).ReadExecutionPreview(Actor(game, "smelter"), Operation, "missing.recipe", out var unknown), "unknown authored recipe refuses");
            c.That(unknown == null, "no malformed recipe substitute");
            c.That(TestKit.Order(game, 0, Queue(game, "smelter", "space.smelt.planetx")).Accepted, "normal physical smelt request");
            var supply = Read(c, game);
            c.That(!supply.ProductiveCompletionStep.HasValue && supply.EstimateStatus == ProductiveEstimateStatus.Blocked
                && supply.Current.Phase == TransformationPhase.Supplying, "physical supply wait never gains invented completion time");
            var twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game, "smelter").Phase == TransformationPhase.Working
                && Process(game, "smelter").Progress.WholeTicks > 0, "physical startup");
            var retained = Read(c, game);
            c.That(retained.EstimateStatus == ProductiveEstimateStatus.Conditional
                && retained.ProductiveCompletionStep == game.Clock.StepIndex + 5
                && game.Clock.Time.Ticks(retained.RemainingProductiveTime.Value) == 5, "actual one-step earned work predicts five remaining productive updates only");
            Configure(c, game, false, Fix.One); twin = factory.Load(game.Save());
            var disabled = Read(c, game);
            c.That(disabled.EstimateStatus == ProductiveEstimateStatus.Blocked && disabled.CurrentBlocker == OperationCodes.Disabled
                && !disabled.ProductiveCompletionStep.HasValue && disabled.Current.Progress.Equals(retained.Current.Progress)
                && disabled.Current.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10), "disabled work keeps actual WIP/progress without ETA");
            c.Equal(OperationCodes.TargetBounds, game.Seat(0).Preview(new ConfigureOperationCommand { Entity = Actor(game, "smelter"),
                Operation = Operation, Enabled = true, Target = Fix.Zero }), "native positive work-rate authoring refuses zero target, no compatibility default");
            Configure(c, game, true, Fix.FromRatio(3, 2)); twin = factory.Load(game.Save());
            var changed = Read(c, game);
            c.That(changed.EstimateStatus == ProductiveEstimateStatus.Conditional
                && changed.Current.Progress.WholeTicks == 2 && changed.Current.Progress.Fraction == (1u << 31)
                && game.Clock.Time.Ticks(changed.NominalCycle.Value) == 4
                && game.Clock.Time.Ticks(changed.RemainingProductiveTime.Value) == 3, "rate1.5 and current fractional progress share original exact math/session quantum");
            game.Clock.Pause(); twin.Clock.Pause(); ulong pauseHash = game.Hash();
            var paused = Read(c, game); Step(game, twin);
            c.That(paused.EstimateStatus == ProductiveEstimateStatus.Paused && !paused.ProductiveCompletionStep.HasValue
                && game.Hash() == pauseHash, "paused sole clock removes schedule estimate without altering earned facts");
            game.Clock.Resume(); twin.Clock.Resume();
            long predicted = Read(c, game).ProductiveCompletionStep.Value;
            Reach(game, twin, () => Process(game, "smelter").QueueCount == 0, "productive completion");
            c.Equal(predicted, game.Clock.StepIndex, "conditional productive estimate matches native completion when unchanged and no output wait");
            c.That(Stock(game, "smelter", "alloy") == Fix.FromInt(6) && Stock(game, "smelter", "slag") == Fix.FromInt(4)
                && retained.Current.Progress.WholeTicks == 1 && retained.Current.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10), "one native whole conversion, retained preview detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "saved completed preview/queue identity");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "preview reads add no commands/RNG/replay events");
            Continuous(c); Faults(c);
        }
        private static void Continuous(Check c)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("planet-x-recipes.json")); var recipe = authored.Get("transformations")[0];
            recipe.Set("mode", JsonValue.Of("continuous")); recipe.Set("cancel_policy", JsonValue.Of("stop"));
            recipe.Set("duration", JsonValue.Null()); recipe.Set("tool_consumption", JsonValue.NewArray());
            recipe.Set("inputs", Json.Parse("[{\"resource\":\"space.planetx.ore\",\"quantity\":2,\"period\":{\"numerator\":5,\"denominator\":1,\"unit\":\"second\"}}]"));
            recipe.Set("outputs", Json.Parse("[{\"resource\":\"space.alloy\",\"quantity\":1,\"period\":{\"numerator\":5,\"denominator\":1,\"unit\":\"second\"}},{\"resource\":\"space.slag\",\"quantity\":1,\"period\":{\"numerator\":5,\"denominator\":1,\"unit\":\"second\"}}]"));
            var factory = Factory(authored.ToString(), worldEdit: Controls); var game = New(factory);
            c.That(TestKit.Order(game, 0, Queue(game, "smelter", "space.smelt.planetx")).Accepted, "authored continuous ordinary command");
            Reach(game, null, () => Process(game, "smelter").Flow.Produced.AmountOf(Resource(game, "alloy")) > Fix.Zero, "physical continuous startup");
            var preview = Read(c, game);
            c.That(preview.Mode == TransformationMode.Continuous && preview.EstimateStatus == ProductiveEstimateStatus.Continuous
                && preview.NominalCycle == null && preview.RemainingProductiveTime == null && preview.ProductiveCompletionStep == null,
                "continuous process has authored throughput, not fabricated batch completion");
            c.That(preview.Inputs.IsEmpty && preview.Outputs.IsEmpty && preview.InputRates.Count == 1 && preview.OutputRates.Count == 2
                && preview.InputRates[0].Quantity == Fix.FromInt(2) && preview.OutputRates.All(r => r.Quantity == Fix.One
                    && r.PeriodNumerator == 5 && r.PeriodDenominator == 1 && r.PeriodUnit == DurationUnit.Second), "exact mass throughput coefficients and period from original JSON contracts");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Read(c, game).Current.Flow.Produced.AmountOf(Resource(game, "alloy")) >= preview.Current.Flow.Produced.AmountOf(Resource(game, "alloy")), "detached continuous facts/saved flow continue without duplicate owner");
        }
        private static void Faults(Check c)
        {
            var probe = new PriorActivityProofModule(); var factory = Factory(proof: probe);
            var game = New(factory, "production-preview-fault"); var healthy = New(factory, "production-preview-fault");
            c.That(game.Modules.Contains(probe.Id), "actual negative update module explicitly selected");
            var retained = Read(c, game); var own = View(game); var other = View(game, 1);
            probe.FaultNextUpdate = true; game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains("proof.prior.first"), "actual first update fault");
            var first = game.Fault;
            foreach (var view in new[] { own, other })
                foreach (var id in new[] { Actor(healthy, "smelter"), EntityId.None })
                {
                    c.Throws<InvalidOperationException>(() => view.ReadExecutionPreview(id, Operation, "space.smelt.planetx", out _), "retained execution read rejects before private/missing fallback");
                    c.Throws<InvalidOperationException>(() => view.TryGetTransformation(id, Operation, out _), "retained current process rejects early fault");
                    c.Throws<InvalidOperationException>(() => view.PreviewEnqueue(id, Operation, "space.smelt.planetx", 1, false, default), "retained enqueue preview rejects early fault");
                }
            c.Throws<InvalidOperationException>(() => own.TransformationEntityOutput("missing.recipe"), "content early absence cannot bypass fault");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted preview session cannot save");
            c.That(ReferenceEquals(first, game.Fault) && retained.Current.QueueCount == 0 && retained.Outputs.Count == 2, "first fault and detached prior output facts preserved");
            healthy.Update(); c.That(healthy.Fault == null && Read(c, healthy).Current.QueueCount == 0, "other session preview remains isolated");
        }
    }
}

