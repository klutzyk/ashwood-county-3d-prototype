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

---

## Reusable Zombie Sound Attraction

Completed:

- Added a lightweight reusable gameplay-noise event with world position,
  audible radius and an optional category
- Added exported sprint and melee noise radii to the player, with interval-based
  sprint emission so walking remains silent
- Added an exported door noise radius and emission on both opening and closing
- Added independent zombie investigation and timed search states using each
  zombie's existing `NavigationAgent3D`
- Preserved behavior priority: attack, visible chase, recent sound, then normal
  wandering
- Allowed a newer audible event to replace an active investigation target
- Kept investigation paths event-driven rather than recalculating every frame

Outcome:

Nearby zombies investigate the last heard sprint, melee or door position, wait
briefly at the destination, then resume their existing idle/wander cycle if the
player is not found.

Validation:

- Runtime checks passed with all five zombie instances loaded
- Sprinting and the melee hook attracted zombies outside their field of view
- Walking emitted no gameplay noise
- Opening a door emitted a categorized noise event
- Newer sounds replaced older investigation positions
- Investigation expired back to the normal idle/wander cycle
- Compatibility-renderer capture completed 180 frames at the fixed 60 FPS target
- Godot headless scene load and C# build completed with no warnings or errors

Scope note:

The repository still has no player melee combat action or zombie health/death
system. `EmitMeleeAttackNoise()` is ready for the planned melee action to call,
and `SetGameplayNoiseResponseEnabled(false)` is ready for a future zombie-death
handler. Those unrelated combat systems were not added in this slice.

---

## Pharmacy Antibiotics Objective

Completed:

- Added a dedicated Antibiotics item in a searchable pharmacy medicine cabinet
- Preserved separate container state and required the existing Take action
- Added structured search, return and completion objective states
- Added a clearly labelled safe point outside the starting house
- Submitted the antibiotics on safe-point interaction and showed completion feedback

Outcome:

The player can search the pharmacy, explicitly take the antibiotics and return
them to the safe point to complete one focused prototype objective.

Validation:

- Automated end-to-end runtime validation passed search, explicit transfer,
  state progression, safe-point delivery and item submission
- C# build completed with no warnings or errors

---

## Prototype Day/Night and Flashlight

Completed:

- Added structured 24-hour world time starting at 17:00
- Rotated the directional light continuously and blended directional, ambient
  and sky energy between readable day and night values
- Added an exported full-day duration and a compact 24-hour clock
- Added an F-toggle camera-directed spotlight with exported range, energy and
  cone angle
- Kept flashlight shadows, volumetrics and post-processing disabled

Outcome:

Late afternoon now transitions smoothly into a dark but readable night, and the
player can use the same lightweight flashlight indoors and outdoors.

Validation:

- Automated runtime transition passed from late afternoon into night
- Ambient and sky lighting reached configured non-black night minimums
- Flashlight toggle, camera attachment, outdoor use and pharmacy interior use passed
- Pharmacy objective regression validation still passed
- C# build completed with no warnings or errors

---

## Minimal Prototype Save/Load

Completed:

- Added explicit JSON save-data classes with save version 1
- Added one local `user://` slot with F5 save and F9 load controls
- Saved player transform, health, stamina, inventory, objective state and world time
- Saved search state and remaining inventory for every existing container
- Saved independent alive/dead state for all five placed prototype zombies
- Added brief save/load status feedback and safe missing/invalid-file handling

Outcome:

The current playable loop persists between sessions without serializing live
nodes or attempting to capture arbitrary future scene state.

Validation:

- Same-session save, state mutation and load-back validation passed
- A second Godot process loaded the persisted validation save successfully
- Missing and malformed saves were rejected without crashes or partial mutation
- Pharmacy objective and day/night flashlight regression validations passed
- C# build completed with no warnings or errors

---

## Reusable Prototype Melee Combat

Completed:

- Added a visible prototype baseball bat and Left Mouse attack input
- Added a short procedural bat swing with an attack lock and configurable cooldown
- Added configurable damage, range, attack arc and knockback values
- Added independent health to every reusable zombie instance
- Applied each swing once to zombies inside the forward attack arc
- Added a brief zombie hit reaction and damped physical knockback
- Used the available zombie death animation and retained dead zombies as visible corpses
- Disabled corpse navigation, attacks and collision while preserving save/load alive state

Outcome:

