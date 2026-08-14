# DESIGN CHANGE BRIEF — for the designer

**Purpose:** everything decided outside the design docs that now contradicts them, so the design docs
can be brought back into line. Nothing in `Design/01-GDD.md` … `09-DESIGN_RULES.md` has been edited —
per Design Rules 11/12 these were flagged, not silently written in.

Each item is tagged:

- **DECIDED** — the project owner has ruled. The design docs are simply out of date and should follow.
- **PROPOSED** — suggested by engineering, **not approved**. Needs a decision before it is written anywhere.
- **CONFLICT** — two locked statements now disagree. Needs resolving, not just recording.

---

## 1. DECIDED — the inventory and armor system is deleted

The player no longer carries, equips or swaps anything. A run is **one weapon, chosen in the Hub
before descending, locked until the run ends.**

This *resolves* a conflict rather than creating one: GDD §Player already said the weapon is "locked
for the full run" and CORE_SYSTEMS §1 already said "no runtime weapon-swapping". The code that
disagreed has been removed.

**Armor no longer exists as a mechanic.** Helmets and armor return **after launch as cosmetics only**
— no stats, no slots that affect balance, no mid-run swapping.

**Docs to update:** anywhere implying wearable gear or an inventory screen. Confirm that BALANCE's
mitigation story is still exclusively Dig-Dash i-frames, Hyper Armor, Iron Skin and Second Skin.

## 2. DECIDED — no body or gender selection

Briefly considered, then dropped. There is exactly one player character.

## 3. CONFLICT — the protagonist contradicts the GDD pitch

GDD §Pitch reads: *"A lone miner dives through a collapsing shaft…"*

The protagonist is now **a specific woman** — cloaked, in light armour, with a name still to be
chosen, a father, and a villain manipulating her. She is not a miner and the story is not about
mining for ore.

This is the single largest documentation change. The pitch, the framing, and every place the player
is described as "the miner" need rewriting. **Ore, Ore Shards and the Hub economy still use mining
vocabulary** — decide whether that vocabulary survives the new premise or is renamed.

## 4. DECIDED — there is now a story, recorded in `Design/10-NARRATIVE.md`

Read that file; it is the source. In brief: she is manipulated by a villain, **Zyno**, into believing
she must capture two children her country is evacuating. The manipulation makes her see everyone as
an enemy. **The Final Boss of the first run is her father.** The first run resolves the story and the
children get out safely.

`10-NARRATIVE.md` is written as **owner-directed and not locked**, and lists its own conflicts.

**This adds a whole system class that exists in no design doc:** dialogue, story state, and
first-time-versus-later-run variation. `02-CORE_SYSTEMS.md` has no narrative section, and
`08-MVP.md` does not mention story in MUST SHIP, SHOULD SHIP or CUT IF NECESSARY. It needs a tier.

## 5. CONFLICT — Final Boss identity

`03-CONTENT_DESIGN.md` names **The Depth Warden** as the Final Boss, "multi-phase; incorporates all
3 biome hazard types in sequence".

Run 1's Final Boss is now **her father**. Either the Depth Warden *is* her father, or the Depth
Warden moves to a different role. See §6 for the proposal.

## 6. PROPOSED — what the game is once the story ends (NOT APPROVED)

The owner raised the real problem: the story completes in one run, but the genre needs many. The
proposal, in full, is in `10-NARRATIVE.md` §4. Summary:

- **Run 1 is the lie; later runs are the same descent seen true.** The manipulation made her see
  everyone as an enemy; afterwards the same enemies are revealed as her countrymen and the children's
  escorts. Same rooms, same fights, new identities. Chosen because it costs **recolours and dialogue,
  not new sprites** — `ART_DIRECTION.md` §4 already uses exactly this technique for Elites.
- **Zyno escapes run 1**, giving later runs a goal.
- **The Depth Warden becomes Zyno**, using a boss already budgeted rather than adding one.
- **Optional escalating difficulty**, skinned as Zyno tightening his grip.

