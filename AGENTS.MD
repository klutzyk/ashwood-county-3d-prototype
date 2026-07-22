# AGENTS.md

# Project

This repository is a feasibility prototype for a potential third-person 3D zombie survival game.

This is NOT the main game.

The objective is to determine whether:

- Godot 4.7.1 .NET is suitable.
- The Compatibility renderer performs well.
- Development is comfortable on integrated graphics.
- A visually appealing 3D game is realistic using AI-assisted development.

Do not expand scope.

---

# Technology

Engine

- Godot 4.7.1 .NET
- C#
- Compatibility Renderer

---

# Design Philosophy

Priorities:

1. Gameplay feel
2. Visual atmosphere
3. Performance
4. Code quality

Do not optimize prematurely.

---

# Scope

Implement only small vertical slices.

Each task should be independently testable.

Avoid implementing multiple gameplay systems simultaneously.

---

# Coding Standards

- Use C#
- Keep classes focused.
- Prefer composition over inheritance.
- Expose tunable values with `[Export]`.
- Use descriptive names.
- Avoid magic numbers.
- Keep methods short.

---

# Repository Structure

assets/
docs/
scenes/
scripts/
third-party-assets/

---

# Workflow

Before every implementation:

1. Inspect the repository.
2. Understand the existing architecture.
3. Implement only the requested feature.
4. Build the project.
5. Report:

- Files changed
- Manual testing
- Build status
- Known limitations
- Suggested commit message

Do not modify unrelated systems.

---

# Current Goal

Build a polished third-person movement prototype.

The prototype should eventually include:

- Third-person movement
- Camera
- One environment
- One house
- Trees
- Props
- One zombie
- Basic lighting

Nothing else.

No inventory.

No combat.

No AI systems.

No save system.

The purpose is to evaluate visuals, performance and development workflow.