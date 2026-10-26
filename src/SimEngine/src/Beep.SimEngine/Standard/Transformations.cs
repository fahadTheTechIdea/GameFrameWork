using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>
    /// A transformation (section "transformations"): inputs, time, and outputs — resources into the transformer's store, or a
    /// entity released through its spatial provider. { "id": "build_truck", "mode":"batch", "cancel_policy": "return_inputs", "tools": {}, "inputs": { "steel": 10 },
    /// "duration": { "numerator": 24, "denominator": 1, "unit": "hour" }, "entity_output": "haul_truck", "output_home": "producer" }. Optional money is authored by Tycoon bindings.
    /// </summary>
    public sealed class TransformationDeclaration
    {
        private bool _resolutionAttempted;
        private TransformationDeclaration() { }
        /// <summary>Validate the actual authored recipe alias, including unused definitions, before consumers.</summary>
        public static void Validate(ITransformationDefinition definition, ContentResolver content)
        {
            if (definition == null || content == null) throw new ArgumentNullException();
            try
            {
                if (!content.Content.Owns(definition) || !ReferenceEquals(content.Content.Find<ITransformationDefinition>(definition.Id), definition)
                    || string.IsNullOrWhiteSpace(definition.Category)
                    || definition.Mode != TransformationMode.Batch && definition.Mode != TransformationMode.Continuous
                    || definition.Cash < Fix.Zero || definition.CancelPolicy != new Code(definition.Mode == TransformationMode.Batch ? "return_inputs" : "stop"))
                    throw content.Fail("transformation requires its actual owned identity and coherent mode/category/cost/cancellation metadata");
                if (definition.Inputs == null || definition.Tools == null || definition.ToolConsumption == null || definition.StartResources == null
                    || definition.Outputs == null || definition.InputRates == null || definition.OutputRates == null
                    || definition.ToolConsumptionRates == null || definition.OperatingInputs == null
                    || definition.InputPorts == null || definition.OutputPorts == null || definition.ToolPorts == null || definition.OperatingPorts == null)
                    throw content.Fail("transformation requires explicit resolved bills, rates and port routes");
                if (definition.Mode == TransformationMode.Batch && (definition.Duration.IsZero || definition.EntityOutput == null && definition.Outputs.IsEmpty
                        || definition.InputRates.Count != 0 || definition.OutputRates.Count != 0 || definition.ToolConsumptionRates.Count != 0)
                    || definition.Mode == TransformationMode.Continuous && (!definition.Duration.IsZero || definition.EntityOutput != null
                        || definition.OutputRates.Count == 0 && definition.InputRates.Count == 0
                        || !definition.Inputs.IsEmpty || !definition.Outputs.IsEmpty || !definition.ToolConsumption.IsEmpty))
                    throw content.Fail("transformation mode requires its explicit duration and result metadata");
                if (definition.EntityOutput != null && !ReferenceEquals(content.Content.Find<EntityDef>(definition.EntityOutput.Id), definition.EntityOutput))
                    throw content.Fail("transformation entity output must be its actual owned prototype");
                if ((definition.EntityOutput == null) != !definition.OutputHome.HasValue ||
                    definition.OutputHome.HasValue && (definition.OutputHome < TransformationOutputHome.Unassigned || definition.OutputHome > TransformationOutputHome.Required) ||
                    (definition.OutputHome == TransformationOutputHome.Producer || definition.OutputHome == TransformationOutputHome.Required) &&
                    (!definition.EntityOutput.HasCapability<IWorker>() || !definition.EntityOutput.HasCapability<IMotion>() || definition.EntityOutput.Component<ISpatialPresenceDefinition>() == null))
                    throw content.Fail("entity output requires explicit applicable output_home metadata; bound homes require a spatial mobile worker");
                foreach (var bill in new[] { definition.Inputs, definition.Tools, definition.ToolConsumption, definition.StartResources, definition.Outputs })
                    foreach (var line in bill.Lines)
                        if (UnitConversion.ValidateAmount(content.Content.Resource(line.Resource)?.QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                            throw content.Fail("transformation bill requires actual representable native resources");
                var startup = new List<ResourceAmount>(definition.Inputs.Lines); startup.AddRange(definition.Tools.Lines);
                Bill expectedStartup = new Bill(startup);
                if (expectedStartup.Count != definition.StartResources.Count)
                    throw content.Fail("startup resources must equal the canonical input and tool bill");
                foreach (var line in expectedStartup.Lines)
                    if (definition.StartResources.AmountOf(line.Resource) != line.Amount)
                        throw content.Fail("startup resources must equal the canonical input and tool bill");
                foreach (var line in definition.ToolConsumption.Lines)
                    if (line.Amount > definition.Tools.AmountOf(line.Resource))
                        throw content.Fail("completion tool consumption exceeds its actual required hold");
                ValidateRates(content, definition.InputRates); ValidateRates(content, definition.OutputRates);
                ValidateRates(content, definition.ToolConsumptionRates); ValidateRates(content, definition.OperatingInputs);
                foreach (var rate in definition.ToolConsumptionRates)
                    if (!definition.Tools.AmountOf(rate.Resource.Resource).IsPositive)
                        throw content.Fail("continuous tool consumption must reference an authored held tool");
                ResourcePortContracts.ValidateRoutes(definition.InputPorts, definition.Inputs, definition.InputRates, "input");
                ResourcePortContracts.ValidateRoutes(definition.OutputPorts, definition.Outputs, definition.OutputRates, "output");
                ResourcePortContracts.ValidateRoutes(definition.ToolPorts, definition.Tools, Array.Empty<IResourceRate>(), "tool");
                ResourcePortContracts.ValidateRoutes(definition.OperatingPorts, Bill.Empty, definition.OperatingInputs, "operating");
                if (definition.OutputQualities == null) throw content.Fail("output quality rules must be explicit resolved metadata");
                var axes = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rule in definition.OutputQualities)
                {
                    if (!ResourceOutputQualityDeclaration.Valid(content.Content, rule) || !axes.Add(rule.Quality.Id)) throw content.Fail("invalid or duplicate output quality rule");
                    bool output = definition.Outputs.AmountOf(rule.Quality.Resource).IsPositive;
                    foreach (var rate in definition.OutputRates) if (rate.Resource.Resource == rule.Quality.Resource) output = true;
                    bool input = rule.Source == null || definition.Inputs.AmountOf(rule.Source.Resource).IsPositive;
                    if (rule.Source != null) foreach (var rate in definition.InputRates) if (rate.Resource.Resource == rule.Source.Resource) input = true;
                    if (!output || !input) throw content.Fail("output quality must reference actual recipe output and consumed feed");
                }
            }
            catch (Exception error) when (!(error is ContentException))
            { throw content.Fail("invalid transformation metadata: " + error.Message); }
        }
        private static void ValidateRates(ContentResolver content, IReadOnlyList<IResourceRate> rates)
        {
            var resources = new HashSet<ResourceId>();
            foreach (var rate in rates)
            {
                if (rate?.Resource == null || !ReferenceEquals(content.Content.Resource(rate.Resource.Resource), rate.Resource)
                    || !resources.Add(rate.Resource.Resource))
                    throw content.Fail("transformation rates require unique actual owned resources");
                // Reuse the original native rate invariant; do not copy quantity or duration math.
                _ = new ResourceRate(rate.Resource, rate.Quantity, rate.PeriodNumerator, rate.PeriodDenominator, rate.PeriodUnit);
            }
        }
        public static TransformationDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new TransformationDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private BillSpec _inputs, _outputs, _tools, _toolConsumption;
        private ResourceRateSpec _operating, _inputRates, _outputRates, _toolConsumptionRates;
        private string _entityOutput;
        private IReadOnlyList<ResourceOutputQualityDeclaration> _outputQualities;
        public IReadOnlyList<IResourceOutputQualityRule> OutputQualities { get; private set; }
        private readonly List<KeyValuePair<string, string>> _outputPorts = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> _inputPorts = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> _toolPorts = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> _operatingPorts = new List<KeyValuePair<string, string>>();
        public IReadOnlyList<ResourcePortRoute> OutputPorts { get; private set; }
        public IReadOnlyList<ResourcePortRoute> InputPorts { get; private set; }
        public IReadOnlyList<ResourcePortRoute> ToolPorts { get; private set; }
        public IReadOnlyList<ResourcePortRoute> OperatingPorts { get; private set; }

        public TransformationMode Mode { get; private set; }
        public IReadOnlyList<IResourceRate> InputRates { get; private set; } = Array.Empty<IResourceRate>();
        public IReadOnlyList<IResourceRate> OutputRates { get; private set; } = Array.Empty<IResourceRate>();

        public Bill Inputs { get; private set; } = Bill.Empty;
        public Bill Tools { get; private set; } = Bill.Empty;
        public Bill ToolConsumption { get; private set; } = Bill.Empty;
        public IReadOnlyList<IResourceRate> ToolConsumptionRates { get; private set; } = Array.Empty<IResourceRate>();
        public Bill StartResources { get; private set; } = Bill.Empty;
        public Bill Outputs { get; private set; } = Bill.Empty;
        public IReadOnlyList<IResourceRate> OperatingInputs { get; private set; }
        public EntityDef EntityOutput { get; private set; }
        public TransformationOutputHome? OutputHome { get; private set; }
        public GameDuration Duration { get; private set; }
        public Fix Cash { get; private set; }
        public string Category { get; private set; }
        public Code CancelPolicy { get; private set; }

        private void ReadCore(JsonReader r)
        {
            _outputQualities = ResourceOutputQualityDeclaration.Read(r);
            string mode = r.String("mode");
            Mode = mode == "batch" ? TransformationMode.Batch : mode == "continuous" ? TransformationMode.Continuous
                : throw r.Error("explicit mode must be batch or continuous");
            if (Mode == TransformationMode.Batch)
            {
                _inputs = BillSpec.Read(r, "inputs"); _outputs = BillSpec.Read(r, "outputs");
            }
            else
            {
                _inputRates = ResourceRateSpec.Read(r, "inputs"); _outputRates = ResourceRateSpec.Read(r, "outputs");
            }
            if (!r.Has("tools")) throw r.Error("explicit tools bill required; author {} for material-free tools");
            ResourcePortContracts.ReadRoutes(r, "output_ports", _outputPorts);
            ResourcePortContracts.ReadRoutes(r, "input_ports", _inputPorts);
            ResourcePortContracts.ReadRoutes(r, "tool_ports", _toolPorts);
            ResourcePortContracts.ReadRoutes(r, "operating_ports", _operatingPorts);
            _tools = BillSpec.Read(r, "tools");
            if (Mode == TransformationMode.Batch)
            {
                if (!r.Has("tool_consumption")) throw r.Error("explicit tool_consumption bill required; author {} for fully reusable tools");
                _toolConsumption = BillSpec.Read(r, "tool_consumption");
            }
            else _toolConsumptionRates = ResourceRateSpec.Read(r, "tool_consumption");
            _operating = ResourceRateSpec.Read(r, "operating_inputs");
            _entityOutput = r.NullableString("entity_output");
            string home = r.NullableString("output_home");
            OutputHome = home == "unassigned" ? TransformationOutputHome.Unassigned : home == "producer" ? TransformationOutputHome.Producer :
                home == "required" ? TransformationOutputHome.Required : home == null ? (TransformationOutputHome?)null : throw r.Error("output_home must be unassigned, producer or required");
            if ((_entityOutput == null) != !OutputHome.HasValue) throw r.Error("entity output requires explicit output_home; resource-only recipes cannot declare it");
            if (Mode == TransformationMode.Batch)
            {
                Duration = (r.Child("duration") ?? throw r.Error("explicit transformation duration required")).RationalDuration();
                if (Duration.IsZero) throw r.Error("batch duration must be positive");
            }
            else
            {
                if (r.Value("duration")?.Kind != JsonKind.Null || _entityOutput != null)
                    throw r.Error("continuous duration must be explicit null and cannot create entities");
                Duration = GameDuration.Zero;
            }
            Category = r.String("category", "general");
            CancelPolicy = new Code(r.String("cancel_policy"));
            if (CancelPolicy != new Code(Mode == TransformationMode.Batch ? "return_inputs" : "stop"))
                throw r.Error("cancel_policy must match the explicit transformation mode");
        }

        public void Resolve(ContentResolver c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (_resolutionAttempted) throw new InvalidOperationException("transformation declaration resolves only once");
            _resolutionAttempted = true;
            var qualityRules = new List<IResourceOutputQualityRule>();
            foreach (var rule in _outputQualities) { rule.Resolve(c); qualityRules.Add(rule); }
            OutputQualities = qualityRules.AsReadOnly();
            Inputs = Mode == TransformationMode.Batch ? _inputs.Resolve(c) : Bill.Empty;
            Tools = _tools.Resolve(c);
            ToolConsumption = Mode == TransformationMode.Batch ? _toolConsumption.Resolve(c) : Bill.Empty;

            if (Mode == TransformationMode.Continuous)
            {
                ToolConsumptionRates = _toolConsumptionRates.Resolve(c);

            }
            OperatingInputs = _operating.Resolve(c);
            if (Mode == TransformationMode.Continuous)
            {
                InputRates = _inputRates.Resolve(c); OutputRates = _outputRates.Resolve(c);

            }
            try
            {
                var startup = new List<ResourceAmount>(Inputs.Lines); startup.AddRange(Tools.Lines);
                StartResources = new Bill(startup);
            }
            catch (OverflowException) { throw c.Fail("combined input/tool native requirement exceeds its representation"); }
            Outputs = Mode == TransformationMode.Batch ? _outputs.Resolve(c) : Bill.Empty;
            OutputPorts = ResourcePortContracts.ResolveRoutes(c, _outputPorts);
            InputPorts = ResourcePortContracts.ResolveRoutes(c, _inputPorts);
            ToolPorts = ResourcePortContracts.ResolveRoutes(c, _toolPorts);
            OperatingPorts = ResourcePortContracts.ResolveRoutes(c, _operatingPorts);
            EntityOutput = c.OptionalDef<EntityDef>(_entityOutput);
            ITransformationCostBinding selectedCost = null;
            foreach (var binding in c.Content.All<ITransformationCostBinding>())
                if (c.Definition is ITransformationDefinition owner && binding.TransformationId == owner.Id)
                {
                    if (selectedCost != null) throw c.Fail("duplicate transformation cost binding");
                    selectedCost = binding;
                }
            Cash = selectedCost?.Amount ?? Fix.Zero; // Zero means no authored monetary intent, not an account/default price.

        }
    }
    /// <summary>Native authored kind. Independent recipe content shares the same declaration.</summary>
    public sealed class TransformationDef : Def, ITransformationDefinition
    {
        private TransformationDeclaration _declaration;
        public IReadOnlyList<IResourceOutputQualityRule> OutputQualities => _declaration.OutputQualities;
        public IReadOnlyList<ResourcePortRoute> OutputPorts => _declaration.OutputPorts;
        public IReadOnlyList<ResourcePortRoute> InputPorts => _declaration.InputPorts;
        public IReadOnlyList<ResourcePortRoute> ToolPorts => _declaration.ToolPorts;
        public IReadOnlyList<ResourcePortRoute> OperatingPorts => _declaration.OperatingPorts;
        public TransformationMode Mode => _declaration.Mode;
        public IReadOnlyList<IResourceRate> InputRates => _declaration.InputRates;
        public IReadOnlyList<IResourceRate> OutputRates => _declaration.OutputRates;
        public Bill Inputs => _declaration.Inputs;
        public Bill Tools => _declaration.Tools;
        public Bill ToolConsumption => _declaration.ToolConsumption;
        public IReadOnlyList<IResourceRate> ToolConsumptionRates => _declaration.ToolConsumptionRates;
        public Bill StartResources => _declaration.StartResources;
        public Bill Outputs => _declaration.Outputs;
        public IReadOnlyList<IResourceRate> OperatingInputs => _declaration.OperatingInputs;
        public EntityDef EntityOutput => _declaration.EntityOutput;
        public TransformationOutputHome? OutputHome => _declaration.OutputHome;
        public GameDuration Duration => _declaration.Duration;
        public Fix Cash => _declaration.Cash;
        public string Category => _declaration.Category;
        public Code CancelPolicy => _declaration.CancelPolicy;
        protected override void ReadCore(JsonReader reader) => _declaration = TransformationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    /// <summary>Sole strict JSON declaration for the original physical processor; no execution state.</summary>
    public sealed class TransformerDeclaration
    {
        private bool _resolved;
        private TransformerDeclaration() { }
        public static TransformerDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new TransformerDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        public static readonly IReadOnlyList<AutomationMetricSchema> Signals = Array.AsReadOnly(new[]
        {
            new AutomationMetricSchema(new Code("operation.enabled"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            new AutomationMetricSchema(new Code("operation.target"), AutomationSignalKind.Number, new Code("unit.work-rate")),
            new AutomationMetricSchema(new Code("operation.running"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            new AutomationMetricSchema(new Code("transformation.progress"), AutomationSignalKind.Number, new Code("unit.work-ticks")),
            new AutomationMetricSchema(new Code("transformation.backlog"), AutomationSignalKind.Number, new Code("unit.count"))
        });
        private string[] _transformationIds;
        private string _idleTransformationId;
        private string _wipCapacity;
        public ResolvedContent Content { get; private set; }
        public ICapacityPolicy WipCapacity { get; private set; }
        public IReadOnlyList<ITransformationDefinition> Transformations { get; private set; }
        public OperationSettings ControlSettings { get; private set; }
        public Code CapabilityId => ControlSettings.Id;
        public EntityStateBindings States { get; private set; }
        public bool AutomaticSupply { get; private set; }
        /// <summary>Explicit automatic finite batch repetition. Continuous streams are enqueued normally.</summary>
        public ITransformationDefinition IdleTransformation { get; private set; }

        private void ReadCore(JsonReader r)
        {
            JsonValue supply = r.Value("automatic_supply");
            if (supply == null || supply.Kind != JsonKind.Bool) throw r.Error("automatic_supply must be explicit");
            AutomaticSupply = supply.Bool;
            _transformationIds = r.Strings("transformations");
            if (_transformationIds.Length == 0) throw r.Error("a transformer lists its transformations");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in _transformationIds) if (!ids.Add(id)) throw r.Error("duplicate supported transformation");
            ControlSettings = OperationSettings.Read(r.Child("operation"));
            if (!ControlSettings.Initial.Minimum.IsPositive) throw r.Error("transformer work-rate bounds must be positive");
            if (ControlSettings.Initial.Unit != new Code("unit.work-rate")) throw r.Error("transformer targets must use unit.work-rate");
            _idleTransformationId = r.NullableString("idle_transformation");
            _wipCapacity = r.String("wip_capacity");
            States = EntityStateBindings.Read(r, "working", "stopped");
        }

        public void Resolve(ContentResolver c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (_resolved) throw new InvalidOperationException("processor declaration resolves only once");
            _resolved = true;
            States = States.Resolve(c);
            Content = c.Content;
            WipCapacity = c.Def<ICapacityPolicyDefinition>(_wipCapacity);
            var transformations = new ITransformationDefinition[_transformationIds.Length];
            for (int i = 0; i < transformations.Length; i++) transformations[i] = c.Def<ITransformationDefinition>(_transformationIds[i]);
            Transformations = Array.AsReadOnly(transformations);
            if (_idleTransformationId != null)
            {
                IdleTransformation = c.Def<ITransformationDefinition>(_idleTransformationId);
                if (Array.IndexOf(_transformationIds, _idleTransformationId) < 0) throw c.Fail("idle_transformation transformation '" + _idleTransformationId + "' is not in the transformer's list");
            }
        }

    }
    /// <summary>Built-in authored processor kind; independent definitions share the same declaration and runtime.</summary>
    public sealed class TransformerDef : ComponentDef<Transformer>, ITransformerWipDefinition, IAutomationSignalDefinition
    {
        private TransformerDeclaration _declaration;
        public int OperationSchemaVersion => 1;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => TransformerDeclaration.Signals;
        public ResolvedContent Content => _declaration.Content;
        public ICapacityPolicy WipCapacity => _declaration.WipCapacity;
        public IReadOnlyList<ITransformationDefinition> Transformations => _declaration.Transformations;
        public OperationSettings ControlSettings => _declaration.ControlSettings;
        public Code CapabilityId => ControlSettings.Id;
        public EntityStateBindings States => _declaration.States;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public ITransformationDefinition IdleTransformation => _declaration.IdleTransformation;
        protected override void ReadCore(JsonReader reader) => _declaration = TransformerDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    public enum TransformationPhase : byte { Idle = 0, Supplying = 1, Working = 2, Blocked = 3, Returning = 4 }

    /// <summary>A transformer's queue and the progress of its head entry.</summary>
    public sealed partial class Transformer : Component, IInspectable, IEntityStateSource, ITransformerTools, IAutomationSignalSource
    {
        public new ITransformerWipDefinition Def => (ITransformerWipDefinition)base.Def;
        public readonly List<TransformationQueueEntry> Queue = new List<TransformationQueueEntry>();
        public TransformationPhase Phase;
        public WorkProgress Progress;
        public Code Blocker;
        private long _runningTicks;
        public Code CapabilityId => Def.ControlSettings.Id;
        private bool _enabled;
        private Fix _target;
        public bool Enabled => _enabled;
        public Fix TargetRate => _target;
        public override void OnCreate() { _enabled = Def.ControlSettings.Initial.Enabled; _target = Def.ControlSettings.Initial.Target; }
        public OperationControlSnapshot ReadControl() => Def.ControlSettings.Snapshot(_enabled, _target);
        public Code CheckControl(bool enabled, Fix target) => Def.ControlSettings.Check(target);
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new ArgumentOutOfRangeException(nameof(target));
            _enabled = enabled; _target = target; Entity.Touch();
        }
        public OperationSnapshot ReadOperation(World world)
            => !_enabled ? new OperationSnapshot(OperationStatus.Disabled, OperationCodes.Disabled)
                : !Blocker.IsNone ? new OperationSnapshot(OperationStatus.Blocked, Blocker)
                : new OperationSnapshot(Phase == TransformationPhase.Working ? OperationStatus.Running : OperationStatus.Idle, Code.None);
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            switch (signal.Metric.Id)
            {
                case "operation.enabled": return signal.Boolean(_enabled);
                case "operation.target": return signal.Number(_target, Def.ControlSettings.Initial.Unit);
                case "operation.running": return signal.Boolean(ReadOperation(world).Status == OperationStatus.Running);
                case "transformation.progress":
                    return Progress.TryAsFix(out Fix scalar)
                    ? signal.Number(scalar, new Code("unit.work-ticks"))
                    : signal.Unavailable(OperationCodes.SignalNumericRange);
                case "transformation.backlog": return signal.Number(Fix.FromInt(Queue.Count), new Code("unit.count"));
                default: return signal.Unavailable(OperationCodes.SignalUnavailable);
            }
        }

        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => !_enabled ? Def.States["stopped"] : Queue.Count == 0 ? null : !Blocker.IsNone ? Def.States["stopped"] : Phase == TransformationPhase.Working ? Def.States["working"] : null;

        public override void Sync(IStateSync s)
        {
            s.Bool(ref _enabled); s.Fix(ref _target);
            if (!CheckControl(_enabled, _target).IsNone) throw new CorruptStateException("transformer target violates authored bounds");
            int n = s.Count(Queue.Count);
            if (s.Reading) Queue.Clear();
            for (int i = 0; i < n; i++)
            {
                TransformationQueueEntry q = s.Reading ? default : Queue[i];
                string id = q.Transformation; bool repeat = q.Repeat; EntityId home = q.Home;
                long execution = q.Execution; s.Long(ref execution);
                s.String(ref id); s.Bool(ref repeat); s.Entity(ref home);
                if (s.Reading)
                {
                    try { Queue.Add(new TransformationQueueEntry(execution, id, repeat, home)); }
                    catch (ArgumentException ex) { throw new CorruptStateException(ex.Message); }
                }
            }
            s.Enum(ref Phase);
            s.Work(ref Progress);
            s.Long(ref _runningTicks);
            if (_runningTicks < 0) throw new CorruptStateException("negative running time");
            string b = Blocker.Id; s.String(ref b); Blocker = b == null ? Code.None : new Code(b);
            SyncWip(s);
        }


        public TransformationSnapshot ReadTransformation(World world)
            => new TransformationSnapshot(Phase, Progress, Blocker, Queue,
                Queue.Count == 0 ? null : world.Content.Get<ITransformationDefinition>(Queue[0].Transformation), world.Time,
                new TransformationWipSnapshot(WorkInProgress.Endpoint, _execution, _wipVersion, new Bill(Wip.Stock), _inputSources), ReadTools(world), ReadOperatingInputs(world), ReadFlow(world), _nextExecution);
        public Code CheckEnqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home)
            => transformation == null || !TransformationContracts.Supports(this, transformation) ? EnqueueTransformationHandler.NotMadeHere
                : count < 1 || !TransformationContracts.IsResolvedOutputHome(transformation, home) || _nextExecution > long.MaxValue - (repeat ? 1L : count) || transformation.Mode == TransformationMode.Continuous && repeat || (long)Queue.Count + (repeat ? 1 : count) > int.MaxValue ? Codes.InvalidArgument : Code.None;
        public void Enqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home)
        {
            if (!CheckEnqueue(transformation, count, repeat, home).IsNone) throw new InvalidOperationException("queue admission changed");
            for (int i = 0; i < (repeat ? 1 : count); i++) Queue.Add(new TransformationQueueEntry(_nextExecution++, transformation.Id, repeat, home));
            Entity.Touch();
        }
        public Code CheckCancel(int index) => index < 0 || index >= Queue.Count ? Codes.InvalidArgument
            : index == 0 && Phase == TransformationPhase.Returning ? TransformerManager.BlockerReturning : Code.None;
        public void Cancel(World world, int index)
        {
            if (!CheckCancel(index).IsNone) throw new InvalidOperationException("queue cancellation changed");
            if (index == 0)
            {
                if (Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(this);
                if (_execution > 0) { Phase = TransformationPhase.Returning; Blocker = Code.None; Entity.Touch(); return; }
                Phase = TransformationPhase.Idle; Progress = WorkProgress.Zero; _runningTicks = 0; Blocker = Code.None;
            }
            Queue.RemoveAt(index); Entity.Touch();
        }
        public void Advance(TransformationExecutionContext context)
        {
            TransformationPhase before = Phase; WorkProgress progress = Progress; Code blocker = Blocker; int count = Queue.Count; long running = _runningTicks;
            AdvanceTransformation(context);
            if (before != Phase || !progress.Equals(Progress) || blocker != Blocker || count != Queue.Count || running != _runningTicks) Entity.Touch();
        }
        private void AdvanceTransformation(TransformationExecutionContext context)
        {
            World world = context.World; Transformer p = this;
            if (!p.Entity.Alive) return;
            if (p.Phase == TransformationPhase.Returning) { ReturnWip(world); return; }
            if (!p.Enabled) { p.Blocker = OperationCodes.Disabled; return; }
            if (p.Queue.Count == 0 && p.Def.IdleTransformation != null)
            {
                if (_nextExecution == long.MaxValue) { p.Blocker = TransformerManager.BlockerIdentityRange; return; }
                Code homeRefusal = TransformationContracts.ResolveOutputHome(world, p, p.Def.IdleTransformation, EntityId.None, out EntityId idleHome);
                if (!homeRefusal.IsNone) { p.Blocker = homeRefusal; return; }
                p.Enqueue(p.Def.IdleTransformation, 1, false, idleHome);
            }
            if (p.Queue.Count == 0) { p.Phase = TransformationPhase.Idle; p.Blocker = Code.None; return; }
            ITransformationDefinition transformation = world.Content.Get<ITransformationDefinition>(p.Queue[0].Transformation);
            Code access = TransformerManager.CheckAccess(world, p.Entity.Owner, transformation, playerIntent: false);
            if (!access.IsNone) { p.Blocker = access; return; }
            Code gate = context.CheckGates(this, transformation);
            if (!gate.IsNone) { p.Blocker = gate; return; }
            p.Blocker = Code.None;
            if (p.Phase == TransformationPhase.Idle) p.Phase = TransformationPhase.Supplying;
            if (p.Phase == TransformationPhase.Supplying)
            {
                Code startup;
                try { startup = CheckStartupInputs(world, transformation); }
                catch (OverflowException) { p.Blocker = transformation.Mode == TransformationMode.Continuous ? TransformerManager.BlockerFlowRange : TransformerManager.BlockerOperatingRange; return; }
                if (!startup.IsNone)
                {
                    p.Blocker = startup;
                    return;
                }
                MonetaryCostDecision cost = BusinessModeRules.EvaluateCost(world, MonetaryCostCategory.Manufacturing, transformation.Cash);
                if (cost.Applicability == MonetaryCostApplicability.UnavailableCapability) { p.Blocker = BusinessModeRules.UnavailableCostCapability; return; }
                bool pay = cost.Applicability == MonetaryCostApplicability.Enabled;
                if (pay && !BusinessModeRules.CanPay(world.Services.Get<IEconomy>(), p.Entity.Owner, cost.AuthoredAmount)) { p.Blocker = WorkSystem.BlockerCash; return; }
                if (transformation.Mode == TransformationMode.Continuous)
                {
                    Code flow = CheckFlow(world, transformation, out _, out _, out _, out _);
                    if (!flow.IsNone) { p.Blocker = flow; return; }
                }
                Code start = StartWip(world, transformation);
                if (!start.IsNone) { p.Blocker = start; return; }
                if (p.Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(p);
                if (pay && !world.Services.Get<IEconomy>().TryCharge(p.Entity.Owner, cost.AuthoredAmount, TransformerManager.LedgerTransformation))
                    throw new InvalidOperationException("economy violated transformation payment preflight");
                p.Phase = TransformationPhase.Working;
                p.Progress = WorkProgress.Zero;
                p._runningTicks = 0;
            }
            if (p.Phase == TransformationPhase.Working)
            {
                if (transformation.Mode == TransformationMode.Continuous) { p.Blocker = AdvanceFlow(world, transformation); return; }
                Code tools=ResourcePortContracts.Blocker(TransformationContracts.ToolBindings(world,p.Entity,transformation,_toolSources));
                if(!tools.IsNone){p.Blocker=tools;return;}
                Code running = ConsumeOperatingInputs(world, transformation);
                if (!running.IsNone) { p.Blocker = running; return; }
                long required = world.Time.Ticks(transformation.Duration);
                p.Progress = p.Progress.Advance(p.TargetRate, required);
                p._runningTicks = checked(p._runningTicks + 1);
                if (p.Progress.Complete(required)) p.Phase = TransformationPhase.Blocked;
                else return;
            }
            if (p.Phase == TransformationPhase.Blocked)
            {
                if (p.Queue[0].Repeat && _nextExecution == long.MaxValue) { p.Blocker = TransformerManager.BlockerIdentityRange; return; }
                Code delivery = Deliver(world, p, transformation);
                if (!delivery.IsNone) { p.Blocker = delivery; return; }
                TransformationQueueEntry done = p.Queue[0];
                p.Queue.RemoveAt(0);
                if (done.Repeat) p.Enqueue(transformation, 1, true, done.Home);
                p.Phase = p.Queue.Count > 0 ? TransformationPhase.Supplying : TransformationPhase.Idle;
                p.Progress = WorkProgress.Zero;
                p._runningTicks = 0;
                _execution = 0;
                _inputSources.Clear();
                _toolSources.Clear();
            }

        }
        /// <summary>Whole resource/result admission and one completion protocol; no eager result creation.</summary>
        private static Code Deliver(World world, Transformer p, ITransformationDefinition transformation)
        {
            Code homeRefusal = TransformationContracts.CheckOutputHome(world, p, transformation, p.Queue[0].Home);
            if (!homeRefusal.IsNone) return homeRefusal;
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            var inputs = new List<ResourceConversionInput>();
            foreach (var receipt in p.WorkInProgress.Reservations)
                inputs.Add(new ResourceConversionInput(ResourceFlowKind.Consumed, receipt, receipt.Amount, false));
            IReadOnlyList<ResourceReservation> tools = p.ToolClaims(world);
            foreach (var receipt in tools)
                inputs.Add(new ResourceConversionInput(ResourceFlowKind.Consumed, receipt, transformation.ToolConsumption.AmountOf(receipt.Resource), true));
            var ports = TransformationContracts.OutputBindings(world, p.Entity, transformation);
            Code unavailable=ResourcePortContracts.Blocker(ports,TransformationContracts.ToolBindings(world,p.Entity,transformation,p.ToolSources));
            if(!unavailable.IsNone)return unavailable;
            var conversion = new ResourceConversionIntent(p.WorkInProgressOwner, p.Entity.Owner, inputs,
                ResourcePortContracts.QuantitiesByEndpoint(ports, transformation.Outputs, transformation.OutputQualities), new Code(transformation.Id));
            SpatialPlacement placement = null;
            IEntityCreation creation = null;
            if (transformation.EntityOutput != null)
            {
                Code refusal = EntityRollout.Find(world, p.Entity, transformation.EntityOutput, out placement);
                if (!refusal.IsNone) return refusal;
                refusal = world.PrepareCreation(transformation.EntityOutput, p.Entity.Owner, p.Entity.Id, placement, out creation);
                if (!refusal.IsNone) return refusal;
            }
            ResourceInventoryResult admission = transactions.EvaluateConversion(conversion, creation);
            if (!admission.Blocker.IsNone) return admission.Blocker;
            if (admission.Status == ResourceTransferStatus.NoCapacity) return TransformerManager.BlockerOutput;
            if (admission.Status == ResourceTransferStatus.Incompatible) return TransformerManager.BlockerOutputCompatibility;
            if (!admission.Ready) throw new InvalidOperationException("transformation lost its actual conversion admission: " + admission.Status);
            ResourceInventoryResult completion = transactions.CommitConversion(conversion, creation);
            if (!completion.Committed)
                throw new InvalidOperationException("conversion changed after complete output admission");
            Entity made = completion.CreatedEntity.IsValid ? world.Entities.Get(completion.CreatedEntity) : null;
            if (!transformation.Outputs.IsEmpty)
                world.Notify(TransformerManager.NoteResources, p.Entity.Owner, p.Entity.Id, transformation.Id);
            if (made != null)
            {
                TransformationContracts.BindOutputHome(world, p, transformation, p.Queue[0].Home, made);
                world.Events.Publish(new EntityTransformed { Entity = made.Id, Transformer = p.Entity.Id, Owner = p.Entity.Owner });
                world.Notify(TransformerManager.NoteEntity, p.Entity.Owner, made.Id, transformation.EntityOutput.Id);
            }
            return Code.None;
        }

        public static readonly Code FacetQueue = new Code("facet.transformation-queue");
        public static readonly Code FacetProgress = new Code("facet.transformation-progress");

        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            if (viewer.IsValid && viewer != Entity.Owner) return;
            into.Add(new Facet(FacetQueue, Fix.FromInt(Queue.Count)));
            if (Queue.Count > 0)
            {
                ITransformationDefinition r = world.Content.Get<ITransformationDefinition>(Queue[0].Transformation);
                if (r.Mode == TransformationMode.Batch) into.Add(new Facet(FacetProgress, Progress.FractionOf(world.Time.Ticks(r.Duration)), r.Id, Fix.One));
            }
        }
    }

    public struct EntityTransformed
    {
        public EntityId Entity;
        public EntityId Transformer;
        public PlayerId Owner;
    }

    /// <summary>
    /// Works every transformer's queue: gets the head transformation's inputs delivered (standing demands on the
    /// transformer's store), takes inputs and cash together, works while the game lets it operate, then
    /// delivers outputs. A factory needs no machine on it — it is the machine.
    /// </summary>
    public sealed partial class TransformerManager : GameSystem, ICompositionValidator, ILogisticsDemandOwner, IResourceEndpointProvider, ITransformationQueue, ITransformationOperationQueue
    {
        private readonly World _world;
        public TransformerManager(World world) { _world = world; }
        public World World => _world;
        public string DemandScope => TransformationModule.ModuleId;
        public Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (!_world.ViewFor(viewer).CanObserveOwned(_world.Entities.Get(destination))) return Codes.NotFound;
            if (source.Scope != DemandScope || source.Identity != destination.Value || source.Entry.IsNone) return Codes.NotFound;
            Entity entity = _world.Entities.Get(destination);
            ITransformer provider = entity?.Owner == company ? entity.Get<ITransformer>(source.Entry) : null;
            return provider != null && TransformationContracts.CanDiscloseExecution(_world, viewer, TransformationContracts.Read(_world, provider))
                ? Code.None : Codes.NotFound;
        }
        public LogisticsDemandSourceCheck CheckSource(LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (source.Scope != DemandScope || source.Identity != destination.Value || source.Entry.IsNone)
                return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            Entity entity = _world.Entities.Get(destination);
            if (entity == null) return LogisticsDemandSourceCheck.Expired(LogisticsDemandCodes.Expired);
            if (entity.Owner != company) return LogisticsDemandSourceCheck.Invalid(Codes.NotOwned);
            ITransformer provider = entity.Get<ITransformer>(source.Entry);
            if (provider == null || !TransformationContracts.Definition(provider).AutomaticSupply)
                return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            int version = entity.Version; PlayerId owner = entity.Owner; EntityId next = _world.Entities.NextIdentity;
            TransformationSnapshot state = TransformationContracts.Read(_world, provider);
            OperationControlSnapshot control = OperationContracts.ReadControl(_world, provider);
            if (entity.Version != version || entity.Owner != owner || _world.Entities.NextIdentity != next ||
                !ReferenceEquals(_world.Entities.Get(entity.Id), entity))
                throw new InvalidOperationException("transformer supply read mutated producer identity/state");
            if (!control.Enabled) return LogisticsDemandSourceCheck.Expired(OperationCodes.Disabled);
            bool nextDraw = state.Operating.NextBlocker.IsNone && state.Flow.NextBlocker.IsNone && state.Blocker != BlockerFlowRange &&
                (!state.Operating.NextInputs.IsEmpty || !state.Flow.NextInputs.IsEmpty || !state.Flow.NextTools.IsEmpty);
            return state.QueueCount > 0 && (state.Phase == TransformationPhase.Idle || state.Phase == TransformationPhase.Supplying ||
                state.Phase == TransformationPhase.Working && nextDraw)
                ? LogisticsDemandSourceCheck.Active(false) : LogisticsDemandSourceCheck.Expired(LogisticsDemandCodes.Expired);
        }
        public override int StateSchemaVersion => 2;
        public static readonly Code NoteEntity = new Code("note.entity-produced");
        public static readonly Code NoteResources = new Code("note.resources-produced");
        public static readonly Code BlockerOutput = new Code("blocker.output-full");
        public static readonly Code BlockerOutputCompatibility = new Code("blocker.output-incompatible");
        public static readonly Code BlockerStopped = new Code("blocker.stopped");
        public static readonly Code LedgerTransformation = new Code("ledger.transformation");

        public override string Id => "sim.transformations";

        public void ValidateComposition(World world)
        {
            StaffingGateBindingContracts.ValidateComposition(world);
            foreach (ITransformationDefinition transformation in world.Content.All<ITransformationDefinition>())
            {
                try { world.Time.Ticks(transformation.Duration); }
                catch (ArgumentException ex) { throw new ContentException(transformation.Id + ": invalid transformation duration: " + ex.Message); }
                catch (OverflowException ex) { throw new ContentException(transformation.Id + ": transformation duration range: " + ex.Message); }
            }
            foreach (EntityDef definition in world.Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition presence = definition.Component<ISpatialPresenceDefinition>();
                if (presence != null && presence.Space != world.Settings.Space) continue;
                foreach (ComponentDef component in definition.Components)
                {
                    if (component is ITransformationCompositionRequirement requirement) requirement.ValidateComposition(world, definition);
                    if (!(component is ITransformerDefinition transformer)) continue;

                    if (transformer.AutomaticSupply && !world.Services.Has<ITransformationSupplyRequests>())
                        throw new ContentException(definition.Id + ": automatic supply requires ITransformationSupplyRequests");
                    foreach (ITransformationDefinition transformation in transformer.Transformations)
                    {

                        if (BusinessModeRules.EvaluateCost(world, MonetaryCostCategory.Manufacturing, transformation.Cash).Applicability == MonetaryCostApplicability.UnavailableCapability)
                            throw new ContentException(definition.Id + ": enabled monetary transformation '" + transformation.Id + "' requires IEconomy");

                        if (transformation.OperatingInputs.Count != 0)
                        {
                            try
                            {
                                var initial = new Dictionary<Code, List<ResourceAmount>>();
                                foreach (var route in transformation.OperatingPorts)
                                {
                                    var selected = definition.Component<IResourcePortDefinition>(route.Port);
                                    var rates = new List<IResourceRate>();
                                    foreach (var rate in transformation.OperatingInputs) if (rate.Resource.Resource == route.Resource) rates.Add(rate);
                                    Bill first = ResourceRates.Between(rates, world.Time, selected.CapacityPolicy, WorkProgress.Zero,
                                        new WorkProgress(1), ResourceRateRounding.Ceiling);
                                    if (!initial.TryGetValue(route.Port, out var amounts)) initial.Add(route.Port, amounts = new List<ResourceAmount>());
                                    amounts.AddRange(first.Lines);
                                }
                                foreach (var group in initial)
                                    if (!CapacityPolicyContracts.Check(definition.Component<IResourcePortDefinition>(group.Key).CapacityPolicy, 
                                        Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), new Bill(group.Value).Lines).Fits)
                                        throw new ContentException(definition.Id + ": selected port cannot hold its complete first operating draw");
                            }
                            catch (OverflowException) { throw new ContentException(definition.Id + ": first operating draw exceeds native representation"); }
                        }

                        if (transformation.EntityOutput == null) continue;
                        if (!ResourceTransactions.CheckResultDefinition(transformation.EntityOutput).IsNone)
                            throw new ContentException(definition.Id + ": conversion entity results require empty authored opening inventory");
                        Code refusal = EntityRollout.CheckDefinition(world, definition, transformation.EntityOutput);
                        if (!refusal.IsNone) throw new ContentException(definition.Id + ": transformation '" + transformation.Id + "' rollout refused: " + refusal);
                    }
                }
            }
        }

        public override void Update(World world, TickInfo tick)
        {
            world.RequireHealthyUpdate();
            ValidateLive();
            var context = new TransformationExecutionContext(world, tick);
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is ITransformer provider)
                        {
                            foreach (Component source in entity.Components)
                                if (source is ITransformationStaffingLifecycle staffing)
                                {
                                    staffing.PrepareStaffing(world, provider);
                                    world.RequireHealthyUpdate();
                                }
                            provider.Advance(context);
                            world.RequireHealthyUpdate();
                        }
            ValidateLive();
        }

        internal static Code CheckAccess(World world, PlayerId owner, ITransformationDefinition transformation, bool playerIntent = true)
        {
            Code Check(IContentDefinition definition) => playerIntent ? world.Services.Get<IContentAccess>().Check(world, owner, definition)
                : world.Services.Get<IContentOperationAccess>().CheckOperation(world, owner, definition);
            Code refusal = Check(transformation);
            return refusal.IsNone && transformation.EntityOutput != null ? Check(transformation.EntityOutput) : refusal;
        }

        public Code CheckEnqueue(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home)
            => CheckQueueIntent(company, entityId, capability, transformationId, count, repeat, home, true);
        public Code CheckEnqueueOperation(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home)
            => CheckQueueIntent(company, entityId, capability, transformationId, count, repeat, home, false);
        private Code CheckQueueIntent(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home, bool playerIntent)
        {
            World world = World;
            world.RequireHealthyUpdate();
            Entity entity = world.Entities.Get(entityId);
            if (entity == null || !entity.Alive || entity.Owner != company || playerIntent && !world.ViewFor(company).CanObserveOwned(entity)) return Codes.NotFound;
            if (capability.IsNone) return Codes.InvalidArgument;
            ITransformer provider = entity.Get<ITransformer>(capability);
            if (provider == null) return Codes.NotFound;
            ITransformationDefinition transformation = world.Content.Find<ITransformationDefinition>(transformationId);
            if (transformation == null) return Codes.UnknownDef;
            var accounts = ObserveQueueAccounts();
            Code result = EntityObservation.ReadProvider(world, provider, entity, () =>
            {
                if (playerIntent && !TransformationContracts.CanDisclose(world, company, transformation)) return Codes.UnknownDef;
                if (!TransformationContracts.Supports(provider, transformation)) return EnqueueTransformationHandler.NotMadeHere;
                Code refusal = CheckAccess(world, company, transformation, playerIntent);
                if (!refusal.IsNone) return refusal;
                if (count < 1) return Codes.InvalidArgument;
                refusal = TransformationContracts.ResolveOutputHome(world, provider, transformation, home, out EntityId resolvedHome);
                return refusal.IsNone ? TransformationContracts.CheckEnqueue(world, provider, transformation, count, repeat, resolvedHome) : refusal;
            });
            accounts?.Verify(); return result;
        }
        public Code Enqueue(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home)
            => EnqueueCore(company, entityId, capability, transformationId, count, repeat, home, true);
        public Code EnqueueOperation(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home)
            => EnqueueCore(company, entityId, capability, transformationId, count, repeat, home, false);
        private Code EnqueueCore(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home, bool playerIntent)
        {
            try { return AppendQueue(company, entityId, capability, transformationId, count, repeat, home, playerIntent); }
            catch (Exception error) { World.LatchFault("transformations.enqueue", error); throw; }
        }
        private Code AppendQueue(PlayerId company, EntityId entityId, Code capability,
            string transformationId, int count, bool repeat, EntityId home, bool playerIntent)
        {
            Code refusal = CheckQueueIntent(company, entityId, capability, transformationId, count, repeat, home, playerIntent);
            if (!refusal.IsNone) return refusal;
            Entity entity = World.Entities.Get(entityId); ITransformer provider = entity.Get<ITransformer>(capability);
            ITransformationDefinition definition = World.Content.Get<ITransformationDefinition>(transformationId);
            refusal = TransformationContracts.ResolveOutputHome(World, provider, definition, home, out EntityId resolvedHome);
            if (!refusal.IsNone) return refusal;
            home = resolvedHome;
            var contextFacts = EntityObservation.Capture(World, entity, allowEntityVersionChange: true);
            var contextSpace = EntityObservation.CaptureSpace(World, World.Space);
            TransformationSnapshot before = TransformationContracts.Read(World, provider); int version = entity.Version;
            OperationControlSnapshot controls = QueueControls(provider);
            ulong siblings = QueueSiblings(entity, provider);
            var accounts = ObserveQueueAccounts();
            provider.Enqueue(definition, count, repeat, home);
            contextFacts.Verify(); contextSpace.Verify();
            accounts?.Verify();
            TransformationSnapshot after = TransformationContracts.Read(World, provider);
            OperationControlSnapshot applied = QueueControls(provider);
            if (controls.Enabled != applied.Enabled || controls.Target != applied.Target || siblings != QueueSiblings(entity, provider))
                throw new InvalidOperationException("transformer changed controls or sibling components while appending its queue");
            if (!SameOperatingCosts(before, after))
                throw new InvalidOperationException("queue append changed earned operating exposure or consumption");
            int appended = repeat ? 1 : count;
            if (entity.Version <= version || (long)before.QueueCount + appended != after.QueueCount || after.NextExecution != checked(before.NextExecution + appended) ||
                after.Phase != before.Phase || !after.Progress.Equals(before.Progress) || after.Blocker != before.Blocker ||
                after.Wip.Execution != before.Wip.Execution || after.Wip.Version != before.Wip.Version ||
                !TransformationContracts.SamePortLinks(after.Wip.Sources, before.Wip.Sources) ||
                !TransformationContracts.SameToolFacts(after.Tools, before.Tools))
                throw new InvalidOperationException("transformer violated exact queue append/state protocol");
            // Local inventory version aliases Entity.Version: the legitimate queue Touch changes it.
            // Tool commitments must stay identical, not retain a stale enclosing-entity revision.
            for (int i = 0; i < after.QueueCount; i++)
            {
                TransformationQueueEntry expected = i < before.QueueCount ? before.Queue[i] : new TransformationQueueEntry(after.Queue[i].Execution, definition.Id, repeat, home);
                TransformationQueueEntry actual = after.Queue[i];
                if (!SameEntry(actual, expected) || i >= before.QueueCount && actual.Execution < before.NextExecution)
                    throw new InvalidOperationException("transformer changed the queue prefix or appended another request");
            }
            return Code.None;
        }
        private ulong QueueSiblings(Entity entity, ITransformer provider)
        {
            var observation = EntityObservation.Capture(World, entity); var hash = new StateHasher();
            foreach (Component component in entity.Components) if (!ReferenceEquals(component, provider)) component.Sync(hash);
            observation.Verify(); return hash.Value;
        }
        private OperationControlSnapshot QueueControls(ITransformer provider)
        {
            return OperationContracts.ReadControl(World, provider);
        }
        private static bool SameEntry(TransformationQueueEntry actual, TransformationQueueEntry expected)
            => actual.Execution == expected.Execution && actual.Transformation == expected.Transformation && actual.Repeat == expected.Repeat && actual.Home == expected.Home;
        private static bool SameBill(Bill actual, Bill expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (ResourceAmount line in expected.Lines) if (actual.AmountOf(line.Resource) != line.Amount) return false;
            return true;
        }
        private static bool SameOperatingCosts(TransformationSnapshot before, TransformationSnapshot after)
            => before.Operating.ElapsedTicks == after.Operating.ElapsedTicks
                && SameBill(after.Operating.Consumed, before.Operating.Consumed);
        private EntityObservation.ProviderScope? ObserveQueueAccounts()
            => World.Services.TryGet<IEconomy>(out var accounts)
                ? EntityObservation.CaptureProvider(World, accounts, "transformations.queue-account")
                : (EntityObservation.ProviderScope?)null;
        public Code CheckCancel(PlayerId company, EntityId entityId, Code capability, int index)
        {
            World world = World;
            world.RequireHealthyUpdate();
            Entity entity = world.Entities.Get(entityId);
            if (entity == null || !entity.Alive || entity.Owner != company || !world.ViewFor(company).CanObserveOwned(entity)) return Codes.NotFound;
            if (capability.IsNone) return Codes.InvalidArgument;
            ITransformer provider = entity.Get<ITransformer>(capability);
            if (provider == null) return Codes.NotFound;
            var accounts = ObserveQueueAccounts();
            TransformationSnapshot state = TransformationContracts.Read(world, provider);
            Code result = index < 0 || index >= state.QueueCount ? Codes.InvalidArgument
                : index == 0 && state.Phase == TransformationPhase.Returning ? BlockerReturning
                : TransformationContracts.CheckCancel(world, provider, index);
            accounts?.Verify(); return result;
        }
        public Code Cancel(PlayerId company, EntityId entityId, Code capability, int index)
        {
            try { return CancelQueue(company, entityId, capability, index); }
            catch (Exception error) { World.LatchFault("transformations.cancel", error); throw; }
        }
        private Code CancelQueue(PlayerId company, EntityId entityId, Code capability, int index)
        {
            Code refusal = CheckCancel(company, entityId, capability, index);
            if (!refusal.IsNone) return refusal;
            Entity entity = World.Entities.Get(entityId); ITransformer provider = entity.Get<ITransformer>(capability);
            var contextFacts = EntityObservation.Capture(World, entity, allowEntityVersionChange: true);
            var contextSpace = EntityObservation.CaptureSpace(World, World.Space);
            TransformationSnapshot before = TransformationContracts.Read(World, provider); int version = entity.Version;
            OperationControlSnapshot controls = QueueControls(provider); ulong siblings = QueueSiblings(entity, provider);
            var accounts = ObserveQueueAccounts();
            provider.Cancel(World, index);
            contextFacts.Verify(); contextSpace.Verify();
            accounts?.Verify();
            TransformationSnapshot after = TransformationContracts.Read(World, provider);
            OperationControlSnapshot applied = QueueControls(provider);
            if (controls.Enabled != applied.Enabled || controls.Target != applied.Target || siblings != QueueSiblings(entity, provider))
                throw new InvalidOperationException("transformer changed controls or sibling components while cancelling its queue");
            if (entity.Version <= version || after.NextExecution != before.NextExecution || after.Wip.Endpoint != before.Wip.Endpoint ||
                after.Wip.Execution != before.Wip.Execution || after.Wip.Version != before.Wip.Version ||
                !SameBill(after.Wip.Stock, before.Wip.Stock) || !TransformationContracts.SamePortLinks(after.Wip.Sources, before.Wip.Sources) ||
                !TransformationContracts.SameToolFacts(after.Tools, before.Tools))
                throw new InvalidOperationException("cancellation changed actual material/tool commitments before physical return");
            bool returning = index == 0 && before.Wip.Execution > 0;
            if ((index > 0 || returning) && !SameOperatingCosts(before, after))
                throw new InvalidOperationException("queue cancellation changed earned operating exposure or consumption");
            if (after.QueueCount != before.QueueCount - (returning ? 0 : 1))
                throw new InvalidOperationException("cancellation did not retain its returning head or remove exactly its selected entry");
            for (int i = 0; i < after.QueueCount; i++)
                if (!SameEntry(after.Queue[i], before.Queue[!returning && i >= index ? i + 1 : i]))
                    throw new InvalidOperationException("cancellation changed an unrelated queue entry");
            if (index > 0)
            {
                if (after.Phase != before.Phase || !after.Progress.Equals(before.Progress) || after.Blocker != before.Blocker)
                    throw new InvalidOperationException("tail cancellation changed active head execution");
            }
            else if (returning)
            {
                if (after.Phase != TransformationPhase.Returning || !after.Progress.Equals(before.Progress) || !after.Blocker.IsNone)
                    throw new InvalidOperationException("active cancellation must request physical return without changing earned work");
            }
            else if (after.Phase != TransformationPhase.Idle || !after.Progress.Equals(WorkProgress.Zero) || !after.Blocker.IsNone)
                throw new InvalidOperationException("removed head retained stale processing state");
            return Code.None;
        }
        private void ValidateLive()
        {
            var active = new Dictionary<ReservationOwner, (ResourceEndpointId Wip, IReadOnlyList<TransformationPortLink> Tools)>();
            foreach (Entity entity in World.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is ITransformer provider)
                        {
                            TransformationSnapshot state = TransformationContracts.Read(World, provider);
                            if (provider is ITransformerWip wip && state.Wip.Execution > 0)
                                active.Add(wip.WorkInProgressOwner, (state.Wip.Endpoint, state.Tools.Sources));
                        }
            ValidateWipClaims(active);
        }
        public override void OnLoaded(World world) => ValidateLive();
        public override void Sync(IStateSync sync)
        {
            if (sync is IStateObservationSync probe && probe.IsObservation && !sync.Reading)
            {
                // This system has no local saved fields. Endpoint reads can occur inside the
                // original WIP transaction, before its settled cross-owner invariant holds.
                // Observe the actual owning entities with their existing codecs, not a validated
                // public snapshot or another WIP representation. Stable checkpoints still validate.
                var owners = new List<Entity>();
                foreach (Entity entity in World.Entities.All)
                    if (entity.Alive)
                        foreach (Component component in entity.Components)
                            if (component is ITransformerWip) { owners.Add(entity); break; }
                sync.Count(owners.Count);
                foreach (Entity entity in owners) entity.Sync(sync);
                return;
            }
            if (!sync.Reading) ValidateLive();
        }


    }

    // ======================================================================== commands





    internal sealed class EnqueueTransformationHandler : CommandHandler<EnqueueTransformationCommand>
    {
        public static readonly Code NotMadeHere = new Code("core.not-made-here");
        protected override Code Check(World world, EnqueueTransformationCommand command)
            => world.Services.Get<ITransformationQueue>().CheckEnqueue(command.Player, command.Transformer, command.Capability,
                command.Transformation, command.Count, command.Repeat, command.Home);
        protected override void Apply(World world, EnqueueTransformationCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<ITransformationQueue>().Enqueue(command.Player, command.Transformer, command.Capability,
                command.Transformation, command.Count, command.Repeat, command.Home);
            if (!refusal.IsNone) throw new InvalidOperationException("queue admission changed: " + refusal);
            context.Entity = command.Transformer; context.Arg0 = command.Repeat ? 1 : command.Count;
        }
    }
    internal sealed class CancelTransformationHandler : CommandHandler<CancelTransformationCommand>
    {
        protected override Code Check(World world, CancelTransformationCommand command)
            => world.Services.Get<ITransformationQueue>().CheckCancel(command.Player, command.Transformer, command.Capability, command.Index);
        protected override void Apply(World world, CancelTransformationCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<ITransformationQueue>().Cancel(command.Player, command.Transformer, command.Capability, command.Index);
            if (!refusal.IsNone) throw new InvalidOperationException("queue cancellation changed: " + refusal);
            context.Entity = command.Transformer;
        }
    }
}
