using System;
using System.Numerics;
using Beep.SimEngine.Content;

namespace Beep.SimEngine
{
    /// <summary>Stable authored quantity-unit identity, independent of a session catalog position.</summary>
    public readonly struct UnitId : IEquatable<UnitId>, IComparable<UnitId>
    {
        public string Id { get; }
        public bool IsValid => Id != null;
        public UnitId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("a unit needs an identity", nameof(id));
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '.' || c == '_' || c == '-'))
                    throw new ArgumentException("unit identities use lowercase letters, digits, underscore, hyphen or dot", nameof(id));
            }
            Id = id;
        }
        public bool Equals(UnitId other) => string.Equals(Id, other.Id, StringComparison.Ordinal);
        public override bool Equals(object other) => other is UnitId unit && Equals(unit);
        public override int GetHashCode() => Id == null ? 0 : StringComparer.Ordinal.GetHashCode(Id);
        public int CompareTo(UnitId other) => string.CompareOrdinal(Id, other.Id);
        public static bool operator ==(UnitId a, UnitId b) => a.Equals(b);
        public static bool operator !=(UnitId a, UnitId b) => !a.Equals(b);
        public override string ToString() => Id ?? "none";
    }

    public interface IUnitDefinition
    {
        UnitId Unit { get; }
        string Dimension { get; }
        long ScaleNumerator { get; }
        long ScaleDenominator { get; }
        bool Discrete { get; }
    }
    /// <summary>Actual authored unit content; pure runtime unit values need no catalog identity.</summary>
    public interface IUnitContentDefinition : IContentDefinition, IUnitDefinition { }

    public readonly struct UnitQuantity
    {
        public UnitId Unit { get; }
        public Fix Amount { get; }
        public UnitQuantity(UnitId unit, Fix amount) { Unit = unit; Amount = amount; }
    }
    public enum UnitConversionStatus : byte
    {
        Converted, InvalidUnit, InvalidAmount, IncompatibleDimension, FractionalDiscrete, Unrepresentable, OutOfRange
    }
    public readonly struct UnitConversionResult
    {
        public UnitConversionStatus Status { get; }
        public Fix Amount { get; }
        internal UnitConversionResult(UnitConversionStatus status, Fix amount = default) { Status = status; Amount = amount; }
    }

    /// <summary>Exact dimension-preserving conversion; no density inference, rounding or mutable state.</summary>
    public static class UnitConversion
    {
        private static bool Valid(IUnitDefinition unit) => unit != null && unit.Unit.IsValid &&
            !string.IsNullOrWhiteSpace(unit.Dimension) && unit.ScaleNumerator > 0 && unit.ScaleDenominator > 0;
        public static UnitConversionStatus ValidateAmount(IUnitDefinition unit, Fix amount)
        {
            if (!Valid(unit)) return UnitConversionStatus.InvalidUnit;
            if (amount < Fix.Zero) return UnitConversionStatus.InvalidAmount;
            if (unit.Discrete && amount.Raw % Fix.One.Raw != 0) return UnitConversionStatus.FractionalDiscrete;
            return UnitConversionStatus.Converted;
        }
        public static UnitConversionResult Convert(Fix amount, IUnitDefinition from, IUnitDefinition to)
        {
            UnitConversionStatus status = ValidateAmount(from, amount);
            if (status != UnitConversionStatus.Converted) return new UnitConversionResult(status);
            if (!Valid(to)) return new UnitConversionResult(UnitConversionStatus.InvalidUnit);
            if (!string.Equals(from.Dimension, to.Dimension, StringComparison.Ordinal))
                return new UnitConversionResult(UnitConversionStatus.IncompatibleDimension);
            BigInteger numerator = (BigInteger)amount.Raw * from.ScaleNumerator * to.ScaleDenominator;
            BigInteger denominator = (BigInteger)from.ScaleDenominator * to.ScaleNumerator;
            BigInteger raw = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            if (remainder != 0) return new UnitConversionResult(UnitConversionStatus.Unrepresentable);
            if (raw > long.MaxValue) return new UnitConversionResult(UnitConversionStatus.OutOfRange);
            Fix converted = Fix.FromRaw((long)raw);
            status = ValidateAmount(to, converted);
            return new UnitConversionResult(status, status == UnitConversionStatus.Converted ? converted : Fix.Zero);
        }
    }
}
