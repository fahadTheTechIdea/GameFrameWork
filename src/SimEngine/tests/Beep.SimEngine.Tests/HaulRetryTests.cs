using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class HaulRetryTests
    {
        public static void Register()
        {
            TestRunner.Add("Haul retry: authored policy, checkpoint guards and private facts", Policy);
            TestRunner.Add("Haul retry: retained physical cargo, timed recovery and saved continuation", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.retry-module\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"retry-provider.json\"]}";
        internal static GameFactory Factory(string profiles = null, string selected = "proof.retry", Action<JsonValue> edit = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Get("settings").Get("sim.work").Set("retry", JsonValue.Of(selected));
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"retry.worker\"}"));
            root.Get("entities")[2].Get("components").Set("worker_execution", Content.Json.Parse("{\"operation\":{\"id\":\"availability\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":1,\"unit\":\"unit.work-rate\"},\"conditions\":[],\"states\":{\"disabled\":\"stopped\"}}"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            var retry = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            var selectedProfiles = Content.Json.Parse(profiles ?? FrameworkProofContent.Read("haul-retry.json"));
            for (int i = 0; i < selectedProfiles.Count; i++) retry.Set(selectedProfiles.KeyAt(i), selectedProfiles.ValueAt(i));
            return PointGame.Factory(text.ToString(), additionalModule: new WorkRetryProofModule(), moduleDeclaration: Declaration,
                moduleDocument: FrameworkProofContent.Read("retry-provider.json"), retryProfiles: retry.ToString(),
                flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"));
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 509 }; request.Mods.Add("proof.retry-module"); request.Mods.Add(ResourceFlowsModule.ModuleId); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing retry entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static IInventory Cargo(SimGame game) => Find(game, "machine").Get<ITransporter>().Cargo;
        private static IHaulExecution Hauling(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static Job Acquire(SimGame game)
        { int id = Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Resource(game), Fix.FromInt(8), 100); return Board(game).Job(id) ?? throw new InvalidOperationException("retry acquisition refused"); }
        private static void Access(Check c, SimGame game, bool enabled)
            => c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = enabled }).Accepted, "player changes actual route access");
        private static void Available(Check c, SimGame game, bool enabled, Fix rate)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, "machine").Id, Operation = new Code("availability"), Enabled = enabled, Target = rate }).Accepted, "player changes actual engine-side carrier control");
        private static void Enabled(Check c, SimGame game, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player changes existing machine policy");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, int limit = 180)
        {
            for (int i = 0; i < limit && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            { var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.ToilIndex + "/loaded=" + job.Loaded + "/retry=" + job.RetryAt + "/fail=" + job.Failures + "/" + job.Blocker); throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts)); }
        }
        private static WorkJobView Read(SimGame game, int id)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); foreach (var fact in facts) if (fact.Id == id) return fact; throw new InvalidOperationException("missing retry projection"); }
        private static Fix Total(SimGame game)
            => Find(game, "source").Get<IInletPort>().Amount(Resource(game)) + Cargo(game).Amount(Resource(game)) + Find(game, "destination").Get<IInletPort>().Amount(Resource(game));
        private static void Policy(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); IWorkRetryPolicy policy = Board(game).RetryPolicy;
            c.That(ReferenceEquals(policy, game.World.Content.Get<WorkRetryProfileDef>("proof.retry")), "policy aliases sole resolved base content definition");
            c.Equal(2L, policy.Delay(game.SimulationTime, 1), "first delay uses authored actual session duration");
            c.Equal(4L, policy.Delay(game.SimulationTime, 2), "second consecutive failure doubles");
            c.Equal(8L, policy.Delay(game.SimulationTime, 3), "third reaches authored maximum");
            c.Equal(8L, policy.Delay(game.SimulationTime, int.MaxValue), "wide failure count saturates without an exponent ceiling or overflow");
            c.That(policy.CanRetry(WorkSystem.BlockerUnreachable) && policy.CanRetry(HaulHandlingCodes.Payment), "authored blocker identities explicitly permit retry");
            c.That(!policy.CanRetry(Codes.NotFound) && !policy.CanRetry(Code.None), "missing endpoints and success never become retry failures");
            c.Throws<ArgumentException>(() => policy.Delay(game.SimulationTime, 0), "zero failure count is not fabricated into an initial delay");
            var authored = (AuthoredWorkRetryProfileDef)policy;
            c.Throws<NotSupportedException>(() => ((IList<Code>)authored.RetryBlockers).Add(Codes.NotFound), "authored blocker set is immutable");
            c.Throws<ContentException>(() => authored.Read(new JsonReader(Content.Json.Parse("{}"), "proof.reread")), "resolved policy cannot be reauthored");
            string profiles = FrameworkProofContent.Read("haul-retry.json");
            foreach (string bad in new[] { profiles.Replace("\"multiplier\": 2", "\"multiplier\": 0"), profiles.Replace("\"numerator\": 2", "\"numerator\": 0"),
                profiles.Replace("\"numerator\": 8", "\"numerator\": 1"), profiles.Replace("\"work.worker-unavailable\"", "\"blocker.unreachable\""),
                "{\"work_retry_profiles\":[{\"id\":\"proof.retry\",\"initial_delay\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"maximum_delay\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"multiplier\":1,\"retry_blockers\":[]}]}" })
                c.Throws<ContentException>(() => New(Factory(bad)), "invalid authored retry profile refuses selected composition");
            c.Throws<ContentException>(() => New(Factory(selected: "missing")), "unknown retry profile refuses before Initialize");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("scenarios")[0].Remove("settings"))), "missing selected settings has no retry default");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("scenarios")[0].Get("settings").Get("sim.work").Remove("retry"))), "missing required profile binding refuses");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"blocker.unreachable\"", "\"\""))), "empty blocker has no inferred failure identity");
            SimGame constant = New(Factory(profiles.Replace("\"multiplier\": 2", "\"multiplier\": 1")));
            c.Equal(2L, Board(constant).RetryPolicy.Delay(constant.SimulationTime, int.MaxValue), "authored multiplier one stays constant");
            SimGame huge = New(Factory(profiles.Replace("\"multiplier\": 2", "\"multiplier\": 2147483647")));
            c.Equal(8L, Board(huge).RetryPolicy.Delay(huge.SimulationTime, 2), "large multiplier caps before multiplication overflow");
            SimGame finer = New(Factory(edit: root => root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"))));
            c.Equal(4L, Board(finer).RetryPolicy.Delay(finer.SimulationTime, 1), "same profile resolves against actual finer session quantum");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "one current work codec owns retry identity and phase state");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking retry semantics use explicit current format");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current identity records retained retry rules");
            Checkpoints(c, factory); Mod(c); Ordinary(c);
        }
        private static void Checkpoints(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Job job = Acquire(game); Tick(game); Available(c, game, false, Fix.One);
            WorkJobView retained = Read(game, job.Id); ulong before = game.Hash();
            c.Equal(1, retained.FailureCount, "immutable view reports existing failure count");
            c.Equal(1L, retained.RetryRemainingTicks, "engine projects remaining delay at the completed checkpoint boundary");
            c.Equal(job.RetryAt, retained.RetryAt, "view reads same saved job deadline");
            var privateFacts = new List<WorkJobView>(); PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyJobs(privateFacts);
            c.Equal(0, privateFacts.Count, "foreign company cannot inspect retry workload");
            c.Throws<InvalidOperationException>(() => Hauling(game).Load(game.World, job, Find(game, "machine"), Find(game, "source"), Resource(game), job.Amount), "direct native loading cannot bypass a retained retry wait");
            c.Equal(before, game.Hash(), "retry reads/refused direct loading change no gameplay state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "paused native retry state restores exactly");
            Tick(game); c.Equal(1L, retained.RetryRemainingTicks, "retained retry facts do not follow live clock");
            int failures = job.Failures; long deadline = job.RetryAt; JobState state = job.State;
            job.Failures = -1; c.Throws<InvalidOperationException>(() => game.Save(), "negative failure counter refuses checkpoint"); job.Failures = failures;
            job.RetryAt = 0; c.Throws<InvalidOperationException>(() => game.Save(), "retry pause without deadline refuses checkpoint"); job.RetryAt = deadline;
            job.RetryAt = game.Tick + 99; c.Throws<InvalidOperationException>(() => game.Save(), "fabricated excessive deadline refuses checkpoint"); job.RetryAt = deadline;
            job.State = JobState.Assigned; c.Throws<InvalidOperationException>(() => game.Save(), "scheduled retry cannot masquerade as runnable assignment"); job.State = state;
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored canonical retry still validates after injected malformed writes");
            var stream = new MemoryStream(); var writer = new StateWriter(stream); Board(game).Sync(writer); writer.Flush();
            byte[] raw = stream.ToArray(); byte[] wrong = Encoding.UTF8.GetBytes("wrong.retry");
            Array.Copy(wrong, 0, raw, 4, wrong.Length);
            c.Throws<CorruptStateException>(() => new WorkBoard(game.World, Board(game).RetryPolicy).Sync(new StateReader(raw, 0, raw.Length)), "sole work reader rejects a saved foreign policy identity");
        }
        private static void Mod(Check c)
        {
            GameFactory factory = Factory(selected: "retry.constant"); SimGame game = New(factory); Job job = Acquire(game);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "machine").Get<PointPresence>().Position > Fix.Zero, "mod loaded departure");
            Access(c, game, false);
            c.Equal(3L, job.RetryAt - (game.Tick - 1), "independent public-only profile supplies its own authored algorithm");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => job.Failures == 2, "mod repeated route attempt", twin);
            c.Equal(3L, job.RetryAt - (game.Tick - 1), "same scheduler uses constant provider without a type switch");
            c.Equal(game.Hash(), twin.Hash(), "mod policy and existing jobs continue identically after restore");
            factory = Factory(selected: "retry.fault"); game = New(factory); job = Acquire(game);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "machine").Get<PointPresence>().Position > Fix.Zero, "fault loaded departure");
            Access(c, game, false);
            for (int i = 0; i < 12 && game.Fault == null; i++) game.Update();
            c.That(game.Fault != null, "invalid provider delay faults normal Update instead of substituting a duration");
            c.Throws<InvalidOperationException>(() => game.Save(), "provider-faulted retry cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "provider-faulted retry cannot hash as success");
        }
        private static void Ordinary(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, "destination").Id }).Accepted, "ordinary work uses same authored policy");
            Order order = Board(game).Orders[0]; Reach(game, () => Find(game, "machine").Get<IMotion>().Status == MoveStatus.Moving, "ordinary approach");
            Access(c, game, false); Job job = Board(game).Job(order.Nodes[0].JobId);
            c.That(job.State == JobState.Open && job.Failures == 1, "ordinary work retains its existing reopen semantics");
            c.Equal(2L, job.RetryAt - (game.Tick - 1), "ordinary delay comes from same profile, not old Tuning constants");
            long age = job.CreatedTick; SimGame twin = factory.Load(game.Save()); Access(c, game, true); Access(c, twin, true);
            Reach(game, () => !order.IsActive, "ordinary saved recovery", twin);
            c.Equal(OrderState.Done, order.State, "ordinary physical work recovers and completes");
            c.Equal(age, job.CreatedTick, "retry never resets original job age");
            c.Equal(game.Hash(), twin.Hash(), "ordinary retained node/work retry continues identically");
        }
        private static void Physical(Check c)
        { Route(c); Downtime(c); Docks(c); Cancellation(c); MissingEndpoint(c); Payment(c); }
        private static void Route(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory), isolated = New(factory); Job job = Acquire(game);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "machine").Get<PointPresence>().Position > Fix.Zero, "physical loaded departure");
            var cargoToken = job.CargoOutReservation; var inletToken = job.InReservation; EntityId carrier = job.Worker; long age = job.CreatedTick;
            Fix position = Find(game, "machine").Get<PointPresence>().Position; Access(c, game, false);
            c.Equal(JobState.Paused, job.State, "unreachable route retains paused actual haul");
            c.Equal(WorkSystem.BlockerUnreachable, job.Blocker, "typed route reason remains with same job");
            c.Equal(2L, job.RetryAt - (game.Tick - 1), "first route failure schedules actual game-time delay");
            c.Equal(Fix.FromInt(8), Cargo(game).Amount(Resource(game)), "route loss never returns, discards or teleports loaded cargo");
            c.Equal(carrier, job.Worker, "retained job keeps same real carrier");
            c.Equal(job.Id, Find(game, "machine").Get<IWorker>().Job, "worker claim remains authoritative");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin); Tick(isolated);
            c.Equal(1, job.Failures, "intermediate wait tick does not attempt again");
            c.Equal(position, Find(game, "machine").Get<PointPresence>().Position, "retry wait grants no physical travel");
            Tick(game); Tick(twin); c.Equal(2, job.Failures, "only due tick performs second actual failure");
            c.Equal(4L, job.RetryAt - (game.Tick - 1), "consecutive failures back off through current profile");
            Reach(game, () => job.Failures == 3, "maximum retry backoff", twin);
            c.Equal(8L, job.RetryAt - (game.Tick - 1), "maximum delay is authored, not failure abandonment");
            c.Equal(cargoToken, job.CargoOutReservation, "route retries retain original cargo token");
            c.Equal(inletToken, job.InReservation, "route retries retain original destination token");
            c.Equal(age, job.CreatedTick, "physical retry retains original dispatch age");
            c.Equal(1, Board(game).Jobs.Count, "retry adds no replacement job or second route queue");
            long due = job.RetryAt; Access(c, game, true); Access(c, twin, true);
            c.Equal(position, Find(game, "machine").Get<PointPresence>().Position, "restored access cannot bypass outstanding deadline");
            while (game.Tick < due) { Tick(game); Tick(twin); }
            c.Equal(JobState.Paused, job.State, "carrier stays paused until deadline");
            Tick(game); Tick(twin); c.Equal(JobState.Assigned, job.State, "due attempt requests actual provider route");
            c.Equal(0, job.Failures, "accepted physical route resets consecutive backoff");
            c.That(Find(game, "machine").Get<PointPresence>().Position > position && Find(game, "machine").Get<PointPresence>().Position < Fix.FromInt(10), "resumed route advances physically in the normal motion phase, never teleports");
            c.Equal(1L, Find(game, "machine").Get<PointMotion>().TicksMoving, "one accepted route gets exactly one declared motion update");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "replanning never delivers distant cargo instantly");
            Reach(game, () => !job.IsLive, "saved route delivery", twin);
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "same retained cargo physically delivers exactly once");
            c.Equal(Fix.FromInt(20), Total(game), "route loss/recovery conserves all native stock");
            c.Equal(game.Hash(), twin.Hash(), "repeated closed routes and saved recovery are deterministic");
            c.Equal(Fix.Zero, Cargo(isolated).Amount(Resource(isolated)), "another session receives no cargo or retry state");
        }
        private static void Downtime(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Job job = Acquire(game); Tick(game); Tick(game);
            var source = job.OutReservation; var incoming = job.CargoInReservation; var held = job.CargoOutReservation; WorkProgress progress = job.HandlingProgress;
            Available(c, game, false, Fix.One);
            c.That(job.State == JobState.Paused && job.Blocker == WorkSystem.BlockerWorkerUnavailable, "saved operation downtime becomes typed retained wait");
            c.Equal(Fix.FromInt(2), job.Loaded, "downtime preserves actual partly loaded cargo");
            c.Equal(progress, job.HandlingProgress, "unavailable carrier earns no loading work");
            c.Equal(Fix.FromInt(6), Find(game, "source").Get<IOutletPort>().ReservedOut(Resource(game)), "remaining source stock stays protected");
            c.Equal(Fix.FromInt(6), Cargo(game).ReservedIn(Resource(game)), "remaining cargo capacity stays protected");
            c.Equal(Fix.FromInt(2), Cargo(game).ReservedOut(Resource(game)), "already received cargo stays protected");
            SimGame twin = factory.Load(game.Save()); Enabled(c, game, false); Enabled(c, twin, false);
            long deadline = job.RetryAt;
            for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(deadline, job.RetryAt, "policy disable never erases or reschedules current retry deadline");
            c.Equal(1, job.Failures, "policy pause suppresses due attempts without creating failures");
            c.Equal(WorkSystem.BlockerWorkerUnavailable, job.Blocker, "outstanding native retry reason is not replaced by a stale policy reason");
            c.Equal(game.Hash(), twin.Hash(), "expired deadline while policy-paused saves exactly");
            Available(c, game, true, Fix.One); Available(c, twin, true, Fix.One); Enabled(c, game, true); Enabled(c, twin, true);
            c.That(job.Loaded > Fix.FromInt(2), "enabled recovered carrier resumes earned native pickup when due");
            c.Equal(source, job.OutReservation, "partial recovery keeps original source token");
            c.Equal(incoming, job.CargoInReservation, "partial recovery keeps original cargo inlet token");
            c.Equal(held, job.CargoOutReservation, "partial recovery grows original cargo hold");
            Reach(game, () => job.ToilIndex == 3 && job.HandlingProgress.WholeTicks >= 2, "real partial unloading", twin);
            Fix loaded = job.Loaded; progress = job.HandlingProgress; Available(c, game, true, Fix.Zero); Available(c, twin, true, Fix.Zero);
            c.Equal(WorkSystem.BlockerWorkerUnavailable, job.Blocker, "zero effective work rate uses same typed retained wait");
            c.Equal(loaded, job.Loaded, "unloading downtime retains actual undelivered stock");
            c.Equal(progress, job.HandlingProgress, "unloading downtime earns no phantom work");
            twin = factory.Load(game.Save()); Available(c, game, true, Fix.One); Available(c, twin, true, Fix.One);
            Reach(game, () => !job.IsLive, "partial downtime recovery delivery", twin);
            c.Equal(Fix.FromInt(20), Total(game), "partial pickup/unloading downtime conserves all stock");
            c.Equal(game.Hash(), twin.Hash(), "saved partial native recovery continues identically");
        }
        private static void Cancellation(Check c)
        {
            SimGame game = New(Factory()); Job job = Acquire(game);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "machine").Get<PointPresence>().Position > Fix.Zero, "cancel loaded departure");
            Access(c, game, false); c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, job), "explicit engine interruption ends retained retry");
            c.That(!job.IsLive && job.RetryAt == 0 && !job.Worker.IsValid, "cancel clears deadline and real worker relation");
            c.Equal(Fix.FromInt(8), Cargo(game).Amount(Resource(game)), "cancel retains actual physical cargo");
            c.Equal(0, Cargo(game).Reservations.Count + Find(game, "destination").Get<IInventory>().Reservations.Count, "cancel releases only original surviving commitments");
            c.Equal(Fix.FromInt(20), Total(game), "cancel never fabricates refund or loses native stock");
            c.Equal(game.Hash(), Factory().Load(game.Save()).Hash(), "interrupted retained cargo saves through existing owners");
        }
        private static void Docks(Check c)
        {
            GameFactory factory = Factory(edit: root =>
            {
                JsonValue docks = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
                root.Set("entities", docks.Get("entities").Clone()); StartupContentAuthoring.UseEntries(root, docks.Get("entity_setups"));
                root.Set("work_policies", docks.Get("work_policies").Clone());
                root.Get("entities")[2].Set("id", JsonValue.Of("machine"));
                root.Get("entities")[2].Get("components").Set("worker_execution", Content.Json.Parse("{\"operation\":{\"id\":\"availability\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":1,\"unit\":\"unit.work-rate\"},\"conditions\":[],\"states\":{\"disabled\":\"stopped\"}}"));
                JsonValue starts = root.Get("entity_setups");
                for (int i = 0; i < starts.Count; i++)
                    if (starts[i].Get("entity").Text == "machine.a") starts[i].Set("entity", JsonValue.Of("machine"));
            });
            SimGame game = New(factory); Job first = Acquire(game), second = Acquire(game);
            Reach(game, () => first.Loaded.IsPositive, "partial physical dock cargo");
            c.Equal(Find(game, "machine").Id, first.Worker, "actual first bay claimant uses controlled carrier");
            Code sourceSlot = first.SourceDockSlot, destinationSlot = first.DestinationDockSlot; Available(c, game, false, Fix.One);
            Fix loaded = first.Loaded; SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(sourceSlot, first.SourceDockSlot, "downtime retries retain physical source bay commitment");
            c.Equal(destinationSlot, first.DestinationDockSlot, "downtime retries retain physical far-end commitment");
            c.Equal(loaded, first.Loaded, "retained bay never earns loading time while unavailable");
            c.That(second.Loaded.IsZero && Read(game, second.Id).DockWaiting, "waiting carrier cannot steal bay or overlap native service");
            c.Equal(game.Hash(), twin.Hash(), "saved dock commitments and backoff continue identically");
            Available(c, game, true, Fix.One); Available(c, twin, true, Fix.One);
            Reach(game, () => !first.IsLive && !second.IsLive, "retained bay recovery and physical departure", twin);
            c.Equal(Fix.FromInt(16), Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "both original shipments physically finish without duplicate stock");
            c.Equal(game.Hash(), twin.Hash(), "recovered bay clearance and waiting promotion stay deterministic");
        }
        private static void MissingEndpoint(Check c)
        {
            GameFactory factory = Factory(FrameworkProofContent.Read("haul-retry.json").Replace("\"haul.stock-wait\"", "\"" + Codes.NotFound.Id + "\"")); SimGame game = New(factory); Job job = Acquire(game);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "machine").Get<PointPresence>().Position > Fix.Zero, "missing endpoint loaded departure");
            Access(c, game, false); Entity destination = Find(game, "destination");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(destination), "ordinary endpoint removal refuses live retry promises");
            game.World.Entities.Destroy(destination, game.World.Space); game.World.Settle(); // Explicit destructive participant-loss fault injection only.
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!job.IsLive && job.RetryAt == 0, "missing participant terminates without waiting for old deadline");
            c.Equal(Fix.FromInt(8), Cargo(game).Amount(Resource(game)), "removed destination does not discard held cargo");
            c.Equal(game.Hash(), twin.Hash(), "saved missing-participant cleanup is deterministic");
        }
        private static void Payment(Check c)
        {
            GameFactory factory = Colony.Factory(retryProfiles: FrameworkProofContent.Read("haul-retry.json"));
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 509 }); World world = game.World; TileMap map = (TileMap)world.Space;
            ResourceId steel = world.Content.ResourceId("steel"); Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity target = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18)); world.Settle(); target.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            int id = Hauling(game).AcquireHaul(new PlayerId(0), source.Id, target.Id, steel, Fix.FromInt(8), 100, true);
            Job job = Board(game).Job(id) ?? throw new InvalidOperationException("paid retry acquisition refused");
            for (int i = 0; i < 3000 && job.Loaded.IsZero; i++) Tick(game);
            if (job.Loaded.IsZero) throw new InvalidOperationException("paid pickup stalled");
            IEconomy economy = world.Services.Get<IEconomy>(); economy.Charge(new PlayerId(0), economy.Cash(new PlayerId(0)), new Code("proof.cash"));
            Tick(game); Fix loaded = job.Loaded; WorkProgress progress = job.HandlingProgress;
            c.That(job.State == JobState.Paused && job.Blocker == HaulHandlingCodes.Payment, "payment refusal schedules retained native retry");
            c.Equal(2L, job.RetryAt - (game.Tick - 1), "payment wait uses same authored game-time policy");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(1, job.Failures, "payment waits do not poll/mutate each intervening tick");
            c.Equal(loaded, job.Loaded, "already paid actual cargo remains held"); c.Equal(progress, job.HandlingProgress, "payment wait earns no service");
            economy.Credit(new PlayerId(0), (job.Amount - job.Loaded) * Fix.FromInt(7), new Code("proof.cash")); twin = factory.Load(game.Save());
            for (int i = 0; i < 3000 && job.IsLive; i++) { Tick(game); Tick(twin); }
            if (job.IsLive) throw new InvalidOperationException("funded retry delivery stalled: " + job.Blocker);
            c.Equal(Fix.FromInt(8), target.Get<IInletPort>().Amount(steel), "same funded remainder physically delivers exactly once");
            c.Equal(Fix.FromInt(17), source.Get<ISupplier>().SupplyStock.Amount(steel), "finite supplier loses only actual sold stock");
            c.Equal(Fix.Zero, economy.Cash(new PlayerId(0)), "retry charges only remaining native chunks");
            c.Equal(game.Hash(), twin.Hash(), "paid retry save/load continues identically");
        }
    }
}
