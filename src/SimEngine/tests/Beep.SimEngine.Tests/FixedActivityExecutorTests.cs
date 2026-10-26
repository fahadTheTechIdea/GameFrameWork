using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class FixedActivityExecutorTests
    {
        public const string Case = "Fixed activity executor: stationary work, physical lost access and saved recovery";
        public static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Access = new Code("proof.fixed-access");
        private static GameFactory Factory(string authored = null)
        {
            var modules = new GameModule[] { new ContentAccessModule(), new InventoryModule(), new MotionModule(),
                new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule() };
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
                if (master.Get("modules")[i].Get("id").Text == WorkModule.ModuleId)
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of("fixed-executor-work.json"));
            var definition = new GameDefinition("native-fixed-executor", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("native-survey-world.json", FrameworkProofContent.Read("fixed-executor-world.json"))
                .Add("native-survey-startup.json", FrameworkProofContent.Read("fixed-executor-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("fixed-executor-work.json", authored ?? FrameworkProofContent.Read("fixed-executor-work.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 752 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.site");
        private static Entity Machine(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.machine");
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fixed executor time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " site=" + Site(game).Get<ITilePresence>().Tile + " nodes=" + string.Join(";",
                Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.Id + "/" + n.Id + "/" + n.State + "/" + n.JobId + "/" + n.Blocker))));
        }
        private static void Move(Check c, SimGame game, int x, SimGame twin = null)
        {
            var command = new MoveVehicleCommand { Vehicle = Site(game).Id, To = new TileCoord(x, 10) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal target movement preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "player moves target through actual motion");
            if (twin != null) c.That(TestKit.Order(twin, 0, new MoveVehicleCommand { Vehicle = Site(twin).Id, To = new TileCoord(x, 10) }).Accepted, "saved twin receives identical player intent");
        }
        private static Order Start(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "proof.fixed-service", Target = Site(game).Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "fixed activity intent preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary fixed activity accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Invalid(Check c, Action<JsonValue> change, string message)
        {
            var json = Content.Json.Parse(FrameworkProofContent.Read("fixed-executor-work.json")); change(json);
            c.Throws<ContentException>(() => New(Factory(json.ToString())), message);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "fixed executor identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same state bytes, strict build identity");
            c.That(!default(ActivityApproachResult).IsValid, "default approach result is not fabricated readiness");
            c.Throws<ArgumentException>(() => ActivityApproachResult.Wait(Code.None), "access wait must be explicit");
            c.Throws<ArgumentOutOfRangeException>(() => ActivityApproachResult.ResumeAt(-1), "negative approach refuses");
            var factory = Factory(); var game = New(factory); var board = Board(game);
            c.That(!game.World.Services.Has<IEconomy>(), "fixed physical work needs no money");
            c.That(Machine(game).Get<IMotion>() == null, "stationary worker has no fake movement provider");
            c.Equal(new TileCoord(17, 10), Machine(game).Get<ITilePresence>().Tile, "native JSON owns stationary placement");
            var definition = game.World.Content.Get<ActivityDef>("proof.fixed-service");
            c.That(ReferenceEquals(definition.Graph.Find("service").Executor, game.World.Content.Get<IActivityExecutor>("proof.fixed")), "same catalog executor alias");
            ResourceId steel = game.World.Content.ResourceId("steel"); var candidates = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
            game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("proof.fixed-service", "service", Site(game).Id, candidates);
            c.Equal(1, candidates.Count, "actual stationary candidate is diagnosed");
            c.Equal(Access, candidates[0].Result.Blocker, "out-of-reach offer uses authored access blocker");
            c.Equal(hash, game.Hash(), "candidate preview allocates no progress or claims");
            Order order = Start(c, game); ActivityNodeExecution node = order.Node("service");
            for (int i = 0; i < 3; i++) Update(game);
            c.Equal(0L, node.Progress.WholeTicks, "unreachable fixed worker earns no work");
            c.Equal(Fix.FromInt(20), Site(game).Get<IInventory>().Amount(steel), "unreachable fixed worker spends no input");
            Move(c, game, 16);
            Reach(game, () => node.Progress.WholeTicks >= 2, "target physically reaches fixed worker");
            Job job = board.Job(node.JobId); int jobId = job.Id;
            c.Equal(1, job.Toils.Count, "fixed itinerary has only productive work");
            c.Equal(WorkPhases.Productive, job.Toils[0].Executor, "no synthetic travel phase");
            c.Equal(0, job.ToilIndex, "productive phase remains actual execution coordinate");
            c.Equal(Machine(game).Id, job.Worker, "real stationary worker owns actual job");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "actual eight consumed once through native transaction");
            c.Equal(new Code("working"), Machine(game).State, "entity Update settles productive state without motion");
            Move(c, game, 30);
            Reach(game, () => node.Blocker == Access, "physical target leaves access");
            c.Equal(JobState.Paused, job.State, "access loss pauses same real job");
            c.Equal(NodeState.Paused, node.State, "access loss pauses same graph node");
            c.Equal(0, job.ToilIndex, "typed wait does not invent a travel index");
            c.Equal(jobId, node.JobId, "access loss retains actual assignment");
            c.Equal(new Code("stopped"), Machine(game).State, "entity Update settles access wait");
            var views = new List<ActivityNodeView>(); hash = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "own immutable access diagnostics");
            var frozen = views.Single(); c.Equal("proof.fixed", frozen.Executor, "view publishes authored executor identity");
            c.Equal(Access, frozen.Blocker, "view publishes actual access blocker");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company has no activity state");
            c.Equal(hash, game.Hash(), "access views are pure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved target motion and retained wait");
            WorkProgress earned = node.Progress;
            for (int i = 0; i < 5; i++) Update(game, twin);
            c.Equal(earned, node.Progress, "continued physical wait earns no progress");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "continued wait never recharges inputs");
            c.Equal(game.Hash(), twin.Hash(), "saved wait continues identically");
            Reach(game, () => Site(game).Get<ITilePresence>().Tile == new TileCoord(30, 10) && Site(game).Get<IMotion>().Status == MoveStatus.Arrived, "target finishes outbound trip", twin);
            Move(c, game, 16, twin);
            Reach(game, () => node.Progress.WholeTicks > earned.WholeTicks, "actual target return restores fixed work", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical recovery is deterministic");
            c.Equal(jobId, node.JobId, "same job resumes without release or reset");
            c.Equal(earned, frozen.Progress, "retained access snapshot never becomes live progress");
            c.Equal(Access, frozen.Blocker, "retained wait snapshot remains immutable");
            Reach(game, () => order.State == OrderState.Done, "fixed physical completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved fixed workflow completes identically");
            c.That(node.Progress.WholeTicks >= 60, "sixty real minute updates earn authored work");
            c.Equal(new TileCoord(17, 10), Machine(game).Get<ITilePresence>().Tile, "fixed worker never teleports or moves");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "recovery consumes no duplicate inputs");
            Move(c, game, 2); Reach(game, () => Site(game).Get<ITilePresence>().Tile == new TileCoord(2, 10), "target leaves completed service");
            Order cancel = Start(c, game); for (int i = 0; i < 3; i++) Update(game);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancel.Id }).Accepted, "ordinary cancellation of fixed access wait");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(steel), "unspent fixed cancellation preserves stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "fixed cancellation checkpoint");
            c.Equal(Fix.FromInt(20), Site(New(factory)).Get<IInventory>().Amount(steel), "interleaved session owns independent native stock");
            Invalid(c, root => root.Get("fixed_activity_executors")[0].Remove("access_blocker"), "fixed access blocker mandatory");
            Invalid(c, root => root.Get("fixed_activity_executors")[0].Set("work", JsonValue.Of("sim.work.travel")), "fixed phase must be productive");
            Invalid(c, root => root.Get("fixed_activity_executors")[0].Set("work", JsonValue.Of("missing.phase")), "fixed phase must be composed");
            Invalid(c, root => root.Get("activities")[0].Get("nodes")[0].Set("roles", Content.Json.Parse("[]")), "fixed executor requires actual worker roles");
        }
    }
}
