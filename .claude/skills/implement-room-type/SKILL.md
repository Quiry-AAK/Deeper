---
name: implement-room-type
description: Build one room type for "Deeper" end to end — design read, reuse decision, code, ASCII layout, prefab, play-mode verification and docs. Takes the room type as an argument (Combat, Secret Vault, Trapped Soul, Mini-Boss, Final Boss, or a specific new layout). Use for "/implement-room-type <name>", "build the Secret Vault room", "add the Trapped Soul room", "author another combat room layout", or any request to implement or extend a room type.
---

# Deeper — Implement a Room Type

**Argument:** the room type to build. Resolve it before anything else (below).

One room type, built the way the first one was: four small classes with one job each, a layout
committed as readable data, a prefab mounted in `TestScene`, and a verification pass that actually
runs it. `Scripts/Rooms/` and `Prefabs/Rooms/CombatRoom_UpperCaves_01.prefab` are the reference
implementation — read them before writing anything, and copy their shape rather than inventing a
second one.

## Step 0 — Resolve the argument

| Argument | What it means |
|---|---|
| `Combat` | Already built. This becomes a **layout** job (another of the 6 per biome) or a gap job — jump to Step 5. |
| `Wave` / `Wave Room` | **Not a room type.** A variant flag on Combat Rooms, and the code already supports it (`WaveSpawner` with 2–3 waves, `IsWaveRoom` derived). Say so, then offer the real work: authoring a layout flagged for it. |
| `Secret` / `Secret Vault` | New type. See `references/room-types.md`. |
| `Trapped Soul` | New type. |
| `Mini-Boss` | New type, and the largest — blocked on a boss actor existing at all. |
| `Final Boss` | Bespoke one-off arena, deliberately outside the generic room system. |
| `Reward Room` | **Cut** (CORE_SYSTEMS §8). Do not build it. Say why and offer the nearest live type. |
| Anything else | Ask. Do not guess a room type into existence — Design Rule 9. |

If the argument is missing or ambiguous, run `check-room-types` and offer the user the unfinished
list rather than picking one yourself.

## Step 1 — Find out what already exists

Run the `check-room-types` audit scoped to this type first. This is not ceremony: the answer
changes the job entirely. A type at `BUILT — GAPS` means the work is its outstanding list, not a new
class, and writing a second `RoomDoor` because you did not look is the exact failure this step
exists to prevent.

## Step 2 — Read the design for this type

`references/room-types.md` gives the sections per type. Read the actual doc lines — they move, and
the reference file is a pointer, not a copy.

Collect three things:

1. **What the room does**, in the design's words.
2. **Its numbers** — from `04-BALANCE.md`. Every number with no source is a number you are about to
   invent, and inventing is allowed (the docs have holes) but recording it is mandatory.
3. **Its open questions.** Several room types have a design hole the Rising Hazard's removal left
   behind — a Secret Vault that costs nothing, a soul that is free to free. You cannot build past
   these silently.

## Step 3 — Decide reuse before writing anything

Design Rule 2: systems get reskinned, not rebuilt. Before adding a class, check it against what
exists:

| Need | Already exists |
|---|---|
| Doors that shut and open | `RoomDoor` |
| "The player walked in" | `RoomEntry` (layer 8 `RoomTrigger`) |
| An encounter as data | `WaveSpawner` + `ActorPool` |
| Arm → lock → clear lifecycle | `CombatRoom` |
| A delayed, telegraphed arrival | `SpawnTelegraph` |
| Enemy stats as data | `EnemyDefinition` |
| A player number changing | `PlayerStats.SetSource` |
| Painting a layout | `Scripts/Editor/RoomLayout.cs`'s shape |

A new class earns its place by having a job none of these has — a soul interactable, a key check, a
boss phase controller. Add it as its own file in `Deeper.Rooms`, named after what it is on screen.
If the feature seems to need a framework, stop and raise it.

## Step 4 — Get the shape signed off

Before writing code, put a short plan to the user: the classes (new vs reused), the layout sketch,
every number you had to invent, and every design conflict you hit. This is a genuine gate for the
types with open design questions — "what does freeing a soul cost now" is the owner's call, not
engineering's, and building on a guess wastes the whole pass.

The owner's instruction overrides the docs when they disagree. Record the divergence in the change
brief; do not block on it and do not edit `Design/`.

## Step 5 — Write the code

House rules, all of them load-bearing here:

- **`namespace Deeper.Rooms`**, one job per class, `sealed`, `[DisallowMultipleComponent]`.
- **Tunables are `[SerializeField]` with a `[Tooltip]`.** No consts, no magic numbers.
- **Wire references in the Inspector**; fall back to `GetComponent`/`GetComponentInParent`, **never
  `RigRefs.Find`** in room code. It searches from `transform.root`, and anything spawned under a
  spawner has the *spawner* as its root — an optional field then resolves against a sibling actor.
- **Every room-scoped trigger volume goes on layer 8 `RoomTrigger`.** Never Default:
  `ThrownRock.blockingLayers` is Default, so a Default-layer volume eats every projectile crossing
  it, which reads as "rocks randomly vanish".
