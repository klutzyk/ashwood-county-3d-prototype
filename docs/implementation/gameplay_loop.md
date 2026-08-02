# Gameplay Loop

## Core Loop

Explore the world.

↓

Notice a location, vehicle or container that may hold supplies.

↓

Evaluate danger.

↓

Fight, avoid or escape nearby zombies.

↓

Search the location.

↓

Open its container inventory.

↓

Choose which items to take and which to leave behind.

↓

Manage limited player inventory, health and stamina.

↓

Use supplies when necessary.

↓

Continue exploring or return to safety.

---

## Container Principle

World containers and the player have separate inventories.

Searching a container reveals or generates its contents.

Items are not automatically awarded.

The player decides what to transfer.

Items left behind remain in the container.

Items placed into the container remain there.

The field inventory is available with `I` (controller View/Back) without first
opening a container. It separates four stable quick slots from four backpack
slots, displays stack and carry-weight limits, and lets the player explicitly
assign backpack items to quick slots. While comparing a container, activating a
row quick-moves as much of that stack as can fit; exact-quantity transfer, split,
store, use and Take All remain deliberate actions. A capacity-limited quick move
leaves the remainder in its original inventory and reports whether slots or
carried weight blocked it.

Examples include:

- Abandoned cars
- Cupboards
- Crates
- Lockers
- Refrigerators
- Corpses
- Backpacks
- Safehouse storage

The world should feel persistent rather than behaving like a sequence of one-time reward buttons.

---

## Combat and World-State Foundation

Melee attacks consume stamina and advance through explicit wind-up, contact and
recovery phases. Confirmed hits clamp to the authored contact frame, briefly
pause both combatants, and drive directional zombie stagger, knockback, blood,
debris, light and audio feedback. The current baseball-bat implementation is a
responsive foundation; its procedural pose layer does not replace the future
need for authored two-handed animation, inverse kinematics and weapon-specific
motion sets.

World time composes with clear, overcast, rain, storm and morning-fog profiles.
The active target condition, schedule/lightning timers and deterministic random
states persist with saves; older version-1 saves that predate weather retain the
district's authored starting condition.

---

## First Playable Survival Loop

Encounter zombie.

↓

Take damage.

↓

Sprint away.

↓

Search abandoned car.

↓

Open car inventory.

↓

Take a bandage.

↓

Leave other supplies inside.

↓

Use bandage.

↓

Return later and find the remaining supplies still there.

---

## Feature Test

Every new feature should strengthen at least one of these:

- Exploration
- Risk assessment
- Resource scarcity
- Player choice
- World persistence
- Survival tension
- Believable consequences

Reconsider features that do not improve the core loop.
