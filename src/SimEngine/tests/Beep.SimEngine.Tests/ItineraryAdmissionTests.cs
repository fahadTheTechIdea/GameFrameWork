using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.RouteOperatingAreaTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class ItineraryAdmissionTests
    {
        internal static void Register()
        {
            TestRunner.Add("Spatial interaction: guarded proximity is independent of route reachability", Proximity);
            TestRunner.Add("Itinerary admission: prospective provider routes and atomic private previews", c => { Admission(c); DurationAdmission(c); JourneyAdmission(c); });
            TestRunner.Add("Itinerary admission: permitted alternatives, physical queues and saved execution", c => { Physical(c); DurationPhysical(c); JourneyPhysical(c); });
        }
        private static GameFactory Facilities()
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            var slots = root.Get("entities")[0].Get("components").Get("tile_work_dock").Get("slots");
            slots[0].Set("id", JsonValue.Of("bay.a")); slots[1].Set("y", JsonValue.Number("4")); slots[2].Set("y", JsonValue.Number("10"));
            slots.Add(Content.Json.Parse("{\"id\":\"bay.b\",\"kind\":\"service\",\"x\":-1,\"y\":2}"));
            var text = new StringBuilder(); root.WriteCanonical(text);
            return Colony.Factory(additionalDefinitions: text.ToString(), additionalDocuments: new Dictionary<string, string>
            {
                ["tile-route-areas.json"] = FrameworkProofContent.Read("tile-route-areas.json"),
                ["itinerary-areas.json"] = FrameworkProofContent.Read("itinerary-areas.json")
            });
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static IHaulExecution Haul(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static MotionRouteCheck Plan(SimGame game, Entity actor, string region, SpatialSnapshot origin, SpatialSnapshot destination, MotionDestinationKind kind = MotionDestinationKind.Exact)
            => MotionAreaContracts.Plan(game.World, actor, game.World.Content.Get<ISpatialRegionDefinition>(region), origin, new MotionDestination(destination, kind));
        private static void Admission(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actor = Setup(c, game); var from = Find(game, "facility.source"); var to = Find(game, "facility.destination");
            var steel = game.World.Content.ResourceId("steel"); Policy(c, game, actor.Id, "route.islands", true);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current itinerary semantics have explicit build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no speculative route codec");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "actual work owner declares itinerary admission");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "actual logistics owner declares atomic itinerary semantics");
            var nextJob = Board(game).NextJobId; var nextEntity = game.World.Entities.NextIdentity; ulong hash = game.Hash();
            var preview = Haul(game).PreviewHaul(new PlayerId(0), from.Id, to.Id, steel, Fix.FromInt(8));
            c.Equal(HaulStatus.PolicyBlocked, preview.Status, "disconnected future delivery leg refuses admission before pickup");
            c.Equal(MotionAreaCodes.Outside, preview.Blocker, "future route failure is a typed engine refusal");
            c.Equal(0, Haul(game).AcquireHaul(new PlayerId(0), from.Id, to.Id, steel, Fix.FromInt(8), 100), "actual acquisition rechecks and refuses same itinerary");
            c.Equal(nextJob, Board(game).NextJobId, "refusal allocates no job identity"); c.Equal(nextEntity, game.World.Entities.NextIdentity, "refusal allocates no entity identity");
            c.Equal(Fix.Zero, from.Get<IOutletPort>().ReservedOut(steel), "refused source stock is unclaimed");
            c.Equal(Fix.Zero, to.Get<IInletPort>().ReservedIn(steel), "refused capacity is unclaimed");
            c.Equal(Fix.Zero, actor.Get<ITransporter>().Cargo.ReservedIn(steel), "refused transporter capacity is unclaimed");
            c.Equal(hash, game.Hash(), "complete refusal changes no authoritative state");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "refused previews leave resumable original state");
            var origin = from.Get<IWorkDock>().CaptureSlot(game.World, new Code("bay")); var target = to.Get<IWorkDock>().CaptureSlot(game.World, new Code("bay"));
            c.Equal(MotionAreaCodes.Outside, Plan(game, actor, "route.islands", origin, target).Blocker, "prospective origin is the loading arrival, not current vehicle coordinates");
            var route = Plan(game, actor, "route.all", origin, target);
            c.Equal(Code.None, route.Blocker, "same native router can prove an unrestricted structural future leg");
            c.Equal(new TileCoord(27, 20), ((TileSpatialSnapshot)route.Arrival).Tile, "query returns its actual reached goal tile");
            c.Equal(new TileCoord(18, 20), actor.Get<ITilePresence>().Tile, "prospective arrival never relocates actual presence");
            c.Equal(MoveStatus.Idle, actor.Get<IMotion>().Status, "prospective query never changes actual motion intent");
            c.Equal(hash, game.Hash(), "success probe also remains pure");
            var foreignGame = New(factory); var foreignActor = Setup(c, foreignGame);
            c.Equal(PathSearchResult.InvalidRequest, game.World.Services.Get<NavService>().Grid("land").ProbeStructuralRoute(foreignActor,
                actor.Get<ITilePresence>().Tile, new TileRect(27, 20, 1, 1), out _, null), "public structural query refuses a foreign session actor");
            route = Plan(game, actor, "route.all", target, from.Get<ISpatialPresence>().Capture(), MotionDestinationKind.Interaction);
            c.That(route.Blocker.IsNone && ((TileSpatialSnapshot)route.Arrival).Tile != from.Get<ITilePresence>().Tile, "ordinary interaction shares real expanded reach instead of entering a blocked building");
            c.Throws<ArgumentException>(() => new MotionDestination(target, (MotionDestinationKind)255), "undefined arrival semantics are rejected");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = actor.Id, To = new TileCoord(19, 21) }).Accepted, "ordinary diagonal travel supplies real fractional origin");
            var fractional = actor.Get<ITilePresence>().Position;
            c.That(fractional != FixVec2.Centre(actor.Get<ITilePresence>().Tile), "current physical origin is not invented tile-center geometry");
            route = Plan(game, actor, "route.all", actor.Get<ISpatialPresence>().Capture(), At("colony.tiles", actor.Get<ITilePresence>().Tile));
            c.Equal(fractional, ((TileSpatialSnapshot)route.Arrival).Position, "already-reached prospective goal preserves actual fractional arrival");

            var alternatives = New(Facilities()); var candidate = Setup(c, alternatives, 22); Policy(c, alternatives, candidate.Id, "itinerary.connected", true);
            var candidates = new List<WorkWorkerCandidate>(); var source = Find(alternatives, "facility.source");
            alternatives.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("facility.inspect", "a", source.Id, candidates);
            bool admitted = false; foreach (var row in candidates) if (row.Worker == candidate.Id) admitted = row.Result.Status == WorkRequirementStatus.Satisfied;
            c.That(admitted, "allowed alternative bay and exit admit ordinary work despite disconnected earlier slots");
            c.Equal(SpatialRegionStatus.Inside, alternatives.World.Content.Get<ISpatialRegionDefinition>("itinerary.connected").Check(source.Get<IWorkDock>().CaptureSlot(alternatives.World, new Code("bay.a"))), "first bay is permitted geometry, not an endpoint-policy exclusion");
            c.Equal(MotionAreaCodes.Outside, Plan(alternatives, candidate, "itinerary.connected", candidate.Get<ISpatialPresence>().Capture(), source.Get<IWorkDock>().CaptureSlot(alternatives.World, new Code("bay.a"))).Blocker,
                "prospective routing identifies the genuinely isolated bay");
            var planPreview = Haul(alternatives).PreviewHaul(new PlayerId(0), source.Id, Find(alternatives, "facility.destination").Id, alternatives.World.Content.ResourceId("steel"), Fix.FromInt(8));
            c.Equal(new Code("bay.b"), planPreview.SourceDockSlot, "stable slot selection chooses a complete permitted itinerary");
            c.Equal(Codes.NotFound, alternatives.Seat(1).View.Feature<MotionView>().ReadExecution(candidate.Id, out var privateFact), "private itinerary-related motion remains company scoped");
            c.That(privateFact == null, "private refusal publishes no facts");

            foreach (string fault in new[] { "route-touch", "route-status", "route-null", "route-reason", "route-space", "route-empty", "route-outside" })
            {
                var broken = NewPoint(Points(fault)); var machine = Find(broken, "machine"); Policy(c, broken, machine.Id, "route.all", false);
                c.Throws<InvalidOperationException>(() => Plan(broken, machine, "route.all", machine.Get<ISpatialPresence>().Capture(), Find(broken, "destination").Get<ISpatialPresence>().Capture()),
                    "invalid provider route refuses usable admission: " + fault);
            }
            var points = Points(); var point = NewPoint(points); var pointMachine = Find(point, "machine"); Policy(c, point, pointMachine.Id, "route.all", false);
            hash = point.Hash(); route = Plan(point, pointMachine, "route.all", new PointSnapshot("field.points", Fix.FromInt(4), Fix.FromInt(4)), Find(point, "destination").Get<ISpatialPresence>().Capture());
            c.Equal(Fix.FromInt(10), ((PointSnapshot)route.Arrival).Position, "public mod supplies detached future arrival through engine contracts");
            c.Equal(hash, point.Hash(), "public prospective query remains pure");
            c.Equal(MotionAreaCodes.Unsupported, Plan(point, pointMachine, "route.stations", pointMachine.Get<ISpatialPresence>().Capture(), Find(point, "destination").Get<ISpatialPresence>().Capture()).Blocker,
                "endpoint-only public region cannot admit a physical itinerary");
            var absent = NewPoint(Points("absent")); var absentMachine = Find(absent, "machine");
            c.Equal(MotionAreaCodes.Unsupported, Plan(absent, absentMachine, "route.all", absentMachine.Get<ISpatialPresence>().Capture(), Find(absent, "destination").Get<ISpatialPresence>().Capture()).Blocker,
                "missing actual prospective provider is not an unrestricted fallback");
            c.That(TestKit.Order(point, 0, new SetPointAccessCommand { Open = false }).Accepted, "ordinary engine access command closes public route");
            hash = point.Hash(); preview = Haul(point).PreviewHaul(new PlayerId(0), Find(point, "source").Id, Find(point, "destination").Id, point.World.Content.ResourceId("supply"), Fix.FromInt(8));
            c.That(preview.Status != HaulStatus.Ready, "current access prevents acquiring impossible future route");
            c.Equal(hash, point.Hash(), "closed-route preview acquires nothing");
        }
        private static void Physical(Check c)
        {
            var factory = Facilities(); var game = New(factory); var first = Setup(c, game, 22); var second = Spawn(game, "facility.worker", 18, 24); Tick(game);
            Policy(c, game, first.Id, "itinerary.connected", true); Policy(c, game, second.Id, "itinerary.connected", true);
            var source = Find(game, "facility.source"); var destination = Find(game, "facility.destination"); var steel = game.World.Content.ResourceId("steel");
            int a = Haul(game).AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(8), 100);
            c.That(a > 0, "complete structural itinerary acquires first real haul"); Tick(game);
            int b = Haul(game).AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(8), 100);
            c.That(b > 0, "future occupied service bay remains eligible through real waiting capacity");
            Job one = Board(game).Job(a), two = Board(game).Job(b);
            c.Equal(new Code("bay.b"), one.SourceDockSlot, "first job claims accessible alternative service bay");
            c.Equal(new Code("wait.a"), two.SourceDockSlot, "second job claims actual finite waiting slot");
            foreach (Entity actor in new[] { first, second }) c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = actor.Id, Home = source.Id }).Accepted, "future home intent clears completed finite exits through physical engine movement");
            Reach(game, () => one.ToilIndex >= 2 && game.World.Entities.Get(one.Worker).Get<IMotion>().Status == MoveStatus.Moving, "loaded admitted future leg");
            var carrier = game.World.Entities.Get(one.Worker); Policy(c, game, carrier.Id, "route.islands", true);
            c.Equal(JobState.Paused, one.State, "post-admission policy edit retains actual loaded job");
            c.Equal(Fix.FromInt(8), carrier.Get<ITransporter>().Cargo.Amount(steel), "loaded cargo remains real and owned");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted queue, cargo, source and capacity claims restore exactly");
            var position = carrier.Get<ITilePresence>().Position;
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(position, carrier.Get<ITilePresence>().Position, "changed itinerary waits physically, no instant repair");
            c.Equal(game.Hash(), twin.Hash(), "saved queued continuation remains isolated and deterministic");
            Policy(c, game, carrier.Id, "itinerary.connected", true, twin);
            EntityId firstActor = one.Worker, secondActor = two.Worker;
            bool firstExit = false, secondExit = false;
            Reach(game, () => !one.IsLive && !two.IsLive, "actual waiting, promotion, delivery and accessible departures", () =>
            {
                foreach (Job job in new[] { one, two })
                {
                    Entity actor = game.World.Entities.Get(job.Id == one.Id ? firstActor : secondActor);
                    c.Equal(SpatialRegionStatus.Inside, game.World.Content.Get<ISpatialRegionDefinition>("itinerary.connected").Check(actor.Get<ISpatialPresence>().Capture()), "actual vehicle stays in permitted connected cells");
                    if (job.Id == one.Id) firstExit |= actor.Get<ITilePresence>().Tile == new TileCoord(27, 28);
                    else secondExit |= actor.Get<ITilePresence>().Tile == new TileCoord(27, 28);
                }
                c.Equal(game.Hash(), twin.Hash(), "physical queue and alternative departures continue identically");
            }, twin);
            c.That(firstExit && secondExit, "both jobs physically use connected second exit, never isolated first exit");
            c.Equal(Fix.FromInt(16), source.Get<IOutletPort>().Amount(steel), "two physical pickups debit exact source quantities");
            c.Equal(Fix.FromInt(16), destination.Get<IInletPort>().Amount(steel), "two physical deliveries consume their actual inbound receipts exactly");
            c.Equal(Fix.Zero, destination.Get<IInletPort>().ReservedIn(steel), "completed actual trips release capacity claims");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed alternative itineraries remain durable");

            var workGame = New(factory); var worker = Setup(c, workGame, 22); Policy(c, workGame, worker.Id, "itinerary.connected", true);
            var post = TestKit.Order(workGame, 0, new PostOrderCommand { Plan = "facility.inspect", Target = Find(workGame, "facility.source").Id });
            c.That(post.Accepted, "player posts ordinary authored activity with isolated earlier bay and exit");
            var order = Board(workGame).Order(checked((int)post.Arg0));
            Reach(workGame, () => !order.IsActive, "ordinary work complete itinerary");
            c.Equal(OrderState.Done, order.State, "same activity owner completes physically through accessible alternatives");
            c.Equal(new TileCoord(19, 28), worker.Get<ITilePresence>().Tile, "actual activity departure avoids isolated first exit");

            var pointFactory = Points(); var points = NewPoint(pointFactory); var machine = Find(points, "machine"); Policy(c, points, machine.Id, "route.all", false);
            var supply = points.World.Content.ResourceId("supply"); int haul = Haul(points).AcquireHaul(new PlayerId(0), Find(points, "source").Id, Find(points, "destination").Id, supply, Fix.FromInt(8), 100);
            c.That(haul > 0, "public non-native provider admits whole prospective itinerary through same engine owner");
            Job job = Board(points).Job(haul); Reach(points, () => job.ToilIndex >= 2, "positive-time public source loading");
            var pointTwin = pointFactory.Load(points.Save());
            Reach(points, () => !job.IsLive, "public physically admitted future travel", twin: pointTwin);
            c.Equal(Fix.FromInt(8), Find(points, "destination").Get<IInletPort>().Amount(supply), "independent provider delivers no duplicated cargo");
            c.Equal(points.Hash(), pointTwin.Hash(), "saved public itinerary continues through its actual route owner");
            var stationaryFactory = Points("stationary"); var stationary = NewPoint(stationaryFactory); var stationaryWorker = Find(stationary, "machine"); Policy(c, stationary, stationaryWorker.Id, "route.all", false);
            post = TestKit.Order(stationary, 0, new PostOrderCommand { Plan = "inspect", Target = Find(stationary, "source").Id });
            c.That(post.Accepted && stationaryWorker.Get<IMotion>() == null, "local stationary work requires no fabricated motion provider");
            order = Board(stationary).Order(checked((int)post.Arg0)); Reach(stationary, () => !order.IsActive, "stationary local activity");
            c.Equal(OrderState.Done, order.State, "stationary work still earns real engine time");
        }
    }
}
