using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceEndpointTests
    {
        public static void Register() => TestRunner.Add("Resource endpoints: scoped providers, pure commits and durable inventory owners", Endpoints);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
        private static Entity Find(SimGame game, string prototype)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing endpoint fixture " + prototype);
        }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("endpoint tick: " + game.Fault); }
        private static ResourceFlowPage History(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static ResourceTransferIntent Acquire(IResourceOutlet source, IResourceInlet destination,
            ResourceId resource, Fix amount, long identity, int company = 0)
        {
            var owner = new ReservationOwner("proof.transfer", identity);
            return new ResourceTransferIntent(owner, new PlayerId(company), source.ReserveOut(owner, resource, amount),
                destination.ReserveIn(owner, resource, amount), resource, amount);
        }
        private static void Release(IResourceOutlet source, IResourceInlet destination, ResourceTransferIntent intent)
        { source.ReleaseOut(intent.Owner, intent.Source); destination.ReleaseIn(intent.Owner, intent.Destination); }
        private static void Endpoints(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory), independent = New(factory);
            World world = game.World; EndpointPools pools = world.Services.Get<EndpointPools>();
            IInventory source = Find(game, "source").Get<IInventory>(), destination = Find(game, "destination").Get<IInventory>();
            ResourceInventoryState first = pools.Get(1), second = pools.Get(2);
            ResourceId steel = world.Content.ResourceId("steel"), water = world.Content.ResourceId("water");
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            c.That(world.Space == null && !world.Services.Has<WorkBoard>(), "non-entity endpoints impose no spatial or worker subsystem");
            c.Equal(1, world.Services.Providers<IResourceEndpointProvider>().Count, "declared service aliases resolve once without a parallel registry");
            c.That(new List<IResourceEndpointProvider>(world.Services.Providers<IResourceEndpointProvider>()).Contains(pools), "endpoint discovery uses the same declared persistent owner, not a singular interface service");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)) && typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)), "every generic and entity outlet is also an inlet");
            c.That(!typeof(IEntityCapability).IsAssignableFrom(typeof(IResourceOutlet)), "pools do not masquerade as entity capabilities");
            c.Equal(Fix.Zero, first.Amount(steel), "authored pool starts empty rather than copying or inventing stock");
            c.Equal(2, History(game).Entries.Count, "only actual authored entity initialization emits opening facts");
            c.Equal(game.Hash(), independent.Hash(), "separate sessions begin identically with distinct owners");
            c.That(first.Endpoint != source.Endpoint && first.Endpoint.Identity == source.Endpoint.Identity, "provider scopes distinguish equal numeric identities");
            c.Equal(new ResourceEndpointId("proof.pools", 5000000000), pools.Get(5000000000).Endpoint, "non-entity IDs are not narrowed to entity integers");
            c.Throws<InvalidOperationException>(() => first.Endpoint.ToEntity(), "a pool cannot silently convert into an entity");
            c.Equal(Find(game, "source").Id, source.Endpoint.ToEntity(), "explicit entity identity uses the reserved scope");
            IdentityCodec(c);

            ResourceTransferIntent inbound = Acquire(source, first, steel, Fix.FromInt(7), 1);
            ulong before = game.Hash(); ResourceTransferCheck preview = transactions.Evaluate(inbound);
            c.That(preview.Ready, "existing entity stock and non-entity admission share one receipt protocol");
            c.Equal(before, game.Hash(), "scoped preflight changes no stock, receipt, sequence or version");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending entity-to-pool receipts are checkpoint-native");
            c.That(transactions.Commit(inbound).Committed, "current-state commit moves actual entity stock to the pool");
            c.Equal(Fix.FromInt(23), source.Amount(steel), "sole entity owner loses exactly seven native units");
            c.Equal(Fix.FromInt(7), first.Amount(steel), "sole pool owner receives exactly seven native units");
            c.Equal(0, source.Reservations.Count + first.Reservations.Count, "both actual receipts are consumed once");
            c.Throws<InvalidOperationException>(() => game.Save(), "deferred history must drain before saving verified transfers");
            Tick(game); ResourceFlowPage page = History(game, 2); ResourceFlowFact fact = page.Entries[0].Item;
            c.Equal(1, page.Entries.Count, "one physical leg emits one historical fact");
            c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "movement is not disguised as consumption or creation");
            c.Equal(source.Endpoint, fact.Source.Endpoint, "history contains the actual entity source scope");
            c.Equal(first.Endpoint, fact.Destination.Endpoint, "history contains the actual pool destination scope");
            c.Equal(Fix.FromInt(30), fact.SourceBefore.Value, "historical source checkpoint is real");
            c.Equal(Fix.FromInt(23), fact.SourceAfter.Value, "historical source delta is exact");
            c.Equal(Fix.FromInt(7), fact.DestinationAfter, "historical destination delta is exact");
            c.That(fact.SourceVersionAfter > fact.SourceVersionBefore && fact.DestinationVersionAfter > fact.DestinationVersionBefore,
                "both actual owner versions advance");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw + page.Totals(steel).ConsumedRaw, "transfer accounting fabricates no lifecycle facts");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status, "scoped history remains company-private");
            before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidReceipt, transactions.Commit(inbound).Status, "spent scoped receipts cannot repeat");
            c.Equal(before, game.Hash(), "repeat refusal is pure");

            ResourceTransferIntent waterIn = Acquire(source, first, water, Fix.FromRatio(5, 2), 2);
            c.That(transactions.Commit(waterIn).Committed, "continuous resources share the same owner and exact protocol"); Tick(game);
            c.Equal(Fix.FromRatio(5, 2), first.Amount(water), "continuous native stock retains its fraction");
            before = game.Hash();
            c.That(!first.CanReserveIn(new ReservationOwner("proof.transfer", 9), steel, Fix.One), "whole-vector admission respects shared mixed capacity");
            c.That(!first.CanReserveOut(new ReservationOwner("proof.transfer", 9), steel, Fix.FromRatio(1, 2)), "discrete native promises cannot be fractional");
            c.Equal(before, game.Hash(), "capacity and quantity refusals are pure");

            ResourceTransferIntent internalMove = Acquire(first, second, steel, Fix.FromInt(3), 3);
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "live pool-to-pool promises and private history have one durable codec");
            int resolveCalls = pools.ResolveCalls;
            c.That(transactions.Commit(internalMove).Committed, "two non-entity endpoints move through the existing transaction owner");
            c.Equal(2, pools.ResolveCalls - resolveCalls, "commit validates and mutates the same resolved owners without a second lookup substitution");
            c.That(twin.World.Services.Get<IResourceTransaction>().Commit(internalMove).Committed, "restored scoped tokens refer to restored real owners");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved pool transfer continues identically");
            c.Equal(Fix.FromInt(4), first.Amount(steel), "old pool retains its actual remainder");
            c.Equal(Fix.FromInt(3), second.Amount(steel), "new pool owns the moved quantity");
            ResourceTransferIntent outbound = Acquire(second, destination, steel, Fix.FromInt(3), 4);
            twin = factory.Load(game.Save());
            c.That(transactions.Commit(outbound).Committed, "pool-to-entity delivery uses no raw stock copy");
            twin.World.Services.Get<IResourceTransaction>().Commit(outbound); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved pool-to-entity continuation is deterministic");
            c.Equal(Fix.FromInt(30), source.Amount(steel) + first.Amount(steel) + second.Amount(steel) + destination.Amount(steel), "all physical legs conserve actual total stock");
            c.Equal(BigInteger.Zero, History(game).Totals(steel).CreatedRaw + History(game).Totals(steel).ConsumedRaw, "all scoped legs remain transfers");
            c.Equal(4, History(game, 2).Entries.Count, "each physical leg is recorded exactly once");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed owners/history checkpoint identically");

            ResourceTransferIntent foreign = Acquire(source, pools.Get(3), steel, Fix.One, 5);
            before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.Evaluate(foreign).Status, "private foreign pool cannot receive another company's transfer");
            c.Equal(ResourceTransferStatus.InvalidRequest, transactions.Commit(foreign).Status, "execution uses the same ownership refusal");
            c.Equal(before, game.Hash(), "foreign refusal spends no receipt or stock"); Release(source, pools.Get(3), foreign);
            ResourceTransferIntent pending = Acquire(first, destination, steel, Fix.One, 6);
            var absent = new ReservationToken(new ResourceEndpointId("proof.absent", 1), 1);
            var missing = new ResourceTransferIntent(pending.Owner, pending.Company, absent, pending.Destination, steel, Fix.One);
            before = game.Hash();
            c.Equal(ResourceTransferStatus.MissingEndpoint, transactions.Commit(missing).Status, "unbound scope has an explicit missing-endpoint refusal");
            c.Equal(before, game.Hash(), "missing scope does not mutate surviving promises");
            var impostor = new ResourceTransferIntent(pending.Owner, pending.Company,
                new ReservationToken(second.Endpoint, pending.Source.Sequence), pending.Destination, steel, Fix.One);
            c.Equal(ResourceTransferStatus.InvalidReceipt, transactions.Commit(impostor).Status, "numeric receipt sequence cannot substitute another endpoint");
            c.Equal(before, game.Hash(), "wrong scoped token leaves real source protected");
            pools.ReturnWrongIdentity = true;
            c.Throws<InvalidOperationException>(() => transactions.Evaluate(pending), "provider may not substitute a different endpoint identity");
            pools.ReturnWrongIdentity = false;
            c.Equal(before, game.Hash(), "invalid resolver result publishes no effect or fabricated rollback");
            Release(first, destination, pending);
            Tick(independent);
            c.Equal(Fix.FromInt(30), Find(independent, "source").Get<IInventory>().Amount(steel), "interleaved session retains its independent entity stock");
            c.Equal(Fix.Zero, independent.World.Services.Get<EndpointPools>().Get(1).Amount(steel), "interleaved pool state does not leak between sessions");
            Composition(c, world);
        }
        private static void Composition(Check c, World foreignWorld)
        {
            Action<EndpointProofModule> reject = module => c.Throws<ContentException>(() => New(EndpointGame.Factory(module)), "invalid provider composition refuses before simulation");
            string content = FrameworkProofContent.Read("endpoints.json");
            c.Throws<ContentException>(() => New(EndpointGame.Factory(new EndpointProofModule(ResourceEndpointId.EntityScope),
                content.Replace("5000000000", "4"))), "reserved entity scope is refused even with representable endpoint IDs");
            reject(new EndpointProofModule("unowned.pools"));
            reject(new EndpointProofModule(duplicate: true));
            reject(new EndpointProofModule(reportedWorld: foreignWorld));
            c.Throws<ContentException>(() => New(EndpointGame.Factory(content: content.Replace("\"identity\":2", "\"identity\":1"))), "duplicate authored endpoint identity cannot create two owners");
            c.That(New(EndpointGame.Factory(new EndpointProofModule("proof.pools.child"))).Fault == null, "module-owned child scopes are valid ordinary providers");
        }
        private static void IdentityCodec(Check c)
        {
            foreach (ResourceEndpointId original in new[] { default(ResourceEndpointId), ResourceEndpointId.ForEntity(new EntityId(1)), new ResourceEndpointId("proof.pools", 5000000000) })
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); ResourceEndpointId value = original; ResourceEndpointId.Sync(writer, ref value); writer.Flush();
                    byte[] bytes = stream.ToArray(); value = default; ResourceEndpointId.Sync(new StateReader(bytes, 0, bytes.Length), ref value);
                    c.Equal(original, value, "endpoint codec preserves scoped wide identity and canonical absence");
                }
            }
            foreach (ResourceEndpointId invalid in new[] { new ResourceEndpointId("proof.pools", 0), new ResourceEndpointId(null, 1), new ResourceEndpointId("proof.pools", -1), new ResourceEndpointId(ResourceEndpointId.EntityScope, 5000000000) })
                c.Throws<CorruptStateException>(() =>
                {
                    using (var stream = new MemoryStream())
                    { var writer = new StateWriter(stream); ResourceEndpointId value = invalid; ResourceEndpointId.Sync(writer, ref value); }
                }, "invalid scoped IDs have no silent default or compatibility conversion");
        }
    }
}
