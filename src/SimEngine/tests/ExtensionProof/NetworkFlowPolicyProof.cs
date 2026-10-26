using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent immutable policy content, compiled against public contracts only.</summary>
    public sealed class ProofNetworkFlowPolicyDef : Def, INetworkFlowPolicy
    {
        private string _ranking;
        protected override void ReadCore(JsonReader reader)
        {
            _ranking = reader.String("ranking");
            if (_ranking != "highest-identity" && _ranking != "equal") throw reader.Error("proof ranking must be explicit");
        }
        public long Rank(NetworkFlowCandidate candidate, EntityId lastServed)
            => _ranking == "equal" ? 0 : -(long)candidate.Entity.Value;
    }

    /// <summary>Fault injection only. No alternate stock, flow or arbitration scheduler.</summary>
    public sealed class FaultNetworkFlowPolicyDef : Def, INetworkFlowPolicy
    {
        public Action ReadFault;
        protected override void ReadCore(JsonReader reader) { }
        public long Rank(NetworkFlowCandidate candidate, EntityId lastServed)
        { ReadFault?.Invoke(); return 0; }
    }
}
