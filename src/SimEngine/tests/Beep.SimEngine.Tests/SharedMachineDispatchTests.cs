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
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class SharedMachineDispatchTests
    {
        public static void Register()
        {
            TestRunner.Add("Shared machine dispatch: private cross-source ranking and declared JSON providers", Admission);
            TestRunner.Add("Shared machine dispatch: physical contention, incident chain and saved sessions", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.shipments\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"shipments.json\"]}";
        private static GameFactory Factory(bool dry = false, bool two = false, bool shipment = false, bool duplicate = false, World foreign = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("entity_states").Add(Content.Json.Parse("{\"id\":\"stopped\",\"state\":\"point.incident\",\"priority\":70}"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            root.Get("entities")[1].Get("components").Set("contract_incident", Content.Json.Parse("{\"id\":\"damage\",\"kind\":\"proof.failure\",\"response\":\"" + (dry ? "shared.dry-response" : "shared.response") + "\"}"));
            JsonValue near = root.Get("entities")[1].Clone(); near.Set("id", JsonValue.Of("near")); near.Get("components").Remove("contract_incident");
            near.Get("components").Get("point_presence").Set("position", JsonValue.Number("4")); root.Get("entities").Add(near);
            StartupContentAuthoring.Add(root, "near", "near", 0, near.Get("components").Get("point_presence"));
            if (two)
            {
                JsonValue second = root.Get("entities")[2].Clone(); second.Set("id", JsonValue.Of("machine2"));
                second.Get("components").Get("point_presence").Set("position", JsonValue.Number("1")); root.Get("entities").Add(second);
                StartupContentAuthoring.Add(root, "machine2", "machine2", 0, second.Get("components").Get("point_presence"));
            }
            var text = new StringBuilder(); root.WriteCanonical(text);
            string source = FrameworkProofContent.Read("dispatch-source.json");
            if (shipment) source = source.Replace("\"enabled\":false", "\"enabled\":true").Replace("\"priority\":0", "\"priority\":200");
            return PointGame.Factory(text.ToString(), FrameworkProofContent.Read("shared-dispatch.json"),
                new DispatchProofModule(duplicate, foreign), Declaration, source);
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 307 }; request.Mods.Add("proof.shipments"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string definition)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; throw new InvalidOperationException("missing shared-dispatch entity " + definition); }
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static void Enable(SimGame game, string worker = "machine", bool emergency = true)
            => Find(game, worker).Get<IWorkPolicy>().Configure(game.World,
                new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, emergency, WorkAssignmentMode.All));
        private static Order Post(SimGame game, string target = "source", int priority = 100)
            => Board(game).Post(game.World.Content.Get<ActivityDef>("shared.work"), new PlayerId(0), Find(game, target).Id, priority);
        private static void Stock(Check c, SimGame game, int priority, int amount = 8, string target = "destination")
            => c.Equal(Code.None, game.World.Services.Get<ILogisticsStockPolicies>().ConfigureStockPolicy(new PlayerId(0), Find(game, target).Id,
                Supply(game), LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(amount), Fix.FromInt(40), priority, LogisticsReserveAccess.Protected))), "player stock intent uses actual native policy authority");
        private static Order Raise(Check c, SimGame game)
        {
            Entity target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted,
                "registered command raises actual bound incident response");
            return Board(game).ActiveResponseOn(target.Id, new Code("damage"));
        }
        private static List<WorkerDispatchOffer> Queue(SimGame game)
        { var offers = new List<WorkerDispatchOffer>(); game.Seat(0).View.Feature<WorkView>().CopyAssignments(offers); return offers; }
        private static Job Assigned(SimGame game, string worker = "machine") => Board(game).Job(Find(game, worker).Get<IWorker>().Job);
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 360 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.Role + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order ordinary = Post(game, priority: int.MaxValue);
            Tick(game); Order response = Raise(c, game); Enable(game);
            ulong before = game.Hash(); var offers = Queue(game);
            c.Equal(2, offers.Count, "one actual arbitration projection combines open work and required response hauling");
            c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "emergency supply precedes ordinary work regardless of system registration order");
            c.That(offers[0].Emergency && offers[0].Ready && !offers[1].Emergency, "source facts, not numeric priority, distinguish urgent supply");
            c.Equal(1, offers[0].Priority, "actual response's low authored priority remains exact");
            c.Equal(int.MaxValue, offers[1].Priority, "ordinary maximum priority does not overflow or manufacture emergency");
            c.That(offers[1].CreatedTick < offers[0].CreatedTick, "emergency precedes older ordinary work");
            c.Equal(offers[0].Worker, offers[1].Worker, "pure offers may compete for the same unclaimed multi-role machine");
            c.Equal(Find(game, "machine").Id, offers[0].Worker, "common facts name the actual provider-selected worker");
            c.Equal(1, offers[1].AttemptsPerPass, "work job attempts are bounded by original intent identity");
            c.Equal(Tuning.LoadsPerDemandPass, offers[0].AttemptsPerPass, "native logistics workload budget is retained, not a content size ceiling");
            c.Equal(before, game.Hash(), "merged reads do not prepare intent, claim carriers or acquire native receipts");
            var hidden = new List<WorkerDispatchOffer>(offers);
            PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyAssignments(hidden);
            c.Equal(0, hidden.Count, "foreign view clears work and logistics facts together");
            PlayerView.Create(game.World, PlayerId.None).Feature<WorkView>().CopyAssignments(hidden);
            c.Equal(2, hidden.Count, "observer sees detached combined facts");
            c.Throws<InvalidOperationException>(() => game.World.Services.Get<IWorkDispatchQueue>().CopyAssignments(New(factory).World, PlayerId.None, hidden), "queue rejects a different session");
            c.Equal(before, game.Hash(), "foreign source reads do not mutate original session");
            Enable(game, emergency: false); offers = Queue(game);
            c.Equal(WorkModule.ModuleId, offers[0].Scope, "ready ordinary work precedes emergency supply with no eligible machine");
            c.Equal(WorkPolicyCodes.Emergency, offers[1].Blocker, "merged unready supply preserves native emergency eligibility reason");
            c.That(!offers[1].Ready && offers[1].Emergency, "unavailable urgent intent is visible but cannot take a machine");
            Enable(game); Tick(game);
            c.That(game.Tick % 10 != 0, "dirty work wakes shared arbitration between native periodic source visits");
            c.That(Assigned(game)?.IsHaul == true, "actual pass selects emergency supply before older maximum-priority activity");
            c.Equal(new LogisticsDemandId(WorkModule.ModuleId, response.Id, Code.None), Assigned(game).HaulDemand, "real haul retains actual requester rather than merged-offer state");
            c.Equal(JobState.Open, Board(game).Job(ordinary.Node("work").JobId).State, "competing ordinary activity has no duplicate claim");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "winner owns exactly one native destination receipt");
            c.Equal(0, Queue(game).FindAll(x => x.Ready).Count, "new view recomputes busy worker and protected inbound supply");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "current work codec owns partial handling and dock commitments");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "current logistics semantics retain the same payload without a saved dispatch queue");
            c.Equal(40, SaveGame.Describe(game.Save()).Format, "current save format retains existing state owners");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "changed engine has explicit current build identity");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual job/receipt/schedule state restores without arbitration state");
            PriorityAgeCost(c); ProviderAdmission(c);
        }
        private static void PriorityAgeCost(Check c)
        {
            SimGame game = New(Factory()); Order work = Post(game); Stock(c, game, 200); Tick(game); Enable(game);
            var offers = Queue(game); c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "ordinary haul priority can beat ordinary work");
            c.Equal(200, offers[0].Priority, "exact haul priority is not subordinated to work system phase");
            c.Equal(offers[0].CreatedTick, offers[1].CreatedTick, "fixture shares real original intent age");
            Stock(c, game, 100); offers = Queue(game);
            c.Equal(WorkModule.ModuleId, offers[0].Scope, "equal age and priority use actual provider travel cost");
            c.That(offers[0].TravelCost < offers[1].TravelCost, "near worker activity is genuinely cheaper than physical supply");
            Tick(game); c.Equal(work.Id, Assigned(game).OrderId, "actual assignment matches current cross-kind cost projection");

            game = New(Factory()); Stock(c, game, 100); Tick(game); work = Post(game); Tick(game); Enable(game); offers = Queue(game);
            c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "older equal-priority shipment precedes newer cheaper activity");
            c.That(offers[0].AgeTicks > offers[1].AgeTicks && offers[0].TravelCost > offers[1].TravelCost, "ordering uses exact original age before cost");
            Tick(game); c.That(Assigned(game).IsHaul, "native acquisition follows cross-kind age ordering");

            game = New(Factory()); Post(game, "destination"); Stock(c, game, 100); Tick(game); Enable(game); offers = Queue(game);
            c.Equal(offers[0].CreatedTick, offers[1].CreatedTick, "stable tie shares creation tick");
            c.Equal(offers[0].TravelCost, offers[1].TravelCost, "stable tie shares actual route cost");
            c.Equal(offers[0].Target, offers[1].Target, "stable tie shares actual destination");
            c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "final tie uses canonical scoped intent identity");
            c.Equal(offers[0].Scope, Queue(game)[0].Scope, "repeated tie projection is deterministic");

            game = New(Factory()); work = Post(game); Stock(c, game, int.MaxValue); Tick(game); Enable(game); ((PointSpace)game.World.Space).SetAccess(false);
            offers = Queue(game); c.Equal(WorkModule.ModuleId, offers[0].Scope, "unreachable maximum-priority shipment cannot steal ready local work");
            c.That(offers[0].Ready && !offers[1].Ready, "actual route accessibility is included before priority");
            Tick(game); c.Equal(work.Id, Assigned(game).OrderId, "blocked physical offer acquires no carrier");

            game = New(Factory()); Stock(c, game, 200); Enable(game); offers = Queue(game);
            WorkerDispatchOffer retained = offers[0]; IWorkerDispatchSource native = game.World.Services.Get<IWorkerDispatchSource>();
            c.Equal(Code.None, game.World.Services.Get<ILogisticsStockPolicies>().ConfigureStockPolicy(new PlayerId(0), Find(game, "destination").Id,
                Supply(game), LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 200, LogisticsReserveAccess.Protected))), "disable standing intent through its original policy owner");
            ulong before = game.Hash(); c.That(!native.TryAcquire(retained.Identity, retained.Entry, retained.Worker, WorkCandidateMode.IdleOnly), "retained ready offer cannot acquire disabled current demand");
            c.Equal(before, game.Hash(), "stale native refusal creates no jobs or promises");
        }
        private static void ProviderAdmission(Check c)
        {
            GameFactory factory = Factory(shipment: true); SimGame game = New(factory); Post(game); Tick(game); Enable(game);
            var sources = game.World.Services.Providers<IWorkerDispatchSource>();
            c.Equal(2, sources.Count, "declared service aliases expose logistics plus one ordinary public-only module");
            c.That(ReferenceEquals(game.World.Services.Get<IWorkerDispatchSource>(), game.World.Services.Get<LogisticsService>()), "native source aliases actual existing logistics owner");
            var offers = Queue(game); c.Equal("proof.shipments", offers[0].Scope, "new JSON-owned workflow participates without selector changes");
            c.That(offers[0].Identity > int.MaxValue && offers[0].Ready, "open source identity is wide and owns no copied job ID");
            c.Equal(200, offers[0].Priority, "master-listed JSON supplies actual module policy");
            c.Equal(10L, game.World.Services.Get<DispatchProofSource>().GetDispatchSchedule(game.SimulationTime).Period, "module interval resolves against actual session time");
            ulong before = game.Hash(); var current = new List<WorkerDispatchOffer>(); game.World.Services.Get<DispatchProofSource>().CopyOffers(new PlayerId(1), WorkCandidateMode.IdleOnly, current);
            c.Equal(0, current.Count, "external source respects company privacy");
            c.That(!game.World.Services.Get<DispatchProofSource>().TryAcquire(long.MaxValue, "shipment", EntityId.None, WorkCandidateMode.IdleOnly), "unknown current intent cannot acquire from retained facts");
            c.Equal(before, game.Hash(), "refused provider acquisition grants nothing");
            Find(game, "machine").Get<IWorkPolicy>().Configure(game.World,
                new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All));
            before = game.Hash();
            c.That(!game.World.Services.Get<DispatchProofSource>().TryAcquire(offers[0].Identity, offers[0].Entry, offers[0].Worker, WorkCandidateMode.IdleOnly), "retained external offer rechecks current disabled worker admission");
            c.Equal(before, game.Hash(), "stale external refusal does not mutate actual resource/carrier owners");
            Enable(game);
            c.Throws<InvalidOperationException>(() => New(Factory(duplicate: true)), "duplicate scope refuses before engine initialization");
            c.Throws<InvalidOperationException>(() => New(Factory(foreign: game.World)), "source from another session refuses composition");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("bad scope", 1, "job", new PlayerId(0), new EntityId(1), new EntityId(2), true, Code.None, false, 0, 0, 0, 0, 1, 0, true), "noncanonical source identity refuses");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("proof.offer", 1, "job", new PlayerId(0), new EntityId(1), EntityId.None, true, Code.None, false, 0, 0, 0, 0, 1, 0, true), "ready fact without actual offered machine refuses");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("proof.offer", 1, "job", new PlayerId(0), new EntityId(1), new EntityId(2), true, Code.None, false, 0, 1, 0, 0, 1, 0, true), "future intent age refuses");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("proof.offer", 1, "job", new PlayerId(0), new EntityId(1), new EntityId(2), true, Code.None, false, 0, 0, 0, -1, 1, 0, true), "negative route cost refuses");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer("proof.offer", 1, "job", new PlayerId(0), new EntityId(1), new EntityId(2), true, Code.None, false, 0, 0, 0, 0, 0, 0, true), "zero attempt envelope refuses");
            Tick(game); c.That(Assigned(game)?.IsHaul == true, "external intent acquires through normal native physical owner");
            c.That(!Assigned(game).HaulDemand.IsValid, "fixed module shipment does not fabricate native demand provenance");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory), isolated = New(factory); Order ordinary = Post(game, priority: int.MaxValue);
            Tick(game); Order response = Raise(c, game); Enable(game); Tick(game); Job haul = Assigned(game);
            c.That(haul?.IsHaul == true, "one multi-role machine first supplies actual incident, not unrelated high-priority activity");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "arbitration grants no instant arrival");
            Reach(game, () => haul.Loaded.IsPositive, "emergency pickup");
            c.Equal(Fix.FromInt(8), haul.Loaded, "physical loading protects real native cargo");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "actual pickup removes source stock once");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save while loaded retains actual machine/receipt/job state");
            Reach(game, () => response.Node("repair").JobId != 0 && Board(game).Job(response.Node("repair").JobId)?.Worker.IsValid == true,
                "same worker switches from delivered cargo to emergency repair", twin, isolated);
            c.Equal(game.Hash(), twin.Hash(), "saved merged contention continues identically through delivery and repair assignment");
            c.Equal(response.Id, Assigned(game).OrderId, "bound emergency repair wins over waiting maximum ordinary priority");
            c.That(!Assigned(game).IsHaul && Assigned(game).Role == new WorkerRoleId("inspect"), "actual carrier becomes ordinary role-compatible repair worker");
            c.Equal(JobState.Open, Board(game).Job(ordinary.Node("work").JobId).State, "ordinary work remains unclaimed until current emergency is done");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "delivered inputs exist before timed repair consumption");
            c.Equal(Fix.Zero, Find(game, "machine").Get<IInventory>().Amount(Supply(game)), "delivered cargo is not duplicated in truck");
            Reach(game, () => response.State == OrderState.Done, "timed response completion", twin, isolated);
            c.Equal(game.Hash(), twin.Hash(), "saved response spending/completion and new work assignment are deterministic");
            c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "only real positive-duration response clears incident");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "repair consumes supplied quantity exactly once");
            Reach(game, () => ordinary.State == OrderState.Done, "ordinary work eventually follows repaired incident", twin, isolated);
            c.Equal(game.Hash(), twin.Hash(), "normal activity after incident continues through the same board");
            c.Equal(Fix.FromInt(20), Find(isolated, "source").Get<IOutletPort>().Amount(Supply(isolated)), "interleaved session borrows no stock or jobs");
            c.Equal(0, Board(isolated).Orders.Count, "interleaved session borrows no incident intent");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "normal activity manufactures no response input refund");
            EmergencyWork(c); RecomputedContention(c); ExternalShipment(c); RetiredRequester(c);
        }
        private static void EmergencyWork(Check c)
        {
            GameFactory factory = Factory(dry: true); SimGame game = New(factory); Stock(c, game, int.MaxValue); Order response = Raise(c, game); Enable(game);
            var offers = Queue(game); c.That(offers[0].Scope == WorkModule.ModuleId && offers[0].Emergency, "actual emergency activity beats ordinary hauling across source boundaries");
            Tick(game); c.Equal(response.Id, Assigned(game).OrderId, "actual response owns sole machine before ordinary stock refill");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => response.State == OrderState.Done, "dry response", twin);
            c.Equal(game.Hash(), twin.Hash(), "dry emergency work transitions deterministically to queued haul");
            Reach(game, () => Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(8), "normal stock follows emergency", twin);
            c.Equal(game.Hash(), twin.Hash(), "ordinary replenishment physically resumes after priority pressure");
        }
        private static void RecomputedContention(Check c)
        {
            GameFactory factory = Factory(two: true); SimGame game = New(factory); Order order = Post(game, priority: 200); Stock(c, game, 100, 16);
            Tick(game); Enable(game); Enable(game, "machine2"); var offers = Queue(game);
            c.Equal(offers[0].Worker, offers[1].Worker, "initial detached cross-kind offers compete for preferred cheapest machine");
            Tick(game); Job first = Assigned(game), second = Assigned(game, "machine2");
            c.Equal(order.Id, first.OrderId, "higher-priority activity claims initial preferred worker");
            c.That(second?.IsHaul == true && second.Worker != first.Worker, "real acquisition rebuild selects remaining native carrier");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "one remaining carrier reserves only its actual capacity");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "partial refill is not fabricated completed stock");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "cross-kind ownership survives checkpoint");
            Reach(game, () => order.State == OrderState.Done && Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(16), "two-machine repeated physical refill", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved recomputation and subsequent same-intent loads remain identical");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "two loads transfer exactly sixteen source units");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "completed loads release their actual native inbound receipts");
        }
        private static void ExternalShipment(Check c)
        {
            GameFactory factory = Factory(shipment: true); SimGame game = New(factory); Order ordinary = Post(game); Tick(game); Enable(game); Tick(game);
            Job haul = Assigned(game); c.That(haul?.IsHaul == true, "master-listed external source wins using actual common arbitration");
            Reach(game, () => haul.Loaded.IsPositive, "module shipment pickup"); SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "stateless source requires no extra intent/priority codec");
            Reach(game, () => ordinary.State == OrderState.Done, "module delivery then actual ordinary work", twin);
            c.Equal(game.Hash(), twin.Hash(), "module-authored transfer uses unchanged physical saved execution");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "external provider sees actual completed delivery");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "external intent causes no duplicated/created input");
            c.Equal(0, Queue(game).Count, "satisfied source vanishes from derived queue without another ledger");
        }
        private static void RetiredRequester(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order ordinary = Post(game, priority: int.MaxValue);
            Tick(game); Order response = Raise(c, game); Enable(game); Tick(game); Job haul = Assigned(game);
            Reach(game, () => haul.Loaded.IsPositive, "retiring requester pickup"); LogisticsDemandId source = haul.HaulDemand;
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = response.Id }).Accepted, "player retires response through actual cancellation owner");
            c.That(Assigned(game)?.Id == haul.Id && haul.Loaded == Fix.FromInt(8), "retired intent does not free claimed carrier or discard loaded cargo");
            c.Equal(source, haul.HaulDemand, "original retired requester remains actual physical provenance");
            c.That(!Board(game).IsEmergency(haul), "retired source removes urgency without a copied queue flag");
            c.Equal(JobState.Open, Board(game).Job(ordinary.Node("work").JobId).State, "ordinary queued activity cannot commandeer retired but acquired shipment");
            Order renewed = Board(game).ActiveResponseOn(Find(game, "destination").Id, new Code("damage"));
            c.That(renewed != null && renewed.Id != response.Id, "active incident posts new work; cancelling execution does not fabricate incident resolution");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "replacement work reuses original real inbound commitment");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "retired loaded commitment saves against complete actual source state");
            Reach(game, () => ordinary.State == OrderState.Done, "retired shipment delivers before waiting activity", twin);
            c.Equal(game.Hash(), twin.Hash(), "retired delivery and subsequent ordinary assignment stay deterministic");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "real replacement repair consumes original delivered inputs once");
            c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "only actual replacement repair resolves incident, not cancellation");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "retirement never manufactures a second pickup or stock refund");
        }
    }
}
