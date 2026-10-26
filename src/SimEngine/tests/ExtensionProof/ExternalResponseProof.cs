using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Reuse the framework's sole episode behavior/state/codec. No mod scheduler or copied incident logic.
    public sealed class ExternalIncident : Incident, IPhasedIncidentResponse
    {
        // Instance-only negative report instrumentation; actual episode state/codec remain in Incident.
        public Func<ExternalIncident, IncidentLifecycleSnapshot, IncidentLifecycleSnapshot> LifecycleFault;
        IncidentLifecycleSnapshot IPhasedIncidentResponse.ReadLifecycle()
        {
            var actual = base.ReadLifecycle();
            return LifecycleFault == null ? actual : LifecycleFault(this, actual);
        }
    }
    public sealed class ExternalIncidentDef : ComponentDef<ExternalIncident>, IIncidentEpisodeDefinition
    {
        private IncidentEpisodeDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public EntityStateBindings States => _declaration.States;
        public bool Supports(Code kind) => kind == Workflow.Kind;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentEpisodeDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world) => IncidentBindingContracts.ValidateEpisode(world, this);
    }
    public sealed class ExternalResponseModule : GameModule
    {
        public const string ModuleId = "proof.external-response";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(IncidentResponseModule.ModuleId, Version),
            new ModuleRequirement(EnvironmentalMetricsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ExternalIncidentDef>("external_incident", 2);
            types.Section<IndependentIncidentWorkflowDef>("independent_incident_workflows");
            types.Section<IndependentIncidentCompletionDef>("independent_incident_completions");
            types.Section<IndependentEnvironmentalMetricDef>("independent_environment_metrics");
        }
        public override void Compose(WorldComposer composer)
        {
            var entity = composer.Content.Get<EntityDef>("response.station");
            var incident = entity.Component<ExternalIncidentDef>();
            var workflow = composer.Content.Get<IIncidentWorkflowDefinition>("response.incident");
            var finish = composer.Content.Get<IIncidentCompletionEffect>("response.finish");
            var metric = composer.Content.Get<IEnvironmentalMetricDefinition>("environment.loss");
            if (incident == null || incident.ComponentType != typeof(ExternalIncident) ||
                !ReferenceEquals(incident.Workflow, workflow) || !(workflow is IndependentIncidentWorkflowDef) ||
                !(finish is IndependentIncidentCompletionDef) || workflow.Response.OnComplete != finish.Id ||
                !(metric is IndependentEnvironmentalMetricDef) || !composer.Content.Owns(workflow) ||
                !composer.Content.Owns(finish) || !composer.Content.Owns(metric))
                throw new ContentException("external response proof requires actual independent selected aliases");
        }
    }
}
