# ART DIRECTION — "Deeper"

Visual style, animation budget, and asset scope for every system defined in the prior docs. Numeric budgets here are what the 07-IMPLEMENTATION_PLAN.md schedule is built against — treat frame counts as a hard ceiling, not a suggestion, since 3 weapons already expanded the animation scope once (see amendment history in GDD).

---

## 0. Protagonist

A single, fixed character — no body/gender selection. A woman, cloaked, in light armour. Name TBD (`10-NARRATIVE.md` §5). Cosmetic armor/helmets are planned post-launch (no stats, no slots) and need an art budget line whenever they're scheduled.

**New, unbudgeted scope from the narrative system (§15 in CORE_SYSTEMS.md):** dialogue UI, the Whisper Layer's on-screen line treatment, the Memory Fragment vignette and Codex screen, and likely character portraits for her, Zyno, and the father. Nothing below this line covers narrative presentation — §5 (UI Style) below covers HUD, Upgrade, and Hub screens only.

---

## 1. Visual Style

- **Genre:** 2D pixel art, top-down.
- **Resolution target:** 32×32 base tile size, player/enemy sprites 32×48 (allows a taller silhouette without breaking tile alignment).
- **Palette approach:** Each biome has a distinct 6–8 color core palette, sharing a common neutral (stone-gray/black) base so UI and player sprite read consistently across all three.
- **Silhouette-first readability:** Enemies and hazards must be identifiable by silhouette alone at combat speed — no relying on fine detail to distinguish threat types, since fights move fast and screen clutter increases with Wave Rooms.

---

## 2. Biome Palettes

| Biome | Core Palette Direction | Accent (hazard/danger color) |
|---|---|---|
| Upper Caves | Cool grays, muted browns, sparse warm torchlight | Orange-red (crack glow / rockfall dust) |
| Flooded Tunnels | Deep blues, teal, slate | Pale cyan-white (current highlights, rising water edge) |
| Molten Depths | Charcoal black, dark red, ember orange | Bright yellow-orange (geyser telegraph, lava front) |

Hazard accent colors are reserved exclusively for danger telegraphs — cracked-tile warnings, geyser wind-ups, enemy attack tells — never reused for decorative purposes, so players always read "that color = danger" instantly regardless of biome. (The Hazard Front they were also reserved for is cut — CORE_SYSTEMS §7 — but the reservation stands for everything else.)

⚠️ **Style conflict, shipped and unresolved:** the Katana Ultimate's cyan-white slash arcs are baked directly into the character frames — cyan-white is reserved above as the Flooded Tunnels hazard accent, so a player-power effect is currently reading in hazard colors. This also means the arcs can't be pulled out as separate VFX without regenerating the frames. Note also that a prior, deliberate fix was reopened: the arc used to be a separate `SlashVFX` layer, pulled out of the character frames because a measurement found the baked arc had 197 bright pixels facing sideways against only 14 facing up (she turns away from camera and a dark arc vanishes against her pale cloak) — that layer was since deleted (owner-agreed) because the attack frames now draw their own arc and two arcs were appearing per swing. **Re-measure the shipped sheets before treating the color conflict as settled**, and if the readability problem still holds, fix it in the art rather than reintroducing a second arc layer.

---

## 3. Player Animation Budget

Per weapon, the following states are required. Frame counts are a ceiling — hitting fewer frames is fine, exceeding it needs a scope conversation per DESIGN_RULES.md.

| State | Frames (max) | Shared across weapons? |
|---|---|---|
| Idle | 4 | Yes — same base rig |
| Move (8-directional, can mirror for 4 base directions) | 6 per direction | Yes |
| Basic Attack | 5 | No — per weapon |
| Heavy Strike | 6 | No — per weapon |
| Ultimate | 8 | No — per weapon |
| Alt Ultimate (if unlocked, replaces Ultimate state) | 8 | No — per weapon, reuses the Ultimate animation slot rather than adding a new one |
| Dig-Dash | 4 | Yes — same across all weapons |
| Hit-taken / stagger | 3 | Yes |
| Death | 5 | Yes |

**Per-weapon unique animation cost:** Basic (5) + Heavy Strike (6) + Ultimate (8) = **19 frames per weapon**, × 3 weapons = 57 frames of weapon-unique animation, on top of ~34 frames of shared rig work. This is the actual cost of the 3-weapon decision in art terms — confirms why the timeline extension was necessary.

