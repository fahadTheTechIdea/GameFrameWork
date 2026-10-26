using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
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
    public static class ResourceLifecycleTests
    {
        public static void Register()
        {
            TestRunner.Add("Resource lifecycle: exact batches, refusals and saved native accounting", Batches);
            TestRunner.Add("Resource lifecycle: authored timed transformation, work and provider violations", Physical);
        }
        private static NewGameRequest Request(string scenario)
        { var request = new NewGameRequest { Scenario = scenario, Seed = 61 }; request.Mods.Add(ResourceFlowsModule.ModuleId); return request; }
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing " + id);
        }
        private static ResourceFlowPage Read(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("lifecycle tick " + game.Tick + ": " + game.Fault); }
        private static ResourceInventoryIntent Intent(Entity entity, Bill bill, ResourceFlowKind kind)
            => new ResourceInventoryIntent(new ReservationOwner("proof.lifecycle", entity.Id.Value), entity.Owner, entity.Id, bill, kind, new Code("proof.authored-effect"));
        private static Bill Quantities(ResourceId a, int amountA, ResourceId b, int amountB)
            => new Bill(new[] { new ResourceAmount(a, Fix.FromInt(amountA)), new ResourceAmount(b, Fix.FromInt(amountB)) });
        private static void Batches(Check c)
        {
            GameFactory factory = PointGame.Factory(FrameworkProofContent.Read("lifecycle-inventory.json"));
            SimGame game = factory.NewGame(Request("points")); Entity bin = Find(game, "bin"); World world = game.World;
            ResourceId a = world.Content.ResourceId("alpha"), b = world.Content.ResourceId("beta"); IInventory stock = bin.Get<IInventory>();
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            var initial = Intent(bin, Quantities(a, 10, b, 20), ResourceFlowKind.Initialized); ulong before = game.Hash();
            c.That(transactions.EvaluateInventory(initial).Ready, "whole mixed-native-unit initialization preflights");
            c.Equal(before, game.Hash(), "batch preview owns no balances or promises");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.CommitInventory(default).Status, "missing explicit intent refuses");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.CommitInventory(new ResourceInventoryIntent(initial.Owner,
                new PlayerId(1), bin.Id, initial.Quantities, initial.Kind, initial.Cause)).Status, "foreign company cannot mutate the endpoint");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.CommitInventory(new ResourceInventoryIntent(initial.Owner,
                bin.Owner, bin.Id, initial.Quantities, ResourceFlowKind.StockTransfer, initial.Cause)).Status, "lifecycle request cannot impersonate a transfer");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.CommitInventory(new ResourceInventoryIntent(initial.Owner,
                bin.Owner, bin.Id, initial.Quantities, initial.Kind, Code.None)).Status, "anonymous effect cause is not fabricated");
            c.Equal(before, game.Hash(), "normal invalid requests change nothing");
            c.Equal(2, transactions.CommitInventory(initial).ChangedResources, "batch reports resource count, never a mixed-unit scalar amount");
            c.Equal(Fix.FromInt(10), stock.Amount(a), "actual alpha stock initialized"); c.Equal(Fix.FromInt(20), stock.Amount(b), "actual beta stock initialized");
            c.Throws<InvalidOperationException>(() => game.Save(), "pending lifecycle facts prevent a lossy checkpoint"); Tick(game);
            ResourceFlowPage page = Read(game);
            c.Equal(2, page.Entries.Count, "one journal records both verified lifecycle lines");
            c.Equal(ResourceFlowKind.Initialized, page.Entries[0].Item.Kind, "initialization is distinct from transformation and transfers");
            c.That(!page.Entries[0].Item.IsTransfer && !page.Entries[0].Item.Source.IsValid && !page.Entries[0].Item.Destination.IsValid,
                "lifecycle fact has no fabricated transfer receipt");
            c.Equal(ResourceEndpointId.ForEntity(bin.Id), page.Entries[0].Item.Endpoint, "fact identifies the sole actual stock owner");
            c.Equal(initial.Cause, page.Entries[0].Item.Cause, "explicit cause survives projection");
            c.Equal(new UnitId("unit.alpha"), page.Entries[0].Item.Unit, "alpha native identity is preserved");
            c.Equal(new UnitId("unit.beta"), page.Entries[1].Item.Unit, "beta mass identity is not summed into count");
            c.Equal(new BigInteger(Fix.FromInt(10).Raw), page.Totals(a).InitializedRaw, "initialization totals derive per resource from facts");
            c.Equal(0, page.Totals(a).TransferLegs, "lifecycle accounting is not mislabeled as movement");
            before = game.Hash();
            c.Equal(ResourceTransferStatus.NoCapacity, transactions.CommitInventory(Intent(bin, Quantities(a, 10, b, 1), ResourceFlowKind.Produced)).Status,
                "whole output capacity refuses before inserting a compatible prefix");
            c.Equal(ResourceTransferStatus.NoStock, transactions.CommitInventory(Intent(bin, Quantities(a, 5, b, 21), ResourceFlowKind.Consumed)).Status,
                "missing second input cannot consume the first input");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.CommitInventory(Intent(bin,
                new Bill(new[] { new ResourceAmount(a, Fix.Half) }), ResourceFlowKind.Consumed)).Status, "fractional native count refuses");
            c.Equal(ResourceTransferStatus.Incompatible, transactions.CommitInventory(Intent(bin,
                new Bill(new[] { new ResourceAmount(b, Fix.Half) }), ResourceFlowKind.Consumed)).Status, "unsupported measured quantum refuses");
            c.Equal(before, game.Hash(), "all quantity/capacity/availability refusals are mutation free");
            ReservationOwner promiseOwner = new ReservationOwner("proof.reserved", 1);
            ReservationToken pickup = stock.ReserveOut(promiseOwner, a, Fix.FromInt(8)); before = game.Hash();
            c.Equal(ResourceTransferStatus.NoStock, transactions.CommitInventory(Intent(bin, Quantities(a, 5, b, 7), ResourceFlowKind.Consumed)).Status,
                "lifecycle consumption respects another job's outbound promises");
            c.Equal(before, game.Hash(), "promise refusal changes no stock or token sequence"); stock.ReleaseOut(promiseOwner, pickup);
            ReservationToken incoming = stock.ReserveIn(promiseOwner, a, Fix.FromInt(10)); before = game.Hash();
            c.Equal(ResourceTransferStatus.NoCapacity, transactions.CommitInventory(Intent(bin,
                new Bill(new[] { new ResourceAmount(b, Fix.One) }), ResourceFlowKind.Produced)).Status, "output preserves another owner's inbound capacity");
            c.Equal(before, game.Hash(), "inbound capacity refusal changes nothing"); stock.ReleaseIn(promiseOwner, incoming);
            var consume = Intent(bin, Quantities(a, 5, b, 7), ResourceFlowKind.Consumed);
            SimGame twin = factory.Load(game.Save());
            c.That(transactions.CommitInventory(consume).Committed, "complete input bill commits");
            twin.World.Services.Get<IResourceTransaction>().CommitInventory(consume); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved whole-bill stock and history continue identically");
            c.Equal(Fix.FromInt(5), stock.Amount(a), "alpha exact consumed delta"); c.Equal(Fix.FromInt(13), stock.Amount(b), "beta exact consumed delta");
            page = Read(game);
            c.Equal(ResourceFlowReadStatus.Expired, page.Status, "retained history cannot claim complete session accounting");
            c.Equal(ResourceFlowKind.Consumed, page.Entries[0].Item.Kind, "consumption shares the existing event/journal type");
            c.Equal(new BigInteger(Fix.FromInt(7).Raw), page.Totals(b).ConsumedRaw, "consumption totals remain native-resource scoped");
            c.Equal(BigInteger.Zero, page.Totals(b).CreatedRaw, "input removal is not creation");
            var lost = Intent(bin, new Bill(new[] { new ResourceAmount(a, Fix.One) }), ResourceFlowKind.Lost);
            var destroyed = Intent(bin, new Bill(new[] { new ResourceAmount(b, Fix.FromInt(2)) }), ResourceFlowKind.Destroyed);
            c.That(transactions.CommitInventory(lost).Committed && transactions.CommitInventory(destroyed).Committed, "explicit loss and destruction use the same exact stock protocol"); Tick(game);
            page = Read(game);
            c.Equal(new BigInteger(Fix.One.Raw), page.Totals(a).LostRaw, "loss is reported distinctly");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), page.Totals(b).DestroyedRaw, "destruction is reported distinctly");
            var output = Intent(bin, Quantities(a, 3, b, 2), ResourceFlowKind.Produced);
            c.That(transactions.CommitInventory(output).Committed, "explicit transformation creation uses the same batch owner"); Tick(game);
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), Read(game).Totals(a).CreatedRaw, "produced amounts are derived from verified lifecycle facts");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "generalized lifecycle codec/hash survives all five effect kinds");
            c.Equal(16, SaveGame.Describe(game.Save()).Format, "breaking lifecycle payload has no old reader");
            Codec(c, factory, game);
            var empty = Intent(bin, Bill.Empty, ResourceFlowKind.Consumed); before = game.Hash();
            c.That(transactions.CommitInventory(empty).Committed, "explicit empty bill succeeds without stock or event mutation");
            c.Equal(before, game.Hash(), "empty batch creates no fake flow");
            Entity destination = world.Spawn(bin.Def, bin.Owner); world.Settle();
            var transferOwner = new ReservationOwner("proof.lifecycle-transfer", 1);
            ReservationToken outgoing = stock.ReserveOut(transferOwner, a, Fix.One);
            ReservationToken admitted = destination.Get<IInventory>().ReserveIn(transferOwner, a, Fix.One);
            c.That(transactions.Commit(new ResourceTransferIntent(transferOwner, bin.Owner, outgoing, admitted, a, Fix.One)).Committed,
                "existing owned transfer protocol interleaves with lifecycle effects"); Tick(game);
            page = Read(game); ResourceFlowFact transferred = page.Entries[page.Entries.Count - 1].Item;
            c.Equal(ResourceFlowKind.StockTransfer, transferred.Kind, "generalized event retains exact transfer classification");
            c.That(transferred.IsTransfer && transferred.Source.Equals(outgoing) && transferred.Destination.Equals(admitted) &&
                !transferred.Endpoint.IsValid && transferred.Cause.IsNone, "transfer payload has actual receipts, not invented lifecycle metadata");
            c.Equal(Fix.FromInt(7), stock.Amount(a) + destination.Get<IInventory>().Amount(a), "interleaved movement conserves actual stock");
            c.Equal(1, page.Totals(a).TransferLegs, "retained creation and transfer metrics stay separate");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "mixed lifecycle/transfer codec restores through the same journal");
        }
        private static void Codec(Check c, GameFactory factory, SimGame game)
        {
            byte[] saved;
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); ((IPersistentService)game.World.Services.Get<IResourceFlowJournal>()).Sync(writer); writer.Flush(); saved = stream.ToArray(); }
            // Restore a journal payload against a fully restored current world, not an invented tick.
            Action<byte[]> restore = bytes =>
            { SimGame target = factory.Load(game.Save()); ((IPersistentService)target.World.Services.Get<IResourceFlowJournal>()).Sync(new StateReader(bytes, 0, bytes.Length)); };
            byte[] corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(-1L), 0, corrupt, 32, 8);
            c.Throws<CorruptStateException>(() => restore(corrupt), "lifecycle codec rejects negative fact ticks");
            byte[] cause = System.Text.Encoding.UTF8.GetBytes("proof.authored-effect"); int offset = -1;
            for (int i = 0; i <= saved.Length - cause.Length; i++)
            { bool matches = true; for (int j = 0; j < cause.Length; j++) if (saved[i + j] != cause[j]) { matches = false; break; } if (matches) { offset = i; break; } }
            if (offset < 4) throw new InvalidOperationException("explicit cause not serialized");
            corrupt = new byte[saved.Length - cause.Length];
            Buffer.BlockCopy(saved, 0, corrupt, 0, offset);
            Buffer.BlockCopy(BitConverter.GetBytes(-1), 0, corrupt, offset - 4, 4);
            Buffer.BlockCopy(saved, offset + cause.Length, corrupt, offset, saved.Length - offset - cause.Length);
            c.Throws<CorruptStateException>(() => restore(corrupt), "lifecycle restore refuses absent cause instead of fabricating one");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(260), 0, corrupt, offset - 12, 4);
            c.Throws<CorruptStateException>(() => restore(corrupt), "lifecycle kind cannot wrap a corrupt integer into a valid byte enum");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(-1), 0, corrupt, offset - 8, 4);
            c.Throws<CorruptStateException>(() => restore(corrupt), "saved lifecycle endpoint must be a real positive entity identity");
        }
        private static GameFactory Workflow(bool process, string fault = null)
        {
            var modules = new List<GameModule> { new InventoryModule(), new ResourceFlowsModule(), new ContentAccessModule(), new WorldSetupModule() };
            if (process) { modules.Add(new OperationsModule()); modules.Add(new TransformationModule()); modules.Add(new ProcessProofModule()); }
            else { modules.Add(new MotionModule()); modules.Add(new WorkModule()); modules.Add(new LocalWorkProofModule()); }
            JsonValue authored = Json.Parse(FrameworkProofContent.Read(process ? "process.json" : "local-work.json"));
            if (fault != null) authored.Get("entities")[0].Get("components").Get(process ? "process_stock" : "local_stock")
                .Set("receipt_fault", JsonValue.Of(fault));
            JsonValue flows = Json.Parse(FrameworkProofContent.Read("resource-flows.json"));
            // This proof inspects all lifecycle legs, not a two-record presentation feed.
            flows.Get("resource_flow_journals")[0].Set("retained_records_per_company", JsonValue.Number("32"));
            var definition = new GameDefinition("lifecycle-workflow", modules);
            var source = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read(process ? "lifecycle-process-manifest.json" : "lifecycle-work-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("flows.json", flows.ToString())
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add(process ? "process.json" : "local-work.json", authored.ToString());
            return new GameFactory(definition, ContentSet.Load(source, definition.ContentTypes));
        }
        // These workflows begin with one verified opening fact; read subsequent effects by cursor.
        private static ResourceFlowPage ReadEffects(SimGame game)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 1);

        private static void Physical(Check c)
        {
            GameFactory factory = Workflow(true); SimGame game = factory.NewGame(Request("process")); Entity assembler = Find(game, "assembler");
            ResourceId supply = game.World.Content.ResourceId("supply");
            c.That(game.World.Space == null && !game.World.Services.Has<IEconomy>(), "unpriced authored process requires no map or invented economy");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler.Id, Transformation = "build", Count = 1 }).Accepted,
                "player command enters the existing authored transformation queue");
            c.Equal(Fix.FromInt(17), assembler.Get<IInventory>().Amount(supply), "physical input batch leaves local storage for original WIP at start");
            ResourceFlowFact input = ReadEffects(game).Entries[0].Item;
            c.Equal(ResourceFlowKind.StockTransfer, input.Kind, "started process records actual local-to-WIP transfer, not spent material");
            c.Equal(ResourceEndpointId.ForEntity(assembler.Id), input.Source.Endpoint, "input transfer leaves the actual local store");
            c.That(input.Destination.Endpoint != input.Source.Endpoint, "WIP is the original distinct physical endpoint");
            SimGame twin = factory.Load(game.Save()); int waited = 0;
            while (!game.World.Entities.All.Any(e => e.Alive && e.Def.Id == "token") && waited++ < 30) { Tick(game); Tick(twin); }
            c.That(waited < 30, "timed transformation reaches actual output within bounded game time");
            ResourceFlowFact output = ReadEffects(game).Entries.Select(e => e.Item).Single(e => e.Kind == ResourceFlowKind.Produced);
            c.Equal(ResourceFlowKind.Produced, output.Kind, "completion records actual output creation");
            c.Equal(new Code("build"), output.Cause, "JSON transformation is the actual conversion cause");
            c.That(output.Tick > input.Tick, "output never appears at input start; authored duration elapses");
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(supply), "actual input and output quantities are distinct");
            c.Equal(game.Hash(), twin.Hash(), "saved work-in-progress and lifecycle journal continue identically");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), ReadEffects(game).Totals(supply).ConsumedRaw, "transformation input accounting is native and exact");
            c.Equal(new BigInteger(Fix.One.Raw), ReadEffects(game).Totals(supply).CreatedRaw, "transformation output accounting is native and exact");
            c.That(Find(game, "token") != null, "entity output still uses the existing transformation completion path");
            Local(c); Provider(c, true, "partial_take"); Provider(c, true, "partial_receive"); Provider(c, false, "partial_take");
        }
        private static void Local(Check c)
        {
            GameFactory factory = Workflow(false); SimGame game = factory.NewGame(Request("local-work")); Entity target = Find(game, "local");
            ResourceId kit = game.World.Content.ResourceId("kit"); IInventory stock = target.Get<IInventory>();
            ReservationOwner owner = new ReservationOwner("proof.competing", 1); ReservationToken held = stock.ReserveOut(owner, kit, Fix.FromInt(9));
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = target.Id }).Accepted, "player posts the existing JSON work plan");
            c.Equal(Fix.FromInt(10), stock.Amount(kit), "work cannot consume another owner's promised materials");
            c.Equal(0, ReadEffects(game).Entries.Count, "blocked work reports no successful consumption");
            WorkBoard board = game.World.Services.Get<WorkBoard>(); var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            Order order = board.Order(orders[0].Id); c.Equal(NodeState.Waiting, order.Nodes[0].State, "refused materials keep the stage supplying, not working");
            stock.ReleaseOut(owner, held); SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            int waits = 0; while (order.IsActive && waits++ < 30) { Tick(game); Tick(twin); }
            c.That(waits < 30 && order.State == OrderState.Done, "local work consumes actual game time to completion");
            c.Equal(Fix.FromInt(8), stock.Amount(kit), "earned work consumes the entire actual bill once");
            var consumed = ReadEffects(game).Entries.Select(e => e.Item).Where(e => e.Kind == ResourceFlowKind.Consumed).ToList();
            c.That(consumed.Count > 0 && consumed.All(e => e.Cause.Id.Contains("settle") && e.Cause.Id.Contains("settling")), "authored requirement identifies actual consumption");
            c.Equal(game.Hash(), twin.Hash(), "saved blocked-to-working completion matches stock and facts");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), ReadEffects(game).Totals(kit).ConsumedRaw, "working/completed updates never consume more than the authored bill");
        }
        private static void Provider(Check c, bool process, string fault)
        {
            GameFactory factory = Workflow(process, fault); SimGame game = factory.NewGame(Request(process ? "process" : "local-work"));
            Entity entity = Find(game, process ? "assembler" : "local");
            Command command = process ? (Command)new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = entity.Id, Transformation = "build", Count = 1 }
                : new PostOrderCommand { Plan = "settle", Target = entity.Id };
            game.Seat(0).Submit(command);
            ResourceFlowPage lastHealthy = ReadEffects(game);
            for (int i = 0; i < 30 && game.Fault == null; i++) { lastHealthy = ReadEffects(game); game.Update(); }
            string expected = !process ? "violated exact owned consumption" : fault == "partial_receive"
                ? "conversion output violated exact production" : "violated exact physical transfer receipt";
            c.That(game.Fault?.Message.Contains(expected) == true, "invalid actual receipt callback faults its physical batch: " + process + "/" + fault + ": " + game.Fault);
            c.Equal(process ? "sim.transformations" : "sim.work", game.Fault?.Phase, "fault identifies the actual execution owner");
            c.That(!lastHealthy.Entries.Any(e => process && fault == "partial_receive"
                ? e.Item.Kind == ResourceFlowKind.StockTransfer && e.Item.Destination.Endpoint == ResourceEndpointId.ForEntity(entity.Id)
                : e.Item.Kind == (process ? ResourceFlowKind.StockTransfer : ResourceFlowKind.Consumed)),
                "failed receipt emits no success; preceding verified lifecycle facts remain history");
            c.Throws<InvalidOperationException>(() => ReadEffects(game), "faulted live history refuses instead of publishing a partial result");
            c.That(!game.World.Events.HasPending, "unverified batch publishes no deferred success event");
            c.Throws<InvalidOperationException>(() => game.Save(), "partial provider execution cannot become a resumable checkpoint");
        }
    }
}
