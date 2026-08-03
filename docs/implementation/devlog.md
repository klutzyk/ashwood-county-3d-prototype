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

---

## Final Autonomous Prototype Expansion Regression

Completed:

- Ran every automated validation scene across menus, settings, gameplay,
  interactions, inventory, items, objectives, world dressing and zombie systems
- Ran same-session and fresh-process save/load validation
- Loaded both the project main menu and gameplay scene through rendered runtime
- Fixed a notification test that selected a random weighted-loot stack by index
  instead of the stable Scrap item identifier
- Deterministically disposed settings `ConfigFile` instances during load and save
- Documented the completed queue, current prototype limits and repeatable runtime
  benchmark result

Outcome:

All autonomous expansion slices are integrated, the queue's regressions are
resolved and the repository remains a focused, performant vertical slice.

Validation:

- All 20 non-save validation scenes passed; one rapid-run managed teardown
  required an isolated retry after its assertions had printed PASS
- Two-process version-1 save/load validation passed
- Main-menu and gameplay rendered entry points loaded without scene or script errors
- Three direct-helper benchmark runs measured 169.67, 174.31 and 174.22 FPS
- Median direct-runtime result was 174.22 FPS average and 9.08 ms median p95
- C# build completed with no warnings or errors

Known limitations:

- Scope remains one small map, one melee weapon, two objectives and one save slot
- Art, icons and atmosphere audio remain prototype-quality
- Godot .NET headless shutdown can emit the existing four-object leak warning and
  can rarely return a teardown access violation during rapid multi-process runs

---

## Residential District Expansion

Completed:

- Added one northern residential district at approximately 66% of the original
  ground footprint
- Continued the main road into one attached residential street
- Added eight lots using the existing modular house exterior and roadside
  dressing, including driveways, fences, gardens, bins and bushes
- Reused current utility poles and two abandoned vehicles
- Added a connected lightweight navigation region over the new road network

Outcome:

The town has a distinct residential edge with believable lot spacing while
remaining a bounded prototype map.

Validation:

- Residential-district validation passed lot count and dressing, road collision,
  navigation coverage, vehicle/pole placement and expansion footprint
- Interaction-consistency and same-session save/load regressions passed
- Representative full-game benchmark measured 143.56 FPS average and 11.31 ms
  p95 versus the session baseline of 114.06 FPS and 18.32 ms p95
- C# build completed with no warnings or errors

---

## Ashwood Diner Landmark

Completed:

- Added one modular diner with separate exterior, interior and root scenes
- Added a continuous collision-preserving entrance using the existing hinged door
- Added dining booths, a service counter, kitchen division and lightweight warm
  interior lighting
- Added separate searchable pantry and fridge inventories using existing food
  loot tables and explicit item transfers

Outcome:

The original town now has one recognisable, enterable food landmark without new
items, NPCs or interaction systems.

Validation:

- Town-landmark validation passed modular composition, collision, door
  interaction, lighting and independent container-state checks
- Interaction-consistency, weighted-loot and same-session save/load regressions
  passed
- C# build completed with no warnings or errors

---

## Town Road Network

Completed:

- Added a short diner access junction as a second town intersection
- Added collidable sidewalks along the original main road and residential street
- Added residential centre markings, stop lines, two stop signs and two named
  street signs
- Added physical barriers to define the bounded residential and main-road ends
- Added connected navigation coverage for the diner approach

Outcome:

The road hierarchy now reads clearly at both commercial and residential
junctions while keeping the prototype boundary explicit.

Validation:

- Town-road validation passed junction collision, sidewalk, marking, sign,
  barrier and navigation checks
- Residential-district and zombie-pursuit regressions passed
- C# build completed with no warnings or errors

---

## Town Atmosphere Pass

Completed:

- Added a commercial alley cluster with a dumpster, utility cabinet and
  newspaper boxes
- Added a diner bench and abandoned shopping trolley
- Added a broken residential fence, verge utility equipment and bush growth
- Added a restrained maintenance-layby tyre pile
- Kept all additions static, visual-only, culled and free of implied interaction

Outcome:

Every town zone now carries a clearer everyday purpose and a stronger sense of
recent abandonment without altering traversal or gameplay systems.

Validation:

- Town-atmosphere validation passed zone coverage, prop identity, geometry
  budget, culling and visual-only safety checks
- Existing town-composition and interaction-consistency regressions passed
- C# build completed with no warnings or errors

---

## Zombie Distribution Pass

Completed:

- Preserved all 15 existing zombie node names, paths and save identities
- Grouped three zombies near the pharmacy and three near the service station
- Distributed four zombies along the new residential street
- Created one tight four-zombie danger hotspot at the northern road end
- Left the diner and safe point comparatively quiet, with one remote southern
  wanderer maintaining uncertainty outside the hotspots