The player can now fight nearby zombies with one deliberately simple melee
attack. The combat and zombie-health components are reusable, and the existing
movement, interaction, inventory, AI, noise-attraction and save systems remain
in place.

Validation:

- C# build completed with no warnings or errors

---

## Searchable Zombie Corpses

Completed:

- Attached the existing reusable searchable-container foundation to every zombie
- Enabled corpse interaction only after that zombie dies
- Added deterministic per-corpse loot rolls derived from stable scene paths
- Added Bandage, Food, Bottled Water, Scrap and empty weighted outcomes
- Generated each corpse's contents only on its first completed search
- Preserved explicit item transfers, uncollected loot and searched state
- Added Scrap to the versioned save/load item-resource registry

Outcome:

Dead zombies remain in the world and become small independent loot containers.
Searching reveals contents without transferring them automatically, and reopening
a searched corpse allows the player to retrieve or store items left there.

Validation:

- C# build completed with no warnings or errors

---

## Prototype Hunger and Thirst

Completed:

- Added independent Hunger and Thirst values with exported maximums and drain rates
- Added compact Hunger and Thirst HUD bars beside Health and Stamina
- Converted prototype Food and Water into reusable need-restoring items
- Prevented consuming Food or Water when its corresponding need is already full
- Added configurable gradual health loss when either need reaches zero
- Saved, validated and restored both needs in the existing version 1 save format
- Kept older version 1 saves compatible by defaulting absent needs to full

Outcome:

Food and Water now matter in the current survival loop without adding nutrition,
cooking, disease or other out-of-scope systems. Need drain and zero-need damage
are slow, exported prototype tuning values.

Validation:

- Save/load validation now covers independent Hunger and Thirst restoration
- C# build completed with no warnings or errors

---

## Expanded Prototype Zombie Population

Completed:

- Increased the authored population from five to fifteen reusable zombie instances
- Distributed zombies across outdoor road, verge and town-perimeter navigation areas
- Kept all starting positions outside the house, pharmacy and service station
- Varied wander radii, idle windows, wandering speeds, chase speeds and detection ranges
- Preserved independent health, corpse loot, pursuit, sound investigation and save state
- Kept the population scene-authored with no spawner, hordes or additional zombie types

Outcome:

The town now carries a broader, less uniform ambient threat while retaining the
same lightweight zombie scene and controller.

Validation:

- C# build completed with no warnings or errors

---

## Prototype Day/Night Atmosphere

Completed:

- Added lightweight, naturally continuous procedural ambient wind
- Added randomized distant zombie groans with varied interval, duration and pitch
- Added daytime insect ambience and night-time cricket ambience
- Crossfaded insects and crickets from the existing world-time daylight value
- Shifted wind and distant-groan levels subtly between day and night
- Exposed independent volume values and groan interval tuning
- Kept the implementation free of music, voices, weather and external audio assets

Outcome:

The prototype now has a changing ambient sound bed that reinforces the clock
without introducing additional gameplay systems or renderer-dependent effects.

Validation:

- Headless main-scene run completed without runtime or audio initialization errors
- Existing day/night flashlight and pharmacy objective regression validations passed
- C# build completed with no warnings or errors

---

## Prototype Town and Zombie Performance Polish

Completed:

- Added configurable near and distant zombie awareness intervals
- Throttled local separation scans while preserving continuous movement
- Reused line-of-sight query data instead of allocating it for every check
- Kept the full fifteen-zombie population and existing visual settings

Outcome:

Zombie perception, navigation and separation retain their visible behaviour with
substantially less repeated physics-query and scene-tree work.

Validation:

- Pre-change headless 600-frame runs averaged 8.97 ms per frame
- Post-change headless 600-frame runs averaged 8.58 ms per frame, a 4.4% reduction
- Headless main-scene validation completed without scene or script errors
- C# build completed with no warnings or errors

---

## Zombie Pursuit and Search Polish

Completed:

- Added configurable travel to and local searching around the last known player position
- Preserved attack, visible chase, player search, sound investigation and wander priority
- Resumed pending sound investigations after an unsuccessful player search
- Strengthened the existing lightweight local separation response
- Kept dead-zombie physics, sensing, navigation and noise response disabled

Outcome:

Breaking line of sight no longer makes a zombie immediately abandon pursuit.
Zombies check the last seen area briefly, then return to other stimuli or wandering.

Validation:

- Focused runtime validation passed visible chase, last-known search, queued sound
  investigation and dead-zombie processing shutdown
