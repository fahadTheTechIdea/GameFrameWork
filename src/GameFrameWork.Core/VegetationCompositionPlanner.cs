using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

public enum VegetationPattern { BorderRow, OrchardGrid }
public sealed record VegetationGrammar(string Id, string ZoneId, string SpeciesAssetId,
    VegetationPattern Pattern, double SpacingMeters, double MarginMeters, string Purpose);
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
/// This first slice supports rectangular plots; other shapes fail explicitly.
/// </summary>
public sealed class VegetationCompositionPlanner
{
    public VegetationComposition Generate(SemanticWorldPackage world,
        IReadOnlyList<VegetationGrammar> grammars)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(grammars);
        var issues = new List<string>();
        var plants = new List<PlannedVegetation>();
        var zones = world.Landscape.ToDictionary(z => z.Id, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grammar in grammars.OrderBy(g => g.Id, StringComparer.Ordinal))
        {
            if (!ids.Add(grammar.Id) || string.IsNullOrWhiteSpace(grammar.Id) ||
                string.IsNullOrWhiteSpace(grammar.SpeciesAssetId) ||
                string.IsNullOrWhiteSpace(grammar.Purpose) ||
                !double.IsFinite(grammar.SpacingMeters) || grammar.SpacingMeters <= 0 ||
                !double.IsFinite(grammar.MarginMeters) || grammar.MarginMeters < 0)
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
            zone.Boundary.Validate();
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
            int cols = 1 + (int)Math.Floor((right - left) / grammar.SpacingMeters);
            int rows = grammar.Pattern == VegetationPattern.BorderRow ? 1 :
                1 + (int)Math.Floor((bottom - top) / grammar.SpacingMeters);
            if ((long)cols * rows > 10000)
            {
                issues.Add("VEGETATION_COUNT_LIMIT:" + grammar.Id);
                continue;
            }
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
                points.Add(new(left + col * grammar.SpacingMeters,
                    top + row * grammar.SpacingMeters));
            if (points.Count == 0)
                issues.Add("VEGETATION_NO_VALID_PATTERN:" + grammar.Id);
            for (int i = 0; i < points.Count; i++)
                plants.Add(new(grammar.Id + "-" + i, zone.Id, grammar.SpeciesAssetId,
                    points[i], grammar.Id, grammar.Purpose));
        }
        return issues.Count == 0 ? new(plants, []) : new([], issues);
    }
}