Outcome:

Threat density now communicates risk by location without increasing the AI or
rendering workload.

Validation:

- Zombie-distribution validation passed population, save-path, hotspot and quiet
  area checks
- Zombie crowd, pursuit and same-session save/load regressions passed
- C# build completed with no warnings or errors

---

## Logical Loot Placement

Completed:

- Kept pharmacy medicine in its existing objective cabinet and added a separate
  bathroom medicine cabinet using the standard medical loot table
- Added a searchable house kitchen fridge and clarified the existing cupboard
  as kitchen storage
- Retained separate diner pantry/fridge and service-station supply inventories
- Added a service-station toolbox using the existing tools loot table
- Added no item definitions and kept every container on the reusable inventory,
  search and explicit-transfer foundation

Outcome:

Container contents now follow the purpose of each room and business while all
new container paths remain additive for version-1 saves.

Validation:

- Loot-placement validation passed location, display-name, loot-table and
  independent inventory checks
- Weighted-loot, inventory-UI and same-session save/load regressions passed
- C# build completed with no warnings or errors

---

## Expanded Environmental Storytelling

Completed:

- Added a failed emergency roadblock with a crashed vehicle, abandoned case and
  spent flare
- Added a hurried residential evacuation scene with luggage, a dropped map and
  a fallen bicycle
- Added a static diner-side casualty trail with a dropped tray, mug and receipt
- Kept all three scenes non-interactive, collision-free, effect-free and
  lightweight

Outcome:

The expanded town now carries connected signs of evacuation, failed containment
and sudden abandonment alongside the four existing story arrangements.

Validation:

- Environmental-storytelling validation passed all seven arrangements,
  geometry budgets, static safety and interaction clearances
- Town-atmosphere and interaction-consistency regressions passed
- C# build completed with no warnings or errors

---

## Expanded World Polish

Completed:

- Shortened residential driveways and pulled gardens behind the street edge
- Stopped Diner Lane at the diner frontage instead of beneath the building
- Cleared the service-station toolbox from the storage shelf
- Rebuilt the original-town navigation mesh against the expanded collision
  geometry, including the diner and access junction
- Matched the default navigation-map cell height to the baked mesh resolution
- Confirmed local building lights remain lightweight and non-shadowed

Outcome:

The expanded town now has cleaner lot/road joins, consistent prop clearance and
navigation that respects the added landmark while retaining the Compatibility
renderer.

Validation:

- World-polish validation passed road/lot clearance, local-light, renderer,
  navigation-resolution and around-diner path checks
- All 27 non-save validation scenes passed; the zombie-distribution test printed
  PASS during the rapid sweep and then passed normally on the documented
  isolated retry
- Two-process version-1 save/load validation passed
- Representative full-game benchmark measured 181.14 FPS average, 4.98 ms
  median, 9.93 ms p95 and 60.25 minimum instantaneous FPS
- C# build completed with no warnings or errors


## Representative Full-Game Performance Benchmark

Completed:

- Audited the original rendered benchmark against the main-menu gameplay flow,
  complete prototype world, autoload and persistent gameplay managers
- Added a benchmark mode that inherits the real gameplay world and bypasses only
  the main menu
- Retained the normal camera, UI, objectives, needs, save system, ambient audio,
  all 15 zombies and their normal AI, navigation and animation processing
- Added fixed 1280 x 720, 17:00, flashlight-off, VSync-off and uncapped controls
- Added a five-second warm-up, 20-second sample and detailed performance monitors
- Extended the direct-launch helper with `-Target FullBenchmark`

Outcome:

The existing short benchmark is not a simplified render scene: it instantiates
the complete world and leaves AI active. The large reported-FPS discrepancy was
traced primarily to display configuration. Normal gameplay inherited the saved
VSync-on setting, while the benchmark explicitly ran uncapped with VSync off.
The short benchmark's approximately five-second lifetime is expected from its
180 warm-up plus 600 sampled rendered frames.

Validation:

- C# build completed with no warnings or errors
- Synthetic benchmark measured 181.21 FPS average, 5.11 ms median and 8.69 ms p95
- Representative full-game benchmark measured 176.09 FPS average, 5.20 ms median
  and 9.07 ms p95 over 20 seconds
- Both audits found 15 active zombies, 15 navigation agents, normal gameplay
  managers and the same camera, UI, lighting and animation workload
- A diagnostic full-game VSync-on run measured 57.95 FPS at the fixed view

---

## Residential District Visual and Traversal Review

Completed:

- Performed a focused visual and traversal review of the new residential district across all eight lots, street connections, and infrastructure
- Verified lot clearances, driveways, fence placement, utility poles, abandoned vehicles, and front door entrances
- Verified navigation coverage connecting all eight residential lots to the main road network without obstructions
- Confirmed zero floating or clipping props, zero blocked entrances, and clean collision bounds across the district

