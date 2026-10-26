using System;
using System.Globalization;
using System.Numerics;

namespace Beep.SimEngine
{
    /// <summary>
    /// Q32.32 fixed-point number: the only numeric type a simulation may use for fractions. Integer
    /// arithmetic only, so every machine, runtime and compiler computes the same bits — the property
    /// lockstep multiplayer and replays depend on.
    /// </summary>
    public readonly struct Fix : IEquatable<Fix>, IComparable<Fix>
    {
        public const int FractionalBits = 32;
        private const long OneRaw = 1L << FractionalBits;

        public readonly long Raw;

        private Fix(long raw) { Raw = raw; }

        public static Fix FromRaw(long raw) => new Fix(raw);

        public static readonly Fix Zero = new Fix(0);
        public static readonly Fix One = new Fix(OneRaw);
        public static readonly Fix Half = new Fix(OneRaw >> 1);
        public static readonly Fix MaxValue = new Fix(long.MaxValue);
        public static readonly Fix MinValue = new Fix(long.MinValue);

        public static Fix FromInt(int v) => new Fix((long)v << FractionalBits);

        /// <summary>Exact rational construction: numerator/denominator without a float in between.</summary>
        public static Fix FromRatio(long numerator, long denominator)
        {
            if (denominator == 0) throw new DivideByZeroException("Fix.FromRatio denominator is zero");
            return Signed(DivideScaled(Magnitude(numerator), Magnitude(denominator)), (numerator ^ denominator) < 0);
        }

        /// <summary>
        /// Parses "12.75", "-3", "0.001", "1e3". Deterministic (no float). Rejects anything else —
        /// content with a malformed number fails to load instead of loading as a different number.
        /// </summary>
        public static Fix Parse(string s)
        {
            if (!TryParse(s, out Fix v)) throw new FormatException("not a number: '" + s + "'");
            return v;
        }

        public static bool TryParse(string s, out Fix value)
        {
            value = Zero;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '-' || s[0] == '+') { neg = s[0] == '-'; i = 1; }
            int digitsStart = i, digits = 0, firstNonzero = -1;
            for (; i < s.Length && s[i] >= '0' && s[i] <= '9'; i++)
            {
                if (s[i] != '0' && firstNonzero < 0) firstNonzero = digits;
                digits++;
            }
            if (i == digitsStart) return false;
            int integerDigits = digits;
            if (i < s.Length && s[i] == '.')
            {
                i++;
                int fracStart = i;
                for (; i < s.Length && s[i] >= '0' && s[i] <= '9'; i++)
                {
                    if (s[i] != '0' && firstNonzero < 0) firstNonzero = digits;
                    digits++;
                }
                if (i == fracStart) return false;
            }
            int mantissaEnd = i;
            long decimalShift = integerDigits;
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                i++;
                bool eneg = false;
                if (i < s.Length && (s[i] == '-' || s[i] == '+')) { eneg = s[i] == '-'; i++; }
                int expStart = i, exp = 0;
                for (; i < s.Length && s[i] >= '0' && s[i] <= '9'; i++)
                {
                    int digit = s[i] - '0';
                    if (exp > (int.MaxValue - digit) / 10) return false;
                    exp = exp * 10 + digit;
                }
                if (i == expStart) return false;
                decimalShift += eneg ? -(long)exp : exp;
            }
            if (i != s.Length) return false;
            if (firstNonzero < 0) return true;
            long wholePlaces = decimalShift - firstNonzero;
            if (wholePlaces > 10) return false;
            // Q32.32 boundaries terminate within 32 decimal places. Later digits cannot affect truncation.
            // This bounds the parsing accumulator to 42 decimal digits even for huge authored exponents.
            long keep = wholePlaces + 32;
            if (keep <= 0) return true;
            BigInteger scaled = BigInteger.Zero;
            int index = 0, used = 0;
            for (int k = digitsStart; k < mantissaEnd && used < keep; k++)
            {
                if (s[k] == '.') continue;
                if (index++ < firstNonzero) continue;
                scaled = scaled * 10 + (s[k] - '0'); used++;
            }
            while (used++ < keep) scaled *= 10;
            BigInteger raw = (scaled << FractionalBits) / DecimalScale;
            BigInteger limit = neg ? (BigInteger.One << 63) : long.MaxValue;
            if (raw > limit) return false;
            value = new Fix((long)(neg ? -raw : raw));
            return true;
        }

        private static readonly BigInteger DecimalScale = BigInteger.Pow(10, 32);

        public static Fix operator +(Fix a, Fix b) => new Fix(checked(a.Raw + b.Raw));
        public static Fix operator -(Fix a, Fix b) => new Fix(checked(a.Raw - b.Raw));
        public static Fix operator -(Fix a) => new Fix(checked(-a.Raw));

        public static Fix operator *(Fix a, Fix b)
        {
            ulong x = Magnitude(a.Raw), y = Magnitude(b.Raw);
            ulong xlo = (uint)x, xhi = x >> 32, ylo = (uint)y, yhi = y >> 32;
            ulong low = xlo * ylo;
            ulong cross = xhi * ylo + (low >> 32);
            ulong middle = (uint)cross + xlo * yhi;
            ulong high = xhi * yhi + (cross >> 32) + (middle >> 32);
            if ((high >> 32) != 0) throw new OverflowException("Fix multiplication exceeds its numeric range");
            ulong shifted = (high << 32) | (middle & 0xFFFFFFFFUL);
            bool negative = (a.Raw ^ b.Raw) < 0;
            // Preserve multiplication's arithmetic-right-shift rounding (floor, including negatives).
            if (negative && (uint)low != 0) shifted = checked(shifted + 1);
            return Signed(shifted, negative);
        }

        public static Fix operator /(Fix a, Fix b)
        {
            if (b.Raw == 0) throw new DivideByZeroException("Fix division by zero");
            return Signed(DivideScaled(Magnitude(a.Raw), Magnitude(b.Raw)), (a.Raw ^ b.Raw) < 0);
        }

        private static ulong Magnitude(long value) => value < 0 ? (ulong)(-(value + 1)) + 1 : (ulong)value;

        private static Fix Signed(ulong magnitude, bool negative)
        {
            ulong limit = negative ? 1UL << 63 : long.MaxValue;
            if (magnitude > limit) throw new OverflowException("Fix exceeds its numeric range");
            return new Fix(negative ? magnitude == 1UL << 63 ? long.MinValue : -(long)magnitude : (long)magnitude);
        }

        /// <summary>Exact unsigned 96-bit scaled division; nearest raw bit, ties away from zero.</summary>
        private static ulong DivideScaled(ulong numerator, ulong denominator)
        {
            ulong remainder = 0, quotient = 0;
            for (int bit = 95; bit >= 0; bit--)
            {
                ulong next = bit >= 32 ? (numerator >> (bit - 32)) & 1UL : 0;
                remainder = (remainder << 1) | next;
                if (remainder < denominator) continue;
                remainder -= denominator;
                if (bit >= 64) throw new OverflowException("Fix division exceeds its numeric range");
                quotient |= 1UL << bit;
            }
            if (remainder >= denominator - remainder) quotient = checked(quotient + 1);
            return quotient;
        }

        public static bool operator ==(Fix a, Fix b) => a.Raw == b.Raw;
        public static bool operator !=(Fix a, Fix b) => a.Raw != b.Raw;
        public static bool operator <(Fix a, Fix b) => a.Raw < b.Raw;
        public static bool operator >(Fix a, Fix b) => a.Raw > b.Raw;
        public static bool operator <=(Fix a, Fix b) => a.Raw <= b.Raw;
        public static bool operator >=(Fix a, Fix b) => a.Raw >= b.Raw;

        public static implicit operator Fix(int v) => FromInt(v);

        public static Fix Abs(Fix v) => v.Raw < 0 ? -v : v;
        public static Fix Min(Fix a, Fix b) => a.Raw < b.Raw ? a : b;
        public static Fix Max(Fix a, Fix b) => a.Raw > b.Raw ? a : b;
        public static Fix Clamp(Fix v, Fix lo, Fix hi) => v.Raw < lo.Raw ? lo : (v.Raw > hi.Raw ? hi : v);

        public bool IsZero => Raw == 0;
        public bool IsPositive => Raw > 0;
        public int ToIntFloor() => (int)(Raw >> FractionalBits);
        public int ToIntCeiling() => checked(ToIntFloor() + ((Raw & 0xFFFFFFFFL) == 0 ? 0 : 1));
        public int ToIntRound() => checked(ToIntFloor() + ((Raw & 0xFFFFFFFFL) >= OneRaw / 2 ? 1 : 0));

        /// <summary>
        /// Square root to the last fractional bit: the largest raw r with r² ≤ raw × 2^32, found bit by bit
        /// on the exact 96-bit product.
        /// </summary>
        public static Fix Sqrt(Fix v)
        {
            if (v.Raw < 0) throw new ArgumentException("Fix.Sqrt of a negative number");
            if (v.Raw == 0) return Zero;
            ulong nHi = (ulong)v.Raw >> 32, nLo = (ulong)v.Raw << 32;
            ulong r = 0;
            for (int b = 47; b >= 0; b--)
            {
                ulong c = r | (1UL << b);
                Square(c, out ulong hi, out ulong lo);
                if (hi < nHi || (hi == nHi && lo <= nLo)) r = c;
            }
            return new Fix((long)r);
        }

        /// <summary>c × c as a 128-bit value (c below 2^48).</summary>
        private static void Square(ulong c, out ulong hi, out ulong lo)
        {
            ulong ch = c >> 32, cl = c & 0xFFFFFFFFUL;      // c = ch·2^32 + cl
            ulong mid = (ch * cl) << 1;                     // 2·ch·cl, below 2^49
            ulong low = cl * cl + (mid << 32);              // may carry out of 64 bits
            ulong carry = low < cl * cl ? 1UL : 0UL;
            lo = low;
            hi = ch * ch + (mid >> 32) + carry;
        }

        /// <summary>Presentation only: a double must never flow back into simulation state.</summary>
        public double ToDouble() => Raw / 4294967296.0;

        public bool Equals(Fix other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fix f && f.Raw == Raw;
        public override int GetHashCode() => Raw.GetHashCode();
        public int CompareTo(Fix other) => Raw.CompareTo(other.Raw);

        /// <summary>Exact invariant decimal for authored JSON, round-tripping every native bit.</summary>
        public string ToExactString()
        {
            BigInteger scaled = BigInteger.Abs(new BigInteger(Raw)) * BigInteger.Pow(5, FractionalBits);
            string digits = scaled.ToString(CultureInfo.InvariantCulture).PadLeft(FractionalBits + 1, '0');
            string text = digits.Insert(digits.Length - FractionalBits, ".").TrimEnd('0').TrimEnd('.');
            return Raw < 0 ? "-" + text : text;
        }

        /// <summary>Invariant decimal text with three places. For logs and debugging.</summary>
        public override string ToString()
        {
            bool neg = Raw < 0;
            ulong r = Magnitude(Raw);
            ulong whole = r >> FractionalBits;
            ulong milli = ((r & 0xFFFFFFFFUL) * 1000UL) >> FractionalBits;
            return (neg ? "-" : "") + whole.ToString(CultureInfo.InvariantCulture)
                   + "." + milli.ToString("000", CultureInfo.InvariantCulture);
        }
    }
}
