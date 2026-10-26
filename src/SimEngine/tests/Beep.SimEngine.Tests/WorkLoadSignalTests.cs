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
    // Explicit authored content, ordinary player inputs, bounded updates and observation only.
    internal static class WorkLoadSignalTests
    {
        private static readonly Code Feed = new Code("workload"), Availability = new Code("availability");
        internal static void Register()
        {
            TestRunner.Add("Workload signals: authored scopes, private facts and query ownership", Contracts);
            TestRunner.Add("Workload signals: physical queues, deferred controls and saved continuation", c => Physical(c, false));
            TestRunner.Add("Workload signals: independent JSON metadata drives physical queues saved controls and replay", c => Physical(c, true));
        }
        private static string Text(JsonValue value) { var output = new StringBuilder(); value.WriteCanonical(output); return output.ToString(); }
        internal static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> signalEdit = null, bool recordFlows = false, bool independent = false)
        {
            var signals = Json.Parse(FrameworkProofContent.Read("workload-signals.json")); signalEdit?.Invoke(signals);
            if (independent) WorkSignalMetadataProof.SelectIndependent(signals);
            return ServiceDockTests.Factory(root =>
            {
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(WorkSignalsModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(AutomationModule.ModuleId));
                if (independent) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.worker-capacity"));
                if (recordFlows) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                root.Get("entities")[0].Get("components").Set("work_load_signal", Json.Parse("{\"id\":\"workload\",\"scope\":\"endpoint\"}"));
                root.Get("entities")[1].Get("components").Set("work_load_signal", Json.Parse("{\"id\":\"workload\",\"scope\":\"company\"}"));
                for (int i = 2; i < root.Get("entities").Count; i++)
                    root.Get("entities")[i].Get("components").Set("work_load_signal", Json.Parse("{\"id\":\"workload\",\"scope\":\"worker\"}"));
                edit?.Invoke(root);
                if (independent) WorkSignalMetadataProof.SelectIndependent(root);
            }, extraModules: independent ? new GameModule[] { new WorkSignalsModule(), new AutomationModule(), new WorkerCapacityProofModule() }
                : new GameModule[] { new WorkSignalsModule(), new AutomationModule() },
                extraDeclarations: "[{\"id\":\"sim.work-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"workload-signals.json\"]},{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}"
                + (independent ? ",{\"id\":\"proof.worker-capacity\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}" : "") + "]",
                extraDocuments: new Dictionary<string, string> { ["workload-signals.json"] = Text(signals) });
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 853 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing workload entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static bool Arrived(SimGame game, Job job)
            => Board(game).ReadDockWaiting(job, out _, out bool arrived).IsNone && arrived;
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static OperationsView Operations(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static WorkLoadSnapshot Read(SimGame game, WorkLoadScope scope, string id = null)
        {
            Code refusal = View(game).ReadWorkLoad(new PlayerId(0), scope, id == null ? EntityId.None : Find(game, id).Id, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("workload query refused: " + refusal); return value;
        }
        private static Fix Signal(SimGame game, string signal, string id = "destination")
        {
            Code refusal = Operations(game).ReadSignal(Find(game, id).Id, Feed, signal, out var value);
            if (!refusal.IsNone || !value.Available) throw new InvalidOperationException("workload signal refused: " + refusal + "/" + value.Blocker);
            return value.Value.Value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); if (twin != null) twin.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("workload update fault at " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin?.Fault); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout at game step " + game.Clock.StepIndex +
                "; live=" + Read(game, WorkLoadScope.Company).LiveJobs + "; waiting=" + Read(game, WorkLoadScope.Company).DockWaiting + DescribeJobs(game));
        }
        internal static string DescribeJobs(SimGame game)
        {
            var text = new StringBuilder();
            foreach (var job in Board(game).Jobs)
                if (job.IsLive) text.Append("; job=").Append(job.Id).Append('/').Append(job.State).Append('/').Append(job.Blocker)
                    .Append(" worker=").Append(job.Worker).Append(" toil=").Append(job.ToilIndex).Append('/').Append(job.Toils.Count)
                    .Append(" slots=").Append(job.SourceDockSlot).Append('/').Append(job.DestinationDockSlot);
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Get<IWorker>() != null)
                    text.Append("; ").Append(entity.Def.Id).Append('@').Append(entity.Get<PointPresence>().Position);
            return text.ToString();
        }
        private static Order Post(SimGame game, string target = "source")
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, target).Id });
            if (!result.Accepted || game.Fault != null) throw new InvalidOperationException("workload order refused: " + result + "/" + game.Fault);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static bool Enabled(SimGame game, string id)
        { if (!Operations(game).TryGetControl(Find(game, id).Id, Availability, out var value)) throw new InvalidOperationException("missing execution control"); return value.Enabled; }
        private static AutomationSettings Settings(SimGame game, string profile, string input, string target)
        {
            var rule = game.World.Content.Get<IControlRule>(profile);
            return new AutomationSettings(profile, true, new ControlBinding(Find(game, target).Id, Availability),
                new Dictionary<string, ControlBinding> { ["jobs"] = new ControlBinding(Find(game, input).Id, Feed) },
                ControlThresholds.Initial(rule), rule.WhenTrue, rule.WhenFalse);
        }
        private static AutomationRuleSnapshot Rule(SimGame game)
        { if (!Operations(game).TryGetAutomationRule(new PlayerId(0), "workload", out var value)) throw new InvalidOperationException("missing workload rule"); return value; }
        private static void Configure(Check c, SimGame game, AutomationSettings settings)
        {
            ulong before = game.Hash(); c.Equal(Code.None, Operations(game).PreviewAutomation("workload", settings), "authored workload preview accepts");
            c.Equal(before, game.Hash(), "workload automation preview is pure");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "workload", Settings = settings }).Accepted, "same settings admit ordinary control rule");
        }
        private static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var query = game.World.Services.Get<IWorkLoad>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "one current workload implementation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing owner payloads unchanged");
            c.That(ReferenceEquals(query, Board(game)), "read-only contract aliases the sole board, no query service state");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "workload adds no job persistence schema");
            c.Equal(Codes.NotFound, Board(game).ReadDockWaiting(new Job { Id = 1 }, out _, out _), "physical waiting query refuses an unowned job instance");
            c.That(Find(game, "machine.a").Get<IWorker>() is Worker && Find(game, "machine.b").Get<IWorker>() is ContractWorker,
                "same counts support native and unrelated public-only worker contracts");
            c.That(Operations(game).TryGetSignalSchema(Find(game, "source").Id, Feed, out var schema) && schema.Version == 2 && schema.Bindings.Count == 14,
                "job and capacity numeric bindings discovered through existing immutable schema");
            ulong hash = game.Hash(); var frozen = Read(game, WorkLoadScope.Company);
            foreach (string metric in new[] { "live", "activity", "haul", "open", "assigned", "paused", "waiting", "arrived" })
                c.Equal(Fix.Zero, Signal(game, "load." + metric), "empty actual workload is exact zero " + metric);
            c.Equal(hash, game.Hash(), "all query/signal/discovery observations leave authoritative hash unchanged");
            c.Equal(Codes.NotFound, View(game, 1).ReadWorkLoad(new PlayerId(0), WorkLoadScope.Company, EntityId.None, out var refused), "foreign aggregate private");
            c.Equal(0, refused.LiveJobs, "foreign refusal exposes no partial snapshot");
            c.Equal(Codes.NotOwned, Operations(game, 1).ReadSignal(Find(game, "source").Id, Feed, "load.live", out _), "foreign signal cannot leak operational activity");
            c.Equal(Codes.NotSeated, query.ReadWorkLoad(new PlayerId(4), WorkLoadScope.Company, EntityId.None, out _), "unknown company refuses");
            c.Equal(Codes.InvalidArgument, query.ReadWorkLoad(new PlayerId(0), (WorkLoadScope)0, EntityId.None, out _), "unknown scope refuses");
            c.Equal(Codes.InvalidArgument, query.ReadWorkLoad(new PlayerId(0), WorkLoadScope.Company, Find(game, "source").Id, out _), "company scope cannot silently discard subject");
            c.Equal(Codes.InvalidArgument, query.ReadWorkLoad(new PlayerId(0), WorkLoadScope.Endpoint, EntityId.None, out _), "endpoint scope requires identity");
            c.Equal(Codes.NotOwned, query.ReadWorkLoad(new PlayerId(1), WorkLoadScope.Endpoint, Find(game, "source").Id, out _), "module queries require actual owner");
            c.Equal(Codes.NotFound, query.ReadWorkLoad(new PlayerId(0), WorkLoadScope.Endpoint, new EntityId(999999), out _), "missing subject cannot silently become another endpoint");
            c.Equal(StandardCodes.NotWorker, query.ReadWorkLoad(new PlayerId(0), WorkLoadScope.Worker, Find(game, "source").Id, out _), "worker query requires worker contract");
            foreach (string field in new[] { "id", "scope" }) c.Throws<ContentException>(() => New(Factory(root => root.Get("entities")[0].Get("components").Get("work_load_signal").Remove(field))), "required authored " + field);
            c.Throws<ContentException>(() => New(Factory(root => root.Get("entities")[0].Get("components").Get("work_load_signal").Set("scope", JsonValue.Of("default")))), "no invented scope fallback");
            c.Throws<ContentException>(() => New(Factory(root => root.Get("entities")[0].Get("components").Get("work_load_signal").Set("scope", JsonValue.Of("worker")))), "nonworker authored binding rejects");
            foreach (string field in new[] { "metric", "unit", "kind" }) c.Throws<ContentException>(() => New(Factory(signalEdit: root => root.Get("signals")[0].Set(field, JsonValue.Of(field == "kind" ? "boolean" : "wrong")))), "typed schema refuses wrong " + field);
            c.Throws<ContentException>(() => New(Factory(signalEdit: root => root.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("1")))), "exact provider schema required");
            var wrongWorld = New(factory);
            c.Throws<ArgumentException>(() => game.World.Content.Get<AutomationSignalDef>("load.live").ReadFrom(wrongWorld.World, Find(game, "source").Get<IAutomationSignalSource>(Feed)), "signals refuse wrong executing world");
            c.Equal(GameState.Faulted, wrongWorld.State, "wrong-world provider observation latches the actual executing session's first fault");
            c.Throws<InvalidOperationException>(() => wrongWorld.Save(), "invalid provider session cannot publish a resumable save");
            var other = New(factory);
            c.Equal(Fix.Zero, Signal(other, "load.live"), "other session remains independent");
            var order = Post(game);
            c.Equal(2, Read(game, WorkLoadScope.Company).LiveJobs, "two parallel nodes create two real jobs");
            c.Equal(2, Read(game, WorkLoadScope.Endpoint, "source").ActivityJobs, "endpoint query counts actual node jobs");
            var loaded = Read(game, WorkLoadScope.Company);
            c.Equal(loaded.LiveJobs, loaded.OpenJobs + loaded.AssignedJobs + loaded.PausedJobs, "state counts partition the same actual live jobs");
            c.Equal(0, View(game, 1).ReadWorkLoad(new PlayerId(1), WorkLoadScope.Company, EntityId.None, out var ownEmpty).IsNone ? ownEmpty.LiveJobs : -1, "other company has its own genuine zero workload");
            c.Equal(0, frozen.LiveJobs, "retained aggregate snapshot remains immutable");
            c.Equal(Fix.FromInt(2), Signal(game, "load.live"), "signal observes sole board admission");
            c.Equal(Read(game, WorkLoadScope.Worker, "machine.b").LiveJobs, checked((int)(Signal(game, "load.live", "machine.b").Raw >> Fix.FractionalBits)), "public worker signal and canonical query agree");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation releases original jobs");
            c.Equal(0, Read(game, WorkLoadScope.Company).LiveJobs, "terminal jobs excluded even before final history pruning");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "stateless bridge restores from existing owners");
            Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "restored empty workload continuation identical");
        }
        private static void Physical(Check c, bool independent)
        { DeferredWork(c, independent); Queues(c, independent); CurrentLeg(c, independent); }
        private static void DeferredWork(Check c, bool independent)
        {
            var factory = Factory(root =>
            {
                for (int i = 2; i < root.Get("entities").Count; i++) root.Get("entities")[i].Get("components").Get("worker_execution").Get("operation").Set("enabled", JsonValue.Of(false));
                // Disabled machines need authored parking outside the sole active machine's exit corridor.
                for (int i = 3; i < root.Get("entity_setups").Count; i++)
                    root.Get("entity_setups")[i].Get("placement").Set("position", JsonValue.Number((24 + i).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }, independent: independent);
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recorder = game.RecordReplay(limits);
            Configure(c, game, Settings(game, "load.dispatch", "destination", "machine.a"));
            var order = Post(game); c.Equal(2, Read(game, WorkLoadScope.Company).OpenJobs, "disabled machines leave actual jobs open");
            c.That(!Enabled(game, "machine.a"), "new work cannot mutate execution in same game update");
            Step(game); c.That(Rule(game).PendingDecision == true && !Enabled(game, "machine.a"), "sole automation pass schedules next-update ordinary enable");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending workload decision and real open jobs save together");
            long coordinate = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            ulong paused = game.Hash(); game.Update(0); twin.Update(0);
            c.Equal(coordinate, game.Clock.StepIndex, "sole game clock pause stops dispatch and deferred actions"); c.Equal(paused, game.Hash(), "paused workload engine does not advance state");
            game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin);
            c.That(Enabled(game, "machine.a"), "next-update normal command enables actual native worker execution");
            Reach(game, () => order.Node("a").Progress.WholeTicks > 0 || order.Node("b").Progress.WholeTicks > 0, "automatically enabled physical work", twin);
            c.That(Read(game, WorkLoadScope.Worker, "machine.a").AssignedJobs == 1, "one actual machine assignment, no generated worker");
            c.That(Find(game, "machine.a").Get<PointPresence>().Position == Fix.One, "positive work occurs at actual physical service bay");
            Reach(game, () => !order.IsActive, "both physical nodes complete", twin);
            c.Equal(OrderState.Done, order.State, "two authored nodes finish with actual travel/work/departure");
            c.Equal(0, Read(game, WorkLoadScope.Company).LiveJobs, "completed work is no longer backlog");
            Reach(game, () => !Enabled(game, "machine.a"), "empty backlog shuts idle worker through deferred normal command", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical backlog rule and saved twin remain deterministic");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved unmodified session has no shared workload state");
            c.Equal(Fix.FromInt(32), Find(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "observation and ordinary work create no imaginary material transfer");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success, "actual backlog control and physical work replay through original engine owners");
            c.Equal(game.Hash(), verified.ActualHash, "backlog replay reaches the actual final live state");
        }
        private static void Queues(Check c, bool independent)
        {
            var factory = Factory(root => FlowGeometry(root, false), independent: independent); var game = New(factory);
            Configure(c, game, Settings(game, "load.queue", "source", "machine.d"));
            int haulId = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id,
                Find(game, "destination").Id, game.World.Content.ResourceId("supply"), Fix.FromInt(8), 100);
            Job haul = Board(game).Job(haulId); c.That(haul != null, "real native haul acquires existing endpoint/cargo/carrier claims");
            Reach(game, () => Find(game, "machine.a").Get<PointPresence>().Position == Fix.FromInt(11), "actual carrier reaches its loading bay");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = haul.Worker, Operation = Availability,
                Enabled = false, Target = Fix.One }).Accepted, "ordinary player control holds the actual loading carrier while the queue forms");
            var order = Post(game);
            c.Equal(3, Read(game, WorkLoadScope.Company).LiveJobs, "one haul and two actual activity jobs counted exactly once");
            c.Equal(1, Read(game, WorkLoadScope.Endpoint, "destination").HaulJobs, "company-scoped destination source sees sole haul");
            c.That(Read(game, WorkLoadScope.Company).DockWaiting >= 1, "physical waiting claims observable before arrival");
            Reach(game, () => Read(game, WorkLoadScope.Endpoint, "source").DockArrivedWaiting == 2, "two machines arrive at actual loading-side waiting slots");
            var frozen = Read(game, WorkLoadScope.Endpoint, "source");
            c.Equal(2, frozen.DockWaiting, "two activity claims wait behind occupied actual haul bay");
            c.Equal(Fix.FromInt(2), Signal(game, "load.arrived", "source"), "typed queue signal reads real physical arrival");
            c.Equal(WorkProgress.Zero, order.Node("a").Progress, "queue observation earns no remote work");
            ulong readHash = game.Hash(); for (int i = 0; i < 3; i++) { Read(game, WorkLoadScope.Company); Signal(game, "load.arrived", "source"); }
            c.Equal(readHash, game.Hash(), "physical arrival diagnostics are pure");
            Reach(game, () => Rule(game).PendingDecision == true, "arrived queue signal earns authored dwell");
            c.That(!Enabled(game, "machine.d"), "queue action is pending, not same-update feedback");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "real mixed queue and pending control retain same claims on save");
            Step(game, twin); c.That(Enabled(game, "machine.d"), "next-update queue action controls actual worker execution");
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, new ConfigureOperationCommand { Entity = haul.Worker, Operation = Availability,
                    Enabled = true, Target = Fix.One }).Accepted, "ordinary saved player control resumes the actual loading carrier");
            Reach(game, () => order.Node("a").Progress.WholeTicks > 0, "first queued activity enters released bay", twin);
            c.Equal(2, frozen.DockArrivedWaiting, "retained arrived snapshot does not track live mutation");
            c.That(Read(game, WorkLoadScope.Endpoint, "source").DockWaiting < frozen.DockWaiting, "actual promotion removes only promoted waiting claim");
            c.Equal(game.Hash(), twin.Hash(), "queue and arrival facts continue identically after load");
            WorkProgress beforePause = order.Node("a").Progress;
            EntityId servicingWorker = Board(game).Job(order.Node("a").JobId).Worker;
            foreach (var current in new[] { game, twin })
            {
                WorkPolicySettings policy = WorkPolicyRules.Read(current.World.Entities.Get(servicingWorker));
                c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand { Worker = servicingWorker,
                    Settings = new WorkPolicySettings(false, policy.Priority, policy.AllowedRoles, policy.Area,
                        policy.Interruptible, policy.EmergencyEligible, WorkAssignmentMode.All) }).Accepted, "normal work policy pauses actual servicing worker");
            }
            Step(game, twin);
            c.Equal(Code.None, View(game).ReadWorkLoad(new PlayerId(0), WorkLoadScope.Worker, servicingWorker, out var pausedWork), "actual assigned worker remains a valid workload subject");
            c.Equal(1, pausedWork.PausedJobs, "paused worker is actual paused work, not new open backlog");
            c.Equal(beforePause, order.Node("a").Progress, "signal and queue controls earn no paused work");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation releases queued physical work");
            c.That(TestKit.Order(twin, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "saved twin accepts same cancellation");
            Reach(game, () => haul.State == JobState.Done, "actual hauling survives unrelated queue cancellation", twin);
            Reach(game, () => !Enabled(game, "machine.d"), "cleared queue earns dwell/cooldown then deferred shutdown", twin);
            c.Equal(0, Read(game, WorkLoadScope.Company).LiveJobs, "delivered/cancelled jobs are excluded from final workload");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "one real haul transfers one native batch");
            c.Equal(game.Hash(), twin.Hash(), "cancelled queue and delivered cargo keep saved identity");
        }
        private static void CurrentLeg(Check c, bool independent)
        {
            var factory = Factory(root => {
                FlowGeometry(root, true);
                root.Get("entities")[1].Get("components").Get("work_load_signal").Set("scope", JsonValue.Of("endpoint"));
            }, independent: independent);
            var game = New(factory); var supply = game.World.Content.ResourceId("supply");
            var hauls = game.World.Services.Get<IHaulExecution>(); var source = Find(game, "source").Id; var destination = Find(game, "destination").Id;
            Job first = Board(game).Job(hauls.AcquireHaul(new PlayerId(0), source, destination, supply, Fix.FromInt(8), 100));
            Job second = Board(game).Job(hauls.AcquireHaul(new PlayerId(0), source, destination, supply, Fix.FromInt(8), 90));
            c.That(first != null && second != null, "two genuine native hauls acquire bounded facility waiting claims");
            c.Equal(new Code("unload.wait.a"), second.DestinationDockSlot, "second trip reserves a future receiving waiting place");
            c.Equal(2, Read(game, WorkLoadScope.Endpoint, "source").HaulJobs, "shared source counts each original job once");
            c.Equal(2, Read(game, WorkLoadScope.Endpoint, "destination").HaulJobs, "shared destination sees real pipeline jobs");
            c.Equal(1, Read(game, WorkLoadScope.Company).DockWaiting, "company counts only current pickup waiting claim");
            c.Equal(Fix.Zero, Signal(game, "load.waiting"), "future destination waiting reservation is not current queue depth");
            ulong beforeFault = game.Hash(); EntityId originalWorker = second.Worker;
            second.Worker = EntityId.None; // Explicit fault instrumentation, not a gameplay workflow.
            c.Equal(Codes.NotFound, View(game).ReadWorkLoad(new PlayerId(0), WorkLoadScope.Company, EntityId.None, out var missing), "missing waiting participant refuses truthful arrival diagnostics");
            c.Equal(0, missing.LiveJobs, "failed aggregate reveals no successfully counted prefix");
            second.Worker = originalWorker;
            c.Equal(beforeFault, game.Hash(), "restored fault instrumentation preserves original job/claim owners");
            Reach(game, () => second.ToilIndex >= 2 && Arrived(game, second), "second vehicle physically reaches receiving waiting slot");
            c.Equal(0, Read(game, WorkLoadScope.Endpoint, "source").DockWaiting, "departed pickup claim is not an ongoing source queue");
            c.Equal(Fix.One, Signal(game, "load.arrived"), "destination queue appears only for actual receiving arrival");
            c.Equal(1, Read(game, WorkLoadScope.Company).DockArrivedWaiting, "same arrived waiting job counted once company-wide");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "receiving queue/loaded cargo save without cached counts");
            Reach(game, () => first.State == JobState.Done && second.State == JobState.Done, "two physical deliveries and exits", twin);
            c.Equal(Fix.Zero, Signal(game, "load.live"), "receiving queue drains through real delivery, not signal mutation");
            c.Equal(Fix.FromInt(16), Find(game, "destination").Get<IInventory>().Amount(supply), "two real transfers deliver exactly sixteen units");
            c.Equal(game.Hash(), twin.Hash(), "current-leg query and physical saved twins remain identical");
        }
        // Authored one-dimensional convoy: waiting behind each bay, exits on both sides, idle parking off-route.
        private static void FlowGeometry(JsonValue root, bool twoCarriers)
        {
            var positions = new Dictionary<string, int> { ["source"] = 10, ["destination"] = 30,
                ["machine.a"] = 10, ["machine.b"] = twoCarriers ? 50 : 9,
                ["machine.c"] = twoCarriers ? 9 : 6, ["machine.d"] = twoCarriers ? 60 : 50 };
            var entities = root.Get("entities");
            for (int i = 0; i < entities.Count; i++)
                entities[i].Get("components").Get("point_presence").Set("position", JsonValue.Number(positions[entities[i].Get("id").Text].ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var entries = root.Get("entity_setups");
            for (int i = 0; i < entries.Count; i++)
                entries[i].Get("placement").Set("position", JsonValue.Number(positions[entries[i].Get("entity").Text].ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var source = entities[0].Get("components").Get("point_dock").Get("slots");
            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i].Get("id").Text;
                if (id == "load.wait.a") source[i].Set("offset", JsonValue.Number("-2"));
                if (id == "load.wait.b") source[i].Set("offset", JsonValue.Number("-3"));
            }
            var destination = entities[1].Get("components").Get("point_dock").Get("slots");
            for (int i = 0; i < destination.Count; i++)
            {
                if (destination[i].Get("id").Text == "unload.exit.d") destination[i].Set("offset", JsonValue.Number("0"));
                if (twoCarriers && destination[i].Get("id").Text == "unload.wait.a") destination[i].Set("offset", JsonValue.Number("-3"));
                if (twoCarriers && destination[i].Get("id").Text == "unload.wait.b") destination[i].Set("offset", JsonValue.Number("-2"));
            }
        }
    }
}
