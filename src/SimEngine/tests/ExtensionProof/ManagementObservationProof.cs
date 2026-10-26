using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Same processor declaration and native runtime; only transient metadata callback probes.
    public sealed class ObservedProcessorDef : IndependentProcessorDef
    {
        internal Action<string> Observe;
        public override IReadOnlyList<ITransformationDefinition> Transformations
        { get { Observe?.Invoke("support"); return base.Transformations; } }
    }
    public sealed class ManagementGateDef : ComponentDef<ManagementGate>, INamedCapabilityDefinition
    {
        public Code CapabilityId { get; private set; }
        protected override void ReadCore(JsonReader reader) => CapabilityId = new Code(reader.String("id"));
    }
    // Engine-side immutable telemetry/fault provider, not another staffing scheduler or work ledger.
    public sealed class ManagementGate : Component<ManagementGateDef>, ITransformationStaffing
    {
        private bool _probeState;
        internal Action<string> Observe;
        internal string HiddenRead;
        internal int Malformed;
        public Code CapabilityId => Def.CapabilityId;
        public ExecutionGateSnapshot ReadGate(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            Observe?.Invoke("gate");
            if (HiddenRead == "gate") _probeState = !_probeState;
            if (Malformed == 1) return null;
            return new ExecutionGateSnapshot(Malformed == 2 ? new Code("wrong.gate") : CapabilityId,
                Malformed != 3, Malformed == 3 ? new Code("invalid.inapplicable") : Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
        }
        public TransformationStaffingSnapshot ReadStaffing(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            Observe?.Invoke("staffing");
            if (HiddenRead == "staffing") _probeState = !_probeState;
            if (Malformed == 1) return null;
            return new TransformationStaffingSnapshot(Malformed == 2 ? new Code("wrong.staffing") : CapabilityId,
                Malformed != 3, Malformed == 3 ? new Code("invalid.inapplicable") : Code.None, Array.Empty<TransformationStaffingNeedSnapshot>());
        }
        public override void Sync(IStateSync sync) => sync.Bool(ref _probeState);
    }
}
