using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class ScenarioGeometryTests
    {
        public const string CaseName = "Scenario geometry: shared prototypes isolated sparse bounds and saved continuation";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static GameFactory Factory(bool independent, Action<JsonValue> edit = null)
        {
            var root = Json.Parse(FrameworkProofContent.Read("scenario-geometry-world.json"));
            edit?.Invoke(root);
            if (independent)
            {
                JsonValue spaces = root.Get("tile_spaces");
                root.Remove("tile_spaces"); root.Set("independent_tile_spaces", spaces);
            }
            var definition = new GameDefinition("scenario-geometry", new GameModule[]
            { new WorldSetupModule(), new CompanyKnowledgeModule(), new KnowledgeVisibilityModule(), new ScenarioGeometryProofModule() });
            var files = new PreloadedFileSource()
                .Add("manifest.json", FrameworkProofContent.Read("scenario-geometry-master.json"))
                .Add("scenario-geometry-world.json", root.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, string scenario) => factory.NewGame(new NewGameRequest
        { Scenario = scenario, Seed = 17, Seats = new List<Controller> { Controller.Human, Controller.Human } });
        private static void Verify(Check check, SimGame game, int width, int height)
        {
            TileMapView map = game.Seat(0).View.Feature<TileMapView>();
            check.Equal(width, map.Width, "selected session width");
            check.Equal(height, map.Height, "selected session height");
            check.Equal("public-test.tiles", map.Space, "same stable spatial identity");
            var town = game.Snapshot.Entities.Single(e => e.Def == "public-test.town");
            check.That(game.Seat(0).View.TryGetEntity(town.Id, out _), "same prototype remains public in each geometry");
            var sample = new List<int>();
            check.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("geometry.terrain", new TileRect(12, 12, 1, 1), sample), "actual sparse layer sample");
            check.Equal(42, sample.Single(), "same authored initialization, separate runtime layer");
            check.That(game.Fault == null, "healthy geometry session");
        }
        private static void Run(Check check)
        {
            foreach (bool independent in new[] { false, true })
            {
                GameFactory factory = Factory(independent);
                SimGame small = New(factory, "geometry.small"), large = New(factory, "geometry.large"), maximum = New(factory, "geometry.max");
                Verify(check, small, 16, 16); Verify(check, large, 1048576, 131072); Verify(check, maximum, int.MaxValue, 64);
                SimGame smallTwin = factory.Load(small.Save()), largeTwin = factory.Load(large.Save()), maxTwin = factory.Load(maximum.Save());
                ulong retainedLarge = large.Hash(), retainedMax = maximum.Hash();
                small.Update(); smallTwin.Update();
                check.Equal(retainedLarge, large.Hash(), "small update cannot alter another geometry session");
                check.Equal(retainedMax, maximum.Hash(), "small update cannot alter maximum coordinate session");
                for (int i = 0; i < 3; i++)
                {
                    small.Update(); smallTwin.Update(); large.Update(); largeTwin.Update(); maximum.Update(); maxTwin.Update();
                    Verify(check, small, 16, 16); Verify(check, large, 1048576, 131072); Verify(check, maximum, int.MaxValue, 64);
                    check.Equal(small.Hash(), smallTwin.Hash(), "small native saved continuation");
                    check.Equal(large.Hash(), largeTwin.Hash(), "large native saved continuation");
                    check.Equal(maximum.Hash(), maxTwin.Hash(), "maximum native saved continuation");
                }
            }
            var edits = new Action<JsonValue>[]
            {
                root => root.Get("tile_spaces")[0].Get("scenarios")[0].Set("width", JsonValue.Number("0")),
                root => root.Get("tile_spaces")[0].Get("scenarios")[0].Set("height", JsonValue.Number("-1")),
                root => root.Get("tile_spaces")[0].Get("scenarios")[1].Set("scenario", JsonValue.Of("geometry.small")),
                root => root.Get("tile_spaces")[0].Get("scenarios")[0].Set("scenario", JsonValue.Of("unknown")),
                root => root.Get("tile_spaces")[0].Get("scenarios")[0].Remove("scenario"),
                root => root.Get("tile_spaces")[0].Set("width", JsonValue.Number("16")),
                root => root.Get("tile_spaces")[0].Set("scenarios", JsonValue.NewArray()),
                root => root.Get("tile_spaces")[0].Get("scenarios")[0].Set("scenario", JsonValue.Of(" "))
            };
            foreach (bool independent in new[] { false, true })
                foreach (Action<JsonValue> edit in edits)
                    check.Throws<ContentException>(() => New(Factory(independent, edit), "geometry.small"), "malformed or missing geometry refuses without fallback");
            check.Throws<ContentException>(() => New(Factory(true, root => root.Get("tile_spaces")[0].Set("proof_fault", JsonValue.Of("mismatch"))), "geometry.small"),
                "independent provider cannot substitute dimensions");
        }
    }
}
