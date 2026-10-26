using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Asset authoring/loader proof only: no host-side gameplay, world generation or updates.
    internal static partial class OilManifestContentTests
    {
        internal const string Case = "Oil master content: every actual document has one owner and resolves through native contracts";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(FinancialCase, FinancialPreservation);
            TestRunner.Add(TimingCase, TemporalPreservation);
            TestRunner.Add(DockCase, SiteDockAuthoring);
        }
        private sealed class Files : IFileSource
        {
            internal readonly Dictionary<string, string> Texts;
            internal readonly List<string> Reads = new List<string>();
            internal Files(IDictionary<string, string> texts) => Texts = new Dictionary<string, string>(texts, StringComparer.Ordinal);
            public bool Exists(string path) => Texts.ContainsKey(path);
            public string ReadText(string path) { Reads.Add(path); return Texts[path]; }
            internal void Edit(string path, Action<JsonValue> edit)
            { var root = Json.Parse(Texts[path], path); edit(root); Texts[path] = root.ToString(); }
        }
        private static Dictionary<string, int> Counts(IEnumerable<string> values)
            => values.GroupBy(value => value, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        private static void Reject(Check c, Action action, string reason, string label)
        {
            try { action(); c.That(false, "real loader must reject " + label); }
            catch (ContentException error) { c.That(error.Message.Contains(reason, StringComparison.Ordinal), "intended " + label + " diagnostic: " + error.Message); }
        }
        private static Dictionary<string, string> AuthoredDocuments()
        {
            var oil = ExternalModuleBoundaryTests.LoadOilAssembly();
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(oil.Location), "../../../../content"));
            return Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        }
        private static ResolvedContent ResolveOil(Files files)
        {
            var api = ExternalModuleBoundaryTests.LoadOilAssembly().GetType("OilCo.Game.OilCoGame", true);
            var load = (Func<IFileSource, ContentSet>)api.GetMethod("LoadContent").CreateDelegate(typeof(Func<IFileSource, ContentSet>));
            var set = load(files); return set.Resolve(set.ModuleIds);
        }
        private static void Run(Check c)
        {
            var oil = ExternalModuleBoundaryTests.LoadOilAssembly(); Type api = oil.GetType("OilCo.Game.OilCoGame", true);
            var load = (Func<IFileSource, ContentSet>)api.GetMethod("LoadContent").CreateDelegate(typeof(Func<IFileSource, ContentSet>));
            var factory = (Func<ContentSet, GameFactory>)api.GetMethod("Factory").CreateDelegate(typeof(Func<ContentSet, GameFactory>));
            var authored = AuthoredDocuments();
            var manifest = Json.Parse(authored["manifest.json"]); var modules = manifest.Get("modules");
            var documents = new Dictionary<string, string>(StringComparer.Ordinal);
            var identities = new List<string>(); var generic = new List<(string Module, string Id, Type Type)>();
            var native = new Dictionary<string, Type>(StringComparer.Ordinal) {
                ["resources"] = typeof(BasicResourceDef), ["entities"] = typeof(EntityDef),
                ["activities"] = typeof(ActivityDef), ["transformations"] = typeof(TransformationDef),
                ["worker_roles"] = typeof(WorkerRoleDef), ["entity_states"] = typeof(EntityStateDef),
                ["capacity_policies"] = typeof(BasicCapacityPolicyDef), ["lines"] = typeof(LineDef)
            };
            for (int i = 0; i < modules.Count; i++) {
                string owner = modules[i].Get("id").Text; var paths = modules[i].Get("documents");
                for (int j = 0; j < paths.Count; j++) {
                    string path = paths[j].Text; c.That(documents.TryAdd(path, owner), "exactly one actual master owner: " + path);
                    var document = Json.Parse(authored[path], path);
                    for (int k = 0; k < document.Count; k++) {
                        string section = document.KeyAt(k); var rows = document.ValueAt(k);
                        if (!rows.IsArray) throw new InvalidOperationException("actual authored section is not an array: " + path + ":" + section);
                        for (int q = 0; q < rows.Count; q++) {
                            string id = rows[q].Get("id").Text; identities.Add(owner + "\n" + id);
                            if (native.TryGetValue(section, out Type type)) generic.Add((owner, id, type));
                        }
                    }
                }
            }
            c.That(authored.Keys.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                documents.Keys.Append("manifest.json").OrderBy(value => value, StringComparer.Ordinal)),
                "every actual JSON asset is manifest-owned; no auto-discovered, stale or hidden document");
            var files = new Files(authored); ContentSet set = load(files); ResolvedContent content = set.Resolve(set.ModuleIds);
            c.That(files.Reads.Count == documents.Count + 1 && files.Reads.Distinct(StringComparer.Ordinal).Count() == files.Reads.Count
                && files.Reads.All(path => path == "manifest.json" || documents.ContainsKey(path)), "actual loader reads each explicit document once, no fallback files");
            c.Equal(modules.Count, set.ModuleIds.Count, "actual code/content manifest module inventory agrees");
            var definitions = content.All<IContentDefinition>(); var expected = Counts(identities);
            var actual = Counts(definitions.Select(def => def.Module + "\n" + def.Id));
            c.That(expected.Count == actual.Count && expected.All(pair => actual.TryGetValue(pair.Key, out int count) && count == pair.Value),
                "complete source identity multiset equals resolved catalog, including legal IDs reused across different kinds");
            c.Equal(identities.Count, definitions.Count, "no fabricated, dropped or duplicated definition");
            foreach (var definition in definitions) c.That(definition is Def def && def.IsFrozen && content.Owns(definition),
                "original frozen canonical definition owner: " + definition.Module + "/" + definition.Id);
            foreach (var entry in generic) c.Equal(1, definitions.Count(def => def.Module == entry.Module && def.Id == entry.Id && def.GetType() == entry.Type),
                "actual generic content uses one native framework implementation: " + entry.Type.Name + "/" + entry.Id);
            c.Equal(content.ResourceCount, content.All<IResourceContentDefinition>().Count, "resource contract aliases original compact catalog");
            c.Equal(content.Count<ActivityDef>(), content.All<IAuthoredActivityDefinition>().Count, "DAG contract aliases actual native activity definitions");
            c.Equal(content.Count<TransformationDef>(), content.All<ITransformationDefinition>().Count, "recipe contracts alias actual native transformations");
            c.That(ReferenceEquals(content, set.Resolve(set.ModuleIds.Reverse())), "requested module arrival order reuses sole deterministic catalog");
            GameFactory game = factory(set); var scenarios = game.Scenarios();
            c.Equal(content.Count<ScenarioDef>(), scenarios.Count, "complete public scenario menu from actual master");
            foreach (var scenario in scenarios) {
                c.Equal(content.Get<ScenarioDef>(scenario.Id).Time.Id, game.ScenarioTime(scenario.Id).Id, "actual profile resolves time metadata: " + scenario.Id);
                c.Equal(scenario.GameMode, game.ScenarioGameMode(scenario.Id).ModeId, "actual profile resolves game-mode metadata: " + scenario.Id);
            }
            var ignored = new Files(authored); ignored.Texts.Add("unlisted-hostile.json", "this is not JSON");
            c.Equal(content.Fingerprint, load(ignored).Resolve(set.ModuleIds).Fingerprint, "unlisted hostile input does not enter content identity");
            c.That(!ignored.Reads.Contains("unlisted-hostile.json"), "engine never reads an unlisted file");
            var reordered = new Files(authored); reordered.Edit("manifest.json", value => {
                var list = value.Get("modules"); for (int i = 0; i < list.Count; i++) {
                    var before = list[i].Get("documents"); var reverse = Json.Parse("[]");
                    for (int j = before.Count - 1; j >= 0; j--) reverse.Add(before[j].Clone());
                    list[i].Set("documents", reverse);
                }
            });
            c.Equal(content.Fingerprint, load(reordered).Resolve(set.ModuleIds).Fingerprint, "document declaration order cannot change canonical content identity");
            var expanded = new Files(authored); string extraId = "oil.manifest-audit.extra";
            var resource = Json.Parse(authored["oil/items.json"]).Get("resources")[0].Clone(); resource.Set("id", JsonValue.Of(extraId));
            var extra = Json.Parse("{\"resources\":[]}"); extra.Get("resources").Add(resource); expanded.Texts.Add("oil/manifest-audit-extra.json", extra.ToString());
            expanded.Edit("manifest.json", value => {
                var list = value.Get("modules"); for (int i = 0; i < list.Count; i++) if (list[i].Get("id").Text == "oil")
                    list[i].Get("documents").Add(JsonValue.Of("oil/manifest-audit-extra.json"));
            });
            var added = load(expanded).Resolve(set.ModuleIds); var introduced = added.Get<IResourceContentDefinition>(extraId);
            c.That(introduced.Module == "oil" && introduced.GetType() == typeof(BasicResourceDef) && added.Owns(introduced), "master-only new file resolves original native resource contract");
            c.Equal(content.ResourceCount + 1, added.ResourceCount, "exactly one new authored resource, no copied catalog");
            c.That(added.Fingerprint != content.Fingerprint && !content.TryResource(extraId, out _), "new document changes only its own content set identity");
            foreach (string defect in new[] { "missing-file", "duplicate-path", "duplicate-module", "module-version", "dependency-version", "unknown-section", "duplicate-resource" }) {
                var invalid = new Files(authored); string reason;
                switch (defect) {
                    case "missing-file": invalid.Texts.Remove("oil/items.json"); reason = "not found"; break;
                    case "duplicate-path": invalid.Edit("manifest.json", value => value.Get("modules")[0].Get("documents").Add(JsonValue.Of("oil/items.json"))); reason = "multiple owners"; break;
                    case "duplicate-module": invalid.Edit("manifest.json", value => value.Get("modules").Add(value.Get("modules")[0].Clone())); reason = "declared twice"; break;
                    case "module-version": invalid.Edit("manifest.json", value => value.Get("modules")[0].Set("version", JsonValue.Of("9.0.0"))); reason = "code/content version differs"; break;
                    case "dependency-version": invalid.Edit("manifest.json", value => {
                        var list = value.Get("modules"); for (int i = 0; i < list.Count; i++) if (list[i].Get("requires").Count > 0) {
                            list[i].Get("requires")[0].Set("version", JsonValue.Of("9.0.0")); break;
                        }
                    }); reason = "dependency/version differs"; break;
                    case "unknown-section": invalid.Edit("oil/items.json", value => value.Set("invalid_unowned_section", Json.Parse("[]"))); reason = "unknown section"; break;
                    default: invalid.Edit("oil/items.json", value => value.Get("resources").Add(value.Get("resources")[0].Clone())); reason = "declared twice"; break;
                }
                Reject(c, () => { var rejected = load(invalid); rejected.Resolve(rejected.ModuleIds); }, reason, defect);
            }
            Console.WriteLine("  actual master: modules=" + modules.Count + ", documents=" + documents.Count + ", definitions=" + definitions.Count
                + ", native-generic-definitions=" + generic.Count + ", profiles=" + scenarios.Count + ", intended-refusals=7");
        }
    }
}
