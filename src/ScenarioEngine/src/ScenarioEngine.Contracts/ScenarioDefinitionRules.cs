using System.Collections.Immutable;

namespace ScenarioEngine.Contracts;

public static class ScenarioDefinitionRules
{
    // Also guards callers constructing definitions directly rather than through the content loader.
    public static ImmutableArray<ObjectiveDefinition> ValidateAndOrder(ScenarioDefinition scenario, LayerCapabilities capabilities)
    {
        if (scenario.Endings.IsEmpty)
            throw new ScenarioContractException("SCENARIO_EMPTY", "At least one ending is required.");
        var objectives = scenario.Objectives.ToDictionary(o => o.Id, StringComparer.Ordinal);
        var facts = FactDefinitions(scenario, capabilities);
        ConditionLogic.Validate(scenario.ClosingCondition, facts);
        ValidateZones(scenario, facts);
        foreach (var ending in scenario.Endings) ConditionLogic.Validate(ending.Condition, facts);
        foreach (var objective in scenario.Objectives)
        {
            foreach (var effects in new[] { objective.SuccessEffects, objective.FailureEffects })
                if (effects.Select(e => e.Key).Distinct(StringComparer.Ordinal).Count() != effects.Length)
                    throw new ScenarioContractException("EFFECT_DUPLICATE_KEY", objective.Id);
            ConditionLogic.Validate(objective.ActivateWhen, facts);
            ConditionLogic.Validate(objective.CompleteWhen, facts);
            ConditionLogic.Validate(objective.FailWhen, facts);
            foreach (var effect in objective.SuccessEffects.Concat(objective.FailureEffects))
                if (!capabilities.Effects.ContainsKey(effect.Type))
                    throw new ScenarioContractException("EFFECT_UNKNOWN", effect.Type);
        }
        var order = ImmutableArray.CreateBuilder<ObjectiveDefinition>();
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var objective in scenario.Objectives.OrderBy(o => o.Id, StringComparer.Ordinal)) Visit(objective.Id);
        return order.ToImmutable();
        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (!objectives.TryGetValue(id, out var objective)) throw new ScenarioContractException("PACK_REFERENCE", id);
            if (!visiting.Add(id)) throw new ScenarioContractException("OBJECTIVE_CYCLE", id);
            foreach (var dependency in objective.Dependencies.OrderBy(d => d.ObjectiveId, StringComparer.Ordinal)) Visit(dependency.ObjectiveId);
            visiting.Remove(id);
            visited.Add(id);
            order.Add(objective);
        }
    }
    private static void ValidateZones(ScenarioDefinition scenario, IReadOnlyDictionary<string, FactDefinition> facts)
    {
        var zones = scenario.Zones.ToDictionary(z => z.Id, StringComparer.Ordinal);
        if (!zones.ContainsKey(scenario.StartZoneId))
            throw new ScenarioContractException("PACK_REFERENCE", "Unknown start zone.");
        foreach (var zone in scenario.Zones)
        {
            var anchors = zone.Anchors.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
            if (anchors.Count != zone.Anchors.Length)
                throw new ScenarioContractException("ANCHOR_DUPLICATE", zone.Id);
            foreach (var exit in zone.Exits)
            {
                if (!zones.ContainsKey(exit.TargetZoneId) || !anchors.Contains(exit.AnchorId))
                    throw new ScenarioContractException("PACK_REFERENCE", "Invalid zone exit.");
                ConditionLogic.Validate(exit.Condition, facts);
            }
        }
        var reached = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        pending.Enqueue(scenario.StartZoneId);
        while (pending.TryDequeue(out var current))
        {
            if (!reached.Add(current)) continue;
            foreach (var exit in zones[current].Exits) pending.Enqueue(exit.TargetZoneId);
        }
        if (reached.Count != zones.Count)
            throw new ScenarioContractException("ZONE_UNREACHABLE", "Potential zone graph is disconnected.");
    }
    public static ImmutableDictionary<string, FactDefinition> FactDefinitions(ScenarioDefinition scenario, LayerCapabilities capabilities)
    {
        var facts = capabilities.Facts.ToBuilder();
        facts.Add("core.tick", new(FactKind.Integer, 0));
        foreach (var objective in scenario.Objectives)
            facts.Add("core.objective." + objective.Id + ".status", new(FactKind.String));
        return facts.ToImmutable();
    }
}
