using ScenarioEngine.Contracts;

namespace ScenarioEngine.Persistence;

public sealed class ScenarioSaveService(IScenarioSaveCodec codec, ISaveStore store, IEngineReporter reporter) : IScenarioSaveRecoveryService
{
    public async Task<SaveResult> SaveAsync(string slotId, SessionSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot.Phase is SessionPhase.Faulted or SessionPhase.Disposed or SessionPhase.Transitioning)
                return new(false, "SAVE_PHASE");
            var bytes = codec.Encode(new(1, ScenarioSaveCodec.BuildId, DateTimeOffset.UtcNow, snapshot));
            var verified = codec.Decode(bytes);
            if (!verified.Report.IsValid) return new(false, "SAVE_ENCODE_INVALID");
            reporter.Audit(new("SavePrepared", slotId, "prepared", snapshot.SessionId));
            await store.WriteAtomicAsync(slotId, bytes, cancellationToken);
            reporter.Report(new("SAVE_COMMITTED", "Save committed.", Severity.Information, snapshot.SessionId));
            return new(true, "SAVE_COMMITTED");
        }
        catch (OperationCanceledException)
        {
            reporter.Report(new("SAVE_CANCELLED", "Save cancelled before commit.", Severity.Information, snapshot.SessionId));
            return new(false, "SAVE_CANCELLED");
        }
        catch (Exception exception)
        {
            reporter.Report(new("SAVE_FAILED", "Save failed; previous committed slot is preserved.", Severity.Error, snapshot.SessionId, exception));
            return new(false, "SAVE_FAILED");
        }
    }
    public async Task<SaveDecodeResult> LoadPreviousAsync(string slotId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (store is not IRecoverableSaveStore recoverable)
            return new(null, new([new("SAVE_RECOVERY_UNAVAILABLE", "The selected store does not support previous generations.")]));
        try
        {
            var bytes = await recoverable.ReadPreviousAsync(slotId, cancellationToken);
            if (bytes is null)
            {
                reporter.Audit(new("PreviousSaveRead", slotId, "missing"));
                return new(null, new([new("SAVE_PREVIOUS_MISSING", "No previous generation exists.")]));
            }
            var decoded = codec.Decode(bytes.Value);
            reporter.Audit(new("PreviousSaveRead", slotId, decoded.Report.IsValid ? "validated" : "rejected"));
            if (!decoded.Report.IsValid)
                reporter.Report(new("SAVE_PREVIOUS_CORRUPT", "Previous generation failed validation; no files were changed.", Severity.Warning));
            return decoded;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            reporter.Report(new("SAVE_RECOVERY_FAILED", "Unable to read the previous generation.", Severity.Error, Exception: exception));
            return new(null, new([new("SAVE_RECOVERY_FAILED", "Previous generation is unavailable; consult diagnostics.")]));
        }
    }
    public async Task<SaveDecodeResult> LoadAsync(string slotId, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await store.ReadAsync(slotId, cancellationToken);
            if (bytes is null) return new(null, new([new("SAVE_MISSING", "Slot does not exist.")]));
            var decoded = codec.Decode(bytes.Value);
            if (!decoded.Report.IsValid)
                reporter.Report(new("SAVE_CORRUPT", "Slot failed validation; original bytes are preserved.", Severity.Warning));
            return decoded;
        }
        catch (OperationCanceledException) { throw; } // Owned caller receives expected cancellation.
        catch (Exception exception)
        {
            reporter.Report(new("RESTORE_FAILED", "Unable to read save.", Severity.Error, Exception: exception));
            return new(null, new([new("RESTORE_FAILED", "Unable to read save; consult diagnostics.")]));
        }
    }
}
