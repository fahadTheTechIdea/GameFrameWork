using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

public enum VegetationPattern { BorderRow, OrchardGrid, Anchored }
public sealed record VegetationGrammar(string Id, string ZoneId, string SpeciesAssetId,
    VegetationPattern Pattern, double SpacingMeters, double MarginMeters, string Purpose,
    IReadOnlyList<string>? AnchorIds = null);
public sealed record PlannedVegetation(string Id, string ZoneId, string SpeciesAssetId,
    WorldPoint PositionMeters, string GrammarId, string Purpose);
public sealed record VegetationComposition(IReadOnlyList<PlannedVegetation> Plants,
    IReadOnlyList<string> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

/// <summary>
/// Generates deliberate vegetation arrangements within authored landscape zones.
/// The grammar determines exact layout; never samples vacant cells or random coordinates.
/// Rows/grids require rectangular plots; anchored layouts preserve exact positions in simple polygons.
/// </summary>
public sealed class VegetationCompositionPlanner
{
    public VegetationComposition Generate(SemanticWorldPackage world,
        IReadOnlyList<VegetationGrammar> grammars, CancellationToken token = default,
        IReadOnlyList<VisualAnchor>? anchors = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(grammars);
        token.ThrowIfCancellationRequested();
        var issues = new List<string>();
        var plants = new List<PlannedVegetation>();
        var zones = world.Landscape.ToDictionary(z => z.Id, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grammar in grammars.OrderBy(g => g.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (!ids.Add(grammar.Id) || string.IsNullOrWhiteSpace(grammar.Id) ||
                string.IsNullOrWhiteSpace(grammar.SpeciesAssetId) ||
                string.IsNullOrWhiteSpace(grammar.Purpose) ||
                !double.IsFinite(grammar.SpacingMeters) || grammar.SpacingMeters <= 0 ||
                !double.IsFinite(grammar.MarginMeters) || grammar.MarginMeters < 0 || !Enum.IsDefined(grammar.Pattern))
            {
                issues.Add("INVALID_VEGETATION_GRAMMAR:" + grammar.Id);
                continue;
            }
            if (!zones.TryGetValue(grammar.ZoneId, out var zone))
            {
                issues.Add("VEGETATION_ZONE_MISSING:" + grammar.Id);
                continue;
            }
            var vertices = zone.Boundary.Vertices;
            try { zone.Boundary.Validate(); }
            catch (ArgumentException) { issues.Add("VEGETATION_ZONE_INVALID:" + grammar.Id); continue; }
            if (grammar.Pattern == VegetationPattern.Anchored)
            {
                var anchored = ComposeAnchors(grammar, zone, anchors ?? [], token);
                issues.AddRange(anchored.Diagnostics);
                plants.AddRange(anchored.Plants);
                if (plants.Count > 10000) issues.Add("VEGETATION_COUNT_LIMIT:" + grammar.Id);
                continue;
            }
            if (grammar.AnchorIds is not null) { issues.Add("VEGETATION_UNEXPECTED_ANCHORS:" + grammar.Id); continue; }
            var xs = vertices.Select(p => p.X).Distinct().Order().ToArray();
            var ys = vertices.Select(p => p.Y).Distinct().Order().ToArray();
            if (vertices.Count != 4 || xs.Length != 2 || ys.Length != 2)
            {
                issues.Add("VEGETATION_ZONE_UNSUPPORTED:" + grammar.Id);
                continue;
            }
            double left = xs[0] + grammar.MarginMeters;
            double right = xs[1] - grammar.MarginMeters;
            double top = ys[0] + grammar.MarginMeters;
            double bottom = ys[1] - grammar.MarginMeters;
            if (left > right || top > bottom)
            {
                issues.Add("VEGETATION_ZONE_TOO_SMALL:" + grammar.Id);
                continue;
            }
            var points = new List<WorldPoint>();
            double requestedCols = 1 + Math.Floor((right-left)/grammar.SpacingMeters);
            double requestedRows = grammar.Pattern == VegetationPattern.BorderRow ? 1 :
                1 + Math.Floor((bottom-top)/grammar.SpacingMeters);
            if (!double.IsFinite(requestedCols*requestedRows) || requestedCols*requestedRows + plants.Count > 10000)
            {
                issues.Add("VEGETATION_COUNT_LIMIT:" + grammar.Id);
                continue;
            }
            int cols = (int)requestedCols, rows = (int)requestedRows;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                token.ThrowIfCancellationRequested();
                points.Add(new(left + col * grammar.SpacingMeters, top + row * grammar.SpacingMeters));
            }
            if (points.Count == 0)
                issues.Add("VEGETATION_NO_VALID_PATTERN:" + grammar.Id);
            for (int i = 0; i < points.Count; i++)
                plants.Add(new(grammar.Id + "-" + i, zone.Id, grammar.SpeciesAssetId,
                    points[i], grammar.Id, grammar.Purpose));
        }
        return issues.Count == 0 ? new(plants, []) : new([], issues);
    }

    /// <summary>Exact authored/captured anchors. Conflicts are diagnosed downstream, never relocated.</summary>
    public static VegetationComposition ComposeAnchors(VegetationGrammar grammar, LandscapeZone zone,
        IReadOnlyList<VisualAnchor> anchors, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(anchors);
        token.ThrowIfCancellationRequested();
        if (grammar.Pattern != VegetationPattern.Anchored || string.IsNullOrWhiteSpace(grammar.Id) ||
            grammar.ZoneId != zone.Id || string.IsNullOrWhiteSpace(grammar.SpeciesAssetId) || string.IsNullOrWhiteSpace(grammar.Purpose) ||
            !double.IsFinite(grammar.SpacingMeters) || grammar.SpacingMeters <= 0 || !double.IsFinite(grammar.MarginMeters) || grammar.MarginMeters < 0 ||
            grammar.AnchorIds is null || grammar.AnchorIds.Count is < 1 or > 10000 || grammar.AnchorIds.Any(string.IsNullOrWhiteSpace) ||
            grammar.AnchorIds.Distinct(StringComparer.Ordinal).Count() != grammar.AnchorIds.Count)
            return new([], ["INVALID_VEGETATION_ANCHORS:" + grammar.Id]);
        try { zone.Boundary.Validate(); }
        catch (ArgumentException) { return new([], ["VEGETATION_ZONE_INVALID:" + grammar.Id]); }
        var byId = anchors.GroupBy(a => a.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var plants = new List<PlannedVegetation>(); var issues = new List<string>();
        foreach (var id in grammar.AnchorIds)
        {
            token.ThrowIfCancellationRequested();
            if (!byId.TryGetValue(id, out var matches) || matches.Length != 1)
            { issues.Add("VEGETATION_ANCHOR_MISSING_OR_AMBIGUOUS:" + id); continue; }
            var anchor = matches[0]; var point = anchor.PositionMeters;
            if (anchor.RegionId != zone.SourceVisualRegionId || !double.IsFinite(point.X) || !double.IsFinite(point.Y) ||
                !zone.Boundary.ContainsPoint(point) || (grammar.MarginMeters > 0 &&
                    TooCloseToEdge(zone.Boundary, point, grammar.MarginMeters)))
            { issues.Add("VEGETATION_ANCHOR_OUTSIDE_ZONE:" + id); continue; }
            plants.Add(new(grammar.Id + "-" + id, zone.Id, grammar.SpeciesAssetId, point, grammar.Id, grammar.Purpose));
        }
        return issues.Count == 0 ? new(plants, []) : new([], issues);

        static bool TooCloseToEdge(WorldPolygon boundary, WorldPoint p, double margin)
        {
            for (int i=0;i<boundary.Vertices.Count;i++)
            {
                var a=boundary.Vertices[i];var b=boundary.Vertices[(i+1)%boundary.Vertices.Count];
                double dx=b.X-a.X,dy=b.Y-a.Y,t=Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy),0,1);
                if(p.DistanceTo(new(a.X+t*dx,a.Y+t*dy))+1e-9<margin)return true;
            }
            return false;
        }
    }
}
