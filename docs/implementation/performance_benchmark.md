# Performance Benchmarks

Two rendered benchmarks are available. Both use the Compatibility renderer,
1280 x 720, 17:00, the documented player/camera transform, flashlight off,
VSync off and no engine FPS cap.

## Representative Full-Game Benchmark

Use this benchmark for gameplay performance decisions:

```powershell
.\tools\launch-runtime.ps1 -Target FullBenchmark
```

The benchmark inherits the real `prototype_world.tscn` as its root, so it
bypasses only the main menu. The `SettingsManager` autoload and the world's
normal player, camera, UI, objectives, needs, notifications, save manager,
ambient audio, 15 zombies, AI, navigation and animation remain active. A
diagnostic child holds the player and normal camera at the fixed transform,
holds world time at 17:00, explicitly turns the normal flashlight off, and
controls benchmark timing. It warms up for 5 seconds, samples for 20 seconds,
reports, and exits.

## Existing Synthetic Benchmark

Use the original short benchmark for comparisons with historical results:

```powershell
.\tools\launch-runtime.ps1 -Target Benchmark
```

This scene creates a wrapper `Node`, instantiates `prototype_world.tscn` below
it, warms for 180 rendered frames, samples 600 rendered frames, reports, and
exits. At approximately 160-200 FPS, its 780 total frames take about 3.9-4.9
seconds plus scene startup. The observed automatic exit after about five
seconds is therefore expected.

Despite the "synthetic" label, it loads the complete world. Its material
difference from normal gameplay is deterministic benchmark control: it freezes
`WorldTime` at 17:00, fixes the starting transform and explicitly disables
VSync and the FPS cap. It does not freeze zombie AI or navigation, change
gameplay process modes, remove zombies, replace the camera, hide geometry or
disable shadows.

## Normal Gameplay Comparison

Normal launch starts at `main_menu.tscn`; New Game and Continue replace it with
the same `prototype_world.tscn`, so the menu is not retained during gameplay.
Continue may restore a different player transform, world time and zombie state.

| System | Normal gameplay | Synthetic | Full-game |
| --- | --- | --- | --- |
| Gameplay root | `prototype_world.tscn` after menu | Wrapper with full world child | Full world inherited as root |
| Autoloads | `SettingsManager` | Same | Same |
| Player/camera/UI | Normal | Same; transform fixed | Same; transform held fixed |
| Objectives/needs/notifications/save/audio | Active | Active | Active |
| Zombies | 15 normal placed zombies | Same 15 | Same 15 |
| AI/navigation | Active | Active | Active |
| Navigation avoidance | Disabled on all 15 by scene configuration | Same | Same |
| Animation/corpses | Normal animation; 15 inactive corpse containers while alive | Same | Same |
| Lights/shadows | Normal preset; one shadow-casting directional light at this view | Same | Same |
| World time | Advances normally | Held at 17:00 | Held at 17:00 |
| Flashlight | Player controlled; defaults off | Explicitly off | Explicitly off |
| Display | Saved settings | Forced 1280 x 720, VSync off, uncapped | Same forced settings |
| Stretch | `canvas_items`, aspect `expand` | Same | Same |
| Physics | Default 60 ticks/second | Same | Same |
| Debug behavior | Same debug assembly when launched this way | Same | Same |

The benchmark prints the inherited and forced display configuration plus scene
counts before sampling. On the investigated machine the saved settings supplied
1280 x 720 with VSync enabled on a 60 Hz display, while both benchmarks changed
VSync to disabled. There is no project FPS cap. This configuration mismatch is
the primary reason an uncapped benchmark cannot be compared directly with the
normal on-screen FPS counter. A diagnostic full-game run with VSync enabled
measured 57.95 FPS at the fixed view; ordinary camera movement and a more
expensive view can reduce that further. The normal counter uses
`Engine.GetFramesPerSecond()` and the benchmark uses elapsed wall-clock frame
times; neither calculation explains a 160-200 versus approximately 45 gap.

## Validated Results

Captured on 2026-07-23 using the AMD Radeon integrated adapter:

| Metric | Synthetic, 600 frames | Full game, 20 seconds |
| --- | ---: | ---: |
| Average FPS | 181.21 | 176.09 |
| Median frame time | 5.11 ms | 5.20 ms |
| p95 frame time | 8.69 ms | 9.07 ms |
| p99 frame time | 9.88 ms | 10.43 ms |
| Minimum instantaneous FPS | 71.14 | 53.56 |
| Process time | 11.095 ms | 9.503 ms |
| Physics time | 3.378 ms | 3.463 ms |
| Navigation time | 0.012 ms | 0.014 ms |
| Draw calls | 455.0 | 474.2 |
| Visible objects | 587.0 | 609.6 |
| Primitives | 209,554.2 | 232,251.0 |

Process, physics and navigation values are sampled Godot performance monitors;
they are diagnostic averages rather than additive subdivisions of measured
wall-clock frame time. Navigation is negligible and the representative result
tracks the synthetic result closely, so the reported 45 FPS is not caused by
missing full-game managers or AI in the old benchmark. With no GPU-time monitor
available in the Compatibility renderer, the uncapped data does not support a
definitive CPU-versus-GPU split. It rules out physics and navigation as the
bottleneck; the normal-run discrepancy is dominated by VSync/presentation
pacing and can be compounded by the camera's visible render workload.

