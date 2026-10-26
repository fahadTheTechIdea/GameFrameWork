using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Deterministic storage shape for a content-authored spatial layer.</summary>
    public enum SpatialLayerValueType : byte { Byte = 1, Int32 = 2, Fixed = 3 }
    public enum SpatialLayerVisibility : byte { Public = 1, CompanyPrivate = 2 }
    /// <summary>Explicit resource-link interpretation; semantic layer types remain game-authored.</summary>
    public enum SpatialResourceMeasure : byte { None = 0, Quantity = 1, Associated = 2 }

    public enum SpatialLayerReadStatus : byte
    {
        Success = 0,
        /// <summary>Missing or not visible. Those cases deliberately cannot be distinguished by a viewer.</summary>
        Unavailable = 1,
        ValueTypeMismatch = 2,
        InvalidRegion = 3,
        /// <summary>The requested result cannot fit in one IList; split the query into smaller regions.</summary>
        BufferCapacityExceeded = 4,
        /// <summary>Invalid offset or capacity; no values are returned.</summary>
        InvalidPage = 5,
        /// <summary>Visible data changed since the requested version. Restart the read explicitly.</summary>
        Invalidated = 6,
        /// <summary>Continuation belongs to another query, layer, region or protocol.</summary>
        InvalidCursor = 7
    }

    /// <summary>
    /// Provider-owned, viewer-scoped regional reading. Coordinates belong to the provider; values use
    /// deterministic types. A refused read clears the destination and never fabricates values.
    /// </summary>
    public interface ISpatialLayerQuery<TRegion, TValue>
    {
        SpatialLayerReadStatus ReadLayer(string layerId, TRegion region, IList<TValue> destination);
    }

    /// <summary>
    /// Optional bounded regional reads. The provider owns geometry, ordering and authorization.
    /// Continuations keep the same layer/region and submit the returned opaque query-bound cursor.
    /// Cursors are transient and must be discarded on load or a new session.
    /// </summary>
    public interface ISpatialLayerPagedQuery<TRegion, TValue>
    {
        SpatialLayerPageResult ReadLayerPage(string layerId, TRegion region,
            SpatialLayerPageRequest request, IList<TValue> destination);
    }

    /// <summary>Per-call limits, never a limit on the world or the selected region.</summary>
    public readonly struct SpatialLayerPageRequest
    {
        public SpatialLayerPageRequest(long offset, int capacity, SpatialRegionCursor cursor = null)
        { Offset = offset; Capacity = capacity; Cursor = cursor; }
        public long Offset { get; }
        public int Capacity { get; }
        public SpatialRegionCursor Cursor { get; }
    }

    /// <summary>Detached page facts. Refusals expose no region size, version or values.</summary>
    public readonly struct SpatialLayerPageResult
    {
        internal SpatialLayerPageResult(long offset, long total, int count, SpatialRegionCursor cursor)
        {
            Status = SpatialLayerReadStatus.Success;
            Offset = offset; TotalCount = total; Count = count; Cursor = cursor;
            NextOffset = checked(offset + count);
        }
        private SpatialLayerPageResult(SpatialLayerReadStatus status)
        { Status = status; Offset = NextOffset = TotalCount = 0; Count = 0; Cursor = null; }
        public SpatialLayerReadStatus Status { get; }
        public long Offset { get; }
        public long NextOffset { get; }
        public long TotalCount { get; }
        public int Count { get; }
        public SpatialRegionCursor Cursor { get; }
        public bool HasMore => Status == SpatialLayerReadStatus.Success && NextOffset < TotalCount;
        public static SpatialLayerPageResult Refused(SpatialLayerReadStatus status)
        {
            if (status == SpatialLayerReadStatus.Success || !Enum.IsDefined(typeof(SpatialLayerReadStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            return new SpatialLayerPageResult(status);
        }
    }

    /// <summary>One pure page-envelope validator; geometry and values remain provider-owned.</summary>
    public static class SpatialLayerPaging
    {
        public static SpatialLayerReadStatus CheckEnvelope(long total, SpatialLayerPageRequest request, out int count)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            count = 0;
            if (request.Offset < 0 || request.Offset > total || request.Capacity <= 0)
                return SpatialLayerReadStatus.InvalidPage;
            count = (int)System.Math.Min((long)request.Capacity, total - request.Offset);
            return SpatialLayerReadStatus.Success;
        }
        public static SpatialLayerPageResult Check(long total, int version, SpatialChangeScope scope,
            string binding, SpatialLayerPageRequest request)
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            SpatialLayerReadStatus envelope = CheckEnvelope(total, request, out int count);
            if (envelope != SpatialLayerReadStatus.Success) return SpatialLayerPageResult.Refused(envelope);
            // Identity/binding is distinct from freshness; future owned versions are stale too.
            if (!scope.TryBaseline(request.Cursor, int.MaxValue, out int before, binding))
                return SpatialLayerPageResult.Refused(SpatialLayerReadStatus.InvalidCursor);
            if (request.Cursor != null && before != version)
                return SpatialLayerPageResult.Refused(SpatialLayerReadStatus.Invalidated);
            return new SpatialLayerPageResult(request.Offset, total, count, scope.Capture(version, binding));
        }
    }

    public static class SpatialLayerAccess
    {
        public static bool CanRead(SpatialLayerVisibility visibility, PlayerId owner, PlayerId viewer)
            => viewer == PlayerId.None || viewer.IsValid &&
                (visibility == SpatialLayerVisibility.Public || owner.IsValid && owner == viewer);

        /// <summary>Actual provider/content authorization; metadata links obey the same visibility policy.</summary>
        public static bool CanSeeDefinition(World world, ISpatialLayer layer, PlayerId viewer)
        {
            if (world == null) return false;
            world.RequireHealthyUpdate();
            if (layer == null || !(world.Space is ISpatialLayerProvider provider) ||
                viewer != PlayerId.None && !world.Players.IsSeat(viewer)) return false;
            ISpatialSpace space = world.Space;
            return EntityObservation.ReadProvider(world, space, null, () =>
            {
                bool actual = false;
                foreach (ISpatialLayer candidate in provider.SpatialLayers)
                    if (ReferenceEquals(candidate, layer)) { actual = true; break; }
                if (!actual) return false;
                ISpatialLayerDefinition definition = world.Content.Find<ISpatialLayerDefinition>(layer.Id);
                if (definition == null || definition.Space.Id != layer.Space || layer.Space != space.Id) return false;
                var view = world.ViewFor(viewer);
                return view.CanSeeContent(definition) && view.CanSeeContent(layer.Unit as IContentDefinition) &&
                    (layer.Resource == null || view.CanSeeContent(layer.Resource as IContentDefinition)) &&
                    (layer.VerticalUnit == null || view.CanSeeContent(layer.VerticalUnit as IContentDefinition));
            }, space: space);
        }

        public static bool CanObserve(World world, ISpatialLayer layer, PlayerId viewer)
            => CanSeeDefinition(world, layer, viewer) &&
                EntityObservation.ReadProvider(world, layer, null, () => layer.CanRead(viewer), space: world.Space);

        /// <summary>Capture only authorized facts, never a runtime layer/content capability.</summary>
        public static SpatialLayerSnapshot Capture(World world, ISpatialLayer layer, PlayerId viewer)
            => !CanObserve(world, layer, viewer) ? null : EntityObservation.ReadProvider(world, layer, null,
                () => new SpatialLayerSnapshot(layer), space: world.Space);

        /// <summary>Provider-defined detached sample geometry; no assumed tile/point representation.</summary>
        public static bool CanObserveSample(World world, ISpatialLayer layer, PlayerId viewer, SpatialSnapshot sample)
        {
            if (!CanObserve(world, layer, viewer) || sample == null || sample.Space != layer.Space) return false;
            if (viewer == PlayerId.None) return true;
            return !world.Services.TryGet<IVisibility>(out IVisibility visibility) || !(visibility is ISpatialSampleVisibility samples)
                || EntityObservation.ReadProvider(world, visibility, null, () =>
                    samples.CanSeeSample(world, viewer, world.Content.Get<ISpatialLayerDefinition>(layer.Id), sample), space: world.Space);
        }

        /// <summary>Appends derived scope identities. A proposed owner supports pure ownership
        /// preparation; it never assigns ownership or authorizes a current sample read.</summary>
        public static void CopyObservationKeys(World world, ISpatialLayer layer, PlayerId viewer, IList<string> destination,
            PlayerId? proposedOwner = null)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (proposedOwner.HasValue) {
                if (!CanSeeDefinition(world, layer, viewer) || !CanRead(layer.Visibility, proposedOwner.Value, viewer)) return;
                if (proposedOwner.Value != PlayerId.None && !world.Players.IsSeat(proposedOwner.Value))
                    throw new ArgumentException("proposed spatial owner must belong to the actual session", nameof(proposedOwner));
            }
            else if (!CanObserve(world, layer, viewer)) return;
            string prefix = layer.Id.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + layer.Id;
            var values = new List<string> { "L" + prefix };
            if (viewer != PlayerId.None && world.Services.TryGet<IVisibility>(out IVisibility visibility) && visibility is ISpatialSampleVisibility samples)
            {
                var keys = EntityObservation.ReadProvider(world, visibility, null, () =>
                {
                    var captured = new List<string>();
                    samples.CopyObservationKeys(world, viewer, world.Content.Get<ISpatialLayerDefinition>(layer.Id), captured);
                    foreach (string key in captured)
                        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("visibility returned an invalid observation identity");
                    return captured;
                }, space: world.Space);
                foreach (string key in keys) values.Add("S" + prefix + key);
            }
            foreach (string value in values) destination.Add(value);
        }
    }

    /// <summary>
    /// A named, typed spatial data channel. Type and metadata are game-authored; the engine treats
    /// their identities as opaque and owns the deterministic values and persistence.
    /// </summary>
    public interface ISpatialLayer
    {
        string Id { get; }
        string Space { get; }
        Code Type { get; }
        SpatialLayerValueType ValueType { get; }
        SpatialLayerVisibility Visibility { get; }
        PlayerId Owner { get; }
        bool CanRead(PlayerId viewer);
        IUnitDefinition Unit { get; }
        IResourceDefinition Resource { get; }
        SpatialResourceMeasure ResourceMeasure { get; }
        Code VerticalDomain { get; }
        IUnitDefinition VerticalUnit { get; }
        string VerticalReference { get; }
        Fix VerticalOffset { get; }
        JsonValue Metadata { get; }
    }

    /// <summary>
    /// One resolved metadata binding shared by spatial representations. The provider owns values,
    /// invalidation and persistence; this base adds no registry or independent codec.
    /// </summary>
    public abstract class SpatialLayer : ISpatialLayer
    {
        private readonly ISpatialLayerDefinition _definition;
        protected SpatialLayer(ISpatialLayerDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.Space == null || definition.Unit == null || definition.Metadata == null)
                throw new ArgumentException("layer definition must be resolved", nameof(definition));
            Owner = PlayerId.None;
        }
        public string Id => _definition.Id;
        public string Space => _definition.Space.Id;
        public Code Type => _definition.Type;
        public SpatialLayerValueType ValueType => _definition.ValueType;
        public SpatialLayerVisibility Visibility => _definition.Visibility;
        public IUnitDefinition Unit => _definition.Unit;
        public IResourceDefinition Resource => _definition.Resource;
        public SpatialResourceMeasure ResourceMeasure => _definition.ResourceMeasure;
        public Code VerticalDomain => _definition.VerticalDomain;
        public IUnitDefinition VerticalUnit => _definition.VerticalUnit;
        public string VerticalReference => _definition.VerticalReference;
        public Fix VerticalOffset => _definition.VerticalOffset;
        public JsonValue Metadata => _definition.Metadata;
        public PlayerId Owner { get; protected internal set; }
        public bool CanRead(PlayerId viewer) => SpatialLayerAccess.CanRead(Visibility, Owner, viewer);
    }

    /// <summary>Detached layer facts, distinct from runtime access/storage capabilities.</summary>
    public interface ISpatialLayerSnapshot
    {
        string Id { get; }
        string Space { get; }
        Code Type { get; }
        SpatialLayerValueType ValueType { get; }
        SpatialLayerVisibility Visibility { get; }
        PlayerId Owner { get; }
        IUnitDefinition Unit { get; }
        IResourceDefinition Resource { get; }
        SpatialResourceMeasure ResourceMeasure { get; }
        Code VerticalDomain { get; }
        IUnitDefinition VerticalUnit { get; }
        string VerticalReference { get; }
        Fix VerticalOffset { get; }
        JsonValue Metadata { get; }
    }
    /// <summary>One detached projection. Unit/resource interfaces contain existing value copies, not content definitions.</summary>
    public sealed class SpatialLayerSnapshot : ISpatialLayerSnapshot
    {
        internal SpatialLayerSnapshot(ISpatialLayer source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Id = source.Id; Space = source.Space; Type = source.Type; ValueType = source.ValueType;
            Visibility = source.Visibility; Owner = source.Owner;
            Unit = new UnitInfo(source.Unit);
            IResourceDefinition resource = source.Resource;
            Resource = resource == null ? null : (IResourceDefinition)new ResourceInfo(resource);
            ResourceMeasure = source.ResourceMeasure;
            VerticalDomain = source.VerticalDomain;
            IUnitDefinition vertical = source.VerticalUnit;
            VerticalUnit = vertical == null ? null : (IUnitDefinition)new UnitInfo(vertical);
            VerticalReference = source.VerticalReference;
            VerticalOffset = source.VerticalOffset;
            Metadata = (source.Metadata ?? throw new ArgumentException("layer metadata is required", nameof(source))).Clone();
            Metadata.Freeze();
        }
        public string Id { get; }
        public string Space { get; }
        public Code Type { get; }
        public SpatialLayerValueType ValueType { get; }
        public SpatialLayerVisibility Visibility { get; }
        public PlayerId Owner { get; }
        public IUnitDefinition Unit { get; }
        public IResourceDefinition Resource { get; }
        public SpatialResourceMeasure ResourceMeasure { get; }
        public Code VerticalDomain { get; }
        public IUnitDefinition VerticalUnit { get; }
        public string VerticalReference { get; }
        public Fix VerticalOffset { get; }
        public JsonValue Metadata { get; }
    }

    /// <summary>Optional spatial-space capability that registers authored layers with the provider that owns their data.</summary>
    public interface ISpatialLayerProvider
    {
        IReadOnlyList<ISpatialLayer> SpatialLayers { get; }
        void RegisterLayer(ISpatialLayerDefinition definition);
        void AssignOwner(World world, string layerId, PlayerId owner);
    }

    /// <summary>Optional provider-owned, transient cursor invalidation after authoritative gameplay or restore.
    /// Knowledge and visibility remain their existing owners; reads must never refresh or mutate this cache.</summary>
    public interface ISpatialVisibilityInvalidation
    {
        void RefreshVisibility(World world);
    }

    public interface ISpatialPresence : IEntityCapability
    {
        string Space { get; }
        void BeginTick();
        SpatialSnapshot Capture();
    }
    public interface ISpatialPresenceDefinition : IComponentCompositionValidation
    {
        string Space { get; }
    }
    public abstract class SpatialSnapshot
    {
        protected SpatialSnapshot(string space)
        { Space = !string.IsNullOrWhiteSpace(space) ? space : throw new ArgumentException("spatial snapshot requires its actual space identity", nameof(space)); }
        public string Space { get; }
        /// <summary>Pure symmetric comparison of every detached spatial fact, including interpolation history.
        /// Different shape types/spaces do not match. No live provider, world or rendering logic belongs here.</summary>
        public abstract bool Matches(SpatialSnapshot other);
    }
    public abstract class SpatialWorldSnapshot
    {
        protected SpatialWorldSnapshot(string space, int version)
        {
            Space = !string.IsNullOrWhiteSpace(space) ? space : throw new ArgumentException("world snapshot requires its actual space identity", nameof(space));
            Version = version >= 0 ? version : throw new ArgumentOutOfRangeException(nameof(version), "world snapshot requires a nonnegative viewer revision");
        }
        public string Space { get; }
        public int Version { get; }
    }
    /// <summary>One optional spatial authority. Its state restores before entity-dependent services.</summary>
    public interface ISpatialSpace : IPersistentService, ICompositionValidator
    {
        /// <summary>Pure nonnegative revision of the provider's existing observation state.
        /// Includes invalidation even when values are restored; transient, not another saved clock or counter.</summary>
        int ObservationVersion { get; }
        void OnSpawn(Entity entity);
        void OnDespawn(Entity entity);
        void OnLoaded(World world);
        SpatialWorldSnapshot Capture(PlayerId viewer);
    }

    public enum SpatialOverlapStatus : byte { Disjoint = 0, Overlapping = 1, Unsupported = 2 }

    /// <summary>
    /// Optional pure geometry comparison on the actual spatial authority. Unsupported shapes are
    /// never presumed disjoint. Current physical extent, not presentation history, determines overlap.
    /// </summary>
    public interface ISpatialOverlap : ISpatialSpace
    {
        SpatialOverlapStatus CheckOverlap(World world, SpatialSnapshot left, SpatialSnapshot right);
    }
}
