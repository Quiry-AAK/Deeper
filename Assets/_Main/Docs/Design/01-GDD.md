# GDD — "Deeper"

## Game Overview

- **Title:** Deeper
- **Pitch:** A woman descends a collapsing shaft, growing stronger with every reckless floor, racing the rising danger below. She believes she is hunting two children her country is evacuating — manipulated by a villain, **Zyno**, into seeing everyone in her path as an enemy. On Floor 16 she confronts her father — the truth breaks there — then faces Zyno himself as the true final battle. Full story: `10-NARRATIVE.md`.
- ⚠️ **NEEDS DECISION:** the game's economy (Ore, Ore Shards, the Hub) still uses mining vocabulary inherited from the old "lone miner" pitch. Confirm whether that vocabulary survives the new premise (she's not a miner) or gets renamed. See `00-DESIGN_CHANGE_BRIEF.md` §3.
- **Genre:** Pixel-art action roguelike (vertical descent)
- **Platform:** PC (Windows/Mac), built in Unity/C#
- **Target Session Length:** 15–25 minutes per run
- **Target Player Experience:** Tense, forward-momentum action with a satisfying power curve within a run, and a longer-term sense of permanent growth across many runs.

## Core Gameplay Loop

1. **Hub:** Player starts in a small surface camp. Spend accumulated Ore Shards on permanent upgrades. Choose starting **Weapon**.
2. **Descend:** Enter the shaft. Move through a sequence of hand-built rooms per floor.
3. **Fight:** Clear enemies in combat rooms using the chosen weapon's basic attack, Heavy Strike, Ultimate, and Dig-Dash.
4. **Collect:** Pick up Ore (run currency) and occasional relics from enemies/chests.
5. **Choose Upgrade:** At the end of each floor, pick 1 of 3 randomized upgrades — or a visible 4th **Curse** option.
6. **Repeat:** Descend deeper. Every 5th floor ends in a Mini-Boss room. Difficulty and hazard pressure increase with depth.
7. **Final Boss:** At the bottom of the mine (floor 16), face the Final Boss.
8. **Win/Die:** Defeat the boss and escape (win), or reach 0 HP / get caught by the rising hazard (die).
9. **Return to Hub:** Ore Shards collected (a separate persistent currency, not run Ore) carry over. Spend on permanent upgrades. Start a new run.

## Player

- **Character:** A single, fixed protagonist — no body or gender selection. She is a woman, cloaked, in light armour, name TBD. There is no inventory or gear-swapping: what she wears never changes and carries no stats (see Weapon below). Full backstory, motivation and the Zyno/father relationship: `10-NARRATIVE.md`.
- **Movement:** 8-directional top-down. Movement **ramps** rather than snapping to full speed: 0.055s to reach full speed, 0.085s to coast to a stop (stopping slower than starting reads as weight). Both values are tunable and can be set to 0 to restore instant on/off velocity if ramping ever feels sluggish.
- **Health:** Starts at a base HP value (see BALANCE.md), increased by run upgrades and permanent upgrades.
- **Weapon:** Player chooses **1 of 3 weapons in the Hub before descending**. Locked for the full run. All 3 are unlocked from the start — no gating. The weapon is the single build-defining choice — it fully determines the player's kit (basic attack, Heavy Strike, Ultimate).
  - **Katana** — Fast light melee. Low damage per hit, quick windup/recovery, short range. **Signature trait: Combo Counter** — consecutive hits without missing or taking damage build a small stacking damage bonus, resets on miss or on taking a hit.
  - **Bow** — Ranged. Trades close-range safety margin for damage at distance. No ammo cost, just cooldown. **Signature trait: Charge Shot** — hold attack to charge bonus damage/pierce, or release early for a fast weak shot.
  - **Greatsword** — Heavy melee. High damage, wide arc, slow windup/recovery, big whiff punish window. **Signature trait: Hyper Armor** — can't be knocked back and takes reduced (not zero) damage during windup; distinct from Dig-Dash i-frames, which remain the only true invulnerability.
  - Each weapon has a clearly different function (fast/low-risk, ranged/positional, slow/high-commitment), giving weapon choice real weight both build-wise and moment-to-moment.
