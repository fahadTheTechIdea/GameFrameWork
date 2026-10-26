using System;
using System.Collections.Generic;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
namespace Beep.SimEngine.Tests
{
    public static class ClippedTileGeometryTests
    {
        public const string CaseName = "Spatial extension boundary: clipped bounds and public metadata queries";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Run(Check check)
        {
            var bounds = new TileRect(0, 0, int.MaxValue, int.MaxValue);
            var corner = new TileRect(int.MaxValue - 2, int.MaxValue - 3, 2, 3);
            check.Equal(new TileRect(int.MaxValue - 3, int.MaxValue - 4, 3, 4), corner.InflateClipped(1, bounds), "maximum-coordinate expansion retains actual corner");
            check.Equal(bounds, corner.InflateClipped(int.MaxValue, bounds), "large authored padding clips only to supplied bounds");
            check.Equal(new TileRect(0, 0, 4, 5), new TileRect(0, 0, 2, 3).InflateClipped(2, bounds), "minimum-coordinate expansion has no negative-origin fabrication");
            var local = new TileRect(10, 20, 30, 40);
            check.Equal(local, new TileRect(12, 22, 2, 3).InflateClipped(1000, local), "nonzero explicit bounds own expansion");
            check.Equal(new TileRect(12, 22, 2, 3), new TileRect(12, 22, 2, 3).InflateClipped(0, local), "zero padding identity");
            check.Equal(new TileRect(40, 60, 0, 0), new TileRect(90, 90, 5, 5).InflateClipped(0, local), "disjoint shapes return empty clipped geometry");
            check.Equal(new TileRect(0, 0, 3, 4), new TileRect(-2, -1, 5, 5).InflateClipped(0, bounds), "partially outside source intersects actual bounds");
            check.Throws<ArgumentException>(() => corner.InflateClipped(-1, bounds), "negative padding refused");
            check.Throws<ArgumentException>(() => default(TileRect).InflateClipped(1, bounds), "empty input refused");
            check.Throws<ArgumentException>(() => corner.InflateClipped(1, default), "empty bounds refused");
            for (int padding = 0; padding < 16; padding++)
            {
                var source = new TileRect(10, 12, 2, 3);
                check.Equal(source.Inflate(padding), source.InflateClipped(padding, new TileRect(-100, -100, 200, 200)), "ordinary expansion identity when fully contained");
                var high = corner.InflateClipped(padding, bounds);
                check.Equal(int.MaxValue, high.Right, "maximum horizontal extent remains representable");
                check.Equal(int.MaxValue, high.Bottom, "maximum vertical extent remains representable");
            }
            var factory = ConstructionPlanTests.Points(additionalModule: new SpatialQueryBoundaryProofModule(),
                moduleDeclaration: "{\"id\":\"proof.spatial-query-boundary\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[\"boundary.json\"]}",
                moduleDocument: "{}");
            var request = new NewGameRequest { Scenario = "points", Seed = 91, Seats = new List<Controller> { Controller.Human, Controller.Human } };
            request.Mods.Add(SpatialQueryBoundaryProofModule.ModuleId);
            SimGame game = factory.NewGame(request);
            var projection = game.Seat(0).View.Feature<SpatialQueryBoundaryProofView>();
            ulong before = game.Hash();
            check.That(projection.KnowsEntityPrototype("point.tower"), "independent module reads actual owned metadata disclosure");
            check.That(!projection.KnowsEntityPrototype("missing"), "independent module does not fabricate missing content");
            check.Equal(before, game.Hash(), "public module guard/disclosure query is read-only");
            SimGame twin = factory.Load(game.Save());
            game.Update(); twin.Update();
            check.That(game.Fault == null && twin.Fault == null, "healthy independent query composition/continuation");
            check.That(twin.Seat(0).View.Feature<SpatialQueryBoundaryProofView>().KnowsEntityPrototype("point.tower"), "loaded module reads same actual catalog");
            check.Equal(game.Hash(), twin.Hash(), "read-only external query adds no state owner");
        }
    }
}
