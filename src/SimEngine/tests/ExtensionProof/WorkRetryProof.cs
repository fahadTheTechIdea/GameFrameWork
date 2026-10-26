using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>A different authored delay algorithm, with an explicit hostile-provider probe.</summary>
    public sealed class ConstantRetryProfileDef : Def, IWorkRetryPolicy
    {
        private GameDuration _delay;
        private bool _fault;
        protected override void ReadCore(JsonReader reader)
        { _delay = reader.Child("delay").RationalDuration(); _fault = reader.Bool("fault_at_two"); if (_delay.IsZero) throw reader.Error("positive delay required"); }
        public bool CanRetry(Code blocker) => blocker == WorkSystem.BlockerUnreachable || blocker == WorkSystem.BlockerWorkerUnavailable;
        public long Delay(ISimulationTime time, int failures)
            => _fault && failures == 2 ? 0 : time.Ticks(_delay);
    }
    /// <summary>Public-only engine extension. Controls own availability; WorkSystem still owns all work/retry state.</summary>
    public sealed class WorkRetryProofModule : GameModule
    {
        public override string Id => "proof.retry-module";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]
        { new ModuleRequirement(WorkModule.ModuleId, Version), new ModuleRequirement(OperationsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ConstantRetryProfileDef>("proof_retry_profiles");
        }
    }
}
