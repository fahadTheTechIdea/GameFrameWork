using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Existing master/module and original metadata; only negative callback installation is added.
    public sealed class OperationalViewProofModule : ControlContractModule
    {
        private World _world;
        public OperationalViewProofModule(InitializeProbe initialize) : base(initialize) { }
        public override void Compose(WorldComposer composer) { base.Compose(composer); _world = composer.World; }
        public void Arm(string point, string fault)
        {
            bool fired = false;
            Action inject = () => {
                if (fired) return; fired = true;
                if (fault == "pause" || fault == "throw-pause") ((GameClock)_world.Clock).Pause();
                if (fault == "throw" || fault == "throw-pause") throw new InvalidOperationException("proof.operational.original");
                if (fault == "neighbor") {
                    foreach (var entity in _world.Entities.All) if (entity.Def.Id == "contract.sensor") {
                        var provider = entity.Get<AutomationProbe>(new Code("probe"));
                        provider.FaultMode = 5; provider.ReadSignal(_world, _world.Content.Get<IAutomationSignalContent>("contract.sensor-level")); provider.FaultMode = 0;
                    }
                }
            };
            if (point == "profile") _world.Content.Get<IndependentControlRuleDef>("contract.incident-alert").ObserveProjection = inject;
            if (point == "targets") _world.Content.Get<IndependentControlActionDef>("contract.sensor-action").ObserveTargets = inject;
            if (point == "sources") _world.Content.Get<EntityDef>("contract.sensor").Component<AutomationProbeDef>(new Code("probe.second")).ObserveIdentity = inject;
        }
    }
}
