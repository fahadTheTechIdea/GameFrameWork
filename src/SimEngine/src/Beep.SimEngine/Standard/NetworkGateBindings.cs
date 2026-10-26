using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored networks and conditions for the original stateless physical flow gate.</summary>
    public interface ISignalNetworkGateDefinition : INamedCapabilityDefinition
    {
        IReadOnlyList<INetworkDefinition> Networks { get; }
        IReadOnlyList<SignalGateCondition> Conditions { get; }
    }
    /// <summary>Sole JSON declaration; SignalGateCondition retains all condition interpretation.</summary>
    public sealed class SignalNetworkGateDeclaration
    {
        private readonly string[] _networks;
        public Code CapabilityId { get; }
        public IReadOnlyList<INetworkDefinition> Networks { get; private set; }
        public IReadOnlyList<SignalGateCondition> Conditions { get; }
        private SignalNetworkGateDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); _networks = reader.Strings("networks");
            try
            {
                ContractIdentity.ValidateId(id, "network gate");
                foreach (string network in _networks) ContractIdentity.ValidateId(network, "gated network");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id);
            if (_networks.Length == 0) throw reader.Error("gate requires explicit networks");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string network in _networks) if (!seen.Add(network)) throw reader.Error("duplicate gated network");
            Conditions = SignalGateCondition.ReadAll(reader);
        }
        public static SignalNetworkGateDeclaration Read(JsonReader reader)
            => new SignalNetworkGateDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Networks != null) throw new InvalidOperationException("network gate metadata resolves once");
            var networks = new List<INetworkDefinition>();
            foreach (string id in _networks) networks.Add(content.Def<INetworkDefinition>(id));
            networks.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id)); Networks = networks.AsReadOnly();
            foreach (var condition in Conditions) condition.Resolve(content);
        }
        public void ValidateOwner(EntityDef owner)
        {
            foreach (var network in Networks) NetworkConnectionContracts.Validate(owner, network);
            foreach (var condition in Conditions) condition.Validate(owner);
        }
    }
    /// <summary>Actual immutable gate admission and observations, not another evaluator/state owner.</summary>
    public static class NetworkGateBindingContracts
    {
        internal static T Observe<T>(World world, ISignalNetworkGateDefinition definition, Entity entity,
            Func<IReadOnlyList<INetworkDefinition>, IReadOnlyList<SignalGateCondition>, T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var scope = entity == null ? default : EntityObservation.CaptureProvider(world, entity, entity.Sync, "network.gate-metadata");
                try
                {
                    var part = ExecutionGateContracts.ValidateReusable(world, definition, typeof(SignalNetworkGate), 1, entity);
                    var networks = definition.Networks; var conditions = definition.Conditions;
                    if (networks == null || networks.Count == 0 || conditions == null || conditions.Count == 0)
                        throw new ContentException(part.Owner.Id + ": gate requires explicit networks and signal conditions");
                    string previous = null;
                    foreach (var network in networks)
                    {
                        if (network == null || !world.Content.Owns(network) || !ReferenceEquals(world.Content.Find<INetworkDefinition>(network.Id), network) ||
                            previous != null && string.CompareOrdinal(previous, network.Id) >= 0)
                            throw new ContentException(part.Owner.Id + ": gate requires actual unique ordered catalog networks");
                        NetworkConnectionContracts.Validate(part.Owner, network); previous = network.Id;
                    }
                    previous = null;
                    foreach (var condition in conditions)
                    {
                        if (condition == null || condition.Signal == null || !world.Content.Owns(condition.Signal) ||
                            !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(condition.Signal.Id), condition.Signal) ||
                            previous != null && string.CompareOrdinal(previous, condition.Id) >= 0)
                            throw new ContentException(part.Owner.Id + ": gate requires actual unique ordered signal conditions");
                        condition.Validate(part.Owner); previous = condition.Id;
                    }
                    return read(networks, conditions);
                }
                finally { if (entity != null) scope.Verify(); }
            });
        public static void ValidateBinding(World world, ISignalNetworkGateDefinition definition)
            => Observe(world, definition, null, (networks, conditions) => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    bool runtime = part.ComponentType == typeof(SignalNetworkGate);
                    if (runtime != (part is ISignalNetworkGateDefinition))
                        throw new ContentException(owner.Id + ": original network gate requires matching metadata contract");
                    if (runtime) ValidateBinding(world, (ISignalNetworkGateDefinition)part);
                }
        }
    }
}
