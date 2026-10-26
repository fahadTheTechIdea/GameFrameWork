using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    /// <summary>Detached facts from the existing authored site and work-order owners.</summary>
    public readonly struct ConstructionSiteSnapshot
    {
        public EntityId Site { get; }
        public string TargetDefinition { get; }
        public int Order { get; }
        internal ConstructionSiteSnapshot(EntityId site, string target, int order)
        { Site = site; TargetDefinition = target; Order = order; }
    }

    public sealed class ConstructionView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal ConstructionView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        /// <summary>Company-private construction intent, independent of prototype spelling or geometry.</summary>
        public bool TryReadSite(EntityId id, out ConstructionSiteSnapshot snapshot)
        {
            snapshot = default;
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return false;
            IConstructionSite site = entity.Get<IConstructionSite>();
            if (site == null) return false;
            var facts = EntityObservation.ReadProvider(_world, site, entity, () =>
            {
                var definition = entity.Def.Component<IConstructionSiteDefinition>();
                EntityDef target = ConstructionBindingContracts.ReadSite(_world, definition, entity, value => value.Target);
                if (!System.Object.ReferenceEquals(site.Entity, entity) || !System.Object.ReferenceEquals(site.Target, target) || site.Order < 0)
                    throw new System.InvalidOperationException("site projection disagrees with its actual authored binding");
                if (!_world.ViewFor(_viewer).CanSeeContent(target)) return (ConstructionSiteSnapshot?)null;
                return new ConstructionSiteSnapshot(entity.Id, target.Id, site.Order);
            });
            _world.RequireHealthyUpdate();
            if (!facts.HasValue) return false;
            snapshot = facts.Value;
            return true;
        }
        public string RecoveryContainer
        { get { EntityDef definition = _world.Services.Get<IResourceRecovery>().Definition?.Container; return _world.ViewFor(_viewer).CanSeeContent(definition) ? definition.Id : null; } }
        public Code CheckRecovery(EntityId entity)
        {
            _world.RequireHealthyUpdate();
            Entity source = _world.Entities.Get(entity);
            if (source == null) return Codes.NotFound;
            if (_viewer.IsValid && source.Owner != _viewer) return Codes.NotOwned;
            return _world.Services.Get<IResourceRecovery>().CheckEvacuation(_world, source);
        }

        /// <summary>Placement check for a structure, exactly as the construct command will make it.</summary>
        public Code CheckPlacement(string def, JsonValue arguments)
            => _world.Services.Get<IConstructionProjects>().CheckPlacement(_world, _viewer, _world.Content.Find<EntityDef>(def), arguments);

        /// <summary>The immediate removal command's current plan and physical preflight, without mutation.</summary>
        public Code CheckDemolition(EntityId target)
            => _world.Services.Get<IConstructionProjects>().PreviewDemolition(_world, _viewer, target, out _);

    }
}
