using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Intent/portable authoring belongs to the existing line definition. LineWork owns execution.
    public sealed partial class LineDeclaration
    {
        private LinePlan _prototype;
        public string PlanKey => "line:" + _owner.Id;
        public IActivityGraph Graph => _prototype.Graph;
        public string OnComplete => FinishEffect;
        private ISpatialPlacementTemplates TemplateSpace(World world)
            => world.Space is ISpatialPlacementTemplates provider && world.Space.Id == Layer.Space.Id
                ? provider : throw new ContentException(PlanKey + ": selected line space does not support placement templates");
        private JsonValue ReadPathArguments(World world, JsonValue arguments)
        {
            var reader = new JsonReader(arguments, PlanKey + ".intent");
            string space = reader.String("space"); JsonValue path = reader.Array("path") ?? throw reader.Error("explicit path required"); reader.End();
            if (space != Layer.Space.Id || space != world.Space?.Id || path.Count == 0)
                throw reader.Error("explicit nonempty path in the selected line space required");
            return path;
        }
        private IReadOnlyList<SpatialPlacement> ReadPath(World world, JsonValue arguments)
            => ReadPath(world, Layer.Space.Id, ReadPathArguments(world, arguments));
        private static ISpatialPlacementIntent PlacementSpace(World world, string space)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate();
            return world.Space is ISpatialPlacementIntent provider && world.Space.Id == space
                ? provider : throw new ContentException("corridor requires its actual selected placement provider");
        }
        // Malformed player JSON is a normal refusal; successful geometry callbacks must remain pure.
        private static T ObservePath<T>(World world, ISpatialPlacementIntent provider, Func<T> read)
        {
            var scope = EntityObservation.CaptureProvider(world, provider);
            try { T value = read(); scope.Verify(); return value; }
            catch (ContentException error)
            {
                try { scope.Verify(); }
                catch { world.LatchFault("line.path", error); }
                throw;
            }
            catch (Exception error) { world.LatchFault("line.path", error); throw; }
        }
        public static IReadOnlyList<SpatialPlacement> ReadPath(World world, string space, JsonValue path)
        {
            var provider = PlacementSpace(world, space);
            if (path == null || path.Kind != JsonKind.Array || path.Count == 0)
                throw new ContentException("corridor requires an explicit nonempty placement path");
            return ObservePath(world, provider, () => {
                var result = new List<SpatialPlacement>();
                for (int i = 0; i < path.Count; i++) {
                    SpatialPlacement placement = provider.ReadPlacement(path[i]);
                    if (placement == null || placement.Space != space) throw new ContentException("corridor placement differs from selected space");
                    result.Add(placement);
                }
                return (IReadOnlyList<SpatialPlacement>)result.AsReadOnly();
            });
        }
        public static JsonValue WritePath(World world, string space, IReadOnlyList<SpatialPlacement> path)
        {
            var provider = PlacementSpace(world, space);
            if (path == null || path.Count == 0) throw new ContentException("corridor requires a nonempty placement path");
            return ObservePath(world, provider, () => {
                var rows = JsonValue.NewArray();
                foreach (SpatialPlacement placement in path) {
                    if (placement == null || placement.Space != space) throw new ContentException("corridor placement differs from selected space");
                    JsonValue row = provider.WritePlacement(placement);
                    SpatialPlacement inverse = provider.ReadPlacement(row);
                    string identity = provider.PlacementIdentity(placement);
                    if (string.IsNullOrWhiteSpace(identity) || inverse == null || inverse.Space != space ||
                        identity != provider.PlacementIdentity(inverse))
                        throw new ContentException("corridor provider placement inverse differs");
                    rows.Add(row.Clone());
                }
                rows.Freeze(); return rows;
            });
        }
        public static string PathIdentity(World world, string space, IReadOnlyList<SpatialPlacement> path)
        {
            var provider = PlacementSpace(world, space);
            if (path == null || path.Count == 0) throw new ContentException("corridor requires a nonempty placement path");
            return ObservePath(world, provider, () => {
                var identity = new StringBuilder();
                identity.Append(space.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(space);
                identity.Append('|').Append(path.Count.ToString(CultureInfo.InvariantCulture));
                foreach (SpatialPlacement placement in path) {
                    if (placement == null || placement.Space != space) throw new ContentException("corridor placement differs from selected space");
                    string value = provider.PlacementIdentity(placement);
                    if (string.IsNullOrWhiteSpace(value)) throw new ContentException("corridor placement identity required");
                    identity.Append('|').Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
                }
                return identity.ToString();
            });
        }
        public string IntentIdentity(World world, JsonValue arguments) => PathIdentity(world, Layer.Space.Id, ReadPath(world, arguments));
        public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments)
        { _ = IntentIdentity(world, arguments); return Array.Empty<EntityId>(); }
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
        { _ = IntentIdentity(world, arguments); return Code.None; }
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
        { _ = IntentIdentity(world, arguments); return arguments.Clone(); }
        public IActivityDefinition BindExecution(World world, EntityId target)
            => world.Services.Get<ILineConstruction>().BindExecution(_owner, target);
        public string ExecutionIdentity(World world, Order order)
            => world.Services.Get<ILineConstruction>().ExecutionIdentity(_owner, order);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
        {
            Code access = world.Services.Get<IContentAccess>().Check(world, company, _owner);
            return access.IsNone ? world.Services.Get<ILineConstruction>().CheckIntent(company, _owner, ReadPath(world, arguments)) : access;
        }
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments)
        {
            Code submission = CheckSubmission(world, company, arguments);
            return submission.IsNone ? world.Services.Get<ILineConstruction>().Check(company, _owner, ReadPath(world, arguments)) : submission;
        }
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
        {
            Code check = CheckActivation(world, company, arguments);
            if (!check.IsNone) throw new InvalidOperationException("line activation refused: " + check);
            return world.Services.Get<ILineConstruction>().Begin(company, _owner, ReadPath(world, arguments), priority);
        }
        private JsonValue PathArguments(JsonValue path)
        {
            var result = JsonValue.NewObject(); result.Set("space", JsonValue.Of(Layer.Space.Id)); result.Set("path", path); return result;
        }
        public PlanIntent CreateIntent(JsonValue path, string group, int priority, bool paused)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return new PlanIntent(PlanKey, PathArguments(path), group, priority, paused);
        }
        public void ValidateTemplate(World world, JsonValue arguments)
        {
            var provider = TemplateSpace(world); var rows = ReadPathArguments(world, arguments);
            for (int i = 0; i < rows.Count; i++) provider.ValidateTemplatePlacement(rows[i]);
        }
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor)
        {
            ValidateTemplate(world, arguments); var provider = TemplateSpace(world); var rows = ReadPathArguments(world, arguments);
            for (int i = 0; i < rows.Count; i++) provider.ReadPlacement(provider.PlaceTemplatePlacement(rows[i], anchor));
        }
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
        {
            ReadPath(world, arguments); var provider = TemplateSpace(world); var rows = ReadPathArguments(world, arguments); var relative = JsonValue.NewArray();
            for (int i = 0; i < rows.Count; i++) relative.Add(provider.CaptureTemplatePlacement(rows[i], origin));
            template = PathArguments(relative); return Code.None;
        }
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
        {
            ValidateTemplate(world, arguments); var provider = TemplateSpace(world); var rows = ReadPathArguments(world, arguments); var placed = JsonValue.NewArray();
            for (int i = 0; i < rows.Count; i++) placed.Add(provider.PlaceTemplatePlacement(rows[i], anchor));
            intent = PathArguments(placed); return Code.None;
        }
    }
}
