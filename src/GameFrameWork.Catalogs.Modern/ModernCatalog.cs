using System.Collections.Frozen;
using GameFrameWork.Core.Planning;
using GameFrameWork.Core;

namespace GameFrameWork.Catalogs.Modern;

public static class ModernCapabilities
{
    public const string Electricity="modern.electricity-network";
    public const string PotableWater="modern.potable-water-network";
    public const string Wastewater="modern.wastewater-network";
    public const string MotorRoads="modern.motor-road-network";
    public const string Districts="modern.district-policy";
    public const string Technology="modern.facility-technology";
    public static IReadOnlySet<string> All { get; } = new[] { Electricity,PotableWater,Wastewater,MotorRoads,Districts,Technology }.ToFrozenSet(StringComparer.Ordinal);
}
public sealed class ModernCatalog : ISettlementCatalogProvider, IWorldPlanPalette
{
    public string SurfaceColor(string materialId)=>materialId switch
    { "modern.park-paving"=>"#d7c7aa","modern.planted-ground"=>"#8dac65","modern.football-pitch"=>"#5d9652","modern.basketball-court"=>"#d28f5d",_=>"#c8c8bc" };
    public string StructureColor(string definitionId)=>definitionId switch
    { "football-stadium"=>"#5a91b5","factory"=>"#bf845e","warehouse"=>"#a79b80","primary-school"=>"#d7b459","office-building"=>"#849dac",_=>"#cbab83" };
    public SettlementCatalogDescriptor Descriptor { get; } = new("modern","Modern",ModernCapabilities.All,
        new HashSet<string>().ToFrozenSet(StringComparer.Ordinal),new(BuildingFunction.Values,BuildingCategory.Values,
            DistrictUse.Values,FacilityTechnology.Values,SitingResourceKind.Values));
    public IReadOnlyList<BuildingDefinition> Structures => ModernStructures.Create();
    public ISettlementCatalogPolicy Policy { get; } = new ModernCatalogPolicy();
    public IInfrastructureDesignStrategy Infrastructure { get; } = new ModernInfrastructureDesignStrategy();
    public ISettlementDemandPolicy Demand { get; } = new ModernDemandPolicy();
    public static SettlementCatalogSelection Selection => new("modern",ModernCapabilities.All);
}
public sealed class ModernCatalogPolicy : ISettlementCatalogPolicy
{
    public string Id => "modern.siting";
    public BuildingDefinition Resolve(BuildingDefinition definition,string technology) => ModernSitingProfiles.Resolve(definition,technology);
    public IReadOnlySet<string> RequiredCapabilities(MockupWorldDesign design) => design.RoadRules.Any(r=>
        design.Paths.Any(p=>p.Id==r.PathId && p.Role=="road"))?new HashSet<string> { ModernCapabilities.MotorRoads }:new HashSet<string>();
    public IReadOnlySet<string> RequiredCapabilities(BuildingDefinition definition,string technology)
    {
        var result=new HashSet<string>(StringComparer.Ordinal) { ModernCapabilities.Districts,ModernCapabilities.Technology };
        foreach (var r in Resolve(definition,technology).SitingProfile!.Requirements.Where(r=>r.WhenTechnologies is null || r.WhenTechnologies.Contains(technology)))
        {
            if (r.Kind==SitingResourceKind.Electricity) result.Add(ModernCapabilities.Electricity);
            if (r.Kind==SitingResourceKind.PotableWater) result.Add(ModernCapabilities.PotableWater);
            if (r.Kind==SitingResourceKind.Wastewater) result.Add(ModernCapabilities.Wastewater);
            if (r.Mode==SitingNetworkMode.Road) result.Add(ModernCapabilities.MotorRoads);
        }
        if (definition.PlacementKind==BuildingPlacementKind.FrontageSite) result.Add(ModernCapabilities.MotorRoads);
        return result;
    }
    public IReadOnlyList<string> ValidateSupply(MockupWorldDesign design,StructureResourceRequirement requirement,SitingResourceSource source,StructureSitingContext context,CancellationToken token=default)
    {
        var errors=new List<string>(); var sources=design.Siting!.Sources.ToDictionary(s=>s.Id,StringComparer.Ordinal);
        if (source.Qualities.Contains("potable") && (source.Qualities.Contains("marine") || source.Qualities.Contains("brackish")))
            errors.Add("potable supply cannot also be declared marine or brackish without treatment");
        if (requirement.Id=="cooling-intake" && context.Dependencies.FirstOrDefault(b=>b.RequirementId=="water-outfall") is { } outfall &&
            source.Port.DistanceTo(sources[outfall.SourceId].Port)<1e-8) errors.Add("cooling intake and thermal outfall cannot be the same service point");
        if (requirement.Kind==SitingResourceKind.ParentFacility && source.ProviderObjectId is null) errors.Add("a real parent facility is required");
        if (requirement.RequiredQualities?.Contains("protected")==true && Contaminated(source.Id,new(StringComparer.Ordinal)))
            errors.Add("protected intake has an incompatible upstream discharge/contamination source");
        return errors;
        bool Contaminated(string id,HashSet<string> visited)
        { token.ThrowIfCancellationRequested(); return visited.Add(id) && (sources[id].Qualities.Contains("contaminated") ||
            sources[id].Kind==SitingResourceKind.WaterOutfall || (sources[id].UpstreamSourceIds ?? []).Any(parent=>Contaminated(parent,visited))); }
    }
    public bool RequiresNeighborPolicy(IReadOnlySet<string> a,IReadOnlySet<string> b) =>
        Pair(a,b,"heavy-impact","sensitive") || Pair(a,b,"nuclear","sensitive") || Pair(a,b,"freight-intensive","sensitive") ||
        Pair(a,b,"airport","wildlife-attractant") || Pair(a,b,"airport","aviation-obstacle") || Pair(a,b,"dark-sky-sensitive","night-lighting");
    private static bool Pair(IReadOnlySet<string> a,IReadOnlySet<string> b,string first,string second) =>
        a.Contains(first)&&b.Contains(second) || b.Contains(first)&&a.Contains(second);
}
