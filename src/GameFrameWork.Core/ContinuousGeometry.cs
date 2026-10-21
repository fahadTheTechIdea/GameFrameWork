namespace GameFrameWork.Core.Geometry;

/// <summary>Planar coordinates in world meters, independent of rendered sprite scale.</summary>
public readonly record struct WorldPoint(double X, double Y)
{
    public double DistanceTo(WorldPoint other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    public WorldPoint Offset(double dx, double dy) => new(X + dx, Y + dy);
}
public sealed record WorldPolyline(IReadOnlyList<WorldPoint> Points)
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Points);
        if (Points.Count < 2) throw new ArgumentException("A polyline requires at least two points.");
        for (int i = 0; i < Points.Count; i++)
        {
            if (!double.IsFinite(Points[i].X) || !double.IsFinite(Points[i].Y))
                throw new ArgumentException("Nonfinite coordinates.");
            if (i > 0 && Points[i].DistanceTo(Points[i - 1]) < 1e-8)
                throw new ArgumentException("Consecutive duplicate points.");
        }
    }
    public double LengthMeters()
    {
        Validate();
        double length = 0;
        for (int i = 1; i < Points.Count; i++)
            length += Points[i - 1].DistanceTo(Points[i]);
        return length;
    }
}
public sealed record RoadCrossSection(double LaneWidthMeters, int LaneCount,
    double SidewalkLeftMeters, double SidewalkRightMeters, double CurbLeftMeters,
    double CurbRightMeters, double MedianMeters = 0)
{
    public double TotalWidthMeters => LaneWidthMeters * LaneCount + SidewalkLeftMeters
        + SidewalkRightMeters + CurbLeftMeters + CurbRightMeters + MedianMeters;
    public void Validate()
    {
        if (LaneCount is < 1 or > 16 || !double.IsFinite(LaneWidthMeters) || LaneWidthMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(LaneWidthMeters));
        if (new[] { SidewalkLeftMeters, SidewalkRightMeters, CurbLeftMeters, CurbRightMeters, MedianMeters }
            .Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Cross-section widths must be finite and nonnegative.");
        if (!double.IsFinite(TotalWidthMeters) || TotalWidthMeters <= 0)
            throw new ArgumentException("Invalid total road width.");
    }
}
public sealed record StreetCorridor(string Id, WorldPolyline Centerline, RoadCrossSection Profile)
{
    /// <summary>Bounds of the swept centerline, expanded by half the road width. Conservative for bends.</summary>
    public (WorldPoint Min, WorldPoint Max) Bounds()
    {
        Centerline.Validate();
        Profile.Validate();
        var half = Profile.TotalWidthMeters / 2;
        return (
            new(Centerline.Points.Min(p => p.X) - half, Centerline.Points.Min(p => p.Y) - half),
            new(Centerline.Points.Max(p => p.X) + half, Centerline.Points.Max(p => p.Y) + half));
    }
    /// <summary>Produces per-segment side offsets; intersections and corner joins require a separate solver.</summary>
    public IReadOnlyList<(WorldPoint LeftStart, WorldPoint LeftEnd, WorldPoint RightStart, WorldPoint RightEnd)> SegmentEdges()
    {
        Centerline.Validate();
        Profile.Validate();
        var half = Profile.TotalWidthMeters / 2;
        var edges = new List<(WorldPoint, WorldPoint, WorldPoint, WorldPoint)>();
        for (int i = 1; i < Centerline.Points.Count; i++)
        {
            var a = Centerline.Points[i - 1];
            var b = Centerline.Points[i];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            var nx = -dy / len * half;
            var ny = dx / len * half;
            edges.Add((a.Offset(nx, ny), b.Offset(nx, ny),
                a.Offset(-nx, -ny), b.Offset(-nx, -ny)));
        }
        return edges;
    }
}
