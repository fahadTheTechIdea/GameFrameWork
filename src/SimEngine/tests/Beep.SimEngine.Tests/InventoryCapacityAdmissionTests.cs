using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Actual file composition, public commands/immutable diagnostics and sole Update only.
    internal static class InventoryCapacityAdmissionTests
    {
        internal const string Case = "Inventory capacity admission: immutable callback vectors exact offers and original physical stock ownership";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool half, out DirectResourceContentModule resources, string fault = "none", string operation = "incoming", bool queryOnly = true)
        {
            resources = new DirectResourceContentModule();
            var data = Json.Parse(FrameworkProofContent.Read("inventory-capacity.json"));
            data.Get("inventory_capacity_policies")[1].Set("fault", JsonValue.Of(fault));
            data.Get("inventory_capacity_policies")[1].Set("query_only", JsonValue.Of(queryOnly));
            return ResourceContentContractTests.Factory(half, resources,
                extraModules: new GameModule[] { new InventoryCapacityProofModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-capacity\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"inventory-capacity.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "inventory-capacity.json", data.ToString() } },
                worldEdit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(InventoryCapacityProofModule.ModuleId));
                    var source = root.Get("entities")[0].Get("components"); source.Remove("point_stock");
                    source.Set("storage", Json.Parse("{\"capacity_policy\":\"inventory.capacity.source\",\"initial_stock\":{\"supply\":20},\"public\":false}"));
                    var destination = root.Get("entities")[1].Get("components"); destination.Remove("point_stock");
                    destination.Set("storage", Json.Parse("{\"capacity_policy\":\"inventory.capacity.destination\",\"initial_stock\":{},\"public\":false}"));
                    var query = Json.Parse("{\"operation\":\"incoming\",\"resource\":\"supply\",\"policy\":\"inventory.capacity.destination\",\"quantity\":8}");
                    query.Set("operation", JsonValue.Of(operation)); destination.Set("inventory_capacity_query", query);
                });
        }
        private static SimGame New(GameFactory f) => ConstructionPlanTests.NewPoints(f);
        private static Fix Stock(SimGame g, string def)
        {
            var rows = new List<Facet>(); g.Seat(0).View.Inspect(Entity(g, def), rows);
            return rows.Where(x => x.Key == Storage.FacetStock && x.Subject == "supply").Aggregate(Fix.Zero, (a, x) => a + x.Value);
        }
        private static void Ready(Check c, SimGame g)
        {
            if (!g.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("actual native quantity missing");
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = Entity(g, "machine"), Resource = resource, Ceiling = Fix.FromInt(8), Priority = 100 });
            for (int i = 0; i < 512 && (Stock(g, "destination") != Fix.FromInt(8) || !Stock(g, "machine").IsZero); i++) Step(g);
            if (Stock(g, "destination") != Fix.FromInt(8) || !Stock(g, "machine").IsZero)
                throw new InvalidOperationException("actual delivery exceeds512Updates time=" + g.Clock.StepIndex + "/jobs=" +
                    string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            c.Equal(Fix.FromInt(12), Stock(g, "source"), "eight original units earned actual physical delivery");
        }
        private static void Negative(Check c, bool half, string fault, string operation)
        {
            var f = Factory(half, out _, fault, operation); var g = New(f); var idle = New(Factory(half, out _)); ulong isolated = idle.Hash();
            Ready(c, g); Fix held = Stock(g, "destination"); var before = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot;
            EntityId target = Entity(g, "destination"); g.Clock.Pause();
            c.Throws<Exception>(() => g.Seat(0).View.Inspect(target, new List<Facet>()), "illegal capacity result/input refuses original endpoint query " + fault + "/" + operation);
            var first = g.Fault; c.That(first != null, "first illegal inventory callback fault retained");
            if (first == null) throw new InvalidOperationException("missing inventory capacity fault " + fault + "/" + operation);
            if (fault.StartsWith("throw-", StringComparison.Ordinal)) c.That(first.Message.Contains("proof.original-capacity-"), "original callback error preserved");
            else if (fault.StartsWith("clock-", StringComparison.Ordinal)) c.That(first.Message.Contains("game-clock"), "sole clock mutation detected");
            else if (fault == "unknown-check") c.That(first.Message.Contains("invalid check status"), "invalid check result diagnosed");
            else if (fault == "zero-quantum") c.That(first.Message.Contains("native quantum"), "invalid exact quantum diagnosed");
            else if (!fault.StartsWith("mutate-", StringComparison.Ordinal)) c.That(first.Message.Contains("bounded native offer"), "invalid capacity offer diagnosed");
            long stopped = g.Clock.StepIndex; g.Update(); var after = g.Seat(0).Snapshot;
            c.Equal(stopped, g.Clock.StepIndex, "faulted inventory admission cannot advance clock");
            c.Equal(before.Tick, after.Tick, "last healthy time retained"); c.Equal(GameState.Faulted, after.State, "faulted publication explicit");
            c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "per-viewer healthy geometry retained");
            c.Equal(0, after.Changed.Count + after.Spawned.Count + after.Removed.Count, "no invalid stock/result prefix published");
            c.Equal(Fix.FromInt(8), held, "finite delivered amount retained before illegal callback");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted inventory cannot hash healthy");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted inventory cannot save healthy");
            c.That(ReferenceEquals(first, g.Fault), "no later replacement of first error"); c.Equal(isolated, idle.Hash(), "separate healthy game unchanged");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared capacity contract build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same stock and reservation codecs");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true }) {
                var f = Factory(half, out var resources); ResourceContentContractTests.Healthy(c, half, cancel, f, resources);
            }
            var cases = new[] {
                ("over-offer", "offer"), ("fractional-offer", "offer"), ("refusal-offer", "offer"),
                ("negative-offer", "offer"), ("unknown-offer", "offer"), ("inconsistent-offer", "offer"),
                ("zero-quantum", "conversion"), ("unknown-check", "incoming"), ("unknown-check", "outgoing"), ("unknown-check", "conversion"),
                ("throw-check", "incoming"), ("clock-check", "incoming"), ("mutate-stock-check", "incoming"), ("mutate-projected-check", "conversion"),
                ("mutate-incoming-check", "incoming"), ("mutate-incoming-check", "outgoing"), ("mutate-incoming-check", "conversion"),
                ("clock-offer", "offer"), ("throw-offer", "offer")
            };
            foreach (var item in cases) foreach (bool half in new[] { false, true }) Negative(c, half, item.Item1, item.Item2);
        }
    }
}
