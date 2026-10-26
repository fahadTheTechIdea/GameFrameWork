using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class EquipmentConditionModule : GameModule
    {
        public const string ModuleId = "sim.equipment-condition";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<EquipmentConditionDef>("equipment_condition", 2);
            types.Component<OperationPolicyDef>("operation_policy", 1);
            types.Section<AuthoredConditionWearDef>("condition_wear_profiles");
            types.Section<ConditionEffectDef>("condition_effects");
            types.Section<ConditionRequirementDef>("condition_requirements");
            types.Section<WorkerConditionEffectDef>("worker_condition_effects");
            types.Section<WorkerConditionRequirementDef>("worker_condition_requirements");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new EquipmentConditionSystem(), 370);
            composer.Commands.Register("sim.configure-operation-policy", new Commands.ConfigureOperationPolicyHandler());
            composer.AddViewFeature(Id, 4, (world, viewer) => new EquipmentConditionView(world, viewer));
        }
    }
}
