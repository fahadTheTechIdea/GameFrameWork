using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;
public readonly record struct GridPoint(int X, int Y);
public readonly record struct GridBounds(int Width, int Height)
{
    public bool Contains(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
}
public enum RoadClass { Arterial, Collector, Local }
public sealed record RoadSegment(string Id, GridPoint Start, GridPoint End, RoadClass Class, string? SourceVisualPathId = null)
{
    public IEnumerable<GridPoint> Cells()
    {
        // Integer supercover: include both side cells at an exact corner crossing.
        // This preserves four-neighbor navigation, including diagonal/reversed lines.
        long nx = Math.Abs((long)End.X-Start.X), ny = Math.Abs((long)End.Y-Start.Y);
        if (nx+ny>1_000_000) throw new ArgumentException("ROAD_RASTER_SEGMENT_BUDGET_EXCEEDED");
        int dx = Math.Sign((long)End.X-Start.X), dy = Math.Sign((long)End.Y-Start.Y);
        long ix = 0, iy = 0;
        var p = Start;
        yield return p;
        while (ix < nx || iy < ny)
        {
            long decision = (1+2*ix)*ny-(1+2*iy)*nx;
            if (decision == 0)
            {
                yield return new(p.X+dx,p.Y);
                yield return new(p.X,p.Y+dy);
                p = new(p.X+dx,p.Y+dy); ix++; iy++;
            }
            else if (decision < 0) { p = new(p.X+dx,p.Y); ix++; }
            else { p = new(p.X,p.Y+dy); iy++; }
            yield return p;
        }
    }
}
public sealed record PlacementDecision(string Id, string Kind, GridPoint Site, string Purpose,
    string ParentId, string ConstraintId, GridPoint AccessRoadCell,
    VisualPlacementSource? VisualSource = null);
public sealed record PlacementDiagnostic(string Code, string Message);
public sealed record SettlementPlan(IReadOnlyList<RoadSegment> Roads,
    IReadOnlyList<PlacementDecision> Buildings, IReadOnlyList<PlacementDiagnostic> Diagnostics,
    WorldDesignPlan? Design = null, IReadOnlyList<StreetCorridor>? Streets = null,
    IReadOnlyList<BuildingSiteFeature>? SiteFeatures = null);
