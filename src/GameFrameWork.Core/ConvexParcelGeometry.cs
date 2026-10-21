using GameFrameWork.Core.Geometry;
namespace GameFrameWork.Core.Geometry;

/// <summary>Robust-enough convex polygon clipping for planned parcels; concave polygons are explicitly unsupported.</summary>
public static class ConvexParcelGeometry
{
    private const double Eps = 1e-8;
    private static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    public static bool IsConvex(WorldPolygon polygon)
    {
        polygon.Validate();
        double sign = 0;
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            double turn = Cross(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Count],
                polygon.Vertices[(i + 2) % polygon.Vertices.Count]);
            if (Math.Abs(turn) <= Eps) continue;
            if (sign != 0 && turn * sign < 0) return false;
            sign = Math.Sign(turn);
        }
        return true;
    }
    /// <summary>Splits a convex polygon by vertical line x=coordinate; halves are non-overlapping.</summary>
    public static (WorldPolygon Left, WorldPolygon Right) SplitVertical(WorldPolygon polygon, double coordinate)
    {
        if (!double.IsFinite(coordinate)) throw new ArgumentOutOfRangeException(nameof(coordinate));
        if (!IsConvex(polygon)) throw new NotSupportedException("Only convex polygons are supported.");
        var min = polygon.Vertices.Min(v => v.X);
        var max = polygon.Vertices.Max(v => v.X);
        if (coordinate <= min + Eps || coordinate >= max - Eps)
            throw new ArgumentOutOfRangeException(nameof(coordinate), "Split must lie inside polygon bounds.");
        var left = Clip(polygon.Vertices, coordinate, true);
        var right = Clip(polygon.Vertices, coordinate, false);
        return (new WorldPolygon(left), new WorldPolygon(right));
    }
    private static IReadOnlyList<WorldPoint> Clip(IReadOnlyList<WorldPoint> input, double x, bool keepLeft)
    {
        var output = new List<WorldPoint>();
        bool Inside(WorldPoint p) => keepLeft ? p.X <= x + Eps : p.X >= x - Eps;
        for (int i = 0; i < input.Count; i++)
        {
            var a = input[i]; var b = input[(i + 1) % input.Count];
            bool ia = Inside(a), ib = Inside(b);
            if (ia && ib) output.Add(b);
            else if (ia != ib)
            {
                double t = (x - a.X) / (b.X - a.X);
                var crossing = new WorldPoint(x, a.Y + (b.Y - a.Y) * t);
                output.Add(crossing);
                if (ib) output.Add(b);
            }
        }
        var clean = new List<WorldPoint>();
        foreach (var p in output)
            if (clean.Count == 0 || clean[^1].DistanceTo(p) > Eps) clean.Add(p);
        if (clean.Count > 1 && clean[0].DistanceTo(clean[^1]) <= Eps) clean.RemoveAt(clean.Count - 1);
        return clean;
    }
}
