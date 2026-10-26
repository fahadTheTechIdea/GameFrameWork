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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class EmergencySupplyTests
    {
        public static void Register()
        {
            TestRunner.Add("Emergency supply: pure dispatch, native blockers and protected carriers", Policy);
            TestRunner.Add("Emergency supply: timed delivery, retained work and saved continuation", Physical);
        }
        private static GameFactory SupplyFactory(string profile = "dispatch.supply", Action<JsonValue> edit = null)
        {
            JsonValue work = Content.Json.Parse(FrameworkProofContent.Read("emergency-preemption.json"));
            for (int i = 0; i < work.Get("activities").Count; i++)
                if (work.Get("activities")[i].Get("id").Text == "preempt.supplied-response")
                    work.Get("activities")[i].Set("automatic_supply", JsonValue.Of(true));
            var text = new StringBuilder(); work.WriteCanonical(text);
            return EmergencyPreemptionTests.Factory(profile, edit: r =>
            {
                r.Get("entities")[1].Get("components").Get("contract_incident").Set("response", JsonValue.Of("preempt.supplied-response"));
                edit?.Invoke(r);
            }, work: text.ToString());
        }
        private static List<LogisticsDispatchOffer> Native(SimGame game)
        { var result = new List<LogisticsDispatchOffer>(); game.World.Services.Get<ILogisticsDispatchQueue>().CopyQueue(new PlayerId(0), result); return result; }
        private static void Policy(Check c)
        {
            GameFactory factory = SupplyFactory(); SimGame game = Start(factory);
            Configure(c, game, interruptible: false); Order ordinary = Post(c, game); Job incumbent = Assigned(game); Order response = Raise(c, game);
            c.Equal(incumbent.Id, Assigned(game).Id, "locked incumbent cannot supply urgent activity");
            c.That(Native(game).Count == 1 && Native(game)[0].Emergency, "actual bound response owns emergency material demand");
            Find(game, "machine").Get<IWorkPolicy>().Configure(game.World, new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All));
            ulong before = game.Hash(); int next = Board(game).NextJobId; var offers = Queue(game); WorkerDispatchOffer retained = offers[0];
            c.That(retained.Ready && retained.Emergency && retained.Scope == LogisticsModule.ModuleId, "combined native emergency supply ready through same selector");
            c.Equal(incumbent.Id, retained.InterruptedJob, "offer names actual ordinary incumbent, not a created haul");
            c.Equal(Find(game, "machine").Id, retained.Worker, "offer uses real carrier relation");
            c.That(Native(game)[0].Haul.Status != HaulStatus.Ready && Native(game)[0].Haul.InterruptedJob == 0, "raw native diagnostics stay explicitly idle-only");
            IHaulExecution hauls = game.World.Services.Get<IHaulExecution>();
            c.That(hauls.PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8)).Status != HaulStatus.Ready,
                "ordinary direct preview does not inherit emergency takeover permission");
            var hidden = new List<WorkerDispatchOffer>(); PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyAssignments(hidden);
            c.Equal(0, hidden.Count, "foreign company learns no carrier/incident/stock takeover facts");
            c.Equal(before, game.Hash(), "all previews preserve actual stock/native holds/jobs/motion");
            c.Equal(next, Board(game).NextJobId, "pure offer allocates no haul ID");
            c.Equal(incumbent.Id, Assigned(game).Id, "retained ready offer alone does not interrupt");
            c.Throws<ArgumentOutOfRangeException>(() => game.World.Services.Get<IWorkerDispatchSource>().CopyOffers(new PlayerId(0), (WorkCandidateMode)9, hidden), "invalid candidate mode refuses");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("sim.logistics", 1, "demand", new PlayerId(0), Find(game, "destination").Id,
                Find(game, "machine").Id, true, Code.None, false, 0, 0, game.Tick, 0, 1, incumbent.Id, true), "ordinary source cannot construct interruption fact");
            Tick(game); c.That(Assigned(game)?.IsHaul == true, "real Update creates actual haul after rechecking interruption");
            c.Equal(JobState.Open, incumbent.State, "same ordinary job reopened");
            c.Equal(incumbent.Id, retained.InterruptedJob, "retained detached offer unchanged");
            foreach (string denial in new[] { "scope", "veto", "locked", "role", "emergency", "stock", "route", "area" })
            {
                Action<JsonValue> edit = denial == "stock" ? r => r.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("10")) :
                    denial == "area" ? r => r.Set("point_interval_regions", Content.Json.Parse("[{\"id\":\"supply.home\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":1}]")) : null;
                SimGame denied = Start(SupplyFactory(denial == "scope" ? "dispatch.preempt" : denial == "veto" ? "dispatch.fair" : "dispatch.supply", edit));
                Configure(c, denied, interruptible: denial != "locked", emergency: denial != "emergency", area: denial == "area" ? "supply.home" : null);
                if (denial == "role") c.That(TestKit.Order(denied, 0, new ConfigureWorkPolicyCommand { Worker = Find(denied, "machine").Id,
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player restricts worker haul role");
                Post(c, denied); Job old = Assigned(denied);
                if (denial == "route") c.That(TestKit.Order(denied, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual supply route");
                Raise(c, denied);
                c.Equal(old.Id, Assigned(denied).Id, "current guard/native blocker refuses supply takeover: " + denial);
                var blocked = Queue(denied); c.That(blocked.Count == 1 && !blocked[0].Ready && blocked[0].InterruptedJob == 0, "truthful unready supply offer: " + denial);
            }
            Idle(c); Ordinary(c); Fault(c);
        }
        private static void Idle(Check c)
        {
            SimGame game = Start(SupplyFactory(edit: r =>
            {
                var text = new StringBuilder(); r.Get("entities")[2].WriteCanonical(text);
                JsonValue spare = Content.Json.Parse(text.ToString()); spare.Set("id", JsonValue.Of("spare")); r.Get("entities").Add(spare);
                StartupContentAuthoring.Add(r, "spare", "spare", 0, spare.Get("components").Get("point_presence"));
            }));
            Configure(c, game); Post(c, game); Job old = Assigned(game);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "spare").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player enables real spare carrier");
            Raise(c, game); c.Equal(old.Id, Assigned(game).Id, "idle supply carrier preferred to interrupting ordinary machine");
            c.That(Board(game).Job(Find(game, "spare").Get<IWorker>().Job)?.IsHaul == true, "real spare acquired native emergency supply");
        }
        private static void Ordinary(Check c)
        {
            SimGame game = Start(SupplyFactory("dispatch.mod-preempt")); Configure(c, game); Post(c, game); Job old = Assigned(game);
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), int.MaxValue, LogisticsReserveAccess.Protected)) }).Accepted, "maximum priority ordinary stock request accepted");
            var offers = Queue(game); c.That(offers.Count == 1 && !offers[0].Ready && !offers[0].Emergency && offers[0].InterruptedJob == 0,
                "unconditional mod profile cannot promote ordinary stock to emergency authority");
            c.Equal(old.Id, Assigned(game).Id, "ordinary stock does not interrupt acquired work");
        }
        private static void Fault(Check c)
        {
            SimGame game = Start(SupplyFactory(edit: WorkerExecutionTestContent.AddJobGate)); Configure(c, game); Post(c, game);
            WorkerExecutionTestContent.Control(game, Find(game, "machine").Id, true, Fix.FromInt(2));
            game.Seat(0).Submit(new RaiseIncidentCommand { Target = Find(game, "destination").Id, Source = new Code("damage"), Kind = new Code("proof.failure") });
            game.Update(); c.That(game.Fault != null, "lost fresh native acquisition after interruption faults normal Update");
            c.Throws<InvalidOperationException>(() => game.Save(), "failed actual supply takeover cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "failed takeover cannot hash as successful native admission");
        }
        private static void Physical(Check c)
        {
            foreach (string profile in new[] { "dispatch.supply", "dispatch.mod-preempt" })
            {
                GameFactory factory = SupplyFactory(profile); SimGame game = Start(factory); Configure(c, game); Order ordinary = Post(c, game);
                Reach(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "ordinary paid activity progress");
                Job old = Assigned(game); long age = old.CreatedTick; WorkProgress earned = ordinary.Node("work").Progress;
                SimGame twin = factory.Load(game.Save()), isolated = Start(factory); ulong untouched = isolated.Hash();
                Order response = Raise(c, game); Raise(c, twin); Job haul = Assigned(game);
                c.That(haul?.IsHaul == true, "current emergency demand creates native physical haul: " + profile);
                c.Equal(JobState.Open, old.State, "original ordinary activity reopened without replacement");
                c.Equal(age, old.CreatedTick, "original age retained");
                c.That(ordinary.Node("work").Progress.WholeTicks >= earned.WholeTicks, "already-earned work preserved");
                WorkProgress retained = ordinary.Node("work").Progress;
                c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "no pickup/refund at takeover");
                c.Equal(Fix.FromInt(8), Find(game, "source").Get<IOutletPort>().ReservedOut(Supply(game)), "only actual new haul protects native supply; old unspent tools released");
                c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "response input not teleported");
                c.Equal(game.Hash(), twin.Hash(), "saved pre-takeover current intent/admission identical");
                Reach(game, () => haul.Loaded.IsPositive, "actual partial timed loading", twin);
                c.That(haul.Loaded < haul.Amount, "first earned native prefix is partial actual cargo");
                c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "partial pickup is not delivered material");
                SimGame saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "partial supply cargo/native commitments save");
                Reach(game, () => response.State == OrderState.Done, "physical delivery then emergency repair", saved);
                c.Equal(game.Hash(), saved.Hash(), "saved partial loading/travel/unloading/repair continuation identical");
                c.Equal(retained, ordinary.Node("work").Progress, "ordinary activity earns nothing during urgent hauling/repair");
                c.Equal(Fix.FromInt(4), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "only startup eight and actual transported eight consumed at source");
                c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "response consumes physically delivered native inputs exactly once");
                c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "actual timed response completion resolves incident");
                Reach(game, () => ordinary.State == OrderState.Done, "original activity resumes after urgent supply/repair", saved);
                c.Equal(game.Hash(), saved.Hash(), "original retained inputs/progress resume identically after save");
                c.Equal(Fix.FromInt(4), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "resume does not pay ordinary startup twice");
                c.Equal(JobState.Done, old.State, "same actual original job completed");
                c.Equal(untouched, isolated.Hash(), "interleaved untouched session isolated");
            }
            ProtectedHaul(c);
            GameFactory activityFactory = EmergencyPreemptionTests.Factory("dispatch.supply");
            SimGame activity = Start(activityFactory); Configure(c, activity); Order original = Post(c, activity);
            Job incumbent = Assigned(activity); Order dry = Raise(c, activity);
            c.That(Assigned(activity)?.OrderId == dry.Id && !Assigned(activity).IsHaul, "generalized interruption protocol still owns direct emergency activity takeover");
            c.Equal(JobState.Open, incumbent.State, "direct activity uses same original reopening path");
            SimGame activityTwin = activityFactory.Load(activity.Save());
            Reach(activity, () => dry.State == OrderState.Done, "direct activity repair after shared protocol migration", activityTwin);
            c.Equal(activity.Hash(), activityTwin.Hash(), "direct activity shared-protocol saved continuation identical");
            Reach(activity, () => original.State == OrderState.Done, "direct original work resumes", activityTwin);
            c.Equal(activity.Hash(), activityTwin.Hash(), "direct interrupted work resumes through same queue/native owner");
        }
        private static void ProtectedHaul(Check c)
        {
            foreach (bool partial in new[] { false, true })
            {
            GameFactory factory = SupplyFactory("dispatch.mod-preempt"); SimGame game = Start(factory); Configure(c, game);
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 1, LogisticsReserveAccess.Protected)) }).Accepted, "ordinary stock request owns real haul");
            Reach(game, () => Assigned(game)?.IsHaul == true, "native standing dispatch"); Job haul = Assigned(game);
            if (partial) Reach(game, () => haul.Loaded.IsPositive, "protected partial loading");
            ReservationToken receipt = haul.InReservation; Fix cargo = haul.Loaded;
            Order response = Raise(c, game);
            c.Equal(haul.Id, Assigned(game).Id, "current incident cannot cancel active native haul even under unconditional profile");
            c.Equal(receipt, haul.InReservation, "acquired destination receipt preserved");
            c.That(haul.Loaded >= cargo, "physical cargo preserved");
            var offers = Queue(game);
            c.That(offers.Count == 1 && !offers[0].Ready && offers[0].InterruptedJob == 0,
                "additional ordinary stock plus response goal cannot interrupt even an unloaded acquired haul");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 1, LogisticsReserveAccess.Protected)) }).Accepted, "player disables extra stock intent, not acquired shipment");
            c.Equal(receipt, haul.InReservation, "intent change leaves acquired native receipt untouched");
            c.That(Queue(game).Count == 0, "current response-only gap accounts for actual inbound claim");
            SimGame saved = factory.Load(game.Save()); Reach(game, () => response.State == OrderState.Done, "protected haul supplies actual response", saved);
            c.Equal(game.Hash(), saved.Hash(), "saved protected hauling/current response continuation identical");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "only acquired physical eight left source");
            }
        }
    }
}
