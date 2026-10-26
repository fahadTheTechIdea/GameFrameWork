using WorldCreator.Core.Planning;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Core;

/// <summary>Checks exact polygon intersection for convex authored terrain fill primitives.
/// Overlap is rejected rather than estimating union coverage from summed areas.</summary>
public static class TerrainCoverageTopology
{
    private const double Epsilon = 1e-8;
    public static IReadOnlyList<string> Validate(WorldPolygon ground,
        IReadOnlyList<StyledTerrainPiece> pieces)
    {
        ArgumentNullException.ThrowIfNull(ground);
        ArgumentNullException.ThrowIfNull(pieces);
        ground.Validate();
        if (!ConvexParcelGeometry.IsConvex(ground))
            return ["GROUND_BOUNDARY_NOT_CONVEX"];
        var errors = new List<string>();
        foreach (var item in pieces)
        {
            if (!ConvexParcelGeometry.IsConvex(item.Geometry))
            {
                errors.Add("TERRAIN_FILL_NOT_CONVEX:" + item.Id);
                continue;
            }
            var total = Math.Abs(item.Geometry.SignedArea());
            var clipped = IntersectionArea(item.Geometry, ground);
            if (total - clipped > Math.Max(Epsilon, total * 1e-8))
                errors.Add("TERRAIN_FILL_OUTSIDE_GROUND:" + item.Id);
        }
        for (int i = 0; i < pieces.Count; i++)
        for (int j = i + 1; j < pieces.Count; j++)
            if (ConvexParcelGeometry.IsConvex(pieces[i].Geometry) &&
                ConvexParcelGeometry.IsConvex(pieces[j].Geometry) &&
                IntersectionArea(pieces[i].Geometry, pieces[j].Geometry) > Epsilon)
                errors.Add("TERRAIN_FILL_OVERLAP:" + pieces[i].Id + ":" + pieces[j].Id);
        return errors;
    }
    public static double IntersectionArea(WorldPolygon subject, WorldPolygon clip)
    {
        var vertices = subject.Vertices.ToList();
        var boundary = clip.Vertices;
        var orientation = Math.Sign(clip.SignedArea());
        for (int i = 0; i < boundary.Count && vertices.Count > 0; i++)
        {
            var a = boundary[i];
            var b = boundary[(i + 1) % boundary.Count];
            var input = vertices;
            vertices = [];
            for (int j = 0; j < input.Count; j++)
            {
                var current = input[j];
                var next = input[(j + 1) % input.Count];
                double dc = Cross(a, b, current) * orientation;
                double dn = Cross(a, b, next) * orientation;
                bool cInside = dc >= -Epsilon;
                bool nInside = dn >= -Epsilon;
                if (cInside != nInside)
                {
                    double t = dc / (dc - dn);
                    vertices.Add(new(current.X + t * (next.X-current.X),
                        current.Y + t * (next.Y-current.Y)));
                }
                if (nInside) vertices.Add(next);
            }
        }
        if (vertices.Count < 3) return 0;
        double sum = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i]; var b = vertices[(i+1)%vertices.Count];
            sum += a.X*b.Y - b.X*a.Y;
        }
        return Math.Abs(sum) / 2;
    }
    private static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
        (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
}
