using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

/// <summary>Deliberate orthogonal bridge, portal and shoreline design using the shared
/// catalog, terrain masks, paths and cross sections. Profiles are game design, not civil certification.</summary>
public sealed class ModernInfrastructureDesignStrategy : IInfrastructureDesignStrategy
{
    public InfrastructureDesignResult Plan(MockupWorldDesign design, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(design);
        token.ThrowIfCancellationRequested();
        var result = new List<PlannedInfrastructure>();
        var issues = new List<string>();
        var rules = design.InfrastructureRules ?? [];
        if (rules.Count > 1000) return new([], ["INFRASTRUCTURE_BUDGET_EXCEEDED"]);
        // Parent design collections are checked by MockupDesignValidator before this planner runs.
        var regions = design.Blueprint.Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var paths = design.Paths.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var catalog = design.BuildingCatalog.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var profiles = design.RoadRules.ToDictionary(r => r.PathId, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var terrainQuery = new MockupTerrainQuery(design);
        foreach (var rule in rules.OrderBy(r => r?.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            int before = issues.Count;
            void Fail(string code) => issues.Add(code + ":" + (rule?.Id ?? ""));
            if (rule is null || string.IsNullOrWhiteSpace(rule.Id) || !ids.Add(rule.Id) ||
                string.IsNullOrWhiteSpace(rule.RegionId) || !regions.TryGetValue(rule.RegionId, out var parent) ||
                string.IsNullOrWhiteSpace(rule.SettlementId) || string.IsNullOrWhiteSpace(rule.Purpose) ||
                string.IsNullOrWhiteSpace(rule.DefinitionId) || !catalog.TryGetValue(rule.DefinitionId, out var definition) ||
                string.IsNullOrWhiteSpace(rule.AlignmentPathId) || !paths.TryGetValue(rule.AlignmentPathId, out var alignment) ||
                string.IsNullOrWhiteSpace(rule.ObstacleRegionId) || !regions.TryGetValue(rule.ObstacleRegionId, out var obstacle) ||
                rule.Settlement is null || rule.RequiredCapacity < 0 ||
                !double.IsFinite(rule.ClearanceMeters) || rule.ClearanceMeters < 1)
            { Fail("INFRASTRUCTURE_RULE_INVALID"); continue; }
            try { rule.Settlement.Validate(); }
            catch (ArgumentException) { Fail("INFRASTRUCTURE_CONTEXT_INVALID"); continue; }
            bool crossing = definition.Id is "road-bridge" or "railway-bridge" or "pedestrian-bridge" or "road-tunnel";
            bool bankSpan = crossing || definition.Id == "dam";
            bool lockSite = definition.Id == "canal-lock";
            bool shore = definition.Id is "breakwater" or "quay" or "ferry-landing" or "jetty";
            bool tunnel = definition.Id == "road-tunnel";
            bool carriesRoute = crossing || definition.Id == "dam" && profiles.ContainsKey(alignment.Id);
            string mode = definition.Id switch { "railway-bridge" => "railway", "pedestrian-bridge" => "pedestrian", _ => "road" };
            if (definition.PlacementKind != (crossing ? BuildingPlacementKind.LinearInfrastructure : BuildingPlacementKind.WaterInfrastructure) ||
                !(bankSpan || lockSite || shore))
            { Fail("INFRASTRUCTURE_DEFINITION_UNSUPPORTED"); continue; }
            if (!definition.AllowedClasses.Contains(rule.Settlement.Class) ||
                definition.AllowedRoles is { } roles && !roles.Contains(rule.Settlement.Role))
            { Fail("INFRASTRUCTURE_CONTEXT_UNSUPPORTED"); continue; }
            var points = alignment.Centerline.Points;
            if (points.Count != 2 || points[0].X != points[1].X && points[0].Y != points[1].Y ||
                !ConvexParcelGeometry.IsConvex(parent.Outline) || !ConvexParcelGeometry.IsConvex(obstacle.Outline))
            { Fail("INFRASTRUCTURE_GEOMETRY_UNSUPPORTED"); continue; }
            var start = points[0]; var end = points[1];
            double length = start.DistanceTo(end), dx = (end.X-start.X)/length, dy = (end.Y-start.Y)/length;
            double demandValue = rule.RequiredCapacity > 0 ? rule.RequiredCapacity : Math.Max(definition.ServiceCapacity,
                Math.Ceiling(rule.Settlement.Population * definition.ScaleDemandPerResident));
            if (!double.IsFinite(demandValue) || demandValue > int.MaxValue)
            { Fail("INFRASTRUCTURE_CAPACITY_UNSUPPORTED"); continue; }
            int demand = (int)demandValue;
            double routeWidth = 0;
            if (carriesRoute)
            {
                if (alignment.Role != mode || !profiles.TryGetValue(alignment.Id, out var route))
                { Fail("INFRASTRUCTURE_ROUTE_PROFILE_REQUIRED"); continue; }
                routeWidth = route.Profile.TotalWidthMeters;
            }
            var variants = definition.SizeVariants ?? [];
            if (rule.SizeVariantId is not null && !variants.Any(v => v.Id == rule.SizeVariantId))
            { Fail("INFRASTRUCTURE_VARIANT_UNKNOWN"); continue; }
            var variant = variants.Where(v => (rule.SizeVariantId is null || v.Id == rule.SizeVariantId) &&
                    v.ServiceCapacity >= demand && v.DepthMeters+1e-8 >= length && v.WidthMeters+1e-8 >= routeWidth &&
                    (v.AllowedClasses is null || v.AllowedClasses.Contains(rule.Settlement.Class)))
                .OrderBy(v => v.WidthMeters*v.DepthMeters).ThenBy(v => v.Id,StringComparer.Ordinal).FirstOrDefault();
            if (variant is null) { Fail("INFRASTRUCTURE_SIZE_OR_CAPACITY_DOES_NOT_FIT"); continue; }
            double half = variant.WidthMeters/2;
            var footprint = Band(start,end,-half,half);
            double margin = Math.Max(Math.Max(rule.ClearanceMeters,variant.SiteClearanceMeters),variant.WidthMeters*.1);
            var reservations = new List<InfrastructureReservation> {
                new(rule.Id+"-left-edge",tunnel ? "portal-safety-edge" : shore ? "waterfront-service-edge" : "abutment-safety-edge",
                    Band(start,end,half,half+margin),"reserve structure edge and maintenance clearance"),
                new(rule.Id+"-right-edge",lockSite ? "lock-maintenance-edge" : "structure-safety-edge",
                    Band(start,end,-half-margin,-half),"keep structures and planting clear of the infrastructure")
            };
            if (bankSpan)
            {
                reservations.Add(new(rule.Id+"-start-approach",tunnel ? "portal-forecourt" : "bank-approach",
                    Band(start.Offset(-dx*margin,-dy*margin),start,-half-margin,half+margin),"reserve connected approach and inspection space"));
                reservations.Add(new(rule.Id+"-end-approach",tunnel ? "portal-forecourt" : "bank-approach",
                    Band(end,end.Offset(dx*margin,dy*margin),-half-margin,half+margin),"reserve connected approach and inspection space"));
            }
            if (footprint.Vertices.Concat(reservations.SelectMany(r => r.Boundary.Vertices)).Any(p => !parent.Outline.ContainsPoint(p)))
                Fail("INFRASTRUCTURE_EXTENT_OUTSIDE_REGION");
            var obstacleTerrain = design.TerrainRules.SingleOrDefault(t => t.RegionId == obstacle.Id);
            if (obstacleTerrain?.Kind != (tunnel ? CellKind.Rock : CellKind.Water)) Fail("INFRASTRUCTURE_OBSTACLE_KIND_INVALID");
            var middle = new WorldPoint((start.X+end.X)/2,(start.Y+end.Y)/2);
            if (bankSpan)
            {
                if (!obstacle.Outline.ContainsPoint(middle) || IsBlocked(start) || IsBlocked(end))
                    Fail("INFRASTRUCTURE_BANKS_OR_PORTALS_INVALID");
                // Full-width end faces must reach dry banks; a narrow centerline alone is insufficient.
                if (new[] { start.Offset(-dy*half,dx*half),start.Offset(dy*half,-dx*half),
                    end.Offset(-dy*half,dx*half),end.Offset(dy*half,-dx*half) }.Any(IsBlocked))
                    Fail("INFRASTRUCTURE_BANK_WIDTH_INVALID");
            }
            if (lockSite && new[] { start,end,middle }.Any(p => TerrainAt(p) != CellKind.Water))
                Fail("INFRASTRUCTURE_LOCK_WATERWAY_INVALID");
            if (shore)
            {
                if (definition.Id == "quay")
                {
                    // Parallel to shore: water along one long edge, dry service land along the other.
                    var sideA = new WorldPolyline([start.Offset(-dy*half,dx*half),end.Offset(-dy*half,dx*half)]);
                    var sideB = new WorldPolyline([start.Offset(dy*half,-dx*half),end.Offset(dy*half,-dx*half)]);
                    if (!(TerrainMatches(sideA,p=>TerrainAt(p)==CellKind.Water) && TerrainMatches(sideB,p=>!IsBlocked(p)) ||
                          TerrainMatches(sideB,p=>TerrainAt(p)==CellKind.Water) && TerrainMatches(sideA,p=>!IsBlocked(p))))
                        Fail("INFRASTRUCTURE_QUAY_SHORELINE_INVALID");
                }
                else if (IsBlocked(start) || TerrainAt(end) != CellKind.Water || !obstacle.Outline.ContainsPoint(end))
                    Fail("INFRASTRUCTURE_SHORE_CONNECTION_INVALID");
            }
            var connections = new List<InfrastructureConnection>();
            var firstContact = definition.Id == "quay" && paths.TryGetValue(rule.StartConnectionPathId ?? "",out var shoreAccess)
                ? shoreAccess.Centerline.Points[^1] : start;
            var accessLinks = new List<InfrastructureAccessLink>();
            if (definition.Id == "quay" && (footprint.DistanceToPoint(firstContact)>1e-8 || IsBlocked(firstContact)))
                Fail("INFRASTRUCTURE_QUAY_ACCESS_INVALID");
            if (definition.Id == "quay")
            {
                double along = Math.Clamp((firstContact.X-start.X)*dx+(firstContact.Y-start.Y)*dy,0,length);
                var target = start.Offset(dx*along,dy*along);
                if (target.DistanceTo(firstContact)>1e-8)
                {
                    double accessWidth = Math.Min(definition.AccessWidthMeters,variant.WidthMeters);
                    if (Band(firstContact,target,-accessWidth/2,accessWidth/2).Vertices.Any(p=>!footprint.ContainsPoint(p)))
                        Fail("INFRASTRUCTURE_ACCESS_LINK_DOES_NOT_FIT");
                    else accessLinks.Add(new(new([firstContact,target]),accessWidth,"pedestrian","connect dry service arrival to the quay deck"));
                }
            }
            Connect(rule.StartConnectionPathId,firstContact,lockSite ? "waterway" : crossing ? mode : "road",carriesRoute);
            if (bankSpan || lockSite) Connect(rule.EndConnectionPathId,end,lockSite ? "waterway" : mode,carriesRoute);
            else if (rule.EndConnectionPathId is not null) Fail("INFRASTRUCTURE_UNEXPECTED_END_CONNECTION");
            if (lockSite)
            {
                if (rule.ServicePathId is null || !paths.TryGetValue(rule.ServicePathId,out var service))
                    Fail("INFRASTRUCTURE_MAINTENANCE_ACCESS_REQUIRED");
                else
                {
                    var contact = service.Centerline.Points[^1];
                    if (footprint.DistanceToPoint(contact) > 1e-8 || IsBlocked(contact)) Fail("INFRASTRUCTURE_MAINTENANCE_ACCESS_INVALID");
                    Connect(rule.ServicePathId,contact,"road",false);
                }
            }
            else if (rule.ServicePathId is not null) Fail("INFRASTRUCTURE_UNEXPECTED_SERVICE_CONNECTION");
            // Unrelated routes cannot pass through structures or reserved edges.
            var allowedPaths = connections.Select(c => c.PathId).Append(alignment.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var other in design.RoadRules.Where(r => !allowedPaths.Contains(r.PathId)))
                foreach (var surface in StreetSurfaceBuilder.Build(new(other.PathId,paths[other.PathId].Centerline,other.Profile)))
                    if (Overlap(new(surface.Polygon),footprint) || reservations.Any(r => Overlap(new(surface.Polygon),r.Boundary)))
                        Fail("INFRASTRUCTURE_UNRELATED_PATH_CONFLICT");
            if (issues.Count != before) continue;
            result.Add(new(rule.Id,definition.Id,alignment.Id,obstacle.Id,variant,demand,length,footprint,
                connections,reservations,new(design.Blueprint.MockupId,design.Blueprint.ApprovedRevision,
                    design.Blueprint.ArtStyleId,parent.Id,rule.Id,middle,footprint,definition.Id,variant.Id,
                    variant.HeightMeters,variant.ServiceCapacity),rule.Purpose,rule.Id,carriesRoute,
                carriesRoute ? mode : lockSite ? "waterway" : definition.Id is "quay" or "ferry-landing" or "jetty" ? "pedestrian" : null,
                carriesRoute ? routeWidth : Math.Max(1,variant.WidthMeters-2*margin),accessLinks,tunnel));

            CellKind? TerrainAt(WorldPoint p) => terrainQuery.KindAt(p);
            bool IsBlocked(WorldPoint p) => TerrainAt(p) is null or CellKind.Water or CellKind.Rock;
            void Connect(string? id,WorldPoint position,string requiredMode,bool throughRoute)
            {
                if (id is null || id == alignment.Id || !paths.TryGetValue(id,out var path) || path.Role != requiredMode ||
                    !path.Centerline.Points.Any(p => p.DistanceTo(position)<1e-8) ||
                    path.Centerline.Points[0].DistanceTo(position)>1e-8 && path.Centerline.Points[^1].DistanceTo(position)>1e-8)
                { Fail("INFRASTRUCTURE_CONNECTION_MISSING_OR_WRONG_MODE"); return; }
                if (requiredMode != "waterway")
                {
                    if (!profiles.TryGetValue(id,out var profile) || profile.Profile.LaneWidthMeters*profile.Profile.LaneCount+1e-8 <
                        Math.Max(definition.MinimumRoadWidthMeters,variant.MinimumRoadWidthMeters))
                    { Fail("INFRASTRUCTURE_ACCESS_UNDERSIZED"); return; }
                    if (throughRoute)
                    {
                        var route = profiles[alignment.Id].Profile;
                        if (profile.Profile != route)
                            Fail("INFRASTRUCTURE_APPROACH_PROFILE_MISMATCH");
                        var neighbor = path.Centerline.Points[0].DistanceTo(position)<1e-8 ? path.Centerline.Points[1] : path.Centerline.Points[^2];
                        double vx=neighbor.X-position.X,vy=neighbor.Y-position.Y;
                        if (Math.Abs(vx*dy-vy*dx)>1e-8 ||
                            (position==start ? vx*dx+vy*dy>=0 : vx*dx+vy*dy<=0))
                            Fail("INFRASTRUCTURE_APPROACH_ALIGNMENT_INVALID");
                    }
                    foreach (var surface in StreetSurfaceBuilder.Build(new(id,path.Centerline,profile.Profile)))
                        if (Overlap(new(surface.Polygon),footprint)) Fail("INFRASTRUCTURE_CONNECTION_INTRUDES_INTO_STRUCTURE");
                }
                else if (!TerrainMatches(path.Centerline,p=>TerrainAt(p)==CellKind.Water))
                    Fail("INFRASTRUCTURE_WATERWAY_APPROACH_INVALID");
                connections.Add(new(id,requiredMode,position));
            }
            bool TerrainMatches(WorldPolyline line,Func<WorldPoint,bool> matches) => terrainQuery.Matches(line,matches,token);
        }
        for (int i=0;i<result.Count;i++)
        for (int j=0;j<i;j++)
        {
            token.ThrowIfCancellationRequested();
            if (new[] { result[i].Footprint }.Concat(result[i].Reservations.Select(r=>r.Boundary)).Any(a =>
                new[] { result[j].Footprint }.Concat(result[j].Reservations.Select(r=>r.Boundary)).Any(b=>Overlap(a,b))))
                issues.Add("INFRASTRUCTURE_RESERVATION_CONFLICT:"+result[i].Id+":"+result[j].Id);
        }
        return new(result,issues.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static bool Overlap(WorldPolygon a,WorldPolygon b) => TerrainCoverageTopology.IntersectionArea(a,b)>1e-8;
    private static WorldPolygon Band(WorldPoint a,WorldPoint b,double low,double high)
    {
        double length=a.DistanceTo(b),nx=-(b.Y-a.Y)/length,ny=(b.X-a.X)/length;
        return new([a.Offset(nx*low,ny*low),b.Offset(nx*low,ny*low),b.Offset(nx*high,ny*high),a.Offset(nx*high,ny*high)]);
    }
}
