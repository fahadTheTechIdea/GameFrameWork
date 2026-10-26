using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Content
{
    /// <summary>
    /// What content a game understands: the def sections its documents may hold ("resources", "entities",
    /// "scenarios", ...) and the component keys an entity may be composed of. Modules declare these once;
    /// a document section or component key nobody declared fails the load.
    /// </summary>
    public sealed class ContentTypes
    {
        internal sealed class SectionType
        {
            public string Name;
            public Type DefType;
            public Func<Def> Create;
            public int Order;
            public string Owner;
        }

        internal sealed class ComponentType
        {
            public StateCodecRegistration Metadata;
            public string Owner => Metadata.Owner;
            public Func<ComponentDef> Create;
            public Type DefType;
            public OperationProviderSchema OperationSchema;
            public AutomationSignalProviderSchema SignalSchema;
        }

        private readonly List<SectionType> _sections = new List<SectionType>();
        private readonly Dictionary<string, SectionType> _sectionsByName = new Dictionary<string, SectionType>(StringComparer.Ordinal);
        private readonly Dictionary<string, ComponentType> _components = new Dictionary<string, ComponentType>(StringComparer.Ordinal);
        private readonly List<string> _componentKeys = new List<string>();
        private string _owner = "engine";
        private bool _frozen;
        internal GameDefinition Definition { get; }

        internal ContentTypes(GameDefinition definition)
        {
            Definition = definition;
            Section<EntityStateDef>("entity_states");
            Section<UnitDef>("units");
            Section<BasicResourceDef>("resources");
            Section<BasicCapacityPolicyDef>("capacity_policies");
            Section<EntityDef>("entities");
            Section<SimulationTimeDef>("time_definitions");
            Section<GameModePropertyDef>("game_mode_properties");
            Section<GameModeDef>("game_modes");
            Section<CycleCalendarDef>("calendars");
            Section<TileSpaceDef>("tile_spaces");
            Section<SpatialLayerDef>("spatial_layers");
            Section<Maps.RectangularTileRegionDef>("rectangular_tile_regions");
            Section<Maps.PaintedTileRegionDef>("painted_tile_regions");
            Component<Maps.TilePresenceDef>("tile_presence", 1);
        }

        /// <summary>Declares a document section whose array entries become <typeparamref name="TDef"/>s.</summary>
        public void Section<TDef>(string name) where TDef : Def, new()
        {
            EnsureDeclaration();
            if (_sectionsByName.ContainsKey(name)) throw new InvalidOperationException("section '" + name + "' declared twice");
            for (int i = 0; i < _sections.Count; i++)
                if (_sections[i].DefType == typeof(TDef)) throw new InvalidOperationException("definition type '" + typeof(TDef).Name + "' declared twice");
            var s = new SectionType { Name = name, DefType = typeof(TDef), Create = () => new TDef(), Order = _sections.Count, Owner = _owner };
            _sections.Add(s);
            _sectionsByName.Add(name, s);
        }

        /// <summary>Declares a component key: an entity's "components": { "<key>": {...} } creates a <typeparamref name="TDef"/>.</summary>
        public void Component<TDef>(string key, int stateSchemaVersion) where TDef : ComponentDef, new()
        {
            EnsureDeclaration();
            if (_components.ContainsKey(key)) throw new InvalidOperationException("component key '" + key + "' declared twice");
            foreach (ComponentType declared in _components.Values)
                if (declared.DefType == typeof(TDef)) throw new InvalidOperationException("component definition type '" + typeof(TDef).Name + "' declared twice");
            var metadata = new StateCodecRegistration(StateCodecKind.Component, key, stateSchemaVersion, _owner, typeof(TDef));
            var prototype = new TDef();
            bool operation = typeof(IOperation).IsAssignableFrom(prototype.ComponentType);
            bool signal = typeof(IAutomationSignalSource).IsAssignableFrom(prototype.ComponentType);
            if (operation != (prototype is IOperationDefinition) || signal != (prototype is IAutomationSignalDefinition))
                throw new ArgumentException("component '" + key + "' must declare matching operation/signal definition contracts");
            var entry = new ComponentType { Metadata = metadata, Create = () => new TDef(), DefType = typeof(TDef) };
            if (prototype is IOperationDefinition operationDefinition)
                entry.OperationSchema = new OperationProviderSchema(metadata, operationDefinition.OperationSchemaVersion,
                    typeof(IControllableOperation).IsAssignableFrom(prototype.ComponentType));
            if (prototype is IAutomationSignalDefinition signalDefinition)
                entry.SignalSchema = new AutomationSignalProviderSchema(metadata, signalDefinition.SignalSchemaVersion, signalDefinition.SignalBindings);
            _components.Add(key, entry);
            _componentKeys.Add(key);
        }

        private void EnsureDeclaration()
        {
            if (_frozen || _owner == null) throw new InvalidOperationException("content types are declared while building the game definition");
        }
        internal void BeginModule(string module) => _owner = module;
        internal void EndModule() => _owner = null;
        internal void Freeze() { _owner = null; _frozen = true; }

        internal IReadOnlyList<SectionType> Sections => _sections;
        internal bool TryGetSection(string name, out SectionType s) => _sectionsByName.TryGetValue(name, out s);
        internal bool TryGetComponent(string key, out ComponentType c) => _components.TryGetValue(key, out c);
        internal IEnumerable<ComponentType> Components => _components.Values;
        public IReadOnlyList<string> ComponentKeys => _componentKeys.AsReadOnly();
        internal bool IsSection(string name) => _sectionsByName.ContainsKey(name);
    }
}
