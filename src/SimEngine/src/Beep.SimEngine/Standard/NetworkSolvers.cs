using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Engine-side immutable solver content; execution uses only the bounded owner context.</summary>
    public interface INetworkSolver : IContentDefinition
    {
        void ValidateTime(ISimulationTime time);
        void Advance(NetworkExecutionContext context);
    }
    /// <summary>Sole standard transfer algorithm; independent content may select it without inheriting native metadata.</summary>
    public static class ResourceTransferNetworkSolver
    {
        public static void ValidateTime(ISimulationTime time)
        { if (time == null) throw new ArgumentNullException(nameof(time)); }
        public static void Advance(NetworkExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            foreach (NetworkFlowRequest request in context.Candidates) context.Transfer(request, request.Candidate.Requested);
        }
    }
    public sealed class ResourceTransferNetworkSolverDef : Def, INetworkSolver
    {
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateTime(ISimulationTime time) => ResourceTransferNetworkSolver.ValidateTime(time);
        public void Advance(NetworkExecutionContext context) => ResourceTransferNetworkSolver.Advance(context);
    }
    /// <summary>Detached due leg, not a stock promise. Only its creating live context can attempt it.</summary>
    public sealed class NetworkFlowRequest
    {
        public NetworkFlowCandidate Candidate { get; }
        public ResourceEndpointId Source { get; }
        public ResourceEndpointId Destination { get; }
        internal NetworkFlowRequest(NetworkFlowCandidate candidate, ResourceEndpointId source, ResourceEndpointId destination)
        { Candidate = candidate; Source = source; Destination = destination; }
    }
    public readonly struct NetworkFlowAttempt
    {
        public Fix Amount { get; }
        public Code Blocker { get; }
        public bool Transferred => Amount.IsPositive;
        internal NetworkFlowAttempt(Fix amount, Code blocker) { Amount = amount; Blocker = blocker; }
    }
    /// <summary>One Update's capability, never a session, clock, queue, stock owner or persistent state.</summary>
    public sealed class NetworkExecutionContext
    {
        public static readonly Code Unavailable = new Code("sim.network.flow-unavailable");
        private bool _active = true, _executing;
        private readonly HashSet<NetworkFlowRequest> _requests, _attempted = new HashSet<NetworkFlowRequest>();
        private readonly Action _validate;
        private readonly Func<NetworkFlowRequest, Fix, NetworkFlowAttempt> _transfer;
        public string Network { get; }
        /// <summary>Timestamp of this completing step from the one game clock, not independently advancing time.</summary>
        public long CompletedAt { get; }
        /// <summary>The session's immutable duration converter; it cannot advance or configure the clock.</summary>
        public ISimulationTime Time { get; }
        public IReadOnlyList<NetworkFlowRequest> Candidates { get; }
        internal NetworkExecutionContext(string network, long completedAt, ISimulationTime time, IEnumerable<NetworkFlowRequest> requests,
            Action validate, Func<NetworkFlowRequest, Fix, NetworkFlowAttempt> transfer)
        {
            Network = network; CompletedAt = completedAt; Time = time;
            Candidates = new List<NetworkFlowRequest>(requests).AsReadOnly(); _requests = new HashSet<NetworkFlowRequest>(Candidates);
            _validate = validate; _transfer = transfer;
        }
        public NetworkFlowAttempt Transfer(NetworkFlowRequest request, Fix maximum)
        {
            if (!_active) throw new InvalidOperationException("network execution context has expired");
            if (_executing) throw new InvalidOperationException("network transfers cannot recursively execute");
            if (request == null || !_requests.Contains(request)) throw new ArgumentException("network request belongs to another context", nameof(request));
            if (!maximum.IsPositive || maximum > request.Candidate.Requested || maximum.Raw % request.Candidate.QuantumRaw != 0)
                throw new ArgumentException("network attempt must fit the positive earned whole-quantum allowance", nameof(maximum));
            _validate();
            if (!_attempted.Add(request)) throw new InvalidOperationException("network request was already attempted this step");
            _executing = true;
            try { return _transfer(request, maximum); }
            finally { _executing = false; }
        }
        internal void Close() { _active = false; }
    }
}
