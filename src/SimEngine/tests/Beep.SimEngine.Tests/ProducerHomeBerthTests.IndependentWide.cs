using System;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class ProducerHomeBerthTests
    {
        internal const string IndependentWideParkingCase = "Producer home: independent wide space preserves region-bound cargo and native rollout";
        private static GameFactory BoundedWideFactory(bool independent)
            => Factory(root => {
                root.Get("work_policies")[0].Set("area", JsonValue.Of("berth.region.all"));
                foreach (int i in new[] { 2, 4 }) root.Get("entities")[i].Get("components").Get("work_policy").Set("profile", JsonValue.Of("berth.region.work"));
            }, "producer-home-wide-berth.json", independentSpace: independent, wideAreas: true);
        private static void IndependentWideParking(Check c)
        {
            RunLoadedParking(c, BoundedWideFactory(true), new TileCoord(20, 7), new TileCoord(16, 11), new TileCoord(17, 11),
                new TileCoord(12, 11), Fix.FromInt(60), Fix.FromInt(100), 512);
            foreach (bool independent in new[] { false, true }) WideAreas(c, independent);
        }
        private static void WideAreaPolicy(Check c, SimGame game, string area)
            => c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = DeliveryEntity(game, "berth.carrier"),
                Settings = new WorkPolicySettings(true, 100, new[] { new WorkerRoleId("haul") }, area, true, false, WorkAssignmentMode.All) }).Accepted,
                "ordinary player policy selects actual authored region/" + area);
        private static void WideAreas(Check c, bool independent)
        {
            var factory = BoundedWideFactory(independent); var game = LiveSensorTests.New(factory);
            var isolated = LiveSensorTests.New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            var carrier = DeliveryEntity(game, "berth.carrier");
            var move = new MoveVehicleCommand { Vehicle = carrier, To = new TileCoord(20, 7) };
            var target = new TileSpatialSnapshot("survey.tiles", move.To, new TileRect(move.To, 1, 1), FixVec2.Centre(move.To),
                FixVec2.Centre(move.To), FixVec2.Zero, false, false);
            foreach (string area in new[] { "berth.region.narrow", "berth.region.bottle" })
            {
                WideAreaPolicy(c, game, area); ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).View.Feature<MotionView>().ReadRoute(carrier, DeliveryPosition(game, "berth.carrier"),
                    new MotionDestination(target, MotionDestinationKind.Exact), out var blocked), "automatic constrained route facts/" + area);
                c.Equal(MotionAreaCodes.Outside, blocked.Blocker, "wide automatic shape cannot use a one-cell region/" + area + "/" + independent);
                c.Equal(before, game.Hash(), "full-shape policy refusal creates no motion or time/" + area);
            }
            WideAreaPolicy(c, game, "berth.region.detour");
            var origin = DeliveryPosition(game, "berth.carrier");
            c.Equal(Code.None, game.Seat(0).View.Feature<MotionView>().ReadRoute(carrier, origin,
                new MotionDestination(target, MotionDestinationKind.Exact), out var route), "public prospective full-footprint area route");
            c.That(route.Blocker.IsNone && route.Arrival is TileSpatialSnapshot arrival && arrival.Footprint.W == 2 && arrival.Footprint.H == 2,
                "prospective arrival retains actual two-by-two shape, not an origin-only proxy");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadRoute(carrier, origin,
                new MotionDestination(target, MotionDestinationKind.Exact), out var hidden), "foreign route query cannot expose owned travel");
            c.That(hidden == null, "foreign query publishes no prospective shape or estimate");
            c.That(TestKit.Order(game, 0, move).Accepted, "explicit manual control can move outside automatic policy");
            c.Equal(Code.None, game.Seat(0).View.Feature<MotionView>().ReadExecution(carrier, out var manual), "actual held movement facts");
            c.That(manual.Area.Area == null, "held player intent is deliberately not automatic work-area movement");
            DeliveryUntil(c, game, null, () => DeliveryPosition(game, "berth.carrier").Position == FixVec2.Centre(move.To), "actual manual positioning before policy-controlled return");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = carrier, Held = false }).Accepted,
                "player release returns original source-home intent to automatic engine policy");
            c.Equal(Code.None, game.Seat(0).View.Feature<MotionView>().ReadExecution(carrier, out var automatic), "actual automatic movement facts");
            c.Equal("berth.region.detour", automatic.Area.Area, "released worker actually reinstates the original assigned operating area");
            var twin = factory.Load(game.Save()); bool sawDetour = false, savedPartial = false, arrived = false;
            var bounds = new TileRect(12, 7, 12, 18); var hole = new TileRect(21, 14, 1, 1);
            for (int i = 0; i < 128; i++)
            {
                var before = DeliveryPosition(game, "berth.carrier").Position;
                DeliveryStep(c, game, twin); var actual = DeliveryPosition(game, "berth.carrier");
                c.That(actual.Footprint.X >= bounds.X && actual.Footprint.Y >= bounds.Y &&
                    actual.Footprint.Right <= bounds.Right && actual.Footprint.Bottom <= bounds.Bottom && !actual.Footprint.Overlaps(hole),
                    "every physical occupied cell remains inside painted operating area/" + independent);
                var delta = actual.Position - before;
                c.That(new BigInteger(delta.X.Raw) * delta.X.Raw + new BigInteger(delta.Y.Raw) * delta.Y.Raw <=
                    new BigInteger(Fix.Half.Raw) * Fix.Half.Raw, "exact represented detour distance cannot exceed authored movement budget");
                if (actual.Footprint.Y <= 14 && actual.Footprint.Bottom > 14 && actual.Tile.X != 20) sawDetour = true;
                if (!savedPartial && i >= 4)
                { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "wide constrained partial motion restores actual cursor and footprint"); savedPartial = true; }
                c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadHome(carrier, out var home), "actual automatic home progress");
                if (home.State == WorkerHomeState.AtHome)
                { c.Equal(DeliveryEntity(game, "berth.source"), home.Home, "automatic recovery retains original player home binding"); arrived = true; break; }
            }
            c.That(arrived && sawDetour && savedPartial, "bounded route really detours a non-origin hole and arrives/" + independent);
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            c.That(game.Update(1000) == 0 && twin.Update(1000) == 0 && game.Hash() == paused && twin.Hash() == paused, "sole paused clock freezes area-bound positions");
            game.Clock.Resume(); twin.Clock.Resume();
            c.Equal(Fix.FromInt(100), DeliveryStock(game, "berth.source").Stock, "manual area proof does not create or spend physical stock");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.carrier").Stock, "no manufactured cargo makes a route appear successful");
            c.Equal(isolatedHash, isolated.Hash(), "provider and region sessions remain isolated");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "authored native/independent area movement replays/" + replay.Failure);
        }
    }
}
