using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public class IndependentActivityContentDef : Def, IAuthoredActivityDefinition
    {
        private ActivityDeclaration _declaration;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => System.Array.AsReadOnly(new IContentDefinition[] { this });
        public bool PlayerOrderable => _declaration.PlayerOrderable;
        public IReadOnlyList<string> TargetTags => _declaration.TargetTags;
        public int Priority => _declaration.Priority;
        public string OnComplete => _declaration.OnComplete;
        public IActivityGraph Graph => _declaration.Graph;
        public string PlanKey => "activity:" + Id;
        protected override void ReadCore(JsonReader reader) => _declaration = ActivityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content, this);
        public bool Targets(EntityDef definition) => ActivityDefinitionContracts.Targets(this, definition);
        public virtual IActivityDefinition BindExecution(World world, EntityId target) => this;
        public virtual Code CheckIntent(World world, PlayerId company, Entity target) => ActivityDefinitionContracts.CheckIntent(this, world, company, target);
        public virtual string IntentIdentity(World world, JsonValue arguments) => ActivityDefinitionContracts.IntentIdentity(this, arguments);
        public virtual string ExecutionIdentity(World world, Order order) => order.WorkerSelection.Identity(order.Target);
        public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => PlanEntityReferenceContracts.Read(this, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
            => PlanEntityReferenceContracts.Check(this, world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
            => PlanEntityReferenceContracts.Replace(this, arguments, source, destination);
        public void ValidateTargetDefinition(EntityDef target) => ActivityDefinitionContracts.ValidateTargetDefinition(this, target);
        public PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority)
            => ActivityDefinitionContracts.CreateTargetIntent(this, target, workers, group, priority);
        public virtual Code CheckSubmission(World world, PlayerId company, JsonValue arguments) => ActivityDefinitionContracts.CheckSubmission(this, world, company, arguments);
        public virtual Code CheckActivation(World world, PlayerId company, JsonValue arguments) => ActivityDefinitionContracts.CheckActivation(this, world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority) => ActivityDefinitionContracts.Activate(this, world, company, arguments, priority);
    }
    public class ActivityContractModule : IncidentContractModule
    {
        public ActivityContractModule(InitializeProbe initialize) : base(initialize) { }
        public bool IndependentActivityContent { get; private set; }
        public bool ConstructionAlias { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types); types.Section<IndependentActivityContentDef>("independent_activities");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            IndependentActivityContent = true;
            foreach (string id in new[]{"contract.inspection","pressure.response","point.tower.build-work"})
            {
                var definition = composer.Content.Get<IAuthoredActivityDefinition>(id);
                IndependentActivityContent &= !(definition is ActivityDef) && composer.Content.Owns(definition);
            }
            ConstructionAlias = ReferenceEquals(composer.Content.Get<EntityDef>("point.tower").Component<IConstructionDefinition>().Workflow,
                composer.Content.Get<IAuthoredActivityDefinition>("point.tower.build-work"));
        }
    }
}
