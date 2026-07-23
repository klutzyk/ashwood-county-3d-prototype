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

---

## Prototype Town Dressing

Completed:

- Extended the prototype road and shoulders across the playable terrain
- Added centre-line markings and a dressed house frontage with a driveway,
  mailbox, rubbish bin, utility cabinet, fencing and reusable small bushes
- Added a lightweight service-station exterior with a shop, canopy, pumps,
  price pylon, forecourt and exterior lighting
- Added a small pharmacy exterior with storefront glazing, pavement, awning,
  signage and rooftop detail
- Reused the existing house, road materials, poles, road sign, tyre, car and
  container assets

Outcome:

The original roadside test now reads as a small town strip with three distinct
points of interest while remaining lightweight for the Compatibility renderer.
No additional third-party assets were required or downloaded.

Validation:

- Godot imported and loaded the new scenes without errors
- Runtime preview capture completed using the Compatibility renderer
- C# build completed without errors

---

## Modular Enterable Buildings

Completed:

- Split the house, pharmacy and service station into independent exterior,
  interior and assembled root scenes
- Added a reusable interaction-driven door controller with smooth hinged motion
  and a collision shape that follows the visible door
- Replaced solid building blockers with simple box-collision wall sections and
  physical doorway gaps
- Added prototype living room, kitchen and bedroom zones to the house
- Added a retail floor, checkout, shelving and back room to the pharmacy
- Added a checkout area, convenience shelving and storage room to the service
  station
- Added lightweight warm interior lights without baked lighting or GI
- Moved the existing searchable cupboard and crate into the explorable house
  layout without changing their inventory or loot behaviour

Outcome:

All three town buildings can be entered continuously through physical doors.
Exterior scenes can now be replaced independently without changing interiors,
door behaviour or other gameplay systems.

Validation:

- Automated runtime checks passed door open/close animation, closed/open doorway
  collision, exterior wall collision and interior presence for all buildings
- Existing zombie detection and pursuit still functioned in an open-road test
- Compatibility-renderer preview completed at approximately 6.1 ms CPU and
  5.0 ms GPU render time per frame during movie capture
- Godot scene load and C# build completed with no warnings or errors

---

## Multiple Zombies and Wandering

Completed:

- Placed five independent instances of the existing prototype-zombie scene
  across the town
- Added nearby random navigation targets within an exported wander radius
- Added exported random idle delays between wander destinations
- Preserved timed chase-path refreshes instead of recalculating every frame
- Added lightweight local separation while retaining physical zombie collision
- Rebuilt the town navigation mesh from static collision shapes so paths route
  around buildings, furniture, vehicles, trees and props
- Kept door openings connected in the navigation mesh for open-door traversal

Outcome:

Zombies independently idle, wander, detect, chase and attack while sharing one
scene and controller. No spawning, hordes, sound attraction, new zombie types,
loot or combat systems were added.

Validation:

- All five zombies independently entered wandering and changed position
- All five independently detected and pursued a visible player
- Existing attack damage remained functional
- Navigation routes bent around the house, pharmacy and service station
- Navigation paths connected through all three open building entrances
- Separation test prevented direct zombie stacking
- Compatibility capture averaged 2.38 ms CPU and 14.54 ms GPU render time per
  frame at a fixed 60 FPS target
- C# build completed with no warnings or errors

Scope note:

Zombie health and death remain unimplemented roadmap items, so dead-zombie
shutdown behaviour was not available to preserve or validate in this slice.
