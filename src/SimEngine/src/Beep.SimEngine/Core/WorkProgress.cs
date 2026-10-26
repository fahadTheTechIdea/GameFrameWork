using System;
using System.Numerics;

namespace Beep.SimEngine
{
    /// <summary>Nonnegative elapsed work: signed 64-bit whole ticks plus exact Q32 fractional ticks.</summary>
    public readonly struct WorkProgress : IEquatable<WorkProgress>
    {
        public long WholeTicks { get; }
        public uint Fraction { get; }
        public static readonly WorkProgress Zero = default;

        public WorkProgress(long wholeTicks, uint fraction = 0)
        {
            if (wholeTicks < 0) throw new ArgumentOutOfRangeException(nameof(wholeTicks));
            WholeTicks = wholeTicks; Fraction = fraction;
        }

        public bool Complete(long requiredTicks)
        {
            if (requiredTicks < 0) throw new ArgumentOutOfRangeException(nameof(requiredTicks));
            return WholeTicks >= requiredTicks;
        }

        /// <summary>Advances once, stopping at the actual requirement instead of overflowing on overshoot.</summary>
        public WorkProgress Advance(Fix work, long requiredTicks)
        {
            if (work < Fix.Zero) throw new ArgumentOutOfRangeException(nameof(work));
            if (requiredTicks < 0) throw new ArgumentOutOfRangeException(nameof(requiredTicks));
            if (Complete(requiredTicks)) return new WorkProgress(requiredTicks);
            long whole = work.Raw >> Fix.FractionalBits;
            ulong fraction = (ulong)Fraction + (uint)work.Raw;
            whole += (long)(fraction >> Fix.FractionalBits);
            if (whole >= requiredTicks - WholeTicks) return new WorkProgress(requiredTicks);
            return new WorkProgress(checked(WholeTicks + whole), (uint)fraction);
        }

        public WorkProgress AdvanceUnbounded(Fix work)
        {
            if (work < Fix.Zero) throw new ArgumentOutOfRangeException(nameof(work));
            ulong fraction = (ulong)Fraction + (uint)work.Raw;
            long whole = checked(WholeTicks + (work.Raw >> Fix.FractionalBits) + (long)(fraction >> Fix.FractionalBits));
            return new WorkProgress(whole, (uint)fraction);
        }

        /// <summary>Exact remaining Q32 work, without narrowing full-width whole ticks.</summary>
        public BigInteger RemainingRaw(long requiredTicks)
        {
            if (requiredTicks < 0) throw new ArgumentOutOfRangeException(nameof(requiredTicks));
            BigInteger remaining = ((BigInteger)requiredTicks - WholeTicks) << Fix.FractionalBits;
            remaining -= Fraction;
            if (remaining.Sign < 0) throw new InvalidOperationException("earned work exceeds its declared requirement");
            return remaining;
        }

        /// <summary>Exact productive updates at a fixed rate, not a supply/delivery or wall-clock ETA.</summary>
        public bool TryRemainingUpdates(Fix rate, long requiredTicks, out long updates)
        {
            updates = 0;
            BigInteger remaining = RemainingRaw(requiredTicks);
            if (remaining.IsZero) return true;
            if (rate <= Fix.Zero) return false;
            BigInteger count = (remaining + rate.Raw - 1) / rate.Raw;
            if (count > long.MaxValue) return false;
            updates = (long)count; return true;
        }

        public bool TryAsFix(out Fix value)
        {
            if (WholeTicks > int.MaxValue) { value = default; return false; }
            value = Fix.FromRaw((WholeTicks << Fix.FractionalBits) | Fraction); return true;
        }

        /// <summary>Detached presentation fraction. No narrowing of whole work into a Fix quantity.</summary>
        public Fix FractionOf(long requiredTicks)
        {
            if (requiredTicks < 0) throw new ArgumentOutOfRangeException(nameof(requiredTicks));
            if (requiredTicks == 0 || Complete(requiredTicks)) return Fix.One;
            BigInteger raw = ((BigInteger)WholeTicks << Fix.FractionalBits) + Fraction;
            return Fix.FromRaw((long)(raw / requiredTicks));
        }

        public bool Equals(WorkProgress other) => WholeTicks == other.WholeTicks && Fraction == other.Fraction;
        public override bool Equals(object obj) => obj is WorkProgress other && Equals(other);
        public override int GetHashCode() => WholeTicks.GetHashCode() ^ Fraction.GetHashCode();
        public override string ToString() => WholeTicks + "+" + Fraction + "/4294967296 ticks";
    }
}
