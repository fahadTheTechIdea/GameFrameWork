using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Read-only migration evidence in the existing content-audit owner, not a gameplay model.
    internal static partial class OilManifestContentTests
    {
        internal const string FinancialCase = "Oil financial migration: immutable original amounts credit bindings and schedules are preserved";
        private static IEnumerable<JsonValue> Rows(JsonValue array)
        { for (int i = 0; i < array.Count; i++) yield return array[i]; }
        private static Fix Money(JsonValue number) => Fix.Parse(number.Text);
        private static void Put(Dictionary<string, string> facts, string key, object value)
            => facts.Add(key, value is GameDuration duration
                ? duration.Literal + "/" + duration.Divisor + "*" + duration.Scale
                : value?.ToString() ?? "null");
        private static void MoneyFact(Dictionary<string, string> facts, string key, Fix value)
            => Put(facts, key, value.Raw);
        private static void ScheduleFacts(Dictionary<string, string> facts, string key, GameDuration period,
            GameDuration offset, Code reason, MonetaryCostCategory category, string condition)
        {
            // These exact authored rational representations are intentionally part of migration evidence.
            Put(facts, key + ".period", period); Put(facts, key + ".offset", offset);
            Put(facts, key + ".reason", reason.Id); Put(facts, key + ".category", category);
            Put(facts, key + ".condition", condition);
        }
        private static void ExpectedCost(Dictionary<string, string> facts, string id, string activity,
            string node, JsonValue amount, bool credit)
        {
            string key = "cost:" + id;
            MoneyFact(facts, key, Money(amount)); Put(facts, key + ".credit", credit);
            Put(facts, key + ".category", MonetaryCostCategory.Construction);
            Put(facts, key + ".activity", activity); Put(facts, key + ".node", node);
            Put(facts, key + ".bound", true);
        }
        private static Dictionary<string, string> OriginalFinancialFacts(Dictionary<string, JsonValue> original)
        {
            var facts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var scenario in Rows(original["scenarios"].Get("scenarios")))
            {
                string id = scenario.Get("id").Text;
                Fix amount = Money(scenario.Get("settings").Get("oil").Get("start").Get("cash"));
                Put(facts, "mode:" + id, amount.IsPositive);
                if (amount.IsPositive) MoneyFact(facts, "account:" + id, amount);
            }
            foreach (var entity in Rows(original["structures"].Get("entities")).Concat(Rows(original["vehicles"].Get("entities"))))
            {
                string id = entity.Get("id").Text; var components = entity.Get("components");
                var upkeep = components.Get("upkeep");
                if (upkeep != null)
                {
                    string key = "upkeep:" + id; MoneyFact(facts, key, Money(upkeep.Get("per_day")));
                    ScheduleFacts(facts, key, GameDuration.Days(1), GameDuration.Minutes(30),
                        new Code("ledger.upkeep"), MonetaryCostCategory.Running, null);
                }
                var power = components.Get("power_source")?.Get("cash_per_hour");
                if (power != null && Money(power).IsPositive)
                {
                    string key = "power:" + id; MoneyFact(facts, key, Money(power));
                    ScheduleFacts(facts, key, GameDuration.Hours(1), GameDuration.Minutes(5),
                        new Code("ledger.power"), MonetaryCostCategory.Running, "power");
                }
                foreach (string kind in new[] { "supplier", "buyer" })
                {
                    var prices = components.Get(kind)?.Get(kind == "supplier" ? "sells" : "buys");
                    if (prices == null) continue;
                    string key = (kind == "buyer" ? "sale:" : "supplier:") + id;
                    for (int i = 0; i < prices.Count; i++) MoneyFact(facts, key + ":" + prices.KeyAt(i), Money(prices.ValueAt(i)));
                    if (kind == "buyer")
                    {
                        Put(facts, key + ".period", GameDuration.Hours(1)); Put(facts, key + ".offset", GameDuration.Minutes(30));
                        Put(facts, key + ".reason", "ledger.sales"); Put(facts, key + ".eligibility", "oil.sale.ready");
                    }
                }
                foreach (var node in Rows(components.Get("construction")?.Get("steps") ?? Json.Parse("[]")))
                    if (node.Get("cash") != null && Money(node.Get("cash")).IsPositive)
                        ExpectedCost(facts, "oil.build." + id + "." + node.Get("id").Text + ".cost",
                            "oil.build." + id, node.Get("id").Text, node.Get("cash"), node.Get("on_credit")?.Bool == true);
            }
            foreach (var line in Rows(original["networks"].Get("lines")))
            {
                string id = line.Get("id").Text;
                ExpectedCost(facts, "oil.line." + id + ".cost", "oil.line." + id + ".activity", "install", line.Get("cash_per_tile"), false);
            }
            foreach (var order in Rows(original["work"].Get("orders")))
                foreach (var node in Rows(order.Get("steps")))
                    if (node.Get("cash") != null && Money(node.Get("cash")).IsPositive)
                    {
                        string id = order.Get("id").Text, oldNode = node.Get("id").Text;
                        string actualNode = (id == "drill_well" || id == "drill_offshore_well") && oldNode == "complete" ? "casing_cementing" : oldNode;
                        ExpectedCost(facts, "oil.activity." + id + "." + oldNode + ".cost", id, actualNode,
                            node.Get("cash"), node.Get("on_credit")?.Bool == true);
                    }
            foreach (var recipe in Rows(original["production"].Get("recipes")))
                if (recipe.Get("cash") != null && Money(recipe.Get("cash")).IsPositive)
                {
                    string key = "manufacture:" + recipe.Get("id").Text;
                    MoneyFact(facts, key, Money(recipe.Get("cash"))); MoneyFact(facts, key + ".compiled", Money(recipe.Get("cash")));
                }
            foreach (var tech in Rows(original["tech"].Get("tech")))
            {
                string key = "research:" + tech.Get("id").Text;
                MoneyFact(facts, key, Money(tech.Get("cash"))); Put(facts, key + ".category", MonetaryCostCategory.Manufacturing);
                Put(facts, key + ".action", "oil.start-research"); Put(facts, key + ".reason", "ledger.research");
                Put(facts, key + ".once", true); Put(facts, key + ".bound", true);
            }
            Put(facts, "report.period", GameDuration.Days(1)); Put(facts, "report.offset", GameDuration.Minutes(30));
            return facts;
        }
        private static Dictionary<string, string> CurrentFinancialFacts(Files files)
        {
            var content = ResolveOil(files);
            var facts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var account in content.All<IBusinessAccountDefinition>())
                MoneyFact(facts, "account:" + account.Id.Substring("oil.accounts.".Length), account.OpeningCash);
            foreach (var scenario in Rows(Json.Parse(files.Texts["oil/scenarios.json"]).Get("scenarios")))
                Put(facts, "mode:" + scenario.Get("id").Text, Rows(scenario.Get("modules")).Any(row => row.Text == TycoonModule.ModuleId));
            foreach (var cost in content.All<IEntityOperatingCostDefinition>())
            {
                string condition = cost.Condition == null ? null : ((RunningOperationCostConditionDef)cost.Condition).Operation.Id;
                string key = (cost.Condition == null ? "upkeep:" : "power:") + cost.TargetDefinition.Id;
                MoneyFact(facts, key, cost.Amount); ScheduleFacts(facts, key, cost.Period, cost.Offset, cost.Reason, cost.Category, condition);
            }
            foreach (var sale in content.All<IResourceSaleDefinition>())
            {
                string key = "sale:" + sale.Target.Id;
                foreach (var price in sale.Prices.Lines) MoneyFact(facts, key + ":" + content.ResourceName(price.Resource), price.Amount);
                Put(facts, key + ".period", sale.Period); Put(facts, key + ".offset", sale.Offset);
                Put(facts, key + ".reason", sale.Reason.Id); Put(facts, key + ".eligibility", sale.Eligibility.Id);
            }
            foreach (var entity in content.All<EntityDef>())
                foreach (var supplier in entity.Components.OfType<SupplierDef>())
                    foreach (var price in supplier.Prices.Lines)
                        MoneyFact(facts, "supplier:" + entity.Id + ":" + content.ResourceName(price.Resource), price.Amount);
            foreach (var cost in content.All<IWorkCostRequirement>())
            {
                string key = "cost:" + cost.Id;
                MoneyFact(facts, key, cost.Amount); Put(facts, key + ".credit", cost.Credit); Put(facts, key + ".category", cost.CostCategory);
                var binding = content.All<IActivityRequirementBinding>().Single(row => row.Entries.Any(entry => ReferenceEquals(entry.Requirement, cost)));
                Put(facts, key + ".activity", binding.ActivityId); Put(facts, key + ".node", binding.NodeId);
                Put(facts, key + ".bound", ReferenceEquals(content.Get<IAuthoredActivityDefinition>(binding.ActivityId).Graph.Find(binding.NodeId).Requirement, binding));
            }
            foreach (var binding in content.All<ITransformationCostBinding>())
            {
                string key = "manufacture:" + binding.TransformationId;
                MoneyFact(facts, key, binding.Amount); MoneyFact(facts, key + ".compiled", content.Get<ITransformationDefinition>(binding.TransformationId).Cash);
            }
            foreach (var charge in content.All<IContentActionChargeDefinition>())
            {
                string key = "research:" + charge.Target.Id;
                MoneyFact(facts, key, charge.Amount); Put(facts, key + ".category", charge.Category);
                Put(facts, key + ".action", charge.Action.Id); Put(facts, key + ".reason", charge.Reason.Id); Put(facts, key + ".once", charge.OncePerCompany);
                Put(facts, key + ".bound", content.All<ContentActionActivityBindingDef>().Count(row =>
                    ReferenceEquals(row.Charge, charge) && ReferenceEquals(row.Activity, charge.Target)) == 1);
            }
            var report = content.All<IBusinessSalesReportDefinition>().Single();
            Put(facts, "report.period", report.Period); Put(facts, "report.offset", report.Offset);
            return facts;
        }
        private static Dictionary<string, JsonValue> OriginalDocuments(Check c, params string[] names)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Beep.SimEngine.slnx"))) root = root.Parent;
            if (root == null) throw new InvalidOperationException("framework source archive workspace missing");
            string auditRoot = Path.Combine(root.FullName, "docs/migration");
            var inventory = Json.Parse(File.ReadAllText(Path.Combine(auditRoot, "source-inventory.json")));
            string archivePath = Path.Combine(auditRoot, "source-before-extraction.zip");
            using (var stream = File.OpenRead(archivePath))
                c.Equal(inventory.Get("recoverySha256").Text.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(stream)), "immutable original archive identity");
            var original = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            using (var archive = ZipFile.OpenRead(archivePath))
                foreach (string name in names)
                {
                    string path = "content/oil/" + name + ".json";
                    var entry = archive.GetEntry(path) ?? throw new InvalidOperationException("original document missing: " + path);
                    byte[] bytes; using (var input = entry.Open()) using (var output = new MemoryStream()) { input.CopyTo(output); bytes = output.ToArray(); }
                    var captured = Rows(inventory.Get("files")).Single(row => row.Get("path").Text == path);
                    c.Equal(captured.Get("sha256").Text.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(bytes)), "immutable original " + path);
                    using var reader = new StreamReader(new MemoryStream(bytes)); original.Add(name, Json.Parse(reader.ReadToEnd(), path));
                }
            return original;
        }
        private static void FinancialPreservation(Check c)
        {
            var original = OriginalDocuments(c, "structures", "vehicles", "networks", "work", "production", "tech", "scenarios");
            var authored = AuthoredDocuments();
            var expected = OriginalFinancialFacts(original); var actual = CurrentFinancialFacts(new Files(authored));
            c.That(expected.Keys.OrderBy(key => key, StringComparer.Ordinal).SequenceEqual(actual.Keys.OrderBy(key => key, StringComparer.Ordinal)),
                "complete original/current financial fact sets: missing=" + string.Join(",", expected.Keys.Except(actual.Keys)) + "; extra=" + string.Join(",", actual.Keys.Except(expected.Keys)));
            foreach (var fact in expected)
                c.That(actual.TryGetValue(fact.Key, out var value) && value == fact.Value, fact.Key + ": expected=" + fact.Value + "; actual=" + value);
            var modules = Json.Parse(authored["manifest.json"]).Get("modules");
            foreach (string name in new[] { "accounts", "costs", "transformation-costs", "operating-costs", "research-costs", "sales", "supply-towns" })
            {
                string path = "oil/business-" + name + ".json";
                c.Equal(TycoonModule.ModuleId, Rows(modules).Single(module => Rows(module.Get("documents")).Any(doc => doc.Text == path)).Get("id").Text,
                    "sole optional business document owner: " + path);
            }
            foreach (string mutation in new[] { "opening", "credit", "construction", "manufacturing", "research", "upkeep-period", "sale-price", "supplier-price" })
            {
                var changed = new Files(authored);
                switch (mutation)
                {
                    case "opening": changed.Edit("oil/business-accounts.json", json => json.Get("business_accounts")[0].Set("opening_cash", JsonValue.Number("6000001"))); break;
                    case "credit": changed.Edit("oil/business-costs.json", json => Rows(json.Get("work_cost_requirements")).Single(row => row.Get("id").Text == "oil.activity.repair.repair.cost").Set("credit", JsonValue.Of(false))); break;
                    case "construction": changed.Edit("oil/business-costs.json", json => json.Get("work_cost_requirements")[0].Set("amount", JsonValue.Number("1501"))); break;
                    case "manufacturing": changed.Edit("oil/business-transformation-costs.json", json => json.Get("transformation_cost_bindings")[0].Set("amount", JsonValue.Number("18001"))); break;
                    case "research": changed.Edit("oil/business-research-costs.json", json => json.Get("content_action_charges")[0].Set("once_per_company", JsonValue.Of(false))); break;
                    case "upkeep-period": changed.Edit("oil/business-operating-costs.json", json => json.Get("entity_operating_costs")[0].Get("period").Set("numerator", JsonValue.Number("2"))); break;
                    case "sale-price": changed.Edit("oil/business-sales.json", json => json.Get("resource_sales")[0].Get("prices").Set("crude", JsonValue.Number("61"))); break;
                    default: changed.Edit("oil/business-supply-towns.json", json => json.Get("entities")[0].Get("components").Get("supplier").Get("sells").Set("cement", JsonValue.Number("451"))); break;
                }
                var drift = CurrentFinancialFacts(changed);
                c.That(expected.Count != drift.Count || expected.Any(fact => !drift.TryGetValue(fact.Key, out var value) || value != fact.Value),
                    "audit detects deliberately changed " + mutation);
            }
            Console.WriteLine("  original financial authoring: documents=" + original.Count + ", compared facts=" + expected.Count + ", deliberate drift cases=8");
        }
    }
}
