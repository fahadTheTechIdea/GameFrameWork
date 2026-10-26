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
    internal static class FleetNavigationTests
    {
        internal const string Case = "Fleet navigation: whole selection, physical queued cargo and saved continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool named = false, string policies = null, string proofPolicies = null,
            bool workControls = false, string workPolicies = null, string proofWork = null, bool homeControls = false,
            bool loadControls = false, string loadPolicies = null, string proofLoad = null,
            bool tripControls = false, string tripPolicies = null, string proofTrip = null,
            bool serviceControls = false, string servicePolicies = null, string proofService = null,
            bool outletControls = false, string outletPolicies = null, string proofOutlet = null,
            IReadOnlyList<GameModule> additionalModules=null,JsonValue additionalDeclarations=null,IReadOnlyDictionary<string,string> additionalDocuments=null,Action<JsonValue> worldEdit=null,
            bool recordFlows = false)
        {
            tripControls |= serviceControls || outletControls;
            workControls |= homeControls;
            var selected = new List<GameModule> { new ContentAccessModule(), new InventoryModule(),
                new MotionModule(), new TileNavigationModule(), new TileConditionsModule(), new WorkModule(), new CompanyKnowledgeModule(),
                new WorldSetupModule(), new OperationsModule(), new KnowledgeVisibilityModule(), new LogisticsModule() };
            if (named) { selected.Add(new MachineFleetsModule()); selected.Add(new FleetPolicyProofModule()); }
            if (workControls) selected.Add(homeControls ? (GameModule)new FleetHomeProofModule() : new FleetWorkPolicyProofModule());
            if (loadControls) selected.Add(new FleetLoadProofModule());
            if (tripControls) selected.Add(new FleetTripProofModule());
            if (serviceControls) selected.Add(new FleetServiceProofModule());
            if (outletControls) selected.Add(new FleetOutletProofModule());
            if (recordFlows) selected.Add(new ResourceFlowsModule());
            if(additionalModules!=null)selected.AddRange(additionalModules);
            var definition = new GameDefinition("native-fleet", selected);
            var world = Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            var fleet = Json.Parse(FrameworkProofContent.Read("native-fleet-navigation.json"));
            var parts = world.Get("entities")[1].Get("components");
            parts.Get("mover").Set("classes", Json.Parse("[\"survey.route\",\"terrain.route\"]"));
            parts.Get("worker").Set("roles", Json.Parse("[\"haul\"]"));
            parts.Get("work_policy").Set("profile", JsonValue.Of("fleet.enabled"));
            parts.Set("storage", Json.Parse("{\"capacity_policy\":\"fleet.cargo\",\"public\":false,\"initial_stock\":{}}"));
            parts.Set("transporter", Json.Parse("{\"handling\":\"fleet.handling\"}"));
            world.Get("entities")[0].Get("components").Set("tile_work_dock", fleet.Get("entities")[0].Get("components").Get("tile_work_dock").Clone());
            var scenario = world.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(LogisticsModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(TileConditionsModule.ModuleId));
            scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("fleet.start"));
            if (recordFlows) scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            worldEdit?.Invoke(world);
            var master = Json.Parse(FrameworkProofContent.Read("native-sensor-master.json")); var modules = master.Get("modules");
            modules.Add(Json.Parse("{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[\"native-route-conditions.json\"]}"));
            if (recordFlows) modules.Add(Json.Parse("{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-flows.json\"]}"));
            if(additionalDeclarations!=null)for(int i=0;i<additionalDeclarations.Count;i++)modules.Add(additionalDeclarations[i]);
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i]; string id = module.Get("id").Text;
                if (id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-layered-navigation-world.json"));
                if (id == "sim.tile-navigation")
                { module.Get("documents").Add(JsonValue.Of("native-layered-navigation.json")); }
                if (workControls && id == "sim.work") module.Get("documents").Add(JsonValue.Of("native-fleet-work-policies.json"));
                if (workControls && id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-fleet-work-startup.json"));
                if (loadControls && id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-fleet-load-startup.json"));
                if (tripControls && id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of(serviceControls ? "native-fleet-service-startup.json" : "native-fleet-trip-startup.json"));
            }
            modules.Add(Json.Parse("{\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"native-fleet-navigation.json\"]}"));
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString());
            if (recordFlows) files.Add("resource-flows.json", FrameworkProofContent.Read("resource-flows.json"));
            if (named)
            {
                scenario.Get("modules").Add(JsonValue.Of(MachineFleetsModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of("proof.fleet-policy"));
                modules.Add(Json.Parse("{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"},{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"native-named-fleets.json\"]}"));
                modules.Add(Json.Parse("{\"id\":\"proof.fleet-policy\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"}],\"documents\":[\"fleet-policy-proof.json\"]}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add("native-named-fleets.json", policies ?? FrameworkProofContent.Read("native-named-fleets.json"))
                    .Add("fleet-policy-proof.json", proofPolicies ?? FrameworkProofContent.Read("fleet-policy-proof.json"));
            }
            if (workControls)
            {
                string proofId = homeControls ? "proof.fleet-home" : "proof.fleet-work-policy";
                string proofDocument = homeControls ? "fleet-home-proof.json" : "fleet-work-policy-proof.json";
                scenario.Get("modules").Add(JsonValue.Of(proofId));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("fleet.work-start"));
                modules.Add(Json.Parse("{\"id\":\"" + proofId + "\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"" + proofDocument + "\"]}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add("native-fleet-work-policies.json", workPolicies ?? FrameworkProofContent.Read("native-fleet-work-policies.json"))
                    .Add("native-fleet-work-startup.json", FrameworkProofContent.Read("native-fleet-work-startup.json"))
                    .Add(proofDocument, proofWork ?? FrameworkProofContent.Read(proofDocument));
            }
            if (loadControls)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.fleet-load"));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("fleet.load-start"));
                foreach (var module in Enumerable.Range(0, modules.Count).Select(i => modules[i]))
                    if (module.Get("id").Text == LogisticsModule.ModuleId) module.Get("documents").Add(JsonValue.Of("native-fleet-load-policies.json"));
                modules.Add(Json.Parse("{\"id\":\"proof.fleet-load\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"fleet-load-proof.json\"]}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add("native-fleet-load-policies.json", loadPolicies ?? FrameworkProofContent.Read("native-fleet-load-policies.json"))
                    .Add("fleet-load-proof.json", proofLoad ?? FrameworkProofContent.Read("fleet-load-proof.json"))
                    .Add("native-fleet-load-startup.json", FrameworkProofContent.Read("native-fleet-load-startup.json"));
            }
            if (tripControls)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.fleet-trip"));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of(serviceControls ? "fleet.service-start" : "fleet.trip-start"));
                string nativeDocument = outletControls ? "native-fleet-outlet-policies.json" : serviceControls ? "native-fleet-service-policies.json" : "native-fleet-trip-policies.json";
                foreach (var module in Enumerable.Range(0, modules.Count).Select(i => modules[i]))
                    if (module.Get("id").Text == LogisticsModule.ModuleId) module.Get("documents").Add(JsonValue.Of(nativeDocument));
                modules.Add(Json.Parse("{\"id\":\"proof.fleet-trip\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":" + (serviceControls || outletControls ? "[]" : "[\"fleet-trip-proof.json\"]") + "}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add(nativeDocument, (outletControls ? outletPolicies : serviceControls ? servicePolicies : tripPolicies) ?? FrameworkProofContent.Read(nativeDocument));
                if (!serviceControls && !outletControls) files.Add("fleet-trip-proof.json", proofTrip ?? FrameworkProofContent.Read("fleet-trip-proof.json"));
                string startup = serviceControls ? "native-fleet-service-startup.json" : "native-fleet-trip-startup.json";
                files.Add(startup, FrameworkProofContent.Read(startup));
            }
            if (outletControls)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.fleet-outlet"));
                modules.Add(Json.Parse("{\"id\":\"proof.fleet-outlet\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"proof.fleet-trip\",\"version\":\"1.0.0\"}],\"documents\":[\"fleet-outlet-machines.json\"]}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add("fleet-outlet-machines.json", proofOutlet ?? FrameworkProofContent.Read("fleet-outlet-machines.json"));
            }
            if (serviceControls)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.fleet-service"));
                modules.Add(Json.Parse("{\"id\":\"proof.fleet-service\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"proof.fleet-trip\",\"version\":\"1.0.0\"}],\"documents\":[\"fleet-service-machines.json\"]}"));
                files.Add("manifest.json", master.ToString()).Add("native-sensor-world.json", world.ToString())
                    .Add("fleet-service-machines.json", proofService ?? FrameworkProofContent.Read("fleet-service-machines.json"));
            }
            foreach (string file in new[] { "native-fleet-navigation.json", "native-sensor-startup.json", "native-sensor-visibility.json",
                "native-survey-navigation.json", "native-survey-work.json", "native-survey-knowledge.json", "work-retry.json",
                "native-layered-navigation.json", "native-layered-navigation-world.json", "native-route-conditions.json" }) files.Add(file, FrameworkProofContent.Read(file));
            if(additionalDocuments!=null)foreach(var entry in additionalDocuments)files.Add(entry.Key,entry.Value);
            files.Add("open-access.json", FrameworkProofContent.Access); return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static Entity[] Fleet(SimGame game) => game.World.Entities.All.Where(e => e.Def.Id == "survey.machine").OrderBy(e => e.Id).ToArray();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Job[] Shipments(SimGame game) => Board(game).Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        private static MotionView View(SimGame game) => game.Seat(0).View.Feature<MotionView>();
        private static ConfigureMotionNavigationGroupCommand Select(EntityId[] ids, string profile)
            => new ConfigureMotionNavigationGroupCommand { Entities = ids, Profile = profile };
        private static string Profiles(Entity[] entities) => string.Join(",", entities.Select(e => e.Get<IMotionNavigation>().ReadNavigation()));
        private static Fix Total(SimGame game)
        {
            var steel = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (Entity entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory stock) total += stock.Amount(steel);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fleet step=" + game.Tick + " " + game.Fault);
            foreach (TileCoord bay in new[] { new TileCoord(15, 10), new TileCoord(31, 10) })
                if (Fleet(game).Count(e => e.Get<ITilePresence>().Tile == bay) > 1) throw new InvalidOperationException("native fleet bay overlap at " + bay);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("native fleet resource conservation step=" + game.Tick);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, Action observe = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) { Update(game, twin); observe?.Invoke(); }
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Tick + " actors=" + string.Join(";", Fleet(game).Select(e =>
                e.Id + "/" + e.Get<ITilePresence>().Position + "/" + e.Get<IMotion>().Status + "/" + e.Get<IMotionNavigation>().ReadNavigation())) +
                " jobs=" + string.Join(";", Shipments(game).Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.SourceDockSlot + "/" + j.Blocker)));
        }
        private static void Policy(Check c, SimGame game, EntityId worker, bool enabled, SimGame twin = null)
        {
            var settings = new WorkPolicySettings(enabled, 100, new[] { new WorkerRoleId("haul") }, null, true, false, WorkAssignmentMode.All);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = worker, Settings = settings }).Accepted, "normal worker policy owns physical dock pause/resume");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureWorkPolicyCommand { Worker = worker, Settings = settings }).Accepted, "saved twin receives same normal policy command");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current fleet-control identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "selection has no second persistent fleet layout");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var actors = Fleet(game); var ids = actors.Select(e => e.Id).ToArray();
            var limited = Find(game, "fleet.restricted"); var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination");
            var foreign = Find(game, "survey.foreign-sensor"); var view = View(game); var steel = game.World.Content.ResourceId("steel");
            foreach (var actor in actors.Concat(new[] { limited })) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = true }).Accepted, "player holds native candidates during policy authoring");
            c.Equal(2, actors.Length, "JSON startup creates actual two-machine selection");
            c.That(!game.World.Services.Has<IEconomy>(), "native fleet work requires no business module");
            var reversed = ids.Reverse().ToArray(); ulong hash = game.Hash();
            var preview = view.PreviewNavigationGroup(reversed, "terrain.route");
            c.Equal(Code.None, preview.Blocker, "whole selection management admission");
            c.Equal(Code.None, game.Seat(0).Preview(Select(reversed, "terrain.route")), "normal whole-selection preview parity");
            c.That(preview.Entries.Select(e => e.Entity).SequenceEqual(ids), "per-entry facts use stable entity order");
            c.That(preview.Entries.All(e => e.Selection.Profile == "survey.route"), "per-entry facts read original owners");
            c.Throws<NotSupportedException>(() => ((IList<MotionNavigationSelectionEntry>)preview.Entries).Clear(), "selection projection is immutable");
            foreach (var bad in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
            {
                var refusal = view.PreviewNavigationGroup(bad, "terrain.route");
                c.Equal(Codes.InvalidArgument, refusal.Blocker, "malformed selection refuses without membership defaults");
                c.Equal(0, refusal.Entries.Count, "malformed selection exposes no admitted prefix");
                c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(Select(bad, "terrain.route")), "normal malformed-set refusal parity");
            }
            var mixed = ids.Concat(new[] { limited.Id }).Reverse().ToArray(); var blocked = view.PreviewNavigationGroup(mixed, "terrain.route");
            c.Equal(MotionNavigationCodes.NotAllowed, blocked.Blocker, "one restricted member refuses complete set");
            c.Equal(2, blocked.Entries.Count(e => e.Blocker.IsNone), "truthful valid members are not silently discarded");
            c.Equal(MotionNavigationCodes.NotAllowed, blocked.Entries.Single(e => e.Entity == limited.Id).Blocker, "explicit member blocker");
            var hidden = view.PreviewNavigationGroup(ids.Concat(new[] { foreign.Id }).ToArray(), "terrain.route");
            c.Equal(Codes.NotFound, hidden.Blocker, "private selection hides foreign existence");
            c.That(hidden.Entries.Single(e => e.Entity == foreign.Id).Selection == null, "foreign selection never disclosed");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Select(ids.Concat(new[] { foreign.Id }).ToArray(), "terrain.route")), "normal foreign-set private admission refusal");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Select(ids.Concat(new[] { new EntityId(9999) }).ToArray(), "terrain.route")), "missing member refuses complete set");
            c.Equal(MotionNavigationCodes.Unsupported, game.Seat(0).Preview(Select(ids.Concat(new[] { source.Id }).ToArray(), "terrain.route")), "non-motion member refuses complete set");
            c.Equal(hash, game.Hash(), "all selection observations and refusals are pure");
            int[] versions = actors.Select(e => e.Version).ToArray(); var refused = TestKit.Order(game, 0, Select(mixed, "terrain.route"));
            c.That(!refused.Accepted && refused.Refusal == MotionNavigationCodes.NotAllowed, "normal rejected set changes no valid prefix");
            c.Equal("survey.route,survey.route", Profiles(actors), "rejected set retains all actual selections");
            c.That(actors.Select(e => e.Version).SequenceEqual(versions), "ordinary refusal does not touch valid owners");
            var left = new StateHasher(); Select(ids, "terrain.route").Sync(left); var right = new StateHasher(); Select(reversed, "terrain.route").Sync(right);
            c.Equal(left.Value, right.Value, "command wire canonicalizes selection order");
            c.That(reversed.SequenceEqual(ids.Reverse()), "canonical wire does not mutate caller order");
            var command = Select(reversed, "terrain.route"); int sequence = game.Seat(0).Submit(command); hash = game.Hash();
            command.Entities[0] = limited.Id; command.Profile = "missing";
            c.Equal(hash, game.Hash(), "submitted selection snapshot ignores later caller mutations");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending fleet command checkpoints through original inbox");
            Update(game, twin); var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            var accepted = results.Single(r => r.Sequence == sequence);
            c.That(accepted.Accepted && accepted.Arg0 == 2 && accepted.Arg1 == 2, "one command commits both actual owners with truthful counts");
            c.Equal("terrain.route,terrain.route", Profiles(actors), "actual selected profile stored only by movers");
            c.That(actors.All(e => e.Get<IMotion>().Status == MoveStatus.Idle), "idle fleet selection grants no destination");
            c.Equal(game.Hash(), twin.Hash(), "saved pending command commits identical complete selection");
            c.That(preview.Entries.All(e => e.Selection.Profile == "survey.route"), "retained group facts stay frozen");
            versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Select(ids, "terrain.route"));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0, "same-value whole selection is idempotent with zero changes");
            c.That(actors.Select(e => e.Version).SequenceEqual(versions), "idempotent selection retains owner versions");

            var route = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            c.That(route.Accepted, "normal player route owns native hauling");
            foreach (var actor in actors) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = false }).Accepted, "normal release permits real carrier acquisition");
            bool usedRoad = false;
            Action observe = () => { usedRoad |= actors.Any(e => e.Get<ITilePresence>().Tile.Y == 8); };
            Reach(game, () => Shipments(game).Length == 2, "two native acquired shipments", observe: observe);
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)route.Arg0), Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 100) }).Accepted, "player disables future dispatch without retiring acquired cargo");
            Reach(game, () => Shipments(game).Any(j => j.Loaded.IsPositive && j.Loaded < j.Amount), "real partial dock loading", observe: observe);
            var first = Shipments(game).Single(j => j.Loaded.IsPositive); var firstActor = game.World.Entities.Get(first.Worker); var second = Shipments(game).Single(j => j != first);
            Policy(c, game, first.Worker, false);
            Reach(game, () => game.World.Entities.Get(second.Worker).Get<ITilePresence>().Tile == new TileCoord(15, 12), "physical waiter behind paused loaded bay", observe: observe);
            c.That(usedRoad, "actual native carriers physically use authored preferred road");
            c.Equal(new Code("wait"), second.SourceDockSlot, "original job owns actual waiting dock slot");
            c.That(first.Loaded.IsPositive && first.Loaded < first.Amount && second.Loaded.IsZero, "physical partial cargo and queued empty carrier");
            var progress = first.HandlingProgress; var amount = first.Loaded; var outgoing = first.CargoOutReservation; var incoming = first.InReservation;
            var waitingSlot = second.SourceDockSlot; var position = firstActor.Get<ITilePresence>().Position;
            c.That(TestKit.Order(game, 0, Select(ids, "survey.route")).Accepted, "whole selection may change policy during retained partial dock service");
            c.Equal(position, firstActor.Get<ITilePresence>().Position, "paused loaded bay does not teleport on fleet policy change");
            c.Equal(amount, first.Loaded, "policy change does not fabricate handling");
            c.That(first.HandlingProgress.Equals(progress), "actual earned service progress retained");
            c.Equal(outgoing, first.CargoOutReservation, "actual cargo receipt retained");
            c.Equal(incoming, first.InReservation, "actual destination room retained");
            c.Equal(waitingSlot, second.SourceDockSlot, "queued job retains its original slot");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "loaded/queued policy is not delivered stock");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial bay/queue/selected policies save through original owners");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "sole clock pause stops native fleet/service");
            c.Equal(game.Hash(), twin.Hash(), "paused fleet checkpoint remains identical");
            game.Clock.Resume(); twin.Clock.Resume(); Policy(c, game, first.Worker, true, twin);
            Reach(game, () => first.Loaded == first.Amount && firstActor.Get<IMotion>().Status == MoveStatus.Moving &&
                first.Toils[first.ToilIndex].Executor == WorkPhases.Travel && first.Toils[first.ToilIndex].At == target.Id, "fully loaded physical departure", twin);
            var mover = firstActor.Get<Mover>(); var goal = mover.Goal; long age = mover.TicksMoving; var worker = first.Worker;
            incoming = first.InReservation; outgoing = first.CargoOutReservation;
            c.That(TestKit.Order(game, 0, Select(ids, "terrain.route")).Accepted, "normal whole selection changes a loaded moving route");
            c.That(TestKit.Order(twin, 0, Select(ids.Reverse().ToArray(), "terrain.route")).Accepted, "saved twin accepts opposite input order with same engine policy");
            c.Equal(goal, mover.Goal, "loaded mover preserves destination");
            c.Equal(age + 1, mover.TicksMoving, "loaded mover preserves accumulated age plus normal Update");
            c.Equal(worker, first.Worker, "same native shipment worker identity");
            c.Equal(incoming, first.InReservation, "moving loaded selection retains destination reservation");
            c.Equal(outgoing, first.CargoOutReservation, "moving loaded selection retains exact cargo reservation");
            c.Equal(game.Hash(), twin.Hash(), "canonical loaded fleet update continues identically");
            twin = factory.Load(game.Save()); Reach(game, () => Shipments(game).All(j => j.State == JobState.Done), "physical two-shipment unload and dock departure", twin);
            c.Equal(Fix.FromInt(16), target.Get<IInventory>().Amount(steel), "both original shipments physically delivered exactly sixteen units");
            c.Equal(Fix.FromInt(4), source.Get<IInventory>().Amount(steel), "original source retains remaining stock");
            c.Equal(Fix.FromInt(20), Total(game), "no cargo duplication/loss across selection and saved physical work");
            c.That(actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "cargo unloaded physically, not copied into endpoint");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded travel/queues/service complete identically");
            c.Equal("survey.route,survey.route", Profiles(Fleet(LiveSensorTests.New(factory))), "independent session keeps authored initial fleet policy");
        }
    }
}
