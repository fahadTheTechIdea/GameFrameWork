using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>A declared species sequence along a deliberately authored path, not random placement.</summary>
public sealed record PathVegetationGrammar(string Id, string PathId,
    IReadOnlyList<string> SpeciesAssetIds, double SpacingMeters,
    double StartOffsetMeters, double EndOffsetMeters, string Purpose);
public sealed record PathVegetationResult(IReadOnlyList<PlannedVegetation> Plants,
    IReadOnlyList<string> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

/// <summary>Arc-length positions for hedge, avenue and forest-edge layouts.
/// Repeated vertices, invalid geometry, and absent asset identities fail closed.</summary>
public sealed class PathVegetationComposer
{
    public PathVegetationResult Generate(SemanticWorldPackage world,
        IReadOnlyList<PathVegetationGrammar> grammars)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(grammars);
        var issues = new List<string>();
        var plants = new List<PlannedVegetation>();
        var paths = world.Paths.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grammar in grammars.OrderBy(g => g.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(grammar.Id) || !seen.Add(grammar.Id) ||
                string.IsNullOrWhiteSpace(grammar.Purpose) ||
                grammar.SpeciesAssetIds is null || grammar.SpeciesAssetIds.Count == 0 ||
                grammar.SpeciesAssetIds.Any(string.IsNullOrWhiteSpace) ||
                !double.IsFinite(grammar.SpacingMeters) || grammar.SpacingMeters <= 0 ||
                !double.IsFinite(grammar.StartOffsetMeters) || grammar.StartOffsetMeters < 0 ||
                !double.IsFinite(grammar.EndOffsetMeters) || grammar.EndOffsetMeters < 0)
            {
                issues.Add("INVALID_PATH_VEGETATION_GRAMMAR:" + grammar.Id);
                continue;
            }
            if (!paths.TryGetValue(grammar.PathId, out var path))
            {
                issues.Add("PATH_VEGETATION_SOURCE_MISSING:" + grammar.Id);
                continue;
            }
            var vertices = path.Geometry.Points;
            if (vertices.Count < 2)
            {
                issues.Add("PATH_VEGETATION_INVALID_GEOMETRY:" + grammar.Id);
                continue;
            }
            var lengths = new List<double>();
            double total = 0;
            for (int i = 1; i < vertices.Count; i++)
            {
                var a = vertices[i - 1]; var b = vertices[i];
                var length = Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));
                if (!double.IsFinite(length) || length <= 1e-9)
                {
                    issues.Add("PATH_VEGETATION_INVALID_GEOMETRY:" + grammar.Id);
                    break;
                }
                lengths.Add(length);
                total += length;
            }
            if (lengths.Count != vertices.Count - 1) continue;
            if (grammar.StartOffsetMeters + grammar.EndOffsetMeters > total)
            {
                issues.Add("PATH_VEGETATION_OFFSETS_EXCEED_LENGTH:" + grammar.Id);
                continue;
            }
            var count = 1 + (int)Math.Floor((total - grammar.EndOffsetMeters -
                grammar.StartOffsetMeters) / grammar.SpacingMeters);
            if (count > 10000)
            {
                issues.Add("PATH_VEGETATION_COUNT_LIMIT:" + grammar.Id);
                continue;
            }
            for (int i = 0; i < count; i++)
            {
                double distance = grammar.StartOffsetMeters + i * grammar.SpacingMeters;
                int section = 0;
                while (section < lengths.Count - 1 && distance > lengths[section])
                {
                    distance -= lengths[section];
                    section++;
                }
                double fraction = distance / lengths[section];
                var a = vertices[section]; var b = vertices[section + 1];
                plants.Add(new(grammar.Id + "-" + i, grammar.PathId,
                    grammar.SpeciesAssetIds[i % grammar.SpeciesAssetIds.Count],
                    new WorldPoint(a.X + (b.X-a.X)*fraction,
                        a.Y + (b.Y-a.Y)*fraction), grammar.Id, grammar.Purpose));
            }
        }
        return issues.Count == 0 ? new(plants, []) : new([], issues);
    }
}
