using System.Collections.Immutable;
using System.Text.Json;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Content;

public sealed partial class ScenarioContentService
{
    private static void ValidateCommonSemantics(Dictionary<string, Dictionary<string, JsonElement>> records, IGameLayer layer)
    {
        var english = records.GetValueOrDefault("localization")?.Values
            .Where(v => v.GetProperty("locale").GetString() == "en")
            .SelectMany(v => v.GetProperty("entries").EnumerateObject())
            .Select(p => p.Name).ToHashSet(StringComparer.Ordinal) ?? [];
        foreach (var category in records.Values)
            foreach (var doc in category.Values)
                foreach (var field in new[] { "titleKey", "summaryKey" })
                    if (doc.TryGetProperty(field, out var key) && !english.Contains(key.GetString()!))
                        throw new ScenarioContractException("LOCALIZATION_MISSING", key.GetString()!);
    }

    private ScenarioDefinition ReadScenario(JsonElement source, Dictionary<string, Dictionary<string, JsonElement>> records, IGameLayer layer)
    {
        var objectives = ImmutableArray.CreateBuilder<ObjectiveDefinition>();
        foreach (var id in Strings(source.GetProperty("objectiveIds")))
        {
            var obj = Find(records, "objective", id);
            var dependencies = obj.GetProperty("dependencies").EnumerateArray().Select(d => new ObjectiveDependency(
                d.GetProperty("objectiveId").GetString()!,
                [.. Strings(d.GetProperty("acceptedStatuses")).Select(Enum.Parse<ObjectiveStatus>)] )).ToImmutableArray();
            objectives.Add(new(id, dependencies,
                ConditionLogic.Parse(obj.GetProperty("activateWhen")), ConditionLogic.Parse(obj.GetProperty("completeWhen")),
                ConditionLogic.Parse(obj.GetProperty("failWhen")), ReadEffects(obj.GetProperty("successEffects"), layer),
                ReadEffects(obj.GetProperty("failureEffects"), layer)));
        }
        var endings = Strings(source.GetProperty("endingIds")).Select(id =>
        {
            var ending = Find(records, "ending", id);
            return new EndingDefinition(id, ending.GetProperty("priority").GetInt32(),
                ConditionLogic.Parse(ending.GetProperty("condition")), [.. Strings(ending.GetProperty("outcomeTags"))]);
        }).ToImmutableArray();
        var zones = Strings(source.GetProperty("zoneIds")).Select(id =>
        {
            var zone = Find(records, "zone", id);
            var anchors = zone.GetProperty("anchors").EnumerateArray().Select(a =>
            {
                var coordinates = a.GetProperty("positionMm").EnumerateArray().Select(v => v.GetInt32()).ToArray();
                return new AnchorDefinition(a.GetProperty("id").GetString()!, new(coordinates[0], coordinates[1], coordinates[2]));
            }).ToImmutableArray();
            var exits = zone.GetProperty("exits").EnumerateArray().Select(e => new ExitDefinition(
                e.GetProperty("id").GetString()!, e.GetProperty("targetZoneId").GetString()!,
                e.GetProperty("anchorId").GetString()!, ConditionLogic.Parse(e.GetProperty("condition")),
                e.GetProperty("durationTicks").GetInt32())).ToImmutableArray();
            return new ZoneDefinition(id, zone.GetProperty("titleKey").GetString()!, zone.GetProperty("sceneRole").GetString()!, anchors, exits);
        }).ToImmutableArray();
        var result = new ScenarioDefinition(source.GetProperty("id").GetString()!,
            ConditionLogic.Parse(source.GetProperty("closingCondition")), objectives.ToImmutable(), endings, source.GetProperty("layerData").Clone(),
            source.GetProperty("titleKey").GetString()!, source.GetProperty("startZoneId").GetString()!, zones);
        ScenarioDefinitionRules.ValidateAndOrder(result, layer.Capabilities);
        return result;
    }
    private ImmutableArray<EffectDefinition> ReadEffects(JsonElement array, IGameLayer layer)
    {
        var result = ImmutableArray.CreateBuilder<EffectDefinition>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            var type = item.GetProperty("type").GetString()!;
            var key = item.GetProperty("key").GetString()!;
            if (!keys.Add(key)) throw new ScenarioContractException("EFFECT_DUPLICATE_KEY", key);
            if (!layer.Capabilities.Effects.TryGetValue(type, out var schema))
                throw new ScenarioContractException("EFFECT_UNKNOWN", type);
            RequireValid(_validator.Validate(schema, item.GetProperty("arguments")));
            result.Add(new(type, key, item.GetProperty("arguments").Clone()));
        }
        return result.ToImmutable();
    }
}
