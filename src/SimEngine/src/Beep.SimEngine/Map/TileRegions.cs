using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Maps
{
    /// <summary>Detached region geometry; contains no invented entity position, movement or blocking facts.</summary>
    public sealed class TileRegionSnapshot : SpatialSnapshot, ITileFootprintSnapshot
    {
        public TileRect Footprint { get; }
        public TileRegionSnapshot(string space, TileRect footprint) : base(space)
        {
            if (footprint.X < 0 || footprint.Y < 0 || footprint.W <= 0 || footprint.H <= 0)
                throw new ArgumentException("region footprint requires nonnegative origin and positive size");
            Footprint = footprint;
        }
        public override bool Matches(SpatialSnapshot other)
            => other is TileRegionSnapshot value && Space == value.Space && Footprint.Equals(value.Footprint);
    }
    public abstract class TileRegionDef : SpatialRegionDef, ISpatialTravelRegion
    {
        public SpatialRegionStatus CheckTravel(SpatialSnapshot from, SpatialSnapshot to)
        {
            if (from is not TileSpatialSnapshot a || to is not TileSpatialSnapshot b || a.Space != Space || b.Space != Space ||
                a.Footprint.W < 1 || a.Footprint.H < 1 || a.Footprint.W != b.Footprint.W || a.Footprint.H != b.Footprint.H ||
                a.Footprint.Origin != a.Tile || b.Footprint.Origin != b.Tile ||
                System.Math.Abs((long)a.Tile.X - b.Tile.X) > 1 || System.Math.Abs((long)a.Tile.Y - b.Tile.Y) > 1)
                return SpatialRegionStatus.Unsupported;
            int x = System.Math.Min(a.Tile.X, b.Tile.X), y = System.Math.Min(a.Tile.Y, b.Tile.Y);
            // The complete swept rectangle excludes diagonal corners for every authored footprint.
            long width = (long)a.Footprint.W + System.Math.Abs((long)a.Tile.X - b.Tile.X);
            long height = (long)a.Footprint.H + System.Math.Abs((long)a.Tile.Y - b.Tile.Y);
            if (width > int.MaxValue || height > int.MaxValue) return SpatialRegionStatus.Unsupported;
            return Contains(new TileRect(x, y, (int)width, (int)height))
                ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside;
        }
        protected sealed override void ResolveShape(ContentResolver content)
        {
            if (!(SpaceDefinition is ITileSpaceDefinition tiles)) throw content.Fail("tile region requires a tile space");
            ValidateTiles(content, tiles);
        }
        protected abstract void ValidateTiles(ContentResolver content, ITileSpaceDefinition space);
        protected sealed override SpatialRegionStatus CheckShape(SpatialSnapshot presence)
            => presence is ITileFootprintSnapshot tiles ? Contains(tiles.Footprint) ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside
                : SpatialRegionStatus.Unsupported;
        protected abstract bool Contains(TileRect footprint);
    }

    public sealed class RectangularTileRegionDef : TileRegionDef, ISpatialObservationRegion
    {
        public override ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape) => BuildGeometry<RectangularTileRegionDef>(id, shape);
        public TileRect Rectangle { get; private set; }
        public IReadOnlyList<SpatialSnapshot> CaptureObservationFootprints()
            => Array.AsReadOnly<SpatialSnapshot>(new SpatialSnapshot[] { new TileRegionSnapshot(Space, Rectangle) });
        protected override void ReadShape(JsonReader reader)
        {
            Rectangle = new TileRect(reader.Int("x"), reader.Int("y"), reader.Int("width"), reader.Int("height"));
            if (Rectangle.X < 0 || Rectangle.Y < 0 || Rectangle.W <= 0 || Rectangle.H <= 0)
                throw reader.Error("region rectangle needs nonnegative origin and positive dimensions");
        }
        protected override void ValidateTiles(ContentResolver content, ITileSpaceDefinition space)
        {
            if (!SpatialSpaceContracts.ContainsAny(space, Rectangle))
                throw content.Fail("region rectangle is outside its authored space");
        }
        protected override bool Contains(TileRect footprint)
            => footprint.W > 0 && footprint.H > 0 && footprint.X >= Rectangle.X && footprint.Y >= Rectangle.Y
                && (long)footprint.X + footprint.W <= (long)Rectangle.X + Rectangle.W
                && (long)footprint.Y + footprint.H <= (long)Rectangle.Y + Rectangle.H;
    }

    public sealed class PaintedTileRegionDef : TileRegionDef, ISpatialObservationRegion
    {
        public override ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape) => BuildGeometry<PaintedTileRegionDef>(id, shape);
        private readonly HashSet<TileCoord> _membership = new HashSet<TileCoord>();
        public IReadOnlyList<TileCoord> Tiles { get; private set; }
        public IReadOnlyList<SpatialSnapshot> CaptureObservationFootprints()
        {
            var shapes = new List<SpatialSnapshot>();
            foreach (TileCoord tile in Tiles) shapes.Add(new TileRegionSnapshot(Space, new TileRect(tile.X, tile.Y, 1, 1)));
            return shapes.AsReadOnly();
        }
        protected override void ReadShape(JsonReader reader)
        {
            JsonValue values = reader.Array("tiles") ?? throw reader.Error("painted region needs explicit tiles");
            if (values.Count == 0) throw reader.Error("painted region cannot be empty");
            var tiles = new List<TileCoord>();
            for (int i = 0; i < values.Count; i++)
            {
                var entry = new JsonReader(values[i], reader.Where + ".tiles[" + i + "]");
                var tile = new TileCoord(entry.Int("x"), entry.Int("y")); entry.End();
                if (tile.X < 0 || tile.Y < 0 || !_membership.Add(tile)) throw entry.Error("painted tiles must be nonnegative and unique");
                tiles.Add(tile);
            }
            tiles.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            Tiles = tiles.AsReadOnly();
        }
        protected override void NormalizeShape(JsonValue shape)
        {
            var values = JsonValue.NewArray();
            foreach (TileCoord tile in Tiles)
            {
                var value = JsonValue.NewObject();
                value.Set("x", JsonValue.Number(tile.X.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                value.Set("y", JsonValue.Number(tile.Y.ToString(System.Globalization.CultureInfo.InvariantCulture))); values.Add(value);
            }
            shape.Set("tiles", values);
        }
        protected override void ValidateTiles(ContentResolver content, ITileSpaceDefinition space)
        {
            foreach (TileSpaceGeometry geometry in space.Geometries)
            {
                bool fits = true;
                foreach (TileCoord tile in Tiles) if (!geometry.Contains(tile)) { fits = false; break; }
                if (fits) return;
            }
            throw content.Fail("painted tiles do not fit any one authored space geometry");
        }
        protected override bool Contains(TileRect footprint)
        {
            if (footprint.W <= 0 || footprint.H <= 0 || footprint.X < 0 || footprint.Y < 0 ||
                (long)footprint.W * footprint.H > _membership.Count ||
                (long)footprint.X + footprint.W > int.MaxValue || (long)footprint.Y + footprint.H > int.MaxValue) return false;
            for (long y = footprint.Y; y < (long)footprint.Y + footprint.H; y++)
                for (long x = footprint.X; x < (long)footprint.X + footprint.W; x++)
                    if (!_membership.Contains(new TileCoord((int)x, (int)y))) return false;
            return true;
        }
    }
}
