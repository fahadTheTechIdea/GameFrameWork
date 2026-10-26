using System;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Tests
{
    // Internal negative ownership/array-capture instrumentation only. Never enters a catalog,
    // spawns an entity or provides a positive gameplay/content admission path.
    internal static class ContentAttachmentProbe
    {
        internal static EntityDef Attach(string id, string module, EntityKind kind, string category,
            string visual, string[] tags, params ComponentDef[] components)
        {
            var json = JsonValue.NewObject();
            json.Set("kind", JsonValue.Of(kind == EntityKind.Generic ? "entity" : kind.ToString().ToLowerInvariant()));
            json.Set("category", JsonValue.Of(category)); json.Set("visual", JsonValue.Of(visual));
            var authoredTags = JsonValue.NewArray();
            foreach (var tag in tags ?? Array.Empty<string>()) authoredTags.Add(JsonValue.Of(tag));
            json.Set("tags", authoredTags);
            var entity = new EntityDef { Id = id, Module = module };
            entity.Read(new JsonReader(json, "negative component attachment"));
            entity.SetComponents(components); return entity;
        }
    }
}
