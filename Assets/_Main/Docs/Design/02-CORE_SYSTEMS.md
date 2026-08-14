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
- Taking damage also fills the gauge, **+1% at base, flat regardless of hit severity** — this used to be exclusive to the "Gauge: Vengeance" upgrade (+5%, CONTENT_DESIGN §7/BALANCE §10). Now that gain-on-damage is base behavior, ⚠️ **Gauge: Vengeance has no remaining job and needs either a new effect or removal from the pool.**
- At 100%, `Ultimate()` becomes callable via R
- On activation, gauge drains instantly to 0% — no partial-use, no banking excess
- Gauge upgrades (from the pool) can further modify gain-per-hit, gain-on-taking-damage, or add a small passive trickle over time

**Ultimate effects per weapon** (full numeric tuning in BALANCE.md):
- **Katana** — a **Buff**, not an attack: a short cast raises an aura on her and the katana; for its duration she deals more damage, attacks faster, and moves faster, and every attack lands through the buff. Deals no damage of its own. **DECIDED: the buff does not touch the Combo Counter at all.** It no longer consumes or converts stacks on cast — the combo just keeps running through the Ultimate cast exactly as it would through any other action. This removes the old "Combo Finisher" framing entirely; the buff and the Combo Counter are now two independent systems that both key off landing hits.
- **Bow** — Full-Charge Piercing Shot: an instant max-charge Charge Shot that pierces all enemies in a line, no hold-time required
- **Greatsword** — Ground Slam: AoE hit centered on the player, knocks back and damages all enemies in radius

**Alt Ultimate (upgrade-gated):** Each weapon has one Epic-tier upgrade (CONTENT_DESIGN.md §2) that swaps `Ultimate()`'s implementation for a more mobile, skill-style variant instead of the default (e.g., Katana's Thousand Cuts becomes a movable flurry). Implemented as a swappable strategy on the `IWeapon` instance — taking the upgrade reassigns which Ultimate implementation `Ultimate()` calls; the gauge-fill and full-drain rules are unchanged regardless of which variant is active. ⚠️ **NEEDS DECISION:** Alt Ultimates are currently written as alternative Attacks only — can a weapon's Alt Ultimate also be a Buff (relevant to Katana specifically), or does an Attack-shaped weapon always get an Attack-shaped Alt?

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

- Flat damage per hit — no crit system anywhere in the game (run or permanent). Permanent power growth is delivered instead through the Hub Stat System's Core Stats (flat/percentage bonuses) and Miner's Traits (unique named effects) — see §10.
- Damage sources: Basic Attack, Heavy Strike, Ultimate, enemy attacks, Hazard Kills, environmental hazards (geysers, scorched ground, currents — per biome)
- **As documented:** a single `OnDamageDealt(source, target, amount)` event that the HUD, Combo Counter, and Ultimate Gauge all subscribe to. **As built:** `AttackHitbox.Landed(action, target, amount)` plus `Damageable.Damaged(amount)` — two events, and **neither carries `source`**. This works today only because the player is the only damage source that exists. Milestone 4's on-hit upgrade procs and any future "damage dealt by X" upgrade will need `source` on the event — ⚠️ **needs settling (add `source` to the built pipeline, or formally drop the single-event model from this doc) before that upgrade pool is authored.**
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

- Each floor pulls 1–3 rooms from a per-biome room pool (deterministic shuffle per run seed, not live procgen)
- Room types: Combat, Reward, Secret (locked, key-gated), Mini-Boss (every 5th floor), Final Boss (floor 16)
- **Room locking:** Combat Rooms lock entry/exit doors until all spawned enemies are defeated (existing logic, unchanged)

**Wave Rooms (variant flag on Combat Room prefabs, not a new room type):**
- A room flagged `IsWaveRoom = true` spawns enemies in 2–3 batches instead of all at once
- Next wave triggers when current wave count drops to ~1 remaining enemy (event-driven threshold check, not a timer — prevents stalling)
- Room-lock logic is unchanged: still locked until the *final* wave is cleared
- No new enemy types — Wave Rooms only resequence existing per-biome enemies
- Capped at 1–2 flagged rooms per biome's pool (pacing constraint, see LEVEL_DESIGN.md)

**Secret Floors:**
- A locked door prefab requires a `SecretKey` flag on the player's inventory, granted by defeating a rare elite spawn earlier in that biome
- Leads to a Vault Room (large Ore payout or guaranteed Legendary-tier upgrade offer)
- No separate hazard override — entering costs real time against the same biome hazard timer, which is the entire risk/reward tension (no new system required)

