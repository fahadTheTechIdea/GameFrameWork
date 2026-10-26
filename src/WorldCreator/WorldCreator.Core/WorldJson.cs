using System.Text.Json;
using System.Text.Json.Serialization;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Shared JSON boundary for engine requests and generated documents.
/// Read-only catalog sets need explicit conversion; enums accept names or numbers.</summary>
public static class WorldJson
{
    public static JsonSerializerOptions CreateOptions(bool writeIndented = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = writeIndented };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ReadOnlySetConverter<SettlementClass>());
        options.Converters.Add(new ReadOnlySetConverter<SettlementRole>());
        options.Converters.Add(new ReadOnlySetConverter<SiteFeatureKind>());
        options.Converters.Add(new ReadOnlySetConverter<string>());
        return options;
    }

    private sealed class ReadOnlySetConverter<T> : JsonConverter<IReadOnlySet<T>> where T : notnull
    {
        public override IReadOnlySet<T> Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<HashSet<T>>(ref reader,options) ?? throw new JsonException("Catalog set must be an array.");
        public override void Write(Utf8JsonWriter writer,IReadOnlySet<T> value,JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer,typeof(T)==typeof(string)
                ? value.OrderBy(x=>x.ToString(),StringComparer.Ordinal).ToArray() : value.OrderBy(x=>x).ToArray(),options);
    }
}
