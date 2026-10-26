using System;
using System.Numerics;

namespace Beep.SimEngine.Kernel
{
    public enum ScheduleKind : byte
    {
        /// <summary>Runs every tick.</summary>
        EveryTick,
        /// <summary>Runs once every <c>Period</c> ticks, on ticks where tick % Period == Phase.</summary>
        Periodic,
        /// <summary>
        /// Runs every tick over one bucket of its entities (entity id % Period == tick % Period), so each
        /// entity is visited once per <c>Period</c> ticks and the cost is spread evenly (RimWorld TickList).
        /// </summary>
        Spread
    }

    /// <summary>When a system's work is due, in game time.</summary>
    public readonly struct Schedule
    {
        public readonly ScheduleKind Kind;
        public readonly long Period;
        public readonly long Phase;

        private Schedule(ScheduleKind kind, long period, long phase)
        {
            if (period < 1) throw new ArgumentOutOfRangeException(nameof(period));
            Kind = kind;
            Period = period;
            Phase = phase;
        }

        public static readonly Schedule EveryTick = new Schedule(ScheduleKind.EveryTick, 1, 0);
        public static Schedule Every(long ticks, long phase = 0)
        {
            if (ticks < 1) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (phase < 0) throw new ArgumentOutOfRangeException(nameof(phase));
            return new Schedule(ScheduleKind.Periodic, ticks, phase % ticks);
        }
        public static Schedule Every(ISimulationTime time, GameDuration duration, long phase = 0) => Every(time.ExactTicks(duration), phase);
        public static Schedule Spread(int ticks) => new Schedule(ScheduleKind.Spread, ticks, 0);

        public bool IsDue(long tick) => Kind != ScheduleKind.Periodic || tick % Period == Phase;

        public override string ToString() => Kind + "/" + Period;
    }

    /// <summary>What a system is told about the tick it is running in.</summary>
    public readonly struct TickInfo
    {
        /// <summary>The tick being simulated.</summary>
        public readonly long Tick;
        /// <summary>Game ticks one visit to an entity stands for: 1 for every-tick systems, the period otherwise.</summary>
        public readonly long Span;
        /// <summary>For spread systems: the bucket to visit this tick.</summary>
        public readonly int Bucket;

        public TickInfo(long tick, long span, int bucket, ISimulationTime time)
        {
            Tick = tick;
            Span = span;
            Bucket = bucket;
            Time = time ?? throw new ArgumentNullException(nameof(time));
        }

        /// <summary>A per-tick amount scaled to the span this visit covers.</summary>
        public Fix Over(Fix perTick) => Fix.FromRaw((long)((BigInteger)perTick.Raw * Span));

        public ISimulationTime Time { get; }
        public CalendarReading Date => Time.Calendar(Tick);
    }

    /// <summary>
    /// One concern of the simulation (XNA's GameComponent, for the world). Systems run in a fixed,
    /// explicit order every due tick; they hold the logic, components hold the data. A system's own
    /// state, if any, is saved and hashed through <see cref="Sync"/>.
    /// </summary>
    public abstract class GameSystem
    {
        /// <summary>Stable id: names the system in saves, faults and hash breakdowns.</summary>
        public abstract string Id { get; }

        /// <summary>Explicit version of this system's Sync layout, including stateless systems.</summary>
        public abstract int StateSchemaVersion { get; }

        public virtual Schedule GetSchedule(ISimulationTime time) => Schedule.EveryTick;

        /// <summary>Called once when the world is built or loaded: build derived indexes. Event declarations/subscriptions belong to module composition.</summary>
        public virtual void Initialize(World world) { }

        /// <summary>Called once for a new game, after world generation, before the first tick.</summary>
        public virtual void OnNewGame(World world) { }

        public abstract void Update(World world, TickInfo tick);

        /// <summary>Engine tick-boundary bookkeeping after all systems, events and entity updates.
        /// Stateful owners finalize their existing stores here, never while another owner iterates them.</summary>
        public virtual void CompleteUpdate(World world) { }

        /// <summary>Saves, loads or hashes the system's own state. Most systems have none.</summary>
        public virtual void Sync(IStateSync s) { }

        /// <summary>After a load: rebuild derived caches from the loaded state.</summary>
        public virtual void OnLoaded(World world) { }

        public override string ToString() => Id;
    }

    /// <summary>A service with state of its own (an economy ledger, a research table).</summary>
    public interface IPersistentService
    {
        string Id { get; }
        int StateSchemaVersion { get; }
        void Sync(IStateSync s);
    }
}
