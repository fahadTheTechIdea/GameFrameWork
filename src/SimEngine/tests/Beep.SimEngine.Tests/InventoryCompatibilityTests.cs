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
    internal static class InventoryCompatibilityTests
    {
        internal const string Case = "Inventory compatibility: explicit named local metadata private resources and scoped provider faults";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InventoryCompatibilityProofModule module, bool hidden = false)
        {
            var definition = new GameDefinition("inventory-compatibility", new GameModule[] { new ContentAccessModule(), new InventoryModule(), new ResourceFlowsModule(), new OperationsModule(),
                new TransformationModule(), new WorldSetupModule(), new EndpointProofModule(), new OutputPortProofModule(), module });
            var manifest = Json.Parse(FrameworkProofContent.Read("output-ports-master.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.inventory-compatibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"proof.output-ports\",\"version\":\"1.0.0\"}],\"documents\":[\"inventory-compatibility.json\"]}"));
            var ports = Json.Parse(FrameworkProofContent.Read("output-ports.json"));
            ports.Get("entities")[0].Get("components").Set("compatibility_port", Json.Parse("[{\"id\":\"probe\",\"pool\":\"ports.waste-pool\"}]"));
            ports.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("compat.named-only-start"));
            ports.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            var probe = Json.Parse(FrameworkProofContent.Read("inventory-compatibility.json"));
            if (hidden) probe.Get("compatibility_visibility")[0].Set("hidden_resource", JsonValue.Of("ports.water"));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString())
                .Add("resource-port-common.json", FrameworkProofContent.Read("resource-port-common.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("endpoints-flows.json", FrameworkProofContent.Read("endpoints-flows.json"))
                .Add("output-ports.json", ports.ToString()).Add("inventory-compatibility.json", probe.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "ports", Seed = 937, Seats = { Controller.Human, Controller.Human } });
        private static ResourceId Resource(SimGame game, string name)
        {
            var resources = new List<ResourceInfo>(); game.Observer.CopyResources(resources);
            return resources.Single(r => r.Id == name).Resource;
        }
        private static EntityId Entity(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == name).Id;
        private static void Run(Check c)
        {
            var module = new InventoryCompatibilityProofModule(); var factory = Factory(module); var game = New(factory);
            var view = game.Seat(0).View.Feature<InventoryView>(); var water = Resource(game, "ports.water"); var alloy = Resource(game, "ports.alloy"); var ore = Resource(game, "ports.ore");
            ulong hash = game.Hash();
            c.That(view.StoreAccepts("ports.factory", ore, Code.None), "explicit local feed accepts ore");
            c.That(!view.StoreAccepts("ports.factory", water, Code.None), "local feed never falls back to named waste");
            c.That(view.StoreAccepts("ports.factory", water, new Code("probe")), "actual independent named metadata accepts waste");
            c.That(!view.StoreAccepts("ports.factory", ore, new Code("probe")), "named port retains authored compatibility");
            c.That(view.StoreAccepts("compat.named-only", alloy, new Code("probe")), "named-only endpoint works without local inventory");
            c.That(!view.StoreAccepts("compat.named-only", alloy, Code.None) && !view.StoreAccepts("compat.named-only", alloy, new Code("missing")), "absent selectors invent no local or named fallback");
            c.Equal(Code.None, view.ReadResourcePort(Entity(game, "ports.factory"), new Code("probe"), water, out var facts), "live metadata and prototype compatibility agree");
            c.That(facts.Ready && facts.Resource == water, "actual detached endpoint facts");
            c.Equal(hash, game.Hash(), "compatibility queries are pure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "normal actual endpoint save");
            game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "interleaved endpoint sessions");

            module = new InventoryCompatibilityProofModule(); factory = Factory(module, true); game = New(factory); view = game.Seat(0).View.Feature<InventoryView>(); water = Resource(game, "ports.water");
            int calls = module.Observations("ports.factory"); hash = game.Hash();
            c.That(!view.StoreAccepts("ports.factory", water, new Code("probe")), "hidden resource compatibility withheld");
            c.Equal(Codes.NotFound, view.ReadResourcePort(Entity(game, "ports.factory"), new Code("probe"), water, out _), "hidden live resource is not disclosed");
            c.Equal(calls, module.Observations("ports.factory"), "hidden query invokes no compatibility callback");
            c.Equal(hash, game.Hash(), "hidden observations are pure");
            foreach (var sample in new[] { (live: false, fault: CompatibilityFault.Throw), (live: false, fault: CompatibilityFault.Clock),
                (live: true, fault: CompatibilityFault.Throw), (live: true, fault: CompatibilityFault.Clock), (live: true, fault: CompatibilityFault.Target),
                (live: true, fault: CompatibilityFault.EntityInventoryClaim) })
            {
                module = new InventoryCompatibilityProofModule(); factory = Factory(module); game = New(factory); view = game.Seat(0).View.Feature<InventoryView>(); water = Resource(game, "ports.water"); var target = Entity(game, "ports.factory");
                module.Arm("ports.factory", sample.fault);
                c.Throws<InvalidOperationException>(() => { if (sample.live) view.ReadResourcePort(target, new Code("probe"), water, out _); else view.StoreAccepts("ports.factory", water, new Code("probe")); }, "actual callback violation refuses " + sample);
                c.That(game.Fault != null, "original session fault latches " + sample);
                var first = game.Fault;
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted compatibility cannot checkpoint " + sample);
                c.Throws<InvalidOperationException>(() => view.StoreAccepts("missing", water, Code.None), "faulted absence cannot return healthy false " + sample);
                c.That(ReferenceEquals(first, game.Fault), "original first fault preserved " + sample);
            }
        }
    }
}
