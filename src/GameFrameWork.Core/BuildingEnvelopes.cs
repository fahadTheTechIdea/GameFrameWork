using GameFrameWork.Core.Geometry;
namespace GameFrameWork.Core.Geometry;
public sealed record BuildingEnvelopeRules(string BuildingType, double FrontSetbackMeters,
    double RearSetbackMeters, double SideSetbackMeters, double MinimumWidthMeters,
    double MinimumDepthMeters)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BuildingType)) throw new ArgumentException("Building type required.");
        if (new[] { FrontSetbackMeters, RearSetbackMeters, SideSetbackMeters }.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Setbacks must be finite and nonnegative.");
        if (!double.IsFinite(MinimumWidthMeters) || !double.IsFinite(MinimumDepthMeters) ||
            MinimumWidthMeters <= 0 || MinimumDepthMeters <= 0)
            throw new ArgumentException("Minimum footprint dimensions must be positive and finite.");
    }
}
public sealed record BuildingEnvelopeResult(WorldPolygon? Footprint, IReadOnlyList<string> Diagnostics)
{
    public bool IsFeasible => Footprint is not null && Diagnostics.Count == 0;
}
/// <summary>Street-fronted rectangular parcel envelope solver; does not silently approximate concave parcels.</summary>
public static class BuildingEnvelopePlanner
{
    private const double Tolerance = 1e-8;
    public static BuildingEnvelopeResult Plan(BuildableParcel parcel, BuildingEnvelopeRules rules)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentNullException.ThrowIfNull(rules);
        rules.Validate();
        parcel.Boundary.Validate();
        var vertices = parcel.Boundary.Vertices;
        var xs = vertices.Select(v => v.X).Distinct().Order().ToArray();
        var ys = vertices.Select(v => v.Y).Distinct().Order().ToArray();
        if (vertices.Count != 4 || xs.Length != 2 || ys.Length != 2)
            return new(null, new[] { "UNSUPPORTED_PARCEL_GEOMETRY" });
        double left = xs[0] + rules.SideSetbackMeters;
        double right = xs[1] - rules.SideSetbackMeters;
        bool frontSouth = Math.Abs(parcel.StreetAccess.Y - ys[1]) < Tolerance;
        bool frontNorth = Math.Abs(parcel.StreetAccess.Y - ys[0]) < Tolerance;
        if (!frontNorth && !frontSouth)
            return new(null, new[] { "STREET_FRONTAGE_NOT_RECOGNIZED" });
        if (parcel.StreetAccess.X < xs[0] - Tolerance || parcel.StreetAccess.X > xs[1] + Tolerance)
            return new(null, new[] { "STREET_ACCESS_OUTSIDE_FRONTAGE" });
        double top = ys[0] + (frontNorth ? rules.FrontSetbackMeters : rules.RearSetbackMeters);
        double bottom = ys[1] - (frontSouth ? rules.FrontSetbackMeters : rules.RearSetbackMeters);
        if (right - left < rules.MinimumWidthMeters - Tolerance ||
            bottom - top < rules.MinimumDepthMeters - Tolerance)
            return new(null, new[] { "INSUFFICIENT_BUILDABLE_ENVELOPE" });
        return new(new WorldPolygon(new[]
        {
            new WorldPoint(left, top), new WorldPoint(right, top),
            new WorldPoint(right, bottom), new WorldPoint(left, bottom)
        }), Array.Empty<string>());
    }
}
