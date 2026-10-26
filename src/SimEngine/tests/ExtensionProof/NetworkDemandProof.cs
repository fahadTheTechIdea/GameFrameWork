using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public-contract observation/fault instrumentation; no clock, stock, allocation or execution owner.</summary>
    public sealed class NetworkDemandProbeDef : Def, INetworkDemandPolicy
    {
        public Func<NetworkDemandContext, NetworkDemandSnapshot> ObservationProbe;
        protected override void ReadCore(JsonReader reader) { }
        public NetworkDemandSnapshot Read(NetworkDemandContext context)
            => ObservationProbe == null ? new NetworkDemandSnapshot(context.Readiness, Code.None) : ObservationProbe(context);
    }
}
