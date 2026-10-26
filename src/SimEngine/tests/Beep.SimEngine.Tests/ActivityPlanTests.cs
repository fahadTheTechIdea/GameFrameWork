using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class ActivityPlanTests
    {
        public static void Register()
        {
            TestRunner.Add("Persistent plans: contracts, private intent and queue controls", Policy);
            TestRunner.Add("Persistent plans: physical execution and saved continuation", Physical);
        }
        private static GameFactory Factory()
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            var text = new StringBuilder(); root.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), FrameworkProofContent.Read("emergency-preemption.json"), new PlanProofModule(),
                "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"activity-plans.json\"]}",
                FrameworkProofContent.Read("activity-plans.json"));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 701 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add("proof.plans"); request.Mods.Add("sim.incidents"); return factory.NewGame(request);
        }
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var result = new List<PlanView>(); View(game, seat).CopyPlans(result); return result; }
        private static PlanView Plan(SimGame game, long id) => Plans(game).Find(p => p.Id == id);
        private static PlanIntent Intent(SimGame game, string activity = "preempt.work", string target = "source", int priority = 30, bool paused = true)
            => new PlanIntent("activity:" + activity, Content.Json.Parse("{\"target\":" + Find(game, target).Id.Value + "}"), "field.alpha", priority, paused);
        private static long Submit(Check c, SimGame game, PlanIntent intent)
        {
            CommandResult result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent });
            c.That(result.Accepted, "player submits authored persistent intent"); return result.Arg0;
        }
        private static void Edit(Check c, SimGame game, long id, bool paused, int priority = 30, string group = "field.alpha")
            => c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = id, Group = group, Priority = priority, Paused = paused }).Accepted,
                "player configures existing plan through shared check");
        private static void Until(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 240 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " plans=" + string.Join(";", Plans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        internal static void Policy(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); WorkBoard board = Board(game);
            c.That(ReferenceEquals(board, game.World.Services.Get<IPlanBoard>()), "contract aliases sole existing state owner");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "one work codec includes intent links");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "simulation identity bumped"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old save reader");
            c.Throws<ArgumentException>(() => new PlanIntent(null, JsonValue.NewObject(), null, 0, false), "missing definition refuses");
            c.Throws<ArgumentException>(() => new PlanIntent("activity:inspect", JsonValue.Null(), null, 0, false), "non-object arguments refuse");
            c.Throws<ArgumentException>(() => new PlanIntent("activity:inspect", JsonValue.NewObject(), null, -1, false), "negative priority refuses");
            c.That(game.World.Content.Get<ActivityDef>("plan.inspection") is PlannedInspectionDef, "master-listed mod definition aliases actual activity catalog");
            c.That(game.World.Content.Get<ActivityDef>("plan.inspection") is IPlayerActivityDefinition, "mod owns player admission contract");
            ulong hash = game.Hash();
            for (int i = 0; i < 3; i++)
            {
                c.Equal(Code.None, View(game).PreviewPlan(Intent(game)), "pure authored preview ready");
                c.Equal(Codes.NotOwned, View(game, 1).PreviewPlan(Intent(game)), "foreign player uses same ownership refusal");
                c.Equal(Codes.UnknownDef, View(game).PreviewPlan(Intent(game, "missing")), "missing authored graph refuses");
                c.Equal(StandardCodes.NotOrderable, View(game).PreviewPlan(Intent(game, "shared.dry-response")), "system-only activity refuses");
                c.Equal(PlannedInspectionDef.WrongSite, View(game).PreviewPlan(Intent(game, "plan.inspection", "destination")), "mod admission is not bypassed");
            }
            c.Equal(hash, game.Hash(), "previews create no jobs stock claims IDs or hash changes");
            CommandResult bad = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = Intent(game, "plan.inspection", "destination") });
            c.Equal(PlannedInspectionDef.WrongSite, bad.Refusal, "plan command and preview match");
            bad = TestKit.Order(game, 0, new PostOrderCommand { Plan = "plan.inspection", Target = Find(game, "destination").Id });
            c.Equal(PlannedInspectionDef.WrongSite, bad.Refusal, "direct command shares provider admission");
            Fix stock = Find(game, "source").Get<IOutletPort>().Amount(Supply(game));
            long a = Submit(c, game, Intent(game)), b = Submit(c, game, Intent(game, "preempt.travel")), d = Submit(c, game, Intent(game, "plan.inspection"));
            c.That(a > 0 && b > a && d > b, "intent identities stable unique and increasing");
            List<PlanView> detached = Plans(game);
            c.Equal(0, Plans(game, 1).Count, "plan views company private"); c.Equal(0, board.Orders.Count, "paused intents create no physical orders");
            c.Equal(0, board.Jobs.Count, "paused intents create no physical jobs");
            c.Equal(stock, Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "paused intents reserve no stock");
            c.Equal(PlanCodes.Paused, Plan(game, a).Blocker, "engine exposes typed pause blocker");
            c.Equal(PlanCodes.Duplicate, View(game).PreviewPlan(Intent(game)), "duplicate live intent preview refuses");
            c.Equal(PlanCodes.Duplicate, TestKit.Order(game, 0, new SubmitPlanCommand { Intent = Intent(game) }).Refusal, "duplicate command parity");
            c.Equal(PlanCodes.NotEnded, View(game).PreviewPlanRepeat(a), "cannot repeat active intent");
            c.Equal(Codes.NotOwned, View(game, 1).PreviewPlanEdit(a, null, 1, false), "private edit permission");
            c.Equal(Codes.NotOwned, View(game, 1).PreviewPlanCancel(new PlanCancellationSelection(false, new[] { a })).Blocker, "private cancellation permission");
            c.Equal(Codes.NotOwned, View(game, 1).PreviewPlanMove(a, b), "private reorder permission");
            c.Equal(Codes.InvalidArgument, View(game).PreviewPlanEdit(a, null, -1, true), "invalid edit refuses before mutation");
            Edit(c, game, a, true, int.MaxValue, "group.renamed");
            c.Equal(30, detached[0].Priority, "retained view is detached"); c.Equal("field.alpha", detached[0].Group, "retained group not mutable");
            c.Equal(int.MaxValue, Plan(game, a).Priority, "no arbitrary priority ceiling");
            c.Equal("group.renamed", Plan(game, a).Group, "group is saved intent metadata");
            c.That(TestKit.Order(game, 0, new MovePlanCommand { Plan = d, Before = a }).Accepted, "player changes pending order");
            c.Equal(d, Plans(game)[0].Id, "same board owns explicit order");
            c.That(TestKit.Order(game, 0, new MovePlanCommand { Plan = d, Before = 0 }).Accepted, "move to tail");
            c.Equal(d, Plans(game)[2].Id, "tail reorder preserves identities");
            c.That(TestKit.Order(game, 0, new MovePlanCommand { Plan = d, Before = d }).Accepted, "self move is harmless");
            SimGame saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "pending settings ordering and IDs save exactly");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { b }) }).Accepted, "cancel intent before execution");
            c.Equal(PlanState.Cancelled, Plan(game, b).State, "cancelled history remains detached intent");
            c.Equal(0, Plan(game, b).Order, "cancelled pending has no invented execution");
            c.Equal(Codes.AlreadyDone, View(game).PreviewPlanCancel(new PlanCancellationSelection(false, new[] { b })).Blocker, "terminal cancel refuses");
            CommandResult repeated = TestKit.Order(game, 0, new RepeatPlanCommand { Plan = b });
            c.That(repeated.Accepted && repeated.Arg0 > d, "repeat creates fresh intent identity");
            c.Equal(PlanState.Active, Plan(game, repeated.Arg0).State, "explicit repeat re-admits normal physical order");
            // Corrupt intent state must refuse checkpoints, rather than silently repairing it.
            IList entries = (IList)typeof(WorkBoard).GetField("_plans", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
            object first = entries[0]; FieldInfo identity = first.GetType().GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic);
            long prior = (long)identity.GetValue(first); identity.SetValue(first, 0L);
            c.Throws<CorruptStateException>(() => game.Save(), "invalid plan identity refuses save"); identity.SetValue(first, prior);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "valid state remains exact after diagnostic refusal");

            SimGame ranked = New(factory);
            long low = Submit(c, ranked, Intent(ranked, priority: 1)), high = Submit(c, ranked, Intent(ranked, "preempt.travel", priority: 100)),
                moved = Submit(c, ranked, Intent(ranked, "plan.inspection", priority: 100));
            c.That(TestKit.Order(ranked, 0, new MovePlanCommand { Plan = moved, Before = high }).Accepted, "equal priority player ordering changed");
            foreach (long id in new[] { low, high, moved })
                ranked.Seat(0).Submit(new ConfigurePlanCommand { Plan = id, Group = "batch", Priority = id == low ? 1 : 100, Paused = false });
            SimGame inbox = factory.Load(ranked.Save()); Tick(ranked); Tick(inbox);
            c.Equal(ranked.Hash(), inbox.Hash(), "saved pending commands use the same intent codec");
            c.That(Plan(ranked, moved).Order < Plan(ranked, high).Order && Plan(ranked, high).Order < Plan(ranked, low).Order,
                "actual promotion follows explicit priority then moved position");
            c.Equal(0, Find(ranked, "machine").Get<IWorker>().Job, "disabled machine acquires no work");
            Edit(c, ranked, low, true, 1); Configure(c, ranked);
            c.That(Assigned(ranked)?.OrderId != Plan(ranked, low).Order, "paused open activity cannot acquire an enabled worker");
            var queue = new List<WorkDispatchOffer>(); View(ranked).CopyDispatchQueue(queue);
            c.That(queue.Exists(o => o.Order == Plan(ranked, low).Order && o.Blocker == PlanCodes.Paused), "sole dispatcher diagnoses paused open job");

            SimGame waiting = New(factory); long wait = Submit(c, waiting, Intent(waiting, "preempt.travel"));
            CommandResult direct = TestKit.Order(waiting, 0, new PostOrderCommand { Plan = "preempt.travel", Target = Find(waiting, "source").Id });
            c.That(direct.Accepted, "normal direct execution can change pending prerequisites");
            Edit(c, waiting, wait, false);
            c.Equal(PlanState.Pending, Plan(waiting, wait).State, "changed native execution blocks promotion");
            c.Equal(Codes.AlreadyDone, Plan(waiting, wait).Blocker, "pending view returns current admission blocker");
            c.Equal(0, Plan(waiting, wait).Order, "blocked intent allocates no native execution");
            c.That(TestKit.Order(waiting, 0, new CancelOrderCommand { Order = checked((int)direct.Arg0) }).Accepted, "player removes actual blocker");
            c.Equal(PlanState.Active, Plan(waiting, wait).State, "same intent revalidates and promotes when valid");
            c.That(TestKit.Order(waiting, 0, new CancelOrderCommand { Order = Plan(waiting, wait).Order }).Accepted, "direct native cancellation remains authoritative");
            c.Equal(PlanState.Cancelled, Plan(waiting, wait).State, "plan observes native cancellation before pruning");
        }
        internal static void Physical(Check c)
        {
            foreach (string activity in new[] { "preempt.work", "preempt.travel", "plan.inspection" })
            {
                GameFactory factory = Factory(); SimGame game = New(factory); Configure(c, game);
                string target = activity == "preempt.travel" ? "destination" : "source";
                Fix stock = Find(game, "source").Get<IOutletPort>().Amount(Supply(game));
                long id = Submit(c, game, Intent(game, activity, target));
                SimGame pending = factory.Load(game.Save()); c.Equal(game.Hash(), pending.Hash(), "pending stage saves exact " + activity);
                Edit(c, game, id, false); Edit(c, pending, id, false); c.Equal(game.Hash(), pending.Hash(), "identical player unpause commands");
                Until(game, () => Plan(game, id).State == PlanState.Active && Assigned(game) != null, "normal machine admission " + activity, pending);
                PlanView activated = Plan(game, id); Order order = Board(game).Order(activated.Order); Job job = Assigned(game);
                c.That(order != null && order.IsActive && job.OrderId == order.Id, "intent aliases exactly one actual graph execution");
                c.Equal(1, Board(game).Orders.Count, "no second work order");
                c.Equal(PlanCodes.NotPending, View(game).PreviewPlanMove(id, 0), "active reordering cannot change native job age");
                c.That(TestKit.Order(game, 0, new SetOrderPriorityCommand { Order = order.Id, Priority = 901 }).Accepted, "ordinary native priority command accepted");
                c.Equal(901, Plan(game, id).Priority, "plan active priority derives actual native order");
                Edit(c, game, id, true, 901); WorkProgress held = order.Node(job.NodeId).Progress;
                int sameJob = job.Id; long age = job.CreatedTick; Fix available = Find(game, "source").Get<IOutletPort>().Available(Supply(game));
                c.Equal(JobState.Paused, job.State, "same physical job paused"); c.Equal(PlanCodes.Paused, job.Blocker, "actual job typed pause");
                c.That(Find(game, "machine").Get<IMotion>().Status != MoveStatus.Moving, "command stops native motion before movement phase");
                Fix position = ((PointSnapshot)Find(game, "machine").Get<ISpatialPresence>().Capture()).Position;
                c.Equal(PlanCodes.Paused, Plan(game, id).Blocker, "private plan diagnosis matches current rule");
                SimGame twin = factory.Load(game.Save()), untouched = New(factory); ulong isolated = untouched.Hash();
                for (int i = 0; i < 12; i++) { Tick(game); Tick(twin); }
                c.Equal(held, order.Node(job.NodeId).Progress, "pause earns no physical work");
                c.Equal(position, ((PointSnapshot)Find(game, "machine").Get<ISpatialPresence>().Capture()).Position, "paused native worker does not travel");
                c.Equal(sameJob, Assigned(game).Id, "pause retains actual worker binding"); c.Equal(age, job.CreatedTick, "pause preserves dispatch age");
                c.Equal(available, Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "pause preserves native commitments");
                c.Equal(game.Hash(), twin.Hash(), "paused graph/motion/claims save and continue identically");
                Edit(c, game, id, false, 901); Edit(c, twin, id, false, 901);
                Until(game, () => Plan(game, id).State == PlanState.Done, "physical completion " + activity, twin);
                c.Equal(game.Hash(), twin.Hash(), "saved physical completion exact");
                c.Equal(OrderState.Done, order.State, "only actual native completion resolves plan");
                c.Equal(order.Id, Plan(game, id).Order, "history retains original execution identity");
                c.Equal(901, Plan(game, id).Priority, "terminal priority captures native result before pruning");
                c.Equal(stock - (activity == "preempt.work" ? Fix.FromInt(8) : Fix.Zero), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "only actual authored consumption spent");
                c.Equal(Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "completion releases native tool holds");
                c.Equal(isolated, untouched.Hash(), "another session unaffected");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended history saves after native pruning");
                CommandResult repeated = TestKit.Order(game, 0, new RepeatPlanCommand { Plan = id });
                c.That(repeated.Accepted && repeated.Arg0 > id, "completed intent repeats through same admission");
                long newId = repeated.Arg0; Until(game, () => Plan(game, newId).State == PlanState.Active, "repeat native admission");
                c.That(Plan(game, newId).Order != order.Id, "repeat gets fresh native execution not cloned progress");
                c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { newId }) }).Accepted, "active cancellation delegates physical end");
                c.Equal(PlanState.Cancelled, Plan(game, newId).State, "actual cancellation settles intent history");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled execution history save exact");
            }
            GameFactory supplyFactory = Factory(); SimGame supplied = New(supplyFactory);
            long supply = Submit(c, supplied, Intent(supplied, "plan.supplied", "destination", paused: false));
            c.Equal(PlanState.Active, Plan(supplied, supply).State, "automatic supply stays with actual graph execution");
            Edit(c, supplied, supply, true); Configure(c, supplied);
            var offers = new List<LogisticsDispatchOffer>(); supplied.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            c.Equal(0, offers.Count, "pause retires unacquired workflow supply intent");
            c.Equal(0, Board(supplied).Jobs.Count, "pause does not create an unrequested physical haul");
            Edit(c, supplied, supply, false);
            Until(supplied, () => Assigned(supplied)?.IsHaul == true, "native supply dispatch");
            Job haul = Assigned(supplied); ReservationToken inbound = haul.InReservation;
            Until(supplied, () => haul.Loaded.IsPositive, "real source loading"); Fix cargo = haul.Loaded;
            Edit(c, supplied, supply, true);
            c.Equal(inbound, haul.InReservation, "pause retains acquired destination receipt");
            c.That(haul.Loaded >= cargo, "pause never discards in-transit material");
            SimGame supplyTwin = supplyFactory.Load(supplied.Save());
            Until(supplied, () => haul.State == JobState.Done, "paused plan still receives acquired physical cargo", supplyTwin);
            c.Equal(PlanState.Active, Plan(supplied, supply).State, "received cargo alone does not complete paused work");
            c.Equal(Fix.FromInt(8), Find(supplied, "destination").Get<IInletPort>().Amount(Supply(supplied)), "actual shipment arrives exactly once");
            c.Equal(supplied.Hash(), supplyTwin.Hash(), "paused delivery saves with real cargo and original requester");
            Edit(c, supplied, supply, false); Edit(c, supplyTwin, supply, false);
            Until(supplied, () => Plan(supplied, supply).State == PlanState.Done, "supplied timed work", supplyTwin);
            c.Equal(supplied.Hash(), supplyTwin.Hash(), "physical supply then work deterministic");
            c.Equal(Fix.Zero, Find(supplied, "destination").Get<IInletPort>().Amount(Supply(supplied)), "only native work consumes delivered inputs");
        }
    }
}
