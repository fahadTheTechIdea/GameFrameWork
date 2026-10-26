using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Mutable declaration/fault-injection instrumentation only, never session simulation state.</summary>
    public static class SchemaProbeDeclaration
    {
        public static int OperationVersion, SignalVersion, OperationReads, SignalReads, BindingReads;
        public static IReadOnlyList<AutomationMetricSchema> Metrics;
        public static Action<SchemaProbeDef> OnAuthoredRead;
        public static void Reset()
        {
            OperationVersion = SignalVersion = 1;
            OperationReads = SignalReads = BindingReads = 0;
            Metrics = new List<AutomationMetricSchema>
            {
                new AutomationMetricSchema(new Code("probe.value"), AutomationSignalKind.Number, new Code("unit.count")),
                new AutomationMetricSchema(new Code("probe.enabled"), AutomationSignalKind.Boolean, new Code("unit.boolean"))
            };
            OnAuthoredRead = null;
        }
    }

    public sealed class SchemaProbeDef : ComponentDef<SchemaProbe>, IOperationDefinition, IAutomationSignalDefinition
    {
        public int OperationSchemaVersion { get { SchemaProbeDeclaration.OperationReads++; return SchemaProbeDeclaration.OperationVersion; } }
        public int SignalSchemaVersion { get { SchemaProbeDeclaration.SignalReads++; return SchemaProbeDeclaration.SignalVersion; } }
        public IReadOnlyList<AutomationMetricSchema> SignalBindings { get { SchemaProbeDeclaration.BindingReads++; return SchemaProbeDeclaration.Metrics; } }
        public OperationSettings Settings { get; private set; }
        public OperationSettings ControlOverride;
        public Code CapabilityId => Settings.Id;
        public OperationSettings ControlSettings => ControlOverride ?? Settings;
        protected override void ReadCore(JsonReader r)
        {
            Settings = OperationSettings.Read(r.Child("operation"));
            SchemaProbeDeclaration.OnAuthoredRead?.Invoke(this);
        }
    }
    public sealed class SchemaProbe : Component<SchemaProbeDef>, IControllableOperation, IAutomationSignalSource
    {
        private bool _enabled;
        private Fix _target, _value;
        // Instrumentation deliberately excluded from the provider's sole authoritative Sync.
        public int SignalReads, ReadCalls;
        public bool BadControl;
        public Code CapabilityId => Def.Settings.Id;
        public override void OnCreate() { _enabled = Def.Settings.Initial.Enabled; _target = Def.Settings.Initial.Target; }
        public OperationSnapshot ReadOperation(World world) => new OperationSnapshot(_enabled ? OperationStatus.Running : OperationStatus.Disabled,
            _enabled ? Code.None : OperationCodes.Disabled);
        public OperationControlSnapshot ReadControl() => BadControl
            ? new OperationControlSnapshot(_enabled, _target, Fix.Zero, Fix.FromInt(10), new Code("unit.wrong"))
            : Def.Settings.Snapshot(_enabled, _target);
        public Code CheckControl(bool enabled, Fix target) => Def.Settings.Check(target);
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new ArgumentOutOfRangeException(nameof(target));
            _enabled = enabled; _target = target; Entity.Touch();
        }
        public void Advance() { if (_enabled) _value += _target; }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            SignalReads++;
            if (signal.Metric == new Code("probe.value")) return signal.Number(_value, new Code("unit.count"));
            if (signal.Metric == new Code("probe.enabled")) return signal.Boolean(_enabled);
            return signal.Unavailable(OperationCodes.SignalUnavailable);
        }
        public override void Sync(IStateSync s)
        {
            if (s.Reading) ReadCalls++;
            s.Bool(ref _enabled); s.Fix(ref _target); s.Fix(ref _value);
            if (s.Reading && !CheckControl(_enabled, _target).IsNone) throw new CorruptStateException("schema probe target bounds");
        }
    }
    public sealed class UndeclaredSchemaDef : ComponentDef<SchemaProbe>, INamedCapabilityDefinition
    {
        public Code CapabilityId => new Code("probe");
        protected override void ReadCore(JsonReader r) { }
    }
    public sealed class WrongRuntimeSchemaDef : ComponentDef<CodecPart>, IOperationDefinition
    {
        public Code CapabilityId => new Code("probe");
        public int OperationSchemaVersion => 1;
        public OperationSettings ControlSettings => null;
        protected override void ReadCore(JsonReader r) { }
    }
    public sealed class SchemaProbeSystem : GameSystem
    {
        public const string Controls = "{ \"id\": \"probe\", \"enabled\": true, \"target\": 2, \"minimum\": 0, \"maximum\": 5, \"unit\": \"unit.count\" }";
        public override string Id => "probe.schema-system";
        public override int StateSchemaVersion => 1;
        public int Initializations, Reads, Loaded;
        public override void Initialize(World world) => Initializations++;
        public override void Sync(IStateSync s) { if (s.Reading) Reads++; }
        public override void OnLoaded(World world) => Loaded++;
        public override void OnNewGame(World world)
        {
            for (int p = 0; p < world.Players.Count; p++)
                foreach (EntityDef def in world.Content.All<EntityDef>())
                    if (def.Component<SchemaProbeDef>() != null)
                        ((TileMap)world.Space).SpawnAt(world, def, new PlayerId(p), new TileCoord(p * 3 + 1, 1));
        }
        public override void Update(World world, TickInfo tick)
        {
            ComponentList<SchemaProbe> parts = world.Entities.Components<SchemaProbe>();
            for (int i = 0; i < parts.Count; i++) parts[i].Advance();
        }
    }
}
