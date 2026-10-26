using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata implementations; the original engine owns all episode and signal execution.
    public sealed class IndependentIncidentEpisodeDef : ComponentDef<Incident>, IIncidentEpisodeDefinition
    {
        private IncidentEpisodeDeclaration _declaration;
        private string _fault;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentWorkflowDefinition Workflow => _fault == "workflow-null" ? null : _declaration.Workflow;
        public EntityStateBindings States => _fault == "states-null" ? null : _declaration.States;
        public bool Supports(Code kind) => kind == _declaration.Workflow.Kind;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("fault"); _declaration = IncidentEpisodeDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world)
        {
            IncidentBindingContracts.ValidateEpisode(world, this);
            if (_fault == "validation-throw") throw new InvalidOperationException("proof.incident.episode");
            if (_fault == "validation-pause") ((GameClock)world.Clock).Pause();
            if (_fault == "validation-reenter") world.System<IncidentResponseSystem>().ValidateComposition(world);
        }
    }
    public sealed class IndependentIncidentSignalDef : ComponentDef<IncidentSignal>, IIncidentSignalDefinition
    {
        private IncidentSignalDeclaration _declaration;
        private string _fault;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Source => _declaration.Source;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public IIncidentPhaseDefinition Phase => _fault == "phase-copy" ? new IncidentPhaseDefinition(_declaration.Phase.Id, _declaration.Phase.Nodes) : _declaration.Phase;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => _fault == "metrics-empty" ? Array.Empty<AutomationMetricSchema>() : IncidentSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("fault"); _declaration = IncidentSignalDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world)
        {
            IncidentBindingContracts.ValidateSignal(world, this);
            if (_fault == "validation-throw") throw new InvalidOperationException("proof.incident.signal");
            if (_fault == "validation-pause") ((GameClock)world.Clock).Pause();
            if (_fault == "validation-reenter") world.System<IncidentResponseSystem>().ValidateComposition(world);
        }
    }
    // Deliberately invalid reusable runtime pairing, including when its entity is never spawned.
    public sealed class UnboundIncidentDef : ComponentDef<Incident>, IPhasedIncidentDefinition
    {
        private IncidentEpisodeDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public bool Supports(Code kind) => kind == Workflow.Kind;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentEpisodeDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IncidentBindingsModule : ControlContractModule
    {
        public IncidentBindingsModule(InitializeProbe initialize) : base(initialize) { }
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Component<IndependentIncidentEpisodeDef>("independent_incident", 2);
            types.Component<IndependentIncidentSignalDef>("independent_incident_signal", 1);
            types.Component<UnboundIncidentDef>("unbound_incident", 2);
        }
        public bool ActualBindings { get; private set; }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var owner = composer.Content.Get<EntityDef>("field.plant");
            var episode = owner.Component<IIncidentEpisodeDefinition>(new Code("pressure.leak"));
            var signal = owner.Component<IIncidentSignalDefinition>(new Code("incident-state"));
            ActualBindings = episode is IndependentIncidentEpisodeDef && signal is IndependentIncidentSignalDef &&
                ReferenceEquals(episode.Workflow, signal.Workflow) &&
                ReferenceEquals(signal.Phase, IncidentPhases.Find(episode.Workflow, signal.Phase.Id));
        }
    }
}
