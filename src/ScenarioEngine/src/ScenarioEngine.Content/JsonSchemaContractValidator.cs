using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Content;

/// <summary>Replaceable JSON Schema provider. Schemas come from trusted application code.</summary>
public sealed class JsonSchemaContractValidator : IContractValidator
{
    private readonly ConcurrentDictionary<string, JsonSchema> _schemas = new(StringComparer.Ordinal);
    public ValidationReport Validate(JsonElement schema, JsonElement value, string file = "")
    {
        var compiled = _schemas.GetOrAdd(ContractJson.HashElement(schema), _ =>
        {
            RejectExternalReferences(schema);
            return JsonSchema.FromText(schema.GetRawText());
        });
        var result = compiled.Evaluate(JsonNode.Parse(value.GetRawText()), new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (result.IsValid) return ValidationReport.Valid;
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        Collect(result);
        return new(diagnostics.ToImmutable());
        void Collect(EvaluationResults item)
        {
            if (item.HasErrors)
                foreach (var error in item.Errors!)
                    diagnostics.Add(new("SCHEMA_VALIDATION", error.Key + ": " + error.Value, file, item.InstanceLocation.ToString()));
            if (item.HasDetails)
                foreach (var detail in item.Details) Collect(detail);
        }
    }
    private static void RejectExternalReferences(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
            {
                if (property.Name is "$ref" or "$dynamicRef" &&
                    !property.Value.GetString()!.StartsWith('#'))
                    throw new ScenarioContractException("SCHEMA_EXTERNAL_REFERENCE", "Schemas must be self-contained; no network resolution.");
                RejectExternalReferences(property.Value);
            }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectExternalReferences(item);
    }
}
