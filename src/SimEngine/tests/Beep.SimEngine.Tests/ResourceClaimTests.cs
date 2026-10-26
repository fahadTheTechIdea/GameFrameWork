using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceClaimTests
    {
        public static void Register()
        {
            TestRunner.Add("Resource claims: canonical whole-set admission, native quantities and ownership", Admission);
            TestRunner.Add("Resource claims: saved exact consumption, release and provider fault boundaries", Durability);
        }
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Cause = new Code("proof.activity-inputs");
        private static ReservationOwner Owner(long id) => new ReservationOwner("proof.activity", id);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
        private static IInventory Source(SimGame game)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "source") return entity.Get<IInventory>();
            throw new InvalidOperationException("missing authored source");
        }
        private static ResourceId Resource(SimGame game, string name) => game.World.Content.ResourceId(name);
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static EndpointPools Pools(SimGame game) => game.World.Services.Get<EndpointPools>();
        private static ResourceFlowPage History(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, cursor);
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("claim tick fault: " + game.Fault); }
        internal static void Seed(SimGame game)
        {
            IInventory source = Source(game); long sequence = 100;
            foreach (var entry in new[] { (1L, "steel", Fix.FromInt(4)), (1L, "water", Fix.FromInt(2)),
                (2L, "steel", Fix.FromInt(3)), (5000000000L, "steel", Fix.FromInt(2)) })
            {
                ResourceInventoryState pool = Pools(game).Get(entry.Item1); ResourceId resource = Resource(game, entry.Item2);
                ReservationOwner owner = new ReservationOwner("proof.seed", sequence++);
                var intent = new ResourceTransferIntent(owner, Company, source.ReserveOut(owner, resource, entry.Item3),
                    pool.ReserveIn(owner, resource, entry.Item3), resource, entry.Item3);
                if (!Transactions(game).Commit(intent).Committed) throw new InvalidOperationException("physical seed transfer refused");
            }
            Tick(game);
        }
        private static ResourceClaimIntent Intent(SimGame game, long endpoint, long owner, string resource, Fix amount)
            => new ResourceClaimIntent(ReservationDirection.Outbound, Owner(owner), Pools(game).Get(endpoint).Endpoint, BillOf(Resource(game, resource), amount));
        private static Bill BillOf(ResourceId resource, Fix amount) => new Bill(new[] { new ResourceAmount(resource, amount) });
        private static ResourceClaimIntent[] Set(SimGame game)
        {
            ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
            return new[]
            {
                Intent(game, 5000000000, 21, "steel", Fix.One),
                new ResourceClaimIntent(ReservationDirection.Outbound, Owner(22), Pools(game).Get(1).Endpoint,
                    new Bill(new[] { new ResourceAmount(water, Fix.FromRatio(1, 2)), new ResourceAmount(steel, Fix.One) })),
                new ResourceClaimIntent(ReservationDirection.Outbound, Owner(21), Source(game).Endpoint, BillOf(water, Fix.One)),
                Intent(game, 2, 21, "steel", Fix.FromInt(2)), Intent(game, 1, 21, "steel", Fix.FromInt(2))
            };
        }
        private static ResourceReservation[] Copy(IReadOnlyList<ResourceReservation> receipts)
        { var result = new ResourceReservation[receipts.Count]; for (int i = 0; i < result.Length; i++) result[i] = receipts[i]; return result; }
        private static void Refusal(Check c, SimGame game, ResourceClaimIntent[] intents, ResourceTransferStatus status, string what, PlayerId? company = null)
        {
            ulong before = game.Hash(); IResourceTransaction transaction = Transactions(game); PlayerId seat = company ?? Company;
            c.Equal(status, transaction.EvaluateClaims(seat, intents, System.Array.Empty<ResourceReservation>()).Status, what + " pure admission");
            ResourceClaimResult result = transaction.AcquireClaims(seat, intents, System.Array.Empty<ResourceReservation>());
            c.Equal(status, result.Status, what + " current-state acquisition");
            c.Equal(0, result.Receipts.Count, what + " has no partial receipts");
            c.Equal(before, game.Hash(), what + " mutates no endpoint/book/version/sequence");
        }
        private static void ReceiptRefusal(Check c, SimGame game, ResourceReservation[] receipts, ResourceTransferStatus status, string what)
        {
            ulong before = game.Hash(); IResourceTransaction transaction = Transactions(game);
            c.Equal(status, transaction.EvaluateClaimReceipts(Company, receipts).Status, what + " pure receipt check");
            c.Equal(status, transaction.ConsumeClaims(Company, receipts, Cause, System.Array.Empty<ResourceReservation>()).Status, what + " spending refusal");
            c.Equal(status, transaction.ReleaseClaims(new ResourceClaimReleaseIntent(Company, receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Status, what + " release refusal");
            c.Equal(before, game.Hash(), what + " leaves every surviving promise/stock unchanged");
        }
        private static void Admission(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory); Seed(game);
            IResourceTransaction transaction = Transactions(game); ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "generic resource claims require no map or work scheduler");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)), "the inlet/outlet contract invariant is preserved");
            ResourceClaimIntent[] intents = Set(game); ulong before = game.Hash();
            c.That(transaction.EvaluateClaims(Company, intents, System.Array.Empty<ResourceReservation>()).Ready, "one set spans actual entity/scoped/wide endpoints and different native units");
            c.Equal(before, game.Hash(), "successful preview is pure including token sequences");
            Refusal(c, game, Array.Empty<ResourceClaimIntent>(), ResourceTransferStatus.InvalidRequest, "empty set");
            Refusal(c, game, null, ResourceTransferStatus.InvalidRequest, "null set");
            Refusal(c, game, new[] { default(ResourceClaimIntent) }, ResourceTransferStatus.InvalidRequest, "missing intent fields");
            var probe = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner(1), Pools(game).Get(1).Endpoint, Bill.Empty) };
            c.That(transaction.EvaluateClaims(Company, probe, System.Array.Empty<ResourceReservation>()).Ready, "empty bill is a pure endpoint-access probe, not a stock claim");
            c.Equal(ResourceTransferStatus.InvalidRequest, transaction.AcquireClaims(Company, probe, System.Array.Empty<ResourceReservation>()).Status, "empty probe cannot acquire a promise");
            c.Equal(before, game.Hash(), "zero-quantity access preserves actual books/stock/sequences");
            Refusal(c, game, new[] { Intent(game, 1, 1, "steel", Fix.FromRatio(1, 2)) }, ResourceTransferStatus.InvalidRequest, "fractional discrete quantity");
            Refusal(c, game, new[] { Intent(game, 1, 1, "steel", Fix.FromInt(3)), Intent(game, 1, 2, "steel", Fix.FromInt(2)) },
                ResourceTransferStatus.NoStock, "aggregate distinct-owner shortage");
            Refusal(c, game, new[] { Intent(game, 1, 1, "steel", Fix.One), Intent(game, 2, 2, "steel", Fix.FromInt(4)) },
                ResourceTransferStatus.NoStock, "later endpoint shortage");
            ResourceClaimIntent duplicate = Intent(game, 1, 1, "steel", Fix.One);
            Refusal(c, game, new[] { duplicate, duplicate }, ResourceTransferStatus.InvalidRequest, "duplicate endpoint-owner-resource");
            Refusal(c, game, new[] { duplicate, new ResourceClaimIntent(ReservationDirection.Outbound, Owner(1), new ResourceEndpointId("proof.absent", 1), BillOf(steel, Fix.One)) },
                ResourceTransferStatus.MissingEndpoint, "missing provider");
            Refusal(c, game, new[] { duplicate, Intent(game, 3, 2, "steel", Fix.One) }, ResourceTransferStatus.InvalidRequest, "foreign-company endpoint");
            Refusal(c, game, intents, ResourceTransferStatus.InvalidRequest, "unseated company", new PlayerId(99));
            Refusal(c, game, intents, ResourceTransferStatus.InvalidRequest, "neutral caller cannot claim private stock", PlayerId.None);
            Refusal(c, game, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner(1), Source(game).Endpoint, BillOf(new ResourceId(999), Fix.One)) },
                ResourceTransferStatus.InvalidRequest, "unknown native resource");
            var filteredContent = Json.Parse(FrameworkProofContent.Read("endpoints.json"));
            filteredContent.Get("resource_pools")[0].Set("resources", Json.Parse("[\"steel\"]"));
            SimGame filtered = New(EndpointGame.Factory(content: filteredContent.ToString()));
            c.That(!Pools(filtered).Get(1).Accepts(filtered.World.Content, Resource(filtered, "water")), "authored source filter actually excludes the requested native resource");
            Refusal(c, filtered, new[] { Intent(filtered, 1, 1, "water", Fix.FromRatio(1, 2)) },
                ResourceTransferStatus.Incompatible, "authored source filter");

            // A retained ready preview is not authority after another owner acquires the stock.
            var competitor = new ReservationOwner("proof.other", 1);
            ReservationToken held = Pools(game).Get(1).ReserveOut(competitor, steel, Fix.FromInt(4));
            Refusal(c, game, intents, ResourceTransferStatus.NoStock, "changed stock promises after preview");
            Pools(game).Get(1).ReleaseOut(competitor, held);
            SimGame twin = factory.Load(game.Save()); ResourceClaimIntent[] shuffled = Set(twin); Array.Reverse(shuffled);
            ResourceClaimResult acquired = transaction.AcquireClaims(Company, intents, System.Array.Empty<ResourceReservation>());
            ResourceClaimResult other = Transactions(twin).AcquireClaims(Company, shuffled, System.Array.Empty<ResourceReservation>());
            c.That(acquired.Committed && other.Committed, "compatible complete sets acquire through existing endpoint books");
            c.Equal(6, acquired.Receipts.Count, "every native bill line returns exactly its acquired receipt");
            c.Equal(game.Hash(), twin.Hash(), "shuffled intent arrival produces identical actual token/version/hash state");
            c.Equal(Fix.FromInt(4), Pools(game).Get(1).Amount(steel), "acquisition does not consume source stock");
            c.Equal(Fix.One, Pools(game).Get(1).Available(steel), "distinct owners share stock only through their actual promises");
            c.Equal(Fix.FromRatio(3, 2), Pools(game).Get(1).Available(water), "continuous native demand keeps exact fractions");
            c.Equal(6, History(game).Entries.Count, "promising emits no fictitious consumption history");
            c.Throws<NotSupportedException>(() => ((IList<ResourceReservation>)acquired.Receipts)[0] = default, "returned records cannot be mutated");
            for (int i = 0; i < acquired.Receipts.Count; i++)
            {
                ResourceReservation receipt = acquired.Receipts[i];
                c.That(receipt.Token.Equals(other.Receipts[i].Token) && receipt.Owner.Equals(other.Receipts[i].Owner) &&
                    receipt.Resource == other.Receipts[i].Resource && receipt.Amount == other.Receipts[i].Amount,
                    "receipt order is canonical across native endpoint/owner/resource identities");
                c.Equal(ReservationDirection.Outbound, receipt.Direction, "claims are owned outbound promises, never anonymous balances");
            }
            Refusal(c, game, new[] { Intent(game, 1, 21, "steel", Fix.One) }, ResourceTransferStatus.InvalidReceipt, "existing owner cannot duplicate its live claim");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "endpoint books remain the sole saved claim authority");
        }
        private static void Durability(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory); Seed(game);
            ResourceId steel = Resource(game, "steel"), water = Resource(game, "water");
            ResourceInventoryState first = Pools(game).Get(1);
            var unrelated = new ReservationOwner("proof.other", 7);
            ReservationToken tool = first.ReserveOut(unrelated, steel, Fix.One);
            ReservationToken incoming = first.ReserveIn(unrelated, water, Fix.FromRatio(1, 2));
            ResourceClaimResult claims = Transactions(game).AcquireClaims(Company, Set(game), System.Array.Empty<ResourceReservation>());
            c.That(claims.Committed, "complete request fits around another owner's tool/incoming promises");
            ResourceReservation[] records = Copy(claims.Receipts); ResourceReservation original = records[0];
            ResourceReservation[] changed = Copy(records);
            changed[changed.Length - 1] = new ResourceReservation(records[records.Length - 1].Token, Owner(999),
                records[records.Length - 1].Resource, records[records.Length - 1].Amount, ReservationDirection.Outbound);
            ReceiptRefusal(c, game, changed, ResourceTransferStatus.InvalidReceipt, "later wrong owner");
            ReceiptRefusal(c, game, new[] { original, original }, ResourceTransferStatus.InvalidReceipt, "duplicate token");
            ReceiptRefusal(c, game, new[] { new ResourceReservation(original.Token, original.Owner, original.Resource, original.Amount + Fix.One,
                ReservationDirection.Outbound) }, ResourceTransferStatus.InvalidReceipt, "wrong exact amount");
            ReceiptRefusal(c, game, new[] { new ResourceReservation(original.Token, original.Owner, original.Resource, original.Amount,
                ReservationDirection.Inbound) }, ResourceTransferStatus.InvalidReceipt, "wrong direction");
            ReceiptRefusal(c, game, new[] { new ResourceReservation(new ReservationToken(new ResourceEndpointId("proof.absent", 1), 1),
                original.Owner, original.Resource, original.Amount, ReservationDirection.Outbound) }, ResourceTransferStatus.MissingEndpoint, "removed endpoint");
            ReceiptRefusal(c, game, Array.Empty<ResourceReservation>(), ResourceTransferStatus.InvalidRequest, "empty receipt set");
            ulong before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidRequest, Transactions(game).ConsumeClaims(Company, records, Code.None, System.Array.Empty<ResourceReservation>()).Status, "consumption must declare a stable cause");
            c.Equal(ResourceTransferStatus.InvalidRequest, Transactions(game).ConsumeClaims(new PlayerId(1), records, Cause, System.Array.Empty<ResourceReservation>()).Status, "receipt possession does not override company ownership");
            c.Equal(before, game.Hash(), "cause/company refusal does not spend any promise");
            c.That(Transactions(game).EvaluateClaimReceipts(Company, records).Ready, "actual complete native receipt set is consumable");
            c.Equal(before, game.Hash(), "receipt admission is pure");
            SimGame twin = factory.Load(game.Save()); ResourceReservation[] reversed = Copy(records); Array.Reverse(reversed);
            c.Equal(before, twin.Hash(), "pending claims save with normal endpoint state, not another claim manager");
            c.That(Transactions(game).ConsumeClaims(Company, records, Cause, System.Array.Empty<ResourceReservation>()).Committed, "actual complete receipt set consumes");
            c.That(Transactions(twin).ConsumeClaims(Company, reversed, Cause, System.Array.Empty<ResourceReservation>()).Committed, "restored shuffled receipts consume the same owners");
            c.Throws<InvalidOperationException>(() => game.Save(), "deferred committed history must settle before checkpointing");
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved/shuffled consumption yields the same stock, promises, versions and history");
            c.Equal(Fix.One, first.Amount(steel), "exact consumption leaves only the protected other-owner tool");
            c.Equal(Fix.Zero, first.Available(steel), "other owner's retained tool remains unavailable");
            c.Equal(Fix.FromRatio(3, 2), first.Amount(water), "fractional fluid spending is native and exact");
            c.Equal(2, first.Reservations.Count, "consumption preserves both unrelated directions");
            c.That(first.CheckReservedOut(unrelated, tool).Ready && first.CheckReservedIn(game.World.Content, unrelated, incoming).Ready,
                "unrelated exact tokens still resolve after the whole-set consumption");
            ResourceFlowPage page = History(game, 6);
            c.Equal(6, page.Entries.Count, "one actual consumption fact per committed native line");
            for (int i = 0; i < records.Length; i++)
            {
                ResourceFlowFact fact = page.Entries[i].Item;
                c.That(fact.Kind == ResourceFlowKind.Consumed && !fact.IsTransfer && fact.Cause == Cause, "consumption is not disguised as transport or production");
                c.Equal(records[i].Token, fact.Source, "historical source is the actual spent outbound receipt");
                c.Equal(records[i].Token.Endpoint, fact.Endpoint, "history retains actual entity/scoped/wide endpoint identity");
                c.Equal(records[i].Owner, fact.Owner, "history retains the actual commitment owner");
                c.Equal(records[i].Amount.Raw, fact.SourceBefore.Value.Raw - fact.SourceAfter.Value.Raw, "history contains the exact verified native stock delta");
                c.That(fact.SourceVersionAfter > fact.SourceVersionBefore && !fact.Destination.IsValid, "owned consumption advances its true owner without inventing a destination");
            }
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), page.Totals(steel).ConsumedRaw, "count totals use native steel alone");
            c.Equal(new BigInteger(Fix.FromRatio(3, 2).Raw), page.Totals(water).ConsumedRaw, "volume totals do not add heterogeneous dimensions");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw, "spending never creates stock");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status, "all scoped consumption history is company-private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "schema4 owns consumed scoped facts through one normal journal codec");
            ReceiptRefusal(c, game, records, ResourceTransferStatus.InvalidReceipt, "already spent receipt set");

            ResourceClaimIntent[] next = { Intent(game, 2, 30, "steel", Fix.One), Intent(game, 5000000000, 30, "steel", Fix.One) };
            ResourceReservation[] release = Copy(Transactions(game).AcquireClaims(Company, next, System.Array.Empty<ResourceReservation>()).Receipts);
            twin = factory.Load(game.Save()); before = game.Hash(); long history = History(game).Latest;
            c.That(Transactions(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, release, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "release frees exact complete promises without spending");
            Array.Reverse(release); c.That(Transactions(twin).ReleaseClaims(new ResourceClaimReleaseIntent(Company, release, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "restored reordered release uses the same native books");
            c.Equal(game.Hash(), twin.Hash(), "release has stable saved continuation");
            c.That(before != game.Hash(), "release advances actual promise owner state");
            c.Equal(Fix.One, Pools(game).Get(2).Amount(steel), "release changes no physical stock");
            c.Equal(history, History(game).Latest, "release creates no fictitious consumption fact");
            ReceiptRefusal(c, game, release, ResourceTransferStatus.InvalidReceipt, "released receipt set");
            ResourceReservation[] interrupted = Copy(Transactions(game).AcquireClaims(Company, next, System.Array.Empty<ResourceReservation>()).Receipts);
            c.That(Transactions(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, new[] { interrupted[1] }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "one independently released endpoint record is actually gone");
            ReceiptRefusal(c, game, interrupted, ResourceTransferStatus.InvalidReceipt, "missing later promise leaves the first promise untouched");
            c.That(Transactions(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, new[] { interrupted[0] }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "surviving first promise can be explicitly released afterward");
            first.ReleaseOut(unrelated, tool); first.ReleaseIn(unrelated, incoming);
            Commands(c, factory);
            Faults(c, factory);
        }
        private static void Commands(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Seed(game);
            game.Seat(0).Submit(new ClaimProbeCommand { Intents = new[] { Intent(game, 2, 50, "steel", Fix.One), Intent(game, 1, 50, "steel", Fix.One) } });
            SimGame twin = factory.Load(game.Save());
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "pending acquisition command restores canonical native bills through the normal inbox codec");
            c.Equal(1, Pools(game).Get(1).Reservations.Count, "command Update creates the first real promise");
            c.Equal(1, Pools(game).Get(2).Reservations.Count, "command Update creates the second real promise");
            var records = new[] { Pools(game).Get(2).Reservations[0], Pools(game).Get(1).Reservations[0] };
            game.Seat(0).Submit(new ClaimProbeCommand { Consume = true, Receipts = records, Cause = Cause });
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "pending spending command restores actual scoped receipts/cause through the normal inbox codec");
            c.Equal(0, Pools(game).Get(1).Reservations.Count + Pools(game).Get(2).Reservations.Count, "normal command spending consumes each exact record once");
            c.Equal(2, History(game, 6).Entries.Count, "normal Update drains exactly its verified consumed facts");
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Count == 2 && results[0].Accepted && results[1].Accepted, "only complete admitted mutations report command success");
        }
        private static void Faults(Check c, GameFactory factory)
        {
            foreach (ClaimFaultMode mode in new[] { ClaimFaultMode.MutatingCheck, ClaimFaultMode.ExtraAcquire, ClaimFaultMode.ShortTake, ClaimFaultMode.IgnoreRelease })
            {
                SimGame game = New(factory); Seed(game); long history = History(game).Latest;
                ResourceClaimIntent[] intents = { Intent(game, 1, 40, "steel", Fix.One), Intent(game, 2, 40, "steel", Fix.One) };
                var command = new ClaimProbeCommand { Intents = intents, Cause = Cause };
                if (mode == ClaimFaultMode.ShortTake || mode == ClaimFaultMode.IgnoreRelease)
                {
                    command.Receipts = Copy(Transactions(game).AcquireClaims(Company, intents, System.Array.Empty<ResourceReservation>()).Receipts);
                    command.Consume = mode == ClaimFaultMode.ShortTake; command.Release = mode == ClaimFaultMode.IgnoreRelease;
                }
                Pools(game).ClaimFault = mode;
                game.Seat(0).Submit(command); game.Update();
                c.That(game.Fault != null, mode + " provider violation faults through the engine's normal command/Update boundary");
                c.Equal(history, History(game).Latest, mode + " publishes no partial-success consumption history");
                c.Throws<InvalidOperationException>(() => game.Save(), mode + " interrupted provider mutation cannot checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), mode + " interrupted provider mutation cannot masquerade as a settled hash");
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Count == 0, mode + " never reports a successful committed player command");
            }
        }
    }
}
