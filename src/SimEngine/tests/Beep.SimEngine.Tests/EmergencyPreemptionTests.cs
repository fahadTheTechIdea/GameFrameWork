using System;
using System.Collections.Generic;
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
    public static class EmergencyPreemptionTests
    {
        public static void Register()
        {
            TestRunner.Add("Emergency preemption: strict policy, pure candidates and protected execution", Policy);
            TestRunner.Add("Emergency preemption: physical interruption, retained inputs and saved continuation", Physical);
        }
        internal static GameFactory Factory(string profile = "dispatch.preempt", string profiles = null,
            Action<JsonValue> edit = null, string work = null, bool selected = true)
            => DispatchAgingTests.Factory(profiles, profile, edit: root => { WorkerExecutionTestContent.Add(root); edit?.Invoke(root); }, selected: selected,
                workDefinitions: work ?? FrameworkProofContent.Read("emergency-preemption.json"));
        internal static SimGame Start(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments"); return factory.NewGame(request); }
        internal static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        internal static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        internal static Job Assigned(SimGame game) => Board(game).Job(Find(game, "machine").Get<IWorker>().Job);
        internal static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        internal static List<WorkerDispatchOffer> Queue(SimGame game)
        { var offers = new List<WorkerDispatchOffer>(); game.Seat(0).View.Feature<WorkView>().CopyAssignments(offers); return offers; }
        internal static void Configure(Check c, SimGame game, bool enabled = true, bool interruptible = true, bool emergency = true, string area = null, bool haulOnly = false)
            => c.That(TestKit.Order(game, 0, ConfigureCommand(Find(game, "machine").Id, enabled, interruptible, emergency, area, haulOnly)).Accepted, "player configures actual policy");
        internal static ConfigureWorkPolicyCommand ConfigureCommand(EntityId worker, bool enabled = true, bool interruptible = true, bool emergency = true, string area = null, bool haulOnly = false)
            => new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(enabled, 0, haulOnly ? new[] { new WorkerRoleId("haul") } :
                    new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, area, interruptible, emergency, WorkAssignmentMode.All) };
        internal static Order Post(Check c, SimGame game, string plan = "preempt.work", string target = "source")
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Plan = plan, Target = Find(game, target).Id, Priority = int.MaxValue });
            c.That(result.Accepted, "player posts ordinary physical work"); return Board(game).Order(checked((int)result.Arg0));
        }
        internal static Order Raise(Check c, SimGame game)
        {
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = Find(game, "destination").Id,
                Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "player raises current bound incident");
            Order response = Board(game).ActiveResponseOn(Find(game, "destination").Id, new Code("damage"));
            c.That(response != null, "engine creates ordinary authored response activity"); return response;
        }
        internal static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 240 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " job=" + Assigned(game)?.Id +
                " state=" + Assigned(game)?.State + " blocker=" + Assigned(game)?.Blocker + " cargo=" + Find(game, "machine").Get<ITransporter>().Cargo.Amount(Supply(game)));
        }
        private static void Policy(Check c)
        {
            string authored = FrameworkProofContent.Read("dispatch-policy.json");
            c.Throws<ContentException>(() => Start(Factory(profiles: authored.Replace("\"preempt_emergencies\":true,", ""))), "missing policy boolean refuses rather than defaults");
            c.Throws<ContentException>(() => Start(Factory(profiles: authored.Replace("\"preempt_emergencies\":true", "\"preempt_emergencies\":7"))), "nonboolean policy refuses");
            foreach (string profile in new[] { "dispatch.fair", "dispatch.disabled", "dispatch.mod" })
            {
                SimGame game = Start(Factory(profile)); Configure(c, game); Order ordinary = Post(c, game); Job job = Assigned(game);
                Order response = Raise(c, game);
                c.Equal(job.Id, Assigned(game).Id, "explicit policy veto retains actual incumbent: " + profile);
                c.Equal(JobState.Open, Board(game).Job(response.Node("repair").JobId).State, "response waits rather than bypass policy");
                c.Equal(0, Queue(game)[0].InterruptedJob, "veto cannot authorize retained interruption");
            }
            SimGame absent = Start(Factory(selected: false)); Configure(c, absent); Post(c, absent); Job active = Assigned(absent); Raise(c, absent);
            c.Equal(active.Id, Assigned(absent).Id, "absent optional capability has no inferred gameplay policy");
            string excluded = authored.Replace("\"id\":\"dispatch.preempt\",\"preempt_emergencies\":true,\"scopes\":[\"sim.work\"]", "\"id\":\"dispatch.preempt\",\"preempt_emergencies\":true,\"scopes\":[]");
            SimGame noScope = Start(Factory(profiles: excluded)); Configure(c, noScope); Post(c, noScope); active = Assigned(noScope); Raise(c, noScope);
            c.Equal(active.Id, Assigned(noScope).Id, "true flag cannot override explicit scope exclusion");
            // Lock the incumbent while generating a live emergency, then unlock without advancing time.
            SimGame preview = Start(Factory()); Configure(c, preview, interruptible: false); Order old = Post(c, preview); active = Assigned(preview); Raise(c, preview);
            c.Equal(active.Id, Assigned(preview).Id, "noninterruptible activity protected");
            Entity machine = Find(preview, "machine");
            machine.Get<IWorkPolicy>().Configure(preview.World, new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All));
            ulong before = preview.Hash(); var offers = Queue(preview); WorkerDispatchOffer retained = offers[0];
            c.That(retained.Ready && retained.Emergency && retained.InterruptedJob == active.Id, "pure combined preview identifies actual interruptible incumbent");
            var candidates = new List<WorkWorkerCandidate>();
            Order responseOrder = Board(preview).ActiveResponseOn(Find(preview, "destination").Id, new Code("damage"));
            c.That(preview.Seat(0).View.Feature<WorkView>().TryCopyWorkerCandidates(responseOrder.Id, "repair", candidates), "same candidate view resolves actual response");
            c.Equal(active.Id, candidates[0].InterruptedJob, "candidate view and combined queue agree");
            PlayerView.Create(preview.World, new PlayerId(1)).Feature<WorkView>().CopyAssignments(offers);
            c.Equal(0, offers.Count, "foreign company has no incumbent/response disclosure");
            c.Equal(before, preview.Hash(), "candidate/private reads do not stop motion, release claims or change jobs");
            c.Equal(active.Id, Assigned(preview).Id, "pure offer does not acquire worker");
            Tick(preview); c.Equal(responseOrder.Id, Assigned(preview).OrderId, "next real Update alone performs takeover");
            c.Equal(active.Id, retained.InterruptedJob, "retained immutable facts never follow later state");
            c.Equal(JobState.Open, active.State, "original job returns to existing queue");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(preview).StateSchemaVersion, "no duplicate job/preemption state codec");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking semantics have explicit save identity");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "build identifies actual preemption semantics");
            foreach (string denial in new[] { "emergency", "role", "route", "inputs", "area", "unavailable" })
            {
                Action<JsonValue> edit = denial == "inputs" ? r => r.Get("entities")[1].Get("components").Get("contract_incident").Set("response", JsonValue.Of("preempt.supplied-response")) :
                    denial == "area" ? r => r.Set("point_interval_regions", Content.Json.Parse("[{\"id\":\"preempt.home\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":1}]")) : null;
                SimGame denied = Start(Factory(edit: edit)); Configure(c, denied); Post(c, denied); Job incumbent = Assigned(denied);
                if (denial == "emergency") Configure(c, denied, emergency: false);
                if (denial == "role") Configure(c, denied, haulOnly: true);
                if (denial == "area") Configure(c, denied, area: "preempt.home");
                if (denial == "route") c.That(TestKit.Order(denied, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes real route");
                if (denial == "unavailable") WorkerExecutionTestContent.Control(denied, Find(denied, "machine").Id, false, Fix.One);
                Order blockedResponse = Raise(c, denied);
                c.Equal(denial == "unavailable" ? 0 : incumbent.Id, Find(denied, "machine").Get<IWorker>().Job,
                    "no emergency takeover; unavailable ordinary work follows native retry: " + denial);
                var blockedOffers = Queue(denied);
                c.That(blockedOffers.Count == 0 ? !blockedResponse.Blocker.IsNone :
                    !blockedOffers[0].Ready && blockedOffers[0].InterruptedJob == 0, "blocked requirement/offer cannot authorize interruption: " + denial);
            }
            SimGame mod = Start(Factory("dispatch.mod-preempt")); Configure(c, mod); Post(c, mod); Order emergency = Raise(c, mod);
            c.Equal(emergency.Id, Assigned(mod).OrderId, "independent content implements optional capability without scheduler edit");
            c.That(ReferenceEquals(mod.World.Services.Get<IWorkerDispatchPolicy>(), mod.World.Services.Get<IWorkerDispatchPreemptionPolicy>()), "both capabilities alias one selected content owner");
            IdleAndEmergencyProtection(c);
            SimGame fault = Start(Factory(edit: WorkerExecutionTestContent.AddJobGate)); Configure(c, fault); Post(c, fault);
            WorkerExecutionTestContent.Control(fault, Find(fault, "machine").Id, true, Fix.FromInt(2));
            fault.Seat(0).Submit(new RaiseIncidentCommand { Target = Find(fault, "destination").Id, Source = new Code("damage"), Kind = new Code("proof.failure") });
            fault.Update();
            c.That(fault.Fault != null, "post-interruption provider refusal faults actual Update instead of fabricating admission");
            c.Throws<InvalidOperationException>(() => fault.Save(), "partial failed takeover cannot checkpoint");
            c.Throws<InvalidOperationException>(() => fault.Hash(), "partial failed takeover cannot hash as success");
        }
        private static void IdleAndEmergencyProtection(Check c)
        {
            SimGame idle = Start(Factory(edit: r =>
            {
                var text = new System.Text.StringBuilder(); r.Get("entities")[2].WriteCanonical(text);
                JsonValue second = Content.Json.Parse(text.ToString()); second.Set("id", JsonValue.Of("spare"));
                r.Get("entities").Add(second); StartupContentAuthoring.Add(r, "spare", "spare", 0, second.Get("components").Get("point_presence"));
            }));
            Configure(c, idle); Post(c, idle); Job old = Assigned(idle); Entity spare = Find(idle, "spare");
            c.That(TestKit.Order(idle, 0, new ConfigureWorkPolicyCommand { Worker = spare.Id, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player enables actual spare machine");
            Order response = Raise(c, idle);
            c.Equal(old.Id, Assigned(idle).Id, "eligible idle spare preferred to interrupting busy worker");
            c.Equal(response.Id, Board(idle).Job(spare.Get<IWorker>().Job).OrderId, "emergency uses real idle spare");
            SimGame busy = Start(Factory(edit: r => r.Get("entities")[0].Get("components").Set("contract_incident",
                Content.Json.Parse("{\"id\":\"alarm\",\"kind\":\"proof.failure\",\"response\":\"shared.dry-response\"}"))));
            Configure(c, busy); Order first = Raise(c, busy); Job active = Assigned(busy);
            c.That(TestKit.Order(busy, 0, new RaiseIncidentCommand { Target = Find(busy, "source").Id, Source = new Code("alarm"), Kind = new Code("proof.failure") }).Accepted, "second current incident raised");
            c.Equal(active.Id, Assigned(busy).Id, "existing emergency cannot be preempted by another emergency");
            c.Equal(first.Id, Assigned(busy).OrderId, "actual response progress retains same worker");
        }
        private static void Physical(Check c)
        {
            foreach (string plan in new[] { "preempt.work", "preempt.travel" })
            {
                GameFactory factory = Factory(); SimGame game = Start(factory); Configure(c, game);
                Order old = Post(c, game, plan, plan == "preempt.work" ? "source" : "destination"); Job original = Assigned(game);
                if (plan == "preempt.work") Reach(game, () => old.Node("work").Progress.WholeTicks >= 3, "ordinary paid work starts");
                else Tick(game);
                long age = original.CreatedTick; WorkProgress progress = old.Node("work").Progress;
                Fix stock = Find(game, "source").Get<IOutletPort>().Amount(Supply(game));
                c.Equal(plan == "preempt.work" ? Fix.FromInt(12) : Fix.FromInt(20), stock, "startup materials paid exactly once before interruption");
                if (plan == "preempt.work") c.Equal(Fix.FromInt(10), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "real tool hold protects unspent quantity");
                SimGame prior = factory.Load(game.Save()); Order response = Raise(c, game); Order twinResponse = Raise(c, prior);
                c.Equal(game.Hash(), prior.Hash(), "saved pre-takeover execution makes identical real admission");
                c.Equal(JobState.Open, original.State, "same original job reopens rather than replacement intent");
                c.Equal(age, original.CreatedTick, "interruption retains original age");
                c.That(old.Node("work").Progress.WholeTicks >= progress.WholeTicks, "earned physical progress retained");
                c.Equal(response.Id, Assigned(game).OrderId, "actual emergency owns worker after interruption");
                c.That(response.State != OrderState.Done && Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "takeover is not instantaneous travel or repair");
                c.Equal(stock, Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "interrupt does not refund spent materials");
                c.Equal(stock, Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "interruption releases only original unspent native claims");
                WorkProgress heldProgress = old.Node("work").Progress;
                SimGame saved = factory.Load(game.Save()), isolated = Start(factory); ulong untouched = isolated.Hash();
                Reach(game, () => response.State == OrderState.Done, "physical emergency arrival/repair", saved);
                c.Equal(game.Hash(), saved.Hash(), "saved emergency travel and repair deterministic");
                c.Equal(heldProgress, old.Node("work").Progress, "ordinary activity earns no work while emergency owns machine");
                c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "actual completion alone restarts incident target");
                Reach(game, () => old.State == OrderState.Done, "retained original activity physically resumes", saved);
                c.Equal(game.Hash(), saved.Hash(), "saved interrupted native inputs and original progress resume identically");
                c.Equal(stock, Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "resume does not consume startup bill twice");
                c.Equal(untouched, isolated.Hash(), "selected immutable policy does not affect another session");
                c.Equal(JobState.Done, original.State, "same retained job physically completed");
                c.Equal(0, old.Node("work").JobId, "completed node clears live execution relation");
                c.That(game.Tick > response.CreatedTick + 10, "travel and repair consumed game time");
            }
            // Native loaded haul protection, even an independent profile that unconditionally accepts takeover.
            GameFactory haulFactory = Factory("dispatch.mod-preempt"); SimGame haulGame = Start(haulFactory); Configure(c, haulGame);
            c.That(TestKit.Order(haulGame, 0, new ConfigureStockPolicyCommand { Store = Find(haulGame, "destination").Id, Resource = Supply(haulGame),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 1, LogisticsReserveAccess.Protected)) }).Accepted, "player requests real stock transfer");
            Reach(haulGame, () => Assigned(haulGame)?.IsHaul == true, "stock demand native dispatch interval");
            Job haul = Assigned(haulGame); c.That(haul.IsHaul, "native job acquires real carrier");
            Reach(haulGame, () => haul.Loaded.IsPositive, "native partial loading"); ReservationToken receipt = haul.InReservation; Fix loaded = haul.Loaded;
            Order repair = Raise(c, haulGame);
            c.Equal(haul.Id, Assigned(haulGame).Id, "emergency cannot automatically cancel active haul");
            c.Equal(receipt, haul.InReservation, "destination receipt remains same native commitment");
            c.That(haul.Loaded >= loaded, "loaded cargo remains physically owned, not discarded");
            c.Equal(0, Queue(haulGame)[0].InterruptedJob, "unconditional mod cannot bypass engine haul protection");
            SimGame haulTwin = haulFactory.Load(haulGame.Save());
            Reach(haulGame, () => haul.State == JobState.Done, "protected haul physical delivery", haulTwin);
            c.Equal(Fix.FromInt(8), Find(haulGame, "destination").Get<IInletPort>().Amount(Supply(haulGame)), "exact native cargo delivered without duplication");
            Reach(haulGame, () => repair.State == OrderState.Done, "emergency after protected departure", haulTwin);
            c.Equal(haulGame.Hash(), haulTwin.Hash(), "saved protected cargo and pending emergency continuation deterministic");
        }
    }
}
