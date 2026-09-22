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

> **Superseded 2026-09-20 — see §28.** This element is no longer on the play screen at all. Everything
> below about *why* an in-run readout exists still stands and is still not in any design doc; only
> its location changed. The strip is now a grid on the pause menu and a compact icon row on the
> offer screen.

The HUD carried a faint column of the upgrades the run is holding, down the left edge under the
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

---

## 22. Floors are sequenced, and rooms are dressed (owner, 2026-08-24)

**Owner-directed.** The owner asked for the Hades model — handcrafted room templates, procedurally
selected and procedurally dressed — plus a new scene that runs it. Built and verified in play mode;
the engineering half is in `Docs/Engineering/00-IMPLEMENTATION_PLAN.md`.

**Most of this is not a divergence at all**, and that is worth stating first. `06-LEVEL_DESIGN.md`
§1 already says "Hand-built, not procedural… pulled from a per-biome pool via a reshuffling bag",
`01-GDD.md` §Randomization already says "Room order within a biome's pool is drawn per run via a
reshuffling bag (not live procgen)", and Design Rule 3 already prefers "a system that generates
variety from existing content" over more static content. The floor loader is the locked design
finally being implemented. **No room geometry is generated**, and none ever should be.

The owner also gave explicit, one-off permission to edit `Docs/Design/*` for this work, and then
directed that the linear/branching question needed no document change. **Nothing in `Design/` was
edited** — staying linear means there was nothing to reopen. Everything below is recorded here as
usual.

### 22.1 PROPOSED — room dressing is a new system with no design doc and no MVP tier

`RoomTheme` + `RoomDressing` give one handcrafted layout a different look each time it is drawn:
weighted floor and wall tile variants picked per cell, a random quarter-turn per floor cell, and
ground decals scattered on a baked eligibility mask.

**Nothing in `Design/` describes this.** The nearest thing is §2's "biome-specific tile dressing"
for the Secret Vault, which is about reusing one layout across biomes, not about varying a room per
visit. It is also absent from `08-MVP.md`'s tiers entirely. Under Rule 1 that makes it a scope
addition needing a cut, an extension, or a demotion — raised here rather than assumed.

Two constraints it adopts, both to avoid inventing anything:
- **Nothing is tinted.** Both shipped tile assets carry `TileFlags.LockColor`, so `Tilemap.SetColor`
  is a silent no-op on them — no error, no effect. Unlocking per cell would work, but tinting is
  also the mechanism most likely to drift into §2's reserved hazard accents by accident, since a hue
  shift on grey stone wanders orange without anyone deciding to. Variety comes from *which tile*.
- **Decoration never collides**, and that is structural rather than a rule to remember: decor is a
  third Tilemap whose tiles are authored `ColliderType.None` and which carries no
  `TilemapCollider2D`. `EnemyChase` has no pathfinding, so one stray solid cell in open floor can
  trap an enemy in a room that then never unlocks. The cost is that decor is **ground-plane only** —
  a Tilemap cannot Y-sort per cell, and anything waist-high also reads as cover that does not block.

### 22.2 CONFLICT — the Secret Vault cannot be on a linear route

`02-CORE_SYSTEMS.md` §8 calls a Secret Floor a **detour** off the route. `06-LEVEL_DESIGN.md` §1
locks "Linear, no branching". A detour requires the branch §1 rules out, and the built
`SecretVault_UpperCaves_01` is a **one-door dead end** by design.

In an eastward corridor every room is walked out of, so the vault is currently **excluded from the
floor pool** — enforced by geometry rather than omission: `RoomBag.Draw` refuses any layout whose
`RoomConnection.HasExit` is false. It remains fully playable in `TestScene` and loses nothing.

Three options, and this is the owner's call, not engineering's:
(a) give the vault an east door and make it an on-route key-gated room, which contradicts "detour";
(b) make it a floor's terminal room, which needs floors to end somewhere rather than run on;
(c) leave it out of runs permanently and reach it another way.

### 22.3 DECIDED — the two PixelLab tools fail in opposite directions

Recorded because it cost four rejected generations and will recur for every future biome.
`create_image_pixflux` gets the **palette** right (with the project's own colours forced via
`color_image_base64`, every returned colour came from that set) and the **form** wrong — it shades
the image as a picture, so a 32×32 floor candidate came back with edge rows averaging 51–58 against
an interior of 75, which cannot repeat. `create_topdown_tileset` gets the **form** right — its base
tiles tile perfectly seamlessly — and the **palette** wrong, returning navy blue, which is the
Flooded Tunnels family and the same class of mistake §2 already flagged for the first tileset.

**The resolution is a luminance remap onto the shipped ramp after generation**, which is pixel-exact
and is the same palette-swap-as-variant technique `05-ART_DIRECTION.md` §68 already uses for the
Deep Warden. This is an engineering technique, not a design change — but it is worth the designer
knowing that **generated environment art will always be recoloured onto the locked palette rather
than trusted to arrive on it.**

### 22.4 CONFLICT — "descend" is currently "walk east"

`01-GDD.md` says floors are "connected linearly downward" and Descend is one of the four core verbs
(Rule 5). What is built is a corridor running east, with the floor number incrementing silently and
**no floor-transition presentation at all**. The door pipeline supports west and east only —
`BuildDoorColumns` buckets by column and has no north/south concept anywhere.

This is a real gap between the built thing and the pitch, and it needs a design answer rather than a
quiet engineering choice: a stairwell room, a fade, a descent beat, or north/south doors.

### 22.5 Invented numbers

- **Rooms per floor is rolled uniformly in [3, 5]** per floor. §8 and §5 say "3–5" and never say how
  it is chosen; uniform is the obvious reading, not a stated one.
- **`decorDensity` 0.12** — the fraction of eligible floor cells taking a decal. No doc has a number
  for this; it is a serialized field so retuning costs no recompile.
- **A one-cell margin around doors, the entry band, spawn markers, the player start, the pedestal
  and the reserved cracked-tile zone.** Walls and interior posts get no margin, because a decal
  against a wall reads well; the margin exists for things a decal would be *misread* against.
- **The Upper Caves palette was extended by two steps** — one darker (26,24,32) and one lighter
  (140,133,148) than the shipped tiles — to give the 3–5 step ramp `style-guide.md` §4 requires.
  §2 specifies a "6–8 colour core" and does not enumerate it, so this fills a gap rather than
  contradicting one. **No orange-red was used**, per §2's reservation.

### 22.6 A seed exists, and it is not the Post-MVP "seeded runs" feature

`RunSeed` holds one `System.Random` for the run and logs its seed. `08-MVP.md` lists daily/weekly
seeded runs and ghost replay as explicitly Post-MVP; **this is not those**. There is no seed UI,
nothing saved, nothing shown to a player — only a number logged so a floor-order bug can be
reproduced by typing it back into the Inspector. Flagged so it is not mistaken for the feature.

### 22.7 Stale doc noticed, not fixed

`03-CONTENT_DESIGN.md` §6 still lists **2 Reward Rooms per biome** and still omits the Trapped Soul
Room, months after §8 and §2 removed and added them respectively. Already recorded in §11; repeated
here only because a room-pool implementation reading §6 literally would build a deleted room type.
The matching error inside `Engineering/00-IMPLEMENTATION_PLAN.md` is an engineering doc and is
fixable in place — offered, not assumed.

