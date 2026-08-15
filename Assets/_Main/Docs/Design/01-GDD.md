# GDD — "Deeper"

## Game Overview

- **Title:** Deeper
- **Pitch:** A manipulated girl descends into a collapsing shaft, cutting down what she's been told are monsters — not knowing they are her own village. She grows stronger with every reckless floor, racing the danger below, and racing a truth she isn't ready to see.
- **DECIDED: the mining-vocabulary economy is renamed.** She isn't a miner — mining is now just environmental flavor for the setting, not the game's theme, and can change further later. Permanent currency **Shards** (was "Ore Shards"), the meta-progression trait tier **Marks** (was "Miner's Traits"). ~~Run currency **Glimmer** (was "Ore")~~ — **the run currency no longer exists at all**: the session changelog replaced it with **XP**, and Shards are awarded once at run end (see Core Gameplay Loop and BALANCE §14). The mining-flavored upgrade renames from that pass are likewise superseded, since those upgrades now scale XP: Keen Eye → **Quick Study**, Glimmer Magnet → **Insight Magnet**, Lucky Find → **deleted** (no chests), Head Start → **Quick Start**. Sixth Sense keeps its new name.
- **Genre:** Pixel-art action roguelike (vertical descent)
- **Platform:** PC (Windows/Mac), built in Unity/C#
- **Target Session Length:** 30–60 minutes per run
- **Target Player Experience:** Tense, forward-momentum action with a satisfying power curve within a run, and a longer-term sense of permanent growth across many runs.

## Narrative Premise

**Protagonist:** A girl manipulated by **Zyno**, who has convinced her that her own village are monstrous enemies. She descends to hunt them down, unaware. Her real motive — surfaced only gradually — is that Zyno used her desire to protect two children (the last descendants of an unspecified elite bloodline) to turn her against her own people.

**Floor 16 — two fights, in this order:** **The Depth Warden is her father**, fought first, using the boss design already budgeted (multi-phase, weapon-check moment — its hazard-themed phases need re-theming, see CORE_SYSTEMS §7) — framed as an ordinary boss on a first playthrough, with no telegraphing that breaks the "he's a monster" illusion. Beating him does not end the floor: **Zyno is fought immediately after, as the true Final Boss.** Biome 1's Mini-Boss remains **The Collapsed King** (CONTENT_DESIGN §5).

**Antagonist:** Zyno — present throughout the descent via the Whisper Layer (see Core Systems), then fought in person on Floor 16 after the father. His MVP fight reuses an existing Mini-Boss moveset/arena, palette-swapped with his own dialogue and identity; a bespoke moveset is SHOULD SHIP (CONTENT_DESIGN §5, 08-MVP.md).

**Visual note:** Biome 1 art stays mine-themed as already built/planned — the narrative reframe is a writing/data layer on top of existing art, not an art change, for MVP. Biomes 2–3 narrative framing is an open item, not required for MVP.

> ✅ **RESOLVED (owner, 2026-08-15).** The session changelog had moved the father to Biome 1 as a Mini-Boss and kept Zyno out of the MVP entirely. The owner reversed that: **the father is the Final Boss, fought before Zyno**, exactly as `03-CONTENT_DESIGN.md` §5 already describes. Floor 16 therefore has its identity back and Zyno stays MUST SHIP. Everything else from the changelog (XP/leveling, 3–5 rooms, run-end Shards, Evolutions, Trapped Souls, the narrative systems below) is unaffected. See `Docs/00-DESIGN_CHANGE_BRIEF.md` §11.

## Core Gameplay Loop

