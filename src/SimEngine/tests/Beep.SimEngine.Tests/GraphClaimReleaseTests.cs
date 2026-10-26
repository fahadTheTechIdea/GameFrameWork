using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Reuses master-authored native activities. Player intent and detached facts only.
    internal static class GraphClaimReleaseTests
    {
        internal const string Case = "Graph claim release: whole-order endpoints protect cancellation completion and surviving receipt cleanup";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(6 * 1024 * 1024, 3000);
        private static GameFactory Factory(bool half) => ActivityClaimProtectionTests.Factory(half);
        private static EntityId Entity(SimGame g, string name) => TransformationContentContractTests.Entity(g, name);
        private static ResourceId Resource(SimGame g) { if (!g.Observer.TryFindResource("supply", out var id)) throw new InvalidOperationException("actual supply missing"); return id; }
        private static ResourceEndpointId Pool(long id) => new ResourceEndpointId("proof.pools", id);
        private static EndpointPoolDiagnostics Facts(SimGame g) => g.Seat(0).View.Feature<EndpointPoolDiagnostics>();
        private static Bill Quantity(SimGame g, int amount) => new Bill(new[] { new ResourceAmount(Resource(g), Fix.FromInt(amount)) });
        private static WorkView Work(SimGame g) => g.Seat(0).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame g, int order)
        { var list = new List<ActivityNodeView>(); if (!Work(g).TryCopyNodes(order, list)) throw new InvalidOperationException("actual graph absent"); return list; }
        private static ResourceReservation[] Held(SimGame g, int order) => Nodes(g, order).SelectMany(x => x.Resources).Where(x => !x.Spent).SelectMany(x => x.Receipts).ToArray();
        private static bool Complete(SimGame g, int order) => Work(g).TryReadActivityCompletion("activity:gp.parallel", Entity(g, "contract.factory"), out var fact) && fact.Order == order;
        private static void Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Step(SimGame g, SimGame twin = null)
        { g.Update(); twin?.Update(); if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash()) throw new InvalidOperationException("release Update/" + g.Fault + "/" + twin?.Fault); }
        private static void Seed(Check c, SimGame g, long pool, int amount)
            => Submit(c, g, new TransferProbeCommand { Move = true, Movements = new[] { new ResourceMovementIntent(new ReservationOwner("proof.gr.seed", g.Clock.StepIndex + 100),
                Company, ResourceEndpointId.ForEntity(Entity(g, "source")), Pool(pool), Quantity(g, amount), Array.Empty<ResourceReservation>(),
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } });
        private static ClaimProbeCommand Release(ResourceReservation[] receipts, ResourceReservation[] participants, ResourceEndpointId[] endpoints, ResourceClaimReleasePolicy policy)
            => new ClaimProbeCommand { Release = true, Receipts = receipts, Participants = participants, ReleaseEndpoints = endpoints, ReleasePolicy = policy };
        private static void Refuse(Check c, SimGame g, ClaimProbeCommand command, string label)
        { ulong hash = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(command), label); c.Equal(hash, g.Hash(), "release refusal pure"); }
        private static void Protocol(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong untouched = idle.Hash(); using var recorder = g.RecordReplay(Limits);
            Seed(c, g, 1, 3); Seed(c, g, 2, 10); var owner = new ReservationOwner("proof.gr.tool", 88);
            Submit(c, g, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(1), Quantity(g, 1)) } });
            var record = Facts(g).Claims(1, owner).Records.Single(); var records = new[] { record }; var refs = new[] { Pool(1), Pool(2) };
            Refuse(c, g, Release(records, records, null, ResourceClaimReleasePolicy.Exact), "explicit null endpoint references invalid");
            Refuse(c, g, Release(records, records, refs, (ResourceClaimReleasePolicy)99), "unknown cleanup policy invalid");
            Refuse(c, g, Release(new[] { record, record }, records, refs, ResourceClaimReleasePolicy.Surviving), "duplicate actual release token invalid");
            var wrong = new ResourceReservation(record.Token, record.Owner, record.Resource, Fix.FromInt(2), record.Direction);
            Refuse(c, g, Release(new[] { wrong }, records, refs, ResourceClaimReleasePolicy.Surviving), "surviving never clamps mismatched live amount");
            var foreign = new ResourceReservation(record.Token, new ReservationOwner("proof.gr.foreign", 89), record.Resource, record.Amount, record.Direction);
            Refuse(c, g, Release(new[] { foreign }, records, refs, ResourceClaimReleasePolicy.Surviving), "surviving never excuses a foreign live owner");
            Refuse(c, g, Release(records, records, new[] { Pool(1), Pool(99) }, ResourceClaimReleasePolicy.Exact), "exact missing protected reference refuses");
            var oldStock = Facts(g).Stock(2); Facts(g).Arm(1, 2, ClaimFaultMode.RefusingOut);
            int sequence = g.Seat(0).Submit(Release(records, records, new[] { Pool(1), Pool(2), Pool(99) }, ResourceClaimReleasePolicy.Surviving));
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending policy and endpoint-reference command saved"); Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "release does not require consumption readiness and skips absent reference explicitly");
            c.That(Facts(g).Claims(1, owner).Records.Count == 0 && Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(3) &&
                Facts(g).Stock(2).AmountOf(Resource(g)) == Fix.FromInt(10) && oldStock.AmountOf(Resource(g)) == Fix.FromInt(10), "release changes promises only and protects unheld peer stock");
            Refuse(c, g, Release(records, Array.Empty<ResourceReservation>(), refs, ResourceClaimReleasePolicy.Exact), "exact double release refuses");
            Submit(c, g, Release(records, records, refs, ResourceClaimReleasePolicy.Surviving));
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "explicit already-gone cleanup durable");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), Limits).Success, "release policy/endpoint/receipt codec replays actual books");
            c.Equal(untouched, idle.Hash(), "independent session isolated");
        }
        private static int Start(Check c, SimGame g)
        { Seed(c, g, 1, 3); Seed(c, g, 2, 10); return checked((int)PermanentRemovalTests.Submit(c, g,
            new PostOrderCommand { Plan = "gp.parallel", Target = Entity(g, "contract.factory"), Priority = 30 }).Arg0); }
        private static void Cancellation(Check c, bool half, bool partial, bool lost)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); using var recorder = g.RecordReplay(Limits); int order = Start(c, g);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "cleanup requires no business module");
            var original = Nodes(g, order); var all = Held(g, order); c.Equal(4, all.Length, "two tools and two startup claims are actual native bindings");
            if (partial) { Step(g); c.That(Nodes(g, order).All(x => x.Progress.WholeTicks > 0), "real earned work before cancellation"); }
            Fix input = Facts(g).Stock(2).AmountOf(Resource(g));
            if (lost) g.Seat(0).Submit(Release(new[] { all.First(x => x.Token.Endpoint == Pool(1)) }, Array.Empty<ResourceReservation>(), Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact));
            int sequence = g.Seat(0).Submit(new CancelOrderCommand { Order = order }); var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending lifecycle cleanup saved");
            Step(g, twin); long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted && !Work(g).TryCopyNodes(order, new List<ActivityNodeView>()), "native cancellation settles graph once");
            foreach (var receipt in all) c.Equal(0, Facts(g).Claims(receipt.Token.Endpoint.Identity, receipt.Owner).Records.Count, "surviving own claims released, already-retired receipt harmless");
            c.That(Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(3) && Facts(g).Stock(2).AmountOf(Resource(g)) == input, "no tool loss or refund of spent input");
            c.That(!Complete(g, order), "cancel never fabricates completion");
            c.That(original.All(x => x.Progress.Equals(WorkProgress.Zero)), "old graph facts detached");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal cleanup durable");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), Limits).Success, "saved cancellation and legitimate prior claim retirement replay");
        }
        private static void Completion(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); using var recorder = g.RecordReplay(Limits); int order = Start(c, g); var all = Held(g, order);
            Step(g); var twin = f.Load(g.Save()); g.Clock.Pause(); twin.Clock.Pause(); ulong hash = g.Hash(); Step(g, twin); c.Equal(hash, g.Hash(), "sole clock pauses partial completion"); g.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 30 && !Complete(g, order); i++) Step(g, twin); if (!Complete(g, order)) throw new InvalidOperationException("completion exceeded30");
            c.That(Facts(g).Stock(2).AmountOf(Resource(g)) == Fix.FromInt(2) && Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(3), "timed eight-input consumption, two surplus inputs and reusable tools conserved");
            foreach (var receipt in all) c.Equal(0, Facts(g).Claims(receipt.Token.Endpoint.Identity, receipt.Owner).Records.Count, "completion releases real remaining promises");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed protected release durable");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), Limits).Success, "timed completion and release replay");
        }
        private static void Violation(Check c, bool half, int stage)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); int order = Start(c, g); if (stage == 1) Step(g);
            Facts(g).Arm(1, 2, ClaimFaultMode.OtherEndpointRelease); if (stage != 2) g.Seat(0).Submit(new CancelOrderCommand { Order = order });
            GameSnapshot previous = g.Snapshot;
            for (int i = 0; i < 30 && g.Fault == null; i++) { previous = g.Snapshot; g.Update(); if (g.Fault == null && Complete(g, order)) break; }
            c.That(g.Fault != null && (g.Fault.Message.Contains("batch endpoint identity/version") || g.Fault.Message.Contains("unexpected batch stock")), "held/unheld original input store protected during lifecycle release/stage=" + stage + "/" + g.Fault);
            long stopped = g.Clock.StepIndex; c.That(g.State == GameState.Faulted && g.Update() == 0 && stopped == g.Clock.StepIndex, "violation stops sole clock");
            c.Equal(previous.Tick, g.Snapshot.Tick, "last verified publication retained, no compensating rollback");
            c.Throws<InvalidOperationException>(() => g.Save(), "no dishonest cleanup save"); c.Throws<InvalidOperationException>(() => g.Hash(), "no dishonest cleanup hash");
            c.Throws<InvalidOperationException>(() => Complete(g, order), "no successful terminal history after cleanup fault");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current release intent identity"); foreach (bool half in new[] { false, true }) { Protocol(c, half); Cancellation(c, half, false, false); Cancellation(c, half, true, false); Cancellation(c, half, false, true); Completion(c, half); foreach (int stage in new[] { 0, 1, 2 }) Violation(c, half, stage); } }
    }
}

