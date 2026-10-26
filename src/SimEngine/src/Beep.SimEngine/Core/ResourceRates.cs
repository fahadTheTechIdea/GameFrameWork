using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;

namespace Beep.SimEngine
{
    public interface IResourceRate
    {
        IResourceDefinition Resource { get; }
        Fix Quantity { get; }
        long PeriodNumerator { get; }
        long PeriodDenominator { get; }
        DurationUnit PeriodUnit { get; }
    }

    /// <summary>Immutable native resource quantity per exact rational game-time period.</summary>
    public sealed class ResourceRate : IResourceRate
    {
        public IResourceDefinition Resource { get; }
        public Fix Quantity { get; }
        public long PeriodNumerator { get; }
        public long PeriodDenominator { get; }
        public DurationUnit PeriodUnit { get; }
        public ResourceRate(IResourceDefinition resource, Fix quantity, long numerator, long denominator, DurationUnit unit)
        {
            if (resource?.QuantityUnit == null || !resource.Resource.IsValid || !quantity.IsPositive || numerator <= 0 || denominator <= 0 ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, quantity) != UnitConversionStatus.Converted)
                throw new ArgumentException("positive native resource rate and rational period required");
            GameDuration.SecondsPerUnit(unit);
            Resource = resource; Quantity = quantity; PeriodNumerator = numerator; PeriodDenominator = denominator; PeriodUnit = unit;
        }
    }

    /// <summary>Exact measured quantity per game-time unit; no nominal rate, stock or timer.</summary>
    public readonly struct MeasuredRate : IEquatable<MeasuredRate>
    {
        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }
        public DurationUnit Per { get; }
        public MeasuredRate(BigInteger numerator, BigInteger denominator, DurationUnit per)
        {
            if (numerator < 0 || denominator <= 0) throw new ArgumentException("invalid measured rate");
            GameDuration.SecondsPerUnit(per);
            BigInteger gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
            Numerator = numerator / gcd; Denominator = denominator / gcd; Per = per;
        }
        internal static MeasuredRate Calculate(BigInteger amountRaw, long elapsed, ISimulationTime time, DurationUnit per)
            => Calculate(amountRaw, elapsed, time, per, Fix.One.Raw);
        internal static MeasuredRate CalculateRaw(BigInteger amountRaw, long elapsed, ISimulationTime time, DurationUnit per)
            => Calculate(amountRaw, elapsed, time, per, BigInteger.One);
        private static MeasuredRate Calculate(BigInteger amountRaw, long elapsed, ISimulationTime time, DurationUnit per, BigInteger scale)
        {
            if (amountRaw < 0 || elapsed <= 0 || time == null || time.QuantumNumerator <= 0 || time.QuantumDenominator <= 0)
                throw new ArgumentException("measured rate requires quantity and positive actual game-time exposure");
            return new MeasuredRate(amountRaw * GameDuration.SecondsPerUnit(per) * time.QuantumDenominator,
                scale * elapsed * time.QuantumNumerator, per);
        }
        public bool Equals(MeasuredRate other) => Numerator == other.Numerator && Denominator == other.Denominator && Per == other.Per;
        public override bool Equals(object other) => other is MeasuredRate rate && Equals(rate);
        public override int GetHashCode() => Numerator.GetHashCode() ^ Denominator.GetHashCode() ^ (int)Per;
    }

    public enum ResourceRateRounding : byte { Ceiling, Floor }

    /// <summary>One cumulative native rate calculator. No timer, remainder, stock or consumption ledger.</summary>
    public static class ResourceRates
    {
        public static Bill Accumulated(IReadOnlyList<IResourceRate> rates, ISimulationTime time, ICapacityPolicy capacity,
            WorkProgress elapsed, ResourceRateRounding rounding)
        {
            if (rates == null || time == null || time.QuantumNumerator <= 0 || time.QuantumDenominator <= 0 || capacity == null ||
                rounding != ResourceRateRounding.Ceiling && rounding != ResourceRateRounding.Floor)
                throw new ArgumentException("invalid running rate context");
            var amounts = new List<ResourceAmount>(); var resources = new HashSet<ResourceId>();
            foreach (IResourceRate rate in rates)
            {
                if (rate?.Resource?.QuantityUnit == null || !rate.Resource.Resource.IsValid || !rate.Quantity.IsPositive ||
                    rate.PeriodNumerator <= 0 || rate.PeriodDenominator <= 0 || !resources.Add(rate.Resource.Resource) ||
                    UnitConversion.ValidateAmount(rate.Resource.QuantityUnit, rate.Quantity) != UnitConversionStatus.Converted)
                    throw new ArgumentException("invalid or duplicate native resource rate");
                CapacityQuantum quantum = CapacityPolicyContracts.Quantum(capacity, rate.Resource.Resource);
                if (quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0 || quantum.Floor(rate.Quantity) != rate.Quantity)
                    throw new ArgumentException("operating rate violates endpoint quantum");
                BigInteger raw = QuantizedGameRates.AccumulatedRaw(rate.Quantity, rate.PeriodNumerator, rate.PeriodDenominator,
                    rate.PeriodUnit, time, elapsed, quantum.Raw, rounding == ResourceRateRounding.Ceiling);
                if (raw > 0) amounts.Add(new ResourceAmount(rate.Resource.Resource, Fix.FromRaw(checked((long)raw))));
            }
            return new Bill(amounts);
        }
        public static Bill Between(IReadOnlyList<IResourceRate> rates, ISimulationTime time, ICapacityPolicy capacity,
            WorkProgress before, WorkProgress after, ResourceRateRounding rounding)
        {
            if (after.WholeTicks < before.WholeTicks || after.WholeTicks == before.WholeTicks && after.Fraction < before.Fraction)
                throw new ArgumentException("rate interval cannot run backwards");
            Bill previous = Accumulated(rates, time, capacity, before, rounding);
            Bill next = Accumulated(rates, time, capacity, after, rounding);
            var delta = new List<ResourceAmount>();
            foreach (ResourceAmount line in next.Lines)
            {
                Fix amount = line.Amount - previous.AmountOf(line.Resource);
                if (amount.IsPositive) delta.Add(new ResourceAmount(line.Resource, amount));
            }
            return new Bill(delta);
        }
    }
}
