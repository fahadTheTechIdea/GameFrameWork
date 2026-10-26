using WorldCreator.Core;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

/// <summary>Authored materials, planting grammars, and transport policy for an existing
/// semantic settlement. This is a renderer-neutral design assembly, not an art approval.</summary>
public sealed record CoordinatedSettlementSpecification(
    string MockupId,
    string MockupRevision,
    IReadOnlyDictionary<string, TerrainCompositionStyle> TerrainStyles,
    IReadOnlyList<LandscapeDesignGrammar> LandscapeGrammars,
    IReadOnlyList<VegetationGrammar> VegetationGrammars,
    SettlementTransportSpecification Transport);

public sealed record CoordinatedSettlementResult(
    DummyVillageWorldDocument? World, IReadOnlyList<string> Issues)
{
    public bool Valid => World is not null && Issues.Count == 0;
}

/// <summary>Runs a coherent settlement composition in dependency order:
/// approved mockup lineage, terrain, gardens, vegetation and controlled transport.
/// World geometry is authored elsewhere and is never guessed or relocated here.
/// Simulation documents remain non-production and require the separate GameArts
/// production gate before any production use.</summary>
public sealed class CoordinatedSettlementWorldBuilder
{
    public CoordinatedSettlementResult Build(
        DummyVillageWorldDocument source,
        SettlementDesignResult settlement,
        CoordinatedSettlementSpecification specification,
        WorldQualityRules qualityRules)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(qualityRules);
        if (source.ProductionApproved)
            return new(null, ["PRODUCTION_WORLD_REQUIRES_GAMEARTS_GATE"]);
        if (source.SemanticWorld.Base.MockupId != specification.MockupId ||
            source.SemanticWorld.Base.MockupRevision != specification.MockupRevision)
            return new(null, ["COORDINATED_MOCKUP_REVISION_MISMATCH"]);

        var semantic = source.SemanticWorld;
        var styleRegions = semantic.Terrain
            .Select(t => new VisualRegion(t.SourceVisualRegionId, t.Boundary, t.Kind))
            .ToArray();
        if (styleRegions.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != styleRegions.Length)
            return new(null, ["COORDINATED_TERRAIN_REGION_DUPLICATE"]);
        var blueprint = new ApprovedVisualWorldBlueprint(1, specification.MockupId,
            specification.MockupRevision, semantic.Base.ArtStyleId, styleRegions, []);
        var terrain = new StyledTerrainComposer().Compose(blueprint, specification.TerrainStyles);
        var landscape = new LandscapeWorldPlanner().Generate(semantic, specification.LandscapeGrammars);
        var vegetation = new VegetationCompositionPlanner().Generate(semantic, specification.VegetationGrammars);
        var issues = terrain.Diagnostics.Concat(landscape.Diagnostics)
            .Concat(vegetation.Diagnostics).ToArray();
        if (issues.Length != 0) return new(null, issues);

        var updated = source with
        {
            TerrainMaterials = terrain.Pieces,
            GardenBeds = landscape.Features,
            Vegetation = vegetation.Plants
        };
        var (integrated, integrationIssues) = new SettlementTransportWorldAssembler()
            .Assemble(updated, settlement, specification.Transport, qualityRules);
        return integrated is null ? new(null, integrationIssues) : new(integrated, []);
    }
}
