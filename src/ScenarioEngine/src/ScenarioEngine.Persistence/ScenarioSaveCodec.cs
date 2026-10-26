using System.Collections.Immutable;
using System.Text.Json;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Persistence;

public sealed class ScenarioSaveCodec : IScenarioSaveCodec
{
    public const string BuildId = "scenario-engine-0.1";
    public byte[] Encode(SaveDocument document)
    {
        if (document.SaveFormat != 1 || document.EngineBuildId != BuildId)
            throw new ScenarioContractException("SAVE_VERSION", "Unsupported save version.");
        var payload = ContractJson.Element(document);
        return ContractJson.Canonical(ContractJson.Element(new
        {
            envelopeVersion = 1, randomAlgorithm = "pcg32-xsh-rr-v1",
            payload, payloadDigest = ContractJson.HashElement(payload)
        }));
    }
    public SaveDecodeResult Decode(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length > 16 * 1024 * 1024) return Failed("SAVE_SIZE", "Save exceeds 16 MiB.");
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 64 });
            var root = document.RootElement;
            ContractJson.RejectDuplicateProperties(root);
            if (root.EnumerateObject().Count() != 4 || root.GetProperty("envelopeVersion").GetInt32() != 1 ||
                root.GetProperty("randomAlgorithm").GetString() != "pcg32-xsh-rr-v1")
                return Failed("SAVE_VERSION", "Unsupported save envelope.");
            var payload = root.GetProperty("payload");
            if (ContractJson.HashElement(payload) != root.GetProperty("payloadDigest").GetString())
                return Failed("SAVE_CORRUPT", "Payload digest mismatch.");
            var save = payload.Deserialize<SaveDocument>(ContractJson.Options);
            if (save is null || save.SaveFormat != 1 || save.EngineBuildId != BuildId)
                return Failed("SAVE_VERSION", "Unsupported save payload.");
            if (save.Snapshot is null || save.Snapshot.Identity is null || save.Snapshot.Layer is null ||
                save.Snapshot.Controllers is null || save.Snapshot.Controllers.IsEmpty ||
                save.Snapshot.Objectives is null || save.Snapshot.RandomStreams is null ||
                save.Snapshot.AppliedEffects is null || save.Snapshot.Receipts is null ||
                save.Snapshot.PendingCommands.IsDefault || save.Snapshot.Tick < 0 ||
                save.Snapshot.Layer.Data.ValueKind != JsonValueKind.Object)
                return Failed("SAVE_CORRUPT", "Required snapshot values are missing or invalid.");
            SessionSnapshotRules.Validate(save.Snapshot);
            return new(save, ValidationReport.Valid);
        }
        catch (ScenarioContractException exception)
        {
            return Failed("SAVE_CORRUPT", "Invalid snapshot: " + exception.Code);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            return Failed("SAVE_CORRUPT", "Invalid save structure: " + exception.GetType().Name);
        }
    }
    private static SaveDecodeResult Failed(string code, string message) => new(null, new([new(code, message)]));
}
