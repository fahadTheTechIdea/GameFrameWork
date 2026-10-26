using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Maps
{
    /// <summary>Selected tile geometry only; no allocation policy, layer storage or renderer.</summary>
    public interface ITileSpatialSpace : ISpatialSpace
    {
        int Width { get; }
        int Height { get; }
    }
    /// <summary>Selected provider's occupancy relocation; TilePresence remains the coordinate/interpolation owner.</summary>
    public interface ITileRelocationSpace : ITileSpatialSpace
    {
        /// <summary>Validate and relocate occupancy before coordinate publication. Do not write presence/entity state.</summary>
        void Relocate(World world, ITilePresence presence, TileCoord destination);
    }
    /// <summary>Current blocking occupancy and approach geometry, not a router or movement owner.</summary>
    public interface ITileNavigationSpace : ITileRelocationSpace
    {
        Entity OccupantAt(TileCoord tile);
        TileRect InteractionGoal(TileRect footprint, int actorWidth, int actorHeight);
    }
    /// <summary>Pure current tile samples and the existing provider-owned write revision.</summary>
    public interface ITileSampleLayer : ISpatialLayer
    {
        int Version { get; }
        Fix GetSample(TileCoord tile);
    }
    public interface ITileWritableSampleLayer : ITileSampleLayer
    {
        Code CheckSample(TileCoord tile, Fix value);
        void SetSample(TileCoord tile, Fix value);
    }
    /// <summary>Actual nonzero cells, in canonical row/column order; no dense world scan or copied grid.</summary>
    public interface ITileSparseSampleLayer : ITileSampleLayer
    {
        void CopyNonZeroTiles(IList<TileCoord> destination);
    }
    public readonly struct TileSampleMutation
    {
        public ISpatialLayerDefinition Layer { get; }
        public TileCoord Tile { get; }
        public Fix Value { get; }
        public TileSampleMutation(ISpatialLayerDefinition layer, TileCoord tile, Fix value)
        { Layer = layer ?? throw new ArgumentNullException(nameof(layer)); Tile = tile; Value = value; }
    }
    /// <summary>Selected storage owner's whole-set preflight and publication, not another layer store.</summary>
    public interface ITileSampleMutationSpace : ITileSpatialSpace
    {
        void ApplySamples(IReadOnlyList<TileSampleMutation> mutations);
    }
    public static class TileSpatialGeometry
    {
        // The existing deterministic tile-neighbour order, shared by routing and network geometry.
        public static IReadOnlyList<TileCoord> Neighbours { get; } = Array.AsReadOnly(new[] {
            new TileCoord(1, 0), new TileCoord(0, 1), new TileCoord(-1, 0), new TileCoord(0, -1),
            new TileCoord(1, 1), new TileCoord(-1, 1), new TileCoord(-1, -1), new TileCoord(1, -1) });
        public static bool InBounds(this ITileSpatialSpace space, TileCoord tile)
            => space != null && tile.X >= 0 && tile.Y >= 0 && tile.X < space.Width && tile.Y < space.Height;
        /// <summary>One detached sample geometry for the tile contract, shared by visibility and public queries.</summary>
        public static SpatialSnapshot SampleSnapshot(ITileSpatialSpace space, TileCoord tile)
        {
            if (space == null) throw new ArgumentNullException(nameof(space));
            return new TileSpatialSnapshot(space.Id, tile, new TileRect(tile, 1, 1), default, default, default, false, false);
        }
        public static bool InBounds(this ITileSpatialSpace space, TileRect bounds)
            => space != null && bounds.X >= 0 && bounds.Y >= 0 && bounds.W > 0 && bounds.H > 0 &&
                (long)bounds.X + bounds.W <= space.Width && (long)bounds.Y + bounds.H <= space.Height;
        public static TileRect AdjacentGoal(ITileSpatialSpace space, TileRect footprint, int actorWidth = 1, int actorHeight = 1)
        {
            if (actorWidth < 1 || actorHeight < 1) throw new ArgumentException("approach requires positive actor dimensions");
            if (actorWidth > space.Width || actorHeight > space.Height) return default;
            int left = (int)Math.Max(0L, (long)footprint.X - actorWidth), top = (int)Math.Max(0L, (long)footprint.Y - actorHeight);
            int right = (int)Math.Min((long)space.Width - actorWidth + 1, (long)footprint.X + footprint.W + 1);
            int bottom = (int)Math.Min((long)space.Height - actorHeight + 1, (long)footprint.Y + footprint.H + 1);
            return new TileRect(left, top, right - left, bottom - top);
        }
        public static SpatialOverlapStatus CheckOverlap(World world, ITileSpatialSpace space, SpatialSnapshot left, SpatialSnapshot right)
        {
            world?.RequireHealthyUpdate();
            if (space == null || !ReferenceEquals(world?.Space, space) || left is not TileSpatialSnapshot a || right is not TileSpatialSnapshot b ||
                a.Space != space.Id || b.Space != space.Id || a.Footprint.W <= 0 || a.Footprint.H <= 0 || b.Footprint.W <= 0 || b.Footprint.H <= 0)
                return SpatialOverlapStatus.Unsupported;
            TileRect x = a.Footprint, y = b.Footprint;
            return (long)x.X < (long)y.X + y.W && (long)y.X < (long)x.X + x.W &&
                (long)x.Y < (long)y.Y + y.H && (long)y.Y < (long)x.Y + x.H
                ? SpatialOverlapStatus.Overlapping : SpatialOverlapStatus.Disjoint;
        }
        /// <summary>One actual registered tile-layer binding; consumers require only their own capability.</summary>
        public static TLayer RequireLayer<TLayer>(World world, ITileSpatialSpace space, ISpatialLayerDefinition definition, bool requirePublic)
            where TLayer : class, ISpatialLayer
        {
            if (world == null || space == null || !ReferenceEquals(world.Space, space) || definition == null ||
                !world.Content.Owns(definition) || definition.Space.Id != space.Id ||
                space is not ISpatialLayerProvider provider || provider.SpatialLayers == null)
                throw new InvalidOperationException("tile samples require their actual selected space and registered layer");
            TLayer selected = null;
            foreach (var candidate in provider.SpatialLayers)
            {
                if (candidate == null) throw new InvalidOperationException("null registered spatial layer");
                if (candidate.Id != definition.Id) continue;
                if (selected != null || candidate is not TLayer layer || layer.Space != space.Id ||
                    layer.Visibility != definition.Visibility || requirePublic && layer.Visibility != SpatialLayerVisibility.Public ||
                    layer.ValueType != definition.ValueType || layer.Type != definition.Type ||
                    !ReferenceEquals(layer.Unit, definition.Unit) || !ReferenceEquals(layer.Resource, definition.Resource) ||
                    layer.ResourceMeasure != definition.ResourceMeasure)
                    throw new InvalidOperationException("tile sample runtime metadata disagrees with authoring");
                selected = layer;
            }
            return selected ?? throw new InvalidOperationException("tile sample layer is not registered on its actual space");
        }
        /// <summary>Sample capability and original revision admission, shared by optical, terrain and readings.</summary>
        public static ITileSampleLayer RequireSampleLayer(World world, ITileSpatialSpace space, ISpatialLayerDefinition definition, bool requirePublic)
        {
            var layer = RequireLayer<ITileSampleLayer>(world, space, definition, requirePublic);
            if (layer.Version < 0) throw new InvalidOperationException("negative tile sample revision");
            return layer;
        }
    }
}
