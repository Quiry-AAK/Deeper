# CONTENT DESIGN — "Deeper"

Full content tables referenced by 01-GDD.md and 02-CORE_SYSTEMS.md. Numeric values (damage, HP, percentages) are placeholders here — exact tuning lives in 04-BALANCE.md. This doc defines *what exists* and *what each thing does*, not the final numbers.

**Design intent for this pass:** build depth for dozens of viable in-run combinations. Permanent power growth is deliberately kept **out of the run-upgrade pool** and lives only in the Hub Stat System (§7) — this preserves the flat, readable in-run damage math while still giving Hades-style meta progression across runs.

---

## 1. Shared Upgrade Pool

Offered regardless of equipped weapon, drawn alongside the weapon-specific sub-pool. Organized by category and rarity tier (Common / Rare / Epic — Legendary is reserved for per-weapon Relics, §4).

### Survivability
| Upgrade | Tier | Effect |
|---|---|---|
| Vitality | Common | +Max HP |
| Second Wind | Common | Heal a % of max HP immediately on pickup |
| Iron Skin | Common | Flat damage reduction from all sources |
| Thorns | Rare | Reflect a % of damage taken back at the attacker |
| Adrenaline | Rare | Heal a small % of max HP whenever the Ultimate is used |
| Last Stand | Epic | Below 25% HP, take reduced damage from all sources |

### Offense
| Upgrade | Tier | Effect |
|---|---|---|
| Heavy Hands | Common | +Flat damage to all attacks |
| Bleeding Strikes | Common | Basic Attacks apply a stacking damage-over-time |
| Momentum | Common | +% damage for a few seconds after a Dig-Dash |
| Overwhelm | Rare | Consecutive hits without missing (any weapon) grant a small stacking damage bonus, resets on miss — a lighter, universal version of Katana's Combo Counter |
| Executioner | Rare | +% bonus damage to enemies below 25% HP |
| Explosive Finish | Epic | Killing blows detonate a small AoE that damages nearby enemies |

### Mobility / Utility
| Upgrade | Tier | Effect |
|---|---|---|
| Fleet Foot | Common | +% move speed |
| Quickstep | Common | -% Dig-Dash cooldown |
| Long Dash | Common | +Dig-Dash distance |
| Phase Step | Rare | Dig-Dash grants slightly longer i-frames |
| Blink Strike | Epic | Dig-Dash deals damage to any enemy it passes through |

### Ore / Meta
| Upgrade | Tier | Effect |
|---|---|---|
| Prospector's Eye | Common | +% Ore drop from enemies |
| Lucky Vein | Common | +% Ore drop from chests |
| Ore Magnet | Rare | Ore is pulled toward the player from a small radius |

### On-Hit Procs
| Upgrade | Tier | Effect |
|---|---|---|
| Frost Touch | Rare | Basic Attacks briefly slow enemies on hit |
| Venom Edge | Rare | Basic Attacks apply a stacking poison DoT (separate stack from Bleeding Strikes — only one can be taken per run) |
| Static Discharge | Epic | On hit, a small bolt of damage arcs to a second nearby enemy |

### Situational
| Upgrade | Tier | Effect |
|---|---|---|
| Curse Synergy | Rare | +% damage for each active Curse taken this run |
| Gambler's Edge | Epic | Upgrade screens show a 4th non-Curse option (in addition to the always-visible Curse slot) for the rest of the run |

That's 24 shared entries across 6 categories — combined with weapon sub-pools below, this is the core lever for build variety.

---

## 2. Weapon-Specific Sub-Pools

Each weapon's sub-pool covers Heavy Strike mods, Signature Trait mods, Ultimate Gauge mods, Ultimate mods (including an unlockable **Alt Ultimate**, §3), and 1–2 build-defining passives unique to that weapon's fantasy. Heavy Strike replacement effects are now differentiated per weapon — no shared/duplicate effects.

