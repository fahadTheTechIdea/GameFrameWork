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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class CompanyWorkerReserveTests
    {
        private static readonly WorkerRoleId Inspect = new WorkerRoleId("inspect"), Haul = new WorkerRoleId("haul");
        internal static void Register()
        {
            TestRunner.Add("Company reserves: authored floors, private controls and fresh admission", Contracts);
            TestRunner.Add("Company reserves: physical dispatch, emergency use and saved continuation", Physical);
        }
        private static GameFactory Fixture(bool publicPolicy = false, bool publicWorker = false, bool second = false,
            bool supplied = false, bool haulWorker = false, Action<JsonValue> author = null, Action<JsonValue> edit = null)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("company-worker-reserves.json")); author?.Invoke(authored);
            return WorkerStandbyTests.Fixture(publicPolicy, publicWorker, supplied, haulWorker: haulWorker,
                author: row => row.Get("work_policies")[0].Set("assignment_mode", JsonValue.Of("all")), edit: root =>
                {
                    root.Get("scenarios")[0].Get("settings").Set(CompanyAlertModule.ModuleId, authored);
                    if (second)
                    {
                        var machine = root.Get("entities")[2].Clone(); machine.Set("id", JsonValue.Of("machine.reserve")); root.Get("entities").Add(machine);
                        StartupContentAuthoring.Add(root, "reserve", "machine.reserve", 0, machine.Get("components").Get("point_presence"));
                    }
                    edit?.Invoke(root);
                }, workAuthor: work => { var other = work.Get("activities")[0].Clone(); other.Set("id", JsonValue.Of("reserve.other")); work.Get("activities").Add(other); });
        }
        private static CompanyAlertView View(SimGame game) => game.Seat(0).View.Feature<CompanyAlertView>();
        private static SimGame Start(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static CompanyAlertSettings Settings(SimGame game, int minimum, WorkerRoleId? role = null, CompanyAlertLevel? level = null)
        {
            var current = game.World.Services.Get<ICompanyAlertPolicy>().ReadAlert(new PlayerId(0)).Settings;
            return new CompanyAlertSettings(current.Profile, level ?? current.Level,
                new[] { new WorkerReserveSetting(role ?? Inspect, minimum) });
        }
        private static void Configure(Check c, SimGame game, int minimum, WorkerRoleId? role = null, CompanyAlertLevel? level = null)
        {
            var settings = Settings(game, minimum, role, level);
            c.Equal(Code.None, View(game).PreviewAlert(new PlayerId(0), settings), "preview admits same current role-floor settings as command");
            c.That(TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = settings }).Accepted,
                "player changes sole company policy owner");
        }
        private static CompanyWorkerReserveSnapshot Read(SimGame game, WorkerRoleId? role = null)
        {
            Code refusal = View(game).TryReadReserve(new PlayerId(0), role ?? Inspect, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException(refusal.ToString()); return value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("reserve update fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.ToilIndex + "/" + job.Loaded);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Supply(game));
        private static byte[] Encode(CompanyAlertSettings settings)
        { using var stream = new MemoryStream(); CompanyAlertSettings.Sync(new StateWriter(stream), settings); return stream.ToArray(); }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "company floors change shared admission semantics"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one clean current policy codec");
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true })
            {
                var factory = Fixture(publicPolicy, publicWorker); var game = Start(factory); var actor = Find(game, "machine");
                var policy = game.World.Services.Get<ICompanyAlertPolicy>(); var reserves = game.World.Services.Get<ICompanyWorkerReservePolicy>();
                c.That(ReferenceEquals(policy, reserves), "alert/reserve contracts alias exactly one company settings owner");
                c.Equal(3, ((IPersistentService)policy).StateSchemaVersion, "company protocol declares current required list");
                var initial = policy.ReadAlert(actor.Owner); var retained = Read(game); ulong hash = game.Hash();
                c.Equal(1, retained.ReadyWorkers, "actual native/public idle enabled responder is ready"); c.Equal(1, retained.MinimumReady, "explicit master-listed scenario settings own floor");
                c.Equal(0, retained.Headroom, "last actual responder has no headroom under floor"); c.Equal(0, retained.Shortfall, "actual readiness meets authored floor");
                c.Equal(CompanyWorkerReserveCodes.MinimumReady, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, actor, actor.Owner, Inspect,
                    new[] { Find(game, "source") }, false), "ordinary admission protects current company floor");
                c.Equal(Code.None, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, actor, actor.Owner, Inspect,
                    new[] { Find(game, "destination") }, true), "actual emergency acquisition can use real reserve");
                c.Equal(hash, game.Hash(), "count/assignment reads create no membership, claim or elapsed time");
                c.Equal(Codes.NotOwned, PlayerView.Create(game.World, new PlayerId(1)).Feature<CompanyAlertView>().TryReadReserve(actor.Owner, Inspect, out _), "foreign company cannot read readiness");
                c.Equal(Codes.InvalidArgument, View(game).TryReadReserve(actor.Owner, new WorkerRoleId("missing"), out _), "unknown role has typed refusal");
                c.Equal(CompanyAlertCodes.NotCompany, reserves.ReadReserve(new PlayerId(9), Inspect, out _), "unknown company never invents reserve settings");
                var invalid = Settings(game, 1, new WorkerRoleId("missing"));
                c.Equal(CompanyAlertCodes.InvalidSettings, View(game).PreviewAlert(actor.Owner, invalid), "unknown current role refuses private control preview");
                var refusal = TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = actor.Owner, Settings = invalid });
                c.That(!refusal.Accepted && refusal.Refusal == CompanyAlertCodes.InvalidSettings, "command and preview share unknown-role refusal");
                var order = Post(c, game); c.Equal(0, actor.Get<IWorker>().Job, "ordinary maximum priority cannot consume protected role");
                c.Equal(CompanyWorkerReserveCodes.MinimumReady, Board(game).Job(order.Node("work").JobId).Blocker, "existing queued job exposes company floor wait");
                Configure(c, game, 1, level: CompanyAlertLevel.Emergency); c.Equal(0, actor.Get<IWorker>().Job, "company alert cannot classify ordinary work as response");
                c.Equal(1, initial.Settings.WorkerReserves[0].MinimumReady, "retained immutable settings do not track later edits");
                game.Seat(0).Submit(new ConfigureCompanyAlertCommand { Company = actor.Owner, Settings = Settings(game, 2) });
                var twin = factory.Load(game.Save()); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending reserve edit saves and executes identically");
                c.Equal(1, Read(game).Shortfall, "shortfall reports actual absent capacity, not fabricated workers");
                c.Equal(1, retained.MinimumReady, "retained readiness fact is detached from current floor");
                var edited = policy.ReadAlert(actor.Owner); Configure(c, game, 2);
                c.Equal(edited.Version, policy.ReadAlert(actor.Owner).Version, "identical role-floor edit does not increment its sole policy revision");
                c.Equal(edited.ChangedTick, policy.ReadAlert(actor.Owner).ChangedTick, "identical floor edit preserves original sole-clock change coordinate");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current reserve settings and queue save exactly");
                var truncated = Encode(Settings(game, 2)); Array.Resize(ref truncated, truncated.Length - 1);
                c.Throws<CorruptStateException>(() => CompanyAlertSettings.Sync(new StateReader(truncated, 0, truncated.Length), initial.Settings), "truncated shared reserve codec rejects");
                AtomicRestore(c, game);
            }
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                root => root.Get("companies")[0].Remove("worker_reserves"), root => root.Get("companies")[0].Set("worker_reserves", JsonValue.Null()),
                root => root.Get("companies")[0].Get("worker_reserves")[0].Set("minimum_ready", JsonValue.Number("-1")),
                root => root.Get("companies")[0].Get("worker_reserves")[0].Remove("minimum_ready"),
                root => root.Get("companies")[0].Get("worker_reserves")[0].Set("minimum_ready", JsonValue.Number("0.5")),
                root => root.Get("companies")[0].Get("worker_reserves")[0].Set("role", JsonValue.Of("missing")),
                root => root.Get("companies")[0].Get("worker_reserves").Add(root.Get("companies")[0].Get("worker_reserves")[0].Clone()) })
                c.Throws<ContentException>(() => Start(Fixture(author: invalid)), "explicit unique nonnegative known authored reserve roles required");
            c.Throws<ArgumentException>(() => new WorkerReserveSetting(WorkerRoleId.None, 1), "default role never substitutes a responder");
            c.Throws<ArgumentException>(() => new WorkerReserveSetting(Inspect, -1), "negative reserve cannot enter current settings");
            c.Throws<ArgumentException>(() => new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, null), "no implicit empty reserve constructor");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(PlayerId.None, Inspect, 1, 1, 0), "public provider facts require actual company identity");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(new PlayerId(0), WorkerRoleId.None, 1, 1, 0), "public provider facts require explicit role");
            c.Throws<ArgumentException>(() => new CompanyWorkerReserveSnapshot(new PlayerId(0), Inspect, 1, -1, 0), "public provider facts reject negative readiness");
            var shuffled = new[] { new WorkerReserveSetting(Inspect, 1), new WorkerReserveSetting(Haul, 2) };
            var a = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, shuffled); Array.Reverse(shuffled);
            var b = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, shuffled);
            c.That(Convert.ToBase64String(Encode(a)) == Convert.ToBase64String(Encode(b)), "role settings serialize canonically independent of authored order");
            c.Equal(Haul, a.WorkerReserves[0].Role, "source input mutation cannot rewrite detached canonical settings");
            foreach (bool noncanonical in new[] { false, true })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); string profile = "alerts.neutral"; CompanyAlertLevel level = CompanyAlertLevel.Normal;
                writer.String(ref profile); writer.Enum(ref level); writer.Count(noncanonical ? 2 : 1);
                string role = "inspect"; int minimum = noncanonical ? 1 : -1; writer.String(ref role); writer.Int(ref minimum);
                if (noncanonical) { role = "haul"; writer.String(ref role); writer.Int(ref minimum); }
                var bytes = stream.ToArray(); c.Throws<CorruptStateException>(() => CompanyAlertSettings.Sync(new StateReader(bytes, 0, bytes.Length), a), "saved negative or noncanonical reserves cannot enter settings");
            }
            var scoped = Start(Fixture(edit: root => {
                var foreign = root.Get("entities")[2].Clone(); foreign.Set("id", JsonValue.Of("machine.foreign")); root.Get("entities").Add(foreign);
                StartupContentAuthoring.Add(root, "foreign", "machine.foreign", 1, foreign.Get("components").Get("point_presence")); }));
            c.Equal(1, Read(scoped).ReadyWorkers, "foreign actual machines never satisfy another company's floor");
            c.Equal(Code.None, scoped.Seat(1).View.Feature<CompanyAlertView>().TryReadReserve(new PlayerId(1), Inspect, out var own), "other human company can read its own actual readiness");
            c.Equal(1, own.ReadyWorkers, "each company's readiness is independently scoped"); c.Equal(0, own.MinimumReady, "explicit empty policy does not inherit another company's floor");
            Readiness(c); CrossRoleHaul(c);
        }
        private static void AtomicRestore(Check c, SimGame game)
        {
            var policy = game.World.Services.Get<ICompanyAlertPolicy>(); ulong before = game.Hash();
            foreach (bool unknownRole in new[] { false, true })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); writer.Count(2);
                for (int i = 0; i < 2; i++)
                {
                    PlayerId company = new PlayerId(i); writer.Player(ref company); var actual = policy.ReadAlert(company);
                    var invalid = new CompanyAlertSettings(unknownRole ? "alerts.neutral" : "missing", CompanyAlertLevel.Normal,
                        unknownRole ? new[] { new WorkerReserveSetting(new WorkerRoleId("missing"), 1) } : Array.Empty<WorkerReserveSetting>());
                    CompanyAlertSettings.Sync(writer, i == 0 ? Settings(game, 7) : invalid);
                    int version = actual.Version; long changed = actual.ChangedTick; writer.Int(ref version); writer.Long(ref changed);
                }
                var bytes = stream.ToArray(); c.Throws<CorruptStateException>(() => ((IPersistentService)policy).Sync(new StateReader(bytes, 0, bytes.Length)), "late profile/role failure cannot commit an earlier reserve row");
                c.Equal(before, game.Hash(), "whole company settings decode publishes no partial state");
            }
        }
        private static void Readiness(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true })
            {
                var game = Start(Fixture(publicPolicy, publicWorker)); var actor = Find(game, "machine"); var initial = WorkPolicyRules.Read(actor);
                foreach (string reason in new[] { "disabled", "emergency", "role" })
                {
                    c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = new WorkPolicySettings(
                        reason != "disabled", initial.Priority, reason == "role" ? Array.Empty<WorkerRoleId>() : initial.AllowedRoles,
                        null, initial.Interruptible, reason != "emergency", WorkAssignmentMode.All) }).Accepted, "player controls actual local readiness: " + reason);
                    c.Equal(0, Read(game).ReadyWorkers, "unready actor cannot satisfy reserve floor: " + reason);
                }
                c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = initial }).Accepted, "restore exact original role policy");
                c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.Zero).Accepted, "player makes actual execution rate zero");
                c.Equal(0, Read(game).ReadyWorkers, "zero effective work capacity never counts as ready");
                c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.One).Accepted, "player restores actual execution capacity");
                actor.Get<IWorker>().Held = true;
                try { c.Equal(0, Read(game).ReadyWorkers, "actual held worker never counts as freely dispatchable"); }
                finally { actor.Get<IWorker>().Held = false; }
                c.Equal(1, Read(game).ReadyWorkers, "current readiness recovers without a roster rebuild or time owner");
            }
            c.Throws<InvalidOperationException>(() => { var bad = Start(Fixture(publicPolicy: true, edit: root =>
                root.Get("entities")[2].Get("components").Get("proof_work_policy").Set("fault", JsonValue.Number("3")))); Read(bad); }, "impure public settings fail rather than inflate ready counts");
        }
        private static void CrossRoleHaul(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true })
            {
                var game = Start(Fixture(publicPolicy)); ulong before = game.Hash(); var hauls = game.World.Services.Get<IHaulExecution>();
                var preview = hauls.PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4));
                c.Equal(CompanyWorkerReserveCodes.MinimumReady, preview.Blocker, "ordinary hauling cannot borrow the last multi-role inspector");
                c.Equal(0, hauls.AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4), int.MaxValue), "fresh haul refuses before cargo/endpoints/worker claims");
                c.Equal(before, game.Hash(), "cross-role reserve refusal allocates no identities or claims");
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true })
            { Fleet(c, publicPolicy, publicWorker); Cancel(c, publicPolicy, publicWorker); Hauling(c, publicPolicy, publicWorker); }
            foreach (bool publicPolicy in new[] { false, true }) Supplied(c, publicPolicy);
        }
        private static void Fleet(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker, second: true); var game = Start(factory); var isolated = Start(factory); ulong untouched = isolated.Hash();
            c.Equal(2, Read(game).ReadyWorkers, "two actual workers meet one floor with one ordinary spare");
            var ordinary = Post(c, game); Until(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "ordinary spare earns actual paid work");
            c.Equal(1, Read(game).ReadyWorkers, "ordinary acquired worker leaves only actual reserve ready");
            var queued = Post(c, game, "reserve.other");
            var candidates = new List<WorkWorkerCandidate>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyWorkerCandidates(queued.Id, "work", candidates), "private candidates expose actual per-machine refusal");
            bool floor = false; foreach (var candidate in candidates) if (candidate.Result.Blocker == CompanyWorkerReserveCodes.MinimumReady) floor = true;
            c.That(floor, "actual idle candidate refuses floor even while first busy candidate has its own blocker");
            var response = Raise(c, game); c.Equal(0, Read(game).ReadyWorkers, "actual emergency consumes reserve, not a virtual additional machine");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long time = game.Clock.StepIndex;
            Step(game, twin); c.Equal(paused, game.Hash(), "sole game clock pause freezes acquired ordinary/emergency work"); c.Equal(time, game.Clock.StepIndex, "no reserve timer advances while paused");
            game.Clock.Resume(); twin.Clock.Resume(); Until(game, () => response.State == OrderState.Done && queued.State == OrderState.Done && ordinary.State == OrderState.Done, "physical ordinary/response/backlog completion under floor", twin);
            c.Equal(Fix.FromInt(4), Stock(game, "source"), "two actual ordinary jobs spend sixteen materials, response spends none");
            c.Equal(2, Read(game).ReadyWorkers, "released actual workers satisfy readiness again");
            c.Equal(game.Hash(), twin.Hash(), "interrupted saved multi-worker dispatch remains exact"); c.Equal(untouched, isolated.Hash(), "company reserve and worker membership stay session-local");
        }
        private static void Cancel(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker, second: true); var game = Start(factory); var ordinary = Post(c, game);
            Until(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "incumbent earns material consumption before floor edit"); var worker = Board(game).Job(ordinary.Node("work").JobId).Worker;
            var queued = Post(c, game, "reserve.other"); Configure(c, game, 2);
            c.Equal(worker, Board(game).Job(ordinary.Node("work").JobId).Worker, "floor edit never retrospectively takes incumbent worker");
            var twin = factory.Load(game.Save()); foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0,
                new CancelOrderCommand { Order = ordinary.Id }).Accepted, "player cancels retained actual work");
            c.Equal(2, Read(game).ReadyWorkers, "cancelled actual worker returns to current ready count");
            c.Equal(CompanyWorkerReserveCodes.MinimumReady, Board(game).Job(queued.Node("work").JobId).Blocker, "queued reacquisition still obeys edited floor");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "actual tool released without material refund");
            Configure(c, game, 0); Configure(c, twin, 0); Until(game, () => queued.State == OrderState.Done, "released policy allows existing queued physical work", twin);
            c.Equal(Fix.FromInt(4), Stock(game, "source"), "new work consumes actual second bill once"); c.Equal(game.Hash(), twin.Hash(), "floor edits/cancel/reacquire/save continuation stay exact");
        }
        private static void Hauling(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker, haulWorker: true); var game = Start(factory); Configure(c, game, 0, Haul);
            int job = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4), 1);
            c.That(job > 0, "ordinary player request acquires actual unprotected carrier"); var haul = Board(game).Job(job);
            Until(game, () => haul.Loaded.IsPositive, "actual loading before company floor edit"); var twin = factory.Load(game.Save());
            Configure(c, game, 1, Haul); Configure(c, twin, 1, Haul);
            c.Equal(job, Find(game, "machine").Get<IWorker>().Job, "loaded incumbent is not released or stranded by floor edit");
            Until(game, () => Stock(game, "destination") == Fix.FromInt(4) && Find(game, "machine").Get<IWorker>().Job == 0, "retained delivery then readiness", twin);
            c.Equal(1, Read(game, Haul).ReadyWorkers, "actual delivered carrier can satisfy edited haul floor");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded delivery remains exact across company policy edit");
        }
        private static void Supplied(Check c, bool publicPolicy)
        {
            var factory = Fixture(publicPolicy, supplied: true); var game = Start(factory); var ordinary = Post(c, game); var response = Raise(c, game);
            Until(game, () => Assigned(game)?.IsHaul == true, "actual emergency demand uses protected multi-role carrier"); var haul = Assigned(game);
            c.Equal(Fix.Zero, Stock(game, "destination"), "reserve does not teleport response stock");
            Until(game, () => haul.Loaded.IsPositive, "real emergency loading before saved continuation"); var twin = factory.Load(game.Save());
            Until(game, () => response.State == OrderState.Done, "physical emergency delivery then response work", twin);
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "actual emergency consumes eight inputs exactly once");
            c.Equal(Fix.Zero, Stock(game, "destination"), "real response consumes delivered stock");
            c.Equal(0L, ordinary.Node("work").Progress.WholeTicks, "ordinary maximum-priority backlog cannot consume reserve after response");
            c.Equal(1, Read(game).ReadyWorkers, "real released responder returns to company floor");
            c.Equal(game.Hash(), twin.Hash(), "saved emergency hauling/response remains deterministic");
        }
    }
}
