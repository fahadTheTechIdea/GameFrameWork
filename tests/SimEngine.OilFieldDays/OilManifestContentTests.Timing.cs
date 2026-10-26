using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Read-only evidence over the same master/catalog/archive; never a gameplay evaluator.
    internal static partial class OilManifestContentTests
    {
        internal const string TimingCase = "Oil temporal migration: original work graphs machine ratings and explicit physical handling are preserved";
        private static void NodeTimingFacts(Dictionary<string, string> facts, string graph, string node,
            GameDuration duration, IEnumerable<string> roles, IEnumerable<string> after, string executor, ISimulationTime time)
        {
            string key = "graph:" + graph + "/" + node;
            Put(facts, key + ".minutes", time.ExactTicks(duration));
            Put(facts, key + ".roles", string.Join(",", roles.OrderBy(value => value, StringComparer.Ordinal)));
            Put(facts, key + ".after", string.Join(",", after.OrderBy(value => value, StringComparer.Ordinal)));
            Put(facts, key + ".executor", executor);
        }
        private static void GraphTimingFacts(Dictionary<string, string> facts, string key, IActivityDefinition activity, ISimulationTime time)
        {
            Put(facts, "graph:" + key + ".binding", activity.PlanKey);
            Put(facts, "graph:" + key + ".count", activity.Graph.Nodes.Count);
            foreach (var node in activity.Graph.Nodes)
                NodeTimingFacts(facts, key, node.Id, node.Duration, node.Roles.Select(role => role.Value), node.Dependencies, node.Executor.Id, time);
        }
        private static void OriginalGraphTiming(Dictionary<string, string> facts, string key, JsonValue nodes,
            string binding, ISimulationTime time, bool splitDrilling = false)
        {
            Put(facts, "graph:" + key + ".binding", "activity:" + binding);
            Put(facts, "graph:" + key + ".count", nodes.Count + (splitDrilling ? 1 : 0));
            foreach (var node in Rows(nodes))
            {
                string id = node.Get("id").Text;
                if (splitDrilling && id == "complete") id = "casing_cementing";
                string role = node.Get("role")?.Text;
                NodeTimingFacts(facts, key, id, GameDuration.Decimal(node.Get("hours").Text, DurationUnit.Hour),
                    role == null ? Array.Empty<string>() : new[] { role }, Rows(node.Get("after") ?? Json.Parse("[]")).Select(value => value.Text),
                    role == null ? "oil.activity.timed" : "oil.activity.staffed", time);
            }
            if (splitDrilling)
                NodeTimingFacts(facts, key, "complete", GameDuration.Zero, Array.Empty<string>(), new[] { "casing_cementing" }, "oil.activity.timed", time);
        }
        private static Dictionary<string, string> OriginalTimingFacts(Dictionary<string, JsonValue> original, Files authored, ISimulationTime time)
        {
            var facts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entity in Rows(original["structures"].Get("entities")))
            {
                string id = entity.Get("id").Text; var parts = entity.Get("components"); var build = parts.Get("construction");
                if (build != null)
                {
                    OriginalGraphTiming(facts, "build:" + id, build.Get("steps"), "oil.build." + id, time);
                    var remove = build.Get("demolish");
                    Put(facts, "graph:remove:" + id + ".binding", "activity:oil.remove." + id);
                    Put(facts, "graph:remove:" + id + ".count", 1);
                    NodeTimingFacts(facts, "remove:" + id, "remove", GameDuration.Decimal(remove.Get("hours").Text, DurationUnit.Hour),
                        new[] { remove.Get("role").Text }, Array.Empty<string>(), "oil.activity.staffed", time);
                }
                if (parts.Get("lab") is JsonValue lab)
                {
                    MoneyFact(facts, "capacity:" + id, Money(lab.Get("points_per_day")));
                    Put(facts, "capacity:" + id + ".pool", "oil.research");
                }
            }
            foreach (var order in Rows(original["work"].Get("orders")))
            {
                string id = order.Get("id").Text;
                OriginalGraphTiming(facts, "work:" + id, order.Get("steps"), id, time,
                    id == "drill_well" || id == "drill_offshore_well");
            }
            foreach (var recipe in Rows(original["production"].Get("recipes")))
            {
                string key = "recipe:" + recipe.Get("id").Text;
                Put(facts, key + ".minutes", time.ExactTicks(GameDuration.Decimal(recipe.Get("hours").Text, DurationUnit.Hour)));
                Put(facts, key + ".mode", TransformationMode.Batch); Put(facts, key + ".entity", recipe.Get("vehicle")?.Text);
            }
            Put(facts, "addition:dispose_produced_water.mode", TransformationMode.Continuous);
            foreach (var tech in Rows(original["tech"].Get("tech")))
            {
                string id = tech.Get("id").Text, key = "research:" + id;
                Put(facts, "graph:" + key + ".binding", "activity:" + id); Put(facts, "graph:" + key + ".count", 1);
                NodeTimingFacts(facts, key, "study", GameDuration.Decimal(tech.Get("points").Text, DurationUnit.Day),
                    Array.Empty<string>(), Array.Empty<string>(), "oil.research.capacity", time);
                Put(facts, key + ".prerequisites", string.Join(",", Rows(tech.Get("requires") ?? Json.Parse("[]")).Select(row => row.Text).OrderBy(value => value, StringComparer.Ordinal)));
            }
            foreach (var line in Rows(original["networks"].Get("lines")))
            {
                string id = line.Get("id").Text, key = "line:" + id;
                Put(facts, "graph:" + key + ".binding", "activity:oil.line." + id + ".activity"); Put(facts, "graph:" + key + ".count", 1);
                NodeTimingFacts(facts, key, "install", GameDuration.Decimal(line.Get("hours_per_tile").Text, DurationUnit.Hour),
                    new[] { line.Get("role").Text }, Array.Empty<string>(), "oil.infrastructure.staffed", time);
            }
            var handling = Json.Parse(authored.Texts["oil/vehicle-handling.json"]).Get("handling_profiles");
            foreach (var vehicle in Rows(original["vehicles"].Get("entities")))
            {
                string id = vehicle.Get("id").Text; var parts = vehicle.Get("components");
                MoneyFact(facts, "speed:" + id, Money(parts.Get("mover").Get("speed")));
                // The immutable original WorkerDef explicitly defaulted omitted work_rate to Fix.One.
                MoneyFact(facts, "worker:" + id, parts.Get("worker").Get("work_rate") is JsonValue rate ? Money(rate) : Fix.One);
                var roles = Rows(parts.Get("worker").Get("roles")).Select(row => row.Text).ToArray();
                Put(facts, "worker:" + id + ".roles", string.Join(",", roles.OrderBy(value => value, StringComparer.Ordinal)));
                if (!roles.Contains("haul", StringComparer.Ordinal)) continue;
                string profile = "oil.handling." + id; Put(facts, "handling:" + id + ".binding", profile);
                Fix basis = Money(parts.Get("storage").Get("capacity"));
                // Handling is a deliberate new physical policy; support rows are explicitly authored, never inferred at runtime.
                foreach (var row in Rows(Rows(handling).Single(row => row.Get("id").Text == profile).Get("resources")))
                {
                    string key = "handling:" + id + ":" + row.Get("resource").Text;
                    MoneyFact(facts, key + ".basis", basis);
                    foreach (string phase in new[] { "loading", "unloading" })
                    { Put(facts, key + "." + phase, 60); Put(facts, key + ".half-" + phase, 30); }
                }
            }
            foreach (var scenario in Rows(original["scenarios"].Get("scenarios")))
            {
                string key = "profile:" + scenario.Get("id").Text;
                Put(facts, key + ".quantum-numerator", 60); Put(facts, key + ".quantum-denominator", 1);
                Put(facts, key + ".frame", 10); Put(facts, key + ".pace", 30);
            }
            return facts;
        }
        private static Dictionary<string, string> CurrentTimingFacts(Files files, ISimulationTime time)
        {
            var content = ResolveOil(files); var facts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entity in content.All<EntityDef>())
            {
                if (entity.Component<IConstructionDefinition>() is IConstructionDefinition build)
                {
                    GraphTimingFacts(facts, "build:" + entity.Id, build.Workflow, time);
                    GraphTimingFacts(facts, "remove:" + entity.Id, build.Demolition.Workflow, time);
                }
                foreach (var capacity in entity.Components.OfType<ICompanyActivityRateDefinition>())
                { MoneyFact(facts, "capacity:" + entity.Id, capacity.NominalRate); Put(facts, "capacity:" + entity.Id + ".pool", capacity.Pool.Id); }
                if (entity.Component<IMobilityDefinition>() is IMobilityDefinition mobility) MoneyFact(facts, "speed:" + entity.Id, mobility.TilesPerHour);
                if (entity.Component<IWorkerDefinition>() is IWorkerDefinition worker)
                { MoneyFact(facts, "worker:" + entity.Id, worker.WorkRate); Put(facts, "worker:" + entity.Id + ".roles", string.Join(",", worker.Roles.Select(role => role.Value).OrderBy(value => value, StringComparer.Ordinal))); }
                if (entity.Component<ITransporterDefinition>() is ITransporterDefinition carrier)
                    Put(facts, "handling:" + entity.Id + ".binding", carrier.Handling.Id);
            }
            foreach (var activity in content.All<IAuthoredActivityDefinition>())
                if (!activity.Id.StartsWith("oil.", StringComparison.Ordinal) && !(activity is ITechnologyDefinition))
                    GraphTimingFacts(facts, "work:" + activity.Id, activity, time);
            foreach (var recipe in content.All<ITransformationDefinition>())
            {
                if (recipe.Id == "dispose_produced_water") { Put(facts, "addition:" + recipe.Id + ".mode", recipe.Mode); continue; }
                string key = "recipe:" + recipe.Id;
                Put(facts, key + ".minutes", time.ExactTicks(recipe.Duration)); Put(facts, key + ".mode", recipe.Mode); Put(facts, key + ".entity", recipe.EntityOutput?.Id);
            }
            foreach (var tech in content.All<ITechnologyDefinition>())
            {
                string key = "research:" + tech.Id; GraphTimingFacts(facts, key, tech, time);
                Put(facts, key + ".prerequisites", string.Join(",", tech.Dependencies.OrderBy(value => value, StringComparer.Ordinal)));
            }
            foreach (var line in content.All<ILineDefinition>()) GraphTimingFacts(facts, "line:" + line.Id, line.Workflow, time);
            foreach (var profile in Rows(Json.Parse(files.Texts["oil/vehicle-handling.json"]).Get("handling_profiles")))
            {
                string id = profile.Get("id").Text.Substring("oil.handling.".Length);
                var definition = content.Get<IResourceHandlingProfile>(profile.Get("id").Text);
                foreach (var row in Rows(profile.Get("resources")))
                {
                    string resource = row.Get("resource").Text, key = "handling:" + id + ":" + resource;
                    Fix basis = Money(row.Get("quantity")); MoneyFact(facts, key + ".basis", basis);
                    if (!content.TryResource(resource, out var resourceId)) throw new InvalidOperationException("missing actual handling resource: " + resource);
                    foreach (var phase in new[] { ResourceHandlingPhase.Loading, ResourceHandlingPhase.Unloading })
                    {
                        string name = phase == ResourceHandlingPhase.Loading ? "loading" : "unloading";
                        var full = definition.CheckHandling(time, resourceId, basis, phase);
                        var half = definition.CheckHandling(time, resourceId, basis / Fix.FromInt(2), phase);
                        if (!full.Ready || !half.Ready) throw new InvalidOperationException("authored handling refused: " + key);
                        Put(facts, key + "." + name, full.RequiredTicks); Put(facts, key + ".half-" + name, half.RequiredTicks);
                    }
                }
            }
            foreach (var scenario in content.All<ScenarioDef>())
            {
                string key = "profile:" + scenario.Id; var definition = scenario.Time;
                Put(facts, key + ".quantum-numerator", definition.QuantumNumerator); Put(facts, key + ".quantum-denominator", definition.QuantumDenominator);
                Put(facts, key + ".frame", definition.FrameTicks); Put(facts, key + ".pace", definition.TicksPerSecond);
            }
            return facts;
        }
        private static void TemporalPreservation(Check c)
        {
            var original = OriginalDocuments(c, "structures", "vehicles", "networks", "work", "production", "tech", "scenarios");
            var authored = AuthoredDocuments(); var baseline = new Files(authored);
            var definition = ResolveOil(baseline).Get<ScenarioDef>("frontier").Time;
            c.Equal(60L, definition.QuantumNumerator, "audit uses explicitly authored minute quantum");
            c.Equal(1L, definition.QuantumDenominator, "exact minute converter, not a second duration parser");
            ISimulationTime time = new GameTime(definition);
            var expected = OriginalTimingFacts(original, baseline, time); var actual = CurrentTimingFacts(baseline, time);
            c.That(expected.Keys.OrderBy(key => key, StringComparer.Ordinal).SequenceEqual(actual.Keys.OrderBy(key => key, StringComparer.Ordinal)),
                "complete temporal fact keys: missing=" + string.Join(",", expected.Keys.Except(actual.Keys)) + "; extra=" + string.Join(",", actual.Keys.Except(expected.Keys)));
            foreach (var fact in expected)
                c.That(actual.TryGetValue(fact.Key, out var value) && value == fact.Value, fact.Key + ": expected=" + fact.Value + "; actual=" + value);
            foreach (string mutation in new[] { "build", "removal", "work", "dependency", "recipe", "research", "line", "speed", "worker", "handling", "profile" })
            {
                var changed = new Files(authored);
                switch (mutation)
                {
                    case "build": changed.Edit("oil/construction.json", json => json.Get("activities")[0].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("9"))); break;
                    case "removal": changed.Edit("oil/construction.json", json => json.Get("activities")[1].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("7"))); break;
                    case "work": changed.Edit("oil/work.json", json => json.Get("activities")[0].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("13"))); break;
                    case "dependency": changed.Edit("oil/work.json", json => json.Get("activities")[0].Get("nodes")[1].Set("after", Json.Parse("[]"))); break;
                    case "recipe": changed.Edit("oil/production.json", json => json.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("37"))); break;
                    case "research": changed.Edit("oil/tech.json", json => json.Get("tech")[0].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("121"))); break;
                    case "line": changed.Edit("oil/networks.json", json => json.Get("activities")[0].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("3"))); break;
                    case "speed": changed.Edit("oil/vehicles.json", json => json.Get("entities")[0].Get("components").Get("mover").Set("speed", JsonValue.Number("17"))); break;
                    case "worker": changed.Edit("oil/vehicles.json", json => json.Get("entities")[0].Get("components").Get("worker").Set("work_rate", JsonValue.Number("2"))); break;
                    case "handling": changed.Edit("oil/vehicle-handling.json", json => json.Get("handling_profiles")[0].Get("resources")[0].Get("loading").Set("numerator", JsonValue.Number("2"))); break;
                    default: changed.Edit("oil/game-modes.json", json => json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("2"))); break;
                }
                var drift = CurrentTimingFacts(changed, time);
                c.That(expected.Count != drift.Count || expected.Any(fact => !drift.TryGetValue(fact.Key, out var value) || value != fact.Value), "audit detects deliberate temporal drift: " + mutation);
            }
            Console.WriteLine("  temporal authoring: documents=" + original.Count + ", facts=" + expected.Count + ", deliberate drift cases=11; no generated worlds or updates");
        }
    }
}
