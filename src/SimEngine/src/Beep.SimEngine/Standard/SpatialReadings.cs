using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public interface ISpatialKnowledgeSubjectDefinition : IKnowledgeSubjectDefinition
    {
        ISpatialLayerDefinition Layer { get; }
        bool CoversSample(SpatialSnapshot sample);
        /// <summary>Pure admission of the actual selected runtime layer, before initialization.</summary>
        void ValidateLayer(World world, ISpatialLayer layer);
    }
    /// <summary>Sole authored reading intent and generic observation strategy. Geometry is supplied
    /// through contracts/delegates, never a required inheritance base or a copied sample owner.</summary>
    public sealed class SpatialReadingDeclaration
    {
        private readonly string _layer;
        public ISpatialLayerDefinition Layer { get; private set; }
        public KnowledgeRetention Retention { get; }
        public GameDuration? FreshFor { get; }
        private SpatialReadingDeclaration(JsonReader reader)
        { _layer = reader.String("layer"); Retention = KnowledgeRetentionPolicy.Read(reader);
            FreshFor = KnowledgeRetentionPolicy.ReadFreshness(reader, Retention); }
        public static SpatialReadingDeclaration Read(JsonReader reader)
            => new SpatialReadingDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) => Layer = content.Def<ISpatialLayerDefinition>(_layer);
        private static ISpatialLayer ActualLayer(World world, ISpatialLayerDefinition definition)
        {
            if (world.Space?.Id != definition.Space.Id || !(world.Space is ISpatialLayerProvider provider)) return null;
            IReadOnlyList<ISpatialLayer> layers = provider.SpatialLayers;
            if (layers == null) throw new ContentException("selected reading provider has no actual layer list");
            foreach (ISpatialLayer layer in layers) if (layer != null && layer.Id == definition.Id && layer.Space == definition.Space.Id) return layer;
            return null;
        }
        /// <summary>Called by the sole all-loaded knowledge admission owner; foreign geometry stays optional.</summary>
        public static void ValidateBinding(World world, ISpatialKnowledgeSubjectDefinition subject)
        {
            ISpatialLayerDefinition definition = subject.Layer;
            ISpatialSpaceDefinition geometry = definition?.Space;
            if (!(definition is Def layerDef) || !layerDef.IsFrozen || !world.Content.Owns(definition)
                || !ReferenceEquals(world.Content.Find<ISpatialLayerDefinition>(definition.Id), definition)
                || !(geometry is Def spaceDef) || !spaceDef.IsFrozen || !world.Content.Owns(geometry)
                || !ReferenceEquals(world.Content.Find<ISpatialSpaceDefinition>(geometry.Id), geometry))
                throw new ContentException(subject.Id + ": reading requires actual frozen layer and space declarations");
            if (geometry.Id != world.Settings.Space) return;
            if (world.Space == null) throw new ContentException(subject.Id + ": selected reading requires its actual layer provider");
            EntityObservation.ReadProvider(world, world.Space, null, () => {
                ISpatialLayer layer = ActualLayer(world, definition);
                if (layer == null) throw new ContentException(subject.Id + ": selected reading requires its actual registered layer provider");
                subject.ValidateLayer(world, layer); return true;
            });
        }
        public Code CheckTarget(World world, Entity target, Func<SpatialSnapshot, Code> checkLocation)
        {
            if (target == null || !target.Alive || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            ISpatialLayer layer = ActualLayer(world, Layer); if (layer == null) return Codes.NotFound;
            if (!world.Players.IsSeat(target.Owner) || !layer.CanRead(target.Owner)) return Codes.NotOwned;
            ISpatialPresence presence = target.Get<ISpatialPresence>();
            return presence == null ? Codes.InvalidArgument : checkLocation(presence.Capture());
        }
        public JsonValue Observe(World world, Entity target, Func<SpatialSnapshot, Code> checkLocation,
            Func<World, ISpatialLayer, Fix> readValue)
        {
            Code check = CheckTarget(world, target, checkLocation); if (!check.IsNone) throw new InvalidOperationException("spatial reading refused: " + check);
            var data = JsonValue.NewObject(); data.Set("space", JsonValue.Of(Layer.Space.Id)); data.Set("layer", JsonValue.Of(Layer.Id));
            data.Set("value_raw", JsonValue.Number(readValue(world, ActualLayer(world, Layer)).Raw.ToString(CultureInfo.InvariantCulture)));
            if (!CheckObservation(world, data).IsNone) throw new InvalidOperationException("provider returned illegal spatial reading");
            return data;
        }
        public Code CheckObservation(World world, JsonValue data)
        {
            try
            {
                var reader = new JsonReader(data, Layer.Id); string space = reader.String("space"), layer = reader.String("layer");
                Fix value = Fix.FromRaw(reader.Long("value_raw")); reader.End();
                ISpatialLayer actual = ActualLayer(world, Layer);
                if (actual == null || space != Layer.Space.Id || layer != Layer.Id) return Codes.InvalidArgument;
                if (actual.ValueType != SpatialLayerValueType.Fixed && (value.Raw % Fix.One.Raw != 0
                    || actual.ValueType == SpatialLayerValueType.Byte && (value < Fix.Zero || value > Fix.FromInt(byte.MaxValue))
                    || actual.ValueType == SpatialLayerValueType.Int32 && (value < Fix.FromInt(int.MinValue) || value > Fix.FromInt(int.MaxValue)))) return Codes.InvalidArgument;
                return SpatialResourceValues.CheckValue(actual, value) == UnitConversionStatus.Converted ? Code.None : Codes.InvalidArgument;
            }
            catch (ContentException) { return Codes.InvalidArgument; }
        }
    }
    public sealed class TileReadingDef : Def, ISpatialKnowledgeSubjectDefinition
    {
        private SpatialReadingDeclaration _declaration;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public KnowledgeRetention Retention => _declaration.Retention;
        public GameDuration? FreshFor => _declaration.FreshFor;
        public TileCoord Tile { get; private set; }
        public bool CoversSample(SpatialSnapshot sample)
            => sample is TileSpatialSnapshot tile && tile.Space == Layer.Space.Id && tile.Tile == Tile
                && tile.Footprint.X == Tile.X && tile.Footprint.Y == Tile.Y && tile.Footprint.W == 1 && tile.Footprint.H == 1;
        protected override void ReadCore(JsonReader reader)
        { _declaration = SpatialReadingDeclaration.Read(reader); Tile = new TileCoord(reader.Int("x"), reader.Int("y")); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!(Layer.Space is ITileSpaceDefinition space) || !SpatialSpaceContracts.ContainsAny(space, new TileRect(Tile, 1, 1)))
                throw content.Fail("tile reading outside actual tile space");
        }
        public void ValidateLayer(World world, ISpatialLayer layer)
        {
            if (!(world.Space is ITileSpatialSpace map) || map.Id != Layer.Space.Id || !(layer is ITileSampleLayer)
                || layer.Id != Layer.Id || layer.Space != map.Id || !map.InBounds(Tile))
                throw new ContentException(Id + ": tile reading requires its actual tile geometry and readable tile layer");
            if (!ReferenceEquals(layer, TileSpatialGeometry.RequireSampleLayer(world, map, Layer, false)))
                throw new ContentException(Id + ": tile reading layer is not its actual registered provider");
        }
        public Code CheckTarget(World world, Entity target) => _declaration.CheckTarget(world, target, CheckLocation);
        public JsonValue Observe(World world, Entity target) => _declaration.Observe(world, target, CheckLocation, ReadValue);
        public Code CheckObservation(World world, JsonValue data) => _declaration.CheckObservation(world, data);
        private Code CheckLocation(SpatialSnapshot target)
            => target is TileSpatialSnapshot tile && tile.Space == Layer.Space.Id && tile.Footprint.Contains(Tile) ? Code.None : Codes.InvalidArgument;
        private Fix ReadValue(World world, ISpatialLayer layer)
        {
            if (layer is ITileSampleLayer samples) return samples.GetSample(Tile);
            throw new InvalidOperationException("selected layer has no tile sample capability");
        }
    }
}
