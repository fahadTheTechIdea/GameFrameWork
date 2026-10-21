namespace GameFrameWork.Core.Geometry;

/// <summary>A simple polygon without holes, in world meters. Coordinates are not image pixels.</summary>
public sealed record WorldPolygon(IReadOnlyList<WorldPoint> Vertices)
{
    public double SignedArea()
    {
        Validate();
        double sum = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i]; var b = Vertices[(i + 1) % Vertices.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return sum / 2;
    }
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Vertices);
        if (Vertices.Count < 3) throw new ArgumentException("Polygon needs three vertices.");
        for (int i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i]; var b = Vertices[(i + 1) % Vertices.Count];
            if (!double.IsFinite(a.X) || !double.IsFinite(a.Y) ||
                a.DistanceTo(b) <= 1e-8) throw new ArgumentException("Invalid polygon vertex or edge.");
        }
        double twice = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i]; var b = Vertices[(i + 1) % Vertices.Count];
            twice += a.X * b.Y - a.Y * b.X;
            for (int j = i + 1; j < Vertices.Count; j++)
            {
                if (j == i + 1 || i == 0 && j == Vertices.Count - 1) continue;
                var c = Vertices[j]; var d = Vertices[(j + 1) % Vertices.Count];
                if (PlanarIntersections.Intersect(a, b, c, d).Kind != IntersectionKind.None)
                    throw new ArgumentException("Polygon edges self-intersect.");
            }
        }
        if (Math.Abs(twice) <= 1e-8) throw new ArgumentException("Polygon area is zero.");
    }
}
public sealed record BuildableParcel(string Id, string BlockId, WorldPolygon Boundary,
    WorldPoint StreetAccess, string PlanningRule);
public sealed record ParcelSubdivision(IReadOnlyList<BuildableParcel> Parcels, IReadOnlyList<string> Diagnostics);

/// <summary>Frontage-based subdivision of a rectangular block. Does not claim arbitrary polygon subdivision.</summary>
public static class FrontageParcelPlanner
{
    public static ParcelSubdivision Subdivide(UrbanBlock block, double TargetFrontageMeters,
        double MinimumFrontageMeters)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (!double.IsFinite(TargetFrontageMeters) || !double.IsFinite(MinimumFrontageMeters) ||
            TargetFrontageMeters <= 0 || MinimumFrontageMeters <= 0 || MinimumFrontageMeters > TargetFrontageMeters)
            throw new ArgumentOutOfRangeException(nameof(TargetFrontageMeters));
        var polygon = new WorldPolygon(block.Boundary);
        polygon.Validate();
        if (polygon.Vertices.Count != 4) return new(Array.Empty<BuildableParcel>(), new[] { "UNSUPPORTED_NONRECTANGULAR_BLOCK" });
        var xs = polygon.Vertices.Select(v => v.X).Distinct().Order().ToArray();
        var ys = polygon.Vertices.Select(v => v.Y).Distinct().Order().ToArray();
        if (xs.Length != 2 || ys.Length != 2) return new(Array.Empty<BuildableParcel>(), new[] { "UNSUPPORTED_NONRECTANGULAR_BLOCK" });
        double width = xs[1] - xs[0], depth = ys[1] - ys[0];
        int count = (int)Math.Floor(width / TargetFrontageMeters);
        if (count < 1) count = 1;
        if (width / count < MinimumFrontageMeters)
            return new(Array.Empty<BuildableParcel>(), new[] { "INSUFFICIENT_FRONTAGE" });
        var parcels = new List<BuildableParcel>();
        for (int i = 0; i < count; i++)
        {
            double left = xs[0] + width * i / count;
            double right = xs[0] + width * (i + 1) / count;
            var shape = new WorldPolygon(new[]
            {
                new WorldPoint(left,ys[0]), new WorldPoint(right,ys[0]),
                new WorldPoint(right,ys[1]), new WorldPoint(left,ys[1])
            });
            parcels.Add(new($"{block.Id}-parcel-{i}", block.Id, shape,
                new((left + right) / 2, ys[0]), "rectangular-street-frontage-v1"));
        }
        return new(parcels, Array.Empty<string>());
    }
}
