# Roadmap

Each checkbox should represent approximately one focused implementation session.

Complete and test one vertical slice before starting the next.

---

# Foundation

- [x] Third-person movement
- [x] Mouse-controlled camera
- [x] Character animations
- [x] Sprint and stamina
- [x] Basic environment
- [x] Road
- [x] Trees
- [x] House exterior
- [x] Abandoned car
- [x] Zombie navigation
- [x] Zombie detection and chase
- [x] Zombie attack
- [x] Player health
- [x] Player damage
- [x] Player death and restart
- [x] Reusable interaction system
- [x] Interaction prompt
- [x] Minimal player inventory
- [x] Prototype bandage usage

---

# Vertical Slice 1 — Search and Loot

## Search Flow

- [x] Hold interaction input to search
- [x] Display search progress
- [x] Cancel search when interaction input is released
- [x] Cancel search when the player leaves interaction range
- [x] Prevent player movement while searching
- [x] Open container inventory when searching completes

## Container Inventory

- [x] Create reusable container inventory data
- [x] Keep container items separate from player inventory
- [x] Create container inventory UI
- [x] Show container display name
- [x] List contained items
- [x] Close container UI
- [x] Pause or restrict player control while container UI is open

## Item Transfers

- [x] Select an item in the container
- [x] Transfer selected item to player inventory
- [x] Remove transferred item from container inventory
- [x] Prevent transfer when player inventory is full
- [x] Display player inventory beside container inventory
- [x] Select an item in player inventory
- [x] Transfer player item into open container
- [x] Update both inventories immediately
- [x] Preserve stack quantities during transfers

## Container State

- [x] Generate container loot only once
- [x] Preserve uncollected items in the container
- [x] Preserve items added by the player
- [x] Remember whether the container has been searched
- [x] Keep state while the current scene remains loaded

## Prototype Loot

- [x] Bandage item
- [x] Water item
- [x] Food item
- [x] Extensible loot-table definition
- [x] Randomise abandoned-car contents
- [x] Allow an empty loot result
- [x] Do not automatically award generated loot

## Item Usage

- [x] Use bandage from player inventory
- [x] Bandage restores health
- [x] Bandage is consumed
- [x] Food placeholder behaviour
- [x] Water placeholder behaviour
- [x] Prevent invalid item usage

## Reusable Containers

- [x] Convert abandoned car to reusable searchable-container setup
- [x] Create searchable crate
- [x] Create searchable cupboard
- [x] Allow per-container display name
- [x] Allow per-container search duration
- [x] Allow per-container loot table

## Inventory Interface Polish

- [x] Highlight and describe the selected player or container item
- [x] Show Take, Store and Use availability from current capacity and player state
- [x] Support mouse selection, keyboard focus navigation and Escape close

---

# Vertical Slice 2 — Melee Survival

## Player Combat

- [x] Basic melee input
- [x] Melee attack animation
- [x] Attack cooldown
- [x] Attack range
- [x] Attack hit detection
- [x] Prevent repeated damage from one attack

## Zombie Health

- [x] Zombie health
- [x] Zombie receives damage
- [x] Zombie hit reaction
- [x] Zombie death
- [x] Disable dead-zombie navigation
- [x] Disable dead-zombie attacks
- [x] Disable dead-zombie collision

## Combat Feedback

- [ ] Player hit feedback
- [x] Zombie hit feedback
- [ ] Basic impact sound
- [ ] Basic attack sound
- [x] Temporary death presentation

## Corpse Loot

- [x] Keep dead zombies as searchable corpses
- [x] Attach reusable container inventory to each corpse
- [x] Generate deterministic randomized corpse loot only once
- [x] Include Bandage, Food, Water and Scrap prototype loot
- [x] Preserve uncollected corpse loot through save/load

---

# Vertical Slice 3 — Building Exploration

## House Entry

- [x] Front-door interaction
- [x] Continuous doorway entry without teleporting
- [x] Placeholder house interior
- [x] Interior collision
- [x] Interior lighting
- [x] Exit through the same physical doorway
- [x] Preserve player position across entry and exit

## Modular Buildings

- [x] Separate house exterior, interior and root scenes
- [x] Separate pharmacy exterior, interior and root scenes
- [x] Separate service-station exterior, interior and root scenes
- [x] Reusable animated door controller
- [x] Collision-preserving hinged doors
- [x] Lightweight warm interior lighting

