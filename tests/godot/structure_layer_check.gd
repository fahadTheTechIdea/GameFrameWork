extends SceneTree

func _initialize() -> void:
	var package = JSON.parse_string(FileAccess.get_file_as_string("res://instance-assembly.json"))
	var expected: Dictionary = {}
	for item in package["Instances"]:
		expected[item["Placement"]["Id"]] = item["Placement"]["VisualSource"]["PositionMeters"]
	var scene = load("res://Structures.tscn") as PackedScene
	assert(scene != null)
	var layer = scene.instantiate() as Node2D
	root.add_child(layer)
	assert(layer.y_sort_enabled)
	assert(layer.get_child_count() == expected.size())
	var seen: Dictionary = {}
	for child in layer.get_children():
		assert(child is Sprite2D)
		var sprite = child as Sprite2D
		var identifier = sprite.get_meta("object_id")
		assert(expected.has(identifier) and not seen.has(identifier))
		seen[identifier] = true
		assert(sprite.texture is AtlasTexture)
		assert((sprite.texture as AtlasTexture).region == Rect2(0, 0, 16, 16))
		assert(sprite.position.is_equal_approx(Vector2(expected[identifier]["X"], expected[identifier]["Y"]) * 16.0))
		assert(sprite.scale == Vector2(16, 16))
		assert(sprite.offset == Vector2(-8, -8) and not sprite.centered)
		assert(sprite.texture_filter == CanvasItem.TEXTURE_FILTER_NEAREST)
		assert(sprite.material is ShaderMaterial)
	print("PASS: native Godot structure layer count, unique IDs, atlas regions, engine positions, ground pivots, scale, nearest filtering and y-sort (synthetic art).")
	quit(0)
