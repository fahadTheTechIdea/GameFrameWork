using WorldCreator.Core.Geometry;
namespace WorldCreator.Core.Geometry;
public sealed record UrbanBlock(string Id, IReadOnlyList<WorldPoint> Boundary, double AreaSquareMeters);
public sealed record UrbanBlockResult(IReadOnlyList<UrbanBlock> Blocks, IReadOnlyList<string> Diagnostics);
/// <summary>
/// Extracts buildable interiors of axis-aligned rectangular loops.
/// Intentionally limited: arbitrary planar face traversal and road unions are later milestones.
/// </summary>
public static class UrbanBlockExtractor
{
    private const double Tolerance = 1e-7;
    public static UrbanBlockResult Extract(IReadOnlyList<StreetCorridor> corridors)
    {
        ArgumentNullException.ThrowIfNull(corridors);
        var diagnostics = new List<string>();
        var blocks = new List<UrbanBlock>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var horizontal = new List<(double Y, double MinX, double MaxX, double HalfWidth)>();
        var vertical = new List<(double X, double MinY, double MaxY, double HalfWidth)>();
        foreach (var road in corridors)
        {
            road.Centerline.Validate();
            road.Profile.Validate();
            var points = road.Centerline.Points;
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var b = points[i];
                var half = road.Profile.TotalWidthMeters / 2;
                if (Math.Abs(a.Y - b.Y) <= Tolerance)
                    horizontal.Add((a.Y, Math.Min(a.X, b.X), Math.Max(a.X, b.X), half));
                else if (Math.Abs(a.X - b.X) <= Tolerance)
                    vertical.Add((a.X, Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y), half));
                else diagnostics.Add($"NON_ORTHOGONAL_SEGMENT:{road.Id}:{i - 1}");
            }
        }
        var xs = vertical.Select(v => v.X).Distinct().Order().ToArray();
        var ys = horizontal.Select(h => h.Y).Distinct().Order().ToArray();
        for (int ix = 1; ix < xs.Length; ix++)
        for (int iy = 1; iy < ys.Length; iy++)
        {
            double x0 = xs[ix - 1], x1 = xs[ix], y0 = ys[iy - 1], y1 = ys[iy];
            bool top = horizontal.Any(h => Math.Abs(h.Y - y0) <= Tolerance && h.MinX <= x0 + Tolerance && h.MaxX >= x1 - Tolerance);
            bool bottom = horizontal.Any(h => Math.Abs(h.Y - y1) <= Tolerance && h.MinX <= x0 + Tolerance && h.MaxX >= x1 - Tolerance);
            bool left = vertical.Any(v => Math.Abs(v.X - x0) <= Tolerance && v.MinY <= y0 + Tolerance && v.MaxY >= y1 - Tolerance);
            bool right = vertical.Any(v => Math.Abs(v.X - x1) <= Tolerance && v.MinY <= y0 + Tolerance && v.MaxY >= y1 - Tolerance);
            if (!(top && bottom && left && right)) continue;
            var insetLeft = vertical.Where(v => Math.Abs(v.X - x0) <= Tolerance).Max(v => v.HalfWidth);
            var insetRight = vertical.Where(v => Math.Abs(v.X - x1) <= Tolerance).Max(v => v.HalfWidth);
            var insetTop = horizontal.Where(h => Math.Abs(h.Y - y0) <= Tolerance).Max(h => h.HalfWidth);
            var insetBottom = horizontal.Where(h => Math.Abs(h.Y - y1) <= Tolerance).Max(h => h.HalfWidth);
            double l = x0 + insetLeft, r = x1 - insetRight, t = y0 + insetTop, b = y1 - insetBottom;
            if (r <= l || b <= t)
            {
                diagnostics.Add($"NO_BUILDABLE_INTERIOR:{ix}:{iy}");
                continue;
            }
            var key = $"{ix}:{iy}";
            if (!seen.Add(key)) continue;
            blocks.Add(new($"block-{ix}-{iy}", new[]
            {
                new WorldPoint(l,t), new WorldPoint(r,t), new WorldPoint(r,b), new WorldPoint(l,b)
            }, (r-l)*(b-t)));
        }
        return new(blocks, diagnostics);
    }
}
