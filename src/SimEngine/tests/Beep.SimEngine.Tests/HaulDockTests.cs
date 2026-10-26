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
    public static class HaulDockTests
    {
        public static void Register()
        {
            TestRunner.Add("Haul docks: authored admission, atomic commitments and checkpoint guards", Admission);
            TestRunner.Add("Haul docks: physical waiting, fair service and saved continuation", Physical);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool queueLayout = false)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions), file = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            root.Set("entities", file.Get("entities")); StartupContentAuthoring.UseEntries(root, file.Get("entity_setups")); root.Set("work_policies", file.Get("work_policies"));
            if (queueLayout)
            {
                var layout = Content.Json.Parse(FrameworkProofContent.Read("point-dock-queue-layout.json"));
                var homes = layout.Get("entities");
                for (int i = 0; i < homes.Count; i++) root.Get("entities").Add(homes[i].Clone());
                root.Get("entities")[0].Get("components").Get("point_dock").Set("slots", layout.Get("source_slots").Clone());
                StartupContentAuthoring.UseEntries(root, layout.Get("entity_setups"));
                var entries = layout.Get("entity_setups"); var entities = root.Get("entities");
                for (int i = 0; i < entries.Count; i++)
                    for (int j = 0; j < entities.Count; j++)
                        if (entities[j].Get("id").Text == entries[i].Get("entity").Text)
                            entities[j].Get("components").Get("point_presence").Set("position", entries[i].Get("placement").Get("position").Clone());
            }
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text); return PointGame.Factory(text.ToString());
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 509 });
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing dock entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static IHaulExecution Hauling(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static HaulPreview Preview(SimGame game) => Hauling(game).PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8));
        private static Job Acquire(SimGame game)
        {
            int id = Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("dock haul admission refused: " + Preview(game).Blocker);
        }
        private static Entity Carrier(SimGame game, Job job) => game.World.Entities.Get(job.Worker);
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Supply(game));
        private static Fix SlotPosition(SimGame game, string endpoint, Code slot)
            => ((PointSnapshot)Find(game, endpoint).Get<IWorkDock>().CaptureSlot(game.World, slot)).Position;
        private static Fix Total(SimGame game)
        {
            Fix total = Fix.Zero;
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Get<IInletPort>() is IInletPort stock) total += stock.Amount(Supply(game));
            return total;
        }
        private static WorkJobView Read(SimGame game, int id)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); foreach (var fact in facts) if (fact.Id == id) return fact; throw new InvalidOperationException("missing dock job view"); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            Fix opening = Total(game);
            for (int i = 0; i < 300 && !done(); i++)
            {
                Tick(game); if (twin != null) Tick(twin);
                foreach (SimGame current in twin == null ? new[] { game } : new[] { game, twin })
                {
                    if (current.Fault != null || Total(current) != opening) throw new InvalidOperationException(stage + ": actual resource conservation/health failed");
                    foreach (Entity endpoint in current.World.Entities.All)
                        if (endpoint.Alive && endpoint.Get<IWorkDock>() is IWorkDock dock)
                            foreach (var slot in dock.Slots)
                                if (slot.Kind == WorkDockSlotKind.Service)
                                {
                                    Fix service = ((PointSnapshot)dock.CaptureSlot(current.World, slot.Id)).Position; int occupants = 0;
                                    foreach (Entity entity in current.World.Entities.All)
                                        if (entity.Alive && entity.Has<ITransporter>() && entity.Get<PointPresence>().Position == service) occupants++;
                                    if (occupants > 1) throw new InvalidOperationException(stage + ": service geometry overlap at " + service);
                                }
                }
                if (twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException(stage + ": saved twins diverged");
            }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs)
                    facts.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/slots=" + job.SourceDockSlot + "," + job.DestinationDockSlot + "/work=" + job.HandlingProgress + "/" + job.Blocker + "/position=" + Carrier(game, job)?.Get<PointPresence>()?.Position);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source");
            IWorkDock dock = source.Get<IWorkDock>(); ulong before = game.Hash(); HaulPreview first = Preview(game);
            c.That(dock is PointDock, "public-only engine provider implements dock contract");
            c.Equal(4, dock.Slots.Count, "JSON authors physical service, waiting and departure locations");
            c.That(ReferenceEquals(dock.Slots, source.Def.Component<IWorkDockDefinition>().Slots), "runtime aliases immutable registered JSON slots, not a parallel definition");
            c.Equal(HaulStatus.Ready, first.Status, "initial native admission is ready");
            c.Equal(new Code("load.bay"), first.SourceDockSlot, "pure preview selects source service slot");
            c.Equal(new Code("unload.bay"), first.DestinationDockSlot, "pure preview selects destination service slot");
            c.Equal(before, game.Hash(), "preview allocates no queue, IDs or receipts");
            Job a = Acquire(game), b = Acquire(game), d = Acquire(game);
            c.Equal(new Code("load.bay"), a.SourceDockSlot, "native acquisition owns actual source bay");
            c.Equal(new Code("load.wait.a"), b.SourceDockSlot, "second shipment reserves distinct waiting geometry");
            c.Equal(new Code("load.wait.b"), d.SourceDockSlot, "third shipment uses next stable waiting identity");
            c.Equal(new Code("unload.wait.b"), b.DestinationDockSlot, "nearer far-end waiting commitment acquired with pickup and carrier");
            c.Equal(new Code("unload.wait.a"), d.DestinationDockSlot, "all endpoint slot claims remain distinct after cost-ranked selection");
            c.Equal(Fix.FromInt(24), source.Get<IOutletPort>().ReservedOut(Supply(game)), "source holds exact three native promises");
            c.Equal(Fix.FromInt(24), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "destination holds same complete native promises");
            c.Equal(HaulStatus.PolicyBlocked, Preview(game).Status, "bounded authored waiting capacity refuses fourth shipment");
            c.Equal(WorkDockCodes.Full, Preview(game).Blocker, "full dock returns typed engine blocker");
            int next = Board(game).NextJobId; before = game.Hash();
            c.Equal(0, Hauling(game).AcquireHaul(new PlayerId(0), source.Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100), "stale ready preview cannot bypass current full dock");
            c.Equal(next, Board(game).NextJobId, "dock refusal precedes job allocation");
            c.Equal(before, game.Hash(), "dock refusal precedes all receipt/worker mutation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "both-endpoint commitments restore in same work codec");
            WorkJobView retained = Read(game, b.Id); c.That(retained.DockWaiting, "private immutable view identifies committed waiter");
            var hidden = new List<WorkJobView>(); PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyJobs(hidden);
            c.Equal(0, hidden.Count, "foreign company sees no dock/job relation");
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, b), "existing interruption releases that shipment only");
            c.That(b.SourceDockSlot.IsNone && b.DestinationDockSlot.IsNone, "ended job retains no parallel queue entry");
            c.Equal(new Code("load.wait.a"), Preview(game).SourceDockSlot, "released slot becomes available through same admission");
            c.Equal(Fix.FromInt(16), source.Get<IOutletPort>().ReservedOut(Supply(game)), "other jobs' native claims remain owned");
            c.Equal(new Code("load.wait.a"), retained.SourceDockSlot, "retained view never mutates with cancellation");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing work codec owns dock and partial cargo semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current layout breaks explicitly");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "build identifies current physical semantics");
            InvalidContent(c); Corrupt(c); FarEndRefusal(c); Undocked(c);
            SimGame closed = New(Factory(r => r.Get("point_spaces")[0].Set("access_open", JsonValue.Of(false))));
            c.Equal(HaulStatus.Unreachable, Preview(closed).Status, "closed spatial access remains route refusal, not dock capacity");
            c.Equal(0, Hauling(closed).AcquireHaul(new PlayerId(0), Find(closed, "source").Id, Find(closed, "destination").Id, Supply(closed), Fix.FromInt(8), 100), "unreachable dock approach promises nothing");
        }
        private static void InvalidContent(Check c)
        {
            Action<Action<JsonValue>, string> invalid = (edit, label) => c.Throws<ContentException>(() => New(Factory(edit)), label);
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Set("slots", Content.Json.Parse("[]")), "empty dock capacity refuses JSON");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[1].Set("id", JsonValue.Of("load.bay")), "duplicate slot IDs refuse");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[1].Set("offset", JsonValue.Number("1")), "overlapping authored locations refuse");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[0].Set("kind", JsonValue.Of("waiting")), "no service bay refuses");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[3].Set("kind", JsonValue.Of("waiting")), "no authored departure refuses instead of inventing parking");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[0].Set("kind", JsonValue.Of("fake")), "undefined slot kind refuses");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Get("slots")[0].Set("offset", JsonValue.Number("101")), "geometry outside selected space refuses");
            invalid(r => r.Get("entities")[0].Get("components").Get("point_dock").Set("resources", Content.Json.Parse("[\"missing\"]")), "unknown dock resource uses existing catalog refusal");
        }
        private static void Corrupt(Check c)
        {
            var factory = Factory(); var idle = New(factory); ulong isolated = idle.Hash();
            for (int mode = 0; mode < 5; mode++)
            {
                SimGame game = New(factory); Job a = Acquire(game), b = Acquire(game);
                if (mode == 0) b.SourceDockSlot = a.SourceDockSlot;
                else if (mode == 1) b.SourceDockSlot = new Code("missing");
                else if (mode == 2) b.SourceDockSlot = Code.None;
                else if (mode == 3) b.HandlingProgress = new WorkProgress(1);
                else b.SourceDockSlot = a.SourceDockSlot;
                if (mode == 1) c.Throws<InvalidOperationException>(() => game.Hash(), "missing slot identity refuses hash");
                else if (mode == 4) c.Throws<InvalidOperationException>(() => game.HashBreakdown(), "duplicate slot identity refuses diagnostic hash publication");
                else c.Throws<InvalidOperationException>(() => game.Save(), "independent corrupt dock binding/progress refuses checkpoint " + mode);
                var first = game.Fault; long at = game.Tick;
                c.That(first != null, "corrupt dock session latches its original fault " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "corrupt dock session remains unsavable " + mode);
                c.Throws<InvalidOperationException>(() => game.Hash(), "corrupt dock session remains unhashable " + mode);
                c.Throws<InvalidOperationException>(() => game.HashBreakdown(), "corrupt dock session cannot publish diagnostic hash prefix " + mode);
                c.Equal(0, game.Update(), "corrupt dock session cannot resume " + mode);
                c.Equal(at, game.Tick, "corrupt dock session cannot advance sole clock " + mode);
                c.That(ReferenceEquals(first, game.Fault), "original dock fault remains authoritative " + mode);
                c.Equal(isolated, idle.Hash(), "negative dock session leaves another session unchanged " + mode);
            }
            c.Equal(isolated, factory.Load(idle.Save()).Hash(), "healthy independent dock session still saves normally");
            var readyFactory = CompositionProbe.Factory(new ProbeModule("probe",new ModuleVersion(1,0,0),compose:composer=>{
                composer.Events.Register<PulseEvent>("probe.checkpoint-readiness",1);
                composer.Events.Subscribe<PulseEvent>("probe.checkpoint-readiness.listener",_=>{});
            }));
            var pending = readyFactory.NewGame(new NewGameRequest{Scenario="probe",Seed=509}); long before = pending.Tick;
            pending.World.Events.Publish(new PulseEvent(1)); // Declared readiness instrumentation, no gameplay effects.
            c.Throws<InvalidOperationException>(() => pending.Save(), "pending events refuse save before codec execution");
            c.Throws<InvalidOperationException>(() => pending.Hash(), "pending events refuse hash before codec execution");
            c.Throws<InvalidOperationException>(() => pending.HashBreakdown(), "pending events also refuse diagnostic hash publication");
            c.That(pending.Fault == null, "ordinary pending-event preflight refusal does not fault a healthy session");
            c.Equal(before, pending.Tick, "checkpoint preflight never advances sole clock");
            c.Equal(1, pending.Update(), "healthy pending event completes on normal engine Update");
            c.Equal(pending.Hash(), readyFactory.Load(pending.Save()).Hash(), "settled healthy session remains resumable");
        }
        private static void FarEndRefusal(Check c)
        {
            SimGame game = New(Factory(r => r.Get("entities")[1].Get("components").Get("point_dock").Set("slots", Content.Json.Parse("[{\"id\":\"only.bay\",\"kind\":\"service\",\"offset\":1},{\"id\":\"exit\",\"kind\":\"departure\",\"offset\":2}]"))));
            Acquire(game); ulong before = game.Hash(); int next = Board(game).NextJobId;
            c.Equal(WorkDockCodes.Full, Preview(game).Blocker, "far end capacity participates before pickup");
            c.Equal(0, Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100), "destination refusal cannot leave a source-only acquisition");
            c.Equal(next, Board(game).NextJobId, "far end refusal allocates no job"); c.Equal(before, game.Hash(), "far end refusal is fully pure");
        }
        private static void Undocked(Check c)
        {
            GameFactory factory = PointGame.Factory(); SimGame game = New(factory);
            Job job = Acquire(game); c.That(job.SourceDockSlot.IsNone && job.DestinationDockSlot.IsNone, "undocked capability deliberately uses ordinary entity interaction");
            Reach(game, () => job.State == JobState.Done, "undocked physical continuation");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "undocked native timed shipment remains physical");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current undocked itinerary restores without old reader");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(queueLayout: true); SimGame game = New(factory); Job a = Acquire(game), b = Acquire(game), d = Acquire(game);
            Entity first = Carrier(game, a), second = Carrier(game, b), third = Carrier(game, d);
            Reach(game, () => a.HandlingProgress.WholeTicks >= 2 && second.Get<PointPresence>().Position == SlotPosition(game,"source",b.SourceDockSlot) && third.Get<PointPresence>().Position == SlotPosition(game,"source",d.SourceDockSlot), "physical pickup waiting");
            c.Equal(SlotPosition(game,"source",a.SourceDockSlot), first.Get<PointPresence>().Position, "service carrier travels to authored bay rather than endpoint center");
            c.Equal(WorkDockCodes.Waiting, b.Blocker, "engine reports physical waiting blocker");
            c.Equal(WorkProgress.Zero, b.HandlingProgress, "waiting does not earn handling time");
            c.Equal(WorkProgress.Zero, d.HandlingProgress, "second waiting position also earns no work");
            c.That(a.Loaded.IsPositive && a.Loaded < a.Amount, "positive elapsed handling transfers only its actual earned partial cargo");
            c.Equal(Fix.FromInt(32)-a.Loaded, Stock(game, "source"), "partial pickup moves source into actual cargo without loss");
            c.Equal(Fix.FromInt(32), Total(game), "partial pickup and waiting conserve all actual buffers");
            c.Equal(new Code("point.blocked"), second.State, "Entity.Update resolves authored stopped waiting state");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "physical queue locations/progress restore exactly");
            WorkProgress progress = a.HandlingProgress; Fix partial = a.Loaded;
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = first.Id, Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player policy pauses occupied bay without preempting queue");
            c.That(TestKit.Order(twin, 0, new ConfigureWorkPolicyCommand { Worker = first.Id, Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "same policy command on saved twin");
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, a.HandlingProgress, "occupied paused bay preserves elapsed time");
            c.Equal(partial, a.Loaded, "paused physical service cannot earn another cargo chunk");
            c.Equal(Fix.FromInt(32), Total(game), "paused occupied service conserves source and cargo");
            c.Equal(new Code("load.wait.a"), b.SourceDockSlot, "waiter cannot preempt paused service claim");
            c.Equal(game.Hash(), twin.Hash(), "saved paused queue remains deterministic");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = first.Id, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player resumes existing physical job");
            c.That(TestKit.Order(twin, 0, new ConfigureWorkPolicyCommand { Worker = first.Id, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "saved twin resumes same policy");
            Reach(game, () => a.Loaded == a.Amount, "first exact native pickup", twin);
            c.Equal(new Code("load.bay"), a.SourceDockSlot, "pickup bay claim survives until physical departure");
            c.Equal(Fix.FromInt(24), Stock(game, "source"), "completed service consumes one batch only");
            c.Equal(Fix.Zero, b.Loaded, "waiting job has not overlapped pickup");
            Reach(game, () => a.SourceDockSlot.IsNone && b.HandlingProgress.WholeTicks > 0, "oldest waiting promotion", twin);
            c.Equal(new Code("load.bay"), b.SourceDockSlot, "oldest arrived waiter receives newly vacant bay");
            c.Equal(new Code("load.wait.b"), d.SourceDockSlot, "younger waiter stays in distinct physical place");
            c.That(first.Get<PointPresence>().Position != SlotPosition(game,"source",new Code("load.bay")), "promotion waits for actual departure, not load receipt alone");
            Reach(game, () => a.ToilIndex == 3 && a.HandlingProgress.WholeTicks >= 2 && b.ToilIndex == 2 && second.Get<PointPresence>().Position == SlotPosition(game,"destination",b.DestinationDockSlot), "physical destination queue", twin);
            c.Equal(new Code("unload.wait.b"), b.DestinationDockSlot, "second loaded carrier waits at actual cost-selected far-end location");
            c.Equal(Fix.FromInt(8), second.Get<ITransporter>().Cargo.Amount(Supply(game)), "far-end waiting retains actual cargo");
            c.That(Stock(game,"destination").IsPositive && Stock(game,"destination") < a.Amount, "partial unload delivers only elapsed earned material");
            c.Equal(a.Amount-a.Loaded, Stock(game, "destination"), "partial unload moves exact actual cargo into destination");
            c.Equal(Fix.FromInt(32), Total(game), "far-end queue and partial delivery conserve all actual buffers");
            ulong before = game.Hash(); c.Throws<InvalidOperationException>(() => Hauling(game).Unload(game.World, b, second, Find(game, "destination"), Supply(game)), "direct unloading cannot bypass waiting/service commitment");
            c.Equal(before, game.Hash(), "early direct service refusal is pure");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded far-end queue survives checkpoint");
            Reach(game, () => a.ToilIndex == 4, "completed first unload awaiting departure", twin);
            c.That(a.IsLive && first.Get<IWorker>().Job == a.Id, "same job retains carrier until it clears service physically");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "native delivery commits before physical exit");
            SimGame departureTwin = factory.Load(game.Save()); c.Equal(game.Hash(), departureTwin.Hash(), "empty-cargo departure phase restores without phantom receipt"); twin = departureTwin;
            Reach(game, () => a.State == JobState.Done && b.State == JobState.Done && d.State == JobState.Done, "three queued deliveries and departures", twin);
            c.Equal(Fix.FromInt(24), Stock(game, "destination"), "three queued hauls deliver exactly once");
            c.Equal(Fix.FromInt(8), Stock(game, "source"), "resource conservation leaves actual remainder");
            Fix unload = SlotPosition(game,"destination",new Code("unload.bay"));
            c.That(first.Get<PointPresence>().Position != unload && second.Get<PointPresence>().Position != unload && third.Get<PointPresence>().Position != unload, "completed carriers vacate service without teleportation");
            c.Equal(0, Find(game, "source").Get<IInventory>().Reservations.Count + Find(game, "destination").Get<IInventory>().Reservations.Count, "finished jobs release all their actual endpoint promises");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved full queue continuation is identical");
            Reach(game, () => WorkerHomeContracts.Read(game.World,first).State == WorkerHomeState.AtHome && WorkerHomeContracts.Read(game.World,second).State == WorkerHomeState.AtHome && WorkerHomeContracts.Read(game.World,third).State == WorkerHomeState.AtHome, "actual completed fleet clears exits and reaches authored homes", twin);
            c.Equal(Fix.FromInt(32), Total(game), "home travel cannot manufacture or erase delivered resources");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical queue and home facts save normally");
            Interrupted(c); MissingEndpoint(c); Isolation(c); BlockedDeparture(c);
        }
        private static void Interrupted(Check c)
        {
            GameFactory factory = Factory(queueLayout: true); SimGame game = New(factory); Job a = Acquire(game), b = Acquire(game); Entity worker = Carrier(game, a);
            Reach(game, () => a.HandlingProgress.WholeTicks >= 2 && b.Blocker == WorkDockCodes.Waiting, "interrupt occupied bay");
            Fix carried = worker.Get<ITransporter>().Cargo.Amount(Supply(game));
            c.That(carried.IsPositive && carried < a.Amount, "interruption happens after real partial pickup");
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, a), "native interruption cancels occupied service job");
            c.Equal(new Code("load.wait.a"), b.SourceDockSlot, "released but still physically occupied bay is not a fake vacancy");
            c.Equal(Fix.FromInt(32)-carried, Stock(game, "source"), "interrupted partial pickup is not refunded or fabricated");
            c.Equal(carried, worker.Get<ITransporter>().Cargo.Amount(Supply(game)), "original carrier retains actual interrupted cargo");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "released actual occupant and waiting job restore");
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = worker.Id, Home = Find(game, "queue.home.a").Id }).Accepted, "player home order moves idle machine through existing engine behavior");
            c.That(TestKit.Order(twin, 0, new SetHomeCommand { Worker = worker.Id, Home = Find(game, "queue.home.a").Id }).Accepted, "twin receives same home order");
            Reach(game, () => b.Loaded == b.Amount, "interrupted occupant physically clears bay", twin);
            c.Equal(Fix.FromInt(8), b.Loaded, "waiting native commitment continues after real clearance");
            c.Equal(game.Hash(), twin.Hash(), "interruption/clearance continuation remains deterministic");
        }
        private static void MissingEndpoint(Check c)
        {
            GameFactory factory = Factory(queueLayout: true); SimGame game = New(factory); Job a = Acquire(game), b = Acquire(game); Entity destination = Find(game, "destination");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(destination), "normal dock removal protects physical/native commitments");
            game.World.Entities.Destroy(destination, game.World.Space); game.World.Settle(); // Fault injection only.
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!a.IsLive && !b.IsLive && a.SourceDockSlot.IsNone && b.SourceDockSlot.IsNone, "removed endpoint releases each surviving job's dock/native claims");
            c.Equal(Fix.FromInt(32), Stock(game, "source"), "removed destination never manufactures pickup");
            c.Equal(0, Find(game, "source").Get<IInventory>().Reservations.Count, "fault cleanup leaves no orphan pickup receipt");
            c.Equal(game.Hash(), twin.Hash(), "saved participant-loss cleanup agrees");
        }
        private static void Isolation(Check c)
        {
            GameFactory factory = Factory(queueLayout: true); SimGame one = New(factory), two = New(factory); Job a = Acquire(one);
            Tick(one); Tick(one); ulong untouched = two.Hash(); Acquire(one); Tick(one);
            c.Equal(untouched, two.Hash(), "other session owns no process-global dock queue");
            Job b = Acquire(two); c.Equal(new Code("load.bay"), b.SourceDockSlot, "fresh session owns its own first service claim");
            c.That(a.HandlingProgress.WholeTicks > b.HandlingProgress.WholeTicks, "interleaved session service progress is independent");
        }
        private static void BlockedDeparture(Check c)
        {
            GameFactory factory = Factory(r =>
            {
                StartupContentAuthoring.Position(r, "machine.d", JsonValue.Number("32"));
                r.Get("entities")[1].Get("components").Get("point_dock").Set("slots", Content.Json.Parse("[{\"id\":\"unload.bay\",\"kind\":\"service\",\"offset\":1},{\"id\":\"unload.wait.a\",\"kind\":\"waiting\",\"offset\":-2},{\"id\":\"unload.exit\",\"kind\":\"departure\",\"offset\":2}]"));
            }, queueLayout: true);
            SimGame game = New(factory); Job job = Acquire(game);
            Reach(game, () => job.Blocker == WorkDockCodes.Occupied, "occupied departure diagnostic");
            c.That(job.IsLive && job.ToilIndex == 4, "occupied departure retains same empty-cargo physical job");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "blocked physical exit never repeats delivered stock");
            c.Equal(SlotPosition(game,"destination",new Code("unload.bay")), Carrier(game, job).Get<PointPresence>().Position, "cannot teleport through occupied exit");
            c.Equal(new Code("unload.bay"), job.DestinationDockSlot, "blocked exit retains actual service commitment");
            SimGame twin = factory.Load(game.Save()); for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(game.Hash(), twin.Hash(), "blocked empty-cargo departure saves and continues deterministically");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "repeated exit waits do not duplicate batch commit");
        }
    }
}
