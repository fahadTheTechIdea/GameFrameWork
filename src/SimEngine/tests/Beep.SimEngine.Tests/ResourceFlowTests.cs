using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceFlowTests
    {
        public static void Register()
        {
            TestRunner.Add("Resource flows: authored retention, private history and durable checkpoints", Journal);
            TestRunner.Add("Resource flows: physical legs, supply origins and rejected provider facts", Physical);
        }
        private static NewGameRequest Request(string scenario, bool flows = true)
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 53 };
            if (flows) request.Mods.Add(ResourceFlowsModule.ModuleId);
            return request;
        }
        private static GameFactory Points() => PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json"));
        private static Entity Find(SimGame game, string definition)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Def.Id == definition) return entity;
            throw new InvalidOperationException("missing " + definition);
        }
        // Transfer-specific assertions exclude opening facts, preserving actual engine sequence IDs.
        private static ResourceFlowPage Read(SimGame game, long cursor = 0, int company = 0)
        {
            ResourceFlowPage page = game.Seat(company).View.Feature<ResourceFlowView>().Read(new PlayerId(company), cursor);
            var transfers = new List<FeedEntry<ResourceFlowFact>>();
            foreach (var entry in page.Entries) if (entry.Item.IsTransfer) transfers.Add(entry);
            return new ResourceFlowPage(page.Status, page.Oldest, page.Latest, transfers);
        }
        private static ResourceTransferIntent Intent(SimGame game, Entity source, Entity destination,
            ResourceId resource, Fix amount, long id, bool purchase = false)
        {
            var owner = new ReservationOwner("proof.flow", id);
            ReservationToken outgoing = purchase ? source.Get<ISupplier>().ReserveSupply(owner, resource, amount)
                : source.Get<IOutletPort>().ReserveOut(owner, resource, amount);
            ReservationToken incoming = destination.Get<IInletPort>().ReserveIn(owner, resource, amount);
            return new ResourceTransferIntent(owner, destination.Owner, outgoing, incoming, resource, amount,
                purchase ? ResourceTransferKind.Supply : ResourceTransferKind.Stock);
        }
        private static void Tick(SimGame game)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("flow checkpoint fault at " + game.Tick + ": " + game.Fault);
        }
        private static void Journal(Check c)
        {
            GameFactory factory = Points(); SimGame game = factory.NewGame(Request("points"));
            Entity source = Find(game, "source"), destination = Find(game, "destination");
            ResourceId resource = game.World.Content.ResourceId("supply");
            c.Equal(2, game.World.Content.All<ResourceFlowJournalDef>()[0].RetainedRecordsPerCompany, "retention is master-listed authored content");
            c.Equal(0, Read(game).Entries.Count, "initial stocks do not masquerade as verified transfers");
            ulong before = game.Hash();
            c.Equal(ResourceFlowReadStatus.CursorAhead, Read(game, -1).Status, "negative cursor refuses even for an empty partition");
            c.Equal(ResourceFlowReadStatus.CursorAhead, Read(game, 2).Status, "future cursor is explicit after opening history");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Status, "foreign company is refused before lookup");
            c.Equal(ResourceFlowReadStatus.NoSuchCompany, game.Observer.Feature<ResourceFlowView>().Read(new PlayerId(99), 0).Status, "observer cannot invent company state");
            c.Equal(before, game.Hash(), "all empty and refused reads are pure");
            ResourceTransferIntent intent = Intent(game, source, destination, resource, Fix.FromInt(30), 1);
            IResourceTransaction transfer = game.World.Services.Get<IResourceTransaction>();
            before = game.Hash();
            c.That(transfer.Evaluate(intent).Ready, "same existing transfer preview remains authoritative");
            c.Equal(before, game.Hash(), "preview emits no historical fact");
            c.That(transfer.Commit(intent).Committed, "exact verified commit succeeds");
            c.That(game.World.Events.HasPending, "success queues one fact in the existing event phase");
            c.Equal(0, Read(game).Entries.Count, "pending event is not a second synchronous journal path");
            c.Throws<InvalidOperationException>(() => game.Save(), "save cannot lose a pending successful-transfer event");
            c.Throws<InvalidOperationException>(() => game.Hash(), "hash cannot omit pending persistent event effects");
            Tick(game);
            c.That(!game.World.Events.HasPending, "normal Update drains deferred facts");
            ResourceFlowPage first = Read(game); ResourceFlowFact fact = first.Entries[0].Item;
            c.Equal(2L, first.Latest, "physical sequence follows company opening history");
            c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "stock movement is not creation or purchase");
            c.Equal(new UnitId("unit.supply"), fact.Unit, "fact uses the catalog's native unit identity");
            c.Equal(intent.Source, fact.Source, "consumed source receipt remains historical identity");
            c.Equal(intent.Destination, fact.Destination, "consumed destination receipt remains historical identity");
            c.Equal(Fix.FromInt(60), fact.SourceBefore.Value, "fact captures actual source stock before mutation");
            c.Equal(Fix.FromInt(30), fact.SourceAfter.Value, "fact captures exact actual source delta");
            c.Equal(Fix.Zero, fact.DestinationBefore, "fact captures destination checkpoint");
            c.Equal(Fix.FromInt(30), fact.DestinationAfter, "fact captures exact admitted stock");
            c.That(fact.SourceVersionAfter > fact.SourceVersionBefore && fact.DestinationVersionAfter > fact.DestinationVersionBefore,
                "facts include actual verified endpoint version changes");
            c.Throws<NotSupportedException>(() => ((IList<FeedEntry<ResourceFlowFact>>)first.Entries)[0] = default, "view entries are immutable");
            before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidReceipt, transfer.Commit(intent).Status, "repeating consumed receipt refuses");
            c.Equal(before, game.Hash(), "refusal appends nothing and changes no checkpoint");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "journal is saved and hashed through its sole service codec");
            c.Equal(2L, Read(twin).Entries[0].Sequence, "saved history sequence restores exactly");
            CorruptJournal(c, factory, game);
            for (int i = 2; i <= 3; i++)
            {
                Entity from = i == 2 ? source : destination, to = i == 2 ? destination : source;
                ResourceTransferIntent next = Intent(game, from, to, resource, Fix.FromInt(30), i);
                c.That(transfer.Commit(next).Committed, "next physical stock leg commits");
                ResourceTransferIntent twinIntent = Intent(twin, Find(twin, from.Def.Id), Find(twin, to.Def.Id), resource, Fix.FromInt(30), i);
                twin.World.Services.Get<IResourceTransaction>().Commit(twinIntent); Tick(game); Tick(twin);
                c.Equal(game.Hash(), twin.Hash(), "saved ring wrap and stocks continue identically");
            }
            ResourceFlowPage retained = Read(game);
            c.Equal(ResourceFlowReadStatus.Expired, retained.Status, "reader is told explicitly that history expired");
            c.Equal(3L, retained.Oldest, "JSON retention bounds history, not the world");
            c.Equal(4L, retained.Latest, "sequence survives retention wrap");
            c.Equal(2, retained.Entries.Count, "only authored retained records remain");
            c.Equal(3L, retained.Entries[0].Sequence, "retained history is chronological");
            c.Equal(ResourceFlowReadStatus.Ready, Read(game, 2).Status, "cursor immediately before oldest has no gap");
            c.Equal(0, Read(game, 4).Entries.Count, "reader cursor consumes no other reader's items");
            c.Equal(1, first.Entries.Count, "previous detached page never changes as journal wraps");
            ResourceFlowTotals totals = retained.Totals(resource);
            c.Equal(2, totals.TransferLegs, "totals count retained physical legs, not logical jobs");
            c.Equal(new BigInteger(Fix.FromInt(60).Raw), totals.MovedRaw, "native raw historical totals use arbitrary precision");
            c.Equal(BigInteger.Zero, totals.CreatedRaw + totals.StockPurchasedRaw, "ordinary movement never invents resource origins");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "wrapped feed restores into canonical layout");
            SimGame optional = factory.NewGame(Request("points", false));
            c.That(optional.Seat(0).View.Feature<ResourceFlowView>() == null, "diagnostics module is optional, not compulsory gameplay");
            ResourceTransferIntent optionalIntent = Intent(optional, Find(optional, "source"), Find(optional, "destination"), resource, Fix.FromInt(30), 1);
            optional.World.Services.Get<IResourceTransaction>().Commit(optionalIntent);
            c.That(!optional.World.Events.HasPending, "unsubscribed event does not create a hidden journal");
            c.Equal(optional.Hash(), factory.Load(optional.Save()).Hash(), "optional omission has no pending journal or extra payload");
            Ring(c); PrivateCompanies(c); InvalidFact(c);
        }
        private static byte[] WriteRing(FeedRing<int> ring)
        {
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); ring.Sync(writer, (IStateSync s, ref int value) => s.Int(ref value)); writer.Flush(); return stream.ToArray(); }
        }
        private static void CorruptJournal(Check c, GameFactory factory, SimGame game)
        {
            byte[] saved;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream);
                ((IPersistentService)game.World.Services.Get<IResourceFlowJournal>()).Sync(writer);
                writer.Flush(); saved = stream.ToArray();
            }
            Action<byte[]> restore = bytes =>
            {
                SimGame target = factory.NewGame(Request("points"));
                ((IPersistentService)target.World.Services.Get<IResourceFlowJournal>()).Sync(new StateReader(bytes, 0, bytes.Length));
            };
            byte[] corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(-1L), 0, corrupt, 32, 8);
            c.Throws<CorruptStateException>(() => restore(corrupt), "journal codec rejects negative saved fact tick");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(99L), 0, corrupt, 32, 8);
            c.Throws<CorruptStateException>(() => restore(corrupt), "journal codec rejects a fact from future simulation time");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(1), 0, corrupt, 4, 4);
            c.Throws<CorruptStateException>(() => restore(corrupt), "journal codec refuses a company outside the authored seats");
            byte[] unit = System.Text.Encoding.UTF8.GetBytes("unit.supply"); int unitOffset = -1;
            for (int i = 0; i <= saved.Length - unit.Length; i++)
            {
                bool match = true; for (int j = 0; j < unit.Length; j++) if (saved[i + j] != unit[j]) { match = false; break; }
                if (match) { unitOffset = i; break; }
            }
            if (unitOffset < 0) throw new InvalidOperationException("journal native unit not serialized");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("unit.otherx"), 0, corrupt, unitOffset, unit.Length);
            c.Throws<CorruptStateException>(() => restore(corrupt), "journal codec rejects a substituted native unit identity");
        }
        private static void LoadRing(FeedRing<int> ring, byte[] bytes)
        {
            ring.Sync(new StateReader(bytes, 0, bytes.Length), (IStateSync s, ref int value) => s.Int(ref value));
        }
        private static void Ring(Check c)
        {
            c.Throws<ArgumentOutOfRangeException>(() => new FeedRing<int>(0), "zero-capacity feed refuses explicitly");
            var ring = new FeedRing<int>(2); ring.Append(1); ring.Append(2); ring.Append(3);
            byte[] saved = WriteRing(ring); var restored = new FeedRing<int>(2); LoadRing(restored, saved);
            long cursor = 0; var values = new List<int>(); restored.CopySince(ref cursor, values);
            c.That(values.Count == 2 && values[0] == 2 && values[1] == 3 && cursor == 3, "common feed copies and persists wrapped chronological entries");
            c.Throws<CorruptStateException>(() => LoadRing(new FeedRing<int>(3), saved), "authored feed capacity mismatch refuses");
            byte[] corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(99L), 0, corrupt, 16, 8);
            c.Throws<CorruptStateException>(() => LoadRing(new FeedRing<int>(2), corrupt), "noncontiguous saved sequence refuses");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(-1), 0, corrupt, 12, 4);
            c.Throws<CorruptStateException>(() => LoadRing(new FeedRing<int>(2), corrupt), "negative saved count refuses");
            corrupt = (byte[])saved.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(long.MaxValue), 0, corrupt, 4, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(long.MaxValue - 2), 0, corrupt, 16, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(long.MaxValue - 1), 0, corrupt, 28, 8);
            LoadRing(restored, corrupt); long latest = restored.Latest;
            c.Throws<OverflowException>(() => restored.Append(4), "sequence exhaustion refuses before mutating history");
            c.Equal(latest, restored.Latest, "failed append leaves feed checkpoint unchanged");
            c.Throws<ContentException>(() => new ResourceFlowJournalDef().Read(new JsonReader(Json.Parse("{\"retained_records_per_company\":0}"), "invalid flows")),
                "authored retention must be positive with no fabricated fallback");
        }
        private static void PrivateCompanies(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(Request("colony"));
            World world = game.World; ResourceId steel = world.Content.ResourceId("steel"); TileMap map = (TileMap)world.Space;
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(1), new TileCoord(30, 18)); world.Settle();
            destination.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            ResourceTransferIntent intent = Intent(game, Find(game, "market"), destination, steel, Fix.FromInt(10), 1, true);
            world.Services.Get<IResourceTransaction>().Commit(intent); Tick(game);
            c.Equal(3L, Read(game, 0, 1).Latest, "company one owns opening, runtime creation and purchase sequence");
            c.Equal(1L, Read(game).Latest, "rival activity does not alter company zero's opening watermark");
            ResourceFlowPage hidden = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0);
            c.Equal(ResourceFlowReadStatus.NotOwned, hidden.Status, "rival records remain private after activity");
            c.That(hidden.Latest == 0 && hidden.Oldest == 0 && hidden.Entries.Count == 0, "private refusal leaks no count or sequence");
            c.Equal(3L, game.Observer.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Latest, "observer can explicitly inspect each company");
            ulong before = game.Hash(); Read(game, 0, 1); Read(game); c.Equal(before, game.Hash(), "private history queries never create or modify partitions");
            c.Equal(before, factory.Load(game.Save()).Hash(), "independent company partitions survive save/load");
        }
        private static void InvalidFact(Check c)
        {
            SimGame game = Points().NewGame(Request("points")); game.World.Events.Publish(default(ResourceFlowFact)); game.Update();
            c.That(game.Fault?.Message.Contains("invalid committed resource-flow fact") == true, "malformed registered event payload faults at the event phase");
            c.Equal(0, Read(game).Entries.Count, "invalid payload creates no historical partition");
            c.Throws<InvalidOperationException>(() => game.Save(), "invalid-event fault is not resumable");
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, int limit, string stage)
        {
            for (int i = 0; i < limit && !done() && game.Fault == null && twin?.Fault == null; i++)
            { game.Update(); twin?.Update(); }
            if (!done() || game.Fault != null || twin?.Fault != null)
                throw new InvalidOperationException(stage + " stalled at " + game.Tick + "; fault=" + game.Fault + "; twin=" + twin?.Fault);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Points(); SimGame game = factory.NewGame(Request("points"));
            World world = game.World; Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId resource = world.Content.ResourceId("supply");
            int id = world.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(30), 100);
            Job job = world.Services.Get<WorkBoard>().Job(id);
            c.That(job != null, "physical job uses the existing haul acquisition owner");
            c.Equal(0, Read(game).Entries.Count, "job assignment/reservations create no stock-transfer history");
            SimGame twin = factory.Load(game.Save());
            Until(game, twin, () => job.Loaded.IsPositive, 30, "journaled pickup");
            ResourceFlowPage pickup = Read(game);
            c.Equal(1, pickup.Entries.Count, "physical pickup emits exactly one verified source-to-cargo leg");
            c.Equal(ResourceEndpointId.ForEntity(machine.Id), pickup.Entries[0].Item.Destination.Endpoint, "pickup destination is the actual transporter, not final delivery");
            c.Equal(Fix.FromInt(30), machine.Get<ITransporter>().Cargo.Amount(resource), "journal accompanies real cargo, not teleportation");
            c.Equal(game.Hash(), twin.Hash(), "pickup journal and physical state continue identically");
            twin = factory.Load(game.Save());
            Until(game, twin, () => !job.IsLive, 200, "journaled delivery");
            ResourceFlowPage delivery = Read(game);
            c.Equal(JobState.Done, job.State, "physical delivery completes through normal engine update");
            c.Equal(2, delivery.Entries.Count, "pickup and delivery are two distinct physical legs");
            c.Equal(ResourceEndpointId.ForEntity(machine.Id), delivery.Entries[1].Item.Source.Endpoint, "delivery source is the actual cargo owner");
            c.Equal(ResourceEndpointId.ForEntity(destination.Id), delivery.Entries[1].Item.Destination.Endpoint, "delivery targets original owned destination");
            c.That(delivery.Entries[1].Item.Tick > delivery.Entries[0].Item.Tick, "travel consumes real game time between recorded legs");
            c.Equal(Fix.FromInt(30), destination.Get<IInventory>().Amount(resource), "final inventory owns delivered stock");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded cargo and journal continue deterministically");
            c.Equal(BigInteger.Zero, delivery.Totals(resource).CreatedRaw, "physical hauling never claims creation");
            Supply(c); Faults(c);
        }
        private static void Supply(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(Request("colony")); World world = game.World;
            TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel"); PlayerId company = new PlayerId(0);
            Entity finite = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), company, new TileCoord(12, 18)); world.Settle();
            destination.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            c.Equal(ResourceSupplyKind.ExistingStock, finite.Get<ISupplier>().SupplyKind, "finite supplier explicitly declares existing stock origin");
            Tick(game); // Runtime opening facts must reach the existing checkpoint boundary before Hash/Save.
            c.That(ReferenceEquals(finite.Get<IInventory>(), finite.Get<ISupplier>().SupplyStock), "finite supply exposes its sole existing inventory, not copied stock");
            IResourceTransaction transfer = world.Services.Get<IResourceTransaction>(); IEconomy economy = world.Services.Get<IEconomy>();
            ResourceTransferIntent intent = Intent(game, finite, destination, steel, Fix.FromInt(15), 1, true);
            Fix cash = economy.Cash(company); economy.Charge(company, cash, new Code("proof.cash")); ulong before = game.Hash();
            c.Equal(ResourceTransferStatus.NoPayment, transfer.Commit(intent).Status, "unaffordable paid flow is a typed normal refusal");
            c.Equal(before, game.Hash(), "refusal changes no journal, balance, receipt or money");
            c.Equal(0, Read(game).Entries.Count, "refused supply emits no successful fact");
            economy.Credit(company, cash, new Code("proof.cash")); c.That(transfer.Commit(intent).Committed, "funded finite stock purchase succeeds"); Tick(game);
            ResourceFlowFact purchase = Read(game).Entries[0].Item;
            c.Equal(ResourceFlowKind.StockPurchase, purchase.Kind, "finite purchase is distinct from creation");
            c.Equal(Fix.FromInt(25), purchase.SourceBefore.Value, "purchase fact reads actual finite source stock");
            c.Equal(Fix.FromInt(10), purchase.SourceAfter.Value, "finite stock delta is verified centrally");
            c.Equal(cash - Fix.FromInt(105), economy.Cash(company), "physical purchase charges exactly once");
            c.Equal(ResourceTransferStatus.InvalidReceipt, transfer.Commit(intent).Status, "consumed finite receipt remains a normal refusal");
            Entity market = Find(game, "market");
            c.Equal(ResourceSupplyKind.AuthoredCreation, market.Get<ISupplier>().SupplyKind, "unlimited source explicitly declares authored creation");
            c.That(market.Get<ISupplier>().SupplyStock == null, "authored creation does not fabricate an inventory");
            intent = Intent(game, market, destination, steel, Fix.FromInt(10), 2, true); SimGame twin = factory.Load(game.Save());
            c.That(transfer.Commit(intent).Committed, "authored source fulfills its owned receipt");
            twin.World.Services.Get<IResourceTransaction>().Commit(intent); Tick(game); Tick(twin);
            ResourceFlowPage page = Read(game); ResourceFlowFact creation = page.Entries[1].Item;
            c.Equal(ResourceFlowKind.AuthoredCreation, creation.Kind, "new source stock is labelled creation, never conserved transfer");
            c.That(!creation.SourceBefore.HasValue && !creation.SourceAfter.HasValue, "creation fact reports no invented source stock balance");
            c.Equal(Fix.FromInt(25), creation.DestinationAfter, "creation is still actual stock admitted to the destination");
            c.Equal(new BigInteger(Fix.FromInt(15).Raw), page.Totals(steel).StockPurchasedRaw, "finite purchases are derived from retained facts");
            c.Equal(new BigInteger(Fix.FromInt(10).Raw), page.Totals(steel).CreatedRaw, "authored creation totals remain separate");
            c.Equal(game.Hash(), twin.Hash(), "saved finite/creation journal facts continue deterministically");
            FiniteFault(c);
        }
        private static void FiniteFault(Check c)
        {
            string authored = Colony.Defs.Replace("\"finite_supplier\": { \"resource\": \"steel\", \"price\": 7 }",
                "\"finite_supplier\": { \"resource\": \"steel\", \"price\": 7, \"receipt_fault\": \"no_stock_delta\" }");
            c.That(authored != Colony.Defs, "finite fault is explicitly authored in the existing content path");
            SimGame game = Colony.Factory(authored).NewGame(Request("colony")); World world = game.World;
            TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity supplier = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18)); world.Settle();
            destination.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            Tick(game);
            int id = world.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), supplier.Id, destination.Id, steel, Fix.FromInt(15), 100, true);
            c.That(id > 0, "lying finite supplier still acquires normal physical supply work");
            for (int i = 0; i < 2880 && game.Fault == null; i++) game.Update();
            c.That(game.Fault?.Message.Contains("source violated exact physical transfer receipt") == true,
                "advanced version and consumed receipt cannot substitute for actual finite stock consumption");
            c.Equal(Fix.FromInt(25), supplier.Get<IInventory>().Amount(steel), "fault exposes unchanged finite source stock instead of claiming conservation");
            c.Equal(0, Read(game).Entries.Count, "lying finite stock purchase emits no verified journal record");
            c.Throws<InvalidOperationException>(() => game.Save(), "partial paid-provider fault is not resumable");
        }
        private static void Faults(Check c)
        {
            foreach (string prototype in new[] { "fault.partial", "fault.retained", "fault.query", "fault.status", "fault.version", "fault.admission" })
            {
                SimGame game = Points().NewGame(Request("points")); World world = game.World;
                Entity broken = world.Spawn(world.Content.Get<EntityDef>(prototype), new PlayerId(0)); world.Settle();
                Tick(game);
                Entity source = prototype == "fault.admission" ? Find(game, "source") : broken;
                Entity destination = prototype == "fault.admission" ? broken : Find(game, "destination");
                ResourceId resource = world.Content.ResourceId("supply");
                world.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(30), 100);
                for (int i = 0; i < 200 && game.Fault == null; i++) game.Update();
                c.That(game.Fault != null, "provider violation faults physical execution: " + prototype);
                c.Equal(prototype == "fault.admission" ? 1 : 0, Read(game).Entries.Count,
                    "unverified mutation adds no successful fact; earlier verified pickup is retained: " + prototype);
                c.That(!world.Events.HasPending, "failed protocol leaves no successful event queued: " + prototype);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted provider mutation cannot be saved as a successful journal checkpoint: " + prototype);
            }
        }
    }
}
