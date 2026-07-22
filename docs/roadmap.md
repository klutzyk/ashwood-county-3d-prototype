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

- [ ] Hold interaction input to search
- [ ] Display search progress
- [ ] Cancel search when interaction input is released
- [ ] Cancel search when the player leaves interaction range
- [ ] Prevent player movement while searching
- [ ] Open container inventory when searching completes

## Container Inventory

- [ ] Create reusable container inventory data
- [ ] Keep container items separate from player inventory
- [ ] Create container inventory UI
- [ ] Show container display name
- [ ] List contained items
- [ ] Close container UI
- [ ] Pause or restrict player control while container UI is open

## Item Transfers

- [ ] Select an item in the container
- [ ] Transfer selected item to player inventory
- [ ] Remove transferred item from container inventory
- [ ] Prevent transfer when player inventory is full
- [ ] Display player inventory beside container inventory
- [ ] Select an item in player inventory
- [ ] Transfer player item into open container
- [ ] Update both inventories immediately
- [ ] Preserve stack quantities during transfers

## Container State

- [ ] Generate container loot only once
- [ ] Preserve uncollected items in the container
- [ ] Preserve items added by the player
- [ ] Remember whether the container has been searched
- [ ] Keep state while the current scene remains loaded

## Prototype Loot

- [ ] Bandage item
- [ ] Water item
- [ ] Food item
- [ ] Extensible loot-table definition
- [ ] Randomise abandoned-car contents
- [ ] Allow an empty loot result
- [ ] Do not automatically award generated loot

## Item Usage

- [x] Use bandage from player inventory
- [x] Bandage restores health
- [x] Bandage is consumed
- [ ] Food placeholder behaviour
- [ ] Water placeholder behaviour
- [ ] Prevent invalid item usage

## Reusable Containers

- [ ] Convert abandoned car to reusable searchable-container setup
- [ ] Create searchable crate
- [ ] Create searchable cupboard
- [ ] Allow per-container display name
- [ ] Allow per-container search duration
- [ ] Allow per-container loot table

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

- [ ] Front-door interaction
- [ ] Enter-house transition
- [ ] Placeholder house interior
- [ ] Interior collision
- [ ] Interior lighting
- [ ] Exit-house interaction
- [ ] Return player outside near the entrance

## Interior Loot

- [ ] Searchable cupboard
- [ ] Searchable crate
- [ ] Interior loot tables
- [ ] Preserve container contents while switching locations

---

# Vertical Slice 4 — Expanded Threat

- [ ] Three independently functioning zombies
- [ ] Independent zombie health
- [ ] Independent detection and pursuit
- [ ] Prevent excessive zombie overlap
- [ ] Basic wandering
- [ ] Basic patrol area
- [ ] Basic sound attraction
- [ ] Performance test with multiple zombies

---

# Vertical Slice 5 — Prototype World

- [ ] Extend the road
- [ ] Add several building exteriors
- [ ] Add fences
- [ ] Add roadside props
- [ ] Add additional abandoned vehicles
- [ ] Add terrain variation
- [ ] Add roadside shoulders
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