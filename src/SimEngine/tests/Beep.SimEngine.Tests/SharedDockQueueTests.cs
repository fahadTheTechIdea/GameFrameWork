using System;
using System.Collections.Generic;
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
    internal static class SharedDockQueueTests
    {
        internal static void Register()
        {
            TestRunner.Add("Shared dock queues: fair admission, eligible waiters and geometry isolation", Admission);
            TestRunner.Add("Shared dock queues: physical promotion and saved continuation", Physical);
        }
        private static GameFactory Points(bool separateBay = false, bool away = false) => ServiceDockTests.Factory(root =>
        {
            root.Get("activities")[0].Set("nodes", Content.Json.Parse("[{\"id\":\"a\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}}]"));
            if (away) root.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[1].Set("offset", JsonValue.Number("40"));
            // An actual idle machine occupies the first bay. Empty allowed roles is an explicit policy.
            root.Get("work_policies").Add(Content.Json.Parse("{\"id\":\"queue.blocker\",\"enabled\":true,\"priority\":0,\"allowed_roles\":[],\"area\":null,\"interruptible\":true,\"emergency_eligible\":true,\"assignment_mode\":\"all\",\"states\":{\"disabled\":\"blocked\",\"standby\":\"blocked\"}}"));
            void Worker(string id, int position, int owner, bool blocker)
            {
                var entity = root.Get("entities")[2].Clone(); entity.Set("id", JsonValue.Of(id));
                var parts = entity.Get("components"); parts.Get("point_presence").Set("position", JsonValue.Number(position.ToString()));
                parts.Remove("transporter"); if (blocker) parts.Get("work_policy").Set("profile", JsonValue.Of("queue.blocker"));
                root.Get("entities").Add(entity); StartupContentAuthoring.Add(root, id, id, owner, parts.Get("point_presence"));
            }
            Worker("queue.blocker", 1, 0, true); Worker("foreign.a", 31, 1, false); Worker("foreign.b", 32, 1, false);
            var alias = root.Get("entities")[0].Clone(); alias.Set("id", JsonValue.Of("alias"));
            var components = alias.Get("components"); components.Get("point_presence").Set("position", JsonValue.Number("20"));
            components.Get("point_dock").Set("slots", Content.Json.Parse("[{\"id\":\"alias.bay.a\",\"kind\":\"service\",\"offset\":-19},{\"id\":\"alias.wait.a\",\"kind\":\"waiting\",\"offset\":11},{\"id\":\"alias.wait.b\",\"kind\":\"waiting\",\"offset\":12},{\"id\":\"alias.exit.a\",\"kind\":\"departure\",\"offset\":20},{\"id\":\"alias.exit.b\",\"kind\":\"departure\",\"offset\":21}]"));
            if (separateBay) components.Get("point_dock").Get("slots").Add(Content.Json.Parse("{\"id\":\"alias.bay.b\",\"kind\":\"service\",\"offset\":1}"));
            root.Get("entities").Add(alias); StartupContentAuthoring.Add(root, "alias", "alias", 1, components.Get("point_presence"));
        });
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static (long Id, Order Order, Job Job) Post(Check c, SimGame game, string target, int seat, bool tile = false)
        {
            var intent = new PlanIntent(tile ? "activity:facility.inspect" : "activity:inspect", Content.Json.Parse("{\"target\":" + Find(game, target).Id.Value + "}"), null, 100, false);
            var result = TestKit.Order(game, seat, new SubmitPlanCommand { Intent = intent });
            c.That(result.Accepted && game.Fault == null, "ordinary player intent submits physical native work: " + result + "/" + game.Fault);
            var plans = new List<PlanView>(); game.Seat(seat).View.Feature<WorkView>().CopyPlans(plans);
            foreach (var plan in plans)
                if (plan.Id == result.Arg0)
                { var order = Board(game).Order(plan.Order); return (plan.Id, order, Board(game).Job(order.Node("a").JobId)); }
            throw new InvalidOperationException("missing submitted native plan/order");
        }
        private static void FreeBay(Check c, SimGame game, bool tile = false, SimGame twin = null)
        {
            foreach (var current in twin == null ? new[] { game } : new[] { game, twin })
                c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = Find(current, "queue.blocker").Id,
                    Home = Find(current, tile ? "facility.destination" : "source").Id }).Accepted, "player sends real idle bay occupant home through engine motion");
        }
        private static Code Preview(SimGame game, Entity carrier, bool tile, out Code slot)
            => Board(game).PreviewDock(Find(game, tile ? "facility.alias" : "alias"), carrier,
                WorkDockRequest.Work(new WorkerRoleId(tile ? "crane" : "inspect")), null, out slot, out _, out _, out _);
        private static void Pause(Check c, SimGame game, long plan, bool paused, SimGame twin = null)
        {
            foreach (var current in twin == null ? new[] { game } : new[] { game, twin })
                c.That(TestKit.Order(current, 0, new ConfigurePlanCommand { Plan = plan, Group = null, Priority = 100, Paused = paused }).Accepted,
                    "ordinary plan pause controls existing physical work");
        }
        private static (GameFactory Factory, SimGame Game, Entity Foreign) NativeSetup(Check c)
        {
            var factory = DockGeometryClaimTests.TilesWithAlias(root =>
            {
                root.Get("activities")[0].Set("nodes", Content.Json.Parse("[{\"id\":\"a\",\"executor\":\"work.staffed\",\"roles\":[\"crane\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}}]"));
                var blocker = root.Get("entities")[2].Clone(); blocker.Set("id", JsonValue.Of("queue.blocker")); root.Get("entities").Add(blocker);
            });
            var game = New(factory); Setup(c, game, 22); Spawn(game, "facility.alias", 22, 20, 1);
            var blocker = Spawn(game, "queue.blocker", 19, 20);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = blocker.Id,
                Settings = new WorkPolicySettings(true, 0, Array.Empty<WorkerRoleId>(), null, true, true, WorkAssignmentMode.All) }).Accepted,
                "explicit empty role policy keeps actual native bay occupant idle, not removed or fabricated");
            var foreign = Spawn(game, "facility.worker", 18, 24, 1); Tick(game); return (factory, game, foreign);
        }
        private static void Admission(Check c)
        {
            var factory = Points(); var game = NewPoint(factory); var older = Post(c, game, "source", 0);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "exact build identifies current shared queue semantics");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing job codec alone owns physical claims");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "same authoritative physical work coordinator and payload");
            Reach(game, () => Find(game, "queue.blocker").Get<PointPresence>().Position == Fix.One &&
                game.World.Entities.Get(older.Job.Worker).Get<PointPresence>().Position == Fix.FromInt(3), "arrived eligible source waiter");
            FreeBay(c, game); c.Equal(Fix.Zero, Find(game, "queue.blocker").Get<PointPresence>().Position, "home travel physically vacates bay after work phase");
            c.Equal(new Code("load.wait.a"), older.Job.DestinationDockSlot, "arrived waiter has not yet been promoted at this real checkpoint");
            ulong hash = game.Hash(); var allocator = game.World.Entities.NextIdentity; int jobs = Board(game).NextJobId;
            c.Equal(Code.None, Preview(game, Find(game, "foreign.a"), false, out var proposed), "new foreign admission can wait at its actual queue place");
            c.Equal(new Code("alias.wait.a"), proposed, "new differently named facility cannot steal the vacant bay from arrived older work");
            c.Equal(hash, game.Hash(), "fair admission preview is pure"); c.Equal(allocator, game.World.Entities.NextIdentity, "fairness allocates no entity");
            c.Equal(jobs, Board(game).NextJobId, "fairness allocates no job or queue row");
            var rows = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(rows); c.Equal(0, rows.Count, "fair admission leaks no foreign private job rows");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "derived queue order needs no additional persistence state");
            var space = (PointSpace)game.World.Space;
            space.MutateOnlyCoincidentOverlap = true; space.MutateOverlapParticipant = game.World.Entities.Get(older.Job.Worker);
            c.Throws<InvalidOperationException>(() => Preview(game, Find(game, "foreign.a"), false, out _), "queue geometry cannot mutate its competing eligible worker and publish usable admission");
            space.MutateOverlapParticipant = null; space.MutateOnlyCoincidentOverlap = false;

            var distinct = NewPoint(Points(separateBay: true)); var first = Post(c, distinct, "source", 0);
            Reach(distinct, () => distinct.World.Entities.Get(first.Job.Worker).Get<PointPresence>().Position == Fix.FromInt(3), "distinct geometry waiter");
            FreeBay(c, distinct);
            c.Equal(Code.None, Preview(distinct, Find(distinct, "foreign.a"), false, out proposed), "unrelated service bay remains admissible");
            c.Equal(new Code("alias.bay.b"), proposed, "FIFO yields overlapping bay only, not every slot of the other facility");

            var paused = NewPoint(Points()); var pausing = Post(c, paused, "source", 0); Pause(c, paused, pausing.Id, true); FreeBay(c, paused);
            c.Equal(Code.None, Preview(paused, Find(paused, "foreign.a"), false, out proposed), "paused plan cannot monopolize released service");
            c.Equal(new Code("alias.bay.a"), proposed, "paused waiter is explicitly ineligible, not a competing service claim");
            c.Equal(WorkProgress.Zero, pausing.Order.Node("a").Progress, "paused waiter earns no service");

            var remote = NewPoint(Points(away: true)); var traveling = Post(c, remote, "source", 0); FreeBay(c, remote);
            c.That(remote.World.Entities.Get(traveling.Job.Worker).Get<PointPresence>().Position != Fix.FromInt(40), "older worker is actually still traveling to its wait place");
            c.Equal(Code.None, Preview(remote, Find(remote, "foreign.a"), false, out proposed), "away waiter cannot reserve an unclaimed service bay through queue order");
            c.Equal(new Code("alias.bay.a"), proposed, "FIFO is among arrived eligible workers, not all old jobs");

            var disabled = NewPoint(Points()); var pending = Post(c, disabled, "source", 0);
            c.That(TestKit.Order(disabled, 0, new ConfigureWorkPolicyCommand { Worker = pending.Job.Worker,
                Settings = new WorkPolicySettings(false, 100, new[] { new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "normal disabled policy retains waiter without granting service");
            FreeBay(c, disabled); c.Equal(Code.None, Preview(disabled, Find(disabled, "foreign.a"), false, out proposed), "disabled waiter does not head-of-line block foreign work");
            c.Equal(new Code("alias.bay.a"), proposed, "disabled queue eligibility shares ordinary policy rules");

            var native = NativeSetup(c); var queued = Post(c, native.Game, "facility.source", 0, true);
            Reach(native.Game, () => native.Game.World.Entities.Get(queued.Job.Worker).Get<ITilePresence>().Tile == new TileCoord(19, 22), "native arrived shared-bay waiter");
            FreeBay(c, native.Game, true);
            Reach(native.Game, () => Find(native.Game, "queue.blocker").Get<ITilePresence>().Tile != new TileCoord(19, 20) &&
                queued.Job.DestinationDockSlot == new Code("wait.a"), "native occupant actually vacates before next work phase");
            hash = native.Game.Hash();
            c.Equal(Code.None, Preview(native.Game, native.Foreign, true, out proposed), "native geometric FIFO admits actual waiting alternative");
            c.Equal(new Code("alias.wait.b"), proposed, "native foreign facility cannot steal shared bay or other facility's waiting claim");
            c.Equal(hash, native.Game.Hash(), "native shared queue preview stays pure");
            c.Equal(native.Game.Hash(), native.Factory.Load(native.Game.Save()).Hash(), "native newly vacant shared queue restores identically");
        }
        private static void Physical(Check c)
        {
            var factory = Points(); var game = NewPoint(factory); var older = Post(c, game, "source", 0); var younger = Post(c, game, "alias", 1);
            EntityId olderWorker = older.Job.Worker;
            Reach(game, () => game.World.Entities.Get(older.Job.Worker).Get<PointPresence>().Position == Fix.FromInt(3) &&
                game.World.Entities.Get(younger.Job.Worker).Get<PointPresence>().Position == Fix.FromInt(31), "two facilities' physical waiting");
            c.Equal(WorkProgress.Zero, younger.Order.Node("a").Progress, "remote facility earns no queued work");
            var twin = factory.Load(game.Save()); FreeBay(c, game, twin: twin);
            ulong hash = game.Hash(); c.Equal(Code.None, Preview(game, Find(game, "foreign.b"), false, out Code selected), "new arrival also waits behind both acquired physical waiters");
            c.Equal(new Code("alias.wait.b"), selected, "new arrival cannot bypass the global shared-bay queue"); c.Equal(hash, game.Hash(), "physical queue preview stays pure");
            Reach(game, () => older.Order.Node("a").Progress.WholeTicks >= 2, "older facility physical promotion", twin: twin);
            c.Equal(new Code("load.bay"), older.Job.DestinationDockSlot, "oldest arrived worker receives the actual shared bay");
            c.Equal(new Code("alias.wait.a"), younger.Job.DestinationDockSlot, "younger facility retains its actual waiting claim");
            c.Equal(WorkProgress.Zero, younger.Order.Node("a").Progress, "shared geometry has no overlapping work");
            Pause(c, game, older.Id, true, twin); WorkProgress earned = older.Order.Node("a").Progress;
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(earned, older.Order.Node("a").Progress, "paused service keeps earned work without advancing");
            c.Equal(new Code("load.bay"), older.Job.DestinationDockSlot, "paused active service is not stolen by queue arbitration");
            c.Equal(WorkProgress.Zero, younger.Order.Node("a").Progress, "younger worker cannot preempt paused physical service");
            c.Equal(game.Hash(), twin.Hash(), "paused acquired shared-bay queue continues identically");
            twin = factory.Load(game.Save()); Pause(c, game, older.Id, false, twin);
            Reach(game, () => younger.Order.Node("a").Progress.WholeTicks >= 2, "younger facility timed promotion after real departure", twin: twin);
            c.Equal(new Code("alias.bay.a"), younger.Job.DestinationDockSlot, "shared bay becomes available under the younger facility's own authored slot ID");
            c.That(game.World.Entities.Get(olderWorker).Get<PointPresence>().Position != Fix.One, "older machine physically departs before younger service");
            Reach(game, () => older.Order.State == OrderState.Done && younger.Order.State == OrderState.Done, "shared facility work completion", twin: twin);
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved completion preserves deterministic queue behavior");
            c.Equal(Fix.FromInt(32), Find(game, "source").Get<IInletPort>().Amount(game.World.Content.ResourceId("supply")), "queue arbitration fabricates no resource consumption");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "fully completed shared queue checkpoints through existing owners");

            var native = NativeSetup(c); var actual = native.Game; var resource = actual.World.Content.ResourceId("steel");
            int haulId = actual.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(actual, "facility.source").Id,
                Find(actual, "facility.destination").Id, resource, Fix.FromInt(8), 100);
            var haul = Board(actual).Job(haulId) ?? throw new InvalidOperationException("native queued haul refused");
            var work = Post(c, actual, "facility.alias", 1, true);
            Reach(actual, () => actual.World.Entities.Get(haul.Worker).Get<ITilePresence>().Tile == new TileCoord(19, 22) &&
                actual.World.Entities.Get(work.Job.Worker).Get<ITilePresence>().Tile == new TileCoord(19, 24), "native mixed cross-company waiting");
            var saved = native.Factory.Load(actual.Save()); FreeBay(c, actual, true, saved);
            Reach(actual, () => !haul.HandlingProgress.Equals(WorkProgress.Zero), "older native haul wins physical shared service", twin: saved);
            c.Equal(new Code("bay"), haul.SourceDockSlot, "arrived older hauling promotes under actual source slot identity");
            c.Equal(WorkProgress.Zero, work.Order.Node("a").Progress, "foreign ordinary worker earns no overlapping service during loading");
            Reach(actual, () => haul.State == JobState.Done && work.Order.State == OrderState.Done, "native mixed shared queue completes", () =>
            {
                int occupants = 0; foreach (Entity e in actual.World.Entities.All)
                    if (e.Alive && e.Has<IWorker>() && e.Get<ITilePresence>()?.Tile == new TileCoord(19, 20)) occupants++;
                if (occupants > 1 || actual.Hash() != saved.Hash()) throw new InvalidOperationException("native shared-bay overlap/divergence at tick " + actual.Tick);
            }, saved);
            c.Equal(Fix.FromInt(24), Find(actual, "facility.source").Get<IInletPort>().Amount(resource), "FIFO preserves single actual source debit");
            c.Equal(Fix.FromInt(8), Find(actual, "facility.destination").Get<IInletPort>().Amount(resource), "FIFO preserves single timed physical delivery");
            c.Equal(actual.Hash(), native.Factory.Load(actual.Save()).Hash(), "native mixed queue completion saves exact current state");
        }
    }
}
