using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Provider-owned one-dimensional region; no tiles, raster resolution or platform limit.</summary>
    public readonly struct PointRegion
    {
        public PointRegion(Fix start, Fix spacing, long count) { Start = start; Spacing = spacing; Count = count; }
        public Fix Start { get; }
        public Fix Spacing { get; }
        public long Count { get; }
        internal Fix PositionAt(long index) => Fix.FromRaw(checked(Start.Raw + checked(Spacing.Raw * index)));
    }

    public sealed class PointLayer : SpatialLayer, ISpatialResourceQuantity<Fix>
    {
        private SortedDictionary<long, Fix> _samples = new SortedDictionary<long, Fix>();
        private readonly PointSpace _space;
        private readonly SpatialResourceCellBindings _resourceCells;
        public PointLayer(ISpatialLayerDefinition definition, PointSpace space) : base(definition)
        { _space = space ?? throw new ArgumentNullException(nameof(space)); _resourceCells = new SpatialResourceCellBindings(this); }
        public void BindResourceCell(Fix point, ISpatialResourceCellGuard cell)
        { _space.CheckBinding(this, point); _resourceCells.Bind(point.Raw, cell); }
        public UnitQuantity ReadQuantity(Fix point) => _space.ReadQuantity(this, point);
        public SpatialResourceChange CheckWithdrawal(Fix point, Fix nativeAmount)
            => _space.CheckWithdrawal(this, point, nativeAmount);
        public SpatialResourceChange Withdraw(Fix point, Fix nativeAmount)
            => _space.Withdraw(this, point, nativeAmount);
        public SpatialResourceChangeCommit PrepareWithdrawal(Fix point, Fix nativeAmount)
            => _space.PrepareWithdrawal(this, point, nativeAmount);
        public SpatialResourceChange CheckDeposit(Fix point, Fix nativeAmount)
            => _space.CheckDeposit(this, point, nativeAmount);
        public SpatialResourceChange Deposit(Fix point, Fix nativeAmount)
            => _space.Deposit(this, point, nativeAmount);
        public SpatialResourceChangeCommit PrepareDeposit(Fix point, Fix nativeAmount)
            => _space.PrepareDeposit(this, point, nativeAmount);
        internal Fix Read(Fix point)
        {
            Fix value = _samples.TryGetValue(point.Raw, out Fix stored) ? stored : Fix.Zero;
            if (!CheckValue(value).IsNone)
                throw new CorruptStateException("invalid live point resource quantity: " + Id);
            return value;
        }
        internal Code CheckValue(Fix value)
        {
            if(ValueType==SpatialLayerValueType.Int32&&value.Raw%Fix.One.Raw!=0)return Codes.InvalidArgument;
            return SpatialResourceValues.CheckValue(this,value)==UnitConversionStatus.Converted?Code.None:Codes.InvalidArgument;
        }
        internal void PreflightValue(Fix point,Fix value)
        {
            if(!CheckValue(value).IsNone)throw new ArgumentException("invalid point layer value: "+Id,nameof(value));
            if(Read(point)!=value&&ResourceMeasure==SpatialResourceMeasure.Quantity)
                _resourceCells.PreflightChange(point.Raw,SpatialResourceValues.ReadQuantity(this,value).Amount);
        }
        internal void CopyPositivePositions(IList<Fix> destination)
        {
            destination.Clear();
            foreach(var sample in _samples)if(sample.Value.IsPositive)destination.Add(Fix.FromRaw(sample.Key));
        }
        internal bool Write(Fix point, Fix value)
        {
            if (!CheckValue(value).IsNone) throw new ArgumentException("invalid point resource quantity: " + Id, nameof(value));
            if (Read(point) == value) return false;
            PreflightValue(point,value);
            if (value == Fix.Zero) _samples.Remove(point.Raw); else _samples[point.Raw] = value;
            _resourceCells.Changed(point.Raw);
            return true;
        }
        internal bool AllowsQuantity(Fix point, Fix after) => _resourceCells.AllowsQuantity(point.Raw, after);
        internal void PreflightOwnerChange() => _resourceCells.PreflightOwnerChange();
        internal void OwnerChanged() => _resourceCells.OwnerChanged();
        internal void Assign(PlayerId owner) => Owner = owner;
        private static readonly Func<(PointLayer Layer, SortedDictionary<long, Fix> Samples), long, Fix> ReadCandidateQuantity =
            (context, key) => SpatialResourceValues.ReadQuantity(context.Layer,
                context.Samples.TryGetValue(key, out Fix value) ? value : Fix.Zero).Amount;
        private static readonly Func<PointLayer, long, Fix> ReadLiveQuantity =
            (layer, key) => SpatialResourceValues.ReadQuantity(layer, layer.Read(Fix.FromRaw(key))).Amount;
        internal void Sync(IStateSync sync, Fix extent, World world)
        {
            PlayerId owner = Owner; sync.Player(ref owner);
            if (owner != PlayerId.None && !world.Players.IsSeat(owner)
                || Visibility == SpatialLayerVisibility.Public && owner != PlayerId.None)
                throw new CorruptStateException("invalid point-layer owner: " + Id);
            int count = sync.Count(_samples.Count);
            if (sync.Reading)
            {
                var restored = new SortedDictionary<long, Fix>(); long previous = -1;
                for (int i = 0; i < count; i++)
                {
                    long coordinate = 0; Fix value = Fix.Zero;
                    sync.Long(ref coordinate); sync.Fix(ref value);
                    if (coordinate <= previous || coordinate > extent.Raw || value == Fix.Zero || !CheckValue(value).IsNone)
                        throw new CorruptStateException("invalid sparse point-layer sample: " + Id);
                    restored.Add(coordinate, value); previous = coordinate;
                }
                Action publish = _resourceCells.StageSync(sync, (this, restored), ReadCandidateQuantity);
                _samples = restored; Owner = owner; publish();
            }
            else foreach (KeyValuePair<long, Fix> sample in _samples)
            {
                long coordinate = sample.Key; Fix value = sample.Value;
                if (coordinate < 0 || coordinate > extent.Raw || value == Fix.Zero || !CheckValue(value).IsNone)
                    throw new CorruptStateException("invalid live point-layer sample: " + Id);
                sync.Long(ref coordinate); sync.Fix(ref value);
            }
            if (!sync.Reading) _resourceCells.StageSync(sync, this, ReadLiveQuantity);
        }
    }

    public sealed partial class PointSpace
    {
        private readonly List<ISpatialLayer> _layers = new List<ISpatialLayer>();
        private bool _layersFrozen;
        private World _world;
        private int _observerVersion, _publicVersion;
        private sealed class LayerChanges
        {
            public int Version;
            public HashSet<string> ReadableScopes = new HashSet<string>(StringComparer.Ordinal);
        }
        private readonly Dictionary<PlayerId, LayerChanges> _privateVersions = new Dictionary<PlayerId, LayerChanges>();
        private bool HasVisibility => _world != null && _world.Services.Has<IVisibility>();
        internal bool CanObserve(ISpatialLayer layer, PlayerId viewer) => SpatialLayerAccess.CanObserve(_world, layer, viewer);
        internal SpatialLayerSnapshot CaptureLayerMetadata(ISpatialLayer layer, PlayerId viewer)
            => SpatialLayerAccess.Capture(_world, layer, viewer);
        private bool CanObserveSample(ISpatialLayer layer, PlayerId viewer, Fix point)
            => SpatialLayerAccess.CanObserveSample(_world, layer, viewer, new PointSnapshot(Id, point, point));
        public IReadOnlyList<ISpatialLayer> SpatialLayers => _layers.AsReadOnly();
        public void RegisterLayer(ISpatialLayerDefinition definition)
        {
            if (_layersFrozen) throw new InvalidOperationException("point layer registration is closed");
            if (definition == null || !ReferenceEquals(definition.Space, _definition))
                throw new ContentException("point layer must belong to this selected definition");
            if (definition.ValueType != SpatialLayerValueType.Fixed && definition.ValueType != SpatialLayerValueType.Int32)
                throw new ContentException(definition.Id + ": point provider supports exact fixed/integer samples only");
            if (Layer(definition.Id) != null) throw new ContentException("duplicate point layer " + definition.Id);
            _layers.Add(new PointLayer(definition, this));
            _layers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }
        internal PointLayer Layer(string id)
        {
            foreach (ISpatialLayer layer in _layers) if (layer.Id == id) return (PointLayer)layer;
            return null;
        }
        internal Code CheckWrite(World world, PlayerId player, string layerId, Fix point, Fix value)
        {
            if (!ReferenceEquals(world.Space, this)) return Codes.InvalidArgument;
            PointLayer layer = Layer(layerId);
            if (!CanObserve(layer, player)) return Codes.NotFound;
            if (!InLayerBounds(point)) return Codes.OutOfBounds;
            if (!CanObserveSample(layer, player, point)) return Codes.NotFound;
            return layer.CheckValue(value);
        }
        private bool InLayerBounds(Fix point) => point >= Fix.Zero && point <= _definition.Extent;
        internal void CheckBinding(PointLayer layer, Fix point)
        {
            if (_layersFrozen || !ReferenceEquals(Layer(layer.Id), layer) || !InLayerBounds(point)) throw new ContentException("point resource cells bind to actual geometry during composition");
        }
        internal UnitQuantity ReadQuantity(PointLayer layer, Fix point)
        {
            if (!ReferenceEquals(Layer(layer.Id), layer)) throw new InvalidOperationException("foreign point-layer owner");
            if (!InLayerBounds(point)) throw new ArgumentOutOfRangeException(nameof(point));
            return SpatialResourceValues.ReadQuantity(layer, layer.Read(point));
        }
        internal SpatialResourceChange CheckWithdrawal(PointLayer layer, Fix point, Fix nativeAmount)
        {
            _world.RequireHealthyUpdate();
            if (!ReferenceEquals(Layer(layer.Id), layer)) throw new InvalidOperationException("foreign point-layer owner");
            if (!InLayerBounds(point)) return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            SpatialResourceChange check = SpatialResourceValues.CheckWithdrawal(layer, layer.Read(point), nativeAmount);
            return check.Ready && !layer.AllowsQuantity(point, check.After)
                ? SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity) : check;
        }
        internal SpatialResourceChange Withdraw(PointLayer layer, Fix point, Fix nativeAmount)
        {
            SpatialResourceChange check = CheckWithdrawal(layer, point, nativeAmount);
            if (!check.Ready) return check;
            return PrepareWithdrawal(layer, point, nativeAmount).Commit();
        }
        internal SpatialResourceChangeCommit PrepareWithdrawal(PointLayer layer, Fix point, Fix nativeAmount)
            => PrepareQuantityChange(layer, point, nativeAmount, false);
        internal SpatialResourceChangeCommit PrepareDeposit(PointLayer layer, Fix point, Fix nativeAmount)
            => PrepareQuantityChange(layer, point, nativeAmount, true);
        private SpatialResourceChangeCommit PrepareQuantityChange(PointLayer layer, Fix point, Fix nativeAmount, bool deposit)
        {
            _world.RequireHealthyUpdate();
            if (!ReferenceEquals(Layer(layer.Id), layer)) throw new InvalidOperationException("foreign point-layer owner");
            if (!InLayerBounds(point)) return new SpatialResourceChangeCommit(SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation));
            var check = deposit ? SpatialResourceValues.CheckDeposit(layer, layer.Read(point), nativeAmount)
                : SpatialResourceValues.CheckWithdrawal(layer, layer.Read(point), nativeAmount);
            if (!check.Ready) return new SpatialResourceChangeCommit(check);
            var write = PrepareSampleWrites(new List<(PointLayer Layer, Fix Point, Fix Value)> { (layer, point, check.StoredAfter) });
            return new SpatialResourceChangeCommit(check, () => CommitSpatialWrite(write));
        }
        internal SpatialResourceChange CheckDeposit(PointLayer layer, Fix point, Fix nativeAmount)
        {
            _world.RequireHealthyUpdate();
            if (!ReferenceEquals(Layer(layer.Id), layer)) throw new InvalidOperationException("foreign point-layer owner");
            if (!InLayerBounds(point)) return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            var check = SpatialResourceValues.CheckDeposit(layer, layer.Read(point), nativeAmount);
            return check.Ready && !layer.AllowsQuantity(point, check.After)
                ? SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity) : check;
        }
        internal SpatialResourceChange Deposit(PointLayer layer, Fix point, Fix nativeAmount)
        {
            var check = CheckDeposit(layer, point, nativeAmount);
            return check.Ready ? PrepareDeposit(layer, point, nativeAmount).Commit() : check;
        }
        public Code WriteLayer(World world, PlayerId player, string layerId, Fix point, Fix value)
            =>WriteLayers(world,player,new[]{(Layer:layerId,Point:point,Value:value)});
        public Code WriteLayers(World world,PlayerId player,IReadOnlyList<(string Layer,Fix Point,Fix Value)> writes)
        {
            _world.RequireHealthyUpdate();
            if(writes==null||writes.Count==0)return Codes.InvalidArgument;
            var changed=new List<(PointLayer Layer,Fix Point,Fix Value)>();
            var seen=new HashSet<(string,long)>();
            foreach(var write in writes) {
                if(!seen.Add((write.Layer,write.Point.Raw)))return Codes.InvalidArgument;
                Code refusal=CheckWrite(world,player,write.Layer,write.Point,write.Value);if(!refusal.IsNone)return refusal;
                PointLayer layer=Layer(write.Layer);
                if(layer.Read(write.Point)!=write.Value)changed.Add((layer,write.Point,write.Value));
            }
            foreach (var write in changed) write.Layer.PreflightValue(write.Point, write.Value);
            CommitSpatialWrite(PrepareSampleWrites(changed));
            return Code.None;
        }
        public void SetAccess(bool open)
        {
            _world?.RequireHealthyUpdate();
            if (_accessOpen == open) return;
            var write = new SpatialWrite { ObserverVersion = _observerVersion, PublicVersion = _publicVersion,
                ObserverChanges = 1, AccessBefore = _accessOpen, AccessAfter = open,
                PublicChanges = HasVisibility ? 0 : 1 };
            // AccessOpen is public world geometry, including to companies with no visible layers.
            if (HasVisibility)
                for (int i = 0; i < _world.Players.Count; i++) {
                    var viewer = new PlayerId(i); write.Viewers.Add(viewer, (PrivateVersion(viewer), 1));
                }
            PreflightSpatialWrite(write);
            CommitSpatialWrite(write);
        }
        private sealed class SpatialWrite
        {
            internal int ObserverVersion, PublicVersion, ObserverChanges, PublicChanges;
            internal bool AccessBefore;
            internal bool? AccessAfter;
            internal PointLayer OwnerLayer;
            internal PlayerId OwnerBefore, OwnerAfter;
            internal readonly List<(PointLayer Layer, Fix Point, Fix Before, Fix After)> Samples = new List<(PointLayer, Fix, Fix, Fix)>();
            internal readonly Dictionary<PlayerId, (int Version, int Delta)> Viewers = new Dictionary<PlayerId, (int, int)>();
            internal readonly Dictionary<PlayerId, HashSet<string>> Scopes = new Dictionary<PlayerId, HashSet<string>>();
        }
        private SpatialWrite PrepareSampleWrites(IReadOnlyList<(PointLayer Layer, Fix Point, Fix Value)> changed)
        {
            var prepared = new SpatialWrite { ObserverVersion = _observerVersion, PublicVersion = _publicVersion,
                ObserverChanges = changed.Count, AccessBefore = _accessOpen };
            foreach (var write in changed) {
                prepared.Samples.Add((write.Layer, write.Point, write.Layer.Read(write.Point), write.Value));
                if(HasVisibility) {
                    foreach(var observer in _privateVersions)
                        if(CanObserveSample(write.Layer,observer.Key,write.Point))Count(observer.Key);
                }
                else if(write.Layer.Visibility==SpatialLayerVisibility.Public)prepared.PublicChanges++;
                else if(write.Layer.Owner.IsValid)Count(write.Layer.Owner);
            }
            PreflightSpatialWrite(prepared);
            return prepared;
            void Count(PlayerId observer)
            {
                int delta = prepared.Viewers.TryGetValue(observer, out var current) ? current.Delta : 0;
                prepared.Viewers[observer] = (PrivateVersion(observer), checked(delta + 1));
            }
        }
        private void PreflightSpatialWrite(SpatialWrite write)
        {
            _ = checked(write.ObserverVersion + write.ObserverChanges);
            int publicAfter = checked(write.PublicVersion + write.PublicChanges);
            var viewers = new HashSet<PlayerId>(_privateVersions.Keys); viewers.UnionWith(write.Viewers.Keys);
            foreach (var viewer in viewers) {
                int ownAfter = write.Viewers.TryGetValue(viewer, out var change)
                    ? checked(change.Version + change.Delta) : PrivateVersion(viewer);
                // Without visibility, the existing public and own-company contributions compose
                // one viewer revision. Validate that actual sum before publishing any source value.
                if (!HasVisibility) _ = checked(publicAfter + ownAfter);
            }
        }
        private void ValidateSpatialWrite(SpatialWrite write)
        {
            if (_observerVersion != write.ObserverVersion || _publicVersion != write.PublicVersion || _accessOpen != write.AccessBefore)
                throw new InvalidOperationException("prepared point spatial revisions changed before publication");
            if (write.OwnerLayer != null && write.OwnerLayer.Owner != write.OwnerBefore)
                throw new InvalidOperationException("prepared point ownership changed before publication");
            foreach (var viewer in write.Viewers)
                if (PrivateVersion(viewer.Key) != viewer.Value.Version) throw new InvalidOperationException("prepared point viewer changed before publication");
        }
        private void CommitSpatialWrite(SpatialWrite write)
        {
            _world?.RequireHealthyUpdate(); ValidateSpatialWrite(write);
            write.OwnerLayer?.PreflightOwnerChange();
            foreach (var sample in write.Samples) {
                if (sample.Layer.Read(sample.Point) != sample.Before) throw new InvalidOperationException("prepared point stock changed before publication");
                sample.Layer.PreflightValue(sample.Point, sample.After);
            }
            ValidateSpatialWrite(write); PreflightSpatialWrite(write);
            _world.Services.Get<ISpatialMutation>().Commit(() => {
                foreach (var sample in write.Samples) sample.Layer.Write(sample.Point, sample.After);
                if (write.OwnerLayer != null) { write.OwnerLayer.Assign(write.OwnerAfter); write.OwnerLayer.OwnerChanged(); }
                if (write.AccessAfter.HasValue) _accessOpen = write.AccessAfter.Value;
                _observerVersion = checked(write.ObserverVersion + write.ObserverChanges);
                _publicVersion = checked(write.PublicVersion + write.PublicChanges);
                foreach (var viewer in write.Viewers) Changes(viewer.Key).Version = checked(viewer.Value.Version + viewer.Value.Delta);
                foreach (var scope in write.Scopes) Changes(scope.Key).ReadableScopes = scope.Value;
            });
        }
        private int PrivateVersion(PlayerId player) => _privateVersions.TryGetValue(player, out LayerChanges value) ? value.Version : 0;
        private LayerChanges Changes(PlayerId player)
        {
            if (!_privateVersions.TryGetValue(player, out LayerChanges changes))
                _privateVersions.Add(player, changes = new LayerChanges());
            return changes;
        }
        public void AssignOwner(World world, string layerId, PlayerId owner)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("point-layer ownership requires its own session");
            world.RequireHealthyUpdate();
            PointLayer layer = Layer(layerId) ?? throw new ArgumentException("unknown point layer");
            if (layer.Visibility != SpatialLayerVisibility.CompanyPrivate) throw new InvalidOperationException("public layer cannot have a company owner");
            if (!world.Players.IsSeat(owner)) throw new ArgumentException("point-layer owner must be a session seat");
            if (layer.Owner == owner) return;
            var write = new SpatialWrite { ObserverVersion = _observerVersion, PublicVersion = _publicVersion,
                ObserverChanges = 1, AccessBefore = _accessOpen, OwnerLayer = layer, OwnerBefore = layer.Owner, OwnerAfter = owner };
            if (HasVisibility) PrepareVisibilityScopes(world, write);
            else {
                if (layer.Owner.IsValid) write.Viewers.Add(layer.Owner, (PrivateVersion(layer.Owner), 1));
                write.Viewers.Add(owner, (PrivateVersion(owner), 1));
            }
            PreflightSpatialWrite(write); CommitSpatialWrite(write);
        }
        internal int VersionFor(PlayerId viewer)
            => viewer == PlayerId.None ? _observerVersion : !viewer.IsValid || _world != null && !_world.Players.IsSeat(viewer) ? 0
                : checked((HasVisibility ? 0 : _publicVersion) + PrivateVersion(viewer));
        internal ISpatialRegionChangeQuery<PointRegion> CreateChangeQuery(PlayerId viewer)
            => new SpatialRegionChangeView<PointRegion>(_world, viewer);
        public SpatialRegionChangeSet<PointRegion> ReadRegionChanges(PlayerId viewer, int sinceVersion)
        {
            _world.RequireHealthyUpdate();
            int version = VersionFor(viewer);
            if (sinceVersion < 0 || sinceVersion > version) throw new ArgumentOutOfRangeException(nameof(sinceVersion));
            // Preserve this provider's explicit coarse invalidation: it has no fine location history.
            return sinceVersion == version ? SpatialRegionChangeSet<PointRegion>.Incremental(version, Array.Empty<PointRegion>())
                : SpatialRegionChangeSet<PointRegion>.Full(version);
        }
        public void RefreshVisibility(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("point visibility requires its actual session");
            world.RequireHealthyUpdate();
            if (!HasVisibility) return;
            var write = new SpatialWrite { ObserverVersion = _observerVersion, PublicVersion = _publicVersion, AccessBefore = _accessOpen };
            PrepareVisibilityScopes(world, write);
            PreflightSpatialWrite(write); CommitSpatialWrite(write);
        }
        private void PrepareVisibilityScopes(World world, SpatialWrite write)
        {
            for (int i = 0; i < world.Players.Count; i++)
            {
                var viewer = new PlayerId(i);
                var keys = new List<string>();
                foreach (ISpatialLayer layer in _layers)
                    SpatialLayerAccess.CopyObservationKeys(world, layer, viewer, keys,
                        ReferenceEquals(layer, write.OwnerLayer) ? write.OwnerAfter : (PlayerId?)null);
                var readable = new HashSet<string>(keys, StringComparer.Ordinal);
                if (_privateVersions.TryGetValue(viewer, out var changes) && changes.ReadableScopes.SetEquals(readable)) continue;
                if (changes == null && readable.Count == 0) { write.Scopes.Add(viewer, readable); continue; }
                write.Viewers.Add(viewer, (PrivateVersion(viewer), 1));
                write.Scopes.Add(viewer, readable);
            }
        }
        private void ResetSpatialVersions() { _observerVersion = 0; _publicVersion = 0; _privateVersions.Clear(); }
        private void SyncLayers(IStateSync sync)
        {
            int count = sync.Count(_layers.Count);
            if (count != _layers.Count) throw new CorruptStateException("point-layer declaration count differs");
            for (int i = 0; i < count; i++)
            {
                var layer = (PointLayer)_layers[i];
                string id = layer.Id; sync.String(ref id);
                if (id != layer.Id) throw new CorruptStateException("point-layer identity/order differs");
                layer.Sync(sync, _definition.Extent, _world);
            }
        }
        internal SpatialLayerReadStatus ReadLayer(PlayerId viewer, string id, PointRegion region, IList<Fix> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            SpatialLayerReadStatus status = ResolveReadableLayer(viewer, id, region, out PointLayer layer);
            if (status != SpatialLayerReadStatus.Success) return status;
            if (region.Count > int.MaxValue) return SpatialLayerReadStatus.BufferCapacityExceeded;
            if (!CanReadSamples(viewer, layer, region, 0, region.Count)) return SpatialLayerReadStatus.Unavailable;
            for (int i = 0; i < region.Count; i++) destination.Add(layer.Read(region.PositionAt(i)));
            return SpatialLayerReadStatus.Success;
        }
        internal SpatialLayerPageResult ReadLayerPage(PlayerId viewer, string id, PointRegion region,
            SpatialChangeScope scope, SpatialLayerPageRequest request, IList<Fix> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            SpatialLayerReadStatus status = ResolveReadableLayer(viewer, id, region, out PointLayer layer);
            if (status != SpatialLayerReadStatus.Success) return SpatialLayerPageResult.Refused(status);
            SpatialLayerReadStatus shape = SpatialLayerPaging.CheckEnvelope(region.Count, request, out int count);
            if (shape != SpatialLayerReadStatus.Success) return SpatialLayerPageResult.Refused(shape);
            if (!CanReadSamples(viewer, layer, region, request.Offset, count)) return SpatialLayerPageResult.Refused(SpatialLayerReadStatus.Unavailable);
            string binding = FormattableString.Invariant($"point:{id.Length}:{id}:{region.Start.Raw}:{region.Spacing.Raw}:{region.Count}");
            SpatialLayerPageResult page = SpatialLayerPaging.Check(region.Count, VersionFor(viewer), scope, binding, request);
            if (page.Status != SpatialLayerReadStatus.Success) return page;
            for (int i = 0; i < page.Count; i++) destination.Add(layer.Read(region.PositionAt(page.Offset + i)));
            return page;
        }
        private bool CanReadSamples(PlayerId viewer, PointLayer layer, PointRegion region, long offset, long count)
        {
            for (long i = 0; i < count; i++) if (!CanObserveSample(layer, viewer, region.PositionAt(offset + i))) return false;
            return true;
        }
        private SpatialLayerReadStatus ResolveReadableLayer(PlayerId viewer, string id, PointRegion region, out PointLayer layer)
        {
            layer = Layer(id);
            if (!CanObserve(layer, viewer)) return SpatialLayerReadStatus.Unavailable;
            if (region.Count < 0 || region.Spacing <= Fix.Zero || region.Start < Fix.Zero || region.Start > _definition.Extent)
                return SpatialLayerReadStatus.InvalidRegion;
            try
            {
                if (region.Count > 0 && region.PositionAt(region.Count - 1) > _definition.Extent)
                    return SpatialLayerReadStatus.InvalidRegion;
            }
            catch (OverflowException) { return SpatialLayerReadStatus.InvalidRegion; }
            return SpatialLayerReadStatus.Success;
        }
    }

    /// <summary>Immutable facts and scoped reads; never exposes the mutable provider to a host.</summary>
    public sealed class PointLayerView : ISpatialLayerQuery<PointRegion, Fix>, ISpatialLayerPagedQuery<PointRegion, Fix>, ISpatialRegionChangeQuery<PointRegion>
    {
        private readonly SpatialChangeScope _pages = new SpatialChangeScope();
        private readonly ISpatialRegionChangeQuery<PointRegion> _changes;
        private readonly PointSpace _space;
        private readonly PlayerId _viewer;
        public PointLayerView(PointSpace space, PlayerId viewer)
        { _space = space; _viewer = viewer; _changes = space.CreateChangeQuery(viewer); }
        public int Version => _space.VersionFor(_viewer);
        public SpatialRegionChanges<PointRegion> ReadChanges(SpatialRegionCursor previous = null)
            => _changes.ReadChanges(previous);
        public IReadOnlyList<SpatialLayerSnapshot> Layers
        {
            get
            {
                var result = new List<SpatialLayerSnapshot>();
                foreach (ISpatialLayer layer in _space.SpatialLayers)
                {
                    SpatialLayerSnapshot snapshot = _space.CaptureLayerMetadata(layer, _viewer);
                    if (snapshot != null) result.Add(snapshot);
                }
                return result.AsReadOnly();
            }
        }
        public SpatialLayerReadStatus ReadLayer(string layerId, PointRegion region, IList<Fix> destination)
            => _space.ReadLayer(_viewer, layerId, region, destination);
        public SpatialLayerPageResult ReadLayerPage(string layerId, PointRegion region,
            SpatialLayerPageRequest request, IList<Fix> destination)
            => _space.ReadLayerPage(_viewer, layerId, region, _pages, request, destination);
    }

    public sealed class WritePointLayerCommand : Command
    {
        public string Layer;
        public Fix Position, Value;
        public override void Sync(IStateSync sync) { sync.String(ref Layer); sync.Fix(ref Position); sync.Fix(ref Value); }
    }
    public sealed class WritePointLayerHandler : CommandHandler<WritePointLayerCommand>
    {
        protected override Code Check(World world, WritePointLayerCommand command)
            => world.Space is PointSpace space ? space.CheckWrite(world, command.Player, command.Layer, command.Position, command.Value) : Codes.InvalidArgument;
        protected override void Apply(World world, WritePointLayerCommand command, CommandContext context)
        {
            Code refusal = ((PointSpace)world.Space).WriteLayer(world, command.Player, command.Layer, command.Position, command.Value);
            if (!refusal.IsNone) throw new InvalidOperationException("point-layer write changed after admission: " + refusal);
        }
    }
    public sealed class AssignPointLayerCommand : Command
    {
        public string Layer;
        public PlayerId Owner;
        public override void Sync(IStateSync sync) { sync.String(ref Layer); sync.Player(ref Owner); }
    }
    public sealed class AssignPointLayerHandler : CommandHandler<AssignPointLayerCommand>
    {
        protected override Code Check(World world, AssignPointLayerCommand command)
        {
            if (!(world.Space is PointSpace space) || !world.Players.IsSeat(command.Owner)) return Codes.InvalidArgument;
            PointLayer layer = space.Layer(command.Layer);
            if (!SpatialLayerAccess.CanSeeDefinition(world, layer, command.Player)) return Codes.NotFound;
            if (layer.Visibility != SpatialLayerVisibility.CompanyPrivate) return Codes.NotAllowed;
            return layer.Owner.IsValid && layer.Owner != command.Player ? Codes.NotOwned : Code.None;
        }
        protected override void Apply(World world, AssignPointLayerCommand command, CommandContext context)
            => ((PointSpace)world.Space).AssignOwner(world, command.Layer, command.Owner);
    }
}
