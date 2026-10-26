using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Landscape production is a set of authored regions and derived geometry, not scattered props.</summary>
public sealed record LandscapeDesignGrammar(string ZoneId, string StyleId, string MaterialId,
    double BorderMeters, double BandWidthMeters, double GapMeters);
public sealed record LandscapeWorldFeature(string Id, string ZoneId, string MaterialId,
    WorldPolygon Boundary, string RuleId, string VisualRegionId);
public sealed record LandscapeWorldPlan(IReadOnlyList<LandscapeWorldFeature> Features,
    IReadOnlyList<string> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

/// <summary>
/// Converts semantic, mockup-derived garden zones into traceable physical planting beds.
/// Fails if a style or source region is missing. Does not choose positions randomly.
/// </summary>
public sealed class LandscapeWorldPlanner
{
    public LandscapeWorldPlan Generate(SemanticWorldPackage world,
        IReadOnlyList<LandscapeDesignGrammar> grammars)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(grammars);
        var diagnostics = new List<string>();
        var features = new List<LandscapeWorldFeature>();
        var mapping = new Dictionary<string, LandscapeDesignGrammar>(StringComparer.Ordinal);
        foreach (var grammar in grammars)
        {
            if (string.IsNullOrWhiteSpace(grammar.ZoneId) || !mapping.TryAdd(grammar.ZoneId, grammar))
                diagnostics.Add("DUPLICATE_OR_INVALID_GARDEN_GRAMMAR:" + grammar.ZoneId);
        }
        foreach (var zone in world.Landscape.OrderBy(z => z.Id, StringComparer.Ordinal))
        {
            if (!mapping.TryGetValue(zone.Id, out var grammar))
            {
                diagnostics.Add("GARDEN_GRAMMAR_MISSING:" + zone.Id);
                continue;
            }
            if (string.IsNullOrWhiteSpace(zone.SourceVisualRegionId) ||
                string.IsNullOrWhiteSpace(zone.DesignRuleId) ||
                string.IsNullOrWhiteSpace(grammar.StyleId) ||
                string.IsNullOrWhiteSpace(grammar.MaterialId))
            {
                diagnostics.Add("GARDEN_PROVENANCE_MISSING:" + zone.Id);
                continue;
            }
            var result = new LandscapeBandComposer().Compose(zone,
                new LandscapeBandStyle(grammar.StyleId, grammar.MaterialId,
                    grammar.BorderMeters, grammar.BandWidthMeters, grammar.GapMeters));
            diagnostics.AddRange(result.Diagnostics);
            foreach (var band in result.Bands)
                features.Add(new(band.Id, zone.Id, band.MaterialId, band.Shape,
                    band.RuleId, zone.SourceVisualRegionId));
        }
        foreach (var grammar in mapping.Values)
            if (!world.Landscape.Any(z => z.Id == grammar.ZoneId))
                diagnostics.Add("UNKNOWN_LANDSCAPE_ZONE:" + grammar.ZoneId);
        return diagnostics.Count == 0
            ? new(features, Array.Empty<string>())
            : new(Array.Empty<LandscapeWorldFeature>(), diagnostics);
    }
}
