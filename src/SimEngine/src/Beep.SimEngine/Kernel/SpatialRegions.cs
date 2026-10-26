using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    public enum SpatialRegionStatus : byte { Inside = 0, Outside = 1, Unsupported = 2 }

    /// <summary>Immutable geometry for one authored or runtime area version. Checks detached presence.</summary>
    public interface ISpatialRegion
    {
        string Id { get; }
        string Space { get; }
        SpatialRegionStatus Check(SpatialSnapshot presence);
    }

    /// <summary>Pure containment of the entire provider-defined sweep, not just its endpoints.</summary>
    public interface ISpatialTravelRegion : ISpatialRegion
    {
        SpatialRegionStatus CheckTravel(SpatialSnapshot from, SpatialSnapshot to);
    }

    public interface ISpatialRegionGeometry : ISpatialRegion
    {
        string ShapeJson { get; }
    }

    /// <summary>Complete detached shapes whose union is exactly this region, not representative points or a guessed envelope.</summary>
    public interface ISpatialObservationRegion : ISpatialRegion
    {
        IReadOnlyList<SpatialSnapshot> CaptureObservationFootprints();
    }
}
