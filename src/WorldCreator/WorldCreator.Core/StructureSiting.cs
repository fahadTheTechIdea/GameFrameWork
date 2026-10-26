using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;




public enum SitingResourceOrigin { UtilityNetwork, SurfaceWater, Atmosphere, ResourceArea, Facility }
public enum SitingNetworkMode { Utility, Road, Railway, Pedestrian, Waterway, SiteInterface }
public enum SitingEvaluationStatus { Passed, Failed, Unresolved }

/// <summary>Quantities are configurable game-service units, not physical design flows or law.</summary>
public sealed record StructureResourceRequirement(string Id, string Kind, SitingNetworkMode Mode,
    string Unit = "game-service-units", double BaseQuantity = 1, double QuantityPerCapacity = .1,
    IReadOnlySet<string>? RequiredQualities = null, SitingResourceOrigin? RequiredOrigin = null,
    IReadOnlySet<string>? WhenTechnologies = null, bool SourceContainsSite = false,
    double MinimumRouteWidthMeters = 0, double? MaximumRouteLengthMeters = null);

public sealed record StructureSitingProfile(string Id, int Revision, IReadOnlyList<string> ResearchRuleIds,
    IReadOnlyList<string> SourceIds, IReadOnlySet<string> AllowedDistricts,
    IReadOnlySet<string> AllowedTechnologies, IReadOnlySet<string> Tags,
    IReadOnlyList<StructureResourceRequirement> Requirements,
    IReadOnlySet<SiteFeatureKind> RequiredSurroundings, IReadOnlySet<string>? Provides = null,
    string OutputUnit = "game-service-units", double OutputUnitsPerCapacity = 1)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Revision<1 || ResearchRuleIds is null || SourceIds is null ||
            ResearchRuleIds.Count==0 || SourceIds.Count==0 || ResearchRuleIds.Any(string.IsNullOrWhiteSpace) ||
            SourceIds.Any(string.IsNullOrWhiteSpace) || AllowedDistricts is null || AllowedDistricts.Count==0 ||
            AllowedDistricts.Any(d=>!SettlementCatalogRegistry.ValidId(d)) || AllowedTechnologies is null || AllowedTechnologies.Count==0 ||
            AllowedTechnologies.Any(t=>!SettlementCatalogRegistry.ValidId(t)) || Tags is null || Tags.Any(string.IsNullOrWhiteSpace) ||
            Requirements is null || Requirements.Count>256 || RequiredSurroundings is null || RequiredSurroundings.Any(s=>!Enum.IsDefined(s)) ||
            Provides?.Any(k=>!SettlementCatalogRegistry.ValidId(k))==true || string.IsNullOrWhiteSpace(OutputUnit) ||
            !double.IsFinite(OutputUnitsPerCapacity) || OutputUnitsPerCapacity<=0)
            throw new ArgumentException("Invalid structure siting profile.");
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach (var requirement in Requirements)
            if (requirement is null || string.IsNullOrWhiteSpace(requirement.Id) || !ids.Add(requirement.Id) ||
                !SettlementCatalogRegistry.ValidId(requirement.Kind) || !Enum.IsDefined(requirement.Mode) || string.IsNullOrWhiteSpace(requirement.Unit) ||
                !double.IsFinite(requirement.BaseQuantity) || requirement.BaseQuantity<0 ||
                !double.IsFinite(requirement.QuantityPerCapacity) || requirement.QuantityPerCapacity<0 ||
                requirement.BaseQuantity+requirement.QuantityPerCapacity<=0 ||
                !double.IsFinite(requirement.MinimumRouteWidthMeters) || requirement.MinimumRouteWidthMeters<0 ||
                requirement.MaximumRouteLengthMeters is { } maximum && (!double.IsFinite(maximum) || maximum<=0) ||
                requirement.RequiredOrigin is { } origin && !Enum.IsDefined(origin) ||
                requirement.RequiredQualities?.Any(string.IsNullOrWhiteSpace)==true ||
                requirement.WhenTechnologies?.Count==0 ||
                requirement.WhenTechnologies?.Any(t=>!SettlementCatalogRegistry.ValidId(t) || !AllowedTechnologies.Contains(t))==true)
                throw new ArgumentException("Invalid siting resource requirement.");
    }
}

public sealed record SitingDependencyBinding(string RequirementId, string SourceId, IReadOnlyList<string> LinkIds,
    WorldPoint? ServicePointMeters = null);
public sealed record StructureSitingContext(string DistrictId, string Technology,
    IReadOnlyList<SitingDependencyBinding> Dependencies);
public sealed record SitingDistrict(string Id, string RegionId, string SettlementId, string Use, string RuleId);
public sealed record SitingResourceSource(string Id, string Kind, SitingResourceOrigin Origin,
    string RegionId, WorldPoint Port, double Capacity, string Unit, IReadOnlySet<string> Qualities, string RuleId,
    string? ProviderObjectId = null, IReadOnlyList<string>? UpstreamSourceIds = null);
public sealed record SitingNetworkLink(string Id, string PathId, SitingNetworkMode Mode,
    double Capacity, string Unit, string RuleId, double ReservationWidthMeters = 1,
    IReadOnlySet<string>? Services = null);
public sealed record SitingSeparationRule(string Id, string FirstTag, string SecondTag,
    double MinimumMeters, string ParameterOrigin, string SourceId);
public sealed record SitingExclusionZone(string Id, string RegionId, IReadOnlySet<string> ExcludedTags, string RuleId);
public sealed record WorldSitingPlan(int SchemaVersion, IReadOnlyList<SitingDistrict> Districts,
    IReadOnlyList<SitingResourceSource> Sources, IReadOnlyList<SitingNetworkLink> Links,
    IReadOnlyList<SitingSeparationRule> Separations, IReadOnlyList<SitingExclusionZone> Exclusions);

public sealed record SitingRuleEvaluation(string ObjectId, string RuleId, SitingEvaluationStatus Status,
    string Message, IReadOnlyList<string> SourceIds, string? DependencyId = null, double? Quantity = null,
    string? Unit = null, IReadOnlyList<string>? LinkIds = null);
public sealed record SitingResourceAllocation(string ObjectId, string RequirementId, string SourceId,
    IReadOnlyList<string> LinkIds, double Quantity, string Unit);
public sealed record SitingNetworkReservation(string LinkId, WorldPolygon Boundary, string RuleId);
public sealed record WorldSitingAssessment(SitingEvaluationStatus Status, IReadOnlyList<SitingRuleEvaluation> Evaluations,
    IReadOnlyList<SitingResourceAllocation> Allocations, IReadOnlyList<SitingNetworkReservation>? Reservations = null)
{
    public IReadOnlyList<string> Rejections => Evaluations.Where(e=>e.Status is SitingEvaluationStatus.Failed or SitingEvaluationStatus.Unresolved)
        .Select(e=>e.RuleId+":"+e.ObjectId+":"+e.Message).ToArray();
}
