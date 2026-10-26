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
    // Authored content, player commands, bounded Update and observations. No queue or service rules.
    internal static class ServiceDockTests
    {
        internal static void Register()
        {
            TestRunner.Add("Service docks: shared contracts, admission and checkpoint ownership", Admission);
            TestRunner.Add("Service docks: physical mixed queues, work and saved continuation", Physical);
        }
        private static JsonValue DockParts(JsonValue root, int index = 0) => root.Get("entities")[index].Get("components").Get("point_dock");
        internal static GameFactory Factory(Action<JsonValue> edit = null, IReadOnlyList<GameModule> extraModules = null,
            string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null, string alertProfiles = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions);
            var file = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            root.Set("entities", file.Get("entities")); StartupContentAuthoring.UseEntries(root, file.Get("entity_setups")); root.Set("work_policies", file.Get("work_policies"));
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            DockParts(root).Set("work_roles", Content.Json.Parse("[\"inspect\"]"));
            DockParts(root).Get("slots").Add(Content.Json.Parse("{\"id\":\"load.exit.b\",\"kind\":\"departure\",\"offset\":9}"));
            DockParts(root).Get("slots").Add(Content.Json.Parse("{\"id\":\"load.exit.c\",\"kind\":\"departure\",\"offset\":20}"));
            DockParts(root, 1).Set("work_roles", Content.Json.Parse("[\"inspect\"]"));
            root.Get("activities")[0].Set("nodes", Content.Json.Parse("[{\"id\":\"a\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}},{\"id\":\"b\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}}]"));
            var worker = root.Get("entities")[3].Get("components"); worker.Remove("worker");
            worker.Set("contract_worker", Content.Json.Parse("{\"role\":\"inspect\",\"rate\":1,\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}"));
            WorkerExecutionTestContent.Add(root); edit?.Invoke(root);
            var text = new StringBuilder(); root.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments, alertProfiles: alertProfiles);
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 919 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing service entity " + id);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static IHaulExecution Hauling(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Supply(game));
        private static Entity Actor(SimGame game, Job job) => game.World.Entities.Get(job.Worker);
        private static Job Haul(SimGame game)
        {
            int id = Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("mixed dock refused initial haul");
        }
        private static Order Post(SimGame game, string target = "source")
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, target).Id });
            if (!result.Accepted || game.Fault != null) throw new InvalidOperationException("service order refused/faulted: " + result + "/" + game.Fault);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static WorkJobView Read(SimGame game, int id)
        {
            var values = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(values);
            foreach (var value in values) if (value.Id == id) return value; throw new InvalidOperationException("missing service job fact");
        }
        private static List<WorkWorkerCandidate> Candidates(SimGame game)
        {
            var values = new List<WorkWorkerCandidate>();
            Code refusal = game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("inspect", "a", Find(game, "source").Id, values);
            if (!refusal.IsNone) throw new InvalidOperationException("service candidate preview refused: " + refusal); return values;
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++)
            {
                Tick(game); if (twin != null) Tick(twin);
                foreach (Fix bay in new[] { Fix.One, Fix.FromInt(11) })
                {
                    int occupants = 0;
                    foreach (Entity entity in game.World.Entities.All)
                        if (entity.Alive && (entity.Has<IWorker>() || entity.Has<ITransporter>()) && entity.Get<PointPresence>()?.Position == bay) occupants++;
                    if (occupants > 1) throw new InvalidOperationException(stage + ": physical bay overlap at " + bay);
                }
            }
            if (done()) return;
            var facts = new List<string>();
            foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/slot=" + job.SourceDockSlot + "," + job.DestinationDockSlot + "/" + job.Blocker + "/position=" + Actor(game, job)?.Get<PointPresence>()?.Position);
            throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
        }
        private static void Policy(Check c, SimGame game, EntityId worker, bool enabled, SimGame twin = null)
        {
            foreach (SimGame current in twin == null ? new[] { game } : new[] { game, twin })
                c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand { Worker = worker,
                    Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                    "same player work policy controls actual queued service job");
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var source = Find(game, "source"); var dock = source.Get<IWorkDock>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current clean service contract revision");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "unchanged envelope fields, explicit current semantics");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "sole job codec owns service commitments");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "sole work system owns activity service timing");
            c.That(dock is PointDock && ReferenceEquals(dock.Slots, source.Def.Component<IWorkDockDefinition>().Slots), "public provider aliases actual immutable definition, not another queue");
            c.Throws<NotSupportedException>(() => ((IList<WorkDockSlot>)dock.Slots).Clear(), "authored physical slots cannot be changed");
            c.Throws<InvalidOperationException>(() => ((ComponentDef)source.Def.Component<IWorkDockDefinition>()).Read(new JsonReader(Content.Json.Parse("{}"), "late dock")), "resolved dock cannot be reauthored");
            c.Throws<InvalidOperationException>(() => ((ComponentDef)source.Def.Component<IWorkDockDefinition>()).Resolve(null), "resolved dock cannot be rebound");
            c.That(WorkDockRequest.Work(new WorkerRoleId("inspect")).IsWork && !WorkDockRequest.Haul(Supply(game), ResourceHandlingPhase.Loading).IsWork,
                "one typed request distinguishes real work and native transfer");
            c.That(!default(WorkDockRequest).IsValid, "default is not a fabricated work request");
            c.Throws<ArgumentException>(() => WorkDockRequest.Work(WorkerRoleId.None), "work requires actual role");
            c.Throws<ArgumentException>(() => WorkDockRequest.Haul(ResourceId.None, ResourceHandlingPhase.Loading), "haul requires resource");
            c.Throws<ArgumentException>(() => WorkDockRequest.Haul(Supply(game), (ResourceHandlingPhase)255), "undefined handling phase cannot enter coordinator");
            ulong hash = game.Hash(); var candidates = Candidates(game);
            c.Equal(4, candidates.Count, "ordinary worker selector includes native and public workers");
            foreach (var value in candidates) c.Equal(WorkRequirementStatus.Satisfied, value.Result.Status, "initial authored service is available");
            c.Equal(hash, game.Hash(), "service candidate cost/readiness is pure");
            Job haul = Haul(game); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            c.Equal(new Code("load.bay"), haul.SourceDockSlot, "native haul owns shared first service bay");
            c.Equal(new Code("load.wait.a"), a.DestinationDockSlot, "activity shares first actual waiting claim");
            c.Equal(new Code("load.wait.b"), b.DestinationDockSlot, "parallel node has distinct actual waiting claim");
            c.That(a.SourceDockSlot.IsNone && b.SourceDockSlot.IsNone && !a.IsHaul && !b.IsHaul, "ordinary jobs do not invent transfer receipts or source slots");
            c.That(Actor(game, a).Get<IWorker>() is ContractWorker, "public worker uses same coordinator without native worker lookup");
            c.Equal(3, a.Toils.Count, "docked activity itinerary includes physical departure");
            c.Equal(WorkPhases.Departure, a.Toils[2].Executor, "existing toil owns departure");
            c.Equal(Fix.FromInt(8), source.Get<IOutletPort>().ReservedOut(Supply(game)), "only actual haul reserves inventory");
            hash = game.Hash(); int next = Board(game).NextJobId;
            var preview = Hauling(game).PreviewHaul(new PlayerId(0), source.Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8));
            c.Equal(WorkDockCodes.Full, preview.Blocker, "ordinary activity slots block native haul admission");
            c.Equal(0, Hauling(game).AcquireHaul(new PlayerId(0), source.Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), 100), "full mixed facility admits no second haul");
            c.Equal(next, Board(game).NextJobId, "refused mixed commitment allocates no job");
            c.Equal(hash, game.Hash(), "full mixed refusal creates no receipt or slot");
            var spare = Candidates(game).Find(value => value.Worker == Find(game, "machine.d").Id);
            c.Equal(WorkDockCodes.Full, spare.Result.Blocker, "ordinary candidate preview uses same live shared capacity");
            WorkJobView retained = Read(game, a.Id); c.That(retained.DockWaiting, "existing private work facts identify activity waiting");
            var hidden = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(hidden);
            c.Equal(0, hidden.Count, "foreign company cannot read service jobs/claims");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(source), "same removal gate protects haul and activity commitments");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "all mixed claims/itineraries restore through sole codec");
            Code accepted = b.DestinationDockSlot;
            b.DestinationDockSlot = haul.SourceDockSlot; c.Throws<InvalidOperationException>(() => game.Save(), "cross-kind duplicate bay cannot checkpoint");
            b.DestinationDockSlot = new Code("missing"); c.Throws<InvalidOperationException>(() => game.Hash(), "unknown activity slot cannot hash");
            b.DestinationDockSlot = Code.None; c.Throws<InvalidOperationException>(() => game.Save(), "assigned activity requires its actual dock slot");
            b.DestinationDockSlot = accepted; b.ToilIndex = 1; c.Throws<InvalidOperationException>(() => game.Save(), "waiting activity cannot claim to be servicing");
            b.ToilIndex = 0; Toil departure = b.Toils[2]; b.Toils[2] = Toil.Work(source.Id);
            c.Throws<InvalidOperationException>(() => game.Save(), "checkpoint rejects malformed service itinerary"); b.Toils[2] = departure;
            c.That(game.Save().Length > 0, "restored fault instrumentation returns to legal shared state");
            c.Equal(Code.None, game.World.System<WorkSystem>().Interrupt(game.World, a), "existing interruption reopens ordinary service work");
            c.That(a.Worker == EntityId.None && a.DestinationDockSlot.IsNone && a.State == JobState.Open, "reopened activity releases only its owned slot");
            c.Equal(new Code("load.wait.a"), retained.DestinationDockSlot, "retained private facts remain frozen");
            c.Equal(new Code("load.bay"), haul.SourceDockSlot, "activity interruption cannot release native haul bay");
            var premature = New(Factory()); Order earlyOrder = Post(premature); Job earlyJob = Board(premature).Job(earlyOrder.Node("a").JobId);
            earlyJob.ToilIndex = 2; c.Throws<InvalidOperationException>(() => premature.Save(), "activity departure cannot checkpoint unearned work");
            earlyJob.ToilIndex = 0; c.That(premature.Save().Length > 0, "restored phase instrumentation returns to earned-work protocol");
            InvalidContent(c); WorkOnly(c); ProviderGuards(c); Undocked(c);
        }
        private static void InvalidContent(Check c)
        {
            foreach (string key in new[] { "work_roles", "resources" })
            {
                c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Remove(key))), "explicit dock array required " + key);
                foreach (var invalid in new[] { JsonValue.Null(), JsonValue.Of("inspect"), JsonValue.Number("1"), Content.Json.Parse("[true]") })
                    c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Set(key, invalid))), "typed dock array refuses " + key + "/" + invalid);
                c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Set(key, Content.Json.Parse("[\"missing\"]")))), "unknown registered reference refuses " + key);
            }
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Set("work_roles", Content.Json.Parse("[\"inspect\",\"inspect\"]")))), "duplicate work role refuses");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Set("resources", Content.Json.Parse("[\"supply\",\"supply\"]")))), "duplicate resource refuses");
            c.Throws<ContentException>(() => New(Factory(root => { DockParts(root).Set("work_roles", Content.Json.Parse("[]")); DockParts(root).Set("resources", Content.Json.Parse("[]")); })), "empty service declaration does not invent a role");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Set("slots", Content.Json.Parse("[]")))), "missing geometry refuses");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Remove("slots"))), "missing slots refuse explicitly rather than a null dereference");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Get("slots")[1].Set("id", JsonValue.Of("load.bay")))), "duplicate physical slot refuses");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Get("slots")[1].Set("offset", JsonValue.Number("1")))), "overlapping physical slots refuse");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Get("slots")[0].Set("kind", JsonValue.Of("waiting")))), "actual service slot required");
            c.Throws<ContentException>(() => New(Factory(root =>
            { var slots = DockParts(root).Get("slots"); for (int i = 3; i < slots.Count; i++) slots[i].Set("kind", JsonValue.Of("waiting")); })), "actual departure slot required");
            c.Throws<ContentException>(() => New(Factory(root => DockParts(root).Get("slots")[0].Set("offset", JsonValue.Number("101")))), "geometry respects authored space extent");
            var game = New(Factory(root => DockParts(root).Set("work_roles", Content.Json.Parse("[\"haul\"]"))));
            foreach (var value in Candidates(game)) c.Equal(WorkDockCodes.Incompatible, value.Result.Blocker, "ordinary selector cannot substitute unsupported service role");
        }
        private static void WorkOnly(Check c)
        {
            var factory = Factory(root => { DockParts(root).Set("resources", Content.Json.Parse("[]")); root.Get("entities")[0].Get("components").Remove("point_stock"); });
            var game = New(factory); var source = Find(game, "source"); Order order = Post(game);
            c.That(source.Get<IWorkDock>() != null && source.Get<IInletPort>() == null && source.Get<IOutletPort>() == null, "work-only facility has no fabricated resource endpoint");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(source), "ordinary service alone protects dock removal");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "work-only actual commitments checkpoint without ports");
            Reach(game, () => !order.IsActive, "work-only physical activity");
            c.Equal(OrderState.Done, order.State, "work-only facility completes ordinary authored nodes");
            c.Equal(Code.None, game.World.CheckDespawn(source), "completed work-only dock holds no hidden queue claims");
        }
        private static void ProviderGuards(Check c)
        {
            foreach (string fault in new[] { "request-touch", "vacancy-touch", "negative-cost", "invalid-reach" })
            {
                var game = New(Factory(root => DockParts(root).Set("fault", JsonValue.Of(fault))));
                c.Throws<InvalidOperationException>(() => Candidates(game), "same candidate path refuses faulty public dock " + fault);
                c.Equal(0, Board(game).Jobs.Count, "bad provider creates no service job " + fault);
            }
            var physical = New(Factory(root => DockParts(root).Set("fault", JsonValue.Of("endpoint-approach-touch"))));
            Post(physical); physical.Update();
            c.That(physical.Fault != null, "normal engine Update faults impure endpoint approach");
            c.Throws<InvalidOperationException>(() => physical.Save(), "faulted provider cannot checkpoint physical state");
        }
        private static void Undocked(Check c)
        {
            var factory = Factory(root => root.Get("entities")[0].Get("components").Remove("point_dock")); var game = New(factory); Order order = Post(game);
            foreach (var node in order.Nodes)
            { Job job = Board(game).Job(node.JobId); c.Equal(2, job.Toils.Count, "deliberately undocked activity has ordinary itinerary"); c.That(job.DestinationDockSlot.IsNone, "undocked work creates no invented facility"); }
            Reach(game, () => !order.IsActive, "undocked ordinary continuation"); c.Equal(OrderState.Done, order.State, "ordinary endpoint work remains engine-owned");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current undocked jobs restore without legacy loader");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var untouched = New(factory); ulong isolated = untouched.Hash();
            Job haul = Haul(game); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            Entity first = Actor(game, haul), second = Actor(game, a), third = Actor(game, b);
            Reach(game, () => haul.HandlingProgress.WholeTicks >= 2 && second.Get<PointPresence>().Position == Fix.FromInt(3) && third.Get<PointPresence>().Position == Fix.FromInt(4), "mixed physical waiting");
            c.Equal(Fix.One, first.Get<PointPresence>().Position, "haul services actual bay, not endpoint center");
            c.Equal(WorkProgress.Zero, order.Node("a").Progress, "queued ordinary node earns no remote work");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "parallel younger node earns no queued work");
            c.Equal(WorkDockCodes.Waiting, a.Blocker, "ordinary activity reports engine waiting blocker");
            c.Equal(WorkDockCodes.Waiting, order.Node("a").Blocker, "same activity graph reports physical queue blocker");
            c.Equal(WorkDockCodes.Waiting, order.Blocker, "owning order reports physical queue blocker");
            c.Equal(new Code("point.blocked"), second.State, "public worker resolves stopped waiting in Entity.Update");
            c.Equal(isolated, untouched.Hash(), "interleaved other session has no shared process-global dock state");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mixed physical waiting saves all native state");
            Reach(game, () => order.Node("a").Progress.WholeTicks >= 2, "oldest activity starts after haul departure", twin);
            c.Equal(Fix.One, second.Get<PointPresence>().Position, "oldest ordinary node works at actual service location");
            c.Equal(new Code("load.bay"), a.DestinationDockSlot, "ordinary node uses released actual haul bay");
            c.Equal(new Code("load.wait.b"), b.DestinationDockSlot, "younger ordinary waiter cannot overtake arrived older node");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "cross-kind promotion never grants overlapping work");
            c.That(first.Get<PointPresence>().Position != Fix.One, "haul physically cleared before activity service");
            WorkProgress progress = order.Node("a").Progress; Policy(c, game, second.Id, false, twin);
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, order.Node("a").Progress, "paused service earns no time");
            c.Equal(new Code("load.bay"), a.DestinationDockSlot, "paused ordinary service claim remains exclusive");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "younger waiter cannot preempt paused bay");
            c.Equal(game.Hash(), twin.Hash(), "paused mixed queue continues identically");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "paused ordinary facility work restores");
            Policy(c, game, second.Id, true, twin);
            Reach(game, () => a.ToilIndex == 2, "completed activity awaiting physical exit", twin);
            c.That(order.Node("a").State != NodeState.Done && a.IsLive && second.Get<IWorker>().Job == a.Id, "work completion retains machine/node until physical departure");
            c.Equal(game.World.Time.Ticks(order.Plan.Graph.Find("a").Duration), order.Node("a").Progress.WholeTicks, "service takes exact authored game time");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "completed-work departure stage checkpoints");
            Reach(game, () => !order.IsActive && haul.State == JobState.Done, "mixed haul and sequential service completion", twin);
            c.Equal(OrderState.Done, order.State, "both authored activity nodes finish after real exit");
            c.Equal(Fix.FromInt(24), Stock(game, "source"), "mixed activity creates no extra pickup");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "one actual haul delivers one exact batch");
            c.Equal(Fix.Zero, first.Get<ITransporter>().Cargo.Amount(Supply(game)), "completed haul has no stranded duplicate cargo");
            c.That(a.DestinationDockSlot.IsNone && b.DestinationDockSlot.IsNone && haul.SourceDockSlot.IsNone && haul.DestinationDockSlot.IsNone, "finished mixed jobs leave no hidden slot claims");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved mixed completion is deterministic");
            Cancelled(c); Removed(c); BlockedDeparture(c);
        }
        private static void Cancelled(Check c)
        {
            var factory = Factory(); var game = New(factory); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            Reach(game, () => order.Node("a").Progress.WholeTicks >= 2 && b.Blocker == WorkDockCodes.Waiting, "occupied service cancellation setup");
            Entity actor = Actor(game, a); WorkProgress progress = order.Node("a").Progress;
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "normal player cancellation ends queued service order");
            c.That(!a.IsLive && !b.IsLive && a.DestinationDockSlot.IsNone && b.DestinationDockSlot.IsNone, "cancellation releases actual claims, no second queue cleanup");
            c.Equal(progress, order.Node("a").Progress, "cancellation preserves earned work history without fake completion");
            Policy(c, game, actor.Id, true); // Keep this actor on inspect roles; another actual carrier must approach the occupied bay.
            Job haul = Haul(game); c.Equal(new Code("load.wait.b"), haul.SourceDockSlot, "released activity bay and first waiter remain physically occupied; native haul selects actual vacant position");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "physical occupant plus native waiter saves");
            foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = actor.Id, Home = Find(current, "destination").Id }).Accepted, "normal engine home order clears idle physical occupant");
            Reach(game, () => haul.State == JobState.Done, "native service after cancelled actor physically leaves", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "cancelled ordinary work never duplicates native receipt");
            c.Equal(game.Hash(), twin.Hash(), "cancellation and real clearance continue identically");
        }
        private static void Removed(Check c)
        {
            var factory = Factory(); var game = New(factory); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId);
            Entity source = Find(game, "source"); game.World.Entities.Destroy(source, game.World.Space); game.World.Settle(); // Participant-loss fault injection, not gameplay logic.
            var twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(OrderState.Cancelled, order.State, "removed activity target cancels through existing order owner");
            c.That(!a.IsLive && a.DestinationDockSlot.IsNone, "participant cleanup frees actual ordinary claim");
            c.Equal(game.Hash(), twin.Hash(), "saved target loss cleans up deterministically");
        }
        private static void BlockedDeparture(Check c)
        {
            var factory = Factory(root =>
            {
                StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number("7"));
                DockParts(root).Set("slots", Content.Json.Parse("[{\"id\":\"load.bay\",\"kind\":\"service\",\"offset\":1},{\"id\":\"load.wait.a\",\"kind\":\"waiting\",\"offset\":3},{\"id\":\"load.wait.b\",\"kind\":\"waiting\",\"offset\":4},{\"id\":\"load.exit\",\"kind\":\"departure\",\"offset\":7}]"));
            });
            var game = New(factory); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            Reach(game, () => a.Blocker == WorkDockCodes.DepartureBlocked, "ordinary physical exit blockage");
            c.That(a.IsLive && a.ToilIndex == 2 && order.Node("a").State != NodeState.Done, "completed work cannot finish or release actor through occupied exit");
            c.Equal(Fix.One, Actor(game, a).Get<PointPresence>().Position, "service actor cannot teleport through blocked exit");
            c.Equal(new Code("load.bay"), a.DestinationDockSlot, "blocked activity retains actual service claim");
            c.Equal(WorkDockCodes.DepartureBlocked, order.Node("a").Blocker, "activity graph reports blocked departure without host inference");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "younger activity waits behind physically blocked bay");
            WorkProgress progress = order.Node("a").Progress; var twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, order.Node("a").Progress, "blocked completed service earns no additional work");
            c.Equal(game.Hash(), twin.Hash(), "blocked ordinary departure restores/continues without phantom completion");
            foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = Find(current, "machine.d").Id, Home = Find(current, "destination").Id }).Accepted, "player moves actual exit occupant through existing home logic");
            EntityId firstActor = a.Worker;
            Reach(game, () => a.State == JobState.Done, "vacant physical departure releases first ordinary work", twin);
            foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = firstActor, Home = Find(current, "destination").Id }).Accepted, "player clears finished actor from the single authored exit");
            Reach(game, () => !order.IsActive, "vacant physical departure releases younger ordinary work", twin);
            c.Equal(OrderState.Done, order.State, "work finishes exactly once after exit really clears"); c.Equal(game.Hash(), twin.Hash(), "exit clearance and completion remain identical");
        }
    }
}
