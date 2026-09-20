# GDD — "Deeper"

## Game Overview

- **Title:** Deeper
- **Pitch:** A manipulated woman descends into a collapsing mineshaft, cutting down what she's been told are monsters — not knowing they are her own people. She grows stronger with every reckless floor, closing in on a truth she isn't ready to see.
- **Setting vs. economy:** Mining is environmental flavor for the setting, not the game's theme. The permanent meta-currency is **Shards**, the meta-progression trait tier is **Marks**, and the run's own resource is **XP** — there is no run currency to bank or spend.
- **Genre:** Pixel-art action roguelike (vertical descent)
- **Platform:** PC (Windows/Mac), built in Unity/C#
- **Target Session Length:** 30–60 minutes per run
- **Target Player Experience:** Tense, forward-momentum action with a satisfying power curve within a run, and a longer-term sense of permanent growth across many runs.

## Narrative Premise

**Protagonist:** A woman manipulated by **Zyno**, who has convinced her that her own village are monstrous enemies. She descends to hunt them down, unaware. Her real motive — surfaced only gradually — is that Zyno used her desire to protect two children (the last descendants of an unspecified elite bloodline) to turn her against her own people.

**Floor 16 is two fights, in this order.** She first faces **The Depth Warden — her father** — using the multi-phase, weapon-check boss design CONTENT_DESIGN §5 budgets for him, framed as an ordinary boss on a first playthrough, with no telegraphing that breaks the "he's a monster" illusion. Beating him does not end the floor: **Zyno is fought immediately after, as the true Final Boss.** Biome 1's Mini-Boss is **The Collapsed King** (CONTENT_DESIGN §5).

**Zyno** is present throughout the descent via the Whisper Layer (CORE_SYSTEMS §15), then fought in person on Floor 16 after the father. His MVP fight reuses an existing Mini-Boss's moveset and arena, palette-swapped with his own dialogue and identity; a bespoke moveset is SHOULD SHIP (CONTENT_DESIGN §5, 08-MVP.md).

Biome 1's art stays mine-themed — the narrative sits as a writing/data layer over existing art, not an art change. Biomes 2–3's narrative framing is not yet decided.

Full backstory, cast and motive live in `10-NARRATIVE.md`, which remains **owner-directed but not yet locked**. What the game is once this story resolves — whether later runs recontextualize the same enemies, whether Zyno survives Floor 16, whether escalating difficulty becomes the replay driver — is still a proposal, not an approved decision (see Open Decisions).

## Core Gameplay Loop

1. **Hub:** The player starts in a small, walkable surface camp at the mine's mouth. Spend accumulated Shards on permanent upgrades. Choose the starting **Weapon**.
2. **Descend:** Enter the shaft. Move through a sequence of hand-built rooms per floor.
3. **Fight:** Clear enemies in combat rooms using the chosen weapon's Basic Attack, Heavy Strike, Ultimate, and Dig-Dash. Enemies drop **XP** on death.
4. **Level Up:** When XP crosses the level threshold, the game pauses and presents an upgrade offer — 3 randomized upgrades drawn from the full shared + weapon pool in one weighted draw (not floor-gated, not tier-gated — Common/Rare/Epic can appear in the same offer), plus a visible 4th **Curse** option. Every 5th level, this is replaced by an **Evolution** offer instead (see Core Systems).
5. **Repeat:** Descend deeper. Floors pull **3–5 rooms** from a per-biome pool via a reshuffling bag (see Core Systems §8). Every 5th floor ends in a Mini-Boss room. Difficulty increases with depth.
6. **Final Boss:** At the bottom of the mine (Floor 16), face the Final Boss sequence.
7. **Win/Die:** Defeat the boss and escape (win), or reach 0 HP (die).
8. **Return to Hub:** Shards earned carry over — computed once at run end from Levels Gained and Depth Reached (BALANCE §14), not collected during the run. Spend on permanent upgrades. Start a new run.

## Player

