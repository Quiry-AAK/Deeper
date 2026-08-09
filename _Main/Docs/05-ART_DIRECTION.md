# ART DIRECTION — "Deeper"

Visual style, animation budget, and asset scope for every system defined in the prior docs. Numeric budgets here are what the 07-IMPLEMENTATION_PLAN.md schedule is built against — treat frame counts as a hard ceiling, not a suggestion, since 3 weapons already expanded the animation scope once (see amendment history in GDD).

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

Hazard accent colors are reserved exclusively for hazard telegraphs and the Hazard Front itself — never reused for decorative purposes, so players always read "that color = danger" instantly regardless of biome.

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
| Elite variant | Palette-swap + 1 additional "aura" VFX layer only — no new frames |

Mini-Bosses and the Final Boss get an expanded budget (their own animation set, phase-specific poses) — exact count deferred to when their movesets are storyboarded, not needed for MVP planning.

---

## 5. UI Style

- **HUD:** minimal, corner-anchored — HP bar top-left, Ultimate Gauge and weapon icon bottom-center (mirrors the "resource, not cooldown" framing — the gauge should visually read as filling, not counting down), Ore counter top-right, hazard proximity meter as a subtle screen-edge vignette rather than a numeric readout (keeps tension environmental, not spreadsheet-y).
- **Upgrade Screen:** 3 cards in shared/weapon-pool color coding (Common = white/gray border, Rare = blue, Epic = purple, Legendary/Relic = gold), 4th Curse card visually distinct with a red/black treatment so it's never confused with a normal offer.
- **Hub Screen:** Core Stats and Miner's Traits visually separated into two distinct panels (per CONTENT_DESIGN §7) rather than one long list, so the "these are different kinds of upgrades" distinction is legible at a glance.

---

## 6. VFX Priorities (must-have for MVP)

- Weapon hit-flash (per weapon, can share a base flash effect tinted per weapon color)
- Ultimate Gauge full pulse (on the HUD element itself)
- Hazard Front edge (biome-specific: rockfall dust / water shimmer / lava glow)
- Dig-Dash trail
- Curse-pick screen flash (red) vs. normal upgrade-pick flash (white/gold)

---

## Open Items for LEVEL_DESIGN.md / IMPLEMENTATION_PLAN.md

- Tileset count per biome (depends on room count from LEVEL_DESIGN.md)
- Mini-Boss/Final Boss unique animation budgets (needs moveset design first)
- Whether Wave Room enemy density needs a "screen clarity" pass (more enemies on screen at once than base Combat Rooms)
