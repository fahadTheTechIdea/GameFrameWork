using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>Detached engine-owned facts and deltas for one session/viewer publication stream.
    /// Retain freely; later updates never mutate these values. Rendering remains outside the framework.</summary>
    public sealed class GameSnapshot
    {
        internal GameSnapshot() : this(0, 0, PlayerId.None, 0, Fix.Zero, default, null,
            new List<EntityView>(), new List<EntityId>(), new List<EntityView>(), new List<EntityId>()) { }
        private GameSnapshot(long revision, long previousRevision, PlayerId viewer, long tick, Fix alpha, GameState state, SpatialWorldSnapshot space,
            List<EntityView> entities, List<EntityId> spawned, List<EntityView> changed, List<EntityId> removed)
        {
            Revision = revision; PreviousRevision = previousRevision; Viewer = viewer;
            Tick = tick; Alpha = alpha; State = state; Space = space;
            Entities = entities.AsReadOnly(); Spawned = spawned.AsReadOnly(); Changed = changed.AsReadOnly(); Removed = removed.AsReadOnly();
        }
        public long Revision { get; }
        /// <summary>Baseline publication revision in this SAME session/viewer stream;0 for initial/load publication.
        /// If the consumer missed this baseline, use full Entities instead of applying an incomplete delta.</summary>
        public long PreviousRevision { get; }
        public PlayerId Viewer { get; }
        /// <summary>Epoch of the projected facts. Fault publication retains the last safe epoch.</summary>
        public long Tick { get; }
        public Fix Alpha { get; }
        public GameState State { get; }
        public SpatialWorldSnapshot Space { get; }
        public IReadOnlyList<EntityView> Entities { get; }
        /// <summary>Newly visible IDs, including first publication. Not necessarily newly created entities.</summary>
        public IReadOnlyList<EntityId> Spawned { get; }
        /// <summary>Current detached values of previously visible entities whose projected facts differ.</summary>
        public IReadOnlyList<EntityView> Changed { get; }
        /// <summary>Previously visible IDs now absent or outside sight; not a hidden destruction report.</summary>
        public IReadOnlyList<EntityId> Removed { get; }

        /// <summary>The original retained snapshot is the ONLY previous projection owner.
        /// Merge against canonical visible IDs; never compare hidden facts or use versions alone.</summary>
        internal static GameSnapshot Capture(World world, long revision, Fix alpha, GameState state, Func<Entity, bool> visible, GameSnapshot previous, PlayerId viewer)
        {
            if (previous != null && previous.Revision != 0 && previous.Viewer != viewer)
                throw new InvalidOperationException("snapshot baseline belongs to another viewer");
            var entities = new List<EntityView>();
            foreach (var entity in world.Entities.All)
                if (entity.Alive && (visible == null || visible(entity))) entities.Add(PlayerView.ViewOf(world, entity));
            entities.Sort((left, right) => left.Id.CompareTo(right.Id));
            var spawned = new List<EntityId>(); var changed = new List<EntityView>(); var removed = new List<EntityId>();
            IReadOnlyList<EntityView> before = previous?.Entities ?? Array.Empty<EntityView>();
            int a = 0, b = 0;
            while (a < entities.Count || b < before.Count)
            {
                if (b == before.Count || a < entities.Count && entities[a].Id.CompareTo(before[b].Id) < 0)
                    spawned.Add(entities[a++].Id);
                else if (a == entities.Count || before[b].Id.CompareTo(entities[a].Id) < 0)
                    removed.Add(before[b++].Id);
                else
                {
                    if (!entities[a].Matches(world, before[b])) changed.Add(entities[a]);
                    a++; b++;
                }
            }
            SpatialWorldSnapshot projectedSpace = EntityObservation.ReadSpace(world, viewer);
            return new GameSnapshot(revision, previous?.Revision ?? 0, viewer, world.Tick, alpha, state, projectedSpace,
                entities, spawned, changed, removed);
        }

        /// <summary>Republish only this viewer's retained facts; never query a faulted provider.</summary>
        internal static GameSnapshot Retain(long revision, GameSnapshot previous, PlayerId viewer)
        {
            if (previous != null && previous.Revision != 0 && previous.Viewer != viewer)
                throw new InvalidOperationException("snapshot baseline belongs to another viewer");
            var entities = new List<EntityView>();
            if (previous != null) foreach (EntityView entity in previous.Entities) entities.Add(entity);
            return new GameSnapshot(revision, previous?.Revision ?? 0, viewer, previous?.Tick ?? 0,
                Fix.Zero, GameState.Faulted, previous?.Space, entities,
                new List<EntityId>(), new List<EntityView>(), new List<EntityId>());
        }
    }
}
