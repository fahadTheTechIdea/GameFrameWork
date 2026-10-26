using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored metadata binds the ORIGINAL native policy runtime and declaration.
    public sealed class IndependentPolicyDef : ComponentDef<OperationPolicy>, IOperationPolicyDefinition
    {
        private OperationPolicyDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Operation => _declaration.Operation;
        public Code Condition => _declaration.Condition;
        public IUnitDefinition Unit => _declaration.Unit;
        public OperationPolicySettings InitialSettings => _declaration.InitialSettings;
        protected override void ReadCore(JsonReader reader) => _declaration = OperationPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class PolicyBindingModule : GameModule
    {
        private World _world;
        public int Reads { get; private set; }
        public bool NativeBinding { get; private set; }
        public override string Id => "proof.policy-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(EquipmentConditionModule.ModuleId, Version),
            new ModuleRequirement(OperationsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<PermitRequirementDef>("work_permits");
            types.Component<IndependentPolicyDef>("independent_policy", 1);
            types.Component<ProofOperationPolicyDef>("proof_operation_policy", 1);
            types.Component<ProofConditionDef>("proof_condition", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var definition = composer.Content.Get<EntityDef>("contract.factory").Component<IOperationPolicyDefinition>(new Code("safety"));
            NativeBinding = definition is IndependentPolicyDef && definition.ComponentType == typeof(OperationPolicy)
                && ReferenceEquals(definition.Unit, composer.Content.Get<IUnitContentDefinition>("unit.condition"));
        }
        public Code ProbeConditionAdmission(ConditionChange change)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory")
                    return ConditionContracts.Check(_world, entity.Get<IConditionProvider>(new Code("integrity")), change);
            throw new InvalidOperationException("missing actual condition probe");
        }
        public void Arm(string point, OperationBindingFault mode)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory")
                {
                    var policy = entity.Get<ProofOperationPolicy>(new Code("safety"));
                    var condition = entity.Get<ProofCondition>(new Code("integrity"));
                    Action<string> read = name =>
                    {
                        if (name != point || Reads != 0) return; Reads++;
                        if (mode == OperationBindingFault.Exception) throw new InvalidOperationException("proof.policy.observation");
                        if (mode == OperationBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (mode == OperationBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        if (mode == OperationBindingFault.HiddenState) {
                            if (point.StartsWith("condition.", StringComparison.Ordinal)) condition.MutateHiddenProbe();
                            else policy.MutateHiddenProbe();
                        }
                    };
                    ((ProofOperationPolicyDef)entity.Def.Component<IOperationPolicyDefinition>(new Code("safety"))).Observe = read;
                    ((ProofConditionDef)entity.Def.Component<IConditionDefinition>(new Code("integrity"))).Observe = read;
                    policy.Observe = read; condition.Observe = read; return;
                }
            throw new InvalidOperationException("missing actual policy probe");
        }
    }
}
