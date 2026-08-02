extends SceneTree

func _init() -> void:
	for path in [
		"res://assets/environment/buildings/MillerHardware/fixtures/miller_lumber_rack.glb",
		"res://assets/environment/buildings/MillerHardware/fixtures/miller_warehouse_shelf.glb",
		"res://assets/environment/buildings/AshwoodGrocery/fixtures/grocery_gondola_aisle.glb",
	]:
		print("SCENE ", path)
		var packed: PackedScene = load(path)
		var instance := packed.instantiate()
		_print_node(instance, "")
		instance.free()
	quit()

func _print_node(node: Node, indent: String) -> void:
	print(indent, node.name, " [", node.get_class(), "]")
	for child in node.get_children():
		_print_node(child, indent + "  ")
