using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached management reference into the original demand owner, not a second policy record.</summary>
    public readonly struct LogisticsStockPolicyTarget
    {
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public LogisticsStockPolicyTarget(Code port, ResourceEndpointId endpoint) { Port = port; Endpoint = endpoint; }
    }
}
