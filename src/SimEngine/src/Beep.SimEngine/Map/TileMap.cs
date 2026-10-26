using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Maps
{
    /// <summary>
    /// One value per tile. Native writes use the shared typed Set/Fill implementation, which invalidates
    /// so a presenter can re-read only the chunks that changed.
    /// </summary>
    public abstract class MapLayer : SpatialLayer, ITileWritableSampleLayer
    {
        protected const int ChunkSize = TileMap.ChunkSize;
        protected MapLayer(ISpatialLayerDefinition definition, TileMap map) : base(definition)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            if (Space != map.Id) throw new ArgumentException("layer belongs to another space", nameof(definition));
        }
        public TileMap Map { get; }
        public abstract Fix GetSample(TileCoord tile);
        public abstract Code CheckSample(TileCoord tile, Fix value);
        public abstract void SetSample(TileCoord tile, Fix value);

        /// <summary>Bumped by every write to this layer: caches that read only some layers watch only those.</summary>
        public int Version { get; internal set; }
        internal abstract void PreflightOwnerChange();
        internal abstract void OwnerChanged();
        internal abstract void Sync(IStateSync s);
        internal abstract TileMap.LayerWrite PrepareSamples(IReadOnlyList<TileSampleMutation> mutations);
        protected static long ChunkKey(int x, int y) => ((long)y << 32) | (uint)x;
        protected static int ChunkX(long key) => (int)(uint)key;
        protected static int ChunkY(long key) => (int)(key >> 32);
        protected static long[] OrderedKeys<T>(Dictionary<long, T> chunks)
        {
            if (chunks.Count == 0) return Array.Empty<long>();
            long[] keys = new long[chunks.Count]; chunks.Keys.CopyTo(keys, 0); Array.Sort(keys); return keys;
        }
        protected void ValidateChunk(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Map.ChunksX || y >= Map.ChunksY)
                throw new CorruptStateException("map layer chunk is outside its authored space");
        }
        protected long SyncChunkKey(IStateSync sync, long[] keys, int index, ref long previous)
        {
            int x = sync.Reading ? 0 : ChunkX(keys[index]);
            int y = sync.Reading ? 0 : ChunkY(keys[index]);
            sync.Int(ref x); sync.Int(ref y);
            ValidateChunk(x, y);
            long key = ChunkKey(x, y);
            if (key <= previous) throw new CorruptStateException("map layer chunks are not in stable unique order");
            previous = key;
            return key;
        }
    }

    /// <summary>
    /// Sole sparse value and checkpoint implementation for native typed tile layers. Semantic metadata
    /// and visibility stay with SpatialLayer; TileMap owns registration and viewer invalidation.
    /// </summary>
    public abstract class MapLayer<T> : MapLayer, ISpatialResourceQuantity<TileCoord>, ITileSparseSampleLayer where T : struct
    {
        private Dictionary<long, T[]> _chunks = new Dictionary<long, T[]>();
        private bool _generating;
        private readonly SpatialResourceCellBindings _resourceCells;
        protected MapLayer(ISpatialLayerDefinition definition, TileMap map) : base(definition, map) { _resourceCells = new SpatialResourceCellBindings(this); }
        private static long CellKey(TileCoord tile) => ((long)tile.Y << 32) | (uint)tile.X;
        private static TileCoord CellTile(long key) => new TileCoord((int)(uint)key, (int)(key >> 32));
        public void BindResourceCell(TileCoord tile, ISpatialResourceCellGuard cell)
        {
            if (Map.LayersFrozen || !Map.InBounds(tile)) throw new ContentException("resource cells bind within their actual space during composition");
            _resourceCells.Bind(CellKey(tile), cell);
        }
        internal override void PreflightOwnerChange() { RequireSettled(); _resourceCells.PreflightOwnerChange(); }
        internal override void OwnerChanged() { _resourceCells.OwnerChanged(); }
        private Fix NativeValue(T value) => SpatialResourceValues.ReadQuantity(this, QuantityValue(value)).Amount;
        private T ValueIn(Dictionary<long, T[]> chunks, TileCoord tile) => chunks.TryGetValue(ChunkKey(tile.X / ChunkSize, tile.Y / ChunkSize), out T[] chunk)
            ? chunk[(tile.Y % ChunkSize) * ChunkSize + tile.X % ChunkSize] : default;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(T value) => EqualityComparer<T>.Default.Equals(value, default);
        protected abstract Fix QuantityValue(T value);
        protected abstract T StoredValue(Fix value);
        public override Fix GetSample(TileCoord tile) => QuantityValue(this[tile]);
        public override Code CheckSample(TileCoord tile, Fix value)
        {
            if (!Map.InBounds(tile)) return Codes.InvalidArgument;
            try
            {
                T stored = StoredValue(value);
                return QuantityValue(stored) == value && CheckValue(stored) == UnitConversionStatus.Converted ? Code.None : Codes.InvalidArgument;
            }
            catch (OverflowException) { return Codes.InvalidArgument; }
        }
        public override void SetSample(TileCoord tile, Fix value)
        {
            Map.RequireHealthyMutation();
            if (!CheckSample(tile, value).IsNone) throw new ArgumentException("invalid exact tile sample", nameof(value));
            Set(tile, StoredValue(value));
        }
        private UnitConversionStatus CheckValue(T value) => ResourceMeasure == SpatialResourceMeasure.Quantity
            ? SpatialResourceValues.CheckValue(this, QuantityValue(value)) : UnitConversionStatus.Converted;

        public T this[int x, int y] => this[new TileCoord(x, y)];
        public T this[TileCoord tile]
        {
            get
            {
                if (!Map.InBounds(tile)) throw new ArgumentOutOfRangeException(nameof(tile));
                T value = _chunks.TryGetValue(ChunkKey(tile.X / ChunkSize, tile.Y / ChunkSize), out T[] chunk)
                    ? chunk[(tile.Y % ChunkSize) * ChunkSize + tile.X % ChunkSize] : default;
                if (CheckValue(value) != UnitConversionStatus.Converted) throw new CorruptStateException("invalid live spatial resource quantity: " + Id);
                return value;
            }
        }

        public void Set(TileCoord tile, T value)
        {
            Map.RequireHealthyMutation();
            RequireSettled();
            if (!Map.InBounds(tile)) throw new ArgumentOutOfRangeException(nameof(tile), tile + " is off the map");
            UnitConversionStatus legality = CheckValue(value);
            if (legality != UnitConversionStatus.Converted) throw new ArgumentException("invalid spatial resource quantity: " + Id + "/" + legality, nameof(value));
            if (EqualityComparer<T>.Default.Equals(ValueIn(_chunks, tile), value)) return;
            if (ResourceMeasure == SpatialResourceMeasure.Quantity) _resourceCells.PreflightChange(CellKey(tile), NativeValue(value));
            TileMap.ValueWrite write = Map.PrepareValueWrite(this, new[] { tile });
            CommitSample(tile, value, write);
        }

        private void CommitSample(TileCoord tile, T value, TileMap.ValueWrite write)
        {
            Map.RequireHealthyMutation();
            Map.ValidateValueWrite(write);
            if (ResourceMeasure == SpatialResourceMeasure.Quantity) _resourceCells.PreflightChange(CellKey(tile), NativeValue(value));
            long key = ChunkKey(tile.X / ChunkSize, tile.Y / ChunkSize);
            int offset = (tile.Y % ChunkSize) * ChunkSize + tile.X % ChunkSize;
            Map.CommitValueWrite(write, () => {
                if (!_chunks.TryGetValue(key, out T[] chunk)) chunk = new T[ChunkSize * ChunkSize];
                chunk[offset] = value;
                if (IsZero(value) && !HasValues(chunk)) _chunks.Remove(key);
                else _chunks[key] = chunk;
            }, () => _resourceCells.Changed(CellKey(tile)));
        }

        internal override TileMap.LayerWrite PrepareSamples(IReadOnlyList<TileSampleMutation> mutations)
        {
            RequireSettled();
            var changed = new List<TileCoord>(); var seen = new HashSet<TileCoord>();
            var candidate = new Dictionary<long, T[]>(_chunks); var copied = new HashSet<long>();
            foreach (var mutation in mutations) {
                if (!seen.Add(mutation.Tile) || !CheckSample(mutation.Tile, mutation.Value).IsNone)
                    throw new ArgumentException("invalid or duplicate tile sample mutation");
                T value = StoredValue(mutation.Value);
                if (EqualityComparer<T>.Default.Equals(ValueIn(_chunks, mutation.Tile), value)) continue;
                long key = ChunkKey(mutation.Tile.X / ChunkSize, mutation.Tile.Y / ChunkSize);
                if (copied.Add(key)) candidate[key] = _chunks.TryGetValue(key, out var old) ? (T[])old.Clone() : new T[ChunkSize * ChunkSize];
                candidate[key][(mutation.Tile.Y % ChunkSize) * ChunkSize + mutation.Tile.X % ChunkSize] = value;
                changed.Add(mutation.Tile);
            }
            if (changed.Count == 0) return null;
            foreach (long key in copied) if (!HasValues(candidate[key])) candidate.Remove(key);
            IReadOnlyList<long> resourceChanges = _resourceCells.PreflightChanges(key => NativeValue(ValueIn(candidate, CellTile(key))));
            var write = Map.PrepareValueWrite(this, changed); var original = _chunks;
            return new TileMap.LayerWrite(write,
                () => { if (!ReferenceEquals(_chunks, original)) throw new InvalidOperationException("staged sparse layer changed before publication");
                    _resourceCells.PreflightChanges(key => NativeValue(ValueIn(candidate, CellTile(key)))); },
                () => _chunks = candidate, () => _resourceCells.ChangesCommitted(resourceChanges));
        }

        public UnitQuantity ReadQuantity(TileCoord tile)
        {
            RequireSettled();
            return SpatialResourceValues.ReadQuantity(this, QuantityValue(this[tile]));
        }

        public SpatialResourceChange CheckWithdrawal(TileCoord tile, Fix nativeAmount)
        {
            Map.RequireHealthyMutation();
            RequireSettled();
            if (!Map.InBounds(tile)) return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            SpatialResourceChange check = SpatialResourceValues.CheckWithdrawal(this, QuantityValue(this[tile]), nativeAmount);
            return check.Ready && !_resourceCells.AllowsQuantity(CellKey(tile), check.After)
                ? SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity) : check;
        }

        public SpatialResourceChange Withdraw(TileCoord tile, Fix nativeAmount)
        {
            SpatialResourceChange check = CheckWithdrawal(tile, nativeAmount);
            if (!check.Ready) return check;
            return PrepareWithdrawal(tile, nativeAmount).Commit();
        }

        public SpatialResourceChangeCommit PrepareWithdrawal(TileCoord tile, Fix nativeAmount)
            =>PrepareQuantityChange(tile,nativeAmount,false);
        public SpatialResourceChangeCommit PrepareDeposit(TileCoord tile,Fix nativeAmount)
            =>PrepareQuantityChange(tile,nativeAmount,true);
        private SpatialResourceChangeCommit PrepareQuantityChange(TileCoord tile,Fix nativeAmount,bool deposit)
        {
            Map.RequireHealthyMutation();
            RequireSettled();
            if (!Map.InBounds(tile)) return new SpatialResourceChangeCommit(SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation));
            var check = deposit?SpatialResourceValues.CheckDeposit(this,QuantityValue(this[tile]),nativeAmount):SpatialResourceValues.CheckWithdrawal(this, QuantityValue(this[tile]), nativeAmount);
            if (!check.Ready) return new SpatialResourceChangeCommit(check);
            TileMap.ValueWrite write = Map.PrepareValueWrite(this, new[] { tile });
            T after = StoredValue(check.StoredAfter);
            return new SpatialResourceChangeCommit(check, () => CommitSample(tile, after, write));
        }
        public SpatialResourceChange CheckDeposit(TileCoord tile,Fix nativeAmount)
        {
            Map.RequireHealthyMutation();
            RequireSettled();if(!Map.InBounds(tile))return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidLocation);
            var check=SpatialResourceValues.CheckDeposit(this,QuantityValue(this[tile]),nativeAmount);
            return check.Ready&&!_resourceCells.AllowsQuantity(CellKey(tile),check.After)?SpatialResourceChange.Refused(SpatialResourceChangeStatus.ReservedQuantity):check;
        }
        public SpatialResourceChange Deposit(TileCoord tile,Fix nativeAmount)
        {var check=CheckDeposit(tile,nativeAmount);return !check.Ready?check:PrepareDeposit(tile,nativeAmount).Commit();}

        /// <summary>Generate privately, then replace the layer and invalidate once. Failure retains prior values.</summary>
        public void Fill(Func<int, int, T> generator)
        {
            Map.RequireHealthyMutation();
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            RequireSettled();
            var generated = new Dictionary<long, T[]>();
            _generating = true;
            try
            {
                for (int y = 0; y < Map.Height; y++)
                    for (int x = 0; x < Map.Width; x++)
                    {
                        T value = generator(x, y);
                        UnitConversionStatus legality = CheckValue(value);
                        if (legality != UnitConversionStatus.Converted) throw new ArgumentException("invalid generated spatial resource quantity: " + Id + "/" + legality, nameof(generator));
                        if (IsZero(value)) continue;
                        long key = ChunkKey(x / ChunkSize, y / ChunkSize);
                        if (!generated.TryGetValue(key, out T[] chunk))
                        {
                            chunk = new T[ChunkSize * ChunkSize];
                            generated.Add(key, chunk);
                        }
                        chunk[(y % ChunkSize) * ChunkSize + x % ChunkSize] = value;
                    }
            }
            finally { _generating = false; }
            // Callbacks can touch other layers; validate current counters at the commit boundary.
            IReadOnlyList<long> changes = _resourceCells.PreflightChanges(key => NativeValue(ValueIn(generated, CellTile(key))));
            TileMap.ValueWrite write = Map.PrepareValueWrite(this, ChangedTiles(generated));
            if (write == null) return;
            Map.CommitValueWrite(write, () => _chunks = generated, () => _resourceCells.ChangesCommitted(changes));
        }

        // Compare actual sparse stores. Includes erased cells and new chunks, never a second world grid.
        private IEnumerable<TileCoord> ChangedTiles(Dictionary<long, T[]> generated)
        {
            var keys = new SortedSet<long>(_chunks.Keys); keys.UnionWith(generated.Keys);
            foreach (long key in keys)
            {
                _chunks.TryGetValue(key, out T[] previous); generated.TryGetValue(key, out T[] next);
                int x = ChunkX(key) * ChunkSize, y = ChunkY(key) * ChunkSize;
                for (int offset = 0; offset < ChunkSize * ChunkSize; offset++)
                {
                    if (EqualityComparer<T>.Default.Equals(previous == null ? default : previous[offset], next == null ? default : next[offset])) continue;
                    var tile = new TileCoord(x + offset % ChunkSize, y + offset / ChunkSize);
                    if (!Map.InBounds(tile)) throw new CorruptStateException("changed layer sample outside its authored space");
                    yield return tile;
                }
            }
        }

        public void CopyNonZeroTiles(IList<TileCoord> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            var cells = new List<TileCoord>();
            foreach (long key in OrderedKeys(_chunks))
            {
                int x = ChunkX(key) * ChunkSize, y = ChunkY(key) * ChunkSize;
                T[] chunk = _chunks[key];
                for (int offset = 0; offset < chunk.Length; offset++)
                    if (!IsZero(chunk[offset]))
                        cells.Add(new TileCoord(x + offset % ChunkSize, y + offset / ChunkSize));
            }
            cells.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            foreach (var cell in cells) into.Add(cell);
        }

        private void RequireSettled()
        {
            if (_generating) throw new InvalidOperationException("same-layer mutation or checkpoint during generation: " + Id);
        }
        private static bool HasValues(T[] values)
        {
            // Byte zero is exactly an all-zero word. Inspect current storage in
            // bounded groups, not one generic equality call for every empty cell.
            // This is only the existing nonempty predicate: padding, material
            // quantities and every codec/hash byte still pass their original checks.
            if (values is byte[] bytes)
            {
                int offset = 0;
                for (; offset <= bytes.Length - sizeof(ulong); offset += sizeof(ulong))
                    if (System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(
                        new ReadOnlySpan<byte>(bytes, offset, sizeof(ulong))) != 0) return true;
                for (; offset < bytes.Length; offset++) if (bytes[offset] != 0) return true;
                return false;
            }
            for (int i = 0; i < values.Length; i++) if (!IsZero(values[i])) return true;
            return false;
        }
        private void ValidateValues(long key, T[] values)
        {
            if (values == null || values.Length != ChunkSize * ChunkSize)
                throw new CorruptStateException("map layer chunk has an invalid value shape");
            int width = System.Math.Min(ChunkSize, Map.Width - ChunkX(key) * ChunkSize);
            int height = System.Math.Min(ChunkSize, Map.Height - ChunkY(key) * ChunkSize);
            if (ResourceMeasure != SpatialResourceMeasure.Quantity)
            {
                // Non-material samples have no per-value quantity legality. A complete
                // chunk only needs nonempty storage; partial chunks also need zero padding.
                // SyncValues still serializes/hashes every cell, with no validation cache.
                if (!HasValues(values)) throw new CorruptStateException("map layer contains an empty sparse chunk");
                if (width == ChunkSize && height == ChunkSize) return;
                for (int y = 0; y < ChunkSize; y++)
                    for (int x = y < height ? width : 0; x < ChunkSize; x++)
                        if (!IsZero(values[y * ChunkSize + x]))
                            throw new CorruptStateException("map layer has nonzero padding outside its authored space");
                return;
            }
            bool occupied = false;
            for (int i = 0; i < values.Length; i++)
            {
                if (IsZero(values[i])) continue;
                if (CheckValue(values[i]) != UnitConversionStatus.Converted)
                    throw new CorruptStateException("invalid checkpoint spatial resource quantity: " + Id);
                if (i % ChunkSize >= width || i / ChunkSize >= height)
                    throw new CorruptStateException("map layer has nonzero padding outside its authored space");
                occupied = true;
            }
            if (!occupied) throw new CorruptStateException("map layer contains an empty sparse chunk");
        }

        protected abstract void SyncValues(IStateSync sync, T[] values);
        private static readonly Func<(MapLayer<T> Layer, Dictionary<long, T[]> Chunks), long, Fix> ReadCheckpointQuantity =
            (context, key) => context.Layer.NativeValue(context.Layer.ValueIn(context.Chunks, CellTile(key)));
        internal sealed override void Sync(IStateSync sync)
        {
            // Fill's candidate is private; observation fingerprints inspect only the unchanged
            // committed chunks. Normal hashes, saves and restores still reject an in-flight Fill.
            if (!(sync is IStateObservationSync observation && observation.IsObservation)) RequireSettled();
            long[] keys = sync.Reading ? null : OrderedKeys(_chunks);
            int count = sync.Count(_chunks.Count);
            Dictionary<long, T[]> restored = sync.Reading ? new Dictionary<long, T[]>() : null;
            long previous = -1;
            for (int i = 0; i < count; i++)
            {
                long key = SyncChunkKey(sync, keys, i, ref previous);
                T[] values = sync.Reading ? new T[ChunkSize * ChunkSize] : _chunks[key];
                if (!sync.Reading) ValidateValues(key, values);
                SyncValues(sync, values);
                if (sync.Reading)
                {
                    ValidateValues(key, values);
                    restored.Add(key, values);
                }
            }
            var candidate = sync.Reading ? restored : _chunks;
            Action publish = _resourceCells.StageSync(sync, (this, candidate), ReadCheckpointQuantity);
            if (sync.Reading) { _chunks = restored; publish(); }
        }
    }

    public sealed class ByteLayer : MapLayer<byte>
    {
        internal ByteLayer(ISpatialLayerDefinition definition, TileMap map) : base(definition, map) { }
        protected override Fix QuantityValue(byte value) => Fix.FromInt(value);
        protected override byte StoredValue(Fix value) => checked((byte)(value.Raw / Fix.One.Raw));
        protected override void SyncValues(IStateSync sync, byte[] values) => sync.Bytes(values);
    }

    public sealed class IntLayer : MapLayer<int>
    {
        internal IntLayer(ISpatialLayerDefinition definition, TileMap map) : base(definition, map) { }
        protected override Fix QuantityValue(int value) => Fix.FromInt(value);
        protected override int StoredValue(Fix value) => checked((int)(value.Raw / Fix.One.Raw));
        protected override void SyncValues(IStateSync sync, int[] values)
        { for (int i = 0; i < values.Length; i++) sync.Int(ref values[i]); }
    }

    public sealed class FixedLayer : MapLayer<Fix>
    {
        internal FixedLayer(ISpatialLayerDefinition definition, TileMap map) : base(definition, map) { }
        protected override Fix QuantityValue(Fix value) => value;
        protected override Fix StoredValue(Fix value) => value;
        protected override void SyncValues(IStateSync sync, Fix[] values)
        { for (int i = 0; i < values.Length; i++) sync.Fix(ref values[i]); }
    }

    /// <summary>
    /// The tile map: a square grid with 8-way movement, holding named layers that modules register
    /// (terrain, water, roads, pipes...). All layers are saved and hashed. Topology is simulation; how a
    /// tile is projected on screen (top-down, isometric) is the host's business.
    /// </summary>
    public sealed partial class TileMap : ITileNavigationSpace, ISpatialOverlap, ISpatialLayerProvider, ISpatialVisibilityInvalidation, ISpatialInteraction, ISpatialRollout, ISpatialConstructionPlacement, ISpatialRecoveryPlacement, ISpatialPlacementTemplates, ITileSampleMutationSpace, ISpatialRegionChangeSource<TileRect>
    {
        public const int ChunkSize = 16;
        public int ObservationVersion => Version;

        public SpatialOverlapStatus CheckOverlap(World world, SpatialSnapshot left, SpatialSnapshot right)
            => TileSpatialGeometry.CheckOverlap(world, this, left, right);

        private readonly List<MapLayer> _layers = new List<MapLayer>();
        private readonly Dictionary<string, MapLayer> _byId = new Dictionary<string, MapLayer>(StringComparer.Ordinal);
        private readonly Dictionary<long, int> _chunkVersions = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _publicChunkVersions = new Dictionary<long, int>();
        private readonly Dictionary<PlayerId, VisibleChanges> _privateChanges = new Dictionary<PlayerId, VisibleChanges>();
        private readonly IReadOnlyList<MapLayer> _layerView;
        private int _version;
        private int _publicVersion;
        private int _fullVersion;
        private int _publicFullVersion;
        private bool _layersFrozen;
        private World _world;
        private bool HasVisibility => _world != null && _world.Services.Has<IVisibility>();
        internal bool LayersFrozen => _layersFrozen;
        private readonly Dictionary<TileCoord, Entity> _occupants = new Dictionary<TileCoord, Entity>();

        // Derived presentation invalidation only. Values still live solely in each layer's Sync.
        private sealed class VisibleChanges
        {
            public int Version;
            public int FullVersion;
            public readonly Dictionary<long, int> Chunks = new Dictionary<long, int>();
            public HashSet<string> ReadableScopes = new HashSet<string>(StringComparer.Ordinal);
        }

        public TileMap(string id, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("space identity is required", nameof(id));
            Id = id;
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "tile space dimensions must be positive");
            Width = width;
            Height = height;
            ChunksX = (width - 1) / ChunkSize + 1;
            ChunksY = (height - 1) / ChunkSize + 1;
            _layerView = _layers.AsReadOnly();
        }

        public int Width { get; }
        public string Id { get; }
        public int StateSchemaVersion => 6;
        public int Height { get; }
        public int ChunksX { get; }
        public int ChunksY { get; }
        public TileRect Bounds => new TileRect(0, 0, Width, Height);

        /// <summary>Bumped by every tile write: a presenter compares it to know whether anything changed.</summary>
        public int Version => _version;

        public int ChunkVersion(int cx, int cy)
        {
            ValidateChunk(cx, cy);
            return _chunkVersions.TryGetValue(ChunkKey(cx, cy), out int version) ? Math.Max(version, _fullVersion) : _fullVersion;
        }

        /// <summary>A viewer-local invalidation cursor; other companies' private writes cannot change it.</summary>
        public int VersionFor(PlayerId viewer)
            => viewer == PlayerId.None ? _version : !viewer.IsValid || _world != null && !_world.Players.IsSeat(viewer) ? 0
                : _privateChanges.TryGetValue(viewer, out VisibleChanges changes) ? changes.Version : _publicVersion;

        public int ChunkVersionFor(PlayerId viewer, int cx, int cy)
        {
            ValidateChunk(cx, cy);
            long key = ChunkKey(cx, cy);
            if (viewer == PlayerId.None)
                return _chunkVersions.TryGetValue(key, out int version) ? Math.Max(version, _fullVersion) : _fullVersion;
            if (!viewer.IsValid || _world != null && !_world.Players.IsSeat(viewer)) return 0;
            if (_privateChanges.TryGetValue(viewer, out VisibleChanges changes))
                return changes.Chunks.TryGetValue(key, out int ownerVersion) ? Math.Max(ownerVersion, changes.FullVersion) : changes.FullVersion;
            return _publicChunkVersions.TryGetValue(key, out int publicVersion) ? Math.Max(publicVersion, _publicFullVersion) : _publicFullVersion;
        }

        private void ValidateChunk(int x, int y)
        { if (x < 0 || y < 0 || x >= ChunksX || y >= ChunksY) throw new ArgumentOutOfRangeException(nameof(x)); }

        private static long ChunkKey(int x, int y) => ((long)y << 32) | (uint)x;

        internal long[] ChangedChunkKeys(PlayerId viewer, int sinceVersion, out bool fullInvalidation)
        {
            if (viewer != PlayerId.None && (!viewer.IsValid || _world != null && !_world.Players.IsSeat(viewer)))
            { fullInvalidation = false; return Array.Empty<long>(); }
            int full = viewer == PlayerId.None ? _fullVersion
                : _privateChanges.TryGetValue(viewer, out VisibleChanges visible) ? visible.FullVersion : _publicFullVersion;
            fullInvalidation = sinceVersion < full;
            if (fullInvalidation) return Array.Empty<long>();
            Dictionary<long, int> changed = viewer == PlayerId.None ? _chunkVersions
                : _privateChanges.TryGetValue(viewer, out visible) ? visible.Chunks : _publicChunkVersions;
            var keys = new List<long>();
            foreach (KeyValuePair<long, int> entry in changed)
                if (entry.Value > sinceVersion && ChunkVersionFor(viewer, (int)(uint)entry.Key, (int)(entry.Key >> 32)) > sinceVersion)
                    keys.Add(entry.Key);
            keys.Sort();
            return keys.ToArray();
        }

        /// <summary>Original native chunk history, exposed as detached geometry, never copied to a view.</summary>
        public SpatialRegionChangeSet<TileRect> ReadRegionChanges(PlayerId viewer, int sinceVersion)
        {
            if (_world == null) throw new InvalidOperationException("regional changes require the actual composed map");
            _world.RequireHealthyUpdate();
            int version = VersionFor(viewer);
            if (sinceVersion < 0 || sinceVersion > version) throw new ArgumentOutOfRangeException(nameof(sinceVersion));
            long[] changed = ChangedChunkKeys(viewer, sinceVersion, out bool full);
            if (full) return SpatialRegionChangeSet<TileRect>.Full(version);
            var regions = new TileRect[changed.Length];
            for (int i = 0; i < changed.Length; i++) {
                int cx = (int)(uint)changed[i], cy = (int)(changed[i] >> 32);
                int x = cx * ChunkSize, y = cy * ChunkSize;
                regions[i] = new TileRect(x, y, System.Math.Min(ChunkSize, Width - x), System.Math.Min(ChunkSize, Height - y));
            }
            return SpatialRegionChangeSet<TileRect>.Incremental(version, regions);
        }

        public bool InBounds(TileCoord t) => t.X >= 0 && t.Y >= 0 && t.X < Width && t.Y < Height;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(TileRect r) => r.X >= 0 && r.Y >= 0 && r.Right <= Width && r.Bottom <= Height;

        public Entity OccupantAt(TileCoord tile) => _occupants.TryGetValue(tile, out Entity entity) && entity.Alive ? entity : null;
        public bool IsFree(TileRect area)
        {
            if (!InBounds(area)) return false;
            for (int y = area.Y; y < area.Bottom; y++)
                for (int x = area.X; x < area.Right; x++) if (OccupantAt(new TileCoord(x, y)) != null) return false;
            return true;
        }
        public Entity SpawnAt(World world, EntityDef definition, PlayerId owner, TileCoord origin)
        {
            ITilePresenceDefinition presence = definition.Component<ITilePresenceDefinition>() ?? throw new ContentException(definition.Id + ": missing tile presence definition");
            FixVec2 centre = new FixVec2(Fix.FromInt(origin.X) + Fix.FromRatio(presence.Width, 2), Fix.FromInt(origin.Y) + Fix.FromRatio(presence.Height, 2));
            var placement = new TilePlacement(Id, origin, centre);
            Code refusal = CheckPlacement(world, definition, placement, null);
            if (!refusal.IsNone) throw new InvalidOperationException("tile placement refused: " + refusal);
            return world.Spawn(definition, owner, e => ConfigurePlacement(e, placement));
        }
        public Entity SpawnMobile(World world, EntityDef definition, PlayerId owner, FixVec2 position)
        {
            ITilePresenceDefinition presence = definition.Component<ITilePresenceDefinition>() ?? throw new ContentException(definition.Id + ": missing tile presence definition");
            if (!presence.Movable || presence.Space != Id || !ReferenceEquals(world.Space, this)) throw new ArgumentException("definition does not declare mobility in this space");
            var placement = new TilePlacement(Id, position.Tile, position);
            Code refusal = CheckPlacement(world, definition, placement, null);
            if (!refusal.IsNone) throw new InvalidOperationException("tile placement refused: " + refusal);
            return world.Spawn(definition, owner, e => ConfigurePlacement(e, placement));
        }

        public SpatialPlacement CapturePlacement(Entity source)
        {
            ITilePresence presence = source.Get<ITilePresence>();
            return presence != null && presence.Placed && presence.Space == Id
                ? new TilePlacement(Id, presence.Tile, presence.Position) : null;
        }

        public SpatialPlacement ReadPlacement(JsonValue arguments)
        {
            var reader = new JsonReader(arguments, Id + ".placement");
            string space = reader.String("space"); int x = reader.Int("x"), y = reader.Int("y"); reader.End();
            if (space != Id) throw reader.Error("placement names a different selected space");
            var origin = new TileCoord(x, y); return new TilePlacement(Id, origin, FixVec2.Centre(origin));
        }
        public JsonValue WritePlacement(SpatialPlacement placement)
        {
            if (!(placement is TilePlacement tile) || tile.Space != Id) throw new ContentException("incompatible tile placement serialization");
            return TilePlacement.Arguments(Id, tile.Origin);
        }
        public string PlacementIdentity(SpatialPlacement placement)
        {
            if (!(placement is TilePlacement tile) || tile.Space != Id) throw new ContentException("incompatible tile placement identity");
            return Id + ":" + tile.Origin.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + tile.Origin.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        private (long X, long Y) ReadRelativePlacement(JsonValue relative)
        {
            var reader = new JsonReader(relative, Id + ".relative-placement"); string space = reader.String("space");
            long x = reader.Long("dx"), y = reader.Long("dy"); reader.End();
            if (space != Id) throw reader.Error("template names another space"); return (x, y);
        }
        public void ValidateTemplatePlacement(JsonValue relative) => ReadRelativePlacement(relative);
        public JsonValue CaptureTemplatePlacement(JsonValue placement, JsonValue origin)
        {
            var at = (TilePlacement)ReadPlacement(placement); var from = (TilePlacement)ReadPlacement(origin);
            var result = JsonValue.NewObject(); result.Set("space", JsonValue.Of(Id));
            result.Set("dx", JsonValue.Number(((long)at.Origin.X - from.Origin.X).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            result.Set("dy", JsonValue.Number(((long)at.Origin.Y - from.Origin.Y).ToString(System.Globalization.CultureInfo.InvariantCulture))); return result;
        }
        public JsonValue PlaceTemplatePlacement(JsonValue relative, JsonValue anchor)
        {
            var delta = ReadRelativePlacement(relative); var at = (TilePlacement)ReadPlacement(anchor);
            int x = checked((int)checked(at.Origin.X + delta.X)), y = checked((int)checked(at.Origin.Y + delta.Y));
            return TilePlacement.Arguments(Id, new TileCoord(x, y));
        }
        public Code CheckConstructionDefinition(EntityDef target, EntityDef site)
            => TilePlacementRules.CheckConstructionDefinition(this, target, site);

        public Code CheckPlacement(World world, EntityDef target, SpatialPlacement placement, Entity replacing)
            => TilePlacementRules.Check(world, this, target, placement, replacing);
        public Code CheckRecoveryDefinition(EntityDef container)
        {
            ITilePresenceDefinition presence = container?.Component<ITilePresenceDefinition>();
            return presence != null && presence.Space == Id && !presence.Blocks && !presence.Movable ? Code.None : Codes.InvalidArgument;
        }

        public void ConfigurePlacement(Entity entity, SpatialPlacement placement)
        {
            if (!(placement is TilePlacement tile) || tile.Space != Id) throw new ArgumentException("incompatible tile placement");
            ITilePresence presence = entity.Get<ITilePresence>() ?? throw new ArgumentException("missing tile presence");
            FixVec2 position = presence.Movable ? tile.Position : new FixVec2(
                Fix.FromInt(tile.Origin.X) + Fix.FromRatio(presence.Footprint.W, 2),
                Fix.FromInt(tile.Origin.Y) + Fix.FromRatio(presence.Footprint.H, 2));
            presence.Place(tile.Origin, position);
        }
        public Code CheckRolloutDefinition(World world, EntityDef producer, EntityDef output)
            => TileRolloutRules.CheckDefinition(world, this, producer, output);
        public Code FindRollout(World world, EntityRolloutRequest request, out SpatialPlacement placement)
            => TileRolloutRules.Find(world, this, request, out placement);
        public Code CheckRolloutPlacement(World world, EntityRolloutRequest request, SpatialPlacement placement)
            => TileRolloutRules.CheckPlacement(world, this, request, placement);

        public void ValidateComposition(World world)
        {
            foreach (EntityDef definition in world.Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition spatial = definition.Component<ISpatialPresenceDefinition>();
                if (spatial != null && spatial.Space == Id && !definition.HasCapability<ITilePresence>())
                    throw new ContentException(definition.Id + ": selected tile space cannot host this presence contract");
            }
            _layersFrozen = true;
            _world = world;
        }
        public void OnSpawn(Entity entity)
        {
            ITilePresence presence = entity.Get<ITilePresence>();
            if (presence == null) return;
            if (!presence.Placed || presence.Space != Id || !InBounds(presence.Footprint)) throw new InvalidOperationException("tile presence is not placed in the selected space");
            if (presence.Blocks)
            {
                if (!IsFree(presence.Footprint)) throw new InvalidOperationException("tile footprint is occupied");
                Claim(entity, presence.Footprint);
            }
        }
        private void Claim(Entity entity, TileRect area)
        {
            for (int y = area.Y; y < area.Bottom; y++)
                for (int x = area.X; x < area.Right; x++) _occupants.Add(new TileCoord(x, y), entity);
        }
        private void Release(Entity entity, TileRect area)
        {
            for (int y = area.Y; y < area.Bottom; y++)
                for (int x = area.X; x < area.Right; x++)
                {
                    var tile = new TileCoord(x, y);
                    if (_occupants.TryGetValue(tile, out Entity owner) && ReferenceEquals(owner, entity)) _occupants.Remove(tile);
                }
        }
        public void OnDespawn(Entity entity)
        {
            ITilePresence presence = entity.Get<ITilePresence>();
            if (presence != null && presence.Placed && presence.Blocks) Release(entity, presence.Footprint);
        }
        public void Relocate(World world, ITilePresence presence, TileCoord tile)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate();
            if (!ReferenceEquals(world.Space, this) || presence == null || presence.Entity == null || presence.Space != Id || !presence.Placed || !presence.Movable ||
                !ReferenceEquals(world.Entities.Get(presence.Entity.Id), presence.Entity) ||
                !ReferenceEquals(presence.Entity.Get<ITilePresence>(), presence) ||
                !InBounds(new TileRect(tile, presence.Footprint.W, presence.Footprint.H)))
                throw new ArgumentException("relocation requires its actual live mobile presence and selected space");
            if (!presence.Blocks || tile == presence.Tile) return;
            var area = new TileRect(tile, presence.Footprint.W, presence.Footprint.H);
            for (int y = area.Y; y < area.Bottom; y++)
                for (int x = area.X; x < area.Right; x++)
                {
                    Entity other = OccupantAt(new TileCoord(x, y));
                    if (other != null && !ReferenceEquals(other, presence.Entity)) throw new InvalidOperationException("movement is blocked");
                }
            Release(presence.Entity, presence.Footprint); Claim(presence.Entity, area);
        }
        public void OnLoaded(World world)
        {
            foreach (MapLayer layer in _layers)
                if (layer.Owner.IsValid && !world.Players.IsSeat(layer.Owner))
                    throw new CorruptStateException("spatial layer '" + layer.Id + "' has an owner outside this session");
            _occupants.Clear();
            foreach (Entity entity in world.Entities.All) OnSpawn(entity);
        }
        public SpatialWorldSnapshot Capture(PlayerId viewer) => new TileWorldSnapshot(Id, VersionFor(viewer), Width, Height);

        /// <summary>The same ordinary reach geometry used by live approach and prospective admission.</summary>
        public TileRect InteractionGoal(TileRect footprint, int actorWidth, int actorHeight)
            => TileSpatialGeometry.AdjacentGoal(this, footprint, actorWidth, actorHeight);

        public bool TryEstimateCost(World world, Entity actor, Entity target, out long cost)
            => TileInteractionRules.TryEstimateCost(world, this, actor, target, out cost);

        public SpatialInteractionStatus Check(World world, Entity actor, Entity target)
            => TileInteractionRules.Check(world, this, actor, target);

        public SpatialProximityStatus CheckProximity(World world, Entity actor, Entity target)
            => TileInteractionRules.CheckProximity(world, this, actor, target);

        public SpatialInteractionStatus Approach(World world, Entity actor, Entity target)
            => TileInteractionRules.Approach(world, this, actor, target);

        public void RegisterLayer(ISpatialLayerDefinition definition)
        {
            if (_layersFrozen) throw new InvalidOperationException("spatial layers are registered during composition");
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Space?.Id != Id)
                throw new ContentException(definition.Id + ": spatial layer belongs to another space");
            switch (definition.ValueType)
            {
                case SpatialLayerValueType.Byte: Add(new ByteLayer(definition, this)); break;
                case SpatialLayerValueType.Int32: Add(new IntLayer(definition, this)); break;
                case SpatialLayerValueType.Fixed: Add(new FixedLayer(definition, this)); break;
                default: throw new ContentException(definition.Id + ": unsupported spatial layer value type");
            }
        }

        public void AssignOwner(World world, string layerId, PlayerId owner)
        {
            if (world == null || !ReferenceEquals(world.Space, this)) throw new ArgumentException("ownership must be assigned through this layer's session", nameof(world));
            world.RequireHealthyUpdate();
            MapLayer layer = Layer<MapLayer>(layerId);
            if (layer.Visibility != SpatialLayerVisibility.CompanyPrivate)
                throw new InvalidOperationException("only company-private spatial layers can be assigned an owner");
            if (!world.Players.IsSeat(owner)) throw new ArgumentException("a company-private layer requires a seat in its session", nameof(owner));
            if (layer.Owner == owner) return;
            int layerVersion = layer.Version, mapVersion = _version, publicVersion = _publicVersion;
            int nextLayerVersion = checked(layerVersion + 1), nextMapVersion = checked(mapVersion + 1);
            PlayerId previous = layer.Owner;
            var updates = HasVisibility ? PrepareScopeChanges(world, layer, owner)
                : new List<(PlayerId Viewer, int Version, HashSet<string> Scopes)>();
            if (!HasVisibility) {
                if (previous.IsValid) updates.Add((previous, CurrentPrivateVersion(previous), null));
                updates.Add((owner, CurrentPrivateVersion(owner), null));
            }
            foreach (var update in updates) _ = checked(update.Version + 1);
            layer.PreflightOwnerChange();
            if (layer.Owner != previous || layer.Version != layerVersion || _version != mapVersion || _publicVersion != publicVersion)
                throw new InvalidOperationException("prepared native ownership changed before publication");
            ValidateScopeChanges(updates);
            world.Services.Get<ISpatialMutation>().Commit(() => {
                layer.Owner = owner; layer.Version = nextLayerVersion;
                _version = nextMapVersion; _fullVersion = nextMapVersion;
                PublishScopeChanges(updates);
                layer.OwnerChanged();
            });
        }

        private VisibleChanges EnsurePrivateChanges(PlayerId owner)
        {
            if (!_privateChanges.TryGetValue(owner, out VisibleChanges changes))
            {
                changes = new VisibleChanges();
                if (!HasVisibility)
                {
                    changes.Version = _publicVersion; changes.FullVersion = _publicFullVersion;
                    foreach (KeyValuePair<long, int> chunk in _publicChunkVersions) changes.Chunks.Add(chunk.Key, chunk.Value);
                }
                _privateChanges.Add(owner, changes);
            }
            return changes;
        }

        public void RefreshVisibility(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("tile visibility requires its actual session");
            world.RequireHealthyUpdate();
            if (!HasVisibility) return;
            var updates = PrepareScopeChanges(world);
            ValidateScopeChanges(updates); PublishScopeChanges(updates);
        }
        private List<(PlayerId Viewer, int Version, HashSet<string> Scopes)> PrepareScopeChanges(World world,
            MapLayer proposedLayer = null, PlayerId? proposedOwner = null)
        {
            var updates = new List<(PlayerId Viewer, int Version, HashSet<string> Scopes)>();
            for (int i = 0; i < world.Players.Count; i++)
            {
                // An actual empty scoped baseline must exist too: absence otherwise means the
                // original unscoped public history to VersionFor/ChunkVersionFor consumers.
                var viewer = new PlayerId(i); var changes = EnsurePrivateChanges(viewer);
                var keys = new List<string>();
                foreach (MapLayer layer in _layers)
                    SpatialLayerAccess.CopyObservationKeys(world, layer, viewer, keys,
                        ReferenceEquals(layer, proposedLayer) ? proposedOwner : null);
                var readable = new HashSet<string>(keys, StringComparer.Ordinal);
                if (changes.ReadableScopes.SetEquals(readable)) continue;
                int version = CurrentPrivateVersion(viewer); _ = checked(version + 1);
                updates.Add((viewer, version, readable));
            }
            return updates;
        }
        private void ValidateScopeChanges(IReadOnlyList<(PlayerId Viewer, int Version, HashSet<string> Scopes)> updates)
        {
            foreach (var update in updates)
                if (CurrentPrivateVersion(update.Viewer) != update.Version)
                    throw new InvalidOperationException("prepared native readable scope changed before publication");
        }
        private void PublishScopeChanges(IReadOnlyList<(PlayerId Viewer, int Version, HashSet<string> Scopes)> updates)
        {
            foreach (var update in updates)
            {
                var changes = EnsurePrivateChanges(update.Viewer);
                changes.Version = checked(update.Version + 1); changes.FullVersion = changes.Version;
                if (update.Scopes != null) { changes.ReadableScopes = update.Scopes; changes.Chunks.Clear(); }
            }
        }

        internal SpatialSnapshot SampleSnapshot(TileCoord tile)
            => TileSpatialGeometry.SampleSnapshot(this, tile);

        private bool SeesSample(PlayerId viewer, MapLayer layer, TileCoord sample)
            => HasVisibility ? SpatialLayerAccess.CanObserveSample(_world, layer, viewer, SampleSnapshot(sample))
                : SpatialLayerAccess.CanRead(layer.Visibility, layer.Owner, viewer);

        private T Add<T>(T layer) where T : MapLayer
        {
            if (_byId.ContainsKey(layer.Id)) throw new InvalidOperationException("map layer '" + layer.Id + "' added twice");
            _layers.Add(layer);
            _byId.Add(layer.Id, layer);
            if (layer.Owner.IsValid) EnsurePrivateChanges(layer.Owner);
            return layer;
        }

        public T Layer<T>(string id) where T : MapLayer
            => _byId.TryGetValue(id, out MapLayer l) && l is T t ? t : throw new InvalidOperationException("no map layer '" + id + "'");

        public bool HasLayer(string id) => _byId.ContainsKey(id);

        public IReadOnlyList<MapLayer> Layers => _layerView;
        IReadOnlyList<ISpatialLayer> ISpatialLayerProvider.SpatialLayers => _layerView;

        // Short-lived native invalidation facts, never persisted or exposed as retained authority.
        internal sealed class ValueWrite
        {
            internal TileMap Map;
            internal MapLayer Layer;
            internal PlayerId Owner;
            internal int LayerVersion, MapVersion, PublicVersion;
            internal long[] Chunks;
            internal readonly SortedDictionary<int, (int Version, long[] Chunks)> Viewers =
                new SortedDictionary<int, (int, long[])>();
        }
        internal sealed class LayerWrite
        {
            internal readonly ValueWrite Cursor;
            internal readonly Action Validate, Publish, Notify;
            internal LayerWrite(ValueWrite cursor, Action validate, Action publish, Action notify)
            { Cursor = cursor; Validate = validate; Publish = publish; Notify = notify; }
        }
        public void ApplySamples(IReadOnlyList<TileSampleMutation> mutations)
        {
            _world.RequireHealthyUpdate();
            if (mutations == null || mutations.Count == 0) throw new ArgumentException("explicit tile sample mutation set required");
            var groups = new SortedDictionary<string, List<TileSampleMutation>>(StringComparer.Ordinal);
            foreach (var mutation in mutations) {
                var layer = TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(_world, this, mutation.Layer, false);
                if (layer is not MapLayer) throw new ArgumentException("native batch requires its actual layer owner");
                if (!groups.TryGetValue(layer.Id, out var group)) groups.Add(layer.Id, group = new List<TileSampleMutation>());
                group.Add(mutation);
            }
            var writes = new List<LayerWrite>();
            foreach (var group in groups) {
                var write = _byId[group.Key].PrepareSamples(group.Value);
                if (write != null) writes.Add(write);
            }
            int publicChanges = 0; var viewerChanges = new SortedDictionary<int, int>();
            foreach (var write in writes) {
                ValidateValueWrite(write.Cursor); write.Validate();
                if (write.Cursor.Layer.Visibility == SpatialLayerVisibility.Public) publicChanges++;
                foreach (int viewer in write.Cursor.Viewers.Keys)
                    viewerChanges[viewer] = checked((viewerChanges.TryGetValue(viewer, out int count) ? count : 0) + 1);
            }
            _ = checked(_version + writes.Count); _ = checked(_publicVersion + publicChanges);
            foreach (var viewer in viewerChanges) _ = checked(CurrentPrivateVersion(new PlayerId(viewer.Key)) + viewer.Value);
            // All preflight callbacks precede publication. Derived viewer records use the old baseline.
            _world.Services.Get<ISpatialMutation>().Commit(() => {
                foreach (int viewer in viewerChanges.Keys) EnsurePrivateChanges(new PlayerId(viewer));
                foreach (var write in writes) write.Publish();
                foreach (var write in writes) PublishValueWrite(write.Cursor);
                foreach (var write in writes) write.Notify();
            });
        }

        private int CurrentPrivateVersion(PlayerId viewer)
            => _privateChanges.TryGetValue(viewer, out VisibleChanges changes) ? changes.Version : HasVisibility ? 0 : _publicVersion;

        // One exact counter/visibility preparation shared by Set and Fill.
        internal ValueWrite PrepareValueWrite(MapLayer layer, IEnumerable<TileCoord> samples)
        {
            if (layer == null || !ReferenceEquals(layer.Map, this) ||
                !_byId.TryGetValue(layer.Id, out MapLayer actual) || !ReferenceEquals(actual, layer) || samples == null)
                throw new ArgumentException("value writes require the actual layer/provider");

            var all = new SortedSet<long>();
            var affected = new SortedDictionary<int, SortedSet<long>>();
            var companies = new SortedSet<int>();
            // No viewer has a published baseline while the engine runs generation stages.
            // StartNewGame resolves scopes and marks their full baseline after all initial values settle.
            // Runtime writes retain the original per-sample authorization and cursor accounting.
            if (_world?.Generating != true)
            {
                foreach (PlayerId viewer in _privateChanges.Keys) companies.Add(viewer.Value);
                if (_world != null) for (int i = 0; i < _world.Players.Count; i++) companies.Add(i);
                if (layer.Owner.IsValid) companies.Add(layer.Owner.Value);
            }
            foreach (TileCoord sample in samples)
            {
                if (!InBounds(sample)) throw new ArgumentOutOfRangeException(nameof(samples));
                long chunk = ChunkKey(sample.X / ChunkSize, sample.Y / ChunkSize);
                all.Add(chunk);
                foreach (int company in companies)
                    if (SeesSample(new PlayerId(company), layer, sample))
                    {
                        if (!affected.TryGetValue(company, out SortedSet<long> chunks))
                            affected.Add(company, chunks = new SortedSet<long>());
                        chunks.Add(chunk);
                    }
            }
            if (all.Count == 0) return null;
            var write = new ValueWrite { Map = this, Layer = layer, Owner = layer.Owner,
                LayerVersion = layer.Version, MapVersion = _version, PublicVersion = _publicVersion,
                Chunks = new List<long>(all).ToArray() };
            _ = checked(layer.Version + 1); _ = checked(_version + 1);
            if (layer.Visibility == SpatialLayerVisibility.Public) _ = checked(_publicVersion + 1);
            foreach (var entry in affected)
            {
                int version = CurrentPrivateVersion(new PlayerId(entry.Key));
                _ = checked(version + 1);
                write.Viewers.Add(entry.Key, (version, new List<long>(entry.Value).ToArray()));
            }
            return write;
        }

        internal void ValidateValueWrite(ValueWrite write)
        {
            if (write == null || !ReferenceEquals(write.Map, this) || write.Layer.Owner != write.Owner ||
                write.Layer.Version != write.LayerVersion || _version != write.MapVersion || _publicVersion != write.PublicVersion)
                throw new InvalidOperationException("prepared layer write changed before native publication");
            foreach (var entry in write.Viewers)
                if (CurrentPrivateVersion(new PlayerId(entry.Key)) != entry.Value.Version)
                    throw new InvalidOperationException("prepared viewer cursor changed before native publication");
        }
        internal void RequireHealthyMutation()
        {
            if (_world == null) throw new InvalidOperationException("spatial mutation requires its actual composed session");
            _world.RequireHealthyUpdate();
        }
        internal void CommitValueWrite(ValueWrite write, Action publish, Action notify)
        {
            RequireHealthyMutation();
            if (publish == null || notify == null) throw new ArgumentNullException("spatial publication callbacks");
            ValidateValueWrite(write);
            _world.Services.Get<ISpatialMutation>().Commit(() => { publish(); PublishValueWrite(write); notify(); });
        }
        private void PublishValueWrite(ValueWrite write)
        {
            // Create any derived record against the OLD public cursor before advancing it.
            foreach (var entry in write.Viewers) EnsurePrivateChanges(new PlayerId(entry.Key));
            write.Layer.Version++;
            _version++;
            foreach (long chunk in write.Chunks) _chunkVersions[chunk] = _version;
            if (write.Layer.Visibility == SpatialLayerVisibility.Public)
            {
                _publicVersion++;
                foreach (long chunk in write.Chunks) _publicChunkVersions[chunk] = _publicVersion;
            }
            foreach (var entry in write.Viewers)
            {
                VisibleChanges changes = _privateChanges[new PlayerId(entry.Key)];
                changes.Version++;
                foreach (long chunk in entry.Value.Chunks) changes.Chunks[chunk] = changes.Version;
            }
        }

        // Restore is a full transient cursor reset, not a bulk value mutation or conservative fallback.
        private void InvalidateRestoredLayer(MapLayer layer)
        {
            var affected = new List<VisibleChanges>();
            for (int i = 0; _world != null && i < _world.Players.Count; i++)
            {
                var viewer = new PlayerId(i);
                if (!(HasVisibility ? SpatialLayerAccess.CanObserve(_world, layer, viewer)
                    : SpatialLayerAccess.CanRead(layer.Visibility, layer.Owner, viewer))) continue;
                _ = checked(CurrentPrivateVersion(viewer) + 1);
                affected.Add(EnsurePrivateChanges(viewer));
            }
            _ = checked(layer.Version + 1); _ = checked(_version + 1);
            if (layer.Visibility == SpatialLayerVisibility.Public) _ = checked(_publicVersion + 1);
            layer.Version++; _version++; _fullVersion = _version;
            if (layer.Visibility == SpatialLayerVisibility.Public)
            { _publicVersion++; _publicFullVersion = _publicVersion; }
            foreach (VisibleChanges changes in affected)
            { changes.Version++; changes.FullVersion = changes.Version; changes.Chunks.Clear(); }
        }

        /// <summary>The 8 neighbours in a fixed order: E, S, W, N, SE, SW, NW, NE (straight first).</summary>
        public void Sync(IStateSync s)
        {
            s.Section("map");
            int w = Width, h = Height;
            s.Int(ref w);
            s.Int(ref h);
            if (w != Width || h != Height) throw new CorruptStateException("map size differs from the save");
            int n = s.Count(_layers.Count);
            if (n != _layers.Count) throw new CorruptStateException("map layer count differs from the save");
            for (int i = 0; i < _layers.Count; i++)
            {
                s.Section(_layers[i].Id);
                int owner = _layers[i].Owner.Value;
                s.Int(ref owner);
                if (s.Reading)
                {
                    if (owner < PlayerId.None.Value ||
                        _layers[i].Visibility == SpatialLayerVisibility.Public && owner != PlayerId.None.Value)
                        throw new CorruptStateException("spatial layer owner disagrees with its authored visibility");
                    _layers[i].Owner = new PlayerId(owner);
                }
                _layers[i].Sync(s);
                if (s.Reading) InvalidateRestoredLayer(_layers[i]);
            }
        }
    }
}
