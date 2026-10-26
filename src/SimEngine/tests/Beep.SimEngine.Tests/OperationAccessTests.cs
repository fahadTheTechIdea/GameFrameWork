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

namespace Beep.SimEngine.Tests
{
    // File-authored native condition/policy metadata and public player inputs; all gameplay stays in engine owners.
    internal static class OperationAccessTests
    {
        internal const string Case = "Operation access: private control and condition policies preserve physical WIP pause and saved continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("process"), Safety = new Code("safety"), Integrity = new Code("integrity");
        private static GameFactory Factory(bool half, DirectResourceContentModule resources)
            => ResourceContentContractTests.Factory(half, resources, worldEdit: root => {
                root.Get("units").Add(Json.Parse(FrameworkProofContent.Read("equipment-condition.json")).Get("units")[0].Clone());
                var authored = Json.Parse(FrameworkProofContent.Read("operation-policies.json")).Get("entities")[0].Get("components");
                var condition = authored.Get("equipment_condition").Clone();
                condition.Get("states").Set("damaged", JsonValue.Of("blocked")); condition.Get("states").Set("failed", JsonValue.Of("blocked"));
                var target = root.Get("entities")[1].Get("components"); target.Set("equipment_condition", condition); target.Set("operation_policy", authored.Get("operation_policy").Clone());
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(EquipmentConditionModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"destination\",\"initial_hidden\":false}"));
            }, extraModules: new GameModule[] { new InventorySightModule(), new EquipmentConditionModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[]}]");
        private static ConfigureOperationCommand Control(EntityId id, bool enabled, Code operation = default, Fix? target = null)
            => new ConfigureOperationCommand { Entity = id, Operation = operation.IsNone ? Process : operation, Enabled = enabled, Target = target ?? Fix.One };
        private static OperationPolicySettings Settings() => new OperationPolicySettings(30, Fix.FromInt(5), Fix.FromInt(4), Fix.FromInt(8), OperationRestartPolicy.Automatic);
        private static ConfigureOperationPolicyCommand Policy(EntityId id, OperationPolicySettings settings) => new ConfigureOperationPolicyCommand { Entity = id, Policy = Safety, Settings = settings };
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            var view = g.Seat(seat).View;
            Code typed = command is ConfigureOperationCommand operation ? view.Feature<OperationsView>().PreviewConfigure(operation.Entity, operation.Operation, operation.Enabled, operation.Target)
                : view.Feature<EquipmentConditionView>().PreviewPolicy(((ConfigureOperationPolicyCommand)command).Entity, ((ConfigureOperationPolicyCommand)command).Policy, ((ConfigureOperationPolicyCommand)command).Settings);
            c.Equal(typed, g.Seat(seat).Preview(command), "typed and ordinary operation/policy admission share one evaluator"); return typed;
        }
        private static TransformationSnapshot Actual(SimGame g, EntityId id)
        { if (!g.Observer.Feature<TransformationManagementView>().TryGetTransformation(id, Process, out var value)) throw new InvalidOperationException("actual WIP unavailable"); return value; }
        private static OperationPolicyView ActualPolicy(SimGame g, EntityId id)
        { var code = g.Observer.Feature<EquipmentConditionView>().ReadPolicy(id, Safety, out var value); if (!code.IsNone) throw new InvalidOperationException("actual policy/" + code); return value; }
        private static void Hidden(Check c, SimGame g, EntityId id)
        {
            ulong before = g.Hash(); var view = g.Seat(0).View.Feature<EquipmentConditionView>(); var ids = new List<Code> { new Code("seed") };
            c.That(!view.CopyIds(id, ids) && ids.Count == 0, "hidden condition IDs clear caller seed");
            ids.Add(new Code("seed")); c.That(!view.CopyPolicyIds(id, ids) && ids.Count == 0, "hidden policy IDs disclose no prefix");
            c.Equal(Codes.NotFound, view.Read(id, Integrity, out _), "hidden condition unavailable");
            c.Equal(Codes.NotFound, view.ReadWear(id, Integrity, out _), "hidden wear unavailable before capability");
            c.Equal(Codes.NotFound, view.ReadPolicy(id, Safety, out var unavailable), "hidden policy unavailable");
            c.That(unavailable.State.Settings == null && unavailable.Policy.IsNone, "hidden policy facts remain absent");
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Control(id, true), Control(id, true, new Code("missing"), Fix.FromInt(99)),
                new ConfigureOperationCommand { Entity = id, Operation = Code.None, Enabled = true, Target = Fix.FromInt(99) }, Policy(id, Settings()), Policy(id, null),
                new ConfigureOperationPolicyCommand { Entity = id, Policy = Code.None, Settings = null } })
                c.Equal(Codes.NotFound, Parity(c, g, seat, command), "private access precedes operation ID/target/policy settings");
            c.Equal(Codes.NotFound, g.Observer.Feature<OperationsView>().PreviewConfigure(id, Process, true, Fix.One), "observer operation diagnosis retains owner refusal");
            c.Equal(Codes.NotFound, g.Observer.Feature<EquipmentConditionView>().PreviewPolicy(id, Safety, Settings()), "observer policy diagnosis retains owner refusal");
            c.Equal(before, g.Hash(), "all hidden current/control observations pure");
        }
        private static void Checkpoint(Check c, GameFactory f, SimGame g, EntityId target, ResourceId resource)
        {
            c.Equal(Code.None, Parity(c, g, 0, Control(target, false)), "visible shutdown admission");
            c.Equal(Code.None, Parity(c, g, 0, Policy(target, Settings())), "visible native policy admission");
            c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Policy(target, null)), "visible malformed policy typed refusal");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Control(target, false)), "foreign operation control private");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Policy(target, Settings())), "foreign policy control private");
            PermanentRemovalTests.Submit(c, g, Policy(target, Settings())); PermanentRemovalTests.Submit(c, g, Control(target, false));
            var retained = Actual(g, target); var oldPolicy = ActualPolicy(g, target);
            var twin = f.Load(g.Save()); long age = g.Clock.StepIndex;
            for (int i = 0; i < 3; i++) TransformationContentContractTests.Step(g, twin);
            c.That(g.Clock.StepIndex > age && Actual(g, target).Progress.Equals(retained.Progress) && Actual(g, target).Wip.Stock.AmountOf(resource) == Fix.FromInt(2), "disabled operation retains actual WIP/tools while game time advances");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true }); Hidden(c, g, target);
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Control(target, true), Policy(target, new OperationPolicySettings(99, Fix.FromInt(5), Fix.FromInt(4), Fix.FromInt(8), OperationRestartPolicy.Manual)) })
                pending.Add((seat, g.Seat(seat).Submit(command)));
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden stopped physical WIP and pending actual commands durable"); Hidden(c, twin, target);
            TransformationContentContractTests.Step(g, twin);
            foreach (var input in pending)
            {
                long cursor = 0; var rows = new List<CommandResult>(); g.Seat(input.Seat).ReadResults(ref cursor, rows);
                var result = rows.Single(x => x.Sequence == input.Sequence); c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed private operation/policy input refuses");
            }
            var actual = ActualPolicy(g, target);
            c.That(!actual.Control.Enabled && actual.State.Settings.Criticality == 30 && actual.Equipment.Value == Fix.FromInt(10), "refused input changes no operation, condition or policy owner");
            c.That(Actual(g, target).Progress.Equals(retained.Progress) && Actual(g, target).Tools.Receipts.Count == retained.Tools.Receipts.Count, "private refused controls keep real progress/tool receipts");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            c.Equal(Code.None, Parity(c, g, 0, Control(target, true)), "revealed control restores actual admission");
            PermanentRemovalTests.Submit(c, g, Control(target, true));
            c.That(!oldPolicy.Control.Enabled && oldPolicy.State.Settings.Criticality == 30 && retained.Wip.Stock.AmountOf(resource) == Fix.FromInt(2), "retained immutable control/policy/WIP facts do not become current authority");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current operation authority identity");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true })
            {
                var resources = new DirectResourceContentModule(); var f = Factory(half, resources);
                ResourceContentContractTests.Healthy(c, half, cancel, f, resources, partialCheckpoint: Checkpoint);
            }
        }
    }
}

