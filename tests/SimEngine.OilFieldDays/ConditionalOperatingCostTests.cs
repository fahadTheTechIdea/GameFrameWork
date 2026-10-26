using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ConditionalOperatingCostTests
    {
        public const string CaseName = "Tycoon running costs: actual operation state and fine game-clock offsets";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void ProjectCost(JsonValue root)
        {
            var fees = root.Get("entity_operating_costs"); var selected = JsonValue.NewArray();
            for (int i = 0; i < fees.Count; i++) if (fees[i].Get("id").Text == "oil.power-cost.grid_connection")
            { fees[i].Set("target", JsonValue.Of("proof.infrastructure.source")); selected.Add(fees[i]); }
            root.Set("entity_operating_costs", selected);
        }
        // Content authoring only. The native transformer owns actual running state, physical work and controls.
        private static void Fixture(JsonValue root)
        {
            root.Set("units", Json.Parse("[{\"id\":\"unit.work-rate\",\"dimension\":\"work-rate\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}]"));
            root.Get("time_definitions")[0].Set("quantum", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}"));
            root.Get("time_definitions")[0].Set("frame_ticks", JsonValue.Number("10"));
            root.Set("transformations", Json.Parse("[{\"id\":\"proof.running-loop\",\"mode\":\"continuous\",\"cancel_policy\":\"stop\",\"duration\":null,\"inputs\":[{\"resource\":\"cement\",\"quantity\":1,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"}}],\"tools\":{},\"tool_consumption\":[],\"input_ports\":{\"cement\":\"proof.source.stock\"},\"tool_ports\":{},\"operating_inputs\":[],\"operating_ports\":{},\"outputs\":[{\"resource\":\"cement\",\"quantity\":1,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"}}],\"output_ports\":{\"cement\":\"proof.source.stock\"}}]"));
            for (int i = 0; i < root.Get("entities").Count; i++)
                if (root.Get("entities")[i].Get("id").Text == "proof.infrastructure.source")
                    root.Get("entities")[i].Get("components").Set("transformer", Json.Parse("{\"automatic_supply\":false,\"wip_capacity\":\"proof.infrastructure.capacity\",\"transformations\":[\"proof.running-loop\"],\"states\":{\"working\":\"working\",\"stopped\":\"stopped\"},\"operation\":{\"id\":\"power\",\"enabled\":true,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}"));
            for (int i = 0; i < root.Get("scenarios").Count; i++) root.Get("scenarios")[i].Get("modules").Add(JsonValue.Of(TransformationModule.ModuleId));
        }
        private static void Run(Check check)
        {
            foreach (string scenario in new[] { "frontier", "adventure", "continent", "sandbox", "showcase", "gulf" })
            {
                var canonical = OilAuthoredInfrastructureTests.New(OilAuthoredInfrastructureTests.Factory(authoredScenario: scenario));
                check.Equal(10, canonical.Clock.TicksPerFrame, "canonical original ten-minute frame");
                foreach (int minutes in new[] { 5, 15, 45, 50 })
                    check.Equal((long)minutes, canonical.SimulationTime.ExactTicks(GameDuration.Minutes(minutes)), "canonical fine Oil schedule offset");
            }
            foreach (int mode in new[] { 0, 1, 2 })
            {
                var factory = OilAuthoredInfrastructureTests.Factory(financialMode: mode, operatingCosts: ProjectCost, editFixture: Fixture);
                var game = OilAuthoredInfrastructureTests.New(factory);
                var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source").Id;
                var business = mode == 0 ? null : game.Seat(0).View.Feature<BusinessView>();
                check.Equal(5L, game.SimulationTime.ExactTicks(GameDuration.Minutes(5)), "five-minute offset represented by the sole clock");
                check.Equal(60L, game.SimulationTime.ExactTicks(GameDuration.Hours(1)), "fine simulation step remains independent of ten-step frame");
                if (business != null)
                {
                    var fees = new List<EntityOperatingCostView>();
                    check.Equal(Code.None, business.CopyOperatingCosts(source, fees), "typed fee eligibility view");
                    check.Equal(Fix.FromInt(60), fees.Single().Cost.AuthoredAmount, "original grid hourly charge");
                    check.Equal(RunningOperationCostConditionDef.NotRunning, fees.Single().EligibilityBlocker, "enabled but idle is not running");
                }
                game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = source, Capability = new Code("power"), Transformation = "proof.running-loop" });
                game.Update(); var twin = factory.Load(game.Save());
                if (mode == 2) check.Equal(Fix.FromInt(4940), Balance(business), "actual running operation charged at first five-minute offset");
                game.Seat(0).Submit(Control(source, false)); twin.Seat(0).Submit(Control(source, false));
                for (int i = 0; i < 8; i++) { game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "disabled saved physical continuation"); }
                if (mode == 2) check.Equal(Fix.FromInt(4940), Balance(business), "disabled operation incurs no later hourly cost");
                game.Seat(0).Submit(Control(source, true)); twin.Seat(0).Submit(Control(source, true));
                for (int i = 0; i < 9; i++) { game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "resumed actual operation continuation"); }
                if (mode == 2) check.Equal(Fix.FromInt(4880), Balance(business), "resumed operation charges once at next actual offset");
                if (mode == 1) check.That(!business.HasAccounts && !business.HasBudgets, "conditional costs disabled without money fallback");
                check.That(game.Fault == null && twin.Fault == null, "healthy actual-operation native sessions");
            }
            check.Throws<ContentException>(() => OilAuthoredInfrastructureTests.New(OilAuthoredInfrastructureTests.Factory(financialMode: 2,
                operatingCosts: root => { ProjectCost(root); root.Get("operation_running_cost_conditions")[0].Set("operation", JsonValue.Of("missing.operation")); }, editFixture: Fixture)), "missing named operation refused before initialization");
        }
        private static ConfigureOperationCommand Control(EntityId source, bool enabled)
            => new ConfigureOperationCommand { Entity = source, Operation = new Code("power"), Enabled = enabled, Target = Fix.One };
        private static Fix Balance(BusinessView view)
        {
            var refusal = view.ReadAccount(new PlayerId(0), out var account);
            if (!refusal.IsNone) throw new InvalidOperationException("native account read refused: " + refusal);
            return account.Balance;
        }
    }
}
