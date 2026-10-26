using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>An engine-side mod binding, authored through JSON and the existing activity/intents.</summary>
    public sealed class DeclaredPlanDef : Def, IPlayerActivityDefinition, IEntityTargetPlanDefinition, IActivityWorkflowBinding
    {
        private string _activity;
        private ActivityDef _workflow;
        public string PlanKey { get; private set; }
        public IActivityDefinition Workflow => _workflow;
        public IActivityGraph Graph => _workflow.Graph;
        public bool AutomaticSupply => _workflow.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => Array.AsReadOnly(new IContentDefinition[] { this, _workflow });
        public string OnComplete => _workflow.OnComplete;
        public int Priority => _workflow.Priority;
        protected override void ReadCore(JsonReader reader)
        {
            PlanKey = reader.String("plan_key"); _activity = reader.String("activity");
            if (string.IsNullOrWhiteSpace(PlanKey)) throw reader.Error("plan_key requires explicit stable identity");
        }
        protected override void ResolveCore(ContentResolver content) => _workflow = content.Def<ActivityDef>(_activity);
        public IActivityDefinition BindExecution(World world, EntityId target) => this;
        public Code CheckIntent(World world, PlayerId company, Entity target) => _workflow.CheckIntent(world, company, target);
        public string IntentIdentity(World world, JsonValue arguments)
        {
            var workers = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
            return workers.Identity(target);
        }
        public string ExecutionIdentity(World world, Order order) => order.WorkerSelection.Identity(order.Target);
        public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => PlanEntityReferenceContracts.Read(this, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
            => PlanEntityReferenceContracts.Check(this, world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
            => PlanEntityReferenceContracts.Replace(this, arguments, source, destination);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
        {
            var workers = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
            Code check = world.Services.Get<WorkBoard>().CheckPlayerActivity(company, PlanKey, target, 0);
            return check.IsNone ? workers.Check(world, company, Graph) : check;
        }
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments) => CheckSubmission(world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
        {
            Code check = CheckActivation(world, company, arguments);
            if (!check.IsNone) throw new InvalidOperationException("declared plan refused: " + check);
            var workers = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
            return world.Services.Get<WorkBoard>().Post(this, company, target, priority, workers);
        }
        public void ValidateTargetDefinition(EntityDef target) => _workflow.ValidateTargetDefinition(target);
        public PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority)
        {
            var selected = new ActivityWorkerSelection(workers?.Bindings ?? throw new ArgumentNullException(nameof(workers)));
            selected.ValidateGraph(Graph);
            return new PlanIntent(PlanKey, ActivityWorkerSelection.CreateArguments(target, selected), group, priority, false);
        }
    }

    public interface IDeclaredPlanSource { }
    public sealed class DeclaredPlanSource : IActivitySource, IDeclaredPlanSource
    {
        private readonly IReadOnlyList<IActivityDefinition> _plans;
        public int Reads { get; private set; }
        public DeclaredPlanSource(ResolvedContent content)
        {
            var plans = new List<IActivityDefinition>();
            foreach (DeclaredPlanDef definition in content.All<DeclaredPlanDef>()) plans.Add(definition);
            _plans = plans.AsReadOnly();
        }
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions { get { Reads++; return _plans; } }
    }

    public sealed class DeclaredPlanModule : GameModule
    {
        public const string ModuleId = "proof.declared-plans";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<DeclaredPlanDef>("declared_plans");
        public override void Compose(WorldComposer composer)
        {
            var source = new DeclaredPlanSource(composer.Content);
            composer.Services.Bind(source); composer.Services.Bind<IDeclaredPlanSource>(source);
        }
    }
}

