using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Worlds;

public sealed record SettlementDesignResult(WorldDesignPlan Design, SettlementPlan Settlement,
    IReadOnlyList<StreetCorridor> Streets, PublicRealmPlan PublicRealm, IReadOnlyList<string> Diagnostics,
    IReadOnlyList<LandscapeZone>? Landscape = null, IReadOnlyList<PlannedVegetation>? Vegetation = null,
    IReadOnlyList<PlannedInfrastructure>? Infrastructure = null, WorldSitingAssessment? Siting = null);

/// <summary>Coordinates mockup-owned streets, typed building sites, public props and
/// vegetation through the existing visual and semantic acceptance contracts.</summary>
public sealed class SettlementDesignPipeline(ISettlementCatalogRegistry catalogs)
{
    public SettlementDesignResult Build(WorldRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var settlement = new SettlementPlanner(catalogs).Plan(request, token);
        var design = settlement.Design ?? new WorldDesignPlan("unresolved-mockup-design",
            new(request.Width,request.Height), []);
        var streets = settlement.Streets ?? [];
        var diagnostics = settlement.Diagnostics.Select(d => d.Code+":"+d.Message).ToList();
        if (diagnostics.Count != 0) return new(design, settlement, streets, new([], []), diagnostics);
        var input = request.Design!;
        var visual = input.Blueprint;
        var publicRealm = new PublicRealmPlanner().Plan(input, token: token);
        diagnostics.AddRange(publicRealm.Diagnostics);
        var objects = design.Objects.ToList();
        var infrastructure = catalogs.Resolve(input.Catalog!).Infrastructure.Plan(input,token);
        diagnostics.AddRange(infrastructure.Issues);
        foreach (var structure in infrastructure.Structures)
            objects.Add(new(structure.Id,structure.DefinitionId,
                WorldRasterGeometry.Project(structure.VisualSource.PositionMeters,input.MetersPerCell),
                structure.Purpose,structure.RuleId,design.Id,VisualSource:structure.VisualSource));
        var footprints = settlement.Buildings.ToDictionary(b => b.Id,
            b => new BuildingEnvelopeResult(b.VisualSource!.Footprint, []), StringComparer.Ordinal);
        var siteFeatures = settlement.SiteFeatures ?? [];
        diagnostics.AddRange(SurfaceClearanceValidator.Validate(input,footprints.Values.Select(f=>f.Footprint!).ToArray(),token).Select(i=>i.Code+":"+i.Message));
        var zones = (input.LandscapeZones ?? []).ToList();
        var grammars = (input.VegetationGrammars ?? []).ToList();
        foreach (var feature in siteFeatures.Where(f => IsGreen(f.Kind)))
        {
            zones.Add(new(feature.Id, feature.Kind.ToString(), feature.Boundary,
                feature.SourceVisualRegionId, feature.RuleId));
            if (feature.SpeciesAssetId is not null)
                grammars.Add(new(feature.Id+"-planting", feature.Id, feature.SpeciesAssetId,
                    VegetationPattern.OrchardGrid, feature.PlantSpacingMeters, feature.PlantMarginMeters, feature.Purpose));
        }
        if (zones.Select(z => z.Id).Distinct(StringComparer.Ordinal).Count() != zones.Count)
            diagnostics.Add("LANDSCAPE_ZONE_ID_CONFLICT");
        if (diagnostics.Count != 0) return new(design,settlement,streets,publicRealm,diagnostics);
        var siting=new StructureSitingPlanner(catalogs).Evaluate(input,objects,siteFeatures,zones,token,publicRealm.Features);
        diagnostics.AddRange(siting.Rejections);
        if (diagnostics.Count!=0) return new(design,settlement,streets,publicRealm,diagnostics,zones,[],infrastructure.Structures,siting);
        var semantic = new SemanticWorldPackage(new(1,design.Id,visual.MockupId,visual.ApprovedRevision,
            visual.ArtStyleId,design.Objects,[]), [], input.Paths.Select(p =>
                new PlannedWorldPath(p.Id,p.Role,p.Centerline,p.Id,"authored-mockup-path")).ToArray(), zones);
        var vegetation = new VegetationCompositionPlanner().Generate(semantic, grammars, token, visual.Anchors);
        diagnostics.AddRange(vegetation.Diagnostics);
        var protectedSurfaces = streets.SelectMany(StreetSurfaceBuilder.Build)
            .Select(s => new VegetationProtectedFootprint(s.RoadId+"-surface-"+s.SegmentIndex+"-"+s.Kind, new(s.Polygon)))
            .Concat(siteFeatures.Where(s => !IsGreen(s.Kind)).Select(s => new VegetationProtectedFootprint(s.Id,s.Boundary)))
            .Concat(infrastructure.Structures.SelectMany(s => new[] { new VegetationProtectedFootprint(s.Id,s.Footprint) }
                .Concat(s.Reservations.Select(r => new VegetationProtectedFootprint(r.Id,r.Boundary)))))
            .Concat((siting.Reservations ?? []).Select((r,i)=>new VegetationProtectedFootprint(r.LinkId+"-utility-"+i,r.Boundary))).ToArray();
        foreach (var structure in infrastructure.Structures)
        foreach (var reserved in new[] { structure.Footprint }.Concat(structure.Reservations.Select(r => r.Boundary)))
        {
            token.ThrowIfCancellationRequested();
            foreach (var building in footprints)
                if (InfrastructureGeometry.Overlap(reserved,building.Value.Footprint!))
                    diagnostics.Add("INFRASTRUCTURE_BUILDING_CONFLICT:"+structure.Id+":"+building.Key);
            foreach (var site in siteFeatures)
                if (InfrastructureGeometry.Overlap(reserved,site.Boundary))
                    diagnostics.Add("INFRASTRUCTURE_SITE_CONFLICT:"+structure.Id+":"+site.Id);
        }
        foreach (var zone in zones)
        {
            token.ThrowIfCancellationRequested();
            foreach (var triangle in LandscapePolygonTriangulator.Triangulate(zone.Boundary))
                foreach (var protectedShape in footprints.Select(p =>
                    new VegetationProtectedFootprint(p.Key,p.Value.Footprint!)).Concat(protectedSurfaces))
                    if (TerrainCoverageTopology.IntersectionArea(triangle,protectedShape.Boundary) > 1e-8)
                        diagnostics.Add("GREEN_ZONE_USE_CONFLICT:"+zone.Id+":"+protectedShape.Id);
        }
        for (int i = 0; i < siteFeatures.Count; i++)
        {
            var site = siteFeatures[i];
            if (site.Kind is not (SiteFeatureKind.AccessWalkway or SiteFeatureKind.Forecourt))
                foreach (var surface in streets.SelectMany(StreetSurfaceBuilder.Build))
                    if (TerrainCoverageTopology.IntersectionArea(site.Boundary,new(surface.Polygon)) > 1e-8)
                        diagnostics.Add("BUILDING_SITE_ROAD_CONFLICT:"+site.Id+":"+surface.RoadId);
            foreach (var building in footprints)
                if (TerrainCoverageTopology.IntersectionArea(site.Boundary,building.Value.Footprint!) > 1e-8)
                    diagnostics.Add("BUILDING_SITE_FOOTPRINT_CONFLICT:"+site.Id+":"+building.Key);
            for (int j = 0; j < i; j++)
            {
                var other = siteFeatures[j];
                bool sharedEntrance = site.BuildingId == other.BuildingId &&
                    (site.Kind == SiteFeatureKind.Forecourt && other.Kind == SiteFeatureKind.AccessWalkway ||
                     other.Kind == SiteFeatureKind.Forecourt && site.Kind == SiteFeatureKind.AccessWalkway);
                if (!sharedEntrance && TerrainCoverageTopology.IntersectionArea(site.Boundary,other.Boundary) > 1e-8)
                    diagnostics.Add("BUILDING_SITE_ZONE_CONFLICT:"+site.Id+":"+other.Id);
            }
        }
        var protectedBuildings = footprints.Concat(protectedSurfaces.Select(s =>
            new KeyValuePair<string,BuildingEnvelopeResult>(s.Id,new(s.Boundary,[]))))
            .ToDictionary(p => p.Key,p => p.Value,StringComparer.Ordinal);
        var clearances = input.VegetationClearances ?? [];
        var accepted = new SpeciesAwareSemanticWorldGate().Accept(semantic,vegetation,new([],[]),
            protectedBuildings,[],publicRealm.Features.Select(f =>
                new VegetationClearanceAnchor(f.Id,f.Position,f.ClearanceRadiusMeters)).ToArray(),clearances,token);
        diagnostics.AddRange(accepted.Issues);
        var bySpecies = clearances.Where(c => c is not null && !string.IsNullOrWhiteSpace(c.AssetId))
            .GroupBy(c => c.AssetId,StringComparer.Ordinal).ToDictionary(g => g.Key,g => g.First(),StringComparer.Ordinal);
        foreach (var feature in publicRealm.Features)
        {
            token.ThrowIfCancellationRequested();
            foreach (var shape in footprints.Select(p => new VegetationProtectedFootprint(p.Key,p.Value.Footprint!)).Concat(protectedSurfaces))
                if (shape.Boundary.DistanceToPoint(feature.Position) <= feature.ClearanceRadiusMeters+1e-8)
                    diagnostics.Add("PUBLIC_REALM_CLEARANCE_CONFLICT:"+feature.Id+":"+shape.Id);
            var parent = visual.Regions.Single(r => r.Id == feature.VisualSource!.RegionId);
            var shapeSource = feature.VisualSource! with {
                Footprint = feature.ClearanceRadiusMeters > 0 ? Square(feature.Position,feature.ClearanceRadiusMeters) : null
            };
            if (shapeSource.Footprint?.Vertices.Any(p => !parent.Outline.ContainsPoint(p)) == true)
                diagnostics.Add("PUBLIC_REALM_EXTENT_OUTSIDE_REGION:"+feature.Id);
            objects.Add(new(feature.Id,feature.Kind.ToString(),WorldRasterGeometry.Project(feature.Position,input.MetersPerCell),
                feature.Purpose,feature.RuleId,design.Id,VisualSource:shapeSource));
        }
        foreach (var plant in vegetation.Plants)
        {
            token.ThrowIfCancellationRequested();
            if (!bySpecies.TryGetValue(plant.SpeciesAssetId,out var profile)) continue;
            var zone = zones.Single(z => z.Id == plant.ZoneId);
            var footprint = Square(plant.PositionMeters,profile.RadiusMeters);
            if (footprint.Vertices.Any(p => !zone.Boundary.ContainsPoint(p)))
                diagnostics.Add("VEGETATION_EXTENT_OUTSIDE_ZONE:"+plant.Id);
            objects.Add(new(plant.Id,"vegetation:"+plant.SpeciesAssetId,
                WorldRasterGeometry.Project(plant.PositionMeters,input.MetersPerCell),plant.Purpose,plant.GrammarId,design.Id,
                VisualSource:new(visual.MockupId,visual.ApprovedRevision,visual.ArtStyleId,
                    zone.SourceVisualRegionId,plant.GrammarId,plant.PositionMeters,footprint)));
        }
        var complete = design with { Objects = objects.OrderBy(o => o.Id,StringComparer.Ordinal).ToArray() };
        diagnostics.AddRange(new WorldDesignValidator().Validate(complete,token).Issues.Select(i => i.Code+":"+i.ObjectId));
        return new(complete,settlement,streets,publicRealm,diagnostics.Distinct(StringComparer.Ordinal).ToArray(),zones,vegetation.Plants,infrastructure.Structures,siting);
    }

    internal static bool IsGreen(SiteFeatureKind kind) => kind is SiteFeatureKind.Garden or SiteFeatureKind.GreenBuffer or SiteFeatureKind.Lawn;
    internal static WorldPolygon Square(WorldPoint p,double radius) => new([
        new(p.X-radius,p.Y-radius),new(p.X+radius,p.Y-radius),new(p.X+radius,p.Y+radius),new(p.X-radius,p.Y+radius)]);
}
