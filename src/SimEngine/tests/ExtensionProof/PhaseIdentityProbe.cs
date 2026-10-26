using System;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Composition-negative proof only; no jobs, stock, timing or gameplay progression.</summary>
    public sealed class PhaseIdentityProbeModule : GameModule
    {
        private readonly Code _identity;
        public PhaseIdentityProbeModule(Code identity) { _identity = identity; }
        public override string Id => "proof.phase-identity";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void Compose(WorldComposer composer)
        {
            var probe = new PhaseIdentityProbe(_identity);
            composer.Services.Bind(probe);
            composer.Services.Bind<IPhaseIdentityProbe>(probe);
        }
    }
    public interface IPhaseIdentityProbe { }
    public sealed class PhaseIdentityProbe : IWorkPhaseExecutor, IPhaseIdentityProbe
    {
        public PhaseIdentityProbe(Code identity) { Id = identity; }
        public Code Id { get; }
        public WorkPhasePurpose Purpose => WorkPhasePurpose.Other;
        public void Execute(WorkPhaseContext context) => throw new InvalidOperationException("composition probe is not an authored physical phase");
    }
}
