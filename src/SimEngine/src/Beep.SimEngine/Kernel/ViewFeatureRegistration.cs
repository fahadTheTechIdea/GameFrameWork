using System;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Immutable identity of one module-owned, viewer-scoped read model. Not its factory or state.</summary>
    public sealed class ViewFeatureRegistration
    {
        internal ViewFeatureRegistration(string id, int schemaVersion, string owner, Type contract,
            Func<World, PlayerId, object> factory)
        {
            ContractIdentity.Validate(id, schemaVersion, "view feature");
            RequireReadModel(contract);
            Id = id;
            SchemaVersion = schemaVersion;
            Owner = owner;
            Contract = contract;
            _factory = factory;
        }

        public string Id { get; }
        public int SchemaVersion { get; }
        public string Owner { get; }
        public Type Contract { get; }
        /// <summary>Explicit primary identity for another contract of the same read model, or null.</summary>
        public string SourceId => Source?.Id;
        public Type SourceContract => Source?.Contract;
        internal ViewFeatureRegistration Source { get; }
        private readonly Func<World, PlayerId, object> _factory;

        internal ViewFeatureRegistration(string id, int schemaVersion, string owner, Type contract, ViewFeatureRegistration source)
        {
            ContractIdentity.Validate(id, schemaVersion, "view feature");
            RequireReadModel(contract);
            if (source == null || source.Source != null || source.Owner != owner || !contract.IsAssignableFrom(source.Contract))
                throw new InvalidOperationException("view contract alias requires its actual same-owner compatible primary registration");
            Id = id; SchemaVersion = schemaVersion; Owner = owner; Contract = contract; Source = source;
        }

        internal object Create(World world, PlayerId viewer)
            => EntityObservation.ReadProvider(world, this, null, () =>
            {
                object value = _factory(world, viewer) ?? throw new InvalidOperationException(
                    "view feature '" + Id + "' owned by '" + Owner + "' returned null");
                RequireReadModel(value.GetType());
                return value;
            }, space: world.Space);

        // Known direct engine authority aliases, not a reflection/security sandbox for custom models.
        private static void RequireReadModel(Type type)
        {
            if (type == typeof(object) || typeof(World).IsAssignableFrom(type) ||
                typeof(IEntity).IsAssignableFrom(type) || typeof(IEntityCapability).IsAssignableFrom(type) ||
                typeof(GameSystem).IsAssignableFrom(type) || typeof(IPersistentService).IsAssignableFrom(type) ||
                typeof(IGameClock).IsAssignableFrom(type) || typeof(EntityStore).IsAssignableFrom(type) ||
                typeof(ServiceContainer).IsAssignableFrom(type) || typeof(PlayerTable).IsAssignableFrom(type) ||
                typeof(CommandRegistry).IsAssignableFrom(type) || typeof(EventBus).IsAssignableFrom(type))
                throw new InvalidOperationException("view feature must expose a read model, not a mutable engine owner: " + type.Name);
        }

    }
}
