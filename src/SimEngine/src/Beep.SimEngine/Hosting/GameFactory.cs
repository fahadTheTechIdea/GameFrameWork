using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>A scenario as a new-game menu lists it.</summary>
    public readonly struct ScenarioInfo
    {
        internal ScenarioInfo(ScenarioDef scenario)
        {
            Id = scenario.Id; Name = scenario.Name; Seats = scenario.Seats; DefaultSeats = scenario.DefaultSeats;
            Space = scenario.DeclaredSpace; Modules = Array.AsReadOnly(new List<string>(scenario.Modules).ToArray());
            GameMode = scenario.DeclaredGameMode;
        }
        public string Id { get; }
        public string Name { get; }
        public int Seats { get; }
        public int DefaultSeats { get; }
        public string Space { get; }
        public string GameMode { get; }
        public IReadOnlyList<string> Modules { get; }
    }

    /// <summary>What a save file says about itself, read without loading it.</summary>
    public struct SaveInfo
    {
        public string Game;
        public string Build;
        public string Scenario;
        public ulong Seed;
        public long Tick;
        public CalendarReading Date;
        public int Seats;
        public bool Compatible;
    }

    /// <summary>
    /// The single way a host starts or resumes a game. It holds the game's definition and its loaded
    /// content; every game it makes is ready to run.
    /// </summary>
    public sealed class GameFactory
    {
        private readonly GameDefinition _definition;
        private readonly ContentSet _content;

        public GameFactory(GameDefinition definition, ContentSet content)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string GameName => _definition.Name;

        public ISimulationTime ScenarioTime(string id)
        {
            ScenarioDef scenario = _content.ResolveScenario(id, null).Get<ScenarioDef>(id);
            return new GameTime(scenario.Time);
        }

        /// <summary>Resolved authored mode intent, not a host implementation of profile rules.</summary>
        public IGameModeConfiguration ScenarioGameMode(string id)
            => _content.ResolveScenario(id, null).Get<ScenarioDef>(id).GameMode;

        /// <summary>Pure engine-owned setup resolution; no provider composition, initialization or session state.</summary>
        public IGameModeConfiguration PreviewGameMode(NewGameRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            IReadOnlyList<GameModeAssignment> overrides = GameModeConfiguration.CaptureAssignments(request.ModeOverrides);
            ResolvedContent selected = _content.ResolveScenario(request.Scenario, request.Mods);
            IGameModeConfiguration configuration = selected.Get<ScenarioDef>(request.Scenario).ResolveGameMode(selected, overrides);
            GameModeConfiguration.ValidateSelectedCapabilities(selected, configuration);
            return configuration;
        }

        /// <summary>
        /// The input map the game's content defines (the one named <paramref name="id"/>, "default" unless
        /// asked), or the framework's fallback (clock and quit) if the content has none.
        /// </summary>
        public App.InputMap InputMap(string id = "default")
        {
            App.InputMapDef def = _content.FindEngineDeclaration<App.InputMapDef>("input_maps", id);
            return def == null ? App.InputMap.Default() : def.Build();
        }

        /// <summary>Immutable authored scenario intent, without resolving unrelated gameplay providers.</summary>
        public IReadOnlyList<ScenarioInfo> Scenarios() => _content.Scenarios();

        public SimGame NewGame(NewGameRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            IReadOnlyList<GameModeAssignment> overrides = GameModeConfiguration.CaptureAssignments(request.ModeOverrides);
            ResolvedContent selected = _content.ResolveScenario(request.Scenario, request.Mods);
            ScenarioDef scenario = selected.Get<ScenarioDef>(request.Scenario);
            var seats = new List<Controller>(request.Seats);
            if (request.Spectate)
            {
                seats.Clear();
                for (int i = 0; i < scenario.Seats; i++) seats.Add(Controller.Autopilot);
            }
            else if (seats.Count == 0)
                for (int i = 0; i < scenario.DefaultSeats; i++) seats.Add(i == 0 ? Controller.Human : Controller.Autopilot);
            if (seats.Count > scenario.Seats)
                throw new ArgumentException("scenario '" + scenario.Id + "' has room for " + scenario.Seats + " seats", nameof(request));
            var settings = new WorldSettings
            {
                Scenario = scenario.Id, Seed = request.Seed, Modules = new List<string>(selected.Modules).ToArray(), Seats = seats.ToArray(),
                Space = scenario.Space?.Id, Time = scenario.Time.Id, GameMode = scenario.DeclaredGameMode,
                HorizonTicks = new GameTime(scenario.Time).Ticks(request.Horizon ?? scenario.Time.Horizon ?? GameDuration.Zero)
            };
            settings.CaptureModeOverrides(overrides);
            World world = _definition.Build(_content, settings, true, out var features);
            var game = new SimGame(world, _definition.Name, features); game.Initialize(); return game;
        }

        /// <summary>Resumes a save from its bytes alone: the header rebuilds the world, the body fills it.</summary>
        public SimGame Load(byte[] save)
        {
            SaveHeader header = SaveGame.ReadHeader(save, out StateReader body);
            if (header.Game != _definition.Name) throw new CorruptStateException("save is from game '" + header.Game + "'");
            World world; IReadOnlyDictionary<Type, ViewFeatureRegistration> features;
            try { world = _definition.Build(_content, header.Settings, false, out features); }
            catch (ContentException error) { throw new CorruptStateException("invalid saved session content/settings: " + error.Message); }
            SaveGame.ReadBody(world, header, body);
            var game = new SimGame(world, _definition.Name, features); game.Initialize(); return game;
        }

        /// <summary>Reads a save's header for a load menu. Throws on a damaged file.</summary>
        public SaveInfo Describe(byte[] save)
        {
            SaveHeader h = SaveGame.Describe(save);
            bool compatible = HeaderMatches(h);
            return new SaveInfo
            {
                Game = h.Game, Build = h.Build, Scenario = h.Settings.Scenario, Seed = h.Settings.Seed, Tick = h.Tick,
                Date = compatible ? new GameTime(_content.Resolve(h.Settings.Modules).Get<SimulationTimeDef>(h.Settings.Time)).Calendar(h.Tick) : null, Seats = h.Settings.Seats.Length,
                Compatible = compatible
            };
        }

        private bool HeaderMatches(SaveHeader header)
        {
            if (header.Build != EngineInfo.Build || header.Format != EngineInfo.SaveFormat || header.Game != _definition.Name) return false;
            try
            {
                IReadOnlyList<ModuleRegistration> selected = _definition.Select(header.Settings.Modules);
                if (selected.Count != header.Settings.Modules.Length || selected.Count != header.Settings.ModuleVersions.Length) return false;
                for (int i = 0; i < selected.Count; i++)
                    if (selected[i].Id != header.Settings.Modules[i] || selected[i].Version != header.Settings.ModuleVersions[i]) return false;
                ResolvedContent resolved = _content.Resolve(header.Settings.Modules);
                ScenarioDef scenario = resolved.Find<ScenarioDef>(header.Settings.Scenario);
                if (resolved.Fingerprint != header.ContentFingerprint || header.Settings.HorizonTicks < 0 || scenario == null
                    || scenario.Time.Id != header.Settings.Time || scenario.Space?.Id != header.Settings.Space
                    || scenario.DeclaredGameMode != header.Settings.GameMode) return false;
                IGameModeConfiguration configuration = scenario.ResolveGameMode(resolved, header.Settings.ModeOverrides);
                GameModeConfiguration.ValidateSelectedCapabilities(resolved, configuration);
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (ContentException) { return false; }
        }
    }
}
