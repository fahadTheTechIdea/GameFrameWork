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
    // Public inputs, detached observations and bounded updates only. Existing engine
    // owners execute every research, resource, plan, process and objective decision.
    internal static class NonspatialProgressionTests
    {
        internal const string Case = "Nonspatial process game: authored research, paused plans, operating control and objective replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory()
        {
            var definition = new GameDefinition("process-progression", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new TransformationModule(), new WorldSetupModule(), new CompanyKnowledgeModule(),
                new TechnologyModule(), new ObjectiveModule(), new ProcessProofModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("process-progression-master.json"))
                .Add("framework/access.json", FrameworkProofContent.Access);
            foreach (string file in new[] { "process.json", "work-retry.json", "process-progression.json" })
                files.Add(file, FrameworkProofContent.Read(file));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory, ulong seed) => factory.NewGame(new NewGameRequest {
            Scenario = "process-progression", Seed = seed, Seats = { Controller.Human, Controller.Human } });
        internal static EntityView Assembler(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "assembler");
        internal static int Tokens(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "token");
        internal static Fix Stock(SimGame game)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Assembler(game).Id, facts)) throw new InvalidOperationException("missing actual assembler stock");
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value;
        }
        internal static TechnologySnapshot Research(SimGame game, int seat = 0)
        {
            var facts = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(facts);
            return facts.Single(f => f.Technology == "process.qualification");
        }
        internal static List<ActivityNodeView> Nodes(SimGame game)
        {
            var nodes = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Research(game).ActiveOrder, nodes))
                throw new InvalidOperationException("missing actual active research graph");
            return nodes;
        }
        internal static List<ObjectiveSnapshot> Objectives(SimGame game, int seat = 0)
        {
            var facts = new List<ObjectiveSnapshot>(); game.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(facts); return facts;
        }
        internal static PlanView Plan(SimGame game, long id)
        {
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
            return plans.Single(p => p.Id == id);
        }
        internal static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(
                Assembler(game).Id, new Code("transformation"), out var state)) throw new InvalidOperationException("missing native process");
            return state;
        }
        internal static string Diagnostics(SimGame game) => "step=" + game.Clock.StepIndex + "/known=" + Research(game).Known +
            "/stock=" + Stock(game) + "/tokens=" + Tokens(game) + "/process=" + Process(game).Phase + "/" + Process(game).Blocker +
            "/objectives=" + string.Join(";", Objectives(game).Select(o => o.Objective + "/" + o.State + "/" + o.Blocker));
        private static void Update(SimGame game, SimGame twin = null, SimGame other = null)
        {
            game.Update(); twin?.Update(); other?.Update();
            if (game.Fault != null || twin?.Fault != null || other?.Fault != null)
                throw new InvalidOperationException("fault: " + game.Fault + "/" + twin?.Fault + "/" + other?.Fault);
            if (twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException("saved twin differs/" + Diagnostics(game));
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame other = null)
        {
            var watchdog = Stopwatch.StartNew();
            for (int i = 0; i < 120 && watchdog.Elapsed.TotalSeconds < 15 && !done(); i++) Update(game, twin, other);
            if (!done()) throw new InvalidOperationException(stage + "/" + Diagnostics(game));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command, Code expected = default)
        {
            ulong hash = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "public preview parity");
            c.Equal(hash, game.Hash(), "preview changes no work, stock or time");
            var result = TestKit.Order(game, 0, command); c.Equal(expected, result.Refusal, "ordinary command admission matches preview");
            if (expected.IsNone) c.That(result.Accepted, "ordinary player intent accepted");
            if (game.Fault != null) throw new InvalidOperationException("command fault " + game.Fault);
            return result;
        }
        private static long PostResearch(Check c, SimGame game)
        {
            var arguments = Json.Parse("{\"target\":" + Assembler(game).Id.Value + "}");
            return Submit(c, game, new SubmitPlanCommand {
                Intent = new PlanIntent("activity:process.qualification", arguments, "qualification", 30, false) }).Arg0;
        }
        internal static EnqueueTransformationCommand Build(SimGame game, int count) => new EnqueueTransformationCommand {
            Transformer = Assembler(game).Id, Capability = new Code("transformation"), Transformation = "build", Count = count };
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory, 839); var other = New(factory, 841);
            var limits = new ReplayLimits(1024 * 1024, 500); var recording = game.RecordReplay(limits);
            c.That(game.Snapshot.Space == null && Assembler(game).Spatial == null, "actual game has no map, coordinates or fabricated spatial provider");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.Modules.Contains(TileNavigationModule.ModuleId) &&
                !game.Modules.Contains(LogisticsModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "research and manufacturing require no business, map or hauling module");
            c.Equal(Fix.FromInt(20), Stock(game), "original external process stock reused without extra grants");
            c.Equal(0, Tokens(game), "original entity output not pre-created");
            c.That(!Research(game).Known && !Research(game, 1).Known, "authored technology grants no starting knowledge");
            c.Equal(0, Objectives(game, 1).Count, "objective enrollment does not leak or invent another company's entries");
            Submit(c, game, Build(game, 2), TechnologyCodes.Locked);
            c.That(Process(game).QueueCount == 0 && Tokens(game) == 0, "locked request creates no queued or completed output");
            long plan = PostResearch(c, game); PostResearch(c, other);
            Until(game, () => Nodes(game).Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks), "earned partial research", other: other);
            c.Equal(Fix.FromInt(18), Stock(game), "native requirement consumes exactly two real research inputs");
            var retained = Research(game); c.That(!retained.Known, "partial work gives no unlock");
            Submit(c, game, new ConfigurePlanCommand { Plan = plan, Group = "qualification", Priority = 70, Paused = true });
            c.That(Plan(game, plan).Priority == 70 && Plan(game, plan).Paused, "immutable plan view exposes changed priority and pause");
            var progress = Nodes(game).Select(n => n.Progress).ToArray();
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "paused research and changed plan priority persist");
            c.That(Plan(twin, plan).Priority == 70 && Plan(twin, plan).Paused, "restored public plan retains actual player settings");
            ulong otherBefore = other.Hash(); long otherStep = other.Clock.StepIndex;
            for (int i = 0; i < 8; i++) Update(game, twin, other);
            c.That(Nodes(game).Select(n => n.Progress).SequenceEqual(progress) && !Research(game).Known, "saved paused graph earns no work or knowledge");
            c.That(other.Clock.StepIndex > otherStep && other.Hash() != otherBefore && Research(other).Known,
                "another genuinely advancing session completes its own research while first is paused");
            c.Equal(Fix.FromInt(18), Stock(game), "paused research never charges inputs again");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex;
            Update(game, twin, other); c.Equal(paused, game.Clock.StepIndex, "sole game clock pause stops simulation age");
            game.Clock.Resume(); twin.Clock.Resume();
            Submit(c, game, new ConfigurePlanCommand { Plan = plan, Group = "qualification", Priority = 70, Paused = false });
            twin = factory.Load(game.Save()); Until(game, () => Research(game).Known, "earned qualification", twin, other);
            c.That(!retained.Known && !Research(game, 1).Known, "retained snapshots and foreign knowledge stay unchanged");
            c.That(Objectives(game).Single(o => o.Objective == "process.research").State == ObjectiveState.Completed,
                "native objective observes actually earned technology");
            c.That(game.Outcome.IsNone && Tokens(game) == 0, "research alone does not win or manufacture");
            Submit(c, other, Build(other, 1));
            Submit(c, game, Build(game, 2));
            c.That(Process(game).Progress.WholeTicks > 0 && Process(game).Progress.WholeTicks < Process(game).Required && Tokens(game) == 0,
                "unlocked process still earns its original six-minute batch duration");
            c.Equal(new Code("process.working"), Assembler(game).State, "Entity.Update projects actual productive state, not host rules");
            var earned = Process(game).Progress; var spent = Stock(game);
            Submit(c, game, new ConfigureOperationCommand { Entity = Assembler(game).Id, Operation = new Code("transformation"), Enabled = false, Target = Fix.One });
            twin = factory.Load(game.Save());
            for (int i = 0; i < 8; i++) Update(game, twin, other);
            c.Equal(earned, Process(game).Progress, "disabled operation retains productive progress without catch-up");
            c.Equal(spent, Stock(game), "disabled process neither fabricates nor consumes material");
            c.Equal(new Code("process.blocked"), Assembler(game).State, "Entity.Update owns authored stopped-state projection");
            c.That(Tokens(other) == 1 && other.Outcome.IsNone && Stock(other) == Fix.FromInt(16), "other session completes exactly its own one batch, not this session's goal");
            Submit(c, game, new ConfigureOperationCommand { Entity = Assembler(game).Id, Operation = new Code("transformation"), Enabled = true, Target = Fix.One });
            twin = factory.Load(game.Save()); Until(game, () => Tokens(game) == 1, "first native output", twin, other);
            c.That(game.Outcome.IsNone && Process(game).QueueCount == 1, "first result leaves sequential second batch and objective incomplete");
            twin = factory.Load(game.Save()); Until(game, () => !game.Outcome.IsNone, "dependent production objective", twin, other);
            c.Equal(new Code("process.completed"), game.Outcome, "engine objective effect alone sets terminal outcome");
            c.That(Objectives(game).All(o => o.State == ObjectiveState.Completed), "entire authored objective dependency chain completed");
            c.Equal(2, Tokens(game), "exactly two entity outputs after two separately earned batches");
            c.Equal(Fix.FromInt(14), Stock(game), "twenty starting inputs minus two research and two net-two batches");
            c.Equal(0, Process(game).QueueCount, "original authoritative queue drained");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal mapless progression save/load");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success, "normal player progression replay/" + replay.Failure + "/" + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replayed terminal state is identical");
            c.That(Tokens(other) == 1 && other.Outcome.IsNone && Stock(other) == Fix.FromInt(16), "other progressing session remains independently unfinished after replay");
        }
    }
}