Use `-GodotPath` or `ASHWOOD_GODOT_PATH` when Godot is not on `PATH`. Close
other GPU-heavy applications and repeat anomalous results.

## Post-Town-Expansion Result

Captured on 2026-07-23 after the eight-slice town expansion and world-polish
pass:

| Metric | Expanded full game, 20 seconds |
| --- | ---: |
| Average FPS | 181.14 |
| Median frame time | 4.98 ms |
| p95 frame time | 9.93 ms |
| p99 frame time | 11.42 ms |
| Minimum instantaneous FPS | 60.25 |
| Process time | 10.230 ms |
| Physics time | 3.744 ms |
| Navigation time | 0.014 ms |
| Draw calls | 415.8 |
| Visible objects | 532.4 |
| Primitives | 191,379.2 |

The run retained 15 active zombies and navigation agents, all gameplay managers,
11 lights with one shadow caster, four active ambience players, and the normal
camera/UI. The fixed benchmark camera does not frame the new northern district,
so this result verifies that the integrated world remains within the established
performance envelope; it should not be interpreted as a worst-case district
view.

## August 2026 World-Presentation A/B

Captured on 2026-08-02 with the Compatibility renderer in a debug build at
1280 x 720. Both runs used the same deterministic benchmark view, 180 rendered
warm-up frames and 600 sampled frames. `World polish on` includes the rolling
county context, seasonal midground tree layer and civic landmark detailing;
`World polish off` disables that presentation root while leaving the rest of
the playable world and gameplay systems unchanged. The measured enabled build
still contained two generated silhouette-ribbon meshes that the final visual
critique removed; the shipped working-tree version is therefore marginally
cheaper than this conservative measurement.

| Metric | World polish on | World polish off | On minus off |
| --- | ---: | ---: | ---: |
| Average FPS | 79.32 | 77.44 | +1.88 (+2.43%) |
| Median frame time | 11.52 ms | 11.95 ms | -0.43 ms |
| p95 frame time | 23.11 ms | 21.55 ms | +1.56 ms |
| p99 frame time | 29.76 ms | 27.54 ms | +2.22 ms |
| 1% low FPS | 22.31 | 26.31 | -3.99 |
| Draw calls | 1,624.3 | 1,602.1 | +22.2 |
| Visible objects | 1,893.0 | 1,901.1 | -8.1 |
| Primitives | 1,654,420.8 | 1,631,105.8 | +23,315.0 |

The positive average-FPS delta and slightly lower visible-object count are run
variance rather than evidence that extra scenery improves performance. The
useful result is the small workload delta: about 22 draw calls and 23,315
primitives for the enabled presentation layer. Its p95/p99 frame times and 1%
low are nevertheless worse, and the current complete scene is not a locked
60-FPS experience. Traversal captures and release builds remain necessary
before treating this fixed-view result as a shipping performance guarantee.

## August 2026 Main Street Density View

The retained density benchmark loads the real Main Street scene, fixes a camera
looking east from `(55.0, 3.3, 0.8)`, holds clear weather at 16:45, warms for
180 rendered frames and samples 600 frames. Run it with the density layer on,
then disable only that layer for the comparison:

```powershell
& $env:ASHWOOD_GODOT_PATH --path . --resolution 1280x720 --scene res://tests/main_street_world_density_performance_benchmark.tscn
$env:ASHWOOD_BENCH_DENSITY = '0'
& $env:ASHWOOD_GODOT_PATH --path . --resolution 1280x720 --scene res://tests/main_street_world_density_performance_benchmark.tscn
Remove-Item Env:ASHWOOD_BENCH_DENSITY
```

The accepted pair was captured sequentially on 2026-08-02 from 18:26:30 to
18:27:43, with process checks confirming that no other Godot renderer was
active before, between or after the runs. Earlier overlapping data was
discarded.

| Metric | Density on | Density off | On minus off |
| --- | ---: | ---: | ---: |
| Average FPS | 31.35 | 30.97 | +0.38 (+1.23%) |
| Median frame time | 30.25 ms | 31.24 ms | -0.99 ms |
| p95 frame time | 40.88 ms | 41.17 ms | -0.29 ms |
| p99 frame time | 47.91 ms | 55.91 ms | -8.00 ms |
| 1% low FPS | 19.59 | 15.28 | +4.31 |
| Draw calls | 6,846.0 | 7,535.5 | -689.5 |
| Visible objects | 4,147.0 | 4,821.6 | -674.6 |
| Primitives | 1,813,112.5 | 2,124,330.5 | -311,218.0 |

This is an end-to-end view comparison, not the raw cost of the added meshes.
The infill façades occlude a dense school/interior region from this camera, so
the enabled view submits less of the world and shows no measured regression.
That result does not erase the larger problem: roughly 31 FPS and a 19.59 FPS
1% low in a debug Compatibility-renderer run are well below a stable 60-FPS
target. A release-build traversal profile, occlusion/visibility audit and
district streaming work remain required.
