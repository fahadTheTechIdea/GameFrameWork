using System.Text;
using System.Text.Json;

namespace ScenarioEngine.Contracts;

/// <summary>One wire-data policy shared by runtime admission and snapshot restoration.</summary>
public static class WireJsonRules
{
    public const int MaxPayloadBytes = 64 * 1024;
    public static ValidationReport Validate(JsonElement value, int maxBytes = MaxPayloadBytes,
        int maxDepth = 32, bool requireObject = false)
    {
        try
        {
            if (value.ValueKind == JsonValueKind.Undefined ||
                (requireObject && value.ValueKind != JsonValueKind.Object))
                return Invalid("JSON_WIRE_SHAPE", "Unexpected JSON payload shape.");
            if (Encoding.UTF8.GetByteCount(value.GetRawText()) > maxBytes)
                return Invalid("JSON_WIRE_SIZE", "JSON payload exceeds its UTF-8 byte budget.");
            ContractJson.RejectDuplicateProperties(value, maxDepth);
            return ValidationReport.Valid;
        }
        catch (Exception exception) when (exception is JsonException or ObjectDisposedException)
        {
            return Invalid("JSON_WIRE_INVALID", "Invalid, duplicate, deep or unavailable JSON payload: " + exception.GetType().Name);
        }
    }
    private static ValidationReport Invalid(string code, string message) => new([new(code, message)]);
}
public static class WorldFrameRules
{
    public const int MaxObservations = 256;
    public const int MaxFrameBytes = 1024 * 1024;
    public static string? ValidateShape(WorldFrame? frame)
    {
        if (frame is null) return "WORLD_FRAME_INVALID";
        if (frame.Observations.IsDefault || frame.Observations.Length > MaxObservations)
            return "WORLD_FRAME_LIMIT";
        var totalBytes = 0;
        foreach (var observation in frame.Observations)
        {
            if (observation is null || string.IsNullOrWhiteSpace(observation.Kind) || observation.Kind.Length > 128 ||
                !WireJsonRules.Validate(observation.Payload).IsValid)
                return "WORLD_OBSERVATION_INVALID";
            totalBytes += Encoding.UTF8.GetByteCount(observation.Payload.GetRawText()) +
                Encoding.UTF8.GetByteCount(observation.Kind) + 256;
            if (totalBytes > MaxFrameBytes) return "WORLD_FRAME_LIMIT";
        }
        return null;
    }
}
