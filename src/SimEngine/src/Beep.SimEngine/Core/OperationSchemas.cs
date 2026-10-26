using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>Resolved catalog unit for a quantity signal, never a quantity or live balance.</summary>
    public interface IAutomationQuantitySignalDefinition
    {
        IUnitDefinition QuantityUnit { get; }
    }
    /// <summary>A provider-declared metric contract, not a value or an executable reader.</summary>
    public readonly struct AutomationMetricSchema
    {
        public Code Metric { get; }
        public AutomationSignalKind Kind { get; }
        public Code Unit { get; }
        /// <summary>Requires an explicit catalog-owned unit on the authored signal definition.</summary>
        public bool UsesAuthoredUnit => Unit.IsNone;
        private AutomationMetricSchema(Code metric)
        {
            ContractIdentity.ValidateId(metric.Id, "signal metric");
            Metric = metric; Kind = AutomationSignalKind.Number; Unit = Code.None;
        }
        public static AutomationMetricSchema Quantity(Code metric) => new AutomationMetricSchema(metric);
        public AutomationMetricSchema(Code metric, AutomationSignalKind kind, Code unit)
        {
            ContractIdentity.ValidateId(metric.Id, "signal metric");
            ContractIdentity.ValidateId(unit.Id, "signal unit");
            if (kind != AutomationSignalKind.Number && kind != AutomationSignalKind.Boolean)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == AutomationSignalKind.Boolean && unit != new Code("unit.boolean"))
                throw new ArgumentException("boolean metrics use unit.boolean", nameof(unit));
            Metric = metric; Kind = kind; Unit = unit;
        }
    }

    public sealed class OperationProviderSchema
    {
        internal OperationProviderSchema(StateCodecRegistration stateCodec, int version, bool controllable)
        {
            ContractIdentity.Validate(stateCodec.Id, version, "operation provider");
            StateCodec = stateCodec; Version = version; Controllable = controllable;
        }
        public StateCodecRegistration StateCodec { get; }
        public int Version { get; }
        public bool Controllable { get; }
        internal void SyncIdentity(IStateSync s)
        {
            int version = Version; bool controllable = Controllable;
            s.Int(ref version); s.Bool(ref controllable);
            if (version != Version || controllable != Controllable)
                throw new CorruptStateException("operation provider schema differs: " + StateCodec.Id);
        }
    }

    public sealed class AutomationSignalProviderSchema
    {
        internal AutomationSignalProviderSchema(StateCodecRegistration stateCodec, int version, IReadOnlyList<AutomationMetricSchema> bindings)
        {
            ContractIdentity.Validate(stateCodec.Id, version, "signal provider");
            if (bindings == null || bindings.Count == 0) throw new ArgumentException("a signal provider declares its metrics", nameof(bindings));
            var copy = new List<AutomationMetricSchema>(bindings.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bindings.Count; i++)
            {
                AutomationMetricSchema binding = bindings[i];
                // Revalidate default structs and capture values, never retain the provider's list.
                if (binding.UsesAuthoredUnit && binding.Kind != AutomationSignalKind.Number)
                    throw new ArgumentException("authored quantity units require a numeric metric");
                copy.Add(binding.UsesAuthoredUnit ? AutomationMetricSchema.Quantity(binding.Metric)
                    : new AutomationMetricSchema(binding.Metric, binding.Kind, binding.Unit));
                if (!seen.Add(binding.Metric.Id)) throw new ArgumentException("duplicate provider metric '" + binding.Metric.Id + "'");
            }
            copy.Sort((a, b) => string.CompareOrdinal(a.Metric.Id, b.Metric.Id));
            StateCodec = stateCodec; Version = version; Bindings = copy.AsReadOnly();
        }
        public StateCodecRegistration StateCodec { get; }
        public int Version { get; }
        public IReadOnlyList<AutomationMetricSchema> Bindings { get; }
        public bool TryGet(Code metric, out AutomationMetricSchema binding)
        {
            for (int i = 0; i < Bindings.Count; i++)
                if (Bindings[i].Metric == metric) { binding = Bindings[i]; return true; }
            binding = default; return false;
        }
        internal void SyncIdentity(IStateSync s)
        {
            int version = Version; s.Int(ref version);
            if (version != Version || s.Count(Bindings.Count) != Bindings.Count)
                throw new CorruptStateException("signal provider schema differs: " + StateCodec.Id);
            for (int i = 0; i < Bindings.Count; i++)
            {
                AutomationMetricSchema binding = Bindings[i];
                string metric = binding.Metric.Id, unit = binding.Unit.Id; int kind = (int)binding.Kind;
                s.String(ref metric); s.Int(ref kind); s.String(ref unit);
                if (metric != binding.Metric.Id || kind != (int)binding.Kind || unit != binding.Unit.Id)
                    throw new CorruptStateException("signal metric schema differs: " + StateCodec.Id + ":" + binding.Metric.Id);
            }
        }
    }
}
