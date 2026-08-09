# IMPLEMENTATION PLAN — "Deeper"

Day-by-day build plan. Original scope was a 30-day MVP; the plan below reflects the **extended timeline** (project owner decision, see GDD history) needed to accommodate the 3-weapon system and Wave Rooms. Target: **45 working days** to MVP, organized in weekly phases. This is a planning sequence, not a rigid contract — days will slip; the phase order is the part that matters (each phase's exit criteria gate the next).

---

## Phase 1 — Foundation & Single-Weapon Combat Loop (Days 1–10)

**Goal:** One weapon (Katana, since it's closest to a "default" melee baseline) fully functional in a single test room, proving the Attack State Machine and Ultimate Gauge before multiplying by 3.

- Days 1–2: Project setup, player movement (8-directional), base Attack State Machine (Windup→Active→Recovery), Dig-Dash + i-frames
- Days 3–4: Katana Basic Attack + Heavy Strike, hitbox/damage pipeline, `OnDamageDealt` event
- Days 5–6: Ultimate Gauge (fill on hit, drain on use), Katana Combo Counter, Katana Ultimate (Combo Finisher)
- Days 7–8: Dash-Attack Cancel, one test enemy (Cave Crawler) with basic AI, damage taken/HP loop
- Days 9–10: Playtest pass on Katana-only loop — **exit criteria: Katana's full kit (Basic/Heavy/Ultimate/Dash) feels good against a single enemy type before touching Bow or Greatsword**

## Phase 2 — Bow & Greatsword, Weapon Interface Generalization (Days 11–18)

**Goal:** Generalize the Katana-specific code into the shared `IWeapon` interface, then implement the other two weapons against it.

- Days 11–12: Refactor Katana implementation behind `IWeapon` interface (per CORE_SYSTEMS §1) — confirms the abstraction actually holds before building on top of it
- Days 13–14: Bow — projectile hit-detection, Charge Shot variable windup, Bow Ultimate (Piercing Shot)
- Days 15–16: Greatsword — wide-arc hitbox, Hyper Armor state, Greatsword Ultimate (Ground Slam)
- Days 17–18: Weapon select screen (Hub stub), playtest all 3 weapons back-to-back — **exit criteria: all 3 weapons feel distinctly different in the same test room**

## Phase 3 — Biome 1 Content: Rooms, Enemies, Hazard (Days 19–26)

**Goal:** First full playable biome, start to finish.

- Days 19–20: Room system (Combat/Reward room loading, room-lock logic), Hazard Front system (timer-driven, Upper Caves variant: rockfall + cracked tiles)
- Days 21–22: Upper Caves enemy roster (Cave Crawler, Rock Slinger, Tunnel Brute) + Elite (Deep Warden)
- Days 23–24: Upper Caves 6 Combat Room layouts (incl. 1–2 Wave Room flags) + 2 Reward Rooms
- Days 25–26: Mini-Boss (The Collapsed King) with weapon-check mechanic, Secret Vault room + key-drop logic — **exit criteria: full Biome 1 clear is playable start to finish with all 3 weapons**

## Phase 4 — Upgrade & Curse System (Days 27–32)

**Goal:** The run-to-run build variety layer goes in.

- Days 27–28: Weighted-draw upgrade system, shared upgrade pool (24 entries) wired to their effects
- Days 29–30: Weapon-specific sub-pools (all 3 weapons, 15 entries each) — Heavy Strike mods, Gauge mods, Ultimate mods, Alt Ultimates
- Days 31–32: Curse pool (8 entries) + always-visible 4th slot, upgrade screen UI — **exit criteria: a full Biome 1 run generates genuinely different builds run-to-run**

## Phase 5 — Biomes 2 & 3 (Days 33–40)

**Goal:** Content-scale the Biome 1 pattern across the remaining two biomes. This phase should be faster per-biome than Phase 3 since the systems already exist — pure content authoring.

- Days 33–35: Flooded Tunnels — enemies, rooms (incl. low/high water tile data), Mini-Boss (Drowned Custodian), hazard variant (rising water + room geometry change)
- Days 36–38: Molten Depths — enemies, rooms (geyser tiles, scorched ground), Mini-Boss (Molten Sentinel), hazard variant (lava flow)
- Days 39–40: Cross-biome playtest — **exit criteria: a full 3-biome run (floors 1–15) is completable**

## Phase 6 — Final Boss, Hub, Meta-Progression (Days 41–45)

**Goal:** Close the loop — death/victory return the player to a Hub that actually matters.

- Day 41: Final Boss (The Depth Warden) — multi-phase, reuses all 3 hazard themes
- Day 42: Escape sequence (post-boss countdown, reuses Dig-Dash/Hazard systems)
- Day 43: Hub Stat System (Core Stats + Miner's Traits), Ore→Ore Shard conversion, Death/Victory screens
- Day 44: Relic Vault, Weapon Mastery stub (tracking only — full node effects are a post-MVP item per CONTENT_DESIGN open items)
- Day 45: Full end-to-end playtest, Hub→Run→Death/Victory→Hub loop — **exit criteria: see 08-MVP.md MUST SHIP list, all items checked**

---

## Sequencing Notes

- **Weapon generalization (Phase 2) is deliberately not Day 1.** Building one weapon fully first, then abstracting, avoids designing an interface for 3 unknowns simultaneously — a common source of over-engineered abstractions that don't fit any of the 3 concrete cases well.
- **Biome 1 (Phase 3) is the expensive one.** Every system built here (room loading, hazard, wave rooms, mini-boss weapon-check pattern) gets reused, not rebuilt, for Biomes 2–3 — which is why Phase 5 covers two biomes in roughly the same time Phase 3 spent on one.
- **Content authoring (room layouts specifically, per LEVEL_DESIGN.md Open Items) is the single biggest risk to this schedule.** 18 Combat Room layouts alone is a lot of hand-authored content — if Phase 3 or Phase 5 slip, this is almost certainly why, and it's the first place to look for cuts per 08-MVP.md.

---

## Open Items for MVP.md

- Confirm which of Phase 4's weapon sub-pool entries (45 total) are MUST SHIP vs. CUT IF NECESSARY — shipping a smaller sub-pool per weapon is a safe scope valve if Phase 4 runs long
- Confirm Weapon Mastery's post-MVP status (already flagged as stub-only in Phase 6)
