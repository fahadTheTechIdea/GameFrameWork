using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    // Authored internal vessels alias the existing pool provider/book. No process, yield or timer logic.
    public sealed class PoolPortDef : ComponentDef<PoolPort>, IResourcePortSelectionDefinition
    {
        public bool BindsEntityInventory => false;
        private string _pool;
        private string[] _alternatives;
        public EndpointPoolDef Pool { get; private set; }
        public IReadOnlyList<EndpointPoolDef> Alternatives { get; private set; }
        public IReadOnlyList<ResourceEndpointId> Choices { get; private set; }
        public ResourceEndpointId InitialEndpoint => new ResourceEndpointId(EndpointScope, Pool.Identity);
        public Code CapabilityId { get; private set; }
        public string EndpointScope { get; private set; }
        public bool RetainsOriginalAccess { get; private set; }
        public ICapacityPolicy CapacityPolicy => Pool.Policy;
        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            if (!Pool.Accepts(resource)) return false;
            foreach (var pool in Alternatives) if (!pool.Accepts(resource)) return false;
            return true;
        }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); _pool = reader.String("pool"); _alternatives = reader.Strings("alternatives");
            EndpointScope = reader.String("endpoint_scope");
            string access = reader.String("original_access");
            if (access != "retained" && access != "current_only") throw reader.Error("explicit original_access retained/current_only required");
            RetainsOriginalAccess = access == "retained";
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Pool = content.Def<EndpointPoolDef>(_pool); var alternatives = new List<EndpointPoolDef>();
            foreach (string id in _alternatives)
            {
                var alternative = content.Def<EndpointPoolDef>(id);
                if (alternative.Policy.Id != Pool.Policy.Id || alternative.Identity == Pool.Identity || alternatives.Contains(alternative))
                    throw content.Fail("explicit alternative ports require unique same-policy pool references");
                alternatives.Add(alternative);
            }
            Alternatives = alternatives.AsReadOnly();
            var choices=new List<ResourceEndpointId>{new ResourceEndpointId(EndpointScope,Pool.Identity)};
            foreach(var alternative in Alternatives)choices.Add(new ResourceEndpointId(EndpointScope,alternative.Identity));
            Choices=choices.AsReadOnly();
        }
        public bool Allows(long identity)
        {
            if (Pool.Identity == identity) return true;
            foreach (var pool in Alternatives) if (pool.Identity == identity) return true;
            return false;
        }
    }
    public sealed class PoolPort : Component<PoolPortDef>, IResourcePortSelection
    {
        public Code CapabilityId => Def.CapabilityId;
        private long _selected;
        // Transient negative-protocol instrumentation only; no second saved availability/stock authority.
        public Func<World, ResourceEndpointId?, ResourcePortResolution?> ResolveProbe;
        public ResourceEndpointId SelectedEndpoint=>new ResourceEndpointId(Def.EndpointScope,_selected);
        public override void OnCreate() => _selected = Def.InitialEndpoint.Identity;
        public override void Sync(IStateSync sync)
        { sync.Long(ref _selected); if (!Def.Allows(_selected)) throw new CorruptStateException("unknown selected authored port pool"); }
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
        {
            var probe = ResolveProbe?.Invoke(world, original);
            if (probe.HasValue) return probe.Value;
            var pools = world.Services.Get<EndpointPools>();
            if (original.HasValue && (original.Value.Scope != pools.EndpointScope || !Def.Allows(original.Value.Identity))) return ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable);
            if (original.HasValue && !Def.RetainsOriginalAccess && original.Value.Identity != _selected) return ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable);
            return ResourcePortResolution.ReadyStore((IResourceOutlet)pools.Resolve(
                original ?? new ResourceEndpointId(pools.EndpointScope, _selected)));
        }
        public IResourceOutlet ResolveChoice(World world,ResourceEndpointId endpoint)
        {
            var pools=world.Services.Get<EndpointPools>();
            return endpoint.Scope==pools.EndpointScope&&Def.Allows(endpoint.Identity)?pools.Get(endpoint.Identity):null;
        }
        public void Select(World world,ResourceEndpointId endpoint) { _selected=endpoint.Identity;Entity.Touch(); }
    }
    public sealed class OutputPortProofModule : GameModule
    {
        public override string Id => "proof.output-ports";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement("proof.pools", Version),
            new ModuleRequirement(TransformationModule.ModuleId, Version),
            new ModuleRequirement(WorldSetupModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<PoolPortDef>("pool_port",4);
            types.Component<ReceivingOnlyDef>("receiving_only",1);
            types.Component<PhysicalAvailabilityProbeDef>("physical_availability_probe",1);
            types.Section<SelectionFaultProfileDef>("selection_fault_profiles");
            types.Component<SelectionFaultPortDef>("selection_fault_port",1);
        }
    }
    public static class OutputPortGame
    {
        public static GameFactory Factory(Action<JsonValue> edit = null, string document = "output-ports.json", string manifest = "output-ports-master.json", Action<JsonValue> editShared = null)
        {
            var definition = new GameDefinition("output-ports", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new ResourceFlowsModule(), new OperationsModule(),
                new TransformationModule(), new WorldSetupModule(), new EndpointProofModule(), new OutputPortProofModule() });
            var content = Json.Parse(FrameworkProofContent.Read(document)); edit?.Invoke(content);
            var shared = Json.Parse(FrameworkProofContent.Read("resource-port-common.json")); editShared?.Invoke(shared);
            var files = new PreloadedFileSource()
                .Add("manifest.json", FrameworkProofContent.Read(manifest))
                .Add("resource-port-common.json", shared.ToString())
                .Add("open-access.json", FrameworkProofContent.Access)
                .Add("endpoints-flows.json", FrameworkProofContent.Read("endpoints-flows.json"))
                .Add(document, content.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
}