### 22.8 The scene is a room-visual sandbox, not a run scene (owner, 2026-08-24)

The owner corrected the deliverable: what was wanted was **another test scene for looking at room
visuals**, with a button that re-rolls the room, not a scene that plays a floor. `RoomLabScene`
replaces `RunScene`, which was deleted.

No design consequence — `02-TEST_SCENE.md` §1 already frames the sandbox as where everything is
"built and tuned first" before reaching a real game scene, and a room lab is that. The floor loader
survives as built and verified code with no scene; it is still CORE_SYSTEMS §8's reshuffling bag,
which `08-MVP.md` lists as MUST SHIP.

> ✅ **SUPERSEDED (owner, 2026-09-08).** `RunScene` is back and the floor loader is mounted in it —
> see §25. The room-visual sandbox stays, and so does `TestScene`; the project now has three scenes
> that each do one job.

### 22.9 Generated environment art is always recoloured, never trusted to arrive on palette

Recorded so the designer knows how every future biome's art will be produced. PixelLab's tileset
tool gets tiling **form** right and has no palette parameter at all — it returned navy for one
request and green for another, and navy is the *Flooded Tunnels* family, the same class of mistake
§2 already flagged. The freeform image tool is the mirror: it obeys a forced palette exactly, and
shades the image as a picture so the result cannot repeat.

Every environment asset is therefore luminance-remapped onto the biome's locked ramp after
generation. That is an engineering technique, not a design change, and the ramps contain no reserved
hazard accent. **One rule it forced is worth knowing:** floors and walls must occupy different bands
of the ramp, because a plain remap made a floor and its wall five luma apart and the wall ring
disappeared in play mode.

---

## 23. The level-up upgrade offer is built (owner-directed, 2026-08-25)

The screen `08-MVP.md` protects above everything else — *Movement → Combat → Dig-Dash → **Upgrade
Pick** → Descend*, `09-DESIGN_RULES.md` Rule 13 — now exists. `PlayerXP.LeveledUp` had been firing
into an empty room since §15; it now pauses the game and opens a three-card offer with the
always-visible Curse slot, exactly as `CORE_SYSTEMS` §12 and §9 describe.

**The owner's scope for this pass was the UI and the content, not the effects.** Every upgrade and
Curse the docs currently name is authored with a real generated icon, drawn from a real weighted
pool, and taken through `RunUpgrades.Add`. What is *not* built is the behavioural half — see §23.6.

### 23.1 What is now true against the locked docs

| Doc | Requirement | State |
|---|---|---|
| GDD §Core Loop 4 | "the game pauses and presents an upgrade offer" | Built. `Time.timeScale = 0`, Player action map disabled, cursor forced visible. |
| CORE_SYSTEMS §9, §12 | 3 cards, one weighted draw across shared + weapon sub-pool, **not tier-gated** | Built. A Common, a Rare and an Epic can and do appear together. |
| CORE_SYSTEMS §9 | 4th slot always a Curse, from its own pool, never mandatory | Built. |
| BALANCE §13 | Common/Rare/Epic 65/30/5, 55/35/10, 45/40/15 by biome | Authored as data on `UpgradePool`. |
| CONTENT_DESIGN §1 | The shared pool | 24 entries authored — see §23.4 on the count. |
| CONTENT_DESIGN §2a | The Katana sub-pool | 13 entries authored — see §23.4. |
| CONTENT_DESIGN §3 | The 8 Curses | All 8 authored. MVP asked for 4–5 as MUST SHIP. |
| ART_DIRECTION §5 | Common white/grey, Rare blue, Epic purple, Legendary gold; Curse red/black | Built — see §23.3 on which red. |
| ART_DIRECTION §6 | Curse-pick flash red, normal pick white/gold | Built. |
| CORE_SYSTEMS §13 | Every 5th level is an **Evolution** offer | **Not built** — see §23.5. |

### 23.2 The card's geometry and typography are invented, because no doc specifies any

`ART_DIRECTION` §5 gives four border colours and "red/black for the Curse", and that is the entire
art spec for this screen. Everything else below was decided in engineering and needs a designer's eye:

- **Card 168 × 196 authored units** (336 × 392 on screen at 1080p), four across with a 12-unit gap
  between the three upgrades and a **30-unit gap before the Curse** — the wider gap is what stops the
  Curse reading as the fourth item in a list of four, which is §5's stated goal.
- **The width is set by text, not taste.** 168 minus padding leaves 148 units, which at the pixel
  face's 7-unit monospaced advance is 21 characters per line.
- **The height is set by the worst case** — a Curse with a three-line cost line. Every card in a row
  must be the same height, so an upgrade with two lines of description has space beneath it.
- **§18a's open question is answered by default, not by decision.** The HUD's 5×7 uppercase face now
  owns this screen too, so every card renders in caps. That was §18a's question 1 and it is still a
  designer's call to confirm; two glyphs (`'` and `&`) had to be added to the face because four names
  in the pool own an apostrophe.
- **Card text must stay inside a 53-glyph set.** `Gambler's Edge` renders because the apostrophe was
  added; an arrow or an em dash would render as a hole in the middle of a word and nothing catches
  it. This is why BALANCE §10's "Stack cap 10 → 14" is authored as "Combo stack cap 10 up to 14".

### 23.3 The Curse card is crimson, not the hazard red

`ART_DIRECTION` §5 asks for "a red/black treatment". §2 reserves **orange-red** *exclusively* for
hazard telegraphs, and §15.4 already confirmed that reservation covers UI chrome. The Curse card and
its cost line therefore use the crimson family the health bar already uses, not the hazard accent.
Same call, same reason, recorded so the two reds stay distinguishable.

### 23.4 Two counts in `CONTENT_DESIGN` do not add up, and BALANCE was followed

- §1 says "That's 24 shared entries" and its own tables list **25** rows — which becomes **24** once
  the three dead currency entries (Keen Eye, Lucky Find, Glimmer Magnet) are replaced by the two live
  ones (Quick Study, Insight Magnet). `BALANCE` §9 lists exactly those 24 and is internally
  consistent, so **24 is what was authored**. §11's "the pool is 23 now" is off by one.
- §2a's footer says "15 entries per weapon", but the section itself strikes through **Finisher+ and
  Echo Slash**. The Katana pool is **13**. The footer, and MVP's "reduce weapon sub-pools from 15",
  both need the number corrected.
- **The Bow and Greatsword sub-pools are deliberately not authored.** Neither weapon exists, so every
  entry would name a system that does not.

### 23.5 The Evolution milestone is not built, and cannot be

`CORE_SYSTEMS` §13 replaces every 5th level's offer with 2–3 mutually exclusive Evolution choices,
and `08-MVP.md` lists one Evolution Tier per weapon as MUST SHIP. **The content does not exist** —
§13's own Open Items list "the 2 Evolution choices per weapon — content, not just the slot". Level 5
therefore shows a normal offer. This is a content gap, not an engineering one; the panel has the seam.

