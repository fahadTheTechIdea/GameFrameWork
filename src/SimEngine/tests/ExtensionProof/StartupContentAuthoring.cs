using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>JSON fixture authoring only. No world access, spawning, validation, stock or simulation.
    /// Callers explicitly supply native entry identity, company, placement and selected includes.
    /// WorldSetup is the sole resolver, validator and executor.</summary>
    public static class StartupContentAuthoring
    {
        private static JsonValue Profile(JsonValue root, string profile = null)
        {
            string id = profile ?? root.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Get("profile").Text;
            JsonValue profiles = root.Get("world_setup_profiles");
            for (int i = 0; i < profiles.Count; i++)
                if (profiles[i].Get("id").Text == id) return profiles[i];
            throw new InvalidOperationException("missing authored startup profile " + id);
        }
        public static void Add(JsonValue root, string id, string entity, int company, JsonValue placement, string profile = null)
        {
            JsonValue entry = JsonValue.NewObject();
            entry.Set("id", JsonValue.Of(id)); entry.Set("after", JsonValue.NewArray());
            entry.Set("entity", JsonValue.Of(entity)); entry.Set("company", JsonValue.Number(company.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            entry.Set("placement", placement?.Clone() ?? JsonValue.Null()); entry.Set("home", JsonValue.Null());
            root.Get("entity_setups").Add(entry); Profile(root, profile).Get("entries").Add(JsonValue.Of(id));
        }
        public static void UseEntries(JsonValue root, JsonValue entries)
        {
            root.Set("entity_setups", entries.Clone()); var ids = JsonValue.NewArray();
            for (int i = 0; i < entries.Count; i++) ids.Add(entries[i].Get("id").Clone());
            Profile(root).Set("entries", ids);
        }
        public static void Include(JsonValue root, params string[] profiles)
        {
            foreach (string id in profiles) Profile(root).Get("includes").Add(JsonValue.Of(id));
        }
        public static void Position(JsonValue root, string entryId, JsonValue position)
        {
            JsonValue entries = root.Get("entity_setups");
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Get("id").Text == entryId) { entries[i].Get("placement").Set("position", position.Clone()); return; }
            throw new InvalidOperationException("missing authored startup entry " + entryId);
        }
        public static void Clear(JsonValue root, string profile = null)
        {
            root.Set("entity_setups", JsonValue.NewArray()); Profile(root, profile).Set("entries", JsonValue.NewArray());
        }
    }
}