Outcome:

The new residential district is visually aligned, collision-safe, and fully traversable with unblocked entrances and connected navigation.

Validation:

- Automated residential district validation (`ResidentialDistrictValidation`) passed with zero errors
- World polish validation (`WorldPolishValidation`) passed with zero errors
- C# build completed with 0 warnings or errors

---

## Reusable Baseball Bat Hand Attachment

Completed:

- Extracted the baseball bat model and tuned grip transform into a reusable
  weapon scene
- Mounted the reusable scene under Remy's existing right-hand bone attachment
- Removed only the obsolete hidden bat visual from the gameplay WeaponPivot
- Preserved the WeaponPivot combat pose, hit detection and two-handed idle

Outcome:

The visible baseball bat now follows the animated right hand while combat
continues to use the existing invisible gameplay pivot.

Validation:

- Weapon-hand attachment validation passed right-hand binding, reusable grip
  transform, visible model, two-handed idle blend and attack-pivot checks
- Melee responsiveness validation passed attack start, timed hits, input
  buffering and ready-pose recovery
- C# build completed with no warnings or errors

---

## Authored Melee Combo Flow

Completed:

- Replaced the procedural-only attack presentation with the Universal Animation
  Library two-handed sword strike retargeted to Remy and retimed for the bat
- Added readable anticipation, impact, follow-through and recovery phases
- Added a maximum three-step combo with one queued link per attack
- Rejected early mashing, fourth-step chains and inputs outside the short
  recovery buffer
- Moved damage to the rendered bat-contact portion of the authored motion

Outcome:

Bat attacks now read as committed full-body swings, while deliberate timing can
chain three hits without allowing animation restart spam.

Validation:

- Rendered pose captures confirmed the authored wind-up and follow-through
- Melee responsiveness validation passed timing, one-hit guarding, combo limits,
  queue restrictions and recovery
- C# build completed with no warnings or errors

---

## Melee Impact Feedback

Completed:

- Replaced the zombie scale pulse with the downloaded Mixamo being-hit animation
- Extended the existing hit pause slightly and preserved directional knockback
  with a heavier decay
- Added a very small, short camera displacement only when a melee hit is confirmed
- Confirmed no attack or impact audio assets are currently available to retime

Outcome:

Successful hits now interrupt and displace zombies with a readable reaction,
while misses remain visually quiet and camera feedback stays restrained.

Validation:

- Melee responsiveness validation passed authored reaction playback, hit stun,
  directional knockback and confirmed-hit-only camera feedback
- C# build completed with no warnings or errors

---

## Downloaded Building Material Pass

Completed:

- Replaced flat placeholder exterior materials with downloaded plastered,
  weathered and concrete PBR texture sets
- Replaced placeholder interior floors and partitions with downloaded textured
  materials
- Applied downloaded slate roofing and weathered door texture sets to all four
  enterable building types
- Reused project-owned material wrappers without editing third-party sources
- Preserved all building meshes, collision shapes, entrances and lighting

Outcome:

The house, pharmacy, service station and diner now have distinct, weathered
surface character inside and out without changing their gameplay geometry.

Validation:

- Town-landmark and world-polish validations passed
- Rendered Compatibility world load completed without scene or material errors
- C# build completed with no warnings or errors

---

## Downloaded Streetside Prop Pass

Completed:

- Replaced selected primitive bins, utility cabinets, crates and road barriers
  with the downloaded textured models
- Applied the detailed bin and utility models to the shared residential-lot
  dressing instead of duplicating them per lot
- Kept simpler distant bins where replacing every instance would exceed the
  established lightweight geometry budget
- Preserved all existing placement, entrances, collision and navigation

Outcome:

Commercial back lots, residential verges and roadside boundaries now carry
recognisable textured objects while retaining the town's existing layout.

Validation:

- Town-composition and town-atmosphere validations passed
- World-polish validation printed PASS before the documented managed teardown
- C# build completed with no warnings or errors

---

## Downloaded Interior Prop Pass

Completed:

- Replaced pharmacy and service-station shelf blocks with the downloaded rusted
  shelving model
- Replaced the house bed block with the downloaded weathered mattress
- Replaced the house fridge and both medicine-cabinet blocks with downloaded
  container models
- Retained every existing collision shape, search component, loot table and
  scene path

Outcome:

Key searchable interiors now read as furnished spaces instead of box layouts,
without changing traversal or container gameplay.

Validation:

- Town-landmark, loot-placement and interaction-consistency validations passed
- Rendered Compatibility world load completed without scene errors
- C# build completed with no warnings or errors

---

## Interaction Feedback Pass

Completed:

- Added live percentage feedback while searching and a concise result message
  when a container is first revealed
