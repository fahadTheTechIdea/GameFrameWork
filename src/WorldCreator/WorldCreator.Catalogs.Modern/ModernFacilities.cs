using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

/// <summary>Regression/example programs using the same authoring compiler as arbitrary catalog content.</summary>
public static class ModernFacilities
{
    public static IReadOnlyList<FacilityDefinition> Create(IReadOnlyList<BuildingDefinition> structures)
    {
        var training = Row("modern.training-block", "Training grounds", [
            ("first-team", "football-training-ground", "first-team training ground"),
            ("academy", "football-training-ground", "academy training ground")]);
        var club = Row("modern.football-club", "Football club campus", [
            ("administration", "office-building", "club administration and staff"),
            ("medical", "clinic", "athlete medical support")], [training],
            [new("training", training.Id, "first-team and academy training precinct")]);
        var factory = Row("modern.factory-campus", "Factory campus", [
            ("receiving", "warehouse", "incoming materials storage"),
            ("production", "factory", "primary production hall"),
            ("dispatch", "warehouse", "finished goods and dispatch storage"),
            ("maintenance", "workshop", "production maintenance workshop"),
            ("administration", "office-building", "factory administration and staff")]);
        return [club, training, factory];

        FacilityDefinition Row(string id, string name, (string Member, string Definition, string Purpose)[] members,
            IReadOnlyList<FacilityDefinition>? existing = null, IReadOnlyList<FacilityProgramChild>? children = null)
        {
            var parts = members.Select(member => {
                var definition = structures.Single(d => d.Id == member.Definition);
                var variant = definition.SizeVariants!.OrderBy(v => v.WidthMeters * v.DepthMeters).ThenBy(v => v.Id, StringComparer.Ordinal).First();
                return new FacilityProgramPart(member.Member, definition.Id, variant.Id, member.Purpose,
                    Components: [new("entrance", "door", "enter " + member.Purpose, "closed", "door-open-close")]);
            }).ToArray();
            return CatalogContentAuthoring.CompileProgram(new(id, name, "modern.authored-facility-rhythm",
                new(new(4, 2, 0, 0, 0, 0), 8, 8, 8, 4, 8, 8), parts, children ?? []), structures, existing ?? []);
        }
    }
}
