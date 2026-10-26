using System;

namespace Beep.SimEngine
{
    /// <summary>Episode history, not response progress. Minus one explicitly means not raised/resolved.</summary>
    public readonly struct IncidentLifecycleSnapshot
    {
        public Code Kind { get; }
        public int Episode { get; }
        public long RaisedTick { get; }
        public long ResolvedTick { get; }
        public bool Active => Episode > 0 && ResolvedTick == -1;
        public IncidentLifecycleSnapshot(Code kind, int episode, long raised, long resolved)
        {
            if (kind.IsNone || episode < 0 || episode == 0 && (raised != -1 || resolved != -1) ||
                episode > 0 && (raised < 0 || resolved < -1 || resolved != -1 && resolved < raised))
                throw new ArgumentException("invalid incident lifecycle episode/timestamps");
            Kind = kind; Episode = episode; RaisedTick = raised; ResolvedTick = resolved;
        }
    }

    public readonly struct IncidentSnapshot
    {
        public Code Kind { get; }
        public long RaisedTick { get; }
        public int Episode { get; }
        public bool Active => !Kind.IsNone;
        private IncidentSnapshot(Code kind, long tick, int episode) { Kind = kind; RaisedTick = tick; Episode = episode; }
        public static IncidentSnapshot Inactive() => new IncidentSnapshot(Code.None, -1, 0);
        public static IncidentSnapshot Raised(Code kind, long tick, int episode)
        {
            if (kind.IsNone || tick < 0 || episode < 1) throw new ArgumentException("active incident requires a kind, nonnegative tick and positive episode");
            return new IncidentSnapshot(kind, tick, episode);
        }
        internal bool IsValid => Active ? RaisedTick >= 0 && Episode > 0 : RaisedTick == -1 && Episode == 0;
    }
}
