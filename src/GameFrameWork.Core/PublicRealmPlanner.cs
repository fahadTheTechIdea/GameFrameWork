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
    string ParentId, string Purpose, string RuleId, double ClearanceRadiusMeters,
    VisualPlacementSource? VisualSource = null);
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
/// <summary>Public props follow authored visual rhythms and offsets. The former
/// distance-only default API fails explicitly rather than inventing lamps or signs.</summary>
public sealed class PublicRealmPlanner
{
    public PublicRealmPlan Plan(IReadOnlyList<StreetCorridor> streets, PublicRealmRules rules,
        SpatialReservationIndex? reservations = null)
    {
        ArgumentNullException.ThrowIfNull(streets);
        rules.Validate();
        return new([], ["MOCKUP_PUBLIC_REALM_DESIGN_REQUIRED"]);
    }

    public PublicRealmPlan Plan(MockupWorldDesign design,
        SpatialReservationIndex? reservations = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(design);
        token.ThrowIfCancellationRequested();
        if (design.SchemaVersion != 1 || design.Blueprint is null || design.Paths is null ||
            design.PublicRealmPatterns is null || design.PublicRealmPatterns.Any(p => p is null) ||
            design.Blueprint.SchemaVersion != 1 || string.IsNullOrWhiteSpace(design.Blueprint.MockupId) ||
            string.IsNullOrWhiteSpace(design.Blueprint.ApprovedRevision) || string.IsNullOrWhiteSpace(design.Blueprint.ArtStyleId))
            return new([], ["MOCKUP_PUBLIC_REALM_DESIGN_INVALID"]);
        reservations ??= new();
        var features = new List<PublicFeature>();
        var diagnostics = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pattern in design.PublicRealmPatterns.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (!ids.Add(pattern.Id) || !Enum.TryParse<PublicFeatureKind>(pattern.Kind, true, out var kind) || !Enum.IsDefined(kind))
            { diagnostics.Add("PUBLIC_REALM_PATTERN_KIND_OR_ID_INVALID:" + pattern.Id); continue; }
            var generated = new VisualPathPatternGenerator().Generate(design.Blueprint, design.Paths, pattern, token);
            diagnostics.AddRange(generated.Issues.Select(i => i.Code + ":" + i.ObjectId));
            if (features.Count + generated.Anchors.Count > 100000)
            { diagnostics.Add("PUBLIC_REALM_COUNT_LIMIT:"+pattern.Id); continue; }
            foreach (var anchor in generated.Anchors)
            {
                if (!reservations.TryReserve(anchor.PositionMeters, pattern.ClearanceRadiusMeters))
                { diagnostics.Add("ACCESSORY_CONFLICT:" + anchor.Id); continue; }
                features.Add(new(anchor.Id, kind, anchor.PositionMeters, pattern.PathId,
                    pattern.Purpose, pattern.RuleId, pattern.ClearanceRadiusMeters,
                    new(design.Blueprint.MockupId, design.Blueprint.ApprovedRevision,
                        design.Blueprint.ArtStyleId, anchor.RegionId, pattern.Id, anchor.PositionMeters)));
            }
        }
        return diagnostics.Count == 0 ? new(features, []) : new([], diagnostics);
    }
}
