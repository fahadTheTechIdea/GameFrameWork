using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class ReferencedUnitVisibilityTests
    {
        internal const string Case = "Metadata visibility: referenced units gate resources policies and spatial facts";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string unit, bool half)
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.layer-access"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            // The base supply measures unit.supply; author a distinct actual capacity link for this case.
            if (unit == "unit.capacity.count") root.Get("resources")[0].Set("capacity", Json.Parse("{\"unit.capacity.count\":1}"));
            root.Get("capacity_policies").Add(Json.Parse("{\"id\":\"policy.private-link\",\"limits\":{\"" + unit + "\":8}}"));
            var config = Json.Parse(FrameworkProofContent.Read("layer-access.json"));
            config.Get("layer_access_probes")[0].Set("hidden_resource", JsonValue.Null());
            config.Get("layer_access_probes")[0].Set("hidden_unit", JsonValue.Of(unit));
            return PointGame.Factory(definitions: root.ToString(), layerDefinitions: FrameworkProofContent.Read("point-layers.json"),
                additionalModule: new LayerAccessProofModule(),
                moduleDeclaration: "{\"id\":\"proof.layer-access\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"points\",\"version\":\"1.0.0\"}],\"documents\":[\"layer-access.json\"]}",
                moduleDocument: config.ToString());
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 7087, Seats = { Controller.Human, Controller.Human } });
        private static void Menus(Check c, SimGame g, string unit)
        {
            var mine = g.Seat(0).View; var other = g.Seat(1).View;
            var units = new List<UnitInfo>(); other.CopyUnits(units);
            c.That(units.All(x => x.Unit.Id != unit), "selected policy directly hides actual referenced unit");
            mine.CopyUnits(units); c.That(units.Any(x => x.Unit.Id == unit), "authorized company retains same actual unit metadata");
            var policies = new List<CapacityPolicyInfo>(); other.CopyCapacityPolicies(policies);
            c.That(policies.All(x => x.Id != "policy.private-link"), "public parent policy cannot disclose hidden limit-unit facts");
            mine.CopyCapacityPolicies(policies); c.That(policies.Any(x => x.Id == "policy.private-link"), "authorized company retains parent policy");
            var resources = new List<ResourceInfo>(); other.CopyResources(resources);
            bool resourceHidden = unit != "point.metre";
            c.Equal(!resourceHidden, resources.Any(x => x.Id == "supply"), "resource menu follows quantity/capacity-unit closure, not unrelated units");
            c.Equal(!resourceHidden, other.TryFindResource("supply", out var foreignId), "resource lookup follows same nested unit gate");
            c.That(resourceHidden ? !foreignId.IsValid : foreignId.IsValid, "hidden lookup clears compact identity rather than exposing parent reference");
            c.That(mine.TryFindResource("supply", out var actual), "authorized company resolves actual native resource");
            c.Equal(resourceHidden ? null : "supply", other.ResourceName(actual), "name query cannot bypass parent visibility using known compact identity");
            g.Observer.CopyResources(resources); c.That(resources.Any(x => x.Id == "supply"), "exact observer retains ordinary resource facts");
            g.Observer.CopyCapacityPolicies(policies); c.That(policies.Any(x => x.Id == "policy.private-link"), "exact observer retains policy links");
            var layers = other.Feature<PointLayerView>().Layers;
            c.Equal(unit != "point.metre", layers.Any(x => x.Id == "point.terrain"), "direct spatial unit uses same selected player policy");
            c.That(layers.All(x => x.Id != "point.deposit"), "owned resource layer still refuses hidden resource units or private ownership");
            var values = new List<Fix> { Fix.One };
            c.Equal(SpatialLayerReadStatus.Unavailable, other.Feature<PointLayerView>().ReadLayer("point.deposit",
                new PointRegion(Fix.FromInt(7), Fix.One, 1), values), "resource-linked sample refuses without hidden unit metadata");
            c.Equal(0, values.Count, "hidden sample refusal clears result");
            var keys = new List<string> { "retained" }; other.Feature<LayerAccessReadModel>().AppendKeys("point.deposit", keys);
            c.That(keys.SequenceEqual(new[] { "retained" }), "hidden referenced units or private ownership disclose no observation keys");
            c.That(mine.Feature<PointLayerView>().Layers.Any(x => x.Id == "point.terrain"), "authorized terrain metadata is not globally hidden");
        }
        private static void Physical(Check c, string unit, bool half)
        {
            var f = Factory(unit, half); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            var target = Entity(g, "destination");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "inspect", Target = target, Priority = 30 });
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "unit-gated metadata preserves pre-completion save");
            bool done = false;
            for (int i = 0; i < 1000; i++)
            {
                ulong before = g.Hash(); long time = g.Clock.StepIndex; Menus(c, g, unit);
                c.Equal(before, g.Hash(), "unit closure/menu/lookup/layer reads remain pure"); c.Equal(time, g.Clock.StepIndex, "metadata policies do not drive time");
                if (g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _)) { done = true; break; }
                Step(g, twin);
            }
            if (!done) throw new InvalidOperationException("referenced-unit physical workflow exceeds1000Updates clock=" + g.Clock.StepIndex);
            c.That(((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x => x.Id == Entity(g, "machine")).Spatial).Position > Fix.Zero,
                "player policy never replaces actual physical worker execution");
            PermanentRemovalTests.Submit(c, g, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) });
            c.That(!g.Seat(1).View.Feature<PointLayerView>().Layers.Any(x => x.Id == "point.deposit"),
                "even actual private owner needs visible linked quantity/capacity/vertical units");
            var restored = f.Load(g.Save()); c.Equal(g.Hash(), restored.Hash(), "referenced-unit visibility follows same saved original state");
            c.That(!restored.Seat(1).View.Feature<PointLayerView>().Layers.Any(x => x.Id == "point.deposit"), "saved ownership cannot bypass nested metadata policy");
            for (int i = 0; i < 4; i++) Step(g, restored);
            c.Equal(idleHash, idle.Hash(), "interleaved company policy session unaffected");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "physical work/ownership replay retains same unit visibility");
        }
        private static void RecipeMenus(Check c, SimGame g, bool known)
        {
            ulong before = g.Hash(); long time = g.Clock.StepIndex;
            var resources = new List<ResourceInfo>(); g.Seat(0).View.CopyResources(resources);
            c.Equal(known, resources.Any(x => x.Id == "supply"), "same discovered unit gates resource and recipe menus");
            c.Equal(known, g.Seat(0).View.TryFindResource("supply", out var id), "unit discovery controls ordinary resource lookup");
            c.That(known ? id.IsValid : !id.IsValid, "unlearned unit never leaks native compact resource identity");
            c.Equal(known ? "contract.result" : null, g.Seat(0).View.Feature<TransformationManagementView>().TransformationEntityOutput("contract.make"),
                "recipe output disclosure reuses shared resource-unit visibility");
            c.Equal(before, g.Hash(), "recipe/resource metadata checks remain pure"); c.Equal(time, g.Clock.StepIndex, "learned metadata reads do not advance time");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current referenced-unit visibility build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current original visibility state and registry");
            foreach (string unit in new[] { "unit.supply", "unit.capacity.count", "point.metre" })
                foreach (bool half in new[] { false, true }) Physical(c, unit, half);
            // Reuse unchanged public manufacturing workflow, not another gameplay implementation or registry run.
            RecipeDisclosureTests.Physical(c, false, "quantity", false, RecipeMenus);
            RecipeDisclosureTests.Physical(c, true, "capacity", true, RecipeMenus);
        }
    }
}
