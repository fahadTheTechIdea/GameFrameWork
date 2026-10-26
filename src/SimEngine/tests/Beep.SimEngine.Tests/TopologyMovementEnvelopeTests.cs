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
    // Trusted extension-protocol probes plus ordinary physical players; no host stock/timing/rule writers.
    internal static class TopologyMovementEnvelopeTests
    {
        internal const string Case = "Topology movement envelope: aggregate mixed-party claims exact release and physical owned-world graphs";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly ReplayLimits Limits = new ReplayLimits(6 * 1024 * 1024, 5000);
        private static GameFactory Factory(bool half) => RecipeDisclosureTests.Factory(false, "result", half,
            knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", JsonValue.NewArray()),
            extraModules: new GameModule[] { new EndpointProofModule() },
            extraDeclarations: "[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"topology-movement-envelope.json\"]}]",
            extraDocuments: new Dictionary<string,string> { ["topology-movement-envelope.json"] = FrameworkProofContent.Read("topology-movement-envelope.json") });
        private static ResourceEndpointId Pool(long id) => new ResourceEndpointId("proof.pools", id);
        private static ResourceId Resource(SimGame g) { if (!g.Observer.TryFindResource("supply", out var id)) throw new InvalidOperationException("actual supply absent"); return id; }
        private static Bill Quantity(SimGame g, int amount) => new Bill(new[] { new ResourceAmount(Resource(g), Fix.FromInt(amount)) });
        private static EndpointPoolDiagnostics Facts(SimGame g, int seat = -1) => seat < 0 ? g.Observer.Feature<EndpointPoolDiagnostics>() : g.Seat(seat).View.Feature<EndpointPoolDiagnostics>();
        private static void Step(SimGame g, SimGame twin = null)
        { g.Update(); twin?.Update(); if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash()) throw new InvalidOperationException("envelope Update/" + g.Fault + "/" + twin?.Fault); }
        private static void Submit(Check c, SimGame g, Command command, int seat = 0)
        {
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(seat).Preview(command), "engine protocol preview"); c.Equal(hash, g.Hash(), "preview pure");
            int sequence = g.Seat(seat).Submit(command); Step(g); long cursor = 0; var results = new List<CommandResult>(); g.Seat(seat).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "engine protocol accepted");
        }
        private static ResourceMovementIntent Move(SimGame g, ReservationOwner owner, PlayerId company, long destination, int amount,
            IReadOnlyList<ResourceReservation> receipts = null, bool release = false)
            => new ResourceMovementIntent(owner, company, Pool(1), Pool(destination), Quantity(g, amount), receipts ?? Array.Empty<ResourceReservation>(),
                Array.Empty<ResourceReservation>(), receipts ?? Array.Empty<ResourceReservation>(), release ? ResourceMovementParticipantDisposition.Release : ResourceMovementParticipantDisposition.Retain);
        private static TransferProbeCommand Probe(params ResourceMovementIntent[] moves) => new TransferProbeCommand { Move = true, SystemEnvelope = true, Movements = moves };
        private static void Seed(Check c, SimGame g)
            => Submit(c, g, new TransferProbeCommand { Move = true, Movements = new[] { new ResourceMovementIntent(new ReservationOwner("proof.tb.seed", 80), new PlayerId(0),
                ResourceEndpointId.ForEntity(TransformationContentContractTests.Entity(g, "source")), Pool(1), Quantity(g, 10), Array.Empty<ResourceReservation>(),
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } });
        private static void Refuse(Check c, SimGame g, TransferProbeCommand command, string label)
        { ulong hash = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(command), label); c.Equal(hash, g.Hash(), "whole envelope refusal changes no state or book"); }
        private static void Mixed(Check c, bool half, bool held)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            using var recording = g.RecordReplay(Limits); Seed(c, g);
            var a = new ReservationOwner("proof.tb.a", 81); var b = new ReservationOwner("proof.tb.b", 82);
            Refuse(c, g, Probe(Move(g, a, new PlayerId(0), 2, 6), Move(g, b, new PlayerId(1), 3, 6)), "parties jointly exceed shared world stock before any claim");
            Refuse(c, g, Probe(Move(g, a, new PlayerId(0), 5, 3), Move(g, b, new PlayerId(1), 5, 3)), "parties jointly exceed shared inlet capacity while source stock fits");
            Refuse(c, g, Probe(Move(g, a, new PlayerId(0), 2, 1), Move(g, a, new PlayerId(1), 3, 1)), "one owner cannot ambiguously attribute to two parties");
            Refuse(c, g, Probe(Move(g, a, PlayerId.None, 2, 1)), "world actor cannot take company destination");
            ulong hash = g.Hash(); c.Equal(Codes.NotOwned, g.Seat(0).Preview(new TransferProbeCommand { Move = true, Movements = new[] { Move(g, b, new PlayerId(1), 3, 1) } }), "ordinary player wrapper remains seat-bound"); c.Equal(hash, g.Hash(), "private normal refusal pure");
            IReadOnlyList<ResourceReservation> ra = Array.Empty<ResourceReservation>(), rb = Array.Empty<ResourceReservation>();
            if (held)
            {
                Submit(c, g, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, a, Pool(1), Quantity(g, 3)) } });
                Submit(c, g, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, b, Pool(1), Quantity(g, 3)) } }, 1);
                ra = Facts(g, 0).EndpointClaims(Pool(1), a).Records; rb = Facts(g, 1).EndpointClaims(Pool(1), b).Records;
            }
            int amount = held ? 2 : 5;
            var command = Probe(Move(g, a, new PlayerId(0), 2, amount, ra, held), Move(g, b, new PlayerId(1), 3, amount, rb, held));
            int sequence = g.Seat(0).Submit(command); var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending multi-party envelope codec durable"); Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results); c.That(results.Single(x => x.Sequence == sequence).Accepted, "complete party envelope committed");
            c.That(Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(10 - 2 * amount) && Facts(g).Stock(2).AmountOf(Resource(g)) == Fix.FromInt(amount) &&
                Facts(g).Stock(3).AmountOf(Resource(g)) == Fix.FromInt(amount), "exact shared stock debit and both private credits");
            c.That(Facts(g, 0).EndpointClaims(Pool(1), a).Records.Count == 0 && Facts(g, 1).EndpointClaims(Pool(1), b).Records.Count == 0, "fresh spent tokens or both actual retained remainders retire once");
            if (held) c.That(ra.Single().Amount == Fix.FromInt(3) && rb.Single().Amount == Fix.FromInt(3), "old held facts detached");
            c.That(g.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Entries.Any(x => x.Item.Destination.Endpoint == Pool(3)), "each party receives actual attributed flow facts");
            c.That(g.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Status == ResourceFlowReadStatus.NotOwned, "multi-party commit does not disclose foreign history");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled complete envelope durable"); c.That(new ReplayVerifier().Verify(f, recording.Finish(), Limits).Success, "multi-party actual authority command replay");
            c.Equal(isolated, idle.Hash(), "isolated session unchanged");
        }
        private static void World(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); using var recording = g.RecordReplay(Limits); Seed(c, g);
            Submit(c, g, Probe(Move(g, new ReservationOwner("proof.tb.world", 83), PlayerId.None, 4, 4)));
            c.That(Facts(g).Stock(1).AmountOf(Resource(g)) == Fix.FromInt(6) && Facts(g).Stock(4).AmountOf(Resource(g)) == Fix.FromInt(4), "existing None actor moves own private world stock without a special kind");
            var facts = g.Observer.Feature<ResourceFlowView>().Read(PlayerId.None, 0).Entries.Select(x => x.Item).ToArray();
            c.That(facts.Any(x => x.Company == PlayerId.None && x.Kind == ResourceFlowKind.StockTransfer && x.Destination.Endpoint == Pool(4)), "ordinary stock fact keeps actual world actor");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "world envelope durable"); c.That(new ReplayVerifier().Verify(f, recording.Finish(), Limits).Success, "world envelope replay");
        }
        private static void Fault(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); Seed(c, g); var previous = g.Snapshot;
            Facts(g).Arm(3, 2, ClaimFaultMode.OtherEndpointReceive);
            g.Seat(0).Submit(Probe(Move(g, new ReservationOwner("proof.tb.fault-a", 85), new PlayerId(0), 2, 5), Move(g, new ReservationOwner("proof.tb.fault-b", 86), new PlayerId(1), 3, 5))); g.Update();
            c.That(g.Fault != null && g.Fault.Message.Contains("batch endpoint identity/version"), "late second-party receive cannot alter first-party store undetected/" + g.Fault);
            long stopped = g.Clock.StepIndex; c.That(g.State == GameState.Faulted && g.Update() == 0 && stopped == g.Clock.StepIndex, "late envelope fault stops clock");
            c.Equal(previous.Tick, g.Snapshot.Tick, "retains last publication, not rollback");
            c.Throws<InvalidOperationException>(() => g.Save(), "no partial envelope save"); c.Throws<InvalidOperationException>(() => g.Hash(), "no partial envelope hash");
            c.Throws<InvalidOperationException>(() => g.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0), "no partial successful history observation");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current whole-topology identity");
            foreach (bool half in new[] { false, true }) { Mixed(c, half, false); Mixed(c, half, true); World(c, half); Fault(c, half); }
            foreach (bool world in new[] { false, true }) foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true })
            {
                var f = world ? WorldNetworkStockTests.Factory(point, half) : TopologyErasePostconditionTests.Factory(point, half);
                var g = TopologyErasePostconditionTests.Healthy(c, point, half, f);
                c.That(TopologyErasePostconditionTests.Pools(g, point).All(x => x.Company == (world ? PlayerId.None : new PlayerId(0))), "whole topology preserves actual party");
            }
        }
    }
}

