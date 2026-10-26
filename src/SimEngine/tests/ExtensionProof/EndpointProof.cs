using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // A normal engine module using public contracts only. No host-side stock or transfer rules.
    public sealed class EndpointPoolDef : Def
    {
        private string _policy;
        private string[] _resources;
        private readonly HashSet<ResourceId> _accepted = new HashSet<ResourceId>();
        public long Identity { get; private set; }
        public PlayerId Owner { get; private set; }
        public bool Public { get; private set; }
        public bool Evacuate { get; private set; }
        public ICapacityPolicy Policy { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Identity = reader.Long("identity"); Owner = new PlayerId(reader.Int("owner")); Public = reader.Bool("public");
            Evacuate = reader.Bool("evacuate");
            _policy = reader.String("capacity_policy"); _resources = reader.Strings("resources");
            if (Identity <= 0 || Owner != PlayerId.None && !Owner.IsValid || _resources.Length == 0) throw reader.Error("pool identity, owner and resources must be authored");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Policy = content.Def<ICapacityPolicyDefinition>(_policy);
            foreach (string resource in _resources)
            {
                ResourceId id = content.Def<IResourceContentDefinition>(resource).Resource;
                if (!_accepted.Add(id)) throw content.Fail("duplicate pool resource");
            }
        }
        public bool Accepts(ResourceId resource) => _accepted.Contains(resource);
    }

    public sealed class EndpointPools : IPersistentService, IResourceEndpointProvider
    {
        private sealed class Pool
        {
            public int Version;
            public readonly ResourceInventoryState Inventory;
            public Pool(World world, EndpointPoolDef definition, string scope)
            {
                Inventory = new ResourceInventoryState(new ResourceEndpointId(scope, definition.Identity),
                    () => definition.Owner, () => Version, () => Version = checked(Version + 1), world.Content,
                    () => definition.Policy, definition.Accepts, definition.Public, definition.Evacuate);
            }
        }
        private readonly SortedDictionary<long, Pool> _pools = new SortedDictionary<long, Pool>();
        public World World { get; }
        public string EndpointScope { get; }
        public string Id => "proof.pools";
        public int StateSchemaVersion => 1;
        // Fault injection belongs only to the proof provider, not the engine's protocol.
        public bool ReturnWrongIdentity;
        public bool InletOnly;
        public ClaimFaultMode ClaimFault;
        public bool ProbeConversion;
        public Action<ResourceInventoryState, ReservationOwner, ResourceId, Fix, ReservationToken> AfterReserveOut;
        public Func<ResourceInventoryState, IReadOnlyList<ResourceAmount>, IReadOnlyList<ResourceAmount>, CapacityCheck> ConversionCheck;
        public Action<ResourceInventoryState, ReservationOwner, IReadOnlyList<ResourceConversionInput>, Bill> ConversionCommit;
        public long FaultEndpoint = 2;
        public long FaultPeerEndpoint = 1;
        public int ResolveCalls;
        public Action<ResourceEndpointId> ResolveObservation;
        public EndpointPools(World world, string scope, World reportedWorld = null)
        {
            World = reportedWorld ?? world; EndpointScope = scope;
            foreach (EndpointPoolDef definition in world.Content.All<EndpointPoolDef>())
            {
                if (definition.Owner != PlayerId.None && !world.Players.IsSeat(definition.Owner) || _pools.ContainsKey(definition.Identity))
                    throw new ContentException("invalid or duplicate authored pool owner/identity");
                _pools.Add(definition.Identity, new Pool(world, definition, scope));
            }
        }
        public IResourceEndpoint Resolve(ResourceEndpointId endpoint)
        {
            ResolveCalls++; // Non-authoritative proof instrumentation, deliberately absent from Sync.
            ResolveObservation?.Invoke(endpoint); // Negative read-boundary instrumentation only; never saved.
            if (endpoint.Scope != EndpointScope) return null;
            if (ReturnWrongIdentity) return Get(endpoint.Identity == 1 ? 2 : 1);
            if (!_pools.TryGetValue(endpoint.Identity, out Pool pool)) return null;
            if (InletOnly && endpoint.Identity == FaultEndpoint) return new ClaimInlet(pool.Inventory);
            if (ProbeConversion && endpoint.Identity == FaultEndpoint) return new CoupledClaimOutlet(pool.Inventory, this);
            return ClaimFault != ClaimFaultMode.None && endpoint.Identity == FaultEndpoint
                ? (IResourceEndpoint)new ClaimFaultOutlet(pool.Inventory, ClaimFault, _pools[FaultPeerEndpoint].Inventory) : pool.Inventory;
        }
        public ResourceInventoryState Get(long identity) => _pools[identity].Inventory;
        public void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect)
        { foreach (var pool in _pools.Values) inspect(pool.Inventory.Endpoint, pool.Inventory.Reservations); }
        public void Sync(IStateSync sync)
        {
            if (sync.Count(_pools.Count) != _pools.Count) throw new CorruptStateException("pool content count differs");
            foreach (KeyValuePair<long, Pool> entry in _pools)
            {
                long identity = entry.Key; sync.Long(ref identity);
                if (identity != entry.Key) throw new CorruptStateException("pool identity differs from selected content");
                sync.Int(ref entry.Value.Version);
                if (entry.Value.Version < 0) throw new CorruptStateException("invalid pool version");
                entry.Value.Inventory.Sync(sync);
            }
        }
    }

    public sealed class DuplicateEndpointScope : IResourceEndpointProvider
    {
        private readonly EndpointPools _provider;
        public DuplicateEndpointScope(EndpointPools provider) { _provider = provider; }
        public World World => _provider.World;
        public string EndpointScope => _provider.EndpointScope;
        public IResourceEndpoint Resolve(ResourceEndpointId endpoint) => _provider.Resolve(endpoint);
        public void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect) => _provider.InspectReservations(inspect);
    }
    public sealed class EndpointProofModule : GameModule
    {
        private readonly string _scope;
        private readonly bool _duplicate;
        private readonly World _reportedWorld;
        public EndpointProofModule(string scope = "proof.pools", bool duplicate = false, World reportedWorld = null)
        { _scope = scope; _duplicate = duplicate; _reportedWorld = reportedWorld; }
        public override string Id => "proof.pools";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]
        { new ModuleRequirement(InventoryModule.ModuleId, Version), new ModuleRequirement(ResourceFlowsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        { types.Section<EndpointPoolDef>("resource_pools"); types.Component<CreationProbeDef>("creation_probe", 1); types.Component<CreationTailProbeDef>("creation_tail", 1); }
        public override void Compose(WorldComposer composer)
        {
            var pools = new EndpointPools(composer.World, _scope, _reportedWorld);
            composer.Services.Bind<EndpointPools>(pools);
            composer.AddViewFeature("proof.pools.diagnostics", 1, (world, viewer) => new EndpointPoolDiagnostics(world, viewer, pools));
            // Endpoint discovery uses the already-declared actual owner, not a singular interface binding.
            composer.Commands.Register("proof.pools.claim-probe", new ClaimProbeHandler());
            composer.Commands.Register("proof.pools.transfer-probe", new TransferProbeHandler());
            composer.Commands.Register("proof.pools.conversion-probe", new ConversionProbeHandler());
            if (_duplicate) composer.Services.Bind<DuplicateEndpointScope>(new DuplicateEndpointScope(pools));
        }
    }
    // Detached observations of the original book. Arm is negative-provider instrumentation only.
    public sealed class EndpointPoolDiagnostics
    {
        private readonly World _world; private readonly PlayerId _viewer; private readonly EndpointPools _pools;
        internal EndpointPoolDiagnostics(World world, PlayerId viewer, EndpointPools pools)
        { _world = world; _viewer = viewer; _pools = pools; }
        private ResourceInventoryState Owned(long identity)
        {
            var pool = _pools.Get(identity);
            if (_viewer != PlayerId.None && pool.Company != _viewer) throw new ArgumentException("actual owned diagnostic pool required");
            return pool;
        }
        public Bill Stock(long identity) => new Bill(Owned(identity).Stock);
        public ResourceClaimSnapshot Claims(long identity, ReservationOwner owner)
            => _world.Services.Get<IResourceTransaction>().ReadClaims(_viewer, Owned(identity).Endpoint, owner);
        public ResourceTransferCheck EvaluateTransfer(ResourceTransferIntent intent)
            => _viewer != PlayerId.None && intent.Company != _viewer ? new ResourceTransferCheck(ResourceTransferStatus.InvalidRequest)
                : _world.Services.Get<IResourceTransaction>().Evaluate(intent);
        public ResourceClaimSnapshot EndpointClaims(ResourceEndpointId endpoint, ReservationOwner owner)
            => _world.Services.Get<IResourceTransaction>().ReadClaims(_viewer, endpoint, owner);
        public void Arm(long endpoint, long peer, ClaimFaultMode mode)
        {
            Owned(endpoint); Owned(peer);
            _pools.FaultEndpoint = endpoint; _pools.FaultPeerEndpoint = peer; _pools.ClaimFault = mode;
        }
    }
    public static class EndpointGame
    {
        public static GameFactory Factory(EndpointProofModule module = null, string content = null)
        {
            var definition = new GameDefinition("endpoint-proof", new GameModule[]
            { new InventoryModule(), new ResourceFlowsModule(), new WorldSetupModule(), module ?? new EndpointProofModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("endpoints-manifest.json"))
                .Add("endpoints.json", content ?? FrameworkProofContent.Read("endpoints.json"))
                .Add("flows.json", FrameworkProofContent.Read("endpoints-flows.json"))
                .Add("endpoint-startup.json", FrameworkProofContent.Read("endpoint-startup.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
}
