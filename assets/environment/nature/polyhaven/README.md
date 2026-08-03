# Ashwood photoscanned vegetation

Project-owned, decimated derivatives of CC0 Poly Haven photoscans, built by
`tools/blender/build_ashwood_vegetation.py`. These replace the stylised lowpoly
trees (salmon-pink trunks, flat cartoon-green foliage) that were breaking the
State of Decay / Mist Survival look.

Rebuild everything with:

```
python tools/download_polyhaven.py --set vegetation
python tools/download_polyhaven.py --set rocks
blender --background --python tools/blender/build_ashwood_vegetation.py
```

Sources under `assets/third_party/polyhaven_2026_08/` are never modified.
Poly Haven assets are CC0, so this is commercial-safe with no attribution
requirement.

## Wrapper scenes

Each `.tscn` is a `Node3D`/`StaticBody3D` root with the `.glb` instanced as
`Visual` and materials bound via `surface_material_override/0` on the
`MeshInstance3D` children - the same shape as
`assets/environment/nature/common_tree_1.tscn`, so
`OldMillBridge.ScatterVegetation` can consume them directly.

| Scene (`res://`) | Asset | Poly Haven slug | Source tris | Final tris | Size | Collision |
| --- | --- | --- | ---: | ---: | --- | --- |
| `res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod0.tscn` | Jacaranda (hero, LOD0) | `jacaranda_tree` | 3,863,832 | **18,982** | 24.6x19.0x19.5 m | cylinder |
| `res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod1.tscn` | Jacaranda (mid/background, LOD1) | `jacaranda_tree` | 3,863,832 | **10,587** | 24.3x19.2x19.4 m | cylinder |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_01.tscn` | Broad low shrub | `shrub_01` | 156,012 | **882** | 2.7x0.3x0.7 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_02_a.tscn` | Leafy shrub A | `shrub_02` | 7,590 | **250** | 1.3x1.7x1.8 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_02_b.tscn` | Leafy shrub B | `shrub_02` | 5,242 | **182** | 1.1x1.3x1.6 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_02_c.tscn` | Leafy shrub C | `shrub_02` | 9,234 | **322** | 1.8x2.3x1.4 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_02_d.tscn` | Leafy shrub D | `shrub_02` | 5,188 | **174** | 1.1x1.1x1.3 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_03_a.tscn` | Small shrub A | `shrub_03` | 2,385 | **90** | 0.2x0.2x0.4 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_03_b.tscn` | Small shrub B | `shrub_03` | 2,134 | **86** | 0.2x0.1x0.4 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_03_c.tscn` | Small shrub C | `shrub_03` | 1,790 | **76** | 0.1x0.1x0.3 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_shrub_03_d.tscn` | Small shrub D | `shrub_03` | 1,978 | **82** | 0.1x0.1x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_fern_02_a.tscn` | Fern A | `fern_02` | 784 | **380** | 0.6x0.6x0.3 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_fern_02_b.tscn` | Fern B | `fern_02` | 2,384 | **380** | 1.0x0.9x0.4 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_fern_02_c.tscn` | Fern C | `fern_02` | 2,248 | **300** | 0.9x0.8x0.4 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_fern_02_d.tscn` | Fern D | `fern_02` | 816 | **300** | 0.6x0.6x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_nettle_tall.tscn` | Nettle clump (tall) | `nettle_plant` | 15,064 | **675** | 0.2x0.1x0.3 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_nettle_medium.tscn` | Nettle clump (medium) | `nettle_plant` | 11,560 | **556** | 0.2x0.1x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_nettle_small.tscn` | Nettle clump (small) | `nettle_plant` | 4,680 | **399** | 0.2x0.0x0.1 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_grass_bermuda_medium.tscn` | Bermuda grass (medium) | `grass_bermuda_01` | 113 | **113** | 0.2x0.1x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_grass_bermuda_small.tscn` | Bermuda grass (small) | `grass_bermuda_01` | 72 | **72** | 0.2x0.0x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_grass_bermuda_dry.tscn` | Bermuda grass (dry) | `grass_bermuda_01` | 330 | **330** | 0.2x0.1x0.2 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_dead_tree_trunk.tscn` | Standing dead trunk | `dead_tree_trunk` | 101,802 | **800** | 3.1x0.3x0.3 m | cylinder |
| `res://assets/environment/nature/polyhaven/ashwood_dead_log.tscn` | Fallen log | `dead_tree_trunk_02` | 83,128 | **700** | 4.0x0.9x1.1 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_bark_debris_a.tscn` | Bark debris A | `bark_debris_01` | 38,488 | **500** | 0.2x0.4x0.1 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_bark_debris_b.tscn` | Bark debris B | `bark_debris_01` | 48,622 | **500** | 0.2x0.5x0.1 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_bark_debris_c.tscn` | Bark debris C | `bark_debris_01` | 65,062 | **499** | 0.1x0.6x0.1 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_bark_debris_d.tscn` | Bark debris D | `bark_debris_01` | 41,234 | **500** | 0.1x0.6x0.1 m | none |
| `res://assets/environment/nature/polyhaven/ashwood_boulder_01.tscn` | Boulder | `boulder_01` | 66,122 | **26,163** | 1.3x1.8x1.0 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_01.tscn` | Mossy rock 01 | `rock_moss_set_01` | 11,000 | **400** | 2.3x3.4x1.6 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_02.tscn` | Mossy rock 02 | `rock_moss_set_01` | 10,996 | **400** | 2.6x3.3x1.3 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_03.tscn` | Mossy rock 03 | `rock_moss_set_01` | 5,000 | **400** | 2.1x2.1x1.1 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_04.tscn` | Mossy rock 04 | `rock_moss_set_01` | 10,589 | **400** | 2.1x2.0x1.8 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_05.tscn` | Mossy rock 05 | `rock_moss_set_01` | 16,548 | **400** | 1.8x3.0x1.2 m | convex |
| `res://assets/environment/nature/polyhaven/ashwood_rock_moss_06.tscn` | Mossy rock 06 | `rock_moss_set_01` | 8,994 | **400** | 2.1x2.8x1.3 m | convex |

