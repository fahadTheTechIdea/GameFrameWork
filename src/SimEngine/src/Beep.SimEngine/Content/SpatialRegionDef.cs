using System;
using System.Text;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Content
{
    public interface ISpatialRegionGeometryFactory
    {
        ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape);
    }
    /// <summary>Actual immutable authored geometry, independent of a native implementation base.</summary>
    public interface ISpatialRegionDefinition : IContentDefinition, ISpatialRegionGeometry, ISpatialRegionGeometryFactory
    {
        new string Id { get; }
        ISpatialSpaceDefinition SpaceDefinition { get; }
    }
    /// <summary>Sole strict space/shape declaration and version builder; no runtime area state.</summary>
    public sealed class SpatialRegionDeclaration
    {
        private readonly string _space;
        private ResolvedContent _content;
        public string ShapeJson { get; }
        public ISpatialSpaceDefinition SpaceDefinition { get; private set; }
        public string Space => SpaceDefinition.Id;
        private SpatialRegionDeclaration(JsonReader reader, Action<JsonReader> readShape, Action<JsonValue> normalize)
        {
            _space = reader.String("space");
            if (string.IsNullOrWhiteSpace(_space)) throw reader.Error("region space is required");
            readShape(reader);
            var shape = reader.Raw.Clone(); shape.Remove("id"); shape.Remove("space"); normalize?.Invoke(shape);
            var text = new StringBuilder(); SortShape(shape).WriteCanonical(text); ShapeJson = text.ToString();
        }
        public static SpatialRegionDeclaration Read(JsonReader reader, Action<JsonReader> readShape, Action<JsonValue> normalize = null)
            => new SpatialRegionDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)),
                readShape ?? throw new ArgumentNullException(nameof(readShape)), normalize);
        public void Resolve(ContentResolver content)
        {
            if (_content != null) throw new InvalidOperationException("region declaration already resolved");
            SpaceDefinition = content.Def<ISpatialSpaceDefinition>(_space); _content = content.Content;
        }
        /// <summary>Decode a version using the provider's same reader, resolver and geometry implementation.</summary>
        public ISpatialRegionGeometry CreateGeometry<T>(ISpatialRegionDefinition prototype, string id, JsonReader shape)
            where T : Def, ISpatialRegionDefinition, new()
        {
            if (!(prototype is Def actual) || !actual.IsFrozen || _content == null || !_content.Owns(prototype) ||
                !ReferenceEquals(_content.Find<ISpatialRegionDefinition>(prototype.Id), prototype) || string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("geometry needs its actual frozen prototype and stable identity");
            var geometry = new T { Id = id, Module = prototype.Module };
            JsonValue authored = shape.Raw.Clone();
            if (authored.Get("space") != null) throw shape.Error("shape cannot replace prototype space");
            authored.Set("space", JsonValue.Of(_space)); authored.Freeze();
            var reader = new JsonReader(authored, shape.Where);
            geometry.Read(reader);
            var resolver = new ContentResolver(_content, geometry);
            geometry.Resolve(resolver); geometry.ValidateResolved(resolver); geometry.Freeze();
            for (int i = 0; i < shape.Raw.Count; i++) shape.Skip(shape.Raw.KeyAt(i));
            shape.End();
            return geometry;
        }
        public SpatialRegionStatus Check(SpatialSnapshot presence, Func<SpatialSnapshot, SpatialRegionStatus> check)
        {
            if (presence == null || presence.Space != Space) return SpatialRegionStatus.Unsupported;
            SpatialRegionStatus result = check(presence);
            if (result > SpatialRegionStatus.Unsupported) throw new InvalidOperationException("region provider returned an invalid status");
            return result;
        }
        private static JsonValue SortShape(JsonValue value)
        {
            if (value.IsObject)
            {
                var keys = new List<string>(); for (int i = 0; i < value.Count; i++) keys.Add(value.KeyAt(i)); keys.Sort(StringComparer.Ordinal);
                var result = JsonValue.NewObject(); foreach (string key in keys) result.Set(key, SortShape(value.Get(key))); return result;
            }
            if (value.IsArray)
            { var result = JsonValue.NewArray(); for (int i = 0; i < value.Count; i++) result.Add(SortShape(value[i])); return result; }
            return value.Clone();
        }
    }
    /// <summary>Optional native authoring helper. Engine consumers select the content contract instead.</summary>
    public abstract class SpatialRegionDef : Def, ISpatialRegionDefinition
    {
        private SpatialRegionDeclaration _declaration;
        public string ShapeJson => _declaration.ShapeJson;
        public ISpatialSpaceDefinition SpaceDefinition => _declaration.SpaceDefinition;
        public string Space => _declaration.Space;
        protected sealed override void ReadCore(JsonReader reader)
            => _declaration = SpatialRegionDeclaration.Read(reader, ReadShape, NormalizeShape);
        protected abstract void ReadShape(JsonReader reader);
        protected sealed override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); ResolveShape(content); }
        protected abstract void ResolveShape(ContentResolver content);
        public abstract ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape);
        protected ISpatialRegionGeometry BuildGeometry<T>(string id, JsonReader shape) where T : Def, ISpatialRegionDefinition, new()
            => _declaration.CreateGeometry<T>(this, id, shape);
        protected virtual void NormalizeShape(JsonValue shape) { }
        public SpatialRegionStatus Check(SpatialSnapshot presence) => _declaration.Check(presence, CheckShape);
        protected abstract SpatialRegionStatus CheckShape(SpatialSnapshot presence);
    }
    public static class SpatialRegionContracts
    {
        public static void ValidateAlias(World world, ISpatialRegionDefinition definition)
            => EntityObservation.ReadProvider(world, definition, null, () => {
                if (!(definition is Def actual) || !actual.IsFrozen || definition is IPersistentService ||
                    !world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<ISpatialRegionDefinition>(definition.Id), definition))
                    throw new ContentException("region needs its actual frozen immutable content alias");
                var space = definition.SpaceDefinition;
                if (space == null || !world.Content.Owns(space) || !ReferenceEquals(world.Content.Find<ISpatialSpaceDefinition>(space.Id), space) ||
                    definition.Space != space.Id || definition.ShapeJson == null)
                    throw new ContentException(definition.Id + ": actual space and explicit shape metadata required");
                var shape = Json.Parse(definition.ShapeJson);
                if (!shape.IsObject || shape.Get("id") != null || shape.Get("space") != null)
                    throw new ContentException(definition.Id + ": shape metadata cannot replace identity or space");
                return true;
            });
    }
}
