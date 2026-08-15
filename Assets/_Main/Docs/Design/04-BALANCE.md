# BALANCE — "Deeper"

Numeric tuning for every system/content item defined in 01-GDD.md, 02-CORE_SYSTEMS.md, and 03-CONTENT_DESIGN.md. All values are **first-pass placeholders** meant to be internally consistent, not final — expect these to move after playtesting. Percentages are additive unless noted otherwise.

---

**Confirmed:** mitigation in this doc is exclusively Dig-Dash i-frames, Hyper Armor, Iron Skin (§9), and Second Skin (§15) — no armor/gear system exists or is implied by any value below (see CORE_SYSTEMS §1, inventory/armor deleted).

## 1. Player Base Stats

| Stat | Value |
|---|---|
| Base Max HP | 100 |
| Base Move Speed | 5.0 units/sec |
| Base Dig-Dash Cooldown | 1.2s |
| Base Dig-Dash Distance | 3.0 units |
| Dig-Dash i-frame duration | 0.25s |

---

## 2. Weapon Timing & Damage (base, no upgrades)

All timing in seconds. Damage in flat HP.

| Weapon | Action | Windup | Active | Recovery | Damage |
|---|---|---|---|---|---|
| Katana | Basic Attack | 0.10 | 0.08 | 0.18 | 8 |
| Katana | Heavy Strike | 0.30 | 0.12 | 0.35 | 20 |
| Bow | Basic (uncharged/tap) | 0.10 | instant | 0.22 | 6 |
| Bow | Basic (full charge, 0.8s hold) | 0.80 | instant | 0.30 | 22 (pierces 1) |
| Bow | Heavy Strike | 0.35 | instant | 0.35 | 18 |
| Greatsword | Basic Attack | 0.40 | 0.15 | 0.45 | 16 |
| Greatsword | Heavy Strike | 0.65 | 0.20 | 0.60 | 35 |

**Dash-Attack Cancel window:** available for the full Recovery phase of any action above; canceling forfeits any remaining Recovery lockout and transitions directly into Dig-Dash.

---

## 3. Signature Trait Values

### Katana — Combo Counter
- Bonus per stack: +2% damage
- Base stack cap: 10 (max +20% damage)
- Resets: instantly on miss; instantly on taking damage (unless Flow State upgrade taken)

### Bow — Charge Shot
- Min hold (tap): 0% charge → base 6 damage, no pierce
- Max hold: 0.8s → 100% charge → 22 damage, pierces 1 enemy
- Damage scales linearly with hold time between these points

### Greatsword — Hyper Armor
- Damage reduction during Windup: 40%
- Duration: full Windup phase only (does not extend into Active/Recovery at base)

---

## 4. Ultimate Gauge & Ultimate Damage

**Gauge gain per landed hit — DECIDED: per-weapon differentiation is restored** (reverts the flat-1%-for-everyone version that had been built; this is a build task, not just a doc fix):

| Weapon | Basic Attack | Heavy Strike |
|---|---|---|
| Katana | +8% | +15% |
| Bow | +6% (uncharged) / +12% (full charge) | +15% |
| Greatsword | +10% | +20% |

**Gain on taking damage, base:** +1% flat, not scaled by hit severity. **DECIDED:** the "Gauge: Vengeance" upgrade now adds +2% on top of this base (3% total), giving it a real job instead of duplicating base behavior — see §9/§10 for the updated value.

**Ultimate damage/effect (base, before Alt Ultimate or mods):**

| Weapon | Ultimate | Base Value |
|---|---|---|
| Katana | **Aura Buff** (was "Combo Finisher," see below) | No damage. **Placeholder, not balanced:** Duration 8s, +50% damage, +40% attack speed (shortens Windup/Active/Recovery on all 3 attack phases together — a buffed Basic runs 0.36s→0.26s, Heavy 0.77s→0.55s), +15% move speed. Re-casting refreshes duration rather than stacking. |
| Bow | Full-Charge Piercing Shot | 35 damage, pierces all enemies in line |
| Greatsword | Ground Slam | 45 damage, radius 3.0 units, knockback |

