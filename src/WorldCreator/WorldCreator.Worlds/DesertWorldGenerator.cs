using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

public sealed class WorldGenerationException(IReadOnlyList<string> issues)
    : InvalidOperationException("Mockup world generation rejected: " + string.Join("; ", issues))
{
    public IReadOnlyList<string> Issues { get; } = issues;
}

/// <summary>Compiles authored terrain, computed building sites and landscape grammars.
/// The raster is an adapter of validated continuous design, never a source of placement.</summary>
public sealed class DesertWorldGenerator(ISettlementCatalogRegistry catalogs) : IWorldGenerator
{
    public Task<WorldDocument> GenerateAsync(WorldRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Design is not null)
        {
            try { request=request with { Design=new FacilityCompositionPlanner(catalogs).Expand(request.Design,cancellationToken) }; }
            catch (ArgumentException ex) { throw new WorldGenerationException([ex.Message]); }
        }
        if (request.Biome != Biome.Desert) throw new NotSupportedException("The current world compiler targets the configured desert profile.");
        var terrain = new MockupTerrainComposer(catalogs).Compose(request,cancellationToken);
        if (terrain.Issues.Count != 0) throw new WorldGenerationException(terrain.Issues);
        var result = new SettlementDesignPipeline(catalogs).Build(request,cancellationToken);
        if (result.Diagnostics.Count != 0) throw new WorldGenerationException(result.Diagnostics);
        var objects = new SettlementDesignGate().Compile(result,cancellationToken);
        var input = request.Design!;
        double scale = input.MetersPerCell;
        var bounds = new GridBounds(request.Width,request.Height);
        var cells = terrain.Cells.ToArray();
        var issues = new HashSet<string>(StringComparer.Ordinal);
        var roadCells = new HashSet<GridPoint>();
        var occupied = new Dictionary<GridPoint,string>();
        var siteCells = new Dictionary<GridPoint,List<BuildingSiteFeature>>();
        var greenCells = new Dictionary<GridPoint,string>();
        var infrastructureCells = new Dictionary<GridPoint,PlannedInfrastructure>();
        var infrastructureLayers = new List<InfrastructureRasterCell>();
        foreach (var structure in result.Infrastructure ?? [])
            foreach (var cell in WorldRasterGeometry.CoveredCells(structure.Footprint,bounds,scale,cancellationToken))
            {
                if (!infrastructureCells.TryAdd(cell,structure)) issues.Add("INFRASTRUCTURE_RASTER_CONFLICT:"+structure.Id);
                var underlying = terrain.Cells[cell.Y*request.Width+cell.X];
                bool expectedObstacle = underlying.SourceVisualRegionId == structure.ObstacleRegionId;
                if (underlying.Kind is CellKind.Water or CellKind.Rock && !expectedObstacle)
                    issues.Add("INFRASTRUCTURE_UNEXPECTED_TERRAIN:"+structure.Id);
                if (underlying.Kind == CellKind.Rock && !structure.IsSubsurface)
                    issues.Add("INFRASTRUCTURE_ROCK_CONFLICT:"+structure.Id);
            }

        foreach (var street in result.Streets)
        {
            var path = input.Paths.Single(p => p.Id == street.Id);
            foreach (var surface in StreetSurfaceBuilder.Build(street))
                foreach (var cell in WorldRasterGeometry.CoveredCells(new(surface.Polygon),bounds,scale,cancellationToken))
                    Road(cell,path.RegionId,street.Id);
        }
        // Keep very narrow authored roads connected even when their centers lie on
        // cell boundaries. Raster overlap is diagnosed, never used to relocate objects.
        foreach (var road in result.Settlement.Roads)
            foreach (var cell in road.Cells())
                Road(cell,input.Paths.Single(p => p.Id == road.SourceVisualPathId).RegionId,road.SourceVisualPathId!);