- C# build completed with no warnings or errors

---

## Inventory and Container Interface Polish

Completed:

- Clarified the open container and player inventory columns with slot capacity
- Added a high-contrast selected-item style and item name, quantity and description
- Kept Take, Store and Use visible while disabling actions that are not currently valid
- Added side-aware selection, initial keyboard focus and clean Escape close behaviour
- Preserved the existing explicit transfer, stacking and capacity rules

Outcome:

Container decisions are readable at a glance with both mouse and keyboard input,
while uncollected and invalid-transfer items remain in their original inventory.

Validation:

- Focused runtime validation passed selection details, action availability, Take,
  Store, Use, full-capacity retention and Escape close
- Existing antibiotics objective-flow regression validation passed
- C# build completed with no warnings or errors

---

## Objective and Gameplay Notification Polish

Completed:

- Replaced overlapping item, objective and save labels with one queued notification display
- Added fade-in, hold and fade-out presentation with duplicate cooldown suppression
- Routed item taken, inventory full, item used, objective update/completion and save/load feedback
- Preserved the structured antibiotics objective and existing save-status signals
- Reworked the safe point as a compact supply crate and sign with subtle local emission

Outcome:

Important survival feedback arrives in order without covering itself, and the
safe point is readable nearby while remaining normally occluded by world geometry.

Validation:

- Focused runtime validation passed queuing, duplicate suppression, fading, all
  required message sources and the occluded physical safe-point treatment
- Antibiotics objective-flow and save/load regression validations passed
- C# build completed with no warnings or errors

---

## Rendered Prototype Performance Pass

Completed:

- Added a repeatable rendered benchmark at 1280 x 720, 17:00 and a fixed camera
- Reduced directional-shadow coverage to the useful town play area
- Removed negligible shadow work and added visibility ranges for road markings
  and small bushes
- Paused zombie animation evaluation while distant or off-screen without
  changing AI, navigation, population or corpse interaction
- Kept the Compatibility renderer, existing lights and major environment features

Outcome:

The same 600-frame rendered benchmark improved from 163.50 FPS with a 9.39 ms
p95 frame time to 198.43 FPS with an 8.26 ms p95 frame time.

Validation:

- Zombie pursuit and search validation passed
- Day/night and flashlight regression validation passed
- Main scene imported successfully in Godot 4.7.1 .NET
- C# build completed with no warnings or errors

---

## Night Visibility and Lighting Balance

Completed:

- Raised the minimum night ambient, sky and directional contributions while
  keeping them well below daytime levels
- Extended the existing flashlight reach while lowering its peak energy and
  softening distance falloff
- Increased the energy of existing house, pharmacy and service-station lights
  without adding lights, shadows or volumetric effects
- Preserved the existing day/night interpolation and settings

Outcome:

Road edges and character silhouettes retain low-level context outside the
flashlight beam, while interiors and entrances remain readable without making
night resemble daytime.

Validation:

- Expanded day/night validation passed configured darkness, silhouette,
  flashlight, interior and entrance-light bounds
- Two post-change rendered checks averaged 187.28 and 188.10 FPS with 8.37 and
  8.20 ms p95 frame times, retaining the performance-pass improvement
- C# build completed with no warnings or errors

---

## Player Flashlight Polish

Completed:

- Added short energy fades for reliable, smooth on and off toggles
- Offset the camera-following beam slightly forward and down to reduce awkward
  illumination immediately around and behind the player
- Tightened the cone and exposed distance and angular attenuation tuning
- Added a subtle central emphasis with a softer cone edge using the existing
  SpotLight3D settings
- Kept flashlight shadows disabled and added no batteries, upgrades or assets

Outcome:

The existing flashlight now turns on immediately without popping to full
brightness, fades out cleanly and gives a more controlled pool of readable light.

Validation:

- Day/night and flashlight validation passed toggle timing, final visibility,
  camera attachment, offset, range, energy, cone and attenuation checks
- C# build completed with no warnings or errors

---

## Zombie Crowd Performance and Separation

Completed:

- Staggered initial awareness, path and separation timing across the fifteen
  authored zombies
- Throttled local-separation scans further when zombies are distant
- Strengthened lightweight close separation and allowed reduced-speed spacing
  while zombies crowd into attack range
- Kept full NavigationAgent avoidance disabled so it does not duplicate the
  existing local steering work