**Heavy Strike chain extensions** (Twin Cut, Heavy Follow-Through, etc.) reuse the base Heavy Strike animation played twice/three times rather than requiring unique frames per chain hit — keeps the upgrade system from silently exploding the animation budget.

---

## 4. Enemy Animation Budget

| State | Frames (max) |
|---|---|
| Idle/Move | 4 |
| Telegraph (wind-up before attack) | 3 |
| Attack | 3 |
| Death | 3 |
| Elite variant | Palette-swap only for MVP — no new frames, **no aura VFX layer** (see decision below) |

**Enemy sprite-sheet contract (built, previously undocumented — this section gave a frame budget but no row order or direction count, and the enemy content pass had to invent one):**

- **Enemies author 3 directions (Down, Up, Side), not the Player's 5** (§3 above). Basics are 4-directional, mirrored to cover all 8 facings; the full 5-row Player set is reserved for bosses. Diagonal rows earn the least per cell, and 2-directional art can't show whether an enemy is *facing you* — the information every telegraphed fight is built on.
- **Sheet layout: 128×576px, 4 columns × 12 rows of 32×48.** Rows 0–2 Idle/Move, 3–5 Telegraph, 6–8 Attack, 9–11 Death.

**DECIDED: Elite aura VFX cut for MVP, deferred to post-MVP polish.** The Deep Warden ships as a palette swap only (Brute recolored violet) — that's now the whole MVP spec, not a gap. The aura layer wasn't a simple add anyway: `AuraVisuals` currently resolves `UltimateBuff` and `AttackStateMachine`, coupling it to the player, and needs rework before it can target an enemy. Revisit for Biome 2/3's Tideheart and Cinder Warden as part of the general post-MVP polish pass.

Mini-Bosses and the Final Boss get an expanded budget (their own animation set, phase-specific poses) — exact count deferred to when their movesets are storyboarded, not needed for MVP planning.

---

## 5. UI Style

- **HUD:** minimal, corner-anchored — HP bar top-left, Ultimate Gauge and weapon icon bottom-center (mirrors the "resource, not cooldown" framing — the gauge should visually read as filling, not counting down), XP bar + level readout top-right. (The hazard proximity vignette is cut with the Rising Hazard, CORE_SYSTEMS §7 — the HUD has no ambient-tension element now.)
- **Upgrade Screen:** 3 cards in shared/weapon-pool color coding (Common = white/gray border, Rare = blue, Epic = purple, Legendary/Relic = gold), 4th Curse card visually distinct with a red/black treatment so it's never confused with a normal offer.
- **Hub Screen:** Core Stats and Marks visually separated into two distinct panels (per CONTENT_DESIGN §7) rather than one long list, so the "these are different kinds of upgrades" distinction is legible at a glance.

---

## 6. VFX Priorities (must-have for MVP)

- Weapon hit-flash (per weapon, can share a base flash effect tinted per weapon color)
- Ultimate Gauge full pulse (on the HUD element itself)
- ~~Hazard Front edge~~ — cut with the Rising Hazard (CORE_SYSTEMS §7). Per-biome environmental telegraphs (cracked-tile warning, geyser wind-up, current direction) still need VFX and are the cheaper remainder of this line
- Dig-Dash trail
- Curse-pick screen flash (red) vs. normal upgrade-pick flash (white/gold)

---

## Open Items for LEVEL_DESIGN.md / IMPLEMENTATION_PLAN.md

- **Flicker Recognition** (narrative mechanic, see CORE_SYSTEMS §15 Post-MVP list) will require an alternate "true form" visual per affected enemy if greenlit — this competes directly with the enemy animation budget already flagged as expensive due to the 3-weapon cost (§4). Do not scope this until a dedicated review happens; it is explicitly Post-MVP.
- Biome 1 art direction is confirmed to stay mine-themed for MVP regardless of the narrative reframe — no new art requirement from the story change itself.
- Tileset count per biome (depends on room count from LEVEL_DESIGN.md)
- Mini-Boss/Final Boss unique animation budgets (needs moveset design first)
- Whether Wave Room enemy density needs a "screen clarity" pass (more enemies on screen at once than base Combat Rooms)
