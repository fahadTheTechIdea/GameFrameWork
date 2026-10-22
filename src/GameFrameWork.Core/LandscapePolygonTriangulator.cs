using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Deterministic ear clipping of simple concave landscape polygons into nonoverlapping
/// triangles. Supports authored irregular beds without guessing object locations or shapes.
/// Does not perform polygon offsetting, holes or self-intersection repair.</summary>
public static class LandscapePolygonTriangulator
{
    public static IReadOnlyList<WorldPolygon> Triangulate(WorldPolygon shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        shape.Validate();
        const double epsilon = 1e-9;
        var points = shape.Vertices;
        var orientation = Math.Sign(shape.SignedArea());
        var remaining = Enumerable.Range(0, points.Count).ToList();
        var result = new List<WorldPolygon>();
        static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
            (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
        bool Inside(WorldPoint p, WorldPoint a, WorldPoint b, WorldPoint c)
        {
            double ab = Cross(a,b,p)*orientation;
            double bc = Cross(b,c,p)*orientation;
            double ca = Cross(c,a,p)*orientation;
            return ab >= -epsilon && bc >= -epsilon && ca >= -epsilon;
        }
        while (remaining.Count > 3)
        {
            bool clipped = false;
            for (int i=0; i<remaining.Count; i++)
            {
                int previous = remaining[(i+remaining.Count-1)%remaining.Count];
                int current = remaining[i];
                int next = remaining[(i+1)%remaining.Count];
                var a=points[previous]; var b=points[current]; var c=points[next];
                if (Cross(a,b,c)*orientation <= epsilon) continue;
                bool containsOther = remaining.Any(index => index != previous && index != current &&
                    index != next && Inside(points[index],a,b,c));
                if (containsOther) continue;
                result.Add(new WorldPolygon([a,b,c]));
                remaining.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) throw new InvalidOperationException(
                "Polygon cannot be triangulated within numeric tolerance; revise approved geometry.");
        }
        result.Add(new WorldPolygon(remaining.Select(i=>points[i]).ToArray()));
        double original = Math.Abs(shape.SignedArea());
        double covered = result.Sum(t=>Math.Abs(t.SignedArea()));
        if (Math.Abs(original-covered)> Math.Max(1e-7,original*1e-8))
            throw new InvalidOperationException("Triangulation failed area conservation.");
        return result;
    }
}
