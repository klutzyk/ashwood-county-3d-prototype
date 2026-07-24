# Roadmap

This roadmap tracks the prototype and the planned transition into a full production game.

---

# Phase 1 — Prototype (Current Goal)

The purpose of this phase is to answer one question:

> "Is this game fun enough and technically feasible to continue building?"

The prototype is intentionally small, focused and highly polished.

---

# Foundation ✅

- [x] Third-person movement
- [x] Mouse camera
- [x] Character animations
- [x] Sprinting
- [x] Stamina
- [x] Health system
- [x] Damage system
- [x] Player death
- [x] Basic interaction system
- [x] Inventory
- [x] Searchable containers
- [x] Loot system
- [x] Basic environment
- [x] Trees
- [x] Road
- [x] House
- [x] Abandoned vehicle
- [x] Zombie AI
- [x] Zombie navigation
- [x] Zombie attacks
- [x] Basic lighting
- [x] Save / Load

---

# Vertical Slice 1 — Search & Loot ✅

Completed.

- [x] Search mechanic
- [x] Loot containers
- [x] Inventory transfer
- [x] Item stacking
- [x] Medical items
- [x] Food
- [x] Water
- [x] Scrap
- [x] Loot persistence
- [x] Search progress and loot-result feedback
- [x] Polished container transfer feedback

---

# Vertical Slice 2 — Melee Survival

Completed except final polish.

- [x] Baseball bat
- [x] Reusable right-hand baseball bat attachment
- [x] Zombie damage
- [x] Knockback
- [x] Zombie death
- [x] Corpse loot
- [x] Authored two-handed attack animation
- [x] Three-click combo using separate Mixamo attack clips
- [x] Three-step melee combo with anticipation and recovery
- [x] Visual impact timing and anti-spam input rules
- [x] Authored zombie hit reactions with directional knockback and hit stun
- [x] Subtle camera feedback on confirmed melee impacts

Remaining

- [ ] Player hit feedback
- [ ] Attack sound effects
- [ ] Impact sound effects

---

# Vertical Slice 3 — Building Exploration

Completed except interior polish.

Completed

- [x] Enterable buildings
- [x] Door transition feedback and input locking
- [x] Continuous interiors
- [x] Searchable buildings

Remaining

- [ ] Interior searchable cupboards
- [ ] Interior searchable crates
- [ ] Interior loot tables
- [ ] Interior loot persistence

---

# Vertical Slice 4 — Expanded Threat ✅

Completed.

- [x] Multiple zombie types
- [x] Larger zombie population
- [x] Better pursuit
- [x] Better search behaviour
- [x] Crowd behaviour

---

# Vertical Slice 5 — Prototype World

Remaining

- [ ] Additional abandoned vehicles
- [ ] Terrain variation
- [ ] Replace prototype trees
- [ ] Define world boundaries
- [ ] Final navigation rebuild
- [ ] Final performance validation

---

# Vertical Slice 6 — Time & Survival ✅

Completed.

- [x] Day/night cycle
- [x] Flashlight
- [x] Hunger
- [x] Thirst
- [x] Ambient sounds
- [x] Localized zombie alert, attack, hurt and death feedback
- [x] Objectives
- [x] Notifications
- [x] Objective progression and completion presentation
- [x] Readable status values and low-condition HUD feedback

---

# Vertical Slice 6.5 — World Polish

Environment

- [ ] Improve road edges
- [ ] Road cracks and decals
- [ ] Terrain blending
- [ ] Grass variation
- [ ] Bush variation
- [x] Better ground materials

Buildings

- [x] Better house materials
- [x] Better pharmacy exterior
- [x] Better diner
- [x] Better service station

Props

- [x] More abandoned vehicles
- [x] Utility poles
- [x] Road signs
- [x] Mailboxes
- [x] Fences
- [x] Garbage
- [x] Tyres
- [x] Crates
- [ ] Pallets

Lighting

- [ ] Better sunset lighting
- [ ] Better moonlight
- [ ] Better interior lighting
- [ ] Better street lighting

---

# Vertical Slice 6.75 — Exploration

Pharmacy

- [x] Shelving
- [x] Medicine cabinets
- [x] Checkout area
- [x] Storage room

Residential Houses

- [x] Bedrooms
- [x] Kitchens
- [x] Bathrooms
- [x] Living rooms

Service Station

- [ ] Garage
- [ ] Workshop
- [x] Store room

Diner

- [x] Dining area
- [x] Kitchen
- [ ] Office

Loot

- [ ] Logical loot placement
- [ ] Rare loot
- [ ] Medical loot
- [ ] Tool loot

---

# Vertical Slice 7 — Survivor Prototype

Keep this intentionally small.

- [ ] One survivor
- [ ] Idle behaviour
- [ ] Follow behaviour
- [ ] Wait command
- [ ] Dialogue window
- [ ] Trust value
- [ ] One player decision affecting trust

Stop after this slice.

---

# Prototype Completion Checklist

The prototype is considered complete when:

- [ ] Every roadmap task is complete
- [ ] Stable 60+ FPS on the development laptop
- [ ] No gameplay-blocking bugs
- [ ] No crashes during normal gameplay
- [ ] One complete gameplay loop is fully playable

Gameplay loop

```
Spawn
↓
Search buildings
↓
Fight zombies
↓
Collect supplies
↓
Complete objectives
↓
Return to safe point
↓
Save
↓
Load
↓
Continue playing
```

Once this checklist is complete, the prototype has achieved its purpose.

---

# Phase 2 — Production Alpha

The prototype becomes a real game.

World

- [ ] Replace all placeholder buildings
- [ ] Replace all placeholder props
- [ ] High-quality terrain
- [ ] Better vegetation
- [ ] Larger handcrafted town

Gameplay

- [ ] Firearms
- [ ] Weapon durability
- [ ] Crafting
- [ ] Barricading
- [ ] Infection
- [ ] Safehouse

Survivors

- [ ] Multiple survivors
- [ ] Relationships
- [ ] Trading
- [ ] Base assignments

Audio

- [ ] Music
- [ ] Footsteps
- [ ] Zombie audio
- [ ] Indoor ambience

Art

- [ ] Final UI
- [ ] Icons
- [ ] VFX
- [ ] Better animations

---

# Phase 3 — Full Game

Only begin after Phase 2 is stable.

World

- [ ] Larger map
- [ ] Multiple towns
- [ ] Forests
- [ ] Rural areas

Gameplay

- [ ] Vehicles
- [ ] Weather
- [ ] Story
- [ ] Quests
- [ ] Radio system
- [ ] Dynamic events

Systems

- [ ] Steam achievements
- [ ] Steam Cloud
- [ ] Controller support
- [ ] Multiple save slots
- [ ] Settings polish
- [ ] Final optimization

Release

- [ ] Beta
- [ ] Release Candidate
- [ ] Steam Release
