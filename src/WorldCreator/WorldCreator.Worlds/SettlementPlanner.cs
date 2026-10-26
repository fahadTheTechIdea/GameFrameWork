using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

/// <summary>Designs settlements from named mockup frontage regions. Region subdivision,
/// type/size selection and building envelopes are engine algorithms. No vacant-site search.</summary>
public sealed class SettlementPlanner(ISettlementCatalogRegistry catalogs)
{
    public SettlementPlan Plan(WorldRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request.Design is not null)
        {
            try { request=request with { Design=new FacilityCompositionPlanner(catalogs).Expand(request.Design,token) }; }
            catch (ArgumentException ex) { return new([],[],[new(ex.Message,"facility composition rejected")]); }
        }
        var invalid = new MockupDesignValidator(catalogs).Validate(request,token);
        if (invalid.Count != 0)
            return new([], [], invalid.Select(i => new PlacementDiagnostic(i.Code, i.ObjectId + ":" + i.Message)).ToArray());
        var input = request.Design!;
        var visual = input.Blueprint;
        double scale = input.MetersPerCell;
        var bounds = new GridBounds(request.Width, request.Height);
        var roads = new List<RoadSegment>();
        var streets = new List<StreetCorridor>();
        var buildings = new List<PlacementDecision>();
        var diagnostics = new List<PlacementDiagnostic>();
        var siteFeatures = new List<BuildingSiteFeature>();
        var anchors = new List<VisualAnchor>(visual.Anchors);
        var decisions = new List<VisualObjectDecision>();
        var details = new Dictionary<string, (VisualPlacementSource Source, SettlementRegionRule Rule, WorldPoint Entrance)>(StringComparer.Ordinal);
        string planId = visual.MockupId + "-world-design-v1";
        var paths = input.Paths.ToDictionary(p => p.Id, StringComparer.Ordinal);
        foreach (var rule in input.RoadRules.OrderBy(r => r.PathId, StringComparer.Ordinal))
        {
            var path = paths[rule.PathId];
            streets.Add(new(path.Id, path.Centerline, rule.Profile));
            for (int i = 1; i < path.Centerline.Points.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var a = WorldRasterGeometry.Project(path.Centerline.Points[i-1], scale);
                var b = WorldRasterGeometry.Project(path.Centerline.Points[i], scale);
                // A dense curve can have several source samples in one cell.
                // Retain the continuous path and its raster anchor; do not drop or move it.
                roads.Add(new(path.Centerline.Points.Count == 2 ? path.Id : $"{path.Id}-segment-{i-1}", a, b, rule.Class,path.Id));
            }
        }
        var catalog = input.BuildingCatalog.ToDictionary(d => d.Id, StringComparer.Ordinal);
        foreach (var rule in input.SettlementRules.OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var definition=catalogs.Resolve(input.Catalog!).Policy.Resolve(catalog[rule.Envelope.BuildingType],rule.Siting!.Technology);
            var region = visual.Regions.Single(r => r.Id == rule.RegionId);
            var points = region.Outline.Vertices;
            var xs = points.Select(p => p.X).Distinct().Order().ToArray();
            var ys = points.Select(p => p.Y).Distinct().Order().ToArray();
            if (points.Count != 4 || xs.Length != 2 || ys.Length != 2)
            { diagnostics.Add(new("SETTLEMENT_REGION_SHAPE_UNSUPPORTED", rule.Id)); continue; }
            var frontage = paths[rule.FrontagePathId].Centerline.Points;
            if (frontage.Count != 2 || frontage[0].Y != frontage[1].Y ||
                Math.Min(frontage[0].X, frontage[1].X) > xs[0] + 1e-8 ||
                Math.Max(frontage[0].X, frontage[1].X) < xs[1] - 1e-8 ||
                Math.Abs(frontage[0].Y-ys[0]) > 1e-8 && Math.Abs(frontage[0].Y-ys[1]) > 1e-8)
            { diagnostics.Add(new("SETTLEMENT_FRONTAGE_NOT_ON_REGION_EDGE", rule.Id)); continue; }
            double count = Math.Max(1, Math.Floor((xs[1]-xs[0])/rule.TargetFrontageMeters));
            if (!double.IsFinite(count) || count > 100000 || buildings.Count + count > 100000)
            { diagnostics.Add(new("SETTLEMENT_GRAMMAR_TOO_LARGE", rule.Id)); continue; }
            var parcels = FrontageParcelPlanner.Subdivide(new UrbanBlock(rule.Id, points,
                Math.Abs(region.Outline.SignedArea())), rule.TargetFrontageMeters, rule.MinimumFrontageMeters);
            diagnostics.AddRange(parcels.Diagnostics.Select(d => new PlacementDiagnostic(d, rule.Id)));
            foreach (var sourceParcel in parcels.Parcels)
            {
                token.ThrowIfCancellationRequested();
                var parcel = sourceParcel with { StreetAccess = new(sourceParcel.StreetAccess.X, frontage[0].Y) };
                var sized = new BuildingSizePlanner().Plan(parcel, rule.Envelope,
                    definition, rule.SizeVariantId, rule.RequiredCapacity, rule.Settlement);
                if (!sized.Valid)
                { diagnostics.AddRange(sized.Issues.Select(i => new PlacementDiagnostic(i, parcel.Id))); continue; }
                var roadProfile = input.RoadRules.Single(r => r.PathId == rule.FrontagePathId).Profile;
                double requiredRoadWidth = Math.Max(definition.MinimumRoadWidthMeters,
                    sized.Variant!.MinimumRoadWidthMeters);
                if (roadProfile.LaneWidthMeters*roadProfile.LaneCount + 1e-8 < requiredRoadWidth)
                { diagnostics.Add(new("BUILDING_ACCESS_ROAD_UNDERSIZED",parcel.Id)); continue; }
                var footprint = sized.Footprint!;
                var position = new WorldPoint(footprint.Vertices.Average(p => p.X), footprint.Vertices.Average(p => p.Y));
                bool frontNorth = Math.Abs(frontage[0].Y-ys[0]) <= 1e-8;
                var entrance = new WorldPoint(position.X, frontNorth
                    ? footprint.Vertices.Min(p => p.Y) : footprint.Vertices.Max(p => p.Y));
                string id = parcel.Id + "-building";
                var site = new BuildingSitePlanner().Plan(id, region.Id, rule.FrontagePathId,
                    parcel, footprint, definition);
                diagnostics.AddRange(site.Issues.Select(i => new PlacementDiagnostic(i, id)));
                siteFeatures.AddRange(site.Features);
                var source = new VisualPlacementSource(visual.MockupId, visual.ApprovedRevision, visual.ArtStyleId,
                    region.Id, rule.Id, position, footprint, rule.Envelope.BuildingType,
                    sized.Variant!.Id, sized.Variant.HeightMeters, sized.Variant.ServiceCapacity,rule.Facility);
                anchors.Add(new(id, region.Id, position, rule.Envelope.BuildingType));
                decisions.Add(new(id, rule.Envelope.BuildingType, id, rule.Purpose, rule.Id));
                details.Add(id, (source, rule, entrance));
                buildings.Add(new(id, rule.Envelope.BuildingType, WorldRasterGeometry.Project(position, scale),
                    rule.Purpose, rule.SettlementId, rule.Id, WorldRasterGeometry.Project(parcel.StreetAccess, scale), source));
            }
        }
        if (diagnostics.Count != 0) return new([], [], diagnostics);
        diagnostics.AddRange(StreetTopologyBuilder.Build(streets,token:token).Diagnostics.Select(d => new PlacementDiagnostic("ROAD_TOPOLOGY_INVALID",d)));
        var planned = new VisualWorldPlanner().Plan(visual with { Anchors = anchors }, decisions, planId, bounds, scale,token);
        if (!planned.IsValid)
            return new([], [], planned.Issues.Select(i => new PlacementDiagnostic(i.Code, i.ObjectId)).ToArray());
        var objects = new List<PlannedWorldObject>();
        foreach (var item in planned.Design!.Objects)
        {
            var detail = details[item.Id];
            objects.Add(item with { VisualSource = detail.Source });
            foreach (var part in detail.Rule.Components.OrderBy(c => c.Id, StringComparer.Ordinal))
                objects.Add(new(item.Id + "-" + part.Id, part.Kind, WorldRasterGeometry.Project(detail.Entrance, scale),
                    part.Purpose, detail.Rule.Id + ":" + part.Id, planId, item.Id, part.State, part.AnimationId,
                    detail.Source with { FeatureId = detail.Rule.Id + ":" + part.Id,
                        PositionMeters = detail.Entrance, Footprint = null,
                        BuildingDefinitionId = null, SizeVariantId = null, HeightMeters = null, ServiceCapacity = null }));
        }
        var design = planned.Design with { Objects = objects };
        diagnostics.AddRange(new WorldDesignValidator().Validate(design,token).Issues.Select(i => new PlacementDiagnostic(i.Code, i.ObjectId)));
        diagnostics.AddRange(SpatialValidation.Validate(bounds, roads, buildings));
        var surfaces = streets.SelectMany(StreetSurfaceBuilder.Build).ToArray();
        for (int i = 0; i < buildings.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var footprint = buildings[i].VisualSource!.Footprint!;
            foreach (var surface in surfaces)
                if (TerrainCoverageTopology.IntersectionArea(footprint, new(surface.Polygon)) > 1e-8)
                    diagnostics.Add(new("BUILDING_ROAD_FOOTPRINT_CONFLICT", buildings[i].Id + ":" + surface.RoadId));
            for (int j = 0; j < i; j++)
            {
                token.ThrowIfCancellationRequested();
                if (TerrainCoverageTopology.IntersectionArea(footprint, buildings[j].VisualSource!.Footprint!) > 1e-8)
                    diagnostics.Add(new("BUILDING_FOOTPRINT_CONFLICT", buildings[i].Id + ":" + buildings[j].Id));
            }
        }
        return diagnostics.Count == 0 ? new(roads, buildings, [], design, streets, siteFeatures) : new([], [], diagnostics);
    }
}
