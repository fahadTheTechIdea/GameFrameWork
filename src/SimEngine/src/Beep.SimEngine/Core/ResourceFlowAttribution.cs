using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>Historical endpoint provenance, not a receipt, live lookup or material balance.</summary>
    public readonly struct ResourceFlowAttribution : IComparable<ResourceFlowAttribution>, IEquatable<ResourceFlowAttribution>
    {
        public ResourceEndpointId Source { get; }
        public ResourceEndpointId Destination { get; }
        public ResourceEndpointId Endpoint { get; }
        public string OwnerScope { get; }
        public ResourceFlowAttribution(ResourceEndpointId source, ResourceEndpointId destination, ResourceEndpointId endpoint, string ownerScope)
        { Source = source; Destination = destination; Endpoint = endpoint; OwnerScope = ownerScope; }
        private static bool Empty(ResourceEndpointId id) => id.Scope == null && id.Identity == 0;
        public bool IsValidFor(ResourceFlowKind kind)
        {
            if (string.IsNullOrWhiteSpace(OwnerScope) || !Enum.IsDefined(typeof(ResourceFlowKind), kind)) return false;
            if (kind <= ResourceFlowKind.AuthoredCreation)
                return Source.IsValid && Destination.IsValid && Source != Destination && Empty(Endpoint);
            return Endpoint.IsValid && Empty(Destination) &&
                (Empty(Source) ? Endpoint.Scope == ResourceEndpointId.EntityScope || kind == ResourceFlowKind.Produced :
                    ResourceConversionInput.IsDispositionKind(kind) && Source == Endpoint);
        }
        private static int Compare(ResourceEndpointId left, ResourceEndpointId right)
        { int order = string.CompareOrdinal(left.Scope, right.Scope); return order != 0 ? order : left.Identity.CompareTo(right.Identity); }
        public int CompareTo(ResourceFlowAttribution other)
        {
            int order = Compare(Source, other.Source); if (order != 0) return order;
            order = Compare(Destination, other.Destination); if (order != 0) return order;
            order = Compare(Endpoint, other.Endpoint); return order != 0 ? order : string.CompareOrdinal(OwnerScope, other.OwnerScope);
        }
        public bool Equals(ResourceFlowAttribution other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is ResourceFlowAttribution other && Equals(other);
        public override int GetHashCode() => Source.GetHashCode() ^ Destination.GetHashCode() ^ Endpoint.GetHashCode() ^ (OwnerScope?.GetHashCode() ?? 0);
        public static void Sync(IStateSync sync, ref ResourceFlowAttribution attribution)
        {
            var source = attribution.Source; var destination = attribution.Destination; var endpoint = attribution.Endpoint; string scope = attribution.OwnerScope;
            ResourceEndpointId.Sync(sync, ref source); ResourceEndpointId.Sync(sync, ref destination); ResourceEndpointId.Sync(sync, ref endpoint); sync.String(ref scope);
            if (sync.Reading) attribution = new ResourceFlowAttribution(source, destination, endpoint, scope);
        }
    }
}