Total across all 34 assets: **67,280 triangles**.

## Per-part detail

`degenerate UV` is the fraction of live triangles with zero UV area - the direct
measure of whether the decimate wrecked the texture mapping. `alpha` is the
surface-area-weighted mean opacity sampled at triangle UV centroids, which is
what catches a misaligned or inverted cut-out mask.

| Asset | Part | Method | Source tris | Final tris | Degenerate UV | Alpha | Cards | Material |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `ashwood_jacaranda_lod0` | Trunk | decimate | 230,112 | 700 | 0.0000 | - | - | `vegetation_jacaranda_trunk.tres` |
| `ashwood_jacaranda_lod0` | Branches | decimate | 1,231,286 | 5,448 | 0.0000 | - | - | `vegetation_jacaranda_branches.tres` |
| `ashwood_jacaranda_lod0` | Leaves | cards | 2,402,434 | 12,834 | 0.0000 | 0.262 | 6,417 of 116,084 @ x1.55 | `vegetation_jacaranda_leaves.tres` |
| `ashwood_jacaranda_lod1` | Trunk | decimate | 230,112 | 220 | 0.0000 | - | - | `vegetation_jacaranda_trunk.tres` |
| `ashwood_jacaranda_lod1` | Branches | decimate | 1,231,286 | 2,977 | 0.0000 | - | - | `vegetation_jacaranda_branches.tres` |
| `ashwood_jacaranda_lod1` | Leaves | cards | 2,402,434 | 7,390 | 0.0000 | 0.264 | 3,695 of 116,084 @ x1.85 | `vegetation_jacaranda_leaves.tres` |
| `ashwood_shrub_01` | Plant | cards | 156,012 | 882 | 0.0000 | 0.999 | 441 of 1,736 @ x2.0 | `vegetation_shrub_01.tres` |
| `ashwood_shrub_02_a` | Plant | cards | 7,590 | 250 | 0.0000 | 0.870 | 125 of 125 @ x1.0 | `vegetation_shrub_02.tres` |
| `ashwood_shrub_02_b` | Plant | cards | 5,242 | 182 | 0.0000 | 0.876 | 91 of 91 @ x1.0 | `vegetation_shrub_02.tres` |
| `ashwood_shrub_02_c` | Plant | cards | 9,234 | 322 | 0.0000 | 0.871 | 161 of 161 @ x1.0 | `vegetation_shrub_02.tres` |
| `ashwood_shrub_02_d` | Plant | cards | 5,188 | 174 | 0.0000 | 0.868 | 87 of 87 @ x1.0 | `vegetation_shrub_02.tres` |
| `ashwood_shrub_03_a` | Plant | cards | 2,385 | 90 | 0.0000 | 0.680 | 45 of 45 @ x1.0 | `vegetation_shrub_03.tres` |
| `ashwood_shrub_03_b` | Plant | cards | 2,134 | 86 | 0.0000 | 0.653 | 43 of 43 @ x1.0 | `vegetation_shrub_03.tres` |
| `ashwood_shrub_03_c` | Plant | cards | 1,790 | 76 | 0.0000 | 0.620 | 38 of 38 @ x1.0 | `vegetation_shrub_03.tres` |
| `ashwood_shrub_03_d` | Plant | cards | 1,978 | 82 | 0.0000 | 0.768 | 41 of 41 @ x1.0 | `vegetation_shrub_03.tres` |
| `ashwood_fern_02_a` | Plant | decimate | 784 | 380 | 0.0000 | 0.541 | - | `vegetation_fern_02.tres` |
| `ashwood_fern_02_b` | Plant | decimate | 2,384 | 380 | 0.0000 | 0.551 | - | `vegetation_fern_02.tres` |
| `ashwood_fern_02_c` | Plant | decimate | 2,248 | 300 | 0.0000 | 0.525 | - | `vegetation_fern_02.tres` |
| `ashwood_fern_02_d` | Plant | decimate | 816 | 300 | 0.0000 | 0.520 | - | `vegetation_fern_02.tres` |
| `ashwood_nettle_tall` | Plant | decimate | 15,064 | 675 | 0.0000 | 0.899 | - | `vegetation_nettle_plant.tres` |
| `ashwood_nettle_medium` | Plant | decimate | 11,560 | 556 | 0.0000 | 0.906 | - | `vegetation_nettle_plant.tres` |
| `ashwood_nettle_small` | Plant | decimate | 4,680 | 399 | 0.0000 | 0.951 | - | `vegetation_nettle_plant.tres` |
| `ashwood_grass_bermuda_medium` | Plant | keep | 113 | 113 | 0.0000 | 0.200 | - | `vegetation_grass_bermuda_01.tres` |
| `ashwood_grass_bermuda_small` | Plant | keep | 72 | 72 | 0.0000 | 0.235 | - | `vegetation_grass_bermuda_01.tres` |
| `ashwood_grass_bermuda_dry` | Plant | keep | 330 | 330 | 0.0000 | 0.224 | - | `vegetation_grass_bermuda_01.tres` |
| `ashwood_dead_tree_trunk` | Body | decimate | 101,802 | 800 | 0.0000 | - | - | `vegetation_dead_tree_trunk.tres` |
| `ashwood_dead_log` | Body | decimate | 83,128 | 700 | 0.0000 | - | - | `vegetation_dead_tree_trunk_02.tres` |
| `ashwood_bark_debris_a` | Body | decimate | 38,488 | 500 | 0.0000 | - | - | `vegetation_bark_debris_01.tres` |
| `ashwood_bark_debris_b` | Body | decimate | 48,622 | 500 | 0.0000 | - | - | `vegetation_bark_debris_01.tres` |
| `ashwood_bark_debris_c` | Body | decimate | 65,062 | 499 | 0.0000 | - | - | `vegetation_bark_debris_01.tres` |
| `ashwood_bark_debris_d` | Body | decimate | 41,234 | 500 | 0.0000 | - | - | `vegetation_bark_debris_01.tres` |
| `ashwood_boulder_01` | Body | decimate | 66,122 | 26,163 | 0.0000 | - | - | `vegetation_boulder_01.tres` |
| `ashwood_rock_moss_01` | Body | decimate | 11,000 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |
| `ashwood_rock_moss_02` | Body | decimate | 10,996 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |
| `ashwood_rock_moss_03` | Body | decimate | 5,000 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |
| `ashwood_rock_moss_04` | Body | decimate | 10,589 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |
| `ashwood_rock_moss_05` | Body | decimate | 16,548 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |
| `ashwood_rock_moss_06` | Body | decimate | 8,994 | 400 | 0.0000 | - | - | `vegetation_rock_moss_set_01.tres` |

