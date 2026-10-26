using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    public interface ICalendar
    {
        void Validate(ISimulationTime time);
        CalendarReading Project(ISimulationTime time, long tick);
    }
    public readonly struct CalendarField
    {
        public CalendarField(string id, long value, long total) { Id = id; Value = value; Total = total; }
        public string Id { get; }
        public long Value { get; }
        public long Total { get; }
    }
    /// <summary>Detached named calendar facts; no compulsory year/day/hour layout.</summary>
    public sealed class CalendarReading
    {
        public CalendarReading(string calendar, long tick, IEnumerable<CalendarField> fields)
        {
            if (string.IsNullOrEmpty(calendar)) throw new ArgumentException("calendar identity is required", nameof(calendar));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            Calendar = calendar; Tick = tick;
            Fields = new List<CalendarField>(fields ?? throw new ArgumentNullException(nameof(fields))).AsReadOnly();
        }
        public string Calendar { get; }
        public long Tick { get; }
        public IReadOnlyList<CalendarField> Fields { get; }
        public CalendarField this[string field]
        {
            get
            {
                for (int i = 0; i < Fields.Count; i++) if (Fields[i].Id == field) return Fields[i];
                throw new ArgumentException("unknown calendar field '" + field + "'", nameof(field));
            }
        }
    }
}
