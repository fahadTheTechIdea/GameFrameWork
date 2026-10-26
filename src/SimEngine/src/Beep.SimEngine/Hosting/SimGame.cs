using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    public enum GameState : byte
    {
        Running = 0,
        Paused = 1,
        Faulted = 2,
        Ended = 3,
        /// <summary>Lockstep: waiting for a peer's orders for the next tick.</summary>
        Stalled = 4,
        /// <summary>Lockstep: a peer's state differs from ours.</summary>
        Desynced = 5
    }

    /// <summary>What a host asks for to start a game.</summary>
    public sealed class NewGameRequest
    {
        public string Scenario;
        public ulong Seed;
        /// <summary>Who plays each seat. Empty: the scenario's default (seat 0 human, the rest autopilot).</summary>
        public List<Controller> Seats = new List<Controller>();
        /// <summary>Watch only: every seat the scenario allows is played by the engine's autopilot (Seats is ignored).</summary>
        public bool Spectate;
        /// <summary>Null uses authored horizon; explicit zero disables it. Conversion and ending remain engine rules.</summary>
        public GameDuration? Horizon;
        /// <summary>Extra content modules (data mods) to enable on top of the scenario's modules.</summary>
        public List<string> Mods = new List<string>();
        /// <summary>Permitted typed rule overrides. Captured by the engine before composing any provider.</summary>
        public List<GameModeAssignment> ModeOverrides = new List<GameModeAssignment>();
    }

    /// <summary>
    /// One player decision contract for host input and authored engine strategy. All actions use
    /// ordinary scoped commands/views; gameplay strategy and durable cadence belong in engine modules.
    /// </summary>
    public interface IPlayerController
    {
        void Decide(PlayerDecisionContext context);
    }

    /// <summary>The same decision contract, not a separate host/AI strategy family.</summary>
    public abstract class PlayerController : IPlayerController
    {
        public abstract void Decide(PlayerDecisionContext context);
    }

    /// <summary>
    /// One company's place in a game: send orders, preview them, read the answers and the news, and
    /// see the world as that company sees it. A client plays a seat; it never touches the world.
    /// </summary>
    public sealed class PlayerSeat
    {
        private readonly SimGame _game;
        private readonly List<IPlayerController> _controllers = new List<IPlayerController>();
        private readonly List<CommandResult> _answers = new List<CommandResult>();
        private long _answerCursor;

        internal PlayerSeat(SimGame game, PlayerId id, PlayerView view)
        {
            _game = game;
            Id = id;
            View = view;
        }

        public PlayerId Id { get; }
        public PlayerView View { get; }

        private GameSnapshot _scoped;

        /// <summary>
        /// This seat's retained immutable simulation snapshot using the same visibility
        /// rules as its view, including private map invalidations and visible spawn/removal deltas.
        /// </summary>
        public GameSnapshot Snapshot => _scoped ?? _game.Snapshot;

        internal GameSnapshot PrepareScoped(World world, long revision, Fix alpha, GameState state)
            => GameSnapshot.Capture(world, revision, alpha, state, View.CanSee, _scoped, Id);
        internal GameSnapshot RetainScoped(long revision) => GameSnapshot.Retain(revision, _scoped, Id);
        internal void PublishScoped(GameSnapshot snapshot) => _scoped = snapshot;
        public Controller Controller => _game.World.Players.ControllerOf(Id);
        public SimGame Game => _game;

        /// <summary>
        /// Queues a snapshot of a registered order. Safe from input threads, including in lockstep;
        /// returns the sequence its answer will carry. Do not mutate the payload during this call.
        /// Previews, view reads, session transport and game lifecycle calls use the game thread.
        /// </summary>
        public int Submit(Command command) => _game.SubmitFor(Id, command);

        /// <summary>What the order would be answered with if applied now (the handler's own check). Changes nothing.</summary>
        public Code Preview(Command command) => _game.World.Check(Id, command);

        /// <summary>This seat's command answers newer than <paramref name="cursor"/>; advances the cursor. Start at 0.</summary>
        public int ReadResults(ref long cursor, IList<CommandResult> dst)
        {
            PlayerId me = Id;
            return _game.World.Results.CopySince(ref cursor, dst, r => r.Player == me);
        }

        /// <summary>News this seat is told, newer than <paramref name="cursor"/>; advances the cursor. Start at 0.</summary>
        public int ReadNotifications(ref long cursor, IList<Notification> dst)
        {
            PlayerId me = Id;
            return _game.World.Notifications.CopySince(ref cursor, dst, n => n.VisibleTo(me));
        }

        /// <summary>Attaches a controller that plays this seat from now on (it hears only what happens from now on).</summary>
        public void Attach(IPlayerController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            foreach (var attached in _controllers)
                if (ReferenceEquals(attached, controller)) throw new InvalidOperationException("controller is already attached to this seat");
            if (_controllers.Count == 0)
            {
                _answerCursor = _game.World.Results.Latest;
            }
            _controllers.Add(controller);
        }

        public void Detach(IPlayerController controller)
        {
            for (int i = 0; i < _controllers.Count; i++)
                if (ReferenceEquals(_controllers[i], controller)) { _controllers.RemoveAt(i); return; }
        }

        internal IPlayerController[] CaptureControllers() => _controllers.ToArray();

        internal void RunControllers(IPlayerController[] controllers)
        {
            if (controllers.Length == 0) return;
            _answers.Clear();
            ReadResults(ref _answerCursor, _answers);
            _answers.RemoveAll(answer => answer.Stream != CommandStreams.Player);
            for (int c = 0; c < controllers.Length; c++)
            {
                var context = new PlayerDecisionContext(_game.World, Id, View, Submit,
                    () => Array.AsReadOnly(_answers.ToArray()), null, FindAnswer);
                context.Invoke(controllers[c]);
            }
        }
        private CommandResult? FindAnswer(int sequence)
        {
            long cursor = 0; var retained = new List<CommandResult>(); ReadResults(ref cursor, retained);
            foreach (CommandResult answer in retained) if (answer.Stream == CommandStreams.Player && answer.Sequence == sequence) return answer;
            return null;
        }
    }

    /// <summary>
    /// One running simulation — the only object a host holds. Initialize publishes the initial facts;
    /// Update runs the fixed ticks the clock says are due and publishes immutable simulation snapshots.
    /// Controllers submit before each tick. The elapsed-time overload accepts a platform delta, while
    /// tools may run a batch with RunTicks. Drawing/rendering belongs to a future game platform only.
    /// </summary>
    public sealed class SimGame : IGameCycle
    {
        private readonly PlayerSeat[] _seats;
        private readonly Func<bool> _tick;
        private readonly string _gameName;
        private bool _lifecycleActive;
        private long _revision;
        private ReplayRecorder _recording;

        internal SimGame(World world, string gameName, IReadOnlyDictionary<Type, ViewFeatureRegistration> features)
        {
            World = world;
            _gameName = gameName;
            _seats = new PlayerSeat[world.Players.Count];
            for (int i = 0; i < _seats.Length; i++)
                _seats[i] = new PlayerSeat(this, new PlayerId(i), new PlayerView(world, new PlayerId(i), features));
            Observer = new PlayerView(world, PlayerId.None, features);
            _tick = TickOnce;
            Snapshot = new GameSnapshot();
        }

        internal World World { get; }

        public string Scenario => World.Settings.Scenario;
        public IGameModeConfiguration GameMode => World.GameMode;
        public ulong Seed => World.Settings.Seed;
        public IReadOnlyList<string> Modules => World.Content.Modules;
        public long Tick => World.Tick;
        public ISimulationTime SimulationTime => World.Time;
        public CalendarReading Date => World.Date;
        public Code Outcome => World.Outcome;

        public GameState State
            => World.Fault != null ? GameState.Faulted
             : Net != null && (Net.State == NetState.Desynced || Net.State == NetState.Broken) ? GameState.Desynced
             : !World.Outcome.IsNone ? GameState.Ended
             : Clock.Paused ? GameState.Paused
             : Net != null && Net.State == NetState.Stalled ? GameState.Stalled
             : GameState.Running;

        /// <summary>The lockstep session, when this game is played across machines.</summary>
        public NetSession Net { get; private set; }

        /// <summary>
        /// Plays this game in lockstep with peers running the same game (same scenario, seed, seats — check
        /// with <see cref="NetSession.Hello"/>). Call before the first frame. Local players' orders then go out
        /// as frames; each tick waits for every human seat's frame.
        /// </summary>
        public NetSession StartLockstep(LockstepOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (Net != null) throw new InvalidOperationException("this game is already in lockstep");
            Net = new NetSession(this, options);
            Clock.Sequenced = true;
            return Net;
        }

        public WorldFault Fault => World.Fault;

        /// <summary>The session's sole current-time and pacing owner.</summary>
        public GameClock Clock => World.GameClock;
        IGameClock IGameCycle.Clock => Clock;

        public int SeatCount => _seats.Length;
        public PlayerSeat Seat(int index) => _seats[index];
        public PlayerSeat Seat(PlayerId id) => _seats[id.Value];

        /// <summary>The all-seeing view, for spectators and replays. A player's client uses its seat's view.</summary>
        public PlayerView Observer { get; }

        /// <summary>Every company's news, for spectators and replays (a seat reads only its own).</summary>
        public int ReadAllNotifications(ref long cursor, IList<Notification> dst) => World.Notifications.CopySince(ref cursor, dst);

        /// <summary>Immutable simulation facts published by the most recent Initialize or Update.</summary>
        public GameSnapshot Snapshot { get; private set; }

        public bool Initialized { get; private set; }
        public GameUpdate Time { get; private set; }
        /// <summary>
        /// Called once after each Update has settled its fixed ticks, while still in the Update phase.
        /// Observers may read engine results or submit input; they cannot run another lifecycle phase.
        /// Gameplay systems remain the authoritative fixed-tick participants.
        /// </summary>
        public event Action<GameUpdate> Updated;

        /// <summary>Once: exposes the composed or restored session as ready to update. Idempotent.</summary>
        public void Initialize()
        {
            RequireIdle();
            if (Initialized) return;
            _lifecycleActive = true;
            try
            {
                Initialized = true;
                PublishSnapshot(0);
                Time = new GameUpdate(World.Tick, World.Tick, -1, Clock.Alpha, Clock.Time);
            }
            finally { _lifecycleActive = false; }
        }

        // ------------------------------------------------------------------ the frame

        /// <summary>Runs the scenario's engine-time fixed ticks, publishes simulation facts, and raises Updated.</summary>
        public int Update() => UpdatePhase(-1, null);

        /// <summary>Runs fixed ticks due for the platform's elapsed time, publishes facts, and raises Updated.</summary>
        public int Update(long platformElapsedMicroseconds)
        {
            if (platformElapsedMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(platformElapsedMicroseconds));
            return UpdatePhase(platformElapsedMicroseconds, null);
        }

        private int UpdatePhase(long elapsedMicroseconds, int? batchTicks)
        {
            RequireInitialized();
            RequireIdle();
            // A stopped fault still publishes retained diagnostics, never live advancement.
            if (World.Fault == null) World.RequireCompletedOperations();
            _lifecycleActive = true;
            long startTick = World.Tick;
            try
            {
                int ran = 0;
                if (batchTicks.HasValue)
                {
                    while (ran < batchTicks.Value && TickOnce()) ran++;
                }
                else if (World.Fault == null && World.Outcome.IsNone)
                    ran = elapsedMicroseconds < 0 ? Clock.PumpFrame(_tick) : Clock.PumpElapsed(elapsedMicroseconds, _tick);
                PublishSnapshot(ran);
                Time = new GameUpdate(startTick, World.Tick, elapsedMicroseconds, Clock.Alpha, Clock.Time);
                Updated?.Invoke(Time);
                return ran;
            }
            finally { _lifecycleActive = false; }
        }

        /// <summary>
        /// Runs <paramref name="ticks"/> ticks now, ignoring pace and pause (tools, tests, fast-forward),
        /// then publishes a snapshot. Stops early on a fault or outcome. Returns the ticks run.
        /// </summary>
        public int RunTicks(int ticks) => UpdatePhase(-1, System.Math.Max(0, ticks));

        private bool TickOnce()
        {
            if (_recording == null) return TickCore();
            // Same inbox lock defines recording/admission boundaries, not another simulation queue.
            lock (World.InputGate) return TickCore();
        }

        private bool TickCore()
        {
            if (World.Fault != null || !World.Outcome.IsNone) return false;
            // Players act once per tick, even if the tick has to wait for a peer and is tried again.
            if (_controllersRanFor != World.Tick)
            {
                _controllersRanFor = World.Tick;
                var controllers = new IPlayerController[_seats.Length][];
                for (int i = 0; i < _seats.Length; i++) controllers[i] = _seats[i].CaptureControllers();
                try
                {
                    for (int i = 0; i < _seats.Length; i++) _seats[i].RunControllers(controllers[i]);
                }
                catch (Exception error)
                {
                    World.LatchFault("player.decide", error);
                    return false;
                }
            }
            if (Net != null)
            {
                Net.Seal(World.Tick);
                if (!Net.TryApply(World.Tick)) return false;
            }
            if (!World.Step()) return false;
            Net?.AfterTick(World.Tick);
            _recording?.CompletedStep();
            return true;
        }

        private long _controllersRanFor = -1;

        /// <summary>
        /// Publishes detached facts as part of the engine's existing lifecycle, without rendering.
        /// </summary>
        private void PublishSnapshot(int completedSteps)
        {
            bool stopped = World.Fault != null || !World.Outcome.IsNone;
            Clock.FinishUpdate(completedSteps, stopped);
            long revision = checked(_revision + 1);
            GameSnapshot observer = null;
            var seats = new GameSnapshot[_seats.Length];
            if (World.Fault == null)
            {
                try
                {
                    var facts = EntityObservation.Capture(World);
                    observer = GameSnapshot.Capture(World, revision, Clock.Alpha, State, null, Snapshot, PlayerId.None);
                    for (int i = 0; i < _seats.Length; i++) seats[i] = _seats[i].PrepareScoped(World, revision, Clock.Alpha, State);
                    facts.Verify();
                    World.RequireHealthyUpdate();
                }
                catch (Exception error) { World.LatchFault("snapshot.capture", error); }
            }
            if (World.Fault != null)
            {
                if (!stopped) Clock.FinishUpdate(completedSteps, true);
                observer = GameSnapshot.Retain(revision, Snapshot, PlayerId.None);
                for (int i = 0; i < _seats.Length; i++) seats[i] = _seats[i].RetainScoped(revision);
            }
            // No extension callback may run after the first stream becomes public.
            Snapshot = observer;
            for (int i = 0; i < _seats.Length; i++) _seats[i].PublishScoped(seats[i]);
            _revision = revision;
        }

        private void RequireInitialized()
        {
            if (!Initialized) throw new InvalidOperationException("Initialize the game before running its lifecycle");
        }

        private void RequireIdle()
        {
            if (_lifecycleActive)
                throw new InvalidOperationException("game lifecycle phases cannot be re-entered");
        }

        internal int SubmitFor(PlayerId player, Command command)
        {
            World.RequireHealthyUpdate();
            return Net != null ? Net.Buffer(player, command) : World.Submit(player, command, CommandStreams.Player, _recording?.Input);
        }

        /// <summary>Records canonical engine inputs/steps, not wall-clock frames or transport packets.</summary>
        public IReplayRecorder RecordReplay(ReplayLimits limits)
        {
            RequireInitialized(); RequireIdle();
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            lock (World.InputGate)
            {
                if (_recording != null) throw new InvalidOperationException("this game already has a replay recorder");
                _recording = new ReplayRecorder(this, limits);
                World.StampedInputObserver = _recording.Admitted;
                return _recording;
            }
        }
        internal void ReleaseReplay(ReplayRecorder recorder)
        {
            RequireIdle();
            lock (World.InputGate)
                if (ReferenceEquals(_recording, recorder)) { World.StampedInputObserver = null; _recording = null; }
        }
        internal void RequireReplayIdle() => RequireIdle();

        // ------------------------------------------------------------------ persistence and diagnostics

        public byte[] Save() => SaveGame.Write(World, _gameName);

        /// <summary>The state hash: two games with the same hash hold the same state.</summary>
        public ulong Hash() => World.Hash();

        /// <summary>Per-part hashes, to find where two games diverged.</summary>
        public IReadOnlyList<KeyValuePair<string, ulong>> HashBreakdown() => World.HashBreakdown();
    }
}
