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

Run:

```powershell
& 'C:\Users\kalz9\Downloads\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --resolution 1280x720 tests/rendered_performance_benchmark.tscn
```

Global command:

```powershell
& "C:\Users\kalz9\Downloads\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --path "C:\ashwood-county-3d-prototype"
```

Record the reported average FPS and p95 frame time. Close other GPU-heavy
applications and repeat a run when a result looks anomalous.
