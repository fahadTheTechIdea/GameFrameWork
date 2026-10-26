using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class TransformerWipTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.movement", 1);
        public static void Register()
        {
            TestRunner.Add("Transformations WIP: authored buffers, pure movement and private checkpoint contracts", Contracts);
            TestRunner.Add("Transformations WIP: timed accounting, blocked cancellation and saved continuation", Execution);
        }
        internal static GameFactory Factory(Action<JsonValue> edit = null) => TransformerTests.Factory(json =>
        {
            json.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            edit?.Invoke(json);
        });
        private static SimGame New(GameFactory factory) => TransformerTests.New(factory);
        private static Entity Machine(SimGame game) => TransformerTests.Find(game, "assembler");
        private static TransformationSnapshot State(SimGame game) => TransformerTests.State(game);
        private static ITransformerWip Provider(SimGame game) => Machine(game).Get<ITransformerWip>(new Code("transformation"));
        private static IInventory Stock(SimGame game) => Machine(game).Get<IInventory>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static void Tick(SimGame game, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                game.Update();
                if (game.Fault != null) throw new InvalidOperationException("WIP Update: " + game.Fault);
            }
        }
        private static int Outputs(SimGame game)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "token") count++; return count; }
        private static void Disable(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Machine(game).Id,
                Operation = new Code("transformation"), Enabled = false, Target = Fix.One }).Accepted, "ordinary control disables processing");
        private static void Cancel(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Machine(game).Id,
                Capability = new Code("transformation"), Index = 0 }).Accepted, "ordinary cancel requests actual material return");
        private static ResourceMovementIntent Move(IResourceOutlet source, IResourceInlet destination, Bill quantities,
            IReadOnlyList<ResourceReservation> claims = null, ReservationOwner? owner = null)
            => new ResourceMovementIntent(owner ?? Owner, Company, source.Endpoint, destination.Endpoint, quantities,
                claims ?? Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        private static Bill BillOf(ResourceId resource, int amount) => new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(amount)) });
        private static void Refuse(Check c, SimGame game, ResourceMovementIntent movement, ResourceTransferStatus expected, string reason)
        {
            ulong before = game.Hash(); long history = History(game).Latest;
            c.Equal(expected, Transactions(game).EvaluateMovement(movement).Status, reason + " pure admission");
            var result = Transactions(game).CommitMovement(movement);
            c.Equal(expected, result.Status, reason + " fresh refusal");
            c.Equal(0, result.CommittedTransfers, reason + " no partial movement");
            c.Equal(before, game.Hash(), reason + " stock, receipts and sequence unchanged");
            c.Equal(history, History(game).Latest, reason + " no success history");
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "clean WIP semantics identify current build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "native material state uses only the new save layout");
            c.That(game.World.Space == null && !game.World.Services.Has<IEconomy>(), "physical WIP does not require a map or money");
            var initial = State(game); ITransformerWip provider = Provider(game);
            c.That(initial.Wip.Endpoint.IsValid && initial.Wip.Endpoint.Identity > int.MaxValue, "WIP has stable scoped positive64 identity");
            c.That(initial.Wip.Stock.IsEmpty && initial.Wip.Execution == 0 && !provider.WorkInProgressOwner.IsValid, "idle WIP is genuinely empty and unowned");
            c.That(((IEntityRemovalGuard)provider).CheckRemoval(game.World).IsNone, "idle empty capability does not impede removal");
            var manager = game.World.Services.Get<TransformerManager>();
            c.That(ReferenceEquals(provider.WorkInProgress, manager.Resolve(initial.Wip.Endpoint)), "same manager resolves actual provider stock, not a mirror");
            c.That(manager.Resolve(new ResourceEndpointId(TransformerManager.WipScope, 1)) == null, "missing entity identity has no substitute");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)), "every physical outlet remains an inlet");
            ulong before = game.Hash();
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Machine(game).Id,
                new Code("transformation"), out _), "foreign progress and WIP remain private");
            c.Equal(before, game.Hash(), "private projection and endpoint resolution are pure");
            TransformerTests.Twin(c, factory, game);
            foreach (string invalid in new[] { "missing-buffer", "unknown-buffer", "small-buffer", "missing-policy", "unknown-policy", "missing-economy" })
                c.Throws<ContentException>(() => New(Factory(json =>
                {
                    JsonValue component = json.Get("entities")[0].Get("components").Get("transformer"), row = json.Get("transformations")[0];
                    switch (invalid)
                    {
                        case "missing-buffer": component.Remove("wip_capacity"); break;
                        case "unknown-buffer": component.Set("wip_capacity", JsonValue.Of("missing")); break;
                        case "small-buffer": component.Set("wip_capacity", JsonValue.Of("fixture.count.1")); break;
                        case "missing-policy": row.Remove("cancel_policy"); break;
                        case "unknown-policy": row.Set("cancel_policy", JsonValue.Of("refund")); break;
                        case "missing-economy": row.Set("cash", JsonValue.Number("1")); break;
                    }
                })), "invalid authored WIP fails before tick one: " + invalid);
            foreach (bool orphan in new[] { false, true })
            {
                SimGame corrupt = New(factory);
                if (orphan) Stock(corrupt).ReserveOut(new ReservationOwner(TransformerManager.WipOwnerScope(State(corrupt).Wip.Endpoint), 1), Supply(corrupt), Fix.One);
                else { var native = Machine(corrupt).Get<Transformer>(); native.Enqueue(corrupt.World.Content.Get<TransformationDef>("build"), 1, false, default); native.Phase = TransformationPhase.Working; }
                c.Throws<CorruptStateException>(() => corrupt.Save(), "checkpoint rejects " + (orphan ? "orphan execution-owned promise" : "active phase without material ownership"));
                corrupt.Update(); c.Equal(GameState.Faulted, corrupt.State, "normal update rejects the same corrupted authoritative state");
            }
            Movement(c);
            MovementFaults(c);
        }
        private static void Movement(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 72 });
            IInventory source = null;
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "source") source = entity.Get<IInventory>();
            var pool = game.World.Services.Get<EndpointPools>().Get(1);
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            Refuse(c, game, default, ResourceTransferStatus.InvalidRequest, "missing explicit movement");
            Refuse(c, game, Move(source, pool, Bill.Empty), ResourceTransferStatus.InvalidRequest, "empty bill");
            Refuse(c, game, Move(source, pool, BillOf(steel, 31)), ResourceTransferStatus.NoStock, "whole stock shortage");
            Refuse(c, game, Move(source, pool, new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(6)), new ResourceAmount(water, Fix.FromInt(5)) })),
                ResourceTransferStatus.NoCapacity, "combined bill overflow");
            Refuse(c, game, new ResourceMovementIntent(Owner, Company, source.Endpoint, pool.Endpoint, BillOf(steel, 1), null, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain),
                ResourceTransferStatus.InvalidRequest, "missing explicit source-claim mode");
            var mixed = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(3)), new ResourceAmount(water, Fix.FromRatio(1, 2)) });
            var movement = Move(source, pool, mixed); ulong hash = game.Hash();
            c.That(Transactions(game).EvaluateMovement(movement).Ready, "complete mixed-native bill preflights before claims");
            c.Equal(hash, game.Hash(), "ready movement cannot reserve a prefix");
            c.That(Transactions(game).CommitMovement(movement).Committed, "same owner acquires both directions and moves exact quantities");
            Tick(game);
            c.Equal(Fix.FromInt(27), source.Amount(steel), "native source moves steel, not consumed stock");
            c.Equal(Fix.FromRatio(9, 2), source.Amount(water), "native fluid remains fractional");
            c.That(source.Reservations.Count == 0 && pool.Reservations.Count == 0, "spent source and inlet receipts are retired");
            var claims = Transactions(game).AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner, pool.Endpoint, mixed) }, System.Array.Empty<ResourceReservation>());
            c.That(claims.Committed, "return starts with actual protected source receipts");
            var copied = new List<ResourceReservation>(claims.Receipts); var returned = Move(pool, source, mixed, copied); copied.Clear();
            c.Equal(2, returned.SourceClaims.Count, "request detaches actual receipt references from caller mutation");
            c.Throws<NotSupportedException>(() => ((IList<ResourceReservation>)returned.SourceClaims).Clear(), "retained movement references are immutable");
            var wrong = Move(pool, source, mixed, claims.Receipts, new ReservationOwner("proof.other", 1));
            Refuse(c, game, wrong, ResourceTransferStatus.InvalidReceipt, "foreign existing source receipts");
            SimGame reordered = factory.Load(game.Save()); var reversed = new List<ResourceReservation>(returned.SourceClaims); reversed.Reverse();
            c.That(Transactions(reordered).CommitMovement(new ResourceMovementIntent(Owner, Company, pool.Endpoint, source.Endpoint, mixed, reversed, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain)).Committed,
                "shuffled exact source receipt references admit together");
            c.That(Transactions(game).CommitMovement(returned).Committed, "existing source receipts transfer rather than reacquire or recreate");
            Tick(game); Tick(reordered);
            c.Equal(game.Hash(), reordered.Hash(), "canonical inlet acquisition preserves hash and history under shuffled references");
            c.That(source.Amount(steel) == Fix.FromInt(30) && source.Amount(water) == Fix.FromInt(5) && pool.Amount(steel) == Fix.Zero && pool.Amount(water) == Fix.Zero,
                "both movement directions conserve exact original quantities");
            c.Equal(BigInteger.Zero, History(game).Totals(steel).CreatedRaw + History(game).Totals(steel).ConsumedRaw, "movement publishes no creation or consumption");
            c.Equal(4, History(game).Totals(steel).TransferLegs + History(game).Totals(water).TransferLegs, "one verified native fact per direction and line");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status, "movement history is company-private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native movement uses the existing endpoint codecs");
            // Pending engine command includes real existing source claims, not a second reservation ledger.
            var held = Transactions(game).AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner, source.Endpoint, mixed) }, System.Array.Empty<ResourceReservation>());
            game.Seat(0).Submit(new TransferProbeCommand { Move = true, Movements = new[] { Move(source, pool, mixed, held.Receipts) } });
            SimGame restored = factory.Load(game.Save()); Tick(game); Tick(restored);
            c.Equal(game.Hash(), restored.Hash(), "pending movement command and exact owned receipts survive save/load");
            foreach (bool absentBill in new[] { false, true })
            {
                SimGame invalid = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 74 }); IInventory actual = null;
                foreach (Entity entity in invalid.World.Entities.All) if (entity.Def.Id == "source") actual = entity.Get<IInventory>();
                var inlet = invalid.World.Services.Get<EndpointPools>().Get(1); long history = History(invalid).Latest;
                invalid.Seat(0).Submit(new TransferProbeCommand { Move = true, Movements = new[] { new ResourceMovementIntent(Owner, Company,
                    actual.Endpoint, inlet.Endpoint, absentBill ? null : mixed, absentBill ? Array.Empty<ResourceReservation>() : null, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } });
                SimGame saved = factory.Load(invalid.Save()); Tick(invalid); Tick(saved);
                c.Equal(invalid.Hash(), saved.Hash(), "absent request field is not fabricated into a valid mode on restore");
                c.That(actual.Amount(steel) == Fix.FromInt(30) && inlet.Amount(steel) == Fix.Zero && actual.Reservations.Count == 0 && inlet.Reservations.Count == 0,
                    "serialized invalid request does not acquire or move a native prefix");
                c.Equal(history, History(invalid).Latest, "serialized invalid request publishes no successful history");
            }
        }
        private static void MovementFaults(Check c)
        {
            foreach (ClaimFaultMode mode in new[] { ClaimFaultMode.MutatingCheck, ClaimFaultMode.ExtraAcquire, ClaimFaultMode.ShortTake, ClaimFaultMode.ShortReceive, ClaimFaultMode.OtherEndpoint })
            {
                GameFactory factory = EndpointGame.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 73 });
                IInventory source = null; foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "source") source = entity.Get<IInventory>();
                var pools = game.World.Services.Get<EndpointPools>(); var first = pools.Get(1); var second = pools.Get(2);
                ResourceId steel = game.World.Content.ResourceId("steel");
                c.That(Transactions(game).CommitMovement(Move(source, second, BillOf(steel, 4))).Committed, "fault fixture uses actual verified native movement");
                Tick(game); long latest = History(game).Latest; pools.ClaimFault = mode;
                pools.FaultEndpoint = mode == ClaimFaultMode.ShortReceive ? 1 : 2;
                game.Seat(0).Submit(new TransferProbeCommand { Move = true, Movements = new[] { Move(second, first, BillOf(steel, 2)) } });
                game.Update();
                c.Equal(GameState.Faulted, game.State, "provider violation faults actual update: " + mode);
                c.Equal(latest, History(game).Latest, "fault publishes no transfer success: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "partial provider state cannot checkpoint: " + mode);
                c.Throws<InvalidOperationException>(() => game.Hash(), "partial provider state cannot appear deterministic: " + mode);
            }
        }
        private static void Execution(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); ResourceId resource = Supply(game);
            c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "normal command starts timed process");
            TransformationSnapshot working = State(game); var port = Provider(game).WorkInProgress;
            c.That(working.Phase == TransformationPhase.Working && working.Wip.Execution == 1 && working.Progress.WholeTicks == 1, "real first update allocates execution and advances once");
            c.Equal(Fix.FromInt(17), Stock(game).Amount(resource), "raw inputs leave general inventory");
            c.Equal(Fix.FromInt(3), port.Amount(resource), "raw inputs occupy actual WIP");
            c.Equal(Fix.Zero, port.Available(resource), "active raw inputs are protected by actual outbound receipt");
            c.Equal(1, port.Reservations.Count, "one native execution-owned claim");
            c.Equal(TransformerManager.BlockerActive, ((IEntityRemovalGuard)Provider(game)).CheckRemoval(game.World), "active execution guards removal and replacement");
            c.Equal(BigInteger.Zero, History(game).Totals(resource).ConsumedRaw, "starting work never consumes raw WIP");
            c.Equal(0, Outputs(game), "no instantaneous entity result");
            TransformerTests.Twin(c, factory, game);
            Tick(game, 5);
            c.That(State(game).QueueCount == 0 && State(game).Wip.Execution == 0 && State(game).Wip.Stock.IsEmpty && port.Reservations.Count == 0, "completion closes real WIP once");
            c.That(Outputs(game) == 1 && Stock(game).Amount(resource) == Fix.FromInt(18), "six timed steps produce exact resource/entity results");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(resource).ConsumedRaw, "actual input consumption occurs at completion");
            c.Equal(new BigInteger(Fix.One.Raw), History(game).Totals(resource).CreatedRaw, "only actual result is produced");
            foreach (var entry in History(game).Entries)
                if (entry.Item.Kind == ResourceFlowKind.Consumed) c.Equal(port.Endpoint, entry.Item.Source.Endpoint, "consumption identifies actual WIP endpoint");
            c.Equal(Fix.FromInt(3), working.Wip.Stock.AmountOf(resource), "retained view stays immutable after live completion");
            TransformerTests.Twin(c, factory, game);
            BlockedOutput(c, factory);
            BlockedReturn(c, factory);
            RepeatAndIsolation(c, factory);
            DisabledAndEmpty(c, factory);
            MultiInput(c);
            SimGame pulse = New(factory); c.That(TransformerTests.Queue(pulse, "emitter", "pulse").Accepted, "unrelated provider still dispatches through same manager");
            var none = TransformerTests.State(pulse, "emitter").Wip;
            c.That(!none.Endpoint.IsValid && none.Execution == 0 && none.Stock.IsEmpty, "unrelated material-free provider explicitly declares no WIP");
            Tick(pulse); c.Equal(1, Outputs(pulse), "external engine-side provider produces its real timed result");
            TransformerTests.Twin(c, factory, pulse);
            SimGame guarded = New(factory); TransformerTests.Queue(guarded, "assembler", "build");
            Entity destination = guarded.World.Spawn(guarded.World.Content.Get<EntityDef>("assembler"), Company); guarded.World.Settle();
            c.That(Transactions(guarded).CommitMovement(Move(Stock(guarded), destination.Get<IInventory>(), BillOf(Supply(guarded), 17))).Committed,
                "removal fixture physically empties general inventory while retaining actual WIP");
            Tick(guarded);
            c.Equal(TransformerManager.BlockerActive, guarded.World.CheckDespawn(Machine(guarded)), "real removal path refuses active WIP even with empty general stock");
            c.Throws<InvalidOperationException>(() => guarded.World.Despawn(Machine(guarded)), "despawn cannot bypass physical execution ownership");
            TransformerTests.Twin(c, factory, guarded);
        }
        private static void BlockedOutput(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceId resource = Supply(game);
            TransformerTests.Queue(game, "assembler", "build");
            var other = new ReservationOwner("proof.output-hold", 1);
            ReservationToken hold = Stock(game).ReserveIn(other, resource, Fix.FromInt(23));
            Tick(game, 5);
            c.That(State(game).Phase == TransformationPhase.Blocked && State(game).Blocker == TransformerManager.BlockerOutput && Outputs(game) == 0,
                "completed work waits for real output room before any result");
            c.That(State(game).Progress.Complete(6) && Provider(game).WorkInProgress.Amount(resource) == Fix.FromInt(3), "blocked outputs retain completed progress and physical raw inputs");
            TransformerTests.Twin(c, factory, game);
            SimGame restored = factory.Load(game.Save()); Stock(restored).ReleaseIn(other, hold);
            Stock(game).ReleaseIn(other, hold); Tick(game); Tick(restored);
            c.Equal(game.Hash(), restored.Hash(), "saved completed work finishes identically after actual capacity release");
            c.That(Outputs(game) == 1 && State(game).Wip.Stock.IsEmpty, "released actual capacity completes once");
            Tick(game, 2); c.Equal(1, Outputs(game), "completed checkpoint cannot duplicate result");
        }
        private static void BlockedReturn(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceId resource = Supply(game);
            TransformerTests.Queue(game, "assembler", "build", repeat: true); TransformationSnapshot retained = State(game);
            Entity donor = game.World.Spawn(game.World.Content.Get<EntityDef>("assembler"), Company); game.World.Settle();
            var other = new ReservationOwner("proof.return-hold", 1);
            ReservationToken hold = Stock(game).ReserveIn(other, resource, Fix.FromInt(3));
            c.That(Transactions(game).CommitMovement(Move(donor.Get<IInventory>(), Stock(game), BillOf(resource, 20))).Committed, "capacity pressure is actual authored donor stock transferred physically");
            Tick(game); Disable(c, game); WorkProgress progress = State(game).Progress;
            Cancel(c, game);
            c.That(State(game).Phase == TransformationPhase.Returning && State(game).Blocker == TransformerManager.BlockerReturnFull, "full destination produces saved returning state, not a refund");
            c.That(State(game).Progress.Equals(progress) && State(game).Wip.Execution == retained.Wip.Execution && State(game).QueueCount == 1, "blocked cancellation retains exact execution/progress/head");
            c.That(Stock(game).Amount(resource) == Fix.FromInt(37) && Provider(game).WorkInProgress.Amount(resource) == Fix.FromInt(3) && donor.Get<IInventory>().Amount(resource) == Fix.Zero,
                "all forty real units remain conserved while return is blocked");
            c.Equal(TransformerManager.BlockerReturning, game.Seat(0).View.Feature<TransformationManagementView>().PreviewCancel(Machine(game).Id,
                new Code("transformation"), 0), "duplicate cancel cannot release or duplicate protected WIP");
            c.Equal(TransformerManager.BlockerActive, ((IEntityRemovalGuard)Provider(game)).CheckRemoval(game.World), "returning WIP guards entity removal");
            TransformerTests.Twin(c, factory, game); Tick(game, 3);
            c.That(State(game).Progress.Equals(progress) && Outputs(game) == 0, "disabled returning execution waits without doing work or producing results");
            SimGame restored = factory.Load(game.Save()); Stock(restored).ReleaseIn(other, hold);
            Stock(game).ReleaseIn(other, hold); Tick(game); Tick(restored);
            c.Equal(game.Hash(), restored.Hash(), "saved disabled returning phase finishes the same physical return");
            c.That(State(game).QueueCount == 0 && State(game).Wip.Execution == 0 && State(game).Wip.Stock.IsEmpty && Stock(game).Amount(resource) == Fix.FromInt(40),
                "disabled operation returns actual inputs when physical capacity is available");
            c.Equal(BigInteger.Zero, History(game).Totals(resource).CreatedRaw + History(game).Totals(resource).ConsumedRaw, "cancel is neither produced refund nor consumed material");
            c.Equal(Fix.FromInt(3), retained.Wip.Stock.AmountOf(resource), "retained WIP snapshot does not follow return mutation");
            Tick(game, 8); c.That(State(game).QueueCount == 0 && Outputs(game) == 0, "cancelled repeat is not silently requeued");
            TransformerTests.Twin(c, factory, game);
        }
        private static void RepeatAndIsolation(Check c, GameFactory factory)
        {
            SimGame game = New(factory); TransformerTests.Queue(game, "assembler", "build", repeat: true); Tick(game, 5);
            c.That(State(game).QueueCount == 1 && State(game).Phase == TransformationPhase.Supplying && State(game).Wip.Execution == 0,
                "repeat boundary retains queue intent but no active raw material");
            TransformerTests.Twin(c, factory, game); Tick(game);
            c.Equal(2L, State(game).Wip.Execution, "each native execution has a new stable identity");
            Cancel(c, game); c.That(State(game).Wip.Stock.IsEmpty && State(game).QueueCount == 0 && Outputs(game) == 1, "active repeated head returns inputs without repeating output");
            SimGame concurrent = New(factory);
            Entity second = concurrent.World.Spawn(concurrent.World.Content.Get<EntityDef>("assembler"), Company); concurrent.World.Settle(); Tick(concurrent);
            Entity first = Machine(concurrent);
            TransformerTests.Queue(concurrent, "assembler", "build");
            c.That(TestKit.Order(concurrent, 0, new EnqueueTransformationCommand { Transformer = second.Id, Capability = new Code("transformation"), Transformation = "build" }).Accepted,
                "two independent machines start through ordinary commands");
            var firstProvider = first.Get<ITransformerWip>(new Code("transformation")); var secondProvider = second.Get<ITransformerWip>(new Code("transformation"));
            c.That(firstProvider.WorkInProgressOwner.Identity == 1 && secondProvider.WorkInProgressOwner.Identity == 1 &&
                !firstProvider.WorkInProgressOwner.Equals(secondProvider.WorkInProgressOwner) && firstProvider.WorkInProgress.Endpoint != secondProvider.WorkInProgress.Endpoint,
                "local execution counters cannot collide across actual capability endpoints");
            TransformerTests.Twin(c, factory, concurrent); Tick(concurrent, 5);
            c.Equal(2, Outputs(concurrent), "shared manager advances each actual machine exactly once per step");
            TransformerTests.Twin(c, factory, concurrent);
            SimGame supplying = New(factory); var claim = Stock(supplying).ReserveOut(Owner, Supply(supplying), Fix.FromInt(20));
            TransformerTests.Queue(supplying, "assembler", "build");
            c.That(State(supplying).Phase == TransformationPhase.Supplying && State(supplying).Wip.Execution == 0 && State(supplying).Wip.Stock.IsEmpty,
                "starved readiness never allocates or consumes an execution");
            TransformerTests.Twin(c, factory, supplying); Cancel(c, supplying);
            c.That(State(supplying).QueueCount == 0 && Stock(supplying).Reservations.Count == 1, "unstarted cancel leaves unrelated native claims untouched");
            Stock(supplying).ReleaseOut(Owner, claim);
        }
        private static void DisabledAndEmpty(Check c, GameFactory factory)
        {
            SimGame game = New(factory); TransformerTests.Queue(game, "assembler", "build"); Disable(c, game);
            WorkProgress progress = State(game).Progress; TransformerTests.Twin(c, factory, game); Tick(game, 3);
            c.That(State(game).Phase == TransformationPhase.Working && State(game).Progress.Equals(progress) && State(game).Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3),
                "disabled saved work retains protected material and cannot advance");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Machine(game).Id, Operation = new Code("transformation"), Enabled = true, Target = Fix.One }).Accepted,
                "ordinary enable resumes same execution");
            Tick(game, 4); c.That(Outputs(game) == 1 && State(game).Wip.Stock.IsEmpty, "resumed work consumes retained inputs and produces once");
            GameFactory free = Factory(json =>
            {
                json.Get("transformations")[0].Get("inputs").Remove("supply");
                json.Get("transformations")[0].Get("input_ports").Remove("supply");
            });
            SimGame empty = New(free); TransformerTests.Queue(empty, "assembler", "build");
            c.That(State(empty).Wip.Execution == 1 && State(empty).Wip.Stock.IsEmpty && Provider(empty).WorkInProgress.Reservations.Count == 0,
                "authored material-free work has a real execution without fabricated stock or receipts");
            TransformerTests.Twin(c, free, empty); Cancel(c, empty);
            c.That(State(empty).QueueCount == 0 && Stock(empty).Amount(Supply(empty)) == Fix.FromInt(20) && Outputs(empty) == 0,
                "material-free cancellation closes only its actual execution");
        }
        private static void MultiInput(Check c)
        {
            GameFactory factory = Factory(json =>
            {
                json.Get("resources").Add(Json.Parse("{\"id\":\"parts\",\"unit\":\"unit.kit\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.kit\":1}}"));
                JsonValue components = json.Get("entities")[0].Get("components"); components.Remove("process_stock");
                components.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{\"supply\":20,\"parts\":5}}"));
                json.Get("transformations")[0].Get("inputs").Set("parts", JsonValue.Number("2"));
                json.Get("transformations")[0].Get("input_ports").Set("parts", JsonValue.Of("material.input"));
            });
            SimGame game = New(factory); ResourceId supply = Supply(game), parts = game.World.Content.ResourceId("parts");
            TransformerTests.Queue(game, "assembler", "build");
            c.That(State(game).Wip.Stock.Count == 2 && Provider(game).WorkInProgress.Reservations.Count == 2, "authored multi-input bill occupies and protects both native WIP lines");
            c.That(Stock(game).Amount(supply) == Fix.FromInt(17) && Stock(game).Amount(parts) == Fix.FromInt(3), "whole input movement has exact native vector");
            TransformerTests.Twin(c, factory, game); Cancel(c, game);
            c.That(Stock(game).Amount(supply) == Fix.FromInt(20) && Stock(game).Amount(parts) == Fix.FromInt(5) && State(game).Wip.Stock.IsEmpty, "whole multi-line cancellation returns all actual inputs once");
            c.Equal(BigInteger.Zero, History(game).Totals(parts).CreatedRaw + History(game).Totals(parts).ConsumedRaw, "multi-input cancel fabricates no material");
            TransformerTests.Queue(game, "assembler", "build"); Tick(game, 5);
            c.That(Outputs(game) == 1 && Stock(game).Amount(parts) == Fix.FromInt(3), "multi-input completion consumes only the actual protected second input");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), History(game).Totals(parts).ConsumedRaw, "each real multi-input resource is accounted separately");
            TransformerTests.Twin(c, factory, game);
        }
    }
}
