using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Open pure content admission; the existing network owner retains all physical execution/state.</summary>
    public interface INetworkDemandPolicy : IContentDefinition
    {
        NetworkDemandSnapshot Read(NetworkDemandContext context);
    }
    /// <summary>Sole capacity-only demand rule; actual readiness comes from the live owner context.</summary>
    public static class CapacityNetworkDemand
    {
        public static NetworkDemandSnapshot Read(NetworkDemandContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new NetworkDemandSnapshot(context.Readiness, Code.None);
        }
    }
    public readonly struct NetworkDemandSnapshot
    {
        public Fix Maximum { get; }
        public Code Blocker { get; }
        public NetworkDemandSnapshot(Fix maximum, Code blocker)
        {
            if (maximum < Fix.Zero || !blocker.IsNone && !maximum.IsZero) throw new ArgumentException("blocked demand must be zero; quantities must be nonnegative");
            Maximum = maximum; Blocker = blocker;
        }
    }
    public sealed class CapacityNetworkDemandPolicyDef : Def, INetworkDemandPolicy
    {
        protected override void ReadCore(JsonReader reader) { }
        public NetworkDemandSnapshot Read(NetworkDemandContext context) => CapacityNetworkDemand.Read(context);
    }
    public sealed class UtilityNetworkDemandPolicyDef : Def, INetworkDemandPolicy
    {
        public static readonly Code Range = new Code("network.demand-range");
        public IReadOnlyList<Code> Consumers { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            string[] names = reader.Strings("consumers");
            if (names.Length == 0) throw reader.Error("utility network demand requires an explicit nonempty consumers group");
            var consumers = new List<Code>(); var distinct = new HashSet<Code>();
            try
            {
                foreach (string name in names)
                {
                    ContractIdentity.ValidateId(name, "network utility consumer"); var consumer = new Code(name);
                    if (!distinct.Add(consumer)) throw reader.Error("duplicate network utility consumer '" + name + "'");
                    consumers.Add(consumer);
                }
            }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
            consumers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Consumers = consumers.AsReadOnly();
        }
        public NetworkDemandSnapshot Read(NetworkDemandContext context)
        {
            if (!context.Pull) return new NetworkDemandSnapshot(context.Readiness, Code.None);
            Fix requested = Fix.Zero; bool declared = false, active = false, anyDrawing = false; ResourceQuantitySnapshot quantity = default;
            foreach (Code consumer in Consumers)
            {
                Code refusal = context.ReadUtility(consumer, out var demand, out var current);
                if (!refusal.IsNone) return new NetworkDemandSnapshot(Fix.Zero, refusal);
                quantity = current; // Every observation is guarded against the same actual receiving endpoint.
                anyDrawing |= demand.Drawing;
                bool memberDeclares = false;
                foreach (ResourceId resource in demand.Resources) if (resource == context.Resource) memberDeclares = true;
                declared |= memberDeclares;
                if (!memberDeclares || !demand.Drawing) continue;
                active = true;
                if (!demand.NextBlocker.IsNone) return new NetworkDemandSnapshot(Fix.Zero, demand.NextBlocker);
                try { requested += demand.NextInputs.AmountOf(context.Resource); }
                catch (OverflowException) { return new NetworkDemandSnapshot(Fix.Zero, Range); }
            }
            if (!active) return new NetworkDemandSnapshot(Fix.Zero, !anyDrawing || declared ? UtilityCodes.Inactive : ResourceSignalCodes.Incompatible);
            Fix missing = quantity.Available >= requested ? Fix.Zero : requested - quantity.Available;
            Fix unpromised = quantity.Inbound >= missing ? Fix.Zero : missing - quantity.Inbound;
            return new NetworkDemandSnapshot(context.Quantum.Floor(Fix.Min(context.Readiness, unpromised)), Code.None);
        }
    }
    /// <summary>One pure admission capability; temporary observations never become another demand store.</summary>
    public sealed class NetworkDemandContext
    {
        internal delegate Code UtilityReader(Code consumer, out UtilityDemandSnapshot demand, out ResourceQuantitySnapshot quantity);
        private readonly UtilityReader _utility;
        private bool _active = true;
        private readonly Fix _readiness;
        private readonly CapacityQuantum _quantum;
        public string Network { get; }
        public EntityId Entity { get; }
        public PlayerId Company { get; }
        public ResourceId Resource { get; }
        public bool Pull { get; }
        public Fix Readiness { get { RequireActive(); return _readiness; } }
        public CapacityQuantum Quantum { get { RequireActive(); return _quantum; } }
        internal NetworkDemandContext(string network, EntityId entity, PlayerId company, ResourceId resource, bool pull,
            Fix readiness, CapacityQuantum quantum, UtilityReader utility)
        { Network = network; Entity = entity; Company = company; Resource = resource; Pull = pull; _readiness = readiness; _quantum = quantum; _utility = utility; }
        private void RequireActive() { if (!_active) throw new InvalidOperationException("network demand context has expired"); }
        public Code ReadUtility(Code consumer, out UtilityDemandSnapshot demand, out ResourceQuantitySnapshot quantity)
        {
            RequireActive();
            if (consumer.IsNone) throw new ArgumentException("utility consumer identity is required", nameof(consumer));
            return _utility(consumer, out demand, out quantity);
        }
        internal void Close() { _active = false; }
    }
}
