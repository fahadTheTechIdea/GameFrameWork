using GameFrameWork.Core.Geometry;
namespace GameFrameWork.Core.Geometry;
public enum StreetSurfaceKind { Carriageway, LeftCurb, RightCurb, LeftSidewalk, RightSidewalk }
public sealed record StreetSurface(string RoadId, int SegmentIndex, StreetSurfaceKind Kind,
    IReadOnlyList<WorldPoint> Polygon);
/// <summary>Creates per-segment rectangular bands. Does not yet merge bends or intersection corners.</summary>
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
