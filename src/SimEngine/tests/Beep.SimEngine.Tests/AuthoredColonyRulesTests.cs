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
    internal static class AuthoredColonyRulesTests
    {
        internal const string Case = "Authored colony rules: terrain admission, physical routing and saved construction";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string file) => Json.Parse(FrameworkProofContent.Read(file));
        private static GameFactory Factory(JsonValue placement = null, JsonValue navigation = null, JsonValue startup = null, JsonValue world = null, JsonValue conditions = null)
        {
            var documents = new Dictionary<string, string>();
            if (placement != null) documents.Add("colony-placement.json", placement.ToString());
            if (navigation != null) documents.Add("colony-navigation.json", navigation.ToString());
            if (startup != null) documents.Add("colony-startup.json", startup.ToString());
            if (conditions != null) documents.Add("colony-conditions.json", conditions.ToString());
            return Colony.Factory(definitions: world?.ToString(), moduleDocuments: documents);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 829 });
        private static IEnumerable<EntityView> Mine(SimGame game, string prototype = null)
            => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
        private static TileCoord Tile(SimGame game, EntityId id) => ((TileSpatialSnapshot)Mine(game).Single(e => e.Id == id).Spatial).Tile;
        private static ConstructCommand Build(TileCoord tile)
            => new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", tile) };
        private static Fix Steel(SimGame game)
        {
            Fix amount = Fix.Zero;
            foreach (var entity in Mine(game))
            {
                var facets = new List<Facet>();
                if (!game.Seat(0).View.Inspect(entity.Id, facets)) throw new InvalidOperationException("missing public entity facts");
                foreach (var facet in facets) if (facet.Key == Storage.FacetStock && facet.Subject == "steel") amount += facet.Value;
            }
            return amount;
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
            if (game.Fault != null) throw new InvalidOperationException("authored rules fault " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("saved rule sessions diverged at " + game.Clock.StepIndex + "/" + twin.Fault);
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
        private static void Refusal(Check c, SimGame game, Command command, Code expected, string label)
        {
            ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), label + " preview");
            c.Equal(before, game.Hash(), label + " pure preview"); var result = TestKit.Order(game, 0, command);
            c.That(!result.Accepted && result.Refusal == expected, label + " command parity");
            c.That(game.Fault == null, label + " typed refusal without engine fault");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored native rules identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "stateless content adds no parallel runtime save owner");
            var factory = Factory(); var game = New(factory);
            c.That(game.Modules.Contains(TileConstructionRulesModule.ModuleId), "master selects optional native construction rule module");
            c.Equal(0L, game.Clock.StepIndex, "rule composition does not advance Initialize time");
            Refusal(c, game, Build(new TileCoord(33, 4)), new Code("colony.on-water"), "non-origin footprint cell exclusion");
            c.That(!Mine(game, "site:tower").Any(), "rejected terrain creates no physical project");
            EntityId truck = Mine(game, "truck").First().Id;
            Refusal(c, game, new MoveVehicleCommand { Vehicle = truck, To = new TileCoord(35, 5) }, new Code("core.unreachable"), "authored impassable land water");

            var policy = Read("colony-placement.json");
            policy.Get("tile_construction_rules")[0].Get("rules")[0].Set("refusal", JsonValue.Of("terrain.wet"));
            var renamed = New(Factory(placement: policy));
            c.Equal(new Code("terrain.wet"), renamed.Seat(0).Preview(Build(new TileCoord(35, 5))), "JSON authors actual typed refusal, no compiled code substitute");
            var bands = Read("colony-conditions.json"); bands.Get("tile_layer_band_conditions")[0].Set("minimum", JsonValue.Number("2"));
            var permissive = New(Factory(placement: policy, conditions: bands));
            c.Equal(Code.None, permissive.Seat(0).Preview(Build(new TileCoord(35, 5))), "JSON threshold changes actual placement admission");
            c.That(TestKit.Order(permissive, 0, Build(new TileCoord(35, 5))).Accepted && Mine(permissive, "site:tower").Count() == 1 && !Mine(permissive, "tower").Any(),
                "authored permission creates only real project, never instant finished inaccessible structure");
            var navigation = Read("colony-navigation.json");
            var passable = New(Factory(navigation: navigation, conditions: bands));
            c.Equal(Code.None, passable.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Mine(passable, "truck").First().Id, To = new TileCoord(35, 5) }),
                "same original routing obeys JSON water threshold edit");
            Ordering(c); Optional(c); Lane(c); Invalid(c);

            game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            var construction = Build(new TileCoord(10, 16)); ulong initial = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(construction), "dry terrain admits normal physical build");
            c.Equal(initial, game.Hash(), "successful terrain preview remains pure");
            var build = TestKit.Order(game, 0, construction); c.That(build.Accepted, "actual selected rule permits construction command");
            c.That(Mine(game, "site:tower").Any(e => e.Id == build.Entity) && !Mine(game, "tower").Any(), "build begins as timed physical project");
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new CancelConstructionCommand { Site = build.Entity }), "authored terrain permission does not bypass company ownership");
            var observations = new Observations();
            Reach(game, observations, () => observations.Loaded && observations.Productive && !Mine(game, "tower").Any(), "partial physical loaded construction");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual saved partial work uses original codecs");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(game, observations, twin);
            c.Equal(step, game.Clock.StepIndex, "sole pause stops travel, hauling and construction");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, observations, () => Mine(game, "tower").Count() == 1, "real staged construction completion", twin);
            c.That(observations.Moving && observations.Loaded && observations.Productive, "actual travel, loaded delivery and positive machine work precede completion");
            c.Equal(Fix.FromInt(20), Steel(game), "native material conservation:50 opening minus30 tower");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed construction with selected policy roundtrip");
            c.Equal(idleHash, idle.Hash(), "shared readonly rule content never shares live session state");
        }
        private static void Ordering(Check c)
        {
            var policy = Read("colony-placement.json"); var rules = policy.Get("tile_construction_rules")[0].Get("rules");
            rules[0].Set("id", JsonValue.Of("placement.z")); rules[0].Set("refusal", JsonValue.Of("terrain.z"));
            var first = rules[0].Clone(); first.Set("id", JsonValue.Of("placement.a")); first.Set("refusal", JsonValue.Of("terrain.a")); rules.Add(first);
            var game = New(Factory(placement: policy));
            c.Equal(new Code("terrain.a"), game.Seat(0).Preview(Build(new TileCoord(35, 5))), "equal priority breaks by stable identity, not declaration order");
            rules[0].Set("priority", JsonValue.Number("200")); game = New(Factory(placement: policy));
            c.Equal(new Code("terrain.z"), game.Seat(0).Preview(Build(new TileCoord(35, 5))), "explicit higher priority determines typed exclusion");
        }
        private static void Optional(Check c)
        {
            var world = Read("colony.json"); var scenario = world.Get("scenarios")[0]; var modules = JsonValue.NewArray();
            for (int i = 0; i < scenario.Get("modules").Count; i++)
                if (scenario.Get("modules")[i].Text != TileConstructionRulesModule.ModuleId) modules.Add(scenario.Get("modules")[i].Clone());
            scenario.Set("modules", modules); scenario.Get("settings").Remove(TileConstructionRulesModule.ModuleId);
            var game = New(Factory(world: world));
            c.That(!game.Modules.Contains(TileConstructionRulesModule.ModuleId), "terrain policy module is genuinely optional");
            c.Equal(Code.None, game.Seat(0).Preview(Build(new TileCoord(35, 5))), "unselected policy has no compiled colony fallback");
            var policy = Read("colony-placement.json"); policy.Get("tile_construction_rules")[0].Set("rules", JsonValue.NewArray());
            c.Equal(Code.None, New(Factory(placement: policy)).Seat(0).Preview(Build(new TileCoord(35, 5))), "explicit empty authored exclusions are intentional, not missing defaults");
        }
        private static void Lane(Check c)
        {
            var world = Read("colony.json"); var entities = world.Get("entities");
            for (int i = 0; i < entities.Count; i++) if (entities[i].Get("id").Text == "truck")
            {
                entities[i].Get("components").Get("mover").Set("classes", Json.Parse("[\"lane\"]"));
                entities[i].Get("components").Get("mover").Set("class", JsonValue.Of("lane"));
            }
            var startup = Read("colony-startup.json");
            startup.Get("entity_setups")[3].Get("placement").Set("x", JsonValue.Number("10"));
            startup.Get("entity_setups")[3].Get("placement").Set("y", JsonValue.Number("9"));
            var ordinary = New(Factory(world: world, startup: startup)); var vehicle = Mine(ordinary, "truck").First().Id;
            c.Equal(Code.None, ordinary.Seat(0).Preview(new MoveVehicleCommand { Vehicle = vehicle, To = new TileCoord(15, 9) }), "native lane mover uses authored public lane samples");
            c.Equal(new Code("core.unreachable"), ordinary.Seat(0).Preview(new MoveVehicleCommand { Vehicle = vehicle, To = new TileCoord(15, 11) }), "native lane rejects cells outside authored road");
            startup.Get("tile_layer_setups")[2].Set("y", JsonValue.Number("11"));
            startup.Get("entity_setups")[3].Get("placement").Set("y", JsonValue.Number("11"));
            var factory = Factory(world: world, startup: startup); var game = New(factory); vehicle = Mine(game, "truck").First().Id;
            c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = vehicle, To = new TileCoord(15, 9) }), "JSON lane edit removes old hardcoded row9 route");
            var move = new MoveVehicleCommand { Vehicle = vehicle, To = new TileCoord(15, 11) };
            c.Equal(Code.None, game.Seat(0).Preview(move), "JSON lane edit admits actual new row11 route");
            c.That(TestKit.Order(game, 0, move).Accepted && Tile(game, vehicle) != new TileCoord(15, 11), "normal command starts real movement, no instant lane teleport");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "moving authored-lane vehicle roundtrip");
            var observations = new Observations(); Reach(game, observations, () => Tile(game, vehicle) == new TileCoord(15, 11), "actual lane travel", twin);
            c.That(game.Clock.StepIndex > 1 && observations.Moving, "authored lane movement consumes real game time");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-rules", "empty-conditions", "missing-layer", "empty-band", "fractional-byte", "missing-bound", "duplicate-id", "duplicate-condition", "empty-refusal", "wrong-space" })
            {
                var policy = Read("colony-placement.json"); var profile = policy.Get("tile_construction_rules")[0]; var rules = profile.Get("rules"); var conditions = rules[0].Get("conditions");
                var bands = Read("colony-conditions.json"); var band = bands.Get("tile_layer_band_conditions")[0];
                if (mode == "missing-rules") profile.Remove("rules");
                if (mode == "empty-conditions") rules[0].Set("conditions", JsonValue.NewArray());
                if (mode == "missing-layer") band.Set("layer", JsonValue.Of("missing"));
                if (mode == "empty-band") band.Set("maximum", JsonValue.Number("1"));
                if (mode == "fractional-byte") band.Set("minimum", JsonValue.Number("0.5"));
                if (mode == "missing-bound") band.Remove("maximum");
                if (mode == "duplicate-id") rules.Add(rules[0].Clone());
                if (mode == "duplicate-condition") conditions.Add(conditions[0].Clone());
                if (mode == "empty-refusal") rules[0].Set("refusal", JsonValue.Of(""));
                if (mode == "wrong-space") profile.Set("space", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => New(Factory(placement: policy, conditions: bands)), "strict authored rule refusal " + mode);
            }
            var world = Read("colony.json"); world.Get("scenarios")[0].Get("settings").Remove(TileConstructionRulesModule.ModuleId);
            c.Throws<ContentException>(() => New(Factory(world: world)), "selected rule module requires explicit profile settings");
            world = Read("colony.json"); world.Get("scenarios")[0].Get("settings").Get(TileConstructionRulesModule.ModuleId).Set("profile", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => New(Factory(world: world)), "missing selected profile never substitutes default");
            world = Read("colony.json"); world.Get("spatial_layers")[0].Set("visibility", JsonValue.Of("company_private"));
            c.Throws<ContentException>(() => New(Factory(world: world)), "private terrain never leaks through public routing/placement bands");
        }
    }
}
