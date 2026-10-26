using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RecoveryDispatchTests
    {
        internal static void Register()
        {
            TestRunner.Add("Recovery dispatch: authored policy, private chronology and shared ranking", Admission);
            TestRunner.Add("Recovery dispatch: physical conservation, cleanup and saved contention", Physical);
            TestRunner.Add("Recovery dock egress: retained bay, physical capacity wait and saved continuation", DockRecoveryPhysical);
            TestRunner.Add("Recovery destination loss: retained cargo, missing dock and saved waiting", DestinationLossPhysical);
            TestRunner.Add("Haul recovery cost: admitted bay selection, retarget and saved cargo", RecoveryCost);
            TestRunner.Add("Dock demand recovery: physical departure, full-cargo wait and saved retarget", DockDemandWait);
            TestRunner.Add("Dock demand recovery: blocked egress preserves claims through saved retry", c => DockDemandWait(c, true));
        }
        private static string Text(JsonValue value) { var result = new StringBuilder(); value.WriteCanonical(result); return result.ToString(); }
        private static GameFactory Factory(string profile = "recovery.high", bool external = false, Action<JsonValue> edit = null, Action<JsonValue> policyEdit = null,
            bool returning = false, bool simultaneous = false, bool secondWorker = false, string boundCargo = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, string recoveryDestination = null, IReadOnlyList<string> startupProfiles = null)
        {
            var root = Json.Parse(PointGame.Definitions); var scenario = root.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2"));
            root.Set("work_policies", Json.Parse("[{\"id\":\"evacuation.worker\",\"enabled\":false,\"priority\":0,\"allowed_roles\":[\"inspect\",\"haul\"],\"area\":null,\"interruptible\":true,\"emergency_eligible\":true,\"assignment_mode\":\"all\",\"states\":{\"disabled\":\"blocked\",\"standby\":\"blocked\"}}]"));
            root.Get("entities")[2].Get("components").Set("work_policy", Json.Parse("{\"profile\":\"evacuation.worker\"}"));
            var source = root.Get("entities")[0].Get("components"); source.Remove("point_stock");
            source.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"evacuate\":true,\"accepts\":[\"supply\"],\"phases\":[],\"initial_stock\":{\"supply\":6}}"));
            if (returning)
            {
                root.Get("entities")[2].Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"supply\":6}"));
                if (!simultaneous)
                {
                    source.Get("storage").Set("initial_stock", JsonValue.NewObject());
                    source.Get("storage").Set("evacuate", JsonValue.Of(false));
                    source.Get("storage").Set("public", JsonValue.Of(false));
                }
            }
            if (boundCargo != null)
            {
                if (!external || !returning) throw new ArgumentException("bound cargo proof explicitly requires external return content");
                var machine = root.Get("entities")[2].Get("components");
                var separate = root.Get("entities")[0].Clone(); separate.Set("id", JsonValue.Of("cargo.container"));
                separate.Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"supply\":6}"));
                root.Get("entities").Add(separate);
                // Authored startup intent creates real stock; binding occurs after canonical settle.
                StartupContentAuthoring.Add(root, "cargo.container", "cargo.container", 0, separate.Get("components").Get("point_presence"));
                machine.Get("storage").Set("initial_stock", JsonValue.NewObject()); machine.Remove("transporter");
                machine.Set("proof_bound_cargo", Json.Parse("{\"cargo\":\"cargo.container\",\"cargo_replacements\":[],\"handling\":\"points.handling\",\"fault\":\"" + boundCargo + "\"}"));
            }
            if (secondWorker)
            {
                var other = root.Get("entities")[2].Clone(); other.Set("id", JsonValue.Of("other.machine"));
                other.Get("components").Get("storage").Set("initial_stock", JsonValue.NewObject()); root.Get("entities").Add(other);
                StartupContentAuthoring.Add(root, "other.machine", "other.machine", 0, other.Get("components").Get("point_presence"));
            }
            var modules = new List<GameModule>(); var documents = new Dictionary<string, string>();
            string declarations = "[]";
            if (profile != null)
            {
                modules.Add(new LogisticsRecoveryPolicyModule()); scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"" + (external ? "recovery.proof" : profile) + "\"}"));
                var policy = Json.Parse(FrameworkProofContent.Read("logistics-recovery.json")); policyEdit?.Invoke(policy);
                if (recoveryDestination != null) for (int i = 0; i < policy.Get("logistics_recovery_profiles").Count; i++)
                    policy.Get("logistics_recovery_profiles")[i].Get("cargo_return").Set("destination_policy", JsonValue.Of(recoveryDestination));
                documents.Add("logistics-recovery.json", Text(policy));
                declarations = "[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}";
                if (external)
                {
                    modules.Add(new LogisticsRecoveryPolicyProofModule()); scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyProofModule.ModuleId));
                    var proofPolicy = Json.Parse(FrameworkProofContent.Read("logistics-recovery-proof.json"));
                    if (recoveryDestination != null) proofPolicy.Get("proof_logistics_recovery_profiles")[0].Get("cargo_return").Set("destination_policy", JsonValue.Of(recoveryDestination));
                    documents.Add("logistics-recovery-proof.json", Text(proofPolicy));
                    declarations += ",{\"id\":\"proof.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery-proof.json\"]}";
                }
                declarations += "]";
            }
            edit?.Invoke(root);
            return PointGame.Factory(Text(root), extraModules: modules, extraDeclarations: declarations, extraDocuments: documents,
                additionalDocuments: additionalDocuments, startupProfiles: startupProfiles);
        }
        private static SimGame Start(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 172 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string prototype)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity; throw new InvalidOperationException(prototype); }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("evacuation update: " + game.Fault + "/" + twin?.Fault); }
        private static void Command(Check c, SimGame game, Command command)
        { c.That(TestKit.Order(game, 0, command).Accepted, "ordinary player command is accepted"); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Enable(Check c, SimGame game)
            => Command(c, game, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static List<WorkerDispatchOffer> Queue(SimGame game, int seat = 0)
        { var result = new List<WorkerDispatchOffer>(); game.Seat(seat).View.Feature<WorkView>().CopyAssignments(result); return result; }
        private static Job Job(SimGame game) => game.World.Services.Get<WorkBoard>().Job(Find(game, "machine").Get<IWorker>().Job);
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 360 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout at step " + game.Clock.StepIndex +
                ": position=" + Find(game, "machine").Get<PointPresence>().Position + ", job=" + Job(game)?.Id +
                ", phase=" + Job(game)?.ToilIndex + ", loaded=" + Job(game)?.Loaded +
                ", recovery=" + Job(game)?.RecoveryCause + ", state=" + Job(game)?.State + "/" + Job(game)?.Blocker);
        }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared evacuation semantics have direct identity");
            c.Throws<ContentException>(() => Start(Factory(edit: root => root.Get("scenarios")[0].Get("settings").Remove(LogisticsRecoveryPolicyModule.ModuleId))), "selected evacuation policy requires explicit scenario selection");
            foreach (string missing in new[] { "enabled", "priority", "attempts_per_pass" })
                c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("evacuation").Remove(missing))), "no implicit evacuation policy field");
            c.Throws<ContentException>(() => Start(Factory("missing.profile")), "unknown selected profile fails before game updates");
            c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("evacuation").Set("attempts_per_pass", JsonValue.Number("0")))), "zero acquisition budget refuses content");
            c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("evacuation").Set("priority", JsonValue.Number("-1")))), "negative authored priority refuses content");
            var absent = Start(Factory(null)); Enable(c, absent); for (int i = 0; i < 25; i++) Step(absent);
            c.That(Queue(absent).Count == 0 && Find(absent, "source").Get<IInventory>().Amount(Supply(absent)) == Fix.FromInt(6), "no selected capability means no legacy automatic evacuation");
            var off = Start(Factory("recovery.off")); Step(off); var offQueue = Queue(off);
            c.That(offQueue.Count == 1 && !offQueue[0].Ready && offQueue[0].Blocker == LogisticsService.EvacuationDisabled, "explicit disabled policy produces typed shared diagnostic");
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external: external); var game = Start(factory); Step(game);
                c.Equal(1, game.World.Services.Providers<IWorkerDispatchSource>().Count, "evacuation uses existing logistics source, not another dispatcher");
                var policy = game.World.Services.Get<ILogisticsRecoveryPolicy>();
                c.That(ReferenceEquals(policy, game.World.Content.Find<ILogisticsRecoveryPolicy>(policy.Id)), "selected native/public policy aliases immutable authored content");
                c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "one current logistics owner embeds recovery intent chronology");
                var pending = Queue(game); ulong hash = game.Hash();
                c.That(pending.Count == 1 && pending[0].Entry.StartsWith("evacuation.resource-", StringComparison.Ordinal) &&
                    pending[0].Identity == Find(game, "source").Id.Value && pending[0].Priority == 100 && pending[0].AttemptsPerPass == 2 && !pending[0].Ready,
                    "native/public authored policy publishes source-owned intent through the existing dispatch contract");
                c.Equal(0, Queue(game, 1).Count, "foreign company sees no evacuation stock or intent age");
                c.Equal(hash, game.Hash(), "private evacuation views create no claims, chronology or extra time");
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "unready original intent saves through LogisticsService's sole codec");
                c.Equal(pending[0].CreatedTick, Queue(twin)[0].CreatedTick, "saved original age is not rebased to the current update");
                Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "shared evacuation acquisition");
                c.That(Job(game).From == Find(game, "source").Id && Job(game).To == Find(game, "destination").Id && Job(game).Loaded == Fix.Zero,
                    "shared acquisition owns actual source, carrier and destination receipts before physical loading");
                c.Equal(Fix.FromInt(6), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "one actual destination promise, not copied evacuation quantities");
                ulong acquired = game.Hash();
                c.That(!game.World.Services.Get<IWorkerDispatchSource>().TryAcquire(pending[0].Identity, pending[0].Entry, Find(game, "machine").Id, WorkCandidateMode.IdleOnly), "stale offer cannot double-book the acquired carrier");
                c.Equal(acquired, game.Hash(), "stale evacuation acquisition refuses without mutations");
            }
            var blockedFactory = Factory(edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("40")));
            var blocked = Start(blockedFactory); Enable(c, blocked); Step(blocked);
            var diagnostics = new List<LogisticsDispatchOffer>(); blocked.Seat(0).View.Feature<LogisticsView>().CopyQueue(diagnostics);
            c.That(diagnostics.Count == 1 && diagnostics[0].Kind == LogisticsDispatchKind.Evacuation &&
                diagnostics[0].RecoveryOrigin == Find(blocked, "source").Id && diagnostics[0].Destination == EntityId.None && diagnostics[0].Haul.Status != HaulStatus.Ready,
                "full destinations expose actual origin and absence of a selected destination, never a invented fallback");
            long created = diagnostics[0].CreatedTick;
            var blockedTwin = blockedFactory.Load(blocked.Save());
            for (int i = 0; i < 25; i++) Step(blocked, blockedTwin);
            blocked.Seat(0).View.Feature<LogisticsView>().CopyQueue(diagnostics);
            c.That(diagnostics.Count == 1 && diagnostics[0].CreatedTick == created && diagnostics[0].AgeTicks > 0 && Job(blocked) == null,
                "periodic blocked retries retain original intent age without carrier or receipt duplication");
            c.Equal(Fix.FromInt(6), Find(blocked, "source").Get<IInventory>().Amount(Supply(blocked)), "blocked evacuation neither deletes stock nor instant-transfers to full output");
            c.Equal(blocked.Hash(), blockedTwin.Hash(), "saved output-blocked chronology continues through the same game clock");
            ReturnAdmission(c);
            BoundCargoAdmission(c);
            ReplanningAdmission(c);
            PartialReplanningAdmission(c);
        }
        private static void Physical(Check c)
        {
            foreach (var fixture in new[] { ("recovery.low", false), ("recovery.high", false), ("recovery.high", true) })
            {
                string profile = fixture.Item1;
                var factory = Factory(profile, external: fixture.Item2); var game = Start(factory); Step(game);
                Command(c, game, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect", Json.Parse("{\"target\":" + Find(game, "destination").Id.Value + "}"), "player", 50, false) });
                var before = Queue(game); c.That(before.Count == 2, "ordinary work and evacuation appear in the same arbitration projection");
                Enable(c, game); Reach(game, () => Job(game) != null, "cross-kind first assignment");
                c.That(Job(game).IsHaul == (profile == "recovery.high"), "authored evacuation priority competes with player work, not system update order");
                var origin = Find(game, "source"); var resource = Supply(game); var destination = Find(game, "destination").Get<IInventory>();
                Reach(game, () => Job(game)?.IsHaul == true && Job(game).Loaded.IsPositive, "actual evacuation loading");
                var load = Job(game); c.That(load.Loaded > Fix.Zero && destination.Amount(resource) == Fix.Zero, "cargo physically loads before delivery");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded evacuation saves native job/cargo/receipts with original intent age");
                Reach(game, () => destination.Amount(resource) == Fix.FromInt(6), "physical evacuation delivery", twin);
                Reach(game, () => !origin.Alive, "empty-container cleanup after live departure", twin);
                c.Equal(Fix.Zero, Find(game, "machine").Get<ITransporter>().Cargo.Amount(resource), "all six units arrived without residual cargo");
                c.Equal(game.Hash(), twin.Hash(), "physical departure and cleanup continue identically from the one saved game clock");
                c.That(Queue(game).TrueForAll(x => !x.Entry.StartsWith("evacuation.resource-", StringComparison.Ordinal)), "expired origin publishes no phantom recovery intent");
            }
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one direct current evacuation intent codec");
            // Provider protocol fixture: another module can promise incoming stock to an empty container.
            var protectedGame = Start(Factory(edit: root => root.Get("entities")[0].Get("components").Get("storage").Set("initial_stock", JsonValue.NewObject())));
            var empty = Find(protectedGame, "source"); var inlet = empty.Get<IInletPort>();
            var owner = new ReservationOwner("proof.empty-protection", 1); var token = inlet.ReserveIn(owner, Supply(protectedGame), Fix.One);
            for (int i = 0; i < 25; i++) Step(protectedGame);
            c.That(empty.Alive && inlet.Reservations.Count == 1, "empty cleanup respects another owner's actual removal guard, not only live haul jobs");
            inlet.ReleaseIn(owner, token);
            c.Equal(Code.None, protectedGame.World.CheckDespawn(empty), "release of actual inlet promise clears the canonical removal blocker");
            Reach(protectedGame, () => !empty.Alive, "empty cleanup after external reservation release");
            c.That(!empty.Alive, "normal maintenance cleanup removes only genuinely unreferenced empty container");
            ReturnPhysical(c);
            BoundCargoPhysical(c);
            ReplanningPhysical(c);
            PartialReplanningPhysical(c);
        }

        private static void BoundCargoAdmission(Check c)
        {
            foreach (string fault in new[] { "mutate-null", "mutate-state" })
            {
                var game = Start(Factory(external: true, returning: true, boundCargo: fault));
                long time = game.Clock.StepIndex;
                c.Throws<InvalidOperationException>(() => game.World.Services.Get<IHaulExecution>().PreviewReturnCargo(Find(game, "machine").Id, Supply(game)),
                    "first cargo getter is guarded, including null result and untouching persisted mutation");
                c.That(game.Fault != null && game.Clock.StepIndex == time, "invalid provider latches fault without advancing the sole game clock");
            }
            var factory = Factory(external: true, returning: true, boundCargo: "none"); var actual = Start(factory);
            var worker = Find(actual, "machine"); var container = Find(actual, "cargo.container"); var resource = Supply(actual);
            var cargo = worker.Get<ITransporter>().Cargo;
            c.That(ReferenceEquals(cargo, container.Get<IInventory>()) && !ReferenceEquals(cargo, worker.Get<IInventory>()),
                "public transporter exposes the actual separate owned inventory, not worker storage or a mirror");
            Command(c, actual, new ConfigureStockPolicyCommand { Store = container.Id, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(40), 0, LogisticsReserveAccess.Protected)) });
            Step(actual); var rows = new List<LogisticsDispatchOffer>(); actual.Seat(0).View.Feature<LogisticsView>().CopyQueue(rows);
            ulong hash = actual.Hash(); var disposition = actual.World.Services.Get<LogisticsService>().ReadCargoDisposition(worker.Id);
            c.That(rows.Count == 1 && rows[0].RecoveryOrigin == worker.Id && rows[0].Requested == Fix.FromInt(4) &&
                disposition.Endpoint == cargo.Endpoint && disposition.Retained.AmountOf(resource) == Fix.FromInt(2),
                "separate cargo policies protect stock while shared intent keeps worker identity");
            c.That(Queue(actual, 1).Count == 0 && actual.Hash() == hash, "separate inventory observations remain private and pure");
            var twin = factory.Load(actual.Save());
            c.That(twin.Hash() == hash && ReferenceEquals(Find(twin, "machine").Get<ITransporter>().Cargo, Find(twin, "cargo.container").Get<IInventory>()),
                "loaded provider binds its own session inventory without rebasing intent or borrowing another session");
            Enable(c, actual); Reach(actual, () => Job(actual)?.IsHaul == true, "bound cargo acquisition");
            c.That(Job(actual).Worker == worker.Id && Job(actual).CargoOutReservation.Endpoint == container.Get<IInventory>().Endpoint &&
                Job(actual).From == EntityId.None && worker.Get<IInventory>().Reservations.Count == 0,
                "acquisition binds the actual separate cargo-out endpoint and actual worker, without native worker-stock promises");
        }
        private static void BoundCargoPhysical(Check c)
        {
            foreach (int keep in new[] { 0, 2 })
            {
                var factory = Factory(external: true, returning: true, boundCargo: "none"); var game = Start(factory);
                var worker = Find(game, "machine"); var container = Find(game, "cargo.container"); var resource = Supply(game);
                if (keep != 0) Command(c, game, new ConfigureStockPolicyCommand { Store = container.Id, Resource = resource,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(keep), Fix.FromInt(keep), Fix.FromInt(40), 0, LogisticsReserveAccess.Protected)) });
                Step(game); Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "bound actual return");
                var output = Find(game, "destination").Get<IInventory>(); long start = game.Clock.StepIndex;
                c.That(output.Amount(resource) == Fix.Zero && Job(game).Loaded == Fix.FromInt(6 - keep), "separate cargo assignment is loaded, not instant delivery");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "separate loaded cargo saves through actual endpoints/job/provider codecs");
                Reach(game, () => output.Amount(resource) == Fix.FromInt(6 - keep) && Job(game) == null, "bound cargo timed unloading/departure", twin);
                c.That(game.Clock.StepIndex > start && container.Get<IInventory>().Amount(resource) == Fix.FromInt(keep) &&
                    worker.Get<IInventory>().Amount(resource) == Fix.Zero && container.Get<IInventory>().Reservations.Count == 0,
                    "actual separate cargo is conserved over time; worker inventory never substitutes for it");
                c.That(Queue(game).Count == 0 && game.Hash() == twin.Hash(), "bound cargo saved twins finish and retained stock creates no phantom return");
            }
        }
        private static GameFactory ReplanningFactory(bool external = false, string profile = "recovery.high", bool returning = true, bool blocked = false, bool docked = false, bool futureDocked = false)
            => Factory(profile, external: external, returning: returning, boundCargo: external && returning ? "none" : null,
                policyEdit: blocked ? root => { foreach (string kind in new[] { "evacuation", "cargo_return" }) root.Get("logistics_recovery_profiles")[1].Get(kind).Set("reroute_blockers", JsonValue.NewArray()); } : null,
                edit: root =>
                {
                    var output = root.Get("entities")[1].Clone(); output.Set("id", JsonValue.Of("alternate"));
                    output.Get("components").Get("point_presence").Set("position", JsonValue.Number("3"));
                    output.Get("components").Get("point_stock").Set("initial", JsonValue.Number("40"));
                    if (futureDocked) output.Get("components").Set("point_dock",
                        Json.Parse(FrameworkProofContent.Read("point-docks.json")).Get("entities")[1].Get("components").Get("point_dock").Clone());
                    root.Get("entities").Add(output);
                    StartupContentAuthoring.Add(root, "alternate", "alternate", 0, output.Get("components").Get("point_presence"));
                    root.Set("point_interval_regions", Json.Parse("[{\"id\":\"recovery.area\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":5}]"));
                    if (docked) root.Get("entities")[1].Get("components").Set("point_dock",
                        Json.Parse(FrameworkProofContent.Read("point-docks.json")).Get("entities")[1].Get("components").Get("point_dock").Clone());
                });
        private static void Area(Check c, SimGame game)
            => Command(c, game, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "recovery.area", true, true, WorkAssignmentMode.All) });
        private static void FreeAlternate(SimGame game)
        {
            // Actual endpoint protocol instrumentation: another engine process consumes its existing stock.
            Find(game, "alternate").Get<IInventory>().Take(Supply(game), Fix.FromInt(40));
        }
        private static void ReplanningAdmission(Check c)
        {
            c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("cargo_return").Remove("reroute_blockers"))), "reroute policy is explicit, never a fallback default");
            c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("cargo_return").Set("reroute_blockers", Json.Parse("[\"core.not-found\",\"core.not-found\"]")))), "duplicate blocker identities refuse content");
            foreach (string invalid in new[] { "null", "true", "[1]", "[\" \"]" })
                c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get("cargo_return").Set("reroute_blockers", Json.Parse(invalid)))), "reroute blockers reject null/wrong type/empty identity instead of implicit policy");
            var game = Start(ReplanningFactory()); Step(game); Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "loaded replan admission");
            Job job = Job(game); ulong hash = game.Hash(); var cargoPromise = job.CargoOutReservation; var destinationPromise = job.InReservation;
            c.That(job.RecoveryKind == LogisticsRecoveryKind.CargoReturn && !game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, WorkPolicyCodes.OutsideArea), "full alternative cannot consume existing cargo or replace promises");
            c.That(hash == game.Hash() && job.CargoOutReservation.Equals(cargoPromise) && job.InReservation.Equals(destinationPromise), "failed replan leaves sole job/claims/history/time unchanged");
            game = Start(ReplanningFactory(blocked: true)); Step(game); Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "disabled reroute policy");
            job = Job(game); FreeAlternate(game); Area(c, game);
            c.That(job.To == Find(game, "destination").Id && Job(game)?.Id == job.Id && job.State == JobState.Paused && job.Blocker == WorkPolicyCodes.OutsideArea,
                "explicit empty blocker policy retains original target and actual loaded promises");
            game = Start(ReplanningFactory(profile: "recovery.off", returning: false)); Enable(c, game);
            int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(6), 100);
            job = game.World.Services.Get<WorkBoard>().Job(id); Reach(game, () => job.Loaded == job.Amount, "fixed manual pickup");
            FreeAlternate(game); Area(c, game);
            c.That(job.RecoveryKind == null && job.To == Find(game, "destination").Id && job.State == JobState.Paused,
                "fixed player haul does not inherit recovery intent or silently reroute");
        }
        private static void ReplanningPhysical(Check c)
        {
            foreach (var fixture in new[] { (false, true), (true, true), (false, false) })
            {
                var factory = ReplanningFactory(fixture.Item1, returning: fixture.Item2); var game = Start(factory); Step(game);
                Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true && Job(game).Loaded == Job(game).Amount, "fully loaded recovery before replan");
                Job job = Job(game); Entity worker = Find(game, "machine"); var resource = Supply(game);
                var cargo = worker.Get<ITransporter>().Cargo; var held = job.CargoOutReservation;
                int id = job.Id; long created = job.CreatedTick, time = game.Clock.StepIndex;
                var oldOutput = Find(game, "destination").Get<IInventory>(); var newOutput = Find(game, "alternate").Get<IInventory>();
                FreeAlternate(game); Area(c, game);
                c.That(Job(game)?.Id == id && job.To == newOutput.Entity.Id && job.Target == newOutput.Entity.Id && job.Worker == worker.Id,
                    "authored invalid-route recovery retargets the existing job to a feasible actual output");
                c.That(job.CargoOutReservation.Equals(held) && cargo.CheckReservedOut(job.ReservationOwner, held).Ready &&
                    oldOutput.ReservedIn(resource) == Fix.Zero && newOutput.ReservedIn(resource) == job.Loaded,
                    "new complete room is promised before old room is withdrawn; actual loaded cargo promise is unchanged");
                c.That(job.CreatedTick == created && newOutput.Amount(resource) == Fix.Zero && cargo.Amount(resource) == Fix.FromInt(6),
                    "replan preserves original chronology and actual stock, never instant delivery");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "retargeted destination itinerary/claims/provenance save in existing codecs");
                var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts);
                c.That(facts.Exists(fact => fact.Id == id && fact.Target == newOutput.Entity.Id && fact.RecoveryKind == job.RecoveryKind),
                    "immutable current-job diagnostics expose exact retarget/provenance without a duplicate route model");
                Reach(game, () => newOutput.Amount(resource) == Fix.FromInt(6) && Job(game) == null, "actual replanned travel unloading departure", twin);
                c.That(game.Clock.StepIndex > time && oldOutput.Amount(resource) == Fix.Zero && cargo.Amount(resource) == Fix.Zero &&
                    newOutput.Reservations.Count == 0 && game.Hash() == twin.Hash(), "native/public return and loaded evacuation continue physically after replan without duplicated stock or time");
            }
        }
        private static void PartialReplanningAdmission(Check c)
        {
            foreach (bool docked in new[] { false, true })
            {
                var game = Start(ReplanningFactory(docked: docked)); Enable(c, game);
                var output = Find(game, "destination").Get<IInventory>(); var resource = Supply(game);
                Reach(game, () => output.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "partial recovery refusal");
                Job job = Job(game); var held = job.CargoOutReservation; var inlet = job.InReservation;
                var progress = job.HandlingProgress; var slot = job.DestinationDockSlot;
                if (docked) FreeAlternate(game);
                ulong hash = game.Hash();
                // Explicit engine recovery-failure protocol injection; this is not a player command or host rule.
                c.That(!game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, docked ? HaulHandlingCodes.Invalid : Codes.NotFound),
                    docked ? "unlisted cause cannot free a partial occupied dock under the carrier" : "partial delivery cannot move to a full alternative");
                c.That(hash == game.Hash() && job.CargoOutReservation.Equals(held) && job.InReservation.Equals(inlet) &&
                    job.HandlingProgress.Equals(progress) && job.UnloadingBaseline.IsZero && job.DestinationDockSlot == slot,
                    "refused partial retarget preserves actual progress, stock, claims, geometry and time");
                if (!docked)
                {
                    job.UnloadingBaseline = job.Amount;
                    c.Throws<InvalidOperationException>(() => game.Hash(), "invalid current-leg prefix faults instead of granting fabricated handling");
                    c.Throws<InvalidOperationException>(() => game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, Codes.NotFound),
                        "retarget validates actual current prefix before changing any room or itinerary");
                    job.UnloadingBaseline = Fix.Zero;
                    c.Equal(hash, game.Hash(), "restored original prefix retains the exact checkpoint");
                }
            }
            var future = Start(ReplanningFactory(futureDocked: true)); Enable(c, future);
            var original = Find(future, "destination").Get<IInventory>(); var supply = Supply(future);
            Reach(future, () => original.Amount(supply).IsPositive && Job(future)?.Loaded.IsPositive == true, "future dock partial origin");
            Job retained = Job(future); FreeAlternate(future);
            c.That(future.World.Services.Get<IHaulExecution>().TryReplanDestination(future.World, retained, Codes.NotFound), "non-docked partial origin can select a future physical dock");
            Fix baseline = retained.UnloadingBaseline;
            c.That(future.World.Services.Get<IHaulExecution>().TryReplanDestination(future.World, retained, Codes.NotFound) &&
                retained.To == original.Entity.Id && retained.UnloadingBaseline == baseline && Find(future, "alternate").Get<IInventory>().ReservedIn(supply).IsZero,
                "an unoccupied future dock intention can change again without losing the retained delivery prefix");
        }
        private static void PartialReplanningPhysical(Check c)
        {
            foreach (var fixture in new[] { (false, true), (true, true), (false, false) })
            {
                var factory = ReplanningFactory(fixture.Item1, returning: fixture.Item2); var game = Start(factory); Enable(c, game);
                Entity worker = Find(game, "machine"); var resource = Supply(game);
                var oldOutput = Find(game, "destination").Get<IInventory>(); var nextOutput = Find(game, "alternate").Get<IInventory>();
                Reach(game, () => oldOutput.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "original partial unloading");
                Job job = Job(game); int id = job.Id; long created = job.CreatedTick;
                var held = job.CargoOutReservation; var cargo = worker.Get<ITransporter>().Cargo;
                Step(game); // One additional real update earns unfinished handling, never transferable to a new output.
                Fix delivered = job.Amount - job.Loaded, remaining = job.Loaded;
                c.That(delivered.IsPositive && remaining.IsPositive && !job.HandlingProgress.Equals(WorkProgress.Zero), "actual partial service has earned prefix and elapsed work");
                FreeAlternate(game);
                // Explicit failure protocol injection through the existing engine owner; all progression stays in Game.Update.
                c.That(game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, Codes.NotFound), "non-docked recovery can retarget its actual remaining cargo");
                c.That(job.Id == id && job.Worker == worker.Id && job.CreatedTick == created && job.Amount == Fix.FromInt(6) &&
                    job.UnloadingBaseline == delivered && job.Loaded == remaining && job.CargoOutReservation.Equals(held) &&
                    job.HandlingProgress.Equals(WorkProgress.Zero) && job.ToilIndex == (job.From.IsValid ? 2 : 0),
                    "same shipment and cargo promise retain committed prefix, not unfinished work at the previous output");
                c.That(oldOutput.Amount(resource) == delivered && oldOutput.ReservedIn(resource).IsZero && nextOutput.Amount(resource).IsZero &&
                    nextOutput.ReservedIn(resource) == remaining && cargo.Amount(resource) == remaining,
                    "retarget promises only remaining room and never moves or duplicates already delivered stock");
                long required = worker.Get<ITransporter>().Handling.CheckHandling(game.World.Time, resource, remaining, ResourceHandlingPhase.Unloading).RequiredTicks;
                var facts = new List<WorkJobView>(); ulong hash = game.Hash(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts);
                WorkJobView retained = facts.Find(fact => fact.Id == id);
                c.That(game.Hash() == hash && retained.UnloadingBaseline == delivered && retained.HandledAmount == delivered &&
                    retained.HandlingRemaining == remaining && retained.HandlingRequiredTicks == required, "private immutable diagnostics use actual remaining-leg duration without query mutation");
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "partial retarget prefix and actual remaining promises save through existing codecs");
                long start = game.Clock.StepIndex;
                Reach(game, () => nextOutput.Amount(resource).IsPositive && job.Loaded.IsPositive, "new output timed service", twin);
                c.That(game.Clock.StepIndex > start && oldOutput.Amount(resource) == delivered && nextOutput.Amount(resource) + cargo.Amount(resource) == remaining,
                    "new travel and unloading consume real game time and conserve all remaining material");
                c.That(retained.UnloadingBaseline == delivered && retained.Loaded == remaining, "retained host snapshot stays detached during updates");
                // Repeat on the same job after another actual delivered prefix, including saved service state.
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mid-leg partial service also survives save/load");
                Fix cumulative = job.Amount - job.Loaded;
                c.That(game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, Codes.NotFound), "same job supports a second partial recovery leg");
                Job other = Job(twin);
                c.That(twin.World.Services.Get<IHaulExecution>().TryReplanDestination(twin.World, other, Codes.NotFound), "saved twin follows the same second recovery decision");
                c.That(job.To == oldOutput.Entity.Id && job.UnloadingBaseline == cumulative && job.CargoOutReservation.Equals(held) &&
                    game.Hash() == twin.Hash(), "repeated recovery preserves cumulative committed prefix without a second shipment or clock");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "repeated retarget checkpoint persists one baseline");
                Reach(game, () => Job(game) == null && oldOutput.Amount(resource) + nextOutput.Amount(resource) == Fix.FromInt(6), "complete repeated recovery", twin);
                c.That(cargo.Amount(resource).IsZero && oldOutput.Reservations.Count == 0 && nextOutput.Reservations.Count == 0 &&
                    job.UnloadingBaseline.IsZero && game.Hash() == twin.Hash(), "all six units physically finish once; cleanup clears only this job's claims and baseline");
            }
            var cancelledFactory = ReplanningFactory(); var cancelled = Start(cancelledFactory); Enable(c, cancelled);
            var final = Find(cancelled, "destination").Get<IInventory>(); var supply = Supply(cancelled);
            Reach(cancelled, () => final.Amount(supply).IsPositive && Job(cancelled)?.Loaded.IsPositive == true, "cancellation partial service");
            Job interrupted = Job(cancelled); FreeAlternate(cancelled);
            c.That(cancelled.World.Services.Get<IHaulExecution>().TryReplanDestination(cancelled.World, interrupted, Codes.NotFound), "partial cancellation fixture owns a real new leg");
            Fix cargoBefore = Find(cancelled, "machine").Get<ITransporter>().Cargo.Amount(supply);
            c.Equal(Code.None, cancelled.World.System<WorkSystem>().Interrupt(cancelled.World, interrupted), "existing interruption cancels the retained leg");
            c.That(interrupted.UnloadingBaseline.IsZero && Find(cancelled, "alternate").Get<IInventory>().Reservations.Count == 0 &&
                Find(cancelled, "machine").Get<ITransporter>().Cargo.Amount(supply) == cargoBefore, "cancellation resets progress and releases room without destroying remaining cargo");
            var cancelledTwin = cancelledFactory.Load(cancelled.Save()); c.Equal(cancelled.Hash(), cancelledTwin.Hash(), "cancelled partial retarget saves cleanly");
            Reach(cancelled, () => Job(cancelled) == null && final.Amount(supply) + Find(cancelled, "alternate").Get<IInventory>().Amount(supply) == Fix.FromInt(6), "residual recovery after cancellation", cancelledTwin);
            c.Equal(cancelled.Hash(), cancelledTwin.Hash(), "normal recovery dispatcher physically finishes cancelled residual cargo in saved twins");
        }
        private static GameFactory DockRecoveryFactory(bool external = false, bool returning = true, bool demandOnly = false)
            => Factory(external: external, returning: returning, secondWorker: true, boundCargo: external && returning ? "none" : null,
                recoveryDestination: demandOnly ? "recovery.demand-only" : null,
                additionalDocuments: demandOnly ? new Dictionary<string, string> { ["dock-demand-recovery.json"] = FrameworkProofContent.Read("dock-demand-recovery.json") } : null,
                edit: root =>
                {
                    root.Get("entities")[2].Get("components").Get("point_motion").Set("rate", JsonValue.Number("30"));
                    var dock = Json.Parse(FrameworkProofContent.Read("point-docks.json")).Get("entities")[1].Get("components").Get("point_dock").Clone();
                    var slots = JsonValue.NewArray();
                    for (int i = 0; i < dock.Get("slots").Count; i++)
                        if (dock.Get("slots")[i].Get("kind").Text != "waiting") slots.Add(dock.Get("slots")[i].Clone());
                    dock.Set("slots", slots); root.Get("entities")[1].Get("components").Set("point_dock", dock);
                    var alternate = root.Get("entities")[1].Clone(); alternate.Set("id", JsonValue.Of("alternate"));
                    alternate.Get("components").Get("point_presence").Set("position", JsonValue.Number("25"));
                    alternate.Get("components").Get("point_stock").Set("initial", JsonValue.Number("0")); root.Get("entities").Add(alternate);
                    StartupContentAuthoring.Add(root, "alternate", "alternate", 0, alternate.Get("components").Get("point_presence"));
                    root.Set("point_interval_regions", Json.Parse("[{\"id\":\"recovery.egress\",\"space\":\"field.points\",\"minimum\":11,\"maximum\":40}]"));
                    for (int i = 0; i < root.Get("entities").Count; i++)
                        if (root.Get("entities")[i].Get("id").Text == "other.machine")
                        {
                            var other = root.Get("entities")[i].Get("components");
                            StartupContentAuthoring.Position(root, "other.machine", JsonValue.Number("40"));
                            // The idle probe owns its own native empty cargo, never the public fixture's shared container.
                            other.Remove("proof_bound_cargo"); other.Set("transporter", Json.Parse("{\"handling\":\"points.handling\"}"));
                        }
                });
        private static WorkJobView ReadJob(SimGame game, int id)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); return facts.Find(fact => fact.Id == id); }
        private static void DockDemandWait(Check c) => DockDemandWait(c, false);
        private static void DockDemandWait(Check c, bool interruptExit)
        {
            // Native destination policy with native/public separate-inventory cargo. No direct reroute signal.
            foreach (bool external in new[] { false, true })
            {
                var factory = DockRecoveryFactory(external, demandOnly: true); var game = Start(factory);
                void Demand(SimGame session, string endpoint, int amount) => Command(c, session, new ConfigureStockPolicyCommand
                { Store = Find(session, endpoint).Id, Resource = Supply(session), Change = LogisticsDemandChange.Replace(
                    new LogisticsDemandSettings(true, Fix.FromInt(amount), Fix.FromInt(amount), Fix.FromInt(40), 100, LogisticsReserveAccess.Protected)) });
                Demand(game, "destination", 6); Enable(c, game);
                Entity worker = Find(game, "machine"), endpoint = Find(game, "destination");
                IInventory original = endpoint.Get<IInventory>(), alternate = Find(game, "alternate").Get<IInventory>(), cargo = worker.Get<ITransporter>().Cargo;
                ResourceId resource = Supply(game);
                Reach(game, () => original.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "actual partial dock unloading");
                Job job = Job(game); int id = job.Id; long created = job.CreatedTick;
                Fix delivered = original.Amount(resource), remaining = job.Loaded;
                ReservationToken held = job.CargoOutReservation, room = job.InReservation;
                Demand(game, "alternate", 1);
                // Demand command can earn another real unload prefix before policy is applied.
                delivered = original.Amount(resource); remaining = job.Loaded; held = job.CargoOutReservation; room = job.InReservation;
                c.That(remaining > Fix.One, "alternative demand1 is smaller than actual remaining commitment");
                var twin = factory.Load(game.Save());
                void Area(SimGame session) => Command(c, session, new ConfigureWorkPolicyCommand { Worker = Find(session, "machine").Id,
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "recovery.egress", true, true, WorkAssignmentMode.All) });
                Area(game); Area(twin);
                Reach(game, () => !job.RecoveryCause.IsNone, "normal policy starts departure without a complete destination", twin);
                c.That(job.Id == id && job.To == endpoint.Id && job.Loaded == remaining && job.CargoOutReservation.Equals(held) &&
                    job.InReservation.Equals(room) && alternate.ReservedIn(resource).IsZero, "same shipment departs without promising incompatible future capacity");
                twin = factory.Load(game.Save());
                if (interruptExit)
                {
                    Reach(game, () => worker.Get<PointPresence>().Position > Fix.FromInt(11) && worker.Get<PointPresence>().Position < Fix.FromInt(12),
                        "demand-only carrier physically travels between retained bay and exit", twin);
                    Code bay = job.RecoveryDockSlot; WorkProgress progress = job.HandlingProgress;
                    Command(c, game, new SetPointAccessCommand { Open = false });
                    Command(c, twin, new SetPointAccessCommand { Open = false });
                    Fix position = worker.Get<PointPresence>().Position;
                    c.That(position > Fix.FromInt(11) && position < Fix.FromInt(12) && !bay.IsNone,
                        "route closure occurs during actual departure, before exit or bay release");
                    long closedAt = game.Clock.StepIndex;
                    for (int i = 0; i < 4; i++) Step(game, twin);
                    c.That(game.Clock.StepIndex > closedAt && worker.Get<PointPresence>().Position == position &&
                        job.RecoveryDockSlot == bay && job.Loaded == remaining && job.HandlingProgress.Equals(progress) && !job.Blocker.IsNone,
                        "one clock advances while blocked physical egress retains bay, cargo and unfinished service");
                    c.That(job.Id == id && job.CreatedTick == created && job.To == endpoint.Id && job.CargoOutReservation.Equals(held) &&
                        job.InReservation.Equals(room) && cargo.CheckReservedOut(job.ReservationOwner, held).Ready &&
                        original.ReservedIn(resource) == remaining && alternate.ReservedIn(resource).IsZero && game.Hash() == twin.Hash(),
                        "blocked demand-only departure preserves exact real claims and original chronology in interleaved sessions");
                    twin = factory.Load(game.Save());
                    c.Equal(game.Hash(), twin.Hash(), "blocked departure checkpoint restores the same motion, job and resource owners");
                    for (int i = 0; i < 3; i++) Step(game, twin);
                    c.That(worker.Get<PointPresence>().Position == position && job.RecoveryDockSlot == bay &&
                        original.Amount(resource) == delivered && cargo.Amount(resource) == remaining && alternate.Amount(resource).IsZero && game.Hash() == twin.Hash(),
                        "restored blocked route cannot unload, teleport, clear claims or fabricate receiving demand");
                    Command(c, game, new SetPointAccessCommand { Open = true });
                    Command(c, twin, new SetPointAccessCommand { Open = true });
                }
                Reach(game, () => job.RecoveryDockSlot.IsNone && ReadJob(game, id).RecoveryStage == HaulRecoveryStage.WaitingForDestination,
                    "actual exit reached and engine clears retained bay before destination becomes ready", twin);
                c.That(job.RecoveryDockSlot.IsNone && worker.Get<PointPresence>().Position == Fix.FromInt(12), "physical departure clears old bay only at actual exit");
                c.That(original.Amount(resource) == delivered && cargo.Amount(resource) == remaining && alternate.Amount(resource).IsZero &&
                    alternate.ReservedIn(resource).IsZero && original.ReservedIn(resource) == remaining, "insufficient destination demand cannot split or teleport committed cargo");
                c.That(job.Id == id && job.CreatedTick == created && job.CargoOutReservation.Equals(held), "exit wait preserves actual identity, age and cargo receipt");
                twin = factory.Load(game.Save()); for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(game.Hash() == twin.Hash() && cargo.Amount(resource) == remaining, "saved capacity wait progresses neither unloading nor cargo");
                Demand(game, "alternate", 6); Demand(twin, "alternate", 6);
                Reach(game, () => job.To == Find(game, "alternate").Id, "normal Update retargets after full compatible demand", twin);
                c.That(job.Id == id && job.CargoOutReservation.Equals(held) && original.ReservedIn(resource).IsZero &&
                    alternate.ReservedIn(resource) == remaining, "same engine job replaces only actual inlet room after departure");
                twin = factory.Load(game.Save());
                Reach(game, () => Job(game) == null && alternate.Amount(resource) == remaining, "saved actual retargeted travel/unload finishes", twin);
                c.That(original.Amount(resource) + alternate.Amount(resource) == Fix.FromInt(6) && cargo.Amount(resource).IsZero &&
                    cargo.Reservations.Count == 0 && alternate.Reservations.Count == 0 && game.Hash() == twin.Hash(), "native/public full-cargo recovery conserves six and clears actual claims");
            }
        }
        private static void DockRecoveryPhysical(Check c)
        {
            foreach (var fixture in new[] { (false, true), (true, true), (false, false) })
            {
                var factory = DockRecoveryFactory(fixture.Item1, fixture.Item2); var game = Start(factory); Enable(c, game);
                Entity worker = Find(game, "machine"), oldEndpoint = Find(game, "destination");
                IInventory original = oldEndpoint.Get<IInventory>(), next = Find(game, "alternate").Get<IInventory>();
                var resource = Supply(game); IInventory cargo = worker.Get<ITransporter>().Cargo;
                Reach(game, () => original.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "docked partial delivery");
                Step(game); Job job = Job(game); int id = job.Id; long created = job.CreatedTick;
                Fix delivered = original.Amount(resource), remaining = job.Loaded;
                WorkProgress progress = job.HandlingProgress; var held = job.CargoOutReservation; var room = job.InReservation;
                Code bay = job.DestinationDockSlot;
                c.Equal(Codes.InvalidArgument, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, worker, worker.Owner,
                    job.Role, Array.Empty<Entity>(), false, job.Id), "ordinary incumbent service cannot bypass destination admission with empty stops");
                Command(c, game, new ConfigureWorkPolicyCommand { Worker = worker.Id,
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "recovery.egress", true, true, WorkAssignmentMode.All) });
                Reach(game, () => !job.RecoveryCause.IsNone, "normal player policy starts physical recovery departure");
                c.That(job.RecoveryCause == WorkPolicyCodes.OutsideArea && job.RecoveryDockSlot == bay && job.To == oldEndpoint.Id &&
                    job.ToilIndex == job.Toils.Count - 1 && job.Loaded == remaining && job.HandlingProgress.Equals(progress),
                    "same actual job leaves the old bay without retargeting or discarding partial service");
                c.Equal(Code.None, game.World.Services.Get<IWorkerDispatch>().CheckJob(game.World, worker, job),
                    "only the actual loaded departure may use exit geometry instead of the invalid old service endpoint");
                c.That(original.ReservedIn(resource) == remaining && next.ReservedIn(resource).IsZero &&
                    job.CargoOutReservation.Equals(held) && job.InReservation.Equals(room), "old inlet/cargo promises survive departure; future output capacity is not fabricated or prematurely promised");
                ulong hash = game.Hash(); var retained = ReadJob(game, id);
                c.That(retained.RecoveryStage == HaulRecoveryStage.Departing && retained.RecoveryDockSlot == bay &&
                    retained.RecoveryCause == WorkPolicyCodes.OutsideArea && game.Hash() == hash, "private detached recovery diagnostics read actual departure without mutation");
                var foreign = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(foreign);
                c.That(!foreign.Exists(fact => fact.Id == id), "another company does not see retained recovery/bay details");
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "loaded departure and both physical claims save through the existing board codec");
                Reach(game, () => worker.Get<PointPresence>().Position > Fix.FromInt(11) && worker.Get<PointPresence>().Position < Fix.FromInt(12), "actual movement between bay and exit", twin);
                c.That(job.RecoveryDockSlot == bay && job.To == oldEndpoint.Id && cargo.Amount(resource) == remaining &&
                    original.Amount(resource) == delivered, "bay remains claimed while actual vehicle is still departing; no instantaneous cargo movement");
                if (job.RecoveryDockSlot != bay || job.To != oldEndpoint.Id)
                    throw new InvalidOperationException("egress transition: retained=" + job.Id + "/" + job.State + "/" + job.Blocker +
                        ", cause=" + job.RecoveryCause + ", oldBay=" + bay + ", bay=" + job.RecoveryDockSlot +
                        ", slot=" + job.DestinationDockSlot + ", target=" + job.To + ", actual=" + Job(game)?.Id +
                        ", position=" + worker.Get<PointPresence>().Position + ", goal=" + worker.Get<PointMotion>().Goal);
                // Pure existing dock protocol probe, not a new player assignment or host scheduler.
                Code blocked = game.World.Services.Get<WorkBoard>().PreviewDock(oldEndpoint, Find(game, "other.machine"),
                    WorkDockRequest.Haul(resource, ResourceHandlingPhase.Loading), null, out _, out _, out _, out _);
                c.That(blocked == WorkDockCodes.Full, "another physical pickup cannot claim the retained old bay even after the vehicle has moved off its center");
                // A normal engine command closes route access after departure begins; no test-owned update loop.
                Command(c, game, new SetPointAccessCommand { Open = false }); Command(c, twin, new SetPointAccessCommand { Open = false });
                Fix position = worker.Get<PointPresence>().Position;
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(worker.Get<PointPresence>().Position == position && job.RecoveryDockSlot == bay && job.Loaded == remaining &&
                    !job.Blocker.IsNone && job.HandlingProgress.Equals(progress), "changed route access physically pauses egress with bay/cargo/progress retained");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "blocked physical departure also saves without releasing the bay");
                // Existing endpoint protocol instrumentation: a competing engine owner promises all currently unpromised room.
                var contenderOwner = new ReservationOwner("proof.capacity-contender", 1);
                var contender = next.ReserveIn(contenderOwner, resource, Fix.FromInt(40));
                var contenderTwin = Find(twin, "alternate").Get<IInventory>().ReserveIn(contenderOwner, Supply(twin), Fix.FromInt(40));
                c.That(contender.IsValid && contenderTwin.IsValid && next.Amount(resource).IsZero, "another engine owner can promise unreserved prospective room without fabricated stock");
                Command(c, game, new SetPointAccessCommand { Open = true }); Command(c, twin, new SetPointAccessCommand { Open = true });
                Reach(game, () => job.Blocker == HaulRecoveryCodes.DestinationUnavailable, "actual departure then no-room wait", twin);
                c.That(worker.Get<PointPresence>().Position == Fix.FromInt(12) && job.RecoveryDockSlot.IsNone && job.To == oldEndpoint.Id &&
                    job.State == JobState.Paused && job.Loaded == remaining && original.ReservedIn(resource) == remaining &&
                    cargo.CheckReservedOut(job.ReservationOwner, held).Ready, "actual exit clears only the old bay; cargo/old inlet remain protected while no complete output exists");
                Code available = game.World.Services.Get<WorkBoard>().PreviewDock(oldEndpoint, Find(game, "other.machine"),
                    WorkDockRequest.Haul(resource, ResourceHandlingPhase.Loading), null, out _, out _, out _, out _);
                c.Equal(Code.None, available, "old bay becomes available only after real physical departure");
                var waiting = ReadJob(game, id);
                c.That(waiting.RecoveryStage == HaulRecoveryStage.WaitingForDestination && waiting.RecoveryDockSlot.IsNone &&
                    retained.RecoveryStage == HaulRecoveryStage.Departing && retained.RecoveryDockSlot == bay,
                    "current wait and retained departure snapshots remain distinct immutable facts");
                long time = game.Clock.StepIndex;
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(game.Clock.StepIndex > time && worker.Get<PointPresence>().Position == Fix.FromInt(12) && job.Loaded == remaining &&
                    job.HandlingProgress.Equals(progress) && game.Hash() == twin.Hash(), "capacity wait advances only the one game clock, never stock or service credit");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "waiting-at-exit checkpoint restores identical protected cargo");
                next.ReleaseIn(contenderOwner, contender);
                Find(twin, "alternate").Get<IInventory>().ReleaseIn(contenderOwner, contenderTwin);
                Reach(game, () => job.To == next.Entity.Id, "complete room becomes available after departure", twin);
                c.That(job.Id == id && job.Worker == worker.Id && job.CreatedTick == created && job.Amount == Fix.FromInt(6) &&
                    job.RecoveryCause.IsNone && job.RecoveryDockSlot.IsNone && job.UnloadingBaseline == delivered &&
                    job.HandlingProgress.Equals(WorkProgress.Zero) && job.CargoOutReservation.Equals(held) &&
                    job.DestinationDockSlot == new Code("unload.bay"), "same shipment retargets to new service, never its departure slot, and restarts only remaining-leg handling");
                c.That(original.ReservedIn(resource).IsZero && next.ReservedIn(resource) == remaining && next.Amount(resource).IsZero &&
                    original.Amount(resource) == delivered && cargo.Amount(resource) == remaining, "new complete room is promised before old room is released; committed material is unchanged");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "post-egress retarget saves current itinerary/baseline/claims");
                Reach(game, () => Job(game) == null && next.Amount(resource) == remaining, "physical new dock travel unloading and final departure", twin);
                c.That(original.Amount(resource) + next.Amount(resource) == Fix.FromInt(6) && cargo.Amount(resource).IsZero &&
                    original.Reservations.Count == 0 && next.Reservations.Count == 0 && job.RecoveryCause.IsNone &&
                    job.RecoveryDockSlot.IsNone && job.UnloadingBaseline.IsZero && game.Hash() == twin.Hash(),
                    "native/public return and evacuation finish real multi-dock recovery with no duplicate material or clock");
            }
        }
        private static void LoseDestination(SimGame game, EntityId destination)
        {
            // Explicit internal participant-loss fault injection. Normal Despawn still checks all guards.
            game.World.Entities.Destroy(game.World.Entities.Get(destination), game.World.Space); game.World.Settle();
        }
        private static void DestinationLossPhysical(Check c)
        {
            foreach (var fixture in new[] { (false, true, false), (true, true, false), (false, false, false),
                (false, true, true), (true, true, true), (false, false, true) })
            {
                bool docked = fixture.Item3;
                var factory = docked ? DockRecoveryFactory(fixture.Item1, fixture.Item2) : ReplanningFactory(fixture.Item1, returning: fixture.Item2);
                var game = Start(factory); Enable(c, game);
                Entity worker = Find(game, "machine"), output = Find(game, "destination");
                ResourceId resource = Supply(game); IInventory original = output.Get<IInventory>(), next = Find(game, "alternate").Get<IInventory>();
                IInventory cargo = worker.Get<ITransporter>().Cargo;
                Reach(game, () => original.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "destination-loss partial delivery");
                Step(game); Job job = Job(game); int id = job.Id; long created = job.CreatedTick;
                Fix committed = job.Amount - job.Loaded, remaining = job.Loaded;
                ReservationToken held = job.CargoOutReservation, missingRoom = job.InReservation;
                WorkProgress progress = job.HandlingProgress;
                var contenderOwner = new ReservationOwner("proof.destination-loss-capacity", 1);
                ReservationToken contender = default;
                if (docked)
                {
                    Command(c, game, new ConfigureWorkPolicyCommand { Worker = worker.Id,
                        Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") },
                            "recovery.egress", true, true, WorkAssignmentMode.All) });
                    c.That(!job.RecoveryCause.IsNone && !job.RecoveryDockSlot.IsNone && worker.Get<PointPresence>().Position < Fix.FromInt(12),
                        "docked fixture starts actual in-transit recovery before participant loss");
                    contender = next.ReserveIn(contenderOwner, resource, Fix.FromInt(40));
                    c.That(contender.IsValid && next.Amount(resource).IsZero, "lost-dock wait uses actual competing room, not fabricated stock");
                }
                c.That(!game.World.CheckDespawn(output).IsNone, "ordinary removal refuses the protected destination; loss is explicit fault injection only");
                LoseDestination(game, output.Id);
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "missing participant checkpoint retains actual job/cargo provenance before cleanup");
                Step(game, twin);
                c.That(ReferenceEquals(Job(game), job) && job.Id == id && job.Worker == worker.Id && job.CreatedTick == created &&
                    job.State == JobState.Paused && job.Blocker == HaulRecoveryCodes.DestinationUnavailable && job.To == output.Id &&
                    job.Loaded == remaining && job.CargoOutReservation.Equals(held) && job.InReservation.Equals(missingRoom),
                    "no complete output retains the original loaded recovery rather than cancelling or creating a new shipment");
                c.That(cargo.Amount(resource) == remaining && cargo.CheckReservedOut(job.ReservationOwner, held).Ready &&
                    job.HandlingProgress.Equals(progress) && !job.TripHeld && job.RetryAt == 0,
                    "stationary wait protects actual cargo and unfinished handling without invented travel funding or retry time");
                Fix position = worker.Get<PointPresence>().Position; long waitingAt = game.Clock.StepIndex;
                ulong waitingHash = game.Hash(); WorkJobView retained = ReadJob(game, id);
                var hidden = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(hidden);
                c.That(retained.RecoveryStage == HaulRecoveryStage.WaitingForDestination && retained.Target == output.Id &&
                    retained.Loaded == remaining && hidden.Count == 0 && game.Hash() == waitingHash,
                    "missing-output diagnostics are company-private immutable reads of the same job, not another recovery store");
                if (!docked)
                {
                    var provenance = job.RecoveryKind; job.RecoveryKind = null;
                    c.Throws<InvalidOperationException>(() => game.Hash(), "missing-output wait without actual authored recovery provenance is corrupt");
                    job.RecoveryKind = provenance; c.Equal(waitingHash, game.Hash(), "restoring actual provenance restores the exact waiting checkpoint");
                }
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "retained missing-output wait saves using existing payloads");
                for (int i = 0; i < 4; i++) Step(game, twin);
                c.That(game.Clock.StepIndex > waitingAt && worker.Get<PointPresence>().Position == position && job.Loaded == remaining &&
                    job.HandlingProgress.Equals(progress) && job.CreatedTick == created && next.Amount(resource) == (docked ? Fix.Zero : Fix.FromInt(40)) &&
                    game.Hash() == twin.Hash(), "one clock advances while absent geometry and full outputs cannot move stock, teleport or earn handling");
                if (docked)
                {
                    next.ReleaseIn(contenderOwner, contender);
                    Find(twin, "alternate").Get<IInventory>().ReleaseIn(contenderOwner, contender);
                }
                else { FreeAlternate(game); FreeAlternate(twin); }
                Reach(game, () => job.To == next.Entity.Id, "missing-output complete replacement room", twin);
                c.That(ReferenceEquals(Job(game), job) && job.Id == id && job.Worker == worker.Id && job.CreatedTick == created &&
                    job.Amount == Fix.FromInt(6) && job.UnloadingBaseline == committed && job.CargoOutReservation.Equals(held) &&
                    job.RecoveryCause.IsNone && job.RecoveryDockSlot.IsNone && job.HandlingProgress.Equals(WorkProgress.Zero),
                    "same shipment uses actual position and only resets unfinished old service when a real new endpoint is admitted");
                c.That(next.Amount(resource).IsZero && next.ReservedIn(resource) == remaining && cargo.Amount(resource) == remaining &&
                    retained.Target == output.Id && retained.RecoveryStage == HaulRecoveryStage.WaitingForDestination,
                    "real replacement room is promised; no lost endpoint stock is recreated and retained snapshots stay detached");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "replacement itinerary/prefix save after missing participant recovery");
                Reach(game, () => Job(game) == null && next.Amount(resource) == remaining, "missing-output physical travel service departure", twin);
                c.That(cargo.Amount(resource).IsZero && cargo.Reservations.Count == 0 && next.Reservations.Count == 0 &&
                    job.UnloadingBaseline.IsZero && job.RecoveryCause.IsNone && job.RecoveryDockSlot.IsNone && game.Hash() == twin.Hash(),
                    "native/public cargo return and evacuation physically finish surviving cargo once and clean only actual owned claims");
            }
            var cancellationFactory = ReplanningFactory(); var cancelled = Start(cancellationFactory); Enable(c, cancelled);
            Reach(cancelled, () => Job(cancelled)?.Loaded == Fix.FromInt(6), "missing-output cancellation loaded job");
            Job cancelling = Job(cancelled); LoseDestination(cancelled, cancelling.To); Step(cancelled);
            IInventory remainingCargo = Find(cancelled, "machine").Get<ITransporter>().Cargo;
            c.That(cancelling.IsLive && cancelling.Blocker == HaulRecoveryCodes.DestinationUnavailable, "cancellation fixture is a real retained missing-output wait");
            c.Equal(Code.None, cancelled.World.System<WorkSystem>().Interrupt(cancelled.World, cancelling), "existing interruption cancels the same waiting job");
            c.That(!cancelling.IsLive && remainingCargo.Amount(Supply(cancelled)) == Fix.FromInt(6) && remainingCargo.Reservations.Count == 0,
                "cancellation releases surviving cargo claims without deleting actual cargo or following a missing endpoint");
            c.Equal(cancelled.Hash(), cancellationFactory.Load(cancelled.Save()).Hash(), "cancelled missing-output wait saves cleanly");
            foreach (bool fixedHaul in new[] { false, true })
            {
                var game = Start(ReplanningFactory(profile: fixedHaul ? "recovery.off" : "recovery.high", returning: !fixedHaul, blocked: true)); Enable(c, game);
                Job original;
                if (fixedHaul)
                {
                    int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id,
                        Find(game, "destination").Id, Supply(game), Fix.FromInt(6), 100);
                    original = game.World.Services.Get<WorkBoard>().Job(id);
                    if (original == null) throw new InvalidOperationException("fixed destination-loss fixture admission refused");
                    Reach(game, () => original.Loaded == original.Amount, "fixed haul loaded before endpoint loss");
                }
                else { Reach(game, () => Job(game)?.Loaded == Fix.FromInt(6), "unlisted recovery loaded before endpoint loss"); original = Job(game); }
                IInventory cargo = Find(game, "machine").Get<ITransporter>().Cargo;
                LoseDestination(game, original.To); Step(game);
                c.That(!original.IsLive && original.Blocker != HaulRecoveryCodes.DestinationUnavailable &&
                    cargo.Amount(Supply(game)) == Fix.FromInt(6) && !original.CargoOutReservation.IsValid,
                    fixedHaul ? "fixed player haul cannot inherit a recovery wait" : "empty authored reroute list cannot silently enable missing-output recovery");
            }
        }
        private static GameFactory RecoveryCostFactory(bool external)
            => Factory(external: external, returning: true, boundCargo: external ? "none" : null, edit: root =>
            {
                // The current physical JSON replaces this fixture's default output; no cloned endpoints.
                foreach (string section in new[] { "entities", "entity_setups" })
                {
                    JsonValue current = root.Get(section), kept = JsonValue.NewArray();
                    for (int i = 0; i < current.Count; i++) if (current[i].Get("id").Text != "destination") kept.Add(current[i].Clone());
                    if (section == "entity_setups") StartupContentAuthoring.UseEntries(root, kept);
                    else root.Set(section, kept);
                }
            }, additionalDocuments: new Dictionary<string, string>
            { ["recovery-output-cost.json"] = FrameworkProofContent.Read("recovery-output-cost.json") }, startupProfiles: new[] { "recovery-output-cost.start" });
        private static void RecoveryCost(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                var factory = RecoveryCostFactory(external); var game = Start(factory); Step(game);
                Entity worker = Find(game, "machine"), nearby = Find(game, "cost.docked"), initial = Find(game, "alternate"), later = Find(game, "later");
                ResourceId resource = Supply(game); var board = game.World.Services.Get<WorkBoard>();
                ulong hash = game.Hash(); long time = game.Clock.StepIndex;
                Code near = board.PreviewDock(nearby, worker, WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), null,
                    out Code bay, out long dockCost, out _, out _);
                Code far = board.PreviewDock(initial, worker, WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), null,
                    out _, out long directCost, out _, out _);
                c.That(near.IsNone && far.IsNone && bay == new Code("unload.bay") && dockCost == Fix.FromInt(15).Raw && directCost == Fix.FromInt(12).Raw,
                    "existing pure dock admission distinguishes actual bay cost from nearer endpoint center");
                c.That(game.Hash() == hash && game.Clock.StepIndex == time, "geometry probes create no route, claim, cargo or independent time");
                Enable(c, game); Job job = Job(game);
                if (job == null) throw new InvalidOperationException("recovery cost fixture did not acquire a loaded return");
                var cargo = worker.Get<ITransporter>().Cargo; var held = job.CargoOutReservation;
                int id = job.Id; long created = job.CreatedTick;
                c.That(job.To == initial.Id && job.From == EntityId.None && job.Loaded == Fix.FromInt(6) &&
                    job.OutReservation.IsValid == false && job.CargoInReservation.IsValid == false &&
                    cargo.CheckReservedOut(job.ReservationOwner, held).Ready && nearby.Get<IInventory>().ReservedIn(resource).IsZero,
                    "fresh recovery chooses admitted cost12 instead of nearer endpoint10 whose actual bay costs15");
                var retained = ReadJob(game, id); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "fresh native/public return selection saves actual cargo and admitted room");
                // A normal player activity consumes forty actual stored units through native work.
                Command(c, game, new PostOrderCommand { Plan = "cost.consume-stock", Target = later.Id, Priority = 100 });
                Command(c, twin, new PostOrderCommand { Plan = "cost.consume-stock", Target = Find(twin, "later").Id, Priority = 100 });
                Reach(game, () => later.Get<IInventory>().Amount(resource).IsZero, "native material consumption frees actual recovery room", twin);
                c.That(Job(game)?.Id == id && cargo.Amount(resource) == Fix.FromInt(6) && initial.Get<IInventory>().Amount(resource).IsZero,
                    "stationary authored worker consumes materials over engine time without altering the travelling cargo shipment");
                c.Equal(game.Hash(), twin.Hash(), "saved parallel material work opens exactly the same physical capacity");
                // Explicit recovery-failure protocol signal; selection and all execution still belong to the engine.
                c.That(game.World.Services.Get<IHaulExecution>().TryReplanDestination(game.World, job, Codes.NotFound) &&
                    twin.World.Services.Get<IHaulExecution>().TryReplanDestination(twin.World, Job(twin), Codes.NotFound),
                    "actual loaded job admits a freshly available complete alternative using the same cost owner");
                c.That(ReferenceEquals(Job(game), job) && job.Id == id && job.CreatedTick == created && job.Worker == worker.Id &&
                    job.To == later.Id && job.CargoOutReservation.Equals(held) && job.Loaded == Fix.FromInt(6) &&
                    initial.Get<IInventory>().ReservedIn(resource).IsZero && nearby.Get<IInventory>().ReservedIn(resource).IsZero &&
                    later.Get<IInventory>().ReservedIn(resource) == Fix.FromInt(6),
                    "continuing recovery chooses admitted direct output13 over endpoint10 with service bay15 and replaces only real room");
                c.That(retained.Target == initial.Id && retained.Loaded == Fix.FromInt(6) && game.Hash() == twin.Hash(),
                    "retained initial views stay detached while same original shipment retargets identically in saved twins");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "cost-selected continuing itinerary and original cargo promise restore identically");
                long started = game.Clock.StepIndex;
                Reach(game, () => Job(game) == null && later.Get<IInventory>().Amount(resource) == Fix.FromInt(6), "cost-selected physical recovery delivery", twin);
                c.That(game.Clock.StepIndex > started && cargo.Amount(resource).IsZero && nearby.Get<IInventory>().Amount(resource).IsZero &&
                    initial.Get<IInventory>().Amount(resource).IsZero && later.Get<IInventory>().Amount(resource) == Fix.FromInt(6),
                    "native/public surviving six-unit cargo finishes actual travel and timed service only at selected output");
                c.That(cargo.Reservations.Count == 0 && later.Get<IInventory>().Reservations.Count == 0 &&
                    initial.Get<IInventory>().Reservations.Count == 0 && game.Hash() == twin.Hash(),
                    "same recovery owner clears actual claims after physical completion without duplicate cargo, state or clock");
            }
        }
        private static void Stock(Check c, SimGame game, int desired)
            => Command(c, game, new ConfigureStockPolicyCommand { Store = Find(game, "machine").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(desired), Fix.FromInt(desired), Fix.FromInt(8), 0, LogisticsReserveAccess.Protected)) });
        private static void ReturnAdmission(Check c)
        {
            foreach (string kind in new[] { "evacuation", "cargo_return" })
            {
                c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Remove(kind))), "each recovery kind must be explicitly authored");
                foreach (string field in new[] { "enabled", "priority", "attempts_per_pass" })
                    c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get(kind).Remove(field))), "each recovery kind has required settings");
                c.Throws<ContentException>(() => Start(Factory(policyEdit: root => root.Get("logistics_recovery_profiles")[1].Get(kind).Set("surprise", JsonValue.Of(true)))), "nested recovery settings reject unknown fields");
            }
            var absent = Start(Factory(null, returning: true)); Enable(c, absent); for (int i = 0; i < 25; i++) Step(absent);
            c.That(Queue(absent).Count == 0 && Job(absent) == null && Find(absent, "machine").Get<IInventory>().Amount(Supply(absent)) == Fix.FromInt(6), "absent recovery module never posts an implicit return or loses cargo");
            var off = Start(Factory("recovery.off", returning: true)); Enable(c, off); Step(off);
            c.That(Queue(off).Count == 1 && Queue(off)[0].Blocker == LogisticsService.CargoReturnDisabled && Job(off) == null, "disabled return uses a typed shared policy blocker");
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external: external, returning: true, secondWorker: true); var game = Start(factory);
                var machine = Find(game, "machine"); var resource = Supply(game); var hauling = game.World.Services.Get<IHaulExecution>();
                ulong original = game.Hash(); var beforePreparation = hauling.PreviewReturnCargo(machine.Id, resource);
                c.That(beforePreparation.Status != HaulStatus.Ready && original == game.Hash(), "pure pre-preparation return view does not manufacture chronology");
                Step(game); var offers = Queue(game); ulong hash = game.Hash();
                c.That(offers.Count == 1 && offers[0].Entry.StartsWith("return.resource-", StringComparison.Ordinal) && offers[0].Identity == machine.Id.Value && offers[0].Target == machine.Id && offers[0].Priority == 100 && offers[0].AttemptsPerPass == 1, "native/public return intent is actual-worker-owned and authored");
                c.That(Queue(game, 1).Count == 0 && game.Hash() == hash, "return views are private and pure");
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "pending return chronology saves through the one logistics codec");
                c.Equal(offers[0].CreatedTick, Queue(twin)[0].CreatedTick, "pending return retains original age after restore");
                var other = Find(game, "other.machine");
                Command(c, game, new ConfigureWorkPolicyCommand { Worker = other.Id, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
                hash = game.Hash();
                c.That(!game.World.Services.Get<IWorkerDispatchSource>().TryAcquire(offers[0].Identity, offers[0].Entry, other.Id, WorkCandidateMode.IdleOnly) && game.Hash() == hash, "another actual compatible worker cannot take the original carrier's return cargo");
                Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "shared return admission"); var job = Job(game);
                c.That(job.From == EntityId.None && job.Loaded == Fix.FromInt(6) && !job.OutReservation.IsValid && !job.CargoInReservation.IsValid && job.CargoOutReservation.IsValid && job.InReservation.IsValid, "already-loaded return owns only real cargo-out/destination-in receipts, no fictional pickup");
                c.Equal(machine.Id, job.Worker, "return retains the actual original machine");
                hash = game.Hash();
                c.That(!game.World.Services.Get<IWorkerDispatchSource>().TryAcquire(offers[0].Identity, offers[0].Entry, machine.Id, WorkCandidateMode.IdleOnly) && game.Hash() == hash, "stale return cannot duplicate assigned carrier or claims");
            }
            var retained = Start(Factory(returning: true)); Stock(c, retained, 2); Step(retained);
            var rows = new List<LogisticsDispatchOffer>(); retained.Seat(0).View.Feature<LogisticsView>().CopyQueue(rows);
            c.That(rows.Count == 1 && rows[0].Kind == LogisticsDispatchKind.CargoReturn && rows[0].Requested == Fix.FromInt(4), "return offer excludes the actual two-unit operating reserve");
            Stock(c, retained, 6); Step(retained);
            c.Equal(0, Queue(retained).Count, "wholly retained operating stock creates no residual cargo intent");
            var fullFactory = Factory(returning: true, edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("40")));
            var full = Start(fullFactory); Enable(c, full); var pending = Queue(full); var fullTwin = fullFactory.Load(full.Save());
            for (int i = 0; i < 25; i++) Step(full, fullTwin);
            var current = Queue(full);
            c.That(pending.Count == 1 && current.Count == 1 && current[0].CreatedTick == pending[0].CreatedTick && Job(full) == null && Find(full, "machine").Get<IInventory>().Amount(Supply(full)) == Fix.FromInt(6), "full-output return waits retain age/cargo without duplicate jobs");
            c.Equal(full.Hash(), fullTwin.Hash(), "saved full-output return waits continue identically");
        }
        private static void ReturnPhysical(Check c)
        {
            foreach (var fixture in new[] { ("recovery.low", false), ("recovery.high", false), ("recovery.high", true) })
            {
                var factory = Factory(fixture.Item1, external: fixture.Item2, returning: true); var game = Start(factory); Step(game);
                Command(c, game, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect", Json.Parse("{\"target\":" + Find(game, "destination").Id.Value + "}"), "player", 50, false) });
                c.Equal(2, Queue(game).Count, "return and ordinary work share the same arbitration projection");
                Enable(c, game); Reach(game, () => Job(game) != null, "return versus player work");
                c.That(Job(game).IsHaul == (fixture.Item1 == "recovery.high"), "authored return priority, not idle-system order, decides contention");
                Reach(game, () => Job(game)?.IsHaul == true, "physical return assignment");
                var machine = Find(game, "machine"); var destination = Find(game, "destination").Get<IInventory>(); var resource = Supply(game);
                c.That(Job(game).Loaded.IsPositive && destination.Amount(resource) == Fix.Zero, "return assignment is not instant delivery");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded return saves existing native claims/job with recovery history");
                long start = game.Clock.StepIndex; game.Clock.Pause(); ulong paused = game.Hash(); game.Update();
                c.That(game.Clock.StepIndex == start && game.Hash() == paused, "pausing the sole game clock pauses return movement and service"); game.Clock.Resume();
                Reach(game, () => destination.Amount(resource) == Fix.FromInt(6) && Job(game)?.IsHaul != true, "timed return unload and departure", twin);
                c.That(game.Clock.StepIndex > start && machine.Get<IInventory>().Amount(resource) == Fix.Zero, "physical return takes game time and conserves all six cargo units");
                c.Equal(game.Hash(), twin.Hash(), "loaded return and physical exit continue identically");
            }
            var reserveFactory = Factory(returning: true); var reserve = Start(reserveFactory); Stock(c, reserve, 2); Enable(c, reserve);
            var reserveTwin = reserveFactory.Load(reserve.Save()); var output = Find(reserve, "destination").Get<IInventory>();
            Reach(reserve, () => output.Amount(Supply(reserve)) == Fix.FromInt(4) && Job(reserve) == null, "return only spare operating stock", reserveTwin);
            c.Equal(Fix.FromInt(2), Find(reserve, "machine").Get<IInventory>().Amount(Supply(reserve)), "physical return leaves actual operating reserve onboard");
            c.Equal(reserve.Hash(), reserveTwin.Hash(), "retained-stock return saves and continues through canonical owners");
            var mixedFactory = Factory(returning: true, simultaneous: true); var mixed = Start(mixedFactory); Step(mixed);
            c.That(Queue(mixed).Count == 2 && Queue(mixed).Exists(x => x.Entry.StartsWith("return.resource-", StringComparison.Ordinal)) && Queue(mixed).Exists(x => x.Entry.StartsWith("evacuation.resource-", StringComparison.Ordinal)), "both recovery kinds coexist without duplicate dispatch owners");
            Enable(c, mixed); var mixedTwin = mixedFactory.Load(mixed.Save()); var origin = Find(mixed, "source"); var final = Find(mixed, "destination").Get<IInventory>();
            Reach(mixed, () => !origin.Alive && final.Amount(Supply(mixed)) == Fix.FromInt(12), "return then actual evacuation", mixedTwin);
            c.That(Find(mixed, "machine").Get<IInventory>().Amount(Supply(mixed)) == Fix.Zero && mixed.Hash() == mixedTwin.Hash(), "mixed recovery conserves stock through real transfer/cleanup and saved continuation");
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external: external, returning: true); var game = Start(factory); Step(game);
                long created = Queue(game)[0].CreatedTick;
                var carrier = Find(game, "machine"); var resource = Supply(game); var cargo = carrier.Get<IInventory>();
                // Provider-protocol instrumentation: another engine provider owns a real outbound commitment.
                var owner = new ReservationOwner("proof.return-protection", 1); var token = cargo.ReserveOut(owner, resource, Fix.FromInt(2));
                Enable(c, game); Reach(game, () => Job(game)?.IsHaul == true, "return excludes unrelated cargo promise");
                c.Equal(Fix.FromInt(4), Job(game).Amount, "shared return reserves only actual unassigned cargo, never another owner's two units");
                var twin = factory.Load(game.Save()); var depot = Find(game, "destination").Get<IInventory>();
                Reach(game, () => depot.Amount(resource) == Fix.FromInt(4) && Job(game) == null, "promised residual stays onboard", twin);
                c.That(cargo.Amount(resource) == Fix.FromInt(2) && cargo.CheckReservedOut(owner, token).Ready, "real foreign cargo promise and stock survive return delivery");
                c.That(Queue(game).Count == 1 && Queue(game)[0].CreatedTick == created && !Queue(game)[0].Ready, "actual promised residual preserves age but cannot acquire another return");
                c.Equal(game.Hash(), twin.Hash(), "foreign native cargo commitment survives loaded saved continuation");
                cargo.ReleaseOut(owner, token); Find(twin, "machine").Get<IInventory>().ReleaseOut(owner, token);
                Reach(game, () => depot.Amount(resource) == Fix.FromInt(6) && Job(game) == null, "physical return after native promise release", twin);
                c.That(cargo.Amount(resource) == Fix.Zero && game.Hash() == twin.Hash(), "released residual returns physically once, without discarded or duplicated cargo");

                game = Start(factory); Step(game); created = Queue(game)[0].CreatedTick; Enable(c, game);
                carrier = Find(game, "machine"); cargo = carrier.Get<IInventory>(); depot = Find(game, "destination").Get<IInventory>();
                Reach(game, () => depot.Amount(resource).IsPositive && Job(game)?.Loaded.IsPositive == true, "partial return service");
                Command(c, game, new ConfigureWorkPolicyCommand { Worker = carrier.Id,
                    Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
                var interrupted = Job(game); Fix alreadyDelivered = depot.Amount(resource);
                // Actual engine interruption protocol; the fixture supplies no replacement job, claims or timer.
                c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, interrupted), "actual interruption releases this partially unloaded return");
                c.That(cargo.Amount(resource) + alreadyDelivered == Fix.FromInt(6) && cargo.Reservations.Count == 0 && depot.Reservations.Count == 0, "interruption preserves actual delivered prefix and residual cargo, releasing only own claims");
                Step(game); c.That(Queue(game).Count == 1 && Queue(game)[0].CreatedTick == created && !Queue(game)[0].Ready, "disabled interrupted return retains original intent rather than fabricating a new age");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted residual return saves without a parallel job or stock owner");
                Enable(c, game); Enable(c, twin);
                Reach(game, () => depot.Amount(resource) == Fix.FromInt(6) && Job(game) == null, "timed interrupted return reacquisition", twin);
                c.That(cargo.Amount(resource) == Fix.Zero && game.Hash() == twin.Hash(), "native/public interrupted return reacquires through shared scheduling and conserves the whole shipment");
            }
        }
    }
}
