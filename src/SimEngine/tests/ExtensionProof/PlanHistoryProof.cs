using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public-only example: authored group retention reads the actual board, not a copied history.</summary>
    public sealed class ProofPlanHistoryRetentionDef : ComponentDef<ProofPlanHistoryRetention>
    {
        public string Group { get; private set; }
        public bool Enabled { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Group = reader.String("group"); Enabled = reader.Bool("enabled"); if (string.IsNullOrWhiteSpace(Group)) throw reader.Error("explicit retained group required"); }
    }
    public sealed class ProofPlanHistoryRetention : Component<ProofPlanHistoryRetentionDef>, IPlanHistoryRetention
    {
        // Test instrumentation only; not game logic, authored state or another clock.
        public Action ReadFault;
        private bool _enabled;
        public override void OnCreate() => _enabled = Def.Enabled;
        public override void Sync(IStateSync sync) => sync.Bool(ref _enabled);
        public void CorruptReadState() => _enabled = !_enabled;
        public bool RetainsPlan(World world, PlayerId company, long plan)
        {
            ReadFault?.Invoke();
            return _enabled && company == Entity.Owner && ProofPlanReferenceState.Matches(world, company, plan, Def.Group);
        }
    }
}
