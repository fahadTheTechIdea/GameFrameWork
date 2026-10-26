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
    // Authored content, player requests and immutable facts. Return/release/timing remain engine-owned.
    internal static class ReturnParticipantTests
    {
        internal const string Case = "Return participants: saved cancellation settles original inputs and reusable tools before movement success";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("process");
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 2000);
        private static GameFactory Factory(bool half) => OutputPortGame.Factory(null, "input-ports.json", "input-ports-master.json",
            editShared: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("denominator", JsonValue.Number("2")); });
        private static SimGame New(GameFactory f) => TransformationInputPortTests.New(f);
        private static EntityId Entity(SimGame g, string name) => TransformationInputPortTests.Find(g, name);
        private static ResourceId Resource(SimGame g, string name) => TransformationInputPortTests.Resource(g, name);
        private static ResourceEndpointId Pool(long id) => new ResourceEndpointId("proof.pools", id);
        private static ResourceEndpointId Sink(SimGame g) => ResourceEndpointId.ForEntity(Entity(g, "ports.sink-entity"));
        private static EndpointPoolDiagnostics Facts(SimGame g) => g.Seat(0).View.Feature<EndpointPoolDiagnostics>();
        private static Bill Bill(SimGame g, string name, int amount) => new Bill(new[] { new ResourceAmount(Resource(g, name), Fix.FromInt(amount)) });
        private static ResourceMovementIntent Move(SimGame g, ReservationOwner owner, ResourceEndpointId source, ResourceEndpointId destination,
            string name, int amount, IReadOnlyList<ResourceReservation> sourceClaims, IReadOnlyList<ResourceReservation> participants,
            ResourceMovementParticipantDisposition disposition) => new ResourceMovementIntent(owner, Company, source, destination,
                Bill(g, name, amount), sourceClaims, Array.Empty<ResourceReservation>(), participants, disposition);
        private static TransferProbeCommand Transfer(params ResourceMovementIntent[] moves) => new TransferProbeCommand { Move = true, Movements = moves };
        private static void Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Step(SimGame g, SimGame twin = null)
        { g.Update(); twin?.Update(); if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash()) throw new InvalidOperationException("return Update/" + g.Fault + "/" + twin?.Fault); }
        private static void Seed(Check c, SimGame g, long pool, string name, int amount, SimGame twin = null)
        {
            Command Request(SimGame game) => Transfer(Move(game, new ReservationOwner("proof.return-seed", game.Clock.StepIndex + 100),
                ResourceEndpointId.ForEntity(Entity(game, "ports.source")), Pool(pool), name, amount,
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
            Submit(c, g, Request(g)); if (twin != null) Submit(c, twin, Request(twin));
        }
        private static TransformationSnapshot State(SimGame g) => TransformationInputPortTests.State(g);
        private static void Configure(Check c, SimGame g, bool enabled, SimGame twin = null)
        { Command Request(SimGame game) => new ConfigureOperationCommand { Entity = Entity(game, "ports.factory"), Operation = Process, Enabled = enabled, Target = Fix.One }; Submit(c, g, Request(g)); if (twin != null) Submit(c, twin, Request(twin)); }
        private static void Select(Check c, SimGame g, string port, long pool, SimGame twin = null)
        { Command Request(SimGame game) => new SelectResourcePortCommand { Entity = Entity(game, "ports.factory"), Port = new Code(port), Endpoint = Pool(pool) }; Submit(c, g, Request(g)); if (twin != null) Submit(c, twin, Request(twin)); }
        private static CancelTransformationCommand Cancel(SimGame g) => new CancelTransformationCommand { Transformer = Entity(g, "ports.factory"), Capability = Process, Index = 0 };
        private static void Start(Check c, SimGame g)
        {
            Seed(c, g, 4, "ports.ore", 4); Seed(c, g, 5, "ports.water", 1); Seed(c, g, 7, "ports.jig", 2);
            Submit(c, g, new EnqueueTransformationCommand { Transformer = Entity(g, "ports.factory"), Capability = Process, Transformation = "ports.tool-batch" });
            Configure(c, g, false);
            c.That(State(g).Wip.Execution > 0 && State(g).Tools.Receipts.Count == 2 && State(g).Progress.WholeTicks > 0, "native earned mixed startup retains real tool receipts");
        }
        private static void Refuse(Check c, SimGame g, Command request, string label)
        { ulong before = g.Hash(); c.Equal(Codes.Blocked, g.Seat(0).Preview(request), label); c.Equal(before, g.Hash(), "invalid disposition is pure"); }

        private static void Release(Check c, bool half)
        {
            var factory = Factory(half); var g = New(factory); var idle = New(factory); ulong idleHash = idle.Hash(); using var recorder = g.RecordReplay(Limits);
            Seed(c, g, 4, "ports.ore", 4); Seed(c, g, 7, "ports.jig", 2);
            var owner = new ReservationOwner("proof.return-release", 91);
            Submit(c, g, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(4), Bill(g, "ports.ore", 3)),
                new ResourceClaimIntent(ReservationDirection.Outbound, owner, Pool(7), Bill(g, "ports.jig", 1)) } });
            var source = Facts(g).Claims(4, owner); var tool = Facts(g).Claims(7, owner); var participants = source.Records.Concat(tool.Records).ToArray();
            ResourceMovementIntent Leg(ResourceEndpointId destination, ResourceMovementParticipantDisposition disposition) => Move(g, owner, Pool(4), destination, "ports.ore", 1, source.Records, participants, disposition);
            Refuse(c, g, Transfer(Leg(Sink(g), (ResourceMovementParticipantDisposition)99)), "unknown enum rejected");
            Refuse(c, g, Transfer(Leg(Sink(g), ResourceMovementParticipantDisposition.Release), Leg(Pool(6), ResourceMovementParticipantDisposition.Retain)), "one shared token cannot both retain and release");
            var request = Transfer(Leg(Sink(g), ResourceMovementParticipantDisposition.Release), Leg(Pool(6), ResourceMovementParticipantDisposition.Release));
            int sequence = g.Seat(0).Submit(request); var twin = factory.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending explicit release disposition saved");
            Step(g, twin); long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "complete two-leg movement and shared release accepted");
            c.That(Facts(g).Claims(4, owner).Records.Count == 0 && Facts(g).Claims(7, owner).Records.Count == 0, "spent source remainder and duplicate nonmoving tool released exactly once");
            c.That(Facts(g).Stock(4).AmountOf(Resource(g, "ports.ore")) == Fix.FromInt(2) && Facts(g).Stock(7).AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2), "release changes promises, never recreates consumed stock or eats tools");
            c.That(source.Records.Single().Amount == Fix.FromInt(3) && tool.Records.Single().Amount == Fix.One, "old receipt facts detached");
            c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "released movement durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "release enum and three receipt sets replay");
            c.Equal(idleHash, idle.Hash(), "independent session isolated");
        }

        private static void Cancellation(Check c, bool half)
        {
            var factory = Factory(half); var g = New(factory); using var recorder = g.RecordReplay(Limits); Start(c, g);
            var active = State(g); var original = active.Wip.Endpoint; var owner = active.Tools.Receipts[0].Owner;
            Select(c, g, "raw", 6); Select(c, g, "tool", 8);
            Seed(c, g, 4, "ports.ore", 2); Seed(c, g, 5, "ports.water", 1);
            int sequence = g.Seat(0).Submit(Cancel(g)); var twin = factory.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending cancellation saved");
            Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results); c.That(results.Single(x => x.Sequence == sequence).Accepted, "ordinary cancel accepted");
            c.That(State(g).Phase == TransformationPhase.Returning && State(g).Blocker == TransformerManager.BlockerReturnFull, "full original vessels block entire return despite changed selected ports");
            c.That(State(g).Tools.Receipts.Count == 2 && State(g).Wip.Stock.Count == 2 && State(g).Progress.Equals(active.Progress), "wait retains original tool/input/progress set");
            twin = factory.Load(g.Save()); g.Clock.Pause(); twin.Clock.Pause(); ulong hash = g.Hash(); Step(g, twin); c.Equal(hash, g.Hash(), "sole clock pauses waiting cancellation"); g.Clock.Resume(); twin.Clock.Resume();
            void Clear(long pool, string name, int amount)
            {
                Command Request(SimGame game) => Transfer(Move(game, new ReservationOwner("proof.return-clear", game.Clock.StepIndex + 100), Pool(pool), Sink(game), name, amount,
                    Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
                Submit(c, g, Request(g)); Submit(c, twin, Request(twin));
            }
            Clear(4, "ports.ore", 2);
            c.That(State(g).Phase == TransformationPhase.Returning && State(g).Tools.Receipts.Count == 2 && State(g).Wip.Stock.Count == 2, "one freed vessel cannot return a prefix or release tools");
            c.That(!g.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Any(x => x.Item.Source.Endpoint == original), "blocked return publishes no successful WIP movement");
            Clear(5, "ports.water", 1);
            c.That(State(g).QueueCount == 0 && State(g).Wip.Stock.IsEmpty && State(g).Tools.Receipts.Count == 0, "complete return and participant release settle original execution once");
            c.That(Facts(g).Claims(4, owner).Records.Count == 0 && Facts(g).Claims(7, owner).Records.Count == 0, "actual source and original tool books have no dangling promises");
            c.That(Facts(g).Stock(4).AmountOf(Resource(g, "ports.ore")) == Fix.FromInt(4) && Facts(g).Stock(5).AmountOf(Resource(g, "ports.water")) == Fix.One &&
                Facts(g).Stock(7).AmountOf(Resource(g, "ports.jig")) == Fix.FromInt(2) && Facts(g).Stock(6).IsEmpty && Facts(g).Stock(8).IsEmpty, "input and reusable tools remain at original stores, not new current selections");
            var returns = g.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Where(x => x.Item.Source.Endpoint == original).ToArray();
            c.That(returns.Length == 2 && returns.Any(x => x.Item.Destination.Endpoint == Pool(4) && x.Item.Amount == Fix.FromInt(2)) &&
                returns.Any(x => x.Item.Destination.Endpoint == Pool(5) && x.Item.Amount == Fix.One), "only complete verified mixed return facts publish");
            c.That(Facts(g).Stock(1).IsEmpty && Facts(g).Stock(2).IsEmpty, "cancellation never produces recipe outputs");
            c.That(active.Tools.Receipts.Count == 2 && active.Wip.Stock.Count == 2, "retained original execution facts immutable");
            c.Equal(g.Hash(), twin.Hash(), "saved returning twin converges"); c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "settled cancellation durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "full-vessel original-source cancellation replay");
        }

        private static void Violation(Check c, bool half, ClaimFaultMode mode)
        {
            var factory = Factory(half); var g = New(factory); Start(c, g); var published = g.Snapshot;
            Facts(g).Arm(4, 7, mode); g.Seat(0).Submit(Cancel(g)); g.Update();
            c.That(g.Fault != null && (g.Fault.Message.Contains("batch endpoint identity/version") || g.Fault.Message.Contains("unexpected batch stock")), "return/release guards detect separate tool mutation/" + mode + "/" + g.Fault);
            long stopped = g.Clock.StepIndex; c.That(g.State == GameState.Faulted && g.Update() == 0 && stopped == g.Clock.StepIndex, "violating cancellation stops sole clock");
            c.That(g.Snapshot.Tick == published.Tick && g.Snapshot.Entities.Count == published.Entities.Count, "previous publication retained, not compensating rollback");
            c.Throws<InvalidOperationException>(() => g.Save(), "dishonest partial return not resumable");
            c.Throws<InvalidOperationException>(() => g.Hash(), "dishonest partial return has no valid hash");
            c.Throws<InvalidOperationException>(() => g.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0), "faulted success history refused");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current release disposition identity"); foreach (bool half in new[] { false, true }) { Release(c, half); Cancellation(c, half); Violation(c, half, ClaimFaultMode.OtherEndpointReceive); Violation(c, half, ClaimFaultMode.OtherEndpointRelease); } }
    }
}

