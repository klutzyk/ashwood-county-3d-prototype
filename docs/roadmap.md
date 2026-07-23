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

---

# Vertical Slice 2 — Melee Survival

## Player Combat

- [ ] Basic melee input
- [ ] Melee attack animation
- [ ] Attack cooldown
- [ ] Attack range
- [ ] Attack hit detection
- [ ] Prevent repeated damage from one attack

## Zombie Health

- [ ] Zombie health
- [ ] Zombie receives damage
- [ ] Zombie hit reaction
- [ ] Zombie death
- [ ] Disable dead-zombie navigation
- [ ] Disable dead-zombie attacks
- [ ] Disable dead-zombie collision

## Combat Feedback

- [ ] Player hit feedback
- [ ] Zombie hit feedback
- [ ] Basic impact sound
- [ ] Basic attack sound
- [ ] Temporary death presentation

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

---

# Vertical Slice 4 — Expanded Threat

- [x] Five independently functioning zombies
- [ ] Independent zombie health
- [x] Independent detection and pursuit
- [x] Prevent excessive zombie overlap
- [x] Basic wandering
- [ ] Basic patrol area
- [x] Basic sound attraction
- [x] Performance test with multiple zombies

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

- [ ] Prototype clock
- [ ] Day/night lighting transition
- [ ] Configurable day length
- [ ] Display current time

## Needs

- [ ] Hunger data
- [ ] Thirst data
- [ ] Hunger reduction over time
- [ ] Thirst reduction over time
- [ ] Food restores hunger
- [ ] Water restores thirst
- [ ] Basic hunger and thirst UI

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

# Later

- Save and load
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
