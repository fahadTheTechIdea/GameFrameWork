using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class ExplorationView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal ExplorationView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private bool Owned(Entity entity) => entity != null && entity.Alive && (_viewer == PlayerId.None || _world.Players.IsSeat(_viewer) && entity.Owner == _viewer);
        public Code Read(EntityId entityId, Code sourceId, out ExplorationSnapshot snapshot)
        {
            snapshot = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(entityId); if (!Owned(entity)) return Codes.NotFound;
            if (sourceId.IsNone) return Codes.InvalidArgument;
            var source = entity.Get<IExplorationSource>(sourceId); if (source == null) return ExplorationCodes.Unsupported;
            var observed = ExplorationContracts.Read(_world, source); _world.RequireHealthyUpdate(); snapshot = observed; return Code.None;
        }
        public void CopySources(IList<ExplorationSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            var snapshots = new List<ExplorationSnapshot>();
            foreach (Entity entity in _world.Entities.All) if (Owned(entity))
                foreach (Component part in entity.Components) if (part is IExplorationSource source) snapshots.Add(ExplorationContracts.Read(_world, source));
            snapshots.Sort((a, b) => { int c = a.Entity.Value.CompareTo(b.Entity.Value); return c != 0 ? c : string.CompareOrdinal(a.Source.Id, b.Source.Id); });
            _world.RequireHealthyUpdate();
            foreach (var snapshot in snapshots) destination.Add(snapshot);
        }
    }
}