**Katana Ultimate is a self-buff, not a damage move** (owner-directed, built) — the old "Combo Finisher" row above (40 damage + 5 per Combo Counter stack consumed) no longer applies. **DECIDED: the buff does not touch the Combo Counter.** It no longer consumes stacks on cast — the combo runs independently and keeps its stacks through an Ultimate cast, exactly as it would through any other action. `Finisher+` and `Echo Slash` (CONTENT_DESIGN §2a) are removed from the Katana Ultimate mod pool as a direct consequence — both were written to modify a stack-consuming conversion that no longer exists (Design Rule 10, redundant/non-functional content). No Windup/Active/Recovery timing exists yet for any Ultimate (Katana's buff-cast included) — three placeholder phase timings are in code and need a real design pass.

**New stat, not yet in the Hub Core Stats table below:** `StatType.AttackSpeed` (added for the Katana buff, appended as value 8 so existing serialized modifiers keep their meaning). ⚠️ **NEEDS DECISION:** should Hub Core Stats (§15) be able to buy permanent attack speed, or does it stay a run-only buff stat?

**Alt Ultimate damage (same total-output budget as default, redistributed for mobility):**

| Weapon | Alt Ultimate | Base Value |
|---|---|---|
| Katana | Thousand Cuts | 6 hits × 8 damage over 1.2s, **full free player movement during the hits** (see note below) |
| Bow | Rain of Arrows | 10 arrows × 5 damage over 2.5s in a 2.5-unit zone |
| Greatsword | Earthbreaker Charge | 4 damage per 0.1s tick while charging (up to 3s) + 30 damage slam on impact/end |

**"Player-mobile" resolved:** all attacks now drive a forward lunge rather than rooting the player (GDD §Combat, "Attack Movement"), so a locked-direction lunge no longer distinguishes anything on its own. **DECIDED:** what distinguishes Thousand Cuts is genuine player-steered movement during the hits — she can actually change direction mid-flurry, unlike the locked-direction lunge every other attack gets. This needs implementing as real free movement, not the existing lunge behavior.

---

## 4a. Enemy Behavior Timing (new — no prior design source, ~30 numbers currently invented in code)

None of the values below trace to any design doc. All are serialized with in-code "placeholder" tooltips and need a balance pass — recorded here so a decision, not a guess, replaces them.

| | Crawler | Slinger | Brute | Warden (Elite) |
|---|---|---|---|---|
| Windup (telegraph) | 0.35 | 0.50 | 0.75 | 0.70 |
| Active | 0.18 | 0.06 | 0.12 | 0.12 |
| Recovery | 0.45 | 0.60 | 0.90 | 0.85 |
| Cooldown | 1.20 | 2.20 | 2.50 | 2.20 |
| Aggro radius | 10 | 12 | 12 | 14 |
| Attack range | 1.6 | 7.0 | 2.0 | 2.2 |
| Stop distance | 0.9 | 5.5 | 1.2 | 1.2 |
| Retreat distance | 0 | 3.5 | 0 | 0 |

Per-move geometry: lunge distance 1.8 (Crawler), slam radius 2.2 / knockback speed 12 / knockback time 0.30 (Brute/Warden), rock speed 4.5 / lifetime 4.0 (Slinger).

Two numbers are anchored to something real: ART_DIRECTION §4 caps enemy Telegraph at 3 frames (0.375s at 8fps), which is where the Crawler's 0.35 Windup came from; and the player moves at 5.0 units/sec (§1 above), so the Slinger's 4.5 rock speed is outrunnable by design. Everything else is a guess pending playtesting.

**How each enemy delivers damage** (an engineering interpretation, not a design source — confirm or overrule):
- **Cave Crawler** — contact damage plus a lunge that closes distance. The pressure enemy.
- **Rock Slinger** — no contact damage; all 6 damage is the thrown rock, so it's safe to stand next to and punishes range instead of proximity.
- **Tunnel Brute / Deep Warden** — no contact damage; all 15/18 is the slam, leaving a safe window beside them between slams (the whiff-punish space LEVEL_DESIGN §2 wants preserved for Greatsword). Giving them both contact and slam damage would double-dip and delete that window.

---

## 5. Enemy Stats (per biome tier)

| Enemy | HP | Damage | Move Speed |
|---|---|---|---|
| Cave Crawler | 20 | 8 | 3.5 |
| Rock Slinger | 15 | 6 | 2.5 |
| Tunnel Brute | 60 | 15 | 2.0 |
| Elite: Deep Warden | 100 | 18 | 2.0 |
| Eel Diver | 18 | 10 | 5.0 (slowed to 3.0 in water) |
| Current Wisp | 20 | 8 | 3.0 |
| Bloated Drifter | 50 | 12 (+5 puddle DoT/tick) | 1.5 |
| Elite: Tideheart | 90 | 16 | 3.0 |
| Ember Wisp | 22 | 9 | 4.5 |
| Magma Crawler | 25 | 10 | 3.0 |
| Forge Golem | 80 | 18 | 1.8 |
| Elite: Cinder Warden | 130 | 22 | 1.8 |

**Deep Warden's Elite spec, resolved:** the palette swap (Brute recolored violet) is now the full MVP spec — the aura VFX layer ART_DIRECTION §4 originally called for is cut for MVP and deferred to post-MVP polish (see that doc). No open item here anymore.

---

## 6. Mini-Boss & Final Boss

| Boss | HP | Phases | Notes |
|---|---|---|---|
| The Collapsed King (Biome 1) | 350 | 2 | Phase 2 at 50% HP: slam AoE cadence increases. Weapon-check: rubble shield = 1 hit (Greatsword) / 3 hits (others) |
| The Drowned Custodian (Biome 2) | 450 | 2 | Phase 2 at 50% HP: water hazard covers 75% of room. Homing projectile speed: 2.5 units/sec (snipeable by Bow for 1.5x damage) |
| The Molten Sentinel (Biome 3) | 600 | 3 | Geyser eruption every 8s; Hyper Armor absorbs 1 tick fully at 40% reduction |
| The Depth Warden (her father) | 1200 | 3 | Phase 1: collapsing-tile theme, Phase 2: rising water theme, Phase 3: lava geyser theme. Weapon-check moment in Phase 3. Fought first, on Floor 16 — no longer the final encounter of the floor. |
| **Zyno** (True Final Boss) | Reuses an existing Mini-Boss's HP/phases for MVP (specific choice TBD) | — | Fought immediately after the Depth Warden, same floor. **DECIDED: MUST SHIP.** MVP version is a palette-swap + dialogue pass on an existing Mini-Boss (cheapest way to make this real for the timeline); a bespoke stat block/moveset is SHOULD SHIP. See `03-CONTENT_DESIGN.md` Floor 16 and `08-MVP.md`. |

---

## 7. Hazard Timer

| Biome | Time for Hazard Front to clear a full floor's rooms |
|---|---|
| Upper Caves | 90s |
| Flooded Tunnels | 75s |
| Molten Depths | 60s |
| Final Boss floor | No hazard front — escape sequence uses a fixed 45s countdown instead |

Hazard-touch = instant death (confirmed, not heavy-damage — keeps tension binary and readable).

---

## 8. Room & Wave Pacing

| Metric | Target |
|---|---|
| Combat Room clear time | 30–60s |
| Wave Room clear time (2–3 waves) | 60–100s |
| Rooms per floor | 3–5 |
| Floor total time budget (vs. Hazard Timer above) | Comfortably under the hazard time at Common-upgrade play; Wave Rooms and Secret Floor detours are what create real time pressure |

---

## 9. Shared Upgrade Pool — Values

| Upgrade | Tier | Value |
|---|---|---|
| Vitality | Common | +15 Max HP |
| Second Wind | Common | Heal 20% max HP on pickup |
| Iron Skin | Common | -2 flat damage taken per hit |
| Thorns | Rare | Reflect 25% of damage taken |
| Adrenaline | Rare | Heal 8% max HP on Ultimate use |
| Last Stand | Epic | -30% damage taken below 25% HP |
| Heavy Hands | Common | +3 flat damage, all attacks |
| Bleeding Strikes | Common | 3 damage/tick DoT, 3 ticks, stacks to 3 |
| Momentum | Common | +15% damage for 3s after Dig-Dash |
| Overwhelm | Rare | +2% damage per consecutive hit, cap 5 stacks, resets on miss |
| Executioner | Rare | +20% damage to enemies below 25% HP |
| Explosive Finish | Epic | Killing blow deals 15 AoE damage, radius 2.0 units |
| Fleet Foot | Common | +10% move speed |
| Quickstep | Common | -20% Dig-Dash cooldown |
| Long Dash | Common | +25% Dig-Dash distance |
| Phase Step | Rare | +0.1s i-frame duration |
| Blink Strike | Epic | Dig-Dash deals 12 damage to enemies passed through |
| Quick Study | Common | +20% XP from enemies |
| Insight Magnet | Rare | XP orb pull radius 3.0 units |
| Frost Touch | Rare | -30% enemy move speed for 1.5s on hit |
| Venom Edge | Rare | 2 damage/tick DoT, 4 ticks, stacks to 5 |
| Static Discharge | Epic | 4 damage arc to 1 nearby enemy (range 3.0 units) |
| Curse Synergy | Rare | +8% damage per active Curse this run |
| Gambler's Edge | Epic | 4th non-Curse option shown for rest of run |

---

## 10. Weapon Sub-Pool Values

### Katana
| Upgrade | Value |
|---|---|
| Twin Cut | 2nd hit: 12 damage |
| Triple Cut | 3rd hit: 12 damage |
| Shadow Step Slash | Teleport range 4.0 units, 28 damage |
| Momentum Edge | Stack cap 10 → 14 |
| Flow State | No reset on damage taken |
| Razor Focus | Decay over 1.5s instead of instant reset |
| Combo Overflow | Stacks beyond cap: +3% gauge each |
| Gauge: Bloodrush | Basic gauge gain +4% (12% total) |
| Gauge: Vengeance | +2% gauge on taking damage, stacks with the 1% base (3% total) |
| Gauge: Adrenal Rush | +50% gauge gain while at 8+ stacks |
| Thousand Cuts | See §4 |
| Deathmark | Marked target takes +25% from next Heavy Strike |
| Windcutter | +15% Basic Attack range, thin pierce (1 extra enemy) |

### Bow
| Upgrade | Value |
|---|---|
| Twin Nock | 2nd arrow: 12 damage |
| Explosive Nock | +8 AoE damage, radius 1.5 units |
| Dynamite Throw | 25 damage, radius 2.5 units, 1.2s fuse |
| Quickdraw | Max charge time 0.8s → 0.55s |
| Piercing Draw | +1 pierce (total 2) |
| Volatile Draw | Lingering zone: 4 damage/tick, 2 ticks |
| Windrunner | Move speed penalty while charging removed |
| Gauge: Sharpshooter | Charged shot gauge +5% (17-20% total) |
| Gauge: Steady Hands | +1% gauge/sec passive |
| Gauge: Focused Reserve | +10% bonus gauge on full charge |
| Piercing Line+ | +2 pierce, 12 AoE on final hit |
| Volley Echo | 2nd shot at 70% value, 0.3s delay |
| Rain of Arrows | See §4 |
| Trick Shot | 30% ricochet chance, 50% damage on ricochet |
| Steady Aim | +20% damage after 0.5s standing still |

### Greatsword
| Upgrade | Value |
|---|---|
| Heavy Follow-Through | 2nd hit: 25 damage |
| Earthbreaker Chain | 3rd hit: 25 damage |
| Grapple Pull | Pull range 5.0 units |
| Fortified Stance | Damage reduction 40% → 55% |
| Unshakable | Hyper Armor extends 0.1s into Active |
| Juggernaut | Hyper Armor (25% reduction) for 0.3s after landing a hit |
| Gauge: Warbringer | Heavy Strike gauge +5% (25% total) |
| Gauge: Vengeance | +2% gauge on taking damage, stacks with the 1% base (3% total) |
| Gauge: Momentum Forge | +50% gauge gain while Hyper Armor active |
| Aftershock | Lingering zone: 6 damage/tick, 2 ticks |
| Seismic Wave | Secondary ring: 20 damage, radius 5.0 units |
| Earthbreaker Charge | See §4 |
| Colossus | +40% knockback; collision damage +15 |
| Unrelenting | -0.05s Windup per hit landed, stacks 3x, resets on miss |

---

## 11. Curse Pool — Values

| Curse | Values |
|---|---|
| Glass Cannon | +40% damage dealt / +100% damage taken |
| Greed's Toll | +200% XP from enemies / Hazard timer -20% (faster) |
| Reckless Vigor | +50% gauge gain / -25% Ultimate damage |
| Frail Grip | +1 free Heavy Strike chain hit / Heavy Strike gauge gain = 0 |
| Blood Debt | Full heal now / Max HP -20% for rest of run |
| Iron Curse | Knockback immunity / -15% move speed |
| Starving Blade | +1% damage per 1% HP missing (up to +60% at 1hp) / Healing effects -50% |
| Overclock | +20% attack speed (all Windup/Recovery) / Hyper Armor and Combo-stability effects disabled |

---

## 12. Relics — Values

| Relic | Value |
|---|---|
| Endless Edge (Katana) | No stack cap; per-stack bonus 2% → 1% |
| Deadeye's Promise (Bow) | Always pierces (ignores charge-time requirement) |
| Mountain's Fall (Greatsword) | Ultimate leaves 25% gauge remaining after use |

---

## 13. Weighted Draw Probability (by rarity)

| Tier | Biome 1 weight | Biome 2 weight | Biome 3 weight |
|---|---|---|---|
| Common | 65% | 55% | 45% |
| Rare | 30% | 35% | 40% |
| Epic | 5% | 10% | 15% |

Curses are drawn from their own pool (flat, uniform weight) and don't participate in this table. Legendary (Relics) are excluded entirely — guaranteed-drop only.

---

## 14. Shards — Run-End Award

```
Shards = (LevelsGained × 15) + (DepthReached × 10)
```

Awarded once, at run end (death or victory) — no in-level currency pickups exist. Example: reaching floor 8 at Level 12 → (12 × 15) + (8 × 10) = 180 + 80 = **260 Shards**. *(Multiplier is a first-pass placeholder — needs playtesting against the new 30–60 min run length before being treated as final.)*

---

## 15. Hub Stat System — Ranks & Costs

**Core Stats:** 5 ranks each. Cost curve: `Cost(rank) = BaseCost × rank^1.5`, rounded to nearest 10.

| Stat | Per-Rank Effect | Max (5 ranks) | Base Cost |
|---|---|---|---|
| Max HP | +10 HP | +50 HP | 100 |
| Base Damage | +2 damage | +10 damage | 120 |
| Move Speed | +2% | +10% | 130 |
| Ultimate Gauge Gain | +4% | +20% | 110 |
| XP Gain | +6% | +30% | 90 |
| Dash Cooldown | -4% | -20% | 100 |

**Marks:** single-purchase flat cost (Death Defiance has 2 ranks, all others are 1).

| Trait | Value | Cost |
|---|---|---|
| Death Defiance (rank 1) | Survive lethal hit once/run, heal to 25% HP | 350 |
| Death Defiance (rank 2) | 2nd charge per run | 550 |
| Boiling Blood | +1% damage per 5% HP missing | 300 |
| Warm-Up | Ultimate Gauge starts each floor at 20% | 280 |
| Nerves of Steel | First hit each floor negated | 320 |
| Quick Start | Start each run at +1 partial level of XP (value TBD) | 200 |
| Sixth Sense | Post-floor-5: 1 guaranteed Rare+ upgrade per offer | 400 |
| Steadfast Grip | Curse downsides reduced 15% | 300 |
| Second Skin | -2 flat damage taken, always active | 250 |

**Non-stat unlocks:**

| Unlock | Cost |
|---|---|
| Second Curse Slot | 400 |
| Relic Cache | 500 |

**Relic Vault:** 600 Shards per Relic, purchasable once discovered (per weapon), guarantees that Relic as an offer once in the next run.

---

## 16. XP Curve (open item)

Level thresholds are not yet finalized — needs a first pass tuned against enemy XP-drop values and the new 30–60 min target, then playtest-adjusted. Flag this explicitly as unresolved rather than guessing a curve now.

Per-enemy XP drop values are unresolved for the same reason: the two numbers only make sense tuned together.

---

## 17. Trapped Souls — Values (MVP: implement first row only)

| Soul | Effect |
|---|---|
| The Warden's Soul (MVP) | Slow-moving guardian, tanks hits, taunts nearby enemies |
| The Thief's Soul (Post-MVP) | Periodically steals a buff from a nearby enemy, grants it to player briefly |
| The Wailing Soul (Post-MVP) | Passive aura, weakens nearby enemy damage, increases local Hazard speed |

---

## Open Items Remaining

- Weapon Mastery node effects (3–5 per weapon) — deferred, needs its own design pass since it's usage-gated rather than numeric-only
- Mini-Boss "Overcharge" buff exact values — deferred pending CORE_SYSTEMS §16 clear-trigger decision
- XP curve and per-enemy XP values (§16) — the single largest unresolved number set in this doc
- Trapped Soul numeric values (§17 gives behaviour only — no HP, taunt radius, duration or slot count yet)
- Shard multipliers in §14 — placeholder, revisit once a run actually takes 30–60 min
- **Zyno has no stat row of his own** (§6 points at "an existing Mini-Boss's HP/phases, specific choice TBD"). Which Mini-Boss he reuses is the next decision — it sets his HP, phase count and arena in one go. The Floor 16 structure itself is settled: father first, Zyno after (owner, 2026-08-15)
- **§7's hazard timer contradicts §8's room count:** 90s to clear a floor vs 3–5 rooms at 30–60s each
- Playtesting pass on all values above — first-pass numbers only, expect significant movement once the core loop is actually playable
