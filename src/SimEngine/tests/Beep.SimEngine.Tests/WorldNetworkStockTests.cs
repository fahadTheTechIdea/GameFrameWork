using System;
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
    // Explicit public/world-owned authoring; shared physical player driver invokes original engine work.
    internal static class WorldNetworkStockTests
    {
        internal const string Case = "World network stock: public tile and point topology conserve stock with explicit world transfer authority";
        internal static void Register() => TestRunner.Add(Case, Run);
        private const string Declaration = "[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[]}]";
        internal static GameFactory Factory(bool point, bool half)
        {
            var modules = new GameModule[] { new EndpointProofModule() };
            if (!point) return PhysicalLineRemovalTests.Create(false,
                editDefinitions: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); },
                editNetwork: root => {
                    root.Get("networks")[0].Set("public", JsonValue.Of(true));
                    var band = root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                    band.Set("minimum", JsonValue.Number("26")); band.Set("maximum", JsonValue.Number("30"));
                },
                editTopology: root => { var line = root.Get("lines")[0]; line.Set("owned", JsonValue.Of(false)); if (line.Has("owner_layer")) line.Remove("owner_layer"); },
                extraModules: modules, extraDeclarations: Declaration);
            return SpatialNetworkGraphTests.Points(half, new PointLineGeometryProofModule(),
                extraAuthor: root => { root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)); root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.pools")); },
                editCorridors: root => { var line = root.Get("lines")[0]; line.Set("owned", JsonValue.Of(false)); if (line.Has("owner_layer")) line.Remove("owner_layer"); },
                editNetwork: root => { root.Get("networks")[0].Set("public", JsonValue.Of(true)); root.Get("networks")[0].Set("capacity_policy", JsonValue.Of("fixture.count.8")); },
                afterPorts: root => {
                    root.Get("entities")[1].Get("components").Get("ports").Get("list")[0].Set("pull", JsonValue.NewArray());
                    root.Get("entities")[0].Get("components").Set("network_resource_port", Json.Parse("{\"id\":\"graph\",\"network\":\"point.network\",\"minimum_members\":1}"));
                }, extraModules: modules, extraDeclarations: Declaration);
        }
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        private static ResourceId Resource(SimGame g, bool point)
        { if (!g.Observer.TryFindResource(point ? "supply" : "steel", out var id)) throw new InvalidOperationException("actual network resource missing"); return id; }
        private static void Authority(Check c, SimGame g, bool point)
        {
            var pool = TopologyErasePostconditionTests.Pools(g, point).Single(); var target = ResourceEndpointId.ForEntity(Entity(g, point ? "machine" : "truck"));
            var resource = Resource(g, point); var quantity = new Bill(new[] { new ResourceAmount(resource, Fix.One) });
            var owner = new ReservationOwner("proof.world-authority", 89);
            PermanentRemovalTests.Submit(c, g, new ClaimProbeCommand { Intents = new[] {
                new ResourceClaimIntent(ReservationDirection.Outbound, owner, pool.Endpoint, quantity),
                new ResourceClaimIntent(ReservationDirection.Inbound, owner, target, quantity) } });
            ResourceReservation[] records = null;
            g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Entity(g, point ? "machine" : "truck"), Code.None, out var incoming);
            var stockRecords = g.Seat(0).View.Feature<EndpointPoolDiagnostics>().EndpointClaims(pool.Endpoint, owner).Records;
            records = stockRecords.Concat(incoming.Where(x => x.Owner.Equals(owner))).ToArray();
            var outgoing = records.Single(x => x.Direction == ReservationDirection.Outbound); var inlet = records.Single(x => x.Direction == ReservationDirection.Inbound);
            var diagnostics = g.Observer.Feature<EndpointPoolDiagnostics>(); ulong hash = g.Hash();
            ResourceTransferIntent Intent(PlayerId actor, ResourceTransferKind kind) => new ResourceTransferIntent(owner, actor, outgoing.Token, inlet.Token, resource, Fix.One, kind);
            c.Equal(ResourceTransferStatus.InvalidRequest, diagnostics.EvaluateTransfer(Intent(PlayerId.None, ResourceTransferKind.Stock)).Status, "actual world actor cannot move stock into company-owned endpoint");
            c.Equal(ResourceTransferStatus.Ready, diagnostics.EvaluateTransfer(Intent(new PlayerId(0), ResourceTransferKind.Stock)).Status, "normal public-source company transfer remains legal");
            c.Equal(hash, g.Hash(), "authority checks mutate no stocks promises clock or sequences");
            PermanentRemovalTests.Submit(c, g, new ClaimProbeCommand { Release = true, Receipts = records });
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current world transfer identity");
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true })
            {
                var f = Factory(point, half); var g = TopologyErasePostconditionTests.Healthy(c, point, half, f);
                c.That(!g.Modules.Contains(TycoonModule.ModuleId), "world-owned topology requires no business module");
                c.That(TopologyErasePostconditionTests.Pools(g, point).All(x => x.Company == PlayerId.None), "public graph retains actual world owner, not fabricated player");
                var journal = g.Observer.Feature<ResourceFlowView>();
                if (journal == null) throw new InvalidOperationException("authored world journal missing/point=" + point + "/half=" + half + "/modules=" + string.Join(",", g.Modules));
                var page = journal.Read(PlayerId.None, 0);
                var facts = page.Entries.Where(x => x.Item.Owner.Scope == "sim.infrastructure.topology").Select(x => x.Item).ToArray();
                c.That(facts.Length >= 2 && facts.All(x => x.Company == PlayerId.None && x.Kind == ResourceFlowKind.StockTransfer && x.IsTransfer), "actual world redistribution journal records physical transfer, never purchase");
                c.That(facts.All(x => x.SourceBefore - x.SourceAfter == x.Amount && x.DestinationAfter - x.DestinationBefore == x.Amount), "world facts preserve exact native debit/credit vectors");
                var hidden = g.Seat(0).View.Feature<ResourceFlowView>().Read(PlayerId.None, 0);
                c.That(hidden.Status == ResourceFlowReadStatus.NotOwned && hidden.Entries.Count == 0 && hidden.Latest == 0, "company view cannot read observer world history or watermark");
                Authority(c, g, point);
                c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "world journal and remaining native pools durable after authority refusals");
            }
        }
    }
}