Still open from §11 and unaffected by this pass: **whether the Evolution offer also shows a Curse**,
and the per-floor-vs-per-level scoping of "only one Curse can be taken per floor", the Second Curse
Slot, and Sixth Sense's guaranteed-Rare+ slot. The panel currently offers a Curse on **every** level,
which is the literal reading of §9's "a 4th slot is always populated".

### 23.6 What a pick actually does — and does not

`RunUpgrades.Add` applies each pick's `StatModifier`s through `PlayerStats`, verified in play mode
through the real panel: Vitality took Max HP 100 → 115, Fleet Foot move speed 5 → 5.5, Heavy Hands
damage bonus 0 → 3. **That is seven entries out of 45.** Every other upgrade and all eight Curses are
behavioural — Thorns, Explosive Finish, Blink Strike, Glass Cannon, Overclock — and land as data with
a name, a description and an icon that changes no number. They need hooks in the damage pipeline that
do not exist.

This is deliberate and was the owner's instruction for this pass. It does mean **a run can currently
take a Curse and receive neither its upside nor its downside**, which is a live gameplay hole in the
same class as the buff Ultimate discarding Combo stacks.

Two pool rules from §1 and §2 *are* enforced, because they are draw logic rather than effects:
Venom Edge and Bleeding Strikes exclude each other ("only one can be taken per run"), and Triple Cut
is never offered before Twin Cut.

### 23.7 No reroll and no skip, because no doc has one

Grepping `Design/` for reroll, re-roll or banish returns nothing outside the room lab's button. §3
makes the *Curse* declinable — by taking one of the three upgrades instead — but no doc says the
three upgrades may be declined. Neither was built. If either is wanted, it is a design decision.

### 23.8 The Secret Vault now presents its Relic instead of granting it silently

§21.3.1 said "`VaultReward.Granted` fires with the upgrade, **so the real panel takes that seam over
later**". It has. Clearing a Secret Vault opens the panel on a single centred gold card reading
**RELIC RECOVERED / CLAIM IT**, with no Curse beside it — a guaranteed Legendary is not an offer, and
pairing it with a Curse would turn a reward into a decision the design never asked for.

The vault still does the granting; the panel is the ceremony around it. §8's wording is still
"guaranteed Legendary-tier upgrade **offer**", and this is a presentation, so that word is now the
only thing left stale in that clause.

### 23.9 Upgrade icons: 46 generated assets, and the palette that made them usable

Every entry has a 128×128 icon generated through PixelLab with a **forced palette built from 25
colours the game already draws** — the Upper Caves greys and browns, the HUD's steel ramp, the four
tier colours and the bar fills. Objectively verified: **all 46 use only those 25 colours, and not one
has a semi-transparent pixel.** `Art/StyleAnchor/UI_IconPalette.png` is written from a committed table
by `Deeper/Generate Icon Palette`, and **contains no hazard accent**, so §2's reservation cannot be
broken decoratively even by accident.

One icon has a design consequence rather than an art one: **Greed's Toll's card says its cost is
missing.** §12 recorded that the Rising Hazard cut left that Curse pure upside; rather than invent a
downside, the card reads "COST PENDING: THE HAZARD IT PAID WAS CUT". It is the only card in the game
that admits to being unfinished, and it should stop being true rather than be reworded.

---

## 24. The Hub is built — a surface camp with a weapon rack and a shaft (owner, 2026-09-07)

The owner asked for the Hub, as a **walkable place** rather than a menu screen, lit as a **night
surface camp at the mine mouth**. Built: the camp itself, weapon select (working), descend (working),
and the stat shrine placed but stubbed. `Scenes/HubScene.unity`, built by `Deeper/Build Hub Scene`.

