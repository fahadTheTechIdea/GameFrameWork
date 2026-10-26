using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent registrations delegate the sole engine declarations; no gameplay scheduler or state.
    public sealed class IndependentConstructionDef : ComponentDef<Constructible>, IConstructionDefinition, IPlanTemplateDefinition
    {
        private ConstructionDeclaration _declaration;
        private string _fault;
        internal Action<string> TemplateObservation; // Explicit negative callback injection only.
        public int Resolutions { get; private set; }
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => _declaration.DisclosureContent;
        public IActivityDefinition Workflow => _declaration.Workflow;
        public EntityDef TargetDefinition => _declaration.TargetDefinition;
        public EntityDef SiteDefinition => _declaration.SiteDefinition;
        public IEntityStateDefinition SiteState => _fault == "null-state" ? null : _declaration.SiteState;
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions => _declaration.ActivityDefinitions;
        public string PlanKey => _declaration.PlanKey;
        public IActivityGraph Graph => _declaration.Graph;
        public string OnComplete => _declaration.OnComplete;
        public IRemovalPlanDefinition Demolition => _declaration.Demolition;
        public EntityStateBindings States => _declaration.States;
        public Bill SupplyCapacity => _declaration.SupplyCapacity;
        public Bill Salvage => _declaration.Salvage;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.Has("metadata_fault") ? reader.String("metadata_fault") : null; _declaration = ConstructionDeclaration.Read(reader, this); }
        protected override void ResolveCore(ContentResolver content) { _declaration.Resolve(content); Resolutions++; }
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public IActivityDefinition BindExecution(World world, EntityId target) => _declaration.BindExecution(world, target);
        public string IntentIdentity(World world, JsonValue arguments) => _declaration.IntentIdentity(world, arguments);
        public System.Collections.Generic.IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => _declaration.ReadEntityReferences(world, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination) => _declaration.CheckEntityReplacement(world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination) => _declaration.ReplaceEntityReference(world, arguments, source, destination);
        public string ExecutionIdentity(World world, Order order) => _declaration.ExecutionIdentity(world, order);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments) => _declaration.CheckSubmission(world, company, arguments);
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments)
        { TemplateObservation?.Invoke("activation"); return _declaration.CheckActivation(world, company, arguments); }
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority) => _declaration.Activate(world, company, arguments, priority);
        public void ValidateTemplate(World world, JsonValue arguments)
        { TemplateObservation?.Invoke("validate"); _declaration.ValidateTemplate(world, arguments); }
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor)
        { TemplateObservation?.Invoke("anchor"); _declaration.ValidateTemplateAnchor(world, arguments, anchor); }
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
        { TemplateObservation?.Invoke("capture"); return _declaration.CaptureTemplate(world, company, arguments, origin, out template); }
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
        { TemplateObservation?.Invoke("place"); return _declaration.PlaceTemplate(world, company, arguments, anchor, out intent); }
    }


    public class IndependentConstructionSiteDef : ComponentDef<Site>, IConstructionSiteDefinition
    {
        private ConstructionSiteDeclaration _declaration;
        internal Action ObserveState;
        public string Fault { get; private set; }
        public EntityDef Target => Fault == "null-target" ? null : _declaration.Target;
        public IEntityStateDefinition ConstructionState
        { get { ObserveState?.Invoke(); return _declaration.ConstructionState; } }
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.Has("metadata_fault") ? reader.String("metadata_fault") : null; _declaration = ConstructionSiteDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class WrongConstructionSiteCodecDef : IndependentConstructionSiteDef { }
    public sealed class UnboundConstructionSiteDef : ComponentDef<Site>
    {
        private ConstructionSiteDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = ConstructionSiteDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ConstructionContentContractsProofModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        private World _world;
        private readonly string _geometryPhase, _geometryFault;
        public ConstructionContentContractsProofModule(InitializeProbe initialize, string geometryPhase = null, string geometryFault = null)
        { _initialize = initialize; _geometryPhase = geometryPhase; _geometryFault = geometryFault; }
        public override string Id => "proof.construction-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ConstructionModule.ModuleId, Version) };
        public bool OneOwner { get; private set; }
        public bool ActualBindings { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentConstructionDef>("independent_construction", 1);
            types.Component<IndependentConstructionSiteDef>("independent_site", 1);
            types.Component<WrongConstructionSiteCodecDef>("wrong_site_codec", 2);
            types.Component<UnboundConstructionSiteDef>("unbound_site", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            OneOwner = ReferenceEquals(composer.Services.Get<IConstructionProjects>(), composer.Services.Get<IResourceRecovery>())
                && !composer.Services.Has<ConstructionService>();
            var target = composer.Content.Get<EntityDef>("point.tower");
            var construction = target.Component<IConstructionDefinition>();
            var site = construction.SiteDefinition.Component<IConstructionSiteDefinition>();
            ActualBindings = ReferenceEquals(construction.TargetDefinition, target)
                && ReferenceEquals(site.Target, target) && ReferenceEquals(site.ConstructionState, construction.SiteState)
                && ReferenceEquals(construction.Graph, construction.Workflow.Graph)
                && ReferenceEquals(construction.ActivityDefinitions[0], construction)
                && construction is IPlanTemplateDefinition && site.ComponentType == typeof(Site)
                && construction.ComponentType == typeof(Constructible);
            if (construction is IndependentConstructionDef independent) ActualBindings &= independent.Resolutions == 1;
            if (_geometryPhase != null) ArmGeometryFault(_geometryPhase, _geometryFault);
            composer.AddSystem(_initialize, 303);
        }
        // Negative instrumentation of the actual selected owner, never another geometry implementation.
        public void ArmGeometryFault(string phase, string fault)
        {
            var space = (PointSpace)_world.Space;
            space.GeometryObservationFault = (called, entity) => {
                if (called != phase) return;
                if (fault == "state" || fault == "state-throw") space.SetAccess(!space.AccessOpen);
                if (fault == "clock" || fault == "clock-throw") ((GameClock)_world.Clock).Pause();
                if (fault == "throw" || fault == "state-throw" || fault == "clock-throw")
                    throw new InvalidOperationException("proof.geometry-first-error");
            };
        }
        // Explicitly armed negative fault injection only, after an ordinary command creates the site.
        public void ArmTemplateFault(string operation, string fault, PlanTemplate template, JsonValue anchor)
        {
            var selected = (IndependentConstructionDef)_world.Content.Get<EntityDef>("point.tower").Component<IConstructionDefinition>();
            selected.TemplateObservation = called => {
                if (called != operation) return;
                if (fault == "content") throw new ContentException("proof.template.content");
                if (fault == "throw") throw new InvalidOperationException("proof.template.first");
                if (fault == "pause") ((GameClock)_world.Clock).Pause();
                if (fault == "identity") _world.Services.Get<IPlanBoard>().SubmitPlan(new PlayerId(0), new PlanIntent("build:point.tower",
                    Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":90}}"), "negative.identity", 1, true));
                if (fault == "reenter" || fault == "swallowed")
                    try { _world.Services.Get<IPlanBoard>().PreviewPlanTemplateStructure(template, anchor); }
                    catch (InvalidOperationException) when (fault == "swallowed") { }
            };
        }
        public void ClearTemplateFault()
            => ((IndependentConstructionDef)_world.Content.Get<EntityDef>("point.tower").Component<IConstructionDefinition>()).TemplateObservation = null;
        public void ArmFault(string fault)
        {
            Entity site = null;
            foreach (var entity in _world.Entities.All) if (entity.Def.Id == "point.tower-site") { site = entity; break; }
            if (site == null) throw new InvalidOperationException("negative probe requires an actual site");
            var definition = (IndependentConstructionSiteDef)site.Def.Component<IConstructionSiteDefinition>();
            definition.ObserveState = () => {
                if (fault == "throw") throw new InvalidOperationException("proof.construction.site-state");
                if (fault == "pause") ((GameClock)_world.Clock).Pause();
                if (fault == "order") site.Get<IConstructionSite>().Order++;
                if (fault == "touch") site.Touch();
                if (fault == "reenter") ((IInspectable)site.Get<IConstructionSite>()).Inspect(_world, site.Owner, new List<Facet>());
            };
        }
    }
}
