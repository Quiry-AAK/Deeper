# DESIGN CHANGE BRIEF — for the designer

**Purpose:** everything decided outside the design docs that now contradicts them, so the design docs
can be brought back into line. Everything in §1–§10 below was flagged rather than written into
`Design/`, per Design Rules 11/12.

**One exception, 2026-08-14/15:** the owner directed edits straight into `Design/` — first applying the
designer's own session changelog (the designer could not reach the repository), then two rulings on top
of it. Recorded in **§11** (the changelog, and the two overrides the owner later reversed) and **§12**
(the Rising Hazard cut). Neither resolves any item in §1–§10.

Each item is tagged:

- **DECIDED** — the project owner has ruled. The design docs are simply out of date and should follow.
- **PROPOSED** — suggested by engineering, **not approved**. Needs a decision before it is written anywhere.
- **CONFLICT** — two locked statements now disagree. Needs resolving, not just recording.

**Where to start.** Most of this page is documentation catching up with decisions already made. Four
items are not, and are worth taking first because they change how the game plays rather than how it is
written down: **§7h** (the Ultimate destroys the Combo Counter and returns nothing — a live gameplay
hole), **§3/§5/§6** (the premise, the Final Boss and what the game is after the story ends — one
coordinated decision, not three), **§7g** (a flat gauge fill has deleted the per-weapon pacing
difference and left an upgrade with no job), and **§13.1–13.2** (two room-authoring rules discovered
by building the first Combat Room, which constrain the other 17 layouts and are written in no design
doc — take these before the next room is drawn, not after).

**Last refreshed** 2026-08-15, when the Dig-Dash, enemy spawn telegraphs and the first real environment
art landed (§14) — notable for how *little* had to be invented, since BALANCE §1–2 and ART_DIRECTION §3
already specified the dash almost completely. Before that, the same day, the first Combat Room (§13) and
the Rising Hazard cut (§12). Before that, 2026-08-14, when the designer's session changelog was applied
to `Design/` (§11).
The refresh before that followed the Biome 1 enemy roster and staged combat pass: §7g–§7n and §10 were
added then, and §7b and §7c were **corrected** — they previously described a 3-hit chain and a gauge
matching BALANCE §4, and both statements were wrong.

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

## 7b. DECIDED — Basic Attack becomes a chain (new design). **Built as 2 hits, not 3.**

**This does not exist in any design doc.** The owner directed it; it is built and needs writing into GDD §Player and CORE_SYSTEMS §1.

What the docs currently give the Katana is the **Combo Counter** (BALANCE §3): +2% damage per landed hit, cap 10, resetting instantly on a miss or on taking damage. That is a damage *stack*, not a multi-hit animation sequence — a different feature, and it still exists alongside the new chain.

The chain reuses the mechanic CORE_SYSTEMS §3 already defines for Heavy Strike: each hit re-enters Windup→Active→Recovery, and the chain breaks unless the player presses again inside a short window (0.25s). Per ART_DIRECTION §46, chain hits **replay the base animation** rather than needing unique art. If each Basic hit should look different, that is a separate art-budget decision worth taking deliberately.

**The third hit was cut, owner-directed.** `basicChainLength` is **2**. The chain loops, so hit 1 following hit 2 already reads as a third distinct cut in play. `CharacterState.BasicAttack3` and its fallback are still in place, so restoring a third hit is a data change plus one art sheet. Write the chain into the docs as *2 hits, looping* — not 3.

**Questions for design:**
- Should each chain hit scale damage (a finisher on the last hit), or all hit for BALANCE §2's flat 8? **Currently all hits deal the same 8.**
- Does the chain interact with the Combo Counter beyond each hit adding a stack?
- Heavy Strike's chain is upgrade-gated (Twin Cut → Triple Cut). Should Basic's chain be free from the start, as currently built, or also unlocked?

## 7c. Ultimate Gauge and Combo Counter are implemented

The Ultimate is purely resource-gated and consumes the whole gauge (CORE_SYSTEMS §4), and the Combo Counter is +2%/stack to a cap of 10, resetting on a miss and on taking damage (BALANCE §3). **The gauge's fill numbers do not match BALANCE §4 — see §7g.**

**BALANCE gives no Windup/Active/Recovery row for Ultimates.** The three phase timings for the Katana Ultimate are placeholders and need a design answer.

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

## 7g. DECIDED — the Ultimate Gauge diverges from BALANCE §4 in two ways

Both owner-directed, both built, neither written into a design doc. Both are serialized and retunable without a recompile (Design Rule 8).

| | Locked doc says | Built |
|---|---|---|
| Fill per landed hit | Katana +8% Basic / +15% Heavy; Bow +6%/+15%; Greatsword +10%/+20% (BALANCE §4) | **1% flat — every weapon, every action.** 100 landed hits to fill |
| Fill on taking damage | The **"Gauge: Vengeance"** upgrade only, +5% (CORE_SYSTEMS §4, BALANCE §10) | **+1% at base**, a flat percentage deliberately not scaled by how hard the hit was |

Two consequences worth a decision, not just a doc edit:

- **The per-weapon fill table is what made the gauge a weapon-differentiating knob.** A flat 1% deletes that difference; the Greatsword's slower, heavier rhythm no longer buys a faster Ultimate. Decide whether that difference is wanted back, or whether weapon feel now lives entirely in timing and damage.
- **If gain-on-damage is base behaviour, the "Gauge: Vengeance" upgrade no longer has a job.** It needs either a new effect or removing from the BALANCE §10 pool.

## 7h. CONFLICT — the Ultimate destroys the Combo Counter and gives nothing back

**This is the one item on this page that is a live gameplay hole, not just a stale document.**

CORE_SYSTEMS §4 defines the Katana Ultimate as *"a rapid multi-hit burst that **consumes and converts** the current Combo Counter stack into bonus damage"*, and BALANCE §4 prices it at *"40 damage + **5 per Combo Counter stack consumed**"*.

The Ultimate is now a buff (§7f) and deals no damage — so there is nothing for the stacks to convert *into*. The built code still consumes them and **discards the result**. What the player experiences:

- Casting at a full 10-stack combo silently throws away **−20% damage**…
- …at the exact moment an **+50% damage** buff starts, so the two owner-directed changes fight each other.
- Because the combo also resets on any hit taken, the optimal play is to cast the Ultimate at *zero* stacks — the opposite of what a "Combo Finisher" is supposed to reward.

**Three ways out; this needs a decision, not a guess:**
1. **Stop consuming.** The buff simply does not touch the combo. Cheapest, and the stacks then ride *into* the buff, which compounds well.
2. **Convert stacks into buff strength** — e.g. duration or damage scaling with stacks spent. Keeps the "finisher" fantasy with a buff's shape, and gives BALANCE a row it can actually tune.
3. **Keep consuming for nothing** and rewrite CORE_SYSTEMS §4 to drop the conversion language entirely.

## 7i. The enemy sprite-sheet contract is new and is in no design doc

`ART_DIRECTION` §4 gives enemies a *frame* budget but defines **no row order and no direction count** — §3's "8-directional, mirrored for 4 base directions" rule sits under **Player** Animation Budget. The enemy pass had to invent one, and it is recorded in the engineering plan rather than in the locked doc.

**Enemies author three directions (Down, Up, Side), not the player's five.** Owner-directed: basics are 4-directional, mirrored to cover all eight facings, and the full 5-row set is reserved for bosses. The diagonals are the rows that earn the least per cell, and 2-directional art cannot show whether an enemy is *facing you* — which in a game where every attack is telegraphed is the information the fight is built on.

Sheet is 128×576: 4 columns × 12 rows of 32×48. Rows 0–2 Idle/Move, 3–5 Telegraph, 6–8 Attack, 9–11 Death.

**This needs folding into ART_DIRECTION §4 as an explicit Enemy Animation contract**, so that Biome 2 and 3's rosters (eight more enemies) are authored against a written rule rather than against this codebase.

## 7j. How each enemy delivers its damage is an engineering interpretation

BALANCE §5 gives every enemy **one Damage number** and never says how it reaches the player. The choices below were made so the three basics differ in *function*, not only in numbers (Design Rule 4) — they are reasonable, but they are design, and design did not make them:

- **Cave Crawler** — touching it hurts (contact damage), plus a lunge that closes the gap. It is the pressure enemy.
- **Rock Slinger** — no contact damage at all. All 6 is the thrown rock, so it is safe to body and punishes standing still at range instead of punishing proximity.
- **Tunnel Brute / Deep Warden** — no contact damage. All 15/18 is the slam, so there is a safe window beside them between slams — the whiff-punish space `LEVEL_DESIGN` §2 asks Combat Rooms to preserve for the Greatsword. Giving the Brute both contact *and* slam would double-dip and delete that space.

Confirm or overrule these, and write the chosen rule into CONTENT_DESIGN's enemy table so the remaining eight enemies are authored against it.

## 7k. Roughly thirty enemy behaviour numbers have no design source

No design doc specifies enemy telegraph length, attack cadence, aggro radius, engagement range or kiting distance. All were invented, all are serialized with tooltips saying so, and all need a balance pass:

| | Crawler | Slinger | Brute | Warden |
|---|---|---|---|---|
| Windup (telegraph) | 0.35 | 0.50 | 0.75 | 0.70 |
| Active | 0.18 | 0.06 | 0.12 | 0.12 |
| Recovery | 0.45 | 0.60 | 0.90 | 0.85 |
| Cooldown | 1.20 | 2.20 | 2.50 | 2.20 |
| Aggro radius | 10 | 12 | 12 | 14 |
| Attack range | 1.6 | 7.0 | 2.0 | 2.2 |
| Stop distance | 0.9 | 5.5 | 1.2 | 1.2 |
| Retreat distance | 0 | 3.5 | 0 | 0 |

Plus per-move geometry: lunge distance 1.8, slam radius 2.2 / knockback speed 12 / knockback time 0.30, rock speed 4.5 / lifetime 4.0.

Only two of these are anchored to anything: **ART_DIRECTION §4 caps Telegraph at 3 frames**, which at the animator's 8fps is 0.375s — that is where the Crawler's 0.35 came from; and the player moves at 5.0, so the rock at 4.5 is outrunnable by design. **BALANCE needs an enemy-timing table**, the same way it has one for weapons — it is the difference between "is this dodgeable" being a tuning question and being a guess.

