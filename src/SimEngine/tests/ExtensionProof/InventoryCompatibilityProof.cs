using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public enum CompatibilityFault { None, Throw, Clock, Target, EntityInventoryClaim }
    public sealed class CompatibilityPortDef : ComponentDef<CompatibilityPort>, IResourcePortDefinition
    {
        public bool BindsEntityInventory => _fault == CompatibilityFault.EntityInventoryClaim;
        private string _pool;
        public EndpointPoolDef Pool { get; private set; }
        public Code CapabilityId { get; private set; }
        public ICapacityPolicy CapacityPolicy => Pool.Policy;
        // Negative-only callback instrumentation, never stock/clock/gameplay ownership.
        private World _faultWorld;
        private Entity _faultTarget;
        private CompatibilityFault _fault;
        public int Observations { get; private set; }
        internal void Arm(World world, Entity target, CompatibilityFault fault) { _faultWorld = world; _faultTarget = target; _fault = fault; }
        protected override void ReadCore(JsonReader reader) { CapabilityId = new Code(reader.String("id")); _pool = reader.String("pool"); }
        protected override void ResolveCore(ContentResolver content) => Pool = content.Def<EndpointPoolDef>(_pool);
        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            Observations++;
            if (_fault == CompatibilityFault.Throw) throw new InvalidOperationException("proof.compatibility-first-error");
            if (_fault == CompatibilityFault.Clock) ((GameClock)_faultWorld.Clock).Pause();
            if (_fault == CompatibilityFault.Target) _faultTarget.Get<IInventory>().Take(content.ResourceId("ports.ore"), Fix.One);
            return Pool.Accepts(resource);
        }
    }
    public sealed class CompatibilityPort : Component<CompatibilityPortDef>, IResourcePort
    {
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
        {
            var pools = world.Services.Get<EndpointPools>(); var endpoint = new ResourceEndpointId(pools.EndpointScope, Def.Pool.Identity);
            return original.HasValue && original.Value != endpoint ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable)
                : ResourcePortResolution.ReadyStore(pools.Get(Def.Pool.Identity));
        }
    }
    public sealed class CompatibilityVisibilityDef : Def
    {
        private string _hidden;
        public IResourceContentDefinition Hidden { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue value = reader.Value("hidden_resource");
            if (value == null || value.Kind != JsonKind.Null && value.Kind != JsonKind.String) throw reader.Error("explicit nullable hidden_resource required");
            _hidden = value.Kind == JsonKind.Null ? null : value.Text;
        }
        protected override void ResolveCore(ContentResolver content) { if (_hidden != null) Hidden = content.Def<IResourceContentDefinition>(_hidden); }
    }
    public sealed class InventoryCompatibilityProofModule : GameModule
    {
        public override string Id => "proof.inventory-compatibility";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("proof.output-ports", Version) };
        private World _world;
        public override void DeclareContent(ContentTypes types)
        { types.Component<CompatibilityPortDef>("compatibility_port", 1); types.Section<CompatibilityVisibilityDef>("compatibility_visibility"); }
        public override void Compose(WorldComposer composer)
        { _world = composer.World; composer.Services.Bind<IVisibility>(new Visibility(composer.Content.Get<CompatibilityVisibilityDef>("compat.visibility"))); }
        private CompatibilityPortDef Metadata(string definition)
            => _world.Content.Get<EntityDef>(definition).Component<CompatibilityPortDef>(new Code("probe"));
        public int Observations(string definition) => Metadata(definition).Observations;
        public void Arm(string definition, CompatibilityFault fault)
        {
            Entity target = null; foreach (Entity entity in _world.Entities.All) if (entity.Def.Id == definition) { target = entity; break; }
            Metadata(definition).Arm(_world, target, fault);
        }
        private sealed class Visibility : IVisibility
        {
            private readonly CompatibilityVisibilityDef _definition;
            internal Visibility(CompatibilityVisibilityDef definition) { _definition = definition; }
            public bool CanSee(World world, PlayerId viewer, Entity entity) => entity != null;
            public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
                => viewer == PlayerId.None || !ReferenceEquals(definition, _definition.Hidden);
        }
    }
}
