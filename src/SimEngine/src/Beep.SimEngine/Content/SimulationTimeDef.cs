using System;
using System.Collections.Generic;
using System.Numerics;

namespace Beep.SimEngine.Content
{
    public sealed class SimulationTimeDef : Def
    {
        public long QuantumNumerator { get; private set; }
        public long QuantumDenominator { get; private set; }
        public int FrameTicks { get; private set; }
        public int TicksPerSecond { get; private set; }
        public GameDuration? Horizon { get; private set; }
        public ICalendar Calendar { get; private set; }
        private string _calendar;
        protected override void ReadCore(JsonReader r)
        {
            JsonReader quantum = r.Child("quantum") ?? throw r.Error("missing quantum");
            long numerator = quantum.Long("numerator"), denominator = quantum.Long("denominator");
            DurationUnit unit = JsonReader.TimeUnit(quantum.String("unit"), quantum); quantum.End();
            if (numerator <= 0 || denominator <= 0) throw r.Error("quantum numerator and denominator must be positive");
            BigInteger scaled = (BigInteger)numerator * GameDuration.SecondsPerUnit(unit);
            BigInteger common = BigInteger.GreatestCommonDivisor(scaled, denominator);
            try { QuantumNumerator = (long)(scaled / common); QuantumDenominator = (long)(denominator / common); }
            catch (OverflowException) { throw r.Error("quantum seconds are not representable"); }
            FrameTicks = r.Int("frame_ticks"); TicksPerSecond = r.Int("ticks_per_second");
            if (FrameTicks < 1 || TicksPerSecond < 1) throw r.Error("frame_ticks and ticks_per_second must be positive");
            JsonValue horizon = r.Value("horizon");
            Horizon = horizon == null || horizon.Kind == JsonKind.Null ? (GameDuration?)null : new JsonReader(horizon, r.Where + ".horizon").RationalDuration();
            JsonValue calendar = r.Value("calendar");
            if (calendar != null && calendar.Kind != JsonKind.Null && calendar.Kind != JsonKind.String) throw r.Error("calendar must name a definition or be null");
            _calendar = calendar?.Kind == JsonKind.String ? calendar.Text : null;
            if (_calendar != null && string.IsNullOrWhiteSpace(_calendar)) throw r.Error("calendar must name a definition or be null");
        }
        protected override void ResolveCore(ContentResolver c) => Calendar = c.OptionalDef<CalendarDef>(_calendar);
    }
    public abstract class CalendarDef : Def, ICalendar
    {
        public abstract void Validate(ISimulationTime time);
        public abstract CalendarReading Project(ISimulationTime time, long tick);
    }
    /// <summary>Optional JSON-authored cycles; modules can register other calendar providers in owned sections.</summary>
    public sealed class CycleCalendarDef : CalendarDef
    {
        public readonly struct Field
        {
            internal Field(string id, GameDuration duration, long radix, long origin)
            { Id = id; Duration = duration; Radix = radix; Origin = origin; }
            public string Id { get; }
            public GameDuration Duration { get; }
            public long Radix { get; }
            public long Origin { get; }
        }
        public IReadOnlyList<Field> Fields { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            JsonValue fields = r.Array("fields") ?? throw r.Error("missing calendar fields");
            if (fields.Count == 0) throw r.Error("calendar fields cannot be empty");
            var result = new List<Field>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < fields.Count; i++)
            {
                var field = new JsonReader(fields[i], r.Where + ".fields[" + i + "]");
                string id = field.String("id"); JsonReader duration = field.Child("duration") ?? throw field.Error("missing duration");
                long radix = field.Long("radix"), origin = field.Long("origin");
                if (string.IsNullOrEmpty(id) || !ids.Add(id) || radix < 0 || origin < 0) throw field.Error("unique field id and nonnegative radix/origin required");
                result.Add(new Field(id, duration.RationalDuration(), radix, origin)); field.End();
            }
            Fields = result.AsReadOnly();
        }
        public override void Validate(ISimulationTime time)
        {
            for (int i = 0; i < Fields.Count; i++)
                if (time.ExactTicks(Fields[i].Duration) <= 0) throw new ContentException(Id + ": calendar unit must span positive exact ticks");
        }
        public override CalendarReading Project(ISimulationTime time, long tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            var values = new List<CalendarField>();
            for (int i = 0; i < Fields.Count; i++)
            {
                Field field = Fields[i]; long total = tick / time.ExactTicks(field.Duration);
                long value = checked((field.Radix == 0 ? total : total % field.Radix) + field.Origin);
                values.Add(new CalendarField(field.Id, value, total));
            }
            return new CalendarReading(Id, tick, values);
        }
    }
}