- **Pooled things reset in `OnEnable`**, not `Awake` — a recycled instance never runs `Awake` again.
- **`Damageable.Died` survives pooling.** Track what is alive by *state* (a pruned list of
  `Damageable`, unsubscribing as it goes), never by a counter — a leaked double-subscription then
  changes nothing, where `--count` opens the doors a kill early.
- **`switch` with an explicit `default` over enums.** A chained ternary silently gives every future
  enum value the last branch.
- **Public methods and `[ContextMenu]` for every state change** (`Arm()` is the model) — the
  verification pass drives these directly.
- **Raise an event for what does not exist yet** rather than reaching forward into it
  (`CombatRoom.Cleared`). Do not build the floor loader as a side effect.
- **Comments explain why** — the alternative rejected, the measurement, the bug the line prevents.
- Keep files under ~300 lines. Past that, it is doing two jobs.

## Step 6 — Author the layout as committed data

The map is an ASCII block in `Scripts/Editor/<Type>Layout.cs`, with the tilemap paint and the marker
positions **derived from that same string** — that is what stops the map and the prefab drifting
apart. `RoomLayout.cs` is the template, including its legend. This is not a room generator:
`LEVEL_DESIGN` §1 locks rooms as hand-built, and a human editing those characters *is* the hand.

Two engine facts bind every layout, and neither is in any design doc:

- **`EnemyChase` has no pathfinding** — straight-line steering only. Interior cover must be isolated
  convex posts with clearance. Any concave pocket traps an enemy and the room never unlocks.
- **Aggro radius binds spawn placement.** `EnemyTarget.Acquired` gates all movement and attacking at
  a radius of 10–12, so `LEVEL_DESIGN` §4's "spawn points at room edges" leaves enemies standing
  still in a 28-wide room. Place spawns inside the radius and note the divergence.

Also honour `LEVEL_DESIGN` §2's "at least 2 viable positioning zones" — a single open box favours
Katana by default, which is not the goal.

## Step 7 — Art

Placeholder art is generated by a committed editor tool (`Scripts/Editor/PlaceholderRoomArt.cs`),
never hand-painted into the project. Real art goes through the **`deeper-art` skill** — invoke it,
do not improvise sprites.

## Step 8 — Build the prefab and mount it

Assemble the prefab, apply the layout via its menu item, and mount it under `Level` in
`TestScene.unity`. Everything testable lives in a prefab or a component, never authored only in the
scene (`Engineering/02-TEST_SCENE.md`).

Add a harness hook — a `Scripts/Testing/` control to re-arm or re-trigger the room, plus a
`TestOverlay` status line. Note that **`F12` was the last free function key**: the next harness
addition needs a modifier chord or a real debug menu, so raise that rather than stealing a key.

## Step 9 — Verify it, and look at it

Structurally-checked code is not working code — the last two blind passes shipped four real defects
that every assertion passed. Read `Engineering/01-VERIFICATION.md` before starting; the essentials:

1. **Compile first** — the whole project compiles headless via the Roslyn shipped in the editor
   install (§10). Cheap, and it turns "I read it carefully" into a number.
2. **`Application.runInBackground = true`** at the start of any automated play-mode session, or the
   player loop is frozen and everything reports as "nothing happened" (§1).
3. **Drive public methods.** Keyboard input does not reach an unfocused play mode; a virtual
   **gamepad** does, with `backgroundBehavior = IgnoreFocus` set first and restored after (§2).
4. **Use a persistent observable** — `execute_code` compiles a fresh assembly per call, so nothing
   survives between calls. Room state, alive counts and door collider states are all readable; damage
   to a parked `TrainingDummy` is the model for anything transient.
5. **Screenshot with a named camera** — `camera="Main Camera"`, `output_folder="Captures"` outside
   `Assets/`. A bare viewport grab is blank in this editor layout (§3).
6. **Clean up in a `finally`.** A leaked `Player(Clone)` looks exactly like an engine bug (§7).

Produce a results table in the engineering plan, in the shape the first Combat Room's has: fresh
state, trigger, the full lifecycle, pooled reuse across several runs, re-arming mid-cycle, and
"left behind" (console clean, `timeScale` 1, no stray `(Clone)` roots, scene not dirty). Then
**look at the pictures** — armed, active, cleared.

## Step 10 — Document in the same pass

- **`Docs/Engineering/00-IMPLEMENTATION_PLAN.md`** — a section for the room type: what was built,
  why it split the way it did, what the verification showed, and an honest `### Outstanding` list.
  That list is what `check-room-types` reads later, so write it for a stranger.
- **`Docs/00-DESIGN_CHANGE_BRIEF.md`** — a numbered entry for every invented number, every
  owner-directed divergence and every conflict found, tagged DECIDED / PROPOSED / CONFLICT.
- **Never touch `Docs/Design/*`.** Flag conflicts into the brief instead; it feeds a single
  coordinated Rule 14 reopen.

## Never

- Never build a room type without reading its design sections in that pass.
- Never resolve a design conflict silently — raise it (Design Rules 9, 11, 12).
- Never author a layout by dragging objects in the scene.
- Never put a room trigger volume on the Default layer.
- Never scaffold the floor loader, the reshuffling bag or a second room type "while you are in
  there". One concrete objective at a time.
- Never report it built without having run it and looked at it.
