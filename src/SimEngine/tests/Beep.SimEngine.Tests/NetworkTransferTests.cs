using System.Linq;
using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class NetworkTransferTests
    {
        public static void Register()
        {
            TestRunner.Add("Network transfers: timed quantum, public ownership, private history and faults", Physical);
            TestRunner.Add("Network transfers: durable topology identities, live claims and exact redistribution", Topology);
        }
        private static GameFactory Factory(string networks = null, bool topologyOnly = false)
        {
            var authored = Json.Parse(networks ?? FrameworkProofContent.Read("network-transfers.json"));
            // This protocol case supplies the actual pool explicitly; unrelated automatic source
            // flow must not add new fluid while it measures exact split/merge redistribution.
            if (topologyOnly) for (int i = 0; i < authored.Get("entities").Count; i++)
                if (authored.Get("entities")[i].Get("id").Text == "network.source") authored.Get("entities")[i].Get("components").Remove("ports");
            return PhysicalRecoveryTestContent.Factory("{}", infrastructure: authored.ToString());
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 107 };
            request.Mods.Add(ResourceFlowsModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("network tick " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int count, SimGame twin = null)
        { for (int i = 0; i < count; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static Entity Spawn(SimGame game, string definition, int x, int owner = 0, int y = 28)
            => ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>(definition), new PlayerId(owner), new TileCoord(x, y));
        private static NetworkService Setup(SimGame game, string lineId = "network.pipe")
        {
            NetworkService lines = game.World.Services.Providers<NetworkService>().Single(); ILineDefinition line = game.World.Content.Get<ILineDefinition>(lineId);
            for (int x = 10; x <= 14; x++) if (!((Beep.SimEngine.Maps.TileLineGeometry)lines.World.Services.Get<ILineGeometry>()).Stamp(line, new TileCoord(x, 28), new PlayerId(0))) throw new InvalidOperationException("network fixture stamp refused");
            Tick(game); return lines;
        }
        private static ResourceFlowPage History(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static ResourceTransferIntent Acquire(IResourceOutlet source, IResourceInlet destination, ResourceId resource, Fix amount, long identity)
        {
            var owner = new ReservationOwner("proof.network", identity);
            return new ResourceTransferIntent(owner, new PlayerId(0), source.ReserveOut(owner, resource, amount),
                destination.ReserveIn(owner, resource, amount), resource, amount);
        }
        private static void Move(SimGame game, IResourceOutlet source, IResourceInlet destination, ResourceId resource, Fix amount, long identity)
        {
            if (!game.World.Services.Get<IResourceTransaction>().Commit(Acquire(source, destination, resource, amount, identity)).Committed)
                throw new InvalidOperationException("network fixture physical transfer refused");
        }
        private static Fix Total(NetworkService lines, string network, ResourceId resource)
        { Fix result = Fix.Zero; for (int i = 0; i < lines.ComponentCount(network); i++) result += lines.Component(network, i).Amount(resource); return result; }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            Entity source = Spawn(game, "network.source", 9), receiver = Spawn(game, "network.receiver", 15);
            NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            NetworkService.NetComponent pool = lines.Component("network.main", 0); ResourceEndpointId endpoint = pool.Endpoint;
            c.That(new List<IResourceEndpointProvider>(game.World.Services.Providers<IResourceEndpointProvider>()).Contains(lines), "network discovery includes the existing graph owner alongside other scopes");
            c.That(ReferenceEquals(pool.Inventory, lines.Resolve(endpoint)), "pool projection and resolver share one actual inventory owner");
            c.That(pool.Inventory is IResourceInlet && !((object)pool.Inventory is IEntityCapability), "network outlet includes inlet without a fake entity");
            c.Equal(Fix.Zero, pool.Amount(steel), "new geometry grants no initial whole resource");
            ulong before = game.Hash(); var views = new List<NetworkPoolView>();
            c.That(game.Seat(0).View.Feature<InfrastructureView>().CopyPools("network.main", views).IsNone, "engine exposes immutable pool facts");
            NetworkPoolView retained = views[0];
            int retainedVersion = retained.Version; int retainedCount = retained.Stock.Count;
            c.Equal(Codes.UnknownDef, game.Seat(0).View.Feature<InfrastructureView>().CopyPools("absent", views), "unknown network view has an explicit refusal");
            game.Seat(1).View.Feature<InfrastructureView>().CopyPools("network.main", views);
            c.Equal(0, views.Count, "private foreign-company pool stock is not exposed");
            c.Equal(before, game.Hash(), "views never rebuild geometry, allocate IDs or mutate stock");
            long cursor = History(game).Latest; Advance(game, 8);
            c.Equal(9L, game.Tick, "fixture observes the declared time quantum");
            c.Equal(Fix.FromInt(29), source.Get<IInventory>().Amount(steel), "elapsed source leg earns its first whole resource independently");
            c.Equal(Fix.Zero, receiver.Get<IInventory>().Amount(steel), "waiting cannot teleport delivery");
            SimGame twin = factory.Load(game.Save()); Advance(game, 5, twin);
            c.Equal(Fix.One, receiver.Get<IInventory>().Amount(steel), "discrete resource rate rounds to the shared exact quantum");
            c.Equal(Fix.FromRaw(Fix.FromInt(9).Raw * 13 / 60), receiver.Get<IInventory>().Amount(water), "continuous receiving leg retains cumulative native quantization");
            c.Equal(Fix.FromInt(28), source.Get<IInventory>().Amount(steel), "actual source loses exactly its two earned native legs");
            c.Equal(Fix.FromRaw(Fix.FromInt(6).Raw - Fix.FromInt(9).Raw * 14 / 60), source.Get<IInventory>().Amount(water), "actual fluid source retains its precise cumulative remainder");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(steel) + receiver.Get<IInventory>().Amount(steel) + pool.Amount(steel), "partial timed pipeline retains every real whole unit");
            c.Equal(0, pool.Inventory.Reservations.Count + source.Get<IInventory>().Reservations.Count + receiver.Get<IInventory>().Reservations.Count,
                "each scheduled leg consumes its owned receipts");
            c.Equal(game.Hash(), twin.Hash(), "saved scheduled flow and scoped facts continue identically");
            c.Equal(endpoint, lines.Component("network.main", 0).Endpoint, "flow changes stock/version, never pool identity");
            c.Equal(retainedVersion, retained.Version, "retained pool snapshot is detached from subsequent owner versions");
            c.Equal(retainedCount, retained.Stock.Count, "retained stock snapshot does not become a balance authority");
            ResourceFlowPage page = History(game, cursor); c.That(page.Entries.Count > 4, "per-Update native flow records actual earned legs, not batched fake effects");
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item;
                c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "pipeline movement is not fake creation or consumption");
                c.That(fact.Source.Endpoint == endpoint || fact.Destination.Endpoint == endpoint, "each fact references the actual scoped pool");
                c.That(fact.SourceVersionAfter > fact.SourceVersionBefore && fact.DestinationVersionAfter > fact.DestinationVersionBefore, "physical owner versions advance");
            }
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw + page.Totals(steel).ConsumedRaw, "network accounting fabricates no lifecycle effects");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor).Status, "actor-attributed history stays private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled network membership/stock/history checkpoint identically");
            Quantized(c, factory); Backpressure(c, factory); Neutral(c, factory); Fault(c, factory);
        }
        private static void Quantized(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity source = Spawn(game, "network.source", 9), receiver = Spawn(game, "network.cased-receiver", 15);
            NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            Advance(game, 9); NetworkService.NetComponent pool = lines.Component("network.main", 0);
            c.Equal(Fix.One, pool.Amount(steel), "whole source quantity enters the actual pool");
            c.Equal(Fix.FromRatio(3, 2), pool.Amount(water), "precise continuous source quantity enters the pool");
            c.Equal(0, receiver.Get<IInventory>().Stock.Count, "sub-quantum destination rate cannot create fractional measured cases");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(steel) + pool.Amount(steel), "blocked exact destination retains all real source/pool steel");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "blocked shared-quantum flow remains durable");
        }
        private static void Backpressure(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity source = Spawn(game, "network.source", 9), receiver = Spawn(game, "network.receiver", 15);
            NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            var owner = new ReservationOwner("proof.capacity", 1); IInventory destination = receiver.Get<IInventory>();
            ReservationToken held = destination.ReserveIn(owner, water, Fix.FromInt(1000));
            SimGame twin = factory.Load(game.Save()); Advance(game, 9, twin);
            c.Equal(Fix.One, lines.Component("network.main", 0).Amount(steel), "full promised destination leaves stock physically in the pool");
            c.Equal(0, destination.Stock.Count, "another owner's admission promise is not spent by network flow");
            c.Equal(held, destination.Reservations[0].Token, "blocked flow preserves the actual foreign-requester receipt");
            c.Equal(game.Hash(), twin.Hash(), "saved backpressure and owner promises continue identically");
            destination.ReleaseIn(owner, held); twin.World.Entities.Get(receiver.Id).Get<IInventory>().ReleaseIn(owner, held);
            Advance(game, 10, twin);
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(steel) + Total(lines, "network.main", steel) + destination.Amount(steel), "unblocked flow conserves actual steel");
            c.Equal(Fix.FromInt(6), source.Get<IInventory>().Amount(water) + Total(lines, "network.main", water) + destination.Amount(water), "unblocked flow conserves actual continuous volume");
            c.Equal(game.Hash(), twin.Hash(), "saved release and subsequent verified legs are deterministic");
        }
        private static void Neutral(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Spawn(game, "network.source", 9); Entity receiver = Spawn(game, "network.receiver", 15, 1);
            NetworkService lines = Setup(game, "network.public-pipe"); ResourceId steel = game.World.Content.ResourceId("steel");
            long receiverCursor = game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Latest;
            Advance(game, 13);
            c.Equal(Fix.One, receiver.Get<IInventory>().Amount(steel), "explicit public-neutral pool permits physically shared company flow");
            c.Equal(PlayerId.None, lines.Component("network.public", 0).Owner, "neutral geometry does not invent a company owner");
            c.Equal(0, lines.Component("network.private-neutral", 0).Pool.Count, "unowned but private network never silently becomes public");
            ResourceFlowPage outgoing = History(game), incoming = game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(1), receiverCursor);
            c.Equal(ResourceFlowKind.StockTransfer, incoming.Entries[0].Item.Kind, "public pull remains a transfer from actual neutral stock");
            c.Equal(new PlayerId(1), incoming.Entries[0].Item.Company, "public pull is attributed to its actual requesting company");
            c.Equal(2, History(game, outgoing.Latest - 2).Entries.Count, "source company sees its own two push legs only");
            var views = new List<NetworkPoolView>(); game.Seat(1).View.Feature<InfrastructureView>().CopyPools("network.public", views);
            c.Equal(1, views.Count, "explicit public-neutral pool diagnostics are visible to both companies");
            game.Seat(1).View.Feature<InfrastructureView>().CopyPools("network.private-neutral", views);
            c.Equal(0, views.Count, "private-neutral pool diagnostics do not leak");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public ownership and private journal partitions checkpoint identically");
        }
        private static void Fault(Check c, GameFactory factory)
        {
            foreach (string mode in new[] { "retain_take", "partial_take" })
            {
                var authored = Json.Parse(FrameworkProofContent.Read("network-transfers.json"));
                for (int i = 0; i < authored.Get("entities").Count; i++) if (authored.Get("entities")[i].Get("id").Text == "network.fault")
                    authored.Get("entities")[i].Get("components").Get("single_resource_port").Set("receipt_fault", JsonValue.Of(mode));
                SimGame game = New(mode == "retain_take" ? factory : Factory(authored.ToString()));
                Entity source = Spawn(game, "network.fault", 9); Setup(game);
                c.Equal(mode, source.Def.Component<Beep.SimEngine.ExtensionProof.SingleResourcePortDef>().ReceiptFault, "actual native provider selects the explicit negative behavior");
                for (int i = 0; i < 12 && game.Fault == null; i++) game.Update();
                c.Equal("resource.transaction", game.Fault?.Phase, "dishonest network receipt preserves the original resource owner's first fault: " + game.Fault);
                string expected = mode == "retain_take" ? "did not advance its claimed mutation version" : "source violated exact physical transfer receipt";
                c.That(game.Fault?.Message.Contains(expected) == true, "first failure identifies the actual selected receipt violation: " + game.Fault);
                c.That(!game.World.Events.HasPending, "failed network leg publishes no resource success prefix");
                c.Throws<InvalidOperationException>(() => History(game), "failed network leg exposes no healthy private live journal");
                c.Throws<InvalidOperationException>(() => game.Save(), "partial/invalid network transfer cannot serialize as healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), "partial/invalid network transfer cannot certify a healthy hash");
                long tick = game.Tick; game.Update(); c.Equal(tick, game.Tick, "faulted flow cannot resume through a compensating fallback");
            }
        }

        private static void Topology(Check c)
        {
            GameFactory factory = Factory(topologyOnly: true); SimGame game = New(factory); Entity source = Spawn(game, "network.source", 9), buffer = Spawn(game, "network.buffer", 20);
            NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            Fix Total(ResourceId resource) => source.Get<IInventory>().Amount(resource) + buffer.Get<IInventory>().Amount(resource) +
                Enumerable.Range(0, lines.ComponentCount("network.main")).Aggregate(Fix.Zero, (sum, index) => sum + lines.Component("network.main", index).Amount(resource));
            Fix originalSteel = Total(steel), originalWater = Total(water);
            ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe"); INetworkTopology topology = game.World.Services.Get<INetworkTopology>();
            NetworkService.NetComponent original = lines.Component("network.main", 0); ResourceEndpointId oldId = original.Endpoint;
            Move(game, source.Get<IInventory>(), original.Inventory, steel, Fix.FromInt(11), 1);
            Move(game, source.Get<IInventory>(), original.Inventory, water, Fix.FromRatio(11, 2), 2); Tick(game);
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)original.Pool).Clear(), "public pool is a read-only projection, never a raw mutable balance list");
            CorruptCodec(c, factory, game);
            var middle = new[] { new TileCoord(12, 28) };
            ResourceTransferIntent reserved = Acquire(original.Inventory, buffer.Get<IInventory>(), steel, Fix.FromInt(3), 3);
            ulong before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))), "destructive split refuses an actual live pool receipt");
            c.Equal(ResourceTransactions.ActivePromises, topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))), "execution preserves the same active-claim blocker");
            c.Equal(before, game.Hash(), "claim refusal consumes no endpoint ID, promise, geometry or stock");
            ((Beep.SimEngine.Maps.TileLineGeometry)lines.World.Services.Get<ILineGeometry>()).Stamp(line, new TileCoord(25, 28), new PlayerId(0));
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "dirty map plus prior component claims save without rebuilding or stock grants");
            c.Equal(oldId, twin.World.Services.Providers<NetworkService>().Single().Component("network.main", 0).Endpoint, "restored pending geometry preserves settled endpoint identity");
            Tick(game); Tick(twin);
            c.That(ReferenceEquals(original, lines.Component("network.main", 0)), "unrelated new geometry retains the same real pool owner");
            c.Equal(2, lines.ComponentCount("network.main"), "new disconnected component gets its own empty endpoint");
            ResourceEndpointId unrelated = lines.Component("network.main", 1).Endpoint;
            c.Equal(reserved.Source, original.Inventory.Reservations[0].Token, "unchanged component retains its exact promise");
            c.Equal(game.Hash(), twin.Hash(), "unchanged claims and newly allocated IDs continue deterministically");
            c.That(game.World.Services.Get<IResourceTransaction>().Commit(reserved).Committed, "durable pool receipt still transfers to the actual destination");
            twin.World.Services.Get<IResourceTransaction>().Commit(reserved); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "restored claimed transfer reaches identical real owners/history");
            c.Equal(originalSteel, Total(steel), "claimed transfer conserves actual source/buffer/all main pool steel");
            c.Equal(originalWater, Total(water), "claimed transfer conserves actual source/buffer/all main pool native fluid");
            Fix splitSteel = original.Amount(steel), splitWater = original.Amount(water);
            long cursor = History(game).Latest; before = game.Hash();
            c.That(topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "released claim permits exact native split");
            c.That(game.Seat(0).View.Feature<InfrastructureView>().PreviewRemoval(line.Id, LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,middle))).IsNone, "view delegates to the same pure graph/stock preflight");
            c.Equal(before, game.Hash(), "candidate allocation is not a live stock store or ID allocation");
            c.That(topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "split executes planned legs against actual old and new owners");
            twin.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), twin.World.Content.Get<ILineDefinition>(line.Id), LineDeclaration.ReadPath(twin.World,(twin.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((twin.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,middle)));
            c.Throws<InvalidOperationException>(() => game.Save(), "committed topology facts drain before checkpoint"); Tick(game); Tick(twin);
            c.Equal(3, lines.ComponentCount("network.main"), "changed component splits while unrelated component survives");
            c.That(lines.Resolve(oldId) == null, "retired endpoint disappears only after all actual stock has moved");
            c.Equal(Fix.Zero, original.Amount(steel) + original.Amount(water), "retired owner is physically emptied, never discarded with stock");
            c.Equal(Fix.FromInt(4), lines.Component("network.main", 0).Amount(steel), "native discrete share is exact");
            c.Equal(Fix.FromRatio(11, 4), lines.Component("network.main", 1).Amount(water), "continuous split retains exact fractional stock");
            c.Equal(unrelated, lines.Component("network.main", 2).Endpoint, "unchanged unrelated identity does not churn");
            c.Equal(game.Hash(), twin.Hash(), "saved deterministic redistribution preserves IDs, receipts, versions and history");
            ResourceFlowPage page = History(game, cursor); c.Equal(4, page.Entries.Count, "split records each actual resource leg, not raw copied stock");
            foreach (var entry in page.Entries)
            {
                c.Equal(ResourceFlowKind.StockTransfer, entry.Item.Kind, "topology redistribution creates no fake lifecycle effect");
                c.Equal(oldId, entry.Item.Source.Endpoint, "split history identifies the old real stock owner");
                c.Equal(entry.Item.Amount, entry.Item.SourceBefore.Value - entry.Item.SourceAfter.Value, "split fact debits the original real pool by exactly the committed native amount");
                c.Equal(entry.Item.Amount, entry.Item.DestinationAfter - entry.Item.DestinationBefore, "split fact credits the actual new pool by that same amount");
                c.That(lines.Resolve(entry.Item.Destination.Endpoint) != null, "split history names a currently registered real destination owner");
            }
            c.Equal(new BigInteger(splitSteel.Raw), page.Totals(steel).MovedRaw, "all split steel moves exactly once through the original transfer journal");
            c.Equal(new BigInteger(splitWater.Raw), page.Totals(water).MovedRaw, "all fractional split fluid moves exactly once through the original transfer journal");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw + page.Totals(steel).ConsumedRaw + page.Totals(water).CreatedRaw + page.Totals(water).ConsumedRaw,
                "topology reclassification produces or consumes no material");
            c.Equal(originalSteel, Total(steel), "split preserves the closed actual steel balance without a mirror pool");
            c.Equal(originalWater, Total(water), "split preserves the closed actual native fluid balance without rounding loss");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "split geometry and actual pool codecs checkpoint identically");
            ((Beep.SimEngine.Maps.TileLineGeometry)lines.World.Services.Get<ILineGeometry>()).Stamp(line, middle[0], new PlayerId(0));
            ((Beep.SimEngine.Maps.TileLineGeometry)twin.World.Services.Providers<NetworkService>().Single().World.Services.Get<ILineGeometry>()).Stamp(twin.World.Content.Get<ILineDefinition>(line.Id), middle[0], new PlayerId(0));
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending merge checkpoint does not rebuild or duplicate stock");
            Tick(game); Tick(twin);
            c.Equal(2, lines.ComponentCount("network.main"), "reconnected components merge through actual transfers");
            c.Equal(Fix.FromInt(8), lines.Component("network.main", 0).Amount(steel), "merge conserves all remaining steel");
            c.Equal(Fix.FromRatio(11, 2), lines.Component("network.main", 0).Amount(water), "merge conserves every native fluid fraction");
            c.Equal(unrelated, lines.Component("network.main", 1).Endpoint, "merge still retains unchanged neighbor identity");
            c.Equal(game.Hash(), twin.Hash(), "saved split/reconnect execution remains deterministic");
            c.Equal(originalSteel, Total(steel), "merge conserves source/buffer/actual pool steel");
            c.Equal(originalWater, Total(water), "merge conserves source/buffer/actual pool native fluid");
            before = game.Hash(); var all = new List<TileCoord>(); for (int x = 10; x <= 14; x++) all.Add(new TileCoord(x, 28));
            c.Equal(NetworkService.OccupiedNetwork, topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,all))), "stocked last-component removal requires real disposition");
            c.Equal(before, game.Hash(), "occupied refusal preserves pool, geometry, ID sequence and history");
            Atomic(c, factory); Bypass(c, factory); Validation(c);
        }
        private static void Atomic(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity source = Spawn(game, "network.source", 9); NetworkService lines = Setup(game);
            ResourceId steel = game.World.Content.ResourceId("steel");
            Move(game, source.Get<IInventory>(), lines.Component("network.main", 0).Inventory, steel, Fix.FromInt(11), 1);
            Move(game, source.Get<IInventory>(), lines.Component("network.secondary", 0).Inventory, steel, Fix.FromInt(10), 2); Tick(game);
            ResourceEndpointId first = lines.Component("network.main", 0).Endpoint, second = lines.Component("network.secondary", 0).Endpoint;
            ulong before = game.Hash();
            c.Equal(NetworkService.NetworkCapacity, game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), game.World.Content.Get<ILineDefinition>("network.pipe"), LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,new[] { new TileCoord(12, 28) }))),
                "later network capacity refusal prevents every earlier prepared redistribution");
            c.Equal(before, game.Hash(), "all-network refusal allocates no IDs and publishes no transfer");
            c.Equal(first, lines.Component("network.main", 0).Endpoint, "first prepared graph retains its real owner");
            c.Equal(second, lines.Component("network.secondary", 0).Endpoint, "refusing graph retains its real owner");
            c.Equal(Fix.FromInt(10), lines.Component("network.secondary", 0).Amount(steel), "capacity refusal retains every actual unit");
        }
        private static void Bypass(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Spawn(game, "network.source", 9); NetworkService lines = Setup(game);
            NetworkService.NetComponent old = lines.Component("network.main", 0); ResourceId steel = game.World.Content.ResourceId("steel");
            ReservationToken token = old.Inventory.ReserveIn(new ReservationOwner("proof.pending", 1), steel, Fix.One);
            ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe");
            ((TileMap)game.World.Space).Layer<ByteLayer>(line.Layer.Id).Set(new TileCoord(12, 28), 0);
            game.Update(); // Layer-version detection must not require a second Stamp to mark dirty.
            c.That(game.Fault?.Message.Contains(ResourceTransactions.ActivePromises.Id) == true, "destructive raw topology bypass faults before losing a live receipt");
            c.That(ReferenceEquals(old, lines.State("network.main").Components[0]), "fault retains the original real owner rather than a partial candidate");
            c.Equal(token, old.Inventory.Reservations[0].Token, "fault preserves the exact promised capacity record");
            c.Throws<InvalidOperationException>(() => game.Save(), "invalid topology bypass cannot become a healthy checkpoint");
        }
        private static void Validation(Check c)
        {
            var zero = Json.Parse(FrameworkProofContent.Read("network-transfers.json"));
            zero.Get("networks")[0].Get("rates")[0].Set("quantity", JsonValue.Number("0"));
            c.Throws<ContentException>(() => New(Factory(zero.ToString())), "authored network needs a positive timed rate");
            var duplicate = Json.Parse(FrameworkProofContent.Read("network-transfers.json"));
            duplicate.Get("networks")[0].Get("rates")[1].Set("resource", JsonValue.Of("steel"));
            c.Throws<ContentException>(() => New(Factory(duplicate.ToString())), "authored duplicate resource rates do not duplicate physical flow");
        }
        private static void CorruptCodec(Check c, GameFactory factory, SimGame healthy)
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); healthy.World.Services.Providers<NetworkService>().Single().Sync(writer); writer.Flush(); payload = stream.ToArray(); }
            var reader = new StateReader(payload, 0, payload.Length); long next = 0; reader.Long(ref next);
            if (reader.Count(0) == 0) throw new InvalidOperationException("network codec fixture requires selected networks");
            string name = null; reader.String(ref name);
            if (name != "network.main") throw new InvalidOperationException("network codec fixture requires the actual canonical main graph first");
            int dirtyAt = reader.Position; bool dirty = false, initialized = false;
            reader.Bool(ref dirty); reader.Bool(ref initialized);
            if (!initialized || reader.Count(0) == 0) throw new InvalidOperationException("network codec fixture requires a real initialized component");
            int identityAt = reader.Position; long identity = 0; int version = 0; PlayerId owner = PlayerId.None;
            reader.Long(ref identity); reader.Int(ref version); reader.Player(ref owner);
            int members = reader.Count(0), firstMemberAt = reader.Position, firstMemberLength = 0;
            string lastMember = null;
            if (members < 2) throw new InvalidOperationException("network codec fixture requires original connected geometry members");
            for (int m = 0; m < members; m++)
            {
                reader.String(ref lastMember);
                if (m == 0) firstMemberLength = reader.Position - firstMemberAt;
            }
            if (reader.Count(0) <= 0) throw new InvalidOperationException("native codec fixture requires real pool stock");
            ResourceId resource = default; reader.Resource(ref resource); int amountAt = reader.Position;
            if (resource != healthy.World.Content.ResourceId("steel")) throw new InvalidOperationException("native codec fixture requires actual discrete steel stock first");
            byte[] duplicateMember;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.String(ref lastMember); writer.Flush(); duplicateMember = stream.ToArray();
            }
            if (duplicateMember.Length != firstMemberLength) throw new InvalidOperationException("native member corruption fixture requires equal encoded field lengths");
            Action<Action<byte[]>> reject = mutate =>
            {
                byte[] invalid = (byte[])payload.Clone(); mutate(invalid); SimGame restored = factory.Load(healthy.Save());
                c.Throws<CorruptStateException>(() => restored.World.Services.Providers<NetworkService>().Single().Sync(new StateReader(invalid, 0, invalid.Length)),
                    "network owner codec rejects corrupt identity/version/ownership/native stock before healthy continuation");
            };
            reject(bytes => BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(0, 8), 0));
            reject(bytes => BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(identityAt, 8), next));
            reject(bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(identityAt + 8, 4), -1));
            reject(bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(identityAt + 12, 4), 1));
            reject(bytes => BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(amountAt, 8), Fix.FromRatio(3, 2).Raw));
            reject(bytes => BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(amountAt, 8), Fix.FromInt(60).Raw));
            reject(bytes =>
            {
                bytes[dirtyAt] = 1;
                Buffer.BlockCopy(duplicateMember, 0, bytes, firstMemberAt, duplicateMember.Length);
            });
        }
    }
}
