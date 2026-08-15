# CORE SYSTEMS — "Deeper"

Technical breakdown of every system described in 01-GDD.md. This document defines *how* each system works — interfaces, state flow, data shape — not the specific numeric values (those live in 04-BALANCE.md) or content tables (those live in 03-CONTENT_DESIGN.md).

---

## 1. Weapon System

All 3 weapons (Katana, Bow, Greatsword) implement a shared interface so the rest of the game (animation triggers, upgrade system, HUD, Ultimate Gauge) never needs to know which weapon is equipped.

**Shared interface — `IWeapon`:**
- `BasicAttack()` — triggers the LClick swing/shot. **Built as a 2-hit chain that loops** (reuses `AttackStateMachine`'s cancel-window pattern already defined for Heavy Strike, §3): each hit re-enters Windup→Active→Recovery, and the chain breaks unless the player presses again within a 0.25s window. Free from the start, not upgrade-gated.
- `HeavyStrike()` — triggers the RClick action (base: single hit; upgradeable, see §3)
- `Ultimate()` — triggers the R action, only callable when Ultimate Gauge is full (see §4). **Branches on weapon data rather than assuming an attack** — `WeaponDefinition.UltimateShape` is `Attack` or `Buff`. Katana is `Buff`; Bow and Greatsword are `Attack` until design says otherwise.
- `OnHitLanded(target)` — callback fired whenever any of the above connects; feeds the Ultimate Gauge and the weapon's Signature Trait system
- `GetAttackTiming()` — returns per-weapon Windup/Active/Recovery frame data (values in BALANCE.md)

**No inventory or armor system.** The player never carries, equips, or swaps gear. A run is one weapon, chosen in the Hub, locked until the run ends — this was already true of the interface above and is now also true of the game as a whole (armor/helmets return post-launch as pure cosmetics, no stats, no slots).

**Per-weapon implementation notes:**
- **Katana** — melee arc hitbox, short Windup/Recovery, feeds Combo Counter (§5a)
- **Bow** — spawns a projectile (reuses enemy-projectile hit-detection component), variable Windup driven by Charge Shot hold-time (§5b)
- **Greatsword** — melee wide-arc hitbox, long Windup/Recovery, grants Hyper Armor during Windup (§5c)

Weapon is selected in the Hub, stored on the run-state object, instantiated once at run start. No runtime weapon-swapping — this keeps the attack state machine simple (Rule 2: reuse, don't branch).

---

## 2. Attack State Machine

Every weapon action (Basic, Heavy Strike, Ultimate) runs through the same 3-phase state machine:

```
IDLE → WINDUP → ACTIVE → RECOVERY → IDLE
```

- **Windup:** wind-up animation plays, no hitbox active, input locked into the action (can't cancel except via Dash-Attack Cancel, see below)
- **Active:** hitbox/projectile live, damage can be dealt
- **Recovery:** cooldown-style lockout before next action, no hitbox

**Dash-Attack Cancel:** During Recovery only, pressing LShift cancels the remaining Recovery frames early and transitions directly into the Dig-Dash state. Not available during Windup or Active (prevents using it to skip commitment entirely — the whiff punish window on Greatsword stays real). No tutorial prompt; intended as discoverable tech.

---

## 3. Heavy Strike Modification Slot

Heavy Strike (RClick) is the primary in-run customization point. At base, it's a single stronger/slower hit per weapon.

**Modification types (delivered via the upgrade pool, §8):**
- **Chain Extension** — adds a 2nd or 3rd hit to the Heavy Strike sequence (each hit re-enters Active→Recovery before the next chain hit can trigger; chain breaks if the player doesn't press RClick again within a short window)
- **Full Replacement** — swaps the entire Heavy Strike behavior for a repurposed effect (e.g., Dynamite Throw: lobs a timed explosive instead of swinging; Grapple Pull: yanks an enemy toward the player). Replacement effects still register as `HeavyStrike()` calls for Ultimate Gauge purposes, so switching doesn't gimp gauge fill.

Only one Heavy Strike modification is "active" at a time — if a player takes a second Heavy Strike upgrade, it either extends the current chain (if compatible) or replaces the effect outright (design call per upgrade, defined in CONTENT_DESIGN.md).

---

## 4. Ultimate Gauge

Replaces a traditional cooldown. Purely resource-gated (Hades Cast-style pacing).

- Gauge range: 0–100%
- **DECIDED: per-weapon gauge fill is restored** (this had been built as a flat 1% for every weapon/action, deleting weapon differentiation — that's now reverted). Exact rates: BALANCE §4. **This is a build task** — the flat-1% version is what's currently in the codebase; it needs to be reverted to the per-weapon table.
- Taking damage also fills the gauge, **+1% at base, flat regardless of hit severity**. **DECIDED:** the "Gauge: Vengeance" upgrade (CONTENT_DESIGN §7/BALANCE §10) now stacks *on top of* that base — it grants an additional +2% on taking damage (3% total), rather than duplicating the base 1% for no gain.
- At 100%, `Ultimate()` becomes callable via R
- On activation, gauge drains instantly to 0% — no partial-use, no banking excess
- Gauge upgrades (from the pool) can further modify gain-per-hit, gain-on-taking-damage, or add a small passive trickle over time

**Ultimate effects per weapon** (full numeric tuning in BALANCE.md):
- **Katana** — a **Buff**, not an attack: a short cast raises an aura on her and the katana; for its duration she deals more damage, attacks faster, and moves faster, and every attack lands through the buff. Deals no damage of its own. **DECIDED: the buff does not touch the Combo Counter at all.** It no longer consumes or converts stacks on cast — the combo just keeps running through the Ultimate cast exactly as it would through any other action. This removes the old "Combo Finisher" framing entirely; the buff and the Combo Counter are now two independent systems that both key off landing hits.
- **Bow** — Full-Charge Piercing Shot: an instant max-charge Charge Shot that pierces all enemies in a line, no hold-time required
- **Greatsword** — Ground Slam: AoE hit centered on the player, knocks back and damages all enemies in radius

**Alt Ultimate (upgrade-gated):** Each weapon has one Epic-tier upgrade (CONTENT_DESIGN.md §2) that swaps `Ultimate()`'s implementation for a more mobile, skill-style variant instead of the default. Implemented as a swappable strategy on the `IWeapon` instance — taking the upgrade reassigns which Ultimate implementation `Ultimate()` calls; the gauge-fill and full-drain rules are unchanged regardless of which variant is active. **DECIDED: Katana's Thousand Cuts stays Attack-shaped**, not converted to a Buff. This gives Katana players a real choice between the default (empower herself, keep attacking normally through the buff) and the Alt (a mobile burst of real damage, no empowerment) — two genuinely different shapes rather than two buffs. Bow and Greatsword's Alt Ultimates remain Attack-shaped as before, unaffected by this question.

---

## 5. Weapon Signature Traits

Each weapon has one passive mechanical hook, always active regardless of upgrades taken (upgrades can amplify it, not remove it).

### 5a. Katana — Combo Counter
- Landing consecutive hits (Basic or Heavy Strike) without missing or taking damage increments a stack counter
- Each stack adds a small flat/percentage damage bonus (values in BALANCE.md)
- Resets to 0 on: a whiffed attack (no `OnHitLanded` in the Active window) or taking any damage
- Feeds directly into the Ultimate (§4)

### 5b. Bow — Charge Shot
- Holding LClick extends the Windup phase, scaling damage/pierce count with hold duration up to a cap
- Releasing early fires a fast, low-damage shot instead (no minimum hold required — this keeps Bow viable at close range in a pinch)
- Charge state is visually telegraphed (draw animation stages) so enemies can theoretically react — consistent with the game's "telegraphed damage" philosophy in Combat (GDD §Combat)

### 5c. Greatsword — Hyper Armor
- Active only during the Windup phase of any Greatsword action (Basic, Heavy Strike, or mid-chain)
- While active: knockback is nullified, incoming damage is reduced by a flat percentage (not zero — stays distinct from Dig-Dash's true invulnerability)
- Does not extend into Active or Recovery — the whiff punish window during Recovery remains real

---

## 6. Damage System

- Flat damage per hit — no crit system anywhere in the game (run or permanent). Permanent power growth is delivered instead through the Hub Stat System's Core Stats (flat/percentage bonuses) and Marks (unique named effects) — see §10.
- Damage sources: Basic Attack, Heavy Strike, Ultimate, enemy attacks, Hazard Kills, environmental hazards (geysers, scorched ground, currents — per biome)
- **DECIDED (per engineering recommendation):** `source` gets added to the built pipeline. Current build has `AttackHitbox.Landed(action, target, amount)` plus `Damageable.Damaged(amount)`, neither carrying `source` — that's fine today only because the player is the only damage source that exists. Milestone 4's on-hit upgrade procs and any future "damage dealt by X" upgrade need it, so both events gain a `source` parameter before that pool is authored. The single-event model this doc originally described (`OnDamageDealt(source, target, amount)`) stays aspirational/simplified for documentation purposes — the two-event split is what's actually built and stays built, just with `source` added to each.
- **Hazard Kills:** if an enemy is pushed/dashed into the Rising Hazard's leading edge, it's an instant kill regardless of remaining HP — implemented as a trigger volume check on the Hazard front, not a damage-system special case

---

## 7. Hazard System

One underlying timer-driven system, reskinned per biome (GDD §Biome Identity).

**Core loop:**
- A `HazardFront` object advances upward (or outward, depending on presentation) on a per-biome timer
- Reaching the player = instant death (or heavy damage + forced retreat, TBD in BALANCE.md playtesting)
- Timer speed increases per biome tier (Upper Caves slowest, Molten Depths fastest)

**Per-biome behavior layer (cosmetic + secondary effect, same underlying timer):**
- **Upper Caves:** visible/audible rockfall front; some floor tiles independently crack and collapse after a few seconds of player weight (separate micro-system, same "collapse" visual language)
- **Flooded Tunnels:** hazard is a rising water level; also triggers room-geometry changes (low-lying tiles become impassable/water-slowed as the level rises) — requires rooms to define "low" vs "high" tile zones in their layout data
- **Molten Depths:** hazard is a spreading lava flow that leaves a `ScorchedGround` trigger volume behind it after passing — deals light recurring damage to anything standing in it, distinct from the instant-kill front itself

---

## 8. Room & Wave System

- Each floor pulls **3–5 rooms** from a per-biome room pool via a **reshuffling bag**: the pool shuffles, is drawn through without immediate repeats, and reshuffles once exhausted. Floors will revisit Combat Room layouts within a single run — by design, this is what makes 30–60 min runs affordable without tripling room-authoring content.
- Room types: Combat, Secret (locked, key-gated), Trapped Soul (new — see §14), Mini-Boss (every 5th floor), Final Boss (floor 16). **Reward Room is removed** — its function (currency payout) no longer exists now that Shards are run-end only.
- **Room locking:** Combat Rooms lock entry/exit doors until all spawned enemies are defeated (existing logic, unchanged)

**Wave Rooms (variant flag on Combat Room prefabs, not a new room type):**
- A room flagged `IsWaveRoom = true` spawns enemies in 2–3 batches instead of all at once
- Next wave triggers when current wave count drops to ~1 remaining enemy (event-driven threshold check, not a timer — prevents stalling)
- Room-lock logic is unchanged: still locked until the *final* wave is cleared
- No new enemy types — Wave Rooms only resequence existing per-biome enemies
- Capped at 1–2 flagged rooms per biome's pool (pacing constraint, see LEVEL_DESIGN.md)

**Secret Floors:**
- A locked door prefab requires a `SecretKey` flag on the player's inventory, granted by defeating a rare elite spawn earlier in that biome
- Leads to a Vault Room (large XP payout or guaranteed Legendary-tier upgrade offer)
- No separate hazard override — entering costs real time against the same biome hazard timer, which is the entire risk/reward tension (no new system required)

---

## 9. Upgrade & Curse System

- On each level-up (§12): draw 3 upgrade offers in **one weighted draw across the combined pool** = shared pool (HP, XP, Speed, Dash) + weapon-specific sub-pool (Heavy Strike mods, Ultimate mods, Ultimate Gauge mods) matching the equipped weapon. Not floor-gated and not tier-gated — Common/Rare/Epic can appear in the same offer
- A 4th slot is always populated with a **Curse** — drawn separately from its own small Curse pool, always visible, never mandatory
- Every 5th level the whole offer is replaced by an **Evolution** offer instead (§13)
- Weighting can vary by depth/biome (e.g., certain upgrade tags weighted higher in later biomes) — same weighted-draw function, just a different weight table per biome, no new system
- Legendary-tier upgrades (Relics, §10) use the same draw pipeline but are excluded from normal weighted draws — only appear via Mini-Boss guaranteed drop, Secret Floor vault, or Relic Vault guarantee

---

## 10. Meta-Progression Systems

- **Shards:** awarded once, at run end (win or death), computed from **Levels Gained + Depth Reached** (formula in BALANCE §14). There is no in-level currency pickup and no in-run shop — the run's own resource is XP (§12), which is spent on nothing and only drives leveling. (This supersedes the Glimmer run currency: the rename survived one pass and the resource itself was then cut.)
- **Hub Stat System:** two tiers, both purchased with Shards, no dependency tree (Rule: bounded, not a skill tree). **Core Stats** are rank-based (Max HP, Base Damage, Move Speed, Ultimate Gauge Gain, XP Gain, Dash Cooldown). **Marks** are unique, mostly single-purchase named effects (Hades Mirror of Night-style — e.g., Death Defiance, Boiling Blood, Warm-Up) that create build-defining decisions rather than flat number lines. Plus two flat non-stat unlocks (extra Curse slot, Relic Cache) that sit outside both tiers. Full table in CONTENT_DESIGN.md §7.
- **Weapon Mastery:** a small per-weapon counter incremented by run-usage (not Shard spend) — e.g., "floors cleared with this weapon equipped" — crossing thresholds unlocks the 3–5 mastery nodes per weapon
- **Relics:** the Legendary upgrade rarity, one per weapon. First time a given weapon's Relic is offered and taken, it's flagged "discovered" on the save file. Discovered Relics become purchasable from the Relic Vault (Hub) at high Shard cost — purchasing guarantees that Relic appears as an offer once in the player's next run.

---

## 11. Boss Weapon-Check Mechanics

Not a separate system — implemented as boss-specific phase logic that reads the player's equipped weapon type and branches a single encounter parameter (e.g., shield HP threshold, projectile speed) accordingly. No new architecture; bosses already have phase/state logic, this just adds a weapon-type read at phase-transition checks.

---

## 12. XP & Leveling

- Enemies drop XP on death (value per enemy type — see BALANCE.md).
- XP accumulates toward a level threshold (curve TBD — open item, needs playtesting, not a fixed formula yet).
- On level-up: game pauses, upgrade panel opens. Offer = 3 cards drawn from the **combined shared + weapon-specific pool in a single weighted draw** (existing rarity weights per biome still apply — see BALANCE §13), plus the always-visible 4th Curse slot.
- **Every 5th level**, the normal offer is replaced by an **Evolution offer** (see §13).

---

## 13. Evolution Tiers

- At level milestones divisible by 5, present 2–3 **Evolution** choices instead of a normal upgrade offer. Evolutions replace a piece of the equipped weapon's kit outright (not a numeric buff) — e.g., Katana's Combo Counter evolving into a different stance system entirely.
- Evolutions are **mutually exclusive and locked for the run** — picking one closes out its sibling option for that run only (not permanently across runs).
- **MVP scope: 1 Evolution Tier per weapon** (one 5th-level milestone, 2 choices each). Additional tiers are Post-MVP.

---

## 14. Trapped Souls

- Certain rooms (flagged, reuse Secret-Vault-style footprint) contain a **bound soul** — a spectral figure the player can free via a short interactable, costing real time against the Hazard.
- Player has **2–3 soul slots per run**. Freeing a soul grants a persistent in-run effect (see BALANCE.md for the 3 soul types). A freed soul can be lost permanently for the run if it dies.
- **MVP scope: 1 soul type implemented**, 1 Trapped Soul room in Biome 1's pool. Remaining soul types are Post-MVP.

---

## 15. Narrative Systems (MVP subset)

Full story content lives in `10-NARRATIVE.md`; this is the system-level shape.

- **Whisper Layer:** Zyno has a persistent HUD/audio presence — short manipulative lines triggered by specific events (enemy kill, low HP, level-up, boss encounter start). **MVP scope: a minimal line set covering Biome 1 + the father fight only**, not the full script. Escalation/glitching behavior for later biomes is Post-MVP (depends on those biomes existing at all — currently SHOULD SHIP, not MUST SHIP).
- **Memory Fragments:** Findable, non-stat pickups that trigger a short forced vignette and bank into a Hub **Codex**, viewable regardless of run outcome. **MVP scope: pickup + Codex UI stub with a small number of Biome-1 fragments**; full fragment content is Post-MVP.
- **Refusal State:** Named encounters can expose a brief window where the player can hold back instead of attacking, triggering an alternate resolution beat instead of standard combat. **MVP scope: the father fight only.** Expanding Refusal State to other named encounters is Post-MVP.

### Story state & dialogue gating (from the earlier narrative pass — still applies)

- **Story state:** a single knowledge flag, `HasSeenTheTruth`, set once when the first run resolves (the manipulation is exposed, the children get out safely). Dialogue is gated on this flag, **never on a run counter** — a player can die on floor 3 of run 4 without ever reaching the father, and a run-count gate would have her react to a man she hasn't yet recognized. This is a hard rule for every future line, not just the launch set.
- **Delivery:** first-run dialogue is minimal by design — the story is carried by the Whisper Layer and by easter eggs the player isn't expected to understand yet. Post-`HasSeenTheTruth` runs add unique lines at specific beats.
- **Not yet built:** dialogue UI and a trigger/line-lookup system. The Whisper Layer, Codex stub and Refusal State are now MUST SHIP — see `08-MVP.md`.
- ⚠️ **NEEDS DECISION (owner proposal, not approved — `10-NARRATIVE.md` §4):** whether post-story runs recontextualize existing enemies (palette swap + new identity/dialogue, reusing the Elite pipeline in ART_DIRECTION §4) rather than adding new content. The related "who is the Final Boss" question is **settled**: the father (as The Depth Warden) is fought first on Floor 16, then Zyno as the true Final Boss (owner, 2026-08-15).

### Explicitly flagged Post-MVP (do not start engineering pre-MVP)

- **Flicker Recognition** — enemies periodically flicker to true forms during combat. Blocked on an animation-budget conversation (ART_DIRECTION.md §4 already caps enemy frame budgets due to the 3-weapon cost; this needs its own pass before scoping).
- **Post-Completion Truth Pass** — game-wide dialogue/bark rewrite unlocked after first story completion (death barks, father fight dialogue, Whisper Layer tone shift). Needs the full Whisper Layer + Refusal State script to exist first, which is itself mostly Post-MVP.

---

## 16. Mini-Boss Weapon Rewards

On Mini-Boss defeat, in addition to the normal level-up upgrade offer, the player receives a temporary "Overcharge" buff scoped to their current weapon, active for the remainder of that biome only (cleared on entering the next biome's Mini-Boss room or floor 1 of the next biome — exact clear trigger TBD in BALANCE.md). Implemented as a timed/scoped buff on the existing upgrade-modifier stack, not a new buff system.

*(Numbered §16, not §12, since §12–15 were added in the XP/Evolution/Souls/Narrative pass. Cross-references in BALANCE.md Open Items and the engineering plan were updated to match.)*

---

## Open Items for BALANCE.md / CONTENT_DESIGN.md

- Exact Ultimate Gauge gain values per hit type per weapon
- Combo Counter stack cap and per-stack bonus
- Charge Shot hold-time-to-damage curve
- Hyper Armor damage-reduction percentage
- Hazard timer speed per biome, and Hazard-touch damage vs. instant-kill decision
- Full Heavy Strike modifier list and which ones chain vs. replace
- Curse pool contents and weighting
- Weapon Mastery node effects (3–5 per weapon)
- Mini-Boss Overcharge effect values and exact clear condition
- XP level-threshold curve, and XP drop value per enemy type (BALANCE §16 — explicitly unresolved)
- The 2 Evolution choices per weapon (§13) — content, not just the slot
- Trapped Soul effect values and how a freed soul is lost (§14, BALANCE §17)
- Whisper Layer trigger list and line set for Biome 1 + the father fight (§15)
