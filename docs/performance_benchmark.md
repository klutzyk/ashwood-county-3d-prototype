# Rendered Performance Benchmark

Use the fixed benchmark scene to compare town-rendering changes under the same
conditions:

- Compatibility renderer
- 1280 x 720 window
- 17:00 world time
- Fixed player and camera transform
- 180 warm-up frames
- 600 measured rendered frames
- VSync and the engine FPS cap disabled

From the repository root, launch the fixed benchmark directly:

```powershell
.\tools\launch-runtime.ps1 -Target Benchmark
```

Launch the normal project entry point without the editor:

```powershell
.\tools\launch-runtime.ps1
```

Use `-Resolution 1600x900` (or another `WIDTHxHEIGHT` value) to override the
default. The helper checks `-GodotPath` first, then `ASHWOOD_GODOT_PATH`, then
the `godot`, `godot4` and `godot-mono` commands on `PATH`. A local executable can
be configured per shell:

```powershell
$env:ASHWOOD_GODOT_PATH = "C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64_console.exe"
.\tools\launch-runtime.ps1 -Target Benchmark
```

Alternatively, pass `-GodotPath` for a one-off launch. The helper fails before
launch with a clear message when the executable or `project.godot` is missing.
Use `-DryRun` to verify path detection and arguments without opening Godot.

Record the reported average FPS and p95 frame time. Close other GPU-heavy
applications and repeat a run when a result looks anomalous.
