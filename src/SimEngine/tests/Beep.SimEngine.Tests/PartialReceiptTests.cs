using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class PartialReceiptTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.partial", 1);
        private static readonly ReservationOwner Other = new ReservationOwner("proof.partial", 2);
        public static void Register()
        {
            TestRunner.Add("Partial receipts: exact native remainders, refusal and saved identity", Native);
            TestRunner.Add("Partial receipts: paid sources, scoped endpoints and provider faults", Providers);
        }
        private static SimGame New(GameFactory factory, string scenario = "endpoints")
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 101 });
        private static Entity Find(SimGame game, string prototype)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing partial receipt fixture " + prototype);
        }
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("partial receipt Update: " + game.Fault); }
        private static ResourceFlowPage History(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, cursor);
        private static ResourceTransferIntent Intent(ResourceId resource, ReservationToken source, ReservationToken destination,
            Fix amount, ResourceTransferKind kind = ResourceTransferKind.Stock)
            => new ResourceTransferIntent(Owner, Company, source, destination, resource, amount, kind);
        private static ResourceReservation Record(IReadOnlyList<ResourceReservation> records, ReservationToken token)
        {
            foreach (ResourceReservation record in records) if (record.Token.Equals(token)) return record;
            throw new InvalidOperationException("missing expected remainder " + token);
        }
        private static void Refuse(Check c, SimGame game, ResourceTransferIntent intent, ResourceTransferStatus status, string label)
        {
            ulong hash = game.Hash();
            c.Equal(status, Transactions(game).Evaluate(intent).Status, label + " preview refuses");
            c.Equal(status, Transactions(game).Commit(intent).Status, label + " commit refuses");
            c.Equal(hash, game.Hash(), label + " preserves all authoritative state");
        }
        private static void Native(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory), independent = New(factory);
            IInventory source = Find(game, "source").Get<IInventory>(), destination = Find(game, "destination").Get<IInventory>();
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            ReservationToken outgoing = source.ReserveOut(Owner, steel, Fix.FromInt(12));
            ReservationToken incoming = destination.ReserveIn(Owner, steel, Fix.FromInt(15));
            ReservationToken protectedOut = source.ReserveOut(Other, steel, Fix.FromInt(3));
            ReservationToken protectedIn = source.ReserveIn(Other, water, Fix.One);
            ReservationToken otherDestination = destination.ReserveIn(Other, water, Fix.FromInt(2));
            var first = Intent(steel, outgoing, incoming, Fix.FromInt(4));
            ulong before = game.Hash(), independentHash = independent.Hash();
            c.That(Transactions(game).Evaluate(first).Ready, "unequal remaining receipts admit a bounded transfer");
            c.Equal(before, game.Hash(), "bounded native preview is pure");
            Refuse(c, game, Intent(steel, outgoing, incoming, Fix.Zero), ResourceTransferStatus.InvalidRequest, "zero");
            Refuse(c, game, Intent(steel, outgoing, incoming, -Fix.One), ResourceTransferStatus.InvalidRequest, "negative");
            Refuse(c, game, Intent(steel, outgoing, incoming, Fix.FromRatio(1, 2)), ResourceTransferStatus.InvalidRequest, "fractional discrete resource");
            Refuse(c, game, Intent(steel, outgoing, incoming, Fix.FromInt(13)), ResourceTransferStatus.InvalidReceipt, "over source remainder");
            Refuse(c, game, Intent(water, outgoing, incoming, Fix.One), ResourceTransferStatus.InvalidReceipt, "wrong resource");
            Refuse(c, game, new ResourceTransferIntent(Other, Company, outgoing, incoming, steel, Fix.One), ResourceTransferStatus.InvalidReceipt, "foreign requester");
            Refuse(c, game, Intent(steel, incoming, outgoing, Fix.One), ResourceTransferStatus.InvalidReceipt, "wrong direction");
            c.Throws<InvalidOperationException>(() => source.TakeReserved(Owner, outgoing, Fix.FromInt(13)), "direct source cannot overconsume");
            c.Throws<InvalidOperationException>(() => destination.ReceiveReserved(game.World.Content, Owner, incoming, Fix.Zero), "direct inlet cannot consume zero");
            c.Equal(before, game.Hash(), "primitive refusal leaves receipts and sequence unchanged");
            ResourceReservation retainedSource = Record(source.Reservations, outgoing), retainedDestination = Record(destination.Reservations, incoming);
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "unspent receipts use their existing native codec");
            c.That(Transactions(game).Commit(first).Committed && Transactions(twin).Commit(first).Committed, "first chunk moves exactly");
            c.Throws<InvalidOperationException>(() => game.Save(), "pending success history must settle before checkpoint");
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "first chunk continues identically after restore");
            c.Equal(Fix.FromInt(26), source.Amount(steel), "source loses actual first quantity");
            c.Equal(Fix.FromInt(4), destination.Amount(steel), "destination receives actual first quantity");
            c.Equal(Fix.FromInt(8), Record(source.Reservations, outgoing).Amount, "source token owns only outstanding eight");
            c.Equal(Fix.FromInt(11), Record(destination.Reservations, incoming).Amount, "larger destination keeps outstanding eleven");
            c.Equal(Owner, Record(source.Reservations, outgoing).Owner, "partial transfer keeps requester identity");
            c.Equal(outgoing, source.Reservations[0].Token, "remainder replaces the same row in place");
            c.Equal(Fix.FromInt(12), retainedSource.Amount, "detached old receipt is immutable");
            c.Equal(Fix.FromInt(15), retainedDestination.Amount, "detached old inlet receipt is immutable");
            c.Equal(Fix.FromInt(3), Record(source.Reservations, protectedOut).Amount, "other outbound owner remains protected");
            c.Equal(Fix.One, Record(source.Reservations, protectedIn).Amount, "unrelated inbound claim is unchanged");
            c.Equal(Fix.FromInt(2), Record(destination.Reservations, otherDestination).Amount, "destination unrelated claim survives");
            c.Equal(Fix.FromInt(5), source.Amount(water), "unrelated physical resource never moves");
            c.Equal(independentHash, independent.Hash(), "interleaved session owns no shared stock or tokens");
            ResourceFlowFact fact = History(game, 2).Entries[0].Item;
            c.Equal(Fix.FromInt(4), fact.Amount, "history records actual chunk, not original promise");
            c.Equal(outgoing, fact.Source, "chunk history retains source identity");
            c.Equal(incoming, fact.Destination, "chunk history retains inlet identity");
            Refuse(c, game, Intent(steel, outgoing, incoming, Fix.FromInt(9)), ResourceTransferStatus.InvalidReceipt, "over updated remainder");
            twin = factory.Load(game.Save());
            foreach (int chunk in new[] { 3, 5 })
            {
                var intent = Intent(steel, outgoing, incoming, Fix.FromInt(chunk));
                c.That(Transactions(game).Commit(intent).Committed && Transactions(twin).Commit(intent).Committed, "same original tokens fund chunk " + chunk);
                Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved remainder continuation " + chunk);
            }
            c.That(!ResourceReservationBook.ContainsToken(source.Reservations, outgoing), "only exhausted source row is removed");
            c.Equal(Fix.FromInt(3), Record(destination.Reservations, incoming).Amount, "unspent destination is not silently released");
            c.Equal(Fix.FromInt(30), source.Amount(steel) + destination.Amount(steel), "all chunks conserve physical stock");
            c.Equal(3, History(game, 2).Entries.Count, "one exact fact per successful chunk");
            Refuse(c, game, Intent(steel, outgoing, incoming, Fix.One), ResourceTransferStatus.InvalidReceipt, "exhausted source");
            destination.ReleaseIn(Owner, incoming); source.ReleaseOut(Other, protectedOut); source.ReleaseIn(Other, protectedIn);
            destination.ReleaseIn(Other, otherDestination);
            c.Equal(0, source.Reservations.Count + destination.Reservations.Count, "explicit release clears only actual remaining commitments");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled native completion restores exactly");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking native semantics use current save identity");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "no legacy protocol identity survives");
            FluidsAndQuantum(c);
        }
        internal static void FluidsAndQuantum(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory);
            IInventory source = Find(game, "source").Get<IInventory>(), destination = Find(game, "destination").Get<IInventory>();
            ResourceId water = game.World.Content.ResourceId("water");
            ReservationToken outgoing = source.ReserveOut(Owner, water, Fix.FromRatio(5, 2)), incoming = destination.ReserveIn(Owner, water, Fix.FromRatio(5, 2));
            foreach (Fix amount in new[] { Fix.FromRatio(3, 4), Fix.FromRatio(7, 4) })
            { c.That(Transactions(game).Commit(Intent(water, outgoing, incoming, amount)).Committed, "continuous native chunks transfer exactly"); Tick(game); }
            c.Equal(Fix.FromRatio(5, 2), destination.Amount(water), "fluid fractions are not rounded or converted into counts");
            c.Equal(0, source.Reservations.Count + destination.Reservations.Count, "fractional completion consumes the original rows");

            factory = PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json")); game = New(factory, "points");
            source = Find(game, "source").Get<IInventory>(); destination = Find(game, "destination").Get<IInventory>();
            ResourceId supply = game.World.Content.ResourceId("supply");
            outgoing = source.ReserveOut(Owner, supply, Fix.FromInt(30)); incoming = destination.ReserveIn(Owner, supply, Fix.FromInt(30));
            Refuse(c, game, Intent(supply, outgoing, incoming, Fix.FromInt(5)), ResourceTransferStatus.Incompatible, "source discrete packing quantum");
            Refuse(c, game, Intent(supply, outgoing, incoming, Fix.FromInt(3)), ResourceTransferStatus.Incompatible, "destination discrete packing quantum");
            c.That(Transactions(game).Commit(Intent(supply, outgoing, incoming, Fix.FromInt(15))).Committed, "bounded chunk honors common source/inlet quantum");
            Tick(game);
            c.Equal(Fix.FromInt(15), Record(source.Reservations, outgoing).Amount, "compatible packing leaves native source remainder");
            c.Equal(Fix.FromInt(15), Record(destination.Reservations, incoming).Amount, "compatible packing leaves native inlet remainder");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public-only point provider partial receipts use existing codec");
        }
        private static void Providers(Check c)
        {
            var physical = New(Colony.Factory(), "colony");
            c.That(!physical.World.Services.Has<IEconomy>() && !physical.World.Services.Has<IBusinessAccounts>() &&
                !physical.World.Services.Has<IMonetaryBudgets>(), "default physical colony has no fabricated monetary services");
            Paid(c, false); Paid(c, true); Scoped(c); Faults(c);
        }
        private static GameFactory PaidFactory()
        {
            var content=Json.Parse(Colony.Defs); var scenario=content.Get("scenarios")[0];
            scenario.Set("game_mode",JsonValue.Of("partial.paid"));
            scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
            scenario.Get("settings").Set(TycoonModule.ModuleId,Json.Parse("{\"account_policy\":\"partial.accounts\"}"));
            var master=Json.Parse(Colony.Manifest); var modules=master.Get("modules");
            for(int i=0;i<modules.Count;i++) if(modules[i].Get("id").Text==TycoonModule.ModuleId)
                modules[i].Get("documents").Add(JsonValue.Of("partial-receipt-business.json"));
            return Colony.Factory(definitions:content.ToString(),masterDocument:master.ToString(),
                moduleDocuments:new Dictionary<string,string> {
                    ["partial-receipt-business.json"]=FrameworkProofContent.Read("partial-receipt-business.json") });
        }
        internal static void Paid(Check c, bool creation)
        {
            GameFactory factory = PaidFactory(); SimGame game = New(factory, "colony"); World world = game.World;
            TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>(creation ? "partial.creation-supplier" : "module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), Company, new TileCoord(12, 18));
            world.Settle(); IInventory dest = destination.Get<IInventory>(); dest.Remove(steel, Fix.FromInt(30));
            ISupplier supplier = source.Get<ISupplier>(); IBusinessAccounts economy = world.Services.Get<IBusinessAccounts>();
            c.That(ReferenceEquals(economy,world.Services.Get<IEconomy>()) && ReferenceEquals(economy,world.Services.Get<IMonetaryBudgets>()),
                "optional monetary aliases share one actual account owner");
            c.Equal(Fix.Zero,economy.Cash(Company),"explicit paid account begins at authored zero, not a default grant");
            Fix price = ((IResourceSupplyPrice)supplier).UnitPrice(steel);
            c.Equal(Fix.FromInt(creation ? 10 : 7),price,"original authored supplier price unchanged");
            ReservationToken outgoing = supplier.ReserveSupply(Owner, steel, Fix.FromInt(10)), incoming = dest.ReserveIn(Owner, steel, Fix.FromInt(10));
            var first = Intent(steel, outgoing, incoming, Fix.FromInt(3), ResourceTransferKind.Supply);
            Refuse(c, game, first, ResourceTransferStatus.NoPayment, "unfunded bounded supply");
            economy.Credit(Company, price * Fix.FromInt(3), new Code("proof.cash"));
            c.That(Transactions(game).Evaluate(first).Ready, "can afford actual chunk without full outstanding price");
            SimGame twin = factory.Load(game.Save());
            c.That(Transactions(game).Commit(first).Committed && Transactions(twin).Commit(first).Committed, "bounded paid source fulfills");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "paid partial continuation uses the same economy and receipts");
            c.Equal(Fix.Zero, economy.Cash(Company), "only actual chunk is charged");
            c.Equal(Fix.FromInt(7), Record(supplier.Reservations, outgoing).Amount, "paid source keeps original token for unsold quantity");
            c.Equal(Fix.FromInt(7), Record(dest.Reservations, incoming).Amount, "paid destination keeps unreceived quantity");
            if (!creation) c.Equal(Fix.FromInt(22), supplier.SupplyStock.Amount(steel), "finite source stock decreases by actual sale only");
            else c.That(supplier.SupplyStock == null, "explicit authored creation exposes no invented physical source balance");
            var next = Intent(steel, outgoing, incoming, Fix.FromInt(7), ResourceTransferKind.Supply);
            Refuse(c, game, next, ResourceTransferStatus.NoPayment, "unfunded remainder");
            economy.Credit(Company, price * Fix.FromInt(7), new Code("proof.cash")); twin = factory.Load(game.Save());
            c.That(Transactions(game).Commit(next).Committed && Transactions(twin).Commit(next).Committed, "funded remainder consumes same tokens");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "completed paid source restores deterministically");
            c.Equal(Fix.FromInt(10), dest.Amount(steel), "paid chunks reach actual inlet exactly");
            c.Equal(Fix.Zero, economy.Cash(Company), "no full-promise double charge");
            c.Equal(0, supplier.Reservations.Count + dest.Reservations.Count, "fully sold promise and received inlet are removed");
        }
        private static void Scoped(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory), independent = New(factory);
            EndpointPools pools = game.World.Services.Get<EndpointPools>(); ResourceId steel = game.World.Content.ResourceId("steel");
            ResourceInventoryState first = pools.Get(1), wide = pools.Get(5000000000); first.Add(steel, Fix.FromInt(8));
            ReservationToken outgoing = first.ReserveOut(Owner, steel, Fix.FromInt(8)), incoming = wide.ReserveIn(Owner, steel, Fix.FromInt(8));
            ulong independentHash = independent.Hash();
            game.Seat(0).Submit(new TransferProbeCommand { Intents = new[] { Intent(steel, outgoing, incoming, Fix.FromInt(3)) } });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "pending chunk command restores through normal inbox codec");
            c.Equal(Fix.FromInt(5), first.Amount(steel), "scoped source retains actual stock");
            c.Equal(Fix.FromInt(3), wide.Amount(steel), "wide-ID scoped inlet receives actual stock");
            c.Equal(Fix.FromInt(5), Record(first.Reservations, outgoing).Amount, "scoped outbound remainder is native");
            c.Equal(Fix.FromInt(5), Record(wide.Reservations, incoming).Amount, "scoped inbound remainder is native");
            ResourceFlowFact fact = History(game, 2).Entries[0].Item;
            c.Equal(wide.Endpoint, fact.Destination.Endpoint, "history never narrows provider identity to an entity");
            c.Equal(Fix.FromInt(3), fact.Amount, "normal Update reports actual bounded quantity");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status, "partial history is private");
            c.Equal(independentHash, independent.Hash(), "normal command does not touch another session");
            ResourceReservation remaining = Record(first.Reservations, outgoing);
            c.That(Transactions(game).ConsumeClaims(Company, new[] { remaining }, new Code("proof.use"), System.Array.Empty<ResourceReservation>()).Committed, "whole claim-set consumption explicitly uses actual remainder");
            Tick(game);
            c.Equal(Fix.Zero, first.Amount(steel), "generalized claim checkpoint still proves exact consumption");
            c.Equal(Fix.FromInt(5), Record(wide.Reservations, incoming).Amount, "claim consumption cannot alter separate destination receipt");
            wide.ReleaseIn(Owner, incoming);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "scoped completion uses existing owners only");
        }
        private static void Faults(Check c)
        {
            foreach (ClaimFaultMode mode in new[] { ClaimFaultMode.ShortTake, ClaimFaultMode.EraseRemainder, ClaimFaultMode.OtherStock, ClaimFaultMode.OtherReceipt, ClaimFaultMode.MutatingCheck, ClaimFaultMode.SilentVersion, ClaimFaultMode.ShortReceive, ClaimFaultMode.EraseInletRemainder })
            {
                SimGame game = New(EndpointGame.Factory()); EndpointPools pools = game.World.Services.Get<EndpointPools>();
                ResourceInventoryState source = pools.Get(2), destination = pools.Get(1);
                ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
                source.Add(steel, Fix.FromInt(6)); source.Add(water, Fix.FromInt(2));
                ReservationToken outgoing = source.ReserveOut(Owner, steel, Fix.FromInt(4)), incoming = destination.ReserveIn(Owner, steel, Fix.FromInt(4));
                source.ReserveOut(Other, steel, Fix.One); source.ReserveIn(Other, water, Fix.One);
                bool inletFault = mode == ClaimFaultMode.ShortReceive || mode == ClaimFaultMode.EraseInletRemainder;
                var history = History(game); long latest=history.Latest; int entries=history.Entries.Count;
                pools.ClaimFault = mode; pools.FaultEndpoint = inletFault ? 1 : 2;
                game.Seat(0).Submit(new TransferProbeCommand { Intents = new[] { Intent(steel, outgoing, incoming, Fix.One) } });
                game.Update();
                c.That(game.Fault != null, mode + " faults normal command/Update rather than reporting a partial success");
                c.Equal(inletFault ? Fix.One : Fix.Zero, destination.Amount(steel), mode + " exposes actual faulted state, never invented compensation");
                c.Throws<InvalidOperationException>(()=>History(game),mode+" exposes no live success history after first fault");
                c.That(history.Latest==latest && history.Entries.Count==entries,mode+" retained last-healthy facts remain detached");
                c.Throws<InvalidOperationException>(() => game.Save(), mode + " inconsistent provider state cannot checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), mode + " inconsistent provider state cannot hash as healthy");
            }
        }
    }
}
