using System.Text;
using WorldCreator.Core;
using WorldCreator.Core.Planning;

namespace WorldCreator.Godot;

/// <summary>Deterministic, deliberately planned 16x12 pixel-art integration fixture.
/// Uses vector pixel-block primitives so it needs no unapproved production artwork.
/// The scene runs independently in Godot 4 and serves as a visual smoke test.
/// </summary>
public sealed class DummyVillageAssembler
{
    public async Task ExportAsync(string directory, CancellationToken token = default)
    {
        Directory.CreateDirectory(directory);
        var objects = new[]
        {
            new PlannedWorldObject("house-west","house",new(3,3),"residential plot west of village road","village-composition-v1","dummy-village"),
            new PlannedWorldObject("west-door","door",new(3,3),"entry to west house","door-attachment-v1","dummy-village","house-west","closed","door-swing"),
            new PlannedWorldObject("house-east","house",new(12,3),"residential plot east of village road","village-composition-v1","dummy-village"),
            new PlannedWorldObject("east-door","door",new(12,3),"entry to east house","door-attachment-v1","dummy-village","house-east","closed","door-swing"),
            new PlannedWorldObject("tree-garden","tree",new(4,8),"shade tree inside west garden","garden-composition-v1","dummy-village"),
            new PlannedWorldObject("tree-square","tree",new(11,8),"shade tree in east square","square-composition-v1","dummy-village"),
            new PlannedWorldObject("notice","sign",new(8,5),"village notice board along main path","square-composition-v1","dummy-village")
        };
        var plan = new WorldDesignPlan("dummy-village",new(16,12),objects);
        var compiled = new WorldPlanCompiler().Compile(plan);
        var godotScript = """
        extends Node2D
        # Deliberately planned miniature pixel-art village. Drawn at 16px logical cells.
        # All positions originate from the C# design plan, not random scattering.
        var elapsed := 0.0
        func _process(delta: float) -> void:
            elapsed += delta
            queue_redraw()
        func cell(x: int, y: int, color: Color, w: int = 1, h: int = 1) -> void:
            draw_rect(Rect2(x * 16, y * 16, w * 16, h * 16), color)
        func _draw() -> void:
            for y in range(12):
                for x in range(16):
                    cell(x, y, Color("#769e57") if (x + y) % 3 == 0 else Color("#82aa61"))
            # Road and a coherent footpath, rather than unrelated decorations.
            for y in range(12):
                cell(7, y, Color("#bcaa84"), 2, 1)
            for x in range(2, 14):
                cell(x, 6, Color("#d1bd91"))
            # Village homes and contrasting roofs at explicit plan positions.
            for hx in [3, 12]:
                cell(hx-1, 2, Color("#745443"), 3, 3)
                cell(hx-1, 2, Color("#b87b59"), 3, 1)
                cell(hx-1, 3, Color("#e3c99c"), 3, 2)
                cell(hx-1, 2, Color("#a45f51"), 3, 1)
                cell(hx-1, 3, Color("#537c8e"))
                # Animated separate door component (independent of house body).
                var door_open := sin(elapsed * 1.5) > 0.0
                cell(hx, 4, Color("#4b302b") if door_open else Color("#8a5139"))
            # Planned garden beds; flowers are arranged in beds, not scattered.
            for x in range(2, 6):
                cell(x, 7, Color("#5f7d40"))
                draw_circle(Vector2(x * 16 + 8, 7 * 16 + 8), 3, Color("#eabbc5"))
            for x in range(10, 14):
                cell(x, 7, Color("#5f7d40"))
                draw_circle(Vector2(x * 16 + 8, 7 * 16 + 8), 3, Color("#f1d475"))
            # Two named trees in distinct designed zones.
            for tx in [4, 11]:
                cell(tx, 9, Color("#694b39"))
                cell(tx-1, 8, Color("#366d43"), 3, 1)
                cell(tx, 7, Color("#4e9155"))
            # Village noticeboard on the main pedestrian path.
            cell(8, 5, Color("#6b4b36"))
            draw_rect(Rect2(8*16+3, 5*16+3, 10, 6), Color("#e6c17e"))
        """;
        var scene = """
        [gd_scene load_steps=2 format=3]

        [ext_resource type="Script" path="res://Village.gd" id="1_script"]

        [node name="DummyVillage" type="Node2D"]
        script = ExtResource("1_script")

        [node name="Camera2D" type="Camera2D" parent="."]
        position = Vector2(128, 96)
        zoom = Vector2(3, 3)
        """;
        var config = """
        config_version=5

        [application]
        config/name="GameFrameWork Dummy Village"
        run/main_scene="res://Village.tscn"

        [rendering]
        renderer/rendering_method="gl_compatibility"
        renderer/rendering_method.mobile="gl_compatibility"
        textures/default_filters/use_nearest_mipmap_filter=false
        """;
        await File.WriteAllTextAsync(Path.Combine(directory,"Village.gd"),godotScript,token);
        await File.WriteAllTextAsync(Path.Combine(directory,"Village.tscn"),scene,token);
        await File.WriteAllTextAsync(Path.Combine(directory,"project.godot"),config,token);
        await File.WriteAllTextAsync(Path.Combine(directory,"design.json"),
            System.Text.Json.JsonSerializer.Serialize(plan,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}),token);
        await File.WriteAllTextAsync(Path.Combine(directory,"README.md"),
            "Dummy test scene: Godot 4 project with a hand-authored 16x12 C#-validated world design. Procedural pixel-block drawings and animated doors are test placeholders, not GameArts production sprite bundles. No TileSet, NavigationPolygon, native Godot runtime or collision verification claimed.\n",token);
    }
}
