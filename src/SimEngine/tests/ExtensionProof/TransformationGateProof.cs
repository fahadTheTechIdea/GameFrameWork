using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent JSON kind, same engine declaration and runtime; no gameplay implementation.</summary>
    public sealed class ProofSignalTransformationGateDef : ComponentDef<SignalTransformationGate>, ISignalTransformationGateDefinition
    {
        private SignalTransformationGateDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<ITransformationDefinition> Transformations => _declaration.Transformations;
        public IReadOnlyList<SignalGateCondition> Conditions => _declaration.Conditions;
        protected override void ReadCore(JsonReader reader) => _declaration = SignalTransformationGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        // Deliberately no ValidateCore override: the engine must admit every selected runtime binding.
    }

    /// <summary>Reuse the actual independent signal/control provider; no gate-specific stock/timer.</summary>
    public sealed class TransformationGateProofModule : GameModule
    {
        public override string Id => "proof.transformation-gates";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<AutomationProbeDef>("gate_probe", 1);
            types.Component<ProofSignalTransformationGateDef>("proof_signal_transformation_gate", 1);
        }
        public override void Compose(WorldComposer composer)
            => composer.Commands.Register("proof.gates.set-signal", new SetAutomationProbeHandler());
    }
}
