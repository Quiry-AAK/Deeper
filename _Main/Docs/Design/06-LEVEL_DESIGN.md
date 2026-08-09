# LEVEL DESIGN — "Deeper"

Room layout philosophy and per-biome pool detail. Builds on the room counts defined in CONTENT_DESIGN.md §6 and the pacing targets in BALANCE.md §8.

---

## 1. Design Philosophy

- **Linear, no branching.** Every floor is a straight sequence of 1–3 rooms. This is a deliberate scope-control decision (per DESIGN_RULES.md) — branching paths would require pathing logic, minimap work, and exponentially more layout variations for the same dev budget.
- **Hand-built, not procedural.** Rooms are authored layouts pulled from a per-biome pool via deterministic shuffle (same seed = same room order for a given run), not live-generated. This keeps every room hand-tuned for the Rising Hazard pacing and avoids the QA burden of procgen edge cases.
- **Every room must justify its combat or reward role.** No purely transitional/empty rooms — each room pull is either a Combat Room, Reward Room, Secret Vault, or Mini-Boss Room (per CONTENT_DESIGN §6).

---

## 2. Room Pool Per Biome (recap + layout notes)

| Room Type | Count | Layout Notes |
|---|---|---|
| Combat Room | 6 | 1–2 flagged `IsWaveRoom` (CORE_SYSTEMS §8). Each needs at least 2 viable player positioning zones so Greatsword's whiff-punish window and Bow's kiting space both have room to exist — a room that's just one open box favors Katana by default, which isn't the goal. |
| Reward Room | 2 | No combat. Contains Ore and/or an upgrade-adjacent prop. Small footprint — these are pacing breathers between Combat Rooms, not exploration challenges. |
| Secret Vault Room | 1 (reused across biomes) | Locked door, key-gated (CORE_SYSTEMS §8). Same base layout reused with biome-specific tile dressing to save art budget — the room's function (large Ore payout or guaranteed Legendary offer) matters more than layout novelty here. |
| Mini-Boss Room | 1 (unique per biome) | Large open arena, must accommodate the boss's full attack radius plus room for the player to kite. Includes the biome's hazard-behavior layer active during the fight (e.g., Molten Sentinel's geysers erupt mid-fight in its own room). |

---

## 3. Per-Biome Layout Requirements

### Upper Caves
- Combat Rooms need 2–4 tiles flagged as "cracked" (collapse after a few seconds of standing weight, per CORE_SYSTEMS §7) placed to create active positioning risk — never blocking the only path, always a risk/reward shortcut or a "don't stand here" zone during a fight.
- At least 1 Combat Room per biome should route through a breakable wall (Dig-Dash shortcut) as a secondary path option within the room itself.

### Flooded Tunnels
- Every room must define explicit "low" vs "high" tile zones (per CORE_SYSTEMS §7) — the rising water hazard needs this data to know what floods first.
- At least 2 Combat Rooms include a water-tile patch covering 20–30% of the room floor, enough to matter tactically (melee slowed, Bow unaffected) without trivializing the room for ranged players.

### Molten Depths
- Each Combat Room needs 1–2 marked geyser tiles with enough warning telegraph space around them (per BALANCE §6, 8s eruption cadence) that a player who's paying attention can always reposition in time — punishing inattention, not punishing the player for existing.
- Scorched ground left behind by the passing Hazard Front should be visually distinct enough that players don't confuse "safe now" floor with "still ticking damage" floor.

---

## 4. Wave Room Design Notes

- Wave Rooms (flagged Combat Rooms, CORE_SYSTEMS §8) need larger open space than standard Combat Rooms — enemies spawn in 2–3 batches, and a cramped room makes batch 2+ spawn-camping trivial rather than tactical.
- Spawn points should be placed at room edges, out of the player's immediate melee range at trigger time — gives a beat to react before the next wave is already swinging.
- Cap confirmed at 1–2 flagged rooms per biome's 6-room Combat pool (BALANCE §8 pacing target: 60–100s clear time vs. 30–60s for standard Combat Rooms).

---

## 5. Pacing Targets (recap from BALANCE.md §8)

| Metric | Target |
|---|---|
| Combat Room clear time | 30–60s |
| Wave Room clear time | 60–100s |
| Rooms per floor | 1–3 |
| Floor time vs. Hazard Timer | Comfortable margin at baseline play; Wave Rooms + Secret Floor detours are the actual source of time pressure, not base room count |

---

## 6. Mini-Boss & Final Boss Arenas

- Mini-Boss arenas are single large rooms, no secondary combat rooms feeding into them — the floor structure is Combat/Reward rooms → Mini-Boss room, full stop.
- The Final Boss arena (Floor 16) is the only room in the game that changes its own geometry mid-fight — per GDD, it "incorporates all 3 biome hazard types in sequence as the arena degrades." This needs its own dedicated layout pass, not reused geometry, given it's a one-time unique space.

---

## Open Items for IMPLEMENTATION_PLAN.md

- Exact tile-by-tile layouts for all 18 Combat Rooms (6 × 3 biomes), 6 Reward Rooms, 3 Mini-Boss Rooms, 1 Secret Vault, and the Final Boss Arena — this is the single largest content-authoring line item in the whole project and should be sequenced early given how much downstream testing depends on it
- Whether Secret Vault dressing swaps are a simple tileset palette-swap or need biome-unique prop additions