- Added live container stack counts and matching feedback for both taking and
  storing loot
- Prevented repeated door input during movement and exposed clear
  opening/closing prompt feedback
- Preserved container ownership, explicit transfers and existing door gameplay

Validation:

- Interaction-consistency and gameplay-notification validations passed
- C# build completed with no warnings or errors

---

## Objective and Status HUD Polish

Completed:

- Added clear two-step objective progression and a distinct completion state
- Added a restrained objective-change highlight
- Added live percentages to stamina, hunger and thirst labels
- Added low-condition colour cues to health and survival status readouts

Validation:

- Antibiotics and service-station objective-flow validations passed
- C# build completed with no warnings or errors

---

## Localized Zombie Audio Feedback

Completed:

- Added lightweight spatial alert, attack, hurt and death cues to every zombie
- Timed attack cues to animation starts and hurt cues to confirmed melee contact
- Bounded cue distance and suppressed rapid repeats to preserve the ambience mix
- Reused the existing procedural-audio approach because no authored combat
  audio assets are currently present

Validation:

- Melee-responsiveness and zombie-variant validations passed
- C# build completed with no warnings or errors

---

## Mixamo Melee Animation Pass

Completed:

- Replaced the regular swing with the first authored combo stage
- Queued rapid clicks as separate right, left and downward combo stages
- Kept one damage event and recovery phase per animation
- Removed the combined Combo Ver. 2 playback path
- Changed the prototype's starting time to late morning for clearer evaluation

Validation:

- Separate combo animations remained hand-bound without body clipping
- Player-melee-animation, melee-responsiveness and weapon-attachment validations passed
- C# build completed with no warnings or errors

---

## Closer Interior Camera Framing

Completed:

- Shortened the third-person camera distance from five metres to 3.25 metres
- Raised the camera pivot for a closer over-the-player view
- Added a small spring-arm collision volume so walls pull the camera inside
  cleanly instead of allowing it to remain outside or clip around wall edges
- Preserved mouse-controlled pitch, movement-relative camera direction and
  melee-impact camera feedback

Outcome:

The regular camera now has a closer survival-game framing. Existing building
collision automatically brings it above and nearer to the player in interiors;
the current building dimensions are sufficient and did not require enlargement.

Validation:

- C# build completed with no warnings or errors
- Godot editor import and prototype-world runtime load completed without scene
  or script errors

---

## Weapon Grip Authoring Tool

Completed:

- Added a dedicated editor-only scene using the visible Warrior skeleton and
  the actual looping two-handed idle animation
- Reused the runtime attachment definition, pose resources, right-hand bone
  binding and weapon scene hierarchy
- Added selectable TwoHandIdle, Locomotion and MeleeAttack pose previews
- Added Inspector buttons to reload a saved pose or persist the current gizmo
  position and rotation to the selected existing pose entry
- Preserved saved pose scale during authoring and centralized the runtime/editor
  transform multiplication in one shared helper

Outcome:

Weapon grip offsets can now be aligned visually with Godot's standard 3D gizmo
and saved directly to an existing attachment resource without modifying other
pose entries, base grip transforms, character scale or weapon-owned transforms.

Validation:

- Weapon grip authoring scene cold-loaded in the Godot editor without errors
- Existing weapon-hand attachment validation passed
- C# build completed with no warnings or errors

---

## Historic Downtown Main Street Phase 1

Completed:

- Added a separate 200 m production Main Street scene without changing the
  prototype gameplay sandbox
- Established the documented 17.6 m street profile with two 3.5 m traffic
  lanes, 2.3 m parallel-parking lanes and 3 m sidewalks
- Added two intersections, segmented curbs and fourteen varied placeholder
  storefront plots using lightweight geometry
- Added project-owned triplanar asphalt and concrete material wrappers using
  the supplied third-party texture sets
- Placed the final bakery exterior at a believable 9.02 m by 10 m footprint on
  the west-end north corner and kept the imported mesh unchanged
- Added the supplied shop-front door with existing interaction, hinge,
  collision, prompt and gameplay-noise systems
- Reused the existing player, camera, HUD, survival, melee, zombie, navigation
  and version-1 save/load implementations
- Kept the slice to two placed zombies and no interiors, loot, audio, decals,
  vehicles, vegetation or later streetscape dressing

Outcome:

Historic Downtown now has a focused, independently testable production
environment foundation while the original prototype world remains available as
the gameplay sandbox.

Validation:

- Main Street Phase 1 validation passed dimensions, intersections, sidewalks,
  storefront count, bakery scale/placement/grounding, door movement/collision,
  gameplay composition, zombie count, navigation and save/load checks
- Direct Main Street runtime cold-start completed without scene or script errors
- C# build completed with no warnings or errors

---

## Main Street Phase 1 Placement and Visibility Correction

