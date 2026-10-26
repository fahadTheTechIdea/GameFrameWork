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
    public static class EmergencyHaulTests
    {
        public static void Register()
        {
            TestRunner.Add("Emergency hauls: current source facts, private priority and eligible native offers", Admission);
            TestRunner.Add("Emergency hauls: physical incident supply, paused cargo and retired saved provenance", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.demands\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"demands.json\"]}";
        internal static GameFactory Factory(bool ordinaryCarrier = false, bool emergency = true, Action<JsonValue> edit = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("entity_states").Add(Content.Json.Parse("{\"id\":\"stopped\",\"state\":\"point.incident\",\"priority\":70}"));
            JsonValue machine = root.Get("entities")[2];
            machine.Get("components").Get("worker").Set("roles", Content.Json.Parse("[\"haul\"]"));
            machine.Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"emergency.carrier\"}"));
            JsonValue near = root.Get("entities")[1].Clone(); near.Set("id", JsonValue.Of("near"));
            near.Get("components").Get("point_presence").Set("position", JsonValue.Number("4")); root.Get("entities").Add(near);
            StartupContentAuthoring.Add(root, "near", "near", 0, near.Get("components").Get("point_presence"));
            JsonValue responder = machine.Clone(); responder.Set("id", JsonValue.Of("responder"));
            responder.Get("components").Remove("storage"); responder.Get("components").Remove("transporter");
            responder.Get("components").Get("worker").Set("roles", Content.Json.Parse("[\"inspect\"]"));
            responder.Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"emergency.responder\"}")); root.Get("entities").Add(responder);
            StartupContentAuthoring.Add(root, "responder", "responder", 0, responder.Get("components").Get("point_presence"));
            if (ordinaryCarrier)
            {
                JsonValue ordinary = machine.Clone(); ordinary.Set("id", JsonValue.Of("ordinary"));
                ordinary.Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"emergency.ordinary\"}")); root.Get("entities").Add(ordinary);
                StartupContentAuthoring.Add(root, "ordinary", "ordinary", 0, ordinary.Get("components").Get("point_presence"));
            }
            root.Get("entities")[1].Get("components").Set("contract_incident", Content.Json.Parse("{\"id\":\"damage\",\"kind\":\"proof.failure\",\"response\":\"emergency.response\"}"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            string demand = FrameworkProofContent.Read("logistics-demands.json").Replace("\"emergency\": false", "\"emergency\": " + (emergency ? "true" : "false"));
            return PointGame.Factory(text.ToString(), FrameworkProofContent.Read("emergency-hauls.json"), new DemandProofModule(incidents: true), Declaration, demand);
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 293 }; request.Mods.Add("proof.demands"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing emergency entity " + id); }
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static List<LogisticsDispatchOffer> Queue(SimGame game)
        { var result = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(result); return result; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var result = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(result); return result; }
        private static Job Haul(SimGame game)
        { foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) if (job.IsLive && job.IsHaul && job.To == Find(game, "destination").Id) return job; return null; }
        private static void Policy(SimGame game, string worker, bool enabled, bool emergency)
        {
            IWorkPolicy policy = Find(game, worker).Get<IWorkPolicy>(); WorkPolicySettings current = policy.ReadPolicy();
            policy.Configure(game.World, new WorkPolicySettings(enabled, current.Priority, current.AllowedRoles, current.Area, current.Interruptible, emergency, WorkAssignmentMode.All));
        }
        private static DemandProofDef Submit(Check c, SimGame game)
        {
            DemandProofDef def = game.World.Services.Get<DemandProofOwner>().Definition;
            c.Equal(Code.None, game.World.Services.Get<ILogisticsDemandRequests>().ReplaceRequests(def.Source, def.Company, Find(game, "destination").Id, new[] { LogisticsSupplyRequest.Entity(Find(game, "destination").Id, def.Quantities) }, def.Priority), "authored mod submits through same native request owner");
            return def;
        }
        private static void Normal(Check c, SimGame game, bool sameStore = false)
            => c.Equal(Code.None, game.World.Services.Get<ILogisticsStockPolicies>().ConfigureStockPolicy(new PlayerId(0), Find(game, sameStore ? "destination" : "near").Id,
                Supply(game), LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), int.MaxValue, LogisticsReserveAccess.Protected))), "ordinary player intent has maximum representable priority");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 320 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(ordinaryCarrier: true); SimGame game = New(factory); DemandProofDef def = Submit(c, game); Normal(c, game);
            ILogisticsDemandSources sources = game.World.Services.Get<ILogisticsDemandSources>();
            c.That(ReferenceEquals(sources, game.World.Services.Get<LogisticsService>()), "source inspection aliases existing coordinator, not another owner registry");
            c.That(sources.InspectSource(def.Source, def.Company, Find(game, "destination").Id).Emergency, "JSON mod explicitly owns active emergency classification");
            c.That(!sources.InspectSource(new LogisticsDemandId(LogisticsService.StockDemandScope, Find(game, "near").Id.Value, Code.None), new PlayerId(0), Find(game, "near").Id).Emergency, "maximum-priority stock is explicitly ordinary");
            c.Equal(LogisticsDemandStatus.Invalid, sources.InspectSource(def.Source, new PlayerId(1), Find(game, "destination").Id).Status, "foreign source binding refuses");
            c.Equal(LogisticsDemandCodes.UnknownOwner, sources.InspectSource(new LogisticsDemandId("missing.owner", 1, Code.None), new PlayerId(0), Find(game, "destination").Id).Refusal, "unknown classification has no emergency fallback");
            c.That(!new LogisticsDemandSourceCheck(LogisticsDemandStatus.Expired, LogisticsDemandCodes.Expired, true).IsWellFormed, "expired source cannot claim emergency classification");
            c.That(!new LogisticsDemandSourceCheck(LogisticsDemandStatus.Active, Codes.NotFound, true).IsWellFormed, "active facts cannot mix emergency and refusal");
            Policy(game, "machine", true, true); Policy(game, "ordinary", true, false);
            ulong before = game.Hash(); var queue = Queue(game);
            c.That(queue[0].Emergency && queue[0].Source.Equals(def.Source), "ready emergency precedes maximum ordinary priority");
            c.Equal(Find(game, "machine").Id, queue[0].Haul.Carrier, "emergency uses actual eligible carrier despite lower machine preference");
            c.Equal(Find(game, "ordinary").Id, queue[1].Haul.Carrier, "ordinary supply may use emergency-ineligible preferred machine");
            c.That(!queue[1].Emergency, "ordinary queue remains ordinary, not an inferred emergency");
            c.Equal(before, game.Hash(), "source facts/queue create no urgency state, promises or jobs");
            var hidden = new List<LogisticsDispatchOffer>(queue);
            PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().CopyQueue(hidden);
            c.Equal(0, hidden.Count, "foreign emergency operational facts stay private");
            Policy(game, "machine", true, false); queue = Queue(game);
            c.That(!queue[0].Emergency && queue[0].Haul.Status == HaulStatus.Ready, "ready ordinary work ranks before physically unavailable emergency offer");
            c.Equal(WorkPolicyCodes.Emergency, queue[1].Haul.Blocker, "emergency wait identifies shared eligibility blocker");
            c.Equal(0, game.World.Services.Get<WorkBoard>().Jobs.Count, "blocked emergency is not a reservation/job");
            Policy(game, "machine", true, true); Tick(game); Job haul = Haul(game);
            c.That(haul != null && haul.HaulDemand.Equals(def.Source), "real haul persists requester identity through sole job authority");
            c.That(Jobs(game).Find(x => x.Id == haul.Id).Emergency, "private live job computes current emergency through same owner");
            var privateJobs = Jobs(game); PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyJobs(privateJobs);
            c.Equal(0, privateJobs.Count, "foreign job view hides requester linkage and urgency");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "requester identity uses current work codec only");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "current logistics semantics retain the sole intent table, not another emergency balance");
            c.Equal(32, SaveGame.Describe(game.Save()).Format, "current work provenance breaks save layout without reader");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "requester and derived urgency restore after all providers load");
            LogisticsDemandId retainedSource = haul.HaulDemand;
            haul.HaulDemand = new LogisticsDemandId("missing.owner", 1, Code.None);
            c.Throws<CorruptStateException>(() => game.Save(), "undeclared requester cannot be checkpointed");
            haul.HaulDemand = new LogisticsDemandId(def.Source.Scope, def.Source.Identity + 1, Code.None);
            c.Throws<CorruptStateException>(() => game.Save(), "declared scope cannot fabricate workflow identity on a haul");
            haul.HaulDemand = retainedSource;
            SameStoreAndRetirement(c); IncidentFacts(c);
        }
        private static void SameStoreAndRetirement(Check c)
        {
            SimGame game = New(Factory()); DemandProofDef def = Submit(c, game); Normal(c, game, true); Policy(game, "machine", true, true);
            var offer = Queue(game)[0];
            c.Equal(def.Source, offer.Source, "emergency contributor wins over maximum-priority ordinary source on same native store");
            c.Equal(65, offer.Priority, "emergency group's priority belongs to actual emergency requester, not copied ordinary maximum");
            c.Equal(Fix.FromInt(8), offer.Requested, "emergency offer excludes unrelated ordinary target from privileged allocation");
            game.World.Services.Get<IDemandProof>().Retire();
            offer = Queue(game)[0]; c.That(!offer.Emergency, "expired contributor immediately loses urgency without view-time pruning");
            c.Equal(int.MaxValue, offer.Priority, "remaining ordinary contributor resumes its own priority");
            c.Equal(Fix.FromInt(8), offer.Requested, "retired quantities are absent from future physical gap");
            SimGame ordinary = New(Factory(emergency: false)); Submit(c, ordinary); Policy(ordinary, "machine", true, false);
            c.That(!Queue(ordinary)[0].Emergency && Queue(ordinary)[0].Haul.Status == HaulStatus.Ready, "same JSON mod contract supports explicit ordinary classification");
        }
        private static void IncidentFacts(Check c)
        {
            SimGame game = New(Factory()); Entity target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "player causes real engine-side incident through registered command");
            Order order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            var source = new LogisticsDemandId(WorkModule.ModuleId, order.Id, Code.None); var sources = game.World.Services.Get<ILogisticsDemandSources>();
            c.That(sources.InspectSource(source, target.Owner, target.Id).Emergency, "bound current response episode classifies its supply as emergency");
            Normal(c, game); Policy(game, "machine", true, true);
            var queue = Queue(game); c.That(queue[0].Emergency && queue[0].Source.Equals(source), "real low-priority incident supply precedes ordinary maximum priority");
            c.Equal(1, queue[0].Priority, "incident classification is distinct from its authored low priority");
            target.Get<ContractIncident>().Renew(game.World);
            var stale = sources.InspectSource(source, target.Owner, target.Id);
            c.Equal(LogisticsDemandStatus.Expired, stale.Status, "renewed episode expires old response workflow binding immediately");
            c.That(!stale.Emergency, "stale episode cannot retain emergency urgency");
            c.That(!Queue(game).Exists(x => x.Source.Equals(source)), "stale supply intent is not an emergency offer");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); DemandProofDef def = Submit(c, game); Normal(c, game); Policy(game, "machine", true, true);
            Tick(game); Job haul = Haul(game);
            c.That(haul != null && haul.HaulDemand.Equals(def.Source), "eligible emergency owns the single real carrier before ordinary high priority");
            c.Equal(Fix.Zero, Find(game, "near").Get<IInletPort>().ReservedIn(Supply(game)), "ordinary high priority cannot pre-book emergency's truck");
            Reach(game, () => haul.Loaded.IsPositive, "actual emergency pickup");
            c.Equal(Fix.FromInt(8), haul.Loaded, "physical pickup transfers actual authored native stock");
            var token = haul.CargoOutReservation; var inbound = haul.InReservation; int toil = haul.ToilIndex;
            Policy(game, "machine", true, false);
            c.Equal(JobState.Paused, haul.State, "emergency eligibility edit pauses existing physical haul");
            c.Equal(WorkPolicyCodes.Emergency, haul.Blocker, "paused live job reports same typed eligibility blocker");
            c.Equal(token, haul.CargoOutReservation, "eligibility pause preserves real cargo receipt");
            c.Equal(inbound, haul.InReservation, "eligibility pause preserves destination promise");
            SimGame twin = factory.Load(game.Save()), isolated = New(factory);
            c.Equal(game.Hash(), twin.Hash(), "paused loaded requester provenance and eligibility restore identically");
            for (int i = 0; i < 12; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(toil, haul.ToilIndex, "ineligible emergency does not progress physical itinerary");
            c.Equal(Fix.FromInt(8), Find(game, "machine").Get<ITransporter>().Cargo.Amount(Supply(game)), "paused cargo never teleports to requester");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "destination remains physically empty while halted");
            c.Equal(game.Hash(), twin.Hash(), "saved paused twins remain identical during real game-time waits");
            Policy(game, "machine", true, true); Policy(twin, "machine", true, true);
            Reach(game, () => Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(8), "eligible resumed emergency delivery", twin, isolated);
            c.Equal(game.Hash(), twin.Hash(), "saved physical resume continues without duplicate material");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInletPort>().Amount(Supply(isolated)), "independent session receives no borrowed emergency state or cargo");
            RetiredLoaded(c); IncidentSupply(c); ReplacedEpisode(c);
        }
        private static void RetiredLoaded(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); DemandProofDef def = Submit(c, game); Policy(game, "machine", true, true); Tick(game);
            Job haul = Haul(game); Reach(game, () => haul.Loaded.IsPositive, "retired emergency pickup"); Policy(game, "machine", true, false);
            c.That(Jobs(game).Find(x => x.Id == haul.Id).Emergency, "live paused urgency is derived before retirement");
            game.World.Services.Get<IDemandProof>().Retire(); game.World.Services.Get<ILogisticsDemandRequests>().WithdrawRequests(def.Source);
            c.That(!Jobs(game).Find(x => x.Id == haul.Id).Emergency && haul.HaulDemand.Equals(def.Source), "retirement drops urgency, not original physical provenance");
            c.That(haul.CargoOutReservation.IsValid && haul.InReservation.IsValid, "retirement retains loaded commitments");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(8), "retired ordinary cargo continuation", twin);
            c.Equal(game.Hash(), twin.Hash(), "expired source restores and delivers without stuck emergency eligibility");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "retirement did not manufacture/refund already picked-up source stock");
        }
        private static void IncidentSupply(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "real incident is raised through engine command");
            Order response = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            Normal(c, game); Policy(game, "machine", true, true);
            Reach(game, () => Haul(game) != null, "real incident haul assignment"); Job haul = Haul(game);
            c.Equal(new LogisticsDemandId(WorkModule.ModuleId, response.Id, Code.None), haul.HaulDemand, "physical incident supply retains the actual response workflow identity");
            c.That(Jobs(game).Find(x => x.Id == haul.Id).Emergency, "real incident supply uses current bound response, not a fake mod flag");
            Reach(game, () => haul.Loaded.IsPositive, "real incident physical pickup"); SimGame twin = factory.Load(game.Save());
            Reach(game, () => target.Get<ContractIncident>().Completed == 1, "machine response consumes delivered supplies over authored time", twin);
            c.Equal(game.Hash(), twin.Hash(), "real incident's loaded supply, timed response and completion survive saved continuation");
            c.That(!target.Get<ContractIncident>().ReadIncident().Active, "physical response clears its actual bound incident");
            c.Equal(Fix.Zero, target.Get<IInletPort>().Amount(Supply(game)), "authored response consumes actual eight delivered units once");
            c.That(response.State == OrderState.Done, "ordinary work DAG owns completed incident response");
        }
        private static void ReplacedEpisode(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "episode replacement begins with actual incident");
            Policy(game, "machine", true, true); Reach(game, () => Haul(game)?.Loaded.IsPositive == true, "old episode physical pickup"); Job haul = Haul(game);
            LogisticsDemandId old = haul.HaulDemand; target.Get<ContractIncident>().Renew(game.World); Policy(game, "machine", true, false);
            c.That(!Jobs(game).Find(x => x.Id == haul.Id).Emergency, "renewed episode retires old shipment's urgency before new posting phase");
            Tick(game); Order current = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            c.That(current != null && current.Id != old.Identity, "normal incident phase posts a new ordinary authoritative response identity");
            c.Equal(old, haul.HaulDemand, "episode replacement never rewrites already acquired shipment history");
            c.That(haul.IsLive && haul.CargoOutReservation.IsValid && haul.InReservation.IsValid, "new episode retains old physical commitments instead of acquiring duplicate incoming stock");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => target.Get<ContractIncident>().Completed == 1, "new response uses existing incoming cargo", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved replacement episode uses existing physical delivery without double consumption");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "new episode never double-books replacement supply");
        }
    }
}
