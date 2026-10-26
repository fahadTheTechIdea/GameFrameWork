using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.App
{
    /// <summary>
    /// A player intent from the platform, already named: "clock.pause", "select", "build". A host maps
    /// its keys, clicks and touches to actions (through an <see cref="InputMap"/>); the application
    /// decides what an action does.
    /// </summary>
    public struct InputAction
    {
        public string Action;
        /// <summary>The tile the pointer was over, if any.</summary>
        public TileCoord Tile;
        /// <summary>The entity under the pointer, if any.</summary>
        public EntityId Target;
        /// <summary>A free argument (a def id picked from a menu, typed text).</summary>
        public string Argument;

        public InputAction(string action, string argument = null) : this()
        {
            Action = action;
            Argument = argument;
            Tile = TileCoord.Invalid;
        }

        public override string ToString() => Action + (Argument != null ? " " + Argument : "");
    }

    /// <summary>Where input comes from: a keyboard and mouse, a touch screen, a script, a network.</summary>
    public interface IInputSource
    {
        /// <summary>Adds the actions for this frame.</summary>
        void Poll(SimApp app, InputMap map, List<InputAction> into);
    }

    /// <summary>
    /// Where the picture goes: a console, a window, a test buffer. Called
    /// once per frame after the game has updated and drawn; it reads the app's game, snapshot and views.
    /// </summary>
    public interface IPresenter
    {
        void Present(SimApp app, AppFrame frame);
    }

    /// <summary>What one application frame is.</summary>
    public struct AppFrame
    {
        public long Number;
        /// <summary>Platform frame time when embedded; -1 for an engine-time frame.</summary>
        public long ElapsedMicroseconds;
        public long TotalMicroseconds;
        public int TicksRun;
    }

    /// <summary>A long-lived part of the application (XNA's GameComponent): runs every frame, in order.</summary>
    public abstract class AppComponent
    {
        public int UpdateOrder { get; protected set; }
        public bool Enabled { get; set; } = true;
        protected SimApp App { get; private set; }

        internal void Attach(SimApp app) { App = app; Initialize(); }
        public virtual void Initialize() { }
        public abstract void Update(AppFrame frame, IReadOnlyList<InputAction> actions);
    }

    /// <summary>One screen of the application: a title menu, a game in play, an end summary.</summary>
    public abstract class Scene
    {
        protected SimApp App { get; private set; }
        internal void Attach(SimApp app) => App = app;
        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract void Update(AppFrame frame, IReadOnlyList<InputAction> actions);
    }

    /// <summary>
    /// The application: the one lifecycle a host runs (XNA's Game). A frame is:
    ///
    ///     input actions → components (UpdateOrder) → current scene → the game's Update → presenter
    ///
    /// Scenes are a stack and may create, load or swap the game. The host supplies only an input source,
    /// a presenter and the elapsed time of each frame; nothing in the application renders or prints.
    /// </summary>
    public sealed class SimApp
    {
        private readonly List<AppComponent> _components = new List<AppComponent>();
        private readonly List<Scene> _scenes = new List<Scene>();
        private readonly List<InputAction> _actions = new List<InputAction>();
        private long _frame;
        private long _total;
        private bool _started;

        public SimApp(GameFactory factory, IInputSource input, IPresenter presenter, InputMap inputMap = null)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            InputMap = inputMap ?? factory.InputMap();
        }

        public GameFactory Factory { get; }
        public IInputSource Input { get; }
        public IPresenter Presenter { get; }
        public InputMap InputMap { get; }

        /// <summary>The game in play, if any. Scenes set it.</summary>
        public SimGame Game { get; set; }

        /// <summary>The seat the local player controls in <see cref="Game"/>, if any.</summary>
        public PlayerSeat LocalSeat { get; set; }

        public Scene Scene => _scenes.Count == 0 ? null : _scenes[_scenes.Count - 1];
        public bool ExitRequested { get; private set; }
        public long FrameNumber => _frame;
        public IReadOnlyList<AppComponent> Components => _components.AsReadOnly();


        public SimApp Add(AppComponent component)
        {
            // Stable: components with the same order run in the order they were added.
            int at = _components.Count;
            while (at > 0 && _components[at - 1].UpdateOrder > component.UpdateOrder) at--;
            _components.Insert(at, component);
            if (_started) component.Attach(this);
            return this;
        }

        public T Component<T>() where T : AppComponent
        {
            for (int i = 0; i < _components.Count; i++) if (_components[i] is T t) return t;
            return null;
        }

        public void Start(Scene first)
        {
            if (_started) throw new InvalidOperationException("the app is already started");
            _started = true;
            for (int i = 0; i < _components.Count; i++) _components[i].Attach(this);
            Push(first);
        }

        public void Push(Scene scene)
        {
            scene.Attach(this);
            _scenes.Add(scene);
            scene.Enter();
        }

        public void Pop()
        {
            if (_scenes.Count == 0) return;
            Scene top = _scenes[_scenes.Count - 1];
            _scenes.RemoveAt(_scenes.Count - 1);
            top.Exit();
            if (_scenes.Count == 0) ExitRequested = true;
        }

        public void Replace(Scene scene)
        {
            if (_scenes.Count > 0)
            {
                Scene top = _scenes[_scenes.Count - 1];
                _scenes.RemoveAt(_scenes.Count - 1);
                top.Exit();
            }
            Push(scene);
        }

        public void Exit() => ExitRequested = true;

        /// <summary>One engine-time frame of the application (the game advances its clock's ticks per frame).</summary>
        public int RunFrame() => Frame(-1);

        /// <summary>One frame when a platform embeds the app and owns the real loop: it passes the frame time it measured.</summary>
        public int RunFrame(long platformElapsedMicroseconds) => Frame(System.Math.Max(0, platformElapsedMicroseconds));

        /// <summary>Runs engine-time frames until the app exits, or <paramref name="maxFrames"/> (0: no limit). Returns frames run.</summary>
        public long Run(long maxFrames = 0)
        {
            long start = _frame;
            while (!ExitRequested && (maxFrames == 0 || _frame - start < maxFrames)) RunFrame();
            return _frame - start;
        }

        private int Frame(long elapsedMicroseconds)
        {
            if (!_started) throw new InvalidOperationException("Start the app first");
            if (ExitRequested) return 0;
            _frame++;
            if (elapsedMicroseconds > 0) _total += elapsedMicroseconds;
            _actions.Clear();
            Input.Poll(this, InputMap, _actions);
            for (int i = 0; i < _actions.Count; i++) if (_actions[i].Action == "app.exit") ExitRequested = true;
            var frame = new AppFrame { Number = _frame, ElapsedMicroseconds = elapsedMicroseconds, TotalMicroseconds = _total };
            for (int i = 0; i < _components.Count; i++) if (_components[i].Enabled) _components[i].Update(frame, _actions);
            Scene?.Update(frame, _actions);
            if (Game != null) frame.TicksRun = elapsedMicroseconds < 0 ? Game.Update() : Game.Update(elapsedMicroseconds);
            Presenter.Present(this, frame);
            return frame.TicksRun;
        }

    }

    /// <summary>
    /// Input names ("space", "plus", "mouse.left") to actions, as content authors them (section
    /// "input_maps"): { "id": "default", "bindings": { "space": "clock.pause", "f4": "clock.speed:4" } }.
    /// A binding is an action id, optionally followed by ":" and an argument. What the names mean (which key
    /// is "space") is the host's; what the actions do is the app's.
    /// </summary>
    public sealed class InputMap
    {
        private readonly Dictionary<string, KeyValuePair<string, string>> _bindings = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.Ordinal);

        /// <summary>The framework's fallback for a game whose content declares no input map: the clock and quitting.</summary>
        public static InputMap Default()
        {
            var m = new InputMap();
            m.Bind("space", "clock.pause");
            m.Bind("plus", "clock.faster");
            m.Bind("minus", "clock.slower");
            m.Bind("escape", "app.exit");
            m.Bind("q", "app.exit");
            return m;
        }

        public InputMap Bind(string input, string binding)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(binding)) throw new ArgumentException("a binding needs an input and an action");
            int colon = binding.IndexOf(':');
            _bindings[input] = colon < 0 ? new KeyValuePair<string, string>(binding, null) : new KeyValuePair<string, string>(binding.Substring(0, colon), binding.Substring(colon + 1));
            return this;
        }

        public int Count => _bindings.Count;

        public bool TryMap(string input, out InputAction action)
        {
            if (_bindings.TryGetValue(input ?? "", out KeyValuePair<string, string> b)) { action = new InputAction(b.Key, b.Value); return true; }
            action = default;
            return false;
        }

        /// <summary>The names bound to an action (for a help screen or a key-config UI), in name order.</summary>
        public IReadOnlyList<string> InputsFor(string action)
        {
            var names = new List<string>();
            foreach (KeyValuePair<string, KeyValuePair<string, string>> kv in _bindings) if (kv.Value.Key == action) names.Add(kv.Key);
            names.Sort(StringComparer.Ordinal);
            return names;
        }
    }

    /// <summary>
    /// Input from a script, for demos, tests and replays. Presses are keyed by game time — "at tick 500",
    /// "on day 3 at 06:00" — so a script reads the same whatever the pace or ticks per frame. Input that
    /// must work while the clock is stopped (resuming a paused game) is keyed by app frame instead, since
    /// game time does not move while paused.
    /// </summary>
    public sealed class ScriptedInput : IInputSource
    {
        private struct Entry { public long Tick; public long Frame; public GameDuration? At; public InputAction Action; public bool Done; }
        private readonly List<Entry> _entries = new List<Entry>();

        /// <summary>Presses a named input (mapped through the app's input map) once the game reaches <paramref name="tick"/>.</summary>
        public ScriptedInput Press(long tick, string input) => Add(new Entry { Tick = tick, Frame = -1, Action = new InputAction("input:" + input) });

        /// <summary>Presses after an authored elapsed duration, resolved by the current game's clock.</summary>
        public ScriptedInput PressAt(GameDuration elapsed, string input) => Add(new Entry { Tick = -1, Frame = -1, At = elapsed, Action = new InputAction("input:" + input) });

        /// <summary>Presses a named input on this app frame (counting from 1), whatever the game clock is doing.</summary>
        public ScriptedInput PressOnFrame(long frame, string input) => Add(new Entry { Tick = -1, Frame = frame, Action = new InputAction("input:" + input) });

        /// <summary>Sends an action directly (no input name) once the game reaches <paramref name="tick"/>.</summary>
        public ScriptedInput Act(long tick, InputAction action) => Add(new Entry { Tick = tick, Frame = -1, Action = action });

        private ScriptedInput Add(Entry e) { _entries.Add(e); return this; }

        public void Poll(SimApp app, InputMap map, List<InputAction> into)
        {
            long tick = app.Game == null ? 0 : app.Game.Tick;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Done) continue;
                if (e.At.HasValue)
                {
                    if (app.Game == null) continue;
                    e.Tick = app.Game.SimulationTime.Ticks(e.At.Value); e.At = null;
                }
                bool due = e.Frame >= 0 ? app.FrameNumber >= e.Frame : tick >= e.Tick && (app.Game != null || e.Tick == 0);
                if (!due) continue;
                e.Done = true;
                _entries[i] = e;
                InputAction a = e.Action;
                if (a.Action.StartsWith("input:", StringComparison.Ordinal)) { if (map.TryMap(a.Action.Substring(6), out InputAction mapped)) into.Add(mapped); }
                else into.Add(a);
            }
        }
    }

    /// <summary>No input: an unattended app.</summary>
    public sealed class NoInput : IInputSource
    {
        public void Poll(SimApp app, InputMap map, List<InputAction> into) { }
    }
}
