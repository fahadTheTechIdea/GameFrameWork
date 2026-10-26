using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class TransformerToolTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Other = new ReservationOwner("proof.tool-other", 1);
        private static readonly Code Capability = new Code("transformation");
        public static void Register()
        {
            TestRunner.Add("Transformation tools: authored contracts, bounded receipts and private ownership", Contracts);
            TestRunner.Add("Transformation tools: physical batch holds, interruption and saved continuation", Processes);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null) => TransformerWipTests.Factory(json =>
        {
            json.Get("transformations")[0].Get("tools").Set("supply", JsonValue.Number("2")); edit?.Invoke(json);
        });
        private static SimGame New(GameFactory factory) => TransformerTests.New(factory);
        private static Entity Machine(SimGame game) => TransformerTests.Find(game, "assembler");
        private static IResourceOutlet ToolStore(SimGame game) => (IResourceOutlet)Machine(game).Get<IResourcePort>(new Code("tool.input")).ResolveStore(game.World).Store;
        private static IInventory Inventory(SimGame game) => Machine(game).Get<IInventory>();
        private static ITransformerTools Tools(SimGame game) => Machine(game).Get<ITransformerTools>(Capability);
        private static TransformationSnapshot State(SimGame game) => TransformerTests.State(game);
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static Bill Quantity(ResourceId resource, int amount) => new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(amount)) });
        private static ResourceMovementIntent Move(IResourceOutlet source, IResourceInlet destination, Bill quantities,
            IReadOnlyList<ResourceReservation> claims, ReservationOwner owner)
            => new ResourceMovementIntent(owner, Company, source.Endpoint, destination.Endpoint, quantities, claims, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        private static int Outputs(SimGame game)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "token") count++; return count; }
        private static void Tick(SimGame game, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                game.Update(); if (game.Fault != null) throw new InvalidOperationException("tools Update: " + game.Fault);
            }
        }
        private static void Cancel(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Machine(game).Id, Capability = Capability, Index = 0 }).Accepted,
                "ordinary cancel requests actual input return and tool release");
        private static void Control(Check c, SimGame game, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Machine(game).Id, Operation = Capability, Enabled = enabled, Target = Fix.One }).Accepted,
                "ordinary operation changes actual processing enable state");
        private static ResourceReservation Find(IResourceOutlet port, ReservationOwner owner, ResourceId resource)
        {
            foreach (ResourceReservation record in port.Reservations) if (record.Owner.Equals(owner) && record.Resource == resource && record.Direction == ReservationDirection.Outbound) return record;
            throw new InvalidOperationException("missing actual tool receipt");
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); ulong hash = game.Hash();
            var definition = game.World.Content.Get<TransformationDef>("build"); ResourceId supply = Supply(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "tool semantics have a breaking build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "there is only the current self-described save identity");
            c.Equal(Fix.FromInt(2), definition.Tools.AmountOf(supply), "master-listed JSON resolves native tool requirements");
            c.Equal(Fix.FromInt(5), definition.StartResources.AmountOf(supply), "same-resource input/tool requirements combine exactly");
            c.That(ReferenceEquals(ToolStore(game), Inventory(game)), "native tool store is the existing actual inventory capability");
            c.That(State(game).Tools.Sources.Count == 0 && State(game).Tools.Held.IsEmpty, "idle view reports actual empty execution commitments");
            c.Equal(hash, game.Hash(), "content and actual tool projection are pure");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Machine(game).Id, Capability, out _), "foreign tool commitments remain private");
            TransformerTests.Twin(c, factory, game);
            foreach (string invalid in new[] { "missing", "unknown", "negative", "fractional", "large", "overflow", "split-quantum", "unsupported" })
                c.Throws<ContentException>(() => New(Factory(json =>
                {
                    JsonValue row = json.Get("transformations")[0], components = json.Get("entities")[0].Get("components");
                    switch (invalid)
                    {
                        case "missing": row.Remove("tools"); break;
                        case "unknown": row.Get("tools").Set("unknown", JsonValue.Number("1")); break;
                        case "negative": row.Get("tools").Set("supply", JsonValue.Number("-1")); break;
                        case "fractional": row.Get("tools").Set("supply", JsonValue.Number("0.5")); break;
                        case "large": row.Get("tools").Set("supply", JsonValue.Number("38")); break;
                        case "overflow": row.Get("tools").Set("supply", JsonValue.Number("2147483647")); break;
                        case "split-quantum":
                            row.Get("tools").Set("supply", JsonValue.Number("1"));
                            json.Get("resources")[0].Get("capacity").Set("unit.kit", JsonValue.Number("0.5"));
                            json.Get("resources")[0].Get("capacity").Set("unit.capacity.mass", JsonValue.Number("1"));
                            components.Get("transformer").Set("wip_capacity", JsonValue.Of("fixture.mass.40")); break;
                        case "unsupported":
                            JsonValue processor = components.Get("transformer"); processor.Remove("automatic_supply"); processor.Remove("wip_capacity");
                            components.Remove("transformer"); components.Set("pulse_transformer", processor); break;
                    }
                })), "invalid authored tool contract refuses before first tick: " + invalid);
            foreach (string corrupt in new[] { "missing", "extra", "foreign-endpoint" })
            {
                SimGame invalid = New(factory); Entity second = null;
                if (corrupt == "foreign-endpoint") { second = invalid.World.Spawn(invalid.World.Content.Get<EntityDef>("assembler"), Company); invalid.World.Settle(); }
                TransformerTests.Queue(invalid, "assembler", "build");
                ITransformerTools tools = Tools(invalid); var actual = Find(ToolStore(invalid), tools.WorkInProgressOwner, Supply(invalid));
                if (corrupt == "missing") ToolStore(invalid).ReleaseOut(actual.Owner, actual.Token);
                else if (corrupt == "extra") ToolStore(invalid).IncreaseReservedOut(actual.Owner, actual.Token, Fix.One);
                else
                {
                    second.Get<IInventory>().ReserveOut(actual.Owner, Supply(invalid), Fix.One);
                }
                c.Throws<CorruptStateException>(() => invalid.Save(), "checkpoint rejects " + corrupt + " actual execution tool claim");
                invalid.Update(); c.Equal(GameState.Faulted, invalid.State, "normal update refuses same invalid tool ownership: " + corrupt);
            }
            BoundedMovement(c);
            NativeFaults(c);
        }
        private static void Refuse(Check c, SimGame game, ResourceMovementIntent intent, ResourceTransferStatus status, string reason)
        {
            ulong hash = game.Hash(); long history = History(game).Latest;
            c.Equal(status, Transactions(game).EvaluateMovement(intent).Status, reason + " pure refusal");
            c.Equal(status, Transactions(game).CommitMovement(intent).Status, reason + " fresh refusal");
            c.Equal(hash, game.Hash(), reason + " no stock/book/version/sequence mutation");
            c.Equal(history, History(game).Latest, reason + " no success history");
        }
        private static void BoundedMovement(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 81 });
            IInventory source = null; foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "source") source = entity.Get<IInventory>();
            var inlet = game.World.Services.Get<EndpointPools>().Get(1); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            var owner = new ReservationOwner("proof.partial-movement", 1);
            var full = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(6)), new ResourceAmount(water, Fix.FromInt(2)) });
            var moved = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(3)), new ResourceAmount(water, Fix.FromRatio(1, 2)) });
            var held = Transactions(game).AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, source.Endpoint, full) }, System.Array.Empty<ResourceReservation>());
            c.That(held.Committed, "actual mixed-native source claims are protected before movement");
            ReservationToken other = source.ReserveOut(Other, steel, Fix.One); ulong before = game.Hash();
            c.That(Transactions(game).EvaluateMovement(Move(source, inlet, moved, held.Receipts, owner)).Ready, "explicit bounded bill admits exact full source records");
            c.Equal(before, game.Hash(), "bounded receipt admission cannot acquire a native prefix");
            Refuse(c, game, Move(source, inlet, Quantity(steel, 7), new[] { Find(source, owner, steel) }, owner), ResourceTransferStatus.InvalidReceipt, "overspent actual record");
            var fabricated = new ResourceReservation(held.Receipts[0].Token, owner, held.Receipts[0].Resource, Fix.One, ReservationDirection.Outbound);
            Refuse(c, game, Move(source, inlet, Quantity(held.Receipts[0].Resource, 1), new[] { fabricated }, owner), ResourceTransferStatus.InvalidReceipt, "fabricated smaller source record");
            SimGame restored = factory.Load(game.Save()); var shuffled = new List<ResourceReservation>(held.Receipts); shuffled.Reverse();
            c.That(Transactions(restored).CommitMovement(Move(source, inlet, moved, shuffled, owner)).Committed, "shuffled full source references commit bounded quantities");
            c.That(Transactions(game).CommitMovement(Move(source, inlet, moved, held.Receipts, owner)).Committed, "native bounded movement retains reusable remainder");
            Tick(game); Tick(restored); c.Equal(game.Hash(), restored.Hash(), "partial native history and tokens are canonical under source order");
            ResourceReservation steelLeft = Find(source, owner, steel), waterLeft = Find(source, owner, water);
            c.Equal(Fix.FromInt(3), steelLeft.Amount, "actual original steel token retains exact reusable quantity");
            c.Equal(Fix.FromRatio(3, 2), waterLeft.Amount, "actual original fluid token retains native fractional remainder");
            c.Equal(held.Receipts[0].Token, steelLeft.Token, "source remainder does not allocate a replacement token");
            c.That(source.Reservations.Count == 3 && inlet.Reservations.Count == 0 && source.ReservedOut(steel) == Fix.FromInt(4), "unrelated owner and reusable source records survive inlet retirement");
            Refuse(c, game, Move(source, inlet, moved, held.Receipts, owner), ResourceTransferStatus.InvalidReceipt, "stale full references after bounded spending");
            c.Equal(BigInteger.Zero, History(game).Totals(steel).ConsumedRaw + History(game).Totals(steel).CreatedRaw, "bounded movement is not a consuming tool operation");
            c.Equal(Fix.FromInt(30), source.Amount(steel) + inlet.Amount(steel), "all native steel remains actual physical stock");
            c.Equal(Fix.FromInt(5), source.Amount(water) + inlet.Amount(water), "all native fluid remains actual physical stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "same-token source remainders save through existing native book");
            c.That(Transactions(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, new[] { steelLeft, waterLeft }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "native tools release their own remaining records");
            c.That(source.Reservations.Count == 1 && source.Reservations[0].Token.Equals(other), "tool release leaves unrelated owner untouched");
        }
        private static void NativeFaults(Check c)
        {
            foreach (string fault in new[] { "partial_take", "retain_take", "mutating_check", "committed_check" })
            {
                SimGame game = New(Factory(json => json.Get("entities")[0].Get("components").Get("process_stock").Set("receipt_fault", JsonValue.Of(fault))));
                long history = History(game).Latest;
                game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = Machine(game).Id, Capability = Capability, Transformation = "build" }); game.Update();
                c.Equal(GameState.Faulted, game.State, "native input/tool acquisition detects provider violation: " + fault);
                c.Equal(history, History(game).Latest, "failed startup emits no verified transfer/consumption: " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "partial startup cannot checkpoint: " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "partial startup cannot hash as resumable: " + fault);
            }
            SimGame released = New(Factory(json => json.Get("entities")[0].Get("components").Get("process_stock").Set("receipt_fault", JsonValue.Of("ignore_release"))));
            c.That(TransformerTests.Queue(released, "assembler", "build").Accepted, "hostile release provider still performs legitimate tool acquisition");
            long latest = History(released).Latest; released.RunTicks(5);
            c.Equal(GameState.Faulted, released.State, "ignored actual tool release faults completion, not silently successful work");
            c.Equal(latest, History(released).Latest, "release violation cannot publish completed conversion history");
            c.Throws<InvalidOperationException>(() => released.Save(), "failed real release cannot checkpoint partial conversion");
            c.Throws<InvalidOperationException>(() => released.Hash(), "failed real release cannot hash as settled completion");
        }
        private static void Processes(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); ResourceId supply = Supply(game);
            ReservationToken other = Inventory(game).ReserveOut(Other, supply, Fix.One);
            c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "normal command starts input/tool batch");
            var retained = State(game); ResourceReservation tool = Find(ToolStore(game), Tools(game).WorkInProgressOwner, supply);
            c.That(retained.Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && retained.Tools.Held.AmountOf(supply) == Fix.FromInt(2), "raw WIP and reusable holds are distinct real ownership");
            c.That(Inventory(game).Amount(supply) == Fix.FromInt(17) && Inventory(game).Available(supply) == Fix.FromInt(14), "only raw inputs move; tool and other-owner stock stay protected locally");
            c.Equal(Fix.FromInt(2), tool.Amount, "overlapping input/tool source receipt becomes the actual tool remainder");
            c.Equal(BigInteger.Zero, History(game).Totals(supply).ConsumedRaw, "neither input startup nor tool hold is consumption");
            c.That(!Inventory(game).CanReserveOut(new ReservationOwner("proof.third", 1), supply, Fix.FromInt(20)), "ordinary oversized consumer cannot claim protected tools");
            TransformerTests.Twin(c, factory, game); Control(c, game, false); var progress = State(game).Progress;
            Tick(game, 3); c.That(State(game).Progress.Equals(progress) && State(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2), "disabled processing retains actual tool commitments");
            TransformerTests.Twin(c, factory, game); Control(c, game, true); Tick(game, 4);
            c.That(Outputs(game) == 1 && State(game).Tools.Held.IsEmpty && State(game).Wip.Stock.IsEmpty, "timed completion closes WIP and releases tools once");
            c.That(Inventory(game).Amount(supply) == Fix.FromInt(18) && Inventory(game).Reservations.Count == 1 && Inventory(game).Reservations[0].Token.Equals(other), "completion conserves tools and unrelated owner's actual token");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(supply).ConsumedRaw, "completion consumes raw bill only, never the two reusable units");
            c.Equal(Fix.FromInt(2), retained.Tools.Held.AmountOf(supply), "retained tool snapshot does not follow live release");
            TransformerTests.Twin(c, factory, game);
            Shortage(c, factory); BlockedOutput(c, factory); Returning(c, factory); Repeats(c, factory); SeparateTools(c); PhysicalSupply(c);
            // Directly affected predecessor workflows execute inside these two gates, not a third suite.
            SimGame plain = TransformerTests.New(TransformerWipTests.Factory()); TransformerTests.Queue(plain, "assembler", "build"); Tick(plain, 5);
            c.That(Outputs(plain) == 1 && State(plain).Tools.Held.IsEmpty, "explicit empty-tool native authoring still completes normally");
            c.That(TransformerTests.Queue(plain, "emitter", "pulse").Accepted, "unrelated provider remains on the same manager path"); Tick(plain);
            c.That(Outputs(plain) == 2 && TransformerTests.State(plain, "emitter").Tools.Held.IsEmpty, "material-free public provider declares explicit empty tools and real output");
        }
        private static void Shortage(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceId supply = Supply(game); ReservationToken hold = Inventory(game).ReserveOut(Other, supply, Fix.FromInt(16));
            c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "valid intent queues while tools are unavailable");
            c.That(State(game).Phase == TransformationPhase.Supplying && State(game).Blocker == TransformerManager.BlockerTools && State(game).Wip.Execution == 0,
                "individually available inputs/tools cannot bypass their combined requirement");
            c.That(Inventory(game).Amount(supply) == Fix.FromInt(20) && Inventory(game).Reservations.Count == 1 && State(game).Tools.Held.IsEmpty && State(game).Wip.Stock.IsEmpty,
                "shortage commits no source/WIP/receipt prefix");
            c.Equal(BigInteger.Zero, History(game).Totals(supply).ConsumedRaw, "tool shortage spends nothing");
            TransformerTests.Twin(c, factory, game); Inventory(game).ReleaseOut(Other, hold); Tick(game);
            c.That(State(game).Phase == TransformationPhase.Working && State(game).Wip.Execution == 1 && State(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2),
                "released actual stock admits same queued batch once");
            Cancel(c, game); c.That(Inventory(game).Amount(supply) == Fix.FromInt(20) && State(game).Tools.Held.IsEmpty && Inventory(game).Reservations.Count == 0, "cancel returns raw inputs and releases actual reusable remainder");
            TransformerTests.Twin(c, factory, game);
        }
        private static void BlockedOutput(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceId supply = Supply(game); TransformerTests.Queue(game, "assembler", "build");
            ReservationToken hold = Inventory(game).ReserveIn(Other, supply, Fix.FromInt(23)); Tick(game, 5);
            c.That(State(game).Phase == TransformationPhase.Blocked && State(game).Blocker == TransformerManager.BlockerOutput && State(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2),
                "full result inventory retains completed work and tools");
            c.Equal(0, Outputs(game), "output wait does not create an entity prefix"); TransformerTests.Twin(c, factory, game);
            SimGame restored = factory.Load(game.Save()); Inventory(game).ReleaseIn(Other, hold); Inventory(restored).ReleaseIn(Other, hold); Tick(game); Tick(restored);
            c.Equal(game.Hash(), restored.Hash(), "saved output capacity release completes same protected tool execution");
            c.That(Outputs(game) == 1 && State(game).Tools.Held.IsEmpty && Inventory(game).Reservations.Count == 0, "output admission releases only completed batch tools");
        }
        private static void Returning(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceId supply = Supply(game); TransformerTests.Queue(game, "assembler", "build", repeat: true);
            Entity donor = game.World.Spawn(game.World.Content.Get<EntityDef>("assembler"), Company); game.World.Settle();
            ReservationToken hold = Inventory(game).ReserveIn(Other, supply, Fix.FromInt(3));
            c.That(Transactions(game).CommitMovement(Move(donor.Get<IInventory>(), Inventory(game), Quantity(supply, 20), Array.Empty<ResourceReservation>(), new ReservationOwner("proof.return-pressure", 1))).Committed,
                "return pressure uses physically transferred authored stock");
            Tick(game); Control(c, game, false); Cancel(c, game);
            c.That(State(game).Phase == TransformationPhase.Returning && State(game).Blocker == TransformerManager.BlockerReturnFull && State(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2),
                "blocked cancellation retains both raw WIP and reusable holds");
            var progress = State(game).Progress; TransformerTests.Twin(c, factory, game); Tick(game, 3);
            c.That(State(game).Progress.Equals(progress) && Inventory(game).ReservedOut(supply) == Fix.FromInt(2), "return wait never spends work or drops tool ownership");
            SimGame restored = factory.Load(game.Save()); Inventory(game).ReleaseIn(Other, hold); Inventory(restored).ReleaseIn(Other, hold); Tick(game); Tick(restored);
            c.Equal(game.Hash(), restored.Hash(), "saved disabled returning phase releases real tools only after material return");
            c.That(Inventory(game).Amount(supply) == Fix.FromInt(40) && Inventory(game).Reservations.Count == 0 && State(game).Tools.Held.IsEmpty && State(game).QueueCount == 0,
                "raw return and reusable release preserve all real material and close repeat");
            c.Equal(BigInteger.Zero, History(game).Totals(supply).CreatedRaw + History(game).Totals(supply).ConsumedRaw, "cancel fabricates no tool refund or conversion");
        }
        private static void Repeats(Check c, GameFactory factory)
        {
            SimGame game = New(factory); TransformerTests.Queue(game, "assembler", "build", repeat: true); Tick(game, 5);
            c.That(State(game).Tools.Held.IsEmpty && Inventory(game).Reservations.Count == 0 && State(game).QueueCount == 1, "repeat boundary releases old actual tool records");
            TransformerTests.Twin(c, factory, game); Tick(game);
            c.That(State(game).Wip.Execution == 2 && State(game).Tools.Held.AmountOf(Supply(game)) == Fix.FromInt(2), "next repeat acquires new execution-owned tools");
            Cancel(c, game); Tick(game, 4); c.That(Outputs(game) == 1 && State(game).QueueCount == 0 && Inventory(game).Reservations.Count == 0, "cancelled repeat cannot leak claims or extra results");
            SimGame shared = New(factory); Entity second = shared.World.Spawn(shared.World.Content.Get<EntityDef>("assembler"), Company); shared.World.Settle(); Tick(shared);
            TransformerTests.Queue(shared, "assembler", "build");
            c.That(TestKit.Order(shared, 0, new EnqueueTransformationCommand { Transformer = second.Id, Capability = Capability, Transformation = "build" }).Accepted, "independent machines acquire through ordinary command owner");
            c.That(!Tools(shared).WorkInProgressOwner.Equals(second.Get<ITransformerTools>(Capability).WorkInProgressOwner), "tools use actual stable machine/capability execution scopes");
            TransformerTests.Twin(c, factory, shared); Tick(shared, 5);
            c.That(Outputs(shared) == 2 && Inventory(shared).Reservations.Count == 0 && second.Get<IInventory>().Reservations.Count == 0, "shared manager completes both actual machine tool sets once");
        }
        private static void PhysicalSupply(Check c)
        {
            JsonValue json = Json.Parse(Colony.Defs); json.Get("transformations")[0].Get("tools").Set("steel", JsonValue.Number("2"));
            json.Get("transformations")[0].Set("cash", JsonValue.Number("7"));
            var text = new StringBuilder(); json.WriteCanonical(text); GameFactory factory = Colony.Factory(text.ToString());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 82 });
            Entity machine = game.World.Entities.Get(Colony.Mine(game, 0, "factory")[0].Id);
            var endpoints = new List<IResourceEndpointProvider>(game.World.Services.Providers<IResourceEndpointProvider>());
            c.That(endpoints.Count == 2 && endpoints.Contains(game.World.Services.Get<TransformerManager>()) && endpoints.Contains(game.World.Services.Providers<NetworkService>().Single()),
                "existing multi-provider discovery composes real transformer and network endpoint owners once each");
            c.That(!game.World.Services.Has<IResourceEndpointProvider>(), "there is no singular endpoint-interface binding or compatibility lookup");
            IInventory stock = machine.Get<IInventory>(); ResourceId steel = game.World.Content.ResourceId("steel");
            TestLedger ledger = (TestLedger)game.World.Services.Get<IEconomy>(); Fix startingCash = ledger.Cash(Company);
            Func<TransformationSnapshot> snapshot = () =>
            {
                if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(machine.Id, Capability, out var facts)) throw new InvalidOperationException("missing supplied process facts");
                return facts;
            };
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = machine.Id, Capability = Capability, Transformation = "build_truck" }).Accepted,
                "player requests actual automatically supplied priced batch");
            c.That(snapshot().Wip.Execution == 0 && stock.Amount(steel) == Fix.Zero && ledger.Cash(Company) == startingCash,
                "empty factory starts no WIP/tool/cash prefix before physical delivery");
            int waited = TestKit.RunUntil(game, () => snapshot().Wip.Execution > 0 || game.Fault != null, 600, 1);
            c.That(game.Fault == null && waited > 0 && waited < 600 && snapshot().Wip.Execution > 0, "existing engine logistics physically delivers complete input/tool bill within bound");
            if (game.Fault != null || snapshot().Wip.Execution == 0) throw new InvalidOperationException("physical tool supply stalled after " + waited + " ticks, phase=" + snapshot().Phase + ", blocker=" + snapshot().Blocker);
            c.That(stock.Amount(steel) == Fix.FromInt(2) && stock.ReservedOut(steel) == Fix.FromInt(2) && snapshot().Wip.Stock.AmountOf(steel) == Fix.FromInt(10),
                "actual delivered steel separates into raw WIP and protected reusable tools");
            c.Equal(startingCash - Fix.FromInt(7), ledger.Cash(Company), "complete physical readiness pays native startup cost exactly once");
            SimGame restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "physical supplied tool state saves through same logistics/source/WIP owners");
            int trucks = Colony.Mine(game, 0, "truck").Count;
            for (int i = 0; i < 180; i++) { Tick(game); Tick(restored); }
            c.Equal(game.Hash(), restored.Hash(), "priced physically supplied work continues identically in interleaved sessions");
            c.That(Colony.Mine(game, 0, "truck").Count == trucks + 1 && snapshot().QueueCount == 0 && stock.Amount(steel) == Fix.FromInt(2) && stock.Reservations.Count == 0,
                "timed supplied manufacturing keeps tools and creates one real vehicle");
            c.Equal(startingCash - Fix.FromInt(7), ledger.Cash(Company), "work and restore never repay startup charge");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = machine.Id, Capability = Capability, Transformation = "build_truck" }).Accepted,
                "second native batch reuses real available tools");
            waited = TestKit.RunUntil(game, () => snapshot().Wip.Execution > 0 || game.Fault != null, 600, 1);
            c.That(game.Fault == null && waited > 0 && waited < 600 && snapshot().Tools.Held.AmountOf(steel) == Fix.FromInt(2), "next supplied batch reacquires actual tools without recreating stock");
            Fix charged = ledger.Cash(Company);
            c.Equal(startingCash - Fix.FromInt(14), charged, "second distinct physical execution pays once");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = machine.Id, Capability = Capability, Index = 0 }).Accepted,
                "priced physical batch cancels through existing command owner");
            c.That(snapshot().QueueCount == 0 && snapshot().Tools.Held.IsEmpty && stock.Amount(steel) == Fix.FromInt(12) && stock.Reservations.Count == 0,
                "priced cancel returns actual raw inputs and releases reusable tools without extra vehicle");
            c.Equal(charged, ledger.Cash(Company), "native cancellation does not fabricate a cash refund");
        }
        private static void SeparateTools(Check c)
        {
            GameFactory factory = Factory(json =>
            {
                json.Get("resources").Add(Json.Parse("{\"id\":\"drill-tool\",\"unit\":\"unit.kit\",\"category\":\"tool\",\"phase\":\"solid\",\"traits\":[\"reusable\"],\"capacity\":{\"unit.kit\":1}}"));
                var components = json.Get("entities")[0].Get("components"); components.Remove("process_stock");
                components.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{\"supply\":20,\"drill-tool\":3}}"));
                json.Get("transformations")[0].Set("tools", Json.Parse("{\"drill-tool\":2}"));
            });
            SimGame game = New(factory); ResourceId tool = game.World.Content.ResourceId("drill-tool"); TransformerTests.Queue(game, "assembler", "build");
            c.That(State(game).Wip.Stock.Count == 1 && State(game).Tools.Held.AmountOf(tool) == Fix.FromInt(2) && Inventory(game).Amount(tool) == Fix.FromInt(3), "distinct authored tool stays in actual source inventory rather than raw WIP");
            TransformerTests.Twin(c, factory, game); Tick(game, 5);
            c.That(Outputs(game) == 1 && Inventory(game).Amount(tool) == Fix.FromInt(3) && Inventory(game).Available(tool) == Fix.FromInt(3), "separate reusable resource survives completion unchanged");
            c.Equal(BigInteger.Zero, History(game).Totals(tool).CreatedRaw + History(game).Totals(tool).ConsumedRaw + History(game).Totals(tool).MovedRaw, "holding a tool emits no fake physical conversion or transfer");
            TransformerTests.Twin(c, factory, game);
            GameFactory heldOnly = Factory(json => json.Get("transformations")[0].Get("inputs").Remove("supply")); SimGame empty = New(heldOnly);
            TransformerTests.Queue(empty, "assembler", "build"); c.That(State(empty).Wip.Stock.IsEmpty && State(empty).Tools.Held.AmountOf(Supply(empty)) == Fix.FromInt(2), "tool-only timed process requires actual tools without fabricated raw inputs");
            TransformerTests.Twin(c, heldOnly, empty); Cancel(c, empty);
            c.That(Inventory(empty).Amount(Supply(empty)) == Fix.FromInt(20) && Inventory(empty).Reservations.Count == 0 && Outputs(empty) == 0, "tool-only cancel releases actual hold without refund stock");
        }
    }
}