Completed:

- Restored the player capsule node to the established centred transform and
  moved the Main Street spawn onto the north sidewalk
- Rotated the bakery's real green-awning storefront toward Main Street and
  aligned its facade with the sidewalk edge
- Measured the imported doorway and door meshes, then fitted the shop door,
  hinge, collision and interaction point to the actual opening
- Realigned the bakery shell collision around the visible storefront doorway
- Changed the Main Street start time to 09:00 and raised only the daytime
  ambient, sky and directional energies for clear Phase 1 evaluation

Validation:

- Rendered player-camera, player side, closed-door, open-door, facade and wide
  street views using the Compatibility renderer
- Visual review confirmed visible feet above the sidewalk, storefront facing
  Main Street, a naturally filled doorway, an outward door swing without heavy
  wall clipping and readable morning lighting
- Updated Main Street validation passed spawn floor offset, bakery orientation,
  doorway-relative door bounds, hinge placement, open swing, lighting,
  navigation and save/load checks
- C# build completed with no warnings or errors

---

## Bakery Interior Phase 1

Completed:

- Built a project-owned modular architectural shell inside the unchanged
  production bakery exterior
- Added a 4.5 m by 9.4 m finished wood floor, painted plaster walls, painted
  ceiling and 2.8 m clear interior height
- Established a front retail zone, dark-wood counter with a 2.5 m staff
  passage, rear kitchen/preparation zone and directly connected storage area
- Kept the utility-partition module empty because another enclosed room would
  make the 42.3 m² interior cramped
- Kept the side and rear shell solid because the imported exterior does not
  provide a traversable service opening; the rear door is explicitly deferred
- Added a front threshold ramp to bridge the 0.742 m finished-floor elevation
  without changing player or zombie movement
- Added a bakery-local navigation region matched to the existing 0.45 m-radius,
  1.8 m-high zombie agent and connected the interior to Main Street
- Added two low-energy temporary interior lights for layout inspection only

Validation:

- Bakery Interior Phase 1 focused validation passed modular hierarchy, room
  clearances, solid shell collision and synchronized front-entry navigation
- Existing Main Street Phase 1 validation passed unchanged
- Compatibility-rendered walkthrough covered the storefront entry, retail,
  counter, kitchen and storage and drove a reduction in temporary light energy
- C# build completed with no warnings or errors

---

## Main Street Environment Art Pass

Completed:

- Replaced the clean road read with damaged PBR asphalt, faded markings,
  hand-placed repairs, cracks, oil staining and curb drainage
- Broke up sidewalks with darker repairs, seam weeds, clustered leaves, papers
  and restrained litter rather than uniform procedural scatter
- Added reusable parking meters, newspaper boxes, dumpsters, bike racks,
  utility poles, transformers, power lines and 25 mph street signage
- Preserved the varied storefront row and six-vehicle parking composition while
  adding distinct bakery, bookstore, bank, diner, delivery and alley clusters
- Added bread shelves, cakes and coffee equipment visible through the bakery
  windows without creating a full interior
- Reused existing licensed vehicles, vegetation, street furniture and PBR
  assets, with distance culling on small repeated props
- Kept the entire abandonment layer under the environment hierarchy without
  changing player, zombie, combat, save or interaction code

Outcome:

Main Street now reads as a hand-dressed, recently abandoned rural American
downtown from street level, with stronger period identity, surface age,
reclaiming vegetation and overhead utility depth while remaining a daytime
Compatibility-renderer scene.

Validation:

- Main Street art-pass validation passed surface, prop-density, business,
  vehicle-variety, collision and culling checks
- Multi-angle Compatibility-rendered review covered both street ends, the road,
  north sidewalk and bakery windows
- Direct Main Street runtime completed a 180-frame cold start without scene,
  import or script errors
- C# build completed with no warnings or errors

---

## Glen's Bakery Production Interior

Completed:

- Replaced the four-piece runtime prototype with a hand-authored bakery
  production interior fitted to the existing imported shell
- Established distinct customer retail, glazed pastry display, register,
  coffee, open baking/prep, sink, storage, and restrained abandonment zones
- Integrated seven compact Poly Haven CC0 assets at 1K resolution, totaling
  approximately 11 MB, with a local license record and source provenance
- Reused realistic stoves, furniture, shelving, register, pastry, lighting,
  spice cabinets, and plastic crates while reserving project geometry for the
  architectural shell, cabinetry, trays, and small supporting details
- Added aged PBR floor, brick, plaster, wood, metal, glass, and enamel
  materials, period menu signage, ceiling grid, warm daytime-readable fixtures,
  papers, discarded packaging, and a fallen tray
- Added simplified collision only to the floor and substantial furniture,
  preserving a clear player route from the working front door into the sales
  floor
