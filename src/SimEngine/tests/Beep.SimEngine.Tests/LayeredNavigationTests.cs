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
    internal static class LayeredNavigationTests
    {
        internal static void Register() => TestRunner.Add("Layered navigation: authored roads, depth gates and physical saved work", Run);
        internal static GameFactory Factory(JsonValue navigation = null, JsonValue layers = null, JsonValue conditions = null, Action<JsonValue> configureWorld = null)
        {
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            world.Get("entities")[1].Get("components").Get("mover").Set("class", JsonValue.Of("terrain.route"));
            world.Get("entities")[1].Get("components").Get("mover").Set("classes", Content.Json.Parse("[\"terrain.route\"]"));
            world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("route.start"));
            configureWorld?.Invoke(world);
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionsModule.ModuleId));
            master.Get("modules").Add(Content.Json.Parse("{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[\"native-route-conditions.json\"]}"));
            var modules = master.Get("modules");
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i]; string id = module.Get("id").Text;
                if (id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-layered-navigation-world.json"));
                if (id == "sim.tile-navigation") module.Get("documents").Add(JsonValue.Of("native-layered-navigation.json"));
            }
            return LiveSensorTests.Factory(world: world.ToString(), master: master.ToString(), additionalDocuments: new Dictionary<string, string> {
                ["native-route-conditions.json"] = (conditions ?? Content.Json.Parse(FrameworkProofContent.Read("native-route-conditions.json"))).ToString(),
                ["native-layered-navigation.json"] = (navigation ?? Content.Json.Parse(FrameworkProofContent.Read("native-layered-navigation.json"))).ToString(),
                ["native-layered-navigation-world.json"] = (layers ?? Content.Json.Parse(FrameworkProofContent.Read("native-layered-navigation-world.json"))).ToString() },
                additionalModules: new[] { new TileConditionsModule() });
        }
        private static Entity Machine(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.machine");
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.site");
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("layered route step=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, Action observe = null)
        {
            for (int i = 0; i < 500 && !done(); i++) {
                Update(game, twin); observe?.Invoke();
                if (i % 25 == 0) Console.WriteLine("   stage: " + stage + " update=" + i + " clock=" + game.Clock.StepIndex +
                    " tile=" + Machine(game).Get<ITilePresence>().Tile + " motion=" + Machine(game).Get<IMotion>().Status);
            }
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Tick + " position=" + Machine(game).Get<ITilePresence>().Tile +
                " motion=" + Machine(game).Get<IMotion>().Status + " orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current authored native navigation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "native layer/path/operation payloads unchanged");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var machine = Machine(game); var site = Site(game);
            Console.WriteLine("   stage: composed native layered world");
            var rule = game.World.Content.Get<LayeredTileNavigationDef>("terrain.route");
            var vessel = game.World.Content.Get<LayeredTileNavigationDef>("terrain.vessel");
            var map = (TileMap)game.World.Space; var surface = map.Layer<IntLayer>("route.surface"); var water = map.Layer<FixedLayer>("route.water");
            var grid = game.World.Services.Get<NavService>().Grid("terrain.route");
            var road = new TileCoord(8, 8); var steel = game.World.Content.ResourceId("steel");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "normal player hold keeps pure route checks stationary");
            c.That(rule is ITileNavigationRule && rule.AuthoringPath.Contains("native-layered-navigation.json"), "actual master document supplies existing navigation contract");
            c.Equal(1, grid.MinCost, "heuristic uses lowest positive authored outcome");
            c.Equal(50, rule.Cost(game.World, new TileCoord(7, 10)), "unmatched tile uses explicit base ground cost");
            c.Equal(1, rule.Cost(game.World, road), "stable equal-priority rule ID wins despite opposite authored order");
            c.Equal(0, rule.Cost(game.World, new TileCoord(-1, 8)), "off-map is not traversable");
            c.Equal(0, vessel.Cost(game.World, road), "explicit zero base makes dry vessel travel unavailable");
            c.That(!game.World.Services.Has<IEconomy>(), "authored terrain and physical work need no Tycoon");
            ulong hash = game.Hash(); var path = new List<TileCoord>();
            c.Equal(PathSearchResult.Found, grid.FindPath(machine.Get<ITilePresence>().Tile, new TileRect(15, 10, 1, 1), path), "same actual pathfinder admits terrain-aware route");
            Console.WriteLine("   stage: native structural preview completed");
            c.That(path.Contains(road), "cheaper longer road preferred over direct expensive ground");
            c.Equal(hash, game.Hash(), "authored cost and existing path preview are pure");
            var reversed = Content.Json.Parse(FrameworkProofContent.Read("native-layered-navigation.json"));
            var authoredRules = reversed.Get("layered_tile_navigation")[0].Get("rules"); var reverseRules = JsonValue.NewArray();
            for (int i = authoredRules.Count - 1; i >= 0; i--) reverseRules.Add(authoredRules[i].Clone());
            reversed.Get("layered_tile_navigation")[0].Set("rules", reverseRules);
            var other = LiveSensorTests.New(Factory(reversed));
            c.Equal(1, other.World.Content.Get<LayeredTileNavigationDef>("terrain.route").Cost(other.World, road), "JSON rule ordering does not alter priority/identity selection");

            // Actual native value protocols, not test-owned terrain logic or player layer-edit commands.
            water.Set(road, Fix.Half);
            c.Equal(0, rule.Cost(game.World, road), "inclusive fixed depth threshold overrides preferred road");
            c.Equal(10, vessel.Cost(game.World, road), "same actual depth permits authored vessel class");
            water.Set(road, Fix.FromRatio(1, 4));
            c.Equal(1, rule.Cost(game.World, road), "exclusive upper depth band admits shallow preferred road");
            c.Equal(0, vessel.Cost(game.World, road), "shallow water fails vessel minimum");
            water.Set(road, Fix.Zero); surface.Set(road, 1);
            c.Equal(0, rule.Cost(game.World, road), "actual signed-int terrain class blocks entry");
            surface.Set(road, -1);
            c.Equal(50, rule.Cost(game.World, road), "unmatched signed terrain value retains explicit base cost");
            surface.Set(road, 2); Update(game);
            var command = new PostOrderCommand { Plan = "survey.sample", Target = site.Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal physical work preview uses actual engine placement and dispatch");
            var posted = TestKit.Order(game, 0, command); c.That(posted.Accepted, "normal player posts actual survey work");
            var order = game.World.Services.Get<WorkBoard>().Order((int)posted.Arg0);
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = false }).Accepted, "normal release permits physical work dispatch");
            var mover = machine.Get<Mover>();
            Reach(game, () => mover.Path.Count > 0 && machine.Get<IWorker>().Job != 0, "native selected route");
            Console.WriteLine("   stage: physical work route selected at " + game.Clock.StepIndex);
            c.That(mover.Path.Contains(road), "actual physical mover selects the same preferred road");
            c.Equal(0L, order.Node("sample").Progress.WholeTicks, "travelling earns no premature productive work");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "travel does not prematurely consume work materials");
            // Simulate a changed actual environment while the same physical route is underway.
            water.Set(road, Fix.One); Update(game); var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "moving route and changed depth checkpoint through original owners");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "sole paused clock stops route and work");
            c.Equal(game.Hash(), twin.Hash(), "paused physical route twins retain identical state");
            game.Clock.Resume(); twin.Clock.Resume(); bool usedRoad = false, enteredFlood = false;
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, "rerouted physical arrival and work", twin, () => {
                var tile = machine.Get<ITilePresence>().Tile;
                usedRoad |= tile.Y == 8 && tile.X >= 2 && tile.X <= 16; enteredFlood |= tile == road;
            });
            c.That(usedRoad, "actual machine traverses preferred road, not a host route or teleport");
            Console.WriteLine("   stage: rerouted physical work started at " + game.Clock.StepIndex);
            c.That(!enteredFlood, "existing mover physically replans around newly impassable layer cell");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "real productive work consumes exactly eight canonical materials");
            c.Equal(game.Hash(), twin.Hash(), "rerouted physical arrival and partial work continue identically after save");
            c.That(!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _), "partial work grants no free geological knowledge");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "completed actual work", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial-work checkpoint completes identically");
            c.That(game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _), "actual completion earns original survey knowledge");
            var fresh = LiveSensorTests.New(factory);
            c.Equal(1, fresh.World.Content.Get<LayeredTileNavigationDef>("terrain.route").Cost(fresh.World, road), "interleaved session retains independent actual depth/terrain");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "negative", "all-zero", "duplicate-id", "empty-conditions", "missing-bound", "empty-band", "duplicate-condition", "fractional-int", "unknown-layer", "private-layer", "foreign-space" })
            {
                var navigation = Content.Json.Parse(FrameworkProofContent.Read("native-layered-navigation.json"));
                var layers = Content.Json.Parse(FrameworkProofContent.Read("native-layered-navigation-world.json"));
                var selected = navigation.Get("layered_tile_navigation")[0]; var rules = selected.Get("rules");
                var conditions = Content.Json.Parse(FrameworkProofContent.Read("native-route-conditions.json"));
                var condition = conditions.Get("tile_layer_band_conditions")[0];
                if (mode == "negative") selected.Set("base_cost", JsonValue.Number("-1"));
                if (mode == "all-zero") { selected.Set("base_cost", JsonValue.Number("0")); selected.Set("rules", JsonValue.NewArray()); }
                if (mode == "duplicate-id") rules.Add(rules[0].Clone());
                if (mode == "empty-conditions") rules[0].Set("conditions", JsonValue.NewArray());
                if (mode == "missing-bound") condition.Remove("minimum");
                if (mode == "empty-band") condition.Set("maximum", JsonValue.Number("2"));
                if (mode == "duplicate-condition") rules[0].Get("conditions").Add(rules[0].Get("conditions")[0].Clone());
                if (mode == "fractional-int") condition.Set("minimum", JsonValue.Number("0.5"));
                if (mode == "unknown-layer") condition.Set("layer", JsonValue.Of("missing.layer"));
                if (mode == "private-layer") condition.Set("layer", JsonValue.Of("survey.deposit"));
                if (mode == "foreign-space")
                {
                    layers.Set("tile_spaces", Content.Json.Parse("[{\"id\":\"other.tiles\",\"width\":1,\"height\":1}]"));
                    layers.Get("spatial_layers")[0].Set("space", JsonValue.Of("other.tiles"));
                }
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(navigation, layers, conditions)), "invalid authored routing " + mode);
            }
        }
    }
}
