using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>"supplier": { "sells": { "steel": 120 } } — a market that sells resources at a unit price, paid when a carrier loads.</summary>
    public sealed class SupplierDef : ComponentDef<Supplier>
    {
        private BillSpec _prices;
        public Bill Prices { get; private set; }
        internal ResolvedContent Content { get; private set; }

        protected override void ReadCore(JsonReader r) => _prices = BillSpec.Read(r, "sells");
        protected override void ResolveCore(ContentResolver c) { Content = c.Content; Prices = _prices.Resolve(c); }

        public bool Sells(ResourceId resource) => Prices.AmountOf(resource).IsPositive;
        public Fix PriceOf(ResourceId resource) => Prices.AmountOf(resource);
    }

    public sealed class Supplier : Component<SupplierDef>, ISupplier, IResourceSupplyPrice
    {
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer)
        { ResourceStateEncoding.RequireWriter(writer); _promises.Sync(writer, Endpoint); }
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        public Fix UnitPrice(ResourceId resource) => Def.PriceOf(resource);
        public Code CheckRemoval(World world) => Reservations.Count != 0 ? ResourceTransactions.ActivePromises : Code.None;
        public ResourceSupplyKind SupplyKind => ResourceSupplyKind.AuthoredCreation;
        public IOutletPort SupplyStock => null;
        private readonly ResourceReservationBook _promises = new ResourceReservationBook();
        public IReadOnlyList<ResourceReservation> Reservations => _promises.Records;
        public CapacityQuantum SupplyQuantum(ResourceId resource)
        {
            IResourceContentDefinition definition = Def.Content.Resource(resource);
            return definition != null && Def.Sells(resource)
                ? new CapacityQuantum(CapacityStatus.Fits, definition.QuantityUnit.Discrete ? Fix.One.Raw : 1)
                : new CapacityQuantum(CapacityStatus.ResourceRejected);
        }
        public Fix Offer(ResourceId resource, Fix requested) => SupplyQuantum(resource).Floor(requested);
        public bool CanReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount)
            => _promises.CanReserve(owner, resource, ReservationDirection.Outbound) && amount.IsPositive &&
                Offer(resource, amount) == amount && amount.Raw <= long.MaxValue - _promises.Amount(resource, ReservationDirection.Outbound).Raw;
        public ReservationToken ReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveSupply(owner, resource, amount)) throw new InvalidOperationException("invalid supply promise");
            ReservationToken token = _promises.Add(Endpoint, owner, resource, amount, ReservationDirection.Outbound);
            Entity.Touch(); return token;
        }
        public void ReleaseSupply(ReservationOwner owner, ReservationToken token)
        { _promises.Release(Endpoint, owner, token, ReservationDirection.Outbound); Entity.Touch(); }
        public ResourceReceiptCheck CheckSupply(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = _promises.Check(Endpoint, owner, token, ReservationDirection.Outbound);
            if (!check.Ready) return check;
            if (!ReferenceEquals(world.Content, Def.Content) || !amount.IsPositive || amount > check.Record.Amount ||
                Offer(check.Record.Resource, amount) != amount)
                return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, check.Record);
            return check;
        }
        public Fix SupplyReserved(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount, ResourceSupplyExecution execution)
        {
            if (execution == null) throw new InvalidOperationException("supply fulfillment requires the transaction owner");
            ResourceReceiptCheck check = CheckSupply(world, buyer, owner, token, amount);
            if (!check.Ready) throw new InvalidOperationException("reserved supply refused: " + check.Status);
            execution.RequireSupply(Endpoint, owner, token, amount);
            _promises.Consume(Endpoint, owner, token, ReservationDirection.Outbound, amount); Entity.Touch(); return amount;
        }
        public override void Sync(IStateSync sync)
        {
            _promises.Sync(sync, Endpoint);
            if (sync.Reading)
                foreach (ResourceReservation record in Reservations)
                    if (record.Direction != ReservationDirection.Outbound || Offer(record.Resource, record.Amount) != record.Amount)
                        throw new CorruptStateException("invalid saved supply promise");
        }
    }

}
