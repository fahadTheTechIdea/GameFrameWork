using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScenarioEngine.Contracts;

// Shared wire codec and canonical form: not a second game-state owner.
public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = MakeOptions();
    private static JsonSerializerOptions MakeOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 64
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
    public static string Hash<T>(T value) => HashElement(Element(value));
    public static string StateHash(SessionSnapshot snapshot)
    {
        var node = JsonSerializer.SerializeToNode(snapshot, Options)!;
        node["controllers"] = JsonSerializer.SerializeToNode(snapshot.Controllers.Order(StringComparer.Ordinal), Options);
        node["appliedEffects"] = JsonSerializer.SerializeToNode(snapshot.AppliedEffects.Order(StringComparer.Ordinal), Options);
        return HashElement(Element(node));
    }
    public static string HashElement(JsonElement value) => Convert.ToHexString(SHA256.HashData(Canonical(value))).ToLowerInvariant();
    public static string HashJson(JsonElement value) => Convert.ToHexString(SHA256.HashData(Canonical(value, integerOnly: false))).ToLowerInvariant();
    public static byte[] Canonical(JsonElement element, bool integerOnly = true)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer)) WriteCanonical(writer, element, integerOnly);
        return buffer.WrittenSpan.ToArray();
    }
    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value, bool integerOnly)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value, integerOnly);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) WriteCanonical(writer, item, integerOnly);
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number:
                if (value.TryGetInt64(out var signed)) writer.WriteNumberValue(signed);
                else if (value.TryGetUInt64(out var unsigned)) writer.WriteNumberValue(unsigned);
                else if (!integerOnly) writer.WriteRawValue(value.GetRawText());
                else throw new JsonException("Only integer rule-state values are supported.");
                break;
            default: value.WriteTo(writer); break;
        }
    }
    public static void RejectDuplicateProperties(JsonElement value, int maxDepth = 64)
    {
        ValidateProperties(value, 1, maxDepth);
    }
    private static void ValidateProperties(JsonElement value, int depth, int maxDepth)
    {
        if (depth > maxDepth) throw new JsonException("JSON nesting exceeds its depth budget.");
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate JSON property: " + property.Name);
                ValidateProperties(property.Value, depth + 1, maxDepth);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) ValidateProperties(child, depth + 1, maxDepth);
    }
}
