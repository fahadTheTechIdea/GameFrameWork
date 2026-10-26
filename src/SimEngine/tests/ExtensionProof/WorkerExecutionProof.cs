using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofWorkerExecutionDef : ComponentDef<ProofWorkerExecution>, IWorkerExecutionDefinition
    {
        public Code CapabilityId { get; private set; }
        public Code Operation { get; private set; }
        public Fix JobRequiredAt { get; private set; }
        public Fix Multiplier { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); Operation = new Code(reader.String("operation"));
            JobRequiredAt = reader.Fix("job_required_at"); Multiplier = reader.Fix("multiplier"); Fault = reader.Int("fault");
            if (Multiplier < Fix.Zero || Fault < 0 || Fault > 5) throw reader.Error("explicit proof worker execution values required");
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IWorker>() || !owner.HasCapability<IControllableOperation>(Operation))
                throw new ContentException(owner.Id + ": proof worker gate requires actual worker/control binding");
        }
    }
    // Engine-side public-only provider, independent of native worker_execution and its gate authoring.
    public sealed class ProofWorkerExecution : Component<ProofWorkerExecutionDef>, IWorkerExecution
    {
        public Code CapabilityId => Def.CapabilityId;
        public ExecutionRateSnapshot ReadExecution(World world)
        {
            if (Def.Fault == 1) Entity.Touch();
            bool blocked = Entity.Get<IControllableOperation>(Def.Operation).ReadControl().Target >= Def.JobRequiredAt && Entity.Get<IWorker>().Job == 0;
            Code blocker = blocked ? new Code("proof.execution-job-required") : Code.None;
            var gate = new ExecutionGateSnapshot(Def.Fault == 2 ? new Code("wrong") : CapabilityId, true, blocker, Array.Empty<ExecutionGateConditionSnapshot>());
            if (Def.Fault == 4) gate = null;
            if (Def.Fault == 5) gate = new ExecutionGateSnapshot(CapabilityId, false, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            return new ExecutionRateSnapshot(gate, Def.Fault == 3 ? -Fix.One : Def.Multiplier);
        }
    }
}
