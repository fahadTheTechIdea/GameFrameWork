using System;
using System.Collections.Generic;
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
    public static class TimedHandlingTests
    {
        public static void Register()
        {
            TestRunner.Add("Progressive handling: native holds, prefix timing and partial checkpoints", Admission);
            TestRunner.Add("Progressive handling: physical chunks, interruptions and saved continuation", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.handling\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"slots.json\"]}";
        private static GameFactory Factory(string profiles = null, bool slots = false, Action<JsonValue> edit = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
            root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"recovery.high\"}"));
            root.Set("work_policies", Content.Json.Parse("[{\"id\":\"handling.worker\",\"enabled\":true,\"priority\":0,\"allowed_roles\":[\"haul\",\"inspect\"],\"area\":null,\"interruptible\":true,\"emergency_eligible\":true,\"assignment_mode\":\"all\",\"states\":{\"disabled\":\"blocked\",\"standby\":\"blocked\"}}]"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"handling.worker\"}"));
            if (slots) root.Get("entities")[2].Get("components").Get("transporter").Set("handling", JsonValue.Of("proof.slots"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), additionalModule: slots ? new HandlingProofModule() : null,
                moduleDeclaration: slots ? Declaration : null, moduleDocument: slots ? FrameworkProofContent.Read("slot-handling.json") : null,
                handlingProfiles: profiles, flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"),
                extraModules: new GameModule[] { new LogisticsRecoveryPolicyModule() },
                extraDeclarations: "[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json") });
        }
        private static SimGame New(GameFactory factory, bool slots = false)
        { var request = new NewGameRequest { Scenario = "points", Seed = 503 }; request.Mods.Add(ResourceFlowsModule.ModuleId); if (slots) request.Mods.Add("proof.handling"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing timed-handling entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static IHaulExecution Hauling(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static IInventory Cargo(SimGame game) => Find(game, "machine").Get<ITransporter>().Cargo;
        private static Job Acquire(SimGame game, int amount = 8)
        {
            int id = Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(amount), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("native acquisition refused");
        }
        private static WorkJobView Read(SimGame game, int id)
        { var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); foreach (var job in jobs) if (job.Id == id) return job; throw new InvalidOperationException("missing live job projection"); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 360 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/" + job.HandlingProgress + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity machine = Find(game, "machine");
            IResourceHandlingProfile profile = machine.Get<ITransporter>().Handling; ResourceId supply = Supply(game);
            c.Equal("points.handling", profile.Id, "transporter explicitly aliases master-listed handling definition");
            c.That(ReferenceEquals(profile, game.World.Content.Get<IResourceHandlingProfile>(profile.Id)), "definition aliases actual content contract");
            c.Throws<InvalidOperationException>(() => game.World.Content.Get<Def>(profile.Id).Read(new JsonReader(Content.Json.Parse("{}"), "proof.reread")), "shared resolved handling content cannot be reauthored");
            c.Equal(8L, profile.CheckHandling(game.SimulationTime, supply, Fix.FromInt(8), ResourceHandlingPhase.Loading).RequiredTicks, "loading scales exact native quantity");
            c.Equal(16L, profile.CheckHandling(game.SimulationTime, supply, Fix.FromInt(8), ResourceHandlingPhase.Unloading).RequiredTicks, "unloading has its own authored rate");
            c.Equal(4L, profile.CheckHandling(game.SimulationTime, supply, Fix.FromInt(4), ResourceHandlingPhase.Loading).RequiredTicks, "half amount takes half service time");
            c.Equal(ResourceHandlingStatus.InvalidRequest, profile.CheckHandling(game.SimulationTime, supply, Fix.FromRatio(3, 2), ResourceHandlingPhase.Loading).Status, "native discrete resource rejects fractional amount");
            c.Equal(ResourceHandlingStatus.UnsupportedResource, profile.CheckHandling(game.SimulationTime, new ResourceId(99), Fix.One, ResourceHandlingPhase.Loading).Status, "unknown handling resource does not receive a default rate");
            c.Equal(ResourceHandlingStatus.InvalidRequest, profile.CheckHandling(game.SimulationTime, supply, Fix.Zero, ResourceHandlingPhase.Loading).Status, "zero amount refuses");
            c.Equal(ResourceHandlingStatus.InvalidRequest, profile.CheckHandling(game.SimulationTime, supply, Fix.One, (ResourceHandlingPhase)0).Status, "undefined phase refuses");
            c.Equal(1L, game.SimulationTime.Ticks(GameDuration.Seconds(10), 6, 1), "quantity ratio resolves before sub-tick rounding");
            c.Equal(2L, game.SimulationTime.Ticks(GameDuration.Seconds(10), 7, 1), "actual rational boundary rounds final duration upward");
            c.Equal(0L, game.SimulationTime.Ticks(GameDuration.Hours(1), 0, 1), "explicit zero ratio stays zero in general converter");
            c.Equal(long.MaxValue, game.SimulationTime.Ticks(GameDuration.Minutes(1), long.MaxValue, 1), "wide scaled duration is not narrowed to Fix or int ticks");
            c.Throws<OverflowException>(() => game.SimulationTime.Ticks(GameDuration.Minutes(2), long.MaxValue, 1), "wide overflow refuses rather than wrapping");
            c.Throws<ArgumentOutOfRangeException>(() => game.SimulationTime.Ticks(GameDuration.Minutes(1), 1, 0), "invalid ratio denominator refuses");
            Job job = Acquire(game); Tick(game); WorkJobView retained = Read(game, job.Id); ulong before = game.Hash();
            c.Equal(1L, retained.HandlingProgress.WholeTicks, "engine job view exposes actual elapsed load service");
            c.Equal(8L, retained.HandlingRequiredTicks.Value, "view computes requirement inside engine");
            var hidden = new List<WorkJobView> { retained }; PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyJobs(hidden);
            c.Equal(0, hidden.Count, "foreign company cannot inspect physical progress");
            PlayerView.Create(game.World, PlayerId.None).Feature<WorkView>().CopyJobs(hidden); c.Equal(1, hidden.Count, "observer receives detached progress");
            c.Equal(ResourceTransferStatus.Ready, Hauling(game).Load(game.World, job, machine, Find(game, "source"), supply, job.Amount).Status, "repeated current service cannot move already handled quantity again");
            c.Equal(before, game.Hash(), "pure view and premature refusal preserve stock, claims and progress");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "partial load service restores current schema");
            Tick(game); c.Equal(1L, retained.HandlingProgress.WholeTicks, "retained progress fact is immutable");
            c.Equal(Fix.One, retained.Loaded, "first earned native unit is actual held cargo");
            c.Equal(Fix.One, retained.HandledAmount, "engine projects actual handled quantity");
            c.Equal(Fix.FromInt(7), retained.HandlingRemaining, "engine projects remaining service quantity");
            c.Equal(supply, retained.Resource, "immutable view contains native resource identity");
            c.Equal(Fix.FromInt(8), retained.Amount, "original intent stays separate from actual held quantity");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "sole work codec owns partial phase semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "changed job semantics use current save format only");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity captures progressive physical handling");
            InvalidContent(c); ScaledProfiles(c); Extension(c); CorruptProgress(c); Growth(c); Quantum(c);
        }
        private static void InvalidContent(Check c)
        {
            string profiles = FrameworkProofContent.Read("point-handling.json");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("entities")[2].Get("components").Get("transporter").Remove("handling"))), "empty old transporter definition refuses, no compatibility fallback");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("entities")[2].Get("components").Get("transporter").Set("handling", JsonValue.Of("missing")))), "unknown profile refuses");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"resource\":\"supply\"", "\"resource\":\"missing\""))), "unknown profile resource refuses");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"quantity\":1", "\"quantity\":0"))), "zero native basis refuses");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"quantity\":1", "\"quantity\":1.5"))), "fractional discrete basis refuses");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"numerator\":1", "\"numerator\":0"))), "zero loading duration refuses");
            JsonValue root = Content.Json.Parse(profiles); JsonValue rows = root.Get("handling_profiles")[0].Get("resources"); rows.Add(rows[0].Clone());
            var text = new StringBuilder(); root.WriteCanonical(text); c.Throws<ContentException>(() => New(Factory(text.ToString())), "duplicate resource rate refuses");
            c.Throws<ContentException>(() => New(Factory("{\"handling_profiles\":[{\"id\":\"points.handling\",\"resources\":[]}]}")), "empty rate table refuses");
            root = Content.Json.Parse(profiles); root.Get("handling_profiles")[0].Get("resources")[0].Remove("unloading");
            text = new StringBuilder(); root.WriteCanonical(text); c.Throws<ContentException>(() => New(Factory(text.ToString())), "absent unloading duration refuses rather than borrowing loading");
        }
        private static void ScaledProfiles(Check c)
        {
            string profiles = FrameworkProofContent.Read("point-handling.json");
            SimGame game = New(Factory(profiles.Replace("\"unit\":\"minute\"", "\"unit\":\"second\"").Replace("\"quantity\":1", "\"quantity\":2")));
            c.Equal(1L, Find(game, "machine").Get<ITransporter>().Handling.CheckHandling(game.SimulationTime, Supply(game), Fix.FromInt(8), ResourceHandlingPhase.Loading).RequiredTicks,
                "sub-tick per-basis duration scales before rounding, not eight rounded ticks");
            game = New(Factory(profiles.Replace("\"numerator\":1", "\"numerator\":9223372036854775807")));
            var preview = Hauling(game).PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8));
            c.Equal(HaulStatus.PolicyBlocked, preview.Status, "unrepresentable real handling refuses carrier before claims");
            c.Equal(HaulHandlingCodes.OutOfRange, preview.Blocker, "time overflow has typed blocker");
            ulong before = game.Hash(); c.Equal(0, Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100), "overflow acquisition allocates no job");
            c.Equal(before, game.Hash(), "overflow refusal promises nothing");
            game = New(Factory(edit: r => r.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"))));
            c.Equal(16L, Find(game, "machine").Get<ITransporter>().Handling.CheckHandling(game.SimulationTime, Supply(game), Fix.FromInt(8), ResourceHandlingPhase.Loading).RequiredTicks,
                "same authored resource rate resolves against actual finer session quantum");
        }
        private static void Extension(Check c)
        {
            GameFactory factory = Factory(slots: true); SimGame game = New(factory, slots: true); IResourceHandlingProfile profile = Find(game, "machine").Get<ITransporter>().Handling;
            c.That(profile is SlotHandlingDef, "independent public-only profile resolves in same content base index");
            c.Equal(10L, profile.CheckHandling(game.SimulationTime, Supply(game), Fix.FromInt(8), ResourceHandlingPhase.Loading).RequiredTicks, "alternate slot provider owns its authored calculation");
            c.Equal(6L, profile.CheckHandling(game.SimulationTime, Supply(game), Fix.FromInt(8), ResourceHandlingPhase.Unloading).RequiredTicks, "alternate unload requirement is independent");
            Job job = Acquire(game); for (int i = 0; i < 9; i++) Tick(game);
            c.Equal(Fix.FromInt(4), job.Loaded, "public-only profile transfers first completed slot but cannot complete second early");
            c.Equal(9L, job.HandlingProgress.WholeTicks, "existing job alone owns extension progress");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "independent profile and partial job restore without new codec");
            Tick(game); c.Equal(Fix.FromInt(8), job.Loaded, "existing execution commits when extension duration completes");
        }
        private static void CorruptProgress(Check c)
        {
            SimGame game = New(Factory()); Job job = Acquire(game); Tick(game); WorkProgress accepted = job.HandlingProgress;
            job.HandlingProgress = new WorkProgress(9); c.Throws<InvalidOperationException>(() => game.Save(), "over-duration live progress cannot checkpoint");
            job.HandlingProgress = new WorkProgress(8, 1); c.Throws<InvalidOperationException>(() => game.Save(), "fraction beyond completion cannot checkpoint");
            job.HandlingProgress = accepted; c.Equal(1L, Read(game, job.Id).HandlingProgress.WholeTicks, "restored test instrumentation returns to real accepted progress");
            job.HandlingProgress = WorkProgress.Zero; c.Throws<InvalidOperationException>(() => game.Save(), "actual cargo cannot checkpoint ahead of earned handling"); job.HandlingProgress = accepted;
            Fix loaded = job.Loaded; job.Loaded += Fix.One; c.Throws<InvalidOperationException>(() => game.Save(), "fabricated partial cargo differs from native receipt remainder"); job.Loaded = loaded;
            Tick(game); c.That(game.Fault == null, "rejected checkpoint instrumentation did not fabricate a resumable faulted save");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Job job = Acquire(game); Entity machine = Find(game, "machine"); ResourceId supply = Supply(game);
            long opened = History(game).Latest; ReservationToken pickup = job.OutReservation, incoming = job.CargoInReservation;
            Tick(game); ReservationToken held = job.CargoOutReservation;
            for (int i = 0; i < 2; i++) Tick(game);
            c.Equal(3L, job.HandlingProgress.WholeTicks, "real carrier accumulates positive loading time");
            c.Equal(Fix.FromInt(17), Find(game, "source").Get<IOutletPort>().Amount(supply), "source loses actual three earned chunks");
            c.Equal(Fix.FromInt(3), Cargo(game).Amount(supply), "partial elapsed handling moves real native cargo");
            c.Equal(Fix.FromInt(5), Cargo(game).ReservedIn(supply), "unreceived cargo capacity stays reserved");
            c.Equal(Fix.FromInt(3), Cargo(game).ReservedOut(supply), "actual received cargo stays protected");
            c.Equal(held, job.CargoOutReservation, "cargo protection grows under the same native token");
            c.Equal(pickup, job.OutReservation, "partial pickup retains original source token");
            c.Equal(incoming, job.CargoInReservation, "partial pickup retains original cargo inlet token");
            c.Equal("point.transfer", machine.State.Id, "entity Update commits authored loading state");
            Pause(c, game, job); ulong paused = game.Hash();
            c.Throws<InvalidOperationException>(() => Hauling(game).Load(game.World, job, machine, Find(game, "source"), supply, job.Amount), "direct pickup cannot bypass a current policy pause");
            c.Equal(paused, game.Hash(), "paused direct execution changes no stock, claims or progress");
            SimGame twin = factory.Load(game.Save()), isolated = New(factory); Job other = Acquire(isolated, 4); Tick(isolated);
            c.Equal(game.Hash(), twin.Hash(), "paused partial service restores exact progress and claims");
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(3L, job.HandlingProgress.WholeTicks, "paused updates never grant service time");
            c.Equal(Fix.FromInt(4), other.Loaded, "independent smaller quantity completes earlier in separate session");
            Resume(c, game); Resume(c, twin); c.Equal(game.Hash(), twin.Hash(), "resume command stream preserves twin state");
            Reach(game, () => job.ToilIndex == 2, "loading complete", twin, isolated);
            c.Equal(Fix.FromInt(8), job.Loaded, "completed handling commits exact native pickup once");
            c.Equal(WorkProgress.Zero, job.HandlingProgress, "real leg transition resets progress once");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(supply), "pickup reduces actual source once");
            Tick(game); Tick(twin); Tick(isolated);
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(supply), "cargo does not teleport while carrier travels");
            Reach(game, () => job.ToilIndex == 3 && job.HandlingProgress.WholeTicks >= 3, "partial unloading", twin, isolated);
            c.Equal(Fix.FromInt(7), Cargo(game).Amount(supply), "unloading already delivered one native unit after two ticks");
            c.Equal(Fix.One, Find(game, "destination").Get<IInletPort>().Amount(supply), "arrival stock is actual elapsed native delivery");
            c.Equal(Fix.FromInt(7), Cargo(game).ReservedOut(supply), "unloading protects only held remainder");
            c.Equal(Fix.FromInt(7), Find(game, "destination").Get<IInletPort>().ReservedIn(supply), "far-end native receipt retains only pending delivery");
            c.Equal(16L, Read(game, job.Id).HandlingRequiredTicks.Value, "destination leg uses its own requirement");
            ulong before = game.Hash(); c.Equal(ResourceTransferStatus.Ready, Hauling(game).Unload(game.World, job, machine, Find(game, "destination"), supply).Status, "direct repeat cannot spend already delivered earned quantity again");
            c.Equal(before, game.Hash(), "early unloading refusal preserves receipts and cargo");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mid-unload service survives save/load");
            Reach(game, () => job.State == JobState.Done, "unloading complete", twin, isolated);
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(supply), "completed unload delivers once");
            c.Equal(Fix.Zero, Cargo(game).Amount(supply), "verified unload empties cargo");
            c.Equal(WorkProgress.Zero, job.HandlingProgress, "ended haul has no detached elapsed stage");
            c.Equal(0, Find(game, "destination").Get<IInventory>().Reservations.Count + Cargo(game).Reservations.Count, "only completed job claims release");
            c.Equal(game.Hash(), twin.Hash(), "saved interleaved full physical continuation is deterministic");
            ResourceFlowPage page = History(game, opened); c.Equal(16, page.Entries.Count, "actual eight pickup/eight delivery chunks have native flow facts");
            foreach (var entry in page.Entries) c.Equal(Fix.One, entry.Item.Amount, "history reports actual chunk rather than original shipment");
            Cancellation(c); MissingEndpoint(c); FractionalRate(c); Docks(c); Payment(c); Faults(c);
        }
        private static void Pause(Check c, SimGame game, Job job)
        {
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "disabled policy pauses real handling");
            c.Equal(JobState.Paused, job.State, "policy wait preserves native haul assignment");
        }
        private static void Resume(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "enabled policy resumes real handling");
        private static void Cancellation(Check c)
        {
            SimGame game = New(Factory()); Job job = Acquire(game); Tick(game); Tick(game);
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, job), "partial load interruption uses existing work engine");
            c.That(!job.IsLive && job.HandlingProgress.Equals(WorkProgress.Zero), "cancelled elapsed stage owns no extra progress ledger");
            c.Equal(Fix.FromInt(18), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "interrupted pickup does not refund actual received chunks");
            c.Equal(Fix.FromInt(2), Cargo(game).Amount(Supply(game)), "interrupted loading retains actual partial cargo");
            c.Equal(0, Find(game, "source").Get<IInventory>().Reservations.Count + Cargo(game).Reservations.Count + Find(game, "destination").Get<IInventory>().Reservations.Count, "cancel releases only real outstanding promises");
            game = New(Factory()); job = Acquire(game); Reach(game, () => job.ToilIndex == 3 && job.HandlingProgress.WholeTicks >= 2, "cancel unload stage");
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, job), "loaded service interruption ends same commitment");
            c.Equal(Fix.FromInt(7), Cargo(game).Amount(Supply(game)), "interrupted unloading retains actual remaining cargo");
            c.Equal(Fix.One, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "interruption preserves actual already delivered quantity");
            game.World.Services.Get<LogisticsService>().PrepareDispatch();
            c.That(game.World.Services.Get<IWorkerDispatchSource>().TryAcquire(Find(game, "machine").Id.Value, "return.resource-" + Supply(game).Value, Find(game, "machine").Id, WorkCandidateMode.IdleOnly), "shared return source accepts retained actual cargo");
            Job returning = Board(game).Job(Find(game, "machine").Get<IWorker>().Job); Tick(game);
            c.That(returning.IsLive && returning.HandlingProgress.WholeTicks > 0 && Cargo(game).Amount(Supply(game)).IsPositive, "return cargo also obeys authored timed unloading");
            SimGame twin = Factory().Load(game.Save()); Reach(game, () => !returning.IsLive, "saved partial cargo return", twin);
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)) + Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) + Cargo(game).Amount(Supply(game)), "interrupted native delivery and timed return conserve all stock");
            c.Equal(game.Hash(), twin.Hash(), "partial native return saves and continues identically");
        }
        private static void MissingEndpoint(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Job job = Acquire(game); Tick(game); Entity destination = Find(game, "destination");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(destination), "ordinary removal protects reserved service destination");
            game.World.Entities.Destroy(destination, game.World.Space); game.World.Settle(); // Fault injection only, not a player path.
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!job.IsLive && job.HandlingProgress.Equals(WorkProgress.Zero), "missing endpoint retires partial handling without pickup");
            c.Equal(Fix.FromInt(19), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "missing destination does not refund actual prior pickup");
            c.Equal(Fix.One, Cargo(game).Amount(Supply(game)), "participant loss preserves actual partial cargo for existing return path");
            c.That(!job.OutReservation.IsValid && !job.CargoInReservation.IsValid && !job.CargoOutReservation.IsValid, "original surviving claims release without deleting actual cargo");
            c.Equal(game.Hash(), twin.Hash(), "saved missing-endpoint cleanup is deterministic");
        }
        private static ResourceFlowPage History(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static void Growth(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 503 });
            ResourceInventoryState stock = game.World.Services.Get<EndpointPools>().Get(1); ResourceId steel = game.World.Content.ResourceId("steel");
            stock.Add(steel, Fix.FromInt(6)); var owner = new ReservationOwner("proof.growth", 1); var other = new ReservationOwner("proof.growth", 2);
            ReservationToken token = stock.ReserveOut(owner, steel, Fix.FromInt(2)), protectedOther = stock.ReserveOut(other, steel, Fix.FromInt(2));
            ResourceReservation original = stock.CheckReservedOut(owner, token).Record; ulong before = game.Hash(); long history = History(game).Latest;
            IResourceTransaction transaction = game.World.Services.Get<IResourceTransaction>();
            c.Equal(ResourceTransferStatus.NoStock, transaction.ExtendClaim(new PlayerId(0), original, Fix.FromInt(3)).Status, "growth cannot spend another owner's protected stock");
            c.Equal(ResourceTransferStatus.InvalidRequest, transaction.ExtendClaim(new PlayerId(0), original, Fix.Zero).Status, "zero growth refuses");
            c.Equal(before, game.Hash(), "growth refusal preserves actual stock/book/version");
            ResourceClaimResult result = transaction.ExtendClaim(new PlayerId(0), original, Fix.One);
            c.That(result.Committed && result.Receipts.Count == 1, "existing transaction owner verifies current native hold growth");
            c.Equal(token, result.Receipts[0].Token, "growth preserves same token identity");
            c.Equal(Fix.FromInt(3), result.Receipts[0].Amount, "grown receipt contains current native protection");
            c.Equal(Fix.FromInt(2), original.Amount, "detached original record remains immutable");
            c.Equal(Fix.FromInt(2), stock.CheckReservedOut(other, protectedOther).Record.Amount, "unrelated outbound promise is unchanged");
            c.Equal(Fix.FromInt(6), stock.Amount(steel), "hold growth changes no physical balance");
            c.Equal(history, History(game).Latest, "growth creates no fictitious flow history");
            before = game.Hash(); c.Equal(ResourceTransferStatus.InvalidReceipt, transaction.ExtendClaim(new PlayerId(0), original, Fix.One).Status, "stale old amount cannot authorize more growth");
            c.Equal(before, game.Hash(), "stale receipt refusal is pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "growth uses existing native book codec only");
        }
        private static void Quantum(Check c)
        {
            GameFactory factory = PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json")); SimGame game = New(factory); Job job = Acquire(game, 30);
            for (int i = 0; i < 5; i++) Tick(game); c.Equal(Fix.Zero, job.Loaded, "source/cargo packing needs an earned six-unit prefix");
            Tick(game); c.Equal(Fix.FromInt(6), job.Loaded, "sixth tick loads exact common native packing quantity");
            c.Equal(Fix.FromInt(24), Cargo(game).ReservedIn(Supply(game)), "partial native cargo capacity remains protected");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => job.ToilIndex == 2, "quantum pickup", twin);
            c.Equal(Fix.FromInt(30), job.Loaded, "all compatible source/cargo chunks finish");
            Reach(game, () => job.ToilIndex == 3 && job.HandlingProgress.WholeTicks >= 19, "quantum unloading prefix", twin);
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "destination packing waits for an earned ten-unit prefix");
            Tick(game); Tick(twin); c.Equal(Fix.FromInt(10), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "twentieth tick delivers compatible destination quantum");
            c.Equal(Fix.FromInt(20), job.Loaded, "current held cargo decreases by actual native delivery");
            c.Equal(game.Hash(), twin.Hash(), "different native phase increments save identically");
            Reach(game, () => !job.IsLive, "quantum completion", twin);
            c.Equal(Fix.FromInt(30), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "all compatible delivery chunks arrive");
            c.Equal(Fix.FromInt(60), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)) + Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "native packing never rounds away stock");
        }
        private static void Docks(Check c)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions), file = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            root.Set("entities", file.Get("entities")); StartupContentAuthoring.UseEntries(root, file.Get("entity_setups")); root.Set("work_policies", file.Get("work_policies"));
            var text = new StringBuilder(); root.WriteCanonical(text); GameFactory factory = PointGame.Factory(text.ToString()); SimGame game = New(factory);
            Job first = Acquire(game), second = Acquire(game); Entity carrier = game.World.Entities.Get(first.Worker);
            Reach(game, () => first.Loaded.IsPositive, "first dock native chunk");
            c.That(Read(game, second.Id).DockWaiting && second.Loaded.IsZero, "queued bay cannot transfer while prior carrier partly loads");
            c.Equal(WorkProgress.Zero, second.HandlingProgress, "waiting at physical dock earns no service");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = carrier.Id, Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player pauses partial dock cargo");
            WorkProgress progress = first.HandlingProgress; Fix loaded = first.Loaded; SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(loaded, first.Loaded, "pause preserves actual received cargo");
            c.Equal(progress, first.HandlingProgress, "pause earns no handling time");
            c.Equal(Fix.Zero, second.Loaded, "waiting job never preempts paused service");
            c.Equal(game.Hash(), twin.Hash(), "partial physical dock wait saves and continues");
            var settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = carrier.Id, Settings = settings }).Accepted, "player resumes partial bay");
            c.That(TestKit.Order(twin, 0, new ConfigureWorkPolicyCommand { Worker = carrier.Id, Settings = settings }).Accepted, "saved twin resumes same partial bay");
            Reach(game, () => !first.IsLive && !second.IsLive, "progressive bay queue completion", twin);
            c.Equal(Fix.FromInt(16), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "physical queue eventually handles both actual shipments");
            c.Equal(game.Hash(), twin.Hash(), "physical departure and queue promotion remain deterministic");
        }
        private static void Payment(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 23 }); World world = game.World;
            TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity dest = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18)); world.Settle(); dest.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            int id = Hauling(game).AcquireHaul(new PlayerId(0), source.Id, dest.Id, steel, Fix.FromInt(8), 100, true);
            Job job = Board(game).Job(id) ?? throw new InvalidOperationException("paid progressive acquisition refused");
            for (int i = 0; i < 3000 && job.Loaded.IsZero; i++) Tick(game);
            if (job.Loaded.IsZero) throw new InvalidOperationException("paid pickup stalled: " + job.State + "/" + job.ToilIndex + "/" + job.Blocker);
            Fix loaded = job.Loaded; IEconomy economy = world.Services.Get<IEconomy>(); economy.Charge(new PlayerId(0), economy.Cash(new PlayerId(0)), new Code("proof.cash"));
            Tick(game); c.Equal(HaulHandlingCodes.Payment, job.Blocker, "changed funds produce typed physical wait rather than discarding cargo");
            WorkProgress progress = job.HandlingProgress; SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.That(job.IsLive, "unpaid remainder retains real execution for retry");
            c.Equal(loaded, job.Loaded, "payment wait preserves already paid native cargo");
            c.Equal(progress, job.HandlingProgress, "blocked transfer earns no extra service");
            c.Equal(Fix.Zero, economy.Cash(new PlayerId(0)), "retry creates no funds or unfunded charges");
            c.Equal(game.Hash(), twin.Hash(), "saved physical payment wait is deterministic");
            economy.Credit(new PlayerId(0), (job.Amount - job.Loaded) * Fix.FromInt(7), new Code("proof.cash")); twin = factory.Load(game.Save());
            for (int i = 0; i < 3000 && job.IsLive; i++) { Tick(game); Tick(twin); }
            if (job.IsLive) throw new InvalidOperationException("funded physical remainder stalled: " + job.State + "/" + job.ToilIndex + "/" + job.Blocker);
            c.Equal(Fix.Zero, economy.Cash(new PlayerId(0)), "actual remaining chunks are paid only once");
            c.Equal(Fix.FromInt(8), dest.Get<IInventory>().Amount(steel), "same actual shipment arrives after funds return");
            c.Equal(Fix.FromInt(17), source.Get<IInventory>().Amount(steel), "finite supply loses actual total sold");
            c.Equal(game.Hash(), twin.Hash(), "funded saved progressive shipment finishes identically");
        }
        private static void Faults(Check c)
        {
            foreach (string fault in new[] { "partial_take", "partial_receive", "bad_growth" })
            {
                GameFactory factory = Factory(edit: root =>
                {
                    if (fault == "bad_growth")
                    {
                        JsonValue components = root.Get("entities")[2].Get("components"); components.Remove("storage");
                        components.Set("point_stock", Content.Json.Parse("{\"resource\":\"supply\",\"capacity_policy\":\"fixture.count.8\",\"initial\":0,\"receipt_fault\":\"bad_growth\"}"));
                    }
                    else root.Get("entities")[fault == "partial_take" ? 0 : 1].Get("components").Get("point_stock").Set("receipt_fault", JsonValue.Of(fault));
                });
                SimGame game = New(factory); Acquire(game);
                for (int i = 0; i < 80 && game.Fault == null; i++) game.Update();
                c.That(game.Fault != null, fault + " exact native violation faults progressive normal Update");
                c.Throws<InvalidOperationException>(() => game.Save(), fault + " partial provider fault cannot checkpoint as healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), fault + " cannot hash as healthy");
                if (fault == "partial_take") c.Equal(Fix.Zero, Cargo(game).Amount(Supply(game)), "failed pickup cannot fabricate cargo");
                if (fault == "partial_receive") c.Equal(Fix.One, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "actual faulted receipt is not hidden by invented compensation");
            }
        }
        private static void FractionalRate(Check c)
        {
            GameFactory factory = Factory(edit: r => r.Get("entities")[2].Get("components").Get("worker").Set("work_rate", JsonValue.Number("0.5")));
            SimGame game = New(factory); Job job = Acquire(game, 4); Tick(game);
            c.Equal(new WorkProgress(0, 2147483648), job.HandlingProgress, "half-rate carrier uses exact fractional elapsed work");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "fractional handling checkpoint retains its sole progress field");
            for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(Fix.FromInt(3), job.Loaded, "fractional carrier transfers three earned units but cannot finish one update early");
            Tick(game); Tick(twin); c.Equal(Fix.FromInt(4), job.Loaded, "eighth half-rate update completes four-tick requirement");
            c.Equal(game.Hash(), twin.Hash(), "fractional completion restores deterministically");
        }
    }
}
