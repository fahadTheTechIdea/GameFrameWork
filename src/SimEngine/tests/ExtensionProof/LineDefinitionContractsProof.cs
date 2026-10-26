using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentLineDef : Def, ILineDefinition
    {
        private LineDeclaration _declaration;
        public bool AutomaticSupply => Fault == "supply-mode" ? !_declaration.AutomaticSupply : _declaration.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => _declaration.DisclosureContent;
        public IActivityDefinition Workflow => _declaration.Workflow;
        public string LayerId => _declaration.LayerId;
        public string SegmentNode => _declaration.SegmentNode;
        public Bill SupplyCapacity => Fault == "supply-null" ? null : Fault == "supply-extra" ? _excessSupply : _declaration.SupplyCapacity;
        public EntityDef WorkMarker => _declaration.WorkMarker;
        public ISpatialLayerDefinition Layer { get { ObserveLayer?.Invoke(); return Fault == "layer-null" ? null : Fault == "foreign-layer" ? new SpatialLayerDef() : _declaration.Layer; } }
        public ISpatialLayerDefinition OwnerLayer => _declaration.OwnerLayer;
        public Fix Value => Fault == "value-zero" ? Fix.Zero : _declaration.Value;
        public bool Owned => _declaration.Owned;
        public ILinePlacementPolicy PlacementPolicy => Fault == "placement-null" ? null : _declaration.PlacementPolicy;
        public string SegmentEffect => Fault == "segment-wrong" ? "segment.finish" : _declaration.SegmentEffect;
        public string FinishEffect => _declaration.FinishEffect;
        public string PlanKey => Fault == "plan-wrong" ? "line:wrong" : _declaration.PlanKey;
        public IActivityGraph Graph => Fault == "graph-null" ? null : Fault == "graph-source" ? _declaration.Workflow.Graph : _declaration.Graph;
        public string OnComplete => _declaration.OnComplete;
        public string Fault { get; private set; }
        internal Action ObserveLayer;
        internal Action ObserveIntent;
        protected override void ReadCore(JsonReader reader) { Fault = reader.String("metadata_fault"); _declaration = LineDeclaration.Read(reader, this); }
        private Bill _excessSupply;
        protected override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); _excessSupply = new Bill(new[] { new ResourceAmount(content.Content.ResourceId("steel"), Fix.One) }); }
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public string IntentIdentity(World world, JsonValue arguments) => _declaration.IntentIdentity(world, arguments);
        public System.Collections.Generic.IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => _declaration.ReadEntityReferences(world, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination) => _declaration.CheckEntityReplacement(world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination) => _declaration.ReplaceEntityReference(world, arguments, source, destination);
        public IActivityDefinition BindExecution(World world, EntityId target) => _declaration.BindExecution(world, target);
        public string ExecutionIdentity(World world, Order order) => _declaration.ExecutionIdentity(world, order);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments) => _declaration.CheckSubmission(world, company, arguments);
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments) => _declaration.CheckActivation(world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority) => _declaration.Activate(world, company, arguments, priority);
        public PlanIntent CreateIntent(JsonValue path, string group, int priority, bool paused)
        { ObserveIntent?.Invoke(); return _declaration.CreateIntent(path, group, priority, paused); }
        public void ValidateTemplate(World world, JsonValue arguments) => _declaration.ValidateTemplate(world, arguments);
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor) => _declaration.ValidateTemplateAnchor(world, arguments, anchor);
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
            => _declaration.CaptureTemplate(world, company, arguments, origin, out template);
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
            => _declaration.PlaceTemplate(world, company, arguments, anchor, out intent);
    }


    public sealed class LineDefinitionContractsModule : GameModule
    {
        public override string Id => "proof.line-definition-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public bool ActualContract { get; private set; }
        public readonly InitializeProbe Initialize = new InitializeProbe();
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentLineDef>("independent_lines");
        public override void Compose(WorldComposer composer)
        {
            var line = composer.Content.Get<ILineDefinition>("segment.road-line");
            ActualContract = line is IndependentLineDef && ReferenceEquals(line, composer.Content.Find<ILineDefinition>(line.Id));
            composer.AddSystem(Initialize, 305);
            composer.AddSystem(new ProbeInstaller(), 307);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id => "proof.line-definition-faults";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time) { }
            public override void Initialize(World world)
            {
                foreach (var line in world.Content.All<IndependentLineDef>())
                    if (line.Fault.StartsWith("live-", StringComparison.Ordinal))
                    {
                        line.ObserveLayer = () => Observe(world, line, false);
                        line.ObserveIntent = () => Observe(world, line, true);
                    }
            }
            private static void Observe(World world, IndependentLineDef line, bool intent)
            {
                if (world.Clock.StepIndex == 0 || line.Fault.StartsWith("live-intent-", StringComparison.Ordinal) != intent) return;
                if (line.Fault.EndsWith("throw", StringComparison.Ordinal)) throw new InvalidOperationException("proof.line-definition." + (intent ? "intent" : "layer"));
                if (line.Fault.EndsWith("pause", StringComparison.Ordinal)) ((GameClock)world.Clock).Pause();
            }
        }
    }
}