- Removed the old window-display backboards that read as flat panels from
  inside while retaining the storefront bread, cake, and coffee dressing
- Kept all player, zombie, combat, save, objective, and interaction systems
  unchanged

Validation:

- Focused bakery validation passed production hierarchy, curated asset usage,
  retail and kitchen detail density, lighting limits, simplified collision,
  entrance clearance, and Compatibility-renderer performance bounds
- Existing Main Street art-pass regression validation passed
- Three-angle Compatibility-rendered review covered the entry view, glazed
  display counter, open prep line, rear storage, and the view back to the
  storefront
- Direct Main Street runtime completed a 120-frame cold start without scene or
  script errors
- C# build completed with no warnings or errors

---

## Greenleaf Pharmacy Production Interior

Completed:

- Rebuilt the pharmacy exterior as an aged painted-brick storefront with real
  glazing, framed display windows, a rusted green awning and sign band,
  readable period identity, external wall fixtures, and a working imported
  shop-front door
- Replaced the prototype room with distinct retail aisles, stocked window
  displays, checkout, waiting area, prescription counter and workspace, staff
  office, enclosed storage, and a furnished restroom based on the supplied
  concept reference
- Integrated 23 compact Poly Haven CC0 models, five Poly Haven CC0 PBR surface
  sets, and a trimmed CC0 OpenGameArt toilet-and-sink subset at 1K resolution;
  the retained third-party source payload is approximately 57.3 MiB
- Used textured project-owned architecture and counter work only where modular
  construction was required; all furniture, sanitary fixtures, equipment,
  retail shelving, lighting, and focal clutter use authored external models
- Added aged linoleum, painted plaster, distressed metal, bathroom tile,
  ceiling plaster, double-sided aisle signs, cool fluorescent fixtures,
  looted product spacing, office paperwork, cartons, and restrained discarded
  clutter
- Widened the storage and restroom openings to 1.2 m and moved the office desk
  cluster so the existing 0.45 m-radius player capsule has a verified route
  through every customer and staff destination
- Kept the concept-appropriate single-storey plan; this shell does not require
  a staircase
- Preserved the established
  `Interior/MedicineCabinet/SearchableContainer` path, loot table, inventory,
  objective, and interaction behaviour
- Replaced the Main Street facade-only pharmacy instance with the assembled
  enterable building while keeping combat, player, zombie, save, and gameplay
  code unchanged
- Corrected two existing environment-resource links to the tracked asphalt
  textures and current `bakery_open.glb`, restoring prototype-world regression
  loading without changing behaviour

Validation:

- Focused Greenleaf Pharmacy validation passed storefront identity, concept
  zones, curated asset density, medicine-cabinet preservation, major-only
  collision, working door movement, lighting limits, node bounds, and live
  player-radius clearance checks
- Main Street art-pass and Glen's Bakery production-interior regressions passed
- Day/night flashlight, inventory UI, loot placement, objective flow,
  gameplay notification, weighted loot, save/load, and town-composition
  regressions passed
- Five-angle Compatibility-rendered review covered the exterior, sales floor,
  cross aisle, prescription workspace, and restroom approach
- Full prototype Compatibility benchmark at 1280 by 720 averaged 161.99 FPS
  with 9.41 ms p95 frame time; the scene retained simplified collision and
  shadowless interior fill lights
- C# build completed with no warnings or errors

---

## Main Street Enterable-Building Expansion (2026-07-29)

Completed:

- Enlarged Greenleaf Pharmacy and Silver Spoon Diner at identity scale, moved
  their entrances to face Main Street, and redistributed fixtures and back
  rooms to provide comfortable player circulation
- Added consistent aged PBR wall finishes across the enterable interior set
  and upgraded the bakery's painted-plaster wrapper from a flat placeholder
- Built Ashwood Grocery with checkouts, stocked aisles, produce,
  refrigeration, household goods, receiving, office, storage, and an enclosed
  restroom
- Built Miller Hardware with stocked sales aisles, a service counter, tool
  displays, back stock, warehouse and loading spaces, office, and restroom
- Built Ashwood Police Station with reception, working offices, evidence and
  armory areas, service rooms, and a connected basement containing two
  furnished prison cells
- Placed the three new buildings on Main Street, corrected the diner
  orientation, cleared nearby street dressing from entrances, and segmented
  the ground beneath the police-station basement
- Added configurable player auto-step and ground snapping for normal curbs,
  thresholds, and stair risers up to 0.32 metres while keeping taller
  obstacles dependent on the existing jump action
- Retained compact 1K source assets and recorded the new Grocery, Hardware, and
  Police Station Poly Haven CC0 packs alongside their local source manifests

Outcome:

Main Street now has a broader set of distinct, explorable businesses with
roomier circulation, textured architecture, business-specific prop density,
and a multi-level civic interior.

