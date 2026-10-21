using GameFrameWork.Core.Geometry;
namespace GameFrameWork.Core.Planning;

public enum PublicFeatureKind { Streetlight, StreetNameSign, Bench, Tree, Flowerbed, Grass }
public sealed record PublicRealmRules(double StreetlightSpacingMeters = 25, double SideOffsetMeters = 1,
    double MinimumClearanceMeters = 1.5)
{
    public void Validate()
    {
        if (!double.IsFinite(StreetlightSpacingMeters) || StreetlightSpacingMeters <= 0 ||
            !double.IsFinite(SideOffsetMeters) || SideOffsetMeters < 0 ||
            !double.IsFinite(MinimumClearanceMeters) || MinimumClearanceMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(StreetlightSpacingMeters));
    }
}
public sealed record PublicFeature(string Id, PublicFeatureKind Kind, WorldPoint Position,
    string ParentId, string Purpose, string RuleId, double ClearanceRadiusMeters);
public sealed record PublicRealmPlan(IReadOnlyList<PublicFeature> Features, IReadOnlyList<string> Diagnostics);
/// <summary>Rejects conflicting feature footprints and reserved no-build regions.</summary>
public sealed class SpatialReservationIndex
{
    private readonly List<(WorldPoint Point, double Radius)> _reserved = [];
    public bool TryReserve(WorldPoint point, double radius)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(radius) || radius < 0)
            throw new ArgumentException("Invalid spatial reservation.");
        if (_reserved.Any(r => r.Point.DistanceTo(point) < r.Radius + radius - 1e-8)) return false;
        _reserved.Add((point, radius));
        return true;
    }
}
/// <summary>First public-realm placement stage: context-derived lights and signs, not random props.</summary>
public sealed class PublicRealmPlanner
{
    public PublicRealmPlan Plan(IReadOnlyList<StreetCorridor> streets, PublicRealmRules rules,
        SpatialReservationIndex? reservations = null)
    {
        ArgumentNullException.ThrowIfNull(streets);
        rules.Validate();
        reservations ??= new();
        var features = new List<PublicFeature>();
        var diagnostics = new List<string>();
        foreach (var street in streets.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            street.Centerline.Validate();
            street.Profile.Validate();
            // Start/end street-name markers and lighting are anchored to measured centerlines.
            var points = street.Centerline.Points;
            double total = street.Centerline.LengthMeters();
            var distances = new SortedSet<double> { 0, total };
            for (int i = 1; i * rules.StreetlightSpacingMeters < total; i++)
                distances.Add(i * rules.StreetlightSpacingMeters);
            int index = 0;
            foreach (var distance in distances)
            {
                var (point, normal) = Sample(points, distance);
                double offset = street.Profile.TotalWidthMeters / 2 + rules.SideOffsetMeters;
                var sidePoint = point.Offset(normal.X * offset, normal.Y * offset);
                var kind = distance == 0 || distance == total
                    ? PublicFeatureKind.StreetNameSign : PublicFeatureKind.Streetlight;
                var clearance = rules.MinimumClearanceMeters;
                if (reservations.TryReserve(sidePoint, clearance))
                    features.Add(new($"{street.Id}-{kind}-{index}", kind, sidePoint, street.Id,
                        kind == PublicFeatureKind.Streetlight ? "illuminate street corridor" : "identify street approach",
                        "street-corridor-v1", clearance));
                else diagnostics.Add($"ACCESSORY_CONFLICT:{street.Id}:{index}");
                index++;
            }
        }
        return new(features, diagnostics);
    }
    private static (WorldPoint Point, WorldPoint Normal) Sample(IReadOnlyList<WorldPoint> points, double distance)
    {
        for (int i = 1; i < points.Count; i++)
        {
            var a = points[i - 1]; var b = points[i];
            var len = a.DistanceTo(b);
            if (distance <= len || i == points.Count - 1)
            {
                var t = Math.Clamp(distance / len, 0, 1);
                return (new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t),
                    new(-(b.Y - a.Y) / len, (b.X - a.X) / len));
            }
            distance -= len;
        }
        throw new InvalidOperationException("Invalid street centerline.");
    }
}
