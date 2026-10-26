using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Beep.SimEngine.Testing
{
    /// <summary>Immutable verification selection/bounds only; not engine configuration or another registry.</summary>
    public sealed class ExplicitTestProfile
    {
        public sealed class Entry
        {
            internal Entry(string name, bool included, int timeout, string reason)
            { Name = name; Included = included; TimeoutMilliseconds = timeout; ExclusionReason = reason; }
            public string Name { get; }
            public bool Included { get; }
            public int TimeoutMilliseconds { get; }
            public string ExclusionReason { get; }
        }
        private ExplicitTestProfile(string id, int grace, int output, List<Entry> entries)
        { Id = id; TerminationMilliseconds = grace; OutputCharacters = output; Entries = entries.AsReadOnly(); }
        public string Id { get; }
        public int TerminationMilliseconds { get; }
        public int OutputCharacters { get; }
        public ReadOnlyCollection<Entry> Entries { get; }
        private static void Fields(JsonElement item, params string[] names)
        {
            if (item.ValueKind != JsonValueKind.Object) throw new ArgumentException("profile requires an object");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in item.EnumerateObject())
                if (Array.IndexOf(names, p.Name) < 0 || !seen.Add(p.Name)) throw new ArgumentException("unknown or duplicate profile field: " + p.Name);
            foreach (string name in names) if (!seen.Contains(name)) throw new ArgumentException("missing profile field: " + name);
        }
        private static int Positive(JsonElement item, string key)
        {
            if (!item.GetProperty(key).TryGetInt32(out int value) || value <= 0) throw new ArgumentException("positive profile bound required: " + key);
            return value;
        }
        private static string Text(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString())) throw new ArgumentException("explicit profile identity/reason required");
            return value.GetString();
        }
        public static ExplicitTestProfile Parse(string json, IReadOnlyList<string> registered)
        {
            if (json == null || registered == null) throw new ArgumentNullException();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement; Fields(root, "schema", "id", "termination_timeout_ms", "output_chars", "cases");
            if (!root.GetProperty("schema").TryGetInt32(out int schema) || schema != 1) throw new ArgumentException("unsupported test profile schema");
            string id = Text(root.GetProperty("id")); int grace = Positive(root, "termination_timeout_ms"), output = Positive(root, "output_chars");
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in registered) if (string.IsNullOrWhiteSpace(name) || !actual.Add(name)) throw new ArgumentException("invalid or duplicate registered case");
            var cases = root.GetProperty("cases");
            if (cases.ValueKind != JsonValueKind.Array || cases.GetArrayLength() == 0) throw new ArgumentException("nonempty exact profile cases required");
            var selected = new HashSet<string>(StringComparer.Ordinal); var entries = new List<Entry>(); bool any = false;
            foreach (var item in cases.EnumerateArray())
            {
                Fields(item, "name", "include", "timeout_ms", "exclusion_reason");
                string name = Text(item.GetProperty("name"));
                if (!actual.Contains(name) || !selected.Add(name)) throw new ArgumentException("unknown or duplicate profile case: " + name);
                var include = item.GetProperty("include");
                if (include.ValueKind != JsonValueKind.True && include.ValueKind != JsonValueKind.False) throw new ArgumentException("explicit inclusion required");
                bool included = include.GetBoolean(); int timeout = Positive(item, "timeout_ms"); var reason = item.GetProperty("exclusion_reason");
                if (included && reason.ValueKind != JsonValueKind.Null) throw new ArgumentException("included case cannot have an exclusion");
                string excluded = included ? null : Text(reason);
                entries.Add(new Entry(name, included, timeout, excluded)); any |= included;
            }
            if (!any || selected.Count != actual.Count) throw new ArgumentException("profile must assign every registered case and include work");
            return new ExplicitTestProfile(id, grace, output, entries);
        }
    }
}