        foreach (var entry in infrastructureCells)
        {
            var cell = entry.Key; var structure = entry.Value;
            bool passage = structure.CarriesRoute && roadCells.Contains(cell);
            var alignment = input.Paths.Single(p=>p.Id==structure.AlignmentPathId).Centerline.Points;
            var center = new WorldPoint((cell.X+.5)*scale,(cell.Y+.5)*scale);
            double lateral = alignment[0].X == alignment[1].X ? Math.Abs(center.X-alignment[0].X) : Math.Abs(center.Y-alignment[0].Y);
            bool deck = structure.PassageMode == "pedestrian" && !structure.CarriesRoute && lateral <= structure.PassageWidthMeters/2;
            deck |= structure.AccessLinks.Any(link => DistanceToSegment(center,link.Centerline.Points[0],link.Centerline.Points[1]) <= link.WidthMeters/2);
            bool channel = structure.PassageMode == "waterway" && lateral <= structure.PassageWidthMeters/2;
            passage |= deck;
            infrastructureLayers.Add(new(structure.Id,terrain.Cells[cell.Y*request.Width+cell.X],
                structure.CarriesRoute && passage,passage || channel ? structure.PassageMode : null));
            Paint(cell,channel ? CellKind.Water : passage ? CellKind.Road : CellKind.Structure,!passage,
                passage ? 1 : int.MaxValue,structure.IsSubsurface ? -1 : 2,
                structure.VisualSource.RegionId,structure.RuleId);
        }
        foreach (var structure in result.Infrastructure ?? [])
            if (!infrastructureLayers.Any(c=>c.InfrastructureId==structure.Id && c.UnderlyingTerrain.SourceVisualRegionId==structure.ObstacleRegionId))
                issues.Add("INFRASTRUCTURE_OBSTACLE_BELOW_RASTER_RESOLUTION:"+structure.Id);

