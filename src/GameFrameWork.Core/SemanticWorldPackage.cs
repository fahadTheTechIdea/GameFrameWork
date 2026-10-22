using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Non-rendering semantic geometry derived from approved visual design.</summary>
public sealed record TerrainRegion(string Id, string Kind, WorldPolygon Boundary,
    string SourceVisualRegionId, string DesignRuleId);
public sealed record PlannedWorldPath(string Id, string Kind, WorldPolyline Geometry,
    string SourceVisualPathId, string DesignRuleId);
public sealed record LandscapeZone(string Id, string Kind, WorldPolygon Boundary,
    string SourceVisualRegionId, string DesignRuleId);

public sealed record SemanticWorldPackage(WorldAssemblyPackage Base,
    IReadOnlyList<TerrainRegion> Terrain,
    IReadOnlyList<PlannedWorldPath> Paths,
    IReadOnlyList<LandscapeZone> Landscape);

public sealed record SemanticWorldResult(SemanticWorldPackage? Package,
    IReadOnlyList<string> Errors)
{
    public bool Valid => Package is not null && Errors.Count == 0;
}

/// <summary>Preserves mockup geography while validating all non-object features.</summary>
public sealed class SemanticWorldBuilder
{
    public SemanticWorldResult Build(WorldAssemblyPackage package,
        ApprovedVisualWorldBlueprint visual,
        IReadOnlyList<TerrainRegion> terrain,
        IReadOnlyList<PlannedWorldPath> paths,
        IReadOnlyList<LandscapeZone> landscape)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(visual);
        var errors = new List<string>();
        if (package.MockupId != visual.MockupId ||
            package.MockupRevision != visual.ApprovedRevision ||
            package.ArtStyleId != visual.ArtStyleId)
            errors.Add("VISUAL_SOURCE_MISMATCH");
        var regionIds = visual.Regions.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        void InspectPolygon(string id, string source, string rule, WorldPolygon polygon)
        {
            if (string.IsNullOrWhiteSpace(id) || !used.Add(id)) errors.Add("DUPLICATE_SEMANTIC_ID:" + id);
            if (!regionIds.Contains(source)) errors.Add("UNKNOWN_VISUAL_REGION:" + id);
            if (string.IsNullOrWhiteSpace(rule)) errors.Add("DESIGN_RULE_MISSING:" + id);
            try { polygon.Validate(); }
            catch (ArgumentException) { errors.Add("INVALID_POLYGON:" + id); }
        }
        foreach (var t in terrain) InspectPolygon(t.Id,t.SourceVisualRegionId,t.DesignRuleId,t.Boundary);
        foreach (var l in landscape) InspectPolygon(l.Id,l.SourceVisualRegionId,l.DesignRuleId,l.Boundary);
        foreach (var p in paths)
        {
            if (string.IsNullOrWhiteSpace(p.Id) || !used.Add(p.Id)) errors.Add("DUPLICATE_SEMANTIC_ID:" + p.Id);
            if (string.IsNullOrWhiteSpace(p.SourceVisualPathId) ||
                string.IsNullOrWhiteSpace(p.DesignRuleId)) errors.Add("PATH_PROVENANCE_MISSING:" + p.Id);
            try { p.Geometry.Validate(); }
            catch (ArgumentException) { errors.Add("INVALID_PATH:" + p.Id); }
        }
        return errors.Count == 0
            ? new(new(package,terrain.ToArray(),paths.ToArray(),landscape.ToArray()),[])
            : new(null,errors);
    }
}
