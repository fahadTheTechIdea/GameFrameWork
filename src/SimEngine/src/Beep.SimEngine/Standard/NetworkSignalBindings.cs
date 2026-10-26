using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable selector for the existing connected-pool signal, never a pool owner.</summary>
    public interface INetworkSignalDefinition : IAutomationSignalDefinition
    {
        INetworkDefinition Network { get; }
    }

    /// <summary>Sole strict JSON declaration shared by native and independent signal definitions.</summary>
    public sealed class NetworkSignalDeclaration
    {
        private readonly string _network;
        public Code CapabilityId { get; }
        public INetworkDefinition Network { get; private set; }
        public static IReadOnlyList<AutomationMetricSchema> Metrics { get; } = Array.AsReadOnly(new[]
        {
            AutomationMetricSchema.Quantity(new Code("network.stored")),
            AutomationMetricSchema.Quantity(new Code("network.available")),
            AutomationMetricSchema.Quantity(new Code("network.inbound")),
            AutomationMetricSchema.Quantity(new Code("network.outbound")),
            AutomationMetricSchema.Quantity(new Code("network.receiving-room"))
        });
        private NetworkSignalDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); _network = reader.String("network");
            try
            {
                ContractIdentity.ValidateId(id, "network signal");
                ContractIdentity.ValidateId(_network, "signal network");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id);
        }
        public static NetworkSignalDeclaration Read(JsonReader reader)
            => new NetworkSignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Network != null) throw new InvalidOperationException("network signal metadata resolves once");
            Network = content.Def<INetworkDefinition>(_network);
        }
        public void ValidateOwner(EntityDef owner) => NetworkConnectionContracts.Validate(owner, Network);
    }

    /// <summary>Sole actual declared connection admission shared by network signal/gate metadata.</summary>
    internal static class NetworkConnectionContracts
    {
        internal static void Validate(EntityDef owner, INetworkDefinition network)
        {
            if (network == null) throw new ContentException(owner.Id + ": network binding requires its explicit resolved network");
            foreach (var component in owner.Components)
                if (component is INetworkPortsDefinition ports && typeof(INetworkPorts).IsAssignableFrom(component.ComponentType))
                {
                    var connections = ports.Connections;
                    if (connections == null) throw new ContentException(owner.Id + ": network binding connection metadata is missing");
                    foreach (var connection in connections) if (ReferenceEquals(connection.Network, network)) return;
                }
            throw new ContentException(owner.Id + ": network binding requires an actual declared connection to '" + network.Id + "'");
        }
    }

    /// <summary>Actual frozen aliases and original scoped observations; no network/stock/clock state.</summary>
    public static class NetworkSignalBindingContracts
    {
        internal static T Observe<T>(World world, INetworkSignalDefinition definition, Entity entity, Func<INetworkDefinition, T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var part = AutomationSignalContracts.ValidateReusable(world, definition, typeof(NetworkSignal), 1, 2,
                    NetworkSignalDeclaration.Metrics, entity);
                var network = definition.Network;
                if (network == null || !world.Content.Owns(network) || !ReferenceEquals(world.Content.Find<INetworkDefinition>(network.Id), network))
                    throw new ContentException(part.Owner.Id + ": signal requires its actual catalog network");
                NetworkConnectionContracts.Validate(part.Owner, network); return read(network);
            }, observeTargetState: true, faultPhase: "network.signal-metadata");
        public static void ValidateBinding(World world, INetworkSignalDefinition definition)
            => Observe(world, definition, null, network => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    bool runtime = part.ComponentType == typeof(NetworkSignal);
                    if (runtime != (part is INetworkSignalDefinition))
                        throw new ContentException(owner.Id + ": original network signal requires matching metadata contract");
                    if (runtime) ValidateBinding(world, (INetworkSignalDefinition)part);
                }
        }
    }
}
