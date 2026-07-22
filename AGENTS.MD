# AGENTS.md

## Project

Ashwood County is a Godot 4.7.1 .NET prototype for a third-person 3D zombie survival game.

The current objective is to build and evaluate one small, polished gameplay vertical slice before expanding the project.

The prototype has already validated:

- Third-person player movement
- Mouse-controlled camera
- Idle, walking and running animations
- Imported 3D environment assets
- Basic environment lighting
- Static collision
- Navigation
- One animated zombie with detection, pursuit and attack behaviour
- Acceptable performance using the Compatibility renderer

Do not expand into a large open world or unrelated systems.

---

## Technology

- Godot 4.7.1 .NET
- C#
- Compatibility renderer

Do not change the engine, renderer or language unless explicitly requested.

---

## Priorities

In order:

1. Gameplay feel
2. Stable vertical slices
3. Performance on the development laptop
4. Visual atmosphere
5. Maintainable code

Prefer a simple working implementation over premature generalisation.

---

## Development Scope

Implement one focused gameplay slice per task.

Each task must:

- Be independently testable
- Preserve working systems
- Avoid unrelated refactoring
- Avoid speculative future architecture
- Stop when the requested feature is complete

Do not implement additional roadmap features unless explicitly requested.

When a requested task is too large, split it into smaller tasks before implementation.

---

## Coding Standards

- Use C#.
- Keep classes focused.
- Prefer composition over deep inheritance.
- Expose genuinely tunable values with `[Export]`.
- Use descriptive names.
- Avoid unexplained magic numbers.
- Keep methods reasonably small.
- Reuse existing systems rather than duplicating behaviour.
- Do not add plugins or external dependencies without approval.
- Do not modify third-party source assets directly when a project-owned wrapper or material can be used.

---

## Repository Structure

```text
assets/
  characters/
    player/
    zombies/
  environment/
    buildings/
    nature/
    roads/
    vehicles/
  materials/
  third_party/

docs/
  assets_reference.md
  devlog.md
  roadmap.md
  vision.md

scenes/
scripts/