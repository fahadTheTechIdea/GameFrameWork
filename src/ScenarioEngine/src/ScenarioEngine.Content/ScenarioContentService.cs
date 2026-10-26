using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Content;

public sealed partial class ScenarioContentService : IScenarioContentService
{
    private readonly IGameLayerRegistry _layers;
    private readonly IContractValidator _validator;
    private readonly IEngineReporter _reporter;
    private readonly JsonElement _schema;
    private readonly object _gate = new();
    private ImmutableDictionary<ScenarioIdentity, ValidatedScenario> _catalog = ImmutableDictionary<ScenarioIdentity, ValidatedScenario>.Empty;

    public ScenarioContentService(IGameLayerRegistry layers, IContractValidator validator, IEngineReporter reporter)
    {
        _layers = layers; _validator = validator; _reporter = reporter;
        var assembly = typeof(ScenarioContentService).Assembly;
        using var resource = assembly.GetManifestResourceStream(
            assembly.GetManifestResourceNames().Single(n => n.EndsWith("content.schema.json", StringComparison.Ordinal)))!;
        using var document = JsonDocument.Parse(resource);
        _schema = document.RootElement.Clone();
    }
    public ImmutableArray<ScenarioSummary> ListPlayable()
    {
        lock (_gate) return [.. _catalog.Keys.OrderBy(k => k.PackId, StringComparer.Ordinal)
            .ThenBy(k => k.ScenarioId, StringComparer.Ordinal).Select(k => new ScenarioSummary(k))];
    }
    public bool TryResolve(ScenarioIdentity identity, out ValidatedScenario scenario)
    {
        lock (_gate) return _catalog.TryGetValue(identity, out scenario!);
    }
    public ContentLoadResult LoadNormalized(ReadOnlyMemory<byte> json, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (json.Length > 1024 * 1024) return Refused("PACK_SIZE", "Normalized pack exceeds 1 MiB.");
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            ContractJson.RejectDuplicateProperties(root);
            var shape = _validator.Validate(_schema, root, "pack.json");
            if (!shape.IsValid) return Refused(shape);
            var manifest = root.GetProperty("manifest");
            var layerId = manifest.GetProperty("gameLayerId").GetString()!;
            var layerVersion = manifest.GetProperty("gameLayerVersion").GetString()!;
            if (!_layers.TryResolve(layerId, layerVersion, out var layer))
                return Refused("LAYER_UNAVAILABLE", "Required layer/version is not registered.");
            if (manifest.GetProperty("dependencies").GetArrayLength() > 0)
                return Refused("PACK_DEPENDENCIES_UNSUPPORTED", "This slice supports self-contained packs only.");
            var docs = root.GetProperty("documents").EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);
            ValidateManifest(manifest, docs);
            var records = IndexDocuments(docs);
            ValidateCommonSemantics(records, layer);
            var digest = Digest(manifest, docs);
            var built = ImmutableArray.CreateBuilder<ValidatedScenario>();
            foreach (var id in records["scenario"].Keys.Order(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = Find(records, "scenario", id);
                RequireValid(_validator.Validate(layer.Capabilities.ContentSchema, source.GetProperty("layerData"), "scenario:" + id));
                RequireValid(layer.Validate(source.GetProperty("layerData")));
                var scenario = ReadScenario(source, records, layer);
                var identity = new ScenarioIdentity(
                    manifest.GetProperty("packId").GetString()!, manifest.GetProperty("packVersion").GetString()!,
                    digest, id, layerId, layerVersion);
                built.Add(new(identity, scenario));
            }
            var entryIds = Strings(manifest.GetProperty("entryScenarioIds")).ToHashSet(StringComparer.Ordinal);
            if (entryIds.Any(id => !records["scenario"].ContainsKey(id)))
                return Refused("PACK_REFERENCE", "An entry scenario is missing.");
            var scenarios = built.Where(s => entryIds.Contains(s.Identity.ScenarioId)).ToImmutableArray();
            lock (_gate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_catalog.Keys.Any(k => k.PackId == scenarios[0].Identity.PackId &&
                    k.PackVersion == scenarios[0].Identity.PackVersion && k.ContentDigest != digest))
                    return Refused("PACK_ID_CONFLICT", "Same pack identity already has different content.");
                _reporter.Audit(new("PackValidated", scenarios[0].Identity.PackId, "accepted"));
                var next = _catalog;
                foreach (var scenario in scenarios) next = next.SetItem(scenario.Identity, scenario);
                _catalog = next;
            }
            return new(scenarios, ValidationReport.Valid);
        }
        catch (OperationCanceledException)
        {
            _reporter.Report(new("PACK_CANCELLED", "Content validation cancelled before registration.", Severity.Information));
            throw;
        }
        catch (ScenarioContractException exception)
        {
            _reporter.Report(new(exception.Code, exception.Message, Severity.Warning, Exception: exception));
            return Refused(exception.Code, exception.Message);
        }
        catch (JsonException exception)
        {
            _reporter.Report(new("PACK_JSON", "Invalid JSON content.", Severity.Warning, Exception: exception));
            return Refused("PACK_JSON", exception.Message);
        }
        catch (Exception exception)
        {
            _reporter.Report(new("PACK_LOAD_FAILED", "Content registration failed.", Severity.Error, Exception: exception));
            return Refused("PACK_LOAD_FAILED", "Content registration failed; consult diagnostics.");
        }
    }
    private ContentLoadResult Refused(string code, string message) => Refused(new ValidationReport([new(code, message)]));
    private ContentLoadResult Refused(ValidationReport report)
    {
        foreach (var diagnostic in report.Diagnostics)
            _reporter.Report(new(diagnostic.Code, diagnostic.Message, diagnostic.Severity));
        return new([], report);
    }
    private static void RequireValid(ValidationReport report)
    {
        if (!report.IsValid) throw new ScenarioContractException(report.Diagnostics[0].Code, report.Diagnostics[0].Message);
    }
    private static string[] Strings(JsonElement value) => value.EnumerateArray().Select(v => v.GetString()!).ToArray();
    private static JsonElement Find(Dictionary<string, Dictionary<string, JsonElement>> records, string kind, string id)
    {
        if (!records.TryGetValue(kind, out var category) || !category.TryGetValue(id, out var result))
            throw new ScenarioContractException("PACK_REFERENCE", "Unknown " + kind + ": " + id);
        return result;
    }
    private static Dictionary<string, Dictionary<string, JsonElement>> IndexDocuments(Dictionary<string, JsonElement> docs)
    {
        var records = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.Ordinal);
        foreach (var doc in docs.Values)
        {
            var kind = doc.GetProperty("kind").GetString()!;
            var id = doc.GetProperty("id").GetString()!;
            if (!records.TryGetValue(kind, out var category)) records[kind] = category = new(StringComparer.Ordinal);
            if (!category.TryAdd(id, doc)) throw new ScenarioContractException("PACK_DUPLICATE_ID", kind + ":" + id);
        }
        return records;
    }
    private static void ValidateManifest(JsonElement manifest, Dictionary<string, JsonElement> docs)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.GetProperty("files").EnumerateArray())
        {
            var path = file.GetProperty("path").GetString()!;
            if (!files.Add(path)) throw new ScenarioContractException("PACK_DUPLICATE_PATH", path);
            if (!docs.TryGetValue(path, out var doc) || doc.GetProperty("kind").GetString() != file.GetProperty("kind").GetString())
                throw new ScenarioContractException("PACK_REFERENCE", path);
        }
        if (files.Count != docs.Count) throw new ScenarioContractException("PACK_UNDECLARED", "Undeclared document.");
    }
    private static string Digest(JsonElement manifest, Dictionary<string, JsonElement> docs)
    {
        // The normalized API accepts JSON records, not raw ZIP byte layout.
        var hashes = docs.ToDictionary(p => p.Key, p => ContractJson.HashElement(p.Value), StringComparer.Ordinal);
        return ContractJson.Hash(new { manifest, files = hashes });
    }
}
