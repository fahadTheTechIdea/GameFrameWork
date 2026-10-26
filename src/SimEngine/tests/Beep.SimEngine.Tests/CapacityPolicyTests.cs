using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    public static class CapacityPolicyTests
    {
        private static readonly ResourceAmount[] Empty = Array.Empty<ResourceAmount>();
        public static void Register()
        {
            TestRunner.Add("Capacity policies: exact combined measures, promises and typed refusals", Measures);
            TestRunner.Add("Capacity policies: authored extension ownership, views and saved sessions", Content);
        }
        internal static GameFactory Factory(ResourceProofModule module, string policies = null, string manifest = null, string binding = null, string workflow = null)
        {
            var definition = new GameDefinition("capacity-proof", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new WorldSetupModule(),
                module, new CapacityProofModule() });
            var files = new PreloadedFileSource().Add("manifest.json", manifest ?? FrameworkProofContent.Read("capacity-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("units.json", FrameworkProofContent.Read("resource-units.json"))
                .Add("ore.json", FrameworkProofContent.Read("resource-ore.json"))
                .Add("workflow.json", (workflow ?? FrameworkProofContent.Read("resource-workflow.json")).Replace(
                    "\"modules\": [\"proof.resources\",\"sim.world-setup\"]", "\"modules\": [\"proof.resources\", \"sim.world-setup\", \"proof.capacity\"]"))
                .Add("capacity.json", policies ?? FrameworkProofContent.Read("capacity.json"))
                .Add("capacity-binding.json", binding ?? FrameworkProofContent.Read("capacity-binding.json"))
                .Add("unlisted.json", "{\"capacity_policies\":[{\"id\":\"not.loaded\",\"limits\":{}}]}");
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "resource-work", Seed = 47 });
        private static ResourceAmount[] Amount(ResolvedContent content, string id, Fix amount)
            => new[] { new ResourceAmount(content.ResourceId(id), amount) };
        private static CapacityUsage Usage(CapacityCheck check, string unit)
        {
            foreach (CapacityUsage usage in check.Usage) if (usage.Unit == new UnitId(unit)) return usage;
            throw new InvalidOperationException("missing calculated usage " + unit + " in " + check.Status);
        }

        private static void Measures(Check c)
        {
            SimGame game = New(Factory(new ResourceProofModule())); ResolvedContent content = game.World.Content;
            ICapacityPolicy combined = content.Get<CapacityPolicyDef>("cargo.combined");
            var stock = new List<ResourceAmount>(Amount(content, "planetx.ore", Fix.FromInt(2)));
            var promised = new List<ResourceAmount>(Amount(content, "crate", Fix.One));
            var incoming = new List<ResourceAmount>(Amount(content, "measured.coolant", Fix.FromInt(2)));
            ulong before = game.Hash(); CapacityCheck check = combined.Check(stock, promised, incoming);
            c.Equal(CapacityStatus.Fits, check.Status, "mass and volume both fit at exact volume boundary");
            c.That(check.Fits && !default(CapacityCheck).Fits, "only explicit Fits admits, not a default result");
            c.Equal(2, check.Usage.Count, "independent authored limits produce a vector, not one scalar total");
            CapacityUsage mass = Usage(check, "unit.kg"), volume = Usage(check, "unit.litre");
            c.Equal(Fix.FromInt(2), mass.Stock, "ore native mass is measured as authored");
            c.Equal(Fix.FromInt(2), mass.Promised, "count-unit crate promises occupy authored mass");
            c.Equal(Fix.One, mass.Incoming, "coolant uses explicit mass measure, not native litre amount");
            c.Equal(Fix.FromInt(5), mass.Projected, "each vector contribution sums in one dimension only");
            c.Equal(Fix.FromInt(4), volume.Projected, "volume independently uses ore, crate and coolant measures");
            c.Equal(new UnitId("unit.kg"), check.Usage[0].Unit, "facts use stable unit ID order rather than JSON key order");
            c.Equal(before, game.Hash(), "capacity evaluation does not mutate session state");
            c.Equal(Fix.FromInt(2), stock[0].Amount, "stock is not changed or cached as a second balance");
            c.Equal(Fix.One, promised[0].Amount, "promises are not acquired/released by a check");
            c.Throws<NotSupportedException>(() => ((IList<CapacityUsage>)check.Usage).Clear(), "result facts are immutable");
            c.Throws<NotSupportedException>(() => ((IList<CapacityLimit>)combined.Limits).Clear(), "shared limits are immutable");

            incoming[0] = new ResourceAmount(content.ResourceId("measured.coolant"), Fix.FromInt(2) + Fix.Half);
            CapacityCheck full = combined.Check(stock, promised, incoming);
            c.Equal(CapacityStatus.LimitExceeded, full.Status, "volume refuses even with sufficient mass space");
            c.Equal(new UnitId("unit.litre"), full.Unit, "exceeded dimension is typed by authored unit");
            c.Equal(Fix.FromInt(4) + Fix.Half, Usage(full, "unit.litre").Projected, "refusal reports full projected amount without clamping");
            c.Equal(Fix.FromInt(4), Usage(check, "unit.litre").Projected, "retained result does not follow later input mutation");
            c.That(content.Get<CapacityPolicyDef>("cargo.mass").Check(stock, promised, incoming).Fits,
                "mass-only policy does not silently impose volume or phase restrictions");
            c.That(!combined.Check(stock, promised, Amount(content, "planetx.ore", Fix.FromInt(7))).Fits,
                "mass refuses even when volume is independently calculable");
            c.Equal(CapacityStatus.LimitExceeded, combined.Check(stock, Amount(content, "crate", Fix.FromInt(5)), Empty).Status,
                "already promised capacity counts even when no incoming delivery is proposed");

            var duplicate = new[] { new ResourceAmount(content.ResourceId("planetx.ore"), Fix.One),
                new ResourceAmount(content.ResourceId("planetx.ore"), Fix.One) };
            CapacityCheck duplicated = combined.Check(duplicate, promised, Empty);
            c.Equal(Fix.FromInt(4), Usage(duplicated, "unit.kg").Projected, "duplicate resource entries are additive, never last-entry wins");
            var reordered = new[] { promised[0], stock[0] };
            CapacityCheck reorderedCheck = combined.Check(reordered, Empty, Empty);
            c.Equal(Usage(duplicated, "unit.kg").Projected, Usage(reorderedCheck, "unit.kg").Projected,
                "valid vector grouping/order does not alter calculated occupancy");
            ICapacityPolicy grams = content.Get<CapacityPolicyDef>("cargo.grams");
            c.Equal(Fix.FromInt(500), Usage(grams.Check(Amount(content, "planetx.ore", Fix.Half), Empty, Empty), "unit.g").Stock,
                "compatible measure units convert exactly through rational scale");
            c.Equal(CapacityStatus.OutOfRange, grams.Check(Amount(content, "planetx.ore", Fix.FromInt(5000000)), Empty, Empty).Status,
                "conversion overflow refuses before narrowing or comparison");
            c.Equal(CapacityStatus.Unrepresentable, content.Get<CapacityPolicyDef>("cargo.third").Check(
                Amount(content, "planetx.ore", Fix.One), Empty, Empty).Status, "unrepresentable rational is not rounded into free space");
            c.Equal(CapacityStatus.Unrepresentable, combined.Check(Amount(content, "measured.coolant", Fix.FromRaw(1)), Empty, Empty).Status,
                "sub-precision capacity product refuses instead of occupying zero mass");
            c.Equal(Fix.FromRaw(1), Usage(content.Get<CapacityPolicyDef>("cargo.mass").Check(
                Amount(content, "scaled.dust", Fix.FromRaw(1)), Empty, Empty), "unit.kg").Stock,
                "exact product and unit conversion combine without an intermediate rounding step");

            ICapacityPolicy slots = content.Get<CapacityPolicyDef>("cargo.slots");
            c.That(slots.Check(Empty, Empty, Amount(content, "crate", Fix.FromInt(3))).Fits, "three authored crate slots fit exactly");
            c.Equal(CapacityStatus.LimitExceeded, slots.Check(Empty, Empty, Amount(content, "crate", Fix.FromInt(4))).Status,
                "slot limit is an independent declared count dimension");
            c.Equal(CapacityStatus.FractionalDiscrete, slots.Check(Empty, Empty, Amount(content, "crate", Fix.Half)).Status,
                "fractional native count refuses even in a bulk mass policy");
            c.Equal(CapacityStatus.FractionalDiscrete, content.Get<CapacityPolicyDef>("cargo.mass").Check(
                Empty, Empty, Amount(content, "crate", Fix.Half)).Status, "native discreteness is independent of capacity unit");
            c.Equal(CapacityStatus.FractionalDiscrete, content.Get<CapacityPolicyDef>("cargo.pairs").Check(
                Empty, Empty, Amount(content, "crate", Fix.One)).Status, "fractional destination count measure refuses");
            c.That(content.Get<CapacityPolicyDef>("cargo.pairs").Check(Empty, Empty, Amount(content, "crate", Fix.FromInt(2))).Fits,
                "compatible discrete scale permits exact whole destination units");
            c.Equal(CapacityStatus.FractionalDiscrete, slots.Check(Empty, Empty, Amount(content, "fractional.slot", Fix.One)).Status,
                "source capacity measure discreteness also refuses fractional measured units");
            c.Equal(CapacityStatus.MissingMeasure, combined.Check(Empty, Empty, Amount(content, "kit", Fix.One)).Status,
                "native count never becomes implicit mass or volume");
            CapacityCheck missing = combined.Check(Empty, Empty, Amount(content, "coolant", Fix.One));
            c.Equal(CapacityStatus.MissingMeasure, missing.Status, "missing authored density refuses rather than inferred conversion");
            c.Equal(content.ResourceId("coolant"), missing.Resource, "missing measure diagnostic names failing resource");
            c.Equal(new UnitId("unit.kg"), missing.Unit, "missing measure diagnostic names required unit dimension");
            c.Equal(CapacityStatus.InvalidAmount, combined.Check(Empty, Amount(content, "crate", -Fix.One), Empty).Status,
                "negative promised amount cannot manufacture space");
            c.Equal(CapacityStatus.UnknownResource, combined.Check(Empty, Empty, new[] { new ResourceAmount(ResourceId.None, Fix.Zero) }).Status,
                "unknown zero resource is not silently ignored");
            c.Equal(CapacityStatus.UnknownResource, combined.Check(Empty, Empty, new[] { new ResourceAmount(new ResourceId(99999), Fix.One) }).Status,
                "unknown compact resource index refuses explicitly");
            c.Equal(CapacityStatus.MissingMeasure, combined.Check(Empty, Empty, Amount(content, "kit", Fix.Zero)).Status,
                "zero quantity still validates resource compatibility with every declared dimension");
            c.Equal(CapacityStatus.InvalidAmount, combined.Check(Amount(content, "planetx.ore", Fix.FromInt(100)), Empty,
                Amount(content, "crate", -Fix.One)).Status, "malformed incoming vector is not hidden by already full stock");
            c.Equal(CapacityStatus.FractionalDiscrete, slots.Check(Empty, Amount(content, "crate", Fix.Half), Empty).Status,
                "promised quantities use the same validation as stock and incoming");
            c.Equal(CapacityStatus.OutOfRange, content.Get<CapacityPolicyDef>("cargo.mass").Check(
                Amount(content, "planetx.ore", Fix.FromRaw(long.MaxValue)), Empty, Amount(content, "planetx.ore", Fix.FromRaw(1))).Status,
                "sum across stock/promised/incoming refuses fixed-point overflow");
            c.Equal(CapacityStatus.OutOfRange, content.Get<CapacityPolicyDef>("cargo.mass").Check(new[] {
                new ResourceAmount(content.ResourceId("planetx.ore"), Fix.FromRaw(long.MaxValue)),
                new ResourceAmount(content.ResourceId("planetx.ore"), Fix.One) }, Empty, Empty).Status,
                "duplicate quantities cannot overflow a single occupancy lane");
            c.That(content.Get<CapacityPolicyDef>("cargo.closed").Check(Empty, Empty, Empty).Fits, "authored zero capacity admits an empty intention");
            c.Equal(CapacityStatus.LimitExceeded, content.Get<CapacityPolicyDef>("cargo.closed").Check(
                Empty, Empty, Amount(content, "planetx.ore", Fix.FromRaw(1))).Status, "authored zero capacity closes positive admission");
            c.That(content.Get<CapacityPolicyDef>("cargo.huge").Check(Amount(content, "planetx.ore", Fix.FromInt(1500000)), Empty, Empty).Fits,
                "capacity has no arbitrary platform-sized ceiling below the numeric representation");
            c.Throws<ArgumentNullException>(() => combined.Check(null, Empty, Empty), "null stock is not an implicit empty buffer");
            c.Throws<ArgumentNullException>(() => combined.Check(Empty, null, Empty), "null promises are not fabricated");
            c.Throws<ArgumentNullException>(() => combined.Check(Empty, Empty, null), "null incoming vector is not fabricated");

            var copiedFacts = new List<CapacityUsage> { mass };
            var detached = new CapacityCheck(CapacityStatus.Fits, usage: copiedFacts); copiedFacts.Clear();
            c.Equal(1, detached.Usage.Count, "public extension result constructor captures facts rather than retaining a mutable list");
            c.Equal(before, game.Hash(), "all admitted/refused checks leave existing game state and promises unchanged");
        }

        private static string Refused(Check c, string label, string policies, string manifest = null, string binding = null)
        {
            var module = new ResourceProofModule(); string message = "";
            try { New(Factory(module, policies, manifest, binding)); c.That(false, label); }
            catch (ContentException error) { message = error.Message; c.That(true, label); }
            c.Equal(0, module.Initializations, label + " invokes no initialization callback");
            return message;
        }

        private static void Content(Check c)
        {
            var module = new ResourceProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            ResolvedContent content = game.World.Content; string json = FrameworkProofContent.Read("capacity.json");
            c.Equal(1, module.Initializations, "ordinary capacity content initializes the same selected game once");
            c.Equal(46, content.Count<CapacityPolicyDef>(), "base policy table aliases built-in and external derived providers");
            ICapacityPolicy minerals = content.Get<CapacityBindingDef>("mine.cargo").Policy;
            c.That(ReferenceEquals(minerals, content.Get<TraitCapacityPolicyDef>("cargo.minerals")) &&
                ReferenceEquals(minerals, content.Get<CapacityPolicyDef>("cargo.minerals")), "consumer, base alias and derived kind share one policy object");
            c.Equal("proof.capacity", content.Get<CapacityPolicyDef>("cargo.minerals").Module, "policy retains normal content ownership");
            c.Equal("space.mineral", content.Get<TraitCapacityPolicyDef>("cargo.minerals").RequiredTrait, "extension reads its authored properties through the shared strict parser");
            c.That(minerals.Check(Empty, Empty, Amount(content, "planetx.ore", Fix.FromInt(8))).Fits,
                "external policy admits authored mineral via the common measured evaluator");
            c.Equal(CapacityStatus.ResourceRejected, minerals.Check(Empty, Empty, Amount(content, "crate", Fix.One)).Status,
                "external contract policy rejects resources without its authored trait");
            c.That(typeof(CapacityPolicyDef).GetMethod("Read").IsFinal && typeof(CapacityPolicyDef).GetMethod("Resolve").IsFinal,
                "derived authoring cannot bypass common limit validation");
            c.Throws<InvalidOperationException>(() => content.Get<CapacityPolicyDef>("cargo.mass").Read(new JsonReader(Json.Parse("{}"), "late policy")),
                "shared policy cannot be reauthored after load");
            c.Throws<InvalidOperationException>(() => content.Get<CapacityPolicyDef>("cargo.mass").Resolve(
                new ContentResolver(content, content.Get<CapacityPolicyDef>("cargo.mass"))), "shared policy cannot resolve again");
            var views = new List<CapacityPolicyInfo>(); ulong before = game.Hash(); game.Observer.CopyCapacityPolicies(views);
            c.Equal(46, views.Count, "host metadata uses the same selected policy definitions");
            c.Equal("cargo.closed", views[0].Id, "host metadata uses stable policy ID order");
            CapacityPolicyInfo retained = views.Find(v => v.Id == "cargo.combined");
            c.Equal(Fix.FromInt(10), retained.Limits[0].Amount, "host reads authored mass limit as detached facts");
            c.Equal("mass", retained.Limits[0].Unit.Dimension, "host receives typed dimension, not an unlabelled number");
            c.Throws<NotSupportedException>(() => ((IList<CapacityLimitInfo>)retained.Limits).Clear(), "host policy facts cannot mutate shared content");
            c.That(typeof(CapacityPolicyInfo).GetProperty("Policy") == null, "host projection exposes no executable provider");
            c.That(content.Find<CapacityPolicyDef>("not.loaded") == null, "unlisted policy file cannot leak into selected content");
            c.Equal(before, game.Hash(), "metadata and specialized evaluation mutate no world state");
            var entities = new List<EntityView>(); game.Observer.CopyEntities(entities);
            IInventory existing = game.World.Entities.Get(entities[0].Id).Get<IInventory>();
            c.Equal(Fix.FromInt(3), Usage(minerals.Check(existing.Stock, Empty, Empty), "unit.kg").Stock,
                "same policy evaluates existing live provider stock without another inventory ledger");
            var command = new Beep.SimEngine.Commands.PostOrderCommand { Plan = "refine", Target = entities[0].Id };
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary timed resource work still executes under selected policy content");
            game.RunTicks(2); byte[] save = game.Save(); SimGame twin = factory.Load(save);
            c.Equal(game.Hash(), twin.Hash(), "selected capacity content and the existing work/inventory state save together");
            game.RunTicks(4); twin.RunTicks(4);
            c.Equal(game.Hash(), twin.Hash(), "saved sessions continue identically with no policy-local mutable balance");
            c.Equal(Fix.FromInt(3) - Fix.Half, Usage(minerals.Check(existing.Stock, Empty, Empty), "unit.kg").Stock,
                "policy sees authoritative consumption instead of stale cached balances");
            c.Equal(Fix.FromInt(10), retained.Limits[0].Amount, "retained host metadata stays immutable during updates/load");
            c.That(factory.Describe(save).Compatible, "save identity includes the master-listed policy document");
            GameFactory changed = Factory(new ResourceProofModule(), json.Replace("\"unit.kg\": 10", "\"unit.kg\": 11"));
            c.That(!changed.Describe(save).Compatible, "changed authored capacity policy invalidates selected save identity");
            c.Throws<CorruptStateException>(() => changed.Load(save), "changed policy content is not loaded through a compatibility path");

            string message = Refused(c, "unknown policy unit refuses", json.Replace("\"unit.litre\": 4, \"unit.kg\": 10", "\"unit.litre\": 4, \"unit.missing\": 10"));
            c.That(message.Contains("proof.capacity@1.0.0 capacity.json:capacity_policies[0](cargo.combined)") &&
                message.Contains(".limits['unit.missing']"), "cross-reference refusal contains exact owner/file/definition/property provenance");
            Refused(c, "empty authored limits refuse", json.Replace("\"unit.litre\": 4, \"unit.kg\": 10", ""));
            Refused(c, "negative authored limit refuses", json.Replace("\"unit.kg\": 10", "\"unit.kg\": -1"));
            Refused(c, "fractional authored count limit refuses", json.Replace("\"unit.slot\": 3", "\"unit.slot\": 0.5"));
            Refused(c, "ambiguous limit dimensions refuse", json.Replace("\"unit.litre\": 4, \"unit.kg\": 10", "\"unit.g\": 4, \"unit.kg\": 10"));
            Refused(c, "unknown policy key refuses", json.Replace("\"id\": \"cargo.mass\"", "\"id\": \"cargo.mass\", \"fallback\": 100"));
            Refused(c, "missing policy limits refuse", json.Replace("\"limits\": { \"unit.kg\": 0 }", "\"capacity\": 0"));
            Refused(c, "missing derived property refuses", json.Replace(", \"required_trait\": \"space.mineral\"", ""));
            Refused(c, "missing policy reference refuses", json, binding: FrameworkProofContent.Read("capacity-binding.json")
                .Replace("\"policy\": \"cargo.minerals\"", "\"policy\": \"cargo.missing\""));
            Refused(c, "base/derived policy collision refuses", json.Replace("\"id\": \"cargo.minerals\"", "\"id\": \"cargo.mass\""));
            Refused(c, "missing master-listed policy document is not supplied", json,
                FrameworkProofContent.Read("capacity-manifest.json").Replace("\"documents\": [\"capacity.json\", \"capacity-binding.json\"]", "\"documents\": [\"capacity-binding.json\"]"));
        }
    }
}
