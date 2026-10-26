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
    public static class ProspectiveRolloutTests
    {
        public const string CaseName = "Prospective rollout: whole planned geometry and initial inventory without spawning";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static GameFactory Factory(bool point)
        {
            var definition = new GameDefinition("prospective-rollout-proof", new GameModule[]
            { new InventoryModule(), new WorldSetupModule(), new ProspectiveRolloutModule() });
            var files = new PreloadedFileSource()
                .Add("manifest.json", FrameworkProofContent.Read("prospective-rollout-master.json"))
                .Add("prospective-world.json", FrameworkProofContent.Read(point
                    ? "prospective-rollout-point.json" : "prospective-rollout-tile.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, string scenario) => factory.NewGame(new NewGameRequest
        { Scenario = scenario, Seed = 17, Seats = new List<Controller> { Controller.Human } });
        private static SpatialPlacement Origin(bool point) => point
            ? (SpatialPlacement)new PointPlacement("kit.points", Fix.One)
            : new TilePlacement("kit.tiles", new TileCoord(1, 1), FixVec2.Centre(new TileCoord(1, 1)));
        private static ProspectiveRolloutView View(SimGame game) => game.Seat(0).View.Feature<ProspectiveRolloutView>();
        private static void Verify(Check check, SimGame game, bool point)
        {
            ulong before = game.Hash();
            var view = View(game); var prefix = new List<SpatialPlacement>(); int count = point ? 4 : 16;
            for (int i = 0; i < count; i++)
            {
                var result = view.Preview("kit.base", "kit.result", Origin(point), prefix);
                check.Equal(Code.None, result.Item1, "same engine accepts next distinct planned output");
                check.That(result.Item2 != null, "native provider returns actual placement");
                if (point)
                {
                    check.Equal(Fix.FromInt(i + 2), ((PointPlacement)result.Item2).Position, "native authored point offset and prefix");
                    foreach (var prior in prefix) check.That(((PointPlacement)prior).Position != ((PointPlacement)result.Item2).Position, "no repeated point");
                }
                else
                {
                    TileCoord at = ((TilePlacement)result.Item2).Origin;
                    check.That(!new TileRect(1, 1, 3, 3).Contains(at), "planned root excluded without a temporary entity");
                    foreach (var prior in prefix) check.That(((TilePlacement)prior).Origin != at, "no repeated tile");
                }
                prefix.Add(result.Item2);
            }
            var full = view.Preview("kit.base", "kit.result", Origin(point), prefix);
            check.Equal(Codes.Blocked, full.Item1, "whole planned space exhausted");
            check.That(full.Item2 == null, "refusal publishes no placement");
            var repeated = view.Preview("kit.base", "kit.result", Origin(point), new SpatialPlacement[0]);
            check.Equal(Code.None, repeated.Item1, "planning creates no durable reservations");
            check.Equal(point ? ((PointPlacement)prefix[0]).Position.Raw : ((TilePlacement)prefix[0]).Origin.X,
                point ? ((PointPlacement)repeated.Item2).Position.Raw : ((TilePlacement)repeated.Item2).Origin.X, "stable selection");
            check.Equal(Codes.UnknownDef, view.Preview("unknown", "kit.result", Origin(point), prefix).Item1, "unknown root refuses");
            check.Equal(Codes.UnknownDef, view.Preview("kit.base", "unknown", Origin(point), prefix).Item1, "unknown output refuses");
            check.Equal(Codes.InvalidArgument, view.Preview("kit.base", "kit.result", null, prefix).Item1, "absent origin refuses");
            check.Equal(Codes.InvalidArgument, view.Preview("kit.base", "kit.result", Origin(point),
                new[] { (SpatialPlacement)new PointPlacement("foreign", Fix.One) }).Item1, "foreign prefix refuses");
            SpatialPlacement outside = point ? (SpatialPlacement)new PointPlacement("kit.points", Fix.FromInt(6))
                : new TilePlacement("kit.tiles", new TileCoord(5, 5), FixVec2.Centre(new TileCoord(5, 5)));
            check.Equal(Codes.OutOfBounds, view.Preview("kit.base", "kit.result", Origin(point),
                new[] { outside }).Item1, "unavailable prefix still requires valid actual geometry");
            check.Equal(ResourceTransferStatus.Ready, view.InitialStock("kit.base", "kit.ore", Fix.FromInt(7)), "whole initial3 plus7 fits");
            check.Equal(ResourceTransferStatus.NoCapacity, view.InitialStock("kit.base", "kit.ore", Fix.FromInt(8)), "whole initial3 plus8 exceeds");
            check.Equal(ResourceTransferStatus.Incompatible, view.InitialStock("kit.base", "kit.other", Fix.One), "actual resource filter");
            check.Equal(ResourceTransferStatus.InvalidRequest, view.InitialStock("kit.base", "kit.ore", Fix.FromRatio(1, 2)), "actual discrete unit");
            check.Equal(ResourceTransferStatus.Incompatible, view.InitialStock("kit.result", "kit.ore", Fix.One), "missing stock capability");
            check.Equal(ResourceTransferStatus.Ready, view.EmptyInitialStock("kit.result"), "explicit empty grant requires no fake stock");
            check.Equal(ResourceTransferStatus.InvalidRequest, view.EmptyInitialStock("unknown"), "unknown definition");
            check.Equal(before, game.Hash(), "all planned geometry and stock checks are pure");
            check.Equal(0, game.Snapshot.Entities.Count, "no fake or temporary root/output spawned");
            check.That(game.Fault == null, "normal refusals do not fault");
            if (!point)
            {
                SpatialPlacement origin = new TilePlacement("kit.tiles", new TileCoord(0, 0), FixVec2.Centre(new TileCoord(0, 0)));
                var first = view.Preview("kit.base", "kit.large", origin, new SpatialPlacement[0]);
                var second = view.Preview("kit.base", "kit.large", origin, new[] { first.Item2 });
                check.Equal(Code.None, first.Item1, "large output first");
                check.Equal(Code.None, second.Item1, "large output next");
                var a = new TileRect(((TilePlacement)first.Item2).Origin, 2, 2);
                var b = new TileRect(((TilePlacement)second.Item2).Origin, 2, 2);
                check.That(!a.Overlaps(b), "complete output footprints excluded, not just origins");
                check.Equal(before, game.Hash(), "large output query remains pure");
            }
        }
        private static void Run(Check check)
        {
            foreach (bool point in new[] { false, true })
            {
                var factory = Factory(point); var planned = New(factory, "kit.planned"); var idle = New(factory, "kit.planned");
                Verify(check, planned, point);
                var twin = factory.Load(planned.Save()); Verify(check, twin, point);
                ulong idleHash = idle.Hash();
                planned.Update(); twin.Update();
                Verify(check, planned, point);
                check.Equal(planned.Hash(), twin.Hash(), "saved planned queries continue identically");
                check.Equal(idleHash, idle.Hash(), "interleaved query/update session isolated");
                var live = New(factory, "kit.live"); EntityId source = live.Snapshot.Entities.Single().Id;
                ulong before = live.Hash();
                var result = View(live).PreviewLive(source, "kit.result");
                check.Equal(Code.None, result.Item1, "original live entity rollout retained through same provider");
                check.That(result.Item2 != null, "live rollout returns placement");
                check.Equal(before, live.Hash(), "live rollout query remains pure");
                check.Equal(Codes.Blocked, View(live).Preview("kit.base", "kit.result", Origin(point),
                    new SpatialPlacement[0]).Item1, "planned root cannot overlap an actual root");
                check.Equal(1, live.Snapshot.Entities.Count, "live query never spawns output");
                check.That(!live.Modules.Contains(TycoonModule.ModuleId), "resource/geometry game has no business module");
            }
        }
    }
}
