using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent engine-side spatial module. Optical rules and controls remain native TileSensor.
    public sealed class SensorTileSpaceDef : Def, ITileSpaceDefinition
    {
        private TileSpaceDeclaration _geometry;
        public IReadOnlyList<TileSpaceGeometry> Geometries => _geometry.Geometries;
        public TileSpaceGeometry ReadGeometry(string scenario) => _geometry.ReadGeometry(scenario);
        protected override void ReadCore(JsonReader reader) => _geometry = TileSpaceDeclaration.Read(reader);
        public void Compose(WorldComposer composer)
        {
            var space = new SensorTileSpace(this, ReadGeometry(composer.Scenario.Id));
            foreach (var layer in composer.Content.All<ISpatialLayerDefinition>())
                if (ReferenceEquals(layer.Space, this)) space.RegisterLayer(layer);
            composer.Services.Bind<ISpatialSpace>(space);
            TileLayerView.Compose(composer, space);
        }
    }

    public sealed class SensorTileSpace : ITileNavigationSpace, ISpatialLayerProvider, ISpatialConstructionPlacement, ISpatialInteraction, ISpatialOverlap, ILineGeometryComposition, ITileSampleMutationSpace, ISpatialRollout, ISpatialVisibilityInvalidation, ISpatialRegionChangeSource<TileRect>
    {
        private readonly List<ISpatialLayer> _layers = new List<ISpatialLayer>();
        private readonly IReadOnlyList<ISpatialLayer> _readOnly;
        private bool _frozen;
        private World _world;
        private bool _visibilityReady;
        private int _fullVersion;
        private readonly SortedDictionary<long, int> _sampleChanges = new SortedDictionary<long, int>();
        // Derived viewer cursors, never sample stock, saved state or a second game clock.
        private sealed class VisibleChanges
        {
            internal int Version;
            internal int FullVersion;
            internal readonly SortedDictionary<long, int> Samples = new SortedDictionary<long, int>();
            internal HashSet<string> Scopes = new HashSet<string>(StringComparer.Ordinal);
        }
        private readonly Dictionary<PlayerId, VisibleChanges> _visibleChanges = new Dictionary<PlayerId, VisibleChanges>();
        // Negative-only instrumentation for the native coordinate publication boundary.
        public Action<World, ITilePresence> BeforeRelocate;
        public Action<World> BeforeOccupant;
        // Negative-only observation probes; absent from normal gameplay and saved state.
        public Action<World> BeforeReadChanges;
        public Func<SpatialRegionChangeSet<TileRect>, SpatialRegionChangeSet<TileRect>> ChangeResultProbe;
        public SensorTileSpace(SensorTileSpaceDef definition, TileSpaceGeometry geometry)
        { Id = definition.Id; Width = geometry.Width; Height = geometry.Height; _readOnly = _layers.AsReadOnly(); }
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int StateSchemaVersion => 2;
        public int ObservationVersion
        {
            get { int version = 0; foreach (SensorSampleLayer layer in _layers) version = checked(version + layer.Version); return version; }
        }
        public IReadOnlyList<ISpatialLayer> SpatialLayers => _readOnly;
        public void RegisterLayer(ISpatialLayerDefinition definition)
        {
            if (_frozen || definition.Space.Id != Id || _layers.Exists(x => x.Id == definition.Id))
                throw new ContentException("invalid independent layer registration");
            _layers.Add(new SensorSampleLayer(definition, this));
        }
        public void AssignOwner(World world, string layerId, PlayerId owner)
        {
            if (!ReferenceEquals(world, _world) || !ReferenceEquals(world.Space, this) ||
                owner != PlayerId.None && !world.Players.IsSeat(owner)) throw new ArgumentException("invalid independent layer owner");
            world.RequireHealthyUpdate();
            var layer = _layers.Find(x => x.Id == layerId) as SensorSampleLayer
                ?? throw new ArgumentException("missing independent layer");
            if (layer.Owner == owner) return;
            int version = ObservationVersion; _ = checked(version + 1);
            var updates = _visibilityReady ? PrepareScopeChanges(world, layer, owner)
                : new List<(PlayerId Viewer, int Version, HashSet<string> Scopes)>();
            var write = layer.PrepareOwnership(owner); write.Validate();
            if (ObservationVersion != version) throw new InvalidOperationException("prepared independent ownership changed before publication");
            ValidateScopeChanges(updates);
            world.Services.Get<ISpatialMutation>().Commit(() => {
                write.Publish();
                _fullVersion = ObservationVersion; _sampleChanges.Clear();
                PublishScopeChanges(updates);
                write.Notify();
            });
        }
        public void ValidateComposition(World world) { _frozen = true; _world = world; }
        private int ViewerVersion(PlayerId viewer) => _visibleChanges.TryGetValue(viewer, out var changes) ? changes.Version : 0;
        internal sealed class SampleObservation
        {
            internal int MapVersion;
            internal long[] Samples;
            internal readonly SortedDictionary<int, (int Version, long[] Samples)> Viewers =
                new SortedDictionary<int, (int, long[])>();
        }
        private static long SampleKey(TileCoord tile) => ((long)tile.Y << 32) | (uint)tile.X;
        internal SampleObservation PrepareSampleObservation(SensorSampleLayer layer, IEnumerable<TileCoord> samples)
        {
            _world.RequireHealthyUpdate();
            if (!_layers.Contains(layer) || samples == null) throw new ArgumentException("observation requires actual independent samples");
            var all = new SortedSet<long>(); var viewers = new SortedDictionary<int, SortedSet<long>>();
            foreach (var sample in samples) {
                if (!this.InBounds(sample)) throw new ArgumentOutOfRangeException(nameof(samples));
                long key = SampleKey(sample); all.Add(key);
                // No player baseline exists until the engine's first setup/restore settlement.
                if (!_visibilityReady) continue;
                for (int i = 0; i < _world.Players.Count; i++)
                    if (SpatialLayerAccess.CanObserveSample(_world, layer, new PlayerId(i), TileSpatialGeometry.SampleSnapshot(this, sample))) {
                        if (!viewers.TryGetValue(i, out var visible)) viewers.Add(i, visible = new SortedSet<long>());
                        visible.Add(key);
                    }
            }
            _ = checked(ObservationVersion + 1);
            var result = new SampleObservation { MapVersion = ObservationVersion, Samples = new List<long>(all).ToArray() };
            foreach (var viewer in viewers) {
                int version = ViewerVersion(new PlayerId(viewer.Key)); _ = checked(version + 1);
                result.Viewers.Add(viewer.Key, (version, new List<long>(viewer.Value).ToArray()));
            }
            return result;
        }
        internal void ValidateSampleObservation(SampleObservation observation)
        {
            if (observation == null || observation.MapVersion != ObservationVersion)
                throw new InvalidOperationException("prepared independent spatial observation changed");
            foreach (var viewer in observation.Viewers)
                if (ViewerVersion(new PlayerId(viewer.Key)) != viewer.Value.Version)
                    throw new InvalidOperationException("prepared independent viewer observation changed");
        }
        internal void PublishSampleObservation(SampleObservation observation)
        {
            foreach (long key in observation.Samples) _sampleChanges[key] = ObservationVersion;
            foreach (var entry in observation.Viewers) {
                var viewer = new PlayerId(entry.Key);
                if (!_visibleChanges.TryGetValue(viewer, out var changes))
                    _visibleChanges.Add(viewer, changes = new VisibleChanges());
                changes.Version = checked(changes.Version + 1);
                foreach (long key in entry.Value.Samples) changes.Samples[key] = changes.Version;
            }
        }
        internal void RequireHealthyMutation() => _world.RequireHealthyUpdate();
        internal void CommitSampleObservation(SampleObservation observation, Action publish, Action notify)
        {
            RequireHealthyMutation(); ValidateSampleObservation(observation);
            _world.Services.Get<ISpatialMutation>().Commit(() => { publish(); PublishSampleObservation(observation); notify(); });
        }
        public void RefreshVisibility(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("independent visibility requires its actual session");
            world.RequireHealthyUpdate();
            var updates = PrepareScopeChanges(world);
            ValidateScopeChanges(updates); PublishScopeChanges(updates);
            if (!_visibilityReady) { _fullVersion = ObservationVersion; _sampleChanges.Clear(); }
            _visibilityReady = true;
        }
        private List<(PlayerId Viewer, int Version, HashSet<string> Scopes)> PrepareScopeChanges(World world,
            SensorSampleLayer proposedLayer = null, PlayerId? proposedOwner = null)
        {
            var updates = new List<(PlayerId Viewer, int Version, HashSet<string> Scopes)>();
            for (int i = 0; i < world.Players.Count; i++) {
                var viewer = new PlayerId(i); var keys = new List<string>();
                foreach (var layer in _layers) SpatialLayerAccess.CopyObservationKeys(world, layer, viewer, keys,
                    ReferenceEquals(layer, proposedLayer) ? proposedOwner : null);
                var scopes = new HashSet<string>(keys, StringComparer.Ordinal);
                if (_visibleChanges.TryGetValue(viewer, out var current) && current.Scopes.SetEquals(scopes) ||
                    current == null && scopes.Count == 0) continue;
                int version = ViewerVersion(viewer); _ = checked(version + 1); updates.Add((viewer, version, scopes));
            }
            return updates;
        }
        private void ValidateScopeChanges(IReadOnlyList<(PlayerId Viewer, int Version, HashSet<string> Scopes)> updates)
        {
            foreach (var update in updates)
                if (ViewerVersion(update.Viewer) != update.Version)
                    throw new InvalidOperationException("prepared independent readable scope changed before publication");
        }
        private void PublishScopeChanges(IReadOnlyList<(PlayerId Viewer, int Version, HashSet<string> Scopes)> updates)
        {
            foreach (var update in updates) {
                if (!_visibleChanges.TryGetValue(update.Viewer, out var changes))
                    _visibleChanges.Add(update.Viewer, changes = new VisibleChanges());
                changes.Scopes = update.Scopes; changes.Version = checked(update.Version + 1);
                changes.FullVersion = changes.Version; changes.Samples.Clear();
            }
        }
        public SpatialRegionChangeSet<TileRect> ReadRegionChanges(PlayerId viewer, int sinceVersion)
        {
            _world.RequireHealthyUpdate(); BeforeReadChanges?.Invoke(_world);
            int version = viewer == PlayerId.None ? ObservationVersion : ViewerVersion(viewer);
            if (sinceVersion < 0 || sinceVersion > version) throw new ArgumentOutOfRangeException(nameof(sinceVersion));
            _visibleChanges.TryGetValue(viewer, out var changes);
            int full = viewer == PlayerId.None ? _fullVersion : changes?.FullVersion ?? 0;
            SpatialRegionChangeSet<TileRect> result;
            if (sinceVersion < full) result = SpatialRegionChangeSet<TileRect>.Full(version);
            else {
                var regions = new List<TileRect>();
                var samples = viewer == PlayerId.None ? _sampleChanges : changes?.Samples;
                if (samples != null) foreach (var sample in samples)
                    if (sample.Value > sinceVersion)
                        regions.Add(new TileRect((int)sample.Key, (int)(sample.Key >> 32), 1, 1));
                result = SpatialRegionChangeSet<TileRect>.Incremental(version, regions);
            }
            return ChangeResultProbe == null ? result : ChangeResultProbe(result);
        }
        public void ComposeLineGeometry(WorldComposer composer) => TileLineGeometry.Compose(composer, this);
        public void ApplySamples(IReadOnlyList<TileSampleMutation> mutations)
        {
            _world.RequireHealthyUpdate();
            if (mutations == null || mutations.Count == 0) throw new ArgumentException("explicit independent sample set required");
            var groups = new SortedDictionary<string, List<TileSampleMutation>>(StringComparer.Ordinal);
            foreach (var mutation in mutations) {
                var layer = TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(_world, this, mutation.Layer, false);
                if (layer is not SensorSampleLayer) throw new ArgumentException("foreign independent sample owner");
                if (!groups.TryGetValue(layer.Id, out var group)) groups.Add(layer.Id, group = new List<TileSampleMutation>());
                group.Add(mutation);
            }
            var writes = new List<SensorSampleLayer.LayerWrite>();
            foreach (var group in groups) {
                var write = ((SensorSampleLayer)_layers.Find(l => l.Id == group.Key)).PrepareSamples(group.Value);
                if (write != null) writes.Add(write);
            }
            foreach (var write in writes) { write.Validate(); ValidateSampleObservation(write.Observation); }
            _ = checked(ObservationVersion + writes.Count);
            var viewerWrites = new SortedDictionary<int, int>();
            foreach (var write in writes) foreach (int viewer in write.Observation.Viewers.Keys)
                viewerWrites[viewer] = checked((viewerWrites.TryGetValue(viewer, out int count) ? count : 0) + 1);
            foreach (var entry in viewerWrites) _ = checked(ViewerVersion(new PlayerId(entry.Key)) + entry.Value);
            _world.Services.Get<ISpatialMutation>().Commit(() => {
                foreach (var write in writes) write.Publish();
                foreach (var write in writes) PublishSampleObservation(write.Observation);
                foreach (var write in writes) write.Notify();
            });
        }
        private bool Contains(TileRect footprint) => footprint.X >= 0 && footprint.Y >= 0 && footprint.W > 0 && footprint.H > 0 &&
            (long)footprint.X + footprint.W <= Width && (long)footprint.Y + footprint.H <= Height;
        public void OnSpawn(Entity entity)
        {
            var presence = entity.Get<ITilePresence>();
            if (presence != null && (!presence.Placed || presence.Space != Id || !Contains(presence.Footprint)))
                throw new InvalidOperationException("presence must be placed within its independent selected space");
        }
        public void OnDespawn(Entity entity) { }
        public void OnLoaded(World world) { foreach (Entity entity in world.Entities.All) OnSpawn(entity); }
        public Entity OccupantAt(TileCoord tile)
        {
            BeforeOccupant?.Invoke(_world);
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive && entity.Get<ITilePresence>() is ITilePresence presence && presence.Placed &&
                    presence.Space == Id && presence.Blocks && presence.Footprint.Contains(tile)) return entity;
            return null;
        }
        public TileRect InteractionGoal(TileRect footprint, int actorWidth, int actorHeight)
            => TileSpatialGeometry.AdjacentGoal(this, footprint, actorWidth, actorHeight);
        public bool TryEstimateCost(World world, Entity actor, Entity target, out long cost)
            => TileInteractionRules.TryEstimateCost(world, this, actor, target, out cost);
        public SpatialInteractionStatus Check(World world, Entity actor, Entity target) => TileInteractionRules.Check(world, this, actor, target);
        public SpatialProximityStatus CheckProximity(World world, Entity actor, Entity target) => TileInteractionRules.CheckProximity(world, this, actor, target);
        public SpatialInteractionStatus Approach(World world, Entity actor, Entity target) => TileInteractionRules.Approach(world, this, actor, target);
        public SpatialOverlapStatus CheckOverlap(World world, SpatialSnapshot left, SpatialSnapshot right)
            => TileSpatialGeometry.CheckOverlap(world, this, left, right);
        public SpatialWorldSnapshot Capture(PlayerId viewer) => new TileWorldSnapshot(Id,
            viewer == PlayerId.None ? ObservationVersion : _world.Players.IsSeat(viewer) ? ViewerVersion(viewer) : 0, Width, Height);
        public void Sync(IStateSync sync) { foreach (SensorSampleLayer layer in _layers) layer.Sync(sync, _world); }
        public SpatialPlacement ReadPlacement(JsonValue arguments)
        {
            var reader = new JsonReader(arguments, "independent tile placement");
            string space = reader.String("space"); var tile = new TileCoord(reader.Int("x"), reader.Int("y")); reader.End();
            // Parse canonical intent only. Shared placement/path admission owns bounds and typed refusals.
            if (space != Id) throw new ContentException("placement names a different independent space");
            return new TilePlacement(Id, tile, FixVec2.Centre(tile));
        }
        public JsonValue WritePlacement(SpatialPlacement placement)
        {
            if (!(placement is TilePlacement tile) || tile.Space != Id || tile.Position != FixVec2.Centre(tile.Origin))
                throw new ContentException("incompatible independent tile placement");
            return TilePlacement.Arguments(Id, tile.Origin);
        }
        public string PlacementIdentity(SpatialPlacement placement) => WritePlacement(placement).ToString();
        public SpatialPlacement CapturePlacement(Entity source)
        {
            var presence = source?.Get<ITilePresence>();
            return presence != null && presence.Placed && presence.Space == Id
                ? new TilePlacement(Id, presence.Tile, presence.Position) : null;
        }
        private Code Vacancy(World world, TileRect area, Entity replacing)
        {
            if (!Contains(area)) return Codes.OutOfBounds;
            foreach (Entity entity in world.Entities.All)
                if (entity != replacing && entity.Alive && entity.Get<ITilePresence>() is ITilePresence other &&
                    other.Placed && other.Space == Id && other.Blocks && area.Overlaps(other.Footprint)) return Codes.Blocked;
            return Code.None;
        }
        public Code CheckPlacement(World world, EntityDef target, SpatialPlacement placement, Entity replacing)
            => TilePlacementRules.Check(world, this, target, placement, replacing);
        public Code CheckRolloutDefinition(World world, EntityDef producer, EntityDef output)
            => TileRolloutRules.CheckDefinition(world, this, producer, output);
        public Code FindRollout(World world, EntityRolloutRequest request, out SpatialPlacement placement)
            => TileRolloutRules.Find(world, this, request, out placement);
        public Code CheckRolloutPlacement(World world, EntityRolloutRequest request, SpatialPlacement placement)
            => TileRolloutRules.CheckPlacement(world, this, request, placement);
        public Code CheckConstructionDefinition(EntityDef target, EntityDef site) => TilePlacementRules.CheckConstructionDefinition(this, target, site);
        public void ConfigurePlacement(Entity entity, SpatialPlacement placement)
        {
            if (!(placement is TilePlacement tile) || tile.Space != Id || entity.Get<ITilePresence>() is not ITilePresence presence)
                throw new InvalidOperationException("incompatible independent presence placement");
            presence.Place(tile.Origin, tile.Position);
        }
        public void Relocate(World world, ITilePresence presence, TileCoord destination)
        {
            if (!ReferenceEquals(world, _world) || !ReferenceEquals(world.Space, this) || presence == null ||
                !ReferenceEquals(world.Entities.Get(presence.Entity.Id), presence.Entity) || !presence.Placed || !presence.Movable || presence.Space != Id)
                throw new ArgumentException("independent relocation requires actual live mobile presence");
            Code refusal = Vacancy(world, new TileRect(destination, presence.Footprint.W, presence.Footprint.H), presence.Entity);
            if (!refusal.IsNone) throw new InvalidOperationException("independent relocation refused " + refusal);
            BeforeRelocate?.Invoke(world, presence);
            // Occupancy is derived from actual entities; only native TilePresence publishes coordinates.
        }
    }
    // Sparse values and codec belong only to this provider. No copied native map or sight algorithm.
    public sealed class SensorSampleLayer : SpatialLayer, ITileWritableSampleLayer, ISpatialResourceQuantity<TileCoord>, ITileSparseSampleLayer
    {
        private Dictionary<long, Fix> _samples = new Dictionary<long, Fix>();
        private int _version;
        private readonly SensorTileSpace _space;
        private readonly SpatialResourceCellBindings _resourceCells;
        public SensorSampleLayer(ISpatialLayerDefinition definition, SensorTileSpace space) : base(definition)
        { _space = space; _resourceCells = new SpatialResourceCellBindings(this); }
        public int Version => _version;
        public void CopyNonZeroTiles(IList<TileCoord> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); long[] keys = new long[_samples.Count]; _samples.Keys.CopyTo(keys, 0); Array.Sort(keys);
            foreach (long key in keys) destination.Add(new TileCoord((int)key, (int)(key >> 32)));
        }
        // Negative-only instrumentation; not part of saved gameplay or an alternative sensor rule.
        public Action BeforeSample;
        public Func<TileCoord, Fix, Fix> SampleResultProbe;
        public int SampleReads;
        private static long Key(TileCoord tile) => ((long)tile.Y << 32) | (uint)tile.X;
        public Fix GetSample(TileCoord tile)
        {
            SampleReads++; BeforeSample?.Invoke();
            Fix value = _samples.TryGetValue(Key(tile), out Fix stored) ? stored : Fix.Zero;
            return SampleResultProbe == null ? value : SampleResultProbe(tile, value);
        }
        public void SetSample(TileCoord tile, Fix value)
        {
            _space.RequireHealthyMutation();
            if (!CheckSample(tile, value).IsNone) throw new ArgumentException("invalid independent scalar sample");
            long key = Key(tile);
            Fix before = _samples.TryGetValue(key, out Fix stored) ? stored : Fix.Zero;
            if (before == value) return;
            PublishSample(tile, value, _space.PrepareSampleObservation(this, new[] { tile }));
        }
        private void PublishSample(TileCoord tile, Fix value, SensorTileSpace.SampleObservation observation)
        {
            _space.RequireHealthyMutation();
            long key = Key(tile);
            int next = checked(_version + 1);
            if (ResourceMeasure == SpatialResourceMeasure.Quantity)
                _resourceCells.PreflightChange(key, SpatialResourceValues.ReadQuantity(this, value).Amount);
            _space.CommitSampleObservation(observation, () => {
                if (value == Fix.Zero) _samples.Remove(key); else _samples[key] = value;
                _version = next;
            }, () => _resourceCells.Changed(key));
        }
        internal sealed class LayerWrite
        {
            internal Action Validate, Publish, Notify;
            internal SensorTileSpace.SampleObservation Observation;
        }
        internal LayerWrite PrepareSamples(IReadOnlyList<TileSampleMutation> mutations)
        {
            var candidate = new Dictionary<long, Fix>(_samples); var seen = new HashSet<long>(); var changedTiles = new List<TileCoord>();
            foreach (var mutation in mutations) {
                long key = Key(mutation.Tile);
                if (!seen.Add(key) || !CheckSample(mutation.Tile, mutation.Value).IsNone) throw new ArgumentException("invalid or duplicate independent sample");
                Fix before = _samples.TryGetValue(key, out Fix value) ? value : Fix.Zero;
                if (before == mutation.Value) continue;
                changedTiles.Add(mutation.Tile);
                if (mutation.Value == Fix.Zero) candidate.Remove(key); else candidate[key] = mutation.Value;
            }
            if (changedTiles.Count == 0) return null;
            int version = _version, next = checked(version + 1); var original = _samples; PlayerId owner = Owner;
            var observation = _space.PrepareSampleObservation(this, changedTiles);
            Fix Quantity(long key) => SpatialResourceValues.ReadQuantity(this, candidate.TryGetValue(key, out Fix value) ? value : Fix.Zero).Amount;
            var changes = _resourceCells.PreflightChanges(Quantity);
            return new LayerWrite {
                Observation = observation,
                Validate = () => { if (_version != version || Owner != owner || !ReferenceEquals(_samples, original)) throw new InvalidOperationException("staged independent sample changed");
                    _resourceCells.PreflightChanges(Quantity); },
                Publish = () => { _samples = candidate; _version = next; }, Notify = () => _resourceCells.ChangesCommitted(changes)
            };
        }
        public Code CheckSample(TileCoord tile, Fix value)
        {
            if (!_space.InBounds(tile)) return Codes.OutOfBounds;
            if (ValueType != SpatialLayerValueType.Fixed && value.Raw % Fix.One.Raw != 0 ||
                ValueType == SpatialLayerValueType.Byte && (value < Fix.Zero || value > Fix.FromInt(byte.MaxValue)) ||
                ValueType == SpatialLayerValueType.Int32 && (value < Fix.FromInt(int.MinValue) || value > Fix.FromInt(int.MaxValue))) return Codes.InvalidArgument;
            return SpatialResourceValues.CheckValue(this, value) == UnitConversionStatus.Converted ? Code.None : Codes.InvalidArgument;
        }
        internal LayerWrite PrepareOwnership(PlayerId owner)
        {
            if (Visibility == SpatialLayerVisibility.Public && owner != PlayerId.None) throw new ArgumentException("public layer cannot have a company owner");
            int version = _version, next = checked(version + 1); PlayerId previous = Owner;
            _resourceCells.PreflightOwnerChange();
            return new LayerWrite {
                Validate = () => {
                    if (_version != version || Owner != previous) throw new InvalidOperationException("prepared independent layer owner changed");
                    _resourceCells.PreflightOwnerChange();
                },
                Publish = () => { Owner = owner; _version = next; }, Notify = _resourceCells.OwnerChanged
            };
        }
        public void BindResourceCell(TileCoord tile, ISpatialResourceCellGuard cell)
        {
            if (!_space.InBounds(tile)) throw new ArgumentOutOfRangeException(nameof(tile));
            _resourceCells.Bind(Key(tile), cell);
        }
        public UnitQuantity ReadQuantity(TileCoord tile)
        {
            if (!_space.InBounds(tile)) throw new ArgumentOutOfRangeException(nameof(tile));
            return SpatialResourceValues.ReadQuantity(this, GetSample(tile));
        }
        public SpatialResourceChange CheckWithdrawal(TileCoord tile, Fix nativeAmount)
        {
            _space.RequireHealthyMutation();
            if (!_space.InBounds(tile)) return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            var result = SpatialResourceValues.CheckWithdrawal(this, GetSample(tile), nativeAmount);
            return result.Ready && !_resourceCells.AllowsQuantity(Key(tile), result.After)
                ? SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity) : result;
        }
        public SpatialResourceChange Withdraw(TileCoord tile, Fix nativeAmount)
        {
            var result = CheckWithdrawal(tile, nativeAmount);
            if (!result.Ready) return result;
            return PrepareWithdrawal(tile, nativeAmount).Commit();
        }
        public SpatialResourceChangeCommit PrepareWithdrawal(TileCoord tile, Fix nativeAmount)
            =>PrepareQuantityChange(tile,nativeAmount,false);
        public SpatialResourceChangeCommit PrepareDeposit(TileCoord tile,Fix nativeAmount)
            =>PrepareQuantityChange(tile,nativeAmount,true);
        private SpatialResourceChangeCommit PrepareQuantityChange(TileCoord tile,Fix nativeAmount,bool deposit)
        {
            _space.RequireHealthyMutation();
            if (!_space.InBounds(tile)) return new SpatialResourceChangeCommit(SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation));
            var result = deposit?SpatialResourceValues.CheckDeposit(this,GetSample(tile),nativeAmount):SpatialResourceValues.CheckWithdrawal(this, GetSample(tile), nativeAmount);
            if (!result.Ready) return new SpatialResourceChangeCommit(result);
            int version = _version; PlayerId owner = Owner; _ = checked(version + 1);
            // Guarded visibility reads must precede the original resource transaction's unsettled
            // publication window, just as the native layer prepares its ValueWrite before commit.
            var observation = _space.PrepareSampleObservation(this, new[] { tile });
            return new SpatialResourceChangeCommit(result, () => {
                if (_version != version || Owner != owner)
                    throw new InvalidOperationException("prepared independent layer changed before publication");
                PublishSample(tile, result.StoredAfter, observation);
            });
        }
        public SpatialResourceChange CheckDeposit(TileCoord tile,Fix nativeAmount)
        {
            _space.RequireHealthyMutation();
            if(!_space.InBounds(tile))return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            var result=SpatialResourceValues.CheckDeposit(this,GetSample(tile),nativeAmount);
            return result.Ready&&!_resourceCells.AllowsQuantity(Key(tile),result.After)?SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity):result;
        }
        public SpatialResourceChange Deposit(TileCoord tile,Fix nativeAmount)
        {var check=CheckDeposit(tile,nativeAmount);return !check.Ready?check:PrepareDeposit(tile,nativeAmount).Commit();}
        private static readonly Func<(SensorSampleLayer Layer, Dictionary<long, Fix> Samples), long, Fix> ReadCheckpointQuantity =
            (context, key) => SpatialResourceValues.ReadQuantity(context.Layer,
                context.Samples.TryGetValue(key, out Fix value) ? value : Fix.Zero).Amount;
        internal void Sync(IStateSync sync, World world)
        {
            int version = _version; PlayerId owner = Owner;
            sync.Int(ref version); sync.Player(ref owner);
            if (version < 0 || Visibility == SpatialLayerVisibility.Public && owner != PlayerId.None ||
                owner != PlayerId.None && !world.Players.IsSeat(owner)) throw new CorruptStateException("invalid independent layer revision/owner");
            long[] keys = new long[_samples.Count]; _samples.Keys.CopyTo(keys, 0); Array.Sort(keys);
            int count = sync.Count(keys.Length);
            var restored = sync.Reading ? new Dictionary<long, Fix>() : _samples;
            long previous = -1;
            for (int i = 0; i < count; i++)
            {
                long key = sync.Reading ? 0 : keys[i]; Fix value = sync.Reading ? Fix.Zero : _samples[key];
                sync.Long(ref key); sync.Fix(ref value);
                var tile = new TileCoord((int)key, (int)(key >> 32));
                if (key <= previous || value == Fix.Zero || !CheckSample(tile, value).IsNone) throw new CorruptStateException("noncanonical independent samples");
                previous = key; if (sync.Reading) restored.Add(key, value);
            }
            Action publish = _resourceCells.StageSync(sync, (this, restored), ReadCheckpointQuantity);
            if (sync.Reading) { _samples = restored; _version = version; Owner = owner; publish(); }
        }
    }

    public sealed class IndependentTileSensorModule : GameModule
    {
        public override string Id => "proof.tile-sensor-space";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<SensorTileSpaceDef>("sensor_tile_spaces");
        }
    }
}
