namespace GameFrameWork.Core.Planning;

public enum SettlementClass { Village, Town, City }
public enum SettlementRole { Agricultural, Residential, Commercial, Industrial, Mixed }
public enum BuildingFunction { Housing, FoodRetail, Education, Health, Administration, Employment, Storage }
public sealed record SettlementDemand(SettlementClass Class, SettlementRole Role, int Population)
{
    public void Validate()
    {
        if (Population < 0 || Population > 10_000_000) throw new ArgumentOutOfRangeException(nameof(Population));
        if (!Enum.IsDefined(Class) || !Enum.IsDefined(Role)) throw new ArgumentException("Unknown settlement profile.");
    }
}
public sealed record BuildingDefinition(string Id, BuildingFunction Function, int ServiceCapacity,
    IReadOnlySet<SettlementClass> AllowedClasses, IReadOnlySet<SettlementRole> FavoredRoles)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || ServiceCapacity <= 0 || AllowedClasses.Count == 0)
            throw new ArgumentException("Invalid building definition.");
    }
}
public sealed record BuildingNeed(string DefinitionId, int Count, string Reason);
public sealed record BuildingProgram(IReadOnlyList<BuildingNeed> Needs, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Configurable building-demand planner; quantities follow service capacity, profile and local role.
/// Example catalog is intentionally illustrative, not a real-world universal planning standard.
/// </summary>
public sealed class BuildingProgramPlanner(IReadOnlyList<BuildingDefinition> catalog)
{
    public BuildingProgram Plan(SettlementDemand settlement)
    {
        settlement.Validate();
        var diagnostics = new List<string>();
        var needs = new List<BuildingNeed>();
        foreach (var definition in catalog.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            definition.Validate();
            if (!definition.AllowedClasses.Contains(settlement.Class)) continue;
            int demand = definition.Function switch
            {
                BuildingFunction.Housing => settlement.Population,
                BuildingFunction.FoodRetail => settlement.Population / 150,
                BuildingFunction.Education => settlement.Population / 200,
                BuildingFunction.Health => settlement.Population / 500,
                BuildingFunction.Administration => settlement.Class == SettlementClass.Village ? 0 : 1,
                BuildingFunction.Employment => definition.FavoredRoles.Contains(settlement.Role)
                    ? settlement.Population / 3 : 0,
                BuildingFunction.Storage => settlement.Role is SettlementRole.Agricultural or SettlementRole.Industrial
                    ? settlement.Population / 20 : 0,
                _ => 0
            };
            if (demand <= 0) continue;
            int count = (int)Math.Ceiling((double)demand / definition.ServiceCapacity);
            needs.Add(new(definition.Id, count,
                $"Required {definition.Function} capacity {demand} for {settlement.Class}/{settlement.Role} population {settlement.Population}."));
        }
        if (settlement.Population > 0 && !needs.Any(n =>
            catalog.Any(d => d.Id == n.DefinitionId && d.Function == BuildingFunction.Housing)))
            diagnostics.Add("HOUSING_DEFINITION_UNAVAILABLE");
        return new(needs, diagnostics);
    }
}
public static class ExampleBuildingCatalog
{
    public static IReadOnlyList<BuildingDefinition> Create() =>
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
            new HashSet<SettlementClass> { SettlementClass.City },
            new HashSet<SettlementRole> { SettlementRole.Industrial }),
        new("warehouse", BuildingFunction.Storage, 10,
            new HashSet<SettlementClass> { SettlementClass.Village, SettlementClass.Town, SettlementClass.City },
            new HashSet<SettlementRole>())
    ];
}