### 2a. Katana

| Upgrade | Category | Tier | Effect |
|---|---|---|---|
| Twin Cut | Heavy Strike (Chain) | Common | Heavy Strike becomes a 2-hit chain |
| Triple Cut | Heavy Strike (Chain) | Rare | Requires Twin Cut; extends to a 3-hit chain |
| Shadow Step Slash | Heavy Strike (Replace) | Rare | RClick teleports the player behind the target and strikes for bonus damage |
| Momentum Edge | Combo Counter | Common | Combo stack cap increased |
| Flow State | Combo Counter | Rare | Combo stack no longer resets on taking damage (still resets on miss) |
| Razor Focus | Combo Counter | Rare | Combo stack decays over a couple seconds on miss instead of resetting instantly |
| Combo Overflow | Combo Counter | Epic | Stacks earned beyond the cap convert into bonus Ultimate Gauge instead of being wasted |
| Gauge: Bloodrush | Ultimate Gauge | Common | Basic Attacks contribute more % to the gauge |
| Gauge: Vengeance | Ultimate Gauge | Common | ⚠️ **Job unclear** — gain-on-taking-damage is now base behavior (BALANCE §4), not upgrade-gated. Needs a new effect or removal from the pool. |
| Gauge: Adrenal Rush | Ultimate Gauge | Rare | Gauge gain increased while Combo Counter is at or near its cap |
~~| Finisher+ | Ultimate mod | Rare | Combo Finisher converts stacks into bonus damage at a higher rate |~~
~~| Echo Slash | Ultimate mod | Epic | Combo Finisher strikes twice |~~
**DECIDED: Finisher+ and Echo Slash are removed from the pool.** Both modified the old Combo-Finisher-consumes-stacks behavior; per the owner's decision that the Katana Ultimate buff no longer touches the Combo Counter at all (BALANCE §4, CORE_SYSTEMS §4), there's nothing left for either upgrade to modify (Design Rule 10).
| **Thousand Cuts** | Alt Ultimate | Epic | Replaces the default Ultimate: a mobile flurry — player can move freely while unleashing a rapid multi-hit dash-strike combo for a short duration. ⚠️ Written as an alternative to the old Combo Finisher *attack*; now that the default Katana Ultimate is a Buff, decide whether Thousand Cuts stays Attack-shaped or should also become buff-shaped. |
| Deathmark | Build-defining | Rare | Basic Attacks mark the target; Heavy Strike deals bonus damage to a marked enemy |
| Windcutter | Build-defining | Common | Basic Attack gains slightly more range and a thin piercing edge |

### 2b. Bow

| Upgrade | Category | Tier | Effect |
|---|---|---|---|
| Twin Nock | Heavy Strike (Chain) | Common | Heavy Strike fires 2 arrows in quick succession |
| Explosive Nock | Heavy Strike (Chain) | Rare | Requires Twin Nock; arrows detonate in a small radius on impact |
| Dynamite Throw | Heavy Strike (Replace) | Rare | RClick lobs a timed explosive instead of firing |
| Quickdraw | Charge Shot | Common | Reduces time to reach max charge |
| Piercing Draw | Charge Shot | Common | Fully charged shots pierce 1 additional enemy |
| Volatile Draw | Charge Shot | Rare | Charged shots leave a brief lingering damage zone at point of impact |
| Windrunner | Charge Shot | Rare | Moving no longer slows charge time |
| Gauge: Sharpshooter | Ultimate Gauge | Common | Charged shots contribute more % to the gauge than fast shots |
| Gauge: Steady Hands | Ultimate Gauge | Common | Small passive gauge trickle over time |
| Gauge: Focused Reserve | Ultimate Gauge | Rare | Fully charged shots grant a bonus gauge burst |
| Piercing Line+ | Ultimate mod | Rare | Piercing Shot pierces further and detonates a small AoE on the final enemy hit |
| Volley Echo | Ultimate mod | Epic | Fires a second, delayed piercing shot along the same line |
| **Rain of Arrows** | Alt Ultimate | Epic | Replaces the default Piercing Shot: fire a targeted zone that rains arrows over a few seconds — area denial/control instead of a single burst line |
| Trick Shot | Build-defining | Rare | Basic Attacks have a chance to ricochet to a second nearby enemy at reduced damage |
| Steady Aim | Build-defining | Common | Standing still for a moment before firing grants a small damage bonus to the next shot |

