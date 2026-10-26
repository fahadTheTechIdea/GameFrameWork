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
    public static class DispatchAgingTests
    {
        public static void Register()
        {
            TestRunner.Add("Dispatch aging: authored thresholds, private ranking and strict composition", Policy);
            TestRunner.Add("Dispatch aging: physical fairness, retained age and saved continuation", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.shipments\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"aging-provider.json\"]}";
        internal static GameFactory Factory(string profiles = null, string profile = "dispatch.fair", bool shipment = false,
            bool selected = true, Action<JsonValue> edit = null, string workDefinitions = null, int shipmentPriority = 0,
            string shipmentDefinitions = null, string shipmentDocument = "aging-provider.json", IReadOnlyList<string> startupProfiles = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions), scenario = root.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
            if (selected) scenario.Get("modules").Add(JsonValue.Of(DispatchPolicyModule.ModuleId));
            scenario.Get("settings").Set(DispatchPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"" + profile + "\"}"));
            scenario.Get("settings").Set(CompanyAlertModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.standard\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            root.Get("entity_states").Add(Content.Json.Parse("{\"id\":\"stopped\",\"state\":\"point.incident\",\"priority\":70}"));
            root.Get("entities")[1].Get("components").Set("contract_incident", Content.Json.Parse("{\"id\":\"damage\",\"kind\":\"proof.failure\",\"response\":\"shared.dry-response\"}"));
            edit?.Invoke(root); var definitions = new StringBuilder(); root.WriteCanonical(definitions);
            JsonValue provider = Content.Json.Parse(shipmentDefinitions ?? FrameworkProofContent.Read("dispatch-source.json"));
            provider.Get("proof_shipments")[0].Set("enabled", JsonValue.Of(shipment));
            provider.Get("proof_shipments")[0].Set("priority", JsonValue.Number(shipmentPriority.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            provider.Set("proof_dispatch_profiles", Content.Json.Parse(FrameworkProofContent.Read("dispatch-aging-provider.json")).Get("proof_dispatch_profiles"));
            var authored = new StringBuilder(); provider.WriteCanonical(authored);
            return PointGame.Factory(definitions.ToString(), workDefinitions ?? FrameworkProofContent.Read("shared-dispatch.json"), new DispatchProofModule(),
                Declaration.Replace("aging-provider.json", shipmentDocument), authored.ToString(), FrameworkProofContent.Read("company-alerts.json"),
                dispatchProfiles: profiles ?? FrameworkProofContent.Read("dispatch-policy.json"), startupProfiles: startupProfiles);
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 601 }; request.Mods.Add("proof.shipments"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing aging entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static Job Assigned(SimGame game) => Board(game).Job(Find(game, "machine").Get<IWorker>().Job);
        private static List<WorkerDispatchOffer> Queue(SimGame game)
        { var result = new List<WorkerDispatchOffer>(); game.Seat(0).View.Feature<WorkView>().CopyAssignments(result); return result; }
        private static Order Post(Check c, SimGame game, string plan = "shared.work", string target = "source", int priority = 1)
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Plan = plan, Target = Find(game, target).Id, Priority = priority });
            c.That(result.Accepted, "player posts actual authored work");
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Enable(Check c, SimGame game, bool enabled = true, bool emergency = true, bool haulOnly = false)
            => c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(enabled, 0, haulOnly ? new[] { new WorkerRoleId("haul") } :
                    new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, emergency, WorkAssignmentMode.All) }).Accepted, "player changes authoritative worker policy");
        private static void Stock(Check c, SimGame game, int priority = 1)
            => c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), priority, LogisticsReserveAccess.Protected)) }).Accepted, "player posts actual stock demand");
        private static void Priority(Check c, SimGame game, Order order, int priority)
            => c.That(TestKit.Order(game, 0, new SetOrderPriorityCommand { Order = order.Id, Priority = priority }).Accepted, "player changes existing priority without new intent");
        private static void Until(SimGame game, long tick) { while (game.Tick < tick) Tick(game); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 360 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/age=" + (game.Tick - job.CreatedTick) + "/" + job.Blocker + "/loaded=" + job.Loaded);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Policy(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            IWorkerDispatchPolicy policy = game.World.Services.Get<IWorkerDispatchPolicy>();
            c.That(ReferenceEquals(policy, game.World.Content.Get<DispatchProfileDef>("dispatch.fair")), "selected policy aliases sole immutable catalog definition");
            c.That(!(policy is IPersistentService), "pure definition adds no mutable policy codec");
            var authored = (AuthoredDispatchProfileDef)policy;
            c.Throws<NotSupportedException>(() => ((IList<string>)authored.DispatchScopes).Add("another"), "declared scopes are immutable");
            c.Throws<ContentException>(() => authored.Read(new JsonReader(Content.Json.Parse("{}"), "proof.reread")), "resolved profile cannot be reauthored");
            string profiles = FrameworkProofContent.Read("dispatch-policy.json");
            foreach (string bad in new[] { profiles.Replace("\"numerator\":5", "\"numerator\":0"),
                profiles.Replace("\"scopes\":[\"sim.work\",\"sim.logistics\",\"proof.shipments\"]", "\"scopes\":[\"sim.work\",\"sim.work\"]"),
                profiles.Replace("\"proof.shipments\"", "\"missing.source\""),
                profiles.Replace("\"scopes\":[\"sim.work\",\"sim.logistics\",\"proof.shipments\"]", "\"scopes\":[\"\"]"),
                profiles.Replace("\"scopes\":[\"sim.work\",\"sim.logistics\",\"proof.shipments\"]", "\"scopes\":false"),
                profiles.Replace("\"numerator\":5", "\"numerator\":-1"),
                profiles.Replace("\"scopes\":[\"sim.work\",\"sim.logistics\",\"proof.shipments\"],", "") })
                c.Throws<ContentException>(() => New(Factory(bad)), "invalid authored selected scope/duration refuses");
            c.Throws<ContentException>(() => New(Factory(profile: "missing")), "unknown selected profile refuses");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("scenarios")[0].Get("settings").Remove(DispatchPolicyModule.ModuleId))), "missing selected policy settings has no default");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("scenarios")[0].Get("settings").Get(DispatchPolicyModule.ModuleId).Remove("profile"))), "missing selected identity refuses");
            SimGame absent = New(Factory(selected: false)); Post(c, absent); Until(absent, 8);
            c.That(!absent.World.Services.Has<IWorkerDispatchPolicy>(), "installed but unselected module injects no gameplay rule");
            c.That(Queue(absent)[0].StarvationAfterTicks == null && !Queue(absent)[0].Starved, "absent capability projects explicitly excluded aging");
            SimGame excluded = New(Factory(profile: "dispatch.disabled")); Post(c, excluded); Until(excluded, 8);
            c.That(Queue(excluded)[0].StarvationAfterTicks == null && !Queue(excluded)[0].Starved, "explicit empty scopes disable aging without a fabricated threshold");
            SimGame workOnly = New(Factory(profile: "dispatch.work-only")); Post(c, workOnly); Stock(c, workOnly); Until(workOnly, 8);
            foreach (WorkerDispatchOffer offer in Queue(workOnly))
                c.That(offer.Scope == WorkModule.ModuleId ? offer.Starved && offer.StarvationAfterTicks == 5 : !offer.Starved && offer.StarvationAfterTicks == null, "scope exclusion is current policy, not source-submitted rank");
            SimGame finer = New(Factory(edit: r => r.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"))));
            Post(c, finer); c.Equal(10L, Queue(finer)[0].StarvationAfterTicks.Value, "duration resolves against actual fine session quantum");
            SimGame wide = New(Factory(profiles.Replace("\"numerator\":5", "\"numerator\":5000000000")));
            Post(c, wide); c.Equal(5000000000L, Queue(wide)[0].StarvationAfterTicks.Value, "authored duration uses wide session ticks with no integer gameplay cap");
            Order old = Post(c, game); Until(game, 4); WorkerDispatchOffer retained = Queue(game)[0];
            c.That(!retained.Starved && retained.StarvationAfterTicks == 5, "before threshold actual age is normal");
            Tick(game); c.That(Queue(game)[0].Starved, "threshold crossing derives starvation at immutable checkpoint");
            c.That(!retained.Starved && retained.AgeTicks == 4, "retained detached facts do not follow future updates");
            ulong before = game.Hash(); var offers = Queue(game);
            var hidden = new List<WorkerDispatchOffer>(offers); PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyAssignments(hidden);
            c.Equal(0, hidden.Count, "foreign company learns no private age/policy/work facts");
            PlayerView.Create(game.World, PlayerId.None).Feature<WorkView>().CopyAssignments(hidden);
            c.Equal(offers.Count, hidden.Count, "observer receives detached current ranking");
            c.Equal(before, game.Hash(), "rank/private reads neither mutate age nor acquire claims");
            Priority(c, game, old, int.MaxValue); c.Equal(offers[0].CreatedTick, Queue(game)[0].CreatedTick, "priority edit retains original age");
            c.Equal(int.MaxValue, Queue(game)[0].Priority, "authored starvation never clamps explicit priority");
            var alert = new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = new CompanyAlertSettings("alerts.wide", CompanyAlertLevel.Urgent, Array.Empty<WorkerReserveSetting>()) };
            c.That(TestKit.Order(game, 0, alert).Accepted, "actual company alert composes with aging");
            c.That(Queue(game)[0].EffectivePriority > int.MaxValue && Queue(game)[0].Starved, "checked wide alert priority and separate starvation fields do not overflow a combined score");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing job codec unchanged; age is not duplicated");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current semantics have explicit breaking identity");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "build identifies new ranking");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "selected content and existing age restore without a policy state store");
            c.Equal(Queue(game)[0].Starved, Queue(twin)[0].Starved, "save continuation derives identical ranking");
            c.Throws<CorruptStateException>(() => Factory(profiles.Replace("\"numerator\":5", "\"numerator\":6")).Load(game.Save()), "different authored threshold cannot load current content fingerprint");
            Mod(c); Faults(c);
        }
        private static void Mod(Check c)
        {
            GameFactory factory = Factory(profile: "dispatch.mod", shipment: true); SimGame game = New(factory); Post(c, game, priority: int.MaxValue); Tick(game);
            DispatchProfileDef selected = game.World.Content.Get<DispatchProfileDef>("dispatch.mod");
            c.That(ReferenceEquals(selected, game.World.Services.Get<IWorkerDispatchPolicy>()), "independent policy uses the same resolved base alias");
            c.Throws<ContentException>(() => selected.Read(new JsonReader(Content.Json.Parse("{}"), "proof.mod-reread")), "independent definition preserves shared immutability after authoring");
            var offers = Queue(game);
            c.Equal("proof.shipments", offers[0].Scope, "independent scope-specific threshold promotes existing external source");
            c.Equal(1L, offers[0].StarvationAfterTicks.Value, "external pure profile supplies shipment duration");
            c.That(offers[0].Starved && !offers[1].Starved, "same scheduler evaluates independent profile without a type switch");
            c.Equal(4L, offers[1].StarvationAfterTicks.Value, "same external profile gives ordinary work a different authored duration");
            SimGame twin = factory.Load(game.Save()); Enable(c, game); Enable(c, twin);
            c.That(Assigned(game)?.IsHaul == true, "external aged shipment acquires actual native carrier and receipts");
            c.Equal(game.Hash(), twin.Hash(), "external policy and physical admission restore identically");
        }
        private static void Faults(Check c)
        {
            foreach (string id in new[] { "dispatch.zero", "dispatch.negative", "dispatch.undeclared" })
            {
                SimGame game = New(Factory(profile: id)); Post(c, game); Stock(c, game);
                for (int i = 0; i < 20 && game.Fault == null; i++) game.Update();
                c.That(game.Fault != null, "invalid independent threshold faults normal Update: " + id);
                c.Throws<InvalidOperationException>(() => game.Save(), "invalid policy does not checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), "invalid policy cannot hash as successful");
            }
        }
        private static void Physical(Check c) { Work(c); Cohort(c); Haul(c); Emergency(c); Blocked(c); RetryAndCancel(c); }
        private static void Cohort(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order oldest = Post(c, game);
            Until(game, 4); Order high = Post(c, game, "shared.second", "destination", int.MaxValue);
            Until(game, 10); Order stream = Post(c, game, "shared.work", "destination", int.MaxValue);
            var offers = Queue(game);
            c.That(offers[0].Starved && offers[1].Starved && !offers[2].Starved, "both older jobs are starved before the new priority stream");
            c.Equal(1, offers[0].Priority, "oldest eligible starvation cohort precedes a younger starved maximum priority");
            c.Equal(oldest.Id, Board(game).Job((int)offers[0].Identity).OrderId, "cohort ordering uses actual low-priority original intent");
            SimGame twin = factory.Load(game.Save()); Enable(c, game); Enable(c, twin);
            c.Equal(oldest.Id, Assigned(game).OrderId, "physical assignment uses oldest first even when both jobs are starved");
            c.Equal(game.Hash(), twin.Hash(), "saved pre-assignment cohort produces identical native worker relation");
            Reach(game, () => oldest.State == OrderState.Done, "oldest cohort physically finishes", twin);
            Reach(game, () => Assigned(game)?.OrderId == high.Id, "older maximum priority before continuing stream", twin);
            c.That(stream.IsActive, "new stream remains real queued intent, not dropped to manufacture fairness");
            c.Equal(game.Hash(), twin.Hash(), "cohort continuation is deterministic after real completion");
        }
        private static void Work(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order old = Post(c, game);
            Until(game, 4); Order newer = Post(c, game, "shared.second", "destination", int.MaxValue);
            var offers = Queue(game); c.That(offers[0].Starved && !offers[1].Starved, "old work crosses threshold ahead of younger high priority");
            c.Equal(old.Id, Board(game).Job((int)offers[0].Identity).OrderId, "actual old low-priority intent leads the shared ranking");
            Enable(c, game); c.Equal(old.Id, Assigned(game).OrderId, "same actual pass assigns old work before newer maximum priority");
            long age = Assigned(game).CreatedTick; int jobId = Assigned(game).Id;
            SimGame twin = factory.Load(game.Save()), isolated = New(factory); ulong untouched = isolated.Hash();
            Priority(c, game, newer, int.MaxValue); Priority(c, twin, Board(twin).Order(newer.Id), int.MaxValue);
            Order stream = Post(c, game, "shared.work", "destination", int.MaxValue);
            Post(c, twin, "shared.work", "destination", int.MaxValue);
            c.Equal(jobId, Assigned(game).Id, "new high-priority stream never preempts real acquired work");
            c.Equal(age, Assigned(game).CreatedTick, "active age is never rebased");
            Reach(game, () => old.State == OrderState.Done, "old work physically completes ahead of waiting stream", twin);
            c.Equal(game.Hash(), twin.Hash(), "starved work and newer stream continue identically after save");
            c.Equal(untouched, isolated.Hash(), "shared immutable policy cannot mutate an interleaved idle session");
            c.That(newer.IsActive && stream.IsActive, "waiting high priority intent is not cancelled by promotion");
            Reach(game, () => Assigned(game)?.OrderId == newer.Id, "oldest waiting starvation cohort dispatch", twin);
            c.Equal(game.Hash(), twin.Hash(), "cohort dispatch uses original age and stable identity");
            // Both pending jobs are starved; the earlier one must win before a still newer priority stream.
            c.That(Board(game).Job(newer.Node("work").JobId).CreatedTick < Board(game).Job(stream.Node("work").JobId).CreatedTick, "starvation ordering uses actual original job ticks");
        }
        private static void Haul(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Stock(c, game); Until(game, 5);
            Order younger = Post(c, game, priority: int.MaxValue); var offers = Queue(game);
            c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "aged native stock demand precedes young maximum-priority work");
            long original = offers[0].CreatedTick;
            Enable(c, game); Job haul = Assigned(game); c.That(haul.IsHaul, "same shared pass acquires actual aged shipment");
            int job = haul.Id; ReservationToken receipt = haul.InReservation;
            Priority(c, game, younger, int.MaxValue); c.Equal(job, Assigned(game).Id, "waiting priority edit does not replace acquired carrier");
            c.Equal(receipt, haul.InReservation, "native destination protection survives rank changes");
            c.That(original < haul.CreatedTick, "standing intent age is not fabricated into physical job creation");
            Reach(game, () => haul.Loaded.IsPositive, "aged shipment actual partial pickup");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "source pickup does not teleport to destination");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded shipment saves without derived queue state");
            Reach(game, () => haul.State == JobState.Done, "aged shipment physical delivery", twin);
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "destination receives real elapsed haul exactly once");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "only actual pickup leaves source");
            c.Equal(game.Hash(), twin.Hash(), "saved aging/cargo continuation conserves native stock");
            Reach(game, () => Assigned(game)?.OrderId == younger.Id, "waiting work after physical departure", twin);
            c.Equal(game.Hash(), twin.Hash(), "oldest remaining work gets same actual machine after haul");
        }
        private static void Emergency(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order old = Post(c, game, priority: int.MaxValue); Until(game, 7);
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = Find(game, "destination").Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "player raises actual authored industrial incident");
            Order response = Board(game).ActiveResponseOn(Find(game, "destination").Id, new Code("damage"));
            Enable(c, game); c.Equal(response.Id, Assigned(game).OrderId, "actual young emergency precedes old ordinary starvation cohort");
            c.Equal(JobState.Open, Board(game).Job(old.Node("work").JobId).State, "emergency precedence does not fabricate ordinary worker claims");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => response.State == OrderState.Done, "actual emergency travels and repairs", twin);
            c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "only real timed response resolves incident");
            c.Equal(game.Hash(), twin.Hash(), "emergency before starved work continues identically");
            Reach(game, () => Assigned(game)?.OrderId == old.Id, "old ordinary work after emergency", twin);
            c.Equal(game.Hash(), twin.Hash(), "ordinary age survives actual emergency pressure");
        }
        private static void Blocked(Check c)
        {
            SimGame game = New(Factory()); Order old = Post(c, game); Until(game, 6); Stock(c, game, int.MaxValue);
            Enable(c, game, haulOnly: true);
            c.That(Assigned(game)?.IsHaul == true, "ready haul precedes old starved activity without a compatible worker");
            c.Equal(JobState.Open, Board(game).Job(old.Node("work").JobId).State, "starvation never bypasses actual role eligibility");
            var offers = Queue(game);
            c.That(offers.Count == 1 && offers[0].Starved && !offers[0].Ready, "blocked starvation stays truthful and cannot claim busy machine");
            // Pure shared comparison also proves emergency and readiness remain absolute.
            var regular = new WorkerDispatchOffer("sim.work", 1, "job", new PlayerId(0), Find(game, "source").Id,
                Find(game, "machine").Id, true, Code.None, false, int.MaxValue, 0, 10, 0, 1, 0, true).WithStarvation(5);
            var urgent = new WorkerDispatchOffer("sim.work", 2, "job", new PlayerId(0), Find(game, "destination").Id,
                Find(game, "machine").Id, true, Code.None, true, 1, 9, 10, 1, 1, 0, true).WithStarvation(5);
            c.That(WorkSystem.CompareAssignments(urgent, regular) < 0, "actual emergency class precedes ordinary cohort regardless of priority");
            var blocked = new WorkerDispatchOffer("sim.work", 3, "job", new PlayerId(0), Find(game, "source").Id,
                EntityId.None, false, WorkSystem.BlockerNoWorker, true, int.MaxValue, 0, 10, null, 1, 0, true).WithStarvation(5);
            c.That(WorkSystem.CompareAssignments(regular, blocked) < 0, "readiness precedes even an aged emergency with no worker");
        }
        private static void RetryAndCancel(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            Order order = Post(c, game, target: "destination"); Until(game, 6); Enable(c, game);
            Job job = Assigned(game); long original = job.CreatedTick;
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual route during starved work travel");
            c.That(job.State == JobState.Open && job.RetryAt > game.Tick, "ordinary physical route failure retains its existing retry owner");
            var offers = Queue(game);
            c.That(offers[0].Starved && !offers[0].Ready && offers[0].Blocker == WorkSystem.BlockerRetry, "aged deferred retry is visible but cannot bypass deadline");
            c.Equal(original, offers[0].CreatedTick, "route retry never resets actual starvation age");
            SimGame twin = factory.Load(game.Save());
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }).Accepted, "real route restoration uses engine command");
            c.That(TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true }).Accepted, "saved twin receives same route command");
            c.Equal(original, Queue(game)[0].CreatedTick, "reopened route alone does not create new age");
            Reach(game, () => job.State == JobState.Assigned, "existing due retry returns to real starved dispatch", twin);
            c.Equal(original, job.CreatedTick, "actual reacquisition retains original timestamp");
            c.Equal(game.Hash(), twin.Hash(), "saved retry and derived policy continue identically");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels actual aged execution");
            c.That(TestKit.Order(twin, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "same explicit cancel in saved twin");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "cancellation releases actual worker relation");
            c.Equal(0, Queue(game).Count, "ended aged intent does not survive in a parallel ranking queue");
            c.Equal(game.Hash(), twin.Hash(), "cancelled age/progress checkpoint remains deterministic");
        }
    }
}