1. **Hub:** Player starts in a small surface camp. Spend accumulated Shards on permanent upgrades. Choose starting **Weapon**.
2. **Descend:** Enter the shaft. Move through a sequence of hand-built rooms per floor.
3. **Fight:** Clear enemies in combat rooms using the chosen weapon's basic attack, Heavy Strike, Ultimate, and Dig-Dash. Enemies drop **XP** on death.
4. **Level Up:** When XP crosses the level threshold, the game pauses and presents an upgrade offer — 3 randomized upgrades drawn from the full shared + weapon pool in one weighted draw (not floor-gated, not tier-gated — Common/Rare/Epic can appear in the same offer), plus a visible 4th **Curse** option. Every 5th level, this is replaced by an **Evolution** offer instead (see Core Systems).
5. **Repeat:** Descend deeper. Floors pull **3–5 rooms** from a per-biome pool via reshuffling bag (see Core Systems §8). Every 5th floor ends in a Mini-Boss room. Difficulty increases with depth.
6. **Final Boss:** At the bottom of the mine (floor 16), face the Final Boss.
7. **Win/Die:** Defeat the boss and escape (win), or reach 0 HP (die).
8. **Return to Hub:** Shards earned carry over — computed once at run end from Levels Gained and Depth Reached (BALANCE §14), not collected during the run. Spend on permanent upgrades. Start a new run.

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
- **Ultimate (R):** Weapon-specific, tied to that weapon's signature trait. Two shapes exist: an **Attack** (a burst of damage, e.g. Bow's full-charge piercing shot, Greatsword's ground-slam AoE) or a **Buff** (a temporary self-empowerment with no damage of its own). **Katana's Ultimate is a Buff**, not the "combo finisher" attack originally documented: a short cast raises an aura on her and the katana, and for its duration she deals more damage, attacks faster, and moves faster — every attack lands *through* the buff rather than the Ultimate being a hit in its own right. It doesn't touch the Combo Counter at all — the two systems run independently. No cooldown either way — gated by an **Ultimate Gauge** that fills per landed attack at a per-weapon rate (see BALANCE.md). Activating the Ultimate fully drains the gauge back to zero. A rare in-run upgrade per weapon (**Alt Ultimate**) can replace the default effect with a more mobile, skill-style alternative — Katana's stays Attack-shaped (real damage, full player-steered movement) even though its default Ultimate is now a Buff, giving a genuine choice of shape. See CONTENT_DESIGN.md. The buff's numbers (duration, damage/speed bonus) remain unbalanced first-pass placeholders — see BALANCE.md.
- **Defense:** No blocking. Damage mitigation comes from avoidance (dig-dash i-frames), Hyper Armor (Greatsword only, partial), and HP/armor upgrades.
- **Dodge/Mobility:** Dig-Dash — short dash in facing direction, grants brief invulnerability frames, can break through cracked walls (used for shortcuts/flanking). **Dash-Attack Cancel:** the recovery frames of any weapon's swing/strike can be canceled early into a dash — no tutorial prompt, a piece of movement tech for players to discover and optimize around.
- **Resource Systems:**
  - **XP (run resource):** Dropped by enemies on death, collected during the run. Drives leveling, which is what triggers upgrade offers (see Core Gameplay Loop and CORE_SYSTEMS §12). Not a currency — there is no in-run shop and nothing to spend it on. It replaces the run currency formerly called Ore, then Glimmer.
  - **Shards (meta currency):** Permanent, spent in the Hub. Awarded **once at run end**, computed from Levels Gained and Depth Reached (BALANCE §14) — there is no in-level pickup object.
- **Death:** Run ends immediately. Player returns to Hub with earned Shards. No mid-run checkpoints/respawns.

## Combat

