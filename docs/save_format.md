# Save Format

The prototype currently uses save version **1**.

- Slot: one local file at `user://ashwood_county_save_v1.json`
- Format: JSON produced from explicit `SaveGameDataV1` data-transfer classes
- Live Godot nodes are never serialized
- Loading accepts version 1 only and validates all item IDs, container paths,
  zombie paths and numeric values before changing the running scene

Version 1 stores player transform, health, stamina, hunger, thirst, inventory,
the antibiotics objective state, world time, existing container search/content
state and the alive/dead state of all placed prototype zombies. Saves created
before needs were added load with full hunger and thirst; broader backward
compatibility is intentionally not provided yet. When new authored containers
or zombies are added to the scene, older valid saves leave those new nodes in
their default state.
