using ScenarioEngine.Contracts;

namespace ScenarioEngine.Content;

public sealed partial class ScenarioContentService
{
    public async Task<ContentLoadResult> LoadAsync(IScenarioPackSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        ReadOnlyMemory<byte> bytes;
        try { bytes = await source.ReadAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            _reporter.Report(new("PACK_CANCELLED", "Content source read cancelled.", Severity.Information));
            throw;
        }
        catch (Exception exception)
        {
            _reporter.Report(new("PACK_SOURCE_FAILED", "Unable to read content source.", Severity.Error, Exception: exception));
            return Refused("PACK_SOURCE_FAILED", "Unable to read content source; consult diagnostics.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        return LoadNormalized(bytes, cancellationToken);
    }

    public ContentRemovalResult RemoveFromCatalog(string packId, string contentDigest)
    {
        lock (_gate)
        {
            var identities = _catalog.Keys.Where(k => StringComparer.Ordinal.Equals(k.PackId, packId) &&
                StringComparer.Ordinal.Equals(k.ContentDigest, contentDigest)).ToArray();
            if (identities.Length == 0) return new(false, "PACK_NOT_INSTALLED", 0);
            // Existing sessions retain their validated immutable definitions.
            try { _reporter.Audit(new("PackRemoved", packId, "committed")); }
            catch (Exception exception)
            {
                _reporter.Report(new("PACK_REMOVE_FAILED", "Pack remains installed because audit failed.", Severity.Error, Exception: exception));
                return new(false, "PACK_REMOVE_FAILED", 0);
            }
            _catalog = _catalog.RemoveRange(identities);
            return new(true, "PACK_REMOVED", identities.Length);
        }
    }
}
