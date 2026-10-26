using System;
using System.Numerics;

namespace Beep.SimEngine.Kernel
{
    public enum SpatialResourceChangeStatus : byte
    {
        InvalidRequest, NotQuantity, InvalidLocation, InvalidAmount, InsufficientQuantity,
        Unrepresentable, Ready, Committed, ReservedQuantity
    }

    /// <summary>Detached admission/commit facts, never a receipt or authority to spend a retained preview.</summary>
    public readonly struct SpatialResourceChange
    {
        private SpatialResourceChange(SpatialResourceChangeStatus status, UnitConversionStatus conversion,
            UnitId nativeUnit = default, Fix amount = default, Fix before = default, Fix after = default,
            Fix storedBefore = default, Fix storedAfter = default)
        {
            Status = status; Conversion = conversion; NativeUnit = nativeUnit; Amount = amount;
            Before = before; After = after; StoredBefore = storedBefore; StoredAfter = storedAfter;
        }
        public SpatialResourceChangeStatus Status { get; }
        public UnitConversionStatus Conversion { get; }
        public UnitId NativeUnit { get; }
        public Fix Amount { get; }
        public Fix Before { get; }
        public Fix After { get; }
        public Fix StoredBefore { get; }
        public Fix StoredAfter { get; }
        public bool Ready => Status == SpatialResourceChangeStatus.Ready;
        public bool Committed => Status == SpatialResourceChangeStatus.Committed;
        public static SpatialResourceChange Refused(SpatialResourceChangeStatus status,
            UnitConversionStatus conversion = UnitConversionStatus.InvalidAmount)
        {
            if (status == SpatialResourceChangeStatus.Ready || status == SpatialResourceChangeStatus.Committed ||
                !Enum.IsDefined(typeof(SpatialResourceChangeStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            return new SpatialResourceChange(status, conversion);
        }
        internal static SpatialResourceChange Admitted(UnitId unit, Fix amount, Fix before, Fix after,
            Fix storedBefore, Fix storedAfter) => new SpatialResourceChange(SpatialResourceChangeStatus.Ready,
                UnitConversionStatus.Converted, unit, amount, before, after, storedBefore, storedAfter);
        /// <summary>The implementing owner calls this only after its actual exact write has committed.</summary>
        public SpatialResourceChange AfterCommit()
        {
            if (!Ready) throw new InvalidOperationException("only an admitted actual quantity change can report a commit");
            return new SpatialResourceChange(SpatialResourceChangeStatus.Committed, Conversion,
                NativeUnit, Amount, Before, After, StoredBefore, StoredAfter);
        }
    }

    /// <summary>
    /// Optional engine-side stock primitive. Geometry, values, invalidation and Sync remain with the
    /// actual layer owner. Not a player command or timed extraction/transfer workflow; receipt-based
    /// coordinators must account for any material moved into inventory. Host views do not expose this.
    /// </summary>
    public interface ISpatialResourceQuantity<TLocation> : ISpatialLayer
    {
        void BindResourceCell(TLocation location, ISpatialResourceCellGuard cell);
        UnitQuantity ReadQuantity(TLocation location);
        SpatialResourceChange CheckWithdrawal(TLocation location, Fix nativeAmount);
        /// <summary>Pure immediate write preparation while resource books are settled. This grants no
        /// receipt authority: Commit must recheck the actual quantity guards and prepared revisions.</summary>
        SpatialResourceChangeCommit PrepareWithdrawal(TLocation location, Fix nativeAmount);
        SpatialResourceChange Withdraw(TLocation location, Fix nativeAmount);
        SpatialResourceChange CheckDeposit(TLocation location, Fix nativeAmount);
        SpatialResourceChangeCommit PrepareDeposit(TLocation location, Fix nativeAmount);
        SpatialResourceChange Deposit(TLocation location, Fix nativeAmount);
    }

    /// <summary>Single-use, unsaved preparation over the actual provider. No stock, book or fallback owner.</summary>
    public sealed class SpatialResourceChangeCommit
    {
        private readonly Action _commit;
        private bool _used;
        public SpatialResourceChange Check { get; }
        public SpatialResourceChangeCommit(SpatialResourceChange check, Action commit = null)
        {
            if (check.Committed || check.Ready != (commit != null)) throw new ArgumentException("quantity preparation needs an exact ready write or refusal");
            Check = check; _commit = commit;
        }
        public SpatialResourceChange Commit()
        {
            if (_used) throw new InvalidOperationException("spatial quantity preparation already consumed");
            _used = true;
            if (!Check.Ready) return Check;
            _commit(); return Check.AfterCommit();
        }
    }

    /// <summary>Material legality only. Spatial providers alone own geometry, values, invalidation and Sync.</summary>
    public static class SpatialResourceValues
    {
        public static UnitConversionStatus CheckValue(ISpatialLayer layer, Fix value)
        {
            if (layer == null) return UnitConversionStatus.InvalidUnit;
            if (layer.ResourceMeasure == SpatialResourceMeasure.None || layer.ResourceMeasure == SpatialResourceMeasure.Associated)
                return UnitConversionStatus.Converted; // Signed non-material measurements retain their own semantics.
            UnitConversionStatus shape = CheckStorage(layer.ValueType, value);
            return shape == UnitConversionStatus.Converted ? ConvertQuantity(layer, value).Status : shape;
        }

        /// <summary>Exact native resource quantity. No conversion is inferred for associated or unlinked layers.</summary>
        public static UnitConversionResult ConvertQuantity(ISpatialLayer layer, Fix value)
            => layer?.ResourceMeasure == SpatialResourceMeasure.Quantity
                ? UnitConversion.Convert(value, layer.Unit, layer.Resource?.QuantityUnit)
                : new UnitConversionResult(UnitConversionStatus.InvalidUnit);

        /// <summary>Actual native material facts; invalid/unsupported live data never becomes a zero fallback.</summary>
        public static UnitQuantity ReadQuantity(ISpatialLayer layer, Fix stored)
        {
            if (layer == null || layer.ResourceMeasure != SpatialResourceMeasure.Quantity || layer.Resource == null)
                throw new InvalidOperationException("spatial layer does not contain material quantities");
            if (CheckValue(layer, stored) != UnitConversionStatus.Converted)
                throw new CorruptStateException("invalid existing spatial material: " + layer.Id);
            return new UnitQuantity(layer.Resource.QuantityUnit.Unit, ConvertQuantity(layer, stored).Amount);
        }

        /// <summary>Exact native debit and representable stored remainder. No mutation, rounding or unit inference.</summary>
        public static SpatialResourceChange CheckWithdrawal(ISpatialLayer layer, Fix stored, Fix nativeAmount)
            =>CheckChange(layer,stored,nativeAmount,false);
        public static SpatialResourceChange CheckDeposit(ISpatialLayer layer,Fix stored,Fix nativeAmount)
            =>CheckChange(layer,stored,nativeAmount,true);
        private static SpatialResourceChange CheckChange(ISpatialLayer layer,Fix stored,Fix nativeAmount,bool deposit)
        {
            if (layer == null || layer.ResourceMeasure != SpatialResourceMeasure.Quantity || layer.Resource == null)
                return SpatialResourceChange.Refused(SpatialResourceChangeStatus.NotQuantity, UnitConversionStatus.InvalidUnit);
            UnitQuantity current = ReadQuantity(layer, stored);
            UnitConversionStatus amountStatus = UnitConversion.ValidateAmount(layer.Resource.QuantityUnit, nativeAmount);
            if (!nativeAmount.IsPositive || amountStatus != UnitConversionStatus.Converted)
                return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InvalidAmount,
                    nativeAmount.IsPositive ? amountStatus : UnitConversionStatus.InvalidAmount);
            Fix before = current.Amount;
            if (!deposit&&nativeAmount > before) return SpatialResourceChange.Refused(SpatialResourceChangeStatus.InsufficientQuantity);
            Fix after;
            try{after=deposit?before+nativeAmount:before-nativeAmount;}
            catch(OverflowException){return SpatialResourceChange.Refused(SpatialResourceChangeStatus.Unrepresentable,UnitConversionStatus.OutOfRange);}
            UnitConversionResult remainder = UnitConversion.Convert(after, layer.Resource.QuantityUnit, layer.Unit);
            UnitConversionStatus shape = remainder.Status == UnitConversionStatus.Converted
                ? CheckStorage(layer.ValueType, remainder.Amount) : remainder.Status;
            if (shape != UnitConversionStatus.Converted)
                return SpatialResourceChange.Refused(SpatialResourceChangeStatus.Unrepresentable, shape);
            return SpatialResourceChange.Admitted(layer.Resource.QuantityUnit.Unit, nativeAmount, before, after, stored, remainder.Amount);
        }

        /// <summary>Largest exact native prefix fitting both endpoint and actual stored representation.</summary>
        public static Fix DepositOffer(ISpatialLayer layer, Fix quantity, Fix reserved, Fix ceiling, CapacityQuantum capacityQuantum)
        {
            if (layer?.ResourceMeasure != SpatialResourceMeasure.Quantity || layer.Resource == null || !ceiling.IsPositive ||
                capacityQuantum.Status != CapacityStatus.Fits || capacityQuantum.Raw <= 0) return Fix.Zero;
            var native = layer.Resource.QuantityUnit; var stored = layer.Unit;
            BigInteger numerator = (BigInteger)native.ScaleNumerator * stored.ScaleDenominator;
            BigInteger denominator = (BigInteger)native.ScaleDenominator * stored.ScaleNumerator;
            BigInteger divisor = denominator * (layer.ValueType == SpatialLayerValueType.Fixed && !stored.Discrete ? 1 : Fix.One.Raw);
            BigInteger storageQuantum = divisor / BigInteger.GreatestCommonDivisor(numerator, divisor);
            BigInteger quantum = storageQuantum / BigInteger.GreatestCommonDivisor(storageQuantum, capacityQuantum.Raw) * capacityQuantum.Raw;
            BigInteger maxStored = layer.ValueType == SpatialLayerValueType.Byte ? (BigInteger)byte.MaxValue * Fix.One.Raw
                : layer.ValueType == SpatialLayerValueType.Int32 ? (BigInteger)int.MaxValue * Fix.One.Raw : long.MaxValue;
            BigInteger maxNative = maxStored * denominator / numerator;
            BigInteger room = BigInteger.Min(ceiling.Raw, BigInteger.Min(long.MaxValue, maxNative) - quantity.Raw - reserved.Raw);
            BigInteger offered = room > 0 ? room / quantum * quantum : BigInteger.Zero;
            return offered > 0 && offered <= long.MaxValue ? Fix.FromRaw((long)offered) : Fix.Zero;
        }

        private static UnitConversionStatus CheckStorage(SpatialLayerValueType type, Fix value)
        {
            if (value < Fix.Zero) return UnitConversionStatus.InvalidAmount;
            if (type == SpatialLayerValueType.Fixed) return UnitConversionStatus.Converted;
            if (type != SpatialLayerValueType.Byte && type != SpatialLayerValueType.Int32) return UnitConversionStatus.InvalidUnit;
            if (value.Raw % Fix.One.Raw != 0) return UnitConversionStatus.FractionalDiscrete;
            return value > Fix.FromInt(type == SpatialLayerValueType.Byte ? byte.MaxValue : int.MaxValue)
                ? UnitConversionStatus.OutOfRange : UnitConversionStatus.Converted;
        }
    }
}
