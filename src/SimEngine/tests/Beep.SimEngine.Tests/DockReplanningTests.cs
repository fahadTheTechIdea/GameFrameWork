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
    internal static class DockReplanningTests
    {
        internal static void Register()
        {
            TestRunner.Add("Dock replanning: pure alternatives, retained claims and typed holds", Admission);
            TestRunner.Add("Dock replanning: physical retargeting, earned progress and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory()
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            foreach (int endpoint in new[] { 0, 1 })
            {
                var slots = root.Get("entities")[endpoint].Get("components").Get("tile_work_dock").Get("slots");
                slots[0].Set("id", JsonValue.Of("bay.a")); slots[2].Set("y", JsonValue.Number("8")); slots[4].Set("y", JsonValue.Number("10"));
                slots.Add(Content.Json.Parse("{\"id\":\"bay.b\",\"kind\":\"service\",\"x\":-1,\"y\":4}"));
            }
            root.Get("entities").Add(Content.Json.Parse("{\"id\":\"facility.obstacle\",\"kind\":\"structure\",\"components\":{\"tile_presence\":{\"space\":\"colony.tiles\",\"width\":1,\"height\":1,\"blocks\":true,\"movable\":false}}}"));
            return Colony.Factory(additionalDefinitions: Text(root), additionalDocuments: new Dictionary<string, string>
            {
                ["tile-route-areas.json"] = FrameworkProofContent.Read("tile-route-areas.json"),
                ["dock-replanning-areas.json"] = FrameworkProofContent.Read("dock-replanning-areas.json")
            });
        }
        private static GameFactory PointFactory(string fault = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions); var docks = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            var entities = Content.Json.Parse("[]"); var starts = Content.Json.Parse("[]");
            for (int i = 0; i < 3; i++) { entities.Add(docks.Get("entities")[i]); starts.Add(docks.Get("entity_setups")[i]); }
            root.Set("entities", entities); StartupContentAuthoring.UseEntries(root, starts); root.Set("work_policies", docks.Get("work_policies"));
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var motion = entities[2].Get("components").Get("point_motion"); motion.Set("rate", JsonValue.Number("6"));
            if (fault != null) motion.Set("area_fault", JsonValue.Of(fault));
            entities[1].Get("components").Get("point_dock").Get("slots")[4].Set("offset", JsonValue.Number("0.5"));
            return PointGame.Factory(Text(root), additionalDocuments: new Dictionary<string, string>
            {
                ["point-route-areas.json"] = FrameworkProofContent.Read("point-route-areas.json"),
                ["dock-replanning-point.json"] = FrameworkProofContent.Read("dock-replanning-point.json")
            });
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Job Acquire(SimGame game)
        {
            bool tile = game.World.Space is TileMap;
            var haul = game.World.Services.Get<IHaulExecution>();
            int id = haul.AcquireHaul(new PlayerId(0), Find(game, tile ? "facility.source" : "source").Id,
                Find(game, tile ? "facility.destination" : "destination").Id, game.World.Content.ResourceId(tile ? "steel" : "supply"), Fix.FromInt(8), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("replanning fixture haul refused");
        }
        private static WorkJobView? Read(SimGame game, int id, int seat = 0)
        {
            var rows = new List<WorkJobView>(); game.Seat(seat).View.Feature<WorkView>().CopyJobs(rows);
            foreach (var row in rows) if (row.Id == id) return row; return null;
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var actor = Setup(c, game);
            Policy(c, game, actor.Id, "route.all", true); var job = Acquire(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current replanning semantics identity");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "sole work coordinator declares recovery semantics");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "no second persisted dock intention or claim store");
            var source = Find(game, "facility.source"); var destination = Find(game, "facility.destination"); var steel = job.Resource;
            c.Equal(new Code("bay.a"), job.SourceDockSlot, "initial stable source commitment");
            var receipt = job.OutReservation; var destinationReceipt = job.InReservation;
            // A genuine new static obstacle changes the existing native map, not a test route algorithm.
            Spawn(game, "facility.obstacle", 19, 20);
            ulong hash = game.Hash(); int nextJob = Board(game).NextJobId; var nextEntity = game.World.Entities.NextIdentity;
            Code refusal = Board(game).PreviewDock(source, actor, WorkDockRequest.Haul(steel, ResourceHandlingPhase.Loading), null,
                out Code alternative, out _, out _, out _, job);
            c.Equal(Code.None, refusal, "same pure selector proves a reachable alternative to an obstructed committed bay");
            c.Equal(new Code("bay.b"), alternative, "pure proposed alternative has stable authored identity");
            c.Equal(new Code("bay.a"), job.SourceDockSlot, "preview does not mutate live commitments");
            c.Equal(hash, game.Hash(), "alternative preview changes no authoritative state");
            c.Equal(receipt, job.OutReservation, "preview retains the exact source receipt");
            var oldView = Read(game, job.Id); c.That(Read(game, job.Id, 1) == null, "foreign company cannot inspect private committed itinerary");
            Tick(game);
            c.Equal(new Code("bay.b"), job.SourceDockSlot, "normal Update reselects using the same actual structural router");
            c.Equal(new Code("bay.a"), oldView.Value.SourceDockSlot, "retained projection remains detached after Update");
            c.Equal(receipt, job.OutReservation, "retargeting retains actual source promise identity");
            c.Equal(destinationReceipt, job.InReservation, "retargeting retains actual inbound promise identity");
            c.Equal(nextJob, Board(game).NextJobId, "retargeting creates no replacement job");
            c.Equal(nextEntity, game.World.Entities.NextIdentity, "retargeting creates no entity");
            c.That(actor.Get<ITilePresence>().Tile != new TileCoord(19, 24), "service arrival requires actual earned movement");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "changed commitments checkpoint through their existing owner");

            var held = New(factory); var vehicle = Setup(c, held); Policy(c, held, vehicle.Id, "route.all", true); var pending = Acquire(held);
            receipt = pending.OutReservation; destinationReceipt = pending.InReservation; var cargoReceipt = pending.CargoInReservation;
            Policy(c, held, vehicle.Id, "replan.no.destination.service", true);
            c.Equal(JobState.Paused, pending.State, "unavailable future destination retains the real pickup job");
            c.Equal(WorkPolicyCodes.OutsideArea, pending.Blocker, "no compatible future service produces typed hold");
            c.Equal(new Code("bay.a"), pending.SourceDockSlot, "failed joint proposal never releases the current source");
            c.Equal(new Code("bay.a"), pending.DestinationDockSlot, "failed joint proposal never half-commits a new destination");
            c.Equal(receipt, pending.OutReservation, "held source token survives"); c.Equal(destinationReceipt, pending.InReservation, "held destination token survives");
            c.Equal(cargoReceipt, pending.CargoInReservation, "held transporter capacity token survives");
            c.Equal(Fix.FromInt(8), Find(held, "facility.source").Get<IOutletPort>().ReservedOut(steel), "held source remains exclusively reserved");
            c.Equal(Fix.Zero, pending.Loaded, "future refusal cannot earn an instant pickup");
            hash = held.Hash(); c.Equal(hash, factory.Load(held.Save()).Hash(), "no-alternative hold is durable");
            Policy(c, held, vehicle.Id, "replan.no.destination.bay", true);
            c.Equal(new Code("bay.a"), pending.SourceDockSlot, "future-only recovery keeps a valid current source claim stable");
            c.Equal(new Code("bay.b"), pending.DestinationDockSlot, "future delivery is reselected before pickup executes");
            c.Equal(destinationReceipt, pending.InReservation, "future-only recovery retains the same endpoint receipt");
            c.Equal(cargoReceipt, pending.CargoInReservation, "future-only recovery retains the same carrier promise");
            c.Equal(held.Hash(), factory.Load(held.Save()).Hash(), "joint source-to-new-destination proposal checkpoints exactly");

            var competition = New(factory); var first = Setup(c, competition); var second = Spawn(competition, "facility.worker", 18, 22);
            var third = Spawn(competition, "facility.worker", 18, 24); Tick(competition);
            foreach (var machine in new[] { first, second, third }) Policy(c, competition, machine.Id, "route.all", true);
            var one = Acquire(competition); var two = Acquire(competition); var three = Acquire(competition);
            c.Equal(new Code("bay.a"), one.SourceDockSlot, "first stable service claim"); c.Equal(new Code("bay.b"), two.SourceDockSlot, "second exclusive service claim");
            c.Equal(new Code("wait.a"), three.SourceDockSlot, "third job owns finite waiting capacity");
            var thirdActor = competition.World.Entities.Get(three.Worker);
            Policy(c, competition, thirdActor.Id, "replan.no.source.wait", true);
            c.Equal(new Code("wait.b"), three.SourceDockSlot, "invalid waiting claim moves to a real alternative queue slot");
            c.Equal(new Code("bay.a"), one.SourceDockSlot, "recovery does not steal the first live service claim");
            c.Equal(new Code("bay.b"), two.SourceDockSlot, "recovery does not steal the second live service claim");
            c.Equal(competition.Hash(), factory.Load(competition.Save()).Hash(), "exclusive queue commitments restore without another claim store");

            // Acquire the genuine claim before the later area-policy retarget invokes
            // this probe. Unconditional route-touch now correctly faults acquisition.
            var faulty = NewPoint(PointFactory("route-touch-in-area")); var faultyJob = Acquire(faulty); var faultyActor = faulty.World.Entities.Get(faultyJob.Worker);
            var originalSlot = faultyJob.DestinationDockSlot;
            faulty.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = faultyActor.Id,
                Settings = new WorkPolicySettings(true, 100, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "route.all", true, true, WorkAssignmentMode.All) });
            faulty.Update(); c.That(faulty.Fault != null, "faulty prospective provider cannot publish usable retargeting");
            c.Equal(originalSlot, faultyJob.DestinationDockSlot, "provider refusal precedes live slot publication");
            c.Throws<InvalidOperationException>(() => faulty.Save(), "faulted provider recovery cannot publish a partial checkpoint");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var actor = Setup(c, game); Policy(c, game, actor.Id, "route.all", true);
            var job = Acquire(game); var actorId = actor.Id; var source = Find(game, "facility.source"); var destination = Find(game, "facility.destination"); var steel = job.Resource;
            Policy(c, game, actorId, "replan.no.source.bay", true);
            c.Equal(new Code("bay.b"), job.SourceDockSlot, "policy edit retargets an obsolete current pickup approach");
            Reach(game, () => job.ToilIndex == 1 && !job.HandlingProgress.Equals(WorkProgress.Zero), "partial physical loading");
            var progress = job.HandlingProgress; var loaded = job.Loaded; var receipt = job.OutReservation;
            Policy(c, game, actorId, "replan.no.source.service", true);
            c.Equal(progress, job.HandlingProgress, "no service alternative retains earned handling time");
            c.Equal(loaded, job.Loaded, "partial cargo is not undone or duplicated by a policy hold");
            c.Equal(receipt, job.OutReservation, "partly consumed receipt keeps its identity");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted partial loading and claims restore exactly");
            var position = actor.Get<ITilePresence>().Position;
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(position, actor.Get<ITilePresence>().Position, "no-alternative job remains physically held");
            c.Equal(progress, job.HandlingProgress, "blocked updates cannot earn service");
            c.Equal(game.Hash(), twin.Hash(), "partial loading holds are isolated deterministic continuations");
            Policy(c, game, actorId, "route.all", true, twin);
            c.Equal(new Code("bay.b"), job.SourceDockSlot, "a still-valid claim remains stable when an earlier bay becomes permitted again");
            Reach(game, () => job.ToilIndex == 2 && job.Loaded == job.Amount, "loaded delivery approach", twin: twin);
            c.Equal(game.Hash(), twin.Hash(), "saved partial loading completes identically");
            var inbound = job.InReservation; var outbound = job.CargoOutReservation;
            Policy(c, game, actorId, "replan.no.destination.bay", true, twin);
            c.Equal(new Code("bay.b"), job.DestinationDockSlot, "loaded trip physically retargets its already-reserved delivery bay");
            c.Equal(inbound, job.InReservation, "delivery retargeting keeps the original destination capacity token");
            c.Equal(outbound, job.CargoOutReservation, "delivery retargeting keeps the cargo promise token");
            c.Equal(Fix.FromInt(8), actor.Get<ITransporter>().Cargo.Amount(steel), "retargeting never teleports or transfers loaded cargo");
            Reach(game, () => job.ToilIndex == 4 && job.DestinationDockSlot == new Code("exit.a"), "committed physical departure", twin: twin);
            ulong departureHash = game.Hash();
            c.Equal(Code.None, Board(game).PreviewDock(destination, actor, WorkDockRequest.Haul(steel, ResourceHandlingPhase.Unloading), null,
                out Code currentExit, out _, out _, out _, job), "current itinerary preview honors the actual departure phase");
            c.Equal(new Code("exit.a"), currentExit, "pure departure preview never proposes another completed service");
            c.Equal(departureHash, game.Hash(), "current departure query remains read-only");
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(steel), "stock arrives only after timed unloading");
            c.Equal(Fix.Zero, job.Loaded, "completed delivery has no remaining cargo to replay");
            // Excluding the vehicle's actual current position cannot authorize a fabricated escape.
            Policy(c, game, actorId, "replan.isolated.exit", true, twin);
            c.Equal(new Code("exit.a"), job.DestinationDockSlot, "a newly forbidden origin holds its existing commitment");
            c.Equal(JobState.Paused, job.State, "forbidden current presence cannot be teleported into a valid region");
            Policy(c, game, actorId, "replan.no.destination.exit", true, twin);
            c.Equal(new Code("exit.b"), job.DestinationDockSlot, "a now-invalid committed exit is replaced when actual origin permits recovery");
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(steel), "exit recovery never repeats delivery");
            var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "new exit intention saves before arrival");
            Reach(game, () => !job.IsLive, "actual alternative exit", () => c.Equal(game.Hash(), saved.Hash(), "retargeted departure continues identically"), saved);
            c.Equal(new TileCoord(27, 30), actor.Get<ITilePresence>().Tile, "carrier reaches the actual alternative exit over game time");
            c.Equal(Fix.FromInt(24), source.Get<IOutletPort>().Amount(steel), "only the admitted pickup was spent");
            c.Equal(Fix.Zero, destination.Get<IInletPort>().ReservedIn(steel), "real completion releases the existing capacity commitment");

            var work = New(factory); var worker = Setup(c, work); Policy(c, work, worker.Id, "route.all", true);
            var post = TestKit.Order(work, 0, new PostOrderCommand { Plan = "facility.inspect", Target = Find(work, "facility.source").Id });
            c.That(post.Accepted, "ordinary player posts authored work"); var order = Board(work).Order(checked((int)post.Arg0));
            Reach(work, () => !order.Node("a").Progress.Equals(WorkProgress.Zero), "earned ordinary machine work");
            var earned = order.Node("a").Progress; Policy(c, work, worker.Id, "replan.no.source.service", true);
            c.Equal(earned, order.Node("a").Progress, "ordinary area hold preserves earned activity work");
            Policy(c, work, worker.Id, "route.all", true);
            Reach(work, () => Board(work).Job(order.Node("a").JobId)?.DestinationDockSlot == new Code("exit.a"), "ordinary committed departure");
            var workJob = Board(work).Job(order.Node("a").JobId); Policy(c, work, worker.Id, "replan.no.source.exit", true);
            c.Equal(new Code("exit.b"), workJob.DestinationDockSlot, "ordinary earned work uses the same committed-exit recovery");
            Reach(work, () => Board(work).Job(order.Node("b").JobId)?.ToilIndex == 2 && worker.Get<ITilePresence>().Tile == new TileCoord(19, 30), "earned activity paused at its actual exit");
            var earnedExit = order.Node("b").Progress;
            var disable = new ConfigureWorkPolicyCommand { Worker = worker.Id,
                Settings = new WorkPolicySettings(false, 100, new[] { new WorkerRoleId("crane"), new WorkerRoleId("haul") }, "replan.no.source.exit", true, true, WorkAssignmentMode.All) };
            c.That(TestKit.Order(work, 0, disable).Accepted, "ordinary player disables a fully-earned activity at physical exit");
            c.Equal(NodeState.Paused, order.Node("b").State, "policy hold retains the actual DAG node at exit");
            c.Equal(earnedExit, order.Node("b").Progress, "exit hold cannot erase earned activity work");
            var workTwin = factory.Load(work.Save()); c.Equal(work.Hash(), workTwin.Hash(), "fully-earned paused exit saves with its existing node/job owners");
            Policy(c, work, worker.Id, "route.all", true, workTwin);
            Reach(work, () => !order.IsActive, "ordinary activity chain and real departure", twin: workTwin);
            c.Equal(OrderState.Done, order.State, "replanning does not replace or fail completed work");
            c.Equal(NodeState.Done, order.Node("b").State, "resumed physical exit completes the same fully-earned DAG node");
            c.Equal(work.Hash(), workTwin.Hash(), "paused exit resumes identically without duplicate work or effects");

            var obstructed = New(factory); var driver = Setup(c, obstructed); Policy(c, obstructed, driver.Id, "route.all", true);
            var obstructedJob = Acquire(obstructed);
            Reach(obstructed, () => obstructedJob.ToilIndex == 4 && obstructedJob.DestinationDockSlot == new Code("exit.a"), "permitted native committed exit");
            c.Equal(SpatialRegionStatus.Inside, obstructed.World.Content.Get<ISpatialRegionDefinition>("route.all").Check(
                Find(obstructed, "facility.destination").Get<IWorkDock>().CaptureSlot(obstructed.World, new Code("exit.a"))), "current exit remains permitted by policy");
            Spawn(obstructed, "facility.obstacle", 27, 26); Tick(obstructed);
            c.Equal(new Code("exit.b"), obstructedJob.DestinationDockSlot, "late static obstacle retargets an already-committed permitted exit");
            c.That(driver.Get<ITilePresence>().Tile != new TileCoord(27, 30), "replacement exit still requires real remaining movement");
            Reach(obstructed, () => !obstructedJob.IsLive, "physical exit around new structural blockage");
            c.Equal(new TileCoord(27, 30), driver.Get<ITilePresence>().Tile, "same native router earns alternative physical exit");
            c.Equal(Fix.FromInt(8), Find(obstructed, "facility.destination").Get<IInletPort>().Amount(steel), "late structural recovery cannot replay stock delivery");
            c.Equal(obstructed.Hash(), factory.Load(obstructed.Save()).Hash(), "new native obstruction and completed alternate exit remain durable");

            var points = PointFactory(); var point = NewPoint(points); var pointJob = Acquire(point); var machine = point.World.Entities.Get(pointJob.Worker);
            Policy(c, point, machine.Id, "route.all", false);
            Reach(point, () => pointJob.ToilIndex == 4 && pointJob.DestinationDockSlot == new Code("unload.exit.a"), "public point committed exit");
            Policy(c, point, machine.Id, "replan.point.exit", false);
            c.Equal(new Code("unload.exit.b"), pointJob.DestinationDockSlot, "independent point interval retargets through public contracts");
            var pointTwin = points.Load(point.Save()); c.Equal(point.Hash(), pointTwin.Hash(), "public changed commitments restore in isolated session");
            Reach(point, () => !pointJob.IsLive, "public timed alternative departure", () => c.Equal(point.Hash(), pointTwin.Hash(), "public saved retargeting continues identically"), pointTwin);
            c.Equal(Fix.FromRatio(21, 2), machine.Get<PointPresence>().Position, "public provider earns physical alternate-exit arrival");
            c.Equal(Fix.FromInt(8), Find(point, "destination").Get<IInletPort>().Amount(pointJob.Resource), "public delivery is committed exactly once");
            c.Equal(point.Hash(), points.Load(point.Save()).Hash(), "completed public recovery remains durable");
        }
    }
}
