using System.Linq;
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
    // Called only by the existing two exact requirement-expression suites, not registered as a third gate.
    internal static class UtilityWorkTests
    {
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null)
        {
            JsonValue networks = Content.Json.Parse(FrameworkProofContent.Read("network-arbitration.json"));
            networks.Get("networks")[0].Set("capacity_policy", JsonValue.Of("network.shared"));
            networks.Get("networks")[0].Set("demand_policy", JsonValue.Of("utility.next"));
            networks.Get("networks")[0].Get("rates")[0].Set("quantity", JsonValue.Number("4"));
            networks.Get("networks")[0].Get("rates")[0].Get("period").Set("numerator", JsonValue.Number("1"));
            JsonValue consumers = Content.Json.Parse(FrameworkProofContent.Read("network-utilities.json"));
            consumers.Get("entities")[0].Get("components").Get("storage").Set("initial_stock", Content.Json.Parse("{\"steel\":20}"));
            consumers.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("120"));
            JsonValue work = Content.Json.Parse(FrameworkProofContent.Read("utility-work.json")); edit?.Invoke(work);
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                FrameworkProofContent.Read("network-transfers.json"), additionalDocuments: new Dictionary<string, string> {
                    ["network-arbitration.json"] = Text(networks), ["network-utilities.json"] = Text(consumers), ["utility-work.json"] = Text(work) });
        }
        private static SimGame Setup(GameFactory factory, out Entity source, out Entity native, out Entity target, out Entity worker)
        {
            SimGame game = New(factory);
            source = Spawn(game, "arbitration.source", 9, 28); native = Spawn(game, "utility.native", 11, 28);
            target = Spawn(game, "utility.independent", 10, 27); worker = Spawn(game, "utility.worker", 8, 26);
            if (!((Beep.SimEngine.Maps.TileLineGeometry)game.World.Services.Providers<NetworkService>().Single().World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 28), new PlayerId(0)))
                throw new InvalidOperationException("utility work fixture line refused");
            return game;
        }
        private static WorkView View(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static WorkRequirementResult Preview(Check c, SimGame game, Entity target, string plan = "utility.timer")
        {
            var facts = new List<WorkRequirementView>();
            c.Equal(Code.None, View(game).PreviewOrderRequirements(plan, target.Id, facts), "normal engine view previews utility requirement");
            return facts[facts.Count - 1].Result;
        }
        private static Order Post(Check c, SimGame game, EntityId target, string plan)
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target, Plan = plan });
            c.That(result.Accepted, "ordinary player order posts utility-guarded activity");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Advance(SimGame game, int count, SimGame twin = null)
        { for (int i = 0; i < count; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 150 && !done(); i++) Advance(game, 1, twin);
            if (!done())
            {
                var details = new List<string>();
                foreach (Order order in game.World.Services.Get<WorkBoard>().Orders) foreach (ActivityNodeExecution node in order.Nodes)
                    details.Add(order.Id + ":" + node.Id + "=" + node.State + "/" + node.Progress + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " stalled at game time " + game.Tick + ": " + string.Join("; ", details));
            }
        }
        private static void Control(Check c, SimGame game, EntityId entity, string operation, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = entity, Operation = new Code(operation), Enabled = enabled, Target = Fix.One }).Accepted,
                "ordinary engine operation command changes physical utility readiness");
        internal static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity native, out Entity target, out Entity worker); Tick(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact utility-work build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "readiness adds no persistent payload");
            var requirement = game.World.Content.Get<TargetUtilityRequirementDef>("utility.target-ready");
            c.That(ReferenceEquals(requirement, game.World.Content.Get<WorkRequirementDef>(requirement.Id)), "utility leaf aliases existing authored provider catalog");
            c.That(game.World.Content.Get<WorkRequirementDef>("utility.worker-ready") is IWorkerWorkRequirement, "worker readiness uses existing scoped provider contract");
            c.Equal(new Code("meter"), requirement.Consumer, "JSON binds exact named utility owner");
            c.Throws<ContentException>(() => requirement.Read(new JsonReader(Content.Json.Parse("{\"consumer\":\"other\"}"), "probe")), "resolved readiness cannot be reauthored");
            ulong before = game.Hash(); WorkRequirementResult result = Preview(c, game, target);
            c.Equal(WorkRequirementStatus.Pending, result.Status, "actual unsupplied public consumer is pending");
            c.Equal(UtilityCodes.Shortage, result.Blocker, "shortage retains actual consumer blocker");
            c.That(!result.Quantity.HasValue && result.Resources.Count == 0, "readiness invents no aggregate units or reservation bill");
            c.Equal(before, game.Hash(), "pure readiness preview consumes/reserves/advances nothing");
            WorkRequirementResult retained = result; Advance(game, 1); result = Preview(c, game, target);
            c.Equal(WorkRequirementStatus.Satisfied, result.Status, "actual network receipt satisfies utility guard");
            c.Equal(WorkRequirementStatus.Pending, retained.Status, "retained utility outcome is detached from later supply");
            c.Equal(UtilityCodes.Inactive, Preview(c, game, native).Blocker, "idle native processor is not fabricated readiness");
            c.Equal(UtilityWorkRequirementDeclaration.MissingConsumer, Preview(c, game, source).Blocker, "missing capability has typed pending blocker");
            result = Preview(c, game, target, "utility.machine");
            c.Equal(WorkRequirementStatus.Satisfied, result.Status, "normal preview evaluates actual eligible worker candidates");
            var nodeDefinition = game.World.Content.Get<ActivityDef>("utility.machine").Graph.Find("work");
            WorkRequirementResult workerless = new WorkRequirementContext(game.World, target.Owner, target, nodeDefinition)
                .Evaluate(game.World.Content.Get<WorkRequirementDef>("utility.worker-ready"));
            c.Equal(WorkSystem.BlockerNoWorker, workerless.Blocker, "explicit workerless context retains existing no-worker semantics");
            result = Preview(c, game, target, "utility.alternative");
            c.Equal(WorkRequirementStatus.Satisfied, result.Status, "existing any composition selects actual target readiness");
            c.Equal("a.target", result.SelectedEntry, "alternatives retain stable authored occurrence identity");
            var candidates = new List<WorkWorkerCandidate>(); before = game.Hash();
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("utility.machine", "work", target.Id, candidates), "existing candidate view reads worker utility scope");
            WorkWorkerCandidate chosen = candidates.Find(x => x.Worker == worker.Id);
            c.Equal(WorkRequirementStatus.Satisfied, chosen.Result.Status, "actual stocked public utility worker is eligible");
            c.Equal(before, game.Hash(), "candidate readiness reserves no worker or stock");
            var owner = new ReservationOwner("proof.utility-work", worker.Id.Value); ResourceId steel = game.World.Content.ResourceId("steel");
            ReservationToken claim = worker.Get<IInventory>().ReserveOut(owner, steel, Fix.One);
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("utility.machine", "work", target.Id, candidates), "reserved worker stock retains typed candidate diagnostics");
            chosen = candidates.Find(x => x.Worker == worker.Id);
            c.Equal(UtilityCodes.Shortage, chosen.Result.Blocker, "outbound commitments cannot satisfy worker utility readiness");
            worker.Get<IInventory>().ReleaseOut(owner, claim);
            var foreign = new List<WorkRequirementView>();
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<WorkView>().PreviewOrderRequirements("utility.timer", target.Id, foreign), "foreign company cannot preview private utility readiness");
            c.Equal(0, foreign.Count, "private refusal exposes no leaf facts");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("target_utility_requirements")[0].Remove("consumer"),
                root => root.Get("target_utility_requirements")[0].Set("consumer", JsonValue.Of("")),
                root => root.Get("worker_utility_requirements")[0].Set("consumer", JsonValue.Of(" bad ")),
                root => root.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("missing")) })
                c.Throws<ContentException>(() => New(Factory(edit)), "missing/invalid utility identity or requirement reference refuses at content admission");
            target.Get<ProofUtilityConsumer>().Fault = 3;
            c.Throws<InvalidOperationException>(() => Preview(c, game, target), "forged named demand faults pure readiness instead of a fallback value");
            target.Get<ProofUtilityConsumer>().Fault = 0;
        }
        internal static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity native, out Entity target, out Entity worker);
            Order timer = Post(c, game, target.Id, "utility.timer"); ActivityNodeExecution node = timer.Node("work");
            c.Equal(WorkProgress.Zero, node.Progress, "unready work earns no elapsed progress before physical delivery");
            Reach(game, () => node.Progress.WholeTicks >= 2, "actual network delivery admits timed activity");
            var owner = new ReservationOwner("proof.utility-work", target.Id.Value); ResourceId steel = game.World.Content.ResourceId("steel");
            Control(c, game, source.Id, "valve", false);
            IResourceOutlet pool = game.World.Services.Providers<NetworkService>().Single().Component("arbitration.main", 0).Inventory;
            ReservationToken pooled = pool.ReserveOut(owner, steel, pool.Available(steel));
            c.That(pooled.IsValid, "existing pool owner protects queued supply before shortage check");
            ReservationToken claim = target.Get<IInventory>().ReserveOut(owner, steel, Fix.One);
            Advance(game, 1); WorkProgress frozen = node.Progress;
            c.Equal(UtilityCodes.Shortage, node.Blocker, "new outbound promise pauses actual timed activity");
            Advance(game, 3); c.Equal(frozen, node.Progress, "paused readiness never burns work time or resets progress");
            var facts = new List<WorkRequirementView>(); c.That(View(game).TryCopyOrderRequirements(timer.Id, facts), "active diagnostics reuse existing node evaluator");
            c.Equal(node.Blocker, facts[facts.Count - 1].Result.Blocker, "active diagnostics and paused execution use identical utility blocker");
            target.Get<IInventory>().ReleaseOut(owner, claim); pool.ReleaseOut(owner, pooled); Control(c, game, source.Id, "valve", true);
            SimGame twin = factory.Load(game.Save()); Reach(game, () => !timer.IsActive, "released utility promise resumes actual timer", twin);
            c.Equal(OrderState.Done, timer.State, "same utility-guarded timer physically finishes"); c.Equal(game.Hash(), twin.Hash(), "saved readiness continuation matches without a second ledger");
            c.Equal(Fix.FromInt(2), target.Get<IInventory>().Amount(steel), "readiness guard never separately consumes utility stock");

            game = Setup(factory, out source, out native, out target, out worker);
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = native.Id, Capability = new Code("meter"), Transformation = "utility.consume", Repeat = false }).Accepted,
                "normal player command starts the actual native consumption owner");
            timer = Post(c, game, native.Id, "utility.timer"); node = timer.Node("work");
            Reach(game, () => node.Progress.WholeTicks >= 2, "native active consumer admits timed work");
            c.That(native.Get<IInventory>().Amount(steel) < Fix.FromInt(20), "native consumer physically burns authored stock while work runs");
            Control(c, game, native.Id, "meter", false); frozen = node.Progress; Fix stock = native.Get<IInventory>().Amount(steel);
            twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(frozen, node.Progress, "disabled actual consumer pauses activity without its own clock");
            c.Equal(UtilityCodes.Inactive, node.Blocker, "paused native work exposes inactive consumer blocker");
            c.Equal(stock, native.Get<IInventory>().Amount(steel), "disabled work guard causes neither utility burn nor duplicate stock movement");
            c.Equal(game.Hash(), twin.Hash(), "saved disabled utility and activity remain identical");
            Control(c, game, native.Id, "meter", true); Control(c, twin, native.Id, "meter", true);
            Reach(game, () => !timer.IsActive, "native utility resume", twin);
            c.Equal(OrderState.Done, timer.State, "native readiness restores the same attained work"); c.Equal(game.Hash(), twin.Hash(), "native work and utility codecs preserve interleaved continuation");

            game = Setup(factory, out source, out native, out target, out worker); Advance(game, 2);
            Order machine = Post(c, game, target.Id, "utility.machine"); node = machine.Node("work");
            Reach(game, () => node.Progress.WholeTicks >= 2, "worker utility candidate physically reaches activity");
            Job job = game.World.Services.Get<WorkBoard>().Job(node.JobId);
            c.Equal(worker.Id, job.Worker, "existing dispatcher selects actual utility-ready compatible machine");
            owner = new ReservationOwner("proof.utility-work", worker.Id.Value); claim = worker.Get<IInventory>().ReserveOut(owner, steel, Fix.One);
            Advance(game, 1); frozen = node.Progress; Advance(game, 3);
            c.Equal(frozen, node.Progress, "worker utility shortage pauses same physical job"); c.Equal(UtilityCodes.Shortage, node.Blocker, "worker shortage has actual typed blocker");
            worker.Get<IInventory>().ReleaseOut(owner, claim); twin = factory.Load(game.Save());
            Reach(game, () => !machine.IsActive, "worker utility resume", twin);
            c.Equal(OrderState.Done, machine.State, "same physically assigned utility worker completes"); c.Equal(game.Hash(), twin.Hash(), "worker readiness and saved physical work have identical state");
            c.Equal(Fix.FromInt(2), worker.Get<IInventory>().Amount(steel), "worker readiness is not a duplicate consumption path");
            c.That(worker.Get<IWorker>().Idle, "completion releases the actual machine through existing job owner");
            machine = Post(c, game, target.Id, "utility.machine"); Advance(game, 1);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = machine.Id }).Accepted, "ordinary cancellation releases utility-guarded job");
            c.Equal(OrderState.Cancelled, machine.State, "cancelled utility work cannot silently restart");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled utility work persists through sole existing codec");
            long now = game.Tick; ulong hash = game.Hash(); game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(now, game.Tick, "sole paused game clock stops work/utility time"); c.Equal(hash, game.Hash(), "utility requirements own no independent clock");
            game = Setup(factory, out source, out native, out target, out worker); Advance(game, 2);
            timer = Post(c, game, target.Id, "utility.timer"); target.Get<ProofUtilityConsumer>().Fault = 3;
            game.Update();
            c.That(game.Fault != null && game.Fault.Phase == "sim.work", "malformed active utility provider faults existing work execution owner");
            c.Throws<InvalidOperationException>(() => game.Save(), "malformed active utility readiness cannot checkpoint healthy");
        }
    }
}
