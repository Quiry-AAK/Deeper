# NARRATIVE — "Deeper"

**Status: OWNER-DIRECTED, NOT YET LOCKED.** Recorded here as dictated by the project owner. The
sections marked *Proposed* are suggestions answering the owner's open question about post-first-run
content and are **not** approved design. Everything in this file postdates 01-GDD.md and contradicts
parts of it — see §7 before treating any of it as settled.

---

## 1. Premise

The player character is a **woman**. She is not acting of her own will: a villain named **Zyno** has
manipulated her.

Her country took in **two children** in order to get them safely out. Under Zyno's manipulation, she
believes her purpose is to **capture those two children**. The manipulation makes her see everyone
around her as an enemy, so descending after the children means killing everything in her path.

The **Final Boss of the first run is her father.**

---

## 2. The first run

The player begins knowing none of this. The first descent plays as a straight roguelike run with no
narrative framing — the story is carried by **easter eggs** the player is not expected to understand
yet.

At the end of the first run the truth lands: the manipulation is exposed and **the two children are
transported safely.** The story, as originally conceived, completes here.

**This is the design problem the owner raised:** the narrative resolves in one run, but the genre
depends on many. §4 proposes an answer.

---

## 3. Post-first-run dialogue (owner-requested)

Unique lines fire on later runs where the first run had none. The owner's example: on reaching the
Final Boss, having learned who he is, she says something to the effect of *"Father… I'm sorry."*

**Gate lines on a knowledge flag, not a run counter.** What she says depends on what she *knows*, not
how many times the player has pressed Descend. A player can die on floor 3 of run 4 without ever
having reached the father; a run-count gate would have her apologise to a man she has not yet
recognised. A single `HasSeenTheTruth` flag, set when the first run resolves, is the correct
condition — and it stays correct however the player reaches it.

---

## 4. *Proposed* — what the game is after the story ends

The owner's question: once the children are safe, why descend again? Four pieces, cheapest first.

### 4a. Run 1 is the lie. Runs 2+ are the same descent, seen true.

The manipulation made her see everyone as an enemy. Once it breaks, the enemies are revealed as what
they always were — her own countrymen, the children's escorts, her father's guards. **Same rooms,
same fights, different identities.**

This is the strongest option for this project specifically because it is *nearly free in art*.
A recontextualised enemy is a palette swap, a new name and new dialogue — not a new sprite set.
ART_DIRECTION §4 already establishes exactly this technique for Elites ("palette-swap + 1 additional
aura VFX layer only — no new frames"), so the pipeline exists. Given generation budget is the
binding constraint on this project, narrative weight that costs recolours rather than new art is
worth far more than its price.

It also pays off §2's easter eggs: what was uninterpretable on run 1 becomes legible on run 2.

### 4b. Zyno is the unfinished thread.

Nothing in the owner's premise says Zyno is defeated — only that the father is fought and the
children are saved. **Zyno escapes run 1.** Runs 2+ change objective from *capture the children* to
*reach Zyno*, which gives the loop a goal without inventing a new premise.

### 4c. The Depth Warden becomes Zyno.

CONTENT_DESIGN §Enemies already budgets a multi-phase Final Boss, **The Depth Warden**, separate from
the three biome Mini-Bosses. Assigning that existing slot to Zyno's true form gives the post-story
game a real final encounter **without adding an unplanned boss**:

| | Final encounter |
|---|---|
| Run 1 | Her father |
| Runs 2+ (after `HasSeenTheTruth`) | The Depth Warden — Zyno |

### 4d. Escalating difficulty, skinned as Zyno tightening his grip.

The genre-standard replay driver (Hades' Pact of Punishment, Dead Cells' Boss Cells) is optional
escalating difficulty. Here it has a ready-made fiction: each tier is **Zyno digging further into her
head**. A mechanical system the game needs anyway, wearing the story's clothes.

### Deliberately not proposed

A spare-or-kill morality choice on the recontextualised enemies is the obvious next idea and is
**not** recommended for MVP: it doubles encounter authoring, needs branching endings, and the MVP
list in 08-MVP.md is already the identified schedule risk.

---

## 5. Cast

| Name | Role | Notes |
|---|---|---|
| *(unnamed)* | Player character | Woman. Needs a name — referenced throughout as "she". |
| **Zyno** | Villain | Manipulator. Never fought in run 1. Proposed as the post-story Final Boss (§4c). |
| Her father | Run 1 Final Boss | Relationship unknown to the player until the run resolves. |
| The two children | Objective | Believed targets to capture; actually being evacuated. Safe after run 1. |

**Open:** the protagonist has no name, the country has no name, and Zyno's motive for the
manipulation is unstated. All three are needed before dialogue can be written.

---

## 6. Art implications

- The protagonist is a **fixed single character** — no body/gender selection. She is a woman with a
  cape and light armour (see the engineering plan's Real Character Art section).
- Narrative requires presentation the art budget has never accounted for: **dialogue UI**, and
  probably **character portraits** for her, Zyno and the father. ART_DIRECTION §5 covers HUD, upgrade
  and Hub screens only. This is new scope.
- Recontextualised enemies (§4a) cost recolours, not new sprites — this is the cheap part.

---

## 7. Conflicts with locked design

Per Design Rules 11/12/14, these are flagged rather than silently resolved. **01-GDD.md has not been
amended.**

1. **The premise contradicts the GDD pitch.** 01-GDD.md §Pitch reads *"A lone miner dives through a
   collapsing shaft, growing stronger with every reckless floor, racing the rising danger below."*
   The protagonist is now a specific named woman with a backstory, a manipulator and a father —
   not a lone miner. The entire framing of the game changes.
2. **The game had no narrative layer at all.** No design doc describes story, dialogue, cutscenes or
   characters. This adds a system class that does not exist in 02-CORE_SYSTEMS.md and is absent from
   08-MVP.md's MUST SHIP / SHOULD SHIP / CUT lists.
3. **Final Boss identity changes.** 03-CONTENT_DESIGN.md names The Depth Warden as the Final Boss,
   "multi-phase; incorporates all 3 biome hazard types". Run 1's final boss is now her father.
4. **Scope.** Dialogue, story state, recontextualised enemy sets and a second final encounter are
   none of them in the MVP. 08-MVP.md already identifies content authoring as the schedule risk.

Resolving these properly is a Rule 14 reopen: **01-GDD.md, 02-CORE_SYSTEMS.md, 03-CONTENT_DESIGN.md,
05-ART_DIRECTION.md and 08-MVP.md need updating together**, not one at a time.