- **Controls:** `LClick` — basic attack. `RClick` — Heavy Strike (slower, stronger, weapon-specific variant). `LShift` — Dig-Dash. `R` — Ultimate (weapon-specific, gauge-gated, see below). No separate ability-select keys — the weapon itself defines the full kit.
- **Attack:** Weapon-dependent swing/shot, short cooldown, hitbox shape and timing vary by weapon (see Combat section). **Basic Attack is a 2-hit chain that loops** (hit 1 → hit 2 → hit 1 again, reusing the same two animations, so it reads as a continuous flurry rather than a fixed 2-count) — each hit re-enters Windup→Active→Recovery and the chain breaks if the player doesn't press again within a 0.25s window after Recovery. Free from the start on all 3 weapons, not upgrade-gated. ⚠️ **NEEDS DECISION:** does each chain hit deal equal damage, or should a later hit act as a finisher? Does the chain interact with the Katana's Combo Counter beyond each hit adding a stack? See `00-DESIGN_CHANGE_BRIEF.md` §7b.
- **Heavy Strike (RClick):** A single stronger, slower hit per weapon at base. The upgrade pool can modify this slot directly — extending it into a 2–3 hit chain, or replacing it outright with a repurposed effect (e.g., a Dynamite Throw or Grapple Pull variant, now framed as a Heavy Strike replacement rather than a separate Hub-selected ability). This is the primary build-customization slot within a run.
- **Ultimate (R):** Weapon-specific, tied to that weapon's signature trait. Two shapes exist: an **Attack** (a burst of damage, e.g. Bow's full-charge piercing shot, Greatsword's ground-slam AoE) or a **Buff** (a temporary self-empowerment with no damage of its own). **Katana's Ultimate is now a Buff**, not the "combo finisher" attack originally documented: a short cast raises an aura on her and the katana, and for its duration she deals more damage, attacks faster, and moves faster — every attack lands *through* the buff rather than the Ultimate being a hit in its own right. No cooldown either way — gated by an **Ultimate Gauge** that fills as a flat 1% per landed attack of any kind, from any weapon (basic and Heavy Strike both contribute equally). Activating the Ultimate fully drains the gauge back to zero. A rare in-run upgrade per weapon (**Alt Ultimate**) can replace the default effect with a more mobile, skill-style alternative — see CONTENT_DESIGN.md. ⚠️ **NEEDS DECISION (several, see `00-DESIGN_CHANGE_BRIEF.md` §7f–§7h):** Katana's Combo Counter is consumed by the Ultimate cast but currently converts into nothing (a real gameplay hole, not just a doc gap); the buff's numbers (duration, damage/speed bonus) are unbalanced placeholders; and whether Alt Ultimates can also be Buffs, or must stay Attacks, is undecided.
- **Defense:** No blocking. Damage mitigation comes from avoidance (dig-dash i-frames), Hyper Armor (Greatsword only, partial), and HP/armor upgrades.
- **Dodge/Mobility:** Dig-Dash — short dash in facing direction, grants brief invulnerability frames, can break through cracked walls (used for shortcuts/flanking). **Dash-Attack Cancel:** the recovery frames of any weapon's swing/strike can be canceled early into a dash — no tutorial prompt, a piece of movement tech for players to discover and optimize around.
- **Resource Systems:**
  - **Ore (run currency):** Collected during a run, converted to Ore Shards at run end based on amount collected and depth reached. No in-run shop.
  - **Ore Shards (meta currency):** Permanent, earned per run, spent in the Hub.
- **Death:** Run ends immediately. Player returns to Hub with earned Ore Shards. No mid-run checkpoints/respawns.

## Combat