- **Character:** A single, fixed protagonist — no body or gender selection. She is a woman, cloaked, in light armour, name TBD. There is no inventory or gear-swapping: what she wears never changes and carries no stats (see Weapon below). Full backstory, motivation and the Zyno/father relationship: `10-NARRATIVE.md`.
- **Movement:** 8-directional top-down. Movement ramps rather than snapping to full speed — a short interval to reach full speed, and a slightly longer one to coast to a stop, so stopping reads heavier than starting. Both intervals are tunable and can be set to zero to restore instant on/off velocity if ramping ever feels sluggish.
- **Health:** Starts at a base HP value (see BALANCE.md), increased by run upgrades and permanent upgrades.
- **Weapon:** Player chooses **1 of 3 weapons in the Hub before descending**. Locked for the full run. All 3 are unlocked from the start — no gating. The weapon is the single build-defining choice — it determines her Basic Attack, Heavy Strike, Ultimate, and (via the Dig-Dash) a weapon-flavored Dash Attack.
  - **Katana** — Fast light melee. Low damage per hit, quick windup/recovery, short range. **Signature trait: Combo Counter** — consecutive hits without missing or taking damage build a small stacking damage bonus, resets on miss or on taking a hit.
  - **Bow** — Ranged. Trades close-range safety margin for damage at distance. No ammo cost, just cooldown. **Signature trait: Charge Shot** — hold attack to charge bonus damage/pierce, or release early for a fast weak shot.
  - **Greatsword** — Heavy melee. High damage, wide arc, slow windup/recovery, big whiff punish window. **Signature trait: Hyper Armor** — can't be knocked back and takes reduced (not zero) damage during windup; distinct from Dig-Dash i-frames, which remain the only true invulnerability.
  - Each weapon has a clearly different function (fast/low-risk, ranged/positional, slow/high-commitment), giving weapon choice real weight both build-wise and moment-to-moment.
- **Controls:** `LClick` — Basic Attack. `RClick` — Heavy Strike (slower, stronger, weapon-specific variant; hold to charge — see below). `LShift` — Dig-Dash. `R` — Ultimate (weapon-specific, gauge-gated, see below). No separate ability-select keys — the weapon itself defines the whole kit, including the Dash Attack that comes out when Basic Attack is pressed during, or just after, a Dig-Dash (see Dodge/Mobility).
- **Attack:** Weapon-dependent swing/shot, short cooldown, hitbox shape and timing vary by weapon (see Combat section). **Basic Attack is a 2-hit chain that loops** (hit 1 → hit 2 → hit 1 again, reusing the same two animations, so it reads as a continuous flurry rather than a fixed 2-count) — each hit re-enters Windup→Active→Recovery, and the chain breaks if the player doesn't press again within a short window after Recovery. Free from the start on all 3 weapons, not upgrade-gated; both hits currently deal equal damage. Whether a later hit should scale as a finisher, and whether the chain should interact with the Katana's Combo Counter beyond each hit adding a stack, are open — see Open Decisions.
- **Heavy Strike (RClick):** A single stronger, slower hit per weapon at base. Holding the button charges it: releasing scales its damage, hitbox size, lunge, hitstop and camera shake, with a full charge taking under a second. Holding roots her in place — she keeps turning to aim and can still Dig-Dash out of the charge to cancel it, but she can't walk through it. The upgrade pool can modify this slot directly — extending it into a 2–3 hit chain, or replacing it outright with a repurposed effect (e.g., a Dynamite Throw or Grapple Pull variant) — making it the primary build-customization slot within a run. Whether charging should be a Katana-only trait, given it now overlaps the Bow's locked Charge Shot identity, is open — see Open Decisions.
- **Ultimate (R):** Weapon-specific, tied to that weapon's signature trait. Two shapes exist: an **Attack** (a burst of damage, e.g. Bow's full-charge piercing shot, Greatsword's ground-slam AoE) or a **Buff** (a temporary self-empowerment with no damage of its own). **Katana's Ultimate is a Buff**: a short cast raises an aura on her and the katana, and for its duration she deals more damage, attacks faster, and moves faster — every attack lands *through* the buff rather than the Ultimate being a hit in its own right. It doesn't touch the Combo Counter at all — the two systems run independently. No cooldown either way — gated by an **Ultimate Gauge** that fills per landed attack at a per-weapon rate (see BALANCE.md). Activating the Ultimate fully drains the gauge back to zero. A rare in-run upgrade per weapon (**Alt Ultimate**) can replace the default effect with a more mobile, skill-style alternative — Katana's stays Attack-shaped (real damage, full player-steered movement) even though its default Ultimate is a Buff, giving a genuine choice of shape. See CONTENT_DESIGN.md. The buff's numbers (duration, damage/speed bonus) remain unbalanced first-pass placeholders — see BALANCE.md.
- **Defense:** No blocking. Damage mitigation comes from avoidance (Dig-Dash i-frames), Hyper Armor (Greatsword only, partial), and defensive upgrades (Iron Skin, Second Skin, etc.).
- **Dodge/Mobility:** The Dig-Dash is a short dash along the held movement direction, falling back to her facing only when no direction is held. It grants brief invulnerability frames and can break through cracked walls (used for shortcuts/flanking). **Dash-Attack Cancel:** the recovery frames of any weapon action can be canceled early into a dash — no tutorial prompt, a piece of movement tech for players to discover and optimize around. **Dash Attack:** pressing Basic Attack during a Dig-Dash, or within a short window after it lands, replaces the ordinary Basic with a unique fourth weapon action — its own animation, a longer lunge, and a damage bump — so the dash is also an approach option, not only a defensive one. (Despite the name, this is a different move from the Dash-Attack Cancel above; the two happen to share a word but are unrelated.)
- **Resource Systems:**
  - **XP (run resource):** Dropped by enemies on death, collected during the run. Drives leveling, which is what triggers upgrade offers (see Core Gameplay Loop and CORE_SYSTEMS §12). Not a currency — there is no in-run shop and nothing to spend it on.
  - **Shards (meta currency):** Permanent, spent in the Hub. Awarded **once at run end**, computed from Levels Gained and Depth Reached (BALANCE §14) — there is no in-level pickup object.
