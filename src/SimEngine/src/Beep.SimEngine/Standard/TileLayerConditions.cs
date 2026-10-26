using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Frozen authored predicate shared by tile gameplay consumers.</summary>
    public interface ITileConditionDefinition : IContentDefinition
    {
        ITileSpaceDefinition Space { get; }
        bool Matches(World world, ITileSpatialSpace map, TileCoord tile);
    }

    /// <summary>One actual-content admission and scoped evaluation boundary; not a predicate catalog.</summary>
    public static class TileConditionContracts
    {
        public static void Validate(ITileConditionDefinition definition, ContentResolver content)
        {
            if (definition == null || !ReferenceEquals(content.Def<ITileConditionDefinition>(definition.Id), definition) ||
                definition.Space == null || !ReferenceEquals(content.Def<ITileSpaceDefinition>(definition.Space.Id), definition.Space))
                throw content.Fail("tile condition requires its actual catalog identity and tile-space alias");
        }
        public static void RequireContext(ITileConditionDefinition definition, World world, ITileSpatialSpace map)
        {
            if (!(definition is Def published) || !published.IsFrozen || world == null || map == null ||
                !ReferenceEquals(world.Space, map) || !world.Content.Owns(definition) ||
                definition.Space == null || !world.Content.Owns(definition.Space) ||
                !ReferenceEquals(world.Content.Get<ITileConditionDefinition>(definition.Id), definition) ||
                !ReferenceEquals(world.Content.Get<ITileSpaceDefinition>(map.Id), definition.Space))
                throw new InvalidOperationException("tile condition requires frozen actual session content and tile space");
        }
        private static readonly Func<(ITileConditionDefinition Definition, World World, ITileSpatialSpace Map, TileCoord Tile), bool> ReadMatch = context => {
            RequireContext(context.Definition, context.World, context.Map);
            if (!context.Map.InBounds(context.Tile)) return false;
            return context.Definition.Matches(context.World, context.Map, context.Tile);
        };
        public static bool Matches(ITileConditionDefinition definition, World world, ITileSpatialSpace map, TileCoord tile)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return EntityObservation.ReadProvider(world, definition, null, (definition, world, map, tile), ReadMatch, map);
        }
    }

    /// <summary>Sole native layer-band authoring and sample interpretation, reusable without inheritance.</summary>
    public sealed class TileLayerBandDeclaration
    {
        private string _space, _layer;
        public ITileSpaceDefinition Space { get; private set; }
        public ISpatialLayerDefinition Layer { get; private set; }
        public Fix? Minimum { get; private set; }
        public Fix? Maximum { get; private set; }
        public void Read(JsonReader reader)
        {
            _space = reader.String("space"); _layer = reader.String("layer");
            Minimum = Bound(reader, "minimum"); Maximum = Bound(reader, "maximum");
            if (string.IsNullOrWhiteSpace(_space) || string.IsNullOrWhiteSpace(_layer) ||
                !Minimum.HasValue && !Maximum.HasValue ||
                Minimum.HasValue && Maximum.HasValue && Minimum.Value >= Maximum.Value)
                throw reader.Error("layer condition needs an explicit space, layer and a nonempty bounded interval");
        }
        private static Fix? Bound(JsonReader reader, string key)
        {
            var value = reader.Value(key) ?? throw reader.Error("explicit " + key + " required (number or null)");
            return value.Kind == JsonKind.Null ? (Fix?)null : reader.Fix(key);
        }
        public void Resolve(ContentResolver content)
        {
            Space = content.Def<ITileSpaceDefinition>(_space);
            Layer = content.Def<ISpatialLayerDefinition>(_layer);
        }
        public void Validate(ContentResolver content)
        {
            if (!ReferenceEquals(Layer.Space, Space) || Layer.Visibility != SpatialLayerVisibility.Public)
                throw content.Fail("layer condition requires public layers in its actual tile space: " + Layer.Id);
            bool Integral(Fix? bound) => !bound.HasValue || bound.Value == Fix.FromInt(bound.Value.ToIntFloor());
            if (Layer.ValueType != SpatialLayerValueType.Fixed && (!Integral(Minimum) || !Integral(Maximum)))
                throw content.Fail("byte/int layer bounds must be integral native values: " + Layer.Id);
        }
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            if (map == null || Space == null || Layer == null || map.Id != Space.Id)
                throw new InvalidOperationException("layer band requires its resolved tile space");
            if (!map.InBounds(tile)) return false;
            var actual = TileSpatialGeometry.RequireSampleLayer(world, map, Layer, true);
            Fix value = actual.GetSample(tile);
            return (!Minimum.HasValue || value >= Minimum.Value) && (!Maximum.HasValue || value < Maximum.Value);
        }
    }

    public sealed class TileLayerBandConditionDef : Def, ITileConditionDefinition
    {
        private readonly TileLayerBandDeclaration _declaration = new TileLayerBandDeclaration();
        public ITileSpaceDefinition Space => _declaration.Space;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public Fix? Minimum => _declaration.Minimum;
        public Fix? Maximum => _declaration.Maximum;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            TileConditionContracts.RequireContext(this, world, map);
            return _declaration.Matches(world, map, tile);
        }
    }

    // One reference reader/linker; actual catalog aliases, never a second condition registry.
    internal sealed class TileConditionReferences
    {
        private readonly string[] _ids;
        internal IReadOnlyList<ITileConditionDefinition> Definitions { get; private set; }
        internal TileConditionReferences(JsonReader reader) : this(reader, false) { }
        internal TileConditionReferences(JsonReader reader, bool allowEmpty)
        {
            if (!reader.Has("conditions")) throw reader.Error("explicit condition references required");
            _ids = reader.Strings("conditions");
            if (_ids.Length == 0 && !allowEmpty) throw reader.Error("a conditional rule requires at least one condition reference");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in _ids)
            {
                try { ContractIdentity.ValidateId(id, "tile condition reference"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (!ids.Add(id)) throw reader.Error("duplicate tile condition reference: " + id);
            }
        }
        internal void Resolve(ContentResolver content)
        {
            var definitions = new ITileConditionDefinition[_ids.Length];
            for (int i = 0; i < _ids.Length; i++) definitions[i] = content.Def<ITileConditionDefinition>(_ids[i]);
            Definitions = Array.AsReadOnly(definitions);
        }
        internal void Validate(ContentResolver content, ITileSpaceDefinition space)
        {
            foreach (var definition in Definitions)
                if (!ReferenceEquals(definition.Space, space))
                    throw content.Fail("tile condition belongs to another actual space: " + definition.Id);
        }
    }

    /// <summary>Explicit footprint quantifier over actual existing predicates; no layer state or sampled cache.</summary>
    public sealed class TileFootprintConditionDeclaration
    {
        private readonly TileConditionReferences _references;
        private ITileSpaceDefinition _space;
        public bool Any { get; }
        public IReadOnlyList<ITileConditionDefinition> Conditions => _references.Definitions;
        private TileFootprintConditionDeclaration(JsonReader reader)
        {
            string match = reader.String("match");
            if (match != "all" && match != "any") throw reader.Error("footprint match must explicitly be all or any");
            Any = match == "any"; _references = new TileConditionReferences(reader, !Any); reader.End();
        }
        public static TileFootprintConditionDeclaration Read(JsonReader reader)
            => new TileFootprintConditionDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) => _references.Resolve(content);
        public void Validate(ContentResolver content, ITileSpaceDefinition space)
        {
            _references.Validate(content, space);
            foreach (var condition in Conditions) TileConditionContracts.Validate(condition, content);
            _space = space;
        }
        public void ValidateBinding(World world, ITileSpaceDefinition space)
        {
            if (!ReferenceEquals(space, _space) || world == null || !world.Content.Owns(space) ||
                !ReferenceEquals(world.Content.Find<ITileSpaceDefinition>(space.Id), space) ||
                !(world.Space is ITileSpatialSpace map) || map.Id != space.Id)
                throw new ContentException("footprint conditions require their actual selected tile space");
            foreach (var condition in Conditions) TileConditionContracts.RequireContext(condition, world, map);
        }
        public bool Matches(World world, ITileSpatialSpace map, TileRect footprint)
        {
            if (_space == null || world == null || map == null || !ReferenceEquals(world.Space, map) || map.Id != _space.Id ||
                !world.Content.Owns(_space))
                throw new InvalidOperationException("footprint conditions require their resolved actual map");
            if (footprint.W <= 0 || footprint.H <= 0 || footprint.X < 0 || footprint.Y < 0 ||
                (long)footprint.X + footprint.W > map.Width || (long)footprint.Y + footprint.H > map.Height) return false;
            for (long y = footprint.Y; y < (long)footprint.Y + footprint.H; y++)
                for (long x = footprint.X; x < (long)footprint.X + footprint.W; x++)
                {
                    bool matches = true;
                    foreach (var condition in Conditions)
                        if (!TileConditionContracts.Matches(condition, world, map, new TileCoord((int)x, (int)y))) { matches = false; break; }
                    if (Any && matches) return true;
                    if (!Any && !matches) return false;
                }
            return !Any;
        }
    }

    /// <summary>Optional native content only; no simulation service, update or clock.</summary>
    public sealed class TileConditionsModule : GameModule
    {
        public const string ModuleId = "sim.tile-conditions";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<TileLayerBandConditionDef>("tile_layer_band_conditions");
            types.Section<TileConditionGroupDef>("tile_condition_groups");
        }
    }
}
