using System;
using System.Collections.Generic;
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
    // Explicit content and ordinary player controls; no production, haul, timing or accounting algorithm.
    internal static class ProcessOnlyGameTests
    {
        internal const string Case = "Process-only game: physical manufacturing without progression controllers knowledge or economy";
        private static readonly string[] RequiredModules = { "points", "sim.access", "sim.inventory", "sim.logistics", "sim.motion", "sim.operations", "sim.transformations", "sim.work", "sim.world-setup" };
        private static readonly Code Capability = new Code("process.operation");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string missing = null)
        {
            var modules = new GameModule[] { new ContentAccessModule(), new InventoryModule(), new OperationsModule(),
                new MotionModule(), new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule() };
            var definition = new GameDefinition("process-only", modules.Where(m => m.Id != missing).ToArray());
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("process-only-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            foreach (string name in new[] { "world", "work", "recipes" })
                files.Add("process-only-" + name + ".json", FrameworkProofContent.Read("process-only-" + name + ".json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "process-only", Seed = 735 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "process." + name).Id;
        private static TransformationManagementView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot Process(SimGame game)
        {
            if (!View(game).TryGetTransformation(Entity(game, "factory"), Capability, out var facts)) throw new InvalidOperationException("missing actual process");
            return facts;
        }
        private static Fix Stock(SimGame game, string entity, string resource)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, entity), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "process." + resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("process step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " phase=" + Process(game).Phase
                + " blocker=" + Process(game).Blocker + " source=" + Stock(game, "source", "input") + " cargo=" + Stock(game, "machine", "input"));
        }
        private static void Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary process input admission");
            c.Equal(hash, game.Hash(), "preview pure"); c.That(TestKit.Order(game, 0, command).Accepted, "normal player command accepted");
        }
        private static EnqueueTransformationCommand Enqueue(SimGame game) => new EnqueueTransformationCommand {
            Transformer = Entity(game, "factory"), Capability = Capability, Transformation = "process.make" };
        private static void Operate(Check c, SimGame game, bool enabled) => Order(c, game,
            new ConfigureOperationCommand { Entity = Entity(game, "factory"), Operation = Capability, Enabled = enabled, Target = Fix.One });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual unchanged engine identity");
            var factory = Factory(); var game = New(factory);
            c.That(game.Modules.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(RequiredModules), "exact nine declared modules, no optional registration or selection");
            c.That(game.GameMode.ModeId == "process.survival" && game.GameMode.Properties.Count == 0, "resource-only mode requires no money or other optional properties");
            c.That(game.Seat(0).View.FeatureRegistrations.All(f => RequiredModules.Contains(f.Owner)), "no optional feature owner injected");
            c.That(game.Seat(0).View.StateCodecs.All(s => s.Owner == "engine" || RequiredModules.Contains(s.Owner)), "only root engine and nine declared module codecs; no optional history/controller/ledger codecs injected");
            c.That(game.Seat(0).Snapshot.Entities.Count == 3 && Stock(game, "source", "input") == Fix.FromInt(20), "sole authored initialization grants actual opening physical stock only");
            c.Equal(Code.None, View(game).ReadMonetaryCost(Entity(game, "factory"), Capability, "process.make", out var cost), "existing monetary decision query");
            c.That(cost.Applicability == MonetaryCostApplicability.NotAuthored && cost.AuthoredAmount == Fix.Zero && !cost.EffectiveAmount.HasValue, "no invented budget, payment or sale");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            var command = Enqueue(game); ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "queue preview needs no progression or account");
            c.Equal(hash, game.Hash(), "queue admission pure"); game.Seat(0).Submit(command);
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Process(game).QueueCount == 1 && Stock(game, "factory", "output") == Fix.Zero, "saved player inbox starts actual queue, not instant output");
            Reach(game, twin, () => Stock(game, "machine", "input") > Fix.Zero, "physical input pickup");
            c.That(Stock(game, "source", "input") < Fix.FromInt(20) && Stock(game, "factory", "output") == Fix.Zero, "real carrier owns supply before factory output");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded carrier and supplier claims checkpoint");
            Reach(game, twin, () => Process(game).Phase == TransformationPhase.Working && Process(game).Progress.WholeTicks > 0, "supplied productive manufacturing");
            var retained = Process(game);
            c.That(retained.Required == 30 && retained.Wip.Stock.Lines.Aggregate(Fix.Zero, (sum, r) => sum + r.Amount) == Fix.FromInt(3)
                && retained.Tools.Held.Lines.Aggregate(Fix.Zero, (sum, r) => sum + r.Amount) == Fix.One
                && Stock(game, "source", "input") == Fix.FromInt(16), "authored duration, three original WIP inputs and one tool held at its source endpoint");
            c.That(!View(game, 1).TryGetTransformation(Entity(game, "factory"), Capability, out _), "other company cannot inspect private process queue or WIP");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Enqueue(game)), "other company shares private queue refusal, no privileged manufacturing path");
            Operate(c, game, false); twin = factory.Load(game.Save()); var paused = Process(game).Progress;
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Process(game).Progress.Equals(paused) && Stock(game, "factory", "output") == Fix.Zero, "disabled process retains actual WIP and productive progress");
            hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one game clock pauses loaded travel and manufacturing");
            game.Clock.Resume(); twin.Clock.Resume(); Operate(c, game, true); twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game).QueueCount == 0, "earned first resource output");
            c.That(Stock(game, "source", "input") == Fix.FromInt(16) && Stock(game, "factory", "input") == Fix.One
                && Stock(game, "factory", "output") == Fix.FromInt(2) && Process(game).Wip.Stock.IsEmpty, "first timed recipe consumes three, returns tool and earns two outputs once");
            c.That(retained.QueueCount == 1 && retained.Wip.Stock.Lines.Aggregate(Fix.Zero, (sum, r) => sum + r.Amount) == Fix.FromInt(3)
                && retained.Tools.Held.Lines.Aggregate(Fix.Zero, (sum, r) => sum + r.Amount) == Fix.One, "retained actual process and tool facts remain detached");
            Order(c, game, Enqueue(game)); twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game).Phase == TransformationPhase.Working && Process(game).Progress.WholeTicks > 0, "second supplied process");
            c.That(Stock(game, "source", "input") == Fix.FromInt(13), "second execution reuses existing tool, hauls only three missing units");
            var cancel = new CancelTransformationCommand { Transformer = Entity(game, "factory"), Capability = Capability, Index = 0 };
            c.Equal(Code.None, game.Seat(0).Preview(cancel), "partial cancellation normal admission"); game.Seat(0).Submit(cancel);
            twin = factory.Load(game.Save()); Step(game, twin);
            Reach(game, twin, () => Process(game).QueueCount == 0 && Process(game).Wip.Stock.IsEmpty, "saved cancellation returns original WIP");
            c.That(Stock(game, "source", "input") == Fix.FromInt(13) && Stock(game, "factory", "input") == Fix.FromInt(4)
                && Stock(game, "factory", "output") == Fix.FromInt(2), "cancelled partial batch returns unconsumed material/tool, no invented output or refund to source");
            Order(c, game, Enqueue(game)); twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game).QueueCount == 0, "local retained inputs produce next earned batch");
            c.That(Stock(game, "source", "input") == Fix.FromInt(13) && Stock(game, "factory", "input") == Fix.One
                && Stock(game, "factory", "output") == Fix.FromInt(4) && Stock(game, "machine", "input") == Fix.Zero, "two successful batches consume exactly six; cancelled input is reused, tools not charged again");
            var work = new PostOrderCommand { Plan = "process.inspect", Target = Entity(game, "factory"), Priority = 50 };
            c.Equal(Code.None, game.Seat(0).Preview(work), "machine work also independent of optional modules");
            var posted = TestKit.Order(game, 0, work); c.That(posted.Accepted, "normal authored physical machine activity");
            twin = factory.Load(game.Save()); var workView = game.Seat(0).View.Feature<WorkView>();
            Reach(game, twin, () => workView.TryReadActivityCompletion("activity:process.inspect", Entity(game, "factory"), out _), "physical machine inspection");
            c.That(workView.TryReadActivityCompletion("activity:process.inspect", Entity(game, "factory"), out var completion)
                && completion.Order == (int)posted.Arg0 && completion.CompletedTick > completion.CreatedTick, "original graph and machine motion earn exact full-work fact");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final resource process/work checkpoint");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary process/work inputs replay without optional modules");
            var isolated = New(factory);
            for (int i = 0; i < 3; i++) { Step(game, twin); Step(isolated); }
            c.That(Process(isolated).QueueCount == 0 && Stock(isolated, "source", "input") == Fix.FromInt(20)
                && Stock(isolated, "factory", "output") == Fix.Zero && isolated.Modules.SequenceEqual(game.Modules), "interleaved isolated session has same minimal composition, no borrowed stock/process/history");
            foreach (string missing in new[] { "sim.inventory", "sim.work" })
                c.Throws<InvalidOperationException>(() => New(Factory(missing)), "module graph refuses missing physical dependency before composition: " + missing);
            c.Throws<ContentException>(() => New(Factory("points")), "master refuses missing spatial content provider before composition");
        }
    }
}