Validation:

- Corrected the real world-space transforms for all seven enterable storefronts
  so their doors and display windows face the Main Street sidewalk
- Replaced the remaining live primitive street blockouts with detailed imported
  benches, aged hydrants, tyres, planter boxes, delivery equipment, barrels,
  repair bags, brooms, and storage carts while retaining only supporting
  project-owned road wear, drainage, utilities, and signage
- Added 190 individually composed vegetation, refuse, service-clutter, damaged
  street-furniture, and environmental-story placements. Shared scenes are
  instanced, entrance approaches stay clear, substantial collision remains
  sparse, and category-specific distance culling protects the Compatibility
  renderer
- Completed full-height wall, doorway-header, and imported-door enclosure for
  the Pharmacy, Diner, Willow Outfitters, Grocery, Miller Hardware, and Police
  back rooms and restrooms; corrected the final Miller bathroom fixture scale
  and grounding
- Added production-scene traversal regressions that drive the real player
  through all seven storefront thresholds and both directions of the police
  basement stairs using forward movement only, with no jump input
- All 18 focused Godot checks passed, covering storefront traversal, player
  step handling, police stairs, room enclosure, every affected building,
  Main Street art and dressing, melee responsiveness, melee animation, and
  save/load
- Compatibility visual reviews confirmed that Bakery, Grocery, Pharmacy,
  Willow Outfitters, Miller Hardware, Silver Spoon Diner, and Ashwood Police
  entrances face their actual sidewalks and that the dense dressing remains
  outside the entry routes and travel lanes
- The nine new Poly Haven 1K source packs occupy approximately 23.4 MB; all 49
  retained files match the MD5 records preserved in the local source manifest
- Compatibility-rendered full-prototype benchmark at 1280 by 720 averaged
  167.13 FPS with 5.40 ms median and 9.69 ms p95 frame time, 474 draw calls,
  and approximately 646 visible objects
- C# build completed with no warnings or errors

---

## User-Supplied Asset Integration and Ashwood County High School (2026-07-29)

Completed:

- Audited the user-supplied Sketchfab download set, retained 34 CC BY 4.0 GLBs,
  and preserved embedded titles, creators, source and license links, and MD5
  hashes in a local source manifest
- Excluded eight assets with redistribution, ShareAlike-duplication,
  performance, quality, or redundancy concerns instead of importing the batch
  indiscriminately
- Added normalized reusable wrappers with sensible grounding, selective simple
  collision, and distance culling, then integrated the detailed props into
  Main Street, Pharmacy, Diner, Willow Outfitters, Miller Hardware, Ashwood
  Grocery, Ashwood Police Station, and the school grounds and interiors
- Built and placed the two-storey Ashwood County High School with a
  street-facing double entrance, shallow threshold, continuous walkable
  staircase, five furnished classrooms, library, cafeteria, offices, two
  enclosed restrooms, and a dedicated double-height gymnasium
- Dressed the school with 45 licensed student desks, lockers, boards, books,
  classroom supplies, furniture, sports equipment, papers, cobwebs, bicycles,
  playground and lunch-yard props while preserving broad circulation and a
  1.8-metre exterior entrance route
- Kept the work environment-art-only and reused the established player and
  traversal systems without changing combat, zombies, inventory, or save data

Validation:

- User-supplied asset-library validation passed all 34 retained files and their
  curated wrapper bounds
- School validation passed with 311 textured architectural pieces, 201 sourced
  prop instances, 29 distinct sourced models, and 45 student desks
- Real-player school entrance validation passed from the actual sidewalk
  through the double entrance using only forward movement and no jump
- Real-player staircase validation passed ascent and descent between both
  storeys using only forward movement and no jump
- Compatibility-rendered school-approach benchmark at 1280 by 720 averaged
  97.76 FPS with 9.88 ms median, 16.63 ms p95, 2,613 draw calls, and 2,612
  visible objects after room-scale interior-prop distance culling while
  retaining the full exterior window set at street-view distance
- Pharmacy, Diner, Willow Outfitters, Miller Hardware, Ashwood Grocery, and
  Ashwood Police Station validators passed after the asset-placement pass;
  Main Street storefront traversal, room enclosure, wall-material, art-pass,
  player-step, and police-basement-stair regressions also passed

---

## Golden-Hour Glen's Bakery Supply Run (2026-08-01)

Completed:

- Changed the production boot flow to the main menu and Main Street while
  keeping a dedicated versioned save slot for this vertical slice
- Built a focused route from a composed relief cache to Glen's Bakery, with a
  clear bakery identity, delivery van, five placed zombies, readable objective
  HUD, warm interior beacon, distant ridges, and a water-tower landmark
