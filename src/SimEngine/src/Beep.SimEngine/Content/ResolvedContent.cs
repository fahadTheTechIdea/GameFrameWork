using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Content
{
    /// <summary>
    /// The typed, frozen content one set of modules sees: every def of the active modules,
    /// read strictly and cross-referenced. Built once per module set by <see cref="Hosting.ContentSet"/>
    /// and shared by every world that uses that set.
    /// </summary>
    public sealed class ResolvedContent : IResourceCatalog
    {
        private readonly Dictionary<Type, List<Def>> _byType = new Dictionary<Type, List<Def>>();
        private readonly Dictionary<Type, Dictionary<string, Def>> _byId = new Dictionary<Type, Dictionary<string, Def>>();
        private readonly List<IResourceContentDefinition> _resources = new List<IResourceContentDefinition> { null };
        private IReadOnlyList<IResourceContentDefinition> _resourceDefinitions;
        private readonly Dictionary<string, ResourceId> _resourceIds = new Dictionary<string, ResourceId>(StringComparer.Ordinal);

        internal ResolvedContent(string[] modules, ulong fingerprint)
        {
            Modules = Array.AsReadOnly((string[])modules.Clone());
            Fingerprint = fingerprint;
        }

        /// <summary>Active content module ids, in load order.</summary>
        public IReadOnlyList<string> Modules { get; }

        /// <summary>Hash of the active modules' documents: two machines with the same value have the same content.</summary>
        public ulong Fingerprint { get; }

        /// <summary>Active authored component declarations in stable ID order.</summary>
        public IReadOnlyList<StateCodecRegistration> ComponentCodecs { get; private set; }
        internal IReadOnlyList<ContentTypes.ComponentType> ComponentDeclarations { get; private set; }
        internal ContentTypes.ComponentType ComponentDeclaration(string key)
        {
            for (int i = 0; i < ComponentDeclarations.Count; i++)
                if (ComponentDeclarations[i].Metadata.Id == key) return ComponentDeclarations[i];
            return null;
        }

        public IEntityStateDefinition DefaultEntityState { get; private set; }

        public bool HasModule(string id)
        {
            for (int i = 0; i < Modules.Count; i++) if (Modules[i] == id) return true;
            return false;
        }

        public IReadOnlyList<IResourceContentDefinition> Resources => _resourceDefinitions
            ?? throw new InvalidOperationException("resource catalog collection is not finalized");
        IReadOnlyList<IResourceDefinition> IResourceCatalog.Definitions => Resources;
        bool IResourceCatalog.TryResolve(string id, out ResourceId resource) => TryResource(id, out resource);
        IResourceDefinition IResourceCatalog.Get(ResourceId id)
            => Resource(id) ?? throw new ContentException("unknown resource index '" + id.Value + "'");
        public int ResourceCount => _resources.Count - 1;

        public bool TryResource(string id, out ResourceId resource) => _resourceIds.TryGetValue(id ?? "", out resource);

        public ResourceId ResourceId(string id)
        {
            if (!TryResource(id, out ResourceId resource)) throw new ContentException("unknown resource '" + id + "'");
            return resource;
        }

        public IResourceContentDefinition Resource(ResourceId id) => id.Value > 0 && id.Value < _resources.Count ? _resources[id.Value] : null;

        public string ResourceName(ResourceId id) => Resource(id)?.Id;

        public IReadOnlyList<TDef> All<TDef>() where TDef : class, IContentDefinition
        {
            if (!_byType.TryGetValue(typeof(TDef), out List<Def> list)) return new TDef[0];
            var r = new TDef[list.Count];
            for (int i = 0; i < r.Length; i++) r[i] = (TDef)(IContentDefinition)list[i];
            return r;
        }

        public int Count<TDef>() where TDef : class, IContentDefinition => _byType.TryGetValue(typeof(TDef), out List<Def> l) ? l.Count : 0;

        public TDef Find<TDef>(string id) where TDef : class, IContentDefinition
        {
            if (id == null || !_byId.TryGetValue(typeof(TDef), out Dictionary<string, Def> map)) return null;
            if (!map.TryGetValue(id, out Def d)) return null;
            if (d == null) throw new ContentException("ambiguous " + typeof(TDef).Name + " '" + id + "'; use a more specific content contract");
            return (TDef)(IContentDefinition)d;
        }

        public TDef Get<TDef>(string id) where TDef : class, IContentDefinition
            => Find<TDef>(id) ?? throw new ContentException("unknown " + typeof(TDef).Name + " '" + id + "'");

        public bool Owns(IContentDefinition definition)
            => definition is Def def && def.Id != null && _byId.TryGetValue(def.GetType(), out var index) &&
                index.TryGetValue(def.Id, out Def actual) && ReferenceEquals(actual, def);

        internal void Add(Def def)
        {
            if (_resourceDefinitions != null) throw new ContentException("catalog collection is finalized");
            if (def == null || def.IsFrozen) throw new ContentException("catalog additions require fresh definition metadata");
            // Base-type indexes alias the same immutable definitions, never copy them or run Resolve twice.
            for (Type t = def.GetType(); t != typeof(Def); t = t.BaseType)
                if (_byId.TryGetValue(t, out Dictionary<string, Def> existing) && existing.TryGetValue(def.Id, out Def previous))
                    throw new ContentException(t.Name + " '" + def.Id + "' is declared twice by modules '" + previous.Module + "' and '" + def.Module + "'");
            if (def is IResourceDefinition && !(def is IResourceContentDefinition))
                throw new ContentException(def.Id + ": resource requires the authored resource content contract");
            if (def is IResourceContentDefinition && _resourceIds.ContainsKey(def.Id))
                throw new ContentException("IResourceContentDefinition '" + def.Id + "' is declared twice");
            def.Index = _byType.TryGetValue(def.GetType(), out List<Def> concrete) ? concrete.Count : 0;
            for (Type t = def.GetType(); t != typeof(Def); t = t.BaseType)
            {
                if (!_byType.TryGetValue(t, out List<Def> list)) { list = new List<Def>(); _byType.Add(t, list); }
                if (!_byId.TryGetValue(t, out Dictionary<string, Def> map)) { map = new Dictionary<string, Def>(StringComparer.Ordinal); _byId.Add(t, map); }
                list.Add(def);
                map.Add(def.Id, def);
            }
            // Contract aliases share these same actual definitions/indexes. Unrelated kinds may
            // reuse an ID, but a broad contract lookup may never silently select the first one.
            foreach (Type contract in def.GetType().GetInterfaces())
            {
                if (!typeof(IContentDefinition).IsAssignableFrom(contract)) continue;
                if (!_byType.TryGetValue(contract, out List<Def> list)) { list = new List<Def>(); _byType.Add(contract, list); }
                if (!_byId.TryGetValue(contract, out Dictionary<string, Def> map)) { map = new Dictionary<string, Def>(StringComparer.Ordinal); _byId.Add(contract, map); }
                list.Add(def);
                if (map.ContainsKey(def.Id)) map[def.Id] = null; else map.Add(def.Id, def);
            }
            if (def is IResourceContentDefinition resource)
            {
                ResourceId identity = new ResourceId(_resources.Count);
                _resources.Add(resource);
                _resourceIds.Add(def.Id, identity);
            }
        }

        /// <summary>
        /// Builds the resolved content for <paramref name="modules"/> from the raw documents: collect the
        /// sections of the active modules in order, read every def strictly, then
        /// resolve cross-references.
        /// </summary>
        internal static ResolvedContent Build(ContentTypes types, IReadOnlyList<RawModule> active)
        {
            var ids = new string[active.Count];
            var fp = new StateHasherLite();
            for (int m = 0; m < active.Count; m++)
            {
                ids[m] = active[m].Id;
                fp.Add(active[m].Id);
                fp.Add(active[m].Version.ToString());
                for (int q = 0; q < active[m].Requires.Length; q++)
                {
                    fp.Add(active[m].Requires[q].Id);
                    fp.Add(active[m].Requires[q].Version.ToString());
                }
                for (int d = 0; d < active[m].Documents.Count; d++) fp.Add(active[m].Documents[d].Canonical);
            }
            var content = new ResolvedContent(ids, fp.Value);
            var codecs = new List<StateCodecRegistration>();
            var declarations = new List<ContentTypes.ComponentType>();
            foreach (ContentTypes.ComponentType declaration in types.Components)
                if (declaration.Owner == "engine" || content.HasModule(declaration.Owner))
                { codecs.Add(declaration.Metadata); declarations.Add(declaration); }
            codecs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            content.ComponentCodecs = codecs.AsReadOnly();
            declarations.Sort((a, b) => string.CompareOrdinal(a.Metadata.Id, b.Metadata.Id));
            content.ComponentDeclarations = declarations.AsReadOnly();

            // 1. Collect raw entries per section, keyed by id, in module/document order.
            var raw = new Dictionary<string, List<RawEntry>>(StringComparer.Ordinal);
            for (int m = 0; m < active.Count; m++)
                for (int d = 0; d < active[m].Documents.Count; d++)
                {
                    RawDocument doc = active[m].Documents[d];
                    for (int k = 0; k < doc.Root.Count; k++)
                    {
                        string section = doc.Root.KeyAt(k);
                        JsonValue arr = doc.Root.ValueAt(k);
                        string where = active[m].Id + "@" + active[m].Version + " " + doc.Path;
                        if (!types.IsSection(section)) throw new ContentException(where + ": unknown section '" + section + "'");
                        if (types.TryGetSection(section, out ContentTypes.SectionType declared) && declared.Owner != "engine" && !content.HasModule(declared.Owner))
                            throw new ContentException(where + ": section '" + section + "' requires active provider '" + declared.Owner + "'");
                        if (!arr.IsArray) throw new ContentException(where + ": section '" + section + "' must be an array");
                        for (int i = 0; i < arr.Count; i++)
                        {
                            var entry = new RawEntry { Module = active[m].Id, Where = where + ":" + section + "[" + i + "]", Json = arr[i].Clone() };
                            if (!raw.TryGetValue(section, out List<RawEntry> list)) { list = new List<RawEntry>(); raw.Add(section, list); }
                            list.Add(entry);
                        }
                    }
                }

            // 2. Read every def, section by section in declaration order.
            var readDefs = new List<Def>();
            for (int s = 0; s < types.Sections.Count; s++)
            {
                ContentTypes.SectionType section = types.Sections[s];
                if (!raw.TryGetValue(section.Name, out List<RawEntry> entries)) continue;
                for (int e = 0; e < entries.Count; e++)
                {
                    RawEntry entry = entries[e];
                    Def def = ReadDefinition(section, entry, types, content);
                    try { content.Add(def); }
                    catch (ContentException error) { throw new ContentException(entry.Where + "(" + def.Id + "): " + error.Message); }
                    readDefs.Add(def);
                }
            }

            // One immutable collection projection of the original compact-ID table, not another catalog.
            // Finalize after all authored kinds collect and before any consumer resolves resource links.
            content._resourceDefinitions = content._resources.GetRange(1, content._resources.Count - 1).AsReadOnly();

            // 3. Cross-reference, validate and publish only the actual master-authored definitions.
            var states = content.All<IEntityStateDefinition>();
            foreach (IEntityStateDefinition state in states)
                EntityStateDeclaration.Validate(state, new ContentResolver(content, (Def)state));
            foreach (EntityDef entity in content.All<EntityDef>())
                foreach (ComponentDef component in entity.Components)
                {
                    ContentTypes.ComponentType declaration = null;
                    foreach (ContentTypes.ComponentType candidate in content.ComponentDeclarations)
                        if (candidate.DefType == component.GetType()) { declaration = candidate; break; }
                    if (declaration == null)
                        throw new ContentException("entity '" + entity.Id + "': component " + component.GetType().Name + " needs an active declared state codec");
                    component.Key = declaration.Metadata.Id;
                    component.StateCodec = declaration.Metadata;
                    component.OperationSchema = declaration.OperationSchema;
                    component.SignalSchema = declaration.SignalSchema;
                }
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Default)
                {
                    if (content.DefaultEntityState != null) throw new ContentException("entity_states must declare exactly one default state");
                    content.DefaultEntityState = states[i];
                }
                for (int k = 0; k < i; k++)
                    if (states[k].State == states[i].State)
                        throw new ContentException("entity_states publish duplicate state code '" + states[i].State.Id + "'");
            }
            if (content.Count<EntityDef>() > 0 && content.DefaultEntityState == null)
                throw new ContentException("entity_states must declare one explicit default for entities");
            // Resource providers can be declared after their consumers. Resolve shared unit/resource
            // contracts first, once; recipes, bills and entity ports then see fully validated metadata.
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is IUnitContentDefinition unit)
                {
                    var resolver = new ContentResolver(content, readDefs[i]);
                    readDefs[i].Resolve(resolver); UnitDeclaration.Validate(unit, resolver);
                }
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is IResourceContentDefinition resource)
                {
                    var resolver = new ContentResolver(content, readDefs[i]);
                    readDefs[i].Resolve(resolver); ResourceDeclaration.Validate(resource, resolver);
                }
            for (int i = 0; i < readDefs.Count; i++) if (readDefs[i] is ICapacityPolicyDefinition) readDefs[i].Resolve(new ContentResolver(content, readDefs[i]));
            // Layer aliases can follow their consumers in declaration/document order.
            // Admit them before a cell, condition, line or visibility rule dereferences metadata.
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is ISpatialLayerDefinition layer)
                {
                    var resolver = new ContentResolver(content, readDefs[i]);
                    readDefs[i].Resolve(resolver); SpatialLayerDeclaration.Validate(layer, resolver);
                }
            for (int i = 0; i < readDefs.Count; i++)
                if (!(readDefs[i] is EntityDef) && !(readDefs[i] is IUnitContentDefinition) && !(readDefs[i] is IResourceContentDefinition) && !(readDefs[i] is ICapacityPolicyDefinition) && !(readDefs[i] is ISpatialLayerDefinition))
                    readDefs[i].Resolve(new ContentResolver(content, readDefs[i]));
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is Standard.ITransformationDefinition transformation)
                    Standard.TransformationDeclaration.Validate(transformation, new ContentResolver(content, readDefs[i]));
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is ISpatialSpaceDefinition space)
                    SpatialSpaceContracts.Validate(space, new ContentResolver(content, readDefs[i]));
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is Standard.ITileConditionDefinition condition)
                    Standard.TileConditionContracts.Validate(condition, new ContentResolver(content, readDefs[i]));
            for (int i = 0; i < readDefs.Count; i++)
            {
                var resolver = new ContentResolver(content, readDefs[i]);
                if (readDefs[i] is Standard.ITileNavigationRule navigation)
                    Standard.TileNavigationContracts.Validate(navigation, resolver);
                if (readDefs[i] is Standard.IConstructionPlacementDefinition placement)
                    Standard.ConstructionPlacementContracts.Validate(placement, resolver);
            }
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is Standard.INetworkDefinition network)
                    Standard.NetworkDefinitionContracts.Validate(network, new ContentResolver(content, readDefs[i]));
            for (int i = 0; i < readDefs.Count; i++) if (readDefs[i] is EntityDef) readDefs[i].Resolve(new ContentResolver(content, readDefs[i]));
            // Linked metadata validation has one declared lifecycle, independent of section/document order.
            foreach (GameModePropertyDef property in content.All<GameModePropertyDef>())
                if (types.Definition.Find(property.Capability.Id) == null)
                    throw new ContentException(property.AuthoringPath + ": unknown registered mode capability '" + property.Capability.Id + "'");
            for (int i = 0; i < readDefs.Count; i++)
                readDefs[i].ValidateResolved(new ContentResolver(content, readDefs[i]));
            foreach (var effect in content.All<Standard.ILineEffectDefinition>())
                Standard.LineEffectContracts.ValidateMetadata(effect, new ContentResolver(content, (Def)effect));
            foreach (var line in content.All<Standard.ILineDefinition>())
                Standard.LineDefinitionContracts.Validate(line, new ContentResolver(content, (Def)line));
            for (int i = 0; i < readDefs.Count; i++)
                if (readDefs[i] is EntityDef e)
                    for (int c = 0; c < e.Components.Count; c++)
                    {
                        e.Components[c].Validate(e);
                        bool networkPorts = typeof(Standard.INetworkPorts).IsAssignableFrom(e.Components[c].ComponentType);
                        if (networkPorts != (e.Components[c] is Standard.INetworkPortsDefinition))
                            throw new ContentException(e.Id + ": network ports require matching metadata/runtime contracts");
                        if (networkPorts) Standard.NetworkPortsContracts.ValidateDefinition(content, e, (Standard.INetworkPortsDefinition)e.Components[c]);
                        if (e.Components[c] is Standard.ITransformerDefinition processor)
                            Standard.TransformationContracts.ValidateDefinition(content, e, processor);
                    }
            for (int i = 0; i < readDefs.Count; i++)
            {
                if (readDefs[i] is EntityDef e)
                    foreach (ComponentDef component in e.Components) component.Freeze();
                readDefs[i].Freeze();
            }
            return content;
        }

        /// <summary>One strict authored reader; discovery uses it only for exact engine-owned declarations.</summary>
        internal static Def ReadDefinition(ContentTypes.SectionType section, RawEntry entry, ContentTypes types, ResolvedContent content)
        {
            entry.Json.Freeze();
            var reader = new JsonReader(entry.Json, entry.Where);
            Def def = section.Create();
            def.Id = reader.String("id");
            def.Module = entry.Module;
            def.AuthoringPath = entry.Where + "(" + def.Id + ")";
            reader = new JsonReader(entry.Json, entry.Where + "(" + def.Id + ")");
            reader.Skip("id");
            def.Read(reader);
            if (def is EntityDef entity) ReadComponents(types, content, entity, reader);
            return def;
        }

        private static void ReadComponents(ContentTypes types, ResolvedContent content, EntityDef entity, JsonReader reader)
        {
            JsonValue comps = reader.Raw.Get("components");
            if (comps == null) { entity.SetComponents(new ComponentDef[0]); return; }
            if (!comps.IsObject) throw reader.Error("'components' must be an object");
            var defs = new List<ComponentDef>();
            for (int i = 0; i < comps.Count; i++)
            {
                string key = comps.KeyAt(i);
                if (!types.TryGetComponent(key, out ContentTypes.ComponentType ct))
                    throw reader.Error("unknown component '" + key + "'");
                if (ct.Owner != "engine" && !content.HasModule(ct.Owner))
                    throw reader.Error("component '" + key + "' requires active provider '" + ct.Owner + "'");
                JsonValue body = comps.ValueAt(i);
                var instances = new List<ComponentDef>();
                int count = body.IsArray ? body.Count : 1;
                if (count == 0) throw reader.Error("named component instances must be nonempty");
                for (int instance = 0; instance < count; instance++)
                {
                    ComponentDef cd = ct.Create(); cd.Key = key;
                    if (body.IsArray && (!(cd is INamedCapabilityDefinition) || !typeof(INamedEntityCapability).IsAssignableFrom(cd.ComponentType)))
                        throw reader.Error("component arrays require named definition/runtime capabilities");
                    JsonValue value = body.IsArray ? body[instance] : body;
                    if (body.IsArray && !value.IsObject) throw reader.Error("named component instance must be an object");
                    var cr = new JsonReader(value.Kind == JsonKind.Null ? JsonValue.NewObject() : value,
                        reader.Where + ".components." + key + (body.IsArray ? "[" + instance + "]" : ""));
                    cd.Read(cr); instances.Add(cd);
                }
                if (body.IsArray) instances.Sort((a, b) => string.CompareOrdinal(((INamedCapabilityDefinition)a).CapabilityId.Id, ((INamedCapabilityDefinition)b).CapabilityId.Id));
                defs.AddRange(instances);
            }
            entity.SetComponents(defs.ToArray());
        }

        internal sealed class RawEntry
        {
            public string Module;
            public string Where;
            public JsonValue Json;
        }

        /// <summary>FNV-1a over strings: the content fingerprint.</summary>
        private struct StateHasherLite
        {
            private ulong _h;
            public ulong Value => _h == 0 ? 14695981039346656037UL : _h;
            public void Add(string s)
            {
                if (_h == 0) _h = 14695981039346656037UL;
                for (int i = 0; i < s.Length; i++) { _h ^= s[i]; _h *= 1099511628211UL; }
                _h ^= 0xFF; _h *= 1099511628211UL;
            }
        }
    }

    internal sealed class RawDocument
    {
        public string Path;
        public JsonValue Root;
        public string Canonical;
    }

    internal sealed class RawModule
    {
        public string Id;
        public ModuleVersion Version;
        public ModuleRequirement[] Requires;
        public List<RawDocument> Documents = new List<RawDocument>();
    }

    internal static class CanonicalText
    {
        public static string Of(JsonValue v)
        {
            var sb = new StringBuilder();
            v.WriteCanonical(sb);
            return sb.ToString();
        }
    }
}
