using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Normal authored inputs and player commands/views/clock only. No recipe or rate execution here.
    internal static class TransformationTimingTests
    {
        internal const string Case = "Transformation timing: one session converter and independent continuous physical flow";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 1800 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " phase=" + Process(game).Phase
                + " blocker=" + Process(game).Blocker + " progress=" + Process(game).Progress.WholeTicks
                + " source=" + Stock(game, "source") + " factory=" + Stock(game, "contract.factory") + " cargo=" + Stock(game, "machine"));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual single timing owner build");
            c.That(typeof(ITransformationDefinition).GetMethod("RequiredTicks") == null
                && typeof(TransformationDef).GetMethod("RequiredTicks") == null
                && typeof(TransformationDeclaration).GetMethod("RequiredTicks") == null, "no duplicate overridable recipe conversion or compatibility helper");
            Batch(c, 60, 2); Batch(c, 10, 9); Continuous(c); Range(c);
        }
        internal static void Batch(Check c, int seconds, long required, GameModule processor = null, Action<JsonValue> setupEdit = null)
        {
            var initialize = new InitializeProbe();
            var factory = Factory(initialize, out var recipes,
                root => root.Get("independent_transformations")[0].Set("duration", Json.Parse("{\"numerator\":90,\"denominator\":1,\"unit\":\"second\"}")),
                setupEdit: setupEdit, worldEdit: root => root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number(seconds.ToString())), extraModule: processor);
            var game = ConstructionPlanTests.NewPoints(factory);
            c.That(recipes.ActualAliases && recipes.OriginalNativeBinding, "same authored contract and native processor with quantum " + seconds);
            if (processor is ProcessorContractModule declaration) c.That(declaration.ActualMetadata, "independent named processor metadata creates original native runtime");
            c.Equal(1, initialize.Initializations, "conversion validated before Initialize");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recorder = game.RecordReplay(limits);
            Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
            c.Equal(required, Process(game).Required, "ninety seconds converted once by selected session " + seconds);
            Reach(game, null, () => Process(game).Phase == TransformationPhase.Working, "physical ninety-second startup");
            var retained = Process(game);
            c.That(retained.Progress.WholeTicks > 0 && retained.Progress.WholeTicks < required
                && game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "physical supply and partial earned work, not instant completion");
            var twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; ulong hash = game.Hash();
            game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "one clock pauses both physical execution and facts"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Process(game).QueueCount == 0, "native completion at selected session duration");
            c.Equal(required - retained.Progress.WholeTicks, game.Clock.StepIndex - at, "execution and detached Required agree without a recipe callback");
            c.That(game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1
                && Stock(game, "source") == Fix.FromInt(16) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero,
                "one timed result, input3 consumed and reusable tool1 returned");
            c.That(retained.Required == required && retained.QueueCount == 1, "retained facts unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed duration save");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "selected-duration ordinary player replay");
        }
        private static Fix Amount(Bill bill) => bill.Lines.Aggregate(Fix.Zero, (sum, line) => sum + line.Amount);
        internal static void Continuous(Check c, GameModule processor = null, Action<JsonValue> setupEdit = null)
        {
            var initialize = new InitializeProbe();
            var factory = Factory(initialize, out var recipes, root =>
            {
                var authored = Json.Parse(FrameworkProofContent.Read("transformation-metadata.json")).Get("metadata_transformations")[1].Clone();
                authored.Remove("fault"); authored.Set("id", JsonValue.Of("contract.make"));
                root.Set("independent_transformations", JsonValue.NewArray()); root.Get("independent_transformations").Add(authored);
            }, setupEdit: setupEdit, extraModule: processor);
            var game = ConstructionPlanTests.NewPoints(factory);
            c.That(recipes.ActualAliases && recipes.OriginalNativeBinding, "independent authored continuous recipe binds original native flow");
            if (processor is ProcessorContractModule declaration) c.That(declaration.ActualMetadata, "independent declaration resolves once and original continuous runtime matches");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recorder = game.RecordReplay(limits);
            Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
            c.That(Process(game).Required == 0 && Process(game).Mode == TransformationMode.Continuous, "unbounded continuous work has no batch duration");
            Reach(game, null, () => Amount(Process(game).Flow.Produced).IsPositive, "physically supplied continuous production");
            var first = Process(game); var twin = factory.Load(game.Save());
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "continuous input/output/tool/operating earned time pauses together");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Amount(Process(game).Flow.Produced) >= Fix.FromInt(3), "physical resupply and repeated continuous conversion");
            var produced = Process(game); Fix output = Amount(produced.Flow.Produced);
            c.That(Amount(produced.Flow.Consumed) == output * Fix.FromInt(2) && Amount(produced.Flow.ToolConsumed) == output
                && Amount(produced.Operating.Consumed) == output, "original native rates account input2, tool1, operating1 for each output1");
            c.That(Amount(first.Flow.Produced) == Fix.One && first.Progress.WholeTicks == 1, "retained first earned flow facts unchanged");
            c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "continuous recipe never creates batch entity output");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "running continuous save");
            Order(c, game, new CancelTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Index = 0 });
            // Apply exactly the same ordinary player input to the saved physical twin.
            Order(c, twin, new CancelTransformationCommand { Transformer = Entity(twin, "contract.factory"), Capability = new Code("contract.process"), Index = 0 });
            Reach(game, twin, () => Process(game).QueueCount == 0, "continuous cancellation releases actual commitments");
            c.That(Process(game).Wip.Stock.IsEmpty && Process(game).Tools.Held.IsEmpty && Process(game).Tools.Receipts.Count == 0, "no leaked WIP/tool commitment after cancellation");
            c.Equal(Fix.FromInt(20) - output * Fix.FromInt(3), Stock(game, "source") + Stock(game, "destination")
                + Stock(game, "machine") + Stock(game, "contract.factory"), "physical conservation across hauling, input/tool/operating draw and generated output");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary continuous enqueue/pause/cancel canonical replay");
        }
        private static void Range(Check c)
        {
            var initialize = new InitializeProbe();
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Factory(initialize, out _, root =>
                root.Get("independent_transformations")[1].Set("duration", Json.Parse("{\"numerator\":9223372036854775807,\"denominator\":1,\"unit\":\"hour\"}")))),
                "unused independent duration overflow refused by original session converter");
            c.Equal(0, initialize.Initializations, "duration range refuses before Initialize");
        }
    }
}
