using System.Text.Json;

namespace WorldCreator.Core;

/// <summary>Validates GameArts 1.0 sampled poses and motion-track metadata; not a Godot playback test.</summary>
public sealed class GameArtsMotionValidator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(string bundleFile, CancellationToken token = default)
    {
        var errors = new List<string>();
        var parsed = await new GameArtsBundleReader().ReadAsync(bundleFile, token);
        if (!parsed.IsValid) return parsed.Errors;
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(bundleFile, token));
        var root = doc.RootElement;
        if (!root.TryGetProperty("sampled_poses", out var poses) || poses.ValueKind != JsonValueKind.Array)
            return ["SAMPLED_POSES_MISSING"];
        var directions = parsed.Bundle!.Directions.ToHashSet(StringComparer.Ordinal);
        var ids = parsed.Bundle.Components.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pose in poses.EnumerateArray())
        {
            try
            {
                string direction = pose.GetProperty("direction").GetString() ?? "";
                double time = pose.GetProperty("normalized_time").GetDouble();
                if (!directions.Contains(direction) || !double.IsFinite(time) || time is < 0 or > 1)
                {
                    errors.Add("POSE_DIRECTION_OR_TIME_INVALID");
                    continue;
                }
                var key = direction + ":" + time.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                if (!seen.Add(key)) errors.Add("DUPLICATE_POSE");
                var nodes = pose.GetProperty("nodes");
                foreach (var id in ids)
                {
                    if (!nodes.TryGetProperty(id, out var node))
                    {
                        errors.Add("POSE_COMPONENT_MISSING:" + id);
                        continue;
                    }
                    var matrix = node.GetProperty("world_matrix_2d");
                    if (matrix.ValueKind != JsonValueKind.Array || matrix.GetArrayLength() != 3 ||
                        matrix.EnumerateArray().Any(row => row.ValueKind != JsonValueKind.Array ||
                            row.GetArrayLength() != 3 ||
                            row.EnumerateArray().Any(v => v.ValueKind != JsonValueKind.Number ||
                                !double.IsFinite(v.GetDouble()))))
                        errors.Add("POSE_MATRIX_INVALID:" + id);
                    if (node.GetProperty("frame_index").GetInt32() < 0)
                        errors.Add("POSE_FRAME_INDEX_INVALID:" + id);
                }
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or
                FormatException or IndexOutOfRangeException)
            {
                errors.Add("POSE_STRUCTURE_INVALID");
            }
        }
        foreach (var direction in directions)
            if (!poses.EnumerateArray().Any(p => p.TryGetProperty("direction", out var d) &&
                d.ValueKind == JsonValueKind.String && d.GetString() == direction))
                errors.Add("POSE_DIRECTION_MISSING:" + direction);
        if (poses.GetArrayLength() == 0) errors.Add("POSES_EMPTY");
        return errors;
    }
}
