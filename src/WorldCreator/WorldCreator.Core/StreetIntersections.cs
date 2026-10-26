using WorldCreator.Core.Geometry;
namespace WorldCreator.Core.Geometry;
public enum IntersectionKind { None, Point, CollinearOverlap }
public sealed record SegmentIntersection(IntersectionKind Kind, WorldPoint? Point = null);
public static class PlanarIntersections
{
    private const double Epsilon = 1e-9;
    private static double Cross(WorldPoint a, WorldPoint b) => a.X * b.Y - a.Y * b.X;
    private static WorldPoint Minus(WorldPoint a, WorldPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static SegmentIntersection Intersect(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d)
    {
        foreach (var p in new[] { a, b, c, d })
            if (!double.IsFinite(p.X) || !double.IsFinite(p.Y))
                throw new ArgumentException("Segment endpoints must be finite.");
        var r = Minus(b, a);
        var s = Minus(d, c);
        double rr = r.X * r.X + r.Y * r.Y;
        double ss = s.X * s.X + s.Y * s.Y;
        if (rr <= Epsilon * Epsilon || ss <= Epsilon * Epsilon)
            throw new ArgumentException("Zero-length segments cannot form a road.");
        var q = Minus(c, a);
        double cross = Cross(r, s);
        double scale = Math.Sqrt(rr * ss);
        if (Math.Abs(cross) <= Epsilon * scale)
        {
            if (Math.Abs(Cross(q, r)) > Epsilon * Math.Sqrt(rr) * Math.Max(1, Math.Sqrt(q.X * q.X + q.Y * q.Y)))
                return new(IntersectionKind.None);
            double t0 = (q.X * r.X + q.Y * r.Y) / rr;
            double t1 = t0 + (s.X * r.X + s.Y * r.Y) / rr;
            double lo = Math.Max(0, Math.Min(t0, t1));
            double hi = Math.Min(1, Math.Max(t0, t1));
            if (hi < lo - Epsilon) return new(IntersectionKind.None);
            if (Math.Abs(hi - lo) <= Epsilon)
                return new(IntersectionKind.Point, new(a.X + lo * r.X, a.Y + lo * r.Y));
            return new(IntersectionKind.CollinearOverlap);
        }
        double t = Cross(q, s) / cross;
        double u = Cross(q, r) / cross;
        if (t < -Epsilon || t > 1 + Epsilon || u < -Epsilon || u > 1 + Epsilon)
            return new(IntersectionKind.None);
        t = Math.Clamp(t, 0, 1);
        return new(IntersectionKind.Point, new(a.X + t * r.X, a.Y + t * r.Y));
    }
}
public sealed record StreetJunction(WorldPoint Location, IReadOnlyList<string> Corridors);
public sealed record StreetNetworkAnalysis(IReadOnlyList<StreetJunction> Junctions, IReadOnlyList<string> Diagnostics);
public static class StreetNetworkAnalyzer
{
    public static StreetNetworkAnalysis Analyze(IReadOnlyList<StreetCorridor> corridors)
    {
        var junctions = new List<StreetJunction>();
        var diagnostics = new List<string>();
        var events = new List<(WorldPoint Point, string A, string B)>();
        for (int i = 0; i < corridors.Count; i++)
        {
            corridors[i].Centerline.Validate();
            corridors[i].Profile.Validate();
            for (int j = i + 1; j < corridors.Count; j++)
                for (int a = 1; a < corridors[i].Centerline.Points.Count; a++)
                    for (int b = 1; b < corridors[j].Centerline.Points.Count; b++)
                    {
                        var result = PlanarIntersections.Intersect(
                            corridors[i].Centerline.Points[a - 1], corridors[i].Centerline.Points[a],
                            corridors[j].Centerline.Points[b - 1], corridors[j].Centerline.Points[b]);
                        if (result.Kind == IntersectionKind.CollinearOverlap)
                            diagnostics.Add($"COLLINEAR_OVERLAP:{corridors[i].Id}:{corridors[j].Id}");
                        else if (result.Point is { } point)
                            events.Add((point, corridors[i].Id, corridors[j].Id));
                    }
        }
        foreach (var group in events.GroupBy(e => (Math.Round(e.Point.X, 6), Math.Round(e.Point.Y, 6))))
            junctions.Add(new(group.First().Point,
                group.SelectMany(e => new[] { e.A, e.B }).Distinct(StringComparer.Ordinal).OrderBy(v => v).ToArray()));
        return new(junctions.OrderBy(j => j.Location.X).ThenBy(j => j.Location.Y).ToArray(), diagnostics);
    }
}
