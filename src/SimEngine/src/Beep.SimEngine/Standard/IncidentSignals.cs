using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored binding over a named incident and its existing compiled phase; no live state.</summary>
    public sealed class IncidentSignalDef : ComponentDef<IncidentSignal>, IIncidentSignalDefinition
    {
        private IncidentSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Source => _declaration.Source;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public IIncidentPhaseDefinition Phase => _declaration.Phase;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => IncidentSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => IncidentBindingContracts.ValidateSignal(world, this);
    }

    /// <summary>Typed stateless facts consumed by existing process gates and next-step automation.</summary>
    public sealed class IncidentSignal : Component, IAutomationSignalSource
    {
        public new IIncidentSignalDefinition Def => (IIncidentSignalDefinition)base.Def;
        public Code CapabilityId => Def.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            var definition = IncidentBindingContracts.ReadSignal(world, Def, Entity);
            IPhasedIncidentResponse response = Entity.Get<IPhasedIncidentResponse>(definition.Source);
            if (response == null || !ReferenceEquals(response.Workflow, definition.Workflow)) return signal.Unavailable(IncidentCodes.NotResponse);
            IncidentLifecycleSnapshot lifecycle = IncidentPhases.ReadLifecycle(world, response, out Order order);
            switch (signal.Metric.Id)
            {
                case "incident.active": return signal.Boolean(lifecycle.Active);
                case "incident.phase-hold":
                    return signal.Boolean(lifecycle.Active && IncidentPhases.CompletedNodes(definition.Phase, order) == definition.Phase.Nodes.Count);
                default: return signal.Unavailable(OperationCodes.SignalUnavailable);
            }
        }
    }
}
