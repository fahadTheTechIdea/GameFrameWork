using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.Kernel
{
    /// <summary>What a world is made from; everything needed to rebuild it for a load. Saved in the header.</summary>
    public sealed class WorldSettings
    {
        public string Scenario = "";
        public ulong Seed;
        /// <summary>Active module ids, in composition order (code modules and their content modules share ids).</summary>
        public string[] Modules = new string[0];
        public ModuleVersion[] ModuleVersions = new ModuleVersion[0];
        public Controller[] Seats = new Controller[0];
        public string Space;
        public string Time;
        /// <summary>Explicit session horizon in its own tick units. Zero means no horizon.</summary>
        public long HorizonTicks;
        /// <summary>Explicit profile identity; defaults and scenario overrides stay in frozen content.</summary>
        public string GameMode;
        private IReadOnlyList<GameModeAssignment> _modeOverrides = Array.AsReadOnly(Array.Empty<GameModeAssignment>());
        public IReadOnlyList<GameModeAssignment> ModeOverrides => _modeOverrides;
        internal void CaptureModeOverrides(IEnumerable<GameModeAssignment> overrides)
            => _modeOverrides = GameModeConfiguration.CaptureAssignments(overrides);

        public void Sync(IStateSync s)
        {
            s.Section("settings");
            s.String(ref Scenario);
            s.ULong(ref Seed);
            int n = s.Count(Modules.Length);
            if (s.Reading) Modules = new string[n];
            for (int i = 0; i < n; i++) s.String(ref Modules[i]);
            if (s.Reading) ModuleVersions = new ModuleVersion[n];
            if (ModuleVersions.Length != n) throw new InvalidOperationException("module versions must match the active module identities");
            for (int i = 0; i < n; i++)
            {
                string version = s.Reading ? null : ModuleVersions[i].ToString();
                s.String(ref version);
                if (s.Reading)
                {
                    if (!ModuleVersion.TryParse(version, out ModuleVersion parsed)) throw new CorruptStateException("invalid saved module version");
                    ModuleVersions[i] = parsed;
                }
            }
            int seats = s.Count(Seats.Length);
            if (s.Reading) Seats = new Controller[seats];
            for (int i = 0; i < seats; i++) s.Enum(ref Seats[i]);
            s.String(ref Space);
            s.String(ref Time); s.Long(ref HorizonTicks);
            if (string.IsNullOrEmpty(Time) || HorizonTicks < 0) throw new CorruptStateException("invalid session time identity or horizon");
            s.String(ref GameMode);
            GameModeConfiguration.SyncAssignments(s, ref _modeOverrides);
        }
    }

    /// <summary>
    /// The simulation state of one game and its fixed tick pipeline:
    ///
    ///     commands        every queued order: Check, then Apply, one answer each
    ///     systems         in order, each when its schedule is due; entity spawns/removals flushed between
    ///     events          dispatched once, in publish order
    ///     entities        each live entity settles its state through Update, in identity order
    ///
    /// The world knows nothing about oil, roads or money: modules add systems, services, commands, map
    /// layers and content. Game code uses it; clients never see it.
    /// </summary>
    public sealed partial class World
    {
        private readonly List<SystemSlot> _systems = new List<SystemSlot>();
        private readonly CommandInbox _inbox;
        private readonly List<CommandResult> _tickResults = new List<CommandResult>();
        private readonly CommandContext _context = new CommandContext();
        private bool _composed;
        private bool _initialized;
        private int _entityInitializationDepth;
        private IReadOnlyList<ISimulationOperationBoundary> _operationBoundaries = Array.Empty<ISimulationOperationBoundary>();
        // Synchronous observation stack scratch only. Never serialized, shared across
        // worlds, or retained after EntityObservation.ReadProvider unwinds.
        internal EntityObservation.SpatialScope? ActiveSpatialObservation { get; set; }
        // Opted-in synchronous provider call stack. Identity/operation scratch only,
        // never saved, hashed, shared across sessions or retained after the call unwinds.
        internal readonly List<(object Provider, string Operation)> ActiveProviderCalls = new List<(object, string)>();
        // Synchronous baseline/codec-read scratch, not state authority or another clock.
        // Actual spatial publication is forbidden only while these original reads run.
        internal int ObservationCaptureDepth;
        /// <summary>Generation precedes the first published session state. Viewer invalidation settles once afterwards.</summary>
        internal bool Generating { get; private set; }

        private sealed class SystemSlot
        {
            public GameSystem System;
            public int Order;
            public StateCodecRegistration Metadata;
            public Schedule Schedule;
        }

        public World(ResolvedContent content, WorldSettings settings)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ScenarioDef scenario = content.Get<ScenarioDef>(settings.Scenario);
            Entities = new EntityStore(scenario.Id);
            if (settings.GameMode != scenario.DeclaredGameMode) throw new CorruptStateException("session game mode identity differs from its scenario");
            GameMode = scenario.ResolveGameMode(content, settings.ModeOverrides);
            GameModeConfiguration.ValidateSelectedCapabilities(content, GameMode);
            GameClock = new GameClock(content.Get<SimulationTimeDef>(settings.Time));
            Events = new EventBus(this);
            if (settings.HorizonTicks < 0) throw new ArgumentOutOfRangeException(nameof(settings));
            Random = new DetRandom(settings.Seed);
            for (int i = 0; i < settings.Seats.Length; i++) Players.Add(settings.Seats[i], i);
            Services.ComposingModule = "engine";
            Services.Bind<ISpatialMutation>(new SpatialMutation(this));
            Services.ComposingModule = null;
            Commands.ComposingModule = "engine";
            Commands.Register("core.set-controller", new Beep.SimEngine.Commands.SetControllerHandler());
            Commands.ComposingModule = null;
            _inbox = new CommandInbox(Commands, settings.Seats.Length, settings.Modules);
        }

        public ResolvedContent Content { get; }
        public WorldSettings Settings { get; }
        /// <summary>The required authored scenario configuration plus captured request overrides.</summary>
        public IGameModeConfiguration GameMode { get; }
        internal GameClock GameClock { get; }
        public IGameClock Clock => GameClock;
        public long Tick => Clock.StepIndex;
        public ISimulationTime Time => Clock.Time;
        public CalendarReading Date => Clock.Date;
        public DetRandom Random { get; }
        public ISpatialSpace Space => Services.TryGet<ISpatialSpace>(out var space) ? space : null;
        public EntityStore Entities { get; }
        public EventBus Events { get; }
        public ServiceContainer Services { get; } = new ServiceContainer();
        public PlayerTable Players { get; } = new PlayerTable();
        public CommandRegistry Commands { get; } = new CommandRegistry();
        public WorldFault Fault { get; private set; }
        internal void FaultReplacement(Exception exception) => LatchFault("entity.replace", exception);
        private void FaultCreation(Exception exception) => LatchFault("entity.create", exception);

        internal void LatchFault(string phase, Exception exception)
        {
            if (Fault == null) Fault = new WorldFault(Tick, phase, exception);
        }

        /// <summary>Module queries share the original fault guard; no module-owned fault or host World escape.</summary>
        public void RequireHealthyUpdate()
        {
            if (Fault != null) throw new InvalidOperationException("game update stopped: " + Fault);
        }
        /// <summary>State owners call before admitting writes, including restore. Reads/encoding may
        /// inspect state but must never invoke another owner's mutation contract.</summary>
        public void RequireStateMutation()
        {
            RequireHealthyUpdate();
            if (ObservationCaptureDepth != 0)
                throw new InvalidOperationException("state mutation during read-only encoding or observation capture");
        }
        internal string ComposingModule { get; set; }

        /// <summary>The view features modules composed (read models per viewer). Set once when the world is built.</summary>
        internal IReadOnlyDictionary<Type, ViewFeatureRegistration> ViewFeatures { get; set; } = new Dictionary<Type, ViewFeatureRegistration>();

        /// <summary>
        /// The immutable, visibility-scoped player surface for engine module code, including controllers.
        /// A module already executing with a World must use this projection for player-visible decisions.
        /// Hosts obtain the same projection from their seat; the hosting facade never exposes the World.
        /// </summary>
        public Hosting.PlayerView ViewFor(PlayerId viewer)
        {
            if (viewer.Value < -1 || viewer.Value >= Players.Count)
                throw new ArgumentOutOfRangeException(nameof(viewer), "viewer must be an existing seat or the observer");
            return Hosting.PlayerView.Create(this, viewer);
        }

        /// <summary>How the game ended (won, lost, retired...), or none while it runs. Set by the game's rules.</summary>
        public Code Outcome { get; private set; }

        /// <summary>The outcome of a game that reached its explicitly selected horizon.</summary>
        public static readonly Code HorizonOutcome = new Code("core.horizon");

        /// <summary>Ends the game with an outcome. The game lifecycle runs no further fixed ticks.</summary>
        public void End(Code outcome)
        {
            if (!Outcome.IsNone) return;
            Outcome = outcome;
        }

        /// <summary>Recent news (presentation feed, not hashed).</summary>
        public FeedRing<Notification> Notifications { get; } = new FeedRing<Notification>(8192);

        /// <summary>Recent command answers (presentation feed, not hashed).</summary>
        public FeedRing<CommandResult> Results { get; } = new FeedRing<CommandResult>(8192);

        // ------------------------------------------------------------------ composition

        public void AddSystem(GameSystem system, int order)
        {
            if (_composed) throw new InvalidOperationException("systems are added while composing");
            if (ComposingModule == null) throw new InvalidOperationException("systems need a composing module owner");
            if (system == null) throw new ArgumentNullException(nameof(system));
            var metadata = new StateCodecRegistration(StateCodecKind.System, system.Id, system.StateSchemaVersion, ComposingModule, system.GetType());
            Schedule schedule = system.GetSchedule(Time);
            if (schedule.Period < 1 || schedule.Phase < 0 || schedule.Phase >= schedule.Period
                || schedule.Kind < ScheduleKind.EveryTick || schedule.Kind > ScheduleKind.Spread
                || schedule.Kind == ScheduleKind.EveryTick && (schedule.Period != 1 || schedule.Phase != 0)
                || schedule.Kind == ScheduleKind.Spread && schedule.Phase != 0)
                throw new ArgumentException("invalid declared system schedule: " + metadata.Id, nameof(system));
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Order == order) throw new InvalidOperationException("systems '" + _systems[i].Metadata.Id + "' and '" + metadata.Id + "' share order " + order);
                if (_systems[i].Metadata.Id == metadata.Id) throw new InvalidOperationException("system id '" + metadata.Id + "' used twice");
            }
            _systems.Add(new SystemSlot { System = system, Order = order, Metadata = metadata, Schedule = schedule });
            _systems.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public T System<T>() where T : GameSystem
        {
            for (int i = 0; i < _systems.Count; i++) if (_systems[i].System is T t) return t;
            return null;
        }

        public IReadOnlyList<GameSystem> Systems
        {
            get
            {
                var list = new List<GameSystem>(_systems.Count);
                for (int i = 0; i < _systems.Count; i++) list.Add(_systems[i].System);
                return list.AsReadOnly();
            }
        }

        internal void FinishComposition(IEnumerable<WorldGenStage> stages)
        {
            _composed = true;
            Services.Freeze();
            // Frozen declaration index only; each provider retains its own live operation state.
            _operationBoundaries = Services.Providers<ISimulationOperationBoundary>();
            Commands.Freeze();
            Events.Freeze();
            ISpatialSpace selectedSpace = Space;
            if (selectedSpace == null)
            {
                if (Settings.Space != null) throw new ContentException("selected space has no matching provider");
            }
            else EntityObservation.ReadProvider(this, selectedSpace, null, () => {
                if (Settings.Space != selectedSpace.Id) throw new ContentException("selected space has no matching provider");
                return true;
            }, selectedSpace);
            ValidateSpatialLayers();
            ValidateEntityDefinitions();
            Services.ValidateComposition(this, Systems, stages, Commands.Entries);
        }

        private void ValidateEntityDefinitions()
        {
            foreach (EntityDef entity in Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition presence = entity.Component<ISpatialPresenceDefinition>();
                bool spatial = entity.HasCapability<ISpatialPresence>();
                if (spatial != (presence != null))
                    throw new ContentException(entity.Id + ": spatial capability requires its actual presence definition");
                foreach (ComponentDef part in entity.Components)
                {
                    if (!(part is IComponentCompositionValidation validation)) continue;
                    bool Validate()
                    {
                        if (!entity.IsFrozen || !part.IsFrozen || !ReferenceEquals(part.Owner, entity)
                            || !ReferenceEquals(Content.Get<EntityDef>(entity.Id), entity))
                            throw new ContentException(entity.Id + ": component composition requires actual frozen catalog metadata");
                        if (ReferenceEquals(part, presence))
                        {
                            string space = presence.Space;
                            if (string.IsNullOrWhiteSpace(space)) throw new ContentException(entity.Id + ": spatial presence requires an authored space identity");
                            // Other scenarios' presence definitions must not require this session's geometry.
                            if (space != Settings.Space) return true;
                        }
                        validation.ValidateComposition(this, entity);
                        return true;
                    }
                    EntityObservation.ReadProvider(this, part, null, () => Space == null ? Validate()
                        : EntityObservation.ReadProvider(this, Space, null, Validate));
                }
            }
        }

        private void ValidateSpatialLayers()
        {
            if (Space == null) return; // A deliberately nonspatial session has no selected layers.
            EntityObservation.ReadProvider(this, Space, null, () => {
                var registered = new Dictionary<string, ISpatialLayer>(StringComparer.Ordinal);
                if (Space is ISpatialLayerProvider provider)
                {
                    IReadOnlyList<ISpatialLayer> layers = provider.SpatialLayers;
                    if (layers == null) throw new ContentException("spatial provider must expose its registered layers");
                    for (int i = 0; i < layers.Count; i++)
                    {
                        ISpatialLayer layer = layers[i];
                        if (layer == null || string.IsNullOrWhiteSpace(layer.Id)) throw new ContentException("spatial provider registered a layer without identity");
                        if (registered.ContainsKey(layer.Id)) throw new ContentException("spatial provider registered duplicate layer '" + layer.Id + "'");
                        ISpatialLayerDefinition definition = Content.Find<ISpatialLayerDefinition>(layer.Id);
                        if (definition == null || definition.Space.Id != Settings.Space || layer.Space != Settings.Space)
                            throw new ContentException("spatial provider registered unselected layer '" + layer.Id + "'");
                        if (layer.Type != definition.Type || layer.ValueType != definition.ValueType || layer.Visibility != definition.Visibility
                            || !ReferenceEquals(layer.Unit, definition.Unit) || !ReferenceEquals(layer.Resource, definition.Resource)
                            || layer.ResourceMeasure != definition.ResourceMeasure || layer.VerticalDomain != definition.VerticalDomain
                            || !ReferenceEquals(layer.VerticalUnit, definition.VerticalUnit) || layer.VerticalReference != definition.VerticalReference
                            || layer.VerticalOffset != definition.VerticalOffset || layer.Metadata == null || !layer.Metadata.Frozen
                            || !ReferenceEquals(layer.Metadata, definition.Metadata) && layer.Metadata.ToString() != definition.Metadata.ToString())
                            throw new ContentException("spatial provider layer '" + layer.Id + "' differs from its authored contract");
                        if (layer.Owner != PlayerId.None && !Players.IsSeat(layer.Owner)
                            || layer.Visibility == SpatialLayerVisibility.Public && layer.Owner != PlayerId.None)
                            throw new ContentException("spatial provider layer '" + layer.Id + "' has invalid ownership");
                        if (!layer.CanRead(PlayerId.None)) throw new ContentException("spatial provider layer '" + layer.Id + "' violates observer visibility");
                        for (int seat = 0; seat < Settings.Seats.Length; seat++)
                        {
                            var viewer = new PlayerId(seat);
                            if (layer.CanRead(viewer) != SpatialLayerAccess.CanRead(layer.Visibility, layer.Owner, viewer))
                                throw new ContentException("spatial provider layer '" + layer.Id + "' violates company visibility");
                        }
                        registered.Add(layer.Id, layer);
                    }
                }
                foreach (ISpatialLayerDefinition definition in Content.All<ISpatialLayerDefinition>())
                    if (definition.Space.Id == Settings.Space && !registered.ContainsKey(definition.Id))
                        throw new ContentException("selected space provider did not register spatial layer '" + definition.Id + "'");
                return true;
            });
        }

        private void InvokeStartup(string phase, Action callback)
        {
            RequireHealthyUpdate();
            try { callback(); RequireHealthyUpdate(); }
            catch (Exception error) { LatchFault(phase, error); throw; }
        }

        internal void InitializeSystems()
        {
            RequireHealthyUpdate();
            if (!_composed) throw new InvalidOperationException("compose before initializing systems");
            if (_initialized) return;
            _initialized = true;
            for (int i = 0; i < _systems.Count; i++)
            {
                SystemSlot slot = _systems[i];
                InvokeStartup("initialize." + slot.Metadata.Id, () => slot.System.Initialize(this));
            }
        }

        internal void StartNewGame()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                SystemSlot slot = _systems[i];
                InvokeStartup("new-game." + slot.Metadata.Id, () => slot.System.OnNewGame(this));
            }
            InvokeStartup("new-game.flush", () => Entities.Flush());
            InvokeStartup("new-game.events", () => Events.Dispatch());
            InvokeStartup("new-game.flush", () => Entities.Flush());
            InvokeStartup("new-game.visibility", () => (Space as ISpatialVisibilityInvalidation)?.RefreshVisibility(this));
        }

        internal void GenerateNewGame(IReadOnlyList<WorldGenStage> stages, ScenarioDef scenario)
        {
            RequireHealthyUpdate();
            if (Generating) throw new InvalidOperationException("world generation cannot reenter");
            InitializeSystems();
            Generating = true;
            try
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    WorldGenStage stage = stages[i];
                    InvokeStartup("world.generate", () => stage.Generate(this, scenario));
                }
            }
            finally { Generating = false; }
            StartNewGame();
        }

        // ------------------------------------------------------------------ entities

        /// <summary>Creates an entity: configure/local OnCreate, register, then capability Initialize.
        /// Restore and state-preserving replacement do not grant new-creation effects.</summary>
        public Entity Spawn(EntityDef def, PlayerId owner, Action<Entity> configure = null)
        {
            RequireHealthyUpdate();
            if (def == null || !ReferenceEquals(Content.Find<EntityDef>(def.Id), def))
                throw new ArgumentException("definition is not from this session", nameof(def));
            if (def.HasCapability<ISpatialPresence>())
            {
                ISpatialPresenceDefinition presence = def.Component<ISpatialPresenceDefinition>();
                if (presence == null) throw new ContentException(def.Id + ": spatial capability has no spatial definition");
                if (Space == null) throw new InvalidOperationException("spatial entity requires a selected provider");
                if (presence.Space != Space.Id)
                    throw new InvalidOperationException(def.Id + ": spatial entity belongs to space '" + presence.Space + "', not selected space '" + Space.Id + "'");
            }
            string phase = "entity.create";
            try
            {
                Entity entity = Entities.Create(def, owner, Tick, configure, Space, RequireHealthyUpdate, FaultCreation);
                phase = "entity.initialize";
                _entityInitializationDepth = checked(_entityInitializationDepth + 1);
                try
                {
                    foreach (Component component in entity.Components)
                        if (component is IEntityInitialization initialization)
                        {
                            initialization.Initialize(this);
                            RequireHealthyUpdate();
                        }
                }
                finally { _entityInitializationDepth--; }
                return entity;
            }
            catch (Exception ex)
            {
                LatchFault(phase, ex);
                throw;
            }
        }

        public Code CheckDespawn(Entity entity) => CheckRemoval(entity, null, null);

        // Actual Despawn still checks every removal guard. Replacement passes its explicit
        // target so a capability's retained-state policy, not its retirement policy, applies.
        internal Code CheckRemoval(Entity entity, IEntityRemovalGuard resolvedByOperation, EntityDef replacement)
        {
            RequireHealthyUpdate();
            if (entity == null || !ReferenceEquals(Entities.Get(entity.Id), entity)) return Codes.NotFound;
            Code refusal = Code.None;
            foreach (Component component in entity.Components)
            {
                Code result;
                if (replacement != null && component is IReplacementGuard retained)
                    result = EntityObservation.ReadProvider(this, retained, entity,
                        () => retained.CheckReplacement(this, replacement), space: Space, observeTargetState: true);
                else if (component is IEntityRemovalGuard guard && !ReferenceEquals(guard, resolvedByOperation))
                    result = EntityObservation.ReadProvider(this, guard, entity,
                        () => guard.CheckRemoval(this), space: Space, observeTargetState: true);
                else continue;
                if (refusal.IsNone && !result.IsNone) refusal = result;
            }
            return refusal;
        }

        /// <summary>Removes only an entity whose capabilities permit it; never silently discards protected state.</summary>
        public void Despawn(Entity entity)
        {
            Code refusal = CheckDespawn(entity);
            if (!refusal.IsNone) throw new InvalidOperationException("entity removal refused: " + refusal.Id);
            try
            {
                Entities.Destroy(entity, Space);
                // A provider may catch a nested terminal failure; it cannot turn the outer
                // removal into successful gameplay or bypass the existing first-fault owner.
                RequireHealthyUpdate();
            }
            catch (Exception error) { LatchFault("entity.remove", error); throw; }
        }

        /// <summary>Hands a seat to a person or to the engine's autopilot (from now on).</summary>
        public void SetController(PlayerId seat, Controller controller)
        {
            Code refusal = CheckController(seat, controller);
            if (!refusal.IsNone) throw new InvalidOperationException("controller selection refused: " + refusal);
            Players.SetController(seat, controller);
        }
        public Code CheckController(PlayerId seat, Controller controller)
        {
            if (!Players.IsSeat(seat)) return Codes.NotSeated;
            if (controller != Controller.Human && controller != Controller.Autopilot) return Codes.InvalidArgument;
            return Services.TryGet<Standard.IPlayerControllers>(out var controllers) ? controllers.CheckSelection(seat, controller) : Code.None;
        }
        internal bool IsPendingCommand(PlayerId company, Code stream, int sequence) => _inbox.IsPending(company, stream, sequence);
        internal bool WasCommandIssued(PlayerId company, Code stream, int sequence) => _inbox.WasIssued(company, stream, sequence);
        internal int LastCommandSequence(PlayerId company, Code stream)
        {
            RequireHealthyUpdate();
            if (!Players.IsSeat(company)) throw new ArgumentOutOfRangeException(nameof(company));
            return _inbox.LastSequence(company, stream);
        }
        internal bool PendingCommandMatches(PlayerId company, Code stream, int sequence, string identity, long issued)
            => _inbox.PendingMatches(company, stream, sequence, identity, issued);

        // ------------------------------------------------------------------ input

        /// <summary>
        /// Queues an order for the next tick, stamping who sent it, when, and its per-seat sequence. The
        /// one place commands are stamped. Submission is thread-safe and snapshots the payload;
        /// execution, previews and views remain on the game thread.
        /// </summary>
        public int Submit(PlayerId player, Command command, Code stream) => Submit(player, command, stream, null);
        internal object InputGate => _inbox.Gate;
        internal Action<Command> StampedInputObserver { get; set; }
        internal int Submit(PlayerId player, Command command, Code stream, Action<Command> admitted)
        {
            RequireHealthyUpdate();
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!Players.IsSeat(player)) throw new ArgumentOutOfRangeException(nameof(player), "not a seat of this game");
            return _inbox.Submit(player, stream, command, Tick, admitted);
        }

        /// <summary>Queues a command already stamped elsewhere (a lockstep peer or a replay).</summary>
        internal void SubmitStamped(Command command)
        {
            RequireHealthyUpdate();
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!Players.IsSeat(command.Player)) throw new ArgumentOutOfRangeException(nameof(command), "not a seat of this game");
            if (command.IssuedTick < 0 || command.Sequence <= 0 || !_inbox.Supports(command.Stream))
                throw new ArgumentException("a stamped command needs a selected producer stream, non-negative tick and positive sequence", nameof(command));
            _inbox.SubmitStamped(command, StampedInputObserver);
        }

        /// <summary>The same check the command will face when applied now. Changes nothing.</summary>
        public Code Check(PlayerId player, Command command)
        {
            RequireHealthyUpdate();
            if (!Players.IsSeat(player)) return Codes.NotSeated;
            if (!Commands.TryGet(command.GetType(), out CommandRegistry.Entry entry)) return Codes.UnknownCommand;
            PlayerId was = command.Player;
            command.Player = player;
            try { return entry.Handler.Check(this, command); }
            finally { command.Player = was; }
        }

        // ------------------------------------------------------------------ news

        public void Notify(Code kind, PlayerId player, EntityId entity = default, string subject = null, Fix amount = default, PlayerId? other = null)
        {
            var n = new Notification
            {
                Sequence = Notifications.NextSequence, Tick = Tick, Kind = kind, Player = player,
                Other = other ?? PlayerId.None, Entity = entity, Subject = subject, Amount = amount
            };
            Notifications.Append(n);
        }

        public IReadOnlyList<CommandResult> LastTickResults => _tickResults.AsReadOnly();

        // ------------------------------------------------------------------ the tick

        /// <summary>True while a tick runs. Derived caches key on it so what a presenter asks between ticks can never change what the simulation sees.</summary>
        public bool Stepping { get; private set; }
        internal bool UpdatingEntities { get; private set; }

        /// <summary>Runs one tick. Returns false if the game has ended or the world is (or becomes) faulted.</summary>
        public bool Step()
        {
            if (Fault != null || !Outcome.IsNone) return false;
            if (Stepping)
            {
                var error = new InvalidOperationException("the engine game update cannot recursively advance itself");
                LatchFault("game.update", error);
                throw error;
            }
            RequireCompletedOperations();
            string phase = "begin";
            Stepping = true;
            try
            {
                long completedTick = GameClock.NextStep();
                ComponentList<ISpatialPresence> presences = Entities.Components<ISpatialPresence>();
                for (int i = 0; i < presences.Count; i++) if (presences[i].Entity.Alive)
                {
                    presences[i].BeginTick();
                    RequireHealthyUpdate();
                }

                phase = "commands";
                ProcessCommands();
                RequireHealthyUpdate();
                Entities.Flush();

                for (int i = 0; i < _systems.Count; i++)
                {
                    GameSystem sys = _systems[i].System;
                    Schedule sch = _systems[i].Schedule;
                    if (!sch.IsDue(Tick)) continue;
                    phase = _systems[i].Metadata.Id;
                    long span = sch.Kind == ScheduleKind.EveryTick ? 1 : sch.Period;
                    int bucket = sch.Kind == ScheduleKind.Spread ? (int)(Tick % sch.Period) : 0;
                    sys.Update(this, new TickInfo(Tick, span, bucket, Time));
                    RequireHealthyUpdate();
                    Entities.Flush();
                }

                phase = "events";
                Events.Dispatch();
                RequireHealthyUpdate();
                Entities.Flush();
                IReadOnlyList<Entity> entities = Entities.All;
                phase = "entities";
                UpdatingEntities = true;
                for (int i = 0; i < entities.Count; i++)
                {
                    Entity entity = entities[i];
                    if (!entity.Alive) continue;
                    entity.Update(new EntityUpdateContext(this, entity));
                    RequireHealthyUpdate();
                }
                UpdatingEntities = false;
                for (int i = 0; i < _systems.Count; i++)
                {
                    phase = _systems[i].Metadata.Id + ".complete";
                    _systems[i].System.CompleteUpdate(this);
                    RequireHealthyUpdate();
                }
                phase = "spatial.visibility";
                (Space as ISpatialVisibilityInvalidation)?.RefreshVisibility(this);
                RequireHealthyUpdate();
                GameClock.CompleteStep(completedTick);
                if (Settings.HorizonTicks > 0 && Tick >= Settings.HorizonTicks) End(HorizonOutcome);
                return true;
            }
            catch (Exception ex)
            {
                LatchFault(phase, ex);
                return false;
            }
            finally { UpdatingEntities = false; Stepping = false; }
        }

        private void ProcessCommands()
        {
            _tickResults.Clear();
            Command[] batch = _inbox.Drain();
            if (batch.Length == 0) return;
            // Fairness: which seat goes first rotates every tick, so no seat always wins a same-tick race.
            int seats = global::System.Math.Max(1, Players.Count);
            int first = (int)(Tick % seats);
            Array.Sort(batch, (a, b) =>
            {
                int c = a.IssuedTick.CompareTo(b.IssuedTick);
                if (c != 0) return c;
                int ra = (a.Player.Value - first + seats) % seats, rb = (b.Player.Value - first + seats) % seats;
                c = ra.CompareTo(rb);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Stream.Id, b.Stream.Id);
                return c != 0 ? c : a.Sequence.CompareTo(b.Sequence);
            });
            for (int i = 0; i < batch.Length; i++)
            {
                Command cmd = batch[i];
                var result = new CommandResult { Player = cmd.Player, Stream = cmd.Stream, Sequence = cmd.Sequence, Tick = Tick };
                if (!Commands.TryGet(cmd.GetType(), out CommandRegistry.Entry entry))
                {
                    result.Command = cmd.GetType().Name;
                    result.Refusal = Codes.UnknownCommand;
                }
                else
                {
                    result.Command = entry.Id;
                    Code refusal = entry.Handler.Check(this, cmd);
                    RequireHealthyUpdate();
                    if (refusal.IsNone)
                    {
                        _context.Reset();
                        entry.Handler.Apply(this, cmd, _context);
                        RequireHealthyUpdate();
                        result.Entity = _context.Entity;
                        result.Arg0 = _context.Arg0;
                        result.Arg1 = _context.Arg1;
                    }
                    result.Accepted = refusal.IsNone;
                    result.Refusal = refusal;
                }
                _tickResults.Add(result);
                Results.Append(result);
            }
        }

        // ------------------------------------------------------------------ persistence

        /// <summary>Stable world capture/advancement cannot begin inside an opted-in provider call.</summary>
        internal void RequireCompletedProviderCalls()
        {
            if (ActiveProviderCalls.Count == 0) return;
            var error = new InvalidOperationException("provider callback must complete before advancing or checkpointing the world");
            LatchFault("provider.operation", error); throw error;
        }

        internal void RequireCompletedOperations()
        {
            RequireCompletedProviderCalls();
            Entities.RequireCompletedLifecycle();
            if (_entityInitializationDepth != 0)
                throw new InvalidOperationException("entity capability initialization must complete before advancing or checkpointing the world");
            foreach (ISimulationOperationBoundary boundary in _operationBoundaries)
                if (EntityObservation.ReadProvider(this, boundary, null, boundary,
                    static provider => provider.OperationInProgress))
                    throw new InvalidOperationException("simulation operation must complete before advancing or checkpointing the world");
        }

        /// <summary>Applies pending spawns and removals, so the state read next is the settled state.</summary>
        public void Settle() => Entities.Flush();

        /// <summary>Saves, loads or hashes the whole simulation state. Settle before writing. Restore validates declarations before initialization or payload callbacks.</summary>
        public void Sync(IStateSync s)
        {
            RequireCompletedOperations();
            if (s.Reading)
            {
                RequireStateMutation();
                SyncState(s);
                return;
            }
            RequireCheckpointWrite();
            EncodeCheckpoint(() => { SyncState(s); return s; });
        }

        // One original codec traversal for both legitimate restoration and read-only encoding.
        private void SyncState(IStateSync s)
        {
            SyncCodecManifest(s);
            GameModeConfiguration.SyncIdentity(s, GameMode);
            if (s.Reading) InitializeSystems();
            SyncCore(s);
            if (Space != null) { s.Section(Space.Id); Space.Sync(s); }
            Entities.Sync(s, Content);
            if (s.Reading) Space?.OnLoaded(this);
            SyncSystems(s);
            SyncServices(s);
            if (s.Reading)
            {
                for (int i = 0; i < _systems.Count; i++) _systems[i].System.OnLoaded(this);
                (Space as ISpatialVisibilityInvalidation)?.RefreshVisibility(this);
            }
            else RequireHealthyUpdate();
        }

        private T EncodeCheckpoint<T>(Func<T> encode)
        {
            var facts = new SpatialMutation.Facts(this);
            int previousCapture = ObservationCaptureDepth;
            try
            {
                ObservationCaptureDepth = checked(previousCapture + 1);
                T result = encode();
                facts.Verify();
                RequireHealthyUpdate();
                return result;
            }
            catch (Exception error) { LatchFault("checkpoint.encode", error); throw; }
            finally { ObservationCaptureDepth = previousCapture; }
        }

        // Readiness is a normal refusal, outside the codec-failure boundary.
        private void RequireCheckpointWrite()
        {
            if (Fault != null) throw new InvalidOperationException("a faulted partial operation cannot checkpoint");
            if (Entities.HasPending) throw new InvalidOperationException("the world has unsettled spawns; call Settle before saving or hashing");
            if (Events.HasPending) throw new InvalidOperationException("the world has pending events; finish Update before saving or hashing");
        }

        /// <summary>The clock, outcome, random state, seats, sequences and the orders queued for the next tick.</summary>
        private void SyncCore(IStateSync s)
        {
            lock (_inbox.Gate)
            {
                s.Section("world");
                GameClock.Sync(s);
                string outcome = Outcome.Id; s.String(ref outcome); Outcome = outcome == null ? Code.None : new Code(outcome);
                ulong rng = Random.State; s.ULong(ref rng);
                if (s.Reading)
                {
                    if (rng == 0) throw new CorruptStateException("random stream has zero state");
                    Random.State = rng;
                }
                _inbox.SyncSequences(s);
                Players.Sync(s);
                _inbox.SyncPending(s);
            }
        }

        private void SyncSystems(IStateSync s)
        {
            s.Section("systems");
            int ns = s.Count(_systems.Count);
            if (ns != _systems.Count) throw new CorruptStateException("system count differs from the save");
            for (int i = 0; i < _systems.Count; i++)
            {
                s.Section(_systems[i].Metadata.Id);
                _systems[i].System.Sync(s);
            }
        }

        private void SyncServices(IStateSync s)
        {
            s.Section("services");
            IReadOnlyList<PersistentServiceRegistration> services = Services.Persistent();
            int nv = s.Count(services.Count);
            if (nv != services.Count) throw new CorruptStateException("service count differs from the save");
            for (int i = 0; i < services.Count; i++)
            {
                s.Section(services[i].Metadata.Id);
                if (!(services[i].Provider is ISpatialSpace)) services[i].Provider.Sync(s);
            }
        }

        public ulong Hash()
        {
            RequireCompletedOperations();
            RequireHealthyUpdate();
            Settle();
            var h = new StateHasher();
            Sync(h);
            return h.Value;
        }

        internal IReadOnlyList<StateCodecRegistration> StateCodecs
        {
            get
            {
                var entries = new List<StateCodecRegistration>(Content.ComponentCodecs);
                for (int i = 0; i < _systems.Count; i++) entries.Add(_systems[i].Metadata);
                foreach (PersistentServiceRegistration service in Services.Persistent()) entries.Add(service.Metadata);
                entries.Sort((a, b) => { int kind = a.Kind.CompareTo(b.Kind); return kind != 0 ? kind : string.CompareOrdinal(a.Id, b.Id); });
                return entries.AsReadOnly();
            }
        }

        private void SyncCodecManifest(IStateSync s)
        {
            s.Section("session-time");
            string time = Settings.Time; long numerator = Time.QuantumNumerator, denominator = Time.QuantumDenominator, horizon = Settings.HorizonTicks;
            s.String(ref time); s.Long(ref numerator); s.Long(ref denominator); s.Long(ref horizon);
            if (time != Settings.Time || numerator != Time.QuantumNumerator || denominator != Time.QuantumDenominator || horizon != Settings.HorizonTicks)
                throw new CorruptStateException("session time declaration differs");
            string space = Settings.Space; s.String(ref space);
            if (space != Settings.Space) throw new CorruptStateException("session space declaration differs");
            s.Section("state-codecs");
            IReadOnlyList<StateCodecRegistration> entries = StateCodecs;
            if (s.Count(entries.Count) != entries.Count) throw new CorruptStateException("state codec count differs from the save");
            for (int i = 0; i < entries.Count; i++) entries[i].SyncIdentity(s);
            if (s.Count(Content.ComponentDeclarations.Count) != Content.ComponentDeclarations.Count)
                throw new CorruptStateException("component capability schema count differs from the save");
            for (int i = 0; i < Content.ComponentDeclarations.Count; i++)
            {
                ContentTypes.ComponentType component = Content.ComponentDeclarations[i];
                string id = component.Metadata.Id;
                bool operation = component.OperationSchema != null, signal = component.SignalSchema != null;
                s.String(ref id); s.Bool(ref operation); s.Bool(ref signal);
                if (id != component.Metadata.Id || operation != (component.OperationSchema != null) || signal != (component.SignalSchema != null))
                    throw new CorruptStateException("component capability declaration differs: " + component.Metadata.Id);
                component.OperationSchema?.SyncIdentity(s);
                component.SignalSchema?.SyncIdentity(s);
            }
            // Execution order and schedule are declarations, not mutable provider state.
            if (s.Count(_systems.Count) != _systems.Count) throw new CorruptStateException("scheduled system count differs from the save");
            for (int i = 0; i < _systems.Count; i++)
            {
                SystemSlot slot = _systems[i];
                string id = slot.Metadata.Id;
                int order = slot.Order, kind = (int)slot.Schedule.Kind;
                long period = slot.Schedule.Period, phase = slot.Schedule.Phase;
                s.String(ref id); s.Int(ref order); s.Int(ref kind); s.Long(ref period); s.Long(ref phase);
                if (id != slot.Metadata.Id || order != slot.Order || kind != (int)slot.Schedule.Kind || period != slot.Schedule.Period || phase != slot.Schedule.Phase)
                    throw new CorruptStateException("system scheduling declaration differs: " + slot.Metadata.Id);
            }
        }

        /// <summary>One hash per part of the state, for finding where two worlds diverged (a desync report).</summary>
        public IReadOnlyList<KeyValuePair<string, ulong>> HashBreakdown()
        {
            RequireCompletedOperations();
            RequireHealthyUpdate();
            Settle();
            RequireCheckpointWrite();
            return EncodeCheckpoint(HashBreakdownCore);
        }

        private IReadOnlyList<KeyValuePair<string, ulong>> HashBreakdownCore()
        {
            var parts = new List<KeyValuePair<string, ulong>>();
            var h = new StateHasher();
            SyncCodecManifest(h);
            parts.Add(new KeyValuePair<string, ulong>("codecs", h.Value));
            h = new StateHasher();
            GameModeConfiguration.SyncIdentity(h, GameMode);
            parts.Add(new KeyValuePair<string, ulong>("game-mode", h.Value));
            h = new StateHasher();
            SyncCore(h);
            parts.Add(new KeyValuePair<string, ulong>("core", h.Value));
            h = new StateHasher(); Entities.Sync(h, Content); parts.Add(new KeyValuePair<string, ulong>("entities", h.Value));
            for (int i = 0; i < _systems.Count; i++)
            {
                h = new StateHasher(); _systems[i].System.Sync(h);
                parts.Add(new KeyValuePair<string, ulong>("system:" + _systems[i].Metadata.Id, h.Value));
            }
            IReadOnlyList<PersistentServiceRegistration> services = Services.Persistent();
            for (int i = 0; i < services.Count; i++)
            {
                h = new StateHasher(); services[i].Provider.Sync(h);
                parts.Add(new KeyValuePair<string, ulong>("service:" + services[i].Metadata.Id, h.Value));
            }
            return parts;
        }
    }
}
