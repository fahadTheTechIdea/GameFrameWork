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
    // Authored geometry and player commands only; no test-owned claim or queue algorithm.
    internal static class DockGeometryClaimTests
    {
        internal static void Register()
        {
            TestRunner.Add("Dock geometry claims: cross-facility admission and checkpoint ownership", Admission);
            TestRunner.Add("Dock geometry claims: physical mixed queues and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory PointsWithAlias(bool foreign = false) => ServiceDockTests.Factory(root =>
        {
            JsonValue alias = root.Get("entities")[0].Clone(); alias.Set("id", JsonValue.Of("alias"));
            alias.Get("components").Get("point_presence").Set("position", JsonValue.Number("20"));
            alias.Get("components").Get("point_dock").Set("slots", Content.Json.Parse(
                "[{\"id\":\"alias.bay\",\"kind\":\"service\",\"offset\":-19},{\"id\":\"alias.wait.a\",\"kind\":\"waiting\",\"offset\":-17},{\"id\":\"alias.wait.b\",\"kind\":\"waiting\",\"offset\":11},{\"id\":\"alias.exit.a\",\"kind\":\"departure\",\"offset\":20},{\"id\":\"alias.exit.b\",\"kind\":\"departure\",\"offset\":21}]"));
            root.Get("entities").Add(alias);
            StartupContentAuthoring.Add(root, "alias", "alias", foreign ? 1 : 0, alias.Get("components").Get("point_presence"));
            if (foreign)
            {
                JsonValue worker = root.Get("entities")[2].Clone(); worker.Set("id", JsonValue.Of("foreign.machine"));
                worker.Get("components").Get("point_presence").Set("position", JsonValue.Number("50")); root.Get("entities").Add(worker);
                StartupContentAuthoring.Add(root, "foreign.machine", "foreign.machine", 1, worker.Get("components").Get("point_presence"));
            }
        });
        internal static GameFactory TilesWithAlias(Action<JsonValue> edit = null)
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            var alias = root.Get("entities")[0].Clone(); alias.Set("id", JsonValue.Of("facility.alias"));
            var slots = alias.Get("components").Get("tile_work_dock").Get("slots");
            for (int i = 0; i < slots.Count; i++)
            { var slot = slots[i]; slot.Set("id", JsonValue.Of("alias." + slot.Get("id").Text)); slot.Set("x", JsonValue.Number("-3")); }
            root.Get("entities").Add(alias); edit?.Invoke(root); return Colony.Factory(additionalDefinitions: Text(root));
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Job Haul(SimGame game)
        {
            bool tile = game.World.Space is TileMap;
            int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, tile ? "facility.source" : "source").Id,
                Find(game, tile ? "facility.destination" : "destination").Id, game.World.Content.ResourceId(tile ? "steel" : "supply"), Fix.FromInt(8), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("initial physical haul refused");
        }
        private static Order Post(SimGame game, string target, bool tile = false, int seat = 0)
        {
            var result = TestKit.Order(game, seat, new PostOrderCommand { Plan = tile ? "facility.inspect" : "inspect", Target = Find(game, target).Id });
            if (!result.Accepted || game.Fault != null) throw new InvalidOperationException("order refused/faulted: " + result + "/" + game.Fault);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static Code Preview(SimGame game, Entity target, Entity actor, out Code slot)
            => Board(game).PreviewDock(target, actor, WorkDockRequest.Work(new WorkerRoleId(game.World.Space is TileMap ? "crane" : "inspect")), null,
                out slot, out _, out _, out _);
        private static void NearestWaiting(Check c, SimGame game, Entity target, Entity actor, Code proposed)
        {
            var dock = target.Get<IWorkDock>(); var origin = actor.Get<ISpatialPresence>().Capture();
            c.That(dock.TryEstimateCost(game.World, actor, new Code("alias.wait.a"), origin, out long first) &&
                dock.TryEstimateCost(game.World, actor, new Code("alias.wait.b"), origin, out long second) && second < first,
                "original provider's actual travel estimate makes waiting B strictly cheaper than A");
            c.Equal(new Code("alias.wait.b"), proposed, "claimed service bay remains exclusive while nearest free waiting slot is selected");
        }
        private static void Admission(Check c)
        {
            var factory = PointsWithAlias(true); var game = NewPoint(factory); var job = Haul(game);
            var alias = Find(game, "alias"); var foreign = Find(game, "foreign.machine");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current build refuses preceding simulation semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no second claim payload"); c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "same job state owner");
            ulong hash = game.Hash(); int nextJob = Board(game).NextJobId; var nextEntity = game.World.Entities.NextIdentity;
            c.Equal(Code.None, Preview(game, alias, foreign, out Code proposed), "foreign activity can use a distinct physical waiting place");
            NearestWaiting(c, game, alias, foreign, proposed);
            c.Equal(hash, game.Hash(), "cross-company geometry preview changes no state");
            c.Equal(nextJob, Board(game).NextJobId, "preview allocates no job"); c.Equal(nextEntity, game.World.Entities.NextIdentity, "preview allocates no entity");
            var rows = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(rows);
            c.Equal(0, rows.Count, "physical refusal exposes no private foreign job facts");
            Order order = Post(game, "alias", seat: 1); Job other = Board(game).Job(order.Node("a").JobId);
            c.Equal(proposed, other.DestinationDockSlot, "normal assignment uses same provider-based claim and travel-cost selection");
            c.Equal(new Code("load.bay"), job.SourceDockSlot, "existing physical source remains claimed");
            hash = game.Hash();
            c.Equal(Code.None, Preview(game, Find(game, "source"), Find(game, "machine.c"), out proposed), "queue admission checks other facilities' waiting claims too");
            c.Equal(new Code("load.wait.b"), proposed, "source skips its differently named waiting place reserved by foreign activity");
            c.Equal(hash, game.Hash(), "waiting alternatives remain pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cross-company bindings restore through sole job codec");
            Code original = other.DestinationDockSlot; other.DestinationDockSlot = new Code("alias.bay");
            c.Throws<InvalidOperationException>(() => game.Save(), "differently named overlapping facilities cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "hash rejects corrupt physical overlap too");
            other.DestinationDockSlot = original;
            var space = (PointSpace)game.World.Space;
            c.Equal(SpatialOverlapStatus.Disjoint, space.CheckOverlap(game.World, new PointSnapshot(space.Id, Fix.One, Fix.FromInt(2)),
                new PointSnapshot(space.Id, Fix.FromInt(2), Fix.One)), "presentation history does not turn disjoint current points into overlap");
            c.Equal(SpatialOverlapStatus.Unsupported, space.CheckOverlap(game.World, new PointSnapshot("other", Fix.One, Fix.One),
                new PointSnapshot(space.Id, Fix.One, Fix.One)), "foreign geometry is not presumed disjoint");
            space.OverlapFault = SpatialOverlapStatus.Unsupported;
            c.Equal(WorkDockCodes.UnsupportedGeometry, Preview(game, alias, foreign, out proposed), "unsupported shape gives typed refusal");
            c.Equal(Code.None, proposed, "unsupported comparison exposes no usable proposed claim");
            space.OverlapFault = (SpatialOverlapStatus)255;
            c.Throws<InvalidOperationException>(() => Preview(game, alias, foreign, out _), "undefined overlap result is rejected");
            space.OverlapFault = SpatialOverlapStatus.Disjoint;
            c.Throws<InvalidOperationException>(() => Preview(game, alias, foreign, out _), "provider cannot declare a slot disjoint from itself");
            space.OverlapFault = null; space.MutateOverlapParticipant = foreign;
            c.Throws<InvalidOperationException>(() => Preview(game, alias, foreign, out _), "mutating pure overlap query cannot produce usable admission");
            space.MutateOverlapParticipant = null;

            var faulty = NewPoint(PointsWithAlias()); Haul(faulty);
            ((PointSpace)faulty.World.Space).MutateOverlapParticipant = Find(faulty, "machine.a"); faulty.Update();
            c.That(faulty.Fault != null, "normal Update faults on mutating geometry provider before usable replanning");
            c.Throws<InvalidOperationException>(() => faulty.Save(), "faulted partial tick cannot become a resumable checkpoint");

            var tiles = TilesWithAlias(); var native = New(tiles); var worker = Setup(c, native, 26); Spawn(native, "facility.alias", 22, 20); Tick(native);
            var haul = Haul(native); Tick(native); hash = native.Hash();
            c.That(worker.Get<ITilePresence>().Tile != new TileCoord(19, 20), "native reservation protects service geometry before physical arrival");
            c.Equal(Code.None, Preview(native, Find(native, "facility.alias"), worker, out proposed), "native footprint geometry shares same coordinator");
            NearestWaiting(c, native, Find(native, "facility.alias"), worker, proposed);
            c.Equal(hash, native.Hash(), "native geometry preview is pure");
            var map = (TileMap)native.World.Space;
            TileSpatialSnapshot Rectangle(int x, int y, int w, int h) => new TileSpatialSnapshot(map.Id, new TileCoord(x, y), new TileRect(x, y, w, h),
                FixVec2.Zero, FixVec2.Zero, FixVec2.Zero, false, false);
            c.Equal(SpatialOverlapStatus.Overlapping, map.CheckOverlap(native.World, Rectangle(10, 10, 3, 4), Rectangle(12, 12, 3, 4)), "whole footprints overlap, not merely origins");
            c.Equal(SpatialOverlapStatus.Disjoint, map.CheckOverlap(native.World, Rectangle(10, 10, 3, 4), Rectangle(13, 10, 2, 4)), "shared tile edge is not overlap");
            c.Equal(SpatialOverlapStatus.Overlapping, map.CheckOverlap(native.World, Rectangle(int.MaxValue - 2, 0, 4, 1), Rectangle(int.MaxValue - 1, 0, 2, 1)), "wide arithmetic adds no engine world-size cap");
            c.Equal(SpatialOverlapStatus.Unsupported, map.CheckOverlap(native.World, Rectangle(0, 0, 0, 1), Rectangle(0, 0, 1, 1)), "empty footprint is explicit unsupported geometry");
            c.Equal(SpatialOverlapStatus.Unsupported, map.CheckOverlap(native.World, new PointSnapshot(map.Id, Fix.One, Fix.One), Rectangle(0, 0, 1, 1)), "selected native provider never invents interpretation for foreign shape");
        }
        private static void Physical(Check c)
        {
            foreach (bool tile in new[] { false, true })
            {
                var factory = tile ? TilesWithAlias() : PointsWithAlias(); var game = tile ? New(factory) : NewPoint(factory);
                if (tile)
                { Setup(c, game); Spawn(game, "facility.alias", 22, 20); Spawn(game, "facility.worker", 18, 26); }
                var haul = Haul(game); Order order = Post(game, tile ? "facility.alias" : "alias", tile);
                Job activity = Board(game).Job(order.Node("a").JobId);
                c.That(activity != null && activity.DestinationDockSlot == new Code("alias.wait.a"), "real activity waits for haul at another facility's claimed bay");
                var receipt = haul.OutReservation; var inbound = haul.InReservation;
                c.That(order.NodesDone == 0 && haul.Loaded.IsZero, "posting never instantly builds work or delivers stock");
                Reach(game, () => !haul.HandlingProgress.Equals(WorkProgress.Zero), "timed first physical handling");
                c.Equal(receipt, haul.OutReservation, "physical claims do not replace source receipt");
                c.Equal(inbound, haul.InReservation, "physical claims do not replace destination receipt");
                c.Equal(WorkProgress.Zero, order.Node("a").Progress, "cross-facility queued node earns no service while haul owns bay");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mid-handling geometry claims load identically");
                int samples = 0;
                Reach(game, () => !haul.IsLive && order.State == OrderState.Done, "mixed geometry queue completion", () =>
                {
                    samples++; int occupants = 0;
                    foreach (Entity e in game.World.Entities.All)
                        if (e.Alive && (e.Has<IWorker>() || e.Has<ITransporter>()) && (tile
                            ? e.Get<ITilePresence>()?.Tile == new TileCoord(19, 20)
                            : e.Get<PointPresence>()?.Position == Fix.One)) occupants++;
                    if (occupants > 1) throw new InvalidOperationException("cross-facility service overlap at tick " + game.Tick);
                    if (game.Hash() != twin.Hash()) throw new InvalidOperationException("saved geometry continuation diverged at tick " + game.Tick);
                }, twin);
                c.That(samples > 5, "mixed work, transfer and departure take multiple actual game updates");
                c.Equal(2, order.NodesDone, "both authored DAG nodes finish through physical service and departure");
                c.Equal(JobState.Done, haul.State, "same native haul completes normally");
                ResourceId resource = game.World.Content.ResourceId(tile ? "steel" : "supply");
                c.Equal(Fix.FromInt(24), Find(game, tile ? "facility.source" : "source").Get<IInletPort>().Amount(resource), "source debited once by physical handling");
                c.Equal(Fix.FromInt(8), Find(game, tile ? "facility.destination" : "destination").Get<IInletPort>().Amount(resource), "destination credited once without duplicate transfer");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed mixed queue checkpoint retains exact state");
            }
        }
    }
}
