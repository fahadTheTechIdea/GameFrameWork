using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable authored binding, not a second obstacle/value owner.</summary>
    public sealed class TileSensorOcclusionSetting
    {
        public ISpatialLayerDefinition Layer { get; }
        public Fix Threshold { get; }
        internal TileSensorOcclusionSetting(ISpatialLayerDefinition layer, Fix threshold) { Layer = layer; Threshold = threshold; }
    }
    public sealed partial class TileSensorDef
    {
        private (string Layer, Fix Threshold)[] _authoredOcclusion;
        public IReadOnlyList<TileSensorOcclusionSetting> Occlusion { get; private set; }
        private void ReadOcclusion(JsonReader reader)
        {
            var entries = reader.Array("occlusion") ?? throw reader.Error("explicit occlusion array required, including [] for unobstructed sensors");
            _authoredOcclusion = new (string, Fix)[entries.Count];
            var selected = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = new JsonReader(entries[i], reader.Where + ".occlusion[" + i + "]");
                string layer = entry.String("layer"); Fix threshold = entry.Fix("threshold"); entry.End();
                if (string.IsNullOrWhiteSpace(layer) || !selected.Add(layer) || !threshold.IsPositive)
                    throw entry.Error("occlusion needs unique explicit layer identities and positive thresholds");
                _authoredOcclusion[i] = (layer, threshold);
            }
            Array.Sort(_authoredOcclusion, (a, b) => StringComparer.Ordinal.Compare(a.Layer, b.Layer));
        }
        private void ResolveOcclusion(ContentResolver content)
        {
            var entries = new TileSensorOcclusionSetting[_authoredOcclusion.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var authored = _authoredOcclusion[i]; var layer = content.Def<ISpatialLayerDefinition>(authored.Layer);
                entries[i] = new TileSensorOcclusionSetting(layer, authored.Threshold);
            }
            Occlusion = Array.AsReadOnly(entries);
        }
        private void ValidateOcclusion(EntityDef owner)
        {
            foreach (var setting in Occlusion)
            {
                var layer = setting.Layer;
                if (layer.Space.Id != Space || !(layer.Space is ITileSpaceDefinition) || layer.Visibility != SpatialLayerVisibility.Public)
                    throw new ContentException(owner.Id + ": tile sensor occlusion requires public layers in its actual tile space: " + layer.Id);
                if (layer.ValueType != SpatialLayerValueType.Fixed && !Integral(setting.Threshold) ||
                    layer.ValueType == SpatialLayerValueType.Byte && setting.Threshold > Fix.FromInt(byte.MaxValue))
                    throw new ContentException(owner.Id + ": occlusion threshold must be exactly representable by its native layer: " + layer.Id);
            }
        }
    }
    public sealed partial class TileSensor
    {
        private ITileSpatialSpace ActualMap(World world)
        {
            if (!(world.Space is ITileSpatialSpace map) || map.Id != Def.Space || map.Width <= 0 || map.Height <= 0)
                throw new InvalidOperationException("sensor needs its actual positive tile geometry contract");
            return map;
        }
        private ITileSampleLayer ActualLayer(World world, ITileSpatialSpace map, TileSensorOcclusionSetting setting)
            => TileSpatialGeometry.RequireSampleLayer(world, map, setting.Layer, true);
        private string ReadOcclusionKey(World world)
        {
            if (Def.Occlusion.Count == 0) { ActualMap(world); return string.Empty; }
            return EntityObservation.ReadProvider(world, world.Space, Entity, () => ReadOcclusionKeyCore(world),
                space: world.Space, observeTargetState: true);
        }
        private string ReadOcclusionKeyCore(World world)
        {
            ITileSpatialSpace map = ActualMap(world); var key = new StringBuilder();
            foreach (var setting in Def.Occlusion)
            {
                var layer = ActualLayer(world, map, setting);
                key.Append(layer.Id.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(layer.Id).Append(':')
                    .Append(layer.Version.ToString(CultureInfo.InvariantCulture)).Append(';');
            }
            return key.ToString();
        }
        public bool CanObserve(World world, SpatialSnapshot sample)
        {
            world.RequireHealthyUpdate();
            if (Def.Occlusion.Count == 0) return CanObserveCore(world, sample);
            return EntityObservation.ReadProvider(world, world.Space, Entity, () => CanObserveCore(world, sample),
                space: world.Space, observeTargetState: true);
        }
        private bool CanObserveCore(World world, SpatialSnapshot sample)
        {
            var coverage = ReadCoverage(world) as TileSensorCoverage;
            if (coverage == null || !coverage.Covers(sample)) return false;
            var target = ((ITileFootprintSnapshot)sample).Footprint; ITileSpatialSpace map = ActualMap(world);
            if (!InBounds(map, coverage.Footprint) || !InBounds(map, target)) return false;
            if (Def.Occlusion.Count == 0) return true;
            // The actual provider owns values and revisions. No alternate obstacle buffer or world-size bound.
            var layers = new ITileSampleLayer[Def.Occlusion.Count]; var versions = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) { layers[i] = ActualLayer(world, map, Def.Occlusion[i]); versions[i] = layers[i].Version; }
            bool clear = true;
            for (long y = target.Y; clear && y < (long)target.Y + target.H; y++)
                for (long x = target.X; clear && x < (long)target.X + target.W; x++)
                    clear = RayClear(coverage.Footprint, new TileCoord((int)x, (int)y), layers);
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].Version != versions[i]) throw new InvalidOperationException("occlusion observation changed actual layer state");
            return clear;
        }
        private static bool InBounds(ITileSpatialSpace map, TileRect rectangle) => rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.W > 0 && rectangle.H > 0 &&
            (long)rectangle.X + rectangle.W <= map.Width && (long)rectangle.Y + rectangle.H <= map.Height;
        private bool RayClear(TileRect source, TileCoord target, ITileSampleLayer[] layers)
        {
            int x = source.X, y = source.Y;
            int sx = System.Math.Sign((long)target.X - x), sy = System.Math.Sign((long)target.Y - y);
            long dx = System.Math.Abs((long)target.X - x), dy = System.Math.Abs((long)target.Y - y), nx = 1, ny = 1;
            bool Clear(int cx, int cy)
            {
                var tile = new TileCoord(cx, cy);
                if (source.Contains(tile) || tile == target) return true;
                for (int i = 0; i < layers.Length; i++)
                    if (layers[i].GetSample(tile) >= Def.Occlusion[i].Threshold) return false;
                return true;
            }
            while (x != target.X || y != target.Y)
            {
                // Coordinates are in [0,int.MaxValue). Boundary products fit signed 64-bit exactly.
                long comparison = dx == 0 ? 1 : dy == 0 ? -1 : (nx * dy).CompareTo(ny * dx);
                if (comparison == 0)
                {
                    if (!Clear(x + sx, y) || !Clear(x, y + sy)) return false;
                    x += sx; y += sy; nx += 2; ny += 2;
                }
                else if (comparison < 0) { x += sx; nx += 2; }
                else { y += sy; ny += 2; }
                if (!Clear(x, y)) return false;
            }
            return true;
        }
    }
}
