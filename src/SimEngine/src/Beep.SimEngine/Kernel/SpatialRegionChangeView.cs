using System;

namespace Beep.SimEngine.Kernel
{
    /// <summary>One engine-owned cursor/observation protocol over actual provider-owned change facts.
    /// Owns only query identity, never samples, revision counters, history or geometry policy.</summary>
    public sealed class SpatialRegionChangeView<TRegion> : ISpatialRegionChangeQuery<TRegion>
    {
        private readonly World _world;
        private readonly ISpatialRegionChangeSource<TRegion> _source;
        private readonly PlayerId _viewer;
        private readonly SpatialChangeScope _scope = new SpatialChangeScope();
        public SpatialRegionChangeView(World world, PlayerId viewer)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _source = world.Space as ISpatialRegionChangeSource<TRegion>
                ?? throw new InvalidOperationException("regional changes require the actual selected provider capability");
            _viewer = viewer;
        }
        public static void Compose(WorldComposer composer, ISpatialRegionChangeSource<TRegion> source, string id)
        {
            if (composer == null || source == null) throw new ArgumentNullException();
            if (!ReferenceEquals(composer.World.Space, source))
                throw new InvalidOperationException("regional changes bind only the actual selected spatial owner");
            composer.AddViewFeature(id, 1, (world, viewer) => new SpatialRegionChangeView<TRegion>(world, viewer));
            composer.AddViewFeatureAlias<ISpatialRegionChangeQuery<TRegion>, SpatialRegionChangeView<TRegion>>(id + ".query", 1);
            composer.RequireViewFeature<SpatialRegionChangeView<TRegion>>(id, 1);
        }
        public SpatialRegionChanges<TRegion> ReadChanges(SpatialRegionCursor previous = null)
        {
            _world.RequireHealthyUpdate();
            return EntityObservation.ReadProvider(_world, _source, null, () => {
                int version = EntityObservation.ReadSpace(_world, _viewer).Version;
                if (!_scope.TryBaseline(previous, version, out int before)) return SpatialRegionChanges<TRegion>.Refused();
                var cursor = _scope.Capture(version);
                if (previous == null) return SpatialRegionChanges<TRegion>.Full(cursor);
                var facts = _source.ReadRegionChanges(_viewer, before);
                if (facts == null || facts.Version != version)
                    throw new InvalidOperationException("regional change facts must match the actual viewer revision");
                if (before == version && (facts.FullInvalidation || facts.ChangedRegions.Count != 0))
                    throw new InvalidOperationException("unchanged regional data cannot report invalidation");
                return facts.FullInvalidation ? SpatialRegionChanges<TRegion>.Full(cursor)
                    : SpatialRegionChanges<TRegion>.Incremental(cursor, facts.ChangedRegions);
            }, space: _source);
        }
    }
}
