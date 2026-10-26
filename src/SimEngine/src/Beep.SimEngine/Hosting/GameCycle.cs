using System;

namespace Beep.SimEngine.Hosting
{
    /// <summary>The simulation lifecycle: initialize once, then update. Platform drawing is not an engine phase.</summary>
    public interface IGameCycle
    {
        bool Initialized { get; }
        IGameClock Clock { get; }
        GameUpdate Time { get; }
        GameSnapshot Snapshot { get; }
        event Action<GameUpdate> Updated;
        void Initialize();
        int Update();
        int Update(long platformElapsedMicroseconds);
    }

    /// <summary>The immutable timing and result of one Update call with its settled simulation snapshot.</summary>
    public readonly struct GameUpdate
    {
        internal GameUpdate(long startTick, long tick, long elapsedMicroseconds, Fix alpha, ISimulationTime time)
        {
            StartTick = startTick;
            Tick = tick;
            ElapsedMicroseconds = elapsedMicroseconds;
            Alpha = alpha;
            ElapsedGameTime = GameDuration.FromSteps(checked(tick - startTick), time);
        }

        public long StartTick { get; }
        public long Tick { get; }
        /// <summary>Actual simulated duration completed by this Update; platform delta is only pacing input.</summary>
        public GameDuration ElapsedGameTime { get; }
        /// <summary>Work completed by this bounded Update call, not the simulation's age.</summary>
        public int TicksRun => checked((int)(Tick - StartTick));
        /// <summary>-1 for engine-time updates; otherwise the embedding platform's reported elapsed time.</summary>
        public long ElapsedMicroseconds { get; }
        public Fix Alpha { get; }
    }
}
