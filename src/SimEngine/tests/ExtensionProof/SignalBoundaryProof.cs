using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declares existing engine-side contracts; no new state, provider or gameplay loop.</summary>
    public sealed class SignalBoundaryProofModule : GameModule
    {
        public override string Id => "proof.signal-boundary";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(OperationsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
            => types.Component<AutomationProbeDef>("boundary_sensor", 1);
        public override void Compose(WorldComposer composer)
            => composer.Commands.Register("proof.set-network-signal", new SetAutomationProbeHandler());
    }
}
