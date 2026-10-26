using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;

namespace Beep.SimEngine
{
    /// <summary>Stateless engine-side admission policy; the calling inventory retains all balances.</summary>
    public interface ICapacityPolicy
    {
        string Id { get; }
        IReadOnlyList<CapacityLimit> Limits { get; }
        CapacityQuantum Quantum(ResourceId resource);
        CapacityCheck Check(IReadOnlyList<ResourceAmount> stock,
            IReadOnlyList<ResourceAmount> promised, IReadOnlyList<ResourceAmount> incoming, long instances = 1);
        CapacityOffer Offer(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1);
    }

    /// <summary>Authored immutable policy identity, distinct from runtime scaled admission policies.</summary>
    public interface ICapacityPolicyDefinition : IContentDefinition, ICapacityPolicy
    {
        new string Id { get; }
    }

    public readonly struct CapacityLimit
    {
        public IUnitDefinition Unit { get; }
        public Fix Amount { get; }
        public CapacityLimit(IUnitDefinition unit, Fix amount)
        {
            UnitConversionStatus status = UnitConversion.ValidateAmount(unit, amount);
            if (status != UnitConversionStatus.Converted) throw new ArgumentException("invalid capacity limit: " + status);
            Unit = unit; Amount = amount;
        }
    }

    public enum CapacityStatus : byte
    {
        InvalidRequest, Fits, LimitExceeded, UnknownResource, InvalidAmount, FractionalDiscrete,
        MissingMeasure, InvalidUnit, Unrepresentable, OutOfRange, ResourceRejected
    }

    /// <summary>A bounded native-resource quantity, not untyped free space or an acquired commitment.</summary>
    public readonly struct CapacityOffer
    {
        public CapacityStatus Status { get; }
        public Fix Amount { get; }
        public CapacityOffer(CapacityStatus status, Fix amount = default) { Status = status; Amount = amount; }
    }

    /// <summary>Exact native-resource step; a default result refuses. No live capacity or balance.</summary>
    public readonly struct CapacityQuantum
    {
        public CapacityStatus Status { get; }
        public long Raw { get; }
        public CapacityQuantum(CapacityStatus status, long raw = 0) { Status = status; Raw = raw; }
        public static CapacityQuantum Combine(params CapacityQuantum[] quanta)
        {
            if (quanta == null) throw new ArgumentNullException(nameof(quanta));
            if (quanta.Length == 0) return default;
            BigInteger common = BigInteger.One;
            foreach (CapacityQuantum quantum in quanta)
            {
                if (quantum.Status != CapacityStatus.Fits) return new CapacityQuantum(quantum.Status);
                if (quantum.Raw <= 0) return default;
                common = common / BigInteger.GreatestCommonDivisor(common, quantum.Raw) * quantum.Raw;
                if (common > long.MaxValue) return new CapacityQuantum(CapacityStatus.OutOfRange);
            }
            return new CapacityQuantum(CapacityStatus.Fits, (long)common);
        }
        public Fix Floor(Fix ceiling) => Status == CapacityStatus.Fits && Raw > 0 && ceiling.IsPositive
            ? Fix.FromRaw(ceiling.Raw / Raw * Raw) : Fix.Zero;
    }

    /// <summary>Detached facts for one dimension. There is intentionally no dimensionless total.</summary>
    public readonly struct CapacityUsage
    {
        public UnitId Unit { get; }
        public Fix Limit { get; }
        public Fix Stock { get; }
        public Fix Promised { get; }
        public Fix Incoming { get; }
        public Fix Projected { get; }
        public CapacityUsage(UnitId unit, Fix limit, Fix stock, Fix promised, Fix incoming, Fix projected)
        { Unit = unit; Limit = limit; Stock = stock; Promised = promised; Incoming = incoming; Projected = projected; }
    }

    /// <summary>Immutable evaluation, not a reservation/transaction receipt. A default result refuses.</summary>
    public readonly struct CapacityCheck
    {
        private readonly IReadOnlyList<CapacityUsage> _usage;
        public CapacityStatus Status { get; }
        public bool Fits => Status == CapacityStatus.Fits;
        public ResourceId Resource { get; }
        public UnitId Unit { get; }
        public IReadOnlyList<CapacityUsage> Usage => _usage ?? Array.Empty<CapacityUsage>();
        public CapacityCheck(CapacityStatus status, ResourceId resource = default, UnitId unit = default,
            IReadOnlyList<CapacityUsage> usage = null)
        {
            Status = status; Resource = resource; Unit = unit;
            var copy = new CapacityUsage[usage?.Count ?? 0];
            for (int i = 0; i < copy.Length; i++) copy[i] = usage[i];
            _usage = Array.AsReadOnly(copy);
        }
    }
}