- Preserved independent state, variants, pursuit, search, sound investigation,
  attacks, death shutdown and corpse interaction

Outcome:

Nearby chasers spread apart more clearly, while distant zombies perform
awareness and local-spacing work less frequently and on staggered frames.

Validation:

- New crowd validation passed population, throttling, avoidance, close-spacing
  and dead-zombie inactivity checks
- Pursuit validation passed chase, last-known-position search, queued sound
  investigation and death shutdown
- Rendered checks varied with device state; the final isolated run measured
  163.95 FPS with a 9.61 ms p95 frame time, matching the original rendered baseline
- C# build completed with no warnings or errors

---

## Melee Responsiveness Pass

Completed:

- Routed melee through the configured input action and started ready attacks in
  the input frame
- Added a short input buffer for presses made during the final cooldown window
- Aligned the damage check with the procedural bat crossing the target
- Preserved the per-swing hit guard, configurable cooldown, range, arc, damage
  and consistent horizontal knockback
- Added a lightweight lowered cooldown pose that visibly returns to a raised
  ready pose

Outcome:

The existing bat responds reliably to quick presses, lands damage at the visible
impact point and communicates when the next swing is ready without adding UI or
new combat systems.

Validation:

- New deterministic melee validation passed immediate start, pre-impact timing,
  one-hit guarding, buffered input, repeated damage and ready-pose feedback
- Zombie crowd regression validation passed
- C# build completed with no warnings or errors

---

## Container and Inventory Quality Pass

Completed:

- Kept selection and keyboard focus attached to an item as it moves between a
  container and the player
- Added explicit success, full-inventory, missing-selection and invalid-use messages
- Expanded item details to show name, quantity, description and numeric effect
- Made ItemList activation use Take or Use consistently for keyboard Enter and
  mouse double-click
- Preserved visible Take, Store and Use buttons, explicit transfers, container
  state and existing save compatibility

Outcome:

Inventory decisions remain stable after transfers and invalid actions now
explain what prevented them, while both mouse and keyboard flows use the same
underlying actions.

Validation:

- Expanded inventory validation passed focus and selection preservation, item
  effects, Take, Store, full capacity, invalid Use, valid Use and Escape close
- Objective-flow regression validation passed
- C# build completed with no warnings or errors

---

## Save System Robustness

Completed:

- Preserved save version 1 and every currently persisted gameplay system
- Validated captured state before writing and all deserialized references,
  collections, capacities, item IDs, paths and values before applying
- Wrote and flushed a temporary file before atomically replacing the primary
  save on the same filesystem
- Safely cleaned temporary and replacement-backup files without risking the
  existing primary save
- Added explicit future-version rejection and concise save/load failure logging

Outcome:

Interrupted writes cannot truncate the existing primary save, and malformed or
newer-version data is rejected without partially changing the live game.

Validation:

- Same-process capture, atomic replacement and full-state reload passed
- Fresh-process reload passed
- Missing, malformed and future-version saves were rejected without mutation
- C# build completed with no warnings or errors

---

## Antibiotics Objective Flow Polish

Completed:

- Kept objective transitions guarded so transfer and completion each update once
- Verified the safe point cannot submit antibiotics while they remain in a container
- Clarified the Take notification when antibiotics enter the player inventory
- Separated restored-state display refreshes from gameplay state-change
  notifications so loading completion cannot replay completion feedback
- Increased the compact safe-point material emission within a restrained range
  for night readability
- Preserved completed objective state through the version 1 save system

Outcome:

The pharmacy-to-safe-point flow clearly communicates item ownership, cannot
complete early or twice, and restores a completed game without duplicate messages.

Validation:

- Objective flow validation passed container ownership, exactly-once transition,
  exactly-once completion, repeat interaction and silent restore checks
- Gameplay notification validation passed transfer, objective and safe-point checks
- Same-process and fresh-process save/load validations passed completed-state persistence
- C# build completed with no warnings or errors

---

## Prototype Ambience Mixing Pass

Completed:

- Rebalanced the existing wind, distant groan, insect and cricket layer levels
- Added shared mix headroom so simultaneous ambient layers do not become excessive
- Added a smoothed equal-power day/night crossfade that also handles clock or
  load-time jumps without snapping
- Increased the randomized delay between intermittent groans while retaining
  the existing single-voice no-overlap generator
- Preserved the exported per-layer controls and existing Master audio bus
- Added no audio assets or sound categories

Outcome:

