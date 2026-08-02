# Save Format

The prototype currently uses save version **1**.

- Slot: one local file at `user://ashwood_county_save_v1.json`
- Format: JSON produced from explicit `SaveGameDataV1` data-transfer classes
- Live Godot nodes are never serialized
- Loading accepts version 1 only and validates all item IDs, container paths,
  zombie paths and numeric values before changing the running scene
- Saving writes and flushes a temporary file before atomically replacing the
  primary file on the same filesystem
- Unsupported future versions and malformed data are rejected before any live
  state is changed

Version 1 stores player transform, health, stamina, hunger, thirst, inventory,
the antibiotics and service-station objective states, world time, existing
container search/content state and the alive/dead state of all placed prototype
zombies. Main Street saves also add the current weather kind, remaining weather
and next-lightning timers, plus independent deterministic schedule/lightning RNG
states. Older version-1 saves omit those weather fields and retain the scene's
authored starting weather. The active transition blend and an in-progress
lightning flash are deliberately transient; loading applies the saved target
weather immediately. Saves created
before needs were added load with full hunger and thirst.
Saves without the later service-station objective field retain a locked default;
when antibiotics were already completed, loading normalizes that state to the
new objective's search step. When new authored containers or zombies are added
to the scene, older valid saves leave those new nodes in their default state.
Safe Point Storage is captured through the same container path, searched-state
and item-stack records as every other world container.

Inventory and container item arrays store one entry per stack. Repeated item
identifiers are valid and preserve intentionally split stacks. Each quantity is
restored within the current item stack limit; an older oversized version 1 entry
is divided into bounded stacks when available inventory capacity permits.

Player stack entries also include an additive `SlotIndex` field. Saves written
before stable backpack slots were introduced omit it and continue to load in
sequential order; current saves preserve quick-slot and backpack placement so
using or transferring one item cannot silently remap another hotkey after a
reload. Container records remain compact and new runtime transfers obey their
authored physical slot capacity. Version-1 container saves created before those
limits existed may restore visibly above the current capacity rather than
discarding items or rejecting the complete save; taking stacks naturally clears
that deterministic overflow. Player inventory capacity/carry-weight validation
remains strict. Each inventory is restored as one notification rather than
exposing partially reconstructed contents to gameplay listeners.
