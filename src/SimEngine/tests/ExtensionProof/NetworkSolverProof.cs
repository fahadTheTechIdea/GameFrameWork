using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent engine-side solver content; no graph, stock, clock or state store.</summary>
    public sealed class LimitedNetworkSolverDef : Def, INetworkSolver
    {
        private BillSpec _limits;
        private Bill _resolved;
        private GameDuration _interval;
        protected override void ReadCore(JsonReader reader)
        {
            _limits = BillSpec.Read(reader, "limits"); if (_limits.IsEmpty) throw reader.Error("solver limits must be explicit native quantities");
            _interval = reader.Child("interval").RationalDuration(); if (_interval.IsZero) throw reader.Error("solver interval must be positive");
        }
        protected override void ResolveCore(ContentResolver content) => _resolved = _limits.Resolve(content);
        public void ValidateTime(ISimulationTime time)
        {
            ResourceTransferNetworkSolver.ValidateTime(time);
            try { if (time.ExactTicks(_interval) <= 0) throw new ContentException(Id + ": solver interval must resolve in the actual session"); }
            catch (ArgumentException error) { throw new ContentException(Id + ": invalid solver interval: " + error.Message); }
            catch (OverflowException error) { throw new ContentException(Id + ": solver interval range: " + error.Message); }
        }
        public void Advance(NetworkExecutionContext context)
        {
            if (context.CompletedAt % context.Time.ExactTicks(_interval) != 0) return;
            foreach (NetworkFlowRequest request in context.Candidates)
                foreach (ResourceAmount limit in _resolved.Lines)
                    if (limit.Resource == request.Candidate.Resource)
                    {
                        long raw = Fix.Min(limit.Amount, request.Candidate.Requested).Raw;
                        raw -= raw % request.Candidate.QuantumRaw;
                        if (raw > 0) context.Transfer(request, Fix.FromRaw(raw));
                    }
        }
    }
    /// <summary>Hostile provider/lifetime instrumentation only, no alternate game rules.</summary>
    public sealed class SolverProbeDef : Def, INetworkSolver
    {
        public Action<NetworkExecutionContext> ExecutionProbe;
        public void ValidateTime(ISimulationTime time) => ResourceTransferNetworkSolver.ValidateTime(time);
        protected override void ReadCore(JsonReader reader) { }
        public void Advance(NetworkExecutionContext context) => ExecutionProbe?.Invoke(context);
    }
}