### 2c. Greatsword

| Upgrade | Category | Tier | Effect |
|---|---|---|---|
| Heavy Follow-Through | Heavy Strike (Chain) | Common | Heavy Strike becomes a 2-hit chain |
| Earthbreaker Chain | Heavy Strike (Chain) | Rare | Requires Heavy Follow-Through; extends to a 3-hit chain |
| Grapple Pull | Heavy Strike (Replace) | Rare | RClick yanks the nearest enemy toward the player instead of striking |
| Fortified Stance | Hyper Armor | Common | Damage reduction during Windup increased |
| Unshakable | Hyper Armor | Common | Hyper Armor extends slightly into early Active frames |
| Juggernaut | Hyper Armor | Rare | Landing a hit grants brief Hyper Armor during the following Recovery |
| Gauge: Warbringer | Ultimate Gauge | Common | Heavy Strike contributes more % to the gauge |
| Gauge: Vengeance | Ultimate Gauge | Common | ⚠️ Same job-loss issue as the Katana version above (line 82) — gain-on-taking-damage is now base behavior. |
| Gauge: Momentum Forge | Ultimate Gauge | Rare | Gauge gain increased while Hyper Armor is active |
| Aftershock | Ultimate mod | Rare | Ground Slam leaves a brief lingering damage zone after impact |
| Seismic Wave | Ultimate mod | Epic | Ground Slam sends out a secondary shockwave ring beyond the initial radius |
| **Earthbreaker Charge** | Alt Ultimate | Epic | Replaces the default Ground Slam: charge forward through enemies, dealing continuous damage along the path and ending in a slam — a mobile skill instead of a static AoE |
| Colossus | Build-defining | Rare | Basic Attack knockback increased; enemies knocked into walls or other enemies take bonus damage |
| Unrelenting | Build-defining | Common | Each landed hit slightly reduces the next Windup duration, stacking a few times, resetting on miss |

That's 15 entries per weapon (45 total weapon-specific), combined with the 24 shared entries — well over the threshold needed for dozens of distinct build paths per weapon, especially factoring in Curse combinations (§3) and Relic anchors (§4).

---

## 3. Curse Pool

Always offered as a visible, optional 4th slot. High-risk, high-reward, never mandatory.

| Curse | Effect |
|---|---|
| Glass Cannon | +40% damage dealt, but take double damage |
| Greed's Toll | Enemies drop 3x Ore, but Rising Hazard advances 20% faster |
| Reckless Vigor | Ultimate Gauge fills 50% faster, but Ultimate deals 25% less damage |
| Frail Grip | +1 free Heavy Strike chain hit, but Heavy Strike no longer contributes to Ultimate Gauge |
| Blood Debt | Heal to full HP now, but max HP is reduced for the rest of the run |
| Iron Curse | Take no knockback from any source, but move speed is reduced |
| Starving Blade | +% damage the lower your current HP is, but healing effects are reduced |
| Overclock | Attack speed increased across the board, but Windup Hyper Armor / Combo stability effects are disabled for the run |

Additional Curse slots beyond the base 1 unlock via a Hub Stat System purchase (§7) — only one Curse can be taken per floor regardless of how many are visible.

---

## 4. Relics (Legendary Tier)

One per weapon, only offered when that weapon is equipped. Excluded from normal weighted draws — only via Mini-Boss guaranteed drop, Secret Floor vault, or a purchased Relic Vault guarantee.

