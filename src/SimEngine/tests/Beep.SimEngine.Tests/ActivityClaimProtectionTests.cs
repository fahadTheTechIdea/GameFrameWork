using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // JSON intent and public player facts only; original WorkBoard owns every quantum and promise.
    internal static class ActivityClaimProtectionTests
    {
        internal const string Case = "Activity claim protection: complete retained tools guard running acquisition and productive consumption";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(6 * 1024 * 1024, 3000);
        internal static GameFactory Factory(bool half) => RecipeDisclosureTests.Factory(false, "result", half,
            setupEdit: root => root.Get("entities")[1].Get("components").Set("pool_port", Json.Parse("[{\"id\":\"gp.tool\",\"pool\":\"gp.tools\",\"alternatives\":[],\"endpoint_scope\":\"proof.pools\",\"original_access\":\"retained\"},{\"id\":\"gp.flow\",\"pool\":\"gp.flow\",\"alternatives\":[],\"endpoint_scope\":\"proof.pools\",\"original_access\":\"retained\"}]")),
            knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", JsonValue.NewArray()),
            extraModules: new GameModule[] { new EndpointProofModule(), new OutputPortProofModule() },
            extraDeclarations: "[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"activity-claim-protection.json\"]},{\"id\":\"proof.output-ports\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"proof.pools\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
            extraDocuments: new Dictionary<string, string> { ["activity-claim-protection.json"] = FrameworkProofContent.Read("activity-claim-protection.json") });
        private static EntityId Entity(SimGame g, string name) => TransformationContentContractTests.Entity(g, name);
        private static ResourceId Resource(SimGame g) { if (!g.Observer.TryFindResource("supply", out var id)) throw new InvalidOperationException("actual supply missing"); return id; }
        private static ResourceEndpointId Pool(long id) => new ResourceEndpointId("proof.pools", id);
        private static EndpointPoolDiagnostics Facts(SimGame g) => g.Seat(0).View.Feature<EndpointPoolDiagnostics>();
        private static Bill Quantity(SimGame g, int amount) => new Bill(new[] { new ResourceAmount(Resource(g), Fix.FromInt(amount)) });
        private static void Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Seed(Check c, SimGame g, long pool, int amount)
            => Submit(c, g, new TransferProbeCommand { Move = true, Movements = new[] { new ResourceMovementIntent(
                new ReservationOwner("proof.gp.seed", g.Clock.StepIndex + 100), Company, ResourceEndpointId.ForEntity(Entity(g, "source")),
                Pool(pool), Quantity(g, amount), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } });
        private static void Step(SimGame g, SimGame twin = null)
        { g.Update(); twin?.Update(); if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash()) throw new InvalidOperationException("protected activity Update/" + g.Fault + "/" + twin?.Fault); }
        private static WorkView Work(SimGame g) => g.Seat(0).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame g, int order)
        { var list = new List<ActivityNodeView>(); if (!Work(g).TryCopyNodes(order, list)) throw new InvalidOperationException("actual nodes absent"); return list; }
        private static ResourceReservation[] Tools(SimGame g, int order) => Nodes(g, order).SelectMany(x => x.Resources).Where(x => x.Choice.Use == WorkResourceUse.Hold).SelectMany(x => x.Receipts).ToArray();
        private static int Post(Check c, SimGame g)
        { var command = new PostOrderCommand { Plan = "gp.parallel", Target = Entity(g, "contract.factory"), Priority = 30 };
            return checked((int)PermanentRemovalTests.Submit(c, g, command).Arg0); }
        private static bool Complete(SimGame g, int order) => Work(g).TryReadActivityCompletion("activity:gp.parallel", Entity(g, "contract.factory"), out var fact) && fact.Order == order;
        private static void Refuse(Check c, SimGame g, ClaimProbeCommand command, string label)
        { ulong before = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(command), label); c.Equal(before, g.Hash(), "participant refusal is pure"); }
        private static void Protocol(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong idleHash = idle.Hash();
            using var recorder = g.RecordReplay(Limits); Seed(c, g, 1, 3); Seed(c, g, 2, 4);
            var toolOwner = new ReservationOwner("proof.gp.tool", 81); var flowOwner = new ReservationOwner("proof.gp.flow", 82);
            Submit(c, g, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, toolOwner, Pool(1), Quantity(g, 2)) } });
            var tool = Facts(g).Claims(1, toolOwner); var requests = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, flowOwner, Pool(2), Quantity(g, 2)) };
            Refuse(c, g, new ClaimProbeCommand { Intents = requests, Participants = null }, "null participants invalid");
            Refuse(c, g, new ClaimProbeCommand { Intents = requests, Participants = new[] { tool.Records[0], tool.Records[0] } }, "duplicate participant invalid");
            var record = tool.Records[0];
            Refuse(c, g, new ClaimProbeCommand { Intents = requests, Participants = new[] { new ResourceReservation(record.Token, record.Owner, record.Resource, Fix.One, record.Direction) } }, "stale actual participant invalid");
            int sequence = g.Seat(0).Submit(new ClaimProbeCommand { Intents = requests, Participants = tool.Records.ToArray() });
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending protected acquisition command saved"); Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results); c.That(results.Single(x => x.Sequence == sequence).Accepted, "protected acquisition accepts distinct real owners");
            var acquired = Facts(g).Claims(2, flowOwner).Records;
            Submit(c, g, new ClaimProbeCommand { Consume = true, Receipts = acquired.ToArray(), Participants = acquired.Concat(tool.Records).ToArray() });
            c.That(Facts(g).Claims(2, flowOwner).Records.Count == 0 && Facts(g).Stock(2).AmountOf(Resource(g)) == Fix.FromInt(2), "exact consumption retires only spent promise");
            c.That(Facts(g).Claims(1, toolOwner).Records.Single().Token.Equals(record.Token) && Facts(g).Claims(1, toolOwner).Version == tool.Version && Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(3), "nonmoving tool book/version/stock untouched");
            Submit(c, g, new ClaimProbeCommand { Release = true, Receipts = tool.Records.ToArray() });
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled actual books durable");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), Limits).Success, "participant claim command shared receipt codec replays");
            c.Equal(idleHash, idle.Hash(), "independent session unchanged");
        }
        private static void Activity(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); using var recorder = g.RecordReplay(Limits);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "timed DAG work requires no business"); Seed(c, g, 1, 3); Seed(c, g, 2, 4);
            int order = Post(c, g); var started = Nodes(g, order);
            c.That(started.Count == 2 && started.All(x => x.State == NodeState.Working && x.Progress.Equals(WorkProgress.Zero)), "authored parallel nodes start without instant earned work");
            var receipts = Tools(g, order); c.Equal(2, receipts.Length, "two actual retained tool promises"); var retained = Facts(g).Stock(1);
            for (int i = 0; i < 4; i++) Step(g);
            var blocked = Nodes(g, order); Step(g);
            c.That(blocked.All(x => x.Progress.WholeTicks > 0) && Nodes(g, order).Zip(blocked, (x, old) => x.Progress.Equals(old.Progress)).All(x => x), "later running shortage preserves both earned node positions");
            c.That(Facts(g).Stock(2).IsEmpty && Tools(g, order).Length == 2, "honest shortage consumes only supplied stock and retains tools");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "blocked actual frontier saved");
            Seed(c, g, 2, 4); Seed(c, twin, 2, 4); c.Equal(g.Hash(), twin.Hash(), "ordinary finite supply resumes same frontier");
            if (!Nodes(g, order).All(x => x.Progress.WholeTicks > 0)) { Step(g, twin); }
            var progress = Nodes(g, order); c.That(progress.All(x => x.Progress.WholeTicks > 0) && !Complete(g, order), "both real nodes earn time, no instant finish");
            c.That(Tools(g, order).Length == 2 && retained.AmountOf(Resource(g)) == Fix.FromInt(3), "reusable tool holds retained through exact input consumption");
            twin = f.Load(g.Save()); g.Clock.Pause(); twin.Clock.Pause(); ulong hash = g.Hash(); Step(g, twin); c.Equal(hash, g.Hash(), "sole clock pauses full productive frontier"); g.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 30 && !Complete(g, order); i++) Step(g, twin);
            if (!Complete(g, order)) throw new InvalidOperationException("productive continuation exceeded30/" + string.Join(";", Nodes(g, order).Select(x => x.Id + "/" + x.Blocker)));
            c.That(Facts(g).Stock(2).IsEmpty && Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(3), "exact eight inputs consumed, reusable tools preserved");
            foreach (var receipt in receipts) c.Equal(0, Facts(g).Claims(1, receipt.Owner).Records.Count, "original completion releases each tool promise");
            c.That(started.All(x => x.Progress.Equals(WorkProgress.Zero)), "old node snapshots remain detached");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed resource graph durable");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), Limits).Success, "authored parallel productive workflow replay");
        }
        private static void Violation(Check c, bool half, ClaimFaultMode mode)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); Seed(c, g, 1, 3); Seed(c, g, 2, 8); int order = Post(c, g);
            c.That(Nodes(g, order).All(x => x.Progress.Equals(WorkProgress.Zero)), "negative provider armed before first earned quantum");
            var published = g.Snapshot; Facts(g).Arm(2, 1, mode); g.Update();
            c.That(g.Fault != null && (g.Fault.Message.Contains("batch endpoint identity/version") || g.Fault.Message.Contains("unexpected batch stock")), "productive complete set detects separate tool stock mutation/" + mode + "/" + g.Fault);
            long stopped = g.Clock.StepIndex; c.That(g.State == GameState.Faulted && g.Update() == 0 && g.Clock.StepIndex == stopped, "violation stops sole clock");
            c.That(g.Snapshot.Tick == published.Tick, "prior publication retained, not rolled back stock");
            c.Throws<InvalidOperationException>(() => g.Save(), "no dishonest productive save"); c.Throws<InvalidOperationException>(() => g.Hash(), "no dishonest productive hash");
            c.Throws<InvalidOperationException>(() => Work(g).TryCopyNodes(order, new List<ActivityNodeView>()), "no successful progress observation after provider fault");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current protected claim identity"); foreach (bool half in new[] { false, true }) { Protocol(c, half); Activity(c, half); Violation(c, half, ClaimFaultMode.OtherEndpointAcquire); Violation(c, half, ClaimFaultMode.OtherEndpoint); } }
    }
}

