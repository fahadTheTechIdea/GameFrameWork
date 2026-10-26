using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Beep.SimEngine.Content
{
    /// <summary>
    /// Strict authored capability modes referencing the existing entity-state catalog.
    /// This is definition metadata, not a state machine or a second state registry.
    /// Modules use the same reader and resolver as the built-in capabilities.
    /// </summary>
    public sealed class EntityStateBindings
    {
        private readonly Dictionary<string, string> _references;
        private readonly IReadOnlyDictionary<string, IEntityStateDefinition> _definitions;

        private EntityStateBindings(Dictionary<string, string> references) => _references = references;
        private EntityStateBindings(Dictionary<string, IEntityStateDefinition> definitions)
            => _definitions = new ReadOnlyDictionary<string, IEntityStateDefinition>(definitions);

        /// <summary>The resolved catalog references; callers cannot replace bindings.</summary>
        public IReadOnlyDictionary<string, IEntityStateDefinition> Definitions
            => _definitions ?? throw new InvalidOperationException("entity state bindings have not been resolved");
        public IEntityStateDefinition this[string mode] => Definitions[mode];

        /// <summary>Reads a required states object with exactly the provider's declared modes.</summary>
        public static EntityStateBindings Read(JsonReader component, params string[] modes)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (modes == null || modes.Length == 0) throw new ArgumentException("state modes are required", nameof(modes));
            JsonReader states = component.Child("states") ?? throw component.Error("missing 'states'");
            var references = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string mode in modes)
            {
                if (string.IsNullOrWhiteSpace(mode) || references.ContainsKey(mode))
                    throw new ArgumentException("state modes must be nonempty and unique", nameof(modes));
                string id = states.String(mode);
                if (string.IsNullOrWhiteSpace(id)) throw states.Error("'" + mode + "' requires an entity-state reference");
                references.Add(mode, id);
            }
            states.End();
            return new EntityStateBindings(references);
        }

        /// <summary>Returns immutable references to this content set's sole state definitions.</summary>
        public EntityStateBindings Resolve(ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (_references == null) throw new InvalidOperationException("entity state bindings are already resolved");
            var definitions = new Dictionary<string, IEntityStateDefinition>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> reference in _references)
                definitions.Add(reference.Key, content.Def<IEntityStateDefinition>(reference.Value));
            return new EntityStateBindings(definitions);
        }
    }
}
