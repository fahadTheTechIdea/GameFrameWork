using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.App
{
    /// <summary>Clock actions (clock.pause, clock.faster, clock.slower, clock.speed N) applied to the app's game.</summary>
    public sealed class ClockControls : AppComponent
    {
        public ClockControls() { UpdateOrder = -100; }

        public override void Update(AppFrame frame, IReadOnlyList<InputAction> actions)
        {
            SimGame game = App.Game;
            if (game == null) return;
            for (int i = 0; i < actions.Count; i++)
            {
                int speed = game.Clock.Speed;
                switch (actions[i].Action)
                {
                    case "clock.pause":
                        // In lockstep every peer pauses at the same tick, so the request travels with the orders.
                        if (game.Net == null) game.Clock.TogglePause();
                        else if (game.Clock.Paused) game.Net.RequestResume();
                        else game.Net.RequestPause();
                        continue;
                    case "clock.faster": if (speed < 64) speed *= 2; break;
                    case "clock.slower": if (speed > 1) speed /= 2; break;
                    case "clock.speed":
                        if (!int.TryParse(actions[i].Argument, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out speed) || speed < 1 || speed > 64) continue;
                        break;
                    default: continue;
                }
                if (game.Net == null) game.Clock.Speed = speed;
                else game.Net.RequestSpeed(speed);
            }
        }
    }

    /// <summary>
    /// Starts a new game and plays it: sets the app's game and local seat, and moves to <paramref name="then"/>
    /// (or exits) when the game ends or faults.
    /// </summary>
    public sealed class PlayScene : Scene
    {
        private readonly Func<SimApp, SimGame> _start;
        private readonly Func<SimGame, Scene> _then;
        private readonly int _localSeat;
        private readonly int _ticksPerFrame;
        private readonly List<Action<SimGame>> _setup = new List<Action<SimGame>>();

        /// <param name="ticksPerFrame">0: the scenario's own pace (what every client uses). A framework test may fix the frame size.</param>
        public PlayScene(NewGameRequest request, int localSeat = 0, int ticksPerFrame = 0, Func<SimGame, Scene> then = null)
            : this(app => app.Factory.NewGame(request), localSeat, ticksPerFrame, then) { }

        public PlayScene(Func<SimApp, SimGame> start, int localSeat = 0, int ticksPerFrame = 0, Func<SimGame, Scene> then = null)
        {
            _start = start ?? throw new ArgumentNullException(nameof(start));
            _localSeat = localSeat;
            _ticksPerFrame = ticksPerFrame;
            _then = then;
        }

        /// <summary>Runs once the game exists: attach controllers to seats, set pace.</summary>
        public PlayScene OnStart(Action<SimGame> setup)
        {
            _setup.Add(setup);
            return this;
        }

        public override void Enter()
        {
            SimGame game = _start(App);
            if (_ticksPerFrame > 0) game.Clock.TicksPerFrame = _ticksPerFrame;
            App.Game = game;
            App.LocalSeat = _localSeat >= 0 && _localSeat < game.SeatCount ? game.Seat(_localSeat) : null;
            for (int i = 0; i < _setup.Count; i++) _setup[i](game);
        }

        public override void Update(AppFrame frame, IReadOnlyList<InputAction> actions)
        {
            SimGame game = App.Game;
            if (game == null) return;
            bool over = game.State == GameState.Faulted || game.State == GameState.Ended;
            if (!over) return;
            Scene next = _then?.Invoke(game);
            if (next != null) App.Replace(next);
            else App.Exit();
        }
    }

    /// <summary>
    /// A seat played by a script (a demo, a tutorial, a test): a coroutine of waits and player-input
    /// actions. Each action receives the current decision context; no context is retained by the script.
    /// It never blocks a frame; it advances only before ticks.
    /// </summary>
    public sealed class ScriptedController : PlayerController
    {
        private readonly Func<IEnumerable<Wait>> _script;
        private IEnumerator<Wait> _run;
        private Wait _waiting;

        public ScriptedController(Func<IEnumerable<Wait>> script)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public bool Finished { get; private set; }

        public override void Decide(PlayerDecisionContext seat)
        {
            if (Finished) return;
            if (_run == null) _run = _script().GetEnumerator();
            for (int guard = 0; guard < 10000; guard++)
            {
                if (_waiting != null)
                {
                    if (!_waiting.Done(seat)) return;
                    _waiting = null;
                }
                if (!_run.MoveNext()) { Finished = true; return; }
                _waiting = _run.Current;
            }
            throw new InvalidOperationException("a scripted controller yielded 10000 times without waiting");
        }
    }

    /// <summary>What a scripted controller waits for between orders.</summary>
    public abstract class Wait
    {
        internal abstract bool Done(PlayerDecisionContext seat);

        /// <summary>Executes player input using this invocation's context, never a retained one.</summary>
        public static Wait Do(Action<PlayerDecisionContext> action) => new DecisionAction(action);

        public static Wait Ticks(long ticks) => new ForTicks(ticks);
        public static Wait Duration(GameDuration duration) => new ForDuration(duration);
        public static Wait Hours(long hours) => Duration(GameDuration.Hours(hours));
        public static Wait Days(long days) => Duration(GameDuration.Days(days));

        /// <summary>Waits until a condition on the seat's view holds; gives up after <paramref name="timeoutTicks"/> (0: never).</summary>
        public static Wait Until(Func<PlayerDecisionContext, bool> condition, long timeoutTicks = 0) => new UntilTrue(condition, timeoutTicks);

        /// <summary>Waits for the answer to an order this script submitted.</summary>
        public static Wait ForAnswer(int sequence, Action<CommandResult> onAnswer = null) => new ForResult(sequence, onAnswer);

        private sealed class DecisionAction : Wait
        {
            private readonly Action<PlayerDecisionContext> _action;
            public DecisionAction(Action<PlayerDecisionContext> action)
                => _action = action ?? throw new ArgumentNullException(nameof(action));
            internal override bool Done(PlayerDecisionContext seat) { _action(seat); return true; }
        }

        private sealed class ForTicks : Wait
        {
            private readonly long _ticks;
            private long _until = -1;
            public ForTicks(long ticks)
            {
                if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
                _ticks = ticks;
            }
            internal override bool Done(PlayerDecisionContext seat)
            {
                if (_until < 0) _until = checked(seat.Tick + _ticks);
                return seat.Tick >= _until;
            }
        }

        private sealed class ForDuration : Wait
        {
            private readonly GameDuration _duration;
            private long _until = -1;
            public ForDuration(GameDuration duration) { _duration = duration; }
            internal override bool Done(PlayerDecisionContext seat)
            {
                if (_until < 0) _until = checked(seat.Tick + seat.Time.Ticks(_duration));
                return seat.Tick >= _until;
            }
        }

        private sealed class UntilTrue : Wait
        {
            private readonly Func<PlayerDecisionContext, bool> _condition;
            private readonly long _timeout;
            private long _deadline = -1;
            public UntilTrue(Func<PlayerDecisionContext, bool> condition, long timeout)
            {
                _condition = condition ?? throw new ArgumentNullException(nameof(condition));
                if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout));
                _timeout = timeout;
            }
            internal override bool Done(PlayerDecisionContext seat)
            {
                if (_deadline < 0 && _timeout > 0) _deadline = checked(seat.Tick + _timeout);
                return _condition(seat) || (_deadline >= 0 && seat.Tick >= _deadline);
            }
        }

        private sealed class ForResult : Wait
        {
            private readonly int _sequence;
            private readonly Action<CommandResult> _onAnswer;
            public ForResult(int sequence, Action<CommandResult> onAnswer) { _sequence = sequence; _onAnswer = onAnswer; }
            internal override bool Done(PlayerDecisionContext seat)
            {
                if (seat.TryGetAnswer(_sequence, out CommandResult answer)) { _onAnswer?.Invoke(answer); return true; }
                return false;
            }
        }
    }
}
