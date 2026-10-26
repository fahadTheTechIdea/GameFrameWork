using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WorldCreator.Core;

namespace WorldCreator.Godot;

/// <summary>Native Sprite2D/AtlasTexture static structure layer. No terrain, collisions,
/// navigation or component animation is implied by this deliberately scoped export.</summary>
public sealed class GodotStructureLayerExporter
{
    public async Task ExportAsync(StructureFirstProductionResult accepted, string newDirectory,
        double worldPixelsPerMeter = 16, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(accepted); token.ThrowIfCancellationRequested();
        if (!accepted.Ready || accepted.Frames.Count != accepted.Draft.Assembly.Instances.Count ||
            !accepted.Frames.Select(f => f.ObjectId).ToHashSet(StringComparer.Ordinal).SetEquals(accepted.Draft.Assembly.Instances.Select(i => i.Placement.Id)))
            throw new ArgumentException("STRUCTURE_LAYER_NOT_ACCEPTED");
        if (!double.IsFinite(worldPixelsPerMeter) || worldPixelsPerMeter is <= 0 or > 1024)
            throw new ArgumentException("STRUCTURE_LAYER_SCALE_INVALID");
        string target = Path.GetFullPath(newDirectory), parent = Path.GetDirectoryName(target)!;
        if (Directory.Exists(target) || File.Exists(target)) throw new IOException("STRUCTURE_LAYER_OUTPUT_EXISTS");
        Directory.CreateDirectory(parent);
        string staging = Path.Combine(parent, ".structure-layer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            Directory.CreateDirectory(Path.Combine(staging, "textures"));
            var textures = accepted.Frames.Select(f => f.ImageSha256).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            string N(double n) => n.ToString("R", CultureInfo.InvariantCulture);
            string Q(string s) => JsonSerializer.Serialize(s);
            var text = new StringBuilder("[gd_scene format=3]\n\n");
            for (int i = 0; i < textures.Length; i++)
            {
                var frame = accepted.Frames.First(f => f.ImageSha256 == textures[i]);
                var bytes = await File.ReadAllBytesAsync(frame.ImageFile, token);
                if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != frame.ImageSha256.ToLowerInvariant())
                    throw new ArgumentException("STRUCTURE_LAYER_IMAGE_CHANGED");
                string relative = "textures/" + textures[i].ToLowerInvariant() + ".png";
                await File.WriteAllBytesAsync(Path.Combine(staging, relative), bytes, token);
                text.Append($"[ext_resource type=\"Texture2D\" path=\"res://{relative}\" id=\"T{i}\"]\n");
            }
            const string shader = "shader_type canvas_item;\nvoid fragment(){vec4 c=texture(TEXTURE,UV);if(distance(c.rgb,vec3(1.0,0.0,1.0))<0.001){c.a=0.0;}COLOR=c;}";
            text.Append($"\n[sub_resource type=\"Shader\" id=\"KeyShader\"]\ncode = {Q(shader)}\n\n[sub_resource type=\"ShaderMaterial\" id=\"KeyMaterial\"]\nshader = SubResource(\"KeyShader\")\n");
            for (int i = 0; i < accepted.Frames.Count; i++)
            {
                var f = accepted.Frames[i]; int texture = Array.IndexOf(textures, f.ImageSha256);
                text.Append($"\n[sub_resource type=\"AtlasTexture\" id=\"A{i}\"]\natlas = ExtResource(\"T{texture}\")\nregion = Rect2({f.FrameX}, {f.FrameY}, {f.FrameWidth}, {f.FrameHeight})\nfilter_clip = true\n");
            }
            text.Append("\n[node name=\"Structures\" type=\"Node2D\"]\ny_sort_enabled = true\n");
            for (int i = 0; i < accepted.Frames.Count; i++)
            {
                var f = accepted.Frames[i]; double scale = worldPixelsPerMeter/f.PixelsPerMeter;
                text.Append($"\n[node name=\"Structure{i}\" type=\"Sprite2D\" parent=\".\"]\ntexture_filter = 1\nmaterial = SubResource(\"KeyMaterial\")\n" +
                    $"position = Vector2({N(f.PositionMeters.X*worldPixelsPerMeter)}, {N(f.PositionMeters.Y*worldPixelsPerMeter)})\n" +
                    $"scale = Vector2({N(scale)}, {N(scale)})\ntexture = SubResource(\"A{i}\")\ncentered = false\noffset = Vector2({-f.Pivot.X}, {-f.Pivot.Y})\nmetadata/object_id = {Q(f.ObjectId)}\n");
            }
            await File.WriteAllTextAsync(Path.Combine(staging, "Structures.tscn"), text.ToString(), token);
            await File.WriteAllTextAsync(Path.Combine(staging, "project.godot"),
                "config_version=5\n[application]\nconfig/name=\"Verified static structure layer\"\nrun/main_scene=\"res://Structures.tscn\"\n[rendering]\nrenderer/rendering_method=\"gl_compatibility\"\n", token);
            await File.WriteAllTextAsync(Path.Combine(staging, "instance-assembly.json"), JsonSerializer.Serialize(accepted.Draft.Assembly, WorldJson.CreateOptions(true)), token);
            await File.WriteAllTextAsync(Path.Combine(staging, "STATUS.md"),
                "Accepted static structure layer only. Native Sprite2D, AtlasTexture, calibrated ground pivots/scale and y-sorting. " +
                "Terrain, collisions, navigation, moving components, whole-game and visual acceptance are separate gates.\n", token);
            token.ThrowIfCancellationRequested(); Directory.Move(staging, target);
        }
        finally
        {
            if (Directory.Exists(staging) && Path.GetDirectoryName(Path.GetFullPath(staging)) == parent &&
                Path.GetFileName(staging).StartsWith(".structure-layer-", StringComparison.Ordinal)) Directory.Delete(staging, true);
        }
    }
}
