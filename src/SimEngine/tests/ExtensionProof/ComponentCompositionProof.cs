using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side metadata admission only. All normal execution stays in existing engine systems.
    public sealed class CompositionRequirementsDef : ComponentDef<CompositionRequirementsPart>, IComponentCompositionValidation
    {
        private string _space;
        public ISpatialSpaceDefinition RequiredSpace { get; private set; }
        public IReadOnlyList<string> Requirements { get; private set; }
        public IReadOnlyList<string> RequiredServices { get; private set; }
        public string Fault { get; private set; }
        public int Calls; // Diagnostic instrumentation only; never simulation state.
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space");
            var requirements = reader.Strings("requires");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string requirement in requirements)
                if (!unique.Add(requirement) || requirement != "layers" && requirement != "placement"
                    && requirement != "resources" && requirement != "networks" && requirement != "construction"
                    && requirement != "templates" && requirement != "recovery" && requirement != "rollout"
                    && requirement != "interaction" && requirement != "overlap" && requirement != "visibility-invalidation"
                    && requirement != "line-geometry" && requirement != "line-removal" && requirement != "network-graph"
                    && requirement != "tile-routing")
                    throw reader.Error("explicit unique supported component prerequisite required");
            Requirements = Array.AsReadOnly(requirements);
            var services=reader.Strings("required_services");var serviceIds=new HashSet<string>(StringComparer.Ordinal);
            if(reader.Raw.Get("required_services")?.Kind!=JsonKind.Array)throw reader.Error("explicit required_services array required");
            foreach(string service in services)if(!serviceIds.Add(service)||service!="spatial-space"&&service!="line-geometry"&&service!="line-removal"&&service!="network-graph"&&service!="networks")
                throw reader.Error("unique typed declared spatial service required");
            RequiredServices=Array.AsReadOnly(services);
            Fault = reader.String("fault");
            if (Fault != "none" && Fault != "throw" && Fault != "pause" && Fault != "geometry")
                throw reader.Error("explicit supported fault instrumentation required");
        }
        protected override void ResolveCore(ContentResolver content)
            => RequiredSpace = content.Def<ISpatialSpaceDefinition>(_space);
        public void ValidateComposition(World world, EntityDef entity)
        {
            Calls++;
            if (!ReferenceEquals(Owner, entity) || !IsFrozen || !entity.IsFrozen
                || !ReferenceEquals(entity.Component<CompositionRequirementsDef>(), this))
                throw new ContentException("component prerequisite requires actual frozen metadata");
            if (!ReferenceEquals(world.Content.Get<ISpatialSpaceDefinition>(RequiredSpace.Id), RequiredSpace)
                || world.Space == null || world.Space.Id != RequiredSpace.Id)
                throw new ContentException(entity.Id + ": required selected space is missing or different");
            foreach (string requirement in Requirements)
            {
                bool available;
                switch(requirement)
                {
                    case "layers":available=world.Space is ISpatialLayerProvider;break;
                    case "placement":available=world.Space is ISpatialPlacementIntent;break;
                    case "construction":available=world.Space is ISpatialConstructionPlacement;break;
                    case "templates":available=world.Space is ISpatialPlacementTemplates;break;
                    case "recovery":available=world.Space is ISpatialRecoveryPlacement;break;
                    case "rollout":available=world.Space is ISpatialRollout;break;
                    case "interaction":available=world.Space is ISpatialInteraction;break;
                    case "overlap":available=world.Space is ISpatialOverlap;break;
                    case "visibility-invalidation":available=world.Space is ISpatialVisibilityInvalidation;break;
                    case "line-geometry":available=world.Services.Has<ILineGeometry>();break;
                    case "line-removal":available=world.Services.Has<ILineRemovalGeometry>();break;
                    case "network-graph":available=world.Services.Has<INetworkGraphGeometry>();break;
                    case "tile-routing":available=world.Services.Has<ITileLineRouting>();break;
                    case "resources":available=world.Services.Has<IResourceTransaction>();break;
                    case "networks":available=world.Services.Has<INetworkTopology>();break;
                    default:throw new ContentException("unknown frozen composition requirement");
                }
                if (!available) throw new ContentException(entity.Id + ": missing required contract " + requirement);
            }
            if (Fault == "throw") throw new InvalidOperationException("authored component composition callback fault");
            if (Fault == "pause") ((Hosting.GameClock)world.Clock).Pause();
            if (Fault == "geometry") ((PointSpace)world.Space).SetAccess(false);
        }
        internal void DeclareServices(WorldComposer composer)
        {
            foreach(string service in RequiredServices)switch(service)
            {
                case "spatial-space":composer.Services.Require<ISpatialSpace>();break;
                case "line-geometry":composer.Services.Require<ILineGeometry>();break;
                case "line-removal":composer.Services.Require<ILineRemovalGeometry>();break;
                case "network-graph":composer.Services.Require<INetworkGraphGeometry>();break;
                case "networks":composer.Services.Require<INetworkTopology>();break;
                default:throw new ContentException("unknown frozen spatial service requirement");
            }
        }
    }
    public sealed class CompositionRequirementsPart : Component<CompositionRequirementsDef> { }

    public sealed class CompositionAuditDef : ComponentDef<CompositionAuditPart>, IComponentCompositionValidation
    {
        public int Calls;
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateComposition(World world, EntityDef entity)
        {
            Calls++;
            if (!ReferenceEquals(entity.Component<CompositionAuditDef>(), this) || !IsFrozen
                || !ReferenceEquals(world.Content.Get<EntityDef>(entity.Id), entity))
                throw new ContentException("second component callback requires its actual catalog owner");
        }
    }
    public sealed class CompositionAuditPart : Component<CompositionAuditDef> { }

    // Negative missing-capability provider only. No normal gameplay, geometry, layer or stock fallback.
    public sealed class BareCompositionSpaceDef : Def, ISpatialSpaceDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        public void Compose(WorldComposer composer) => composer.Services.Bind<ISpatialSpace>(new BareCompositionSpace(Id));
    }
    public sealed class BareCompositionSpace : ISpatialSpace
    {
        public BareCompositionSpace(string id) { Id = id; }
        public string Id { get; }
        public int ObservationVersion => 0; // Immutable negative composition surface, no gameplay state.
        public int StateSchemaVersion => 1;
        public void Sync(IStateSync sync) { }
        public void ValidateComposition(World world) { }
        public void OnSpawn(Entity entity) { }
        public void OnDespawn(Entity entity) { }
        public void OnLoaded(World world) { }
        public SpatialWorldSnapshot Capture(PlayerId viewer) => throw new NotSupportedException("negative composition probe is not gameplay geometry");
    }
    public sealed class ComponentCompositionModule : GameModule
    {
        private readonly bool _native;
        public ComponentCompositionModule(bool native = false) { _native = native; }
        public override string Id => "proof.component-composition";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => _native
            ? new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) } : Array.Empty<ModuleRequirement>();
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.component-composition.initialize");
        public CompositionRequirementsDef Requirements { get; private set; }
        public CompositionAuditDef Audit { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<CompositionRequirementsDef>("composition_requirements", 1);
            types.Component<CompositionAuditDef>("composition_audit", 1);
            types.Section<BareCompositionSpaceDef>("bare_composition_spaces");
            types.Section<AbsentSpatialSpaceDef>("absent_composition_spaces");
        }
        public override void Compose(WorldComposer composer)
        {
            var prototype = composer.Content.Get<EntityDef>("composition.unused");
            Requirements = prototype.Component<CompositionRequirementsDef>();
            Audit = prototype.Component<CompositionAuditDef>();
            Requirements.DeclareServices(composer);
            composer.AddSystem(Initialize, 303);
        }
    }
}
