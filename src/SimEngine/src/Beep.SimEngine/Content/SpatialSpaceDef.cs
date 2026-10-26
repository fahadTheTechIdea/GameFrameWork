using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Content
{
    /// <summary>Explicit authored selection of a module's spatial provider.</summary>
    public interface ISpatialSpaceDefinition : IContentDefinition
    {
        void Compose(WorldComposer composer);
    }
    /// <summary>Immutable declared geometries, selected by the actual session scenario identity.</summary>
    public interface ITileSpaceDefinition : ISpatialSpaceDefinition
    {
        IReadOnlyList<TileSpaceGeometry> Geometries { get; }
        TileSpaceGeometry ReadGeometry(string scenario);
    }

    public readonly struct TileSpaceGeometry : IEquatable<TileSpaceGeometry>
    {
        public TileSpaceGeometry(string scenario, int width, int height)
        {
            if (width < 1 || height < 1) throw new ArgumentException("tile dimensions must be positive");
            if (scenario != null) ContractIdentity.ValidateId(scenario, "tile geometry scenario");
            Scenario = scenario; Width = width; Height = height;
        }
        /// <summary>Null is an explicitly fixed geometry for all scenarios, not a missing-variant fallback.</summary>
        public string Scenario { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Contains(TileCoord tile) => tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;
        public bool Contains(TileRect rectangle) => rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.W > 0 && rectangle.H > 0
            && (long)rectangle.X + rectangle.W <= Width && (long)rectangle.Y + rectangle.H <= Height;
        public bool Equals(TileSpaceGeometry other) => Scenario == other.Scenario && Width == other.Width && Height == other.Height;
        public override bool Equals(object other) => other is TileSpaceGeometry geometry && Equals(geometry);
        public override int GetHashCode()
        {
            int hash = 17;
            unchecked
            {
                if (Scenario != null) foreach (char value in Scenario) hash = hash * 31 + value;
                return (hash * 31 + Width) * 31 + Height;
            }
        }
        public static bool operator ==(TileSpaceGeometry a, TileSpaceGeometry b) => a.Equals(b);
        public static bool operator !=(TileSpaceGeometry a, TileSpaceGeometry b) => !a.Equals(b);
    }

    public static class SpatialSpaceContracts
    {
        internal static void ValidateGeometries(IReadOnlyList<TileSpaceGeometry> geometries)
        {
            if (geometries == null || geometries.Count == 0) throw new ContentException("explicit tile geometries required");
            string previous = null;
            for (int i = 0; i < geometries.Count; i++)
            {
                TileSpaceGeometry geometry = geometries[i];
                if (geometry.Width < 1 || geometry.Height < 1) throw new ContentException("tile geometries require positive dimensions");
                if (geometry.Scenario == null)
                {
                    if (geometries.Count != 1) throw new ContentException("fixed and scenario geometries cannot be mixed");
                    continue;
                }
                try { ContractIdentity.ValidateId(geometry.Scenario, "tile geometry scenario"); }
                catch (ArgumentException error) { throw new ContentException(error.Message); }
                if (previous != null && string.CompareOrdinal(previous, geometry.Scenario) >= 0)
                    throw new ContentException("tile geometry scenarios must be unique and canonical");
                previous = geometry.Scenario;
            }
        }

        internal static TileSpaceGeometry SelectGeometry(IReadOnlyList<TileSpaceGeometry> geometries, string scenario)
        {
            if (string.IsNullOrWhiteSpace(scenario)) throw new ContentException("tile geometry needs an actual scenario identity");
            ValidateGeometries(geometries);
            if (geometries.Count == 1 && geometries[0].Scenario == null) return geometries[0];
            foreach (TileSpaceGeometry geometry in geometries) if (geometry.Scenario == scenario) return geometry;
            throw new ContentException("no tile geometry authored for scenario '" + scenario + "'");
        }

        public static TileSpaceGeometry ReadGeometry(ITileSpaceDefinition definition, string scenario)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            TileSpaceGeometry expected = SelectGeometry(definition.Geometries, scenario);
            TileSpaceGeometry actual = definition.ReadGeometry(scenario);
            if (actual != expected) throw new ContentException(definition.Id + ": selected geometry differs from its immutable declaration");
            return actual;
        }

        /// <summary>Metadata shape may be authored for one variant. Actual selected bounds are checked by its consuming owner.</summary>
        public static bool ContainsAny(ITileSpaceDefinition definition, TileRect rectangle)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var geometries = definition.Geometries; ValidateGeometries(geometries);
            foreach (TileSpaceGeometry geometry in geometries) if (geometry.Contains(rectangle)) return true;
            return false;
        }

        internal static void Validate(ISpatialSpaceDefinition definition, ContentResolver content)
        {
            if (!content.Content.Owns(definition) || !ReferenceEquals(content.Def<ISpatialSpaceDefinition>(definition.Id), definition))
                throw content.Fail("spatial definition must be the actual owned root contract alias");
            if (!(definition is ITileSpaceDefinition tiles)) return;
            if (!ReferenceEquals(content.Def<ITileSpaceDefinition>(tiles.Id), tiles))
                throw content.Fail("tile space requires its actual owned tile contract alias");
            ValidateGeometries(tiles.Geometries);
            foreach (TileSpaceGeometry geometry in tiles.Geometries)
                if (geometry.Scenario != null && !ReferenceEquals(content.Def<ScenarioDef>(geometry.Scenario).Space, definition))
                    throw content.Fail("tile geometry must name an actual scenario selecting this space");
            foreach (ScenarioDef scenario in content.Content.All<ScenarioDef>())
                if (ReferenceEquals(scenario.Space, definition)) _ = ReadGeometry(tiles, scenario.Id);
        }
    }

    /// <summary>Sole authored native tile geometry and composition. Contains no live map or selected-scenario state.</summary>
    public sealed class TileSpaceDeclaration
    {
        public IReadOnlyList<TileSpaceGeometry> Geometries { get; }
        private TileSpaceDeclaration(JsonReader reader)
        {
            var geometries = new System.Collections.Generic.List<TileSpaceGeometry>();
            if (reader.Has("scenarios"))
            {
                if (reader.Has("width") || reader.Has("height")) throw reader.Error("fixed dimensions and scenario geometries are exclusive");
                JsonValue values = reader.Array("scenarios") ?? throw reader.Error("explicit scenario geometries required");
                for (int i = 0; i < values.Count; i++)
                {
                    var entry = new JsonReader(values[i], reader.Where + ".scenarios[" + i + "]");
                    string scenario = entry.String("scenario"); int width = entry.Int("width"), height = entry.Int("height"); entry.End();
                    if (string.IsNullOrWhiteSpace(scenario)) throw entry.Error("scenario geometry requires an explicit scenario identity");
                    try { geometries.Add(new TileSpaceGeometry(scenario, width, height)); }
                    catch (ArgumentException error) { throw entry.Error(error.Message); }
                }
                geometries.Sort((a, b) => string.CompareOrdinal(a.Scenario, b.Scenario));
            }
            else
            {
                try { geometries.Add(new TileSpaceGeometry(null, reader.Int("width"), reader.Int("height"))); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
            }
            Geometries = geometries.AsReadOnly(); SpatialSpaceContracts.ValidateGeometries(Geometries);
        }
        public static TileSpaceDeclaration Read(JsonReader reader)
            => new TileSpaceDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public TileSpaceGeometry ReadGeometry(string scenario) => SpatialSpaceContracts.SelectGeometry(Geometries, scenario);
        public void Compose(WorldComposer composer, ITileSpaceDefinition definition)
        {
            if (composer == null || definition == null) throw new ArgumentNullException();
            var selected = EntityObservation.ReadProvider(composer.World, definition, null, () => {
                if (!composer.Content.Owns(definition) || !ReferenceEquals(composer.Content.Get<ISpatialSpaceDefinition>(definition.Id), definition) ||
                    !ReferenceEquals(composer.Content.Get<ITileSpaceDefinition>(definition.Id), definition))
                    throw new ContentException("tile composition requires its actual owned authored geometry");
                TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(definition, composer.Scenario.Id);
                if (geometry != ReadGeometry(composer.Scenario.Id))
                    throw new ContentException("tile definition does not expose its actual native declaration");
                return (Id: definition.Id, Geometry: geometry);
            });
            var map = new TileMap(selected.Id, selected.Geometry.Width, selected.Geometry.Height);
            foreach (ISpatialLayerDefinition layer in composer.Content.All<ISpatialLayerDefinition>())
                if (ReferenceEquals(layer.Space, definition)) map.RegisterLayer(layer);
            composer.Services.Bind(map);
            composer.Services.Bind<ISpatialSpace>(map);
            TileLayerView.Compose(composer, map);
            composer.AddViewFeature("tiles." + selected.Id, 8, (world, viewer) => new TileMapView(world, viewer));
        }
    }
    public sealed class TileSpaceDef : Def, ITileSpaceDefinition
    {
        private TileSpaceDeclaration _declaration;
        public IReadOnlyList<TileSpaceGeometry> Geometries => _declaration.Geometries;
        public TileSpaceGeometry ReadGeometry(string scenario) => _declaration.ReadGeometry(scenario);
        protected override void ReadCore(JsonReader reader) => _declaration = TileSpaceDeclaration.Read(reader);
        public void Compose(WorldComposer composer) => _declaration.Compose(composer, this);
    }

    /// <summary>
    /// Authored meaning and value shape for one spatial layer. Semantic/vertical identities remain
    /// open; units and resources alias the existing resolved catalog, never an independent registry.
    /// </summary>
    public interface ISpatialLayerDefinition : IContentDefinition
    {
        string SpaceId { get; }
        ISpatialSpaceDefinition Space { get; }
        Code Type { get; }
        SpatialLayerValueType ValueType { get; }
        SpatialLayerVisibility Visibility { get; }
        IUnitDefinition Unit { get; }
        IResourceDefinition Resource { get; }
        SpatialResourceMeasure ResourceMeasure { get; }
        Code VerticalDomain { get; }
        IUnitDefinition VerticalUnit { get; }
        string VerticalReference { get; }
        Fix VerticalOffset { get; }
        JsonValue Metadata { get; }
    }

    /// <summary>Sole layer authoring rule; no spatial values, material balances or provider state.</summary>
    public sealed class SpatialLayerDeclaration
    {
        private string _space;
        private string _resource;
        private string _type;
        private string _valueType;
        private string _verticalDomain;
        private string _visibility;
        private string _unit;
        private string _verticalUnit;

        public ISpatialSpaceDefinition Space { get; private set; }
        public string SpaceId => _space;
        public Code Type { get; private set; }
        public SpatialLayerValueType ValueType { get; private set; }
        public SpatialLayerVisibility Visibility { get; private set; }
        public IUnitDefinition Unit { get; private set; }
        public IResourceDefinition Resource { get; private set; }
        public SpatialResourceMeasure ResourceMeasure { get; private set; }
        public Code VerticalDomain { get; private set; }
        public IUnitDefinition VerticalUnit { get; private set; }
        public string VerticalReference { get; private set; }
        public Fix VerticalOffset { get; private set; }
        public JsonValue Metadata { get; private set; }

        public void Read(JsonReader r)
        {
            _space = r.String("space");
            _type = r.String("type");
            _valueType = r.String("value_type");
            _visibility = r.String("visibility");
            _unit = r.String("unit");
            _resource = r.String("resource", null);
            string measure = r.String("resource_measure", null);
            _verticalDomain = r.String("vertical_domain", null);
            _verticalUnit = r.String("vertical_unit", null);
            VerticalReference = r.String("vertical_reference", null);
            VerticalOffset = r.Fix("vertical_offset", Fix.Zero);
            Metadata = (r.Object("metadata") ?? JsonValue.NewObject()).Clone();
            Metadata.Freeze();
            if (string.IsNullOrWhiteSpace(_space)) throw r.Error("spatial layer requires a space");
            if (string.IsNullOrWhiteSpace(_type)) throw r.Error("spatial layer requires a type identity");
            if (string.IsNullOrWhiteSpace(_unit)) throw r.Error("spatial layer unit must not be empty");
            if (_resource != null && string.IsNullOrWhiteSpace(_resource)) throw r.Error("resource identity must not be empty");
            if (_resource == null)
            {
                if (measure != null) throw r.Error("resource_measure requires resource");
                ResourceMeasure = SpatialResourceMeasure.None;
            }
            else switch (measure)
            {
                case "quantity": ResourceMeasure = SpatialResourceMeasure.Quantity; break;
                case "associated": ResourceMeasure = SpatialResourceMeasure.Associated; break;
                default: throw r.Error("resource requires explicit resource_measure: quantity or associated");
            }
            if (_verticalDomain != null && string.IsNullOrWhiteSpace(_verticalDomain)) throw r.Error("vertical_domain must not be empty");
            if (_verticalDomain != null && string.IsNullOrWhiteSpace(_verticalUnit)) throw r.Error("vertical_domain requires vertical_unit");
            if (_verticalDomain == null && (_verticalUnit != null || VerticalReference != null || VerticalOffset != Fix.Zero))
                throw r.Error("vertical metadata requires vertical_domain");
            if (VerticalReference != null && string.IsNullOrWhiteSpace(VerticalReference)) throw r.Error("vertical_reference must not be empty");
            switch (_valueType)
            {
                case "byte": ValueType = SpatialLayerValueType.Byte; break;
                case "int": ValueType = SpatialLayerValueType.Int32; break;
                case "fixed": ValueType = SpatialLayerValueType.Fixed; break;
                default: throw r.Error("value_type must be byte, int or fixed");
            }
            switch (_visibility)
            {
                case "public": Visibility = SpatialLayerVisibility.Public; break;
                case "company_private": Visibility = SpatialLayerVisibility.CompanyPrivate; break;
                default: throw r.Error("visibility must be public or company_private");
            }
            Type = new Code(_type);
            VerticalDomain = string.IsNullOrEmpty(_verticalDomain) ? Code.None : new Code(_verticalDomain);
        }

        public void Resolve(ContentResolver c)
        {
            Space = c.Def<ISpatialSpaceDefinition>(_space);
            Unit = c.Def<IUnitContentDefinition>(_unit);
            Resource = _resource == null ? null : c.Def<IResourceContentDefinition>(_resource);
            VerticalUnit = _verticalUnit == null ? null : c.Def<IUnitContentDefinition>(_verticalUnit);
        }

        // Admit every actual alias before dependent resolution, unused definitions included.
        public static void Validate(ISpatialLayerDefinition layer, ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!(layer is Def actual) || !content.Content.Owns(layer) || layer.Id != actual.Id || layer.Module != actual.Module ||
                !ReferenceEquals(content.Content.Find<ISpatialLayerDefinition>(actual.Id), layer))
                throw content.Fail("spatial layer requires its actual owned catalog alias");
            string spaceId = layer.SpaceId;
            ISpatialSpaceDefinition space = layer.Space;
            IUnitDefinition unit = layer.Unit, verticalUnit = layer.VerticalUnit;
            IResourceDefinition resource = layer.Resource;
            SpatialResourceMeasure measure = layer.ResourceMeasure;
            JsonValue metadata = layer.Metadata;
            if (string.IsNullOrWhiteSpace(spaceId) || space == null || space.Id != spaceId || !content.Content.Owns(space) ||
                !ReferenceEquals(content.Content.Find<ISpatialSpaceDefinition>(spaceId), space) ||
                string.IsNullOrWhiteSpace(layer.Type.Id) || (byte)layer.ValueType < (byte)SpatialLayerValueType.Byte || (byte)layer.ValueType > (byte)SpatialLayerValueType.Fixed ||
                (byte)layer.Visibility < (byte)SpatialLayerVisibility.Public || (byte)layer.Visibility > (byte)SpatialLayerVisibility.CompanyPrivate ||
                !(unit is IUnitContentDefinition authoredUnit) || !content.Content.Owns(authoredUnit) ||
                !ReferenceEquals(content.Content.Find<IUnitContentDefinition>(authoredUnit.Id), unit) ||
                metadata == null || metadata.Kind != JsonKind.Object || !metadata.Frozen)
                throw content.Fail("spatial layer needs actual space/unit, declared type/value/access and immutable object metadata");
            if ((byte)measure > (byte)SpatialResourceMeasure.Associated ||
                resource == null && measure != SpatialResourceMeasure.None ||
                resource != null && (measure == SpatialResourceMeasure.None ||
                    !(resource is IResourceContentDefinition authoredResource) || !content.Content.Owns(authoredResource) ||
                    !ReferenceEquals(content.Content.Find<IResourceContentDefinition>(authoredResource.Id), resource) ||
                    !ReferenceEquals(content.Content.Resource(authoredResource.Resource), resource)) ||
                measure == SpatialResourceMeasure.Quantity && unit.Dimension != resource.QuantityUnit.Dimension)
                throw content.Fail("spatial layer resource metadata must match its actual native resource and quantity dimension");
            if (layer.VerticalDomain.IsNone)
            {
                if (verticalUnit != null || layer.VerticalReference != null || layer.VerticalOffset != Fix.Zero)
                    throw content.Fail("vertical layer metadata requires a declared domain");
            }
            else if (string.IsNullOrWhiteSpace(layer.VerticalDomain.Id) || !(verticalUnit is IUnitContentDefinition authoredVertical) ||
                !content.Content.Owns(authoredVertical) || !ReferenceEquals(content.Content.Find<IUnitContentDefinition>(authoredVertical.Id), verticalUnit) ||
                layer.VerticalReference != null && string.IsNullOrWhiteSpace(layer.VerticalReference))
                throw content.Fail("vertical layer metadata needs an actual unit and valid reference");
        }
    }

    /// <summary>Ordinary native JSON registration; not a mandatory extension base.</summary>
    public sealed class SpatialLayerDef : Def, ISpatialLayerDefinition
    {
        private readonly SpatialLayerDeclaration _declaration = new SpatialLayerDeclaration();
        public string SpaceId => _declaration.SpaceId;
        public ISpatialSpaceDefinition Space => _declaration.Space;
        public Code Type => _declaration.Type;
        public SpatialLayerValueType ValueType => _declaration.ValueType;
        public SpatialLayerVisibility Visibility => _declaration.Visibility;
        public IUnitDefinition Unit => _declaration.Unit;
        public IResourceDefinition Resource => _declaration.Resource;
        public SpatialResourceMeasure ResourceMeasure => _declaration.ResourceMeasure;
        public Code VerticalDomain => _declaration.VerticalDomain;
        public IUnitDefinition VerticalUnit => _declaration.VerticalUnit;
        public string VerticalReference => _declaration.VerticalReference;
        public Fix VerticalOffset => _declaration.VerticalOffset;
        public JsonValue Metadata => _declaration.Metadata;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
}
