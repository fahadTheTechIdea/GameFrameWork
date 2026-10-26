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
    public static class AuthoredSegmentWorkflowTests
    {
        public const string Case = "Authored segment workflows: parallel DAG, physical inputs and earned tile prefix";
        public static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string work = null, string infrastructure = null, GameModule extraModule = null,
            string extraDocument = null, string extraJson = null, Action<JsonValue> worldAuthor = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new InfrastructureModule() };
            if (extraModule != null) modules.Add(extraModule);
            var definition = new GameDefinition("native-authored-segments", modules);
            var master = Json.Parse(FrameworkProofContent.Read("authored-segment-master.json"));
            var world = Json.Parse(FrameworkProofContent.Read("authored-segment-world.json"));
            worldAuthor?.Invoke(world);
            if (extraModule != null)
            {
                master.Get("modules").Add(Json.Parse("{\"id\":\"" + extraModule.Id + "\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"" + extraDocument + "\"]}"));
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(extraModule.Id));
            }
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString());
            foreach (string name in new[] { "authored-segment-world.json", "authored-segment-startup.json",
                "native-survey-navigation.json", "native-survey-work.json", "native-survey-knowledge.json", "work-retry.json" })
                files.Add(name, name == "authored-segment-world.json" ? world.ToString() : FrameworkProofContent.Read(name));
            if (extraModule != null) files.Add(extraDocument, extraJson);
            files.Add("authored-segment-work.json", work ?? FrameworkProofContent.Read("authored-segment-work.json"))
                .Add("authored-segment-infrastructure.json", infrastructure ?? FrameworkProofContent.Read("authored-segment-infrastructure.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 754 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static TileMap Map(SimGame game) => game.World.Services.Get<TileMap>();
        private static Fix Stock(SimGame game)
        {
            Fix stock = Fix.Zero; ResourceId steel = game.World.Content.ResourceId("steel");
            foreach (Entity e in game.World.Entities.All) if (e.Get<IInventory>() is IInventory inventory) stock += inventory.Amount(steel);
            return stock;
        }
        private static Entity Cursor(SimGame game, Order order) => game.World.Entities.Get(order.Target);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("segment time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.PlanKey + "/" + n.Id + "/" + n.State + "/" + n.JobId + "/" + n.Blocker))));
        }
        private static string Node(int segment, string name) => "segment." + segment + "." + name;
        private static TileCoord[] Path(int y = 10) => new[] { new TileCoord(16, y), new TileCoord(17, y), new TileCoord(18, y) };
        private static Order Begin(Check c, SimGame game, string line = "segment.road-line", int y = 10)
        {
            var command = new LayLineCommand { Line = line }; command.Tiles.AddRange(Path(y));
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews actual line binding");
            c.Equal(hash, game.Hash(), "preview is pure");
            c.That(TestKit.Order(game, 0, command).Accepted, "player requests real physical corridor");
            return Board(game).Orders.Last();
        }
        private static void Invalid(Check c, bool infrastructure, Action<JsonValue> edit, string message)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read(infrastructure ? "authored-segment-infrastructure.json" : "authored-segment-work.json"));
            edit(root); c.Throws<ContentException>(() => New(infrastructure ? Factory(infrastructure: root.ToString()) : Factory(work: root.ToString())), message);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit authored segment identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same actual graph/job/cursor payloads");
            var factory = Factory(); var game = New(factory);
            var road = game.World.Content.Get<ILineDefinition>("segment.road-line"); var pipe = game.World.Content.Get<ILineDefinition>("segment.pipe-line");
            var profile = game.World.Content.Get<ActivityDef>("segment.shared");
            c.That(ReferenceEquals(road.Workflow, profile) && ReferenceEquals(pipe.Workflow, profile), "two native line prototypes bind the same selected activity");
            c.Equal("inspect|prepare|install", string.Join("|", profile.Graph.Nodes.Select(n => n.Id)), "single compiler handles reverse authoring and parallel branches");
            c.That(!game.World.Services.Has<IEconomy>(), "physical construction needs no money/business module");
            c.Equal(Fix.FromInt(40), Stock(game), "native startup grants explicit stock once");
            Entity[] machines = game.World.Entities.All.Where(e => e.Def.Id == "segment.machine").ToArray();
            var origins = machines.Select(e => e.Get<ITilePresence>().Position).ToArray();
            Order order = Begin(c, game);
            c.That(ReferenceEquals(((IActivityWorkflowBinding)order.Plan).Workflow, profile), "actual path binding retains the same immutable source profile");
            c.Equal(9, order.Nodes.Count, "three authored nodes for each actual path tile");
            for (int segment = 0; segment < 3; segment++)
            {
                foreach (var authored in profile.Graph.Nodes)
                {
                    var bound = order.Plan.Graph.Find(Node(segment, authored.Id));
                    c.That(ReferenceEquals(bound.Executor, authored.Executor) && ReferenceEquals(bound.Requirement, authored.Requirement) &&
                        bound.Duration.Equals(authored.Duration) && bound.RequirementMultiplier == authored.RequirementMultiplier &&
                        string.Join("|", bound.Roles) == string.Join("|", authored.Roles), "bound node preserves authored execution/requirements/duration/roles");
                    c.Equal(authored.Effect, bound.Effect, "geometry binding does not invent completion rules");
                }
            }
            c.Equal(Node(0, "install"), order.Plan.Graph.Find(Node(1, "prepare")).Dependencies.Single(), "next root waits on prior segment terminal");
            c.Equal(Node(0, "inspect") + "|" + Node(0, "prepare"), string.Join("|", order.Plan.Graph.Find(Node(0, "install")).Dependencies), "both parallel prerequisites preserved");
            var effect = game.World.Content.Get<ILineEffectDefinition>("segment.stamp"); ulong before = game.Hash();
            c.Equal(WorkEffectStatus.Failed, effect.Apply(game.World, order, order.Plan.Graph.Find(Node(1, "install"))).Status, "future terminal cannot stamp unearned tile");
            c.Equal(before, game.Hash(), "refused effect has no mutation");
            c.That(order.Nodes.All(n => n.Progress.WholeTicks == 0), "placement earns no work");
            Reach(game, () => order.Node(Node(0, "prepare")).Progress.WholeTicks > 0 &&
                order.Node(Node(0, "inspect")).Progress.WholeTicks > 0, "parallel physical preparation");
            c.That(machines.Where((e, i) => e.Get<ITilePresence>().Position != origins[i]).Any(), "actual machines travel rather than teleport");
            c.That(Board(game).Jobs.Count(j => j.IsLive && j.OrderId == order.Id) >= 2, "parallel authored branches use same actual job coordinator");
            c.Equal(Fix.FromInt(40), Stock(game), "held tool is reserved, never consumed");
            c.That(machines.Any(e => e.Get<IInventory>().Reservations.Count > 0), "tool promise belongs to actual machine inventory");
            c.Equal(0L, order.Node(Node(0, "install")).Progress.WholeTicks, "final work cannot precede both roots");
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("segment.road")[Path()[0]], "root work stamps no tile");
            var views = new List<ActivityNodeView>(); ulong hash = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "engine publishes own detached stage progress");
            var frozen = views.Single(n => n.Id == Node(0, "prepare")); var frozenProgress = frozen.Progress;
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company cannot read workflow state");
            c.Equal(hash, game.Hash(), "immutable diagnostics do not advance or mutate simulation");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial parallel branches restore exact actual claims/jobs");
            Reach(game, () => order.Node(Node(0, "install")).State == NodeState.Done, "first earned final stage", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved parallel execution continues identically");
            c.Equal(frozenProgress, frozen.Progress, "detached view never changes with live Update");
            c.Equal(3, order.NodesDone, "completed node count is not tile count");
            c.Equal((byte)1, Map(game).Layer<ByteLayer>("segment.road")[Path()[0]], "only earned final stage stamps first tile");
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("segment.road")[Path()[1]], "future tile stays absent");
            c.Equal(Path()[1], Cursor(game, order).Get<ITilePresence>().Tile, "empty intent cursor advances exactly one segment");
            c.Equal(0, Cursor(game, order).Get<IInventory>().Stock.Count, "cursor carries no teleported material");
            c.Equal(Fix.FromInt(38), Stock(game), "first install consumes exact real worker bill once");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "earned prefix with nine-node graph restores");
            Reach(game, () => order.State == OrderState.Done, "three-segment authored corridor", twin);
            c.Equal(game.Hash(), twin.Hash(), "full saved corridor continuation matches");
            c.Equal(Fix.FromInt(34), Stock(game), "three physical installs consume six units exactly");
            c.That(Cursor(game, order) == null, "normal final effect removes only completed cursor");
            foreach (TileCoord tile in Path()) c.Equal((byte)1, Map(game).Layer<ByteLayer>("segment.road")[tile], "all earned geometry remains");
            c.That(machines.All(e => e.Get<IInventory>().Reservations.Count == 0), "completion releases actual tool claims");
            c.Equal(Fix.FromInt(40), Stock(New(factory)), "interleaved fresh session has independent actual stock");

            game = New(factory); order = Begin(c, game, "segment.pipe-line", 14);
            Reach(game, () => order.Node(Node(0, "install")).State == NodeState.Done &&
                order.Node(Node(1, "prepare")).Progress.WholeTicks > 0, "partial second prototype");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels through same engine work owner");
            Reach(game, () => Cursor(game, order) == null, "cancelled native cursor cleanup");
            c.Equal((byte)1, Map(game).Layer<ByteLayer>("segment.pipe")[Path(14)[0]], "cancel preserves completed physical prefix");
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("segment.pipe")[Path(14)[1]], "cancel cannot earn incomplete next tile");
            c.Equal(Fix.FromInt(38), Stock(game), "cancel preserves real unspent materials and releases held tools");
            c.That(game.World.Entities.All.Where(e => e.Def.Id == "segment.machine").All(e => e.Get<IInventory>().Reservations.Count == 0), "cancel releases actual claims");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled execution checkpoints without legacy graph translation");

            Invalid(c, true, r => r.Get("lines")[0].Remove("activity"), "explicit profile reference required");
            Invalid(c, true, r => r.Get("lines")[0].Set("activity", JsonValue.Of("missing")), "missing profile refuses");
            Invalid(c, true, r => r.Get("lines")[0].Set("segment_node", JsonValue.Of("prepare")), "only explicit final stamping node accepted");
            Invalid(c, true, r => r.Get("lines")[0].Set("duration_per_tile", Content.Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}")), "deleted flat execution schema refuses");
            Invalid(c, false, r => r.Get("activities")[0].Set("player_orderable", JsonValue.Of(true)), "profile cannot bypass native placement by standalone order");
            Invalid(c, false, r => r.Get("activities")[0].Get("nodes")[0].Set("after", Content.Json.Parse("[\"prepare\"]")), "unjoined parallel branch refuses");
            Invalid(c, false, r => r.Get("activities")[0].Get("nodes")[1].Set("effect", JsonValue.Of("segment.stamp")), "early side-branch stamping refuses");
            Invalid(c, false, r => r.Get("activities")[0].Get("nodes")[0].Set("effect", JsonValue.Of("segment.finish")), "terminal cannot finish whole corridor prematurely");
            Invalid(c, false, r => r.Get("activities")[0].Get("nodes")[1].Set("after", Content.Json.Parse("[\"install\"]")), "cycles rejected by sole compiler");
            Invalid(c, false, r => r.Get("activities")[0].Get("nodes")[0].Set("after", Content.Json.Parse("[\"missing\"]")), "missing dependency rejected");
        }
    }
}

