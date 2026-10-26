using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored inputs and public facts only; byte corruption is protocol fixture data, not game state.
    internal static class CanonicalResourceCatalogTests
    {
        internal const string Case = "Canonical resources: stable contract aliases wide physical stock and strict authored saves";
        internal static void Register() => TestRunner.Add(Case, Run);
        private const int NativeKinds = 255, KindCount = NativeKinds + 2;
        private static void Wide(JsonValue root)
        {
            var initial = JsonValue.NewObject();
            for (int i = 0; i < NativeKinds; i++)
            {
                string id = "native.resource." + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                var resource = Json.Parse("{\"id\":\"placeholder\",\"unit\":\"unit.supply\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.supply\":1}}");
                resource.Set("id", JsonValue.Of(id)); root.Get("resources").Add(resource); initial.Set(id, JsonValue.Number("1"));
            }
            initial.Set("supply", JsonValue.Number("1")); initial.Set("direct.unused", JsonValue.Number("2"));
            root.Get("capacity_policies").Add(Json.Parse("{\"id\":\"fixture.catalog.300\",\"limits\":{\"unit.supply\":300}}"));
            var actor = Json.Parse("{\"id\":\"bulk-stock\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":40},\"storage\":{\"capacity_policy\":\"fixture.catalog.300\",\"public\":false}}}");
            actor.Get("components").Get("storage").Set("initial_stock", initial); root.Get("entities").Add(actor);
            root.Get("entity_setups").Add(Json.Parse("{\"id\":\"bulk-stock\",\"after\":[],\"entity\":\"bulk-stock\",\"company\":0,\"placement\":{\"space\":\"field.points\",\"position\":40},\"home\":null}"));
            root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("bulk-stock"));
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 7219, Seats = { Controller.Human, Controller.Human } });
        private static void Stock(Check c, SimGame g, EntityId entity)
        {
            var facets = new List<Facet>(); ulong hash = g.Hash();
            c.That(g.Seat(0).View.Inspect(entity, facets), "actual native wide stock entity exists");
            var stock = facets.Where(x => x.Key == Storage.FacetStock).ToArray();
            c.Equal(KindCount, stock.Length, "one actual inventory holds all257 resource kinds without item-count cap");
            c.Equal(KindCount, stock.Select(x => x.Subject).Distinct().Count(), "one row per stable identity, no copied balance");
            c.That(stock.All(x => x.Value == (x.Subject == "direct.unused" ? Fix.FromInt(2) : Fix.One)), "original authored quantities are granted once");
            c.That(g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(entity, Code.None, out var capacity) && capacity.Fits,
                "actual measured capacity, not resource-count fallback, admits wide stock");
            c.Equal(Fix.FromInt(KindCount), capacity.Usage.Single().Stock, "capacity uses all exact authored coefficients once");
            g.Seat(1).View.Inspect(entity, facets);
            c.That(!facets.Any(x => x.Key == Storage.FacetStock), "foreign inspector does not disclose wide stock");
            c.That(!g.Seat(1).View.Feature<InventoryView>().TryGetCapacity(entity, Code.None, out _), "foreign capacity facts remain private");
            c.Equal(hash, g.Hash(), "stock/capacity/private queries create no secondary state");
        }
        private static void Healthy(Check c, bool half, bool cancel)
        {
            var module = new DirectResourceContentModule();
            var f = ResourceContentContractTests.Factory(half, module, worldEdit: Wide);
            var idle = New(f); ulong untouched = idle.Hash(); var g = New(f);
            c.That(module.ActualAliases && module.StableProjection && module.CatalogCount == KindCount,
                "native and independent definitions share one stable catalog projection and original identity aliases");
            var metadata = new List<ResourceInfo>(); g.Seat(0).View.CopyResources(metadata);
            c.Equal(KindCount, metadata.Count, "public detached catalog contains every actual kind exactly once");
            c.Equal(KindCount, metadata.Select(x => x.Resource).Distinct().Count(), "no compact-ID collisions");
            c.That(metadata.Single(x => x.Id == "supply").Resource.Value > byte.MaxValue,
                "actual high-index resource crosses former byte-width boundaries");
            var retained = metadata.ToArray(); var stock = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "bulk-stock").Id;
            Stock(c, g, stock);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "wide original stock/resource identities survive save without regrant");
            Stock(c, twin, stock); g.Update(); twin.Update(); c.Equal(g.Hash(), twin.Hash(), "wide saved twins continue through ordinary Update");
            Stock(c, g, stock); c.Equal(KindCount, retained.Length, "retained catalog facts remain detached");
            c.That(g.Seat(0).View.Feature<BusinessView>() == null, "resource-only physical game does not compose business");
            c.Equal(untouched, idle.Hash(), "catalog and wide stock do not mix interleaved sessions");
            LegacySaves(c, f, g);
            ResourceContentContractTests.Healthy(c, half, cancel, f, module, KindCount);
        }
        private static void Seal(byte[] data)
        {
            ulong hash = 14695981039346656037UL;
            unchecked { for (int i = 0; i < data.Length - 8; i++) { hash ^= data[i]; hash *= 1099511628211UL; } }
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(data.Length - 8), hash);
        }
        private static void LegacySaves(Check c, GameFactory f, SimGame g)
        {
            var current = g.Save(); ulong hash = g.Hash();
            var legacyFormat = (byte[])current.Clone(); BinaryPrimitives.WriteInt32LittleEndian(legacyFormat.AsSpan(8, 4), EngineInfo.SaveFormat - 1); Seal(legacyFormat);
            var legacyBuild = (byte[])current.Clone();
            int bytes = BinaryPrimitives.ReadInt32LittleEndian(legacyBuild.AsSpan(12, 4)); legacyBuild[16 + bytes - 1] = (byte)'9'; Seal(legacyBuild);
            foreach (var old in new[] { legacyFormat, legacyBuild })
            {
                c.Throws<CorruptStateException>(() => f.Load(old), "checksum-valid old protocol refuses rather than migrate balances");
                c.Throws<CorruptStateException>(() => f.Describe(old), "load menu accepts no legacy schema substitute");
            }
            c.Equal(hash, g.Hash(), "legacy byte refusals do not alter current session");
            c.Equal(hash, f.Load(current).Hash(), "actual current save remains valid after legacy refusals");
        }
        private static void Refusals(Check c)
        {
            foreach (string old in new[] { "capacity", "capacity_volume", "max_resources", "max_items" })
            {
                var module = new DirectResourceContentModule();
                c.Throws<ContentException>(() => {
                    var f = ResourceContentContractTests.Factory(false, module, worldEdit: root =>
                        root.Get("entities")[2].Get("components").Get("storage").Set(old, JsonValue.Number("8")));
                    New(f);
                }, "obsolete implicit storage field has no compatibility reader: " + old);
                c.That(!module.Composed, "obsolete storage refuses before Compose");
            }
            foreach (string old in new[] { "volume", "weight", "worth", "resource_type" })
            {
                var module = new DirectResourceContentModule();
                c.Throws<ContentException>(() => {
                    var f = ResourceContentContractTests.Factory(false, module, edit: root =>
                        root.Get("contract_resources")[1].Set(old, JsonValue.Number("1"))); New(f);
                }, "obsolete resource field has no compatibility reader: " + old);
                c.That(!module.Composed, "obsolete resource metadata refuses before Compose");
            }
            var duplicate = new DirectResourceContentModule();
            c.Throws<ContentException>(() => New(ResourceContentContractTests.Factory(false, duplicate, duplicate: true)),
                "native/independent duplicate identity is refused instead of second catalog");
            c.That(!duplicate.Composed, "cross-kind duplicate refuses before Compose");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual canonical catalog build");
            c.That(typeof(ResourceId).GetField("Value").FieldType == typeof(int), "resource identity is not narrowed to byte or fixed enum");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true }) Healthy(c, half, cancel);
            Refusals(c);
        }
    }
}
