using System;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached current feasible facts; no executable endpoint, world, inventory or job handle.</summary>
    public readonly struct RecoveryDestinationFacts
    {
        public PlayerId Company { get; }
        public EntityId Source { get; }
        public EntityId Destination { get; }
        public EntityId Carrier { get; }
        public ResourceEndpointId SourceEndpoint { get; }
        public Code DestinationPort { get; }
        public ResourceEndpointId DestinationEndpoint { get; }
        public ResourceId Resource { get; }
        public Fix FeasibleAmount { get; }
        public CapacityQuantum QuantityQuantum { get; }
        public Fix DemandGap { get; }
        /// <summary>Already acquired delivery capacity at this exact endpoint; never new demand.</summary>
        public Fix CommittedAmount { get; }
        public int DemandPriority { get; }
        public long TravelCost { get; }
        public bool HasDemand => DemandGap.IsPositive;
        public RecoveryDestinationFacts(PlayerId company, EntityId source, EntityId destination, EntityId carrier,
            ResourceId resource, Fix feasible, CapacityQuantum quantityQuantum, Fix gap, int priority, long travel,
            ResourceEndpointId sourceEndpoint, Code destinationPort, ResourceEndpointId destinationEndpoint, Fix committedAmount)
        {
            if (!company.IsValid || !source.IsValid || !destination.IsValid || !carrier.IsValid || !resource.IsValid ||
                !sourceEndpoint.IsValid || !destinationEndpoint.IsValid || sourceEndpoint == destinationEndpoint ||
                !feasible.IsPositive || quantityQuantum.Status != CapacityStatus.Fits || quantityQuantum.Raw <= 0 ||
                quantityQuantum.Floor(feasible) != feasible || gap < Fix.Zero || priority < 0 || travel < 0 ||
                committedAmount < Fix.Zero || committedAmount > feasible || quantityQuantum.Floor(committedAmount) != committedAmount)
                throw new ArgumentException("invalid recovery destination facts");
            Company = company; Source = source; Destination = destination; Carrier = carrier; Resource = resource;
            SourceEndpoint = sourceEndpoint; DestinationPort = destinationPort; DestinationEndpoint = destinationEndpoint;
            CommittedAmount = committedAmount;
            FeasibleAmount = feasible; QuantityQuantum = quantityQuantum; DemandGap = gap; DemandPriority = priority; TravelCost = travel;
        }
    }
    public readonly struct RecoveryDestinationDecision
    {
        public bool Allowed { get; }
        public Fix Ceiling { get; }
        public long Preference { get; }
        public Code Blocker { get; }
        public RecoveryDestinationDecision(Fix ceiling, long preference)
        {
            if (!ceiling.IsPositive || preference < 0) throw new ArgumentException("recovery needs positive ceiling and nonnegative preference");
            Allowed = true; Ceiling = ceiling; Preference = preference; Blocker = Code.None;
        }
        public RecoveryDestinationDecision(Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("refused recovery needs a typed blocker");
            Allowed = false; Ceiling = Fix.Zero; Preference = 0; Blocker = blocker;
        }
    }
    public interface IRecoveryDestinationPolicy : IContentDefinition
    {
        RecoveryDestinationDecision Evaluate(RecoveryDestinationFacts facts);
    }
    public sealed class AuthoredRecoveryDestinationPolicyDef : Def, IRecoveryDestinationPolicy
    {
        private RecoveryDestinationDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = RecoveryDestinationDeclaration.Read(reader);
        public RecoveryDestinationDecision Evaluate(RecoveryDestinationFacts facts) => _declaration.Evaluate(facts);
    }
    /// <summary>One immutable authored mode and native quantized decision, shared by content implementations.</summary>
    public sealed class RecoveryDestinationDeclaration
    {
        public static readonly Code NoDemand = new Code("logistics.recovery-no-demand");
        public static readonly Code QuantumGap = new Code("logistics.recovery-quantum-gap");
        private readonly string _mode;
        private RecoveryDestinationDeclaration(string mode) { _mode = mode; }
        public static RecoveryDestinationDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit recovery destination mode required");
            string mode = reader.String("mode");
            if (mode != "nearest_available" && mode != "demand_first" && mode != "demand_only")
                throw reader.Error("explicit recovery destination mode must be nearest_available, demand_first or demand_only");
            return new RecoveryDestinationDeclaration(mode);
        }
        public RecoveryDestinationDecision Evaluate(RecoveryDestinationFacts facts)
        {
            if (_mode == "nearest_available") return new RecoveryDestinationDecision(facts.FeasibleAmount, 0);
            // A valid acquired delivery is not a new stock request. Losing its physical site
            // cannot withdraw its actual capacity promise or manufacture another demand gap.
            if (facts.CommittedAmount.IsPositive)
                return new RecoveryDestinationDecision(facts.CommittedAmount, (long)facts.DemandPriority + 1);
            if (facts.HasDemand)
            {
                Fix amount = facts.QuantityQuantum.Floor(Fix.Min(facts.FeasibleAmount, facts.DemandGap));
                return amount.IsPositive ? new RecoveryDestinationDecision(amount, (long)facts.DemandPriority + 1)
                    : new RecoveryDestinationDecision(QuantumGap);
            }
            return _mode == "demand_first" ? new RecoveryDestinationDecision(facts.FeasibleAmount, 0) : new RecoveryDestinationDecision(NoDemand);
        }
    }
}
