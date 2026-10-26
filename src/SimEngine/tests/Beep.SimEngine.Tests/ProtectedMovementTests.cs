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
    // Player requests and detached facts only. Transfer probes exercise contracts, not hauling shortcuts.
    internal static class ProtectedMovementTests
    {
        internal const string Case = "Protected movement: complete participants survive acquisition and shared-tool startup or fault peer mutation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Process = new Code("process");
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 1000);
        private static GameFactory Factory() => TransformationInputPortTests.Factory();
        private static EntityId Entity(SimGame g, string name) => TransformationInputPortTests.Find(g, name);
        private static ResourceId Resource(SimGame g, string name) => TransformationInputPortTests.Resource(g, name);
        private static ResourceEndpointId Pool(long id) => new ResourceEndpointId("proof.pools", id);
        private static EndpointPoolDiagnostics Facts(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<EndpointPoolDiagnostics>();
        private static Bill Bill(SimGame g, string name, int amount) => new Bill(new[] { new ResourceAmount(Resource(g, name), Fix.FromInt(amount)) });
        private static ResourceMovementIntent Move(SimGame g, ReservationOwner owner, ResourceEndpointId source, ResourceEndpointId destination,
            string name, int amount, IReadOnlyList<ResourceReservation> outgoing, IReadOnlyList<ResourceReservation> incoming,
            IReadOnlyList<ResourceReservation> participants)
            => new ResourceMovementIntent(owner, Company, source, destination, Bill(g, name, amount), outgoing, incoming, participants, ResourceMovementParticipantDisposition.Retain);
        private static TransferProbeCommand Transfer(params ResourceMovementIntent[] movements) => new TransferProbeCommand { Move = true, Movements = movements };
        private static void Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Seed(Check c, SimGame g, long pool, string name, int amount)
            => Submit(c, g, Transfer(Move(g, new ReservationOwner("proof.protected-seed", g.Clock.StepIndex + 100),
                ResourceEndpointId.ForEntity(Entity(g, "ports.source")), Pool(pool), name, amount,
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>())));
        private static void Step(SimGame g, SimGame twin = null)
        { g.Update(); twin?.Update(); if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash()) throw new InvalidOperationException("protected Update/" + g.Fault + "/" + twin?.Fault); }
        private static TransformationSnapshot State(SimGame g) => TransformationInputPortTests.State(g);
        private static void Refuse(Check c, SimGame g, TransferProbeCommand command, string label)
        { ulong hash = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(command), label); c.Equal(hash, g.Hash(), "complete refusal pure"); }

        private static void Movement(Check c, bool held)
        {
            var factory = Factory(); var g = TransformationInputPortTests.New(factory); var idle = TransformationInputPortTests.New(factory); ulong idleHash = idle.Hash();
            using var recorder = g.RecordReplay(Limits);
            Seed(c, g, 4, "ports.ore", 4); Seed(c, g, 7, "ports.jig", 2);
            var owner = new ReservationOwner("proof.protected", 81);
            var claims = new List<ResourceClaimIntent> { new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(7), Bill(g, "ports.jig", 1)) };
            var sink = ResourceEndpointId.ForEntity(Entity(g, "ports.sink-entity"));
            if (held) { claims.Add(new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(4), Bill(g, "ports.ore", 3))); claims.Add(new ResourceClaimIntent(ReservationDirection.Inbound, owner, sink, Bill(g, "ports.ore", 3))); }
            Submit(c, g, new ClaimProbeCommand { Intents = claims.ToArray() });
            var peer = Facts(g).Claims(7, owner); var retainedStock = Facts(g).Stock(7);
            var outgoing = held ? Facts(g).Claims(4, owner).Records : Array.Empty<ResourceReservation>();
            IReadOnlyList<ResourceReservation> incoming = Array.Empty<ResourceReservation>();
            if (held) { g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Entity(g, "ports.sink-entity"), Code.None, out incoming); }
            var participants = peer.Records.Concat(outgoing).Concat(incoming).ToArray();
            TransferProbeCommand Request(IReadOnlyList<ResourceReservation> records) => Transfer(Move(g, owner, Pool(4), sink, "ports.ore", 2, outgoing, incoming, records));
            Refuse(c, g, Request(null), "null participants invalid");
            Refuse(c, g, Request(new[] { peer.Records.Single(), peer.Records.Single() }), "duplicate participant invalid");
            var p = peer.Records.Single();
            Refuse(c, g, Request(new[] { new ResourceReservation(p.Token, new ReservationOwner("proof.foreign", 82), p.Resource, p.Amount, p.Direction) }), "foreign owner invalid");
            Refuse(c, g, Request(new[] { new ResourceReservation(p.Token, p.Owner, p.Resource, Fix.FromInt(2), p.Direction) }), "stale actual participant invalid");
            c.Throws<ArgumentException>(() => Facts(g, 1).Claims(7, owner), "private peer not disclosed to foreign company");
            c.Throws<NotSupportedException>(() => ((IList<ResourceReservation>)peer.Records).Clear(), "participant facts immutable");
            int sequence = g.Seat(0).Submit(Request(participants)); var twin = factory.Load(g.Save());
            c.Equal(g.Hash(), twin.Hash(), "pending complete participant command saved with shared codec");
            Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "complete protected movement committed");
            c.That(Facts(g).Claims(7, owner).Records.Single().Token.Equals(p.Token) && Facts(g).Claims(7, owner).Version == peer.Version, "nonmoving tool token and version untouched");
            c.That(Facts(g).Stock(7).AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2) && retainedStock.AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2), "tool stock remains two, detached facts retained");
            c.Equal(Fix.FromInt(2), Facts(g).Stock(4).AmountOf(Resource(g, "ports.ore")), "source exactly debited");
            if (held) { c.Equal(Fix.One, Facts(g).Claims(4, owner).Records.Single().Amount, "shared participant/source receipt reduces only by spent vector"); c.Equal(Fix.FromInt(3), outgoing.Single().Amount, "old source receipt detached"); }
            else c.Equal(0, Facts(g).Claims(4, owner).Records.Count, "fresh acquisition consumed once");
            var remaining = new List<ResourceReservation>(Facts(g).Claims(7, owner).Records); remaining.AddRange(Facts(g).Claims(4, owner).Records);
            if (held) { g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Entity(g, "ports.sink-entity"), Code.None, out var rest); remaining.AddRange(rest); }
            Submit(c, g, new ClaimProbeCommand { Release = true, Receipts = remaining.ToArray() });
            c.Equal(0, Facts(g).Claims(7, owner).Records.Count, "unspent participant remains releasable through sole book");
            c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "settled movement save durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "three-set participant command replays actual native books");
            c.Equal(idleHash, idle.Hash(), "independent session unchanged");
        }

        private static void Prime(Check c, SimGame g)
        { Seed(c, g, 4, "ports.ore", 4); Seed(c, g, 5, "ports.water", 1); Seed(c, g, 7, "ports.jig", 2); }
        private static EnqueueTransformationCommand Enqueue(SimGame g) => new EnqueueTransformationCommand { Transformer = Entity(g, "ports.factory"), Capability = Process, Transformation = "ports.tool-batch" };
        private static void Physical(Check c)
        {
            var factory = Factory(); var g = TransformationInputPortTests.New(factory);
            using var recorder = g.RecordReplay(Limits); Prime(c, g); Submit(c, g, Enqueue(g));
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "native protected startup requires no business");
            var startup = State(g);
            c.That(startup.Phase == TransformationPhase.Working && startup.Progress.WholeTicks == 1 && startup.QueueCount == 1, "native batch starts, not instant completion");
            c.That(startup.Wip.Stock.AmountOf(Resource(g, "ports.ore")) == Fix.FromInt(2) && startup.Wip.Stock.AmountOf(Resource(g, "ports.water")) == Fix.One, "complete mixed input physically enters original WIP");
            c.That(startup.Tools.Receipts.Count == 2 && startup.Tools.Receipts.Any(x => x.Token.Endpoint == Pool(7) && x.Amount == Fix.One) &&
                startup.Tools.Receipts.Any(x => x.Token.Endpoint == Pool(4) && x.Amount == Fix.FromInt(2)), "separate and shared tool receipts persist in original execution");
            c.That(Facts(g).Stock(4).AmountOf(Resource(g, "ports.ore")) == Fix.FromInt(2) && Facts(g).Stock(7).AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2), "shared remainder and nonmoving tool stock exact");
            c.Equal(Fix.Zero, Facts(g).Stock(1).AmountOf(Resource(g, "ports.alloy")), "no output before authored duration");
            var twin = factory.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "whole startup graph saved");
            g.Clock.Pause(); twin.Clock.Pause(); ulong hash = g.Hash(); Step(g, twin); c.Equal(hash, g.Hash(), "one clock pauses all protected work");
            g.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 12 && State(g).QueueCount != 0; i++) Step(g, twin);
            if (State(g).QueueCount != 0) throw new InvalidOperationException("native completion exceeded twelve updates/" + State(g).Blocker);
            c.That(State(g).Wip.Stock.IsEmpty && State(g).Tools.Receipts.Count == 0, "original WIP and tools settled once");
            c.That(Facts(g).Stock(1).AmountOf(Resource(g, "ports.alloy")) == Fix.FromInt(2) && Facts(g).Stock(2).AmountOf(Resource(g, "ports.water")) == Fix.One, "exact mixed outputs");
            c.That(Facts(g).Stock(4).AmountOf(Resource(g, "ports.ore")) == Fix.FromInt(2) && Facts(g).Stock(7).AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2), "reusable tools not consumed");
            c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "completed batch durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "native timed protected batch replays");
        }

        private static void Fault(Check c, ClaimFaultMode mode, bool native)
        {
            var factory = Factory(); var g = TransformationInputPortTests.New(factory); TransferProbeCommand movement = null;
            if (native) Prime(c, g);
            else
            {
                Seed(c, g, 4, "ports.ore", 3); Seed(c, g, 7, "ports.jig", 2);
                var owner = new ReservationOwner("proof.protected-fault", 89);
                var claims = new List<ResourceClaimIntent> { new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(7), Bill(g, "ports.jig", 1)) };
                if (mode != ClaimFaultMode.OtherEndpointAcquire) claims.Add(new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(4), Bill(g, "ports.ore", 2)));
                Submit(c, g, new ClaimProbeCommand { Intents = claims.ToArray() });
                movement = Transfer(Move(g, owner, Pool(4), ResourceEndpointId.ForEntity(Entity(g, "ports.sink-entity")), "ports.ore", 1,
                    Facts(g).Claims(4, owner).Records, Array.Empty<ResourceReservation>(), Facts(g).Claims(7, owner).Records));
            }
            var published = g.Snapshot; var history = g.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
            Facts(g).Arm(4, 7, mode); // Engine-side negative extension instrumentation, never production/player gameplay.
            g.Seat(0).Submit(native ? (Command)Enqueue(g) : movement); g.Update();
            c.That(g.Fault != null && (g.Fault.Message.Contains("batch endpoint identity/version") || g.Fault.Message.Contains("unexpected batch stock")), "complete set detects peer mutation/" + mode + "/native=" + native + "/" + g.Fault);
            long stopped = g.Clock.StepIndex; c.That(g.State == GameState.Faulted && g.Update() == 0 && g.Clock.StepIndex == stopped, "violating provider latches stopped engine without inventing player pause");
            c.That(g.Snapshot.Tick == published.Tick && g.Snapshot.Entities.Count == published.Entities.Count && history.Entries.Count > 0, "retains prior publication, not invented rollback");
            c.Throws<InvalidOperationException>(() => g.Save(), "fault has no resumable checkpoint");
            c.Throws<InvalidOperationException>(() => g.Hash(), "fault has no valid hash");
            c.Throws<InvalidOperationException>(() => g.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0), "no successful history observation after provider fault");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current participant protocol identity"); Movement(c, false); Movement(c, true); Physical(c); Fault(c, ClaimFaultMode.MutatingOtherEndpointCheck, false); Fault(c, ClaimFaultMode.OtherEndpointAcquire, false); Fault(c, ClaimFaultMode.OtherEndpoint, false); Fault(c, ClaimFaultMode.OtherEndpoint, true); }
    }
}

