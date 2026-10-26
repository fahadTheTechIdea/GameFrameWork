using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Portable provider-owned schema: no live identity, execution, stock or progress.</summary>
    public interface IPlanTemplateDefinition : IPlanDefinition
    {
        void ValidateTemplate(World world, JsonValue arguments);
        /// <summary>Structural anchor/geometry validation before any player or target exists.</summary>
        void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor);
        Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template);
        Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent);
    }
    public sealed class PlanTemplateEntry : IDependencyNode
    {
        public string Id { get; }
        public string Definition { get; }
        public JsonValue Arguments { get; }
        public int Priority { get; }
        public bool Paused { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public PlanTemplateEntry(string id, string definition, JsonValue arguments, int priority, bool paused, IEnumerable<string> dependencies = null)
        {
            ContractIdentity.ValidateId(id, "template entry");
            if (string.IsNullOrWhiteSpace(definition) || arguments == null || !arguments.IsObject || priority < 0)
                throw new ArgumentException("template entry needs explicit definition, object arguments and nonnegative priority");
            Id = id; Definition = definition; Arguments = arguments.Clone(); Arguments.Freeze(); Priority = priority; Paused = paused;
            var links = dependencies == null ? new List<string>() : new List<string>(dependencies); links.Sort(StringComparer.Ordinal);
            for (int i = 0; i < links.Count; i++)
            {
                ContractIdentity.ValidateId(links[i], "template prerequisite");
                if (links[i] == id || i > 0 && links[i] == links[i - 1]) throw new ArgumentException("template prerequisites must be distinct and cannot reference self");
            }
            Dependencies = links.AsReadOnly();
        }
    }
    public sealed class PlanTemplate
    {
        public string Space { get; }
        public IReadOnlyList<PlanTemplateEntry> Entries { get; }
        public PlanTemplate(string space, IEnumerable<PlanTemplateEntry> entries)
        {
            if (space != null) ContractIdentity.ValidateId(space, "template space");
            var copy = entries == null ? throw new ArgumentNullException(nameof(entries)) : new List<PlanTemplateEntry>(entries);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (copy.Count == 0) throw new ArgumentException("template needs explicit nonempty entries");
            foreach (PlanTemplateEntry entry in copy) if (entry == null || !ids.Add(entry.Id)) throw new ArgumentException("template entries need unique stable IDs");
            try { _ = new DependencyGraph<PlanTemplateEntry>(copy, "plan template"); }
            catch (ContentException error) { throw new ArgumentException(error.Message); }
            Space = space; Entries = copy.AsReadOnly();
        }
        public static PlanTemplate Read(JsonReader reader)
        {
            JsonValue selected = reader.Value("space");
            if (selected == null || selected.Kind != JsonKind.Null && selected.Kind != JsonKind.String) throw reader.Error("space must explicitly name a provider or be null");
            string space = selected.Kind == JsonKind.Null ? null : selected.Text;
            JsonValue rows = reader.Array("entries") ?? throw reader.Error("entries required");
            var entries = new List<PlanTemplateEntry>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".entries[" + i + "]");
                string id = row.String("id"), definition = row.String("definition");
                JsonValue arguments = row.Child("arguments")?.Raw ?? throw row.Error("arguments required");
                int priority = row.Int("priority"); bool paused = row.Bool("paused");
                JsonValue after = row.Array("after") ?? throw row.Error("after array required"); var dependencies = new List<string>();
                for (int j = 0; j < after.Count; j++)
                { if (after[j].Kind != JsonKind.String) throw row.Error("after entries must be stable entry IDs"); dependencies.Add(after[j].Text); }
                row.End();
                try { entries.Add(new PlanTemplateEntry(id, definition, arguments, priority, paused, dependencies)); }
                catch (ArgumentException e) { throw row.Error(e.Message); }
            }
            try { return new PlanTemplate(space, entries); } catch (ArgumentException e) { throw reader.Error(e.Message); }
        }
        public JsonValue ToJson()
        {
            var root = JsonValue.NewObject(); root.Set("space", Space == null ? JsonValue.Null() : JsonValue.Of(Space));
            var rows = JsonValue.NewArray(); root.Set("entries", rows);
            foreach (PlanTemplateEntry entry in Entries)
            {
                var row = JsonValue.NewObject(); row.Set("id", JsonValue.Of(entry.Id)); row.Set("definition", JsonValue.Of(entry.Definition));
                row.Set("arguments", entry.Arguments.Clone()); row.Set("priority", JsonValue.Number(entry.Priority.ToString(CultureInfo.InvariantCulture)));
                row.Set("paused", JsonValue.Of(entry.Paused)); rows.Add(row);
                var after = JsonValue.NewArray(); foreach (string dependency in entry.Dependencies) after.Add(JsonValue.Of(dependency)); row.Set("after", after);
            }
            root.Freeze(); return root;
        }
        internal static void Sync(IStateSync sync, ref PlanTemplate template)
        {
            bool present = template != null; sync.Bool(ref present); if (!present) { if (sync.Reading) template = null; return; }
            string text = null; if (!sync.Reading) { var encoded = new StringBuilder(); template.ToJson().WriteCanonical(encoded); text = encoded.ToString(); }
            sync.String(ref text);
            if (sync.Reading) { var reader = new JsonReader(Json.Parse(text), "plan-template"); template = Read(reader); reader.End(); }
        }
    }
    /// <summary>Master catalog owns authored templates; same value/reader as captured player content.</summary>
    public interface IPlanTemplateContent : IContentDefinition
    {
        PlanTemplate Template { get; }
    }
    public sealed class PlanTemplateDef : Def, IPlanTemplateContent
    {
        public PlanTemplate Template { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Template = PlanTemplate.Read(reader); }
    }
}