- Added a project-owned, AI-generated golden-hour cloud cover, sustained
  late-afternoon lighting, atmospheric fog, warmer surface response, and
  restrained ambient audio
- Added Glen's Bakery's persistent emergency pantry using the shared container
  inventory foundation; searching reveals guaranteed canned food and water but
  transfer remains an explicit player action, and alternate food sources cannot
  bypass the required Bakery search
- Preserved separate world and player inventories, delivery consumption,
  searched state, player-added container items, objective state, and all five
  zombies through production save/load; the menu rejects incomplete production
  persistence sets before enabling Continue, and runtime validation rejects
  mismatched sets before live state can be mutated
- Added camera-distance visual culling for dense distant buildings and vehicles;
  collision, interaction, navigation, inventories, and save state remain active
- Added a deterministic five-plate 1920 by 1080 visual-review scene, focused
  supply-run validation, and a representative rendered performance benchmark

Validation:

- C# build completed with no warnings or errors
- Main menu, Main Street Phase 1, Main Street art, apocalypse dressing, Bakery
  interior, supply-run, enterable-building, and storefront-traversal checks
  passed
- The median result from three consecutive Compatibility-rendered
  bakery-approach runs at 1280 by 720 with the live player, five zombies, HUD,
  and golden-hour environment was 83.92 FPS average, 11.35 ms median, 19.79 ms
  p95, 23.26 ms p99, 39.19 FPS one-percent low, and 1,199.9 draw calls over
  600 sampled frames per run; average throughput exceeded 60 FPS, but sustained
  tail latency did not yet justify marking the stable-60-FPS roadmap goal done
- Independent visual review found a clear improvement from the earlier blockout
  and functional passes for the route, threshold, cache, landmark, lighting,
  and HUD, while still rejecting AAA parity because hero assets, vegetation,
  animation, atmosphere, and final-art UI remain below that benchmark

## Old Mill Bridge - Blackwater River crossing

Built the Old Mill Bridge landmark west of Main Street, the first Ashwood County
location outside Historic Downtown and the crossing named on the county planning
map.

Changes:

- Added `scripts/world/OldMillBridge.cs`, a [Tool] Node3D that generates the
  entire location procedurally: gorge terrain, river surface, steel truss, deck,
  abutments, guardrails, the Old Mill ruin, checkpoint dressing and all collision
- Generated the gorge as a heightfield with a meandering channel, terraced rock
  bedding planes on the banks, and a rolling landform beyond the rim; the bank is
  held under the character controller's 45 degree floor limit so a player who
  slides in can climb back out
- Authored `assets/materials/blackwater_river.gdshader`, a fully procedural
  water shader. The Compatibility renderer exposes no depth texture, so the
  shader carries an analytic copy of the terrain height function and derives true
  water depth from it; depth drives the shore foam band and an alpha falloff that
  terminates the surface exactly at the waterline
- Built the bridge as a Parker through truss: 72 m single span in 8 panels, with
  a polygonal top chord, inclined end posts, Pratt web, portal frames, top
  lateral bracing, gusset plates, transverse floor beams and stringers
- Batched all structural members into four MultiMesh instances, one per material,
  so roughly 450 truss members and the rest of the built geometry cost four draw
  calls rather than several hundred
- Scattered the existing project tree and bush library across the rim and banks
  via rejection sampling against slope, water level, the road corridor and the
  built structures. Their materials are carried as surface_material_override
  entries on the source MeshInstance3D, which MultiMeshInstance3D cannot express,
  so the overrides are baked into duplicated meshes
- Instanced the location into `scenes/world/ashwood/main_street.tscn`, moved the
  PrototypeSafetyBoundary west wall from x = -110.5 out to x = -256.5, and added
  corridor and connector walls so the expanded world stays sealed

Validation:

- C# build completed with no warnings or errors
- `tests/old_mill_bridge_validation.tscn` passed against the integrated Main
  Street scene. Checks are physics queries, not node-name inspections: a
  continuous walkable lane is proven by raycasting 71 stations from x = -108 to
  x = -248, parapets are proven at five points along the span on both sides, the
  old containment wall is proven removed, and the new one proven present
- `tests/old_mill_bridge_visual_review.tscn` produced ten 1920 by 1080 renders,
  eight controlled framings plus two using Main Street's own lighting and fog
- Density benchmark measured 31.58 FPS average against a recorded 31.35 FPS
  baseline, so no regression on that view. The benchmark camera faces east, away
  from the bridge, so this bounds the cost of the location being loaded rather
  than the cost of looking at it

Known gaps:

- The navigation mesh is a baked resource covering Main Street only, so zombies
  cannot path onto the crossing
- The mill has no searchable container yet
- Visual quality is a strong blockout with real materials, weathering and
  vegetation, but hero-asset fidelity, decals, and water interaction remain
  below the State of Decay reference