## 7l. The Deep Warden is missing half of what ART_DIRECTION specifies for an Elite

ART_DIRECTION §4 defines an Elite variant as *"palette-swap + 1 additional 'aura' VFX layer only — no new frames."* The palette swap ships (the Warden is the Brute in violet); **the aura layer does not.**

This is not just unbuilt art — `AuraVisuals` resolves `UltimateBuff` and `AttackStateMachine`, so it is coupled to the player and cannot be pointed at an enemy without being rewritten. Worth knowing before Biomes 2 and 3 add two more Elites (Tideheart, Cinder Warden) that need the same layer.

## 7m. Attacks lunge; they do not root the player

Previously flagged as an inference and now settled in code. BALANCE §2 singles out the alt Ultimate "Thousand Cuts" as *"player-mobile"*, which only distinguishes it if attacks are normally rooted — that reading drove an earlier root-in-place behaviour, and it was **the main reason attacks felt weightless**.

Attacks now drive movement instead: 0.75 / 1.15 / 0.9 world units on an ease-out curve, direction locked at the start of the hit. The player cannot steer mid-swing, but she is not stationary.

**Decide what "player-mobile" was meant to distinguish**, since it no longer distinguishes the alt Ultimate from a base attack.

## 7n. Two inconsistencies *inside* the locked docs, found while cross-checking

Neither was caused by implementation — they are pre-existing and worth fixing in the same pass:

- **GDD §UI lists a "Heavy Strike cooldown icon" in the HUD, but no Heavy Strike cooldown exists anywhere.** BALANCE §2 gives Heavy Strike Windup/Active/Recovery and no cooldown row, and CORE_SYSTEMS §3 describes no gate on it. Either Heavy Strike gains a cooldown (a real balance change — it is currently limited only by its 0.77s of animation) or that HUD element comes out.
- **CORE_SYSTEMS §6's single `OnDamageDealt(source, target, amount)` event does not exist as specified.** The damage pipeline is built as `AttackHitbox.Landed(action, target, amount)` plus `Damageable.Damaged(amount)`, and **neither carries the `source`**. It works today because the only source is the player. Milestone 4's on-hit upgrade procs and any "damage dealt by X" upgrade will need it, so this is worth settling before that pool is authored rather than after.

## 8. Art budget consequences

- **New, unbudgeted:** dialogue UI, and probably **portraits** for her, Zyno and the father.
  `ART_DIRECTION.md` §5 covers HUD, Upgrade and Hub screens only.
- **Cheaper than feared:** recontextualised enemies (§6) are palette swaps.
- **Delivered and within budget:** the player's Idle and Move are **4 frames per direction** against
  §3's ceilings of 4 and 6. Five directions are authored and the other three are mirrored, exactly as
  §3 allows. Enemy frame counts also sit inside §4's budget — but see §7i, the *row and direction*
  contract they use is invented.
- **Cosmetic armor** (§1) still needs an art budget line whenever it is scheduled.
- **The Elite aura layer §4 requires has not been made** — see §7l.

### Two style conflicts in shipped art

- **The Ultimate's cyan slash arcs are baked into the character frames.** `ART_DIRECTION` §2 reserves
  cyan-white as a **hazard accent**, so a player power reading in hazard colours is a style conflict.
  It also means the arcs are not separable as VFX without regenerating the frames.
- **A measured defect was re-opened, deliberately and with the owner's agreement.** The separate
  `SlashVFX` arc layer was **deleted** because the attack frames now draw their own arc and two arcs
  appeared on every swing. But the reason the arc was pulled *out* of the character frames originally
  was a measurement: the baked arcs had **197 bright pixels facing side against 14 facing up**,
  because she turns away and a dark arc vanishes against her pale cloak. That measurement predates
  the current art. **Re-measure the shipped sheets before treating this as settled** — and if it still
  holds, fix it in the art, not by re-adding a second arc layer.

## 9. Documents that need editing

| Document | What changes |
|---|---|
| `01-GDD.md` | Pitch and premise (§3); protagonist identity; confirm weapon-lock wording; drop any gear implication; **ramped movement (§7d)**; **Basic Attack chain (§7b)**; **drop or justify the Heavy Strike cooldown icon (§7n)** |
| `02-CORE_SYSTEMS.md` | Add narrative/dialogue as a system; confirm no inventory; **Basic Attack chain (§7b)**; **`OnDamageDealt` needs a `source` (§7n)** |
| `02-CORE_SYSTEMS.md` §4 | **Ultimate is a buff, not an attack — and decide whether `IWeapon.Ultimate()` must cover both shapes (§7f)**; **the Combo Counter conversion no longer has anything to convert into (§7h)**; **gain-on-damage is now base behaviour (§7g)** |
| `03-CONTENT_DESIGN.md` | Final Boss identity (§5); cast list; enemy recontextualisation if §6 is approved; **how each enemy delivers its damage (§7j)**; **"Gauge: Vengeance" needs a new job (§7g)** |
| `04-BALANCE.md` | Confirm no armor-derived mitigation; **replace the Katana Ultimate's 40-damage row with buff values (§7f)**; **gauge fill is 1% flat, not the per-weapon table (§7g)**; **add an enemy-timing table (§7k)**; **decide what "player-mobile" means now (§7m)** |
| `05-ART_DIRECTION.md` | Protagonist description; portraits and dialogue UI; cosmetic-armor budget; **write the enemy row/direction contract into §4 (§7i)**; **the Elite aura layer (§7l)**; **cyan arcs vs the hazard-accent reservation (§8)** |
| `08-MVP.md` | Give story/dialogue a tier — it currently appears in none |
| `10-NARRATIVE.md` | Promote from owner-directed to locked once §5–§7 are decided |

Per **Design Rule 14** these are one coordinated reopen, not eight independent edits — §3 and §5 in
particular cannot be resolved in one document alone.

**None of the rows above were touched by the 2026-08-14 pass (§11).** That pass applied the
designer's own changelog; every row here is still outstanding, and `03-CONTENT_DESIGN.md`'s row grew
— see §11's "left stale" list.

---

## 10. Locked design that is still unbuilt — context for the conversation

Not divergences, and not a to-do list for the designer. Listed because several open questions above
have no real answer until these exist, and because adding new mechanics on top of them changes what
they cost.

- **Player death / run-end.** `Damageable.Died` fires on the player with **no subscriber** — she stops
  taking damage and sits at 0 HP. There is no death screen, no run-end and no respawn. Four enemies
  that can genuinely kill her now exist, and `10-NARRATIVE.md`'s "the first run resolves the story"
  has nowhere to resolve *to*.
- **Dig-Dash and the Dash-Attack Cancel.** Both locked (GDD §Player, BALANCE §1–2, CORE_SYSTEMS §2)
  and neither built. `AttackStateMachine.CanCancel` already exposes the cancel window and nothing
  calls it. This matters to any new mechanic discussed: **Dig-Dash i-frames are currently the entire
  mitigation story** (BALANCE), so until it exists the player has no defensive option at all.
- **No HP bar, no floor indicator, no Ore counter, no hazard meter.** Of the eight HUD elements GDD
  §UI lists, only the Ultimate Gauge exists.
- **Nothing above Milestone 1 exists** — no rooms, no floors, no Hazard Front, no upgrades or curses,
  no Hub, no meta-progression, no mini-boss or final boss. Everything runs in `TestScene`.

---

## 11. APPLIED — the designer's session changelog was written into `Design/` (2026-08-14)

**Owner-directed, one-off.** The designer could not reach the repository, so their session changelog
was applied to the locked docs from this side instead of being flagged here. This is the only pass in
which `Design/` was edited from engineering. It resolves none of §1–§10 — in particular **§7h is still
the live gameplay hole.**

**Applied on top of the designer's own three pushed passes** (`fa89f2f`, `d46c84b`, `881a589`), which
landed while this was being written. Those passes stay intact except where the changelog contradicts
them; the owner's instruction was that **the changelog wins every conflict.** The three collisions are
listed under "Where the changelog overrode a pushed decision" below.

### What was applied

