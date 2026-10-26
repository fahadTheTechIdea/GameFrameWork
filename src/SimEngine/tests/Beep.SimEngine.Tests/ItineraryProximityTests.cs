using System;
using System.Diagnostics;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.RouteOperatingAreaTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class ItineraryAdmissionTests
    {
        private static void Proximity(Check c)
        {
            var factory = Points(); var point = NewPoint(factory); var machine = Find(point, "machine");
            var destination = Find(point, "destination"); var space = (PointSpace)point.World.Space;
            int routes = 0; space.BeforeReachability = () => routes++;
            ulong hash = point.Hash();
            for (int i = 0; i < 16; i++)
                c.Equal(SpatialProximityStatus.OutsideReach, SpatialInteraction.CheckProximity(point.World, machine, destination), "actual point distance is outside contact without asking for a route");
            c.Equal(0, routes, "proximity does not invoke the provider's reachability query");
            c.Equal(SpatialProximityStatus.WithinReach, SpatialInteraction.CheckProximity(point.World, machine, machine), "actual point self-contact");
            c.Equal(Code.None, point.World.Services.Get<IWorkerDispatch>().CheckAssignment(point.World, machine, machine.Owner,
                new WorkerRoleId("haul"), new[] { destination }, false), "physical assignment policy remains eligible");
            c.Equal(0, routes, "incumbent eligibility tests physical contact without planning a speculative route");
            c.Equal(hash, point.Hash(), "all physical proximity reads preserve actual authoritative state");
            c.Equal(SpatialInteractionStatus.Reachable, SpatialInteraction.Check(point.World, machine, destination), "explicit full query still evaluates actual reachability");
            c.That(routes > 0, "full reachability remains a separate executed provider contract");
            space.BeforeReachability = null;
            c.That(TestKit.Order(point, 0, new SetPointAccessCommand { Open = false }).Accepted, "ordinary player access control closes the route");
            c.Equal(SpatialProximityStatus.OutsideReach, SpatialInteraction.CheckProximity(point.World, machine, destination), "closed route does not invent physical contact");
            c.Equal(SpatialInteractionStatus.Unreachable, SpatialInteraction.Check(point.World, machine, destination), "full route query retains its refusal");
            hash = point.Hash();
            c.That(Haul(point).PreviewHaul(machine.Owner, Find(point, "source").Id, destination.Id,
                point.World.Content.ResourceId("supply"), Fix.FromInt(8)).Status != HaulStatus.Ready, "proximity does not bypass real itinerary admission");
            c.Equal(hash, point.Hash(), "closed-route admission acquires nothing");
            routes = 0; space.BeforeReachability = () => routes++;
            c.Equal(MotionAreaCodes.Outside, Board(point).PreviewDock(destination, machine, WorkDockRequest.Work(new WorkerRoleId("haul")),
                new PointSnapshot(space.Id, Fix.FromInt(4), Fix.FromInt(4)), out _, out _, out _, out _), "future-origin admission still validates and refuses its actual closed route");
            c.Equal(0, routes, "future-origin dock preview never queries irrelevant current-origin reachability");
            c.Equal(hash, point.Hash(), "future-origin refusal changes no actual route or stock");
            space.BeforeReachability = null;
            var restored = factory.Load(point.Save());
            c.Equal(point.Hash(), restored.Hash(), "contact adds no second saved state");
            c.Equal(SpatialProximityStatus.OutsideReach, SpatialInteraction.CheckProximity(restored.World, Find(restored, "machine"), Find(restored, "destination")), "saved closed route retains real geometry");
            var foreign = NewPoint(factory);
            c.Equal(SpatialProximityStatus.Unsupported, SpatialInteraction.CheckProximity(point.World, machine, Find(foreign, "destination")), "foreign entity identity is refused before provider reads");
            c.Equal(SpatialProximityStatus.Unsupported, SpatialInteraction.CheckProximity(point.World, null, destination), "missing actor is not fabricated contact");

            foreach (string fault in new[] { "actor", "target", "target-private", "space-private", "clock", "throw", "target-throw" })
            {
                var broken = NewPoint(factory); var actor = Find(broken, "machine"); var target = Find(broken, "destination");
                var owner = (PointSpace)broken.World.Space; var original = new ApplicationException("proximity original failure");
                owner.BeforeProximity = () => {
                    if (fault == "actor") actor.Touch();
                    if (fault == "target" || fault == "target-throw") target.Touch();
                    if (fault == "target-private") typeof(PointPresence).GetField("_position", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target.Get<PointPresence>(), Fix.FromInt(9));
                    if (fault == "space-private") typeof(PointSpace).GetField("_accessOpen", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, !owner.AccessOpen);
                    if (fault == "clock") broken.Clock.Pause();
                    if (fault == "throw" || fault == "target-throw") throw original;
                };
                Exception caught = null;
                try { SpatialInteraction.CheckProximity(broken.World, actor, target); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && broken.Fault != null, "mutating/failing physical provider cannot publish a healthy result: " + fault);
                if (fault == "throw" || fault == "target-throw") c.That(ReferenceEquals(original, caught), "original callback failure is retained: " + fault);
                c.Throws<InvalidOperationException>(() => broken.Save(), "faulted contact observation is not resumable: " + fault);
                c.Throws<InvalidOperationException>(() => SpatialInteraction.CheckProximity(broken.World, null, null), "fault gates invalid/empty proximity before early returns: " + fault);
                c.Equal(0, broken.Update(), "first provider fault halts native Update: " + fault);
            }
            c.That(foreign.Fault == null, "independent session remains healthy after negative callbacks");

            var independentFactory = IndependentTileSensorTests.Factory(); var independent = IndependentTileSensorTests.New(independentFactory);
            var independentActor = Find(independent, "sensor.source"); var independentTarget = Find(independent, "sensor.target");
            c.That(independent.World.Space is SensorTileSpace, "same spatial contract runs on the independent tile owner");
            hash = independent.Hash();
            c.Equal(SpatialProximityStatus.WithinReach, SpatialInteraction.CheckProximity(independent.World, independentActor, independentActor), "independent tile self-contact");
            c.Equal(SpatialProximityStatus.OutsideReach, SpatialInteraction.CheckProximity(independent.World, independentActor, independentTarget), "independent tile geometry has real distant contact semantics");
            c.Equal(hash, independent.Hash(), "independent tile contact preserves its actual state");
            c.Equal(hash, independentFactory.Load(independent.Save()).Hash(), "independent tile contact adds no saved shadow state");

            var tileFactory = Tiles(); var tiles = New(tileFactory); var worker = Setup(c, tiles); var source = Find(tiles, "facility.source");
            c.Equal(SpatialProximityStatus.OutsideReach, SpatialInteraction.CheckProximity(tiles.World, worker, source), "native tile proximity uses actual expanded reach, not a route");
            c.Equal(SpatialProximityStatus.WithinReach, SpatialInteraction.CheckProximity(tiles.World, worker, worker), "native tile self-contact");
            c.That(TestKit.Order(tiles, 0, new MoveVehicleCommand { Vehicle = worker.Id, To = new TileCoord(19, 20) }).Accepted, "ordinary player movement approaches the facility");
            var twin = tileFactory.Load(tiles.Save()); var timer = Stopwatch.StartNew(); int steps = 0;
            while (SpatialInteraction.CheckProximity(tiles.World, worker, source) != SpatialProximityStatus.WithinReach && steps++ < 1200 && timer.ElapsedMilliseconds < 30000)
            {
                tiles.Update(); twin.Update();
                if (tiles.Fault != null || twin.Fault != null) throw new InvalidOperationException("physical proximity continuation fault");
                c.Equal(tiles.Hash(), twin.Hash(), "actual saved motion retains deterministic state");
            }
            c.Equal(SpatialProximityStatus.WithinReach, SpatialInteraction.CheckProximity(tiles.World, worker, source), "actual native travel reaches physical contact without teleportation");
            c.Equal(tiles.Hash(), twin.Hash(), "proximity owns neither a clock nor speculative route state");
        }
    }
}
