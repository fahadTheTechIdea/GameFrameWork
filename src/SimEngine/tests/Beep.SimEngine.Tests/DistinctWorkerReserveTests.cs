using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class DistinctWorkerReserveTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly WorkerRoleId Haul = new WorkerRoleId("haul"), Inspect = new WorkerRoleId("inspect");
        internal static void Register()
        {
            TestRunner.Add("Distinct reserves: maximum coverage, private contracts and deterministic matching", Contracts);
            TestRunner.Add("Distinct reserves: physical dispatch, emergencies and saved continuation", Physical);
        }
        // Only author the existing manifest-selected scenario/content. No test-side dispatch or worker state.
        private static GameFactory Fixture(bool publicPolicy = false, bool publicWorker = false,
            int[] fleet = null, Action<JsonValue> edit = null, Action<JsonValue> floors = null)
        {
            fleet = fleet ?? new[] { 3, 3 }; // bit 1 = haul, bit 2 = inspect, bit 3 = both
            var settings = Json.Parse(FrameworkProofContent.Read("distinct-worker-reserves.json")); floors?.Invoke(settings);
            return WorkerStandbyTests.Fixture(publicPolicy, publicWorker,
                author: row => row.Get("work_policies")[0].Set("assignment_mode", JsonValue.Of("all")),
                edit: root =>
                {
                    root.Get("scenarios")[0].Get("settings").Set(CompanyAlertModule.ModuleId, settings);
                    var original = root.Get("entities")[2];
                    if (!publicWorker) Roles(original, fleet[0]);
                    for (int i = 1; i < fleet.Length; i++)
                    {
                        var actor = original.Clone(); actor.Set("id", JsonValue.Of("reserve." + i));
                        var parts = actor.Get("components");
                        if (publicWorker)
                        {
                            parts.Get("contract_worker").Set("role", JsonValue.Of(fleet[i] == 1 ? "haul" : "inspect"));
                        }
                        else Roles(actor, fleet[i]);
                        root.Get("entities").Add(actor);
                        StartupContentAuthoring.Add(root, "reserve." + i, "reserve." + i, 0, actor.Get("components").Get("point_presence"));
                    }
                    edit?.Invoke(root);
                });
        }
        private static void Roles(JsonValue actor, int bits)
        {
            var roles = Json.Parse("[]");
            if ((bits & 1) != 0) roles.Add(JsonValue.Of("haul"));
            if ((bits & 2) != 0) roles.Add(JsonValue.Of("inspect"));
            actor.Get("components").Get("worker").Set("roles", roles);
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static CompanyAlertView View(SimGame game) => game.Seat(0).View.Feature<CompanyAlertView>();
        private static CompanyWorkerReserveCoverage Read(SimGame game)
        {
            var code = View(game).TryReadReserveCoverage(Company, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("coverage refused: " + code); return facts;
        }
        private static CompanyWorkerReserveSnapshot Row(CompanyWorkerReserveCoverage facts, WorkerRoleId role)
        { foreach (var row in facts.Roles) if (row.Role == role) return row; throw new InvalidOperationException("coverage role missing"); }
        private static void Policy(Check c, SimGame game, int haul, int inspect)
        {
            var current = game.World.Services.Get<ICompanyAlertPolicy>().ReadAlert(Company).Settings;
            var settings = new CompanyAlertSettings(current.Profile, current.Level,
                new[] { new WorkerReserveSetting(Inspect, inspect), new WorkerReserveSetting(Haul, haul) });
            c.Equal(Code.None, View(game).PreviewAlert(Company, settings), "private preview admits current authored roles");
            c.That(TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = Company, Settings = settings }).Accepted,
                "player edits existing company policy through its sole command owner");
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("reserve game update failed: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs)
                    facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.Worker + "/" + job.ToilIndex + "/" + job.Loaded);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Supply(game));
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "distinct coverage changes actual admission identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "only current self-described save identity");
            foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Fixture(publicPolicy); var game = New(factory); ulong hash = game.Hash(); var facts = Read(game);
                c.Equal(2L, facts.RequiredWorkers, "two distinct roles request two real machines");
                c.Equal(2, facts.CoveredWorkers, "two compatible machines cover both roles simultaneously");
                c.Equal(0L, facts.Shortfall, "no absent capacity is fabricated"); c.That(facts.Complete, "complete capacity coverage");
                c.Equal(2, Row(facts, Haul).ReadyWorkers, "raw haul compatibility remains observable");
                c.Equal(2, Row(facts, Inspect).ReadyWorkers, "raw inspection compatibility remains observable");
                c.Equal(1, Row(facts, Haul).Headroom, "raw role headroom is not permission to consume a distinct reserve");
                c.That(facts.Assignments[0].Worker != facts.Assignments[1].Worker, "one worker never covers two roles");
                foreach (var entity in game.World.Entities.All) if (entity.Has<IWorker>())
                    c.Equal(CompanyWorkerReserveCodes.MinimumReady, game.World.Services.Get<ICompanyWorkerReservePolicy>().CheckOrdinaryAssignment(entity),
                        "ordinary acquisition preserves simultaneous reserve capacity");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<CompanyAlertView>().TryReadReserveCoverage(Company, out var foreign), "coverage is company private");
                c.That(foreign == null, "foreign read leaks no worker assignment");
                c.Equal(CompanyAlertCodes.NotCompany, game.World.Services.Get<ICompanyWorkerReservePolicy>().ReadCoverage(new PlayerId(9), out var absent), "unknown company refuses through existing policy contract");
                c.That(absent == null, "unknown company never gets invented coverage");
                c.Equal(hash, game.Hash(), "coverage and admission reads allocate no claims, jobs or game time");
                c.That(ReferenceEquals(game.World.Services.Get<ICompanyAlertPolicy>(), game.World.Services.Get<ICompanyWorkerReservePolicy>()), "both contracts share one company policy");
                c.Equal(3, ((IPersistentService)game.World.Services.Get<ICompanyAlertPolicy>()).StateSchemaVersion, "current policy semantic version");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "current shared dispatch semantics");
                c.Throws<NotSupportedException>(() => ((IList<WorkerReserveAssignment>)facts.Assignments).Clear(), "retained assignment list is immutable");
                c.Throws<NotSupportedException>(() => ((IList<CompanyWorkerReserveSnapshot>)facts.Roles).Clear(), "retained role list is immutable");
                Policy(c, game, 2, 2); var shortage = Read(game);
                c.Equal(2L, shortage.Shortfall, "two multi-role workers cannot cover four floor slots");
                c.Equal(2, Row(shortage, Haul).CoveredWorkers, "stable ordinal role precedence for scarce capacity");
                c.Equal(0, Row(shortage, Inspect).CoveredWorkers, "raw compatibility is not double-counted");
                c.Equal(2, Row(shortage, Inspect).Shortfall, "per-role shortage uses distinct coverage");
                c.Equal(1, Row(facts, Inspect).CoveredWorkers, "old detached facts retain original role allocation");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "save/hash contains existing policies, not a coverage cache");
            }
            var augment = New(Fixture(fleet: new[] { 3, 1 })); var matched = Read(augment);
            c.Equal(2, matched.CoveredWorkers, "augmenting path reassigns lowest-ID multi-role machine rather than greedy starvation");
            c.Equal(Inspect, matched.Assignments[0].Role, "flexible first machine moves to inspection");
            c.Equal(Haul, matched.Assignments[1].Role, "single-role machine covers hauling");
            var singleton = New(Fixture(fleet: new[] { 3 })); var scarce = Read(singleton);
            c.Equal(1, scarce.CoveredWorkers, "one machine means one protected role"); c.Equal(1L, scarce.Shortfall, "one real missing responder");
            c.Equal(CompanyWorkerReserveCodes.MinimumReady, singleton.World.Services.Get<ICompanyWorkerReservePolicy>().CheckOrdinaryAssignment(Find(singleton, "machine")), "existing shortage cannot worsen by taking its only compatible actor");
            var huge = New(Fixture(floors: root => { var rows = root.Get("companies")[0].Get("worker_reserves"); for (int i = 0; i < rows.Count; i++) rows[i].Set("minimum_ready", JsonValue.Number("2147483647")); }));
            c.Equal(4294967294L, Read(huge).RequiredWorkers, "aggregate requested capacity is signed 64 bit");
            c.Equal(4294967292L, Read(huge).Shortfall, "billions of absent slots do not allocate synthetic workers");
            Availability(c); Provider(c); InvalidFacts(c); MatchingMatrix(c);
        }
        private static void Availability(Check c)
        {
            foreach (bool extension in new[] { false, true })
            {
                var game = New(Fixture(extension)); var actor = Find(game, "machine"); var original = WorkPolicyRules.Read(actor);
                foreach (string cause in new[] { "disabled", "emergency", "roles" })
                {
                    c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = new WorkPolicySettings(
                        cause != "disabled", original.Priority, cause == "roles" ? Array.Empty<WorkerRoleId>() : original.AllowedRoles,
                        original.Area, original.Interruptible, cause != "emergency", original.AssignmentMode) }).Accepted, "player changes real ready policy: " + cause);
                    c.Equal(1, Read(game).CoveredWorkers, "unready worker is excluded from distinct coverage: " + cause);
                }
                c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = original }).Accepted, "restore actual worker policy");
                c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.Zero).Accepted, "zero actual execution capacity");
                c.Equal(1, Read(game).CoveredWorkers, "zero-rate worker cannot supply a floor slot");
                c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.One).Accepted, "restore actual execution rate");
                c.Equal(2, Read(game).CoveredWorkers, "ready capacity restores without a roster or alternate time");
            }
            c.Throws<InvalidOperationException>(() => Read(New(Fixture(publicPolicy: true, edit: root =>
                root.Get("entities")[2].Get("components").Get("proof_work_policy").Set("fault", JsonValue.Number("3"))))),
                "impure provider settings cannot inflate matching facts");
        }
        private static void Provider(Check c)
        {
            foreach (bool policy in new[] { false, true })
            {
                var game = New(Fixture(policy, publicWorker: true, fleet: new[] { 2, 1 })); var facts = Read(game);
                c.Equal(2, facts.CoveredWorkers, "public single-role workers cover both authored floors through contracts");
                c.Equal(1, Row(facts, Haul).ReadyWorkers, "public haul role is actually declared");
                c.Equal(1, Row(facts, Inspect).ReadyWorkers, "public inspection role is actually declared");
            }
            var scoped = New(Fixture(edit: root => {
                var foreign = root.Get("entities")[2].Clone(); foreign.Set("id", JsonValue.Of("foreign.worker")); root.Get("entities").Add(foreign);
                StartupContentAuthoring.Add(root, "foreign.worker", "foreign.worker", 1, foreign.Get("components").Get("point_presence")); }));
            c.Equal(2, Read(scoped).CoveredWorkers, "another company's actual worker cannot inflate coverage");
            c.Equal(2, Row(Read(scoped), Inspect).ReadyWorkers, "raw readiness remains company scoped too");
            c.Equal(Code.None, scoped.Seat(1).View.Feature<CompanyAlertView>().TryReadReserveCoverage(new PlayerId(1), out var foreignFacts), "another company may query its own empty reserve policy");
            c.Equal(0, foreignFacts.CoveredWorkers, "foreign explicit empty policy does not inherit player floors");
            var empty = New(Fixture(floors: root => root.Get("companies")[0].Set("worker_reserves", Json.Parse("[]"))));
            c.That(Read(empty).Complete && Read(empty).Assignments.Count == 0, "explicit empty floors reserve no worker");
            c.Equal(Code.None, View(empty).TryReadReserve(Company, Inspect, out var unprotected), "valid unconfigured role remains explicitly unprotected");
            c.Equal(0, unprotected.MinimumReady, "unconfigured role has no implicit policy floor");
            c.Equal(0, unprotected.CoveredWorkers, "zero floor acquires no illustrative slot");
            c.Equal(2, unprotected.ReadyWorkers, "raw compatibility still truthful for unconfigured role");
        }
        private static void InvalidFacts(Check c)
        {
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(Company, Inspect, 1, 2, 2), "covered facts cannot exceed floor");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(Company, Inspect, 2, 1, 2), "covered facts cannot exceed actual compatible count");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(Company, Inspect, 1, 1, -1), "covered count must be nonnegative");
            c.Throws<ArgumentException>(() => new WorkerReserveAssignment(EntityId.None, Inspect), "no phantom worker coverage");
            var row = new CompanyWorkerReserveSnapshot(Company, Inspect, 1, 2, 1); var worker = new EntityId(1);
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveCoverage(Company, new[] { row, row }, Array.Empty<WorkerReserveAssignment>()), "duplicate role rows reject");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveCoverage(Company, new[] { row }, Array.Empty<WorkerReserveAssignment>()), "row coverage must match distinct worker assignments");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveCoverage(Company, new[] { row }, new[] { new WorkerReserveAssignment(worker, Inspect), new WorkerReserveAssignment(worker, Inspect) }), "duplicate actual worker rejects");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveCoverage(Company, new[] { row }, new[] { new WorkerReserveAssignment(worker, Haul) }), "unknown assignment role rejects");
            var rows = new[] { row }; var assignments = new[] { new WorkerReserveAssignment(worker, Inspect) };
            var detached = new CompanyWorkerReserveCoverage(Company, rows, assignments); rows[0] = default; assignments[0] = default;
            c.Equal(1, detached.CoveredWorkers, "input arrays cannot mutate detached public projection");
            c.Equal(Inspect, detached.Assignments[0].Role, "copied assignments preserve explicit original role");
        }
        // Exhaustive small test oracle; it never assigns an engine worker or advances gameplay.
        private static int Maximum(int[] fleet, int index, int haul, int inspect)
        {
            if (index == fleet.Length) return 0;
            int best = Maximum(fleet, index + 1, haul, inspect);
            if (haul > 0 && (fleet[index] & 1) != 0) best = Math.Max(best, 1 + Maximum(fleet, index + 1, haul - 1, inspect));
            if (inspect > 0 && (fleet[index] & 2) != 0) best = Math.Max(best, 1 + Maximum(fleet, index + 1, haul, inspect - 1));
            return best;
        }
        private static void MatchingMatrix(Check c)
        {
            for (int a = 1; a <= 3; a++) for (int b = 1; b <= 3; b++) for (int d = 1; d <= 3; d++)
            {
                var fleet = new[] { a, b, d }; var game = New(Fixture(fleet: fleet));
                c.Equal(Maximum(fleet, 0, 1, 1), Read(game).CoveredWorkers, "maximum matching agrees with independent small oracle");
                Policy(c, game, 2, 1);
                c.Equal(Maximum(fleet, 0, 2, 1), Read(game).CoveredWorkers, "capacity matching supports several slots per role");
                var retained = Read(game); var twin = New(Fixture(fleet: fleet, floors: root => {
                    var rows = root.Get("companies")[0].Get("worker_reserves"); rows[0].Set("minimum_ready", JsonValue.Number("2"));
                    var first = rows[0].Clone(); var last = rows[1].Clone(); root.Get("companies")[0].Set("worker_reserves", Json.Parse("[]"));
                    root.Get("companies")[0].Get("worker_reserves").Add(last); root.Get("companies")[0].Get("worker_reserves").Add(first); }));
                var equivalent = Read(twin);
                for (int i = 0; i < retained.Assignments.Count; i++)
                {
                    c.Equal(retained.Assignments[i].Worker, equivalent.Assignments[i].Worker, "authored row order does not alter deterministic worker selection");
                    c.Equal(retained.Assignments[i].Role, equivalent.Assignments[i].Role, "authored row order does not alter deterministic role allocation");
                }
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool policy in new[] { false, true }) { EmergencyAndWork(c, policy); Cancellation(c, policy); Hauling(c, policy); }
            SelfService(c);
        }
        private static void EmergencyAndWork(Check c, bool publicPolicy)
        {
            var factory = Fixture(publicPolicy); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); var clock = game.Clock;
            var ordinary = Post(c, game); var job = Board(game).Job(ordinary.Node("work").JobId);
            c.Equal(CompanyWorkerReserveCodes.MinimumReady, job.Blocker, "two protected multi-role actors cannot service ordinary maximum-priority job");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "blocked work consumes no actual materials");
            var candidates = new List<WorkWorkerCandidate>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyWorkerCandidates(ordinary.Id, "work", candidates), "existing view returns truthful candidates");
            foreach (var candidate in candidates) c.Equal(CompanyWorkerReserveCodes.MinimumReady, candidate.Result.Blocker, "each protected ordinary candidate refuses shared reserve gate");
            var response = Raise(c, game); c.Equal(1, Read(game).CoveredWorkers, "genuine emergency may use a real protected actor");
            c.Equal(1L, Read(game).Shortfall, "busy emergency actor is not simultaneously ready");
            c.That(response.State != OrderState.Done, "emergency dispatch is not instant repair");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long time = game.Clock.StepIndex;
            Step(game, twin); c.Equal(paused, game.Hash(), "one paused game clock freezes all work and response state");
            c.Equal(time, game.Clock.StepIndex, "matching reads do not advance any independent time"); game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => response.State == OrderState.Done && Read(game).CoveredWorkers == 2, "actual travel and response then released readiness", twin);
            c.Equal(0L, ordinary.Node("work").Progress.WholeTicks, "emergency use does not authorize unrelated queued work");
            c.Equal(game.Hash(), twin.Hash(), "saved physical response restores deterministic matching facts");
            Policy(c, game, 1, 0); Policy(c, twin, 1, 0);
            Until(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "released inspection floor admits ordinary physical work", twin);
            EntityId incumbent = Board(game).Job(ordinary.Node("work").JobId).Worker;
            Policy(c, game, 1, 1); Policy(c, twin, 1, 1);
            c.Equal(incumbent, Board(game).Job(ordinary.Node("work").JobId).Worker, "higher floors never claw back an acquired incumbent");
            c.Equal(1, Read(game).CoveredWorkers, "busy paid incumbent leaves real capacity shortage");
            Until(game, () => ordinary.State == OrderState.Done && Read(game).CoveredWorkers == 2, "existing paid work completes through current owner", twin);
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "physical work consumes its eight materials once");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "completion releases actual unspent tool hold");
            c.Equal(game.Hash(), twin.Hash(), "policy edits and physical saved continuation stay exact");
            c.Equal(untouched, isolated.Hash(), "another session receives no matching or reserve state");
            c.That(ReferenceEquals(clock, game.Clock), "same single clock owns entire scenario");
        }
        private static void Cancellation(Check c, bool publicPolicy)
        {
            var factory = Fixture(publicPolicy); var game = New(factory); Policy(c, game, 1, 0); var order = Post(c, game);
            Until(game, () => order.Node("work").Progress.WholeTicks >= 3, "paid cancellable work");
            var twin = factory.Load(game.Save()); Policy(c, game, 1, 1); Policy(c, twin, 1, 1);
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels incumbent through existing command");
            c.Equal(2, Read(game).CoveredWorkers, "cancelled worker returns to actual simultaneous coverage");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "cancellation never refunds already consumed material");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "cancellation releases unspent held tools");
            var blocked = Post(c, game, "preempt.travel", "destination"); Post(c, twin, "preempt.travel", "destination");
            c.Equal(CompanyWorkerReserveCodes.MinimumReady, Board(game).Job(blocked.Node("work").JobId).Blocker, "cancelled incumbent privilege does not authorize fresh ordinary assignment");
            c.Equal(0L, blocked.Node("work").Progress.WholeTicks, "fresh refused work has no fabricated progress");
            c.Equal(game.Hash(), twin.Hash(), "saved cancellation, resource release and fresh refusal stay deterministic");
        }
        private static void Hauling(Check c, bool publicPolicy)
        {
            var factory = Fixture(publicPolicy, fleet: new[] { 3, 1, 1 }); var game = New(factory);
            var hauls = game.World.Services.Get<IHaulExecution>(); ulong hash = game.Hash();
            var offer = hauls.PreviewHaul(Company, Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4));
            c.Equal(Code.None, offer.Blocker, "extra compatible actual machine permits ordinary hauling");
            c.Equal(hash, game.Hash(), "physical haul preview acquires no claim");
            int id = hauls.AcquireHaul(Company, Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4), 1);
            c.That(id > 0, "normal public haul execution acquires real spare"); var job = Board(game).Job(id);
            EntityId carrier = job.Worker;
            c.That(job.Worker != Find(game, "machine").Id, "only actual inspector remains protected");
            c.Equal(2, Read(game).CoveredWorkers, "two distinct protected roles remain ready while spare hauls");
            Until(game, () => job.Loaded.IsPositive, "real partial loading before checkpoint"); var twin = factory.Load(game.Save());
            Policy(c, game, 2, 1); Policy(c, twin, 2, 1);
            c.Equal(id, FindById(game, carrier).Get<IWorker>().Job, "loaded incumbent retains cargo after increased floor");
            Until(game, () => Stock(game, "destination") == Fix.FromInt(4) && FindById(game, carrier).Get<IWorker>().Job == 0, "physical delivery then worker release", twin);
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "delivery conserves real source material");
            c.Equal(3, Read(game).CoveredWorkers, "released carrier restores three actual slots");
            c.Equal(game.Hash(), twin.Hash(), "partial cargo save and reserve edits continue identically");
        }
        private static Entity FindById(SimGame game, EntityId id) => game.World.Entities.Get(id);
        private static void SelfService(Check c)
        {
            var factory = ReservedSelfServiceTests.Fixture(publicPolicy: true, publicWorker: true, publicSource: true);
            var game = FacilitySelectionTests.New(factory); var actor = FacilitySelectionTests.Find(game, "machine.b");
            c.Equal(4, Read(game).CoveredWorkers, "four actual ready inspectors cover authored service reserve");
            c.That(TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = actor.Id, Source = new Code("fleet.service"),
                Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(1), new Code("service.nearest")) }).Accepted, "player enables real opted-in self-service source");
            Until(game, () => actor.Get<IWorker>().Job != 0, "self-service acquires true reserved worker");
            c.Equal(3, Read(game).CoveredWorkers, "self-service exemption does not fake ready coverage");
            c.That(!Board(game).IsEmergency(Board(game).Job(actor.Get<IWorker>().Job)), "self-service is not a fabricated emergency");
            c.That(TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = actor.Id, Source = new Code("fleet.service"),
                Settings = new RecurringPlanSettings(false, 60, GameDuration.Minutes(1), new Code("service.nearest")) }).Accepted, "disable future posts without cancelling incumbent");
            long plan = FacilitySelectionTests.SourceView(game, true).Schedule.LastPlan; var twin = factory.Load(game.Save());
            Until(game, () => FacilitySelectionTests.Plan(game, plan).State == PlanState.Done && Read(game).CoveredWorkers == 4, "physical service and reserve recovery", twin);
            c.Equal(Fix.FromInt(10), FacilitySelectionTests.Condition(game, "machine.b"), "actual worker condition repairs through existing effect");
            c.Equal(Fix.FromInt(29), FacilitySelectionTests.Stock(game, "source"), "actual service pays three spares and releases tools");
            c.Equal(game.Hash(), twin.Hash(), "public worker/policy/source service saves exactly with coverage");
        }
    }
}
