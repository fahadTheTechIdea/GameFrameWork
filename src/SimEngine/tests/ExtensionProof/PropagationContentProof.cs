using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentPointPropagationRuleDef : Def, IIncidentPropagationRule
    {
        private IncidentPropagationPolicyDeclaration _policy;
        private string _fault;
        private Fix _radius;
        public GameDuration Interval => _fault == "interval-zero" ? GameDuration.Zero : _policy.Interval;
        public GameDuration Delay => _policy.Delay;
        public Code TargetSource => _policy.TargetSource;
        public Code TargetKind => _fault == "target-none" ? Code.None : _policy.TargetKind;
        public bool CrossCompany => _policy.CrossCompany;
        protected override void ReadCore(JsonReader reader)
        {
            _policy = IncidentPropagationPolicyDeclaration.Read(reader); _radius = reader.Fix("radius"); _fault = reader.String("fault");
            if (_radius < Fix.Zero) throw reader.Error("point propagation radius cannot be negative");
        }
        protected override void ResolveCore(ContentResolver content) => _policy.Resolve(content);
        public void ValidateSource(EntityDef source) => _policy.ValidateSource(source);
        public void ValidateBinding(World world)
        {
            _policy.ValidateBinding(world, Id);
            if (_fault == "validation-throw") throw new InvalidOperationException("proof.propagation.validation");
            if (_fault == "validation-pause") ((GameClock)world.Clock).Pause();
            if (_fault == "validation-reenter") ((ICompositionValidator)world.Services.Get<IIncidentPropagationState>()).ValidateComposition(world);
        }
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
        {
            if (_fault == "observe-default") return default;
            if (_fault == "observe-throw") throw new InvalidOperationException("proof.propagation.observe");
            if (_fault == "observe-pause") ((GameClock)context.World.Clock).Pause();
            if (_fault == "observe-touch") target.Touch();
            if (_fault == "observe-source-private") context.Source.Get<ExposurePrivateProbe>().Corrupt();
            if (_fault == "observe-target-private") target.Get<ExposurePrivateProbe>().Corrupt();
            if (_fault == "observe-reenter") ((ICompositionValidator)context.World.Services.Get<IIncidentPropagationState>()).ValidateComposition(context.World);
            Code blocker = _policy.CheckConditions(context, target, Id);
            if (!blocker.IsNone) return IncidentExposureObservation.Refused(blocker);
            var a = context.Source.Get<ISpatialPresence>()?.Capture() as PointSnapshot;
            var b = target.Get<ISpatialPresence>()?.Capture() as PointSnapshot;
            if (a == null || b == null || a.Space != b.Space) return IncidentExposureObservation.Refused(IncidentPropagationCodes.Presence);
            return Fix.Abs(a.Position - b.Position) <= _radius ? IncidentExposureObservation.Ready : IncidentExposureObservation.Refused(IncidentPropagationCodes.Distance);
        }
    }
    public sealed class ReusedPropagationEmitterDef : ComponentDef<IncidentPropagationEmitter>, IIncidentPropagationDefinition
    {
        private IncidentPropagationBindingDeclaration _declaration;
        private string _fault;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Source => _declaration.Source;
        public IIncidentPropagationRule Rule => _fault == "rule-null" ? null : _fault == "rule-copy" ? new IndependentPointPropagationRuleDef() : _declaration.Rule;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("metadata_fault"); _declaration = IncidentPropagationBindingDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => IncidentPropagationContracts.ValidateBinding(world, this);
    }
    // Negative probes are deliberately separate from gameplay state, to test actual whole-entity codecs.
    public sealed class ExposurePrivateProbeDef : ComponentDef<ExposurePrivateProbe>
    { protected override void ReadCore(JsonReader reader) { } }
    public sealed class ExposurePrivateProbe : Component
    {
        private int _value;
        public int Value => _value;
        public void Corrupt() => _value++;
        public override void Sync(IStateSync sync) => sync.Int(ref _value);
    }
    public sealed class UnboundPropagationEmitterDef : ComponentDef<IncidentPropagationEmitter>, INamedCapabilityDefinition
    {
        private IncidentPropagationBindingDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentPropagationBindingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class PropagationContentModule : ControlContractModule
    {
        public PropagationContentModule(InitializeProbe initialize) : base(initialize) { }
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(IncidentPropagationModule.ModuleId, Version) };
        public bool ActualRule { get; private set; }
        public bool IndependentBinding { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentPointPropagationRuleDef>("independent_incident_propagation_rules");
            types.Component<ReusedPropagationEmitterDef>("independent_propagation", 1);
            types.Component<ExposurePrivateProbeDef>("exposure_private_probe", 1);
            types.Component<UnboundPropagationEmitterDef>("unbound_propagation", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var rule = composer.Content.Get<IIncidentPropagationRule>("contract.field-spread");
            var binding = composer.Content.Get<EntityDef>("field.plant").Component<IIncidentPropagationDefinition>(new Code("spread"));
            ActualRule = rule.GetType().BaseType == typeof(Def) && composer.Content.Owns(rule) && ReferenceEquals(binding.Rule, rule);
            IndependentBinding = binding is ReusedPropagationEmitterDef;
        }
    }
}
