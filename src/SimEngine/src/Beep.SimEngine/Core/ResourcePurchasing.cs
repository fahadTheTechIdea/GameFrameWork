using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>Optional pure trading admission supplied by the economic module, not by transport.</summary>
    public interface IResourceTradingRules
    {
        ResourceTransferStatus CheckTrading();
    }
    /// <summary>Authored unit prices in the selected account currency. No balance or payment authority.</summary>
    public interface IResourceSupplyPrice
    {
        ResourceEndpointId Endpoint { get; }
        Fix UnitPrice(ResourceId resource);
    }
    public readonly struct ResourcePurchaseIntent
    {
        public PlayerId Company { get; }
        public ResourceId Resource { get; }
        public Fix Quantity { get; }
        public IResourceSupplyPrice Pricing { get; }
        public ResourcePurchaseIntent(PlayerId company, ResourceId resource, Fix quantity, IResourceSupplyPrice pricing)
        { Company = company; Resource = resource; Quantity = quantity; Pricing = pricing; }
    }
    /// <summary>One optional account-backed settlement participant for the entire physical transfer set.</summary>
    public interface IResourcePurchaseSettlement
    {
        ResourceTransferStatus CheckPurchases(IReadOnlyList<ResourcePurchaseIntent> purchases);
        ResourceTransferStatus PreparePurchases(IReadOnlyList<ResourcePurchaseIntent> purchases, out IResourcePurchaseCommit commit);
    }
    public interface IResourcePurchaseCommit { void Commit(); }

    /// <summary>Ephemeral exact fulfillment scope created only by the physical transaction owner.
    /// It prevents an endpoint callback being used as a second purchase API. Never saved or retained.</summary>
    public sealed class ResourceSupplyExecution
    {
        private readonly IReadOnlyList<ResourceTransferIntent> _intents;
        private readonly bool[] _used;
        private bool _closed;
        internal ResourceSupplyExecution(IReadOnlyList<ResourceTransferIntent> intents)
        { _intents = intents; _used = new bool[intents.Count]; }
        public void RequireSupply(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token, Fix quantity)
        {
            if (!_closed)
                for (int i = 0; i < _intents.Count; i++)
                {
                    ResourceTransferIntent intent = _intents[i];
                    if (!_used[i] && intent.Kind == ResourceTransferKind.Supply && intent.Source.Endpoint == endpoint &&
                        intent.Owner.Equals(owner) && intent.Source.Equals(token) && intent.Amount == quantity)
                    { _used[i] = true; return; }
                }
            throw new InvalidOperationException("supply fulfillment requires its exact active transaction scope");
        }
        internal void Complete()
        {
            for (int i = 0; i < _intents.Count; i++)
                if (_intents[i].Kind == ResourceTransferKind.Supply && !_used[i])
                    throw new InvalidOperationException("supplier did not use its admitted transaction scope");
            _closed = true;
        }
        internal void Close() => _closed = true;
    }
}