| Weapon | Relic | Effect |
|---|---|---|
| Katana | Endless Edge | Combo Counter has no stack cap; each stack's bonus is reduced but stacks can climb indefinitely within a room |
| Bow | Deadeye's Promise | Charge Shot always pierces all enemies in line, regardless of hold time |
| Greatsword | Mountain's Fall | Ultimate does not fully drain the gauge — leaves 25% remaining after use |

Relics are independent of the Alt Ultimate unlock (§2) — a player can run a Relic and an Alt Ultimate together for a fully specialized build.

---

## 5. Enemy Roster (per biome)

Unchanged from prior pass — numeric stats live in BALANCE.md, this defines role and behavior only.

### Biome 1 — Upper Caves
| Enemy | Role | Behavior |
|---|---|---|
| Cave Crawler | Basic melee | Walks directly at player, short telegraphed lunge attack |
| Rock Slinger | Basic ranged | Throws slow, telegraphed projectiles from range |
| Tunnel Brute | Heavy melee | Slow, high HP, telegraphed overhead slam with knockback |
| **Elite: Deep Warden** | Rare elite (key drop) | Tougher Tunnel Brute variant, drops Secret Floor key on death |
| **Mini-Boss: The Collapsed King** | Biome 1 boss | Ground-slam AoE phase, weapon-check: Greatsword breaks its rubble shield in 1 hit, others need 3+ |

### Biome 2 — Flooded Tunnels
| Enemy | Role | Behavior |
|---|---|---|
| Eel Diver | Fast melee | Quick, low-HP, darts in and out; slowed by water tiles like the player |
| Current Wisp | Ranged/utility | Fires a projectile that also pushes player via current on hit |
| Bloated Drifter | Heavy/area | Slow-moving, explodes into a damaging puddle on death |
| **Elite: Tideheart** | Rare elite (key drop) | Tougher Current Wisp variant, drops Secret Floor key on death |
| **Mini-Boss: The Drowned Custodian** | Biome 2 boss | Summons rising water hazard early in its room; fires slow homing projectiles Bow players can snipe for bonus damage |

### Biome 3 — Molten Depths
| Enemy | Role | Behavior |
|---|---|---|
| Ember Wisp | Fast ranged | Erratic movement, fires quick low-damage bolts |
| Magma Crawler | Basic melee | Leaves a small scorched patch where it dies |
| Forge Golem | Heavy melee | High HP, resistant to knockback, telegraphed slam |
| **Elite: Cinder Warden** | Rare elite (key drop) | Tougher Forge Golem variant, drops Secret Floor key on death |
| **Mini-Boss: The Molten Sentinel** | Biome 3 boss | Periodic geyser eruptions mid-fight; Greatsword's Hyper Armor lets players tank a tick and keep swinging |

### Floor 16 — Final Boss Sequence

**DECIDED (owner-directed): two fights, not one.** The Depth Warden **is her father** — same boss slot and mechanics as originally designed, reidentified rather than replaced. Defeating him doesn't end the floor: **Zyno is the true Final Boss**, fought immediately after.

| Enemy | Role | Behavior |
|---|---|---|
| **The Depth Warden (her father)** | First fight, Floor 16 | Unchanged from the original design: multi-phase, incorporates all 3 biome hazard types in sequence as the arena degrades, weapon-check moment per the Mini-Boss pattern. Stats/phases: BALANCE §6 (existing "Depth Warden" row, reidentified — no numbers need to change). |
| **Zyno** | True Final Boss, Floor 16 (after the Warden) | ⚠️ **Entirely new boss content — no moveset, no stats, no arena, no art exist for this fight yet.** See the scope note below. |

