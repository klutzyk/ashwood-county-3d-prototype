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

## Inventory and Container Rules

World containers and the player must use separate inventory state.

Searching a container may generate or reveal its contents, but must not automatically transfer those contents to the player.

Items move only through explicit transfer actions.

Containers must retain:

- Uncollected items
- Items added by the player
- Their searched or generated state

Do not create separate inventory implementations for cars, cupboards, crates or other container types. They should reuse the same container-inventory foundation with configurable names, capacities, search durations and loot tables.

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

---

## Development Workflow

Before implementation:

1. Read this file.
2. Read `docs/roadmap.md`.
3. Read `docs/devlog.md`.
4. Inspect only the files relevant to the requested feature.

Implementation rules:

- Implement one gameplay slice only.
- Preserve existing systems unless explicitly changing them.
- Avoid unrelated refactoring.
- Keep changes as small as practical.
- Stop when the requested feature is complete.

Before finishing:

- Build the C# project.
- Fix compilation errors introduced by the change.
- Update `docs/roadmap.md`.
- Append a short entry to `docs/devlog.md`.
- Do not continue with future roadmap items.

Keep responses concise.

Include:

- Summary of changes
- Required editor setup (if any)
- Build result
- Suggested commit message

Only include additional detail when requested.

---

## Autonomous Development

When implementing a requested roadmap slice:

- Read the project documentation before making changes.
- Implement only the requested slice.
- Build the project.
- Validate the affected gameplay.
- Update roadmap.md.
- Update devlog.md.
- Review the Git diff.
- Stage only relevant files.
- Create a Git commit with a concise conventional commit message.
- Do not push.
- Stop after the commit.

Do not continue to future roadmap tasks unless explicitly instructed.

---

## Project Documents

- `docs/vision.md` describes the long-term direction.
- `docs/roadmap.md` defines the implementation order.
- `docs/devlog.md` records completed milestones.
- `docs/assets_reference.md` tracks third-party assets and licences.

Follow these documents unless explicitly instructed otherwise.