using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofMotionExecutionDef : ComponentDef<ProofMotionExecution>, IMotionExecutionDefinition
    {
        public Code CapabilityId { get; private set; }
        public Fix Multiplier { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); Multiplier = reader.Fix("multiplier"); Fault = reader.Int("fault");
            if (Multiplier < Fix.Zero || Fault < 0 || Fault > 5) throw reader.Error("invalid authored motion proof");
        }
        protected override void ValidateCore(EntityDef owner)
        { if (!owner.HasCapability<IMotion>()) throw new ContentException(owner.Id + ": proof requires actual motion"); }
    }
    // Public-only engine-side provider, independent of the native control implementation.
    public sealed class ProofMotionExecution : Component<ProofMotionExecutionDef>, IMotionExecution
    {
        public Code CapabilityId => Def.CapabilityId;
        public ExecutionRateSnapshot ReadExecution(World world)
        {
            if (Def.Fault == 1) Entity.Touch();
            var gate = Def.Fault == 4 ? null : new ExecutionGateSnapshot(Def.Fault == 2 ? new Code("wrong") : CapabilityId,
                Def.Fault != 5, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            return new ExecutionRateSnapshot(gate, Def.Fault == 3 ? -Fix.One : Def.Multiplier);
        }
    }
}
