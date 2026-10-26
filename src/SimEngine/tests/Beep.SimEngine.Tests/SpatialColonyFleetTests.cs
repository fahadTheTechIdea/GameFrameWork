using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Only public intent, observation, bounded Update and invariants. No route,
    // transfer, worker assignment, material grant or completion implementation.
    internal static class SpatialColonyFleetTests
    {
        internal const string Case = "Independent spatial colony: scarce stock priorities, loaded route detour and alternative-role construction";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory()
        {
            var definition = new GameDefinition("spatial-colony", new GameModule[] {
                new IndependentTileSensorModule(), new WorldSetupModule(), new OperationsModule(), new CompanyKnowledgeModule(),
                new KnowledgeVisibilityModule(), new MotionModule(), new TileNavigationModule(), new TileConditionsModule(),
                new ContentAccessModule(), new InventoryModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(),
                new SpatialColonyProofModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("spatial-colony-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            foreach (string file in new[] { "independent-tile-sensor-world.json", "independent-tile-sensor-startup.json",
                "independent-tile-navigation-startup.json", "independent-tile-navigation.json", "independent-tile-navigation-conditions.json",
                "independent-tile-sensor-visibility.json", "independent-tile-work.json", "work-retry.json",
                "independent-tile-construction.json", "spatial-colony-fleet.json" }) files.Add(file, FrameworkProofContent.Read(file));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "spatial-colony", Seed = 853, Seats = { Controller.Human, Controller.Human } });
        private static IEnumerable<EntityView> Mine(SimGame game) => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0));
        private static EntityView Entity(SimGame game, string prototype) => Mine(game).Single(e => e.Def == prototype);
        private static EntityView Entity(SimGame game, EntityId id) => Mine(game).Single(e => e.Id == id);
        private static TileSpatialSnapshot Geometry(SimGame game, EntityId id) => (TileSpatialSnapshot)Entity(game, id).Spatial;
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static Fix Stock(SimGame game, EntityId? endpoint = null)
        {
            Fix sum = Fix.Zero;
            foreach (var entity in Mine(game).Where(e => !endpoint.HasValue || e.Id == endpoint.Value)) {
                var facts = new List<Facet>();
                if (!game.Seat(0).View.Inspect(entity.Id, facts)) throw new InvalidOperationException("missing own inventory facts");
                foreach (var fact in facts) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") sum += fact.Value;
            }
            return sum;
        }
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows)) throw new InvalidOperationException("missing actual construction graph");
            return rows.Single();
        }
        private static bool Home(SimGame game, EntityId worker)
            => game.Seat(0).View.Feature<WorkView>().ReadHome(worker, out var home).IsNone && home.State == WorkerHomeState.AtHome;
        private static string Diagnostics(SimGame game) => "step=" + game.Clock.StepIndex + "/stock=" + Stock(game) +
            "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker)) +
            "/places=" + string.Join(";", Mine(game).Select(e => e.Def + "/" + ((TileSpatialSnapshot)e.Spatial).Tile + "/" + e.State));
        private static void Observe(SimGame game)
        {
            var occupied = new HashSet<TileCoord>();
            foreach (var entity in Mine(game)) if (!occupied.Add(((TileSpatialSnapshot)entity.Spatial).Tile))
                throw new InvalidOperationException("physical occupancy overlap/" + Diagnostics(game));
            Fix total = Stock(game);
            if (total != Fix.FromInt(12) && total != Fix.FromInt(4))
                throw new InvalidOperationException("resource creation/loss outside exact native build debit/" + Diagnostics(game));
        }
        private static void Update(SimGame game, SimGame twin = null, Action observe = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("fault " + game.Fault + "/" + twin?.Fault);
            Observe(game); observe?.Invoke();
            if (twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException("saved twins differ/" + Diagnostics(game));
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null, Action observe = null)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 480 && timer.Elapsed.TotalSeconds < 20 && !done(); i++) Update(game, twin, observe);
            if (!done()) throw new InvalidOperationException(stage + "/" + Diagnostics(game));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); long step = game.Clock.StepIndex;
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal public preview admits player intent");
            c.Equal(hash, game.Hash(), "preview changes no authoritative state");
            c.Equal(step, game.Clock.StepIndex, "preview drives no clock");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal command accepted/" + result.Refusal);
            if (game.Fault != null) throw new InvalidOperationException("command fault " + game.Fault);
            Observe(game); return result;
        }
        private static ConstructCommand Build(int x, int y) => new ConstructCommand { Def = "sensor.facility", Priority = 100,
            Placement = Json.Parse("{\"space\":\"sensor.tiles\",\"x\":" + x + ",\"y\":" + y + "}") };
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000); var recording = game.RecordReplay(limits);
            var hauler = Entity(game, "colony.hauler").Id; var native = Entity(game, "colony.builder").Id;
            var alternative = Entity(game, "colony.fabricator").Id; var blocker = Entity(game, "colony.blocker").Id;
            var high = Mine(game).Single(e => e.Def == "colony.store" && ((TileSpatialSnapshot)e.Spatial).Tile.Y == 10).Id;
            var low = Mine(game).Single(e => e.Def == "colony.store" && ((TileSpatialSnapshot)e.Spatial).Tile.Y == 22).Id;
            var retainedHauler = Entity(game, hauler);
            c.That(game.Snapshot.Space != null && !game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "independent selected tile space and physical work need no business");
            c.Equal(Fix.FromInt(12), Stock(game), "original twelve depot materials reused, no new grants");
            c.That(game.Seat(0).View.TryFindResource("steel", out var steel), "actual immutable resource catalog resolves normal stock-policy input");
            Submit(c, game, new SetWorkerHeldCommand { Worker = hauler, Held = true });
            Submit(c, game, new SetWorkerHeldCommand { Worker = native, Held = true });
            var canceled = Submit(c, game, Build(24, 24));
            c.That(Mine(game).Any(e => e.Id == canceled.Entity && e.Def == "sensor.build-site"), "build intent creates only a physical site");
            Submit(c, game, new CancelConstructionCommand { Site = canceled.Entity });
            c.That(!Mine(game).Any(e => e.Id == canceled.Entity) && !Mine(game).Any(e => e.Def == "sensor.facility"), "normal early cancellation creates no completed structure");
            c.Equal(Fix.FromInt(12), Stock(game), "unstarted cancellation loses no material or promises");
            void Demand(EntityId target, int priority) => Submit(c, game, new ConfigureStockPolicyCommand {
                Store = target, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(
                    true, Fix.FromInt(8), Fix.FromInt(8), Fix.FromInt(8), priority, LogisticsReserveAccess.Protected)) });
            Demand(low, 10); Demand(high, 80);
            Submit(c, game, new SetWorkerHeldCommand { Worker = hauler, Held = false });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Destination == high && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(8)),
                "real partially loaded priority haul");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial native loading/receipts checkpoint");
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Destination == high && j.Loaded == Fix.FromInt(8)) &&
                Geometry(game, hauler).Tile.X >= 5 && Geometry(game, hauler).Tile.Y < 16,
                "high-priority full cargo begins shorter crossing route", twin);
            var first = Jobs(game).Single(j => j.IsHaul && j.Destination == high && j.Loaded == Fix.FromInt(8));
            c.Equal(Fix.Zero, Stock(game, low), "higher priority wins finite supply despite low request being authored first");
            c.Equal(Fix.Zero, Stock(game, high), "real loaded cargo has not teleported to its destination");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "in-flight claimed cargo and native route checkpoint");
            Submit(c, game, new MoveVehicleCommand { Vehicle = blocker, To = new TileCoord(10, 10) });
            c.Equal(new TileCoord(10, 10), Geometry(game, blocker).Tile, "ordinary elapsed native movement now obstructs shorter crossing");
            twin = factory.Load(game.Save()); bool detour = false, deliveredBeforeRetirement = false;
            void Detour()
            {
                var live = Jobs(game).FirstOrDefault(j => j.Id == first.Id);
                if (live.Id == first.Id) {
                    if (live.FailureCount != 0 || live.State != JobState.Assigned && live.State != JobState.Done)
                        throw new InvalidOperationException("original haul was replaced/failed/" + Diagnostics(game));
                    detour |= live.State == JobState.Assigned && live.Loaded > Fix.Zero && Geometry(game, hauler).Tile.Y >= 19;
                }
                else {
                    // Automatic jobs intentionally compact after completion. Absence before
                    // actual delivery would expose failure/reassignment, not successful detour.
                    if (Stock(game, high) != Fix.FromInt(8)) throw new InvalidOperationException("haul retired before actual delivery/" + Diagnostics(game));
                    deliveredBeforeRetirement = true;
                }
            }
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex;
            Update(game, twin); c.Equal(paused, game.Clock.StepIndex, "single clock pause freezes changed physical route");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => Stock(game, high) == Fix.FromInt(8) && Stock(game, low) == Fix.FromInt(4) && Home(game, hauler),
                "saved loaded detour and two finite priority deliveries", twin, Detour);
            c.That(detour, "same loaded haul physically reroutes through other crossing rather than overlap or teleport");
            c.That(deliveredBeforeRetirement, "original acquired haul delivers before normal live-job compaction, without failed-job substitution");
            c.Equal(Fix.FromInt(12), Stock(game), "all scarce stock conserved through detour and partial low fulfillment");
            c.That(((TileSpatialSnapshot)retainedHauler.Spatial).Tile == new TileCoord(4, 16), "retained opening spatial facts remain detached");
            Submit(c, game, new SetWorkerHeldCommand { Worker = hauler, Held = true });
            Submit(c, game, new ConfigureStockPolicyCommand { Store = high, Resource = steel,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(8), Fix.FromInt(8), Fix.FromInt(8), 80, LogisticsReserveAccess.Protected)) });
            var built = Submit(c, game, Build(24, 16));
            c.That(!Mine(game).Any(e => e.Def == "sensor.facility"), "later build still requires physical materials and productive work");
            c.That(game.Seat(0).View.Feature<ConstructionView>().TryReadSite(built.Entity, out var site), "public site view binds the actual native order");
            Submit(c, game, new SetWorkerHeldCommand { Worker = hauler, Held = false });
            twin = factory.Load(game.Save());
            Until(game, () => Node(game, site.Order).Progress.WholeTicks > 0 && Node(game, site.Order).Progress.WholeTicks < Node(game, site.Order).RequiredTicks,
                "physically supplied alternative-role productive work", twin);
            var node = Node(game, site.Order);
            c.Equal(alternative, node.Worker, "native scheduler selects independently declared worker, not held native-role worker");
            c.Equal(new WorkerRoleId("sensor.fabricator"), node.SelectedRole, "actual alternative authored role selected from same graph");
            c.Equal(8L, node.RequiredTicks, "original four-minute build resolves to eight half-minute updates");
            c.Equal(Fix.FromInt(4), Stock(game), "exact eight material consumption belongs to native requirement owner");
            c.Equal(new Code("working"), Entity(game, alternative).State, "Entity.Update projects independent metadata's productive state");
            c.Equal(new TileCoord(22, 18), Geometry(game, native).Tile, "held alternative native-role worker never moves to bypass dispatch");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partially earned alternative-role construction checkpoints");
            Until(game, () => Mine(game).Any(e => e.Def == "sensor.facility"), "physical final construction", twin);
            c.Equal(1, Mine(game).Count(e => e.Def == "sensor.facility"), "one actual structure after earned build");
            c.That(!Mine(game).Any(e => e.Def == "sensor.build-site"), "native completion retires temporary site");
            c.Equal(Fix.FromInt(4), Stock(game), "final finite resources neither duplicate nor vanish");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished independent spatial game saves");
            c.Equal(isolatedHash, isolated.Hash(), "another independent session retains its own startup/topology/resources");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success, "public command route/policy/build replay/" + replay.Failure + "/" + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "spatial fleet replay exactly matches final native state");
            c.Equal(isolatedHash, isolated.Hash(), "replay does not mutate another session");
        }
    }
}
