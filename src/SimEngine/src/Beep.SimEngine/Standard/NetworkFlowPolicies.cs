using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached due-service facts. No stock owner, entity or session handle is exposed.</summary>
    public readonly struct NetworkFlowCandidate
    {
        public EntityId Entity { get; }
        public PlayerId Company { get; }
        public ResourceId Resource { get; }
        public bool Pull { get; }
        public Fix Requested { get; }
        public long QuantumRaw { get; }
        public long EligibleTicks { get; }
        public NetworkFlowCandidate(EntityId entity, PlayerId company, ResourceId resource, bool pull,
            Fix requested, long quantumRaw, long eligibleTicks)
        {
            if (!entity.IsValid || !resource.IsValid || !requested.IsPositive || quantumRaw <= 0 || eligibleTicks <= 0)
                throw new ArgumentException("network candidate requires actual positive due-service facts");
            Entity = entity; Company = company; Resource = resource; Pull = pull;
            Requested = requested; QuantumRaw = quantumRaw; EligibleTicks = eligibleTicks;
        }
    }

    /// <summary>Pure immutable content ranking. Lower rank wins; the engine breaks ties by entity ID.</summary>
    public interface INetworkFlowPolicy : IContentDefinition
    {
        long Rank(NetworkFlowCandidate candidate, EntityId lastServed);
    }

    /// <summary>Successful-service round-robin. Idle ticks and failed attempts cannot rotate order.</summary>
    public static class RoundRobinNetworkRanking
    {
        public static long Rank(NetworkFlowCandidate candidate, EntityId lastServed)
            => (long)candidate.Entity.Value + (lastServed.IsValid && candidate.Entity.Value <= lastServed.Value ? int.MaxValue : 0L);
    }

    public sealed class RoundRobinNetworkFlowPolicyDef : Def, INetworkFlowPolicy
    {
        protected override void ReadCore(JsonReader reader) { }
        public long Rank(NetworkFlowCandidate candidate, EntityId lastServed)
            => RoundRobinNetworkRanking.Rank(candidate, lastServed);
    }
}
