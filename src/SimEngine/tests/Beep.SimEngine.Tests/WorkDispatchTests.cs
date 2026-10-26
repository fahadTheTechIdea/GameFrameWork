using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class WorkDispatchTests
    {
        public static void Register()
        {
            TestRunner.Add("Work dispatch: private queue, priority age cost and canonical admission", Admission);
            TestRunner.Add("Work dispatch: physical contention, incident ordering and saved continuation", Physical);
        }
        private static string Json => FrameworkProofContent.Read("work-dispatch.json");
        private static GameFactory Points(string extra = null)
            => PointGame.Factory(PointGame.Definitions.Replace("\"worker\": { \"roles\":", "\"work_policy\": {\"profile\":\"dispatch.initial\"}, \"worker\": { \"roles\":"), extra ?? Json);
        private static SimGame Point(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 91 });
        private static Entity Find(SimGame game, string definition)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; throw new InvalidOperationException("missing dispatch entity " + definition); }
        private static WorkPolicySettings Policy(bool enabled, bool emergency = true)
            => new WorkPolicySettings(enabled, 4, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, emergency, WorkAssignmentMode.All);
        private static Order Post(Check c, SimGame game, string plan, Entity target, int priority = 0)
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Plan = plan, Target = target.Id, Priority = priority });
            c.That(result.Accepted, "player posts authored order through engine commands");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void EnableProbe(SimGame game)
            => Find(game, "machine").Get<IWorkPolicy>().Configure(game.World, Policy(true));
        private static List<WorkDispatchOffer> Queue(SimGame game)
        {
            var queue = new List<WorkDispatchOffer>();
            game.Seat(0).View.Feature<WorkView>().CopyDispatchQueue(queue); return queue;
        }
        private static void Reach(SimGame game, Func<bool> predicate, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 240 && !predicate(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!predicate())
            {
                var facts = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Points(); SimGame game = Point(factory); Entity machine = Find(game, "machine"), near = Find(game, "source"), far = Find(game, "destination");
            Order first = Post(c, game, "dispatch.first", far), second = Post(c, game, "dispatch.second", near);
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            c.Equal(0, board.Job(first.Node("work").JobId).Priority, "node job does not copy order priority");
            c.Equal(0, board.Job(second.Node("work").JobId).Priority, "second node job has one parent priority authority");
            EnableProbe(game);
            ulong before = game.Hash(); List<WorkDispatchOffer> queue = Queue(game);
            c.Equal(2, queue.Count, "private dispatch queue contains actual open jobs");
            c.Equal(first.Id, queue[0].Order, "older equal-priority work precedes newer cheaper work");
            c.That(queue[0].AgeTicks > queue[1].AgeTicks, "queue reports exact original job age");
            c.That(queue[0].TravelCost > queue[1].TravelCost, "age ordering is not replaced by nearest-target selection");
            c.Equal(WorkRequirementStatus.Satisfied, queue[0].Status, "actual compatible worker is ready");
            c.Equal(machine.Id, queue[0].Worker, "queue names current actual offered worker");
            c.Equal("inspect", queue[0].Role.Value, "queue retains offered provider role");
            c.That(!queue[0].Emergency, "high ordinary work is not inferred emergency work");
            c.Equal(before, game.Hash(), "queue reads acquire and consume nothing");
            var hidden = new List<WorkDispatchOffer>(queue);
            PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyDispatchQueue(hidden);
            c.Equal(0, hidden.Count, "foreign queue query clears all private facts");
            PlayerView.Create(game.World, PlayerId.None).Feature<WorkView>().CopyDispatchQueue(hidden);
            c.Equal(2, hidden.Count, "observer reads detached queue facts");
            c.Throws<InvalidOperationException>(() => game.World.Services.Get<IWorkDispatchQueue>().CopyQueue(Point(factory).World, PlayerId.None, hidden), "queue rejects another session owner");
            c.Equal(before, game.Hash(), "foreign queries do not mutate own session");

            // Provider-state probe before the next update lets pure views inspect ready work.
            machine.Get<IWorkPolicy>().Configure(game.World, Policy(false));
            c.Equal(Code.None, game.Seat(0).Preview(new SetOrderPriorityCommand { Order = second.Id, Priority = int.MaxValue }), "priority preview has no arbitrary 100 ceiling");
            c.That(TestKit.Order(game, 0, new SetOrderPriorityCommand { Order = second.Id, Priority = int.MaxValue }).Accepted, "player changes sole order priority");
            EnableProbe(game); queue = Queue(game);
            c.Equal(second.Id, queue[0].Order, "explicit current player priority precedes age");
            c.Equal(int.MaxValue, queue[0].Priority, "queue compares exact maximum priority without score overflow");
            c.Equal(50, queue[1].Priority, "another order keeps its own authored priority");
            c.Equal(50, hidden[0].Priority, "retained queue facts stay detached after priority change");
            c.Equal(0, board.Job(second.Node("work").JobId).Priority, "command does not copy priority into jobs");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new SetOrderPriorityCommand { Order = second.Id, Priority = -1 }), "negative priority refuses purely");
            c.Equal(Codes.NotSeated, game.World.Check(new PlayerId(1), new SetOrderPriorityCommand { Order = second.Id, Priority = 5 }), "nonseated priority command refuses");
            machine.Get<IWorkPolicy>().Configure(game.World, Policy(false));
            c.That(TestKit.Order(game, 0, new SetOrderPriorityCommand { Order = second.Id, Priority = 0 }).Accepted, "explicit zero order priority is valid");
            EnableProbe(game); c.Equal(first.Id, Queue(game)[0].Order, "zero priority does not silently restore authored priority");
            Job deferred = board.Job(first.Node("work").JobId); long age = deferred.CreatedTick;
            deferred.RetryAt = game.Tick + 5; queue = Queue(game);
            c.Equal(second.Id, queue[0].Order, "ready work precedes a deferred higher-priority job");
            c.Equal(WorkSystem.BlockerRetry, queue[1].Blocker, "retry wait has typed engine diagnostics");
            c.Equal(WorkRequirementStatus.Pending, queue[1].Status, "retry does not offer a premature assignment");
            c.Equal(age, queue[1].CreatedTick, "retry keeps original creation age");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "saved queue restores original age and retry deadline");
            c.Equal(queue[1].AgeTicks, Queue(twin)[1].AgeTicks, "saved view derives the same current age");
            deferred.RetryAt = 0; deferred.CreatedTick = game.Tick + 1;
            c.Throws<CorruptStateException>(() => game.Save(), "future job ages refuse checkpoint");
            deferred.CreatedTick = age; deferred.Priority = 4;
            c.Throws<CorruptStateException>(() => game.Save(), "duplicate parent priority refuses checkpoint");
            deferred.Priority = 0; long created = first.CreatedTick; first.CreatedTick = game.Tick + 1;
            c.Throws<CorruptStateException>(() => game.Save(), "future order age refuses checkpoint"); first.CreatedTick = created;
            c.Throws<ContentException>(() => Points(Json.Replace("\"priority\":50", "\"priority\":-1")).NewGame(new NewGameRequest { Scenario = "points" }), "negative authored activity priority refuses initialization");
            c.Equal(32, SaveGame.Describe(game.Save()).Format, "save identity migrates without old reader");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "sole work codec migrates conditional node priority and haul requester");
            SameTick(c); Standalone(c);
        }
        private static void SameTick(Check c)
        {
            SimGame game = Point(Points()); WorkBoard board = game.World.Services.Get<WorkBoard>(); Entity near = Find(game, "source"), far = Find(game, "destination");
            Order first = board.Post(game.World.Content.Get<ActivityDef>("dispatch.first"), new PlayerId(0), far.Id, 50);
            Order second = board.Post(game.World.Content.Get<ActivityDef>("dispatch.second"), new PlayerId(0), near.Id, 50);
            Tick(game); EnableProbe(game); var queue = Queue(game);
            c.Equal(first.Node("work").JobId, queue[1].Job, "earlier ID does not beat actual cheaper same-age work");
            c.Equal(second.Id, queue[0].Order, "same-age cost breaks priority/age ties");
            c.Equal(queue[0].CreatedTick, queue[1].CreatedTick, "cost proof uses same actual creation checkpoint");
            Tick(game); c.Equal(second.Node("work").JobId, Find(game, "machine").Get<IWorker>().Job, "execution chooses the same cheaper offer");

            game = Point(Points()); Order parallel = Post(c, game, "dispatch.parallel", Find(game, "destination"));
            EnableProbe(game); queue = Queue(game);
            c.Equal("a", queue[0].Node, "equal age/cost target uses canonical stable node identity");
            c.Equal("z", queue[1].Node, "authored array order does not alter canonical tie");
            Tick(game); c.Equal(parallel.Node("a").JobId, Find(game, "machine").Get<IWorker>().Job, "canonical offer and actual assignment agree");
        }
        private static void Standalone(Check c)
        {
            GameFactory factory = Points(); SimGame game = Point(factory); EnableProbe(game); WorkBoard board = game.World.Services.Get<WorkBoard>();
            Job job = board.AddJob(new PlayerId(0), new WorkerRoleId("inspect"), int.MaxValue, Find(game, "destination").Id);
            job.Toils.Add(Toil.GoTo(job.Target)); job.Toils.Add(Toil.Work(job.Target));
            c.Equal(int.MaxValue, Queue(game)[0].Priority, "standalone job retains its own explicit priority");
            SimGame twin = factory.Load(game.Save()); c.Equal(int.MaxValue, Queue(twin)[0].Priority, "standalone priority survives conditional codec");
            c.Equal(game.Hash(), twin.Hash(), "standalone queue restore hashes identically");
            c.Throws<ArgumentOutOfRangeException>(() => board.AddJob(new PlayerId(0), new WorkerRoleId("inspect"), -1, job.Target), "direct negative standalone priority refuses");
            c.Throws<ArgumentOutOfRangeException>(() => board.Post(game.World.Content.Get<ActivityDef>("dispatch.first"), new PlayerId(0), job.Target, -1), "direct negative order priority refuses");
        }
        private static void Physical(Check c) { PriorityExecution(c); Retry(c); Contention(c); Responses(c); }
        private static void PriorityExecution(Check c)
        {
            GameFactory factory = Points(); SimGame game = Point(factory); Entity machine = Find(game, "machine"), near = Find(game, "source"), far = Find(game, "destination");
            Order low = Post(c, game, "dispatch.first", near, 1), high = Post(c, game, "dispatch.second", far, int.MaxValue);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = machine.Id, Settings = Policy(true) }).Accepted, "player enables dispatch through engine policy command");
            c.Equal(high.Node("work").JobId, machine.Get<IWorker>().Job, "new distant high-priority work is assigned first");
            c.Equal(0L, high.Node("work").Progress.WholeTicks, "assignment does not advance work before physical arrival");
            Reach(game, () => high.Node("work").Progress.WholeTicks > 1, "physical priority work");
            Job actual = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job); int id = actual.Id; long born = actual.CreatedTick;
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, actual), "engine interruption releases actual work without resetting job identity");
            c.Equal(born, actual.CreatedTick, "physical interruption preserves original queue age");
            c.Equal(id, Queue(game)[0].Job, "interrupted job returns to existing queue, not a replacement record");
            WorkProgress progress = high.Node("work").Progress;
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => high.State == OrderState.Done, "resumed high-priority work", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved interrupted queue resumes deterministic physical work");
            c.That(progress.WholeTicks > 0, "saved interrupted work had real progress to preserve");
            Reach(game, () => low.State == OrderState.Done, "aged remaining work", twin);
            c.Equal(game.Hash(), twin.Hash(), "remaining low-priority work executes when higher work finishes");
            c.Equal(0, machine.Get<IWorker>().Job, "finished jobs release machine normally");
        }
        private static void Retry(Check c)
        {
            GameFactory factory = Points(); SimGame game = Point(factory); Entity machine = Find(game, "machine");
            Order order = Post(c, game, "dispatch.first", Find(game, "destination"));
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = machine.Id, Settings = Policy(true) }).Accepted, "player enables real routed work");
            Job job = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job); long born = job.CreatedTick;
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "engine spatial provider closes actual approach access");
            c.Equal(JobState.Open, job.State, "unreachable physical approach returns same job to queue");
            c.That(job.Failures > 0 && job.RetryAt > game.Tick, "actual failure sets existing deterministic retry deadline");
            c.Equal(born, job.CreatedTick, "actual route failure does not restart age");
            c.Equal(WorkSystem.BlockerRetry, Queue(game)[0].Blocker, "actual failure exposes deferred retry rather than phantom readiness");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }).Accepted, "player opens provider access without bypassing retry time");
            SimGame twin = factory.Load(game.Save()); long now = game.Tick;
            bool unclaimed = true; int steps = 0;
            while (game.Tick < job.RetryAt && steps++ < 120)
            { unclaimed &= job.State == JobState.Open && machine.Get<IWorker>().Job == 0; Tick(game); Tick(twin); }
            if (game.Tick < job.RetryAt) throw new InvalidOperationException("retry deadline probe exceeded bound");
            c.That(unclaimed && game.Tick > now, "saved retry waits through real game time without claiming a worker early");
            c.Equal(game.Hash(), twin.Hash(), "retry wait is saved and deterministic");
            Reach(game, () => job.State == JobState.Assigned, "physical retry assignment", twin);
            c.Equal(born, job.CreatedTick, "successful retry retains original job age");
            c.Equal(0L, order.Node("work").Progress.WholeTicks, "retry assignment cannot skip travel or timed work");
            Reach(game, () => order.State == OrderState.Done, "physical retried work", twin);
            c.Equal(game.Hash(), twin.Hash(), "retry continuation physically completes after saved waiting");

            game = Point(factory); order = Post(c, game, "dispatch.parallel", Find(game, "destination"));
            c.Equal(2, Queue(game).Count, "parallel blocked work has two actual open jobs");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels pending physical plan normally");
            c.Equal(0, Queue(game).Count, "cancellation removes queue offers through existing final pruning");
            c.Equal(OrderState.Cancelled, order.State, "retained detached execution reference records actual cancellation");
        }
        private static void Contention(Check c)
        {
            GameFactory factory = Points(); SimGame game = Point(factory); EnableProbe(game); WorkBoard board = game.World.Services.Get<WorkBoard>(); Entity machine = Find(game, "machine"), far = Find(game, "destination"), near = Find(game, "source");
            Entity other = game.World.Spawn(game.World.Content.Get<EntityDef>("machine"), new PlayerId(0)); game.World.Settle();
            other.Get<PointPresence>().Move(game.World, Fix.FromInt(10)); other.Get<IWorkPolicy>().Configure(game.World, Policy(true));
            Entity middle = game.World.Spawn(game.World.Content.Get<EntityDef>("source"), new PlayerId(0)); game.World.Settle();
            middle.Get<PointPresence>().Move(game.World, Fix.FromRatio(1, 2)); far.Get<PointPresence>().Move(game.World, Fix.FromInt(9));
            Order first = board.Post(game.World.Content.Get<ActivityDef>("dispatch.first"), new PlayerId(0), near.Id, 50);
            Order second = board.Post(game.World.Content.Get<ActivityDef>("dispatch.second"), new PlayerId(0), middle.Id, 50);
            Order third = board.Post(game.World.Content.Get<ActivityDef>("dispatch.first"), new PlayerId(0), far.Id, 50);
            Tick(game);
            c.Equal(first.Node("work").JobId, machine.Get<IWorker>().Job, "first same-age work claims its actual zero-cost machine");
            c.Equal(third.Node("work").JobId, other.Get<IWorker>().Job, "next choice uses recalculated cost 1 instead of stale second-job cost 0.5");
            c.That(machine.Get<IWorker>().Job != other.Get<IWorker>().Job, "competing jobs do not double-claim machine");
            var queue = Queue(game);
            c.Equal(1, queue.Count, "assigned jobs are not advertised as unreserved queue offers");
            c.Equal(second.Id, queue[0].Order, "unclaimed middle job remains existing work rather than disappearing");
            c.Equal(WorkSystem.BlockerWorkerBusy, queue[0].Blocker, "remaining job reports actual busy machines");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => first.State == OrderState.Done && second.State == OrderState.Done && third.State == OrderState.Done, "contended physical queue", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved contended queue completes all work identically");
        }
        private static void Responses(Check c)
        {
            GameFactory factory = Factory(FrameworkProofContent.Read("tile-work-policies.json")); SimGame game = New(factory);
            ComponentList<IWorker> workers = game.World.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++) c.That(TestKit.Order(game, workers[i].Entity.Owner.Value,
                new MoveVehicleCommand { Vehicle = workers[i].Entity.Id, To = workers[i].Entity.Get<ITilePresence>().Tile }).Accepted, "fixture players hold existing workers");
            Entity target = Spawn(game, "policy.target", 20, 20), worker = Spawn(game, "policy.external", 16, 20);
            var disabled = new WorkPolicySettings(false, 4, new[] { new WorkerRoleId("crane") }, null, true, true, WorkAssignmentMode.All);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = worker.Id, Settings = disabled }).Accepted, "player disables external response worker before competing orders");
            Order ordinary = Post(c, game, "policy.tile-work", target, int.MaxValue);
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new SetOrderPriorityCommand { Order = ordinary.Id, Priority = 5 }), "foreign seated priority command refuses");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "engine incident creates actual bound response");
            Order response = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            worker.Get<IWorkPolicy>().Configure(game.World, new WorkPolicySettings(true, 4, new[] { new WorkerRoleId("crane") }, null, true, false, WorkAssignmentMode.All));
            var queue = Queue(game);
            c.Equal(ordinary.Id, queue[0].Order, "ready ordinary work precedes an emergency with no eligible machine");
            c.That(queue.Exists(x => x.Order == response.Id && x.Status != WorkRequirementStatus.Satisfied), "ineligible response remains visible without claiming readiness");
            c.Equal(0, worker.Get<IWorker>().Job, "pure queue cannot claim even a ready machine");
            worker.Get<IWorkPolicy>().Configure(game.World, new WorkPolicySettings(true, 4, new[] { new WorkerRoleId("crane") }, null, true, true, WorkAssignmentMode.All));
            queue = Queue(game);
            c.Equal(response.Id, queue[0].Order, "valid ready incident precedes older maximum ordinary priority");
            c.That(queue[0].Emergency && !queue[1].Emergency, "queue emergency facts use actual incident binding only");
            c.Equal(int.MaxValue, queue[1].Priority, "emergency ranking does not inflate or overwrite ordinary priority");
            SimGame twin = factory.Load(game.Save());
            Tick(game); Tick(twin);
            c.Equal(response.Node("work").JobId, worker.Get<IWorker>().Job, "actual external worker assignment follows emergency queue");
            c.Equal(0L, ordinary.Node("work").Progress.WholeTicks, "ordinary job waits for actual unavailable machine");
            Reach(game, () => !target.Get<ContractIncident>().ReadIncident().Active, "ordered incident response", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved emergency queue physically completes with identical hash");
            Reach(game, () => ordinary.State == OrderState.Done, "ordinary work after response", twin);
            c.Equal(game.Hash(), twin.Hash(), "ordinary competing work resumes after real response completion");

            // Renewed provider binding is rejected by both diagnostics and existing execution guards.
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = worker.Id, Settings = disabled }).Accepted, "player holds response for binding probe");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "next incident posts fresh normal work");
            response = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            target.Get<ContractIncident>().Renew(game.World);
            queue = Queue(game);
            c.Equal(IncidentCodes.StaleResponse, queue[0].Blocker, "stale response is a typed invalid offer");
            c.Equal(WorkRequirementStatus.Invalid, queue[0].Status, "stale response cannot receive an emergency assignment");
            c.That(!queue[0].Emergency, "invalid binding never fabricates emergency eligibility");
            Tick(game); c.Equal(OrderState.Cancelled, response.State, "existing response owner retires stale execution normally");
        }
    }
}
