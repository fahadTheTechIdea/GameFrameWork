using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>Stable endpoint identity owned by a declared provider; a pool need not fabricate an entity.</summary>
    public readonly struct ResourceEndpointId : IEquatable<ResourceEndpointId>
    {
        public const string EntityScope = "sim.entity";
        public string Scope { get; }
        public long Identity { get; }
        public ResourceEndpointId(string scope, long identity) { Scope = scope; Identity = identity; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Scope) && Identity > 0 && (Scope != EntityScope || Identity <= int.MaxValue);
        public static ResourceEndpointId ForEntity(EntityId entity) => new ResourceEndpointId(EntityScope, entity.Value);
        public EntityId ToEntity()
        {
            if (Scope != EntityScope || !IsValid) throw new InvalidOperationException("resource endpoint is not an entity");
            return new EntityId(checked((int)Identity));
        }
        public bool Equals(ResourceEndpointId other) => Scope == other.Scope && Identity == other.Identity;
        public override bool Equals(object obj) => obj is ResourceEndpointId other && Equals(other);
        public override int GetHashCode() => (Scope?.GetHashCode() ?? 0) ^ Identity.GetHashCode();
        public static bool operator ==(ResourceEndpointId a, ResourceEndpointId b) => a.Equals(b);
        public static bool operator !=(ResourceEndpointId a, ResourceEndpointId b) => !a.Equals(b);
        public static void Sync(IStateSync sync, ref ResourceEndpointId endpoint)
        {
            string scope = endpoint.Scope; long identity = endpoint.Identity;
            sync.String(ref scope); sync.Long(ref identity);
            var value = identity == 0 && string.IsNullOrEmpty(scope) ? default : new ResourceEndpointId(scope, identity);
            if (identity != 0 || !string.IsNullOrEmpty(scope))
                if (!value.IsValid) throw new CorruptStateException("invalid resource endpoint identity");
            if (sync.Reading) endpoint = value;
        }
    }

    public interface IResourceEndpoint
    {
        ResourceEndpointId Endpoint { get; }
        PlayerId Company { get; }
        int Version { get; }
        /// <summary>Pure canonical encoding of this actual endpoint's material, promises and receipt
        /// sequence. Excludes auxiliary entity references. Writer-only; never restores or mutates state.</summary>
        void WriteResourceState(IStateSync writer);
    }

    /// <summary>One guard for endpoint-only observations; actual stock/book codecs remain with their owner.</summary>
    public static class ResourceStateEncoding
    {
        public static void RequireWriter(IStateSync writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (writer.Reading) throw new InvalidOperationException("resource observation encoding cannot restore state");
        }
    }

    /// <summary>
    /// Declared session service resolves its own endpoints from its existing authoritative store.
    /// Scope and World are immutable composition metadata. Resolve is a pure lookup, never acquisition,
    /// creation or stock mutation; returned endpoints retain identity and company and report owner versions.
    /// IDs are deterministic session-local references, not credentials or cross-session object handles.
    /// Resolutions may use different wrappers over that same authoritative stock and reservation book.
    /// </summary>
    public interface IResourceEndpointProvider
    {
        World World { get; }
        string EndpointScope { get; }
        IResourceEndpoint Resolve(ResourceEndpointId endpoint);
        /// <summary>Pure engine-side validation of existing books; no endpoint discovery, creation or second registry.</summary>
        void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect);
    }

    public interface IResourceInlet : IResourceEndpoint
    {
        bool Public { get; }
        ICapacityPolicy CapacityPolicy { get; }
        IReadOnlyList<ResourceAmount> InboundPromises { get; }
        IReadOnlyList<ResourceReservation> Reservations { get; }
        CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming);
        Fix Offer(ResourceId resource, Fix ceiling);
        Fix Amount(ResourceId resource);
        bool Accepts(ResolvedContent content, ResourceId resource);
        Fix Receive(ResolvedContent content, ResourceId resource, Fix amount);
        Fix ReservedIn(ResourceId resource);
        bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount);
        ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount);
        void ReleaseIn(ReservationOwner owner, ReservationToken token);
        ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token);
        /// <summary>Consume an explicit native quantity from this owned receipt; retain its unspent remainder.</summary>
        Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount);
    }

    /// <summary>Every outgoing endpoint also implements the same incoming contract.</summary>
    public interface IResourceOutlet : IResourceInlet
    {
        bool Evacuate { get; }
        Fix Available(ResourceId resource);
        Fix Take(ResourceId resource, Fix amount);
        Fix ReservedOut(ResourceId resource);
        IReadOnlyList<ResourceAmount> OutboundPromises { get; }
        bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount);
        ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount);
        /// <summary>Protect additional actual stock with this existing outbound token; never change its identity.</summary>
        void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount);
        void ReleaseOut(ReservationOwner owner, ReservationToken token);
        ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token);
        Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount);
    }
}
