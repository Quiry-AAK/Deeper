---
name: check-room-types
description: Audit every room type "Deeper" 's design defines against what is actually built, and report a per-type implementation status — including what is still outstanding on the ones that already work. Use for "which room types are implemented", "room status", "what rooms are left", "is the Secret Vault built", "room type audit", or any question about how far the room system has got. Reports only; building a room type is the `implement-room-type` skill.
---

# Deeper — Room Type Status Audit

Answer the question "which room types are done" by **re-deriving it from the docs and the
filesystem every time**. Never answer it from memory, from `CLAUDE.md`'s prose, or from a previous
run of this skill. All three are summaries written at a moment in time; the room list itself has
already changed once (Reward Room cut, Trapped Soul added), and a status report that is quietly a
month old is worse than no report.

The audit is cheap — five greps and two globs. Run it.

## The two halves of an honest answer

A room type is not binary. Most of this project's rooms are somewhere between "no file exists" and
"a player could meet this in a run", and the useful part of the report is usually *where* on that
line each one sits.

So every entry gets a status **and**, for anything past `NOT STARTED`, a list of what remains. A
type reported as built with nothing after it is a claim that a player could meet it in a real run —
do not make that claim casually.

| Status | Means |
|---|---|
| `CUT` | The design removed it. Say so, cite the removal, stop. |
| `NOT STARTED` | No script, no prefab, no layout, no data asset. |
| `PARTIAL` | Some pieces exist. Name the ones that do and the ones that do not. |
| `BUILT — GAPS` | Runs end to end in `TestScene`, with specific work outstanding. |
| `DONE` | Meets the design's definition with nothing outstanding anywhere. |

`DONE` is currently expected to be reportable for nothing. If you are about to report it, check the
three global gates below first — they disqualify almost everything.

## Evidence sweep, in this order

**1. Get the room-type list from the design, not from this file.** The canonical list is one line:
`Design/02-CORE_SYSTEMS.md` §8 ("Room types: …"). Cross-check it against `Design/06-LEVEL_DESIGN.md`
§2 (the per-biome pool table, with counts) and §6 (Mini-Boss and Final Boss arenas). Note that
`Design/03-CONTENT_DESIGN.md` §6 is a known-stale third copy — see *Known doc conflicts* below.

**2. Code** — glob `Assets/_Main/Scripts/Rooms/**`. Which room types have a class at all.

**3. Prefabs** — glob `Assets/_Main/Prefabs/Rooms/**`. A room type with a script but no prefab is
`PARTIAL`, not built: the design's unit of content is a *layout*, not a component.

**4. Layouts** — glob `Assets/_Main/Scripts/Editor/*Layout*.cs`. A room's map is committed as data
(`RoomLayout.cs`), because a layout assembled by dragging is one nobody can reproduce, review or
diff. A prefab whose layout exists only inside the prefab is a finding worth reporting.

**5. Content data** — glob `Assets/_Main/Data/**` for anything the room type needs as authored data
(waves are serialized on `WaveSpawner`; soul types, key drops and boss phases would be `Data/`).

**6. Engineering plan** — `Docs/Engineering/00-IMPLEMENTATION_PLAN.md`. Grep the type's name. For
anything already built this is the **primary source of what is left**: each built pass carries a
`### Outstanding` list written by whoever built it. Read that list; do not re-derive it.

**7. Change brief** — `Docs/00-DESIGN_CHANGE_BRIEF.md`. Grep the type's name for divergences,
invented numbers and conflicts affecting it. These belong in the report — a room built to numbers
nobody approved is a real caveat on its status.

**8. Scene mounting** — grep `Assets/_Main/Scenes/TestScene.unity` for the prefab's GUID if you need
to know whether a built room is actually reachable in the sandbox.

## Three gates that disqualify a "done"

Check these against every type you are about to call built. They are the reasons the honest answer
is usually `BUILT — GAPS`.

- **Layout count.** `LEVEL_DESIGN` §2 gives a per-biome count (6 Combat Rooms, 1 Secret Vault, 1
  Trapped Soul, 1 Mini-Boss). One working prefab out of six is `BUILT — GAPS`, and the report should
  carry the fraction. `LEVEL_DESIGN`'s own open items call this the largest content line item in the
  project.
- **Nothing sequences rooms.** Room loading, `RoomManager` and the reshuffling-bag draw of 3–5 rooms
  per floor are unbuilt; `CombatRoom.Cleared` is the hook written for them. Until that exists, *no*
  room type is reachable outside `TestScene`, which caps every type's status. Confirm it is still
  true rather than repeating it.
- **The design hole.** Some types are marked unresolved in the design itself — CORE_SYSTEMS §8 flags
  that Secret Floors lost their risk half when the Rising Hazard was cut, and §14 flags the same for
  the Trapped Soul interactable. A room whose *design* has an open question cannot be `DONE` however
  complete the code is. Report the question with the status.

Also worth surfacing when relevant: **there is still no player death / run-end**, which is sharpest
inside a room type whose whole mechanic is locking the player in.

## Known doc conflicts to report, not resolve

- `03-CONTENT_DESIGN.md` §6 still lists **2 Reward Rooms per biome** after CORE_SYSTEMS §8 and
  LEVEL_DESIGN §2 removed the type. Already recorded in the change brief — cite it, do not re-record
  it, and never report Reward Room as an unimplemented type.
- Check whether `Engineering/00-IMPLEMENTATION_PLAN.md`'s Milestone 3 deliverables still list Reward
  Rooms while its implementation-order line says they are cut. That one is an *engineering* doc, so
  it is fixable in place — offer, don't assume.

**Never edit `Assets/_Main/Docs/Design/*`.** Those belong to the designer. A conflict found during
an audit goes in the report, and into the change brief only if the user asks.

## Report format

Lead with the table, then detail only the types that are not finished. Keep the citations — the
value of this audit is that the user can check it.

```
| Room type | Status | Left to do |
|---|---|---|
| Combat Room | BUILT — GAPS | 1 of 6 layouts; no cracked tiles; no breakable wall |
| …           | NOT STARTED  | … |
```

Then, per unfinished type, a short block: what exists, what the design asks for (with the section
number), what is missing, and any open design question blocking it. Close with the shared blockers
that cap everything — floor sequencing, player death — so they are stated once rather than repeated
in every row.

## Never

- Never report a status you did not just verify against a file.
- Never call something built without stating what is outstanding on it.
- Never treat `CLAUDE.md` or an earlier answer in the conversation as evidence. Both lag the repo.
- Never fix anything during an audit. Report it; `implement-room-type` builds it.
- Never invent a room type the design does not list, and never silently resolve a conflict between
  two design docs (Design Rules 9, 11, 12).
