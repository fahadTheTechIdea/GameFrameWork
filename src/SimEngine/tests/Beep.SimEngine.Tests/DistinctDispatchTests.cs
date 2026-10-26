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
    internal static class DistinctDispatchTests
    {
        internal static void Register()
        {
            TestRunner.Add("Distinct dispatch: candidate contracts, ranked matching and refusal", Contracts);
            TestRunner.Add("Distinct dispatch: parallel work, logistics and saved continuation", Physical);
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static GameFactory Mixed(bool provider)
            => DispatchAgingTests.Factory(shipment: provider, shipmentPriority: 100,
                workDefinitions: FrameworkProofContent.Read("shared-dispatch.json").Replace("\"enabled\":false", "\"enabled\":true"), edit: root =>
                {
                    var clone = root.Get("entities")[2].Clone(); clone.Set("id", JsonValue.Of("courier"));
                    clone.Get("components").Get("worker").Set("roles", Json.Parse("[\"haul\"]"));
                    root.Get("entities").Add(clone); StartupContentAuthoring.Add(root, "courier", "courier", 0, clone.Get("components").Get("point_presence"));
                });
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("distinct dispatch fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Worker + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static void PostOrdinary(SimGame game)
            => game.Seat(0).Submit(new PostOrderCommand { Plan = "shared.work", Target = Find(game, "destination").Id, Priority = 0 });
        private static void Demand(SimGame game)
            => game.Seat(0).Submit(new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 100, LogisticsReserveAccess.Protected)) });
        private static List<WorkerDispatchOffer> Candidates(SimGame game, WorkerDispatchOffer offer)
        { var candidates = new List<WorkerDispatchOffer>(); game.World.System<WorkSystem>().CopyAssignmentCandidates(game.World, offer, candidates); return candidates; }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "distinct dispatch has current deterministic identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking behavior uses exact current saves");
            Matching(c);
            foreach (bool provider in new[] { false, true })
            {
                var game = New(Mixed(provider)); var logistics = game.World.Services.Get<LogisticsService>();
                Entity courier = Find(game, "courier"), machine = Find(game, "machine");
                var haul = game.World.Services.Get<IHaulExecution>(); ulong hash = game.Hash(); long age = game.Clock.StepIndex;
                var preview = haul.PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), worker: courier.Id);
                c.Equal(HaulStatus.Ready, preview.Status, "explicit eligible carrier is previewed by actual haul owner");
                c.Equal(courier.Id, preview.Carrier, "selected carrier is not substituted by smaller flexible actor");
                c.Equal(HaulStatus.NoCarrier, haul.PreviewHaul(new PlayerId(0), preview.Source, preview.Destination, Supply(game), Fix.FromInt(8), worker: new EntityId(int.MaxValue)).Status,
                    "removed/unresolved explicit carrier never falls back");
                c.Equal(HaulStatus.InvalidRequest, haul.PreviewHaul(new PlayerId(0), preview.Source, preview.Destination, Supply(game), Fix.FromInt(8), worker: new EntityId(-1)).Status,
                    "malformed selector is not automatic selection");
                c.Equal(hash, game.Hash(), "selected-carrier reads do not reserve materials or machines"); c.Equal(age, game.Clock.StepIndex, "queries cannot advance another clock");
                c.That(ReferenceEquals(game.Clock, game.World.Clock), "one session clock feeds world and game");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "one work owner has current dispatch semantics");
                c.Equal(StateCodecTests.ExpectedLogisticsSchema, logistics.StateSchemaVersion, "existing logistics codec owns selected acquisition");
                if (provider)
                {
                    var offers = Queue(game); var offer = offers[0]; var candidates = Candidates(game, offer);
                    c.Equal(2, candidates.Count, "public-only source supplies actual alternative carrier edges");
                    c.Equal(machine.Id, candidates[0].Worker, "source preserves original preferred edge");
                    c.Equal(courier.Id, candidates[1].Worker, "public provider exposes distinct alternative");
                    c.Equal(hash, game.Hash(), "candidate admission remains a pure observation");
                    var foreign = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(foreign);
                    c.Equal(0, foreign.Count, "private view cannot leak competing source candidates");
                    var source = game.World.Services.Get<DispatchProofSource>();
                    foreach (int fault in new[] { 1, 2, 3 })
                    {
                        source.CandidateFault = fault;
                        c.Throws<InvalidOperationException>(() => Candidates(game, offer), "duplicate/missing/mismatched candidate contract refuses: " + fault);
                        c.Equal(hash, game.Hash(), "contract rejection acquires nothing");
                    }
                    source.CandidateFault = 0;
                    c.That(!source.TryAcquire(offer.Identity, offer.Entry, EntityId.None, WorkCandidateMode.IdleOnly), "dispatch acquisition requires explicit selected worker");
                    c.That(!source.TryAcquire(offer.Identity, offer.Entry, new EntityId(int.MaxValue), WorkCandidateMode.IdleOnly), "stale explicit identity refuses without substitution");
                    c.Equal(hash, game.Hash(), "fresh refusals retain stock, claims and jobs");
                    c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = courier.Id, Settings = new WorkPolicySettings(false, 0,
                        new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player disables selected actor through command");
                    hash = game.Hash(); c.That(!source.TryAcquire(offer.Identity, offer.Entry, courier.Id, WorkCandidateMode.IdleOnly), "retained candidate cannot acquire now-disabled actor");
                    c.Equal(hash, game.Hash(), "disabled selection refuses before claims");
                }
                else
                {
                    int job = haul.AcquireHaul(new PlayerId(0), preview.Source, preview.Destination, Supply(game), Fix.FromInt(8), 100, worker: courier.Id);
                    c.That(job > 0, "engine-side command executor can acquire explicitly selected carrier");
                    c.Equal(courier.Id, Board(game).Job(job).Worker, "real native job preserves selected carrier");
                    c.Equal(0, machine.Get<IWorker>().Job, "selected acquisition does not take another compatible machine");
                    hash = game.Hash(); c.Equal(0, haul.AcquireHaul(new PlayerId(0), preview.Source, preview.Destination, Supply(game), Fix.FromInt(8), 100, worker: courier.Id), "busy selected carrier refuses");
                    c.Equal(hash, game.Hash(), "refusal creates no duplicate job/receipts");
                    c.Equal(Fix.FromInt(8), Find(game, "source").Get<IOutletPort>().ReservedOut(Supply(game)), "selected haul reserves actual source stock once");
                    c.Equal(Fix.FromInt(8), courier.Get<ITransporter>().Cargo.ReservedIn(Supply(game)), "selected haul reserves selected cargo capacity once");
                }
            }
        }
        private static void Matching(Check c)
        {
            var a = new EntityId(1); var b = new EntityId(2); var d = new EntityId(3);
            var preferred = DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { b, a } });
            c.Equal(b, preferred[0].Worker, "candidate preference is retained instead of re-sorting by worker ID");
            var chain = DistinctWorkerMatching.Solve(new[] { 1, 1, 1 }, new IReadOnlyList<EntityId>[] { new[] { a, b }, new[] { b, d }, new[] { a } });
            c.Equal(3, chain.Count, "augmenting chain covers all distinct compatible demands");
            c.Equal(2, chain[0].Demand, "specialist edge releases flexible demand through full chain");
            var scarce = DistinctWorkerMatching.Solve(new[] { 1, 1 }, new IReadOnlyList<EntityId>[] { new[] { a }, new[] { a } });
            c.Equal(1, scarce.Count, "one worker cannot satisfy simultaneous multiple jobs"); c.Equal(0, scarce[0].Demand, "higher-ranked intent retains coverage under shortage");
            for (int mask = 0; mask < 512; mask++)
            {
                var rows = new IReadOnlyList<EntityId>[3];
                for (int i = 0; i < 3; i++) { var row = new List<EntityId>(); for (int j = 0; j < 3; j++) if ((mask & (1 << (i * 3 + j))) != 0) row.Add(new EntityId(j + 1)); rows[i] = row; }
                var matches = DistinctWorkerMatching.Solve(new[] { 1, 1, 1 }, rows);
                int best = 0, preferredMask = 0;
                for (int x = -1; x < 3; x++) for (int y = -1; y < 3; y++) for (int z = -1; z < 3; z++)
                {
                    int[] choice = { x, y, z }; bool valid = true; int count = 0; var seen = new HashSet<int>();
                    for (int i = 0; i < 3; i++) if (choice[i] >= 0) { count++; if ((mask & (1 << (i * 3 + choice[i]))) == 0 || !seen.Add(choice[i])) valid = false; }
                    int coverage = (x >= 0 ? 4 : 0) | (y >= 0 ? 2 : 0) | (z >= 0 ? 1 : 0);
                    if (valid && (count > best || count == best && coverage > preferredMask)) { best = count; preferredMask = coverage; }
                }
                c.Equal(best, matches.Count, "all small bipartite graphs match exhaustive capacity oracle: " + mask);
                int actualMask = 0; foreach (DistinctWorkerMatch match in matches) actualMask |= 1 << (2 - match.Demand);
                c.Equal(preferredMask, actualMask, "earlier ranked demands preserve lexicographic coverage: " + mask);
            }
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { a, a } }), "duplicate candidate edges refuse");
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { -1 }, new IReadOnlyList<EntityId>[] { new[] { a } }), "negative demands refuse");
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { EntityId.None } }), "invalid worker identity refuses");
        }
        private static void Physical(Check c)
        {
            foreach (bool native in new[] { false, true }) foreach (bool policy in new[] { false, true }) foreach (bool worker in new[] { false, true })
            {
                var factory = IncidentPreparednessTests.Fixture(native, policy, worker); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
                Order order = Raise(c, game); Until(game, () => order.Node("a.flex").JobId != 0 && order.Node("b.inspect").JobId != 0, "parallel jobs posted");
                Job flex = Board(game).Job(order.Node("a.flex").JobId), inspect = Board(game).Job(order.Node("b.inspect").JobId);
                c.Equal(JobState.Assigned, flex.State, "flexible frontier starts physically without specialist starvation");
                c.Equal(JobState.Assigned, inspect.State, "specialist frontier starts in same game update");
                c.Equal(Find(game, "prepared.worker.1").Id, flex.Worker, "actual flexible node uses alternative haul-only worker");
                c.Equal(Find(game, "machine").Id, inspect.Worker, "actual inspection keeps uniquely compatible machine");
                c.That(flex.Worker != inspect.Worker, "physical parallel nodes own distinct actors");
                c.That(order.State != OrderState.Done, "allocation cannot instantly complete physical work");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "parallel real job/claims restore without matching state");
                long age = game.Clock.StepIndex; ulong paused = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(age, game.Clock.StepIndex, "sole paused game clock freezes both assignments"); c.Equal(paused, game.Hash(), "paused dispatch creates no independent progress");
                game.Clock.Resume(); twin.Clock.Resume(); Until(game, () => order.State == OrderState.Done, "parallel physical response completion", twin);
                c.Equal(Fix.FromInt(4), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "shared inputs consumed once by native work owner");
                c.Equal(game.Hash(), twin.Hash(), "saved parallel continuation is deterministic"); c.Equal(untouched, isolated.Hash(), "interleaved sessions do not share matching state");
            }
            foreach (bool provider in new[] { false, true })
            {
                var factory = Mixed(provider); var game = New(factory); PostOrdinary(game); if (!provider) Demand(game); Step(game);
                Job hauling = Board(game).Job(Find(game, "courier").Get<IWorker>().Job), work = Board(game).Job(Find(game, "machine").Get<IWorker>().Job);
                c.That(hauling?.IsHaul == true && hauling.State == JobState.Assigned, "higher-ranked haul uses alternative selected carrier: " + provider);
                c.That(work != null && !work.IsHaul && work.State == JobState.Assigned, "same pass realizes lower-ranked specialist work: " + provider);
                c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "one matched haul owns actual destination claim");
                c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "matching is not instant delivery");
                var twin = factory.Load(game.Save()); Until(game, () => Find(game, "destination").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(8) && work.State == JobState.Done,
                    "mixed-provider travel, timed work, load and delivery", twin);
                c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInletPort>().Amount(Supply(game)), "real hauling conserves material");
                c.Equal(game.Hash(), twin.Hash(), "mixed source continuation matches saved twin");
            }
            var shortageFactory = IncidentPreparednessTests.Fixture(fleet: new[] { 3 }); var shortage = New(shortageFactory); Order sequential = Raise(c, shortage);
            var restored = shortageFactory.Load(shortage.Save()); Until(shortage, () => sequential.State == OrderState.Done, "one-worker shortage legitimately sequences parallel intent", restored);
            c.Equal(shortage.Hash(), restored.Hash(), "shortage has no duplicated worker or deadlock");
            SharedInputs(c); Interruption(c); IdlePreferred(c); LiveGate(c);
        }
        private static void SharedInputs(Check c)
        {
            var factory = IncidentPreparednessTests.Fixture(document: root =>
            { root.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("prepared.inputs")); },
                edit: root =>
                {
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("4"));
                    root.Get("scenarios")[0].Get("settings").Get(CompanyAlertModule.ModuleId).Get("companies")[0].Set("worker_reserves", Json.Parse("[]"));
                });
            var game = New(factory); Order order = Raise(c, game); var twin = factory.Load(game.Save());
            Until(game, () => order.Node("a.flex").State == NodeState.Done, "first real admission wins contested stock", twin);
            c.That(order.Node("b.inspect").State != NodeState.Done && order.Node("restart").State != NodeState.Done, "maximum workers cannot duplicate a shared consumed bill");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "one actual bill consumes exactly available stock");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IInletPort>().Amount(Supply(game)), "matching does not fabricate supply or bypass authored automatic-supply flag");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "prepared.worker.1").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "player explicitly allows released courier to perform ordinary replenishment");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(4), Fix.FromInt(40), 100, LogisticsReserveAccess.Protected)) }).Accepted,
                "player replenishes shortage using physical logistics intent");
            Until(game, () => Find(game, "source").Get<IInletPort>().Amount(Supply(game)) == Fix.FromInt(16), "real courier loads replenishment");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.One, Fix.FromInt(4), Fix.FromInt(40), 100, LogisticsReserveAccess.Protected)) }).Accepted,
                "player stops repeat replenishment without cancelling acquired physical load");
            twin = factory.Load(game.Save()); Until(game, () => order.State == OrderState.Done, "contended work waits for physical replenishment", twin);
            c.Equal(game.Hash(), twin.Hash(), "material contention continues from current claims only");
            c.Equal(Fix.FromInt(16), Find(game, "source").Get<IInletPort>().Amount(Supply(game)), "physical replenishment transfers consumed four units once");
        }
        private static void Interruption(Check c)
        {
            var factory = IncidentPreparednessTests.Fixture(edit: root =>
                root.Get("scenarios")[0].Get("settings").Get(CompanyAlertModule.ModuleId).Get("companies")[0].Set("worker_reserves", Json.Parse("[]")));
            var game = New(factory); Configure(c, game); Order old = Post(c, game); Job incumbent = Assigned(game);
            c.That(incumbent != null && incumbent.OrderId == old.Id, "ordinary physical incumbent is acquired through player command");
            Order response = Raise(c, game);
            c.Equal(JobState.Open, incumbent.State, "real emergency interruption returns old job to authoritative queue");
            c.Equal(Find(game, "machine").Id, Board(game).Job(response.Node("b.inspect").JobId).Worker, "selected specialist identity survives actual interruption");
            c.Equal(Find(game, "prepared.worker.1").Id, Board(game).Job(response.Node("a.flex").JobId).Worker, "idle flexible alternative works in parallel with preempted specialist");
            var twin = factory.Load(game.Save()); Until(game, () => response.State == OrderState.Done && old.State == OrderState.Done, "physical preemption and old work resume", twin);
            c.Equal(game.Hash(), twin.Hash(), "matching adds no separate interruption state on restore");
        }
        private static void LiveGate(Check c)
        {
            var factory = IncidentPreparednessTests.Fixture(); var game = New(factory); Order order = Raise(c, game);
            c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine").Id, true, Fix.Zero).Accepted, "player stops selected specialist operation");
            var twin = factory.Load(game.Save()); Until(game, () => order.Node("a.flex").State == NodeState.Done, "unblocked parallel actor completes independently", twin);
            c.That(order.Node("b.inspect").State != NodeState.Done, "live zero-rate operation cannot advance selected specialist work");
            c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine").Id, true, Fix.One).Accepted, "player restores effective operation");
            twin = factory.Load(game.Save()); Until(game, () => order.State == OrderState.Done, "selected specialist resumes actual progress", twin);
            c.Equal(game.Hash(), twin.Hash(), "live gate changes preserve saved physical continuation");
        }
        private static void IdlePreferred(Check c)
        {
            var factory = IncidentPreparednessTests.Fixture(fleet: new[] { 3, 3 }, document: root =>
            {
                root.Get("activities")[0].Set("nodes", Json.Parse("[{\"id\":\"a.flex\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}}]"));
                root.Get("incident_workflows")[0].Set("phases", Json.Parse("[{\"id\":\"repair\",\"nodes\":[\"a.flex\"]}]"));
            }, edit: root => root.Get("scenarios")[0].Get("settings").Get(CompanyAlertModule.ModuleId).Get("companies")[0].Set("worker_reserves", Json.Parse("[]")));
            var game = New(factory); Configure(c, game); Entity idle = Find(game, "prepared.worker.1");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = idle.Id, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "player enables second actual worker");
            Order old = Post(c, game); Job incumbent = Assigned(game);
            game.Seat(0).Submit(new SubmitPlanCommand { Intent = new PlanIntent("activity:preempt.work", Json.Parse("{\"target\":" + Find(game, "source").Id.Value +
                ",\"workers\":[{\"node\":\"work\",\"role\":null,\"worker\":" + idle.Id.Value + "}]}"), "ordinary", 0, false) });
            game.Seat(0).Submit(new RaiseIncidentCommand { Target = Find(game, "destination").Id, Source = new Code("damage"), Kind = new Code("proof.failure") });
            Step(game); Order response = Board(game).ActiveResponseOn(Find(game, "destination").Id, new Code("damage"));
            c.Equal(incumbent.Id, Assigned(game)?.Id ?? 0, "capacity matching does not interrupt incumbent merely to fit lower-ranked work");
            c.Equal(idle.Id, Board(game).Job(response.Node("a.flex").JobId).Worker, "emergency prefers truly idle compatible worker");
            var twin = factory.Load(game.Save()); Until(game, () => response.State == OrderState.Done && old.State == OrderState.Done, "idle-preferred response and ordinary continuation", twin);
            c.Equal(game.Hash(), twin.Hash(), "idle preference remains deterministic through save/continue");
        }
    }
}