- **Attack Behavior:** Weapon-dependent (Katana / Bow / Greatsword — see Player section). Each weapon implements a shared attack interface (basic attack, Heavy Strike, Ultimate) so the underlying system stays uniform even though feel differs.
- **Attack Timing:** Fixed windup → active hitbox/hit frame(s) → recovery, per weapon (Bow's windup is variable-length due to Charge Shot). Values tuned in BALANCE.md.
- **Attack Movement:** Attacks are not rooted in place — each swing drives a short forward lunge (0.75/1.15/0.9 world units for Basic/Heavy/Greatsword, on an ease-out curve, direction locked at the start of the hit). The player can't steer mid-swing, but she isn't stationary either — this is what gives attacks their sense of weight.
- **Hit Detection:** Katana and Greatsword use melee arc/box hitboxes in front of the player; Bow uses a projectile, reusing the hit-detection pattern already established for enemy ranged attacks.
- **Damage:** Flat damage per hit within a run — no crit system anywhere in the game, keeping in-run math simple and readable. Permanent power growth comes from the Hub Stat System's Core Stats and Marks (see Progression), not from a crit roll.
- **Enemy Damage:** Flat damage per enemy attack, telegraphed with a short wind-up animation/color flash.
- **Player Survivability:** Base HP + dig-dash i-frames + Hyper Armor (Greatsword) + optional defensive upgrades (see CONTENT_DESIGN.md).
- **Invulnerability Frames:** Granted only during dig-dash active frames, and briefly after taking damage (standard hit-stun immunity). Hyper Armor reduces damage but does not grant invulnerability — it stays mechanically distinct from Dig-Dash.
- **Feedback:** Screen shake on hit, hit-flash on enemies, damage numbers optional (nice-to-have), sound cue per hit type, per weapon where relevant.
- ~~**Hazard Kills:**~~ **Removed with the Rising Hazard (owner, 2026-08-15).** There is no hazard edge to push enemies into, so the knockback-as-a-kill tactic no longer exists. Greatsword's Colossus upgrade (CONTENT_DESIGN §2c) still rewards knockback via wall/enemy collision damage; nothing else replaces this.

## Roguelike Structure

- **Run Start:** Player exits Hub into Floor 1 of Biome 1 (Upper Caves), with chosen Weapon.
- **Rooms/Encounters:** Each floor = 3–5 hand-built rooms pulled from a per-biome room pool via a reshuffling bag, connected linearly downward. No branching paths (keeps navigation simple and always-forward). Layouts will repeat within a run — see CORE_SYSTEMS §8.
  - **Wave Rooms:** A subset of existing Combat Room layouts (capped at 1–2 per biome's pool) are flagged as Wave Rooms. Instead of spawning all enemies at once, enemies spawn in 2–3 triggered batches — the next wave begins when the current one drops to roughly one enemy remaining, not on a pure timer, so players can't stall it out. The room stays locked until the final wave clears, reusing the existing room-lock logic. No new enemy types are introduced for Wave Rooms — only existing per-biome enemies, resequenced.
- **Secret Floors:** Some floors contain a visible locked door requiring a key dropped by a rare elite enemy encountered earlier in that biome. The door leads to a small bonus vault room (large XP payout or a guaranteed Legendary-tier upgrade). ⚠️ **HOLE — the Rising Hazard was cut (owner, 2026-08-15), and the clock it created *was* this feature's cost.** With no timer, a Secret Floor is pure upside and the risk/reward decision is gone. It needs a new cost or it stops being a decision.
- **Trapped Souls:** Some floors contain a bound soul the player can free, granting a persistent in-run effect (CORE_SYSTEMS §14). ⚠️ **Same hole:** "costs real time against the Hazard" was the price of freeing one.
- **Rewards:** XP drops from enemies; upgrade choice on level-up; occasional relics (see Progression).
- **Randomization:** Room order within a biome's pool is drawn per run via a reshuffling bag (not live procgen). Upgrade offers are randomly drawn (weighted) from the upgrade pool on each level-up.
- **Upgrades:** On each level-up, the player picks 1 of 3 offered upgrades, drawn in a single weighted draw across the shared pool (HP, XP, Speed, Dash) *plus* the weapon-specific sub-pool matching the equipped weapon — including Heavy Strike modifiers (extra hits in the chain, full replacement effects), Ultimate effect modifiers, and Ultimate Gauge modifiers (faster gain per hit, gain on taking damage, etc.) (see CONTENT_DESIGN.md for full tables). Every 5th level offers an **Evolution** instead (CORE_SYSTEMS §13).
- **Curses:** Alongside the normal 3 upgrade offers, a 4th slot always presents a visible **Curse** — a high-risk, high-reward modifier (e.g., "+40% damage, but take double damage"). Optional every time; gives runs a distinct identity for players who want them. ⚠️ **One Curse lost its downside:** Greed's Toll traded XP for a faster hazard — see CONTENT_DESIGN §3 / BALANCE §11.
- **Difficulty Progression:** Enemy HP/damage scale up per biome tier. ⚠️ **This is now the only escalation axis** — the hazard timer speeding up per biome was the other one.
- **Boss:** Mini-boss every 5th floor (end of each biome); Final Boss at floor 16. Bosses include at least one phase or mechanic that meaningfully rewards or punishes specific weapon types (e.g., a breakable shield that Greatsword drops in one hit but Katana needs several, or slow projectiles Bow players can snipe for bonus damage but melee players must dodge). Not a hard gate — just a moment where weapon choice matters at peak tension.
- **Death:** Immediate run end, return to Hub.
- **Restart:** New run begins from Hub with fresh run-state but persistent meta-progression.

## Biome Identity

**DECIDED (owner, 2026-08-15): the Rising Hazard is cut from the game.** No hazard front, no per-biome timer, no chase. Each biome keeps its *environmental* mechanic — those were always separate micro-systems — but loses the pursuing hazard half of its identity:

- **Upper Caves (Biome 1):** Certain floor tiles show warning cracks and collapse after a few seconds of standing on them — live positioning pressure during fights. ~~Hazard: rockfall front.~~
- **Flooded Tunnels (Biome 2):** Water tiles slow movement and swings; currents can push the player or enemies toward ledges. Bow travels unaffected through water, giving it a situational edge; melee is punished for wading in. ~~Hazard: rising water level that changes room geometry.~~
- **Molten Depths (Biome 3):** Periodic lava geysers erupt from marked tiles, forcing mid-fight repositioning. Greatsword's Hyper Armor is especially valuable here — tanking a geyser tick and continuing to swing. ~~Hazard: spreading lava flow leaving scorched ground.~~

⚠️ **Two holes this leaves.** First, **the game no longer has a clock** — nothing pushes the player downward, and the pitch's "racing the danger below" now describes a mechanic that doesn't exist. Whether descent pressure comes back in another form (a per-floor timer, an escalating spawn rate, something else) or the game becomes purely combat-paced is undecided. Second, **Biomes 2 and 3 are thinner than Biome 1**: cracked tiles are a real fight mechanic, while "water slows you" and "geysers erupt" carried less of the identity than the water level and lava flow did (DESIGN_RULES Rule 5 requires biomes to differ mechanically, not just in stats).

## Progression

**Run-based progression (resets every run):**
- Levels gained this run (XP earned)
- HP upgrades gained
- Damage/attack upgrades gained (shared and weapon-specific)
- Heavy Strike modifiers gained (chain extensions, replacement effects)
- Ultimate effect and Ultimate Gauge modifiers gained
- Evolution taken per weapon (every 5th level, CORE_SYSTEMS §13)
- Trapped Souls freed (CORE_SYSTEMS §14)
- Curses taken

> **Shards** (the persistent Hub currency) are now awarded **only at run end**, computed from Depth Reached and Levels Gained — there is no in-level currency pickup object. See BALANCE.md for the formula.

**Permanent/meta progression (persists across runs):**
- Shards (currency)
- **Hub Stat System:** rank-based Core Stats (Max HP, Base Damage, Move Speed, Ultimate Gauge Gain, XP Gain, Dash Cooldown) plus **Marks** — unique named effects in the style of Hades' Mirror of Night (e.g., Death Defiance, Boiling Blood, Warm-Up) — plus two flat non-stat unlocks (extra Curse slot, Relic Cache). Full table in CONTENT_DESIGN.md.
- **Weapon Mastery:** a small permanent track (3–5 nodes) per weapon, unlocked by *using* that weapon across runs rather than by spending Shards alone — encourages mastering all three instead of settling on one.
- **Relics:** the Legendary-tier upgrade rarity, one per weapon, only offered when that weapon is equipped — resolves "relic" as a concrete term rather than undefined flavor text. Once found at least once, a relic becomes purchasable from the **Relic Vault** in the Hub at high Shard cost, guaranteeing it as an offer once per future run.

This remains a **small, bounded** meta-progression tree — not a skill tree, a short flat list plus the two additions above (Weapon Mastery, Relic Vault) — see CONTENT_DESIGN.md.

## Game Flow

1. Launch game → Main Menu → Hub.
2. Hub: view/spend Shards, select Weapon, start run.
3. Run: descend through Biome 1 (floors 1–5) → Mini-Boss 1 → Biome 2 (floors 6–10) → Mini-Boss 2 → Biome 3 (floors 11–15) → Mini-Boss 3 → Floor 16: Final Boss.
4. Win: defeat Final Boss → Victory screen → return to Hub with all earned Shards.
5. Die: death screen showing depth reached and Shards earned → return to Hub.
6. Repeat.

## UI

- **HUD (in-run):** HP bar, equipped weapon icon, Ultimate Gauge (fill meter, not a cooldown), current floor/depth indicator, XP bar + current level, Wave indicator (e.g. "Wave 2/3") shown only inside Wave Rooms, and the Whisper Layer line area (CORE_SYSTEMS §15). **DECIDED: no Heavy Strike cooldown icon.** Heavy Strike has no cooldown anywhere in CORE_SYSTEMS or BALANCE — it's gated only by its own 0.3–0.65s Windup/Recovery, and that stays true. The HUD element is dropped for good.
- **Upgrade Screen:** 3 standard cards (icon, name, short description) plus a 4th, visually distinct Curse card. Opens on level-up (game paused), not at floor end. Every 5th level it is replaced by the Evolution offer — 2–3 mutually exclusive cards (CORE_SYSTEMS §13).
- **Hub Screen:** Shard total, list of permanent upgrades (purchased/available/locked), Weapon selector, Weapon Mastery progress per weapon, Relic Vault, "Descend" button.
- **Death/Victory Screen:** Depth reached, Shards earned, run time, weapon used, "Return to Hub" button.

## Audio

- **Music:** One looping track per biome (3 total) + 1 boss track (reused for all mini-bosses) + 1 final boss track + 1 hub track. 5 tracks total.
- **SFX (must-have):** Per-weapon basic attack, Heavy Strike, and Ultimate sounds (Katana, Bow, Greatsword), Ultimate Gauge full cue, player hit-taken, player death, enemy hit, enemy death, dig-dash, dash-attack cancel, upgrade pick, curse pick, XP pickup, level-up, floor transition, wave trigger, boss intro roar, victory jingle.

## Victory

The player wins by defeating the Final Boss on Floor 16 and surviving the escape sequence that follows — a short, timed dash back to the surface on a fixed countdown (BALANCE §7). ⚠️ **This is now the only timer in the game.** It used to be justified as reusing the Rising Hazard system; with that cut, the escape sequence is a small standalone system, and whether it survives at all is worth a deliberate call rather than inheritance.