The ambience changes more gently across dawn, dusk and loaded time changes, with
quieter intermittent groans and a less crowded continuous sound bed.

Validation:

- New atmosphere validation passed randomized-delay, headroom, Master bus,
  day mix, smooth transition and night mix checks
- Day/night and flashlight regression validation passed
- Main scene initialized all audio generators without errors
- C# build completed with no warnings or errors

---

## Expanded Prototype Item Catalogue

Completed:

- Added Medkit, Painkillers, Soda, Canned Food and Chocolate definitions while
  retaining the existing Scrap, Bandage, Food, Water and Antibiotics identifiers
- Added category, stack-limit and lightweight project-owned placeholder icon
  metadata to every prototype item
- Made Medkits restore more health than Bandages and added temporary 25% damage
  resistance to Painkillers
- Added combined hunger/thirst restoration for Soda and distinct food values
- Registered every new stable identifier with the version 1 save loader

Outcome:

The existing inventory resource architecture now covers a small readable
survival catalogue without adding crafting, equipment or general buff systems.

Validation:

- Item catalogue validation passed metadata, item effects and non-usable Scrap
- Inventory UI, objective flow, version 1 save/load and day/night regressions passed
- C# build completed with no warnings or errors

---

## Reusable Weighted Container Loot

Completed:

- Extended reusable loot tables with bounded minimum and maximum roll counts
- Added weighted quantities and explicit empty outcomes to eight location profiles
- Assigned appropriate profiles to the existing car trunk, cupboard, pharmacy
  medicine cabinet and zombie corpses
- Kept objective Antibiotics as an authored medicine-cabinet item alongside
  optional generated medicine
- Preserved one-time generation, explicit transfers and player-added contents

Outcome:

Containers can now share location-appropriate data resources without type-specific
generation code, while saved searched state and remaining contents stay compatible.

Validation:

- Weighted-loot validation passed all eight profiles, roll and quantity bounds,
  empty outcomes, one-time generation and retained player-added contents
- Objective Antibiotics remained deterministic
- C# build completed with no warnings or errors

---

## Inventory Stack Management

Completed:

- Enforced per-item stack limits across player and container inventories
- Merged compatible stacks before creating bounded overflow stacks
- Added chosen-quantity Take and Store actions plus a reusable quantity dialog
- Added selected-stack splitting with predictable focus and selection
- Kept failed additions and transfers atomic when capacity is insufficient
- Extended version 1 loading to accept and preserve repeated split-stack entries

Outcome:

The four-slot player inventory now supports deliberate quantity decisions while
containers retain explicit contents and transfers without adding weight,
equipment, sorting, drag-and-drop or crafting.

Validation:

- Stack-management validation passed merging, overflow, capacity, splitting,
  chosen-quantity transfers, selection and quantity display
- Inventory, weighted-loot, objective, notification and save/load regressions passed
- Same-process split-stack save/load passed
- C# build completed with no warnings or errors

---

## Zombie Behaviour Variants

Completed:

- Added reusable Slow Walker, Walker and Runner profile resources
- Applied profile-controlled movement speed, health, attack damage, detection,
  hearing sensitivity and last-known-position search duration to the shared AI
- Added subtle per-instance material overrides without editing source character assets
- Assigned profiles across the existing fifteen zombies with only two Runners
- Preserved the shared controller, animations, navigation, corpse loot and save state

Outcome:

The unchanged population now has readable threat variation without duplicated
AI scripts, special powers, new animations or arcade colours.

Validation:

- Variant validation passed all profile values, population counts, subtle tints
  and corpse-loot presence
- Crowd, pursuit, melee and save/load regressions passed
- Fixed 1280x720 rendered benchmark measured 192.94 FPS average and 8.08 ms p95
- C# build completed with no warnings or errors

---

## Reusable Melee Weapon Definition

Completed:

- Moved baseball-bat identifier, display name, damage, range, attack arc,
  cooldown, knockback and noise radius into one structured resource
- Made the existing combat controller consume its assigned weapon definition
- Routed melee noise through the weapon value rather than player-controller tuning
- Kept attack duration, impact moment, input buffer and procedural swing unchanged
- Kept the baseball bat as the only authored and usable weapon

Outcome:

A later melee weapon can be assigned through data without rewriting hit
detection or input flow, while the current baseball-bat feel remains unchanged.

Validation:

- Melee responsiveness validation passed definition values, immediate input,
  impact timing, one-hit guarding, buffering, repeat damage and ready feedback
- Zombie pursuit and crowd regressions passed
- C# build completed with no warnings or errors

---

## World Interaction Consistency

Completed:

- Standardized prompts as `Press [E]` or `Hold [E]` instructions
- Kept nearest-distance selection and filtered candidates through one cached
  lightweight line-of-sight query
- Allowed each interactable's own collision while rejecting intervening walls
- Propagated door and searched-container prompt changes immediately
- Cleared prompts and hold progress as soon as a target becomes disabled or invalid
- Blocked both input-driven and direct interactions while dead, paused or in
  blocking inventory UI

Outcome:

Doors, containers, corpses, building entrances and the safe point now follow
the same candidate, prompt and blocking rules without changing their actions.

Validation:

- Interaction consistency validation passed nearest selection, prompt updates,
  wall occlusion, death/UI blocking and hold cancellation
- Inventory, objective, notification, weighted-loot, save/load and pursuit
  regressions passed
- C# build completed with no warnings or errors

---

## Prototype Main Menu

Completed:

- Added a lightweight project-owned main menu with New Game, Continue, Settings
  and Quit
- Disabled Continue for missing, malformed or unsupported save files
- Added overwrite confirmation only when a valid save exists
- Started New Game through a clean scene load after removing prior save artifacts
- Added one-shot Continue launch intent so gameplay loads after its scene is ready
- Preserved direct launching of gameplay, benchmark and validation scenes
- Added mouse and keyboard focus flow

Outcome:

Normal project launch now has a clear entry point without introducing profiles,
multiple slots, character selection or cinematic startup.

Validation:

- Main-menu validation passed save-state gating, overwrite confirmation,
  Settings/Back focus and clean one-shot launch state
- Rendered project entry loaded without scene or script errors
- Direct save/load and inventory validation scenes still passed
- Fixed rendered gameplay benchmark remained above target at 162.19 FPS average
  and 9.89 ms p95
- C# build completed with no warnings or errors

---

## Persistent Prototype Settings

Completed:

- Added reusable Settings UI for Master, Ambient and Effects volume, mouse
  sensitivity, display mode, VSync, resolution and graphics preset
- Persisted settings separately in `user://ashwood_county_settings.cfg`
- Routed procedural atmosphere through an Ambient bus that remains under Master
- Applied mouse sensitivity live to the player controller
- Added Low, Medium and High presets using the existing directional-shadow distance
- Preserved the current 42 m shadow distance as the default High preset
- Added a ten-second confirmation/revert flow for resolution and display-mode changes
- Kept the Compatibility renderer and exposed no unsupported graphics options

Outcome:

The prototype now has a focused, reusable settings panel ready for both the main
and pause menus without coupling settings to gameplay save data.

Validation:

- Settings validation passed persistence, bus routing, control values, menu
  application, mouse sensitivity, graphics presets and the revert timer
- Main-menu, atmosphere, day/night, flashlight and melee regressions passed
- Fixed rendered benchmark measured 176.15 FPS average and 9.10 ms p95
- C# build completed with no warnings or errors

---

## Gameplay Pause Menu

Completed:

- Added an Escape-driven pause menu with Resume, Save, Load, Settings, Return to
  Main Menu and Quit actions
- Paused scene simulation while keeping the menu and shared settings UI responsive
- Routed Save and Load through the existing single-slot save manager
- Prevented pause from opening over container inventory or the death presentation
- Removed the player controller's former Escape-only mouse release to give modal
  input one consistent owner
- Restored pause state and mouse capture cleanly when resuming, loading or leaving
  gameplay

Outcome:

Gameplay now has a complete lightweight pause flow without duplicating settings or
save systems and without allowing overlapping modal screens.

Validation:

- Pause-menu validation passed simulation pause, interaction blocking, settings,
  Save/Load, inventory/death exclusions and Resume cleanup
- Inventory, save/load and settings regressions passed
- Fixed rendered benchmark measured 160.23 FPS average and 9.64 ms p95
- C# build completed with no warnings or errors

---

## Service-Station Supplies Objective

Completed:

- Added a structured second objective that unlocks after antibiotics delivery
- Added a searchable service-station shelf with guaranteed Canned Food and Water
  plus the existing weighted service-station loot profile
