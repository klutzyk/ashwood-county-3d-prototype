# Tree Small 02 source record

- Asset: Tree Small 02
- Author: Rico Cilliers
- Provider: Poly Haven
- Source: https://polyhaven.com/a/tree_small_02
- License: CC0 1.0 Universal
- License terms: https://polyhaven.com/license
- Downloaded: 2 August 2026 through Poly Haven's official public API
- Commercial use: permitted; attribution is not required

The untouched retained 1K glTF source consists of
`tree_small_02_1k.gltf`, `tree_small_02.bin`, and the required maps under
`textures/`. Every downloaded file matched the MD5 supplied by Poly Haven's
API response.

The source directory contains `.gdignore`: it remains available for provenance
and rebuilding the derivative, but Godot does not import the unused 2M-triangle
authoring mesh. The repository therefore loads only the optimized project GLB
at runtime. The retained source is approximately 101 MB and should use Git LFS
or be replaced by a reproducible acquisition step before a public repository
release.

## Project derivative

- Output: `res://assets/environment/nature/ashwood_hero_tree_small_02.glb`
- Reproducible tool:
  `res://tools/blender/optimize_poly_haven_tree_small_02.py`
- Changes: separated material parts, dissolved redundant planar leaf
  tessellation, applied conservative part-specific decimation, rejoined the
  mesh, and exported a project-owned game derivative. The original source is
  unchanged.
- Measured source geometry: 2,062,487 triangles
- Project derivative: 120,429 triangles (94.16% reduction)
- Derivative SHA-256:
  `AB221DAB7AC84262D37EE857F325D110E2D7B2588158AAF317B4E7F486182B00`
- Runtime policy: four seasonal close/midground instances with visibility
  ranges; existing lightweight trees remain the distant canopy.

Attribution is included for provenance even though CC0 does not require it.
