using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // JSON, player input, bounded normal updates and observations; explicit corruption probes are labelled.
    internal static class SelectedWorkerTests
    {
        internal static void Register()
        {
            TestRunner.Add("Selected workers: intent contracts, admission and checkpoint ownership", Contracts);
            TestRunner.Add("Selected workers: physical service, parallel nodes and saved continuation", Physical);
        }
        private static GameFactory Factory(bool extension = false, Action<JsonValue> edit = null)
            => WorkerServiceTests.Factory(extension, root =>
            {
                for (int i = 2; i < root.Get("entities").Count; i++)
                {
                    var parts = root.Get("entities")[i].Get("components");
                    (parts.Get("equipment_condition") ?? parts.Get("proof_service_condition")).Set("initial", JsonValue.Number("4"));
                }
                edit?.Invoke(root);
            }, activityDocument: "selected-workers.json");
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 966 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing selected worker " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static Fix Condition(SimGame game, string entity) => ConditionContracts.Read(game.World, Find(game, entity).Get<IConditionProvider>(new Code("integrity"))).Value;
        private static Fix Stock(SimGame game) => Find(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static PlanIntent Intent(SimGame game, string worker = "machine.d", bool paused = false)
            => Raw("selected.service", "{\"target\":" + Find(game, "source").Id.Value + ",\"workers\":[{\"node\":\"service\",\"role\":null,\"worker\":" + Find(game, worker).Id.Value + "}]}", paused);
        private static PlanIntent Raw(string definition, string arguments, bool paused = true)
            => new PlanIntent("activity:" + definition, Json.Parse(arguments), "service.group", 40, paused);
        private static PlanView Plan(SimGame game, long id)
        {
            var plans = new List<PlanView>(); View(game).CopyPlans(plans);
            foreach (var plan in plans) if (plan.Id == id) return plan; throw new InvalidOperationException("missing plan " + id);
        }
        private static long Submit(Check c, SimGame game, PlanIntent intent)
        { var result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent }); c.That(result.Accepted, "ordinary command admits exact selected worker intent"); return result.Arg0; }
        private static Order Active(SimGame game, long id) => Board(game).Order(Plan(game, id).Order) ?? throw new InvalidOperationException("missing actual plan order");
        private static ActivityNodeView Node(SimGame game, int order, string id)
        {
            var nodes = new List<ActivityNodeView>(); if (!View(game).TryCopyNodes(order, nodes)) throw new InvalidOperationException("private node read refused");
            foreach (var node in nodes) if (node.Id == id) return node; throw new InvalidOperationException("missing selected node");
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("selected worker game update failed: " + game.Fault + "/" + twin?.Fault); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 240 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (var order in Board(game).Orders)
                    foreach (var node in order.Nodes) facts.Add(order.Id + "/" + node.Id + "/" + node.State + "/requested=" + order.WorkerSelection.WorkerFor(node.Id) + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " timeout at game update " + game.Clock.StepIndex + ": " + string.Join(";", facts));
            }
        }
        private static void Policy(Check c, SimGame game, string worker, bool enabled, SimGame twin = null)
        {
            foreach (var current in twin == null ? new[] { game } : new[] { game, twin })
            {
                Entity entity = Find(current, worker); var policy = WorkPolicyRules.Read(entity);
                c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand { Worker = entity.Id,
                    Settings = new WorkPolicySettings(enabled, policy.Priority, policy.AllowedRoles, policy.Area, policy.Interruptible, policy.EmergencyEligible, WorkAssignmentMode.All) }).Accepted, "normal policy manages selected worker eligibility");
            }
        }
        private static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var definition = game.World.Content.Get<ActivityDef>("selected.parallel");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current selected worker build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean order layout migration");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing board owns selection codec");
            var original = new List<ActivityWorkerBinding> { new ActivityWorkerBinding("b", Find(game, "machine.b").Id), new ActivityWorkerBinding("a", Find(game, "machine.a").Id) };
            IActivityWorkerSelection selection = new ActivityWorkerSelection(original); original.Clear();
            c.Equal(2, selection.Bindings.Count, "selection captures immutable input"); c.Equal("a", selection.Bindings[0].Node, "bindings have stable ordinal order");
            c.Equal(EntityId.None, selection.WorkerFor("unbound"), "unbound node explicitly retains any-compatible policy");
            c.Throws<NotSupportedException>(() => ((IList<ActivityWorkerBinding>)selection.Bindings).Clear(), "read-only contract cannot mutate selection");
            c.Throws<ArgumentException>(() => new ActivityWorkerBinding("a", EntityId.None), "empty selected identity rejects");
            c.Throws<ArgumentException>(() => new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("a", new EntityId(1)), new ActivityWorkerBinding("a", new EntityId(2)) }), "duplicate requested node rejects");
            c.Throws<ArgumentException>(() => new ActivityWorkerSelection(new[] { default(ActivityWorkerBinding) }), "default struct cannot fabricate binding");
            var canonicalSelection = (ActivityWorkerSelection)selection;
            byte[] selectionBytes = Encode(canonicalSelection);
            var reversed = new ActivityWorkerSelection(new[] { canonicalSelection.Bindings[1], canonicalSelection.Bindings[0] });
            c.Equal(Convert.ToBase64String(selectionBytes), Convert.ToBase64String(Encode(reversed)), "embedded selection codec has canonical order");
            var decoded = ActivityWorkerSelection.Sync(new StateReader(selectionBytes, 0, selectionBytes.Length), null);
            c.Equal(canonicalSelection.WorkerFor("b"), decoded.WorkerFor("b"), "same embedded decoder restores exact binding");
            c.Throws<CorruptStateException>(() => ActivityWorkerSelection.Sync(new StateReader(selectionBytes, 0, selectionBytes.Length - 1), canonicalSelection), "truncated selection cannot replace immutable value");
            foreach (var malformed in new[] { BindingBytes("b", 3, "a", 4), BindingBytes("a", 3, "a", 4), BindingBytes("a", 0), BindingBytes(null, 3) })
                c.Throws<CorruptStateException>(() => ActivityWorkerSelection.Sync(new StateReader(malformed, 0, malformed.Length), canonicalSelection), "malformed/noncanonical embedded selection rejects");
            c.Equal(Convert.ToBase64String(selectionBytes), Convert.ToBase64String(Encode(canonicalSelection)), "failed decode cannot mutate original immutable selection");
            int target = Find(game, "source").Id.Value, actor = Find(game, "machine.d").Id.Value;
            foreach (string workers in new[] { "[]", "null", "[{\"node\":\"missing\",\"role\":null,\"worker\":" + actor + "}]", "[{\"node\":\"settle\",\"role\":null,\"worker\":" + actor + "}]", "[{\"node\":\"service\",\"role\":null,\"worker\":0}]", "[{\"node\":\"service\",\"role\":null,\"worker\":-1}]", "[{\"node\":\"service\",\"role\":null,\"worker\":" + actor + ",\"other\":1}]", "[{\"node\":\"service\",\"role\":null,\"worker\":" + actor + "},{\"node\":\"service\",\"role\":null,\"worker\":" + actor + "}]" })
            {
                var bad = Raw("selected.service", "{\"target\":" + target + ",\"workers\":" + workers + "}");
                c.Equal(Codes.InvalidArgument, View(game).PreviewPlan(bad), "malformed or non-machine binding refuses preview");
                c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, new SubmitPlanCommand { Intent = bad }).Refusal, "malformed binding command parity");
            }
            c.Equal(Codes.InvalidArgument, View(game).PreviewPlan(Raw("selected.zero", "{\"target\":" + target + ",\"workers\":[{\"node\":\"zero\",\"role\":null,\"worker\":" + actor + "}]}")), "zero-time role node cannot request machine");
            c.Equal(Codes.NotFound, View(game).PreviewPlan(Raw("selected.service", "{\"target\":" + target + ",\"workers\":[{\"node\":\"service\",\"role\":null,\"worker\":2147483647}]}")), "unallocated selected machine refuses");
            c.Equal(WorkSystem.BlockerNoWorker, View(game).PreviewPlan(Raw("selected.service", "{\"target\":" + target + ",\"workers\":[{\"node\":\"service\",\"role\":null,\"worker\":" + target + "}]}")), "facility cannot be selected as worker");
            Entity foreign = Find(game, "machine.d"); foreign.Owner = game.Seat(1).Id;
            c.Equal(Codes.NotOwned, View(game).PreviewPlan(Intent(game, paused: true)), "foreign machine refuses without operational detail"); foreign.Owner = game.Seat(0).Id;
            var paused = Intent(game, paused: true); ulong hash = game.Hash();
            c.Equal(Code.None, View(game).PreviewPlan(paused), "exact owned machine preview ready"); c.Equal(hash, game.Hash(), "preview selection is pure");
            long id = Submit(c, game, paused); c.Equal(0, Board(game).Orders.Count, "paused intent creates no execution");
            c.Equal(0, Find(game, "machine.d").Get<IWorker>().Job, "requested identity is not an acquired worker");
            c.Equal(Fix.FromInt(32), Stock(game), "selection reserves no spares");
            var privatePlans = new List<PlanView>(); View(game, 1).CopyPlans(privatePlans); c.Equal(0, privatePlans.Count, "foreign plan selection remains private");
            c.Throws<InvalidOperationException>(() => Plan(game, id).Arguments.Set("target", JsonValue.Number("99")), "stored JSON selection is immutable");
            var loaded = factory.Load(game.Save()); c.Equal(game.Hash(), loaded.Hash(), "pending selected intent restores exactly");
            c.Equal(PlanCodes.Duplicate, View(game).PreviewPlan(paused), "same selected intent duplicate refuses");
            Policy(c, game, "machine.d", false);
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = id, Group = "service.group", Priority = 40, Paused = false }).Accepted, "normal activation retains selected worker");
            Order order = Active(game, id); var execution = order.Node("service");
            c.Equal(Find(game, "machine.d").Id, Node(game, order.Id, "service").RequestedWorker, "private view distinguishes requested identity");
            c.Equal(EntityId.None, Node(game, order.Id, "service").Worker, "disabled requested actor remains unassigned");
            Policy(c, game, "machine.d", true);
            Job job = Board(game).Job(execution.JobId) ?? throw new InvalidOperationException("eligible selected machine did not earn an actual job");
            c.Throws<InvalidOperationException>(() => Board(game).Assign(job, Find(game, "machine.a").Get<IWorker>(), job.Role), "actual claim cannot bypass requested machine");
            c.Throws<InvalidOperationException>(() => Board(game).Assign(job, new WorkWorkerCandidate(Find(game, "machine.a").Id, new WorkerRoleId("inspect"), 0, WorkRequirementResult.Satisfied(), 0, 0), null, null), "retained candidate cannot substitute machine before resource acquisition");
            c.Equal(Fix.FromInt(32), Stock(game), "rejected wrong-machine acquisition consumes no resources");
            c.Equal(ActivityWorkerSelection.Mismatch, game.World.Services.Get<IWorkerDispatch>().CheckJob(game.World, Find(game, "machine.a"), job), "live job checks same exact constraint");
            EntityId savedWorker = job.Worker; job.Worker = Find(game, "machine.a").Id;
            c.Throws<CorruptStateException>(() => game.Hash(), "explicit wrong-assignment fault instrumentation cannot checkpoint"); job.Worker = savedWorker;
            ActivityWorkerSelection valid = order.WorkerSelection; order.WorkerSelection = new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("service", game.World.Entities.NextIdentity) });
            c.Throws<CorruptStateException>(() => game.Hash(), "future selected identity cannot checkpoint"); order.WorkerSelection = valid;
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored actual selection remains durable");
            var wrongRole = New(Factory(edit: root => root.Get("entities")[5].Get("components").Get("worker").Set("roles", Json.Parse("[\"haul\"]"))));
            c.Equal(WorkSystem.BlockerNoWorker, View(wrongRole).PreviewPlan(Intent(wrongRole, paused: true)), "selected machine needs an authored compatible role");
            var queued = New(factory); queued.Seat(0).Submit(new SubmitPlanCommand { Intent = Intent(queued) }); var queuedTwin = factory.Load(queued.Save());
            Step(queued, queuedTwin); c.Equal(queued.Hash(), queuedTwin.Hash(), "pending selected-player command saves and executes identically");
            var canonical = New(factory); int a = Find(canonical, "machine.a").Id.Value, b = Find(canonical, "machine.b").Id.Value, t = Find(canonical, "source").Id.Value;
            JsonValue first = Json.Parse("{\"target\":" + t + ",\"workers\":[{\"node\":\"a\",\"role\":null,\"worker\":" + a + "},{\"node\":\"b\",\"role\":null,\"worker\":" + b + "}]}");
            JsonValue second = Json.Parse("{\"target\":" + t + ",\"workers\":[{\"node\":\"b\",\"role\":null,\"worker\":" + b + "},{\"node\":\"a\",\"role\":null,\"worker\":" + a + "}]}");
            c.Equal(definition.IntentIdentity(game.World, first), definition.IntentIdentity(game.World, second), "selection identity ignores JSON array order");
            Submit(c, canonical, new PlanIntent(definition.PlanKey, first, null, 30, true));
            c.Equal(PlanCodes.Duplicate, View(canonical).PreviewPlan(new PlanIntent(definition.PlanKey, second, null, 30, true)), "reordered bindings cannot duplicate actual intent");
        }
        private static byte[] Encode(ActivityWorkerSelection selection)
        { using var stream = new MemoryStream(); ActivityWorkerSelection.Sync(new StateWriter(stream), selection); return stream.ToArray(); }
        private static byte[] BindingBytes(string first, int worker, string second = null, int other = 0)
        {
            using var stream = new MemoryStream(); var writer = new StateWriter(stream); writer.Count(second == null ? 1 : 2);
            writer.String(ref first); EntityId identity = new EntityId(worker); writer.Entity(ref identity);
            if (second != null) { writer.String(ref second); identity = new EntityId(other); writer.Entity(ref identity); }
            return stream.ToArray();
        }
        private static void Physical(Check c)
        { Service(c, false); Service(c, true); Unavailable(c); Parallel(c, false); Parallel(c, true); }
        private static void Service(Check c, bool extension)
        {
            var factory = Factory(extension); var game = New(factory); var untouched = New(factory); ulong untouchedHash = untouched.Hash();
            string prototype = extension ? "machine.b" : "machine.d"; EntityId selected = Find(game, prototype).Id;
            long id = Submit(c, game, Intent(game, prototype)); Order order = Active(game, id); var node = order.Node("service");
            c.Equal(selected, Board(game).Job(node.JobId).Worker, "bound machine wins despite nearer compatible damaged alternatives");
            c.Equal(Fix.FromInt(4), Condition(game, prototype), "intent does not instantly repair selected worker");
            var snapshot = Node(game, order.Id, "service");
            Reach(game, () => node.Progress.WholeTicks >= 3, "selected machine reaches and earns bay work");
            c.Equal(Fix.One, Find(game, prototype).Get<PointPresence>().Position, "selected actor actually works at facility bay");
            c.Equal(Fix.FromInt(29), Stock(game), "physical selected work consumes real spares");
            var twin = factory.Load(game.Save()); Policy(c, game, prototype, false, twin); WorkProgress progress = node.Progress;
            Step(game, twin); Step(game, twin); c.Equal(progress, node.Progress, "selected actor policy pause earns no work");
            c.Equal(Fix.FromInt(4), Condition(game, prototype), "paused selected service has no effect");
            long coordinate = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); game.Update(0); twin.Update(0);
            c.Equal(coordinate, game.Clock.StepIndex, "one clock owns selected service time"); c.Equal(paused, game.Hash(), "clock pause has no hidden service progression");
            game.Clock.Resume(); twin.Clock.Resume(); Policy(c, game, prototype, true, twin);
            Reach(game, () => Board(game).Job(node.JobId)?.ToilIndex == 2, "selected service awaits departure", twin);
            c.Equal(Fix.FromInt(4), Condition(game, prototype), "full work does not repair before exit");
            twin = factory.Load(game.Save()); Reach(game, () => node.State == NodeState.Done, "physical departure repairs selected actor", twin);
            c.Equal(selected, node.CompletedBy, "earned final actor equals requested actor"); c.Equal(Fix.FromInt(10), Condition(game, prototype), "correct native/public condition owner repaired");
            c.Equal(Fix.FromInt(4), Condition(game, "machine.a"), "nearer unselected machine remains damaged"); c.Equal(Fix.FromInt(4), Condition(game, "source"), "facility is not worker repair subject");
            c.Equal(EntityId.None, snapshot.CompletedBy, "retained view cannot acquire future receipt");
            c.Equal(selected, Node(game, order.Id, "service").RequestedWorker, "node reports same immutable selection after claim release");
            EntityId earned = node.CompletedBy; node.CompletedBy = Find(game, "machine.a").Id;
            c.Throws<CorruptStateException>(() => game.Hash(), "completion receipt from alternate actor cannot checkpoint"); node.CompletedBy = earned;
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual selected completion saves exactly");
            Reach(game, () => Plan(game, id).State == PlanState.Done, "selected ordinary plan settles", twin);
            c.Equal(game.Hash(), twin.Hash(), "native/public selected service saved continuation identical");
            c.Equal(Fix.FromInt(29), Stock(game), "tool release neither refunds spares nor repeats repair"); c.Equal(untouchedHash, untouched.Hash(), "interleaved session has no shared selection/service state");
        }
        private static void Unavailable(Check c)
        {
            var factory = Factory(); var game = New(factory); Policy(c, game, "machine.d", false);
            long id = Submit(c, game, Intent(game)); var order = Active(game, id); var node = order.Node("service");
            for (int i = 0; i < 8; i++) Step(game);
            c.Equal(WorkProgress.Zero, node.Progress, "disabled selected machine waits instead of substituting available damaged worker");
            c.Equal(Fix.FromInt(32), Stock(game), "waiting selection consumes no spares");
            var candidates = new List<WorkWorkerCandidate>(); c.That(View(game).TryCopyWorkerCandidates(order.Id, "service", candidates), "private selected offer read succeeds");
            c.Equal(1, candidates.Count, "no alternative machine offer appears"); c.Equal(Find(game, "machine.d").Id, candidates[0].Worker, "only selected disabled actor offered");
            c.That(candidates[0].Result.Status != WorkRequirementStatus.Satisfied, "disabled actor cannot be acquired");
            var hidden = new List<WorkWorkerCandidate>(); c.That(!View(game, 1).TryCopyWorkerCandidates(order.Id, "service", hidden) && hidden.Count == 0, "foreign selected offers are hidden");
            var twin = factory.Load(game.Save()); Policy(c, game, "machine.d", true, twin);
            Reach(game, () => node.Progress.WholeTicks > 0, "same selected machine resumes", twin);
            c.Equal(Find(game, "machine.d").Id, Board(game).Job(node.JobId).Worker, "reenabled original selected actor resumes real work");
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "ordinary selected service cancellation succeeds");
            c.Equal(Fix.FromInt(4), Condition(game, "machine.d"), "cancelled selected work cannot repair"); c.Equal(game.Hash(), twin.Hash(), "selected cancellation retains spent work/resources identically");
            var removed = New(factory); Policy(c, removed, "machine.d", false); long removedId = Submit(c, removed, Intent(removed));
            Order removedOrder = Active(removed, removedId); Entity actor = Find(removed, "machine.d"); EntityId identity = actor.Id;
            removed.World.Despawn(actor); // Explicit entity-loss instrumentation; no player-side service decision.
            for (int i = 0; i < 8; i++) Step(removed);
            c.Equal(WorkProgress.Zero, removedOrder.Node("service").Progress, "removed selected machine never substitutes another worker");
            c.That(View(removed).TryCopyWorkerCandidates(removedOrder.Id, "service", candidates), "missing selected actor has typed private diagnostics");
            c.Equal(1, candidates.Count, "missing selection publishes no alternative facts"); c.Equal(Codes.NotFound, candidates[0].Result.Blocker, "selected loss is explicit");
            c.Equal(identity, removedOrder.WorkerSelection.WorkerFor("service"), "actual stable missing identity remains selected intent");
            c.Equal(removed.Hash(), factory.Load(removed.Save()).Hash(), "missing selected worker wait saves without fabricated replacement");
            var foreign = New(factory); Policy(c, foreign, "machine.d", false); long foreignId = Submit(c, foreign, Intent(foreign));
            Order foreignOrder = Active(foreign, foreignId); Entity changed = Find(foreign, "machine.d");
            changed.Owner = foreign.Seat(1).Id; // Explicit ownership-loss instrumentation, not host dispatch logic.
            for (int i = 0; i < 5; i++) Step(foreign);
            c.Equal(WorkProgress.Zero, foreignOrder.Node("service").Progress, "ownership loss cannot authorize a substitute or foreign worker");
            c.That(View(foreign).TryCopyWorkerCandidates(foreignOrder.Id, "service", candidates), "ownership-loss selected diagnostics remain private");
            c.Equal(1, candidates.Count, "foreign selected actor reveals no alternative operational facts");
            c.Equal(Codes.NotOwned, candidates[0].Result.Blocker, "changed owner is an explicit wait blocker");
            c.That(!candidates[0].TravelCost.HasValue && candidates[0].Role == WorkerRoleId.None, "foreign selected worker reveals no route or role");
            c.Equal(foreign.Hash(), factory.Load(foreign.Save()).Hash(), "ownership-loss intent saves without changing its original selected identity");
        }
        private static void Parallel(Check c, bool sameWorker)
        {
            var factory = Factory(true); var game = New(factory); int a = Find(game, "machine.a").Id.Value, b = Find(game, sameWorker ? "machine.a" : "machine.b").Id.Value;
            var intent = Raw("selected.parallel", "{\"target\":" + Find(game, "source").Id.Value + ",\"workers\":[{\"node\":\"a\",\"role\":null,\"worker\":" + a + "},{\"node\":\"b\",\"role\":null,\"worker\":" + b + "}]}", false);
            long id = Submit(c, game, intent); Order order = Active(game, id); var left = order.Node("a"); var right = order.Node("b");
            int assigned = 0; foreach (var job in Board(game).Jobs) if (job.OrderId == order.Id && job.Worker.IsValid) assigned++;
            c.Equal(sameWorker ? 1 : 2, assigned, "parallel intent uses only actual worker claims; shared actor serializes");
            Reach(game, () => left.Progress.WholeTicks > 0 || right.Progress.WholeTicks > 0, "parallel selected nodes earn physical work");
            var twin = factory.Load(game.Save());
            Reach(game, () => left.State == NodeState.Done && right.State == NodeState.Done, "parallel selected nodes finish without deadlock", twin);
            c.Equal(new EntityId(a), left.CompletedBy, "first parallel node has its requested final actor"); c.Equal(new EntityId(b), right.CompletedBy, "second parallel node has its requested final actor");
            c.Equal(new EntityId(a), Node(game, order.Id, "a").RequestedWorker, "canonical node view exposes independent first selection");
            c.Equal(Fix.FromInt(32), Stock(game), "parallel held tools release without consumption or duplication");
            Reach(game, () => Plan(game, id).State == PlanState.Done, "parallel ordinary plan settles", twin);
            c.Equal(game.Hash(), twin.Hash(), "distinct/shared actor DAG restores identical physical continuation");
            c.Equal(0, Find(game, "machine.a").Get<IWorker>().Job, "original worker claim released through existing owner");
        }
    }
}
