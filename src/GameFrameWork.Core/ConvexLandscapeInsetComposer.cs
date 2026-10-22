using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Concentric, explicitly designed landscape borders following the shape of a convex zone.</summary>
public sealed record LandscapeInsetStyle(string Id, string MaterialId, double InsetMeters)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(MaterialId) ||
            !double.IsFinite(InsetMeters) || InsetMeters <= 0)
            throw new ArgumentException("Invalid landscape inset style.");
    }
}
public sealed record LandscapeInsetResult(WorldPolygon? Interior, IReadOnlyList<string> Diagnostics);

public sealed class ConvexLandscapeInsetComposer
{
    public LandscapeInsetResult Compose(LandscapeZone zone, LandscapeInsetStyle style)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(style);
        style.Validate();
        zone.Boundary.Validate();
        if (!ConvexParcelGeometry.IsConvex(zone.Boundary))
            return new(null, ["UNSUPPORTED_CONCAVE_ZONE:" + zone.Id]);
        var vertices = zone.Boundary.Vertices;
        bool ccw = zone.Boundary.SignedArea() > 0;
        var lines = new List<(WorldPoint P, WorldPoint D)>();
        for (int i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i]; var b = vertices[(i + 1) % vertices.Count];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            double nx = (ccw ? -dy : dy) / length * style.InsetMeters;
            double ny = (ccw ? dx : -dx) / length * style.InsetMeters;
            lines.Add((a.Offset(nx, ny), new(dx, dy)));
        }
        var result = new List<WorldPoint>();
        for (int i = 0; i < lines.Count; i++)
        {
            var a = lines[(i + lines.Count - 1) % lines.Count];
            var b = lines[i];
            double determinant = a.D.X * b.D.Y - a.D.Y * b.D.X;
            if (Math.Abs(determinant) < 1e-10)
                return new(null, ["PARALLEL_INSET_EDGES:" + zone.Id]);
            double rx = b.P.X - a.P.X, ry = b.P.Y - a.P.Y;
            double t = (rx * b.D.Y - ry * b.D.X) / determinant;
            result.Add(a.P.Offset(t * a.D.X, t * a.D.Y));
        }
        var polygon = new WorldPolygon(result);
        try
        {
            polygon.Validate();
            if (!ConvexParcelGeometry.IsConvex(polygon) ||
                Math.Sign(polygon.SignedArea()) != Math.Sign(zone.Boundary.SignedArea()) ||
                Math.Abs(polygon.SignedArea()) >= Math.Abs(zone.Boundary.SignedArea()))
                return new(null, ["INSET_EXCEEDS_ZONE:" + zone.Id]);
        }
        catch (ArgumentException)
        {
            return new(null, ["INSET_EXCEEDS_ZONE:" + zone.Id]);
        }
        return new(polygon, []);
    }
}
