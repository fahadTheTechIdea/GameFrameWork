using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>
    /// The session's sole clock: authoritative game time and update pacing. It never reads a wall clock.
    /// Fixed ticks are exact time coordinates, not independent clocks. GameTime only converts units.
    ///
    /// - An engine-time update (<see cref="SimGame.Update()"/>) runs <see cref="TicksPerFrame"/> ×
    ///   <see cref="Speed"/> ticks — none while paused. This is how Beep.SimEngine's own app, demos and headless
    ///   runs move: frame by frame, deterministic, as fast as frames come.
    /// - A host that owns its own real loop
    ///   passes the frame time it measured to <see cref="SimGame.Update(long)"/>; the clock turns it into
    ///   whole ticks at <see cref="TicksPerSecond"/> × Speed with an exact integer accumulator and a catch-up
    ///   budget, retaining any backlog. <see cref="Alpha"/> reports only the fractional next tick.
    ///
    /// Either way, speed changes how many ticks run, never what a tick does.
    /// </summary>
    public sealed class GameClock : IGameClock
    {
        public const long MicrosecondsPerSecond = 1000000L;
        public const int DefaultCatchUpCap = 64;

        private int _ticksPerFrame;
        private int _ticksPerSecond;
        private int _speed = 1;
        private bool _paused;
        private int _catchUpCap = DefaultCatchUpCap;
        private long _budget;

        internal GameClock(SimulationTimeDef definition)
        {
            Time = new GameTime(definition ?? throw new ArgumentNullException(nameof(definition)));
            TicksPerFrame = definition.FrameTicks;
            TicksPerSecond = definition.TicksPerSecond;
        }

        public long StepIndex { get; private set; }
        public ISimulationTime Time { get; }
        public GameDuration TotalGameTime => GameDuration.FromSteps(StepIndex, Time);
        public GameDuration StepDuration => GameDuration.FromSteps(1, Time);
        public CalendarReading Date => Time.Calendar(StepIndex);

        internal long NextStep() => checked(StepIndex + 1);
        internal void CompleteStep(long completed)
        {
            if (completed != NextStep()) throw new InvalidOperationException("clock completion must advance exactly one game update");
            StepIndex = completed;
        }

        /// <summary>The sole current-time codec, embedded once by World. Local pacing is not simulation age.</summary>
        internal void Sync(IStateSync state)
        {
            long step = StepIndex;
            state.Long(ref step);
            if (step < 0) throw new CorruptStateException("game clock time must be nonnegative");
            if (state.Reading) StepIndex = step;
        }

        /// <summary>Ticks one engine-time frame runs at speed 1.</summary>
        public int TicksPerFrame
        {
            get => _ticksPerFrame;
            set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _ticksPerFrame = value; }
        }

        /// <summary>For embedding platforms only: ticks per second of platform frame time at speed 1.</summary>
        public int TicksPerSecond
        {
            get => _ticksPerSecond;
            set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _ticksPerSecond = value; }
        }

        /// <summary>Speed multiplier, 1..64. Pausing keeps it.</summary>
        public int Speed
        {
            get => _speed;
            set { RequireLocal(); ApplySpeed(value); }
        }

        public bool Paused => _paused;
        public void Pause() { RequireLocal(); ApplyPause(); }
        public void Resume() { RequireLocal(); ApplyResume(); }
        public void TogglePause() { if (_paused) Resume(); else Pause(); }

        /// <summary>True in a lockstep game: pause and speed are sequenced session actions (<see cref="NetSession"/>), never a hand change.</summary>
        public bool Sequenced { get; internal set; }

        private void RequireLocal()
        {
            if (Sequenced) throw new InvalidOperationException("this game is in lockstep: ask the session (Net.RequestPause / RequestResume / RequestSpeed)");
        }

        internal void ApplySpeed(int value) { if (value < 1 || value > 64) throw new ArgumentOutOfRangeException(nameof(value), "1..64"); _speed = value; }
        internal void ApplyPause() => _paused = true;
        internal void ApplyResume() => _paused = false;

        /// <summary>Work budget per normal Update. Excess due ticks remain pending, never dropped.</summary>
        public int CatchUpCap
        {
            get => _catchUpCap;
            set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _catchUpCap = value; }
        }

        public bool IsRunningSlowly { get; private set; }
        public int TicksThisFrame { get; private set; }
        /// <summary>Whole earned ticks awaiting an Update budget or remote input; not extra simulation state.</summary>
        public long PendingTicks => _budget / MicrosecondsPerSecond;

        /// <summary>Fraction of the next tick, always [0,1). Pause hides it but does not discard earned work.</summary>
        public Fix Alpha => _paused ? Fix.Zero : Fix.FromRaw((_budget % MicrosecondsPerSecond) * (1L << 32) / MicrosecondsPerSecond);

        /// <summary>An engine-time frame: TicksPerFrame × Speed ticks through <paramref name="tick"/> (none while paused).</summary>
        internal int PumpFrame(Func<bool> tick)
        {
            return Pump(checked((long)_ticksPerFrame * _speed * MicrosecondsPerSecond), tick);
        }

        /// <summary>A platform frame: converts the platform's elapsed time into due ticks (exact, capped).</summary>
        internal int PumpElapsed(long elapsedMicroseconds, Func<bool> tick)
        {
            if (elapsedMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedMicroseconds));
            if (_paused) return Pump(0, tick);
            return Pump(checked(elapsedMicroseconds * _ticksPerSecond * _speed), tick);
        }

        private int Pump(long earned, Func<bool> tick)
        {
            long budget = _paused ? _budget : checked(_budget + earned);
            TicksThisFrame = 0;
            IsRunningSlowly = false;
            if (_paused) return 0;
            _budget = budget;
            long due = _budget / MicrosecondsPerSecond;
            IsRunningSlowly = due > _catchUpCap;
            int allowed = (int)System.Math.Min(due, _catchUpCap);
            int ran = 0;
            try
            {
                while (ran < allowed && !_paused && tick()) ran++;
                return ran;
            }
            finally
            {
                _budget -= (long)ran * MicrosecondsPerSecond;
                TicksThisFrame = ran;
            }
        }

        /// <summary>One published Update result. Only terminal outcome/fault discards unapplied pacing.</summary>
        internal void FinishUpdate(int ticksRun, bool stopped)
        {
            TicksThisFrame = ticksRun;
            if (!stopped) return;
            _budget = 0;
            IsRunningSlowly = false;
        }
    }
}
