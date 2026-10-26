using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class ConstructionPlanTests
    {
        public static void Register()
        {
            TestRunner.Add("Plans: authored intent and construction admission contracts", c => { Admission(c); RemovalAdmission(c); });
            TestRunner.Add("Plans: physical sites, pause and saved continuation", c => { Physical(c); RemovalPhysical(c); });
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Points(Action<JsonValue> edit = null, string recovery = "point.recovery", string recoveryWork = null, string flows = null, Action<JsonValue> scenarioEdit = null,
            string retryProfiles = null, GameModule additionalModule = null, string moduleDeclaration = null, string moduleDocument = null, string templateDefinitions = null, string knowledgeDefinitions = null,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, IReadOnlyList<string> startupProfiles = null, Action<JsonValue> worldEdit = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ConstructionModule.ModuleId));
            JsonValue selection = JsonValue.NewObject(); selection.Set("recovery", recovery == null ? JsonValue.Null() : JsonValue.Of(recovery));
            root.Get("scenarios")[0].Get("settings").Set(ConstructionModule.ModuleId, selection);
            scenarioEdit?.Invoke(root.Get("scenarios")[0]);
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            JsonValue authored = Content.Json.Parse(FrameworkProofContent.Read("point-construction.json"));
            // These are explicitly selected proof documents, never an engine default or mod scan.
            JsonValue work = Content.Json.Parse(FrameworkProofContent.Read("emergency-preemption.json"));
            work.Remove("response_effects");
            work.Set("activities", JsonValue.NewArray());
            worldEdit?.Invoke(root);
            edit?.Invoke(authored); return PointGame.Factory(Text(root), Text(work), constructionDefinitions: Text(authored), recoveryDefinitions: recoveryWork, flowDefinitions: flows,
                retryProfiles: retryProfiles, additionalModule: additionalModule, moduleDeclaration: moduleDeclaration, moduleDocument: moduleDocument, templateDefinitions: templateDefinitions, knowledgeDefinitions: knowledgeDefinitions,
                extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments, additionalDocuments: additionalDocuments, startupProfiles: startupProfiles);
        }
        internal static SimGame NewPoints(GameFactory factory, params string[] mods)
        { var request = new NewGameRequest { Scenario = "points", Seed = 719 }; foreach (string mod in mods) request.Mods.Add(mod); request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static WorkView View(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game)
        { var list = new List<PlanView>(); View(game).CopyPlans(list); return list; }
        private static PlanView Read(SimGame game, long id) => Plans(game).Find(p => p.Id == id);
        private static JsonValue Location(SimGame game, int at)
            => game.World.Space is TileMap ? TilePlacement.Arguments(game.World.Space.Id, new TileCoord(at, 16))
                : Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + at + "}");
        private static PlanIntent Build(SimGame game, string prototype, int at, bool paused = true, int priority = 70)
        {
            var arguments = JsonValue.NewObject(); arguments.Set("placement", Location(game, at));
            return new PlanIntent("build:" + prototype, arguments, "field.construction", priority, paused);
        }
        private static long Submit(Check c, SimGame game, PlanIntent intent)
        { CommandResult result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent }); c.That(result.Accepted, "player submits construction intent"); return result.Arg0; }
        private static void Pause(Check c, SimGame game, long id, bool paused)
            => c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = id, Group = "field.construction", Priority = 70, Paused = paused }).Accepted, "player changes shared plan control");
        private static Entity FindDefinition(SimGame game, string definition)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; return null; }
        private static int CountDefinition(SimGame game, string definition)
        { int n = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) n++; return n; }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 480 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " plans=" + string.Join(";", Plans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void Admission(Check c)
        {
            ActivityPlanTests.Policy(c);
            GameFactory pointFactory = Points(); SimGame points = NewPoints(pointFactory);
            c.That(!(points.World.Space is TileMap), "point construction does not select a tile map");
            c.That(ReferenceEquals(points.World.Services.Providers<ConstructionService>().Single(), points.World.Services.Get<IConstructionProjects>()), "projects contract aliases the same construction owner");
            IConstructionDefinition definition = points.World.Content.Get<EntityDef>("point.tower").Component<IConstructionDefinition>();
            c.That(definition != null && definition.SiteDefinition.AuthoringPath.Contains("point-construction.json:entities"), "site prototype comes from its separately master-listed authored document");
            c.That(ReferenceEquals(definition.SiteDefinition.Component<IConstructionSiteDefinition>().Target, definition.TargetDefinition), "site/target contracts alias exact catalog definitions");
            JsonValue arguments = Content.Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}");
            var intent = new PlanIntent("build:point.tower", arguments, "group", 1, true);
            arguments.Get("placement").Set("position", JsonValue.Number("30"));
            c.Equal("12", intent.Arguments.Get("placement").Get("position").Text, "caller JSON mutation does not change intent");
            c.Throws<InvalidOperationException>(() => intent.Arguments.Get("placement").Set("position", JsonValue.Number("13")), "nested intent data frozen");
            ulong hash = points.Hash();
            c.Equal(Code.None, View(points).PreviewPlan(intent), "point syntax and submission valid");
            c.Equal(Codes.InvalidArgument, View(points).PreviewPlan(new PlanIntent("build:point.tower", Content.Json.Parse("{\"placement\":{\"x\":12}}"), null, 1, true)), "wrong geometry schema refuses");
            c.Equal(Codes.InvalidArgument, View(points).PreviewPlan(new PlanIntent("build:point.tower", Content.Json.Parse("{\"placement\":{\"space\":\"other\",\"position\":12}}"), null, 1, true)), "wrong selected space refuses");
            c.Equal(Codes.OutOfBounds, View(points).PreviewPlan(Build(points, "point.tower", 101)), "authored point bounds apply without a tile cap");
            c.Equal(hash, points.Hash(), "generic previews leave identity geometry and resources unchanged");
            long id = Submit(c, points, intent);
            c.Equal(EntityId.None, Read(points, id).Target, "pending placement is not a ghost entity");
            c.Equal(0, points.World.Services.Get<WorkBoard>().Orders.Count, "paused construction has no project order");
            c.Equal(0, CountDefinition(points, "point.tower-site"), "paused construction creates no site");
            c.Equal(0, CountDefinition(points, "point.tower"), "paused construction creates no completed object");
            var reordered = new PlanIntent("build:point.tower", Content.Json.Parse("{\"placement\":{\"position\":12.0,\"space\":\"field.points\"}}"), null, 999, true);
            c.Equal(PlanCodes.Duplicate, View(points).PreviewPlan(reordered), "identity uses provider geometry not JSON order or decimal spelling");
            var privatePlans = new List<PlanView>(); points.Seat(1).View.Feature<WorkView>().CopyPlans(privatePlans);
            c.Equal(0, privatePlans.Count, "construction intention stays company private");
            c.That(TestKit.Order(points, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "pending construction can cancel");
            c.Equal(0, CountDefinition(points, "point.tower-site"), "pending cancellation leaves no fabricated cleanup project");
            c.Equal(points.Hash(), pointFactory.Load(points.Save()).Hash(), "pending cancelled placement history saves");

            var direct = new ConstructCommand { Def = "point.tower", Placement = Location(points, 60), Priority = 5 };
            hash = points.Hash(); c.Equal(Code.None, points.Seat(0).Preview(direct), "direct point construction shares actual admission");
            c.Equal(hash, points.Hash(), "direct placement preview is pure");
            points.Seat(0).Submit(direct); SimGame directTwin = pointFactory.Load(points.Save()); Tick(points); Tick(directTwin);
            c.Equal(points.Hash(), directTwin.Hash(), "queued generic direct construction command saves before execution");
            Entity directSite = FindDefinition(points, "point.tower-site");
            c.That(directSite != null, "direct command creates the same authored physical site");
            c.Equal(5, points.World.Services.Get<WorkBoard>().Order(directSite.Get<IConstructionSite>().Order).Priority, "explicit command priority belongs to real native order");
            c.Equal(1, Plans(points).Count, "direct construction creates no parallel persistent intent");
            c.Equal(Codes.InvalidArgument, points.Seat(0).Preview(new ConstructCommand { Def = "point.tower", Placement = Location(points, 101), Priority = -1 }), "negative priority refuses without a fallback");
            c.That(TestKit.Order(points, 0, new CancelConstructionCommand { Site = directSite.Id }).Accepted, "direct point project cancellation uses the same site owner");
            c.Equal(0, CountDefinition(points, "point.tower-site"), "direct empty site cancels without tile geometry");

            foreach (string fault in new[] { "missing-site", "wrong-target", "starting-stock", "site-capacity", "site-filter" })
                c.Throws<ContentException>(() => NewPoints(Points(root =>
                {
                    JsonValue target = root.Get("entities")[0], site = root.Get("entities")[1];
                    if (fault == "missing-site") target.Get("components").Get("construction").Remove("site");
                    if (fault == "wrong-target") site.Get("components").Get("site").Set("target", JsonValue.Of("source"));
                    if (fault == "starting-stock") site.Get("components").Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":1}"));
                    if (fault == "site-capacity") site.Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("fixture.count.4"));
                    if (fault == "site-filter") site.Get("components").Get("storage").Set("phases", Content.Json.Parse("[\"liquid\"]"));
                })), "invalid authored " + fault + " refuses before Initialize");

            GameFactory tilesFactory = Colony.Factory(); SimGame tiles = ResourceDispositionTests.New(tilesFactory);
            Entity obstructing = Spawn(tiles, "tower", 10, 16); PlanIntent blocked = Build(tiles, "tower", 10, false);
            c.Equal(Codes.Blocked, tiles.Seat(0).View.Feature<ConstructionView>().CheckPlacement("tower", Location(tiles, 10)), "same current placement owner diagnoses occupancy");
            c.Equal(Code.None, View(tiles).PreviewPlan(blocked), "valid occupied placement intent can be recorded");
            long waiting = Submit(c, tiles, blocked);
            c.Equal(PlanState.Pending, Read(tiles, waiting).State, "occupied intent waits without project creation");
            c.Equal(Codes.Blocked, Read(tiles, waiting).Blocker, "current blocker belongs to engine");
            c.Equal(0, CountDefinition(tiles, "site:tower"), "waiting placement holds no ghost occupancy");
            SimGame waitingTwin = tilesFactory.Load(tiles.Save()); c.Equal(tiles.Hash(), waitingTwin.Hash(), "blocked geometry intent saves exactly");
            c.That(TestKit.Order(tiles, 0, new DemolishCommand { Target = obstructing.Id }).Accepted, "player removes actual placement obstruction physically");
            Reach(tiles, () => Read(tiles, waiting).State == PlanState.Active, "placement re-admits after timed demolition");
            c.That(tiles.Tick > 1 && Read(tiles, waiting).Target.IsValid, "native time and current placement alone create actual site");

            foreach (string fault in new[] { "wrong-target", "extra-order" })
            {
                JsonValue root = Content.Json.Parse(PointGame.Definitions);
                string provider = "{\"planned_inspections\":[{\"id\":\"plan.fault\",\"admission_target\":\"source\",\"activation_fault\":\"" + fault + "\",\"player_orderable\":true,\"priority\":1,\"automatic_supply\":false,\"nodes\":[{\"id\":\"inspect\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}}]}]}";
                GameFactory faultyFactory = PointGame.Factory(Text(root), additionalModule: new PlanProofModule(),
                    moduleDeclaration: "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"plan-fault.json\"]}", moduleDocument: provider);
                var request = new NewGameRequest { Scenario = "points", Seed = 720 }; request.Mods.Add("proof.plans");
                SimGame faulty = faultyFactory.NewGame(request);
                long planned = Submit(c, faulty, new PlanIntent("activity:plan.fault", Content.Json.Parse("{\"target\":" + FindDefinition(faulty, "source").Id.Value + "}"), null, 1, true));
                faulty.Seat(0).Submit(new ConfigurePlanCommand { Plan = planned, Priority = 1, Paused = false }); faulty.Update();
                c.That(faulty.Fault != null, "provider " + fault + " faults the actual Update instead of a substitute execution");
                c.Throws<InvalidOperationException>(() => faulty.Save(), "partial activation cannot checkpoint");
                c.Throws<InvalidOperationException>(() => faulty.Hash(), "partial activation cannot present a successful hash");
            }
        }
        private static void Physical(Check c)
        {
            ActivityPlanTests.Physical(c);
            foreach (bool point in new[] { false, true })
            {
                GameFactory factory = point ? Points() : Colony.Factory(); SimGame game = point ? NewPoints(factory) : ResourceDispositionTests.New(factory);
                if (point) EmergencyPreemptionTests.Configure(c, game);
                string prototype = point ? "point.tower" : "tower", siteDef = point ? "point.tower-site" : "site:tower";
                long id = Submit(c, game, Build(game, prototype, point ? 12 : 10));
                c.Equal(0, CountDefinition(game, prototype), "intent submission does not complete construction");
                Pause(c, game, id, false);
                Reach(game, () => Read(game, id).State == PlanState.Active, "native site admission");
                PlanView plan = Read(game, id); Entity site = game.World.Entities.Get(plan.Target);
                c.That(site != null && site.Def.Id == siteDef && site.Get<IConstructionSite>().Order == plan.Order, "plan links actual authored site/order contracts");
                Order order = game.World.Services.Get<WorkBoard>().Order(plan.Order);
                c.That(ReferenceEquals(order.Plan, game.World.Content.Get<EntityDef>(prototype).Component<IConstructionDefinition>()), "execution graph aliases authored construction definition");
                c.Equal(1, CountDefinition(game, siteDef), "one physical site only");
                c.Equal(0, CountDefinition(game, prototype), "unfinished physical work has no completed entity");
                Pause(c, game, id, true); var progress = new List<WorkProgress>(); foreach (ActivityNodeExecution node in order.Nodes) progress.Add(node.Progress);
                SimGame twin = factory.Load(game.Save());
                for (int i = 0; i < 8; i++) { Tick(game); Tick(twin); }
                for (int i = 0; i < progress.Count; i++) c.Equal(progress[i], order.Nodes[i].Progress, "paused construction earns no graph progress");
                c.Equal(game.Hash(), twin.Hash(), "actual paused site/claims/graph saves identically");
                Pause(c, game, id, false); Pause(c, twin, id, false);
                Reach(game, () => Read(game, id).State == PlanState.Done, "physical supplied construction completes", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved construction physical supply/travel/completion identical");
                Entity built = FindDefinition(game, prototype);
                c.That(built != null && built.Id != site.Id, "normal completed entity exists only after native effect");
                c.Equal(0, CountDefinition(game, siteDef), "empty actual site retires on real completion");
                c.Equal(1, CountDefinition(game, prototype), "completed target created exactly once");
                c.Equal(site.Id, Read(game, id).Target, "terminal history captures actual old execution target");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed placement intent history durable");
                if (point)
                {
                    c.Equal(Fix.FromInt(12), ((PointSnapshot)built.Get<ISpatialPresence>().Capture()).Position, "point geometry retained without tile conversion");
                    ResourceId resource = game.World.Content.ResourceId("supply");
                    c.Equal(Fix.FromInt(12), FindDefinition(game, "source").Get<IOutletPort>().Amount(resource), "eight actual supplied inputs consumed without spawning free material");
                }
                else c.Equal(new TileCoord(10, 16), built.Get<ITilePresence>().Tile, "tile geometry retained by its provider");
            }
            GameFactory cancelFactory = Points(); SimGame cancel = NewPoints(cancelFactory);
            long cancelled = Submit(c, cancel, Build(cancel, "point.tower", 20, false));
            c.That(TestKit.Order(cancel, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { cancelled }) }).Accepted, "active empty site cancellation uses native disposition");
            c.Equal(PlanState.Cancelled, Read(cancel, cancelled).State, "actual native cancellation settles generic intent");
            c.Equal(0, CountDefinition(cancel, "point.tower-site"), "cancelled empty point site physically retires through same construction owner");
            c.Equal(cancel.Hash(), cancelFactory.Load(cancel.Save()).Hash(), "cancelled generic construction history saves");
        }
    }
}
