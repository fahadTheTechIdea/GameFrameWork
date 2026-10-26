using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class OilAuthoredNavigationTests
    {
        public const string CaseName = "Oil navigation: canonical JSON native costs layers and saved scenarios";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static GameFactory Factory()
        {
            var definition = new GameDefinition("oil-navigation-proof", new GameModule[]
            { new MotionModule(), new TileNavigationModule(), new TileConditionsModule(),
              new WorldSetupModule(), new OilNavigationProofModule() });
            var files = new PreloadedFileSource();
            foreach (string file in new[] { "oil-navigation-master.json", "oil-navigation-fixture.json",
                                            "oil-spatial-world.json", "oil-navigation.json" })
                files.Add(file == "oil-navigation-master.json" ? "manifest.json" : file, OilFieldDaysProofContent.Read(file));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static void Verify(Check check, SimGame game, int dimension)
        {
            ulong before = game.Hash();
            TileMapView map = game.Seat(0).View.Feature<TileMapView>();
            OilNavigationProofView nav = game.Seat(0).View.Feature<OilNavigationProofView>();
            check.Equal(dimension, map.Width, "canonical scenario width");
            check.Equal(dimension, map.Height, "canonical scenario height");
            check.Equal("oil.tiles", map.Space, "canonical space");
            check.Equal(4, nav.MinimumCost("land"), "native minimum land cost");
            check.Equal(10, nav.MinimumCost("sea"), "native minimum sea cost");
            int[] land = { 10, 16, 22, 12, 18, 36, 0, 0, 0, 4, 6, 0, 0 };
            int[] sea = { 0, 0, 0, 0, 0, 0, 0, 10, 10, 10, 0, 0, 0 };
            int[] terrain = { 0, 1, 2, 3, 4, 5, 0, 0, 0, 5, 0, 6, 0 };
            int[] water = { 0, 0, 0, 0, 0, 0, 1, 2, 3, 2, 1, 0, 4 };
            int[] road = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 0, 0 };
            foreach (var layer in new[] { ("terrain", terrain), ("water", water), ("road", road) })
            {
                var values = new List<int>();
                check.Equal(SpatialLayerReadStatus.Success,
                    map.ReadLayer(layer.Item1, new TileRect(1, 1, land.Length, 1), values), "native public layer");
                check.Equal(land.Length, values.Count, "native setup sample count");
                for (int i = 0; i < values.Count; i++) check.Equal(layer.Item2[i], values[i], "canonical layer sample");
            }
            for (int i = 0; i < land.Length; i++)
            {
                check.Equal(land[i], nav.EntryCost("land", new TileCoord(i + 1, 1)), "native land rule cost");
                check.Equal(sea[i], nav.EntryCost("sea", new TileCoord(i + 1, 1)), "native sea rule cost");
            }
            check.Equal(0, nav.EntryCost("land", new TileCoord(dimension, 1)), "outside geometry blocked");
            check.Equal(before, game.Hash(), "observations do not mutate state");
            check.That(game.Fault == null, "healthy session");
        }
        private static void Run(Check check)
        {
            GameFactory factory = Factory();
            string[] names = { "frontier", "adventure", "continent", "sandbox", "showcase", "gulf" };
            int[] dimensions = { 96, 128, 224, 64, 112, 96 };
            var games = new List<SimGame>(); var twins = new List<SimGame>();
            for (int i = 0; i < names.Length; i++)
            {
                SimGame game = factory.NewGame(new NewGameRequest
                { Scenario = names[i], Seed = 17, Seats = new List<Controller> { Controller.Human } });
                games.Add(game); twins.Add(factory.Load(game.Save()));
                Verify(check, game, dimensions[i]); Verify(check, twins[i], dimensions[i]);
                check.Equal(game.Hash(), twins[i].Hash(), "saved native state");
            }
            ulong retained = games[1].Hash();
            games[0].Update(); twins[0].Update();
            check.Equal(retained, games[1].Hash(), "interleaved scenarios isolated");
            for (int i = 0; i < games.Count; i++)
            {
                games[i].Update(); twins[i].Update();
                Verify(check, games[i], dimensions[i]);
                check.Equal(games[i].Hash(), twins[i].Hash(), "native saved continuation");
            }
        }
    }
}
