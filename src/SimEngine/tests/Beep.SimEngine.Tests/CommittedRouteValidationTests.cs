using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Existing JSON world and independent tile provider, actual native mover, ordinary commands.
    internal static class CommittedRouteValidationTests
    {
        internal const string Case = "Native committed route: guarded suffix validation, physical saved twins and replay";
        internal const string RevalidationCase = "Native committed route: changed geometry, hypothetical origins and late provider faults";
        internal static void Register() { TestRunner.Add(Case, Physical); TestRunner.Add(RevalidationCase, Revalidation); }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Cart(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.cart");
        private static TileSpatialSnapshot Position(SimGame game) => (TileSpatialSnapshot)Cart(game).Spatial;
        private static Mover Motion(SimGame game) => game.World.Entities.Get(Cart(game).Id).Get<Mover>();
        private static TileSpatialSnapshot At(string space, FixVec2 position)
            => new TileSpatialSnapshot(space, position.Tile, new TileRect(position.Tile, 1, 1), position, position, FixVec2.Zero, false, true);
        private static MotionDestination Destination(SimGame game)
        {
            TileRect goal = Motion(game).Goal;
            return new MotionDestination(At(Position(game).Space, FixVec2.Centre(new TileCoord(goal.X, goal.Y))), MotionDestinationKind.Exact);
        }
        private static MotionRouteCheck Read(SimGame game, TileSpatialSnapshot origin = null, MotionDestination destination = null)
        {
            Code code = game.Seat(0).View.Feature<MotionView>().ReadRoute(Cart(game).Id, origin ?? Position(game), destination ?? Destination(game), out var route);
            if (!code.IsNone || route == null) throw new InvalidOperationException("route query unavailable: " + code);
            return route;
        }
        private static void Update(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Start(Check c, SimGame game)
        {
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(12, 14) }).Accepted,
                "ordinary move starts actual native path in the existing JSON world");
            c.That(Motion(game).Status == MoveStatus.Moving && Motion(game).PathIndex >= 0 && Motion(game).Path.Count > 1,
                "actual movement, not an injected route, earns its native cursor");
            c.That(Position(game).Position != FixVec2.Centre(Position(game).Tile), "actual origin is fractional, not merely a matching tile");
        }
        private static void Physical(Check c)
        {
            var factory = IndependentTileNavigationTests.Factory(); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000);
            using (var recorder = game.RecordReplay(limits))
            {
                Start(c, game); var mover = Motion(game); var origin = Position(game);
                int suffix = mover.Path.Count - mover.PathIndex, reads = 0;
                ((SensorTileSpace)game.World.Space).BeforeOccupant = _ => reads++;
                ulong retained = game.Hash(); long time = game.Clock.StepIndex;
                var route = Read(game); int actualReads = reads;
                c.That(route.Blocker.IsNone && route.Travel?.RemainingTicks > 0, "current committed goal has freshly proven structural travel");
                c.That(actualReads >= suffix && actualReads <= 1 + 3 * suffix,
                    "validate current origin and each remaining segment/corner, not a new search frontier");
                Console.WriteLine("native committed route;remaining-segments=" + suffix + ";guarded-entry-reads=" + actualReads + ";bound=" + (1 + 3 * suffix));
                c.That(route.Arrival is TileSpatialSnapshot arrival && mover.Goal.Contains(arrival.Tile), "validated native suffix ends at original current goal");
                c.Equal(retained, game.Hash(), "read allocates no route, state, work or stock authority");
                c.Equal(time, game.Clock.StepIndex, "read advances no second clock");
                ((SensorTileSpace)game.World.Space).BeforeOccupant = null;
                var twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "actual fractional route and cursor restore unchanged");
                c.Equal(route.Travel.Value.RemainingTicks, Read(twin).Travel.Value.RemainingTicks, "restored native suffix recalculates same conditional duration");
                int updates = 0; bool checkedFractionalGoal = false;
                while (Motion(game).Status == MoveStatus.Moving && updates++ < 160)
                {
                    if (!checkedFractionalGoal && Motion(game).Goal.Contains(Position(game).Tile) &&
                        Position(game).Position != FixVec2.Centre(Position(game).Tile))
                    {
                        ulong atGoal = game.Hash();
                        c.That(Read(game).Travel.Value.RemainingTicks > 0, "entering the goal tile is not physical arrival at its centre");
                        c.Equal(atGoal, game.Hash(), "fractional final-segment estimate grants no instant arrival");
                        checkedFractionalGoal = true;
                    }
                    var previous = Position(game).Position; Update(game); Update(twin);
                    c.Equal(game.Hash(), twin.Hash(), "ordinary physical motion remains identical through saved continuation");
                    c.That(Position(game).Position != previous || Motion(game).Status == MoveStatus.Arrived, "real native movement continues, not cached completion");
                }
                c.That(updates < 160 && Motion(game).Status == MoveStatus.Arrived, "original physical travel finishes within bounded Updates");
                c.That(checkedFractionalGoal, "physical saved route exercises a fractional position inside its final tile");
                c.Equal(FixVec2.Centre(new TileCoord(12, 14)), Position(game).Position, "actual provider presence reaches requested goal");
                c.That(origin.Position != Position(game).Position, "retained origin facts remain detached");
                c.Equal(isolated, idle.Hash(), "interleaved session remains untouched");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "original public command replays physical native path and queries");
            }
        }

        // Geometry edits below are explicit isolated invalidation/fault injection, never positive
        // player movement, path, work, resource or completion writes.
        private sealed class Region : ISpatialTravelRegion
        {
            internal string Mode;
            internal TileCoord Blocked;
            internal int Sweeps;
            internal Action Late;
            public string Id => "proof.native-route-region";
            public string Space => "sensor.tiles";
            public SpatialRegionStatus Check(SpatialSnapshot geometry) => SpatialRegionStatus.Inside;
            public SpatialRegionStatus CheckTravel(SpatialSnapshot start, SpatialSnapshot end)
            {
                if (++Sweeps == 2) Late?.Invoke();
                var from = (TileSpatialSnapshot)start; var to = (TileSpatialSnapshot)end;
                if (Mode == "fractional" && from.Position != to.Position && from.Position != FixVec2.Centre(from.Tile)) return SpatialRegionStatus.Outside;
                return Mode == "blocked" && to.Tile == Blocked ? SpatialRegionStatus.Outside : SpatialRegionStatus.Inside;
            }
        }
        private static void Revalidation(Check c)
        {
            var factory = IndependentTileNavigationTests.Factory();
            SimGame Moving() { var value = New(factory); Start(c, value); return value; }
            var game = Moving(); var mover = Motion(game); int reads = 0;
            var space = (SensorTileSpace)game.World.Space; space.BeforeOccupant = _ => reads++;
            ulong hash = game.Hash(); var original = Position(game);
            var exact = Read(game); int retainedReads = reads;
            var hypothetical = At(original.Space, original.Position + new FixVec2(Fix.FromRatio(1, 16), Fix.Zero));
            c.Equal(original.Tile, hypothetical.Tile, "hypothetical origin deliberately shares the actual tile");
            reads = 0; var otherOrigin = Read(game, hypothetical);
            c.That(otherOrigin.Blocker.IsNone && reads > retainedReads, "different fractional origin needs original prospective search, not native suffix reuse");
            reads = 0;
            var otherGoal = new MotionDestination(At(original.Space, FixVec2.Centre(new TileCoord(11, 14))), MotionDestinationKind.Exact);
            c.That(Read(game, destination: otherGoal).Blocker.IsNone && reads > retainedReads, "different requested goal uses original prospective search");
            c.Equal(hash, game.Hash(), "hypothetical searches mutate no real motion or clock"); space.BeforeOccupant = null;
            Entity entity = game.World.Entities.Get(Cart(game).Id);
            var slower = EntityObservation.ReadProvider(game.World, mover, entity,
                () => mover.CheckRoute(game.World, null, original, Destination(game), Fix.FromRatio(1, 2)));
            c.That(slower.Travel.Value.RemainingTicks > exact.Travel.Value.RemainingTicks, "suffix duration is freshly calculated from supplied current rate");
            var stopped = EntityObservation.ReadProvider(game.World, mover, entity,
                () => mover.CheckRoute(game.World, null, original, Destination(game), Fix.Zero));
            c.Equal(MotionArrivalCodes.ZeroBudget, stopped.Travel.Value.Blocker, "zero current budget never borrows an earlier known ETA");

            foreach (string mode in new[] { "segment", "corner", "origin", "goal" })
            {
                var changed = Moving(); var live = Motion(changed); var origin = Position(changed);
                var screen = (SensorSampleLayer)((SensorTileSpace)changed.World.Space).SpatialLayers.Single();
                TileCoord cell = mode == "origin" ? origin.Tile : mode == "goal" ? new TileCoord(live.Goal.X, live.Goal.Y) :
                    mode == "corner" ? new TileCoord(live.Path[live.PathIndex].X, origin.Tile.Y) : live.Path[live.Path.Count / 2];
                c.That(cell != origin.Tile || mode == "origin", "invalidation selects an actual future segment/corner or explicit origin");
                screen.SetSample(cell, Fix.FromInt(2));
                hash = changed.Hash(); var path = live.Path.ToArray(); int index = live.PathIndex;
                var result = Read(changed);
                if (mode == "origin" || mode == "goal") c.Equal(MotionAreaCodes.Outside, result.Blocker, "newly blocked structural origin/goal refuses: " + mode);
                else c.That(result.Blocker.IsNone && result.Travel.Value.RemainingTicks > 0, "invalid suffix falls back to a genuinely alternative structural path: " + mode);
                c.That(index == live.PathIndex && path.SequenceEqual(live.Path), "prospective replanning does not replace live physical route: " + mode);
                c.Equal(hash, changed.Hash(), "changed-topology query remains pure: " + mode);
            }
            foreach (string mode in new[] { "blocked", "fractional" })
            {
                var areaGame = Moving(); var live = Motion(areaGame); var actor = areaGame.World.Entities.Get(Cart(areaGame).Id);
                var region = new Region { Mode = mode, Blocked = live.Path[live.Path.Count / 2] }; hash = areaGame.Hash();
                var result = MotionAreaContracts.Plan(areaGame.World, actor, region, Position(areaGame), Destination(areaGame));
                c.That(region.Sweeps > 1, "actual remaining sweeps are checked after origin containment: " + mode);
                if (mode == "fractional") c.Equal(MotionAreaCodes.Outside, result.Blocker, "fallback search also honors the actual fractional departure sweep");
                else c.That(result.Blocker.IsNone, "new area exclusion invalidates native suffix but permits an alternative current route");
                c.Equal(hash, areaGame.Hash(), "area revalidation leaves native motion unchanged: " + mode);
            }
            foreach (bool area in new[] { false, true }) foreach (string fault in new[] { "throw", "clock", "layer" })
            {
                var bad = Moving(); var idle = New(factory); ulong isolated = idle.Hash();
                var provider = (SensorTileSpace)bad.World.Space; int callbacks = 0;
                Action damage = () => {
                    callbacks++;
                    if (fault == "throw") throw new InvalidOperationException("proof.native-suffix-original");
                    if (fault == "clock") ((GameClock)bad.World.Clock).Speed = 2;
                    if (fault == "layer") ((SensorSampleLayer)provider.SpatialLayers.Single()).SetSample(new TileCoord(20, 20), Fix.One);
                };
                var actor = bad.World.Entities.Get(Cart(bad).Id); var region = new Region { Late = damage };
                int entries = 0; if (!area) provider.BeforeOccupant = _ => { if (++entries == 2) damage(); };
                Exception thrown = null;
                try { if (area) MotionAreaContracts.Plan(bad.World, actor, region, Position(bad), Destination(bad)); else Read(bad); }
                catch (Exception error) { thrown = error; }
                c.That(callbacks == 1 && thrown != null && bad.Fault != null, "late retained-suffix callback faults before publishing travel: " + area + "/" + fault);
                if (fault == "throw") c.That(bad.Fault.Message.Contains("proof.native-suffix-original"), "original late callback exception is retained");
                c.Throws<InvalidOperationException>(() => bad.Save(), "compromised suffix cannot save healthy");
                c.Equal(0, bad.Update(), "faulted suffix never advances movement");
                c.Equal(isolated, idle.Hash(), "late callback violation is session-isolated");
            }
        }
    }
}
