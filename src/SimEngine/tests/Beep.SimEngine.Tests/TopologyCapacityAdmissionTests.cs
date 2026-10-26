using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Ordinary players: same shared physical cut/reconnect driver, independently authored capacity.
    internal static partial class TopologyCapacityAdmissionTests
    {
        internal const string Case = "Topology capacity admission: immutable inputs bounded native offers and callback first-fault cutoff";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(RefusalCase, Refusals); }
        private static GameFactory Factory(bool point, bool half, string fault = "none", int? pointLimit = null)
        {
            string file = point ? "topology-capacity.json" : "topology-capacity-tile.json";
            var data = Json.Parse(FrameworkProofContent.Read(file));
            for (int i = 0; i < data.Get("topology_capacity_policies").Count; i++)
                data.Get("topology_capacity_policies")[i].Set("fault", JsonValue.Of(fault));
            if (pointLimit.HasValue) {
                if (!point) throw new ArgumentException("point capacity variant requires the point fixture");
                data.Get("topology_capacity_policies")[0].Get("limits").Set("unit.capacity.count", JsonValue.Number(pointLimit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            var docs = new Dictionary<string, string> { { file, data.ToString() } };
            var modules = new GameModule[] { new TopologyCapacityProofModule() };
            string declarations = "[{\"id\":\"proof.topology-capacity\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"" + file + "\"]}]";
            if (!point) return PhysicalLineRemovalTests.Create(false,
                editDefinitions: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); },
                editNetwork: root => {
                    root.Get("networks")[0].Set("capacity_policy", JsonValue.Of("topology.capacity.tile"));
                    var band = root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                    band.Set("minimum", JsonValue.Number("26")); band.Set("maximum", JsonValue.Number("30"));
                }, extraModules: modules, extraDeclarations: declarations, extraDocuments: docs);
            return SpatialNetworkGraphTests.Points(half, new PointLineGeometryProofModule(),
                extraAuthor: root => root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TopologyCapacityProofModule.ModuleId)),
                editCorridors: root => { root.Get("lines")[0].Set("owned", JsonValue.Of(true)); root.Get("lines")[0].Set("owner_layer", JsonValue.Of("corridor.company")); },
                moreDocuments: new Dictionary<string, string> { { "point-owned-network.json", FrameworkProofContent.Read("point-owned-network.json") } },
                editNetwork: root => { root.Get("networks")[0].Set("public", JsonValue.Of(false)); root.Get("networks")[0].Set("capacity_policy", JsonValue.Of("topology.capacity.point")); },
                afterPorts: root => {
                    root.Get("entities")[1].Get("components").Get("ports").Get("list")[0].Set("pull", JsonValue.NewArray());
                    root.Get("entities")[0].Get("components").Set("network_resource_port", Json.Parse("{\"id\":\"graph\",\"network\":\"point.network\",\"minimum_members\":1}"));
                }, extraModules: modules, extraDeclarations: declarations, moduleDocuments: docs);
        }
        private static void Negative(Check c, string fault, bool half, int? pointLimit = null)
        {
            var f = Factory(true, half, fault, pointLimit); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(Factory(true, half));
            ulong isolated = idle.Hash(); TopologyErasePostconditionTests.Ready(c, f, g, true, checkpoint: false);
            var retained = TopologyErasePostconditionTests.Pools(g, true).Single(); Fix stock = retained.Stock.Single().Amount;
            var before = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot;
            g.Clock.Pause();
            var path = new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) };
            c.Throws<Exception>(() => g.Seat(0).View.Feature<InfrastructureView>().PreviewRemoval("corridor.point-line", path),
                "malformed topology capacity cannot admit mutation " + fault);
            var first = g.Fault; c.That(first != null, "callback refusal faults authoritative admission " + fault);
            if (first == null) throw new InvalidOperationException("missing capacity fault " + fault);
            if (fault.StartsWith("throw-", StringComparison.Ordinal))
                c.That(first.Message.Contains("proof.original-capacity-"), "original callback exception retained");
            else if (fault.StartsWith("clock-", StringComparison.Ordinal))
                c.That(first.Message.Contains("game-clock"), "sole clock mutation detected");
            else if (fault.EndsWith("-quantum", StringComparison.Ordinal)) c.That(first.Message.Contains("native quantum"), "invalid quantum rejected before topology");
            else if (fault != "mutate-offer") c.That(first.Message.Contains("bounded native offer"), "malformed offer diagnosed explicitly");
            long stopped = g.Clock.StepIndex; g.Update(); var after = g.Seat(0).Snapshot;
            c.Equal(stopped, g.Clock.StepIndex, "faulted admission cannot advance game time");
            c.Equal(GameState.Faulted, after.State, "faulted callback publication explicit");
            c.Equal(before.Tick, after.Tick, "last healthy time retained");
            c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "both viewers retain healthy physical topology");
            c.Equal(0, after.Changed.Count + after.Spawned.Count + after.Removed.Count, "no invalid topology delta prefix");
            c.Equal(3, retained.MemberCount, "retained actual graph membership untouched");
            c.Equal(stock, retained.Stock.Single().Amount, "original detached stock not mutated");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted admission cannot save healthy");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted admission cannot hash healthy");
            c.Throws<InvalidOperationException>(() => TopologyErasePostconditionTests.Pools(g, true), "faulted graph facts cannot publish");
            c.That(ReferenceEquals(first, g.Fault), "first fault preserved");
            c.Equal(isolated, idle.Hash(), "fault session isolated from interleaved game");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current capacity boundary engine");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original codecs unchanged");
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true })
                TopologyErasePostconditionTests.Healthy(c, point, half, Factory(point, half));
            foreach (string fault in new[] { "throw-offer", "clock-offer", "mutate-offer", "over-offer", "fractional-offer",
                "refusal-offer", "unknown-offer", "negative-offer", "throw-quantum", "clock-quantum", "zero-quantum" })
                foreach (bool half in new[] { false, true }) Negative(c, fault, half);
        }
    }
}
