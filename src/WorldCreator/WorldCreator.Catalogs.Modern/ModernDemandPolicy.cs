using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

public sealed class ModernDemandPolicy : ISettlementDemandPolicy
{
    public BuildingProgram Plan(IReadOnlyList<BuildingDefinition> catalog, SettlementDemand settlement)
    {
        settlement.Validate();
        var diagnostics = new List<string>();
        var needs = new List<BuildingNeed>();
        foreach (var definition in catalog.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            definition.Validate();
            if (!definition.AutomaticDemand) continue;
            if (!definition.AllowedClasses.Contains(settlement.Class)) continue;
            if (definition.AllowedRoles is { } roles && !roles.Contains(settlement.Role)) continue;
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
                BuildingFunction.Sports => (int)Math.Min(int.MaxValue, Math.Ceiling(settlement.Population * definition.ScaleDemandPerResident)),
                _ => 0
            };
            if (demand <= 0) continue;
            int capacity = definition.ServiceCapacity;
            string? selectedVariant = null;
            if (definition.SizeVariants is { Count: > 0 })
            {
                double target = Math.Max(definition.ServiceCapacity,
                    Math.Ceiling(settlement.Population*definition.ScaleDemandPerResident));
                var variant = definition.SizeVariants.Where(v => v.ServiceCapacity >= target &&
                    (v.AllowedClasses is null || v.AllowedClasses.Contains(settlement.Class)))
                    .OrderBy(v => v.WidthMeters*v.DepthMeters).ThenBy(v => v.Id,StringComparer.Ordinal).FirstOrDefault();
                if (variant is null)
                { diagnostics.Add("BUILDING_DEMAND_SIZE_UNAVAILABLE:"+definition.Id); continue; }
                capacity = variant.ServiceCapacity;
                selectedVariant = variant.Id;
            }
            int count = (int)Math.Ceiling((double)demand / capacity);
            needs.Add(new(definition.Id, count,
                $"Required {definition.Function} capacity {demand} for {settlement.Class}/{settlement.Role} population {settlement.Population}.",
                selectedVariant,demand));
        }
        if (settlement.Population > 0 && !needs.Any(n =>
            catalog.Any(d => d.Id == n.DefinitionId && d.Function == BuildingFunction.Housing)))
            diagnostics.Add("HOUSING_DEFINITION_UNAVAILABLE");
        return new(needs, diagnostics);
    }
}
