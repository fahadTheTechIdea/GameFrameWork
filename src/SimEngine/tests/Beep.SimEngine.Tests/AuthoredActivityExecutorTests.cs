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
    public static class AuthoredActivityExecutorTests
    {
        public const string Case = "Authored activity executors: custom physical phase, timed DAG and saved waits";
        public static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string authored = null, string common = null)
        {
            var modules = new GameModule[] { new ContentAccessModule(), new InventoryModule(), new MotionModule(),
                new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new AuthoredExecutorProofModule() };
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                JsonValue module = master.Get("modules")[i];
                if (module.Get("id").Text == WorkModule.ModuleId) module.Get("documents").Add(JsonValue.Of("authored-executor-work.json"));
            }
            master.Get("modules").Add(Content.Json.Parse("{\"id\":\"proof.activity-executor\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"authored-executor-phase.json\"]}"));
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.activity-executor"));
            var definition = new GameDefinition("native-authored-executor", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("native-survey-world.json", world.ToString())
                .Add("native-survey-startup.json", FrameworkProofContent.Read("native-survey-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("authored-executor-work.json", authored ?? FrameworkProofContent.Read("authored-executor-work.json"))
                .Add("authored-executor-phase.json", FrameworkProofContent.Read("authored-executor-phase.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", common ?? FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 751 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.site");
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("executor time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" + string.Join(";",
                Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.Id + "/" + n.Id + "/" + n.State + "/" + n.JobId + "/" + n.Blocker))));
        }
        private static Order Start(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "proof.authored-executor", Target = Site(game).Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "authored executor intent preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player activity accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Invalid(Check c, Action<JsonValue> change, string message)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("authored-executor-work.json")); change(root);
            c.Throws<ContentException>(() => New(Factory(root.ToString())), message);
        }
        private static JsonValue Node(JsonValue root) => root.Get("activities")[0].Get("nodes")[1];
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit node executor identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "strict new authored/view identity without old reader");
            var factory = Factory(); var game = New(factory); var board = Board(game);
            var definition = game.World.Content.Get<ActivityDef>("proof.authored-executor");
            c.Equal("operate|settle", string.Join("|", definition.Graph.Nodes.Select(n => n.Id)), "same DAG canonicalizes reverse authored order");
            c.That(ReferenceEquals(definition.Graph.Find("operate").Executor, game.World.Content.Get<IActivityExecutor>("proof.delayed-staffed")), "exact selected authored executor alias");
            c.That(ActivityExecutorContracts.RequiresWorker(definition.Graph.Find("operate")) && !ActivityExecutorContracts.RequiresWorker(definition.Graph.Find("settle")), "explicit worker/timed contracts");
            c.Equal(WorkPhasePurpose.Productive, game.World.Services.Get<IWorkPhaseCatalog>().Purpose(new Code("proof.work.delayed")), "custom phase semantic purpose");
            c.That(!game.World.Services.Has<IEconomy>(), "physical authored work needs no business");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "same node/job byte owner unchanged");
            ResourceId steel = game.World.Content.ResourceId("steel"); Order order = Start(c, game); var node = order.Node("operate");
            Reach(game, () => node.Blocker == new Code("proof.phase-wait"), "physical custom phase wait");
            Job job = board.Job(node.JobId);
            c.Equal(new Code("proof.work.delayed"), job.Toils[job.ToilIndex].Executor, "actual job invokes authored custom phase");
            c.Equal(JobState.Paused, job.State, "actual worker/job retains phase pause");
            c.Equal(NodeState.Paused, node.State, "actual node exposes phase pause");
            c.Equal(0L, node.Progress.WholeTicks, "phase delay earns no fake work");
            c.Equal(Fix.FromInt(20), Site(game).Get<IInventory>().Amount(steel), "wait spends no startup input");
            c.Equal(0L, order.Node("settle").Progress.WholeTicks, "dependent timer does not bypass physical predecessor");
            var views = new List<ActivityNodeView>(); ulong before = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "private detached node views");
            var frozen = views.Single(n => n.Id == "operate");
            c.Equal("proof.delayed-staffed", frozen.Executor, "view reports authored executor ID only");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company sees no executor execution");
            c.Equal(before, game.Hash(), "views are pure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save actual custom phase wait");
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(0L, node.Progress.WholeTicks, "bounded continued wait uses only actual game clock");
            c.Equal(game.Hash(), twin.Hash(), "saved phase wait remains deterministic");
            Reach(game, () => node.Progress.WholeTicks > 0, "authored schedule admits actual physical work", twin);
            c.Equal(game.Hash(), twin.Hash(), "custom phase resumes identically");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "native transaction spends actual eight once");
            c.Equal(NodeState.Paused, frozen.State, "retained view never mutates into live execution");
            c.Equal(0L, frozen.Progress.WholeTicks, "retained progress remains detached");
            var partial = factory.Load(game.Save()); c.Equal(game.Hash(), partial.Hash(), "save custom productive work");
            Reach(game, () => node.State == NodeState.Done, "physical predecessor earns completion", partial);
            c.Equal(game.Hash(), partial.Hash(), "custom productive continuation identical");
            c.That(order.State == OrderState.Active && order.Node("settle").JobId == 0, "timed successor has no fabricated machine job");
            Reach(game, () => order.State == OrderState.Done, "authored timed successor", partial);
            c.Equal(game.Hash(), partial.Hash(), "timed successor saved continuation identical");
            c.That(order.Node("settle").Progress.WholeTicks >= 5, "five actual one-minute updates earned timed work");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "timer never invents materials or recharges input");
            Order cancel = Start(c, game); Reach(game, () => cancel.Node("operate").Blocker == new Code("proof.phase-wait"), "fresh execution schedule");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancel.Id }).Accepted, "ordinary cancellation during custom phase wait");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "unspent wait cancellation neither consumes nor refunds stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled custom phase checkpoint");
            c.Equal(Fix.FromInt(20), Site(New(factory)).Get<IInventory>().Amount(steel), "interleaved session is isolated");
            Invalid(c, root => Node(root).Remove("executor"), "missing executor refuses without roles fallback");
            Invalid(c, root => Node(root).Set("executor", JsonValue.Of("missing.executor")), "unknown executor reference refuses");
            Invalid(c, root => Node(root).Set("executor", JsonValue.Of("work.timed")), "timed executor refuses machine roles");
            Invalid(c, root => Node(root).Remove("duration"), "missing typed duration refuses");
            Invalid(c, root => Node(root).Set("hours", JsonValue.Number("0.5")), "old hours field explicitly unsupported");
            Invalid(c, root => root.Get("staffed_activity_executors")[0].Set("work", JsonValue.Of("unknown.phase")), "missing composed phase refuses before Initialize");
            Invalid(c, root => root.Get("staffed_activity_executors")[0].Set("work", JsonValue.Of("sim.work.pickup")), "wrong physical phase purpose refuses");
            JsonValue common = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json")); common.Get("timed_activity_executors")[0].Set("rate", JsonValue.Number("0"));
            c.Throws<ContentException>(() => New(Factory(common: common.ToString())), "zero timed rate refuses, no invented default");
        }
    }
}