⚠️ **SCOPE FLAG (per PROJECT scope-discipline rules — this needs an explicit call, not a silent add to a tier):** Zyno as a full second final-boss encounter is new content on top of everything `07-IMPLEMENTATION_PLAN.md` and `08-MVP.md` already account for. It competes directly with the same Phase 3/5 content-authoring risk those docs already flag as this project's binding constraint. Options, cheapest first:
- **Post-MVP:** ship the Depth Warden/father fight as Floor 16's Final Boss for MVP (this is a pure rename of already-budgeted content — zero new cost); add Zyno as a true final encounter after MVP.
- **Reuse-heavy MVP version:** Zyno's fight for MVP reuses an existing Mini-Boss's moveset/arena with a palette-swap + new dialogue (matches the "recontextualization is nearly free" logic already established for the post-story enemies in `10-NARRATIVE.md` §4a), deferring a bespoke Zyno moveset to post-MVP polish.
- **Full new boss for MVP:** budget it properly — new moveset, stats, arena, art, balance pass — which is a genuine timeline conversation, not a rounding error.

---

## 6. Room Pool Composition (per biome)

Unchanged from prior pass.

| Room Type | Count per Biome | Notes |
|---|---|---|
| Combat Room | 6 layouts | 1–2 of the 6 flagged `IsWaveRoom = true` per biome |
| Reward Room | 2 layouts | Ore and/or upgrade-adjacent, no combat |
| Secret Vault Room | 1 layout | Locked, key-gated, reused across biomes with biome-specific dressing |
| Mini-Boss Room | 1 layout | Unique per biome |

---

## 7. Permanent Progression — Hub Stat System

Replaces the old flat upgrade list. Two tiers, both purchased with Ore Shards, no dependency tree between entries (bounded, not a skill tree).

**Core Stats** — rank-based, purchased independently:

| Stat | Effect per Rank |
|---|---|
| Max HP | +Flat HP |
| Base Damage | +Flat damage to all attacks |
| Move Speed | +% move speed |
| Ultimate Gauge Gain | +% gauge gained per landed hit, all weapons |
| Ore Gain | +% Ore collected from all sources |
| Dash Cooldown | -% Dig-Dash cooldown |

**Miner's Traits** — unique, mostly single-purchase named effects (Hades Mirror of Night-style), each creates a small build-defining decision rather than a flat number line:

| Trait | Effect |
|---|---|
| Death Defiance | Survive a killing blow once per run, healing to 25% HP (2nd rank: 2 charges per run) |
| Boiling Blood | +1% damage per 5% max HP missing |
| Warm-Up | Ultimate Gauge starts each floor at 20% instead of 0% |
| Nerves of Steel | The first hit taken each floor is fully negated |
| Old Prospector | Start each run with a flat bonus amount of Ore |
| Miner's Sixth Sense | After floor 5, one Common upgrade slot each offer is guaranteed upgraded to Rare or higher |
| Steadfast Grip | Curse downside effects are reduced by 15% |
| Second Skin | Small permanent flat damage reduction, always active |

**Non-stat flat unlocks (kept deliberately outside both tiers above):**

| Unlock | Effect |
|---|---|
| Second Curse Slot | Unlocks an additional Curse offer per floor (max 1 still takeable per floor) |
| Relic Cache | Start each run with a small random bonus themed after your equipped weapon's Relic — not the full Relic effect, a taste of it |

**Weapon Mastery** (per-weapon, usage-unlocked rather than Shard-purchased) and the **Relic Vault** remain as previously defined — separate from the Stat System, since they're earned through play rather than currency.

---

## Open Items for BALANCE.md

- All numeric values for every upgrade/curse/relic above (%s, flat amounts, stack caps, DoT tick rates)
- Enemy HP/damage/speed per biome tier
- Mini-Boss and Final Boss HP/phase thresholds
- Weighted-draw probability table by rarity tier (Common/Rare/Epic), and per-biome weighting shifts
- Hub Stat System rank counts and Ore Shard cost curve per Core Stat, and flat costs for each Miner's Trait
- Ore Shard cost for Relic Vault purchases and the two non-stat unlocks
