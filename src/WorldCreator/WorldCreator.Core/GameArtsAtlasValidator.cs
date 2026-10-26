using System.Buffers.Binary;
using System.Text.Json;

namespace WorldCreator.Core;

/// <summary>Validates GameArts v7.7 measured atlas geometry against the actual PNG header.</summary>
public sealed class GameArtsAtlasValidator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(string atlasFile, string pngFile,
        CancellationToken token = default)
    {
        var errors = new List<string>();
        await using var png = File.OpenRead(pngFile);
        var header = new byte[24];
        if (await png.ReadAtLeastAsync(header, 24, throwOnEndOfStream: false, token) != 24 ||
            !header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ||
            !header.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            return ["PNG_HEADER_INVALID"];
        var width = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(20, 4));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(atlasFile, token));
        var root = json.RootElement;
        try
        {
            if (root.GetProperty("schema_version").GetString() != "1.0")
                errors.Add("ATLAS_SCHEMA_UNSUPPORTED");
            if (root.GetProperty("mask_color").GetString() != "#FF00FF" ||
                root.GetProperty("background_alpha").GetInt32() != 255)
                errors.Add("ATLAS_MASK_METADATA_INVALID");
            var size = root.GetProperty("image_size");
            if (size[0].GetInt32() != width || size[1].GetInt32() != height)
                errors.Add("ATLAS_IMAGE_SIZE_MISMATCH");
            int index = 0;
            foreach (var frame in root.GetProperty("frames").EnumerateArray())
            {
                var rect = frame.GetProperty("rect");
                int x = rect[0].GetInt32(), y = rect[1].GetInt32();
                int w = rect[2].GetInt32(), h = rect[3].GetInt32();
                if (x < 0 || y < 0 || w <= 0 || h <= 0 ||
                    (long)x + w > width || (long)y + h > height)
                    errors.Add($"FRAME_OUT_OF_BOUNDS:{index}");
                var pivot = frame.GetProperty("pivot");
                if (pivot[0].GetInt32() < 0 || pivot[1].GetInt32() < 0 ||
                    pivot[0].GetInt32() >= w || pivot[1].GetInt32() >= h)
                    errors.Add($"PIVOT_OUT_OF_BOUNDS:{index}");
                if (frame.TryGetProperty("anchors", out var anchors))
                    foreach (var anchor in anchors.EnumerateObject())
                    {
                        int ax = anchor.Value[0].GetInt32(), ay = anchor.Value[1].GetInt32();
                        if (ax < 0 || ay < 0 || ax >= w || ay >= h)
                            errors.Add($"ANCHOR_OUT_OF_BOUNDS:{index}:{anchor.Name}");
                    }
                index++;
            }
            if (index == 0) errors.Add("ATLAS_FRAMES_EMPTY");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or
            IndexOutOfRangeException or ArgumentOutOfRangeException or FormatException)
        {
            errors.Add("ATLAS_STRUCTURE_INVALID");
        }
        return errors;
    }
}
