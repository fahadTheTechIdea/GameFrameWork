using System.Collections.Immutable;
using System.Text.Json;

namespace ScenarioEngine.Contracts;

public sealed class ScenarioContractException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

// One grammar shared by authoring, loading and simulation.
public static class ConditionLogic
{
    public static Condition Parse(JsonElement json)
    {
        var count = 0;
        return ParseNode(json, 1, ref count);
    }
    private static Condition ParseNode(JsonElement json, int depth, ref int count)
    {
        if (depth > 8 || ++count > 64) throw new ScenarioContractException("CONDITION_LIMIT", "Condition budget exceeded.");
        if (json.TryGetProperty("all", out var all)) return new AllCondition(ParseChildren(all, depth, ref count));
        if (json.TryGetProperty("any", out var any)) return new AnyCondition(ParseChildren(any, depth, ref count));
        if (json.TryGetProperty("not", out var not)) return new NotCondition(ParseNode(not, depth + 1, ref count));
        return new CompareCondition(json.GetProperty("fact").GetString()!,
            json.GetProperty("op").GetString()!, ParseValue(json.GetProperty("value")));
    }
    private static ImmutableArray<Condition> ParseChildren(JsonElement array, int depth, ref int count)
    {
        if (array.GetArrayLength() is < 1 or > 16) throw new ScenarioContractException("CONDITION_LIMIT", "Invalid child count.");
        var result = ImmutableArray.CreateBuilder<Condition>();
        foreach (var item in array.EnumerateArray()) result.Add(ParseNode(item, depth + 1, ref count));
        return result.ToImmutable();
    }
    public static FactValue ParseValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => FactValue.From(true),
        JsonValueKind.False => FactValue.From(false),
        JsonValueKind.Number when value.TryGetInt64(out var number) => FactValue.From(number),
        JsonValueKind.String => FactValue.From(value.GetString()!),
        _ => throw new ScenarioContractException("CONDITION_TYPE", "Expected boolean, integer or string.")
    };
    public static void Validate(Condition condition, IReadOnlyDictionary<string, FactDefinition> definitions)
    {
        var count = 0;
        Visit(condition, 1);
        void Visit(Condition node, int depth)
        {
            if (depth > 8 || ++count > 64) throw new ScenarioContractException("CONDITION_LIMIT", "Condition budget exceeded.");
            switch (node)
            {
                case AllCondition all:
                    CheckCount(all.Children.Length);
                    foreach (var child in all.Children) Visit(child, depth + 1);
                    break;
                case AnyCondition any:
                    CheckCount(any.Children.Length);
                    foreach (var child in any.Children) Visit(child, depth + 1);
                    break;
                case NotCondition not: Visit(not.Child, depth + 1); break;
                case CompareCondition compare:
                    if (!definitions.TryGetValue(compare.Fact, out var definition))
                        throw new ScenarioContractException("CONDITION_FACT", "Unknown fact " + compare.Fact);
                    ValidateValue(compare.Fact, compare.Value, definition);
                    if (compare.Operation is not ("eq" or "ne" or "lt" or "lte" or "gt" or "gte") ||
                        (compare.Operation is not ("eq" or "ne") && compare.Value.Kind != FactKind.Integer))
                        throw new ScenarioContractException("CONDITION_OPERATOR", "Comparison is not supported.");
                    break;
                default: throw new ScenarioContractException("CONDITION_CONTRACT", "Unknown condition node.");
            }
        }
    }
    private static void CheckCount(int count)
    {
        if (count is < 1 or > 16) throw new ScenarioContractException("CONDITION_LIMIT", "Invalid child count.");
    }
    public static void ValidateValue(string key, FactValue value, FactDefinition definition)
    {
        if (definition.Kind != value.Kind)
            throw new ScenarioContractException("CONDITION_TYPE", "Fact type mismatch: " + key);
        if (value.Kind == FactKind.Integer &&
            ((definition.Minimum is { } min && value.Integer < min) ||
             (definition.Maximum is { } max && value.Integer > max)))
            throw new ScenarioContractException("CONDITION_RANGE", "Fact outside declared range: " + key);
        if (value.Kind == FactKind.String && (value.Text is null || value.Text.Length > 4096))
            throw new ScenarioContractException("CONDITION_RANGE", "Invalid fact string: " + key);
    }
    public static bool Evaluate(Condition condition, IReadOnlyDictionary<string, FactValue> facts) => condition switch
    {
        AllCondition all => all.Children.All(child => Evaluate(child, facts)),
        AnyCondition any => any.Children.Any(child => Evaluate(child, facts)),
        NotCondition not => !Evaluate(not.Child, facts),
        CompareCondition compare => Compare(compare, facts),
        _ => throw new ScenarioContractException("CONDITION_CONTRACT", "Unknown condition node.")
    };
    private static bool Compare(CompareCondition condition, IReadOnlyDictionary<string, FactValue> facts)
    {
        if (!facts.TryGetValue(condition.Fact, out var actual) || actual.Kind != condition.Value.Kind)
            throw new ScenarioContractException("CONDITION_CONTRACT", "Missing or mistyped fact: " + condition.Fact);
        var expected = condition.Value;
        var comparison = actual.Kind switch
        {
            FactKind.Integer => actual.Integer.CompareTo(expected.Integer),
            FactKind.Boolean => actual.Boolean.CompareTo(expected.Boolean),
            FactKind.String => StringComparer.Ordinal.Compare(actual.Text, expected.Text),
            _ => throw new ScenarioContractException("CONDITION_TYPE", "Unknown fact kind.")
        };
        return condition.Operation switch
        {
            "eq" => comparison == 0, "ne" => comparison != 0,
            "lt" => comparison < 0, "lte" => comparison <= 0,
            "gt" => comparison > 0, "gte" => comparison >= 0,
            _ => throw new ScenarioContractException("CONDITION_OPERATOR", "Unknown comparison.")
        };
    }
}
