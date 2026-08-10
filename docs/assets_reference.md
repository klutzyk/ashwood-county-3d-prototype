# Asset Reference

This document records third-party visual assets used by Ashwood County. Keep the
source manifest with the imported files so individual download URLs and file sizes
remain auditable.

## Poly Haven Environment Pack (August 2026)

- Source: https://polyhaven.com
- License: CC0 1.0 Universal
- License page: https://polyhaven.com/license
- Attribution required: no
- Download manifest: `res://assets/third_party/polyhaven_2026_08/MANIFEST.json`
- Imported source root: `res://assets/third_party/polyhaven_2026_08/`
- Project-owned Godot wrappers: `res://assets/environment/nature/polyhaven/`
- Project-owned material overrides: `res://assets/materials/vegetation_*.tres`

The manifest is the authoritative record of exact source URLs, byte sizes, and
the original download timestamp. Relevant nature assets include:

- Trees: `fir_tree_01`, `pine_tree_01`, saplings, and `tree_small_02`
- Understory: `fern_02`, `nettle_plant`, `shrub_01` through `shrub_03`, grasses,
  and moss
- Forest floor: `bark_debris_01`, `dry_branches_medium_01`, `pine_roots`, dead
  trunks, logs, and stumps
- Stone: `boulder_01`, `rock_moss_set_01`, `aerial_rocks_02`, and `cliff_side`
- Ground materials: `forrest_ground_01`, `brown_mud_leaves_01`, dry mud, gravel,
  pebbles, grass-rock, and asphalt sets
- Lighting: the Syferfontein clear and Kloofendal partly-cloudy HDR skies

### County Usage

`CountyVegetation` uses deterministic MultiMesh batches throughout every streamed
playable chunk. The layers now include mature and distant canopy, conifer
understory, forest-edge regrowth, shrubs, ferns, meadow and field grass, parcel
hedgerows, wet-ground plants, forest-floor litter, deadwood, wild boulders,
upland debris, scree, and riverbank growth. Placement is driven by the county's
ten habitats rather than isolated hand-placed clusters.

Distance ranges and density scale with the graphics preset. Short-range grass,
wet ground, rocks, and litter retain minimum density on Low so the immediate
play space remains dressed, while distant alpha-heavy geometry is thinned. The
editor preview additionally builds a single 19,000-plus-tree imposter batch over
the full landmass, outside the detailed local preview radius, so the county does
not appear empty while editing `main_street.tscn` or `prototype_world.tscn`.

`CountyMap.FieldMarginStrength` generates continuous hedgerow habitat directly
from the rotated farm parcel grid. `CountyMap.HabitatAt` classifies settled land,
fields, meadows, upland scrub, forest edge, mixed woodland, conifer interior,
riparian ground, scree, and alpine terrain for both scatter and validation.

`CountyNaturalFeatures` uses the collidable wrapper scenes for Blackwater Cavern,
Mill Creek Grotto, Granite Narrows, Pine Ridge Overlook, South Ridge Escarpment,
and Old Growth Hollow. These landmarks stream independently of mapped settlement
POIs and retain the original source assets unchanged.

No additional third-party binary files were downloaded for the August 10 nature
pass. The existing CC0 pack already covered the required forest, rock, debris, and
ground categories, avoiding duplicate assets and unnecessary repository growth.
