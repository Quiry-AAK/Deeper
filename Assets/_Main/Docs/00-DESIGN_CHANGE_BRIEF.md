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

**Where to start.** Most of this page is documentation catching up with decisions already made. Three
items are not, and are worth taking first because they change how the game plays rather than how it is
written down: **§7h** (the Ultimate destroys the Combo Counter and returns nothing — a live gameplay
hole), **§3/§5/§6** (the premise, the Final Boss and what the game is after the story ends — one
coordinated decision, not three), and **§7g** (a flat gauge fill has deleted the per-weapon pacing
difference and left an upgrade with no job).

**Last refreshed** 2026-08-15, when the Rising Hazard was cut (§12). Before that, 2026-08-14, when the
designer's session changelog was applied to `Design/` (§11).
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
