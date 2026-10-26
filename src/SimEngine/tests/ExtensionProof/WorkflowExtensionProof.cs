using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Real extension declarations; no custom loader, graph, scheduler, reservations or gameplay.
    public sealed class WorkflowExtensionModule : GameModule
    {
        public const string ModuleId = "proof.workflow-extension";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(WorkModule.ModuleId, Version), new ModuleRequirement(ConstructionModule.ModuleId, Version),
            new ModuleRequirement("points", Version), new ModuleRequirement(WorldSetupModule.ModuleId, Version) };
        public InitializeProbe Initialization { get; } = new InitializeProbe("proof.workflow-extension.init");
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentActivityContentDef>("colony_workflows");
            types.Section<IndependentWorkerRoleDef>("colony_roles");
            types.Section<IndependentStaffedExecutorDef>("colony_staffed");
            types.Section<IndependentTimedExecutorDef>("colony_timed");
            types.Section<IndependentTargetResourceDef>("colony_inputs");
            types.Section<PriorActivityProofDef>("colony_precedents");
            types.Component<RemovalAdmissionProbeDef>("removal_admission_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            var workflow = composer.Content.Get<IAuthoredActivityDefinition>("extension.build");
            var construction = composer.Content.Get<EntityDef>("extension.facility").Component<IConstructionDefinition>();
            var role = composer.Content.Get<IWorkerRoleDefinition>("extension.builder");
            var staffed = composer.Content.Get<IActivityExecutor>("extension.staffed");
            ActualAliases = workflow is IndependentActivityContentDef && role is IndependentWorkerRoleDef
                && staffed is IndependentStaffedExecutorDef && composer.Content.Owns(workflow)
                && ReferenceEquals(construction.Workflow, workflow) && ReferenceEquals(construction.Graph, workflow.Graph)
                && ReferenceEquals(workflow.Graph.Find("assemble").Executor, staffed)
                && ReferenceEquals(workflow.Graph.Find("materials").Requirement, composer.Content.Get<IWorkRequirement>("extension.materials"))
                && composer.Content.Get<EntityDef>("extension.machine").Component<IWorkerDefinition>() is ContractWorkerDef;
            composer.AddSystem(Initialization, 300);
        }
    }
}