Explicitly **not** proposed: a spare-or-kill morality choice. It doubles encounter authoring and
needs branching endings, and MVP content authoring is already the identified schedule risk.

## 7. PROPOSED — dialogue must be gated on knowledge, not run count

The owner wants unique later-run lines (his example: on reaching the Final Boss, *"Father… I'm
sorry."*).

Gate these on a **flag meaning "she knows the truth"**, not on "runs completed ≥ 1". A player can die
on floor 3 of run 4 without ever having reached the father — a run-count gate would have her
apologise to a man she has not yet recognised. This is a design rule, not an implementation detail,
because it decides how every future line is written.

## 7b. DECIDED — Basic Attack becomes a 3-hit chain (new design)

**This does not exist in any design doc.** The owner directed it; it is built and needs writing into GDD §Player and CORE_SYSTEMS §1.

What the docs currently give the Katana is the **Combo Counter** (BALANCE §3): +2% damage per landed hit, cap 10, resetting instantly on a miss or on taking damage. That is a damage *stack*, not a multi-hit animation sequence — a different feature, and it still exists alongside the new chain.

The chain reuses the mechanic CORE_SYSTEMS §3 already defines for Heavy Strike: each hit re-enters Windup→Active→Recovery, and the chain breaks unless the player presses again inside a short window. Per ART_DIRECTION §46, chain hits **replay the base animation** rather than needing unique art. If each Basic hit should look different, that is a separate art-budget decision worth taking deliberately.

**Questions for design:**
- Should each chain hit scale damage (a 3rd-hit finisher), or all hit for BALANCE §2's flat 8?
- Does the chain interact with the Combo Counter beyond each hit adding a stack?
- Heavy Strike's chain is upgrade-gated (Twin Cut → Triple Cut). Should Basic's 3 hits be free from the start, as currently built, or also unlocked?

## 7c. Ultimate Gauge and Combo Counter are implemented

Both follow the documented numbers: gauge 0–100 filling +8% per Basic and +15% per Heavy for the Katana (BALANCE §4), Ultimate purely resource-gated and consuming the whole gauge (CORE_SYSTEMS §4), Combo Counter +2%/stack to a cap of 10.

**BALANCE gives no Windup/Active/Recovery row for Ultimates.** The three phase timings for Combo Finisher are placeholders and need a design answer.

## 7d. DECIDED — movement now has acceleration and deceleration

`01-GDD.md` §Movement reads *"8-directional top-down movement, fixed speed, no acceleration curve (keeps controls crisp and easy to tune)."*

Movement now ramps: **0.055s** to reach full speed, **0.085s** to coast to a stop. The asymmetry is deliberate — stopping slower than starting is what reads as weight rather than float.

The intent behind the original rule is preserved. It was protecting *crispness*, and 55ms is about three frames at 60fps — imperceptible as lag, but enough to remove the robotic on/off quality of instant velocity. If it ever does feel sluggish, both numbers are serialized and tune to zero, which restores the documented behaviour exactly.

Update GDD §Movement to describe ramped movement with the times as tunable values.

## 7e. Working rule — the owner's instruction overrides the docs

Recorded because it changes how future conflicts get handled: when the project owner asks for something that contradicts a design doc, **the change gets built**, and the divergence is written into this brief for the designer to reconcile. A locked value in a doc is not a blocker; it is a record of what was true when written.

This does not remove the value of flagging the conflict in passing — knowing which documents fall out of date is exactly what this brief is for.

## 7f. Owner-directed — the Ultimate is a BUFF, not a damage move

**Built. This contradicts two locked documents and needs a designer decision.**

The owner's direction: the Ultimate is a short cast — she raises the katana — that brings up an
**aura on her and a much stronger aura on the katana**, lasting a few seconds. While it is up she is
stronger and **every attack trails that aura**. It deals no damage of its own.

| | Locked docs say | Built |
|---|---|---|
| `02-CORE_SYSTEMS.md` §4 | Ultimate is an attack; Alt Ultimates are a swappable strategy on `IWeapon` | Ultimate is a self-buff; no damage |
| `04-BALANCE.md` §2 | Katana Ultimate deals **40 damage** | No damage row applies |
| Gauge behaviour | Resource-gated, drains fully on use | **Unchanged** — still resource-gated, still drains fully |

What this leaves open for the designer:

- **BALANCE has no numbers for a buff.** Duration (8s), damage (+50%), **attack speed (+40%)** and
  move speed (+15%) are **placeholders picked to be playable**, not designed values. They need a
  balance pass. Attack speed shortens all three attack phases together, so a buffed Basic runs
  0.36s → 0.26s and a Heavy 0.77s → 0.55s.
- **`StatType.AttackSpeed` is new and is not in the Hub Core Stats table.** It was added for this
  buff and appended (value 8) so existing serialised modifiers keep their meaning. If Hub Core Stats
  should be able to buy attack speed, BALANCE needs a row; if not, it stays a run-only stat.
- **`IWeapon` must cover both shapes — decided, and already built into the data.** The Ultimate's
  shape is now `WeaponDefinition.UltimateShape` (`Attack` or `Buff`) with a `UltimateBuffSpec`
  payload, so `IWeapon.Ultimate()` branches on weapon data instead of assuming an attack. Katana is
  `Buff`; Bow and Greatsword stay `Attack` until design says otherwise. The interface itself does
  not exist yet (Milestone 2), but it will now inherit a model that expresses both.
- **Alt Ultimates** (CONTENT_DESIGN) are described as alternative attacks. Do they become alternative
  buffs, or can a weapon have either shape?
- The Ultimate is **no longer a damage spike**, which changes the shape of every boss fight that
  assumed burst damage was available on demand.

Implementation notes: the buff registers its modifiers through `PlayerStats.SetSource`, the same
pipeline run upgrades and Hub Core Stats will use, so it stacks with them correctly and cannot leak.
Re-casting refreshes the duration rather than stacking. The aura needs **no art** — it is an additive
copy of whatever sprite she is already drawing, which is why the near-white blade blows out far more
than her dark armour.

## 8. Art budget consequences

- **New, unbudgeted:** dialogue UI, and probably **portraits** for her, Zyno and the father.
  `ART_DIRECTION.md` §5 covers HUD, Upgrade and Hub screens only.
- **Cheaper than feared:** recontextualised enemies (§6) are palette swaps.
- **Delivered and within budget:** the player's Idle and Move are **4 frames per direction** against
  §3's ceilings of 4 and 6. Five directions are authored and the other three are mirrored, exactly as
  §3 allows.
- **Cosmetic armor** (§1) still needs an art budget line whenever it is scheduled.

## 9. Documents that need editing

| Document | What changes |
|---|---|
| `01-GDD.md` | Pitch and premise (§3); protagonist identity; confirm weapon-lock wording; drop any gear implication |
| `02-CORE_SYSTEMS.md` | Add narrative/dialogue as a system; confirm no inventory |
| `03-CONTENT_DESIGN.md` | Final Boss identity (§5); cast list; enemy recontextualisation if §6 is approved |
| `04-BALANCE.md` | Confirm no armor-derived mitigation; **replace the Katana Ultimate's 40-damage row with buff values (§7f)** |
| `02-CORE_SYSTEMS.md` §4 | **Ultimate is a buff, not an attack — and decide whether `IWeapon.Ultimate()` must cover both shapes (§7f)** |
| `05-ART_DIRECTION.md` | Protagonist description; portraits and dialogue UI; cosmetic-armor budget |
| `08-MVP.md` | Give story/dialogue a tier — it currently appears in none |
| `10-NARRATIVE.md` | Promote from owner-directed to locked once §5–§7 are decided |

Per **Design Rule 14** these are one coordinated reopen, not seven independent edits — §3 and §5 in
particular cannot be resolved in one document alone.
