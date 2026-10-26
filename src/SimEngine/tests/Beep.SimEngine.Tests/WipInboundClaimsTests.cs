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
    // Public player requests and detached facts. Atomic admission/stock/timing are engine-owned.
    internal static class WipInboundClaimsTests
    {
        internal const string Case = "WIP inbound claims: held destination movement preserves remainders and physical startup reserves complete capacity";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Capability = new Code("contract.process");
        private static GameFactory Factory(bool half, bool small = false)
            => RecipeDisclosureTests.Factory(false, "result", half,
                setupEdit: root => { if (small) root.Get("independent_capacity_policies")[3].Get("limits").Set("unit.capacity.count", JsonValue.Number("2")); },
                knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", JsonValue.NewArray()),
                extraModules: new GameModule[] { new EndpointProofModule() },
                extraDeclarations: "[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"wip-inbound-claims.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["wip-inbound-claims.json"] = FrameworkProofContent.Read("wip-inbound-claims.json") });
        private static EntityId Entity(SimGame g, string name) => TransformationContentContractTests.Entity(g, name);
        private static ResourceId Resource(SimGame g)
        { if (!g.Observer.TryFindResource("supply", out var value)) throw new InvalidOperationException("actual supply absent"); return value; }
        private static Bill Quantity(SimGame g, int amount) => new Bill(new[] { new ResourceAmount(Resource(g), Fix.FromInt(amount)) });
        private static ResourceEndpointId Endpoint(SimGame g, string name) => ResourceEndpointId.ForEntity(Entity(g, name));
        private static IReadOnlyList<ResourceReservation> Records(SimGame g, string name)
        { if (!g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Entity(g, name), Code.None, out var records)) throw new InvalidOperationException("actual records absent"); return records; }
        private static ResourceReservation Receipt(SimGame g, string name, ReservationDirection direction) => Records(g, name).Single(x => x.Direction == direction);
        private static Fix Stock(SimGame g, string name)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(Entity(g, name), Resource(g), out var value); if (!code.IsNone) throw new InvalidOperationException("actual stock/" + code); return value.Stock; }
        private static ResourceMovementIntent Move(SimGame g, ReservationOwner owner, string source, string destination, int amount,
            IReadOnlyList<ResourceReservation> outgoing, IReadOnlyList<ResourceReservation> incoming)
            => new ResourceMovementIntent(owner, new PlayerId(0), Endpoint(g, source), Endpoint(g, destination), Quantity(g, amount), outgoing, incoming, System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        private static TransferProbeCommand Transfer(params ResourceMovementIntent[] moves) => new TransferProbeCommand { Move = true, Movements = moves };
        private static void Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Refuse(Check c, SimGame g, TransferProbeCommand command, string reason)
        { ulong hash = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(command), reason); c.Equal(hash, g.Hash(), "refusal leaves all books/stock/sequences pure"); }
        private static void Held(Check c, bool half, bool convergence)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(6 * 1024 * 1024, 4000); using var recording = g.RecordReplay(limits);
            var empty = Array.Empty<ResourceReservation>(); var owner = new ReservationOwner("proof.held-movement", 81);
            if (convergence) Submit(c, g, Transfer(Move(g, new ReservationOwner("proof.seed-movement", 80), "source", "machine", 2, empty, empty)));
            var claims = new List<ResourceClaimIntent> {
                new ResourceClaimIntent(ReservationDirection.Outbound, owner, Endpoint(g, "source"), Quantity(g, convergence ? 2 : 3)),
                new ResourceClaimIntent(ReservationDirection.Inbound, owner, Endpoint(g, "contract.factory"), Quantity(g, 3)) };
            if (convergence) claims.Add(new ResourceClaimIntent(ReservationDirection.Outbound, owner, Endpoint(g, "machine"), Quantity(g, 2)));
            Submit(c, g, new ClaimProbeCommand { Intents = claims.ToArray() });
            var source = Receipt(g, "source", ReservationDirection.Outbound); var destination = Receipt(g, "contract.factory", ReservationDirection.Inbound);
            var retained = Records(g, "contract.factory");
            c.That(!g.Seat(1).View.Feature<InventoryView>().TryGetReservations(Entity(g, "contract.factory"), Code.None, out _), "foreign player cannot read held capacity");
            Refuse(c, g, Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, null)), "explicit null destination set rejected");
            var wrong = new ResourceReservation(destination.Token, owner, destination.Resource, destination.Amount, ReservationDirection.Outbound);
            Refuse(c, g, Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, new[] { wrong })), "wrong destination direction rejected");
            var foreign = new ResourceReservation(destination.Token, new ReservationOwner("proof.foreign", 82), destination.Resource, destination.Amount, destination.Direction);
            Refuse(c, g, Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, new[] { foreign })), "foreign destination owner rejected");
            if (convergence)
            {
                var other = Receipt(g, "machine", ReservationDirection.Outbound);
                Refuse(c, g, Transfer(Move(g, owner, "source", "contract.factory", 2, new[] { source }, new[] { destination }),
                    Move(g, owner, "machine", "contract.factory", 2, new[] { other }, new[] { destination })), "aggregate destination overdraw rejected before either leg");
                Submit(c, g, Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, new[] { destination }),
                    Move(g, owner, "machine", "contract.factory", 1, new[] { other }, new[] { destination })));
            }
            else Submit(c, g, Transfer(Move(g, owner, "source", "contract.factory", 2, new[] { source }, new[] { destination })));
            c.Equal(Fix.FromInt(2), Stock(g, "contract.factory"), "held inlet receives exact partial vector without double reservation");
            c.That(Receipt(g, "contract.factory", ReservationDirection.Inbound).Amount == Fix.One && retained.Single().Amount == Fix.FromInt(3), "real inbound remainder one; old receipt snapshot detached");
            source = Receipt(g, "source", ReservationDirection.Outbound);
            Refuse(c, g, Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, new[] { destination })), "stale original destination amount rejected");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "both held directions survive save");
            var remaining = Receipt(g, "contract.factory", ReservationDirection.Inbound);
            var command = Transfer(Move(g, owner, "source", "contract.factory", 1, new[] { source }, new[] { remaining }));
            Submit(c, g, command); Submit(c, twin, Transfer(Move(twin, owner, "source", "contract.factory", 1,
                new[] { Receipt(twin, "source", ReservationDirection.Outbound) }, new[] { Receipt(twin, "contract.factory", ReservationDirection.Inbound) })));
            c.That(g.Hash() == twin.Hash() && Stock(g, "contract.factory") == Fix.FromInt(3) && Records(g, "contract.factory").Count == 0, "saved held remainder commits once, inbound token retired");
            if (convergence) { Submit(c, g, new ClaimProbeCommand { Release = true, Receipts = new[] { Receipt(g, "machine", ReservationDirection.Outbound) } }); }
            c.That(Records(g, "source").Count == 0, "original source receipt retired exactly");
            c.Equal(idleHash, idle.Hash(), "claim sessions isolated"); c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled held movement durable");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "new bidirectional command codec replays actual receipts and retirement");
        }
        private static TransformationSnapshot Process(SimGame g)
        { if (!g.Observer.Feature<TransformationManagementView>().TryGetTransformation(Entity(g, "contract.factory"), Capability, out var value)) throw new InvalidOperationException("actual process absent"); return value; }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        { for (int i = 0; i < 512 && !done(); i++) TransformationContentContractTests.Step(g, twin); if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/phase=" + Process(g).Phase + "/blocker=" + Process(g).Blocker); }
        private static void Physical(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f);
            var limits = new ReplayLimits(6 * 1024 * 1024, 5000); using var recording = g.RecordReplay(limits);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "complete startup requires no business module");
            Submit(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            Submit(c, g, new EnqueueTransformationCommand { Transformer = Entity(g, "contract.factory"), Capability = Capability, Transformation = "contract.make" });
            {
                Reach(g, null, () => Process(g).Phase == TransformationPhase.Working && Process(g).Progress.WholeTicks > 0, "physical full startup and positive work");
                var retained = Process(g); c.That(retained.Wip.Stock.AmountOf(Resource(g)) == Fix.FromInt(3) && retained.Tools.Receipts.Single().Amount == Fix.One, "input moved into real WIP and tool-only remainder protected");
                var twin = f.Load(g.Save()); g.Clock.Pause(); twin.Clock.Pause(); ulong hash = g.Hash(); TransformationContentContractTests.Step(g, twin); c.Equal(hash, g.Hash(), "sole clock pauses complete startup"); g.Clock.Resume(); twin.Clock.Resume();
                Reach(g, twin, () => Process(g).QueueCount == 0, "saved physical production completion");
                c.That(g.Snapshot.Entities.Count(x => x.Def == "contract.result") == 1 && Stock(g, "source") == Fix.FromInt(16) && Stock(g, "contract.factory") == Fix.One && Stock(g, "machine") == Fix.Zero, "exact physical result/material/tool/cargo accounting");
                c.That(Process(g).Wip.Stock.IsEmpty && Process(g).Tools.Receipts.Count == 0 && Records(g, "contract.factory").Count == 0, "input/inbound/tool promises all settled once");
            }
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "startup/result save durable"); c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "ordinary physical startup/result replay");
        }
        private static void InvalidCapacity(Check c, bool half)
        {
            Exception error = null;
            try { ConstructionPlanTests.NewPoints(Factory(half, true)); } catch (Exception caught) { error = caught; }
            c.That(error is ContentException && error.Message.Contains("WIP must hold complete supported native inputs"), "undersized authored WIP rejected before a game or partial startup exists");
            if (error == null) throw new InvalidOperationException("missing native WIP preflight refusal");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current inbound intent identity"); foreach (bool half in new[] { false, true }) { foreach (bool convergence in new[] { false, true }) Held(c, half, convergence); Physical(c, half); InvalidCapacity(c, half); } }
    }
}
