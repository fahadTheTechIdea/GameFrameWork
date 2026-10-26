using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

public enum SiteFeatureKind { AccessWalkway, Forecourt, LoadingYard, Parking, Garden, GreenBuffer, Lawn,
    CoolingPlant, UtilityPlant, SecurityCompound, BiosecurityApron }
public enum SiteBand { Front, Rear, Left, Right }

/// <summary>Archetype-specific site grammar, interpreted inside the building's parcel.
/// Planting requires an explicit species asset; no arbitrary species or filler greenery.</summary>
public sealed record BuildingSiteRule(string Id, SiteFeatureKind Kind, SiteBand Band,
    double MinimumDepthMeters, double InsetMeters = 0, bool Required = true,
    string? SpeciesAssetId = null, double PlantSpacingMeters = 0, double PlantMarginMeters = 0,
    double? MaximumDepthMeters = null);

public sealed record BuildingSiteFeature(string Id, string BuildingId, SiteFeatureKind Kind,
    WorldPolygon Boundary, string SourceVisualRegionId, string RuleId, string TargetPathId,
    string Purpose, string? SpeciesAssetId = null, double PlantSpacingMeters = 0, double PlantMarginMeters = 0);

public sealed record BuildingSitePlan(IReadOnlyList<BuildingSiteFeature> Features, IReadOnlyList<string> Issues);

/// <summary>Derives access, service and green zones from the selected physical footprint
/// and its street frontage. Zones cannot spill into roads, buildings or neighboring parcels.</summary>
public sealed class BuildingSitePlanner
{
    public BuildingSitePlan Plan(string buildingId, string regionId, string pathId,
        BuildableParcel parcel, WorldPolygon footprint, BuildingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentNullException.ThrowIfNull(footprint);
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        parcel.Boundary.Validate();
        footprint.Validate();
        if (parcel.Boundary.Vertices.Count != 4 || footprint.Vertices.Count != 4 ||
            parcel.Boundary.Vertices.Select(p => p.X).Distinct().Count() != 2 ||
            parcel.Boundary.Vertices.Select(p => p.Y).Distinct().Count() != 2 ||
            footprint.Vertices.Select(p => p.X).Distinct().Count() != 2 ||
            footprint.Vertices.Select(p => p.Y).Distinct().Count() != 2 ||
            footprint.Vertices.Any(p => !parcel.Boundary.ContainsPoint(p)))
            return new([], ["BUILDING_SITE_GEOMETRY_UNSUPPORTED"]);
        var features = new List<BuildingSiteFeature>();
        var issues = new List<string>();
        double px0 = parcel.Boundary.Vertices.Min(p => p.X), px1 = parcel.Boundary.Vertices.Max(p => p.X);
        double py0 = parcel.Boundary.Vertices.Min(p => p.Y), py1 = parcel.Boundary.Vertices.Max(p => p.Y);
        double bx0 = footprint.Vertices.Min(p => p.X), bx1 = footprint.Vertices.Max(p => p.X);
        double by0 = footprint.Vertices.Min(p => p.Y), by1 = footprint.Vertices.Max(p => p.Y);
        if (Math.Abs(parcel.StreetAccess.Y-py0) > 1e-8 && Math.Abs(parcel.StreetAccess.Y-py1) > 1e-8)
            return new([], ["BUILDING_SITE_FRONTAGE_INVALID"]);
        bool north = Math.Abs(parcel.StreetAccess.Y-py0) <= 1e-8;
        double cx = (bx0+bx1)/2, half = definition.AccessWidthMeters/2;
        if (cx-half < px0 || cx+half > px1)
            return new([], ["BUILDING_ACCESS_WIDTH_DOES_NOT_FIT"]);
        Add("access", SiteFeatureKind.AccessWalkway, cx-half, north ? py0 : by1,
            cx+half, north ? by0 : py1, "connect the building entrance to its designed street", null);
        foreach (var rule in definition.SiteRules ?? [])
        {
            double x0 = px0, x1 = px1, y0 = by0, y1 = by1;
            if (rule.Band is SiteBand.Front or SiteBand.Rear)
            {
                bool top = rule.Band == SiteBand.Front ? north : !north;
                y0 = top ? py0 : by1;
                y1 = top ? by0 : py1;
            }
            else if (rule.Band == SiteBand.Left) x1 = bx0;
            else x0 = bx1;
            if (rule.MaximumDepthMeters is { } maximum)
            {
                if (rule.Band == SiteBand.Left) x0 = Math.Max(x0,x1-maximum);
                else if (rule.Band == SiteBand.Right) x1 = Math.Min(x1,x0+maximum);
                else
                {
                    bool top = rule.Band == SiteBand.Front ? north : !north;
                    if (top) y0 = Math.Max(y0,y1-maximum); else y1 = Math.Min(y1,y0+maximum);
                }
            }
            x0 += rule.InsetMeters; x1 -= rule.InsetMeters;
            y0 += rule.InsetMeters; y1 -= rule.InsetMeters;
            double depth = rule.Band is SiteBand.Left or SiteBand.Right ? x1-x0 : y1-y0;
            if (depth + 1e-8 < rule.MinimumDepthMeters || x1-x0 <= 1e-8 || y1-y0 <= 1e-8)
            {
                if (rule.Required) issues.Add("BUILDING_SITE_ZONE_DOES_NOT_FIT:" + rule.Id);
                continue;
            }
            // Green/service front bands are split around the entrance. A forecourt
            // intentionally includes its access walk as shared circulation space.
            if (rule.Band == SiteBand.Front && rule.Kind != SiteFeatureKind.Forecourt)
            {
                Add(rule.Id+"-left", rule.Kind, x0,y0,Math.Min(x1,cx-half),y1, "front " + rule.Kind, rule);
                Add(rule.Id+"-right", rule.Kind, Math.Max(x0,cx+half),y0,x1,y1, "front " + rule.Kind, rule);
            }
            else Add(rule.Id, rule.Kind, x0,y0,x1,y1, rule.Band + " " + rule.Kind, rule);
        }
        return issues.Count == 0 ? new(features, []) : new([], issues);

        void Add(string id, SiteFeatureKind kind, double x0, double y0, double x1, double y1,
            string purpose, BuildingSiteRule? rule)
        {
            if (x1-x0 <= 1e-8 || y1-y0 <= 1e-8)
            {
                if (rule is null || rule.Required) issues.Add("BUILDING_SITE_ZONE_EMPTY:" + id);
                return;
            }
            features.Add(new(buildingId+"-"+id, buildingId, kind,
                new([new(x0,y0),new(x1,y0),new(x1,y1),new(x0,y1)]), regionId,
                definition.Id+":"+id, pathId, purpose, rule?.SpeciesAssetId,
                rule?.PlantSpacingMeters ?? 0, rule?.PlantMarginMeters ?? 0));
        }
    }
}
