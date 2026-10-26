using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceTransferBatchTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.batch", 1);
        private static readonly ReservationOwner Other = new ReservationOwner("proof.batch", 2);
        public static void Register()
        {
            TestRunner.Add("Resource transfer batches: whole-set admission, native ownership and canonical commits", Native);
            TestRunner.Add("Resource transfer batches: saved continuation, shared endpoints and provider fault boundaries", Durability);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
        private static IInventory Stock(SimGame game, string name = "source")
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == name) return entity.Get<IInventory>();
            throw new InvalidOperationException("missing authored batch endpoint");
        }
        private static EndpointPools Pools(SimGame game) => game.World.Services.Get<EndpointPools>();
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static ResourceId Resource(SimGame game, string name) => game.World.Content.ResourceId(name);
        private static ResourceFlowPage History(SimGame game, long cursor = 0) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, cursor);
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("batch Update: " + game.Fault); }
        private static ResourceTransferIntent Acquire(IResourceOutlet source, IResourceInlet destination, ResourceId resource,
            Fix amount, Fix? protectedAmount = null, ReservationOwner? owner = null)
        {
            Fix held = protectedAmount ?? amount;
            ReservationOwner requester = owner ?? Owner;
            return new ResourceTransferIntent(requester, Company, source.ReserveOut(requester, resource, held),
                destination.ReserveIn(requester, resource, held), resource, amount);
        }
        private static ResourceTransferIntent Change(ResourceTransferIntent intent, Fix? amount = null, ResourceId? resource = null,
            ReservationToken? source = null, ReservationToken? destination = null, PlayerId? company = null,
            ReservationOwner? owner = null, ResourceTransferKind? kind = null)
            => new ResourceTransferIntent(owner ?? intent.Owner, company ?? intent.Company, source ?? intent.Source,
                destination ?? intent.Destination, resource ?? intent.Resource, amount ?? intent.Amount, kind ?? intent.Kind);
        private static ResourceReservation Record(IResourceInlet endpoint, ReservationToken token)
        {
            foreach (ResourceReservation record in endpoint.Reservations) if (record.Token.Equals(token)) return record;
            throw new InvalidOperationException("missing actual batch receipt");
        }
        private static ResourceTransferIntent[] Mixed(SimGame game)
        {
            IInventory source = Stock(game); ResourceInventoryState first = Pools(game).Get(1), wide = Pools(game).Get(5000000000);
            ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
            var set = new[] {
                Acquire(source, first, steel, Fix.FromInt(3), Fix.FromInt(5)),
                Acquire(source, first, water, Fix.FromRatio(1, 2), Fix.FromInt(2)),
                Acquire(source, wide, steel, Fix.FromInt(2), owner: new ReservationOwner("proof.batch", 3)) };
            source.ReserveOut(Other, steel, Fix.One); first.ReserveIn(Other, water, Fix.One);
            return set;
        }
        private static void Refuse(Check c, SimGame game, IReadOnlyList<ResourceTransferIntent> set, ResourceTransferStatus status, string what)
        {
            ulong before = game.Hash(); long history = History(game).Latest;
            c.Equal(status, Transactions(game).EvaluateTransfers(set).Status, what + " pure all-leg admission");
            ResourceTransferBatchResult result = Transactions(game).CommitTransfers(set);
            c.Equal(status, result.Status, what + " fresh whole-set refusal");
            c.Equal(0, result.CommittedTransfers, what + " reports no committed prefix");
            c.Equal(before, game.Hash(), what + " changes no stock/book/version/token sequence");
            c.Equal(history, History(game).Latest, what + " publishes no partial history");
        }
        private static void Native(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory); ResourceTransferIntent[] set = Mixed(game);
            IResourceTransaction transaction = Transactions(game); IInventory source = Stock(game);
            ResourceInventoryState first = Pools(game).Get(1), wide = Pools(game).Get(5000000000);
            ResourceId steel = Resource(game, "steel"), water = Resource(game, "water"); ulong before = game.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "new protocol has an explicit engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "native endpoint codec remains unchanged");
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "native batches require no map, money or work scheduler");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)), "every generic outlet is still an inlet");
            c.That(transaction.EvaluateTransfers(set).Ready, "actual mixed count/fluid receipts admit together");
            c.Equal(before, game.Hash(), "ready preview is pure including endpoint versions and sequences");
            Refuse(c, game, null, ResourceTransferStatus.InvalidRequest, "null set");
            Refuse(c, game, Array.Empty<ResourceTransferIntent>(), ResourceTransferStatus.InvalidRequest, "empty set");
            Refuse(c, game, new[] { set[0], default }, ResourceTransferStatus.InvalidRequest, "malformed later leg");
            Refuse(c, game, new[] { set[0], set[0] }, ResourceTransferStatus.InvalidReceipt, "repeated receipt pair");
            Refuse(c, game, new[] { set[0], Change(set[1], source: set[0].Source) }, ResourceTransferStatus.InvalidReceipt, "shared outbound token");
            Refuse(c, game, new[] { set[0], Change(set[1], destination: set[0].Destination) }, ResourceTransferStatus.InvalidReceipt, "shared inbound token");
            Refuse(c, game, new[] { set[1], Change(set[2], amount: Fix.FromRatio(1, 2)) }, ResourceTransferStatus.InvalidRequest, "fractional discrete leg");
            Refuse(c, game, new[] { set[0], Change(set[2], amount: Fix.Zero) }, ResourceTransferStatus.InvalidRequest, "zero leg");
            Refuse(c, game, new[] { set[0], Change(set[2], amount: -Fix.One) }, ResourceTransferStatus.InvalidRequest, "negative leg");
            Refuse(c, game, new[] { set[0], Change(set[2], resource: water) }, ResourceTransferStatus.InvalidReceipt, "mismatched actual native receipt");
            Refuse(c, game, new[] { set[0], Change(set[2], amount: Fix.FromInt(3)) }, ResourceTransferStatus.InvalidReceipt, "overspent later receipt");
            Refuse(c, game, new[] { set[0], Change(set[2], owner: Other) }, ResourceTransferStatus.InvalidReceipt, "foreign claim owner");
            Refuse(c, game, new[] { set[0], Change(set[2], company: new PlayerId(1)) }, ResourceTransferStatus.InvalidRequest, "foreign company");
            Refuse(c, game, new[] { set[0], Change(set[2], company: new PlayerId(7)) }, ResourceTransferStatus.InvalidRequest, "undeclared company");
            Refuse(c, game, new[] { set[0], Change(set[2], kind: ResourceTransferKind.Supply) }, ResourceTransferStatus.Incompatible, "ordinary stock cannot masquerade as a supplier in mixed batch");
            Refuse(c, game, new[] { set[0], Change(set[2], kind: (ResourceTransferKind)77) }, ResourceTransferStatus.InvalidRequest, "unknown transfer kind");
            Refuse(c, game, new[] { set[0], Change(set[2], destination: new ReservationToken(new ResourceEndpointId("proof.pools", 99), 1)) },
                ResourceTransferStatus.MissingEndpoint, "missing later endpoint");
            Refuse(c, game, new[] { set[0], Change(set[2], destination: new ReservationToken(Pools(game).Get(3).Endpoint, 1)) },
                ResourceTransferStatus.InvalidRequest, "private foreign endpoint");
            Refuse(c, game, new[] { set[0], Change(set[2], destination: new ReservationToken(source.Endpoint, 77)) },
                ResourceTransferStatus.InvalidRequest, "same source/destination endpoint");
            int resolutions = Pools(game).ResolveCalls;
            c.That(transaction.EvaluateTransfers(set).Ready, "repeat admission resolves all actual scoped participants");
            c.Equal(2, Pools(game).ResolveCalls - resolutions, "shared endpoints resolve once per call, not once per leg");
            c.Equal(Fix.FromInt(3), first.ReservedIn(water), "whole native capacity protects both requested and unrelated inbound quantities");
            c.That(!first.CanReserveIn(Other, steel, Fix.FromInt(3)), "aggregate promises, not per-line spare capacity, constrain later claims");

            SimGame twin = factory.Load(game.Save()); var shuffled = (ResourceTransferIntent[])set.Clone(); Array.Reverse(shuffled);
            c.Equal(game.Hash(), twin.Hash(), "existing owned receipts restore before the first commit");
            ResourceTransferBatchResult result = transaction.CommitTransfers(set);
            c.That(result.Committed && result.CommittedTransfers == 3, "entire set commits exact native legs");
            c.That(Transactions(twin).CommitTransfers(shuffled).Committed, "shuffled caller order has the same current-state commit");
            c.Equal(wide.Endpoint, shuffled[0].Destination.Endpoint, "canonical ordering never reorders the caller's retained request array");
            c.Throws<InvalidOperationException>(() => game.Save(), "pending verified events must settle before checkpoint");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "canonical native mutations and event history are order-independent");
            c.Equal(Fix.FromInt(25), source.Amount(steel), "actual source loses the two steel legs only");
            c.Equal(Fix.FromRatio(9, 2), source.Amount(water), "native fractional fluid quantity remains exact");
            c.Equal(Fix.FromInt(3), first.Amount(steel), "first actual inlet receives three count units");
            c.Equal(Fix.FromRatio(1, 2), first.Amount(water), "first actual inlet receives a fluid half-unit");
            c.Equal(Fix.FromInt(2), wide.Amount(steel), "positive 64-bit endpoint is never narrowed to an entity");
            c.Equal(Fix.FromInt(2), Record(source, set[0].Source).Amount, "partial outbound retains its same token and two native units");
            c.Equal(Fix.FromInt(2), Record(first, set[0].Destination).Amount, "partial inbound retains the same token");
            c.Equal(Fix.FromRatio(3, 2), Record(source, set[1].Source).Amount, "fluid remainder is not rounded into a count");
            c.Equal(Fix.One, source.ReservedOut(steel) - Fix.FromInt(2), "unrelated source owner remains protected");
            c.Equal(Fix.FromRatio(5, 2), first.ReservedIn(water), "unrelated inlet owner survives partial transfer");
            ResourceFlowPage page = History(game, 2); c.Equal(3, page.Entries.Count, "one historical fact per verified leg after whole-set success");
            c.Equal(first.Endpoint, page.Entries[0].Item.Destination.Endpoint, "history follows canonical scoped endpoint order");
            c.Equal(wide.Endpoint, page.Entries[2].Item.Destination.Endpoint, "wide endpoint sorts after first pool without conversion");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw + page.Totals(steel).ConsumedRaw, "transfer never invents lifecycle stock");
            c.Equal(Fix.FromInt(30), source.Amount(steel) + first.Amount(steel) + wide.Amount(steel), "all native steel is conserved");
            c.Equal(Fix.FromInt(5), source.Amount(water) + first.Amount(water), "fluid stock is conserved separately");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status, "batch history remains company-private");
            Refuse(c, game, new[] { Change(set[0], amount: Fix.One), Change(set[1], amount: Fix.FromRatio(1, 2)), set[2] },
                ResourceTransferStatus.InvalidReceipt, "spent later token cannot recommit earlier surviving legs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "whole-set output and unspent receipts use the original sole codecs");
            StaleAndRefusing(c, factory);
            // Directly affected native quantum cases use the shared single-leg protocol, not a third suite.
            PartialReceiptTests.FluidsAndQuantum(c);
        }

        private static void StaleAndRefusing(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceTransferIntent[] set = Mixed(game);
            c.That(Transactions(game).EvaluateTransfers(set).Ready, "preview is ready before native claim changes");
            Pools(game).Get(5000000000).ReleaseIn(set[2].Owner, set[2].Destination);
            Refuse(c, game, set, ResourceTransferStatus.InvalidReceipt, "released later receipt invalidates a retained preview");
            foreach (bool inlet in new[] { false, true })
            {
                game = New(factory); set = Mixed(game); c.That(Transactions(game).EvaluateTransfers(set).Ready, "current batch starts admitted");
                if (!inlet)
                {
                    // The entity source cannot be instrumented by the scoped provider; use a real supplied pool instead.
                    game = New(factory);
                    SeedPools(game); ResourceId steel = Resource(game, "steel");
                    set = new[] { Acquire(Pools(game).Get(1), Stock(game, "destination"), steel, Fix.One),
                        Acquire(Pools(game).Get(2), Pools(game).Get(5000000000), steel, Fix.One) };
                }
                Pools(game).FaultEndpoint = inlet ? 5000000000 : 2;
                Pools(game).ClaimFault = inlet ? ClaimFaultMode.RefusingIn : ClaimFaultMode.RefusingOut;
                Refuse(c, game, set, inlet ? ResourceTransferStatus.NoCapacity : ResourceTransferStatus.NoStock, "current later provider blocker");
            }
        }
        private static void SeedPools(SimGame game)
        {
            IInventory source = Stock(game); ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
            var transfers = new List<ResourceTransferIntent>();
            foreach (long identity in new[] { 1L, 2L })
            {
                var requester = new ReservationOwner("proof.batch.seed", identity);
                transfers.Add(Acquire(source, Pools(game).Get(identity), steel, Fix.FromInt(8), owner: requester));
                transfers.Add(Acquire(source, Pools(game).Get(identity), water, Fix.One, owner: requester));
            }
            if (!Transactions(game).CommitTransfers(transfers).Committed) throw new InvalidOperationException("actual pool supply refused");
            Tick(game);
        }
        private static void Durability(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory), independent = New(factory);
            ulong independentHash = independent.Hash(); ResourceTransferIntent[] set = Mixed(game);
            game.Seat(0).Submit(new TransferProbeCommand { Intents = set });
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending full-set command restores its actual scoped receipt requests");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "ordinary Update commits one batch identically after load");
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Count == 1 && results[0].Accepted, "one ordinary command reports only complete batch success");
            c.Equal(independentHash, independent.Hash(), "interleaved sessions share no stock, promises, order or history");
            var remaining = new[] { Change(set[0], amount: Fix.FromInt(2)), Change(set[1], amount: Fix.FromRatio(3, 2)) };
            game.Seat(0).Submit(new TransferProbeCommand { Intents = remaining }); twin = factory.Load(game.Save());
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "partially spent native tokens finish once through a saved next command");
            c.Equal(Fix.FromInt(23), Stock(game).Amount(Resource(game, "steel")), "saved second batch consumes only its two native count units");
            c.Equal(Fix.FromInt(3), Stock(game).Amount(Resource(game, "water")), "saved second batch consumes only its native fluid remainder");
            c.Equal(5, History(game, 2).Entries.Count, "two commands produce three then two verified native facts");
            c.Equal(Fix.One, Pools(game).Get(1).ReservedIn(Resource(game, "water")), "unrelated inbound promise survives batch completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed batch remains a normal durable world checkpoint");
            SharedEndpoints(c, factory); SingleParity(c, factory); Faults(c, factory);
            // Paid supplier coverage belongs to its separately registered receipt-provider
            // case. This literal exercises physical batch durability, not hidden extra suites.
        }
        private static void SharedEndpoints(Check c, GameFactory factory)
        {
            SimGame game = New(factory); SeedPools(game); ResourceId steel = Resource(game, "steel");
            ResourceInventoryState first = Pools(game).Get(1), second = Pools(game).Get(2), wide = Pools(game).Get(5000000000);
            var set = new[] { Acquire(first, second, steel, Fix.One), Acquire(second, wide, steel, Fix.FromInt(2)) };
            SimGame twin = factory.Load(game.Save()); ulong before = game.Hash();
            c.That(Transactions(game).EvaluateTransfers(set).Ready, "an intermediate endpoint has its own pre-existing protected outbound stock");
            c.Equal(before, game.Hash(), "shared participant admission is pure");
            c.That(Transactions(game).CommitTransfers(set).Committed, "one shared checkpoint advances through both inlet and outlet roles");
            Array.Reverse(set); c.That(Transactions(twin).CommitTransfers(set).Committed, "shared endpoint commit ignores caller leg order");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "shared participant continuation remains deterministic");
            c.Equal(Fix.FromInt(7), first.Amount(steel), "first stock loses one actual native unit");
            c.Equal(Fix.FromInt(7), second.Amount(steel), "intermediate stock receives one then sends two actual units");
            c.Equal(Fix.FromInt(2), wide.Amount(steel), "final endpoint receives exactly its committed quantity");
            c.Equal(Fix.FromInt(9), History(game, 6).Entries[1].Item.SourceBefore.Value, "later historical leg uses the intermediate owner's then-current actual stock");
            c.Equal(0, first.Reservations.Count + second.Reservations.Count + wide.Reservations.Count, "each actual intermediate receipt is consumed exactly once");
        }
        private static void SingleParity(Check c, GameFactory factory)
        {
            SimGame game = New(factory); ResourceTransferIntent intent = Mixed(game)[0]; SimGame twin = factory.Load(game.Save());
            ResourceTransferCheck check = Transactions(game).Evaluate(intent);
            c.That(check.Ready && Transactions(twin).EvaluateTransfers(new[] { intent }).Ready, "single and set admission use the same actual protocol");
            c.Equal(Stock(game).Version, check.SourceVersion, "single admission still exposes the actual source checkpoint");
            ResourceTransferResult single = Transactions(game).Commit(intent);
            c.That(single.Committed && single.Amount == Fix.FromInt(3), "single API returns only its actual committed quantity");
            c.That(Transactions(twin).CommitTransfers(new[] { intent }).Committed, "one-leg set uses identical receipt mutation");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "no parallel single-transfer implementation or event path survives");
        }
        private static void Faults(Check c, GameFactory factory)
        {
            foreach (ClaimFaultMode mode in new[] { ClaimFaultMode.ShortTake, ClaimFaultMode.EraseRemainder, ClaimFaultMode.OtherStock,
                ClaimFaultMode.OtherReceipt, ClaimFaultMode.MutatingCheck, ClaimFaultMode.SilentVersion,
                ClaimFaultMode.ShortReceive, ClaimFaultMode.EraseInletRemainder, ClaimFaultMode.OtherEndpoint,
                ClaimFaultMode.MutatingOtherEndpointCheck })
            {
                SimGame game = New(factory); SeedPools(game); ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
                ResourceInventoryState first = Pools(game).Get(1), second = Pools(game).Get(2), wide = Pools(game).Get(5000000000);
                var set = new[] { Acquire(first, Stock(game, "destination"), steel, Fix.One), Acquire(second, wide, steel, Fix.One, Fix.FromInt(4)) };
                second.ReserveOut(Other, steel, Fix.One); second.ReserveIn(Other, water, Fix.One);
                bool inlet = mode == ClaimFaultMode.ShortReceive || mode == ClaimFaultMode.EraseInletRemainder;
                var history = History(game); long latest = history.Latest; int entries = history.Entries.Count;
                Pools(game).ClaimFault = mode; Pools(game).FaultEndpoint = inlet ? 5000000000 : 2;
                game.Seat(0).Submit(new TransferProbeCommand { Intents = set }); game.Update();
                c.That(game.Fault != null, mode + " faults normal command/Update, not a partial success");
                c.Throws<InvalidOperationException>(() => History(game), mode + " faulted partial batch exposes no live success history");
                c.That(history.Latest == latest && history.Entries.Count == entries, mode + " retained last-healthy history is immutable");
                c.Throws<InvalidOperationException>(() => game.Save(), mode + " partial provider mutation cannot be saved");
                c.Throws<InvalidOperationException>(() => game.Hash(), mode + " faulted partial state cannot masquerade as a settled hash");
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.Equal(0, results.Count, mode + " cannot report accepted command completion");
                if (mode != ClaimFaultMode.MutatingCheck && mode != ClaimFaultMode.MutatingOtherEndpointCheck)
                    c.Equal(Fix.One, Stock(game, "destination").Amount(steel), mode + " retains real earlier mutation without fabricated compensation");
            }
        }
    }
}
