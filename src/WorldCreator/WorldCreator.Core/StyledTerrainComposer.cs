using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>A visual fill primitive. Triangles preserve the authored polygon without
/// guessing where decoration, terrain, or other objects should be placed.</summary>
public sealed record StyledTerrainPiece(string Id, string SourceRegionId, string StyleId,
    string MaterialId, WorldPolygon Geometry, string DesignRuleId);
public sealed record StyledTerrainComposition(IReadOnlyList<StyledTerrainPiece> Pieces,
    IReadOnlyList<string> Diagnostics);

public sealed record TerrainCompositionStyle(string Id, string MaterialId, string RuleId);

/// <summary>Turns approved concave/convex visual regions into exact terrain fill geometry.
/// A region has one intentional material/style; mixed materials require separate authored regions.
/// No random points or empty-space fallback is used.</summary>
public sealed class StyledTerrainComposer
{
    public StyledTerrainComposition Compose(ApprovedVisualWorldBlueprint blueprint,
        IReadOnlyDictionary<string, TerrainCompositionStyle> regionStyles)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(regionStyles);
        var pieces = new List<StyledTerrainPiece>();
        var diagnostics = new List<string>();
        if (blueprint.SchemaVersion != 1 || string.IsNullOrWhiteSpace(blueprint.MockupId) ||
            string.IsNullOrWhiteSpace(blueprint.ApprovedRevision) ||
            string.IsNullOrWhiteSpace(blueprint.ArtStyleId))
            return new([], ["APPROVED_MOCKUP_REQUIRED"]);
        foreach (var region in blueprint.Regions.OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            if (!regionStyles.TryGetValue(region.Id, out var style))
            {
                diagnostics.Add("REGION_STYLE_MISSING:" + region.Id);
                continue;
            }
            if (string.IsNullOrWhiteSpace(style.Id) || string.IsNullOrWhiteSpace(style.MaterialId) ||
                string.IsNullOrWhiteSpace(style.RuleId))
            {
                diagnostics.Add("REGION_STYLE_INVALID:" + region.Id);
                continue;
            }
            try
            {
                var triangles = LandscapePolygonTriangulator.Triangulate(region.Outline);
                for (int i = 0; i < triangles.Count; i++)
                    pieces.Add(new($"{region.Id}-fill-{i}",region.Id,style.Id,
                        style.MaterialId,triangles[i],style.RuleId));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                diagnostics.Add("REGION_GEOMETRY_INVALID:" + region.Id);
            }
        }
        foreach (var id in regionStyles.Keys)
            if (!blueprint.Regions.Any(r => r.Id == id))
                diagnostics.Add("UNKNOWN_STYLE_REGION:" + id);
        return diagnostics.Count == 0 ? new(pieces,[]) : new([],diagnostics);
    }
}
