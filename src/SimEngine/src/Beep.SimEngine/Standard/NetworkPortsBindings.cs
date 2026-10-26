using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
namespace Beep.SimEngine.Standard
{
    /// <summary>Sole strict connection authoring, never another topology, stock or transfer owner.</summary>
    public sealed class NetworkPortsDeclaration
    {
        private readonly JsonValue _raw;
        private readonly string _where;
        public IReadOnlyList<PortSpec> Connections { get; private set; }
        private NetworkPortsDeclaration(JsonReader reader)
        {
            _raw = reader.Array("list") ?? throw reader.Error("ports need an explicit 'list'");
            _where = reader.Where;
        }
        public static NetworkPortsDeclaration Read(JsonReader reader)
            => new NetworkPortsDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Connections != null) throw new InvalidOperationException("network port metadata resolves once");
            var specs = new PortSpec[_raw.Count];
            var networks = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _raw.Count; i++)
            {
                var row = new JsonReader(_raw[i], _where + ".list[" + i + "]");
                string networkId = row.String("network");
                string[] push = row.Strings("push"), pull = row.Strings("pull"); row.End();
                try
                {
                    ContractIdentity.ValidateId(networkId, "port network");
                    foreach (string id in push) ContractIdentity.ValidateId(id, "push resource");
                    foreach (string id in pull) ContractIdentity.ValidateId(id, "pull resource");
                }
                catch (ArgumentException error) { throw content.Fail(error.Message); }
                var network = content.Def<INetworkDefinition>(networkId);
                if (!networks.Add(network.Id)) throw content.Fail("duplicate network port connection");
                var resolvedPush = new ResourceId[push.Length]; var resolvedPull = new ResourceId[pull.Length];
                for (int k = 0; k < push.Length; k++) resolvedPush[k] = Carried(content, network, push[k]);
                for (int k = 0; k < pull.Length; k++) resolvedPull[k] = Carried(content, network, pull[k]);
                if (new HashSet<ResourceId>(resolvedPush).Count != resolvedPush.Length ||
                    new HashSet<ResourceId>(resolvedPull).Count != resolvedPull.Length)
                    throw content.Fail("duplicate network push or pull resource");
                specs[i] = new PortSpec(network, resolvedPush, resolvedPull);
            }
            Connections = Array.AsReadOnly(specs);
        }
        private static ResourceId Carried(ContentResolver content, INetworkDefinition network, string resource)
        {
            ResourceId id = content.Resource(resource);
            if (!network.Carries(id)) throw content.Fail("network '" + network.Id + "' does not carry '" + resource + "'");
            return id;
        }
        public void ValidateOwner(EntityDef owner) => NetworkPortsContracts.ValidateOwner(owner, Connections);
    }

    /// <summary>Actual authored/runtime connection admission and original scoped observations.</summary>
    public static class NetworkPortsContracts
    {
        internal static void ValidateOwner(EntityDef owner, IReadOnlyList<PortSpec> connections)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (connections == null) throw new ContentException(owner.Id + ": explicit network connection metadata required");
            var presence = owner.Component<ISpatialPresenceDefinition>();
            if (presence == null) throw new ContentException(owner.Id + ": network ports require actual spatial presence");
            foreach (var connection in connections)
            {
                if (connection.Network?.Line?.Layer == null || connection.Push == null || connection.Pull == null)
                    throw new ContentException(owner.Id + ": actual network line and push/pull lists required");
                if (connection.Network.Line.Layer.Space.Id != presence.Space)
                    throw new ContentException(owner.Id + ": network '" + connection.Network.Id + "' belongs to a different spatial space");
                if (connection.Pull.Count > 0 && !owner.HasCapability<IInletPort>())
                    throw new ContentException(owner.Id + ": importing network connection requires an inlet capability");
                if (connection.Push.Count > 0 && !owner.HasCapability<IOutletPort>())
                    throw new ContentException(owner.Id + ": exporting network connection requires an outlet capability");
            }
        }
        public static IReadOnlyList<PortSpec> ValidateDefinition(ResolvedContent content, EntityDef owner, INetworkPortsDefinition definition)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (owner == null || string.IsNullOrWhiteSpace(owner.Id) || !content.Owns(owner) ||
                !(definition is ComponentDef part) || !ReferenceEquals(part.Owner, owner) ||
                !ReferenceEquals(owner.Component<INetworkPortsDefinition>(), definition) ||
                !typeof(INetworkPorts).IsAssignableFrom(part.ComponentType) ||
                part.Slot < 0 || part.Slot >= owner.Components.Count || !ReferenceEquals(owner.Components[part.Slot], part))
                throw new ContentException("network ports require their actual singular catalog owner/metadata/runtime");
            var connections = definition.Connections;
            ValidateConnections(content, owner, connections);
            return connections;
        }
        private static void ValidateConnections(ResolvedContent content, EntityDef owner, IReadOnlyList<PortSpec> connections)
        {
            if (connections == null) throw new ContentException(owner.Id + ": explicit network connection metadata required");
            var networks = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in connections)
            {
                if (!(connection.Network is Def actual) || string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                    !content.Owns(connection.Network) || connection.Network.Id != actual.Id || connection.Network.Module != actual.Module ||
                    !ReferenceEquals(content.Find<INetworkDefinition>(actual.Id), connection.Network) || !networks.Add(actual.Id))
                    throw new ContentException(owner.Id + ": unique actual catalog network connections required");
                ValidateResources(content, owner, connection.Network, connection.Push);
                ValidateResources(content, owner, connection.Network, connection.Pull);
            }
            ValidateOwner(owner, connections);
        }
        private static void ValidateResources(ResolvedContent content, EntityDef owner, INetworkDefinition network, IReadOnlyList<ResourceId> resources)
        {
            if (resources == null) throw new ContentException(owner.Id + ": explicit push/pull resource lists required");
            var seen = new HashSet<ResourceId>();
            foreach (ResourceId resource in resources)
                if (!resource.IsValid || content.Resource(resource) == null || !seen.Add(resource) || !network.Carries(resource))
                    throw new ContentException(owner.Id + ": unique actual carried push/pull resources required");
        }
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    bool runtime = typeof(INetworkPorts).IsAssignableFrom(part.ComponentType);
                    if (runtime != (part is INetworkPortsDefinition))
                        throw new ContentException(owner.Id + ": network ports require matching metadata and runtime contracts");
                    if (!runtime) continue;
                    EntityObservation.ReadProvider(world, part, null, () =>
                    {
                        if (!part.IsFrozen || part.StateCodec == null ||
                            !ReferenceEquals(world.Content.ComponentDeclaration(part.Key)?.Metadata, part.StateCodec) ||
                            part.ComponentType == typeof(Ports) && part.StateCodec.SchemaVersion != 1)
                            throw new ContentException(owner.Id + ": ports require actual immutable registered metadata and original codec");
                        ValidateDefinition(world.Content, owner, (INetworkPortsDefinition)part);
                        return true;
                    });
                }
        }
        internal static IReadOnlyList<PortSpec> ReadConnections(World world, INetworkPorts provider)
            => EntityObservation.ReadProvider(world, provider, null, () =>
            {
                if (!(provider is Component component) || component.Entity == null || !component.Entity.Alive ||
                    !ReferenceEquals(world.Entities.Get(component.Entity.Id), component.Entity) ||
                    !ReferenceEquals(component.Entity.Get<INetworkPorts>(), provider) ||
                    !(component.Def is INetworkPortsDefinition definition) || !component.Def.IsFrozen)
                    throw new ContentException("network connection requires its actual live provider and immutable metadata");
                var entity = component.Entity;
                var scope = EntityObservation.CaptureProvider(world, entity, entity.Sync, "network.ports-read");
                try
                {
                    var authored = ValidateDefinition(world.Content, entity.Def, definition);
                    var live = provider.Connections;
                    if (!ReferenceEquals(live, authored))
                        throw new ContentException(entity.Def.Id + ": runtime connection list must alias its actual authored metadata");
                    return live;
                }
                finally { scope.Verify(); }
            });
    }
}
