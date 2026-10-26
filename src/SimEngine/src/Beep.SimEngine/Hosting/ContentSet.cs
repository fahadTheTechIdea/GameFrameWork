using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>
    /// Where a game's content files come from. The host supplies one: files on disk, files read up
    /// front, an embedded archive. Paths use '/' and are relative
    /// to the content root.
    /// </summary>
    public interface IFileSource
    {
        bool Exists(string path);
        string ReadText(string path);
    }

    /// <summary>
    /// A file source over files the host has already read (for platforms whose file access is
    /// asynchronous: read everything first, then hand it over).
    /// </summary>
    public sealed class PreloadedFileSource : IFileSource
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);
        public PreloadedFileSource Add(string path, string text) { _files[path] = text; return this; }
        public bool Exists(string path) => _files.ContainsKey(path);
        public string ReadText(string path) => _files.TryGetValue(path, out string t) ? t : throw new System.IO.FileNotFoundException(path);
    }

    /// <summary>
    /// A game's content, loaded once (the XNA ContentManager): every content module's documents read
    /// and parsed from <c>manifest.json</c>, frozen, and resolved into typed defs per module set on
    /// demand. One content set serves any number of games, saves and loads in the process.
    /// </summary>
    public sealed class ContentSet
    {
        private readonly ContentTypes _types;
        private readonly List<RawModule> _modules;
        private readonly Dictionary<string, ResolvedContent> _resolved = new Dictionary<string, ResolvedContent>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        private ContentSet(ContentTypes types, List<RawModule> modules)
        {
            _types = types;
            _modules = modules;
        }

        /// <summary>The content module ids the manifest declares, in load order.</summary>
        public IReadOnlyList<string> ModuleIds
        {
            get
            {
                var ids = new List<string>(_modules.Count);
                for (int i = 0; i < _modules.Count; i++) ids.Add(_modules[i].Id);
                return ids.AsReadOnly();
            }
        }

        /// <summary>
        /// Reads <c>manifest.json</c> and every document it lists. <paramref name="types"/> is what the
        /// game understands (its modules declare it); an unknown section or component fails later, when a
        /// module set is resolved.
        /// </summary>
        public static ContentSet Load(IFileSource files, ContentTypes types)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (types == null) throw new ArgumentNullException(nameof(types));
            if (!files.Exists("manifest.json")) throw new ContentException("content root has no manifest.json");
            JsonValue manifest = Json.Parse(files.ReadText("manifest.json"), "manifest.json");
            var mr = new JsonReader(manifest, "manifest.json");
            if (mr.Int("schema") != 1) throw mr.Error("unsupported manifest schema");
            JsonValue list = mr.Array("modules") ?? throw mr.Error("missing 'modules'");
            mr.End();
            var modules = new List<RawModule>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var documentOwners = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < list.Count; i++)
            {
                var r = new JsonReader(list[i], "manifest.json:modules[" + i + "]");
                string id = r.String("id");
                ModuleRegistration code = types.Definition.Find(id);
                if (code == null) throw r.Error("no registered code module '" + id + "'");
                if (!ModuleVersion.TryParse(r.String("version"), out ModuleVersion version)) throw r.Error("version must be canonical major.minor.patch");
                if (version != code.Version) throw r.Error("code/content version differs for '" + id + "'");
                var requirements = new List<ModuleRequirement>();
                JsonValue dependencies = r.Array("requires");
                if (dependencies != null)
                    for (int q = 0; q < dependencies.Count; q++)
                    {
                        var dependency = new JsonReader(dependencies[q], r.Where + ".requires[" + q + "]");
                        string dependencyId = dependency.String("id");
                        if (!ModuleVersion.TryParse(dependency.String("version"), out ModuleVersion dependencyVersion)) throw dependency.Error("invalid dependency version");
                        dependency.End();
                        if (requirements.Exists(existing => existing.Id == dependencyId)) throw dependency.Error("duplicate requirement '" + dependencyId + "'");
                        if (types.Definition.Find(dependencyId) == null) throw dependency.Error("unknown dependency '" + dependencyId + "'");
                        requirements.Add(new ModuleRequirement(dependencyId, dependencyVersion));
                    }
                if (requirements.Count != code.Requires.Count) throw r.Error("manifest dependencies differ from code module '" + id + "'");
                for (int q = 0; q < code.Requires.Count; q++)
                    if (!requirements.Exists(req => req.Id == code.Requires[q].Id && req.Version == code.Requires[q].Version))
                        throw r.Error("manifest dependency/version differs for '" + code.Requires[q].Id + "'");
                requirements.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                var module = new RawModule { Id = id, Version = version, Requires = requirements.ToArray() };
                string[] docs = r.Strings("documents");
                r.End();
                if (!seen.Add(module.Id)) throw r.Error("module '" + module.Id + "' declared twice");
                for (int d = 0; d < docs.Length; d++)
                {
                    ValidateDocumentPath(docs[d], r);
                    if (!documentOwners.Add(docs[d])) throw r.Error("document '" + docs[d] + "' has multiple owners");
                    if (!files.Exists(docs[d])) throw r.Error("document '" + docs[d] + "' not found");
                    JsonValue root = Json.Parse(files.ReadText(docs[d]), docs[d]);
                    if (!root.IsObject) throw new ContentException(docs[d] + ": a document must be an object of sections");
                    root.Freeze();
                    module.Documents.Add(new RawDocument { Path = docs[d], Root = root, Canonical = CanonicalText.Of(root) });
                }
                module.Documents.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
                modules.Add(module);
            }
            if (modules.Count != types.Definition.Modules.Count) throw new ContentException("manifest must declare every registered code module, including modules without documents");
            modules.Sort((a, b) => IndexOf(types.Definition, a.Id).CompareTo(IndexOf(types.Definition, b.Id)));
            return new ContentSet(types, modules);
        }

        private static int IndexOf(GameDefinition definition, string id)
        {
            for (int i = 0; i < definition.Modules.Count; i++) if (definition.Modules[i].Id == id) return i;
            throw new ContentException("unknown module '" + id + "'");
        }

        private static void ValidateDocumentPath(string path, JsonReader reader)
        {
            if (string.IsNullOrEmpty(path) || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0)
                throw reader.Error("document paths must be canonical relative '/' paths");
            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
                if (segments[i].Length == 0 || segments[i] == "." || segments[i] == "..")
                    throw reader.Error("document path escapes or aliases the content root: '" + path + "'");
        }

        public bool HasModule(string id)
        {
            for (int i = 0; i < _modules.Count; i++) if (_modules[i].Id == id) return true;
            return false;
        }

        // Transient projections over this loader's raw documents, not a second typed catalog.
        private IEnumerable<ResolvedContent.RawEntry> EngineEntries<TDef>(string section) where TDef : Def
        {
            if (!_types.TryGetSection(section, out ContentTypes.SectionType declaration)
                || declaration.Owner != "engine" || declaration.DefType != typeof(TDef))
                throw new InvalidOperationException("discovery requires an exact engine-owned declaration: " + section);
            for (int m = 0; m < _modules.Count; m++)
                for (int d = 0; d < _modules[m].Documents.Count; d++)
                {
                    RawDocument doc = _modules[m].Documents[d];
                    JsonValue entries = doc.Root.Get(section);
                    if (entries == null) continue;
                    string where = _modules[m].Id + "@" + _modules[m].Version + " " + doc.Path + ":" + section;
                    if (!entries.IsArray) throw new ContentException(where + " must be an array");
                    for (int i = 0; i < entries.Count; i++)
                        yield return new ResolvedContent.RawEntry { Module = _modules[m].Id, Where = where + "[" + i + "]", Json = entries[i] };
                }
        }

        internal TDef FindEngineDeclaration<TDef>(string section, string id) where TDef : Def
        {
            ResolvedContent.RawEntry found = null;
            foreach (ResolvedContent.RawEntry entry in EngineEntries<TDef>(section))
            {
                string candidate = new JsonReader(entry.Json, entry.Where).String("id");
                if (candidate != id) continue;
                if (found != null) throw new ContentException(entry.Where + "(" + id + "): declared twice; first declaration " + found.Where);
                found = entry;
            }
            if (found == null) return null;
            _types.TryGetSection(section, out ContentTypes.SectionType declaration);
            return (TDef)ResolvedContent.ReadDefinition(declaration, found, _types, null);
        }

        internal IReadOnlyList<ScenarioInfo> Scenarios()
        {
            var list = new List<ScenarioInfo>();
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            _types.TryGetSection("scenarios", out ContentTypes.SectionType declaration);
            foreach (ResolvedContent.RawEntry entry in EngineEntries<ScenarioDef>("scenarios"))
            {
                var scenario = (ScenarioDef)ResolvedContent.ReadDefinition(declaration, entry, _types, null);
                if (seen.TryGetValue(scenario.Id, out string previous))
                    throw new ContentException(entry.Where + "(" + scenario.Id + "): declared twice; first declaration " + previous);
                seen.Add(scenario.Id, entry.Where);
                list.Add(new ScenarioInfo(scenario));
            }
            return list.AsReadOnly();
        }

        internal ResolvedContent ResolveScenario(string id, IEnumerable<string> additions)
        {
            ScenarioDef authored = FindEngineDeclaration<ScenarioDef>("scenarios", id)
                ?? throw new ArgumentException("unknown scenario '" + id + "'", nameof(id));
            var requested = new List<string>(authored.Modules);
            if (additions != null) requested.AddRange(additions);
            var selected = new List<string>();
            foreach (ModuleRegistration module in _types.Definition.Select(requested)) selected.Add(module.Id);
            if (!selected.Contains(authored.Module))
                throw new ContentException("scenario '" + authored.Id + "' must select its declaring module '" + authored.Module + "'");
            return Resolve(selected);
        }

        /// <summary>
        /// The typed content for a set of content modules. The set is taken in manifest order (so the same
        /// modules always resolve the same way); every requirement must be present. Cached.
        /// </summary>
        public ResolvedContent Resolve(IEnumerable<string> moduleIds)
        {
            var wanted = new HashSet<string>(moduleIds ?? throw new ArgumentNullException(nameof(moduleIds)), StringComparer.Ordinal);
            var active = new List<RawModule>();
            for (int i = 0; i < _modules.Count; i++)
                if (wanted.Contains(_modules[i].Id)) active.Add(_modules[i]);
            if (active.Count != wanted.Count)
                foreach (string id in wanted)
                    if (!HasModule(id)) throw new ContentException("no content module '" + id + "'");
            for (int i = 0; i < active.Count; i++)
                for (int q = 0; q < active[i].Requires.Length; q++)
                    if (!wanted.Contains(active[i].Requires[q].Id))
                        throw new ContentException("content module '" + active[i].Id + "' requires '" + active[i].Requires[q].Id + "'");
            var key = new System.Text.StringBuilder();
            for (int i = 0; i < active.Count; i++) key.Append(active[i].Id).Append('|');
            lock (_gate)
            {
                if (_resolved.TryGetValue(key.ToString(), out ResolvedContent cached)) return cached;
                ResolvedContent built = ResolvedContent.Build(_types, active);
                _resolved.Add(key.ToString(), built);
                return built;
            }
        }
    }
}
