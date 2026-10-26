namespace WorldCreator.Core.Planning;

public enum SettlementClass { Village, Town, City }
public enum SettlementRole { Agricultural, Residential, Commercial, Industrial, Mixed }


public enum BuildingPlacementKind { FrontageSite, LinearInfrastructure, WaterInfrastructure }
public sealed record SettlementDemand(SettlementClass Class, SettlementRole Role, int Population)
{
    public void Validate()
    {
        if (Population < 0 || Population > 10_000_000) throw new ArgumentOutOfRangeException(nameof(Population));
        if (!Enum.IsDefined(Class) || !Enum.IsDefined(Role)) throw new ArgumentException("Unknown settlement profile.");
    }
}
public sealed record BuildingDefinition(string Id, string Function, int ServiceCapacity,
    IReadOnlySet<SettlementClass> AllowedClasses, IReadOnlySet<SettlementRole> FavoredRoles,
    IReadOnlyList<BuildingSizeVariant>? SizeVariants = null, double ScaleDemandPerResident = 0,
    IReadOnlyList<BuildingSiteRule>? SiteRules = null, double AccessWidthMeters = 1.5,
    double MinimumRoadWidthMeters = 0, IReadOnlySet<SettlementRole>? AllowedRoles = null,
    string Category = "Unspecified", string CapacityUnit = "service-units",
    bool AutomaticDemand = true, BuildingPlacementKind PlacementKind = BuildingPlacementKind.FrontageSite,
    StructureSitingProfile? SitingProfile = null, StructureCompositionAssessment? Composition = null)
{
    public void Validate()
    {
        SitingProfile?.Validate();
        Composition?.Validate();
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Function) || ServiceCapacity <= 0 ||
            AllowedClasses is null || AllowedClasses.Count == 0 || FavoredRoles is null ||
            !double.IsFinite(ScaleDemandPerResident) || ScaleDemandPerResident < 0 ||
            !double.IsFinite(AccessWidthMeters) || AccessWidthMeters <= 0 ||
            !double.IsFinite(MinimumRoadWidthMeters) || MinimumRoadWidthMeters < 0 ||
            string.IsNullOrWhiteSpace(Category) || !Enum.IsDefined(PlacementKind) || string.IsNullOrWhiteSpace(CapacityUnit))
            throw new ArgumentException("Invalid building definition.");
        if (AllowedClasses.Any(c => !Enum.IsDefined(c)) || FavoredRoles.Any(r => !Enum.IsDefined(r)) ||
            AllowedRoles?.Any(r => !Enum.IsDefined(r)) == true)
            throw new ArgumentException("Invalid settlement class or role in building definition.");
        if (SizeVariants is not null)
        {
            if (SizeVariants.Any(v => v is null) || SizeVariants.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != SizeVariants.Count)
                throw new ArgumentException("Building size variants must have unique IDs.");
            foreach (var variant in SizeVariants) variant.Validate();
        }
        var rules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in SiteRules ?? [])
            if (rule is null || string.IsNullOrWhiteSpace(rule.Id) || !rules.Add(rule.Id) ||
                !Enum.IsDefined(rule.Kind) || !Enum.IsDefined(rule.Band) || rule.Kind == SiteFeatureKind.AccessWalkway ||
                !double.IsFinite(rule.MinimumDepthMeters) || rule.MinimumDepthMeters <= 0 ||
                !double.IsFinite(rule.InsetMeters) || rule.InsetMeters < 0 ||
                !double.IsFinite(rule.PlantSpacingMeters) || !double.IsFinite(rule.PlantMarginMeters) ||
                rule.PlantMarginMeters < 0 || rule.SpeciesAssetId is not null &&
                (string.IsNullOrWhiteSpace(rule.SpeciesAssetId) || rule.PlantSpacingMeters <= 0) ||
                rule.MaximumDepthMeters is { } maximum && (!double.IsFinite(maximum) || maximum < rule.MinimumDepthMeters))
                throw new ArgumentException("Invalid building site grammar.");
    }
}

/// <summary>Physical dimensions and service capacity of one authored archetype variant.
/// Sizes are never normalized to parcel size. Extra clearance reserves site space
/// for circulation, loading or spectator access as appropriate to the game.</summary>
public sealed record BuildingSizeVariant(string Id, double WidthMeters, double DepthMeters,
    double HeightMeters, int ServiceCapacity, double SiteClearanceMeters = 0,
    IReadOnlySet<SettlementClass>? AllowedClasses = null, double MinimumRoadWidthMeters = 0)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || ServiceCapacity <= 0 ||
            !double.IsFinite(WidthMeters) || WidthMeters <= 0 ||
            !double.IsFinite(DepthMeters) || DepthMeters <= 0 ||
            !double.IsFinite(HeightMeters) || HeightMeters <= 0 ||
            !double.IsFinite(SiteClearanceMeters) || SiteClearanceMeters < 0 ||
            !double.IsFinite(MinimumRoadWidthMeters) || MinimumRoadWidthMeters < 0)
            throw new ArgumentException("Invalid building size variant.");
        if (AllowedClasses?.Any(c => !Enum.IsDefined(c)) == true)
            throw new ArgumentException("Invalid settlement class in building size variant.");
    }
}
public sealed record BuildingNeed(string DefinitionId, int Count, string Reason,
    string? SizeVariantId = null, int? RequiredCapacity = null);
public sealed record BuildingProgram(IReadOnlyList<BuildingNeed> Needs, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Configurable building-demand planner; quantities follow service capacity, profile and local role.
/// Demand is delegated to the selected pack; the common engine supplies no setting default.
/// </summary>
public sealed class BuildingProgramPlanner(IReadOnlyList<BuildingDefinition> catalog, ISettlementDemandPolicy policy)
{
    public BuildingProgram Plan(SettlementDemand settlement) => policy.Plan(catalog,settlement);
}
