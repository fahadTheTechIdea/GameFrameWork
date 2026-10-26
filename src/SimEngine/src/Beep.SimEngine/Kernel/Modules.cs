using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Beep.SimEngine.Content;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Kernel
{
    /// <summary>
    /// One module of a game (core, oil, a mod): the content types it understands and what it adds
    /// to a world — systems, services, commands, map layers, world-generation stages, view features. Its
    /// id is also the id of its content module in the manifest.
    /// </summary>
    public abstract class GameModule
    {
        public abstract string Id { get; }
        public abstract ModuleVersion Version { get; }

        /// <summary>Modules that must be composed before this one.</summary>
        public virtual IReadOnlyList<ModuleRequirement> Requires => Array.Empty<ModuleRequirement>();

        /// <summary>Declares the def sections and component keys this module's content uses.</summary>
        public virtual void DeclareContent(ContentTypes types) { }

        /// <summary>Adds this module's parts to a world being built.</summary>
        public virtual void Compose(WorldComposer composer) { }
    }

    /// <summary>Captured module identity owned by a game definition, not live module getters.</summary>
    public sealed class ModuleRegistration
    {
        internal ModuleRegistration(GameModule implementation)
        {
            Implementation = implementation;
            Id = implementation.Id;
            ModuleRequirement.ValidateId(Id);
            Version = implementation.Version;
            IReadOnlyList<ModuleRequirement> supplied = implementation.Requires
                ?? throw new InvalidOperationException("module '" + Id + "' has null requirements");
            var requirements = new ModuleRequirement[supplied.Count];
            for (int i = 0; i < requirements.Length; i++)
            {
                ModuleRequirement requirement = supplied[i];
                ModuleRequirement.ValidateId(requirement.Id);
                requirements[i] = requirement;
            }
            Array.Sort(requirements, (a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (int i = 1; i < requirements.Length; i++)
                if (requirements[i - 1].Id == requirements[i].Id)
                    throw new InvalidOperationException("duplicate module requirement '" + requirements[i].Id + "'");
            Requires = Array.AsReadOnly(requirements);
        }

        public string Id { get; }
        public ModuleVersion Version { get; }
        public IReadOnlyList<ModuleRequirement> Requires { get; }
        internal GameModule Implementation { get; }
    }

    /// <summary>A step of world generation (terrain, water, resources, starts), run once for a new game.</summary>
    public abstract class WorldGenStage
    {
        public abstract string Id { get; }
        public abstract void Generate(World world, ScenarioDef scenario);
    }

    /// <summary>What a module uses to add itself to a world while it is being composed.</summary>
    public sealed class WorldComposer
    {
        private readonly List<KeyValuePair<int, WorldGenStage>> _stages = new List<KeyValuePair<int, WorldGenStage>>();
        private readonly Dictionary<Type, ViewFeatureRegistration> _features = new Dictionary<Type, ViewFeatureRegistration>();
        private readonly List<(Type Contract, string Id, int SchemaVersion, string Owner)> _featureRequirements
            = new List<(Type, string, int, string)>();
        private bool _featuresFrozen;
        private string _module;

        internal WorldComposer(World world, ScenarioDef scenario)
        {
            World = world;
            Scenario = scenario;
        }

        public World World { get; }
        public ScenarioDef Scenario { get; }
        public ResolvedContent Content => World.Content;
        public ServiceContainer Services => World.Services;
        public CommandRegistry Commands => World.Commands;
        public EventBus Events => World.Events;

        internal void BeginModule(string module)
        {
            _module = module;
            World.ComposingModule = module;
            Services.ComposingModule = module;
            Commands.ComposingModule = module;
            Events.ComposingModule = module;
        }

        internal void EndModule()
        {
            _module = null;
            World.ComposingModule = null;
            Services.ComposingModule = null;
            Commands.ComposingModule = null;
            Events.ComposingModule = null;
        }

        public void AddSystem(GameSystem system, int order) => World.AddSystem(system, order);

        public void AddWorldGen(WorldGenStage stage, int order)
        {
            if (_module == null) throw new InvalidOperationException("world generation is registered by a composing module");
            if (stage == null || string.IsNullOrEmpty(stage.Id)) throw new ArgumentException("a world-generation stage needs an id", nameof(stage));
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Value.Id == stage.Id) throw new InvalidOperationException("world-gen id '" + stage.Id + "' used twice");
                if (_stages[i].Key == order) throw new InvalidOperationException("world-gen stages '" + _stages[i].Value.Id + "' and '" + stage.Id + "' share order " + order);
            }
            _stages.Add(new KeyValuePair<int, WorldGenStage>(order, stage));
        }

        /// <summary>
        /// Adds a read model the player views expose (e.g. a company panel), built once per viewer by
        /// <paramref name="factory"/>. Hosts reach it through <c>view.Feature&lt;T&gt;()</c>; it must only read,
        /// and it must show a viewer only what that company may see.
        /// </summary>
        public void AddViewFeature<T>(string id, int schemaVersion, Func<World, PlayerId, T> factory) where T : class
        {
            CheckViewFeature(typeof(T), id, schemaVersion);
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _features.Add(typeof(T), new ViewFeatureRegistration(id, schemaVersion, _module, typeof(T), (w, p) => factory(w, p)));
        }

        /// <summary>Explicit interface binding to the SAME cached read model in each actual player projection.</summary>
        public void AddViewFeatureAlias<TContract, TPrimary>(string id, int schemaVersion)
            where TContract : class where TPrimary : class
        {
            CheckViewFeature(typeof(TContract), id, schemaVersion);
            if (!_features.TryGetValue(typeof(TPrimary), out var primary))
                throw new InvalidOperationException("view contract alias requires an already declared primary " + typeof(TPrimary).Name);
            _features.Add(typeof(TContract), new ViewFeatureRegistration(id, schemaVersion, _module, typeof(TContract), primary));
        }

        private void CheckViewFeature(Type contract, string id, int schemaVersion)
        {
            if (_featuresFrozen) throw new InvalidOperationException("view feature declarations are frozen");
            if (_module == null) throw new InvalidOperationException("views are registered by a composing module");
            ContractIdentity.Validate(id, schemaVersion, "view feature");
            if (_features.ContainsKey(contract)) throw new InvalidOperationException("view feature " + contract.Name + " added twice");
            foreach (ViewFeatureRegistration feature in _features.Values)
                if (feature.Id == id) throw new InvalidOperationException("view feature id '" + id + "' owned by '" + feature.Owner + "' used twice");
        }

        /// <summary>Requires this exact view contract, identity and schema before any system Initialize.</summary>
        public void RequireViewFeature<T>(string id, int schemaVersion) where T : class
        {
            if (_featuresFrozen) throw new InvalidOperationException("view feature declarations are frozen");
            if (_module == null) throw new InvalidOperationException("views are required by a composing module");
            ContractIdentity.Validate(id, schemaVersion, "view feature");
            _featureRequirements.Add((typeof(T), id, schemaVersion, _module));
        }

        internal IReadOnlyDictionary<Type, ViewFeatureRegistration> FreezeFeatures()
        {
            foreach (var requirement in _featureRequirements)
                if (!_features.TryGetValue(requirement.Contract, out ViewFeatureRegistration feature)
                    || feature.Id != requirement.Id || feature.SchemaVersion != requirement.SchemaVersion)
                    throw new InvalidOperationException("module '" + requirement.Owner + "' requires view '"
                        + requirement.Id + "' schema " + requirement.SchemaVersion + " for " + requirement.Contract.Name
                        + "; provider missing or identity/schema differs");
            _featuresFrozen = true;
            return new ReadOnlyDictionary<Type, ViewFeatureRegistration>(_features);
        }

        internal List<WorldGenStage> Stages()
        {
            _stages.Sort((a, b) => a.Key.CompareTo(b.Key));
            var list = new List<WorldGenStage>(_stages.Count);
            for (int i = 0; i < _stages.Count; i++) list.Add(_stages[i].Value);
            return list;
        }
    }

    /// <summary>
    /// A scenario: which modules a game uses, the map, the seats it has room for, and per-module
    /// settings. Content, not code; a host only names it.
    /// </summary>
    public sealed class ScenarioDef : Def
    {
        public string Name { get; private set; }
        public IReadOnlyList<string> Modules { get; private set; }
        public ISpatialSpaceDefinition Space { get; private set; }
        private string _space;
        internal string DeclaredSpace => _space;
        /// <summary>Most seats a game of this scenario can have.</summary>
        public int Seats { get; private set; }
        /// <summary>Seat controllers when a request names none: seat 0 human, the rest autopilot.</summary>
        public int DefaultSeats { get; private set; }
        private string _time;
        public SimulationTimeDef Time { get; private set; }
        private JsonValue _settings;
        private string _gameMode;
        private IReadOnlyList<GameModeAssignment> _modeOverrides;
        private GameModeDef _modeDefinition;
        public IGameModeConfiguration GameMode { get; private set; }
        internal string DeclaredGameMode => _gameMode;

        internal IGameModeConfiguration ResolveGameMode(ResolvedContent content, IReadOnlyList<GameModeAssignment> requestOverrides)
        {
            if (requestOverrides.Count == 0) return GameMode;
            return GameModeConfiguration.Resolve(content, _modeDefinition, _modeOverrides, AuthoringPath + ".request_overrides", requestOverrides);
        }

        protected override void ReadCore(JsonReader r)
        {
            Name = r.String("name", Id);
            Modules = Array.AsReadOnly(r.Strings("modules"));
            if (Modules.Count == 0) throw r.Error("a scenario lists its modules");
            JsonValue space = r.Value("space");
            if (space != null && space.Kind != JsonKind.Null && space.Kind != JsonKind.String) throw r.Error("space must name a definition or be null");
            _space = space?.Kind == JsonKind.String ? space.Text : null;
            if (_space != null && string.IsNullOrWhiteSpace(_space)) throw r.Error("space must name a definition or be null");
            Seats = r.Int("seats", 4);
            DefaultSeats = r.Int("default_seats", 1);
            if (Seats < 1 || DefaultSeats < 1 || DefaultSeats > Seats) throw r.Error("need 1 <= default_seats <= seats");
            _time = r.String("time");
            _gameMode = r.String("game_mode");
            GameModePropertyDef.ValidateId(_gameMode, "game mode", r);
            if (!r.Has("mode_overrides")) throw r.Error("mode_overrides must be explicitly authored, including an empty object");
            _modeOverrides = GameModeConfiguration.ReadAssignments(r.Object("mode_overrides"), r.Where + ".mode_overrides");
            _settings = (r.Object("settings") ?? JsonValue.NewObject()).Clone();
            _settings.Freeze();
        }

        /// <summary>A strict reader over <c>settings.&lt;module&gt;</c>, or null. The module must call End().</summary>
        public JsonReader SettingsFor(string module)
        {
            JsonValue v = _settings.Get(module);
            return v == null ? null : new JsonReader(v, "scenario " + Id + ".settings." + module);
        }

        protected override void ResolveCore(ContentResolver c)
        {
            Time = c.Def<SimulationTimeDef>(_time); Space = c.OptionalDef<ISpatialSpaceDefinition>(_space);
            _modeDefinition = c.Def<GameModeDef>(_gameMode);
        }

        protected override void ValidateResolvedCore(ContentResolver c)
        {
            GameMode = GameModeConfiguration.Resolve(c.Content, _modeDefinition, _modeOverrides, AuthoringPath);
        }

        public bool HasModule(string module)
        {
            for (int i = 0; i < Modules.Count; i++) if (Modules[i] == module) return true;
            return false;
        }
    }

    /// <summary>
    /// A whole game made of modules: which exist, and how a world is put together from them. The
    /// factory uses this; a game assembly (e.g. OilCo.Game) provides one.
    /// </summary>
    public sealed class GameDefinition
    {
        private readonly List<ModuleRegistration> _modules = new List<ModuleRegistration>();
        private readonly IReadOnlyList<ModuleRegistration> _moduleView;
        private readonly ContentTypes _types;

        public GameDefinition(string name, IEnumerable<GameModule> modules)
        {
            Name = name;
            _types = new ContentTypes(this);
            _types.Section<ScenarioDef>("scenarios");
            _types.Section<App.InputMapDef>("input_maps");
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            var remaining = new List<ModuleRegistration>();
            foreach (GameModule m in modules)
            {
                if (m == null) throw new ArgumentException("null module", nameof(modules));
                var registration = new ModuleRegistration(m);
                for (int i = 0; i < remaining.Count; i++)
                    if (remaining[i].Id == registration.Id) throw new InvalidOperationException("module '" + registration.Id + "' listed twice");
                remaining.Add(registration);
            }
            for (int i = 0; i < remaining.Count; i++)
                for (int q = 0; q < remaining[i].Requires.Count; q++)
                {
                    ModuleRequirement dependency = remaining[i].Requires[q];
                    ModuleRegistration provider = remaining.Find(m => m.Id == dependency.Id);
                    if (provider == null || provider.Version != dependency.Version)
                        throw new InvalidOperationException("module '" + remaining[i].Id + "' requires " + dependency.Id + "@" + dependency.Version + "; provider missing or version differs");
                }
            remaining.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            while (remaining.Count > 0)
            {
                int ready = -1;
                for (int i = 0; i < remaining.Count; i++)
                {
                    bool satisfied = true;
                    for (int q = 0; q < remaining[i].Requires.Count; q++)
                        if (Find(remaining[i].Requires[q].Id) == null) satisfied = false;
                    if (satisfied) { ready = i; break; }
                }
                if (ready < 0) throw new InvalidOperationException("module dependency cycle in " + Name);
                ModuleRegistration module = remaining[ready];
                remaining.RemoveAt(ready);
                _modules.Add(module);
            }
            _moduleView = _modules.AsReadOnly();
            // Validate the complete graph before any module declaration callback can run.
            for (int i = 0; i < _modules.Count; i++)
            {
                ModuleRegistration module = _modules[i];
                _types.BeginModule(module.Id);
                try { module.Implementation.DeclareContent(_types); }
                finally { _types.EndModule(); }
            }
            _types.Freeze();
        }

        public string Name { get; }
        public ContentTypes ContentTypes => _types;
        public IReadOnlyList<ModuleRegistration> Modules => _moduleView;

        public ModuleRegistration Find(string id)
        {
            for (int i = 0; i < _modules.Count; i++) if (_modules[i].Id == id) return _modules[i];
            return null;
        }

        /// <summary>The modules for a game, in definition order, with every requirement included.</summary>
        public IReadOnlyList<ModuleRegistration> Select(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids) Include(id, wanted);
            var list = new List<ModuleRegistration>();
            for (int i = 0; i < _modules.Count; i++) if (wanted.Contains(_modules[i].Id)) list.Add(_modules[i]);
            return list.AsReadOnly();
        }

        private void Include(string id, HashSet<string> wanted)
        {
            ModuleRegistration m = Find(id) ?? throw new InvalidOperationException("no module '" + id + "' in " + Name);
            if (!wanted.Add(id)) return;
            for (int q = 0; q < m.Requires.Count; q++) Include(m.Requires[q].Id, wanted);
        }

        /// <summary>
        /// Builds a world: resolve content for the modules, compose every module, then either generate a
        /// new map (<paramref name="generate"/>) or prepare an uninitialized world for restore. Restore
        /// validates the saved declarations through World.Sync before initializing systems and reading payloads.
        /// </summary>
        public World Build(Hosting.ContentSet contentSet, WorldSettings settings, bool generate, out IReadOnlyDictionary<Type, ViewFeatureRegistration> features)
        {
            IReadOnlyList<ModuleRegistration> modules = Select(settings.Modules);
            var ids = new string[modules.Count];
            for (int i = 0; i < modules.Count; i++) ids[i] = modules[i].Id;
            if (settings.ModuleVersions.Length > 0)
            {
                if (settings.ModuleVersions.Length != ids.Length || settings.Modules.Length != ids.Length)
                    throw new CorruptStateException("saved module identity is incomplete");
                for (int i = 0; i < ids.Length; i++)
                    if (settings.Modules[i] != ids[i] || settings.ModuleVersions[i] != modules[i].Version)
                        throw new CorruptStateException("saved module '" + settings.Modules[i] + "' version/order differs");
            }
            settings.Modules = ids;
            settings.ModuleVersions = new ModuleVersion[ids.Length];
            for (int i = 0; i < ids.Length; i++) settings.ModuleVersions[i] = modules[i].Version;
            ResolvedContent content = contentSet.Resolve(ids);
            ScenarioDef scenario = content.Find<ScenarioDef>(settings.Scenario);
            if (scenario == null) throw new ContentException("scenario '" + settings.Scenario + "' is not in the content of modules " + string.Join(",", ids));
            if (settings.Time != scenario.Time.Id) throw new CorruptStateException("session time identity differs from its scenario");
            if (settings.Space != scenario.Space?.Id) throw new CorruptStateException("session space identity differs from its scenario");
            var world = new World(content, settings);
            var composer = new WorldComposer(world, scenario);
            if (scenario.Space != null)
            {
                composer.BeginModule(scenario.Space.Module);
                try { scenario.Space.Compose(composer); }
                finally { composer.EndModule(); }
            }
            for (int i = 0; i < modules.Count; i++)
            {
                composer.BeginModule(modules[i].Id);
                try { modules[i].Implementation.Compose(composer); }
                finally { composer.EndModule(); }
            }
            world.ViewFeatures = composer.FreezeFeatures();
            List<WorldGenStage> stages = composer.Stages();
            world.FinishComposition(stages);
            features = world.ViewFeatures;
            if (generate)
            {
                world.GenerateNewGame(stages, scenario);
            }
            return world;
        }
    }
}
