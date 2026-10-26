using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

internal static partial class ModernStructures
{
    /// <summary>Illustrative, configurable dimensions and capacity relationships;
    /// not building regulations, exhaustive coverage or real venue specifications.</summary>
    public static IReadOnlyList<BuildingDefinition> Create() => Definitions().Select(d => d with {
        SizeVariants = Sizes(d.Id),
        SiteRules = SiteRules(d.Id),
        AccessWidthMeters = d.Id == "football-stadium" ? 8 : d.Id is "factory" or "warehouse" ? 4 : 1.5,
        MinimumRoadWidthMeters = d.Id switch { "factory" => 8, "warehouse" => 6, "football-stadium" => 4, _ => 3 },
        Category = d.Id switch {
            "village-house" or "townhouse" or "apartment" => BuildingCategory.Residential,
            "local-market" or "city-retail" => BuildingCategory.Retail,
            "school" => BuildingCategory.EducationResearch,"clinic" => BuildingCategory.Healthcare,
            "civic-office" => BuildingCategory.CivicSafety,"football-stadium" => BuildingCategory.SportRecreation,
            _ => BuildingCategory.IndustryExtraction
        },
        CapacityUnit = d.Id switch {
            "village-house" or "townhouse" or "apartment" => "residents",
            "football-stadium" => "seats","factory" or "workshop" => "workers",
            "warehouse" => "storage-units",_ => "service-units"
        },
        AllowedRoles = d.Id is "factory" or "warehouse" ? new HashSet<SettlementRole> {
            SettlementRole.Industrial,SettlementRole.Mixed,SettlementRole.Agricultural } : null,
        ScaleDemandPerResident = d.Id switch { "football-stadium" => .15, "factory" => .002, "warehouse" => .001, _ => 0 }
    }).Concat(ExpandedDefinitions()).Select(ModernSitingProfiles.Attach).Select(ModernCompositionProfiles.Attach).OrderBy(d => d.Id,StringComparer.Ordinal).ToArray();

    private static IReadOnlyList<BuildingDefinition> Definitions() =>
    [
        new("village-house", BuildingFunction.Housing, 5,
            new HashSet<SettlementClass> { SettlementClass.Village }, new HashSet<SettlementRole>()),
        new("townhouse", BuildingFunction.Housing, 8,
            new HashSet<SettlementClass> { SettlementClass.Town }, new HashSet<SettlementRole>()),
        new("apartment", BuildingFunction.Housing, 48,
            new HashSet<SettlementClass> { SettlementClass.City }, new HashSet<SettlementRole>()),
        new("local-market", BuildingFunction.FoodRetail, 3,
            new HashSet<SettlementClass> { SettlementClass.Village, SettlementClass.Town }, new HashSet<SettlementRole>()),
        new("city-retail", BuildingFunction.FoodRetail, 20,
            new HashSet<SettlementClass> { SettlementClass.City }, new HashSet<SettlementRole>()),
        new("school", BuildingFunction.Education, 4,
            new HashSet<SettlementClass> { SettlementClass.Town, SettlementClass.City }, new HashSet<SettlementRole>()),
        new("clinic", BuildingFunction.Health, 2,
            new HashSet<SettlementClass> { SettlementClass.Town, SettlementClass.City }, new HashSet<SettlementRole>()),
        new("civic-office", BuildingFunction.Administration, 1,
            new HashSet<SettlementClass> { SettlementClass.Town, SettlementClass.City }, new HashSet<SettlementRole>()),
        new("workshop", BuildingFunction.Employment, 20,
            new HashSet<SettlementClass> { SettlementClass.Village, SettlementClass.Town },
            new HashSet<SettlementRole> { SettlementRole.Agricultural, SettlementRole.Industrial }),
        new("factory", BuildingFunction.Employment, 200,
            new HashSet<SettlementClass> { SettlementClass.Town, SettlementClass.City },
            new HashSet<SettlementRole> { SettlementRole.Industrial }),
        new("warehouse", BuildingFunction.Storage, 10,
            new HashSet<SettlementClass> { SettlementClass.Village, SettlementClass.Town, SettlementClass.City },
            new HashSet<SettlementRole>()),
        new("football-stadium", BuildingFunction.Sports, 250,
            new HashSet<SettlementClass> { SettlementClass.Village, SettlementClass.Town, SettlementClass.City },
            new HashSet<SettlementRole>())
    ];

    private static IReadOnlyList<BuildingSizeVariant> Sizes(string id) => id switch
    {
        "village-house" => [new("cottage", 8, 10, 5, 5), new("family-house", 12, 16, 7, 8)],
        "townhouse" => [new("compact", 8, 12, 9, 8), new("large", 12, 18, 12, 12)],
        "apartment" => [new("low-rise", 20, 24, 15, 48), new("mid-rise", 30, 35, 30, 160), new("tower", 40, 45, 75, 500)],
        "local-market" => [new("small", 10, 12, 5, 3), new("covered", 25, 30, 8, 12)],
        "city-retail" => [new("shop-row", 20, 25, 8, 20), new("retail-center", 80, 100, 16, 200)],
        "school" => [new("local", 30, 35, 9, 4, 3), new("campus", 80, 90, 15, 20, 5)],
        "clinic" => [new("local", 16, 20, 8, 2), new("health-center", 45, 55, 15, 12)],
        "civic-office" => [new("local", 20, 25, 12, 1), new("municipal", 50, 60, 25, 5)],
        "workshop" => [new("small", 12, 16, 7, 20, 1), new("large", 25, 35, 10, 80, 2)],
        "factory" => [new("small", 24, 18, 10, 200, 3), new("medium", 60, 40, 16, 800, 6), new("large", 140, 90, 24, 3000, 10)],
        "warehouse" => [new("small", 12, 10, 8, 10, 2), new("medium", 30, 20, 12, 50, 4),
            new("large", 60, 40, 18, 200, 6), new("distribution-hub", 120, 80, 24, 1000, 10)],
        "football-stadium" => [new("local-ground", 130, 90, 10, 500, 5,MinimumRoadWidthMeters:4),
            new("town-ground",150,110,16,3000,8,MinimumRoadWidthMeters:6),
            new("regional", 180, 140, 30, 12000, 15,MinimumRoadWidthMeters:8),
            new("major-city", 260, 220, 55, 60000, 25,MinimumRoadWidthMeters:12)],
        _ => []
    };

    private static IReadOnlyList<BuildingSiteRule> SiteRules(string id) => id switch
    {
        "village-house" or "townhouse" => [new("rear-garden", SiteFeatureKind.Garden, SiteBand.Rear, 2, .25,MaximumDepthMeters:20)],
        "factory" or "warehouse" or "workshop" => [new("service-yard", SiteFeatureKind.LoadingYard, SiteBand.Rear, 4,MaximumDepthMeters:60),
            new("landscape-buffer", SiteFeatureKind.GreenBuffer, SiteBand.Left, 2, .25,MaximumDepthMeters:8)],
        "football-stadium" => [new("entrance-plaza", SiteFeatureKind.Forecourt, SiteBand.Front, 10,MaximumDepthMeters:60),
            new("parking",SiteFeatureKind.Parking,SiteBand.Left,10,MaximumDepthMeters:50),
            new("rear-green-belt", SiteFeatureKind.GreenBuffer, SiteBand.Rear, 5, 1,MaximumDepthMeters:20)],
        _ => [new("front-court", SiteFeatureKind.Forecourt, SiteBand.Front, 2)]
    };
}