- **Death:** Run ends immediately. Player returns to Hub with earned Shards. No mid-run checkpoints/respawns.

## Combat

- **Attack Behavior:** Weapon-dependent (Katana / Bow / Greatsword — see Player section). Each weapon implements a shared attack interface (Basic Attack, Heavy Strike, Ultimate) so the underlying system stays uniform even though feel differs.
- **Attack Timing:** Fixed windup → active hitbox/hit frame(s) → recovery, per weapon (Bow's windup is variable-length due to Charge Shot; a charged Heavy Strike's windup shortens as the charge fills). Values tuned in BALANCE.md.
- **Attack Movement:** Attacks are not rooted in place — each swing drives the player forward a short distance on an ease-out curve, with the direction locked at the start of the hit. She can't steer mid-swing, but she isn't stationary either, which is what gives attacks their sense of weight. Exact lunge distances per weapon and action are serialized placeholders that BALANCE.md does not yet carry a row for (design change brief §7m).
- **Hit Detection:** Katana and Greatsword use melee arc/box hitboxes in front of the player; Bow uses a projectile, reusing the hit-detection pattern already established for enemy ranged attacks.
- **Damage:** Flat damage per hit within a run — no crit system anywhere in the game, keeping in-run math simple and readable. Permanent power growth comes from the Hub Stat System's Core Stats and Marks (see Progression), not from a crit roll.
- **Enemy Damage:** Flat damage per enemy attack, telegraphed with a short wind-up animation/color flash.
- **Player Survivability:** Base HP + Dig-Dash i-frames + Hyper Armor (Greatsword) + optional defensive upgrades (see CONTENT_DESIGN.md).
- **Invulnerability Frames:** Granted only during Dig-Dash active frames, and briefly after taking damage (standard hit-stun immunity). Hyper Armor reduces damage but does not grant invulnerability — it stays mechanically distinct from Dig-Dash.
- **Feedback:** Screen shake on hit, hit-flash on enemies, damage numbers optional (nice-to-have), sound cue per hit type, per weapon where relevant.
- **No environmental hazard exists to knock enemies into.** Colossus (a Greatsword upgrade) is the one upgrade that still rewards knockback, via wall/enemy collision damage.

## Roguelike Structure

