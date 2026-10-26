using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole file authoring and portable intent for the original physical construction owner.</summary>
    public sealed class ConstructionDeclaration
    {
        private readonly ComponentDef _owner;
        private readonly IConstructionDefinition _definition;
        private ConstructionDeclaration(ComponentDef owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = owner as IConstructionDefinition ?? throw new ArgumentException("construction declaration requires its actual definition", nameof(owner));
        }
        public static ConstructionDeclaration Read(JsonReader reader, ComponentDef owner)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new ConstructionDeclaration(owner); declaration.ReadCore(reader); return declaration;
        }

        public bool AutomaticSupply => Workflow.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => Array.AsReadOnly(new IContentDefinition[] { TargetDefinition, SiteDefinition, (IContentDefinition)Workflow });
        private string _site, _activity, _removalActivity;
        /// <summary>The exact selected authored definition; binding never copies or recompiles its graph.</summary>
        public IActivityDefinition Workflow { get; private set; }
        public EntityDef TargetDefinition => _owner.Owner;
        public EntityDef SiteDefinition { get; private set; }
        public IEntityStateDefinition SiteState => States["site"];
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions => _demolish == null
            ? Array.AsReadOnly(new IActivityDefinition[] { _definition }) : Array.AsReadOnly(new IActivityDefinition[] { _definition, _demolish });
        private DemolishPlan _demolish;

        public string PlanKey => "build:" + _owner.Owner.Id;
        public IActivityDefinition BindExecution(World world, EntityId target) => _definition;
        public IActivityGraph Graph => Workflow.Graph;
        public string OnComplete => ConstructionService.FinishEffect;
        public IRemovalPlanDefinition Demolition => _demolish;
        public EntityStateBindings States { get; private set; }

        /// <summary>Conservative authored target-input envelope for site admission, never actual consumed stock.</summary>
        public Bill SupplyCapacity { get; private set; } = Bill.Empty;
        public Bill Salvage { get; private set; } = Bill.Empty;
        private BillSpec _salvage;

        private JsonValue PlacementArguments(JsonValue arguments)
            => EntityTargetPlanTemplates.PlacementArguments(arguments, PlanKey);
        public string IntentIdentity(World world, JsonValue arguments)
        {
            if (!(world.Space is ISpatialConstructionPlacement provider)) throw new ContentException("construction requires a placement provider");
            return provider.PlacementIdentity(provider.ReadPlacement(PlacementArguments(arguments)));
        }
        public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments)
        { _ = IntentIdentity(world, arguments); return Array.Empty<EntityId>(); }
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
        { _ = IntentIdentity(world, arguments); return Code.None; }
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
        { _ = IntentIdentity(world, arguments); return arguments.Clone(); }
        public string ExecutionIdentity(World world, Order order)
        {
            Entity site = world.Entities.Get(order.Target); IConstructionSite execution = site?.Get<IConstructionSite>();
            if (site == null || !ReferenceEquals(site.Def, SiteDefinition) || !ReferenceEquals(execution?.Target, _owner.Owner) ||
                execution.Order != order.Id || !(world.Space is ISpatialConstructionPlacement provider))
                throw new CorruptStateException("construction plan does not name its real authored site");
            return provider.PlacementIdentity(provider.CapturePlacement(site));
        }
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
            => world.Services.Get<IConstructionProjects>().CheckIntent(world, company, _owner.Owner, PlacementArguments(arguments));
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments)
            => world.Services.Get<IConstructionProjects>().CheckPlacement(world, company, _owner.Owner, PlacementArguments(arguments));
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
        {
            Entity site = world.Services.Get<IConstructionProjects>().PlaceSite(world, company, _owner.Owner, PlacementArguments(arguments), priority);
            return world.Services.Get<WorkBoard>().Order(site.Get<IConstructionSite>().Order);
        }
        private static ISpatialPlacementTemplates TemplateSpace(World world)
            => world.Space as ISpatialPlacementTemplates ?? throw new ContentException("selected space does not support portable placement templates");
        public void ValidateTemplate(World world, JsonValue arguments)
            => TemplateSpace(world).ValidateTemplatePlacement(PlacementArguments(arguments));
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor)
            => TemplateSpace(world).ReadPlacement(TemplateSpace(world).PlaceTemplatePlacement(PlacementArguments(arguments), anchor));
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
        {
            template = JsonValue.NewObject(); template.Set("placement", TemplateSpace(world).CaptureTemplatePlacement(PlacementArguments(arguments), origin)); return Code.None;
        }
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
        {
            intent = JsonValue.NewObject(); intent.Set("placement", TemplateSpace(world).PlaceTemplatePlacement(PlacementArguments(arguments), anchor)); return Code.None;
        }

        private void ReadCore(JsonReader r)
        {
            _site = r.String("site");
            if (string.IsNullOrWhiteSpace(_site)) throw r.Error("construction requires an explicit site");
            _activity = r.String("activity");
            if (string.IsNullOrWhiteSpace(_activity)) throw r.Error("construction requires an explicit activity");
            JsonReader d = r.Child("demolish");
            if (d != null)
            {
                _removalActivity = d.String("activity");
                if (string.IsNullOrWhiteSpace(_removalActivity)) throw d.Error("removal requires an explicit activity");
                if (!d.Has("salvage")) throw d.Error("removal requires an explicit salvage bill, empty when no yield is authored");
            }
            _salvage = d == null ? null : BillSpec.Read(d, "salvage");
            d?.End();
            States = EntityStateBindings.Read(r, "site");
        }

        public void Resolve(ContentResolver c)
        {
            States = States.Resolve(c);
            SiteDefinition = c.Def<EntityDef>(_site);
            Workflow = Binding(c, _activity);
            if (_removalActivity != null) _demolish = new DemolishPlan(_definition, Binding(c, _removalActivity));
            SupplyCapacity = WorkBoard.SupplyCapacity(Graph, c.Content);
            Salvage = _salvage == null ? Bill.Empty : _salvage.Resolve(c); _salvage = null;
        }

        private static IAuthoredActivityDefinition Binding(ContentResolver content, string id)
        {
            IAuthoredActivityDefinition workflow = content.Def<IAuthoredActivityDefinition>(id);
            if (workflow.PlayerOrderable || workflow.TargetTags.Count != 0 || workflow.OnComplete != null)
                throw content.Fail("bound construction activity must be non-orderable, unrestricted and have no standalone final effect: " + id);
            if (workflow.Graph == null || !workflow.Graph.HasMachineWork(null))
                throw content.Fail("bound construction activity requires authored physical machine work: " + id);
            return workflow;
        }

        public void ValidateOwner(EntityDef owner)
        {
            if (!ReferenceEquals(owner, _owner.Owner) || owner.Kind != EntityKind.Structure) throw new ContentException(owner.Id + ": only structures are constructed (vehicles are produced)");
        }

        private sealed class DemolishPlan : IRemovalPlanDefinition, IPlanTemplateDefinition
        {
            public bool AutomaticSupply => _workflow.AutomaticSupply;
            public IReadOnlyList<IContentDefinition> DisclosureContent => Array.AsReadOnly(new IContentDefinition[] { _def.TargetDefinition, (IContentDefinition)_workflow });
            private readonly IConstructionDefinition _def;
            private readonly IPlayerActivityDefinition _workflow;
            public IActivityDefinition Workflow => _workflow;
            public DemolishPlan(IConstructionDefinition def, IPlayerActivityDefinition workflow) { _def = def; _workflow = workflow; }
            public string PlanKey => "demolish:" + _def.TargetDefinition.Id;
            public int Priority => _workflow.Priority;
            public IActivityDefinition BindExecution(World world, EntityId target) => this;
            public IActivityGraph Graph => _workflow.Graph;
            public string OnComplete => ConstructionService.DemolishEffect;
            public void ValidateTemplate(World world, JsonValue arguments)
            {
                ValidateTargetDefinition(null);
                EntityTargetPlanTemplates.Validate(world, arguments, PlanKey);
            }
            public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor)
            {
                ValidateTemplate(world, arguments);
                EntityTargetPlanTemplates.ValidateAnchor(world, arguments, anchor, PlanKey);
            }
            public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
                => EntityTargetPlanTemplates.Capture(world, company, _def.TargetDefinition, Graph, PlanKey, arguments, origin, out template);
            public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
            {
                ValidateTemplate(world, arguments);
                return EntityTargetPlanTemplates.Place(world, company, _def.TargetDefinition, PlanKey, arguments, anchor, out intent);
            }
            public void ValidateTargetDefinition(EntityDef target)
            {
                if (Graph.Nodes.Count == 0 || target != null && !ReferenceEquals(target, _def.TargetDefinition))
                    throw new ContentException(PlanKey + ": removal requires an authored compatible target");
            }
            public PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority)
            {
                ValidateTargetDefinition(null);
                var selection = new ActivityWorkerSelection(workers?.Bindings ?? throw new ArgumentNullException(nameof(workers)));
                selection.ValidateGraph(Graph);
                return new PlanIntent(PlanKey, ActivityWorkerSelection.CreateArguments(target, selection), group, priority, false);
            }
            public string IntentIdentity(World world, JsonValue arguments)
            { var selection = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target); return selection.Identity(target); }
            public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => PlanEntityReferenceContracts.Read(this, arguments);
            public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
                => PlanEntityReferenceContracts.Check(this, world, company, arguments, source, destination);
            public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
                => PlanEntityReferenceContracts.Replace(this, arguments, source, destination);
            public string ExecutionIdentity(World world, Order order) => order.WorkerSelection.Identity(order.Target);
            public Code CheckIntent(World world, PlayerId company, Entity target)
            {
                if (Graph.Nodes.Count == 0) return ConstructionService.NotConstructible;
                if (!ReferenceEquals(target.Def, _def.TargetDefinition)) return StandardCodes.WrongTarget;
                return world.Services.Get<IContentAccess>().Check(world, company, _def.TargetDefinition);
            }
            public Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
            {
                var selection = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
                Code check = world.Services.Get<WorkBoard>().CheckPlayerActivity(company, PlanKey, target, 0);
                return check.IsNone ? selection.Check(world, company, Graph) : check;
            }
            public Code CheckActivation(World world, PlayerId company, JsonValue arguments)
            {
                Code check = CheckSubmission(world, company, arguments); if (!check.IsNone) return check;
                ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
                return world.Services.Get<IConstructionProjects>().CheckDemolition(world, world.Entities.Get(target));
            }
            public Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
            {
                Code check = CheckActivation(world, company, arguments);
                if (!check.IsNone) throw new InvalidOperationException("removal plan admission changed: " + check);
                var selection = ActivityWorkerSelection.Read(arguments, Graph, PlanKey + ".intent", out EntityId target);
                return world.Services.Get<WorkBoard>().Post(this, company, target, priority, selection);
            }
        }
    }

    /// <summary>Sole strict target declaration for reusable construction sites.</summary>
    public sealed class ConstructionSiteDeclaration
    {
        private string _target;
        private ConstructionSiteDeclaration() { }
        public EntityDef Target { get; private set; }
        public IEntityStateDefinition ConstructionState => Target.Component<IConstructionDefinition>().SiteState;
        public static ConstructionSiteDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            string target = reader.String("target");
            if (string.IsNullOrWhiteSpace(target)) throw reader.Error("site requires an explicit target");
            return new ConstructionSiteDeclaration { _target = target };
        }
        public void Resolve(ContentResolver content)
        {
            Target = content.Def<EntityDef>(_target);
            if (Target.Component<IConstructionDefinition>() == null) throw content.Fail("site target requires its authored construction definition");
        }
    }
}