- **Attack Behavior:** Weapon-dependent (Katana / Bow / Greatsword — see Player section). Each weapon implements a shared attack interface (basic attack, Heavy Strike, Ultimate) so the underlying system stays uniform even though feel differs.
- **Attack Timing:** Fixed windup → active hitbox/hit frame(s) → recovery, per weapon (Bow's windup is variable-length due to Charge Shot). Values tuned in BALANCE.md.
- **Attack Movement:** Attacks are not rooted in place — each swing drives a short forward lunge (0.75/1.15/0.9 world units for Basic/Heavy/Greatsword, on an ease-out curve, direction locked at the start of the hit). The player can't steer mid-swing, but she isn't stationary either — this is what gives attacks their sense of weight.
- **Hit Detection:** Katana and Greatsword use melee arc/box hitboxes in front of the player; Bow uses a projectile, reusing the hit-detection pattern already established for enemy ranged attacks.
- **Damage:** Flat damage per hit within a run — no crit system anywhere in the game, keeping in-run math simple and readable. Permanent power growth comes from the Hub Stat System's Core Stats and Miner's Traits (see Progression), not from a crit roll.
- **Enemy Damage:** Flat damage per enemy attack, telegraphed with a short wind-up animation/color flash.
- **Player Survivability:** Base HP + dig-dash i-frames + Hyper Armor (Greatsword) + optional defensive upgrades (see CONTENT_DESIGN.md).
- **Invulnerability Frames:** Granted only during dig-dash active frames, and briefly after taking damage (standard hit-stun immunity). Hyper Armor reduces damage but does not grant invulnerability — it stays mechanically distinct from Dig-Dash.
- **Feedback:** Screen shake on hit, hit-flash on enemies, damage numbers optional (nice-to-have), sound cue per hit type, per weapon where relevant.
- **Hazard Kills:** Pushing or dashing an enemy into the Rising Hazard's edge instakills it — a tactical option layered onto the existing Damage and Hazard systems.

## Roguelike Structure

- **Run Start:** Player exits Hub into Floor 1 of Biome 1 (Upper Caves), with chosen Weapon.
- **Rooms/Encounters:** Each floor = 1–3 hand-built rooms pulled from a per-biome room pool, connected linearly downward. No branching paths (keeps navigation simple and always-forward).
  - **Wave Rooms:** A subset of existing Combat Room layouts (capped at 1–2 per biome's pool) are flagged as Wave Rooms. Instead of spawning all enemies at once, enemies spawn in 2–3 triggered batches — the next wave begins when the current one drops to roughly one enemy remaining, not on a pure timer, so players can't stall it out. The room stays locked until the final wave clears, reusing the existing room-lock logic. No new enemy types are introduced for Wave Rooms — only existing per-biome enemies, resequenced.
- **Secret Floors:** Some floors contain a visible locked door requiring a key dropped by a rare elite enemy encountered earlier in that biome. The door leads to a small bonus vault room (large Ore payout or a guaranteed Legendary-tier upgrade) — reachable, but costs time against the rising hazard, creating a risk/clock decision.
- **Rewards:** Ore drops from enemies/chests; upgrade choice at end of each floor; occasional relics (see Progression).
- **Randomization:** Room order within a biome's pool is shuffled per run (deterministic shuffle, not live procgen). Upgrade offers are randomly drawn (weighted) from the upgrade pool each floor.
- **Upgrades:** Player picks 1 of 3 offered upgrades at the end of each floor, drawn from a shared pool (HP, Ore, Speed, Dash) plus a weapon-specific sub-pool matching the equipped weapon — including Heavy Strike modifiers (extra hits in the chain, full replacement effects), Ultimate effect modifiers, and Ultimate Gauge modifiers (faster gain per hit, gain on taking damage, etc.) (see CONTENT_DESIGN.md for full tables).
- **Curses:** Alongside the normal 3 upgrade offers, a 4th slot always presents a visible **Curse** — a high-risk, high-reward modifier (e.g., "+40% damage, but take double damage" or "Enemies drop 3x Ore, but hazard rises 20% faster"). Optional every time; gives runs a distinct identity for players who want them.
- **Difficulty Progression:** Enemy HP/damage scale up per biome tier; rising hazard timer speeds up in later biomes.
- **Boss:** Mini-boss every 5th floor (end of each biome); Final Boss at floor 16. Bosses include at least one phase or mechanic that meaningfully rewards or punishes specific weapon types (e.g., a breakable shield that Greatsword drops in one hit but Katana needs several, or slow projectiles Bow players can snipe for bonus damage but melee players must dodge). Not a hard gate — just a moment where weapon choice matters at peak tension.
- **Death:** Immediate run end, return to Hub.
- **Restart:** New run begins from Hub with fresh run-state but persistent meta-progression.

## Biome Identity

Each biome has a distinct environmental mechanic and hazard "personality," not just adjusted stats:

- **Upper Caves (Biome 1):** Certain floor tiles show warning cracks and collapse after a few seconds of standing on them — live positioning pressure during fights. Hazard: a visible, audible rockfall front.
- **Flooded Tunnels (Biome 2):** Water tiles slow movement and swings; currents can push the player or enemies toward hazards or ledges. Bow travels unaffected through water, giving it a situational edge; melee is punished for wading in. Hazard: a rising water level that also changes room geometry — low areas flood first, forcing route changes mid-floor.
- **Molten Depths (Biome 3):** Periodic lava geysers erupt from marked tiles, forcing mid-fight repositioning. Greatsword's Hyper Armor is especially valuable here — tanking a geyser tick and continuing to swing. Hazard: a spreading lava flow that leaves lightly damaging scorched ground behind it, even after the front has passed.

All three reuse the same underlying Rising Hazard timer system — only presentation and secondary effects differ per biome.

## Progression

**Run-based progression (resets every run):**
- HP upgrades gained
- Damage/attack upgrades gained (shared and weapon-specific)
- Heavy Strike modifiers gained (chain extensions, replacement effects)
- Ultimate effect and Ultimate Gauge modifiers gained
- Curses taken
- Ore collected this run

**Permanent/meta progression (persists across runs):**
- Ore Shards (currency)
- **Hub Stat System:** rank-based Core Stats (Max HP, Base Damage, Move Speed, Ultimate Gauge Gain, Ore Gain, Dash Cooldown) plus **Miner's Traits** — unique named effects in the style of Hades' Mirror of Night (e.g., Death Defiance, Boiling Blood, Warm-Up) — plus two flat non-stat unlocks (extra Curse slot, Relic Cache). Full table in CONTENT_DESIGN.md.
- **Weapon Mastery:** a small permanent track (3–5 nodes) per weapon, unlocked by *using* that weapon across runs rather than by spending Shards alone — encourages mastering all three instead of settling on one.
- **Relics:** the Legendary-tier upgrade rarity, one per weapon, only offered when that weapon is equipped — resolves "relic" as a concrete term rather than undefined flavor text. Once found at least once, a relic becomes purchasable from the **Relic Vault** in the Hub at high Ore Shard cost, guaranteeing it as an offer once per future run.

This remains a **small, bounded** meta-progression tree — not a skill tree, a short flat list plus the two additions above (Weapon Mastery, Relic Vault) — see CONTENT_DESIGN.md.

## Game Flow

1. Launch game → Main Menu → Hub.
2. Hub: view/spend Ore Shards, select Weapon, start run.
3. Run: descend through Biome 1 (floors 1–5) → Mini-Boss 1 → Biome 2 (floors 6–10) → Mini-Boss 2 → Biome 3 (floors 11–15) → Mini-Boss 3 → Floor 16: Final Boss.
4. Win: defeat Final Boss → Victory screen → return to Hub with all earned Ore Shards.
5. Die: death screen showing depth reached and Ore Shards earned → return to Hub.
6. Repeat.

## UI

- **HUD (in-run):** HP bar, equipped weapon icon, ~~Heavy Strike cooldown icon~~, Ultimate Gauge (fill meter, not a cooldown), current floor/depth indicator, hazard proximity meter, Ore counter, Wave indicator (e.g. "Wave 2/3") shown only inside Wave Rooms. ⚠️ **NEEDS DECISION:** Heavy Strike has no cooldown anywhere in CORE_SYSTEMS or BALANCE — it's gated only by its own 0.3–0.65s Windup/Recovery. Either give it a real cooldown (a balance change) or drop this HUD element for good. See `00-DESIGN_CHANGE_BRIEF.md` §7n.
- **Upgrade Screen:** 3 standard cards (icon, name, short description) plus a 4th, visually distinct Curse card.
- **Hub Screen:** Ore Shard total, list of permanent upgrades (purchased/available/locked), Weapon selector, Weapon Mastery progress per weapon, Relic Vault, "Descend" button.
- **Death/Victory Screen:** Depth reached, Ore Shards earned, run time, weapon used, "Return to Hub" button.

## Audio

- **Music:** One looping track per biome (3 total) + 1 boss track (reused for all mini-bosses) + 1 final boss track + 1 hub track. 5 tracks total.
- **SFX (must-have):** Per-weapon basic attack, Heavy Strike, and Ultimate sounds (Katana, Bow, Greatsword), Ultimate Gauge full cue, player hit-taken, player death, enemy hit, enemy death, dig-dash, dash-attack cancel, upgrade pick, curse pick, Ore pickup, floor transition, wave trigger, boss intro roar, victory jingle.

## Victory

The player wins by defeating the Final Boss on Floor 16 and surviving the escape sequence that follows (a short, hazard-timed dash back to the surface — reuses the dig-dash and rising-hazard mechanics already built, no new systems required).
