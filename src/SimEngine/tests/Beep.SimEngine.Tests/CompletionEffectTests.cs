using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class CompletionEffectTests
    {
        public static void Register()
        {
            TestRunner.Add("Completion effects: typed contracts and private retry state", c => { Contracts(c); UpgradeContracts(c); });
            TestRunner.Add("Completion effects: physical blockers and saved continuation", c => { Physical(c); UpgradePhysical(c); });
            TestRunner.Add("Completion effects: outcome selected replacement keeps earned execution and saved history", OutcomeReplacement);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(bool retry = true, Action<JsonValue> edit = null, Action<JsonValue> proofEdit = null)
        {
            JsonValue policy = Content.Json.Parse(FrameworkProofContent.Read("completion-retry.json"));
            if (!retry) policy.Get("work_retry_profiles")[0].Set("retry_blockers", Content.Json.Parse("[\"blocker.unreachable\",\"proof.completion-rejected\"]"));
            JsonValue proof = Content.Json.Parse(FrameworkProofContent.Read("completion-effects.json")); proofEdit?.Invoke(proof);
            return ConstructionPlanTests.Points(edit, recoveryWork: FrameworkProofContent.Read("point-recovery.json"),
                retryProfiles: Text(policy), additionalModule: new CompletionEffectModule(),
                moduleDeclaration: "{\"id\":\"proof.completion\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"completion-effects.json\"]}",
                moduleDocument: Text(proof));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 719 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); request.Mods.Add("proof.completion");
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype)
        { foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Def.Id == prototype) return e; return null; }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("completion tick=" + game.Tick + " " + game.Fault); }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 720 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready()) throw new InvalidOperationException(stage + " stalled tick=" + game.Tick + " orders=" +
                string.Join(";", new List<Order>(Board(game).Orders).ConvertAll(o => o.PlanKey + "/" + o.State + "/" + o.Blocker + "/" + o.CompletionRetryAt)));
        }
        private static long Submit(Check c, SimGame game, string activity, Entity target)
        {
            CommandResult result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("activity:" + activity,
                Content.Json.Parse("{\"target\":" + target.Id.Value + "}"), "completion", 70, false) });
            c.That(result.Accepted, "player plans " + activity + ": " + result.Refusal); return result.Arg0;
        }
        private static Order Order(SimGame game, EntityId target, string key)
        { foreach (Order o in Board(game).Orders) if (o.Target == target && o.PlanKey == key) return o; return null; }
        private static void Release(SimGame game, EntityId target, ReservationOwner owner, ReservationToken token)
            => game.World.Entities.Get(target).Get<IInventory>().ReleaseIn(owner, token);
        private static List<ActivityNodeView> Nodes(SimGame game, Order order, int seat = 0)
        { var nodes = new List<ActivityNodeView>(); game.Seat(seat).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes); return nodes; }

        private static void Contracts(Check c)
        {
            c.Equal(WorkEffectStatus.Applied, WorkEffectResult.Applied.Status, "success has one typed status");
            c.That(WorkEffectResult.Applied.Blocker.IsNone, "applied has no fabricated blocker");
            c.Throws<ArgumentException>(() => WorkEffectResult.Pending(Code.None), "pending requires an explicit blocker");
            c.Throws<ArgumentException>(() => WorkEffectResult.Failed(Code.None), "failed requires an explicit blocker");
            foreach (string activity in new[] { "proof.node", "proof.order" })
            {
                GameFactory factory = Factory(); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game); Entity target = Find(game, "destination");
                var owner = new ReservationOwner("proof.foreign-completion", 1);
                ReservationToken token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
                long plan = Submit(c, game, activity, target); Order order = Order(game, target.Id, "activity:" + activity);
                Reach(game, () => activity == "proof.node" ? order.Node("work").State == NodeState.Completing : order.State == OrderState.Completing, activity + " pending");
                ActivityNodeExecution node = order.Node("work");
                c.That(node.Progress.Complete(game.SimulationTime.Ticks(order.Plan.Graph.Find("work").Duration)), "pending effect retains earned physical work");
                c.Equal(0, node.JobId, "completed work releases its actual job link");
                c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "machine is not trapped behind a final-step promise");
                c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Supply(game)), "pending effect produces nothing");
                if (activity == "proof.node") c.Equal(NodeState.Waiting, order.Node("next").State, "dependent work waits for actual effect application");
                long deadline = activity == "proof.node" ? node.CompletionRetryAt : order.CompletionRetryAt;
                c.Equal(2L, deadline - (game.Tick - 1), "first wait uses actual scheduling tick and explicit session minutes");
                ulong hash = game.Hash(); var views = Nodes(game, order);
                c.That(views.Count > 0 && Nodes(game, order, 1).Count == 0, "completion facts remain company-private");
                var summaries = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(summaries);
                c.Equal(hash, game.Hash(), "detached completion diagnostics never evaluate or mutate effects");
                if (activity == "proof.node") c.Equal(deadline, views[0].CompletionRetryAt, "node view reports actual deadline");
                else c.Equal(deadline, summaries[0].CompletionRetryAt, "order view reports actual deadline");
                SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending phase and actual foreign promise roundtrip");
                // A codec violation permanently faults that session. Probe isolated native
                // loads instead of repairing private fields and continuing a poisoned world.
                byte[] pending = game.Save();
                void RefuseCorrupt(Action<Order> corrupt, string message)
                {
                    var broken = factory.Load(pending); corrupt(Board(broken).Order(order.Id));
                    c.Throws<CorruptStateException>(() => broken.Hash(), message);
                    c.That(broken.Fault?.Phase == "checkpoint.encode", "invalid completion latches its actual codec failure");
                    c.Throws<InvalidOperationException>(() => broken.Save(), "faulted malformed completion cannot produce a resumable save");
                    c.Throws<InvalidOperationException>(() => broken.Hash(), "faulted malformed completion cannot publish a later healthy hash");
                }
                if (activity == "proof.node")
                {
                    RefuseCorrupt(o => o.Node("work").CompletionRetryAt = checked(game.Tick + 100), "unearned far-future node retry cannot checkpoint");
                    RefuseCorrupt(o => o.Node("work").Progress = WorkProgress.Zero, "pending effect cannot checkpoint invented completed work");
                    RefuseCorrupt(o => o.Node("work").JobId = 99, "completing node cannot own a second live work link");
                }
                else
                {
                    RefuseCorrupt(o => o.CompletionFailures = 0, "pending final effect requires positive failure history");
                    RefuseCorrupt(o => o.Node("work").State = NodeState.Working, "order effect cannot precede completed graph");
                }
                c.Equal(game.Hash(), twin.Hash(), "checkpoint refusal leaves original gameplay/claims unchanged");
                Tick(game); Tick(twin);
                c.Equal(deadline, activity == "proof.node" ? node.CompletionRetryAt : order.CompletionRetryAt, "no same-tick/every-tick retry loop");
                Tick(game); Tick(twin);
                c.Equal(2, activity == "proof.node" ? node.CompletionFailures : order.CompletionFailures, "consecutive refusal counts on native execution");
                c.Equal(4L, (activity == "proof.node" ? node.CompletionRetryAt : order.CompletionRetryAt) - (game.Tick - 1), "second wait uses authored backoff from attempt tick");
                c.Equal(hash == game.Hash(), false, "elapsed authoritative retry state changes identity");
                c.Equal(game.Hash(), twin.Hash(), "interleaved saved retries stay deterministic");
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = plan, Group = "completion", Priority = 70, Paused = true }).Accepted, "player pauses completing plan");
                twin = factory.Load(game.Save()); int failures = activity == "proof.node" ? node.CompletionFailures : order.CompletionFailures;
                for (int i = 0; i < 10; i++) { Tick(game); Tick(twin); }
                c.Equal(failures, activity == "proof.node" ? node.CompletionFailures : order.CompletionFailures, "paused completion performs no attempts past deadline");
                c.Equal(game.Hash(), twin.Hash(), "paused overdue completion remains valid and deterministic");
                Release(game, target.Id, owner, token); Release(twin, target.Id, owner, token);
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = plan, Group = "completion", Priority = 70, Paused = false }).Accepted, "player resumes actual completion");
                TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = plan, Group = "completion", Priority = 70, Paused = false });
                Reach(game, () => order.State == OrderState.Done, "resumed complete graph", twin);
                c.Equal(Fix.One, target.Get<IInventory>().Amount(Supply(game)), "applied effect commits one native unit exactly once");
                c.Equal(0, node.CompletionFailures, "successful node clears retry history");
                c.Equal(0L, order.CompletionRetryAt, "successful order has no dormant retry timer");
                c.Equal(game.Hash(), twin.Hash(), "saved resumed work has identical native effect/history");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished native plan and effect remain durable");
            }
            foreach (bool authorize in new[] { true, false })
            {
                GameFactory factory = Factory(authorize); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game); Entity target = Find(game, "destination");
                if (!authorize) target.Get<IInventory>().ReserveIn(new ReservationOwner("proof.permanent", 1), Supply(game), Fix.One);
                string activity = authorize ? "proof.failed" : "proof.node";
                Submit(c, game, activity, target); Order order = Order(game, target.Id, "activity:" + activity);
                Reach(game, () => !order.IsActive, "terminal/unlisted effect refusal");
                c.Equal(OrderState.Failed, order.State, "explicit Failed or unlisted Pending cannot enter retry");
                c.Equal(authorize ? CompletionEffectDef.Rejected : ResourceTransactions.ActivePromises, order.Blocker, "original typed refusal survives terminal cleanup");
                c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Supply(game)), "failed effect changes no stock");
                c.Equal(0L, order.Node("work").CompletionRetryAt, "failed node owns no retry timer");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal refusal is checkpointable");
            }
            GameFactory faulty = Factory(proofEdit: root => root.Get("completion_effects")[0].Set("mode", JsonValue.Of("mutating_pending")));
            SimGame faulted = New(faulty); EmergencyPreemptionTests.Configure(c, faulted);
            Submit(c, faulted, "proof.node", Find(faulted, "destination"));
            TestKit.RunUntil(faulted, () => faulted.Fault != null, 720, 1);
            c.That(faulted.Fault?.Phase == "work.effect", "mutation on a pending refusal latches the owning effect boundary during ordinary Update");
            c.Throws<InvalidOperationException>(() => faulted.Save(), "faulted partial provider mutation cannot save");
            c.Throws<InvalidOperationException>(() => faulted.Hash(), "faulted partial provider mutation cannot hash");
            PointRecoveryTests.Admission(c);
        }

        private static void Physical(Check c)
        {
            GameFactory factory = Factory(edit: root => root.Get("activities")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("point.surplus-inputs")));
            SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game);
            CommandResult submitted = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower",
                Content.Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}"), "completion", 70, false) });
            c.That(submitted.Accepted, "player starts physically supplied tool-backed construction");
            Entity site = Find(game, "point.tower-site"); Order order = Board(game).Order(site.Get<IConstructionSite>().Order);
            Reach(game, () => !order.Node("build").Progress.Equals(WorkProgress.Zero), "consumed real construction inputs");
            var owner = new ReservationOwner("proof.foreign-completion", 1);
            ReservationToken token = site.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            Reach(game, () => order.State == OrderState.Completing, "temporarily blocked native final construction");
            c.Equal(ResourceTransactions.ActivePromises, order.Blocker, "real final-step promise blocks without failing site");
            c.That(site.Alive && Find(game, "point.tower") == null, "completing site survives without instant fallback");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(Supply(game)), "held tools remain real unconsumed stock");
            c.Equal(1, site.Get<IInventory>().Reservations.Count, "completion releases only its own tool promise");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "completed construction frees physical worker");
            WorkProgress earned = order.Node("build").Progress;
            SimGame twin = factory.Load(game.Save()); for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(earned, order.Node("build").Progress, "retry does not rebuild or consume inputs again");
            c.Equal(game.Hash(), twin.Hash(), "blocked native completion saves all tools and real promise links");
            Release(game, site.Id, owner, token); Release(twin, site.Id, owner, token);
            Reach(game, () => Find(game, "point.tower") != null, "native completion after genuine promise release", twin);
            c.That(!site.Alive && order.State == OrderState.Done, "same native completion owner builds after retry");
            c.Equal(game.Hash(), twin.Hash(), "saved final construction and recovery commit identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native completed site/recovery is durable");
            Cancellation(c); Recovery(c); Replacement(c); PointRecoveryTests.Physical(c);
        }

        private static void Recovery(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game);
            Entity source = Find(game, "source"); var owner = new ReservationOwner("proof.foreign-completion", 1);
            ReservationToken token = source.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "point.recover", Target = source.Id, Priority = 70 }).Accepted, "player posts native timed recovery with a live foreign promise");
            Order order = Order(game, source.Id, "activity:point.recover");
            Reach(game, () => order.Node("recover").State == NodeState.Completing, "actual native recovery effect pending");
            c.Equal(ResourceTransactions.ActivePromises, order.Node("recover").Blocker, "native resource effect classifies actual promise through authored retry");
            c.Equal(Fix.FromInt(20), source.Get<IInventory>().Amount(Supply(game)), "pending native recovery moves no stock");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "native recovery frees the actual completed machine");
            SimGame twin = factory.Load(game.Save()); Release(game, source.Id, owner, token); Release(twin, source.Id, owner, token);
            Reach(game, () => order.State == OrderState.Done, "same native recovery after actual release", twin);
            c.Equal(Fix.Zero, source.Get<IInventory>().Amount(Supply(game)), "retried native recovery transfers exact actual stock");
            Fix total = Fix.Zero; int containers = 0;
            foreach (Entity e in game.World.Entities.All) if (e.Alive)
            { total += e.Get<IInventory>()?.Amount(Supply(game)) ?? Fix.Zero; if (e.Def.Id == "point.recovery-container") containers++; }
            c.Equal(5, containers, "native retried recovery creates five real capacity-four containers");
            c.Equal(Fix.FromInt(20), total, "retried recovery conserves stock, not fake salvage");
            c.Equal(game.Hash(), twin.Hash(), "native node retry/real receipts save and continue identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native recovered stock completion is durable");
        }

        private static void Cancellation(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game); Entity target = Find(game, "destination");
            var owner = new ReservationOwner("proof.foreign-completion", 1); ReservationToken token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            long id = Submit(c, game, "proof.node", target); Order order = Order(game, target.Id, "activity:proof.node");
            Reach(game, () => order.Node("work").State == NodeState.Completing, "cancellable completing node");
            WorkProgress earned = order.Node("work").Progress;
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "player cancels completing plan through native command");
            c.Equal(OrderState.Cancelled, order.State, "pending node becomes truly cancelled");
            c.Equal(earned, order.Node("work").Progress, "cancellation retains earned progress/history");
            c.Equal(0L, order.Node("work").CompletionRetryAt, "cancel clears its native deadline");
            c.Equal(1, target.Get<IInventory>().Reservations.Count, "cancel never clears another owner's promise");
            Release(game, target.Id, owner, token); for (int i = 0; i < 12; i++) Tick(game);
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Supply(game)), "cancelled pending effect never resurrects or produces");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled final-step history saves");
        }

        private static void Replacement(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game);
            Entity target = game.World.Spawn(game.World.Content.Get<EntityDef>("point.tower"), new PlayerId(0), e => ((ISpatialPlacement)game.World.Space).ConfigurePlacement(e, new PointPlacement(game.World.Space.Id, Fix.FromInt(22))));
            Submit(c, game, "proof.replace", target); Order order = Order(game, target.Id, "activity:proof.replace");
            Reach(game, () => order.State == OrderState.Completing, "applied effect with blocked replacement");
            c.That(!order.CompletionEffectApplied && order.Node("work").State == NodeState.Done,
                "native node owns applied production; pending replacement remains the unapplied final effect");
            c.Equal(Fix.One, target.Get<IInventory>().Amount(Supply(game)), "actual applied production is committed once");
            c.Equal(Upgrades.InventoryReserved, order.Blocker, "effect's real claim blocks replacement through ordinary guard");
            SimGame twin = factory.Load(game.Save()); for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(Fix.One, target.Get<IInventory>().Amount(Supply(game)), "retry skips already applied irreversible effect");
            c.Equal(1, target.Get<IInventory>().Reservations.Count, "retry cannot multiply actual claims");
            c.Equal(game.Hash(), twin.Hash(), "saved applied-effect phase retains replacement wait");
            ResourceReservation receipt = target.Get<IInventory>().Reservations[0];
            Release(game, target.Id, receipt.Owner, receipt.Token); Release(twin, target.Id, receipt.Owner, receipt.Token);
            Reach(game, () => order.State == OrderState.Done, "replacement after actual claim release", twin);
            c.That(!target.Alive, "ordinary replacement retires original identity");
            Fix total = Fix.Zero; foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Def.Id == "point.tower") total += e.Get<IInventory>().Amount(Supply(game));
            c.Equal(Fix.One, total, "native replacement transfers actual effect stock without duplication");
            c.Equal(game.Hash(), twin.Hash(), "saved replacement/effect commit matches");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "replacement completion is durable");
        }
    }
}
