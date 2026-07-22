# Development Log

---

## Prototype Started

Initial Godot 4.7.1 .NET project created.

Goal:

Evaluate whether a third-person 3D zombie survival game is feasible using Godot and AI-assisted development.

---

## Player Controller

Completed:

- Third-person movement
- Mouse camera
- Idle animation
- Walking animation
- Running animation

Outcome:

Movement feels responsive and forms a good baseline.

---

## Environment Prototype

Completed:

- Road
- Ground
- Trees
- House
- Abandoned car

Outcome:

Successfully imported third-party assets.

Performance remained around 60 FPS using the Compatibility renderer.

---

## Zombie Prototype

Completed:

- Detection
- Navigation
- Chase
- Attack

Outcome:

Zombie successfully detects, chases and attacks the player.

This validates navigation, animation transitions and basic AI behaviour.

---

## Player Stamina

Completed:

- Sprint stamina drain
- Delayed stamina regeneration
- Exhaustion lockout and recovery threshold
- Stamina HUD bar
- Run animation tied to actual sprint state

Outcome:

Walking remains unchanged, while sprinting is now a limited resource with clear visual feedback.

---

## Interaction Framework

Completed:

- Reusable interactable component and callback signal
- Closest interactable selection within a configurable range
- E-key interaction input
- Contextual interaction prompt
- Abandoned car interaction callback

Outcome:

The player can select and trigger one nearby interactable at a time. The abandoned car prints a prototype message; searching, items and loot remain unimplemented.

---

## Searching Prototype

Completed:

- Hold-E search action
- Two-second progress bar
- Movement and sprint lock during searching
- Cancellation when E is released or the target becomes invalid
- Abandoned car search completion callback

Outcome:

The abandoned car can now be searched through a timed, cancellable interaction. Search results, items and loot remain unimplemented.

---

## Item Prototype

Completed:

- Reusable item definition resource
- Minimal quantity-based player inventory
- Bandage item resource
- One-time Bandage reward from searching the abandoned car

Outcome:

Completing the car search grants exactly one Bandage. Item display and item use remain unimplemented.

---

## Inventory Display

Completed:

- Fixed four-slot player inventory
- Four-slot HUD display
- Automatic slot refresh when items are added
- Bandage name and quantity shown after searching the car

Outcome:

The player can see the Bandage in the first inventory slot after searching the abandoned car. Item selection and use remain unimplemented.

---

## Bandage Use

Completed:

- Slot 1 item-use input
- Bandage health restoration
- Successful-use item consumption
- Full-health and death safeguards

Outcome:

Pressing 1 uses the Bandage to restore 40 health and removes it from the inventory. The Bandage is not consumed when healing cannot occur.

---


## Container Loot Design Decision

Decision:

World containers and the player will maintain separate inventories.

Searching reveals or generates container contents but does not automatically award items.

The player must explicitly transfer selected items.

Items left behind or placed inside a container remain there for the duration of the loaded world state.

Reason:

This creates stronger resource decisions, supports revisiting locations and provides a reusable foundation for cars, cupboards, lockers, crates, corpses and safehouse storage.

Next objective:

Implement search progress followed by the reusable container inventory UI.

---

## Search and Loot Vertical Slice

Completed:

- Reusable searchable-container setup for the abandoned car, crate and prototype cupboard
- Hold-E search progress with movement lock and cancellation on release or distance
- Independent, ordered container inventories that retain scene-lifetime state
- Side-by-side container and four-slot player inventory UI
- Explicit Take and Store transfers with immediate updates, stack preservation and capacity checks
- Bandage, Water and Food item use, including placeholder feedback for unimplemented needs
- Configurable weighted loot tables with a one-time 40/30/20/10 Bandage/Water/Food/Nothing roll
- Escape-to-close behavior and player-control restriction while container UI is open

Outcome:

Searching reveals container contents without automatically transferring them. Containers remember generated loot and subsequent item changes until the scene is reloaded; save/load and ground dropping remain intentionally unimplemented.

Validation:

- Automated scene test passed search completion and cancellation, transfers, full-inventory handling, item use, one-time loot and retained container state
- Godot scene load and C# build completed without errors

---

## Current Milestone

Completed:

Vertical Slice 1 search-and-loot feasibility test.

Next objective:

Awaiting the next focused vertical-slice task.

---

## Notes

Important findings:

- Godot 4.7.1 performs well on the current laptop.
- Compatibility renderer is sufficient.
- Mixamo integrates smoothly.
- AI-assisted workflow is productive.
- Small focused implementation tasks produce the best results.
- Raw third-party asset packs should remain outside the project until curated.