        foreach (var feature in result.Settlement.SiteFeatures ?? [])
            if (!SettlementDesignPipeline.IsGreen(feature.Kind))
                foreach (var cell in WorldRasterGeometry.CoveredCells(feature.Boundary,bounds,scale,cancellationToken))
                {
                    if (infrastructureCells.ContainsKey(cell)) issues.Add("SITE_INFRASTRUCTURE_RASTER_CONFLICT:"+feature.Id);
                    if (cells[cell.Y*request.Width+cell.X].Kind is CellKind.Rock or CellKind.Water)
                        issues.Add("SITE_TERRAIN_CONFLICT:"+feature.Id);
                    Paint(cell,CellKind.Road,false,1,0,feature.SourceVisualRegionId,feature.RuleId);
                    if (!siteCells.TryGetValue(cell,out var entries)) siteCells[cell] = entries = [];
                    entries.Add(feature);
                }
        foreach (var zone in (result.Landscape ?? []).Where(z=>z.Role==LandscapeZoneRole.GroundCover))
            foreach (var triangle in LandscapePolygonTriangulator.Triangulate(zone.Boundary))
                foreach (var cell in WorldRasterGeometry.CoveredCells(triangle,bounds,scale,cancellationToken))
                {
                    if (infrastructureCells.ContainsKey(cell)) issues.Add("GREEN_ZONE_INFRASTRUCTURE_RASTER_CONFLICT:"+zone.Id);
                    if (roadCells.Contains(cell)) issues.Add("GREEN_ZONE_ROAD_RASTER_CONFLICT:"+zone.Id);
                    if (siteCells.ContainsKey(cell)) issues.Add("GREEN_ZONE_SITE_RASTER_CONFLICT:"+zone.Id);
                    if (cells[cell.Y*request.Width+cell.X].Kind is CellKind.Rock or CellKind.Water)
                        issues.Add("GREEN_ZONE_TERRAIN_CONFLICT:"+zone.Id);
                    Paint(cell,CellKind.Grass,false,3,0,zone.SourceVisualRegionId,zone.DesignRuleId);
                    greenCells.TryAdd(cell,zone.Id);
                }
        var plants = (result.Vegetation ?? []).ToDictionary(p => p.Id,StringComparer.Ordinal);
        var species = (input.VegetationClearances ?? []).ToDictionary(p => p.AssetId,StringComparer.Ordinal);
        foreach (var item in result.Design.Objects.Where(o => o.ParentObjectId is null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((result.Infrastructure ?? []).Any(s => s.Id == item.Id)) continue;
            var source = item.VisualSource!;
            IEnumerable<GridPoint> footprint = source.Footprint is { } polygon
                ? WorldRasterGeometry.CoveredCells(polygon,bounds,scale,cancellationToken) : [item.Site];
            bool isPlant = plants.TryGetValue(item.Id,out var plant);
            bool blocks = !isPlant || species[plant!.SpeciesAssetId].BlocksMovement;
            foreach (var cell in footprint)
            {
                if (infrastructureCells.ContainsKey(cell)) issues.Add("OBJECT_INFRASTRUCTURE_RASTER_CONFLICT:"+item.Id);
                if (roadCells.Contains(cell)) issues.Add("OBJECT_ROAD_RASTER_CONFLICT:"+item.Id);
                if (siteCells.TryGetValue(cell,out var sites))
                    foreach (var site in sites)
                        if (site.BuildingId != item.Id || site.Kind is not (SiteFeatureKind.AccessWalkway or SiteFeatureKind.Forecourt))
                            issues.Add("OBJECT_SITE_RASTER_CONFLICT:"+item.Id+":"+site.Id);
                if (source.BuildingDefinitionId is not null && greenCells.TryGetValue(cell,out var green))
                    issues.Add("BUILDING_GREEN_RASTER_CONFLICT:"+item.Id+":"+green);
                if (occupied.TryGetValue(cell,out var other)) issues.Add("OBJECT_RASTER_CONFLICT:"+item.Id+":"+other);
                else occupied.Add(cell,item.Id);
                if (cells[cell.Y*request.Width+cell.X].Kind is CellKind.Rock or CellKind.Water)
                    issues.Add("OBJECT_TERRAIN_CONFLICT:"+item.Id);
                Paint(cell,isPlant ? blocks ? CellKind.Tree : CellKind.Grass : CellKind.Structure,
                    blocks,blocks ? int.MaxValue : 3,2,source.RegionId,item.DesignRuleId,
                    isPlant ? cells[cell.Y*request.Width+cell.X].SurfaceMaterialId : null);
            }
        }
        if (issues.Count != 0) throw new WorldGenerationException(issues.Order(StringComparer.Ordinal).ToArray());
        return Task.FromResult(new WorldDocument(1,request,cells,objects,result.Design,
            result.Settlement.SiteFeatures,result.Vegetation,result.Landscape,result.Infrastructure,infrastructureLayers,result.Siting,catalogs.Resolve(input.Catalog!).Provenance,input.PlannedFacilities));

        void Road(GridPoint cell,string region,string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = terrain.Cells[cell.Y*request.Width+cell.X];
            if (infrastructureCells.TryGetValue(cell,out var structure))
            {
                bool ownRoute = structure.CarriesRoute && structure.AlignmentPathId == path;
                bool connectedApproach = structure.Connections.Any(c => c.PathId == path) &&
                    original.Kind is not (CellKind.Rock or CellKind.Water);
                if (!ownRoute && !connectedApproach) issues.Add("ROAD_INFRASTRUCTURE_CONFLICT:"+path);
                if (original.Kind is CellKind.Rock or CellKind.Water &&
                    (!ownRoute || original.SourceVisualRegionId != structure.ObstacleRegionId))
                    issues.Add("ROAD_TERRAIN_CONFLICT:"+path);
            }
            else if (original.Kind is CellKind.Rock or CellKind.Water)
                issues.Add("ROAD_TERRAIN_CONFLICT:"+path);
            roadCells.Add(cell);
            Paint(cell,CellKind.Road,false,1,0,region,"mockup-path:"+path);
        }
        void Paint(GridPoint cell,CellKind kind,bool blocked,int cost,int layer,string region,string rule,string? material=null)
        {
            int index = cell.Y*request.Width+cell.X;
            cells[index] = cells[index] with { Kind = kind,BlocksMovement = blocked,
                TraversalCost = cost,RenderLayer = layer,SourceVisualRegionId = region,DesignRuleId = rule,SurfaceMaterialId=material };
        }
    }

    private static double DistanceToSegment(WorldPoint p,WorldPoint a,WorldPoint b)
    {
        double dx=b.X-a.X,dy=b.Y-a.Y;
        double t=Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy),0,1);
        return p.DistanceTo(new(a.X+t*dx,a.Y+t*dy));
    }
}
