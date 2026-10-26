using WorldCreator.Core.Geometry;
namespace WorldCreator.Core.Geometry;
public enum StreetSurfaceKind { Carriageway, LeftCurb, RightCurb, LeftSidewalk, RightSidewalk }
public sealed record StreetSurface(string RoadId, int SegmentIndex, StreetSurfaceKind Kind,
    IReadOnlyList<WorldPoint> Polygon);
/// <summary>Creates segment bands and bevel joins along captured/sampled curves.
/// Returned polygons form a swept surface union, not a disjoint material mesh.</summary>
public static class StreetSurfaceBuilder
{
    public static IReadOnlyList<StreetSurface> Build(StreetCorridor corridor)
    {
        ArgumentNullException.ThrowIfNull(corridor);
        corridor.Centerline.Validate();
        corridor.Profile.Validate();
        var result = new List<StreetSurface>();
        var p = corridor.Profile;
        double carriage = p.LaneWidthMeters * p.LaneCount + p.MedianMeters;
        double leftCurb = p.CurbLeftMeters, rightCurb = p.CurbRightMeters;
        double leftWalk = p.SidewalkLeftMeters, rightWalk = p.SidewalkRightMeters;
        var bands = new (StreetSurfaceKind Kind, double Low, double High)[]
        {
            (StreetSurfaceKind.Carriageway, -carriage / 2, carriage / 2),
            (StreetSurfaceKind.LeftCurb, carriage / 2, carriage / 2 + leftCurb),
            (StreetSurfaceKind.RightCurb, -carriage / 2 - rightCurb, -carriage / 2),
            (StreetSurfaceKind.LeftSidewalk, carriage / 2 + leftCurb, carriage / 2 + leftCurb + leftWalk),
            (StreetSurfaceKind.RightSidewalk, -carriage / 2 - rightCurb - rightWalk, -carriage / 2 - rightCurb)
        };
        for (int i = 1; i < corridor.Centerline.Points.Count; i++)
        {
            var a = corridor.Centerline.Points[i - 1];
            var b = corridor.Centerline.Points[i];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double nx = -dy / len, ny = dx / len;
            foreach (var band in bands)
            {
                if (band.High - band.Low <= 0) continue;
                result.Add(new(corridor.Id, i - 1, band.Kind, new[]
                {
                    a.Offset(nx * band.Low, ny * band.Low),
                    b.Offset(nx * band.Low, ny * band.Low),
                    b.Offset(nx * band.High, ny * band.High),
                    a.Offset(nx * band.High, ny * band.High)
                }));
            }
        }
        int joinIndex = -1;
        for (int i = 1; i < corridor.Centerline.Points.Count-1; i++)
        {
            var a = corridor.Centerline.Points[i-1]; var at = corridor.Centerline.Points[i]; var b = corridor.Centerline.Points[i+1];
            double first = a.DistanceTo(at), second = at.DistanceTo(b);
            var n0 = new WorldPoint(-(at.Y-a.Y)/first,(at.X-a.X)/first);
            var n1 = new WorldPoint(-(b.Y-at.Y)/second,(b.X-at.X)/second);
            double cross = n0.X*n1.Y-n0.Y*n1.X;
            if (Math.Abs(cross) < 1e-10) continue;
            foreach (var band in bands.Where(band => band.High-band.Low > 1e-8))
            {
                if (band.Low < 0 && band.High > 0)
                {
                    Join([at,at.Offset(n0.X*band.Low,n0.Y*band.Low),at.Offset(n1.X*band.Low,n1.Y*band.Low)],band.Kind);
                    Join([at,at.Offset(n0.X*band.High,n0.Y*band.High),at.Offset(n1.X*band.High,n1.Y*band.High)],band.Kind);
                }
                else Join([at.Offset(n0.X*band.Low,n0.Y*band.Low),at.Offset(n1.X*band.Low,n1.Y*band.Low),
                    at.Offset(n1.X*band.High,n1.Y*band.High),at.Offset(n0.X*band.High,n0.Y*band.High)],band.Kind);
            }
            void Join(IReadOnlyList<WorldPoint> polygon,StreetSurfaceKind kind)
            {
                if (Math.Abs(SignedArea(polygon)) > 1e-8) result.Add(new(corridor.Id,joinIndex--,kind,polygon));
            }
        }
        return result;
    }
    public static double SignedArea(IReadOnlyList<WorldPoint> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        if (polygon.Count < 3) throw new ArgumentException("Polygon requires at least three points.");
        double twice = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
            twice += a.X * b.Y - b.X * a.Y;
        }
        return twice / 2;
    }
}
