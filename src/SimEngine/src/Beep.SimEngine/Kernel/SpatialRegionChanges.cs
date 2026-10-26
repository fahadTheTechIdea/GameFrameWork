using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    public enum SpatialRegionChangeStatus : byte { Success = 0, InvalidCursor = 1 }

    /// <summary>Detached transient baseline; contains no world, space provider or saved authority.</summary>
    public sealed class SpatialRegionCursor
    {
        internal SpatialRegionCursor(SpatialChangeScope scope, int version, string binding)
        { Scope = scope; Version = version; Binding = binding; }
        internal SpatialChangeScope Scope { get; }
        internal string Binding { get; }
        public int Version { get; }
    }

    /// <summary>Opaque identity for one query stream. Owns no version counter or history.</summary>
    public sealed class SpatialChangeScope
    {
        public SpatialRegionCursor Capture(int version, string binding = null)
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            return new SpatialRegionCursor(this, version, binding);
        }
        public bool TryBaseline(SpatialRegionCursor cursor, int currentVersion, out int previousVersion, string binding = null)
        {
            if (currentVersion < 0) throw new ArgumentOutOfRangeException(nameof(currentVersion));
            previousVersion = -1;
            if (cursor == null) return true;
            if (!ReferenceEquals(cursor.Scope, this) || !string.Equals(cursor.Binding, binding, StringComparison.Ordinal)
                || cursor.Version > currentVersion) return false;
            previousVersion = cursor.Version;
            return true;
        }
    }

    /// <summary>Provider-owned regional invalidation, shared by tile and other authored spaces.
    /// Retained cursors belong to this exact query stream, not another viewer/session/restored view.</summary>
    public interface ISpatialRegionChangeQuery<TRegion>
    {
        SpatialRegionChanges<TRegion> ReadChanges(SpatialRegionCursor previous = null);
    }

    /// <summary>Actual spatial owner of transient regional invalidation. The version must equal
    /// Capture(viewer).Version. Regions are detached immutable provider geometry in deterministic
    /// order; no values or hidden sample identities may appear. A current baseline yields empty
    /// incremental facts. Full invalidation is an actual scope/history reset, not an unknown fallback.</summary>
    public interface ISpatialRegionChangeSource<TRegion> : ISpatialSpace
    {
        SpatialRegionChangeSet<TRegion> ReadRegionChanges(PlayerId viewer, int sinceVersion);
    }

    /// <summary>Detached owner facts, without a client cursor or another revision/history authority.</summary>
    public sealed class SpatialRegionChangeSet<TRegion>
    {
        private SpatialRegionChangeSet(int version, bool full, IEnumerable<TRegion> regions)
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            var copy = new List<TRegion>();
            foreach (TRegion region in regions) {
                if (region is null) throw new ArgumentException("changed regions require detached geometry", nameof(regions));
                copy.Add(region);
            }
            Version = version; FullInvalidation = full; ChangedRegions = copy.AsReadOnly();
        }
        public int Version { get; }
        public bool FullInvalidation { get; }
        public IReadOnlyList<TRegion> ChangedRegions { get; }
        public static SpatialRegionChangeSet<TRegion> Full(int version)
            => new SpatialRegionChangeSet<TRegion>(version, true, Array.Empty<TRegion>());
        public static SpatialRegionChangeSet<TRegion> Incremental(int version, IEnumerable<TRegion> regions)
            => new SpatialRegionChangeSet<TRegion>(version, false, regions);
    }

    /// <summary>Detached invalidation facts; regions must themselves be immutable provider geometry.
    /// Full invalidation never enumerates a huge space; read data through its original scoped query.</summary>
    public sealed class SpatialRegionChanges<TRegion>
    {
        private SpatialRegionChanges(SpatialRegionChangeStatus status, SpatialRegionCursor cursor,
            bool fullInvalidation, TRegion[] regions)
        {
            Status = status; Cursor = cursor; FullInvalidation = fullInvalidation;
            ChangedRegions = Array.AsReadOnly(regions);
        }
        public SpatialRegionChangeStatus Status { get; }
        public SpatialRegionCursor Cursor { get; }
        public bool FullInvalidation { get; }
        public IReadOnlyList<TRegion> ChangedRegions { get; }
        public static SpatialRegionChanges<TRegion> Full(SpatialRegionCursor cursor)
            => new SpatialRegionChanges<TRegion>(SpatialRegionChangeStatus.Success,
                cursor ?? throw new ArgumentNullException(nameof(cursor)), true, Array.Empty<TRegion>());
        public static SpatialRegionChanges<TRegion> Incremental(SpatialRegionCursor cursor, IEnumerable<TRegion> regions)
        {
            if (cursor == null) throw new ArgumentNullException(nameof(cursor));
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            return new SpatialRegionChanges<TRegion>(SpatialRegionChangeStatus.Success, cursor, false, new List<TRegion>(regions).ToArray());
        }
        public static SpatialRegionChanges<TRegion> Refused()
            => new SpatialRegionChanges<TRegion>(SpatialRegionChangeStatus.InvalidCursor, null, false, Array.Empty<TRegion>());
    }
}
