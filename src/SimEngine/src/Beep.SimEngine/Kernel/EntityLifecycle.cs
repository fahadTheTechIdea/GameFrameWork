using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>An entity's update contract. The game's loop invokes it once per game-time update.</summary>
    public interface IEntity
    {
        EntityId Id { get; }
        bool Alive { get; }
        Code State { get; }
        long StateSince { get; }
        void Update(EntityUpdateContext context);
    }

    /// <summary>Engine-issued entity update permission, after systems/events. All time comes from the sole game clock.</summary>
    public readonly struct EntityUpdateContext
    {
        private readonly Entity _entity;
        // Captured permission coordinate, not another current-time owner or counter.
        private readonly long _issuedUpdate;
        internal EntityUpdateContext(World world, Entity entity)
        { World = world; _entity = entity; _issuedUpdate = world.Clock.StepIndex; }
        internal bool IsCurrent(Entity entity) => World != null && World.UpdatingEntities &&
            ReferenceEquals(_entity, entity) && ReferenceEquals(World.Entities.Get(entity.Id), entity) &&
            _issuedUpdate == World.Clock.StepIndex;
        public World World { get; }
        public IGameClock Clock => World.Clock;
        public long Tick => Clock.StepIndex;
    }

    /// <summary>
    /// A capability's current state, or null when inactive. Implementations only read settled facts;
    /// they do not advance work. References are resolved in the component def before tick zero.
    /// </summary>
    public interface IEntityStateSource
    {
        IEntityStateDefinition GetState(EntityUpdateContext context);
    }
}
