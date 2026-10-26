using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Content assembly and normal player intents only; no live World access or bootstrap repair.
    internal static class AuthoredColonySetupTests
    {
        internal const string Case = "Authored colony setup: rectangular layers, stable homes and physical saved work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory, string scenario = "colony")
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 823 });
        private static GameFactory ColonyFactory(JsonValue setup)
            => Colony.Factory(moduleDocuments: new Dictionary<string, string> { ["colony-startup.json"] = setup.ToString() });
        private static IEnumerable<EntityView> Mine(SimGame game, string prototype = null, int seat = 0)
            => game.Seat(seat).Snapshot.Entities.Where(e => e.Owner == new PlayerId(seat) && (prototype == null || e.Def == prototype));
        private static TileCoord Tile(EntityView entity) => ((TileSpatialSnapshot)entity.Spatial).Tile;
        private static JsonValue Startup() => Json.Parse(FrameworkProofContent.Read("colony-startup.json"));
        private static int[] Samples(Check c, SimGame game, string layer, TileRect region, int seat = 0)
        {
            var values = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(seat).View.Feature<TileMapView>().ReadLayer(layer, region, values), "authorized rectangular samples");
            return values.ToArray();
        }
        private static void Homes(Check c, SimGame game)
        {
            for (int seat = 0; seat < 2; seat++)
            {
                EntityId depot = Mine(game, "depot", seat).Single().Id;
                foreach (var worker in Mine(game, null, seat).Where(e => e.Def == "crane" || e.Def == "truck"))
                {
                    c.Equal(Code.None, game.Seat(seat).View.Feature<WorkView>().ReadHome(worker.Id, out var home), "authored worker has public home facts");
                    c.Equal(depot, home.Home, "stable setup dependency resolves to actual same-company depot");
                }
            }
        }
        private static string Projection(SimGame game)
            => string.Join(";", game.Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + e.Owner + "/" + Tile(e)));
        private static void Reverse(JsonValue parent, string field)
        {
            var original = parent.Get(field); var reversed = JsonValue.NewArray();
            for (int i = original.Count - 1; i >= 0; i--) reversed.Add(original[i].Clone());
            parent.Set(field, reversed);
        }
        private sealed class Observations
        {
            internal bool Moving, Loaded, Productive;
            internal void Read(SimGame game)
            {
                Moving |= Mine(game).Any(e => e.State == new Code("state.moving"));
                var work = game.Seat(0).View.Feature<WorkView>(); var jobs = new List<WorkJobView>(); work.CopyJobs(jobs);
                Loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                var orders = new List<OrderView>(); work.CopyOrders(orders);
                foreach (var order in orders)
                {
                    var nodes = new List<ActivityNodeView>();
                    if (work.TryCopyNodes(order.Id, nodes)) Productive |= nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
                }
            }
        }
        private static void Update(SimGame game, Observations observations, SimGame twin = null)
        {
            game.Update(); observations.Read(game);
            if (game.Fault != null) throw new InvalidOperationException("authored startup continuation fault " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash()) throw new InvalidOperationException("authored saved sessions diverged at " + game.Clock.StepIndex + "/" + twin.Fault);
        }
        private static void Reach(SimGame game, Observations observations, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, observations, twin);
            if (done()) return;
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            throw new InvalidOperationException(stage + "/clock=" + game.Clock.StepIndex + "/orders=" +
                string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)) + "/jobs=" +
                string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "direct authored rectangle identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "startup has no new persisted registry or legacy reader");
            var factory = Colony.Factory(); var game = New(factory);
            c.Equal(0L, game.Clock.StepIndex, "Initialize creates authored world without advancing sole clock");
            c.Equal(10, game.Snapshot.Entities.Count, "master-listed setup creates exact authored entities");
            c.That(game.Modules.Contains(WorldSetupModule.ModuleId) && !game.Modules.Contains(TycoonModule.ModuleId), "selected native startup, no implicit monetary module");
            Homes(c, game);
            int[] lake = Samples(c, game, "water", new TileRect(34, 4, 11, 11));
            bool allAuthored = lake.Length == 121;
            for (int y = 0; y < 11; y++) for (int x = 0; x < 11; x++)
                allAuthored &= lake[y * 11 + x] == (x >= 4 && x < 7 && y >= 4 && y < 7 ? 0 : 1);
            c.That(allAuthored && lake.Count(v => v == 1) == 112, "entire rectangle and dependent island obey authored ordered overlap");
            c.That(Samples(c, game, "water", new TileRect(33, 4, 1, 11)).All(v => v == 0), "rectangle never writes neighboring cells");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "initial authored state roundtrip without double startup");

            var reordered = Startup(); Reverse(reordered, "entity_setups"); Reverse(reordered, "tile_layer_setups");
            Reverse(reordered.Get("world_setup_profiles")[0], "entries"); var reversed = New(ColonyFactory(reordered));
            c.Equal(Projection(game), Projection(reversed), "dependencies and stable IDs determine initialization, not declaration order");
            Homes(c, reversed);
            c.That(lake.SequenceEqual(Samples(c, reversed, "water", new TileRect(34, 4, 11, 11))), "reordered declarations preserve dependent layer values");
            // Content identity may change when authored document order changes; do not claim equal hashes across different packs.
            var edited = Startup(); edited.Get("tile_layer_setups")[0].Set("width", JsonValue.Number("2"));
            edited.Get("tile_layer_setups")[0].Set("height", JsonValue.Number("2"));
            edited.Get("entity_setups")[3].Get("placement").Set("x", JsonValue.Number("10"));
            var changed = New(ColonyFactory(edited));
            c.That(Samples(c, changed, "water", new TileRect(34, 4, 3, 1)).SequenceEqual(new[] { 1, 1, 0 }), "editing JSON extent changes actual world, no compiled startup substitute");
            c.That(Mine(changed, "truck").Any(e => Tile(e) == new TileCoord(10, 6)), "editing JSON entity intent changes actual placement");
            c.That(!Mine(game, "truck").Any(e => Tile(e) == new TileCoord(10, 6)), "separate authored sessions do not share live initial state");

            NativeRectangles(c); Invalid(c);
            var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var depot = Mine(game, "depot").Single().Id; var plant = Mine(game, "factory").Single().Id;
            c.Equal(Fix.FromInt(50), Colony.Stock(game, depot, "steel"), "native authored inventory initialized once");
            var command = new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal physical construction preview");
            c.Equal(before, game.Hash(), "preview neither advances clock nor modifies startup");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player starts physical project");
            c.That(Mine(game, "site:tower").Any(e => e.Id == result.Entity) && !Mine(game, "tower").Any(), "construction creates intent/project, not finished structure");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant, Transformation = "build_truck", Count = 1, Home = depot }).Accepted,
                "normal authored machine recipe queues physical manufacture");
            var observations = new Observations();
            Reach(game, observations, () => observations.Loaded && observations.Productive && !Mine(game, "tower").Any(), "loaded partial physical work");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial physical work saves existing owners only");
            c.Equal(10, twin.Snapshot.Entities.Count(e => e.Def != "site:tower"), "load does not duplicate authored startup entities");
            long pausedAt = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(game, observations, twin);
            c.Equal(pausedAt, game.Clock.StepIndex, "one clock pause stops all physical work");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, observations, () => Mine(game, "tower").Count() == 1 && Mine(game, "truck").Count() == 3,
                "actual machine and staged structure complete", twin);
            c.That(observations.Moving && observations.Loaded && observations.Productive, "engine owns actual travel, loaded delivery and positive elapsed work");
            c.Equal(Fix.FromInt(10), Mine(game).Aggregate(Fix.Zero, (sum, e) => sum + Colony.Stock(game, e.Id, "steel")), "50 authored steel pays 30 construction and 10 manufacture physically");
            c.Equal(Fix.FromInt(50), Colony.Stock(game, Mine(game, "depot", 1).Single().Id, "steel"), "other company's authored materials remain independent");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final physical continuation roundtrip");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved initialized idle session remains independent");
            c.That(lake.SequenceEqual(Samples(c, game, "water", new TileRect(34, 4, 11, 11))), "ordinary updates do not regenerate startup layer intent");
        }
        private static void NativeRectangles(Check c)
        {
            var startup = Json.Parse(FrameworkProofContent.Read("native-survey-startup.json"));
            for (int i = 0; i < startup.Get("tile_layer_setups").Count; i++)
            { startup.Get("tile_layer_setups")[i].Set("width", JsonValue.Number("2")); startup.Get("tile_layer_setups")[i].Set("height", JsonValue.Number("2")); }
            var factory = NativeSurveyTests.Factory(startup: startup.ToString()); var game = New(factory, "native-survey");
            var region = new TileRect(16, 10, 2, 2); var fixedValues = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("survey.deposit", region, fixedValues), "private fixed-point rectangle readable by authored owner");
            c.That(fixedValues.Count == 4 && fixedValues.All(v => v == Fix.FromInt(31)), "all fixed samples retain actual quantity semantics");
            c.That(Samples(c, game, "survey.terrain", region).SequenceEqual(new[] { 42, 42, 42, 42 }), "byte rectangle retains exact native values");
            c.That(Samples(c, game, "survey.class", region).SequenceEqual(new[] { -7, -7, -7, -7 }), "integer rectangle preserves signed native values");
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(1).View.Feature<TileMapView>().ReadLayer("survey.deposit", region, fixedValues), "rectangle cannot leak company-private deposits");
            c.Equal(0, fixedValues.Count, "private refusal clears previous readable samples");
            c.Equal(0L, game.Clock.StepIndex, "native rectangular setup also performs no Update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "typed rectangular layers persist through original spatial codec");
            foreach (string mode in new[] { "fractional-byte", "negative-quantity" })
            {
                var bad = startup.Clone();
                bad.Get("tile_layer_setups")[mode == "fractional-byte" ? 1 : 0].Set("value", JsonValue.Number(mode == "fractional-byte" ? "0.5" : "-1"));
                c.Throws<ContentException>(() => New(NativeSurveyTests.Factory(startup: bad.ToString()), "native-survey"), "rectangular native value refusal " + mode);
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-width", "zero-height", "negative-origin", "wide-overflow", "out-of-bounds", "missing-layer", "cycle", "home-dependency", "foreign-home", "unseated", "collision" })
            {
                var bad = Startup(); var layer = bad.Get("tile_layer_setups")[0]; var entities = bad.Get("entity_setups");
                if (mode == "missing-width") layer.Remove("width");
                if (mode == "zero-height") layer.Set("height", JsonValue.Number("0"));
                if (mode == "negative-origin") layer.Set("x", JsonValue.Number("-1"));
                if (mode == "wide-overflow") layer.Set("width", JsonValue.Number("2147483647"));
                if (mode == "out-of-bounds") layer.Set("height", JsonValue.Number("32"));
                if (mode == "missing-layer") layer.Set("layer", JsonValue.Of("missing"));
                if (mode == "cycle") layer.Set("after", Json.Parse("[\"colony.island\"]"));
                if (mode == "home-dependency") entities[2].Set("after", JsonValue.NewArray());
                if (mode == "foreign-home")
                { entities[2].Set("home", JsonValue.Of("colony.company-1.depot")); entities[2].Set("after", Json.Parse("[\"colony.company-1.depot\"]")); }
                if (mode == "unseated") entities[2].Set("company", JsonValue.Number("7"));
                if (mode == "collision") { entities[1].Get("placement").Set("x", JsonValue.Number("4")); entities[1].Get("placement").Set("y", JsonValue.Number("4")); }
                c.Throws<ContentException>(() => New(ColonyFactory(bad)), "strict authored setup refusal " + mode);
            }
            var world = Json.Parse(Colony.Defs); world.Get("scenarios")[0].Get("settings").Remove(WorldSetupModule.ModuleId);
            c.Throws<ContentException>(() => New(Colony.Factory(definitions: world.ToString())), "selected startup requires explicit profile, no implicit compiled default");
        }
    }
}