- **Run Start:** Player exits Hub into Floor 1 of Biome 1 (Upper Caves), with chosen Weapon.
- **Rooms/Encounters:** Each floor = 3–5 hand-built rooms pulled from a per-biome room pool via a reshuffling bag, connected linearly downward. No branching paths (keeps navigation simple and always-forward). Layouts repeat within a run — see CORE_SYSTEMS §8.
  - **Wave Rooms:** A subset of existing Combat Room layouts (capped at 1–2 per biome's pool) are flagged as Wave Rooms. Instead of spawning all enemies at once, enemies spawn in 2–3 triggered batches — the next wave begins when the current one drops to roughly one enemy remaining, not on a pure timer, so players can't stall it out. The room stays locked until the final wave clears, reusing the existing room-lock logic. No new enemy types are introduced for Wave Rooms — only existing per-biome enemies, resequenced.
- **Secret Floors:** A locked door, requiring a key dropped by a rare elite encountered earlier in that biome, leads to a small Secret Vault — one guarded fight, tougher than a standard Combat Room, that pays out a guaranteed Legendary-tier upgrade on clear. The key is consumed on opening the door, so a second vault in the same run costs a second elite kill. Whether the Secret Vault can sit on a floor's linear route at all, or stays reachable only as a detour, is undecided — see Open Decisions.
- **Trapped Souls:** Some floors contain a bound soul the player can free via a short interactable, granting a persistent in-run effect (CORE_SYSTEMS §14). What freeing one should cost is still undecided — see Open Decisions.
- **Rewards:** XP drops from enemies; upgrade choice on level-up; occasional relics (see Progression).
- **Randomization:** Room order within a biome's pool is drawn per run via a reshuffling bag (not live procgen). Upgrade offers are randomly drawn (weighted) from the upgrade pool on each level-up.
- **Upgrades:** On each level-up, the player picks 1 of 3 offered upgrades, drawn in a single weighted draw across the shared pool (HP, XP, Speed, Dash) *plus* the weapon-specific sub-pool matching the equipped weapon — including Heavy Strike modifiers (extra hits in the chain, full replacement effects), Ultimate effect modifiers, and Ultimate Gauge modifiers (faster gain per hit, gain on taking damage, etc.) (see CONTENT_DESIGN.md for full tables). Every 5th level offers an **Evolution** instead (CORE_SYSTEMS §13).
- **Curses:** Alongside the normal 3 upgrade offers, a 4th slot always presents a visible **Curse** — a high-risk, high-reward modifier (e.g., "+40% damage, but take double damage"). Optional every time; gives runs a distinct identity for players who want them. One Curse, Greed's Toll, currently has no working downside — see Open Decisions.
- **Difficulty Progression:** Enemy HP and damage scale up per biome tier.
- **Boss:** Mini-boss every 5th floor (end of each biome); Final Boss sequence at Floor 16. Bosses are designed to include at least one phase or mechanic that meaningfully rewards or punishes specific weapon types (e.g., a breakable shield that Greatsword drops in one hit but Katana needs several, or slow projectiles Bow players can snipe for bonus damage but melee players must dodge). Not a hard gate — just a moment where weapon choice matters at peak tension.
- **Death:** Immediate run end, return to Hub.
- **Restart:** New run begins from Hub with fresh run-state but persistent meta-progression.

## Biome Identity

Each biome's identity is its own standalone environmental mechanic that creates positioning pressure inside a fight — there is no pursuing hazard front and no per-biome timer chasing the player.

- **Upper Caves (Biome 1):** Certain floor tiles show warning cracks and collapse after a few seconds of standing on them — live positioning pressure during fights.
- **Flooded Tunnels (Biome 2):** Water tiles slow movement and swings; currents can push the player or enemies toward ledges. Bow travels unaffected through water, giving it a situational edge; melee is punished for wading in.
- **Molten Depths (Biome 3):** Periodic lava geysers erupt from marked tiles, forcing mid-fight repositioning. Greatsword's Hyper Armor is especially valuable here — tanking a geyser tick and continuing to swing.

Whether this gives Biomes 2 and 3 a mechanical identity as distinct as Biome 1's, and whether descent needs a clock of any kind now that the pursuing hazard is gone, are both open — see Open Decisions.

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

Shards, the persistent Hub currency, are awarded once at run end, computed from Depth Reached and Levels Gained — there is no in-level currency pickup object. See BALANCE §14 for the formula.

**Permanent/meta progression (persists across runs):**
- Shards (currency)
- **Hub Stat System:** rank-based Core Stats (Max HP, Base Damage, Move Speed, Ultimate Gauge Gain, XP Gain, Dash Cooldown) plus **Marks** — unique named effects in the style of Hades' Mirror of Night (e.g., Death Defiance, Boiling Blood, Warm-Up) — plus two flat non-stat unlocks (extra Curse slot, Relic Cache). Full table in CONTENT_DESIGN.md.
- **Weapon Mastery:** a small permanent track (3–5 nodes) per weapon, unlocked by *using* that weapon across runs rather than by spending Shards alone — encourages mastering all three instead of settling on one.
- **Relics:** the Legendary-tier upgrade rarity, one per weapon, only offered when that weapon is equipped — resolves "relic" as a concrete term rather than undefined flavor text. Once found at least once, a relic becomes purchasable from the **Relic Vault** in the Hub at high Shard cost, guaranteeing it as an offer once per future run.

This remains a **small, bounded** meta-progression tree — not a skill tree, a short flat list plus the two additions above (Weapon Mastery, Relic Vault) — see CONTENT_DESIGN.md.

## Game Flow

1. Launch game → Main Menu → Hub.
2. Hub: view/spend Shards, select Weapon, start run.
3. Run: descend through Biome 1 (floors 1–5) → Mini-Boss 1 → Biome 2 (floors 6–10) → Mini-Boss 2 → Biome 3 (floors 11–15) → Mini-Boss 3 → Floor 16: the Final Boss sequence.
4. Win: defeat the Final Boss sequence → Victory screen → return to Hub with all earned Shards.
5. Die: death screen showing depth reached and Shards earned → return to Hub.
6. Repeat.

## UI

- **HUD (in-run):** HP bar, equipped weapon icon, Ultimate Gauge (fill meter, not a cooldown), a Dig-Dash cooldown pip, current floor/depth indicator, XP bar + current level, a Wave indicator (e.g. "Wave 2/3") shown only inside Wave Rooms, a faint strip listing the upgrades taken this run (tier-colored, for reference), and the Whisper Layer line area (CORE_SYSTEMS §15). There is no Heavy Strike cooldown icon — Heavy Strike has no cooldown; it is gated only by its own Windup/Recovery (and, while charging, by the hold itself).
- **Upgrade Screen:** 3 offer cards (tier-colored border, a unique icon, an effect-category glyph, and a compressed effect line) plus a 4th, visually distinct Curse card. Opens on level-up (game paused), not at floor end. Every 5th level it is replaced by the Evolution offer — 2–3 mutually exclusive cards (CORE_SYSTEMS §13).
- **Hub Screen:** Shard total, list of permanent upgrades (purchased/available/locked), Weapon selector, Weapon Mastery progress per weapon, Relic Vault, "Descend" button.
- **Death/Victory Screen:** One screen for both outcomes, distinguished by title and accent color. Shows depth reached, Shards earned, run time, weapon used, and a "Return to Hub" button.

## Audio

- **Music:** One looping track per biome (3 total) + 1 boss track (reused for all mini-bosses) + 1 final boss track + 1 hub track. 5 tracks total.
- **SFX (must-have):** Per-weapon Basic Attack, Heavy Strike, and Ultimate sounds (Katana, Bow, Greatsword), Ultimate Gauge full cue, player hit-taken, player death, enemy hit, enemy death, Dig-Dash, Dash Attack Cancel, upgrade pick, curse pick, XP pickup, level-up, floor transition, wave trigger, boss intro roar, victory jingle.

## Victory

The player wins by defeating the Final Boss sequence on Floor 16 and surviving the escape sequence that follows — a short, timed dash back to the surface on a fixed countdown (BALANCE §7). This is the only timer left in the game since the Rising Hazard was cut; whether it should stay is an open call rather than something it inherits automatically — see Open Decisions.

## Open Decisions

Collected here rather than scattered through the sections above. Each names the question, what it blocks, and where it's recorded.

1. **Is there a clock at all?** The pitch describes forward momentum, but nothing currently pushes the player downward — the Rising Hazard that used to be the game's clock is cut, with nothing decided in its place. Blocks: whether Secret Floors, Trapped Souls, Greed's Toll and per-biome escalation ever get a second axis of tension besides raw enemy stats. *Design change brief §12 (holes 1, 3, 5).*
2. **Can the Secret Vault sit on a floor's route?** CORE_SYSTEMS §8 calls it a detour; LEVEL_DESIGN §1 requires strictly linear floors with no branching. As built, the vault is a one-door dead end and is excluded from the floor pool entirely. Options on the table: give it a second door and put it on-route (dropping "detour"), make it a floor's terminal room, or leave it reachable only outside a normal run. *Design change brief §22.2.*
3. **What does freeing a Trapped Soul cost?** Its price used to be "time against the hazard"; with that gone, the interactable currently costs nothing. (The Secret Vault's equivalent question is answered — a fight plus a spent key — but Trapped Souls were not part of that decision.) *CORE_SYSTEMS §14; design change brief §12 (hole 2).*
4. **Are Biomes 2 and 3 mechanically distinct enough?** Flooded Tunnels and Molten Depths each kept one environmental mechanic when the Rising Hazard was cut, and DESIGN_RULES Rule 4/5 asks for a real functional hook, not just different flavor on the same fight. As currently built, both biomes also reuse Biome 1's room layouts and enemy roster outright, differing only in palette. Blocks: whether Biomes 2–3 need new rooms/enemies, a stronger environmental hook, or both. *Design change brief §12 (hole 5), §25.5.*
5. **Does the Floor 16 escape sequence survive?** It is the last timer left in the game and no longer inherits its justification from the Rising Hazard. Keep it as a standalone system, or cut it along with the rest of the clock. *BALANCE §7; design change brief §12.*
6. **"Descend" currently plays as "walk east."** Floors are a straight corridor with no vertical-descent presentation and no floor-transition beat. Needs a design answer — a stairwell room, a fade, a descent beat, or literal north/south doors — rather than a quiet engineering default. *Design change brief §22.4.*
7. **No boss has a phase or a weapon-check yet.** All five bosses (three Mini-Bosses, the Depth Warden, Zyno) currently ship as a single scaled, tinted enemy with HP only — the weapon-check moment this document and CONTENT_DESIGN §5 both describe as the point of a boss fight does not exist in any of them. Not a design disagreement, but worth the owner knowing the description above is still the intended target, not the current build. *Design change brief §25.4.*
8. **Greed's Toll has no downside.** Its cost was a faster Rising Hazard; with that cut, the Curse is pure upside as written. Needs a new downside, or removal from the pool. *CONTENT_DESIGN §3; BALANCE §11; design change brief §12 (hole 3).*
9. **What is "Deeper" once the story resolves?** `10-NARRATIVE.md` §4 proposes that later runs recontextualize the same enemies (new identities/dialogue on the same rooms and fights) and that difficulty escalation is reframed as Zyno tightening his grip after surviving Floor 16 in some form — none of this is approved. Blocks: whether the game needs new content at all past run 1, or whether the existing pipeline (palette-swap recontextualization) is sufficient. *`10-NARRATIVE.md` §4, §7; design change brief §3, §5, §6.*
10. **Does charging belong on every weapon's Heavy Strike?** It now overlaps the Bow's locked signature trait (Charge Shot). Three options stand: keep it on all three weapons since it lives on a different button/action than the Bow's charge; recast the Bow's signature trait as something else; or make charging Katana-only. *Design change brief §17c, §20.*
11. **Does the Basic Attack chain need a finisher, and does it interact with the Combo Counter?** It is built as a free, 2-hit looping chain with both hits dealing equal damage — "equal" is the current behaviour, not a ruling. Open: whether a later hit should scale up as a finisher instead, and whether landing a chain hit should interact with the Katana's Combo Counter beyond each hit adding one stack (as every other landed hit already does). *Design change brief §7b.*