- Required explicit container-to-player transfers before objective progression
- Accepted one Canned Food and either one Water or Soda at the safe point
- Consumed exactly the submitted quantities while preserving all other inventory
- Extended objective display, prompt and notification feedback for the new flow
- Persisted the additional state in save version 1 with a safe default and
  normalization for saves created before the field existed

Outcome:

The vertical slice now has a short sequential objective loop that reuses the
existing container, inventory, interaction, notification and save foundations.

Validation:

- Service-station objective validation passed activation, guaranteed container
  contents, explicit transfer, exact submission, persistence and prior-save loading
- Original objective, notification and two-process save/load regressions passed
- Fixed rendered benchmark measured 146.22 FPS average and 10.32 ms p95
- C# build completed with no warnings or errors

---

## Persistent Safe-Point Storage

Completed:

- Added an empty Safe Point Storage inventory using the existing searchable
  container and container-inventory UI foundations
- Unlocked storage interaction after antibiotics delivery without adding a second
  competing world prompt
- Kept Take and Store as explicit player actions
- Prioritized valid objective submissions, then opened storage when no delivery
  occurred
- Preserved unrelated player and storage items during supply submission
- Persisted storage search state and item stacks through normal container save data

Outcome:

The safe point now doubles as a small reusable community cache after the first
objective while objective deliveries remain explicit and isolated.

Validation:

- Safe-point storage validation passed unlock gating, Take/Store, naming,
  interaction ownership, persistence and objective separation
- Service-station objective, inventory UI and two-process save/load regressions
  passed
- Fixed rendered benchmark measured 173.11 FPS average and 8.90 ms p95
- C# build completed with no warnings or errors

---

## Prototype Town Composition

Completed:

- Added one lightweight project-owned composition scene around the pharmacy,
  service station, house, roadside and safe point
- Reused current bushes and tyres with restrained primitive bins, a utility
  cabinet, an empty crate, low fencing and roadside barriers
- Placed rear-lot and side-yard dressing away from all building entrances
- Kept the safe-point approach open and all additions visual-only
- Added distance culling and disabled shadows on small primitive details
- Added no collision, physics objects, interactions, assets or map expansion

Outcome:

Previously sparse edges now have clearer land use and visual rhythm while the
established traversal and objective routes remain unchanged.

Validation:

- Town-composition validation passed area coverage, restrained geometry count,
  entrance clearances and visual-only node checks
- Interaction-consistency validation passed
- Rendered gameplay scene loaded without scene or script errors
- Fixed rendered benchmark measured 177.11 FPS average and 8.74 ms p95
- C# build completed with no warnings or errors

---

## Environmental Storytelling Scenes

Completed:

- Added exactly four small static arrangements: roadside breakdown debris,
  looted pharmacy clutter, a barricaded unused house door and an abandoned
  service-station camp
- Used current tyre assets and project-owned primitive geometry only
- Kept each arrangement visually distinct through simple object placement
- Positioned all arrangements away from required doors and interactables
- Added no collision, physics, gore, lights, particles or implied interactions
- Applied distance culling and disabled shadows on the lightweight details

Outcome:

The town now communicates recent abandonment and scavenging through restrained
visual placement without changing gameplay routes or adding systems.

Validation:

- Environmental-storytelling validation passed the four-scene limit, geometry
  budgets, interaction clearance and static-safety checks
- Interaction-consistency validation passed
- Rendered gameplay scene loaded without scene or script errors
- Fixed rendered benchmark measured 171.69 FPS average and 9.09 ms p95
- C# build completed with no warnings or errors

---

## Direct Runtime Launch Tool

Completed:

- Added `tools/launch-runtime.ps1` for launching the game or rendered benchmark
  without the editor
- Added an optional `WIDTHxHEIGHT` resolution argument
- Supported an explicit Godot path, `ASHWOOD_GODOT_PATH`, and common PATH command
  names in a clear precedence order
- Added clear pre-launch failures for missing Godot or `project.godot`
- Added a dry-run mode for checking local configuration without opening the game
- Replaced machine-specific benchmark commands with portable helper examples

Outcome:

Direct runtime and benchmark launches now use one repository-owned command that
works from the project root without embedding a private machine path.

Validation:

- Game and benchmark dry runs passed explicit-path, environment-path and
  resolution handling
- Missing-Godot validation returned the documented clear failure
- The helper launched the real rendered benchmark successfully at 1280 x 720
- Direct helper benchmark measured 174.89 FPS average and 8.87 ms p95
- C# build completed with no warnings or errors