## How the leaves are built

The Poly Haven canopies are not solid meshes. jacaranda_tree's 2.4M-triangle
canopy is 116,084 separate leaf-spray cards of ~20 triangles each, and the
shrubs are built the same way. Collapse-decimating that to a game budget spends
under one triangle per card, welding the canopy into pulp and smearing UVs
across the atlas.

Instead each UV island is measured and rebuilt as a single quad. Every island is
a parameterised patch, so a least-squares fit of the affine map
`(u, v, 1) -> position` reproduces it exactly; measured residual on jacaranda is
2.8% of island diagonal (worst 5.7%). Cards are thinned on a spatial grid rather
than randomly, so the crown silhouette survives, and card normals are blended
50% towards "outward from the crown" so the canopy shades as a rounded volume
instead of a pile of flat chips.

**Known trade-off:** the surviving cards are scaled up to hold canopy coverage,
so jacaranda leaf sprays render larger than life (roughly 3.4x at LOD0). A
4,000-triangle budget cannot hold 116,084 leaves at native scale - this is the
standard game-tree compromise, but it is a real deviation from the scan and it
is the first thing to re-tune (`card_scale` in the `ASSETS` table) if the
canopy reads as coarse in-game.

## Materials

`assets/materials/vegetation_*.tres`. Foliage uses **alpha scissor**
(`transparency = 2`) with `cull_mode = 2` (disabled) so leaves are two-sided.
Alpha blending is deliberately not used - it has no correct draw order and looks
worst exactly where foliage overlaps. Albedo, ARM (AO in red, roughness in
green) and OpenGL normal maps are all wired; nothing renders unlit.

Alpha threshold is **0.33**, not 0.5. Godot mipmaps the albedo, and averaging a
hard mask drives thin-foliage alpha towards the local coverage fraction, so a
0.5 test erodes leaves with distance and thins the canopy out.

The composited RGBA albedos in `textures/` exist because Poly Haven's glTF
references only the JPEG diffuse and JPEG has no alpha channel. The cut-out
silhouette ships as a separate `Alpha` map the glTF never mentions, and
`fern_02`'s diffuse has a dilated colour bleed instead of a black background -
so without compositing it renders as a solid green rectangle. RGB is also
dilated outwards under the transparent region so mipmapping never bleeds the
atlas background in along leaf edges.

## Not yet verified

Triangle counts, UV integrity, alpha coverage, texture wiring, asset scale and
node/override structure are all machine-checked above and in the build log.
**In-engine appearance is not** - these were built and previewed in Blender, not
rendered in Godot. Worth eyeballing on first run: canopy density and leaf scale
at gameplay camera distance, alpha-scissor edge quality on the Compatibility
renderer, and the backlight strength on leaf materials.
