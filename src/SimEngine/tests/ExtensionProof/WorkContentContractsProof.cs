using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentWorkerRoleDef : Def, IWorkerRoleDefinition
    {
        public WorkerRoleId Role { get; private set; }
        protected override void ReadCore(JsonReader reader) => Role = WorkerRoleContracts.Read(reader.String("role",Id),reader);
    }
    public sealed class IndependentStaffedExecutorDef : Def, IActivityExecutor
    {
        private StaffedActivityExecution _execution;
        public string Fault { get; private set; }
        internal Action<string, World, Entity, Entity> Observer;
        string IContentDefinition.Id => Fault == "id-alias" ? "wrong.alias" : Id;
        string IContentDefinition.Module => Fault == "module-alias" ? "wrong.module" : Module;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault", "none"); _execution = StaffedActivityExecution.Read(reader); }
        public void ValidateDefinition(IActivityNodeDefinition definition)
        {
            // Invalid authoring/metadata diagnostics only, never normal execution or a second classifier.
            if (Fault == "definition-throw") throw new ContentException("proof.executor.definition");
            if (Fault == "definition-inputs" && (definition.Requirement == null ||
                !definition.RequirementMultiplier.IsPositive || definition.Dependencies == null || definition.Effect != "survey.observe"))
                throw new ContentException("validator requires complete captured node inputs/effect/dependencies");
            _execution.ValidateDefinition(definition);
        }
        public void ValidateComposition(World world)
        { Observer?.Invoke("composition", world, null, null); _execution.ValidateComposition(world); }
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context)
        { Observer?.Invoke("create", context.World, context.Target, context.Worker); return _execution.CreateItinerary(context); }
        public bool MatchesItinerary(ActivityExecutorContext context,IReadOnlyList<Toil> itinerary)
        { Observer?.Invoke("match", context.World, context.Target, context.Worker); return _execution.MatchesItinerary(context,itinerary); }
        public Code CheckWorker(WorkRequirementContext context)
        { Observer?.Invoke("check", context.World, context.Target, context.Worker); return _execution.CheckWorker(context); }
        public ActivityApproachResult Reapproach(ActivityExecutorContext context,int current)
        { Observer?.Invoke("approach", context.World, context.Target, context.Worker); return _execution.Reapproach(context,current); }
        public Fix ReadRate(ActivityExecutorContext context)
        { Observer?.Invoke("rate", context.World, context.Target, context.Worker); return _execution.ReadRate(context); }
    }
    public sealed class IndependentRequirementGroupDef : Def, IWorkRequirementGroup
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private WorkRequirementGroupDeclaration _declaration;
        private IReadOnlyList<string> _declared;
        public WorkRequirementMode Mode => _declaration.Mode;
        public IReadOnlyList<WorkRequirementEntry> Entries => _declaration.Entries;
        public IReadOnlyList<string> Dependencies => _declared ?? WorkRequirementContracts.Dependencies(this);
        protected override void ReadCore(JsonReader reader)
        {
            if(reader.Value("dependencies") is JsonValue dependencies)
                _declared=Array.AsReadOnly(new JsonReader(Json.Parse("{\"ids\":"+dependencies.ToString()+"}"),reader.Where).Strings("ids"));
            _declaration=WorkRequirementGroupDeclaration.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public WorkRequirementResult Evaluate(WorkRequirementContext context) => context.Evaluate(this);
    }
    public sealed class IndependentTargetResourceDef : Def, IWorkSupplyRequirement, IWorkRequirementBindingValidator
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _resource.ContentDependencies;
        private ResourceRequirementDeclaration _resource;
        private EntityResourceSourceDeclaration _source;
        public Bill Quantities => _resource.Quantities;
        public IReadOnlyList<IResourceLotSelectionDefinition> Selections => _resource.Selections;
        public WorkResourceUse Use => _resource.Use;
        public IReadOnlyList<IResourceRate> Rates => _resource.Rates;
        public Code Port => _source.Port;
        public IReadOnlyList<string> Dependencies => WorkRequirementContracts.Dependencies(this);
        protected override void ReadCore(JsonReader reader)
        { _resource=ResourceRequirementDeclaration.Read(reader);_source=EntityResourceSourceDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _resource.Resolve(content);
        public WorkRequirementResult Evaluate(WorkRequirementContext context) => WorkRequirementResult.Satisfied();
        public void ValidateBinding(World world) => _source.ValidateBinding(world,this);
        public Code CopySources(WorkRequirementContext context,IList<WorkResourceSource> destination) => _source.CopySources(context,context.Target,this,destination);
        public WorkRequirementResult CheckSource(WorkRequirementContext context,ResourceEndpointId endpoint) => _source.CheckSource(context,context.Target,this,endpoint);
    }
    public sealed class IndependentWorkerGuardDef : Def, IWorkerWorkRequirement
    {
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        private string _role;
        private WorkerRoleId _selected;
        private Code _blocker;
        public IReadOnlyList<string> Dependencies => WorkRequirementContracts.Dependencies(this);
        protected override void ReadCore(JsonReader reader) { _role=reader.String("role");_blocker=new Code(reader.String("blocker")); }
        protected override void ResolveCore(ContentResolver content)
        { var role=content.Def<IWorkerRoleDefinition>(_role); _selected=role.Role; ContentDependencies=Array.AsReadOnly(new IContentDefinition[] { role }); }
        public WorkRequirementResult Evaluate(WorkRequirementContext context)
            => context.Worker!=null&&context.Role==_selected&&WorkerExecutionContracts.Can(context.World,context.Worker,_selected)
                ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(_blocker);
    }
    public class WorkContentContractModule : ActivityContractModule
    {
        public WorkContentContractModule(InitializeProbe initialize) : base(initialize) { }
        public bool IndependentWorkContent { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentWorkerRoleDef>("independent_worker_roles");
            types.Section<IndependentStaffedExecutorDef>("independent_staffed_executors");
            types.Section<IndependentRequirementGroupDef>("independent_requirement_groups");
            types.Section<IndependentTargetResourceDef>("independent_target_resources");
            types.Section<IndependentWorkerGuardDef>("independent_worker_guards");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var role=composer.Content.Get<IWorkerRoleDefinition>("contract.inspect");
            var executor=composer.Content.Get<IActivityExecutor>("contract.staffed");
            var requirement=composer.Content.Get<IWorkRequirement>("contract.inputs");
            var activity=composer.Content.Get<IAuthoredActivityDefinition>("contract.inspection");
            IndependentWorkContent=!(role is WorkerRoleDef)&&executor is IndependentStaffedExecutorDef&&!(requirement is WorkRequirementDef)
                &&composer.Content.Owns(role)&&composer.Content.Owns(executor)&&composer.Content.Owns(requirement)
                &&ReferenceEquals(activity.Graph.Find("inspect").Executor,executor)
                &&ReferenceEquals(activity.Graph.Find("inspect").Requirement,composer.Content.Get<IWorkRequirement>("contract.native-root"))
                &&ReferenceEquals(((IWorkRequirementGroup)composer.Content.Get<IWorkRequirement>("contract.native-root")).Entries[0].Requirement,requirement);
        }
    }
}
