# NARRATIVE — "Deeper"

**Status: OWNER-DIRECTED, NOT YET LOCKED.** Recorded here as dictated by the project owner. The
sections marked *Proposed* are suggestions answering the owner's open question about post-first-run
content and are **not** approved design. Everything in this file postdates 01-GDD.md and contradicts
parts of it — see §7 before treating any of it as settled.

> ✅ **§1's Floor 16 decision stands.** The session changelog briefly moved the father to Biome 1 as a
> Mini-Boss and kept Zyno out of the MVP; the owner reversed that on 2026-08-15 — **the father is the
> Final Boss, fought before Zyno**, exactly as §1 says. The other docs were updated back to match.
> The changelog's remaining additions do apply to this file's world: the **Whisper Layer** now carries
> Zyno through the whole descent, **Memory Fragments** bank into a Hub Codex, and the **Refusal State**
> gives the player a chance to hold back during the father fight. See
> `Docs/00-DESIGN_CHANGE_BRIEF.md` §11.

---

## 1. Premise

The player character is a **woman**. She is not acting of her own will: a villain named **Zyno** has
manipulated her.

Her country took in **two children** in order to get them safely out. Under Zyno's manipulation, she
believes her purpose is to **capture those two children**. The manipulation makes her see everyone
around her as an enemy, so descending after the children means killing everything in her path.

**DECIDED: Floor 16 is two fights.** She first fights **The Depth Warden — who is her father** —
using his existing boss design (multi-phase, all 3 biome hazard themes, weapon-check moment).
Defeating him doesn't end the run: **Zyno is the true Final Boss**, fought immediately after, on the
same floor. This resolves what was previously an open conflict between this file and
`03-CONTENT_DESIGN.md` (which named "The Depth Warden" without connecting him to the father) — see
`03-CONTENT_DESIGN.md`'s Floor 16 entry and its scope flag; Zyno's fight is new content with no
moveset/stats/art yet, and needs an MVP-vs-post-MVP call before it's built.

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

⚠️ **§4b and §4c below are superseded by §1's decision** that Zyno is fought as the true Final Boss
*within run 1*, immediately after the Depth Warden/father. Zyno no longer "escapes run 1," and The
Depth Warden doesn't need to *become* Zyno later — they're both already on Floor 16. §4a and §4d are
unaffected and remain open proposals. Whether Zyno is defeated, escapes, or something else happens
at the end of that fight is now the actual open question for "why descend again" — not yet answered
anywhere.

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

### 4b. ~~Zyno is the unfinished thread, escapes run 1~~ — superseded, see note above.

### 4c. ~~The Depth Warden becomes Zyno~~ — superseded, see note above. Both are now fought in run 1,
back to back, on Floor 16.

### 4d. Escalating difficulty, skinned as Zyno tightening his grip.

The genre-standard replay driver (Hades' Pact of Punishment, Dead Cells' Boss Cells) is optional
escalating difficulty. Here it has a ready-made fiction: each tier is **Zyno digging further into her
head**. A mechanical system the game needs anyway, wearing the story's clothes. ⚠️ Needs a fictional
tweak now that Zyno is fought (not just implied) in run 1 — this framing works better if he survives
that fight in some form rather than being cleanly defeated.

### Deliberately not proposed

A spare-or-kill morality choice on the recontextualised enemies is the obvious next idea and is
**not** recommended for MVP: it doubles encounter authoring, needs branching endings, and the MVP
list in 08-MVP.md is already the identified schedule risk.

---

## 5. Cast

| Name | Role | Notes |
|---|---|---|
| *(unnamed)* | Player character | Woman. Needs a name — referenced throughout as "she". |
| **Zyno** | Villain, **True Final Boss** | Manipulator. Fought in run 1, immediately after her father, on Floor 16 — new boss content, not yet built (see `03-CONTENT_DESIGN.md`'s scope flag). |
| Her father | The Depth Warden — first Floor 16 fight | Relationship unknown to the player until the run resolves. Uses the existing Depth Warden boss design. |
| The two children | Objective | Believed targets to capture; actually being evacuated. Safe after run 1. |

**Open:** the protagonist has no name, the country has no name, Zyno's motive for the manipulation is
unstated, and **what happens to Zyno at the end of that Floor 16 fight** (defeated? escapes? something
else?) is now the open thread for "why descend again" — see §4's note.

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

Per Design Rules 11/12/14, these are flagged rather than silently resolved.

1. **The premise contradicts the GDD pitch — RESOLVED.** 01-GDD.md §Pitch has been rewritten to match
   this file's premise (woman, Zyno's manipulation, her father) instead of "a lone miner."
2. **The game had no narrative layer at all — PARTIALLY RESOLVED.** 02-CORE_SYSTEMS.md §13 now
   describes the system shape (HasSeenTheTruth flag, dialogue gating rule). Still absent from
   08-MVP.md's MUST SHIP / SHOULD SHIP / CUT lists — see that doc's open flag.
3. **Final Boss identity — RESOLVED.** 03-CONTENT_DESIGN.md now names The Depth Warden as her father,
   fought first on Floor 16, followed immediately by Zyno as the true Final Boss (new content, not yet
   built — see that doc's scope flag).
4. **Scope — PARTIALLY RESOLVED.** The session changelog gives the narrative layer an MVP subset
   (minimal Whisper Layer, Memory Fragment pickup + Codex stub, Refusal State on the father fight
   only) and puts everything else in 08-MVP.md's Explicitly Post-MVP list. Recontextualised enemy
   sets and any second final encounter remain out.
5. **Floor 16 — RESOLVED, and this file was right.** The session changelog moved the father to Biome 1
   and cut Zyno from the MVP; the owner reversed both on 2026-08-15. The structure is **the father
   (as The Depth Warden) first, Zyno immediately after**, and every other doc has been put back to
   match. Items 3 and 4 above stand as written.

Resolving these properly is a Rule 14 reopen: **01-GDD.md, 02-CORE_SYSTEMS.md, 03-CONTENT_DESIGN.md,
05-ART_DIRECTION.md and 08-MVP.md need updating together**, not one at a time.
