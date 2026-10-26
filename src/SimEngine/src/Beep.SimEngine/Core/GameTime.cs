using System;
using System.Globalization;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    public enum DurationUnit { Second, Minute, Hour, Day }

    /// <summary>Exact authored duration, independent of any session tick quantum.</summary>
    public readonly struct GameDuration
    {
        internal string Literal { get; }
        internal long Scale { get; }
        internal long Divisor { get; }
        public bool IsZero => Literal == null || ZeroLiteral(Literal);
        public static readonly GameDuration Zero = default;
        public GameDuration(long numerator, long denominator, DurationUnit unit)
            : this(numerator.ToString(CultureInfo.InvariantCulture), denominator, unit) { }
        private GameDuration(string literal, long divisor, DurationUnit unit)
        {
            if (!Json.IsNumberLiteral(literal)) throw new FormatException("invalid duration number");
            if (literal[0] == '-' && !ZeroLiteral(literal)) throw new ArgumentOutOfRangeException(nameof(literal));
            if (divisor < 1) throw new ArgumentOutOfRangeException(nameof(divisor));
            Literal = literal; Divisor = divisor; Scale = SecondsPerUnit(unit);
        }
        private GameDuration(string literal, long divisor, long scale)
        { Literal = literal; Divisor = divisor; Scale = scale; }
        public static GameDuration Decimal(string literal, DurationUnit unit) => new GameDuration(literal, 1, unit);
        public static GameDuration Seconds(long value) => new GameDuration(value, 1, DurationUnit.Second);
        public static GameDuration Minutes(long value) => new GameDuration(value, 1, DurationUnit.Minute);
        public static GameDuration Hours(long value) => new GameDuration(value, 1, DurationUnit.Hour);
        /// <summary>An elapsed day is 24 hours; optional calendars define their own displayed cycles.</summary>
        public static GameDuration Days(long value) => new GameDuration(value, 1, DurationUnit.Day);
        internal static GameDuration FromSteps(long steps, ISimulationTime time)
        {
            if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
            if (time == null) throw new ArgumentNullException(nameof(time));
            return new GameDuration(((BigInteger)steps * time.QuantumNumerator).ToString(CultureInfo.InvariantCulture),
                time.QuantumDenominator, DurationUnit.Second);
        }
        public GameDuration Times(long count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return IsZero || count == 0 ? Zero : new GameDuration(Literal, Divisor, checked(Scale * count));
        }
        /// <summary>Exact duration value codec. It stores no current time and advances no clock.</summary>
        public static GameDuration Sync(IStateSync state, GameDuration value)
        {
            string literal = value.Literal; long scale = value.Scale, divisor = value.Divisor;
            state.String(ref literal); state.Long(ref scale); state.Long(ref divisor);
            if (literal == null)
            {
                if (scale != 0 || divisor != 0) throw new CorruptStateException("invalid zero duration representation");
                return Zero;
            }
            try
            {
                if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
                // Validate through the existing strict constructor; preserve even authored zero literals.
                var checkedValue = new GameDuration(literal, divisor, DurationUnit.Second);
                return new GameDuration(checkedValue.Literal, checkedValue.Divisor, scale);
            }
            catch (ArgumentException) { throw new CorruptStateException("invalid exact duration representation"); }
            catch (FormatException) { throw new CorruptStateException("invalid exact duration number"); }
        }
        internal static long SecondsPerUnit(DurationUnit unit)
        {
            switch (unit)
            {
                case DurationUnit.Second: return 1;
                case DurationUnit.Minute: return 60;
                case DurationUnit.Hour: return 3600;
                case DurationUnit.Day: return 86400;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
        private static bool ZeroLiteral(string literal)
        {
            for (int i = 0; i < literal.Length && literal[i] != 'e' && literal[i] != 'E'; i++)
                if (literal[i] >= '1' && literal[i] <= '9') return false;
            return true;
        }
    }

    public interface ISimulationTime
    {
        string Id { get; }
        long QuantumNumerator { get; }
        long QuantumDenominator { get; }
        long Ticks(GameDuration duration);
        long Ticks(GameDuration duration, long numerator, long denominator);
        long ExactTicks(GameDuration duration);
        long Hours(long hours);
        long Days(long days);
        Fix RateFor(Fix perUnit, DurationUnit unit, long ticks = 1);
        CalendarReading Calendar(long tick);
    }

    /// <summary>The one immutable time converter selected by the authored session definition.</summary>
    public sealed class GameTime : ISimulationTime
    {
        private readonly ICalendar _calendar;
        public GameTime(SimulationTimeDef definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.QuantumNumerator <= 0 || definition.QuantumDenominator <= 0)
                throw new ArgumentException("time definition must be loaded and resolved", nameof(definition));
            Id = definition.Id; QuantumNumerator = definition.QuantumNumerator;
            QuantumDenominator = definition.QuantumDenominator; _calendar = definition.Calendar;
            try { _calendar?.Validate(this); }
            catch (ArgumentException ex) { throw new ContentException(Id + ": calendar is not aligned to this quantum: " + ex.Message); }
            catch (OverflowException ex) { throw new ContentException(Id + ": calendar exceeds the tick range: " + ex.Message); }
        }
        public string Id { get; }
        public long QuantumNumerator { get; }
        public long QuantumDenominator { get; }
        public long Hours(long hours) => Ticks(GameDuration.Hours(hours));
        public long Days(long days) => Ticks(GameDuration.Days(days));
        public long Ticks(GameDuration duration) => Convert(duration, false, 1, 1);
        public long Ticks(GameDuration duration, long numerator, long denominator)
        {
            if (numerator < 0 || denominator <= 0) throw new ArgumentOutOfRangeException(nameof(numerator));
            return numerator == 0 ? 0 : Convert(duration, false, numerator, denominator);
        }
        public long ExactTicks(GameDuration duration) => Convert(duration, true, 1, 1);
        public CalendarReading Calendar(long tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            return _calendar?.Project(this, tick);
        }
        public Fix RateFor(Fix perUnit, DurationUnit unit, long ticks = 1)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            BigInteger raw = (BigInteger)perUnit.Raw * QuantumNumerator * ticks;
            return Fix.FromRaw((long)(raw / ((BigInteger)QuantumDenominator * GameDuration.SecondsPerUnit(unit))));
        }
        private long Convert(GameDuration duration, bool exact, long numerator, long denominator)
        {
            if (duration.IsZero) return 0;
            string literal = duration.Literal;
            int exponentAt = literal.IndexOfAny(new[] { 'e', 'E' });
            int end = exponentAt < 0 ? literal.Length : exponentAt;
            int dot = literal.IndexOf('.');
            int decimals = dot >= 0 && dot < end ? end - dot - 1 : 0;
            string digits = literal.Substring(0, end).Replace(".", "").TrimStart('0');
            long exponent = 0;
            if (exponentAt >= 0 && !long.TryParse(literal.Substring(exponentAt + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
            {
                if (literal[exponentAt + 1] == '-') return SubTick(exact);
                throw new OverflowException("duration exceeds the tick representation");
            }
            BigInteger multiplier = (BigInteger)duration.Scale * QuantumDenominator * numerator;
            BigInteger divisor = (BigInteger)duration.Divisor * QuantumNumerator * denominator;
            // Linear digit traversal with bounded carry, never an input-sized power of ten.
            char[] product = new char[checked(digits.Length + multiplier.ToString(CultureInfo.InvariantCulture).Length)];
            int pos = product.Length; BigInteger carry = 0;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                BigInteger next = (digits[i] - '0') * multiplier + carry;
                product[--pos] = (char)('0' + (int)(next % 10)); carry = next / 10;
            }
            while (carry > 0) { product[--pos] = (char)('0' + (int)(carry % 10)); carry /= 10; }
            long places;
            try { places = checked(product.Length - pos + checked(exponent - decimals)); }
            catch (OverflowException) { if (exponent < 0) return SubTick(exact); throw; }
            if (places <= 0) return SubTick(exact);
            if (places > (divisor * long.MaxValue).ToString(CultureInfo.InvariantCulture).Length)
                throw new OverflowException("duration exceeds the tick representation");
            BigInteger whole = 0;
            for (int i = 0; i < places; i++) whole = whole * 10 + (pos + i < product.Length ? product[pos + i] - '0' : 0);
            bool fraction = false;
            for (int i = pos + (int)places; i < product.Length; i++) if (product[i] != '0') { fraction = true; break; }
            if (exact && (fraction || whole % divisor != 0)) throw new ArgumentException("duration is not an exact number of session ticks");
            return (long)((whole + divisor - 1 + (fraction ? 1 : 0)) / divisor);
        }
        private static long SubTick(bool exact)
        {
            if (exact) throw new ArgumentException("duration is not an exact number of session ticks");
            return 1;
        }
    }
}
