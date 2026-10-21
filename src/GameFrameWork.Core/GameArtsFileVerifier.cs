using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace GameFrameWork.Core;

/// <summary>
/// Reads real GameArts files under a trusted asset root. Checks SHA-256 and
/// PNG IHDR dimensions plus basic JSON validity. This does NOT prove measured
/// frame/pivot/attachment accuracy or GameArts provenance.
/// </summary>
public sealed class GameArtsFileVerifier(string assetRoot)
{
    private readonly string _root = Path.GetFullPath(assetRoot);

    public async Task<ArtValidationResult> VerifyAsync(ApprovedArtManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var issues = new List<AssetValidationIssue>();
        foreach (var sheet in manifest.Sheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await CheckAsync(sheet.ImagePath, sheet.ImageSha256, sheet.Id, true);
            await CheckAsync(sheet.AtlasPath, sheet.AtlasSha256, sheet.Id, false);
        }
        return new(issues);

        async Task CheckAsync(string relative, string expectedHash, string sheetId, bool png)
        {
            string path;
            try
            {
                if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                    throw new ArgumentException("Asset path must be relative.");
                path = Path.GetFullPath(Path.Combine(_root, relative));
                if (!path.StartsWith(_root.TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(path))
                    throw new IOException("Asset missing or escapes asset root.");
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
            {
                issues.Add(new("ASSET_PATH_INVALID", sheetId, ex.Message));
                return;
            }
            if (expectedHash.Length != 64 || !expectedHash.All(Uri.IsHexDigit))
            {
                issues.Add(new("ASSET_HASH_INVALID", sheetId, "Expected SHA-256 must contain 64 hex digits."));
                return;
            }
            await using var stream = File.OpenRead(path);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            if (!actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new("ASSET_HASH_MISMATCH", sheetId, "Asset bytes do not match approved SHA-256."));
                return;
            }
            stream.Position = 0;
            if (png)
            {
                var header = new byte[24];
                if (await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false,
                    cancellationToken) != header.Length ||
                    !header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ||
                    !header.AsSpan(12, 4).SequenceEqual("IHDR"u8) ||
                    BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(16, 4)) == 0 ||
                    BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(20, 4)) == 0)
                    issues.Add(new("PNG_HEADER_INVALID", sheetId, "Invalid PNG signature or dimensions."));
            }
            else
            {
                try
                {
                    using var atlas = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    if (atlas.RootElement.ValueKind != JsonValueKind.Object)
                        issues.Add(new("ATLAS_JSON_INVALID", sheetId, "Atlas root must be an object."));
                }
                catch (JsonException)
                {
                    issues.Add(new("ATLAS_JSON_INVALID", sheetId, "Atlas is not valid JSON."));
                }
            }
        }
    }
}