| Document | Change |
|---|---|
| `01-GDD.md` | New pitch; session length 15–25 → **30–60 min**; new **§Narrative Premise** (Zyno, the village, father as Biome 1 Boss, art unaffected) with the Floor 16 conflict flagged in place; Core Loop rewritten around XP and level-up; Shards note under Progression |
| `02-CORE_SYSTEMS.md` | §8 rewritten (3–5 rooms via **reshuffling bag**, **Reward Room removed**, Trapped Soul added); new **§12 XP & Leveling**, **§13 Evolution Tiers**, **§14 Trapped Souls**, **§15 Narrative Systems** (which absorbs the previous pass's Narrative & Dialogue section as a subsection) + the Post-MVP block |
| `04-BALANCE.md` | Currency upgrades → **Quick Study / Insight Magnet** (Lucky Find deleted); Greed's Toll → XP; §14 is now a **run-end** award `(LevelsGained × 15) + (DepthReached × 10)`; Glimmer Gain → **XP Gain**; Head Start → **Quick Start**; rooms/floor 3–5; new **§16 XP Curve (open)**, **§17 Trapped Souls** |
| `05-ART_DIRECTION.md` | Two Open Items (Flicker Recognition Post-MVP, Biome 1 stays mine-themed); HUD currency counter → XP bar; §0's narrative cross-ref repointed §13 → §15 |
| `06-LEVEL_DESIGN.md` | Room pool table rebuilt (**Reward Room row gone**, **Trapped Soul Room added**, Biome 1 Mini-Boss is the father); rooms/floor 3–5 |
| `07-IMPLEMENTATION_PLAN.md` | **Scope Addendum** section — what moved into MVP and what is explicitly Post-MVP. Phases deliberately **not** renumbered |
| `08-MVP.md` | 9 new MUST SHIP items; the narrative "no tier" flag closed; Zyno MUST SHIP marked superseded; Gambler's Edge cut-line retired; 7 new Post-MVP items |
| `10-NARRATIVE.md` | Status note + a new §7 item 5 recording the Floor 16 contradiction |

### Where the changelog overrode a pushed decision

1. ~~**The father moves from Floor 16 to Biome 1.**~~ **REVERSED by the owner, 2026-08-15.** The
   changelog made him Biome 1's Mini-Boss; the owner ruled that **the father is the Final Boss, fought
   before Zyno**, restoring pass 2's structure. Floor 16 is two fights — The Depth Warden (her father)
   then Zyno — and Biome 1's Mini-Boss is **The Collapsed King** again. GDD §Narrative Premise,
   CORE_SYSTEMS §15, BALANCE §6, LEVEL_DESIGN §2, the plan's Day 41 note, MVP and NARRATIVE §7.5 were
   all put back. `03-CONTENT_DESIGN.md` never moved and needed no edit.
2. ~~**Zyno drops out of MUST SHIP.**~~ **REVERSED in the same ruling.** Zyno is **MUST SHIP**, fought
   immediately after the father, at the cheap version pass 3 costed: an existing Mini-Boss's
   moveset/arena, palette-swapped, with his own dialogue and identity. A bespoke fight is SHOULD SHIP.
   Two things this leaves open: **which** Mini-Boss he reuses (that choice sets his HP, phases and
   arena in one go — BALANCE §6 has no Zyno stat row), and the fact that **the 45-day plan schedules
   one boss on Day 41, not two.**
3. **The Glimmer rename is moot for the run currency.** Pass 3 renamed Ore → **Glimmer** (run),
   Ore Shards → **Shards** (meta), Miner's Traits → **Marks**. The changelog then **deletes the run
   currency outright** in favour of XP. So: **Shards and Marks survive and are used throughout**;
   Glimmer does not exist; and pass 3's mining-flavored upgrade renames are superseded where those
   upgrades now scale XP — Keen Eye → **Quick Study**, Glimmer Magnet → **Insight Magnet**, Lucky Find
   → **deleted** (no chests exist), Head Start → **Quick Start**. Sixth Sense keeps its new name.
   *Note the changelog itself still says "Ore Shards" and "Ore Gain" — pre-rename vocabulary, applied
   under the newer names rather than reverting a decision the changelog wasn't arguing with.*

### Three places the changelog could not be applied literally

1. **Section numbers collided.** `CORE_SYSTEMS §12` was already **Mini-Boss Weapon Rewards**. The new
   sections took §12–15 as the changelog specifies, and Mini-Boss Weapon Rewards moved to **§16**.
   Both external references to the old number were updated (`BALANCE` Open Items, the engineering
   plan's Open Engineering Questions). If the designer would rather keep §12 where it was, the new
   sections shift to §13–16 and four cross-references move with them.
2. **Sentences elsewhere in the same documents would have contradicted the change.** These were
   updated as mechanical consequences, not new design, and each is a one-line revert if unwanted:
   GDD's Resource Systems, Rewards, Randomization, Upgrades, Curses example, HUD line, Progression
   list, Hub Core Stat name and the currency-pickup SFX; CORE_SYSTEMS §9's floor-gated draw, §10's
   Glimmer conversion and Hub stat name; ART_DIRECTION §5's HUD "Glimmer counter" and §0's narrative
   cross-reference; LEVEL_DESIGN §1, §6 and its Open Items; MVP's Biome 1 room list, Hub-loop line and
   Definition of Done step 3.
   The **Whisper Layer has no visual specification** — GDD §UI now lists a line area for it and
   ART_DIRECTION §5 does not describe one, because inventing that is design, not translation.
3. **"Large Glimmer payout" had nowhere to go.** The Secret Vault's reward is described that way in
   GDD, CORE_SYSTEMS §8 and LEVEL_DESIGN §2, and the changelog deletes the in-level currency without
   saying what replaces it. All three now read **"large XP payout"** — the most literal translation,
   but it is a guess, and "guaranteed Legendary offer" may be the better answer now that XP is a
   pacing resource rather than a currency.

### Left stale on purpose — needs the designer

- **`03-CONTENT_DESIGN.md` was not in the changelog at all**, and is now the most out-of-date document
  in the set: its currency upgrade category still lists **Keen Eye, Lucky Find and Glimmer Magnet**;
  "That's 24 shared entries" is now **23**; Greed's Toll still reads in Glimmer; §6's room pool still
  has **2 Reward Rooms**; §7 still has the **Glimmer Gain** Core Stat and **Head Start**; §5 still
  names **The Collapsed King** as Biome 1's Mini-Boss where the changelog puts the father; and its
  Floor 16 entry still carries the Depth-Warden-is-the-father plus Zyno structure the changelog
  overrides. It is the one design doc this pass did not touch.
- **`08-MVP.md`'s cut valve** still says "reduce the shared pool from 24" — the pool is 23 now, and
  the line is annotated rather than renumbered.
- **"Per floor" no longer means what it did.** Offers are per *level*, but several effects are still
  scoped per floor: the Second Curse Slot and CONTENT_DESIGN §3's "only one Curse per floor",
  Warm-Up ("gauge starts each floor at 20%"), Nerves of Steel ("first hit each floor negated") and
  Sixth Sense ("one slot per offer guaranteed Rare+", which also interacts with the new mixed-tier
  single draw). Each needs re-scoping to level, floor or offer explicitly.
- ~~**The hazard timer and the new room count contradict each other.**~~ **Resolved by deletion** — the
  Rising Hazard was cut on 2026-08-15, see §12.
- **Whether the Evolution offer also shows a Curse** is undefined (CORE_SYSTEMS §13 says the normal
  offer is "replaced").
- **Which Mini-Boss does Zyno's MVP fight reuse?** He is MUST SHIP with no stat row of his own; picking
  the donor sets his HP, phase count and arena at once. Until then Day 41 can't be scheduled honestly.

---

## 12. DECIDED — the Rising Hazard is cut from the game (owner, 2026-08-15)

**No hazard front, no per-biome timer, no chase, no instant-kill edge, no scorched ground.** Asked what
the hazard timer was, the owner's answer was that there is no hazard timer at all; asked to choose
between "not built yet", "keep it but drop the fixed times" and "cut it entirely", they chose **cut it
entirely**. `CORE_SYSTEMS §7` and `BALANCE §7` are now removal notices — the section *numbers* are kept
so the ~20 cross-references to §8–§17 across the docs stay valid.

**Surviving:** each biome's *environmental* mechanics, which were always separate micro-systems —
Upper Caves' cracked tiles, Flooded Tunnels' slowing water and pushing currents, Molten Depths'
erupting geysers. Also the Floor 16 escape sequence's fixed 45s countdown, which never used the
hazard front.

### What the cut removes from the build

Genuinely less work: `HazardFront` and its three reskins, the per-biome timer tuning, the low/high
flood-zone data every Flooded Tunnels room needed, scorched-ground volumes, the Hazard Front VFX
(rockfall dust / water shimmer / lava glow), and the HUD's proximity vignette.

### Six holes, none of them filled here

1. **The game has no clock.** Nothing pushes the player downward. GDD's pitch still says "racing the
   danger below," which now describes nothing. Whether descent pressure returns in another form or the
   game becomes purely combat-paced is undecided — and it is the question the other five hang off.
2. **Secret Floors are pure upside.** "Costs time against the hazard" *was* the risk half. Same for
   **Trapped Souls**, whose interactable was priced the same way.
3. **Greed's Toll has no downside** (CONTENT_DESIGN §3, BALANCE §11) — its cost was a faster hazard.
4. **Hazard Kills are gone** (GDD §Combat, CORE_SYSTEMS §6) — no edge to push enemies into. Nothing
   replaces knockback-as-a-kill; Greatsword's Colossus is the only knockback payoff left.
5. **Biomes 2 and 3 are now thin.** They lose the larger half of their identity (rising water changing
   room geometry; the lava flow), leaving "water slows you" and "geysers erupt" against Biome 1's
   cracked tiles. `DESIGN_RULES` Rule 5 requires mechanical differentiation, not stat variation — as
   written, they no longer clear it. This also weakens Milestone 5's "pure reskin" assumption, since
   the hazard was the biggest system Biomes 2–3 were going to inherit.
6. **The Depth Warden's phases were the three hazard themes** (CONTENT_DESIGN §5, BALANCE §6), and the
   Final Boss arena was specified as degrading through all three (LEVEL_DESIGN §6). Both need
   re-theming onto what survives. The Drowned Custodian's "summons rising water" needs the same call.

### Run length is now unbounded

With no clock, a floor is 3–5 rooms at 30–60s each — 90–300s — across 16 floors, so **24–80 minutes of
pure combat** against a stated 30–60 minute session target, before detours. The hazard used to be what
capped this. Worth checking early, because it argues for fewer rooms per floor, not more.

---

## 13. The first Combat Room is built — and most of its specifics were invented (2026-08-15)

**Owner-directed.** The first room type is built and playable in `TestScene`:
`Prefabs/Rooms/CombatRoom_UpperCaves_01.prefab`, driven by `Scripts/Rooms/` (`CombatRoom`,
`WaveSpawner`, `RoomDoor`, `RoomEntry`).

**The docs specify a Combat Room's *intent* precisely and its *geometry* not at all.** CORE_SYSTEMS §8
gives the whole rule as one line — "Combat Rooms lock entry/exit doors until all spawned enemies are
defeated" — and LEVEL_DESIGN §2–4 adds positioning-zone and spawn-placement intent. Between them they
never state a room's dimensions, how a room is *entered*, what a door is or does, how many enemies a
room holds, or what it is composed of. All of that had to be decided to build one, and none of it
belongs in `Design/` until you have ruled on it.

### Owner's decisions this session

| | Ruling |
|---|---|
| Room lock | **Trigger on entry** — Armed → she crosses the room's half-way line → both doors shut and the wave spawns → Fighting → last enemy dies → Cleared |
| The fight | **One wave of six**: 3× Cave Crawler + 2× Rock Slinger + 1× Tunnel Brute = **150 enemy HP** |
| Cracked tiles | **Deferred.** The layout reserves a 2×2 zone; the micro-system is not built |
| Door art | Flat-colour placeholder, not routed through the `deeper-art` skill |

### Invented numbers — no design doc specifies any of these

All are serialized with `[Tooltip]`s saying so, retunable without a recompile (Design Rule 8).

| | Value |
|---|---|
| Room footprint | 28×16, 1-tile wall ring (inherited from the existing test room, which is engineering precedent, not design) |
| Door gaps | 2 tiles tall × 1 wide, at `y = 7,8` on both side walls |
| Door sprite | 32×64, 32 PPU; doors open and close **instantly**, no tween |
| Entry volume | 2×14, spanning `x = 13–14` over the room's full interior height |
| Enemy composition | 3 Crawler + 2 Slinger + 1 Brute |
| `nextWaveAtRemaining` | **1** — §8 says "~1 remaining"; the exact 1 is engineering's |
| Inter-group spawn stagger | **0 s** — a whole wave appears on one frame |
| Player walk-in position | `(4.5, 8.5)`, 8.5 units west of the lock line ≈ 1.7 s of approach |
| Six spawn points | `(8.5,12.5) (8.5,3.5) (19.5,8.5) (22.5,13.5) (22.5,2.5) (2.5,8.5)` |
| Six interior posts | tiles `(7,6) (7,10) (20,4) (20,11) (23,6) (23,10)` |
| Cracked-tile reserve | 2×2 at tiles `(17–18, 11–12)` |

**BALANCE §8's 30–60 s clear target is still unverified.** Six enemies at 150 HP against the Katana is
an estimate; whether it actually plays at 30–60 s needs a human at a focused Game view.

### Divergences that are design decisions, not just numbers — these need confirming or overruling

1. **Spawn points are not at the room edges, and cannot be.** LEVEL_DESIGN §4 asks for "spawn points
   at room edges, out of the player's immediate melee range at trigger time". Enemy aggro radii are
   **10** (Cave Crawler) and **12** (Slinger, Brute), and `EnemyTarget.Acquired` gates *all* movement
   and attacking — so in a 28-wide room a spawn on the literal far wall is 15–23 units from the lock
   line and **stands completely still until the player walks to it.** The six points instead sit
   6.0–11.2 units from the lock line, each inside the aggro radius of the enemy assigned to it. §4's
   intent (a beat to react, out of melee range) is preserved; its letter is not. Either the rule needs
   rewording, or aggro radii need to scale with room size.

   > **Superseded 2026-08-16 by a third option the owner chose — see §19.2.** Neither the rule nor the
   > radii changed. `WaveSpawner` now picks, per spawn, the authored marker **farthest from the player
   > that is still inside that enemy's aggro radius**, so markers can be authored at the edges as §4
   > asks without any of them becoming a dead drop. §4's letter is now *partly* satisfied — the
   > markers may sit at the edges; whether one is used still depends on where she is standing. The
   > hand-tuned inward placement described above is no longer required of a layout, and room 01's
   > markers are now chosen dynamically rather than by their authored order.
2. **Interior cover must be isolated convex posts, because there is no pathfinding.** `EnemyChase` is
   straight-line steering with a stop/retreat band. Any concave pocket or narrow slot is a permanent
   enemy trap, and a trapped enemy means a room that never unlocks. This is a hard constraint on every
   one of the 18 Combat Room layouts still to be authored, and it is not written anywhere in
   LEVEL_DESIGN — it should be, before the other 17 are drawn.
3. **`IsWaveRoom` is derived, not a stored flag.** §8 names it `IsWaveRoom = true`. It is implemented
   as `WaveCount > 1`, because a bool sitting beside the wave array is a second source of truth that
   can contradict it. Flagging a room is authoring a second wave. Same feature, no boolean to keep in
   sync.
4. **The room unlocks on the killing blow, not when the bodies are gone.** `EnemyDeath` holds a corpse
   0.45 s for its death animation, so for about half a second the doors are open with enemies still
   visibly falling over. This is deliberate — the alternative is a half-second of standing at an open
   door that has not opened yet — but it will look like a bug to anyone who has not been told.
5. **The entry volume listens on trigger-*stay* as well as trigger-enter**, so re-arming the room while
   the player is standing on the line restarts the fight rather than leaving the room armed forever.
6. **Enemies spawned by the test harness (F6–F9) are never counted by the room.** Killing them does not
   unlock the doors. Correct — they are not the room's encounter — but worth knowing while testing.
7. **The room ships short of LEVEL_DESIGN §3 in two ways.** It has **no cracked tiles** (§3 wants 2–4
   per Upper Caves Combat Room; the micro-system does not exist) and **no breakable-wall Dig-Dash
   shortcut** (§3 wants at least one per biome; Dig-Dash does not exist). The layout reserves space for
   the first. Neither is a divergence so much as an outstanding dependency, but the room is not §3-
   compliant until both land.

### One engineering note with a design consequence

A new physics layer, **8 `RoomTrigger`**, was added for the entry volume. On the Default layer the
volume would have silently destroyed every Rock Slinger projectile crossing the middle of the room —
`ThrownRock` despawns on entering anything in its blocking mask, and Default is that mask. Demonstrated
both ways before shipping. It matters to design only in that **any future room-scoped trigger volume
(Trapped Soul interactables, Secret Vault doors, geyser warning zones) must use this layer, not
Default.**

### Still outstanding from §10, and now louder

The room can kill the player and there is still **no player death or run-end** — she sits at 0 HP
inside a locked room with no way out, because the doors only open when the enemies are dead. That was
survivable in an open sandbox; a room that locks makes it a dead end in the literal sense.

---

## 14. Dig-Dash, spawn VFX and the first real environment art (2026-08-15)

**Owner-directed.** The Dig-Dash is built, enemies now telegraph before they arrive, and the first
non-placeholder environment art exists. This closes the largest locked-but-unbuilt mechanic in the
project: BALANCE's preamble says mitigation is *"exclusively Dig-Dash i-frames, Hyper Armor, Iron Skin
and Second Skin"*, and until now none of those existed — the Combat Room locked the player in with six
enemies and gave her no dodge.

It also unblocks content that was previously unauthorable: **five of 23 shared upgrades** (Momentum,
Quickstep, Long Dash, Phase Step, Blink Strike) and **one of six Hub Core Stats** (Dash Cooldown) are
dash-gated.

### What matched the locked design exactly

Pleasingly little had to be invented. BALANCE §1's **1.2 s cooldown** and **3.0 unit distance** were
already the serialized bases on `PlayerStats`, and are now read every dash rather than sitting unused.
BALANCE §1's **0.25 s i-frames** and BALANCE §2's **"available for the full Recovery phase"** cancel
window are both implemented as written. GDD §Controls' **LShift** is the bind. ART_DIRECTION §3's
**4-frame, shared-across-weapons** Dig-Dash budget is exactly what was generated.

### Invented — no design doc specifies these

| | Value |
|---|---|
| Dash **duration** | **0.18 s** — BALANCE §1 gives a distance and a cooldown but never a duration |
| Velocity curve | ease-out `2·d/t·(1−t)`, matching the attack lunge and the Brute's knockback |
| Gamepad bind | `<Gamepad>/rightShoulder` — a stick-click dodge is unusable |
| Trail afterimages | 5 images, 0.22 s fade, 0.45 start alpha, cool grey-violet tint |
| Spawn telegraph delay | **0.5 s**, and its ground-decal sorting (Default layer, above the tilemaps) |

### Divergences that need confirming or overruling

1. **The dash and the post-hit hit-stun share one immunity clock.** `Damageable` gained
   `GrantInvulnerability(float)`, which **extends but never shortens** `_invulnerableUntil`. One clock
   means every damage source is covered for free — `ContactDamage`, `OverheadSlam`, `LungeAttack`,
   `ThrownRock` all funnel through the single `TakeDamage` entry point, and none of them changed. The
   consequence: the windows are semantically different (0.6 s post-hit vs 0.25 s dash) but share one
   timestamp, so **a dash taken during a post-hit window inherits the longer one.** The alternative —
   a dash-owned flag — would require every damage source to ask the dash first.
2. **Phase Step has nowhere to live.** BALANCE §9 prices it at *"+0.1 s i-frame duration"*, but there
   is no `StatType` for i-frames and `DashCooldown`/`DashDistance` both have one. It ships as a
   serialized field on the dash component; when that upgrade is authored it needs either an appended
   `DashIFrames` stat or a direct write. Flagged rather than scaffolded.
3. **An enemy spawn telegraph is new scope and uses the reserved accent.** ART_DIRECTION §6's MVP VFX
   list is hit-flash, gauge pulse, environmental telegraphs, Dig-Dash trail and the upgrade/curse
   flashes — **there is no spawn effect on it.** And per the owner's ruling it uses §2's reserved
   orange-red, on the reading that "an enemy is about to exist here" is the same class of information
   as an enemy attack tell. That widens what §2's reservation covers. Both are designer calls.
4. **The wave now takes 0.5 s to arrive.** The doors shut, the marks play, then the enemies appear.
   This changes encounter timing against BALANCE §8's 30–60 s target by half a second per wave.

### Housekeeping done in the same pass

- `Sprint` (Unity template leftover, on LShift, read by no code) was **renamed to `Dash`**, keeping
  its binding and GUID.
- The dead template actions **`Jump`, `Crouch`, `Previous`, `Next` were deleted** — nothing read any
  of them, and the Player map is shipped content that the rebinding UI will read from.
- **A real pre-existing bug fixed:** `<Gamepad>/buttonNorth` was bound to *both* `Interact` and
  `HeavyStrike`, so both fired on one press. `Interact` moved to `buttonSouth`. The map now has zero
  duplicate binding paths.
- **Doc bug, worth correcting in the designer's own pass:** `AttackStateMachine.cs`,
  `CharacterPose.cs` and this brief's §7b all cite **"ART_DIRECTION §46"** for the
  chain-replays-base-animation rule. ART_DIRECTION has §0–§6; the rule is in **§3**.

### The environment-art finding, which is the one worth acting on

`.claude/skills/deeper-art` Phase 0 requires a **style anchor** — one approved canonical asset that
every later generation references — and it has never been done. It did not matter while all art was
character art, because `create_character`/`animate_character` anchor to an existing character and hold
style perfectly: the dash frames came back indistinguishable from the shipped idle, first try.

**The freeform generators have no such anchor, and it shows.** Two environment generations were
rejected outright before one passed:

- The first tileset returned a **pale cyan-white** wall — which §2 reserves cross-biome as the
  *Flooded Tunnels hazard accent* — over an orange crosshatch floor, with a uniform dark outline and
  no upper-left light. A direct violation of the locked readability rule.
- The spawn-VFX burst ignored `no_background` entirely (all 2304 pixels opaque) and came back navy
  and purple.

Restating the palette far more explicitly, and raising prompt adherence, fixed the *colour* on the
second tileset — it is now correctly cool grey with warm olive-brown veins, no blue, no cyan. It did
not fix the *form*: the floor reads as tidy dungeon cobblestone rather than a mine, and the wall reads
as a raised kerb rather than rock.

**The recommendation is to do Phase 0 properly before any more environment art** — generate and
approve one canonical Upper Caves tile, then pass it as the colour/init reference for everything else,
the way the character tools already do implicitly. ART_DIRECTION also has no tile budget at all
(§Open Items: *"Tileset count per biome"*), and no lighting section — the fixed upper-left key light
every asset is drawn to is recorded only in code comments, and one of them cites a §2 that does not
state it.

---

## 15. The in-run HUD is built, with real art (2026-08-15)

**Owner-directed.** ART_DIRECTION §5's HUD now exists as shipped UI rather than the debug text of the
test overlay: HP top-left, XP bar + level badge top-right, Ultimate Gauge and weapon icon
bottom-centre, plus the wave indicator and a depth readout. `UltimateGaugeHUD`'s own doc comment said
it *"will be replaced wholesale by the real UI art pass (ART_DIRECTION §5)"* — this is that pass, and
it is kept rather than replaced because it already owns §6's must-have Ultimate full-pulse.

### Every GDD §UI element, and what it is actually driven by

| GDD §UI element | Status |
|---|---|
| HP bar | **Real** — `Damageable` |
| Equipped weapon icon | **Real** — `RunLoadout.Weapon.Icon` |
| Ultimate Gauge | **Real** — `UltimateGauge`, keeps its full-pulse |
| XP bar + current level | **Real, but its source is new** — see below |
| Wave indicator, "only inside Wave Rooms" | **Real** — `CombatRoom.IsWaveRoom`, hidden outside one |
| Current floor / depth indicator | **No source at all** — the number is authored, not measured |
| Whisper Layer line area | **Not built.** ART_DIRECTION §5 still describes no visual for it (§11 records this); inventing one is design, not translation |
| ~~Heavy Strike cooldown icon~~ | Correctly absent — GDD §UI says "DECIDED: no Heavy Strike cooldown icon" |

### Things that needed inventing, or that are new scope

1. **A Dig-Dash pip is on the HUD and is in no design doc.** GDD §UI's list does not include one. It
   is there because the dash is the player's entire defensive option, its cooldown is a **Hub Core
   Stat**, and **two run upgrades** modify it — a resource the player is asked to invest in has to be
   visible. Drawn as a radial cooldown wipe rather than a fourth bar, so it does not read as another
   filling resource next to the gauge.
2. **XP now exists as a system, minimally.** CORE_SYSTEMS §12 defines XP and levelling; nothing
   implemented it, so an XP bar would have been decoration. `PlayerXP` accumulates and levels;
   `XPReward` on an enemy credits on death. **The level curve is invented** — BALANCE §16 is titled
   "XP Curve (open)" and gives nothing — and so are the per-enemy values, which §16 also records as
   unresolved. The level-up **upgrade offer is Milestone 4 and is not built**: `LeveledUp` fires and
   nothing listens.
3. **XP is credited directly, not dropped as orbs.** BALANCE §9's "Insight Magnet" prices an *orb
   pull radius*, so the design clearly intends a physical pickup. Orbs need a spawnable, a magnet and
   a collection radius; this credits on the killing blow so the bar has a real source now, and it is
   the half that gets replaced when orbs are built.
4. **The HP bar is crimson, deliberately not the reserved orange-red.** ART_DIRECTION §2's
   reservation explicitly covers UI chrome, and a health bar sharing a colour with hazard telegraphs
   is exactly the confusion the rule prevents. Worth confirming the crimson is far enough away.
5. **The HUD art is finer-grained than the world art.** The generated pieces are native 448×129-class
   pixel art; the world is 32×48 sprites at 32 PPU. Drawn 1:1 on a 1920×1080 reference canvas the HUD
   is crisp and internally consistent, but it does not share a pixel scale with the game it sits over.
   That is a legitimate style choice and a common one, but it is a choice — and the style guide's
   "one art pixel = one screen pixel" rule does not settle it either way. **Needs an eyeball pass.**
6. **`StatType.OreGain` is the XP Gain stat.** The docs renamed Ore → XP (§11) but the enum value and
   the serialized field were left alone, because renaming a serialized field silently drops its
   authored value. `PlayerXP` uses it as the XP multiplier. The rename is still owed.

### One thing that only showed up by looking

The generated frames ship with a **filled** stone interior, so a fill drawn under them is invisible and
a fill drawn over them hides the rivets and leather banding that make the frame worth having. The fix
was knocking the interior panel out to transparency so the fill reads *through* the frame — which is
how a framed bar is supposed to work, and which no assertion would ever have caught. Every bar frame
in `Art/UI/` is hollowed; a future one must be too.

---

## 16. §14 and §15 are now actually running — and what that changed (2026-08-15)

**Owner-directed.** The Dig-Dash pass (§14) and the HUD pass (§15) were both written with the Unity
connection down: no compile, no prefab wiring, no play mode. Both are now imported, wired and verified
in the editor. Everything below is what running them changed or exposed; the engineering plan carries
the bug fixes.

### The one with a design consequence

**BALANCE §1's 3.0-unit dash was not the distance the game moved her.** The dash covered **13.2 units**
under a normal frame hitch and the overshoot scaled with frame rate — it was a timing defect, not a
tuning one, and it is fixed (`DigDash` now ticks on the physics clock). It matters here because the
number in BALANCE was never what the game did, so **the dash has never actually been felt at its
authored distance.** 3.0 units is now what it travels, and it should be re-judged on that basis before
anyone retunes it.

### Invented — no design doc specifies these

1. **Per-enemy XP drops: Cave Crawler 4, Rock Slinger 3, Tunnel Brute 12, Deep Warden 20.** §15 left
   these unset, which would have made an Elite worth the same as a trash mob. They are derived as
   **`maxHealth / 5`** rather than picked individually, so the rule is inspectable and retunes as one
   number if BALANCE §16 ever lands. A full six-enemy wave pays 30 XP against a 10 XP first level.

### Confirmations and narrowings of things already recorded

2. **§14's spawn-marker colour divergence stands, and is in the art rather than the tint.**
   `SpawnBurst.png` already carries ART_DIRECTION §2's reserved orange-red in its own pixels, so
   `SpawnTelegraph.tint` is set to **white** — multiplying by orange turned the dark ground fissures
   muddy brown and flattened the glowing core into a red blob. The question for the designer is
   unchanged: may a *spawn* marker use the reserved hazard accent, which §2 reserves for attack and
   environmental telegraphs?
3. **§15.5's pixel-density question now has a picture behind it, and still needs the owner.** Drawn at
   the 1920×1080 reference the HUD is crisp and 1:1. It only degrades when the window is smaller,
   where `CanvasScaler` resamples point-filtered art — in the current collapsed Game view it renders
   at 45% and softens. So: correct at the reference resolution, and the open question is whether the
   game should letterbox to preserve it or accept the resampling at other sizes.
4. **The Upper Caves Wang tileset is rejected, not deferred.** §14 shipped it as a candidate. Looked
   at on its grid, its stone/moss edge runs **cross the 32×32 cell boundaries instead of sitting
   inside them**, so it is not a valid 16-tile Wang set and a `RuleTile` built from it would seam at
   every join. Its olive-green also sits outside the cool grey-purple the built rooms use. It stays
   unreferenced. §14's finding — *do the `deeper-art` Phase 0 anchor pass before any more environment
   art* — is the thing that prevents the next one.

### New scope, owner-directed: the run's upgrade strip

The HUD now carries a faint column of the upgrades the run is holding, down the left edge under the
health bar. **GDD §UI lists no such element** — ART_DIRECTION §5 covers the upgrade *offer* screen and
its rarity colour coding, but nothing in-run. It is here because a roguelike run is defined by its
picks and the player currently has no way to check what they took. It reuses §5's card colours
(Common white/grey, Rare blue, Epic purple, Legendary gold) so a tier reads the same in the strip as
on the card it came from, and it is deliberately drawn faint — it is a reference you consult, not a
readout you track.

Two consequences the designer should know about:

1. **`RunUpgrades` and `UpgradeDefinition` now exist, but the pool does not.** CORE_SYSTEMS §9's
   weighted draw, the three-card offer, the Curse slot and the Evolution milestones are all still
   Milestone 4. What was built is the seam they attach to, so the strip shows something real — the
   same call `PlayerXP` made for the XP bar.
2. **Only the Common tier of BALANCE §9's shared pool is expressible today.** Seven entries
   (Vitality, Iron Skin, Heavy Hands, Fleet Foot, Quickstep, Long Dash, Quick Study) are pure stat
   changes and are authored at §9's exact values. **Every Rare and Epic in that pool is behavioural**
   — Thorns reflects damage, Executioner scales by target health, Explosive Finish detonates on a
   kill — and none of them can be a stat modifier. They need damage-pipeline hooks. Nothing is being
   asked of the designer here; it is a note that the pool's *content* is gated on engineering work
   the design already implies (CORE_SYSTEMS §5's `source` decision), not on more authoring.

### Weapon icons were never icons

`WeaponDefinition.icon` and `bodyLayer` pointed at the **same sprite** — a frame of the character's
weapon paper-doll layer, which draws the weapon in the pose and arm position she holds it in. At HUD
icon size that reads as a small figure rather than as a weapon. Katana, Bow and Greatsword now have
standalone item icons. No design doc specified either behaviour; recorded because "the weapon icon"
in GDD §UI turns out to be a separate art requirement from the weapon's character layer, and the
other two weapons will need the same split when they are built.

### The player-death hole, now with a cost attached

§13 and the engineering plan both record that `Damageable.Died` has no subscriber on the player. This
pass is the first time that was watched happening: she reaches 0 HP, **keeps her collider, and gets
shoved around the room by the enemies still hitting her** — 13 units in a few seconds — with no death
state, no run end and no way out of a locked room. It is not a new divergence, but it is no longer
theoretical, and it is the most visible unfinished thing in the build.

---

## 17. Two new player moves and a dash that goes where you point it (owner, 2026-08-16)

Five owner-directed changes this pass. Two are cosmetic and need no design attention (a new dash
icon; the dash HUD slot reverted from a circle to the square that matches the weapon slot beside
it). The other three change what the player can do, and all three want a designer's ruling.

### 17a. The Dig-Dash now travels along the movement keys, not the facing

GDD §Player: *"Dig-Dash — short dash in facing direction."* It now dashes along the **movement
input**, falling back to facing only when no direction is held.

This is not a contradiction of the intent so much as a casualty of a change made after that line was
written. When the GDD was locked, facing *was* the movement direction. Mouse aim (owner-directed,
Children-of-Morta style, recorded earlier) moved facing onto the cursor — so "dash in facing
direction" silently became "dash at the cursor", and holding S to back away from something and
pressing dash threw her **into** it. Reading the movement keys restores what the original line
meant.

Consequence a designer should confirm: she can now dash sideways and backwards while still facing
the cursor, which is a genuinely larger defensive vocabulary than a forward-only dash. BALANCE §1's
3.0 units and 1.2s cooldown were priced against the smaller one.

### 17b. NEW MOVE — the Dash Attack

**In no design doc.** A Basic Attack pressed during a Dig-Dash, or within 0.35s of it landing,
comes out as a unique fourth weapon action instead of the ordinary Basic: its own animation, its own
timing row, a longer lunge, and slightly more damage.

Note the name collision, which is worth fixing in whichever doc adopts this: CORE_SYSTEMS §2's
**Dash-Attack Cancel** is the opposite move (a dash cancelling an attack's Recovery). Both now
exist and they are not related.

What this changes at the design level:

- The Dig-Dash stops being purely defensive. GDD §Combat and BALANCE's preamble both frame it as
  the game's *only* mitigation tool; it is now also an approach, which makes spending it offensively
  a real decision rather than a mistake.
- It is a fourth entry in a kit the GDD describes as three actions ("basic attack, Heavy Strike,
  Ultimate"), and GDD §Player calls the weapon "the single build-defining choice" that "fully
  determines the player's kit". A fourth action per weapon is a scope increase across all three.
- **Invented numbers**, none of which BALANCE §2 has a row for: 0.06 windup / 0.10 active / 0.20
  recovery / 12 damage, a 1.4-unit lunge (the longest of the four), and the 0.35s follow-up window.
  It fills the Ultimate Gauge at the Basic rate rather than getting a third column in BALANCE §4's
  table — that felt like a design decision to make in a lookup, so it was not made.

### 17c. NEW MECHANIC — the Heavy Strike charges

**Conflicts with a locked signature trait.** Holding RClick now charges the Heavy Strike; releasing
scales its damage (×2.2 at full), its hitbox radius (×1.35) and its lunge, hitstop and camera shake
(×1.6). Full charge is a 0.9s hold.

The conflict is GDD §Player and CORE_SYSTEMS §5b: **"hold to charge" is the Bow's signature trait**,
on its Basic Attack — *"Signature trait: Charge Shot — hold attack to charge bonus damage/pierce,
or release early for a fast weak shot."* Giving every weapon a charged Heavy weakens the thing that
was supposed to make the Bow feel different. This needs a designer's call, and there are at least
three defensible answers: the Bow's charge stays distinct because it is on a *different button and a
different action*; or the Bow's trait is re-cast as something else; or charging is Katana-only.

**The code is already built for whichever answer wins.** Chargeability is `ChargeSpec` data on
`WeaponDefinition`, not a rule in the state machine, so switching it off per weapon is a checkbox on
an asset. It currently defaults **on for all three**, which is the state that needs confirming or
overruling.

Things it does *not* break:

- GDD §UI's *"DECIDED: no Heavy Strike cooldown icon. Heavy Strike has no cooldown anywhere"*
  survives — a charge is a hold, not a cooldown, and nothing was added to the HUD.
- CORE_SYSTEMS §5b's own model of charging is followed rather than invented: holding *extends the
  Windup phase*. The authored 0.30s Windup lerps down to 0.07s as the charge fills, so a full charge
  comes out fast instead of charging up and then winding up. At zero charge the action is BALANCE
  §2's Heavy Strike untouched, which is what makes a tapped RClick still exactly the documented move.

**Invented numbers**, none in BALANCE: everything in the first paragraph above, plus a 0.45×
movement speed while charging and a 0.6 charge threshold before the charged animation plays.

One deliberate feel decision worth flagging: **she can still walk and still aim while charging.**
Rooting her for up to a second inside a room that locks six enemies in with her would make holding
the button a punishment. It also means a charge is not a commitment, so the Dash-Attack Cancel was
extended to let a dash break out of one.

### 17d. Three more animation states than ART_DIRECTION §3 budgets

§3's player table lists Idle, Move, Basic, Heavy, Ultimate, Dig-Dash, Hit-taken and Death. This pass
adds **Dash Attack**, **Heavy Charge** (the held pose) and **Heavy Charged** (the released swing) —
5 authored directions each, at 5 frames, on the Katana. §3 says exceeding the budget "needs a scope
conversation per DESIGN_RULES.md", so this is that flag. Priced across all three weapons it is 45
frames of new weapon-unique animation on top of the 57 §3 already counts.

Two mitigations are already in the code. Every new state falls back to an older sibling clip when a
weapon has no art for it (Dash Attack → Basic, both charge states → Heavy Strike), which is
ART_DIRECTION §3's own rule for Heavy chain extensions applied more widely — so the Bow and
Greatsword can ship these moves with **zero** new frames if the budget conversation goes that way.
And the charge hold is one looping clip rather than a clip per charge level.

### 17e. Where these need to land

Adding to §9's list of documents that need editing:

- **GDD** — §Player's dash line ("facing direction"), the kit description if the Dash Attack is
  kept, and the Bow's signature trait if the Heavy charge is kept.
- **CORE_SYSTEMS** — §1's action list, §2's Dash-Attack Cancel section (the name collision), §5b
  (charging is no longer Bow-only).
- **BALANCE** — §2's timing table needs a Dash Attack row and a charge row per weapon; §4's gauge
  table needs a decision on whether the Dash Attack gets its own fill rate.
- **ART_DIRECTION** — §3's animation budget, per 17d.

---

## 18. The HUD is restyled as pixel art (owner, 2026-08-16)

**Owner-directed:** *"Look at HUD. It's too simple for pixel game."*

Worth reading together with §15 and §16, because this reverses the *look* of an earlier owner note
without reversing the decision behind it. When the generated kit was called "too much", the problem
was **size** — a 448×129 health bar across a fifth of the screen. The fix stripped every frame to a
four-pixel band of one flat grey, which solved the size and overshot into a wireframe. This pass
keeps every footprint from that correction and puts the craft back as *material*: a real bevel, an
inverted bevel around each channel so bars read as cut into the plate, riveted end caps, segment
ticks on HP, shaded fills, and a bitmap font. Nothing on screen is larger than it was.

Most of this is engineering finish and needs nothing from the designer. Five things do.

### 18a. The HUD has a typeface now, and no doc has ever mentioned typography

`ART_DIRECTION` §5 specifies the HUD's *arrangement* (HP top-left, gauge and weapon bottom-centre,
XP and level top-right) and §1–§4 budget sprites, animation and tilesets. **Nothing anywhere
specifies text.** Until now every label used Unity's built-in `LegacyRuntime.ttf` — an anti-aliased
vector face, which was the loudest remaining "this is not a pixel game" element once the frames were
fixed.

There is now a shipped 5×7 bitmap face (`Art/UI/HUD_Font.png` + `.fontsettings`, 51 authored glyphs,
uppercase only with lowercase aliased onto the same cells). Two questions follow:

1. **Does it own the other two screens?** §5 also covers the Upgrade Screen and the Hub Screen, and
   §0 flags an entire unbudgeted narrative UI (dialogue, the Whisper Layer's line treatment, the
   Codex). A HUD face that does not extend to those leaves the game with two typographic styles.
2. **Uppercase-only is a design constraint, not just an art one.** Any narrative text — which is
   sentence case by nature — cannot use this face as authored.

### 18b. The HP bar has a third colour on it, and it is new feedback

A **chase bar** now sits behind the health fill: it holds where the fill was, waits ~0.35s, then
drains down to it, so a hit reads as a *block* whose size is the damage taken. No design doc
describes this; it is here because the fill alone tells you where you are and not what just
happened.

It is drawn in a muted rose (`0.69, 0.52, 0.54`) — deliberately **not** the orange-red `ART_DIRECTION`
§2 reserves for Upper Caves danger telegraphs, on the same reasoning §15 recorded for the health
colour itself. The designer's call: is a damage-feedback element on the HP bar one of the things
that reservation is protecting, or one of the things it should cover?

### 18c. HP is drawn in 8 segments, and no balance number backs that 8

The health bar carries seven dividers cutting it into eighths. That is a readability choice made in
the art, but a segmented bar makes an implicit claim — that a segment is a meaningful unit. `BALANCE`
gives no HP figure that divides into 8 cleanly, and enemy damage values are not priced against
"one segment". Either the count should follow a real number (max HP, or the biggest single hit in
the biome) or the segments should be understood as pure decoration. **Currently they are decoration.**

### 18d. Two small readouts changed shape

- **The level badge shows the number alone**, not "LV 7". The hexagon is the thing that says
  "level" — it is the one shape in the HUD that is neither a bar nor a slot — and at the pixel
  font's fixed advance "LV 12" runs straight through the badge's walls.
- **The wave indicator sits on a translucent plaque.** GDD §UI lists the indicator and §5 says
  *"shown only inside Wave Rooms"*; neither describes a background. It is translucent rather than
  solid because it sits over the middle of the play area during a fight.

### 18e. §15.5's pixel-density question now has a second layer

§15.5 asked whether the HUD and the world should share a pixel scale (they do not — HUD art is 1:1
at the 1920×1080 reference, world sprites are 32×48 at 32 PPU). The font adds a third scale: it is
authored at 5×7 and packed at **2×**, so HUD *text* has a 2px grid where HUD *chrome* has a 1px one.
This was deliberate — a 7px face is unreadable at 1080p, and packing at 2× makes 14 the font's
native size so nothing is ever resampled — but it means one HUD now contains two pixel grids, and
that is a style call rather than an engineering one. It is visible: the letters are chunkier than
the frames around them.

### 18f. RESOLVED — §15.5's pixel-density question, at least for the HUD's own scaling

§15.5 and §16.3 left this open: *"correct at the reference resolution, and the open question is
whether the game should letterbox to preserve it or accept the resampling at other sizes."* The
restyle forced an answer, because the answer turned out not to be a preference.

The owner's first look at the new HUD was **"it's the same UI, what is changed?"** — and that was an
accurate report. The canvas was on `ScaleWithScreenSize`, which produces a *fractional* factor at any
window that is not exactly 1920×1080; in the editor's Game view that was **0.45**. At that factor
every detail in the restyle is smaller than the resampling error. The 1px bevel, the rivets and the
bars' segment ticks all disappear, and the bitmap font renders `74 / 128` as `r4 / 128` — the 7 loses
its top stroke. The HUD came out as flat untextured bars, which is precisely the look the pass was
commissioned to replace.

**So "accept the resampling at other sizes" is not a viable option and is off the table.** The HUD
canvas is now pinned to whole-number scaling (`PixelPerfectHUDScale`): factor 1 at and just above the
reference height, 2 at 2160, and never below 1. Letterboxing is no longer required to keep the HUD
sharp — it is sharp at every window size.

The first attempt at this got the size wrong and the owner caught it — *"you made UI bigger in low
resolutions"*. Pinning the factor to a whole number while the art was still authored 1:1 forced that
factor to **1** at every window below 1080, so the health bar spanned a third of a 906px view instead
of a sixth. **The chrome is now authored at half its on-screen size**, which makes the normal factor
at 1080p **2**: identical on screen at the design target, half the footprint on a small window, and
sharp at both. The font moved to 1× packing for the same reason, which incidentally closes the "two
pixel grids" divergence §18e recorded — chrome and text now share one grid.

Nothing is left open here for the designer. The remaining behaviour is inherent to whole-number
scaling: between 1080 and 2160 the factor stays at 2, so on a 1440p display the HUD is proportionally
a little smaller than at 1080p. The world camera is a separate concern and was not touched.

---

## 19. The Upper Caves Wave Room is authored, and spawn placement stopped being hand-tuned (2026-08-16)

**Owner-directed.** `/implement-room-type Wave Room` resolved to a **layout** job, not a new room type:
CORE_SYSTEMS §8 already calls a Wave Room "a variant flag on Combat Room prefabs, not a new room type",
and the code already carried it — `WaveSpawner.waves` takes 2–3 batches, `nextWaveAtRemaining` is §8's
"~1 remaining" threshold driven off `Damageable.Died`, and `CombatRoom.IsWaveRoom` is derived from the
wave count. The engineering plan recorded that path as verified back in §13. What did not exist was an
authored Wave Room. It does now: `WaveRoom_UpperCaves_02`, layout **2 of the 6** LEVEL_DESIGN §2 asks
Upper Caves for, and the **1** flagged room MVP §55 caps the biome at for MVP.

### 19.1 The encounter is invented, but derived rather than guessed

BALANCE §8 gives Wave Rooms **60–100 s** against a standard room's 30–60 s and specifies nothing else —
no composition, no batch sizes, no per-wave ramp. What was authored:

| Wave | Composition | Total HP |
|---|---|---|
| 1 | 4× Cave Crawler | 80 |
| 2 | 3× Cave Crawler, 2× Rock Slinger | 90 |
| 3 | 1× Tunnel Brute, 2× Rock Slinger | 90 |
| | **12 enemies** | **260** |

The 260 is reasoned from room 01 rather than picked: that room is 150 HP against §8's 30–60 s, so 260
is **1.73×** it, landing at roughly 52–104 s. That is a placeholder in the Design Rule 8 sense and the
band is only as good as room 01's own untested timing — **whether either room actually clears in its
target window still needs a human**, and that has now been outstanding since §13.

Peak concurrency is **6** (one straggler plus a five-enemy batch) — deliberately the same density room
01 already ships, so this does not widen ART_DIRECTION §105's open question about whether Wave Room
enemy density needs a screen-clarity pass. It does not answer it either.

**No Deep Warden.** §8 says Wave Rooms "only resequence existing per-biome enemies", and the Warden is
an existing Upper Caves enemy — but it is the **Elite**, and CORE_SYSTEMS §8 ties the Elite to the
`SecretKey` drop that gates Secret Floors. Putting one in a standard pool room would pre-empt an
unbuilt system and quietly change what an Elite means. Excluded on that reading; **overrule this if the
Elite is meant to appear in normal rooms too.**

### 19.2 DECIDED — spawn placement is now chosen at runtime, which partly resolves §13.1

§13.1 recorded that LEVEL_DESIGN §4's "spawn points at room edges" is unimplementable as written,
because `EnemyTarget.Acquired` gates all movement on a 10–12 aggro radius and an edge spawn in a
28-wide room simply stands still. It offered two resolutions: reword the rule, or scale the radii with
room size. **The owner chose a third.** Neither the rule nor the radii moved. Instead `WaveSpawner`
now picks, for each individual spawn, the authored marker **farthest from the player that is still
inside that enemy's own aggro radius**, falling back to the old cycling order if nothing is in range.

This matters far more in a Wave Room than a Combat Room, and that is why it surfaced now: waves 2 and 3
arrive while the player is already mobile somewhere in a 32-wide room, so a fixed marker order that was
tuned for someone standing on the lock line is wrong for two of the three batches.

What this changes for design:

- **Markers can now be authored at the room edges**, as §4 asks. The Wave Room has ten, spread to all
  four corners. §4's *letter* is now satisfiable; whether a given edge marker is *used* depends on
  where she is standing, which §4 does not describe either way.
- **The rule §13.1 asked for is no longer needed as a layout constraint.** The replacement constraint
  is weaker and geometric: *every floor tile should have at least one marker within 10 units*, so the
  fallback never fires. Verified for this room — worst tile is 7.21 units from its nearest marker.
- **Room 01's behaviour changed underneath it.** Its six markers are no longer consumed in authored
  order. Its original pass asserted "6 spawned, one per marker"; that is now "6 spawned, each inside
  its own aggro radius", and when she springs the room from an unusual position two enemies can share a
  marker. Re-verified end to end; the room still locks, counts and unlocks identically.
- **§13.2 is untouched and still binding** — no pathfinding, so interior cover must be isolated convex
  posts. The new layout honours it: nine single-tile posts, minimum 4 tiles of clearance.

### 19.3 Numbers with no design source, invented here

| Thing | Value | Why |
|---|---|---|
| Room footprint | **32×18** (room 01 is 28×16) | LEVEL_DESIGN §4 says a Wave Room "needs larger open space"; it gives no size |
| Interior posts | **9** — 2 west, 7 east | §2 wants "at least 2 viable positioning zones"; built as a *character* difference (open west hall for Bow kiting and Greatsword whiff-punish, east pillar field for cover) rather than just more floor |
| Spawn markers | **10** | Enough that wave 1's four arrivals each get a distinct tile from the lock line — measured, not assumed: 5 markers are in range there at radius 10 |
| Cracked-tile reserve | 2×2 at tiles (21–22, 10–11) | Same reservation room 01 makes; the micro-system still does not exist |
| Door gaps | 2 tiles, west and east | Matches room 01 and LEVEL_DESIGN §1's linear left-to-right floors |

### 19.4 The same two §3 gaps room 01 has

No cracked tiles and no breakable-wall Dig-Dash shortcut, so this room is **not LEVEL_DESIGN §3
compliant** either. §3 wants 2–4 cracked tiles per Upper Caves Combat Room and at least one breakable
wall per biome. The cracked micro-system is still unbuilt; the breakable wall now *could* be built,
since Dig-Dash exists as of §14, and no room has one.

### 19.5 Not a design matter, but it removes a blocker the designer was told about

§13 closed by noting `F12` was the last free function key. The sandbox now has a **test config HUD** —
a toggled clickable panel holding every cheat and a room selector — so further harness work costs a
button rather than a key. The room selector is explicitly **not** the floor loader: nothing sequences
rooms, nothing draws from a bag, and `CombatRoom.Cleared` is still the untouched hook for that.

### Still outstanding, and now louder again

**There is still no player death or run-end.** §13 flagged this as a dead end in the literal sense for a
room that locks you in. A three-wave room lengthens the exposure: she is now locked in for 12 enemies
across three batches rather than 6 in one, with no death state and no way out at 0 HP.

---

## 20. DECIDED — charging a Heavy Strike now roots her (owner, 2026-08-16)

**Owner-directed, and it reverses §17's reasoning rather than filling a gap.** When the chargeable
Heavy Strike shipped, engineering chose to let her keep walking at **0.45×** through the hold, and
wrote the argument into the code: *"rooting her for up to a second in a room that locks six enemies in
with her turns a charged swing into a punishment."* The owner has overruled that. **A charge now costs
position.** `chargeMoveScale` is **0**.

No design doc specifies either behaviour. BALANCE and CORE_SYSTEMS do not describe a charged Heavy at
all — the whole mechanic is §17's owner-directed addition — so this is not a divergence from locked
design, it is a revision of an engineering decision that was recorded as one. Nothing in `Design/`
needs to change; the Rule 14 pass that eventually writes up the charge should write up **this**
version of it.

### What actually changed

One serialized number. `AttackStateMachine.MoveSpeedScale` already returned `chargeMoveScale` during
`AttackPhase.Charging`, and `PlayerController` already multiplied her walk speed by it, so rooting her
needed no new code — which is the payoff of that number having been a tunable rather than a constant.

### What deliberately did NOT change, and why it matters to the feel

- **She still aims through the charge.** `PlayerAim` reads `IsCommitted`, which stays false while
  Charging, so the cursor keeps turning her. Aiming a charged swing while holding it is the reason to
  hold it, and the owner asked for *movement*, not aim. **Say so if the intent was to freeze facing
  too** — that is a second decision, not part of this one.
- **She can still Dig-Dash out of a charge**, and this matters more now than it did: the dash is the
  only way to leave the spot she is committed to. `DigDash.TryDash` cancels the charge outright.
- **`Charging` is still not `IsCommitted`.** The rooting is a *speed of zero*, not a commitment.
  Folding Charging into `IsCommitted` would have looked equivalent and been three bugs at once: it
  freezes her aim, makes the charge undashable, and hands movement to `LungeVelocity`, whose ease-out
  reads an `_elapsed` that is still zero during a hold and therefore reports **peak lunge speed for
  the entire charge**.
- **She decelerates into the root** rather than snapping still, because the controller smooths toward
  the new target of zero over `decelerationTime`.

### Verified in play mode

Driven through the real input path with a virtual gamepad (`Move` = leftStick, `HeavyStrike` =
buttonNorth), not by poking fields:

| Case | Result |
|---|---|
| Stick held, no charge | walked **52 units**, velocity **5.00** |
| Stick held, charging | displacement **0.0001 units**, velocity **0.0000**, `MoveSpeedScale` 0 |
| During the charge | `IsCommitted` false (aim live, dash legal), `LungeVelocity` zero |
| On release | swing fires, phase returns to Idle, lunge still carries her **1.92 units** |

### The open question this reopens

§17 recorded that charging takes the Bow's locked signature trait (CONTENT_DESIGN gives the Bow the
charged shot). A rooted charge sharpens that: a rooted charged shot is much closer to the Bow's
intended identity than a mobile one was, so the Katana now overlaps it harder. Still unresolved, still
a designer call.

**And the standing one:** whether a rooted second inside a locked room is survivable cannot be
answered until there is a reason to fear dying in one — there is still **no player death or run-end**.

---

## 21. The Secret Vault is built, and it closes two questions §8 and §11 left open (2026-08-16)

**Owner-directed.** `/implement-room-type Secret Vault`. Unlike §19's Wave Room, this **is** a new room
type: CORE_SYSTEMS §8 gives a Secret Floor a locked door, a `SecretKey` from a rare elite, and a payout,
and none of the three existed in the build. `SecretVault_UpperCaves_01` is the **1 layout** LEVEL_DESIGN
§2 and CONTENT_DESIGN §6 budget for the type, meant to be reused across all three biomes with tile
dressing swapped — which is why it is named for the type and not for the Upper Caves alone.

⚠️ **Not verified in play mode.** It is written, imported and wired; nobody has run it. Everything below
is what the code does *by construction*, not what was observed. §16 is the reason that distinction is
called out rather than assumed away — structurally-checked code is not working code, and that pass cost
four defects to learn it. What needs checking is listed at the end of this section.

### 21.1 DECIDED — the vault's replacement cost is a fight plus a spent key

CORE_SYSTEMS §8 closes with a flagged hole rather than a rule. With the Rising Hazard cut (§12), "a
Secret Floor is pure upside — it needs a new cost (a fight, a resource, a one-per-run limit) or it stops
being a decision." **The owner chose the first two of those three.**

- **The fight.** The vault chamber holds an encounter, so the payout is guarded rather than collected.
  One wave, 6 enemies, **190 HP** — 2× Tunnel Brute, 2× Rock Slinger, 2× Cave Crawler.
- **The resource.** The key is **consumed** on opening the door, so a second vault in the same run costs
  a second elite. §8 says the door "requires a `SecretKey` flag" and never says the flag is spent; a flag
  that survives opens every later vault for free, which deletes the elite's reason to exist. This is
  engineering's reading, not a locked rule — `VaultDoor.consumeKey` is a serialized checkbox so
  overruling it is not a recompile.
- **No one-per-run limit**, the third option, because nothing in the build counts runs or floors yet.

**Why one wave and not two or three.** §8 caps flagged Wave Rooms at 1–2 per biome's pool and the Upper
Caves' allocation is already spent on `WaveRoom_UpperCaves_02` (§19). So the vault buys its difficulty
with *composition* rather than with batches: 190 HP is **1.27×** room 01's 150 and well under the Wave
Room's 260, spent on two Brutes instead of more bodies, keeping peak concurrency at the **6** room 01
already ships. **BALANCE has no Secret Vault row at all** — not in §8's pacing table, not anywhere — so
there is no target window to have hit or missed.

### 21.2 RESOLVED — §11.3's "large XP payout vs. guaranteed Legendary" picks the Legendary

§11.3 recorded that the vault's reward had been rewritten to "large XP payout" as the most literal
translation of the deleted Glimmer, while noting *"'guaranteed Legendary offer' may be the better answer
now that XP is a pacing resource rather than a currency."* **Built as the Legendary.** GDD,
CORE_SYSTEMS §8 and LEVEL_DESIGN §2 all still read "large XP payout or guaranteed Legendary-tier upgrade
offer"; only the second half is implemented, and the XP alternative is not built at all.

The relic handed over is the **equipped weapon's own**, which makes CONTENT_DESIGN §4's "only offered
when that weapon is equipped" literally true rather than a pool-filtering rule: `VaultReward` asks the
run's weapon for its relic and never names one. `Upgrade_EndlessEdge` exists as a Legendary-tier
`UpgradeDefinition` carrying BALANCE §12's numbers, and `ComboCounter` reads them — no stack cap, and
the per-stack bonus reduced from 2% to **1%** to pay for it. BALANCE §13's "Legendary excluded entirely
— guaranteed-drop only" needs no code, because the weighted pool does not exist yet (Milestone 4) and
this relic is reachable only through the vault.

**The Bow's Deadeye's Promise and the Greatsword's Mountain's Fall are not authored.** Both need a system
that is unwritten — the Bow's Charge Shot and the Greatsword's Ultimate — so a vault entered with either
weapon logs a warning and pays nothing. That is a build gap, not a design question.

### 21.3 Divergences that need confirming or overruling

1. **The vault grants; it does not offer.** §8 says "guaranteed Legendary-tier upgrade *offer*". The
   three-card offer panel is Milestone 4 and does not exist, and with exactly one guaranteed Legendary
   there is nothing to choose between — a one-card panel would be ceremony around a grant.
   `VaultReward.Granted` fires with the upgrade, so the real panel takes that seam over later.
2. **The payout lands on the clear, not on a touch.** The pedestal is a prop that reads the room's state;
   she does not interact with it. §8 does not say which, and there is no interaction system (the same gap
   Trapped Souls will hit).
3. **The key is credited on the killing blow, not dropped.** `KeyReward` mirrors `XPReward` exactly:
   there is **no pickup system anywhere in the project**, and building the first one inside a room type
   would be a second objective smuggled in. §8's wording is "granted by defeating a rare elite spawn",
   which this is literally. When XP orbs land, a key on the ground is the same job.
4. **The vault is a dead end with a single floor door.** §8 calls a Secret Floor a detour off the route,
   not a room on it, so there is nothing to walk through to. Every other room has two doors. **Say so if
   a vault is meant to rejoin the floor** rather than being backtracked out of.
5. **The elite that drops the key appears in no room.** `KeyReward` is on `DeepWarden.prefab`, but §19.1
   deliberately excluded the Warden from standard pool rooms, no room places one, and there is no floor
   sequencing to put one on the way. In a real run the key currently has **no source** — it is reachable
   only from the sandbox's spawner or its debug button. Filling this needs the floor loader, not a design
   ruling, but it means the whole gate is untested end to end as a *player* experience.

### 21.4 Invented numbers — no design doc specifies any of these

| Thing | Value | Why |
|---|---|---|
| Room footprint | **22×16** | LEVEL_DESIGN §2 asks a vault for function over layout novelty and gives no size. Deliberately the **smallest room in the game** — Combat Room 28×16, Wave Room 32×18 — because a vault is one fight in one chamber, not a hall |
| Encounter | 6 enemies, **190 HP** | 1.27× room 01's 150, derived the way §19.1 derived 260; BALANCE has no vault row |
| Key drop | **1** per elite | §8 gives no drop rate. A *chance* to drop would make the elite's entire reward invisible on the roll that fails |
| Keys at run start | **0** | Non-zero is the testing shortcut only |
| Interior posts | **4**, single-tile | §2's "at least 2 positioning zones", built as a character difference: an open middle lane for Bow kiting and Greatsword whiff-punish, four posts giving the Katana line-of-sight breaks to close through. 5 tiles apart, ≥2 floor tiles from any wall, so §13.2's no-pathfinding rule holds |
| Spawn markers | **6** — 4 corners, 2 mid-edge | Farthest is **9.0 units** from the entry band, inside even the Cave Crawler's 10-unit aggro radius, so `WaveSpawner`'s cycling fallback is unreachable here. *(The layout's own comment first claimed 8 and 5-tile post clearance; both were written blind, and both are corrected to the measured values.)* |
| Lock volume | 2 × 2.4 units | Wider and taller than the 1×2 doorway on purpose: the door's barrier stops her *before* the gap, so a volume the size of the gap is one she can never reach |

### 21.5 One engineering shape with a design consequence

**Relics live on the weapon asset**, as `RelicSpec` beside the `ChargeSpec` and `UltimateBuffSpec` that
are already there — not in a relic registry keyed by weapon type. That is what lets the vault pay out
without ever naming a weapon or a relic, and it means adding the Bow's relic later touches no room code.
The consequence for design is small but real: **a relic is now part of a weapon's definition**, so if a
relic is ever meant to be weapon-agnostic, or a weapon to have two, that shape has to change.

**The interior wall is load-bearing, not decoration.** `VaultDoor` seals on `RoomState.Fighting` and
reopens on the clear. This is not flavour: `EnemyChase` has no pathfinding (§13.2), so a guard following
her back through the 1-wide doorway jams on the wall and the room never unlocks. Widening the gap is not
a substitute, and neither is removing the wall.

### 21.6 The same two §3 gaps rooms 01 and 02 have

No cracked tiles and no breakable wall, so the vault is **not LEVEL_DESIGN §3 compliant** either. §3
wants 2–4 cracked tiles per Upper Caves room and at least one breakable wall per biome; the cracked
micro-system is still unbuilt, the breakable wall has been *possible* since §14, and now **three** rooms
lack one. A vault is the most obvious place in the game for a Dig-Dash breakable wall — an alternate way
in that costs no key — and that would be a design decision, not a fix.

### Still outstanding, and unchanged by this

**There is still no player death or run-end**, and the vault is the worst room yet for it: she is locked
in a 22×16 chamber with two Brutes, and at 0 HP she is shoved around by enemies whose deaths are the only
thing that opens the door. Every room type added since §13 has made this louder.

### What a human still has to check

The key drop crediting on a Warden kill; the door consuming exactly one key and refusing to open on
zero; the seal holding for the whole fight; the payout arriving once on the clear and not again on a
re-arm; and whether 190 HP in 22×16 is a fight worth a key — which is a feel judgement, and **BALANCE
§8's 30–60 s target has now gone unmeasured for three rooms running**.
