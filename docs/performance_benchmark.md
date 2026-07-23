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
