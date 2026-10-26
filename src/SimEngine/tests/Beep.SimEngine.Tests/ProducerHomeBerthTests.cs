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
    internal static partial class ProducerHomeBerthTests
    {
        internal const string Case = "Producer home: idle output clears service access for physical replenishment";
        internal const string ControlCase = "Producer home: parking honors manual hold and reports incompatible authored roles";
        internal const string PointCase = "Producer home: independent point docks share parking movement and provider fault guards";
        internal const string BlockedCase = "Producer home: occupied parking and operating areas retry through physical saved movement";
        internal const string HomeOnlyCase = "Producer home: parking-only facilities do not invent service capabilities";
        internal const string RetargetCase = "Producer home: arrived parking stops superseded home motion without drift";
        internal const string RankingCase = "Producer home: parking ranks costs before route probes and arrival never searches";
        internal const string RankingRulesCase = "Producer home: ranked parking preserves ties route refusals and stable blockers";
        internal const string ArrivedAdmissionCase = "Producer home: arrived parking admits only its actual slot and rejects incompatible arrival";
        internal static void Register()
        {
            TestRunner.Add(Case, Run); TestRunner.Add(ControlCase, Controls); TestRunner.Add(PointCase, Points);
            TestRunner.Add(BlockedCase, Blocked); TestRunner.Add(HomeOnlyCase, HomeOnly);
            TestRunner.Add(RetargetCase, Retarget);
            TestRunner.Add(RankingCase, RankedParking);
            TestRunner.Add(RankingRulesCase, RankingRules);
            TestRunner.Add(ArrivedAdmissionCase, ArrivedAdmission);
            TestRunner.Add(LoadedParkingCase, LoadedParking);
            TestRunner.Add(LoadedWideParkingCase, LoadedWideParking);
            TestRunner.Add(IndependentWideParkingCase, IndependentWideParking);
        }
        internal static GameFactory Factory(Action<JsonValue> edit = null, string document = "producer-home-berth.json", bool independentSpace = false, bool wideAreas = false)
        {
            var world = Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("berth.start"));
            var modules = world.Get("scenarios")[0].Get("modules");
            modules.Add(JsonValue.Of(TransformationModule.ModuleId)); modules.Add(JsonValue.Of(LogisticsModule.ModuleId)); modules.Add(JsonValue.Of(TileConditionsModule.ModuleId));
            var master = Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            master.Get("modules").Add(Json.Parse("{\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"" + document + "\"]}"));
            master.Get("modules").Add(Json.Parse("{\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            master.Get("modules").Add(Json.Parse("{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[]}"));
            var fixture = Json.Parse(FrameworkProofContent.Read(document)); edit?.Invoke(fixture);
            var documents = new Dictionary<string,string> { [document] = fixture.ToString() };
            var providers = new List<GameModule> { new TransformationModule(), new LogisticsModule(), new TileConditionsModule() };
            if (independentSpace)
            {
                world.Remove("tile_spaces"); modules.Add(JsonValue.Of("proof.tile-sensor-space"));
                master.Get("modules").Add(Json.Parse("{\"id\":\"proof.tile-sensor-space\",\"version\":\"1.0.0\",\"documents\":[\"producer-home-independent-space.json\"]}"));
                documents.Add("producer-home-independent-space.json", FrameworkProofContent.Read("producer-home-independent-space.json"));
                providers.Add(new IndependentTileSensorModule());
            }
            if (wideAreas)
            {
                var declarations = master.Get("modules");
                for (int i = 0; i < declarations.Count; i++) if (declarations[i].Get("id").Text == WorkModule.ModuleId)
                    declarations[i].Get("documents").Add(JsonValue.Of("producer-home-wide-areas.json"));
                documents.Add("producer-home-wide-areas.json", FrameworkProofContent.Read("producer-home-wide-areas.json"));
            }
            return LiveSensorTests.Factory(world: world.ToString(), master: master.ToString(),
                additionalDocuments: documents, additionalModules: providers);
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("home parking fault/" + game.Fault + "/" + twin?.Fault); }
        private static CommandResult Enqueue(SimGame game, string recipe) => TestKit.Order(game, 0,
            new EnqueueTransformationCommand { Transformer = Find(game, "berth.producer").Id, Capability = new Code("production"), Transformation = recipe, Count = 1 });
        private static void Run(Check c)
        {
            var factory = Factory(); var game = LiveSensorTests.New(factory); var producer = Find(game, "berth.producer");
            var isolated = LiveSensorTests.New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            foreach (string recipe in new[] { "berth.worker", "berth.product" })
                c.That(Enqueue(game, recipe).Accepted, "ordinary authored production queued");
            var view = game.Seat(0).View.Feature<TransformationManagementView>();
            TransformationSnapshot State() { if (!view.TryGetTransformation(producer.Id, new Code("production"), out var state)) throw new InvalidOperationException("missing actual production"); return state; }
            var twin = factory.Load(game.Save()); bool savedParking = false, savedCargo = false, moved = false;
            for (int i = 0; i < 120 && State().QueueCount != 0; i++)
            {
                Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "every saved production/home/haul update matches");
                var output = game.World.Entities.All.FirstOrDefault(e => e.Def.Id == "berth.observer");
                if (output != null && output.Get<ITilePresence>().Tile != new TileCoord(8, 5)) moved = true;
                if (!savedParking && output?.Get<IMotion>().Status == MoveStatus.Moving)
                { twin = factory.Load(game.Save()); savedParking = true; c.Equal(game.Hash(), twin.Hash(), "actual partial parking motion saved through original codec"); }
                if (!savedCargo && Find(game, "berth.carrier").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")).IsPositive)
                { twin = factory.Load(game.Save()); savedCargo = true; c.Equal(game.Hash(), twin.Hash(), "actual loaded supply saved without a parking ledger"); }
                c.That(game.World.Entities.All.Where(e => e.Get<ITilePresence>()?.Movable == true)
                    .GroupBy(e => e.Get<ITilePresence>().Tile).All(group => group.Count() == 1), "moving output and carrier never overlap");
            }
            var workers = game.World.Entities.All.Where(e => e.Def.Id == "berth.observer").ToArray();
            c.Equal(1, workers.Length, "full authored first worker produced once");
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            Console.WriteLine("BERTH time=" + game.Tick + ";phase=" + State().Phase + ";queue=" + State().QueueCount +
                ";workers=" + string.Join(";", workers.Select(e => e.Id + "/" + e.Get<ITilePresence>().Position + "/" + WorkerHomeContracts.Read(game.World, e).State)) +
                ";jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Loaded)));
            c.Equal(0, State().QueueCount, "idle produced worker must not permanently obstruct subsequent physical replenishment");
            c.That(savedParking && savedCargo && moved, "actual timed home departure and loaded travel were both exercised");
            c.Equal(new TileCoord(7, 6), workers.Single().Get<ITilePresence>().Tile, "engine uses authored parking, not the service berth");
            c.Equal(producer.Id, workers.Single().Get<IWorker>().Home, "one original worker home binding remains producer-owned");
            c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, workers.Single()).State, "home readiness uses actual parking arrival");
            Fix total = State().Wip.Stock.AmountOf(game.World.Content.ResourceId("steel"));
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory stock) total += stock.Amount(game.World.Content.ResourceId("steel"));
            c.Equal(Fix.FromInt(17), total, "20 real stock minus4 input plus1 product; no cargo loss or grants");
            c.Equal(isolatedHash, isolated.Hash(), "parking reads/travel do not affect another session");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "normal manufacturing/home/haul sequence replays identically/" + replay.Failure);
        }
        private static void RankedParking(Check c)
        {
            foreach (bool arrived in new[] { false, true })
            {
                var factory = PointFactory(root => {
                    var dock = root.Get("entities")[1].Get("components").Get("point_dock");
                    dock.Set("resources", Json.Parse("[]"));
                    dock.Set("slots", Json.Parse("[{\"id\":\"a.far\",\"kind\":\"parking\",\"offset\":3},{\"id\":\"b.near\",\"kind\":\"parking\",\"offset\":" + (arrived ? "-4" : "-3") + "},{\"id\":\"c.middle\",\"kind\":\"parking\",\"offset\":-1}]"));
                });
                var game = NewPoint(factory); var worker = Find(game, "machine.d"); var home = Find(game, "destination");
                var dock = home.Get<PointDock>(); int before = dock.ReachChecks; ulong hash = game.Hash();
                bool declared = game.World.Services.Get<WorkBoard>().ReadHomeParking(home, worker, out var selected, out var status, out var blocker);
                c.That(declared && blocker.IsNone, "actual authored parking query succeeds");
                c.Equal(new Code("b.near"), selected, "nearest reachable or current actual slot wins, not first ID");
                c.Equal(arrived ? SpatialInteractionStatus.WithinReach : SpatialInteractionStatus.Reachable, status, "truthful independent actual geometry");
                Console.WriteLine("parking ranked query;arrived=" + arrived + ";reach-checks=" + (dock.ReachChecks - before));
                c.Equal(arrived ? 0 : 1, dock.ReachChecks - before, "arrived needs no route search; nearest success avoids dominated route probes");
                c.Equal(hash, game.Hash(), "diagnostic counter is not gameplay state; selection is pure");
                c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = home.Id }).Accepted, "ordinary home command retains physical execution");
                var twin = factory.Load(game.Save());
                for (int i = 0; i < 5; i++) { Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "ranked parking motion and arrival save identically"); }
                c.Equal(Fix.FromInt(arrived ? 6 : 7), worker.Get<PointPresence>().Position, "no remote birth or movement beyond actual parking");
                c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "actual arrival remains stable");
            }
        }

        private static void RankingRules(Check c)
        {
            foreach (string mode in new[] { "tie", "zero", "refuse", "refuse-route", "blocked-best", "first-blocker" })
            {
                var factory = PointFactory(root => {
                    var dock = root.Get("entities")[1].Get("components").Get("point_dock");
                    dock.Set("resources", Json.Parse("[]"));
                    dock.Set("slots", Json.Parse("[{\"id\":\"a.far\",\"kind\":\"parking\",\"offset\":3},{\"id\":\"b.near\",\"kind\":\"parking\",\"offset\":-1}]"));
                    if (mode == "zero" || mode == "refuse" || mode == "refuse-route") dock.Set("fault", JsonValue.Of(mode == "zero" ? "zero-cost" : "refuse-cost"));
                    StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number(mode == "blocked-best" || mode == "refuse-route" ? "14" : "11"));
                    if (mode == "blocked-best" || mode == "refuse-route") root.Get("entities")[5].Get("components").Get("point_motion").Set("area_fault", JsonValue.Of("departure-blocked"));
                    if (mode == "first-blocker") StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("13"));
                });
                var game = NewPoint(factory); var worker = Find(game, "machine.d"); var home = Find(game, "destination");
                if (mode == "first-blocker") c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "actual route closes through ordinary engine command");
                ulong hash = game.Hash(); int before = home.Get<PointDock>().ReachChecks;
                c.That(game.World.Services.Get<WorkBoard>().ReadHomeParking(home, worker, out var selected, out var status, out var blocker), "actual ranked authored layout/" + mode);
                if (mode == "refuse" || mode == "refuse-route" || mode == "first-blocker")
                {
                    c.Equal(Code.None, selected, "failed candidates publish no fabricated parking/" + mode);
                    c.Equal(mode == "refuse" ? WorkSystem.BlockerUnreachable : mode == "refuse-route" ? MotionAreaCodes.Outside : WorkDockCodes.Occupied, blocker, "original stable-ID typed refusal wins, not evaluation-order accident/" + mode);
                }
                else
                {
                    c.Equal(new Code(mode == "blocked-best" ? "b.near" : "a.far"), selected, "cost ties use stable ID and blocked cheapest permits next valid route/" + mode);
                    c.Equal(Code.None, blocker, "valid ranked result has no stale refusal/" + mode);
                    c.Equal(SpatialInteractionStatus.Reachable, status, "even zero cost never invents actual arrival/" + mode);
                    c.Equal(mode == "blocked-best" ? 2 : 1, home.Get<PointDock>().ReachChecks - before, "only necessary route candidates are evaluated/" + mode);
                }
                c.Equal(hash, game.Hash(), "ranked refusal/cost observation is pure/" + mode);
            }
        }

        private static void ArrivedAdmission(Check c)
        {
            foreach (string mode in new[] { "clear", "occupied-first", "incompatible", "area-first-blocker" })
            {
                var factory = PointFactory(root => {
                    var dock = root.Get("entities")[1].Get("components").Get("point_dock");
                    dock.Set("resources", Json.Parse("[]"));
                    dock.Set("slots", Json.Parse("[{\"id\":\"a.far\",\"kind\":\"parking\",\"offset\":3},{\"id\":\"b.current\",\"kind\":\"parking\",\"offset\":-4,\"work_role\":\"inspect\"},{\"id\":\"c.middle\",\"kind\":\"parking\",\"offset\":-1}]"));
                    if (mode == "occupied-first" || mode == "area-first-blocker") StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("13"));
                    if (mode == "incompatible") root.Get("entities")[5].Get("components").Get("worker").Set("roles", Json.Parse("[\"haul\"]"));
                    if (mode == "area-first-blocker") root.Set("point_interval_regions", Json.Parse("[{\"id\":\"admit.only-routes\",\"space\":\"field.points\",\"minimum\":8,\"maximum\":14}]"));
                });
                var game = NewPoint(factory); var worker = Find(game, "machine.d"); var home = Find(game, "destination");
                bool blocked = mode == "area-first-blocker";
                if (blocked) c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand {
                    Worker = worker.Id, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, "admit.only-routes", true, true, WorkAssignmentMode.All)
                }).Accepted, "ordinary area policy excludes the current arrived tile");
                var dock = home.Get<PointDock>(); int vacancies = dock.VacancyChecks, costs = dock.CostChecks;
                ulong hash = game.Hash();
                c.That(game.World.Services.Get<WorkBoard>().ReadHomeParking(home, worker, out var selected, out var status, out var blocker), "actual original parking owner/" + mode);
                c.Equal(blocked ? WorkDockCodes.Occupied : Code.None, blocker, "valid admission or original earlier-ID refusal, not arrived-pass order/" + mode);
                c.Equal(blocked ? Code.None : new Code(mode == "incompatible" ? "c.middle" : "b.current"), selected, "arrival never overrides authored role or area eligibility/" + mode);
                c.Equal(blocked ? SpatialInteractionStatus.Unreachable : mode == "incompatible" ? SpatialInteractionStatus.Reachable : SpatialInteractionStatus.WithinReach, status, "actual eligibility and geometry agree/" + mode);
                Console.WriteLine("parking arrived admission;mode=" + mode + ";vacancy=" + (dock.VacancyChecks - vacancies) + ";cost=" + (dock.CostChecks - costs));
                if (mode == "clear" || mode == "occupied-first")
                {
                    c.Equal(1, dock.VacancyChecks - vacancies, "only the actual arrived slot needs vacancy admission/" + mode);
                    c.Equal(0, dock.CostChecks - costs, "arrived workers estimate no irrelevant alternative/" + mode);
                }
                c.Equal(hash, game.Hash(), "pure arrived-slot observations publish no persistent selection state/" + mode);
                c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = home.Id }).Accepted, "normal home intent, not driver relocation/" + mode);
                var twin = factory.Load(game.Save());
                for (int i = 0; i < 5; i++) { Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "arrived admission and physical fallback save deterministically/" + mode); }
                c.Equal(Fix.FromInt(mode == "incompatible" ? 9 : 6), worker.Get<PointPresence>().Position, "compatible arrival stays; incompatible arrival physically moves/" + mode);
                c.Equal(blocked ? WorkerHomeState.Blocked : WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "final truthful eligibility, not physical proximity alone/" + mode);
            }
        }

        private static void Controls(Check c)
        {
            foreach (bool incompatible in new[] { false, true })
            {
                var factory = Factory(incompatible ? data => data.Get("entities")[0].Get("components").Get("tile_work_dock").Get("slots")[2].Set("work_role", JsonValue.Of("haul")) : null);
                var game = LiveSensorTests.New(factory); c.That(Enqueue(game, "berth.worker").Accepted, "ordinary worker manufacture");
                for (int i = 0; i < 20 && !game.World.Entities.All.Any(e => e.Def.Id == "berth.observer"); i++) Step(game);
                var worker = Find(game, "berth.observer"); ulong hash = game.Hash();
                var home = WorkerHomeContracts.Read(game.World, worker);
                c.Equal(hash, game.Hash(), "parking readiness is pure");
                if (incompatible)
                {
                    c.Equal(WorkerHomeState.Blocked, home.State, "unmatched parking role is blocked, never service-contact fallback");
                    c.Equal(WorkDockCodes.Incompatible, home.Blocker, "typed authored parking incompatibility");
                    for (int i = 0; i < 8; i++) Step(game);
                    c.Equal(new TileCoord(8, 5), worker.Get<ITilePresence>().Tile, "no invented parking place or teleport");
                }
                else
                {
                    c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker.Id, Held = true }).Accepted, "player can hold a produced worker during home travel");
                    var at = worker.Get<ITilePresence>().Position; var twin = factory.Load(game.Save());
                    for (int i = 0; i < 8; i++) { Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "held parking travel survives saved continuation"); }
                    c.Equal(at, worker.Get<ITilePresence>().Position, "home policy cannot move a manually held worker");
                    c.Equal(WorkerHomeState.Held, WorkerHomeContracts.Read(game.World, worker).State, "manual hold precedes home policy");
                    c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker.Id, Held = false }).Accepted, "normal release resumes engine-owned parking");
                    for (int i = 0; i < 20 && WorkerHomeContracts.Read(game.World, worker).State != WorkerHomeState.AtHome; i++) Step(game);
                    c.Equal(new TileCoord(7, 6), worker.Get<ITilePresence>().Tile, "released worker physically finishes authored home route");
                }
            }
        }
        private static GameFactory PointFactory(Action<JsonValue> edit = null)
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var data = Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            data.Get("entities")[1].Get("components").Get("point_dock").Get("slots")
                .Add(Json.Parse("{\"id\":\"home.park\",\"kind\":\"parking\",\"offset\":-1,\"work_role\":\"haul\"}"));
            root.Set("entities", data.Get("entities")); root.Set("work_policies", data.Get("work_policies"));
            StartupContentAuthoring.UseEntries(root, data.Get("entity_setups")); edit?.Invoke(root);
            return PointGame.Factory(root.ToString(), additionalDocuments: new Dictionary<string,string> {
                ["home-parking-areas.json"] = "{\"point_interval_regions\":[{\"id\":\"home.route\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":8},{\"id\":\"home.parking\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":9}]}" });
        }
        private static SimGame NewPoint(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 509, Seats = { Controller.Human, Controller.Human } });
        private static void AddPointHome(JsonValue root)
        {
            root.Get("entities").Add(Json.Parse("{\"id\":\"home.clear\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":20}}}"));
            StartupContentAuthoring.Add(root, "home.clear", "home.clear", 0, Json.Parse("{\"space\":\"field.points\",\"position\":20}"));
        }
        private static void Retarget(Check c)
        {
            var factory = PointFactory(AddPointHome);
            var game = NewPoint(factory); var worker = Find(game, "machine.d");
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = Find(game, "home.clear").Id }).Accepted, "first home starts real independent motion");
            Step(game); Step(game);
            Fix position = worker.Get<PointPresence>().Position;
            c.Equal(Fix.FromInt(9), position, "three elapsed updates earn three real travel steps");
            c.Equal(MoveStatus.Moving, worker.Get<IMotion>().Status, "superseded home has actual pending motion");
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = Find(game, "destination").Id }).Accepted, "player selects parking at current actual location");
            c.Equal(position, worker.Get<PointPresence>().Position, "arrived home intent stops the old goal before movement, without one-step drift");
            c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "retargeted arrival is truthful in the same update");
            c.That(worker.Get<IMotion>().Status != MoveStatus.Moving, "arrival retains no motion toward the superseded home");
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++)
            {
                Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "retargeted arrived parking survives saved continuation");
                c.Equal(position, worker.Get<PointPresence>().Position, "arrived worker never resumes superseded home motion");
            }
        }
        private static void Points(Check c)
        {
            foreach (string fault in new[] { "none", "vacancy-touch", "geometry-null", "invalid-reach",
                "arrival-touch", "arrival-payload", "arrival-carrier-touch", "arrival-carrier-payload", "arrival-space", "arrival-clock", "arrival-throw",
                "intent-touch", "intent-payload", "intent-carrier-touch", "intent-carrier-payload", "intent-space", "intent-clock", "intent-throw" })
            {
                var factory = PointFactory(root => {
                    if (fault != "none") root.Get("entities")[1].Get("components").Get("point_dock").Set("fault", JsonValue.Of(fault)); });
                var game = NewPoint(factory);
                var worker = Find(game, "machine.d"); var home = Find(game, "destination");
                var command = new SetHomeCommand { Worker = worker.Id, Home = home.Id };
                if (fault == "none")
                {
                    ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "independent geometry admits authored parking");
                    c.Equal(hash, game.Hash(), "independent parking preview is pure");
                    c.That(TestKit.Order(game, 0, command).Accepted, "ordinary home intent uses independent dock provider");
                    var twin = factory.Load(game.Save());
                    for (int i = 0; i < 20 && WorkerHomeContracts.Read(game.World, worker).State != WorkerHomeState.AtHome; i++)
                    { Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "independent saved parking travel matches"); }
                    c.Equal(Fix.FromInt(9), worker.Get<PointPresence>().Position, "actual point-space position is authored parking, not home/service anchor");
                    c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "point provider reports truthful parking readiness");
                    c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadHome(worker.Id, out var hidden), "foreign home view stays private");
                    c.That(hidden == null, "no parking policy or position leaks through refusal");
                }
                else
                {
                    long time = game.Tick; Exception thrown = null;
                    try { game.Seat(0).Preview(command); } catch (Exception error) { thrown = error; }
                    c.That(thrown != null && game.Fault != null, "parking inherits original dock purity/geometry/status guards/" + fault);
                    var first = game.Fault; game.Update();
                    c.That(ReferenceEquals(first, game.Fault) && game.Tick == time, "failed parking provider cannot resume work/" + fault);
                    if (fault.StartsWith("intent-", StringComparison.Ordinal))
                    {
                        c.Throws<InvalidOperationException>(() => game.Save(), "failed intent observation cannot publish a healthy save/" + fault);
                        if (fault == "intent-throw") c.Equal("dock.probe.intent.throw", thrown?.Message, "original intent callback exception remains visible");
                    }
                }
            }
        }
        private static void Blocked(Check c)
        {
            foreach (string obstruction in new[] { "area", "occupied", "route" })
            {
                var factory = PointFactory(root => {
                    if (obstruction != "occupied") return;
                    StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("9"));
                    AddPointHome(root);
                });
                var game = NewPoint(factory); var worker = Find(game, "machine.d"); var home = Find(game, "destination");
                void Area(SimGame current, string area) => c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand {
                    Worker = worker.Id, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, area, true, true, WorkAssignmentMode.All)
                }).Accepted, "ordinary policy edits the actual parking area");
                if (obstruction == "area") Area(game, "home.route");
                if (obstruction == "route") c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "ordinary engine command closes actual route");
                c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = home.Id }).Accepted, "blocked home remains explicit future intent");
                Code blocker = obstruction == "area" ? WorkPolicyCodes.OutsideArea : obstruction == "occupied" ? WorkDockCodes.Occupied : WorkSystem.BlockerUnreachable;
                var twin = factory.Load(game.Save()); var position = worker.Get<PointPresence>().Position;
                for (int i = 0; i < 5; i++)
                {
                    Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "blocked parking keeps one durable motion/home authority/" + obstruction);
                    c.Equal(blocker, WorkerHomeContracts.Read(game.World, worker).Blocker, "typed current parking blocker/" + obstruction);
                    c.Equal(position, worker.Get<PointPresence>().Position, "blocked worker cannot overlap, teleport or use service fallback/" + obstruction);
                }
                foreach (var current in new[] { game, twin })
                {
                    if (obstruction == "area") Area(current, "home.parking");
                    if (obstruction == "route") c.That(TestKit.Order(current, 0, new SetPointAccessCommand { Open = true }).Accepted, "route reopening preserves original home intent");
                    if (obstruction == "occupied") c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = Find(current, "machine.c").Id, Home = Find(current, "home.clear").Id }).Accepted,
                        "occupant gets its own ordinary home command, never relocated by a parking query");
                }
                for (int i = 0; i < 25 && WorkerHomeContracts.Read(game.World, worker).State != WorkerHomeState.AtHome; i++)
                {
                    Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "recovered actual parking motion is deterministic/" + obstruction);
                    var moving = game.World.Entities.All.Where(e => e.Get<IMotion>() != null).ToArray();
                    c.That(moving.GroupBy(e => e.Get<PointPresence>().Position).All(group => group.Count() == 1), "physical parking recovery never overlaps another worker");
                }
                c.Equal(Fix.FromInt(9), worker.Get<PointPresence>().Position, "actual recovery reaches parking, not service or excluded home anchor/" + obstruction);
                c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "saved blocked parking becomes actual arrived state/" + obstruction);
                c.Equal(home.Id, worker.Get<IWorker>().Home, "recovery retains the player's original home identity/" + obstruction);
            }
        }
        private static void HomeOnly(Check c)
        {
            var factory = Factory(data => {
                data.Get("entities")[1].Get("components").Set("tile_work_dock", Json.Parse("{\"work_roles\":[],\"resources\":[],\"slots\":[{\"id\":\"parking\",\"kind\":\"parking\",\"x\":-1,\"y\":-2,\"work_role\":\"survey\"}]}"));
            });
            var game = LiveSensorTests.New(factory); c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game, "berth.producer").Id,
                Capability = new Code("production"), Transformation = "berth.worker", Count = 1, Home = Find(game, "berth.source").Id }).Accepted,
                "native producer uses actual launch geometry while selecting a parking-only home");
            for (int i = 0; i < 25; i++) Step(game);
            var worker = Find(game, "berth.observer");
            c.Equal(new TileCoord(7, 6), worker.Get<ITilePresence>().Tile, "parking-only native facility uses ordinary physical motion");
            c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State, "native home needs no fabricated resource service bay");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "parking-only native geometry restores through original codecs");
            var pointFactory = PointFactory(root => {
                var dock = root.Get("entities")[1].Get("components").Get("point_dock"); dock.Set("resources", Json.Parse("[]"));
                dock.Set("slots", Json.Parse("[{\"id\":\"home.park\",\"kind\":\"parking\",\"offset\":-1,\"work_role\":\"haul\"}]"));
            });
            var point = NewPoint(pointFactory); var actor = Find(point, "machine.d"); var destination = Find(point, "destination");
            c.That(TestKit.Order(point, 0, new SetHomeCommand { Worker = actor.Id, Home = destination.Id }).Accepted, "independent parking-only contract accepts explicit home intent");
            for (int i = 0; i < 15; i++) Step(point);
            c.Equal(Fix.FromInt(9), actor.Get<PointPresence>().Position, "independent geometry needs no invented service/departure");
            var request = WorkDockRequest.Haul(point.World.Content.ResourceId("supply"), ResourceHandlingPhase.Unloading);
            c.Equal(WorkDockCodes.Incompatible, destination.Get<IWorkDock>().CheckRequest(point.World, actor, request), "parking-only endpoint never invents resource service");
            c.That(!destination.Get<IWorkDock>().Slots[0].Matches(WorkDockRequest.Work(new WorkerRoleId("haul"))), "parking role never becomes a staffed-work service request");
            foreach (string slots in new[] {
                "[]",
                "[{\"id\":\"bad\",\"kind\":\"service\",\"x\":0,\"y\":1}]",
                "[{\"id\":\"bad\",\"kind\":\"departure\",\"x\":0,\"y\":1}]",
                "[{\"id\":\"bad\",\"kind\":\"waiting\",\"x\":0,\"y\":1}]",
                "[{\"id\":\"bay\",\"kind\":\"service\",\"x\":0,\"y\":1},{\"id\":\"exit\",\"kind\":\"departure\",\"x\":0,\"y\":2}]",
                "[{\"id\":\"bad\",\"kind\":\"parking\",\"x\":0,\"y\":1,\"resource\":\"steel\"}]"
            }) c.Throws<ContentException>(() => LiveSensorTests.New(Factory(data => {
                var dock = data.Get("entities")[0].Get("components").Get("tile_work_dock"); dock.Set("resources", Json.Parse("[]")); dock.Set("slots", Json.Parse(slots));
            })), "malformed parking/service geometry refuses before initialization");
        }
    }
}