Most of this is design that already exists — GDD §Game Loop 1 ("Player starts in a small surface
camp"), §Player ("chooses 1 of 3 weapons in the Hub before descending... all 3 unlocked from the
start"), and CORE_SYSTEMS §1's weapon lock. The items below are where it goes past the docs.

### 24a. PROPOSED — a fourth palette that ART_DIRECTION §2 does not have

§2's table has three biomes: Upper Caves, Flooded Tunnels, Molten Depths. The Hub is above ground and
belongs to none of them, so a palette was invented: **packed brown earth and muted olive grass inside
a grey stone retaining wall, washed cool by a moonlight ambient.** It is deliberately the highest-value
contrast in the game — every biome is a dark hole, and the one safe place is outdoors.

The night is **not baked into the tiles**. They are authored at neutral daylight value and the camp's
`Light2D` ambient (0.55, 0.60, 0.86) makes it night, which is style-guide §8's own recommendation —
bake form, light mood. Asking the generator for "moonlit" art as well darkened everything twice and
looked muddy; that was tried and rejected.

**Needs a designer decision:** does the Hub get a row in §2's table, and is this the right direction
for it?

### 24b. CONFLICT — the campfire is the one warm light, and warm is a reserved colour

ART_DIRECTION §2 reserves orange-red, pale cyan-white and bright yellow-orange **exclusively** for
hazard telegraphs, "never reused for decorative purposes". The camp's fire is decorative and warm,
and it is the strongest colour in the scene — that is the entire point of it, because it is what
makes the camp read as safe against a cold night.

Argued for shipping it as-is: the reservation is a **readability** rule, and its purpose is that a
player reads "that colour means danger" instantly. The Hub contains no hazards, no enemies and no
telegraphs of any kind, so there is nothing there for it to be confused with. The flame is also held
to a muted amber rather than the saturated hazard orange.

This is the same *class* of conflict as the Katana Ultimate's cyan-white arcs already recorded in §2,
but with a better defence, and it is flagged rather than quietly taken. **If the reservation is meant
to be absolute regardless of context, the fire needs re-colouring and the camp loses its focal point.**

### 24c. The stat shrine is placed but does nothing, on purpose

CONTENT_DESIGN §7's Hub Stat System and the Relic Vault are Milestone 6 and are not built — there is
no Shard currency, no save file and no run-end award. The shrine is still placed, carrying a
`HubNotice` that answers "THE SHRINE IS COLD. NO SHARDS TO SPEND YET."

A station with no listener reads in play as a *broken* fixture: the prompt says the key works and
pressing it does nothing. Saying "not yet" costs one line of text and reads as unfinished instead.

### 24d. Descending leads to `TestScene`, because no run scene exists

There is no scene that plays a run — `FloorLoader` is written but mounted nowhere. The shaft
therefore loads the sandbox. This is an engineering gap, not a design change, and the destination is
a serialized field. **It does mean the Hub→Run→Hub loop of MVP §30 is only half real**: you can
descend, and nothing brings you back.

> ✅ **RESOLVED (owner, 2026-09-08).** The shaft leads to `RunScene`, and the run-end screen's
> Return to Hub button leads back. **MVP §30's Hub→Run→Hub loop is closed**, with Shards carrying
> over — verified in play mode. See §25.

### 24e. Two numbers with no design source

- **Camp size, 16×14 cells.** LEVEL_DESIGN sizes combat rooms, not the Hub. Chosen so the whole camp
  is legible at once at the current camera.
- **Station reach, 1.6 units** (against a 1.4×0.7 blocker). Invented; it is the distance at which E
  starts working, and it has to exceed the box she cannot walk into.

### 24f. Not a design matter, but the designer should know

The camp's first builds looked wrong for a reason that turned out to be **in the tile art, not the
code**: generated isometric tiles ship with a thick side wall that a 2:1 grid cannot hide, so floors
rendered as a field of raised blocks. **The shipped Upper Caves floor tiles have the identical
defect** and every room in the game is drawing floors that way. Separately, the project has no
pixel-perfect camera, so the world renders at a fractional zoom. Both are written up in the
engineering plan; both are plausible answers to "the tiles look bad", and both are the owner's call
to schedule.

### 24g. Usable fixtures are now marked, and the camp stopped lying about its lanterns

No design doc says how an interactable object announces itself — GDD §UI lists the HUD, the upgrade
screen and the Hub screen, and nothing covers world-space affordance. Invented, and flagged here:

- A **floating mark above every usable fixture** — a dim chevron at distance, an **E keycap** in
  range. Scenery deliberately gets none; the mark *is* the distinction between the weapon rack and
  the crates.
- **Every lantern post moved to stand beside a station**, so "lit means usable" is true across the
  camp. They were previously free scenery, which made the camp actively misleading.

Both are **bone-white on near-black, with no colour coding at all**, and that is the design-relevant
part: ART_DIRECTION §2 reserves orange-red, pale cyan-white and bright yellow-orange exclusively for
danger. The obvious treatment for "you can use this" is a warm glow, and it is exactly the treatment
the palette forbids. If a designer later wants interactables colour-coded, §2 has to give them a
colour that is not one of the three reserved ones — otherwise the Hub teaches a colour language that
the biomes then contradict.

**Open question for the designer:** does this marker generalise to the whole game — chests, doors,
Trapped Souls, the Secret Vault pedestal — or is the Hub a special case? It was built as if it
generalises (nothing in it is camp-specific), but that is an engineering guess, not a design call.

### 24h. The Shard total is on screen, and it is honest about being empty

GDD §UI lists "Shard total" first on the Hub Screen, so the readout itself needs no design decision.
Three things around it do:

- **It always reads 0**, because nothing awards Shards. GDD §Currency and BALANCE §14 compute them
  once at run end from Levels Gained and Depth Reached, and there is no run end — §24d's missing run
  scene is the same hole seen from the other side. The counter is real, the currency is not yet.
- **Shards do not persist between sessions.** `ShardBank` is a ScriptableObject, so a balance set at
  runtime survives the editor session and is discarded in a build. That is wrong for a *permanent*
  currency and is knowingly temporary — Milestone 6's `SaveData` is what makes it true.
- **The icon is violet** — `168,115,219`, the Epic tier colour the game already ships. Worth stating
  because the intuitive choice for a precious mineral is gold, and gold sits close enough to
  ART_DIRECTION §2's reserved bright yellow-orange to be a bad habit to start. **If a designer wants
  Shards to read as gold, §2 has to say so explicitly**, the way it would have to for the interaction
  markers in §24g.

**Open question:** the shrine's stub currently says "THE SHRINE IS COLD. NO SHARDS TO SPEND YET."
Once Shards can be earned but the Hub Stat System still is not built, that line becomes wrong in a new
way — it will be shards you *have* and cannot spend. Whoever builds the award should revisit it.

### 24i. The tent is now the Codex, and the descent is played rather than cut to

Two design-touching decisions from the polish pass (owner, 2026-09-08).

**PROPOSED — the Codex lives in her tent.** The tent was inert scenery and, at 3.6 x 3.5 world units,
the largest object in the camp; the owner's note was that it is "too big for decoration" and should
be removed if it will never do anything. It now does: CORE_SYSTEMS §15 banks **Memory Fragments**
into a Hub **Codex**, and MVP §29 lists a Codex UI stub as MUST SHIP. Nothing in the design says
*where* the Codex is, so putting it in her tent is a placement invention — a small one, and the
alternative was deleting a fixture the Hub will need a home for anyway. It is stubbed exactly like
the shrine, answering "NO MEMORIES RECOVERED YET."

**The Hub now has four stations**: weapon rack (works), mine shaft (works), stat shrine (stub) and
Codex (stub). The Relic Vault, the fifth thing GDD §UI lists on the Hub Screen, still has no fixture.

**The descent is a played sequence, not an instant load.** Pressing E at the shaft locks input, walks
her into the pit, sinks her behind the brick lip and fades to black before the run loads — about 1.2
seconds. No design doc describes a Hub→run transition at all, so its existence and its timings are
invented and listed here with §24e's other unsourced numbers: approach 0.45s, sink 0.7s over 1.6
units, fade 0.55s.

It is worth a designer's attention because **it is the one moment the Hub exists to set up** — the
handoff from safety to the descent — and a hard cut reads as the game glitching rather than as going
underground. If the narrative layer (`Design/10-NARRATIVE.md`) ever wants a line, a look back at the
camp, or Zyno's voice on the way down, this sequence is where it goes.

---

## 25. The run is playable end to end, on placeholder bosses (owner, 2026-09-08)

The owner asked for two things: rooms are too big ("we only use first half of the room"), and a
descent that means something when a room is cleared. Placeholders for all bosses so a run can be
finished, and the floor system built so the room types that do not exist yet drop in later.

A run now goes Hub → Floor 1 → 3–5 rooms per floor → Mini-Boss on floors 5, 10 and 15 → Floor 16's
two fights → a Death/Victory screen → back to the Hub with Shards. **Nothing in the locked design
changed**; what follows is what engineering had to invent because no doc specifies it, plus the
gaps the placeholders leave.

### 25.1 Invented — every room dimension in the game

**No design doc gives a room a size.** LEVEL_DESIGN §2 describes room *character* ("at least 2
viable player positioning zones"), §4 says a Wave Room "needs larger open space", and §6 wants a
"large open arena" for a Mini-Boss — all relative, none numeric. The numbers were therefore invented
when the first room was built, and have now been invented again, smaller:

| Room | was | now | world footprint |
|---|---|---|---|
| Combat Room ×6 | 28 × 16 | **16 × 10** | 26.0 × 13.0 |
| Wave Room | 32 × 18 | **20 × 12** | 32.0 × 16.0 |
| Secret Vault | 22 × 16 | **16 × 10** | 26.0 × 13.0 |
| Mini-Boss arena | — | **20 × 14** | 34.0 × 17.0 |
| Final Boss arenas ×2 | — | **22 × 16** | 38.0 × 19.0 |

The arithmetic a designer needs: on the isometric grid a `w × h` map draws a diamond **`(w+h)` wide
by `(w+h)/2` tall**, so the footprint depends only on the sum. The camera shows **28.4 × 16** world
units. The old Combat Room was 44 × 22 — a room and a half wide — which is why the owner could only
ever see part of one.

**The relative order §2 and §6 ask for is preserved**: a standard Combat Room is the smallest fight
room, the Wave Room is larger than it, and the boss arenas are larger again. Only the absolute
numbers moved.

**A tuning question, not a design one** — but worth a designer's eye, because room size sets fight
density, and BALANCE §8's 30–60s clear target for a standard Combat Room has not been re-measured
against a room a third of the old area.

### 25.2 Invented — the composition of five new fights

Combat Rooms 2–6 needed encounters. Each is one batch of at most six bodies (the peak concurrency
room 01 already proved readable) for **145–170 HP** against room 01's authored 150, which keeps all
six in BALANCE §8's 30–60s band. What varies is composition, matched to the layout: the
ranged-leaning fight in the room with the long approach, the swarm in the room ringed with cover.
BALANCE §8 gives a clear-time target and no per-room roster, so these are engineering's.

### 25.3 Invented — everything about a boss except its HP

Five placeholder bosses exist: **The Collapsed King, The Drowned Custodian, The Molten Sentinel, The
Depth Warden and Zyno**. Each is a prefab variant of the Tunnel Brute with its own stat asset, scaled
1.6× and tinted — the technique ART_DIRECTION §4 already uses for the Deep Warden Elite.

**HP is BALANCE §6 verbatim**: 350 / 450 / 600 / 1200. **Zyno's is a choice this pass made** — §6
says it "reuses an existing Mini-Boss's HP/phases for MVP (specific choice TBD)", and a TBD cannot
be built, so it takes the Molten Sentinel's 600. That choice is the designer's to confirm or change.

Everything else is invented: move speed 2.1, attack cooldown 2.2s, attack range 2.6, stop distance
1.6, and an **aggro radius of 24** — the one number that is not taste, because at the Brute's 12 a
boss stands still in a 34 × 17 arena until the player walks most of the way to it.

### 25.4 CONFLICT — no boss has phases or a weapon-check

BALANCE §6 gives every boss a phase count (2 / 2 / 3 / 3) and a phase-transition trigger, and
CONTENT_DESIGN §5 gives every one a weapon-check moment — the Collapsed King's rubble shield that
the Greatsword breaks in one hit, the Drowned Custodian's snipeable homing projectiles, the Molten
Sentinel's Hyper Armor window, the Depth Warden's Phase 3 check. GDD §Bosses calls that moment the
point of a boss: "just a moment where weapon choice matters at peak tension."

**None of it exists.** There is no boss phase system, and each of these fights is one large enemy in
a locked room that chases and slams. The room type, the arena, the sequencing and the run's ending
are real; the boss design is not. This is the largest single gap the pass leaves and it is flagged
here rather than quietly logged as done.

Related: LEVEL_DESIGN §6 calls Floor 16 "the only room in the game that changes its own geometry
mid-fight". `FinalBossArena_01` and `_02` are flat boxes that do not.

### 25.5 DECIDED — Biomes 2 and 3 are Upper Caves in a different colour

Owner's call, taking "all 16 floors" over "Biome 1 only". Flooded Tunnels and Molten Depths have
themes and tiles but **no room layouts and no enemy roster** (CONTENT_DESIGN §5 defines both rosters;
neither is built). Floors 6–15 therefore draw the Upper Caves' seven layouts and its three basic
enemies, dressed in the deeper biome's theme, and floors 6–10 / 11–15 differ from floors 1–5 only in
how they look.

Expressed as data — two pool assets listing the Upper Caves rooms — rather than as a fallback in
code, so building the real content is an Inspector change. A designer reading "Biome 2 is playable"
should read it as "Biome 2 has a colour", not as content.

### 25.6 The Secret Vault is now *declared* and still not on the route

§22.2's conflict is unchanged: CORE_SYSTEMS §8 calls a Secret Floor a *detour*, and a detour needs
the branch LEVEL_DESIGN §1 rules out. What changed is only that the vault now has a named slot
(`RoomRole.SecretVault`) on all three biome pools, filled and never drawn. The seam exists; the
design question is untouched and still needs an answer.

The same is true of the **Trapped Soul Room** (CORE_SYSTEMS §14, LEVEL_DESIGN §2): its role is
declared, no room fills it.

### 25.7 Invented — the run-end screen's wording and its delay

GDD §UI specifies the Death/Victory screen's contents exactly — "depth reached, Shards earned, run
time, weapon used, Return to Hub button" — and all five are on it. What it does not give is the
words or the timing: the titles **"YOU DIED"** and **"DESCENT COMPLETE"**, the 1.1s pause between the
killing blow and the screen (long enough for the death animation and the last hitstop to read as the
end of a fight rather than a cut), and the screen's own geometry, are engineering's, on the same
footing as §23.2's card measurements.

One design-touching detail: **it is one screen for both endings**, differing in its title and its
accent colour. GDD §Game Loop 7 has two outcomes reporting the same five numbers, and BALANCE §14
pays identically on "death or victory".

### 25.8 A death currently draws her standing up

`CharacterState.Death` falls back to `Idle`, because ART_DIRECTION §3's death frames do not exist for
the protagonist (the enemies have theirs). At 0 HP she stops moving, her collider goes off and the
screen opens over her — she does not fall over. A placeholder, and the fallback is a single line to
delete once the art lands. Noted because "the player dies" now reads as a finished feature and its
most visible part is missing.

### 25.9 Not a design matter, but the designer should know

The pass found three pre-existing bugs, all fixed, and one of them is worth a designer's attention
because it explains a complaint: **the entry trigger that springs a room was sized to the axis-aligned
bounding box of a diagonal band**, so it covered roughly twice the intended area and reached into
both halves of the room. Fights were springing earlier and from further away than any map looked like
it should — which is a large part of what "we only use first half of the room" was describing. Room
pacing measured before 2026-09-08 was measured against a trigger twice the authored size.

---

## 26. PROPOSED — a new effect-category taxonomy for the icon-led offer card (owner, 2026-09-17)

**Not approved.** The offer card was rebuilt icon-led: a tier pip badge in place of the spelled-out
tier word, and a small effect-category glyph plus a compressed value/detail line in place of the full
prose description. That glyph needed something to key off, and no design doc defines one, so
engineering invented a 12-value `UpgradeCategory` enum (`Scripts/Upgrades/EffectSummary.cs`): Health,
Defense, Damage, OnHit, Movement, Dash, Experience, HeavyStrike, Combo, Gauge, Ultimate, Utility.

**This is a different axis from `CONTENT_DESIGN` §2a's `Category` column**, and the doc comment on
the enum says so explicitly rather than letting the two quietly disagree. §2a's column answers "which
pool/sub-pool is this drawn from" (Shared/Katana/Bow/Greatsword, or a tag like "Build-defining");
`UpgradeCategory` answers "what does picking it up change on screen". They agree for most of the 46
entries and diverge on three, all Katana-only:

| Entry | §2a's `Category` | This taxonomy | Why they diverge |
|---|---|---|---|
| Windcutter | Build-defining | `Damage` | It grants +15% Basic Attack range and +1 pierce — a damage-shape effect, whatever pool it draws from |
| Deathmark | Build-defining | `HeavyStrike` | The bonus applies to "the next Heavy Strike" specifically |
| Thousand Cuts | Alt Ultimate | `Ultimate` | The one entry where the two axes actually agree — flagged anyway so the table is complete |

Two more judgment calls worth recording alongside the mapping:

- **Thorns is tagged `Defense`, not `Damage`**, even though its effect (reflect 25% of damage taken)
  deals damage back to the attacker. It follows §1's own Survivability placement rather than what the
  number happens to do mechanically.
- **Overclock (Curse: +20% attack speed) has no clean bucket in §1's six headers.** Assigned `Damage`
  for lack of a better fit — attack speed is a damage-rate effect, but it is not damage the way Heavy
  Hands or Executioner are. Flagged as the one genuinely arbitrary placement in the set.

**The Greed's Toll cost line is a wording decision, not a taxonomy one, and is recorded here because
it was made in the same pass.** `BALANCE` §11 already flags this Curse as broken rather than tuned —
its cost was a faster Rising Hazard, and the Hazard was cut (§12 above). The card's compressed cost
line keeps that admission near-verbatim ("Cost pending: its Hazard was cut" — trimmed 2026-09-18 from
"Cost pending - the Hazard it paid was cut", which overflowed the card; see the last addendum below)
rather than shortening it to something like "No cost", which would assert the Curse is pure upside —
a design claim engineering does not get to make on its own.

**Not covered here, and not needing its own entry:** the individual `Value`/`Detail` strings on all 46
entries. Every one is a compression of the `description`/`upside` text CONTENT_DESIGN and BALANCE
already authored, not a new number or a new claim — the full mapping lives in `Scripts/Editor/
UpgradeCatalog.cs`, reviewable there. Two compression rules worth the designer knowing about, since
they explain why some cards read the way they do rather than the way BALANCE writes them: **per-tick
DoT values stay as authored** ("3 x 3", never the product "9", for Bleeding Strikes and Venom Edge —
collapsing to a total would publish a number no doc states), and **an upgrade that grants a stacking
bonus on top of a running total shows only what that pick grants** in the big `Value`, with the total
demoted to the small `Detail` line (Gauge: Bloodrush shows "+4%" / "Basic Attack, 12% total", not "12%"
as the headline).

**What this needs from a designer:** a ruling on whether the three-entry disagreement with §2a is
acceptable as a permanent two-axis system, or whether one of the two columns should be renamed to stop
implying they are the same question. Nothing about this pass changes a number, a mechanic, or which
pool an upgrade draws from — it is a presentation layer over already-authored content.

**Follow-up flagged, not fixed:** `Scripts/UI/WeaponCard.cs`, the Hub's weapon-select card, still
prints the weapon's full prose description. It was out of scope for this pass and will now read as
visually inconsistent next to the icon-led offer card — worth a follow-up pass once (or if) the same
treatment is wanted there.

**Addendum, 2026-09-17 — the card's badge layout and description order finalized, owner-directed.**
Three changes to `UpgradeCard.cs`/`BuildUpgradePanel.cs`, on top of the still-open taxonomy question
above:

- **The category glyph now owns the card's top-center band** — the thing worth seeing first —
  instead of overlapping the icon's bottom-right corner. **The tier pip badge moved to a small
  top-left corner accent** to make room, since it was already redundant with the tier-coloured
  frame. Neither move touches the taxonomy question §26 raises; it is a position change only.
- **`Value` and `Detail` collapsed into one line**, `"<Detail> <Value>"` (e.g. "Dig-Dash cooldown
  -20%"), replacing the old two-line stack that read the number before what it modified. This freed
  exactly the vertical room the category glyph's larger band needed, so the card kept its 168×196
  size.
- **The combined line drops from 14pt to 7pt when it doesn't fit one line at 14pt** — both sizes are
  on-grid multiples of the packed pixel font's native size, chosen over Unity's continuous best-fit
  specifically to avoid resampling the bitmap font off-grid (see CLAUDE.md's HUD scale-factor
  rules). **In practice this means nearly every entry renders at 7pt**: the card's label is 148
  units wide, and 14pt's monospaced advance (14 units) only fits 10 characters — every sampled
  entry in play mode ("Dig-Dash cooldown -20%", "XP from enemies +20%", "Damage dealt +40%") was
  well over that and rendered small. The two-tier switch is implemented and verified correct, but a
  designer should confirm uniformly small description text is the intended look rather than the
  occasional-shrink the owner asked for — the alternative is dropping the two-tier system for a
  single fixed size, or authoring shorter `Detail` strings across the 46 entries so more of them
  clear the 10-character bar.

**Addendum, 2026-09-18 — the card rearranged to an owner mockup, and the 14pt flag above largely
answered.** The owner supplied a drawing of the card and named what each region holds. Presentation
only: no widget added or removed, no content edited, the card still 168×196, and the taxonomy question
§26 opens is untouched.

- **The category glyph and the tier badge now share one left-aligned top row** — glyph at the card's
  top-left corner, badge beside it — replacing yesterday's arrangement of a centred glyph with the
  badge tucked into the corner behind it. The badge art was re-emitted at 64×16 (from 32×8) so it
  reads as the tier-coloured line the mockup asks for rather than a sliver.
- **The description is now a block, not a line, and it wraps.** This corrects a real defect rather
  than a preference: the single line did not wrap, held 21 characters at 7pt, and the seven authored
  entries over 30 characters — Overwhelm's "Per hit, cap 5. Resets on a miss +2%" among them — were
  drawing about 30 units past *each* edge of the card. Nobody had looked at those entries on a card.
- **Two title lines instead of one largely answers the flag above.** "Max HP +15" and "All attacks +3"
  now render at 14pt; the long entries still drop to 7pt, which is what the small size is for. So the
  two-tier switch behaves as the owner originally described — an occasional shrink — rather than as a
  uniform small. **One part of that flag stands:** all eight Curse upsides are 16 characters or more
  and a Curse gets only the first of the block's two lines (its cost mark and cost line take the
  rest), so **every Curse card still renders its upside at 7pt**. Whether that is acceptable, or
  whether the eight upside strings should be compressed to 10 characters so a Curse's upside reads as
  loudly as an upgrade's, is a designer call.
- **`ART_DIRECTION` §5 is not contradicted by any of this.** It fixes the tier colour coding and asks
  that the Curse card be "visually distinct with a red/black treatment"; both are unchanged, and it
  says nothing about where on the card anything sits. Recorded here as an invented arrangement, not a
  conflict.

**Flagged in passing — four authored strings render with a lowercase x.** `PixelFontGlyphs` authors a
real lowercase `x` (it is needed for "2x damage taken", "3 x 3", "2 x 4") while every other lowercase
letter aliases onto its capital. So Vitality's detail `Max HP` draws as `MAx HP`, and at 14pt on the
new block it reads as a typo. The others are Blood Debt's cost line ("-20% Max HP for the rest of the
run") and the names `Executioner` and `Explosive Finish`. The fix is to author those four with a
capital X — a content edit to authored strings, which is why engineering flagged it rather than
making it. **Resolved 2026-09-18 a better way — see below.**

**Addendum, 2026-09-18 — all 46 compressed lines re-authored, because collapsing them onto one line
had broken about twenty of them.** Owner-reported, after seeing the restyled cards.

**This is a consequence of the 2026-09-17 decision recorded two paragraphs above, and it is the kind
of thing this brief exists to catch.** `UpgradeCard` renders `Detail + " " + Value` — the number
always lands last. Those pairs were authored for the *two-line* card, where a big `Value` sat on its
own line above a small `Detail`, so two numbers stayed visually apart. Collapsing them onto one line
turned about twenty into nonsense, and nobody re-read the set afterwards:

| Entry | What the card actually drew |
|---|---|
| Static Discharge | `Arcs within 3.0 4` |
| Venom Edge | `Poison, stacks to 5 2 x 4` |
| Gauge: Bloodrush | `Basic Attack, 12% total +4%` |
| Windcutter | `Attack range, pierce +1 +15%` |
| Iron Curse | `To knockback Immune` |
| Gambler's Edge | `For the rest of the run 4th Card` |
| Blood Debt | `Heal now Full HP` |
| Overwhelm | `Per hit, cap 5. Resets on a miss +2%` |
| Momentum Edge | `Stack cap 10-14` — reads as a range, means "10, raised to 14" |

**What the designer needs to know:** no number, mechanic, tier, category or pool changed. Every
rewrite was checked against that entry's own `description`/`upside`, which are untouched and are still
what `UpgradeStatusReport` and every other reader goes through. §26's standing position — that these
compressed lines are a presentation layer over content `CONTENT_DESIGN` and `BALANCE` already
authored — is unchanged; this is the same compression done again, correctly. The two compression
rules §26 records both still hold: Bleeding Strikes and Venom Edge still publish per-tick × ticks
("3 damage x 3 ticks") and never the product, and Gauge: Bloodrush still leads with what the single
pick grants and trails the running total.

Two wordings worth a designer's eye, both kept deliberately:

- **Overwhelm** now reads `+2% per hit, cap 5, resets on a miss`. §26 flags that dropping either
  "cap 5" or "resets on a miss" misstates the stack; both survive, at the cost of being the longest
  upgrade line in the set.
- **Momentum Edge** now reads `Stack cap 10 to 14` rather than `10-14`. The HUD face has no arrow
  glyph (§26's note on the glyph set still applies), and the hyphen was being read as a range.

**The lowercase-x flag above is closed, by a different route than the one proposed.** Rather than
authoring four strings with a capital X — which would have put `EXecutioner` into data every prose
reader shares — `UpgradeCard` now uppercases its three labels at draw time. The HUD face is an
uppercase bitmap, so this changes nothing anywhere else, and the multiplier strings keep their
meaning: "2x damage taken" draws as `2X DAMAGE TAKEN`.

**Not a design matter, but the designer should know:** verifying this pass found that the Curse
card's cost mark has never drawn its art. `HUD_CostMark` is generated, but `BuildUpgradePanel` builds
that `Image` with a null sprite and nothing assigns one, so UGUI draws a solid quad — a plain crimson
bar where a rule-and-triangle should be. Same defect class as the white weapon slot in
`01-VERIFICATION.md` §4. Flagged for a one-line engineering fix, not a design question.

**Addendum, 2026-09-18 — one description size on every card, owner-directed. Closes the 14pt/7pt
flag above.** The owner saw an offer where Executioner's line drew at 14pt beside three cards at 7pt
("the font sizes are changing") and asked for one size. **The two-size switch is gone**: every card's
description, name and cost line now draws at 7pt, the face's native size. 14pt was not a candidate
for the single size — it is the only other size that stays on the pixel grid, it holds 10 characters
to a line, and most of the 46 lines cannot be said in 20 characters (one word alone,
`INVULNERABILITY`, is 15). This answers both open questions the earlier addenda left for a designer:
uniformly small description text *is* the intended look, and the eight Curse upsides no longer need
compressing to match the upgrades, because nothing renders louder than they do now.

**Every line was then checked against the one size**, through UGUI's own text generator on the real
card, not by estimate: all 46 fit. Nothing needed rewriting except one, which was already visibly
broken in the owner's screenshot:

- **Greed's Toll's cost line** is now `Cost pending: its Hazard was cut`, from
  `Cost pending - the Hazard it paid was cut`. The old line wrapped to three lines in a box that
  holds two, and the third drew across the card's bottom border. Same admission, same refusal to
  claim the Curse is free — "its" carries what "it paid" did. `Downside`, the prose every other
  reader uses, is untouched.

The asset build now warns on any line that wraps past its box (one line for a name, five for an
upgrade's summary, two each for a Curse's upside and cost line), so a future edit that overflows is
caught when it is authored, not when it is seen.

---

## 27. DECIDED — a level-up beat between the pause and the offer (owner, 2026-09-18)

**Owner's note:** "when level up suddenly rogue like upgrade panel is enabled it's a bit shocking maybe
we need a vfx for level up thing." Until now the offer came up on the same frame as the killing blow
that earned it — full-speed fight, then a frozen full-screen panel, with nothing between. The owner
chose both the shape of the fix and its art source when asked:

1. **The fight slows to a stop instead of cutting to one.** `RunPause` gained an eased entry: time
   scale follows `(1 − p)²` from 1 to 0 over 0.45 real seconds. Only ≈0.15s of game time passes, so
   enemies and her animation visibly wind down rather than lurching. The owner picked this over an
   instant freeze followed by the effect.
2. **A burst of light plays on her** — `LevelUpVFX`, a generated flipbook drawn behind her body.
3. **The panel makes an entrance.** The scrim fades up, the heading follows, and the cards rise into
   place one after another (`OfferReveal`). A second queued offer re-deals its cards without
   re-fading the scrim.

**How it reads against locked design.** CORE_SYSTEMS §12 — "on level-up: game pauses, upgrade panel
opens" — is still true, with about 0.6s between the two instead of none. Input stops, and the pause
counts as held, **on the first frame**: the ease is presentation, not a window to keep fighting in.
ART_DIRECTION §6's must-have VFX list has no level-up effect; this adds one at the owner's direction.

**Invented numbers, all serialized:**

| Where | Value | What it is |
|---|---|---|
| `UpgradeOffer.levelUpSlowMo` | 0.45s | real time the ease to zero takes |
| `RunPause.easeExponent` | 2 | curve shape — fast drop, crawling tail |
| `UpgradeOffer.levelUpRevealDelay` | 0.6s | level-up to panel entrance; coupled to the burst's length |
| `LevelUpVFX.framesPerSecond` | 14 | burst playback |
| `OfferReveal` | scrim 0.2s, heading from 0.08s over 0.15s, first card at 0.14s, 0.06s stagger, 0.18s per card, 10-unit rise | the entrance |

**Palette.** The burst is pale champagne gold into warm off-white with cool violet edges, generated
against a forced palette of colours the game already draws: the Legendary tier gold, the level
badge's own level-up flash, the Upper Caves ochres and violet-greys, and the Epic violet. **Bright
yellow-orange is kept out on purpose** — style guide §3 reserves it for hazard telegraphs, and a
level-up reading as danger is the conflict that rule exists to stop. The placeholder `HitFlash.png`
does carry a saturated yellow (253,197,2); that is placeholder art and was not copied.

**Deliberately not done:**

- **No i-frames during the ease.** About 0.15s of game time passes; granting invulnerability for it
  would be a mechanic, not presentation. The known edge is that a hit already landing in that window
  can still kill her, and the offer can then come up alongside the death screen.
  `RunSummaryPanel.ReturnToHub` already force-resets the time scale for exactly that case (an offer
  open when she died), so nothing is stranded.
- **No ease back out.** Combat resumes on the frame of the pick, as before. A symmetric ease-out
  would delay the player's control for a transition nobody complained about.

**Adjacent defect, fixed in the same pass.** ART_DIRECTION §6's pick flash (white/gold, red for a
Curse) was built *inside* the panel, and the panel is switched off on the pick. So the flash never
showed after the last offer of a level-up, which is every offer that is not followed by another. It
now sits beside the panel rather than inside it.

---

## 28. The HUD's corners are regrouped, and the upgrade readout leaves the play screen (owner, 2026-09-20)

Four changes, one pass. The driver is the second one; the rest followed from it.

### 28.1 CONFLICT — HP, XP and the level badge share the bottom-left corner

ART_DIRECTION §5 puts HP top-left and XP top-right. They are now together, **bottom-left**, with the
level badge at the far left of the pair. The owner's reasoning: those are the numbers that say how
the run is going, and splitting them across two opposite corners made the eye travel for one reading.

What did **not** move, and deliberately:

- **The Ultimate Gauge, the dash slot and the weapon icon stay bottom-centre.** Those are what she
  *spends*; the cluster is what she *has*. Piling all six into one corner would have made the corner
  the HUD.
- **The depth readout keeps the top-right corner** the XP bar vacated. It is the only number on
  screen about the descent rather than about her.

§5's corner assignment is therefore wrong as written. Nothing else in it changed — the bars, their
colours, the chase bar and the segment ticks are all as they were.

### 28.2 CONFLICT — the in-run upgrade readout is off the play screen entirely

> **This one contradicts locked design, and it is the most important line in this section.**
> `GDD §UI`'s HUD list — as rewritten by the design owner on 2026-09-18 — includes *"a faint strip
> listing the upgrades taken this run (tier-colored, for reference)"*. That element has now been
> **removed from the in-run HUD** at the same owner's direction. The strip was folded into the GDD
> five weeks after it was built and two days before it was taken out again; this is a Rule 14 reopen
> of a clause that has never been wrong, only overtaken.

The strip recorded further up this document is gone from the HUD. The owner's reason, verbatim in
substance: *"We'll have a lot of upgrades so if we put it on main screen it would be a problem."*

A run has **no cap on how many upgrades it takes and no max level**, so a column of sockets down the
edge of the screen either grows off it or starts lying through its overflow count. The readout now
appears only where the game is already stopped:

- **The pause menu** (§28.3), as a grid of 48 sockets — the complete view.
- **The level-up offer screen**, as a compact row of up to 24 sockets along the top band, labelled
  `CARRYING` — the glance-sized view, so a pick is made against what the run already holds.

**It is icons alone, with a hover popup** (owner-directed): the socket shows the pick's art in its
tier colour, and pointing at it opens a plate with the upgrade's **name as a header and its
description underneath**. A Curse shows its upside with its cost under it in the Curse red. No new
text is authored for this — the popup draws `UpgradeDefinition.description` and
`CurseDefinition.upside`/`downside` verbatim, which is the same prose CONTENT_DESIGN writes and
`UpgradeStatusReport` reads. The offer card's compressed `EffectSummary` line is deliberately not
reused: that line exists because a card is 21 characters wide, and the popup is not.

**What each doc says now, precisely:**

| Clause | Doc | Status after this pass |
|---|---|---|
| "a faint strip listing the upgrades taken this run (tier-colored, for reference)" in the HUD list | `GDD §UI` | **Contradicted.** No such element is on the play screen. Its content moved to the pause menu and the offer screen. |
| HP top-left, XP top-right, Ultimate and weapon bottom-centre | `ART_DIRECTION §5` | **Contradicted** for HP and XP — see §28.1. The rest holds. |
| A hover tooltip, anywhere | — | **Not in any doc.** No tooltip exists elsewhere in the project either. |
| A pause screen | — | **Not in any doc.** See §28.3. |
| An always-visible readout of taken picks on the offer screen | `ART_DIRECTION §5`, `GDD §UI` (Upgrade Screen) | **Not in either.** Both describe the three cards and the Curse card only. |

The *reason* the readout exists is unchanged and still undocumented: a roguelike run is defined by
its picks, and the player has no other way to check what they took twenty minutes ago.

### 28.3 DECIDED — the run has a pause menu, on Escape

`Scripts/UI/PauseMenu.cs`, `Scripts/Editor/BuildPauseMenu.cs`. A scrim, a `PAUSED` header, a button
column of **RESUME** and **ABANDON RUN**, and the upgrade grid beside it. It takes a `RunPause` hold
like every other modal, so time, the Player action map and the hardware cursor all move together.

**GDD §UI lists no pause screen.** The MVP tiers do not mention one either. It is here because the
upgrade readout needed somewhere to live and a roguelike with no pause is its own problem.

Three engineering notes the designer may want to know:

1. **Escape is read off the keyboard device, not through an `InputAction`.** `RunPause` disables the
   whole Player map while it holds, so a key bound through the shared asset would switch itself off
   the instant the menu opened. The sandbox's debug menu already does it this way.
2. **It refuses to open over the level-up offer or the run-end screen.** A level-up is a forced
   choice, and once the run is over there is nothing left to pause.
3. **The grid holds 48 sockets, which cannot overflow today.** `RunUpgrades` refuses duplicates and
   the authored content is 38 upgrades and 8 Curses, so 46 is the hard ceiling a run can reach. The
   overflow count is kept as the thing that tells the truth if that content ever grows.

### 28.4 PROPOSED — abandoning a run pays out as a death

**ABANDON RUN ends the run through the same path dying does** (`RunEnd.Finish(Died)`), so BALANCE
§14's Shard award for the depth reached is paid exactly as it would have been had she died there.
The run-end screen then reads `YOU DIED`. It takes two clicks: the first swaps the button to
`CONFIRM?`.

**No design doc specifies abandoning, so this is invented.** The alternatives, for the designer to
rule on:

- **Pay as a death** (what is built). Quitting a bad run costs the player their time and nothing
  else, which is the forgiving reading and the one that needs no new outcome.
- **Forfeit the Shards.** Makes quitting strictly worse than fighting on, which is the reading that
  protects the descent's tension. It needs a third `RunEnd.Outcome`, a summary screen that reads
  `ABANDONED`, and a decision about whether a player who abandons at Floor 15 really earns nothing.
- **No abandon at all.** The only exits stay dying and winning.

A second question inside it: the summary screen currently says `YOU DIED` for an abandoned run,
which is a small lie. It is left that way rather than inventing a third piece of screen wording.

### 28.5 Not a design matter, but the designer should know

- **The pool will run dry on a long run.** `RunUpgrades.Add` refuses duplicates, and the authored
  content is 38 upgrades plus the equipped weapon's sub-pool. With levels uncapped, a long enough run
  exhausts what can be offered and the draw has nothing left to show. This is not new and this pass
  neither causes nor fixes it — but the owner's "we don't limit upgrade count or max level" makes it
  reachable rather than theoretical. It needs either stacking upgrades (CONTENT_DESIGN does not say
  whether they stack), more content, or a level cap.
- **Two new HUD pieces were drawn, not generated:** `HUD_Tooltip`, the popup's plate, which is the
  offer card's plate at 168×86; and nothing else — the grid and the strip reuse `HUD_SlotSquare` and
  `HUD_SlotUpgrade`, which already existed.
