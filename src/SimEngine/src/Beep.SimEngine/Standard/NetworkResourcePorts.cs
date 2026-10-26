using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Engine-side access to the original connected endpoint. Hosts receive immutable views, never this handle.</summary>
    public interface INetworkResourceAccess
    {
        ResourcePortResolution ResolveStore(EntityId connection, EntityDef footprint, INetworkDefinition network, PlayerId company,
            long minimumMembers, ResourceEndpointId? original = null);
    }
    public interface INetworkResourcePortDefinition : IResourcePortDefinition
    {
        INetworkDefinition Network { get; }
        long MinimumMembers { get; }
    }
    /// <summary>Sole strict file declaration for native and independently authored connected ports.</summary>
    public sealed class NetworkResourcePortDeclaration
    {
        private readonly string _network;
        public Code CapabilityId { get; }
        public long MinimumMembers { get; }
        public INetworkDefinition Network { get; private set; }
        public ICapacityPolicy CapacityPolicy { get; private set; }
        private NetworkResourcePortDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); _network = reader.String("network");
            MinimumMembers = reader.Long("minimum_members");
            try
            {
                ContractIdentity.ValidateId(id, "network resource port");
                ContractIdentity.ValidateId(_network, "port network");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (MinimumMembers <= 0) throw reader.Error("network resource port requires positive minimum_members");
            CapabilityId = new Code(id);
        }
        public static NetworkResourcePortDeclaration Read(JsonReader reader)
            => new NetworkResourcePortDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Network != null) throw new InvalidOperationException("network port resolves once");
            Network = content.Def<INetworkDefinition>(_network);
            CapacityPolicy = new InstancedCapacityPolicy(Network.CapacityPolicy, MinimumMembers);
        }
        public bool Accepts(ResolvedContent content, ResourceId resource) => Network.Carries(resource);
        public void ValidateOwner(EntityDef owner) => NetworkConnectionContracts.Validate(owner, Network);
        public static void ValidateBinding(World world, EntityDef owner, INetworkResourcePortDefinition definition)
        {
            if (definition == null || !(definition is ComponentDef part) || !ReferenceEquals(part.Owner, owner) ||
                !ReferenceEquals(owner.Component<INetworkResourcePortDefinition>(definition.CapabilityId), definition) ||
                !typeof(IResourcePort).IsAssignableFrom(part.ComponentType) || !world.Content.Owns(owner) ||
                part.Slot < 0 || part.Slot >= owner.Components.Count || !ReferenceEquals(owner.Components[part.Slot], part))
                throw new ContentException("network resource port requires its actual catalog owner/metadata/runtime");
            EntityObservation.ReadProvider(world, definition, null, () =>
            {
                var network = definition.Network;
                if (network == null || !world.Content.Owns(network) ||
                    !ReferenceEquals(world.Content.Find<INetworkDefinition>(network.Id), network) ||
                    definition.MinimumMembers <= 0 || !(definition.CapacityPolicy is IInstancedCapacityPolicy capacity) ||
                    !ReferenceEquals(capacity.Definition, network.CapacityPolicy) || capacity.Instances != definition.MinimumMembers)
                    throw new ContentException(owner.Id + ": actual network and explicitly instanced capacity required");
                NetworkConnectionContracts.Validate(owner, network);
                NetworkPortsContracts.ValidateDefinition(world.Content, owner, owner.Component<INetworkPortsDefinition>());
                if (owner.Component<ISpatialPresenceDefinition>()?.Space != network.Line.Layer.Space.Id ||
                    network.Line.Layer.Space.Id != world.Space?.Id || !world.Services.Has<INetworkResourceAccess>())
                    throw new ContentException(owner.Id + ": network resource port requires its selected spatial pool access");
                return true;
            });
        }
    }
    public sealed class NetworkResourcePortDef : ComponentDef<NetworkResourcePort>, INetworkResourcePortDefinition,
        IComponentCompositionValidation
    {
        private NetworkResourcePortDeclaration _declaration;
        public bool BindsEntityInventory => false;
        public Code CapabilityId => _declaration.CapabilityId;
        public INetworkDefinition Network => _declaration.Network;
        public long MinimumMembers => _declaration.MinimumMembers;
        public ICapacityPolicy CapacityPolicy => _declaration.CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _declaration.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkResourcePortDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateComposition(World world, EntityDef owner) => NetworkResourcePortDeclaration.ValidateBinding(world, owner, this);
    }
    public sealed class NetworkResourcePort : Component, IResourcePort
    {
        private INetworkResourcePortDefinition Binding => (INetworkResourcePortDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
        {
            NetworkResourcePortDeclaration.ValidateBinding(world, Entity.Def, Binding);
            return world.Services.Get<INetworkResourceAccess>().ResolveStore(Entity.Id, Entity.Def, Binding.Network, Entity.Owner,
                Binding.MinimumMembers, original);
        }
    }
    public sealed partial class NetworkService : INetworkResourceAccess
    {
        public static readonly Code InsufficientMembers = new Code("network.insufficient-members");
        public ResourcePortResolution ResolveStore(EntityId connection, EntityDef footprint, INetworkDefinition network, PlayerId company,
            long minimumMembers, ResourceEndpointId? original = null)
        {
            if (network == null || minimumMembers <= 0 || !ReferenceEquals(_world.Content.Find<INetworkDefinition>(network.Id), network))
                throw new ArgumentException("connected stock requires actual network metadata and positive required membership");
            IResourceOutlet captured = null;
            Code refusal = ObserveConnection(connection, network.Id, company, footprint, pool =>
            {
                if (original.HasValue && pool.Endpoint != original.Value) return ResourcePortCodes.OriginalUnavailable;
                if (pool.MemberCount < minimumMembers) return InsufficientMembers;
                captured = pool.Inventory; return Code.None;
            });
            return refusal.IsNone ? ResourcePortResolution.ReadyStore(captured) : ResourcePortResolution.Unavailable(refusal);
        }
    }
}