---

## 9. Upgrade & Curse System

- End of each floor: draw 3 upgrade offers via weighted random draw from a pool = shared pool (HP, Ore, Speed, Dash) + weapon-specific sub-pool (Heavy Strike mods, Ultimate mods, Ultimate Gauge mods) matching the equipped weapon
- A 4th slot is always populated with a **Curse** — drawn separately from its own small Curse pool, always visible, never mandatory
- Weighting can vary by depth/biome (e.g., certain upgrade tags weighted higher in later biomes) — same weighted-draw function, just a different weight table per biome, no new system
- Legendary-tier upgrades (Relics, §10) use the same draw pipeline but are excluded from normal weighted draws — only appear via Mini-Boss guaranteed drop, Secret Floor vault, or Relic Vault guarantee

---

## 10. Meta-Progression Systems

- **Ore → Ore Shards:** conversion happens once, at run end (win or death), formula based on Ore collected + depth reached (formula in BALANCE.md). No in-run shop — Ore has zero utility until the run ends.
- **Hub Stat System:** two tiers, both purchased with Ore Shards, no dependency tree (Rule: bounded, not a skill tree). **Core Stats** are rank-based (Max HP, Base Damage, Move Speed, Ultimate Gauge Gain, Ore Gain, Dash Cooldown). **Miner's Traits** are unique, mostly single-purchase named effects (Hades Mirror of Night-style — e.g., Death Defiance, Boiling Blood, Warm-Up) that create build-defining decisions rather than flat number lines. Plus two flat non-stat unlocks (extra Curse slot, Relic Cache) that sit outside both tiers. Full table in CONTENT_DESIGN.md §7.
- **Weapon Mastery:** a small per-weapon counter incremented by run-usage (not Shard spend) — e.g., "floors cleared with this weapon equipped" — crossing thresholds unlocks the 3–5 mastery nodes per weapon
- **Relics:** the Legendary upgrade rarity, one per weapon. First time a given weapon's Relic is offered and taken, it's flagged "discovered" on the save file. Discovered Relics become purchasable from the Relic Vault (Hub) at high Shard cost — purchasing guarantees that Relic appears as an offer once in the player's next run.

---

## 11. Boss Weapon-Check Mechanics

Not a separate system — implemented as boss-specific phase logic that reads the player's equipped weapon type and branches a single encounter parameter (e.g., shield HP threshold, projectile speed) accordingly. No new architecture; bosses already have phase/state logic, this just adds a weapon-type read at phase-transition checks.

---

## 12. Mini-Boss Weapon Rewards

On Mini-Boss defeat, in addition to the normal floor-end upgrade offer, the player receives a temporary "Overcharge" buff scoped to their current weapon, active for the remainder of that biome only (cleared on entering the next biome's Mini-Boss room or floor 1 of the next biome — exact clear trigger TBD in BALANCE.md). Implemented as a timed/scoped buff on the existing upgrade-modifier stack, not a new buff system.

---

## 13. Narrative & Dialogue System (new — did not exist in any prior doc)

Full story content lives in `10-NARRATIVE.md`; this is the system-level shape.

- **Story state:** a single knowledge flag, `HasSeenTheTruth`, set once when the first run resolves (the manipulation is exposed, the children get out safely). Dialogue is gated on this flag, **never on a run counter** — a player can die on floor 3 of run 4 without ever reaching the father, and a run-count gate would have her react to a man she hasn't yet recognized. This is a hard rule for every future line, not just the launch set.
- **Delivery:** first-run dialogue is minimal/absent by design — the story is carried by easter eggs the player isn't expected to understand yet. Post-`HasSeenTheTruth` runs add unique lines at specific beats (e.g. reaching the Final Boss).
- **Not yet built:** dialogue UI and a trigger/line-lookup system. Not in any MVP tier yet — see `08-MVP.md`.
- ⚠️ **NEEDS DECISION (owner proposal, not approved — `10-NARRATIVE.md` §4):** whether post-story runs recontextualize existing enemies (palette swap + new identity/dialogue, reusing the Elite aura pipeline in ART_DIRECTION §4) rather than adding new content, and whether the Final Boss becomes Zyno (via The Depth Warden slot) on runs 2+. Until this is decided, "what the game is after the story ends" has no answer.

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
