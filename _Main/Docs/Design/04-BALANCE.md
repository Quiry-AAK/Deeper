# BALANCE — "Deeper"

Numeric tuning for every system/content item defined in 01-GDD.md, 02-CORE_SYSTEMS.md, and 03-CONTENT_DESIGN.md. All values are **first-pass placeholders** meant to be internally consistent, not final — expect these to move after playtesting. Percentages are additive unless noted otherwise.

---

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

**Gauge gain per landed hit (base, before upgrades):**

| Weapon | Basic Attack | Heavy Strike |
|---|---|---|
| Katana | +8% | +15% |
| Bow | +6% (uncharged) / +12% (full charge) | +15% |
| Greatsword | +10% | +20% |

**Ultimate damage/effect (base, before Alt Ultimate or mods):**

| Weapon | Ultimate | Base Value |
|---|---|---|
| Katana | Combo Finisher | 40 damage + 5 per Combo Counter stack consumed |
| Bow | Full-Charge Piercing Shot | 35 damage, pierces all enemies in line |
| Greatsword | Ground Slam | 45 damage, radius 3.0 units, knockback |

**Alt Ultimate damage (same total-output budget as default, redistributed for mobility):**

| Weapon | Alt Ultimate | Base Value |
|---|---|---|
| Katana | Thousand Cuts | 6 hits × 8 damage over 1.2s, player-mobile |
| Bow | Rain of Arrows | 10 arrows × 5 damage over 2.5s in a 2.5-unit zone |
| Greatsword | Earthbreaker Charge | 4 damage per 0.1s tick while charging (up to 3s) + 30 damage slam on impact/end |

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

---

## 6. Mini-Boss & Final Boss

| Boss | HP | Phases | Notes |
|---|---|---|---|
| The Collapsed King (Biome 1) | 350 | 2 | Phase 2 at 50% HP: slam AoE cadence increases. Weapon-check: rubble shield = 1 hit (Greatsword) / 3 hits (others) |
| The Drowned Custodian (Biome 2) | 450 | 2 | Phase 2 at 50% HP: water hazard covers 75% of room. Homing projectile speed: 2.5 units/sec (snipeable by Bow for 1.5x damage) |
| The Molten Sentinel (Biome 3) | 600 | 3 | Geyser eruption every 8s; Hyper Armor absorbs 1 tick fully at 40% reduction |
| The Depth Warden (Final Boss) | 1200 | 3 | Phase 1: collapsing-tile theme, Phase 2: rising water theme, Phase 3: lava geyser theme. Weapon-check moment in Phase 3 |

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
| Rooms per floor | 1–3 |
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
| Prospector's Eye | Common | +20% Ore from enemies |
| Lucky Vein | Common | +30% Ore from chests |
| Ore Magnet | Rare | Pull radius 3.0 units |
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
| Gauge: Vengeance | +5% gauge on taking damage |
| Gauge: Adrenal Rush | +50% gauge gain while at 8+ stacks |
| Finisher+ | Conversion rate +40% |
| Echo Slash | 2nd Finisher hit at 60% value |
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
| Gauge: Vengeance | +5% gauge on taking damage |
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
| Greed's Toll | +200% Ore from enemies / Hazard timer -20% (faster) |
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

## 14. Ore → Ore Shards Conversion

```
OreShards = floor(OreCollected × 0.5) + (DepthReached × 10)
```

Example: dying on floor 8 with 200 Ore collected → floor(200 × 0.5) + (8 × 10) = 100 + 80 = **180 Ore Shards**.

---

## 15. Hub Stat System — Ranks & Costs

**Core Stats:** 5 ranks each. Cost curve: `Cost(rank) = BaseCost × rank^1.5`, rounded to nearest 10.

| Stat | Per-Rank Effect | Max (5 ranks) | Base Cost |
|---|---|---|---|
| Max HP | +10 HP | +50 HP | 100 |
| Base Damage | +2 damage | +10 damage | 120 |
| Move Speed | +2% | +10% | 130 |
| Ultimate Gauge Gain | +4% | +20% | 110 |
| Ore Gain | +6% | +30% | 90 |
| Dash Cooldown | -4% | -20% | 100 |

**Miner's Traits:** single-purchase flat cost (Death Defiance has 2 ranks, all others are 1).

| Trait | Value | Cost |
|---|---|---|
| Death Defiance (rank 1) | Survive lethal hit once/run, heal to 25% HP | 350 |
| Death Defiance (rank 2) | 2nd charge per run | 550 |
| Boiling Blood | +1% damage per 5% HP missing | 300 |
| Warm-Up | Ultimate Gauge starts each floor at 20% | 280 |
| Nerves of Steel | First hit each floor negated | 320 |
| Old Prospector | +50 starting Ore | 200 |
| Miner's Sixth Sense | Post-floor-5: 1 guaranteed Rare+ upgrade per offer | 400 |
| Steadfast Grip | Curse downsides reduced 15% | 300 |
| Second Skin | -2 flat damage taken, always active | 250 |

**Non-stat unlocks:**

| Unlock | Cost |
|---|---|
| Second Curse Slot | 400 |
| Relic Cache | 500 |

**Relic Vault:** 600 Ore Shards per Relic, purchasable once discovered (per weapon), guarantees that Relic as an offer once in the next run.

---

## Open Items Remaining

- Weapon Mastery node effects (3–5 per weapon) — deferred, needs its own design pass since it's usage-gated rather than numeric-only
- Mini-Boss "Overcharge" buff exact values — deferred pending CORE_SYSTEMS §12 clear-trigger decision
- Playtesting pass on all values above — first-pass numbers only, expect significant movement once the core loop is actually playable
