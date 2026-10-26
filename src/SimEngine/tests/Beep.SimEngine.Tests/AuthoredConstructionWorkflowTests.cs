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
    public static partial class AuthoredConstructionWorkflowTests
    {
        public const string Case = "Authored construction workflows: shared DAG, physical tools and staged removal";
        public static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(ClearanceCase, Clearance); }
        private static GameFactory Factory(string workflows = null, string entities = null, string startup = null)
        {
            var modules = new GameModule[] { new ContentAccessModule(), new InventoryModule(), new MotionModule(),
                new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new ConstructionModule() };
            JsonValue master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
                if (master.Get("modules")[i].Get("id").Text == WorkModule.ModuleId)
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of("authored-construction-work.json"));
            master.Get("modules").Add(Content.Json.Parse("{\"id\":\"sim.construction\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"authored-construction-entities.json\"]}"));
            JsonValue world = Content.Json.Parse(FrameworkProofContent.Read("fixed-executor-world.json"));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ConstructionModule.ModuleId));
            world.Get("scenarios")[0].Get("settings").Set(ConstructionModule.ModuleId, Content.Json.Parse("{\"recovery\":null}"));
            var definition = new GameDefinition("native-authored-construction", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("native-survey-world.json", world.ToString())
                .Add("native-survey-startup.json", startup ?? FrameworkProofContent.Read("authored-construction-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("authored-construction-work.json", workflows ?? FrameworkProofContent.Read("authored-construction-work.json"))
                .Add("authored-construction-entities.json", entities ?? FrameworkProofContent.Read("authored-construction-entities.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 753 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Builder(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "proof.builder");
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.FirstOrDefault(e => e.Def.Id == id);
        private static IConstructionDefinition Definition(SimGame game, string id) => game.World.Content.Get<EntityDef>(id).Component<IConstructionDefinition>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("authored construction time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" + string.Join(";",
                Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.PlanKey + "/" + n.Id + "/" + n.State + "/" + n.JobId + "/" + n.Blocker))));
        }
        private static Entity Build(Check c, SimGame game, string prototype, int x = 16, int y = 10)
        {
            var command = new ConstructCommand { Def = prototype, Priority = 60, Placement = Content.Json.Parse("{\"space\":\"survey.tiles\",\"x\":" + x + ",\"y\":" + y + "}") };
            c.Equal(Code.None, game.Seat(0).Preview(command), "native placement and workflow preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player posts actual authored construction");
            return game.World.Entities.Get(result.Entity);
        }
        private static void InvalidWork(Check c, Action<JsonValue> edit, string message)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("authored-construction-work.json")); edit(root);
            c.Throws<ContentException>(() => New(Factory(workflows: root.ToString())), message);
        }
        private static void InvalidEntities(Check c, Action<JsonValue> edit, string message)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("authored-construction-entities.json")); edit(root);
            c.Throws<ContentException>(() => New(Factory(entities: root.ToString())), message);
        }
        private static JsonValue Component(JsonValue root) => root.Get("entities")[1].Get("components").Get("construction");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored binding engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same node/job/site bytes remain authoritative");
            var factory = Factory(); var game = New(factory); var board = Board(game);
            var tower = Definition(game, "proof.tower"); var shelter = Definition(game, "proof.shelter");
            var profile = game.World.Content.Get<ActivityDef>("proof.shared-build");
            c.That(ReferenceEquals(profile, tower.Workflow) && ReferenceEquals(profile, shelter.Workflow), "two prototypes share exact catalog activity contract");
            c.That(ReferenceEquals(tower.Graph, profile.Graph) && ReferenceEquals(shelter.Graph, profile.Graph), "no cloned or recompiled construction graphs");
            c.Equal("prepare|erect", string.Join("|", profile.Graph.Nodes.Select(n => n.Id)), "authored reverse dependency order canonicalized once");
            c.That(ReferenceEquals(tower.Demolition.Workflow, game.World.Content.Get<ActivityDef>("proof.shared-remove")), "removal binds same selected activity catalog");
            c.Equal(67, tower.Demolition.Priority, "removal priority comes from sole authored profile");
            c.That(shelter.Demolition == null && shelter.ActivityDefinitions.Count == 1, "no fabricated empty removal graph");
            c.That(!game.World.Services.Has<IEconomy>() && Builder(game).Get<IMotion>() == null, "stationary physical tools/work need no business or fake navigation");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "same board codec");
            ResourceId steel = game.World.Content.ResourceId("steel");
            var standalone = new PostOrderCommand { Plan = profile.Id, Target = Builder(game).Id };
            c.Equal(StandardCodes.NotOrderable, game.Seat(0).Preview(standalone), "bound workflow cannot bypass native construction admission");
            Entity site = Build(c, game, "proof.tower"); Order order = board.Order(site.Get<IConstructionSite>().Order);
            c.That(ReferenceEquals(order.Plan, tower), "native order uses binding definition, not standalone activity order");
            c.Equal("build:proof.tower", order.PlanKey, "native execution identity preserved");
            c.That(Find(game, "proof.tower") == null, "placing a site never fabricates completed structure");
            Reach(game, () => order.Node("prepare").Progress.WholeTicks > 0, "physical tool-backed preparation");
            c.Equal(Fix.FromInt(20), Builder(game).Get<IInventory>().Amount(steel), "held tool is not consumed");
            c.That(Builder(game).Get<IInventory>().Reservations.Count > 0, "actual worker inventory owns held tool receipt");
            c.Equal(0L, order.Node("erect").Progress.WholeTicks, "dependent work cannot skip preparation");
            c.Equal(new Code("building"), site.State, "site entity Update settles authored state");
            var nodes = new List<ActivityNodeView>(); ulong hash = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes), "own detached construction progress");
            ActivityNodeView frozen = nodes.Single(n => n.Id == "prepare");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company gets no project state");
            c.Equal(hash, game.Hash(), "project progress reads are pure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "tool-backed site checkpoint");
            Reach(game, () => order.Node("erect").Progress.WholeTicks > 0, "actual staged material consumption", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved preparation and material stage agree");
            c.Equal(Fix.FromInt(12), Builder(game).Get<IInventory>().Amount(steel), "real worker-source eight consumed once");
            c.That(order.Node("prepare").State == NodeState.Done && frozen.Progress.WholeTicks == 1, "frozen project snapshot never tracks live completion");
            var partial = factory.Load(game.Save()); c.Equal(game.Hash(), partial.Hash(), "partial authored construction checkpoint");
            Reach(game, () => order.State == OrderState.Done, "native construction finalization", partial);
            c.Equal(game.Hash(), partial.Hash(), "saved finalization is identical");
            Entity built = Find(game, "proof.tower");
            c.That(built != null && !site.Alive && built.Id != site.Id, "native construction replaces actual site once");
            c.That(order.Node("prepare").Progress.WholeTicks >= 3 && order.Node("erect").Progress.WholeTicks >= 4, "authored stages earn actual game time");
            c.Equal(0, Builder(game).Get<IInventory>().Reservations.Count, "completed construction releases actual tool claims");
            var removeCommand = new DemolishCommand { Target = built.Id };
            c.Equal(Code.None, game.Seat(0).Preview(removeCommand), "native authored removal preview");
            c.That(TestKit.Order(game, 0, removeCommand).Accepted, "player posts staged removal through ordinary plan owner");
            Reach(game, () => board.Orders.Any(o => o.PlanKey == "demolish:proof.tower"), "removal activation");
            Order remove = board.Orders.Single(o => o.PlanKey == "demolish:proof.tower");
            Reach(game, () => remove.Node("isolate").Progress.WholeTicks > 0, "actual removal isolation");
            c.That(built.Alive && remove.Node("remove").Progress.WholeTicks == 0, "isolation does not instantly remove target");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "held-tool removal checkpoint");
            Reach(game, () => remove.State == OrderState.Done, "physical authored removal completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved staged removal agrees");
            c.That(!built.Alive && remove.Node("isolate").Progress.WholeTicks >= 3 && remove.Node("remove").Progress.WholeTicks >= 3, "native removal requires both actual stages");
            c.Equal(Fix.FromInt(12), Builder(game).Get<IInventory>().Amount(steel), "empty salvage and held tool invent no materials");
            c.Equal(0, Builder(game).Get<IInventory>().Reservations.Count, "removal releases actual held tool");
            Entity secondSite = Build(c, game, "proof.shelter"); Order second = board.Order(secondSite.Get<IConstructionSite>().Order);
            Reach(game, () => second.State == OrderState.Done, "second prototype uses same authored graph");
            c.Equal(Fix.FromInt(4), Builder(game).Get<IInventory>().Amount(steel), "second physical execution consumes its own eight");
            c.That(ReferenceEquals(second.Plan.Graph, order.Plan.Graph), "shared definition never clones or shares runtime progress");
            c.Equal(ConstructionService.NotConstructible, game.Seat(0).Preview(new DemolishCommand { Target = Find(game, "proof.shelter").Id }), "absent removal refuses without default graph");
            Entity cancelledSite = Build(c, game, "proof.tower", 17, 9); Order cancelled = board.Order(cancelledSite.Get<IConstructionSite>().Order);
            Reach(game, () => cancelled.Node("prepare").Progress.WholeTicks > 0, "new project held-tool stage before cancellation");
            c.That(TestKit.Order(game, 0, new CancelConstructionCommand { Site = cancelledSite.Id }).Accepted, "ordinary native project cancellation");
            c.That(!cancelledSite.Alive && cancelled.State == OrderState.Cancelled, "empty cancelled site retires through native owner");
            c.Equal(Fix.FromInt(4), Builder(game).Get<IInventory>().Amount(steel), "unspent cancellation neither charges nor refunds materials");
            c.Equal(0, Builder(game).Get<IInventory>().Reservations.Count, "cancellation releases only actual tool claims");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled binding checkpoint");
            c.Equal(Fix.FromInt(20), Builder(New(factory)).Get<IInventory>().Amount(steel), "shared immutable content does not share session stock");
            InvalidEntities(c, root => Component(root).Remove("activity"), "construction must reference authored activity");
            InvalidEntities(c, root => Component(root).Set("activity", JsonValue.Of("missing.activity")), "missing profile fails before initialization");
            InvalidEntities(c, root => Component(root).Set("nodes", Content.Json.Parse("[]")), "old inline nodes unsupported");
            InvalidEntities(c, root => Component(root).Get("demolish").Remove("activity"), "removal must reference authored activity");
            InvalidWork(c, root => root.Get("activities")[0].Set("player_orderable", JsonValue.Of(true)), "bound profile cannot be a standalone player activity");
            InvalidWork(c, root => root.Get("activities")[0].Set("target_tags", Content.Json.Parse("[\"foreign.target\"]")), "bound target restriction cannot silently disappear");
            InvalidWork(c, root => root.Get("activities")[0].Set("on_complete", JsonValue.Of("sim.construction.finish")), "bound final effect cannot silently override native completion");
            InvalidWork(c, root => {
                for (int i = 0; i < root.Get("activities")[0].Get("nodes").Count; i++)
                { JsonValue node = root.Get("activities")[0].Get("nodes")[i]; node.Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"}")); }
            }, "bound construction cannot become an instant machine-free fallback");
        }
    }
}