## Interior Loot

- [ ] Searchable cupboard
- [ ] Searchable crate
- [ ] Interior loot tables
- [ ] Preserve container contents while switching locations

## Antibiotics Objective Prototype

- [x] Place antibiotics in a searchable pharmacy container
- [x] Require explicit transfer into the player inventory
- [x] Track search, return and completion as structured states
- [x] Submit antibiotics at the marked safe point
- [x] Display objective guidance and brief completion feedback

## Objective and Player Feedback Polish

- [x] Queue and fade item, capacity, objective, save and load notifications
- [x] Suppress repeated notification spam
- [x] Replace the oversized safe-point glow with compact physical dressing

---

# Vertical Slice 4 — Expanded Threat

- [x] Approximately fifteen independently functioning zombies
- [x] Independent zombie health
- [x] Independent detection and pursuit
- [x] Prevent excessive zombie overlap
- [x] Basic wandering
- [x] Basic patrol area
- [x] Basic sound attraction
- [x] Performance test with multiple zombies

## Performance Polish

- [x] Throttle zombie awareness and separation work by distance and interval
- [x] Reuse zombie line-of-sight query data to avoid per-check allocations

## Pursuit Polish

- [x] Pursue and search the player's last known position after losing sight
- [x] Resume pending sound investigation or wandering after player search
- [x] Keep dead-zombie sensing and navigation inactive
- [x] Strengthen lightweight local separation

---

# Vertical Slice 5 — Prototype World

- [x] Extend the road
- [x] Add several building exteriors
- [x] Add fences
- [x] Add roadside props
- [ ] Add additional abandoned vehicles
- [ ] Add terrain variation
- [x] Add roadside shoulders
- [ ] Replace mismatched prototype trees
- [ ] Define world boundaries
- [ ] Rebuild navigation for expanded area
- [ ] Confirm acceptable performance

---

# Vertical Slice 6 — Time and Survival

## Time

- [x] Prototype clock
- [x] Day/night lighting transition
- [x] Configurable day length
- [x] Display current time

## Flashlight

- [x] Toggle a camera-directed flashlight with F
- [x] Expose beam range, energy and cone angle
- [x] Keep the light lightweight for indoor and outdoor use

## Needs

- [x] Hunger data
- [x] Thirst data
- [x] Hunger reduction over time
- [x] Thirst reduction over time
- [x] Food restores hunger
- [x] Water restores thirst
- [x] Basic hunger and thirst UI

## Atmosphere

- [x] Ambient wind
- [x] Randomized distant zombie groans
- [x] Daytime insects
- [x] Night-time crickets
- [x] Day/night ambient crossfade
- [x] Exported atmosphere volume controls

---

# Vertical Slice 7 — Survivor Prototype

- [ ] Survivor character
- [ ] Survivor idle behaviour
- [ ] Survivor follow behaviour
- [ ] Basic dialogue UI
- [ ] Dialogue choice
- [ ] Trust value
- [ ] Structured memory record
- [ ] One player choice that changes trust

---

# Prototype Save/Load

- [x] Versioned save-data classes without live-node serialization
- [x] One local F5/F9 save slot with brief status feedback
- [x] Restore player transform, health, stamina and inventory
- [x] Restore objective state and world time
- [x] Restore existing container search state and remaining items
- [x] Restore placed zombie alive/dead state
- [x] Handle missing and invalid save files safely

---

# Autonomous Polish Batch

- [x] Improve rendered prototype performance and add a repeatable fixed-scene benchmark
- [x] Improve nighttime lighting readability
- [x] Polish the player flashlight
- [x] Improve zombie crowd performance and separation
- [x] Improve melee responsiveness
- [ ] Refine inventory and container usability
- [ ] Harden prototype save loading
- [ ] Polish antibiotics objective flow
- [ ] Balance prototype ambience
- [ ] Validate and clean the autonomous polish batch

---

# Later

- Community and safehouse
- Advanced survivor relationships
- Character needs and schedules
- Weapons
- Firearms
- Infection
- Vehicles
- Weather
- Story
- Quests
- Radio system
- Larger handcrafted world
- Steam integration
