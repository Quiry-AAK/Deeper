# MVP — "Deeper"

Defines what must be playable at the end of the 45-day extended timeline (see 07-IMPLEMENTATION_PLAN.md). Priority tiers per PROJECT rules: **MUST SHIP → SHOULD SHIP → CUT IF NECESSARY**. The core loop — **Movement → Combat → Dig-Dash → Room Clear → Upgrade Pick → Descend** — is protected above everything below it.

---

✅ **RESOLVED: narrative now has tiers.** The session changelog gives the narrative layer an explicit MVP subset — minimal Whisper Layer lines (Biome 1 + the father fight), a Memory Fragment pickup with a Codex UI stub, and the Refusal State on the father fight only — all MUST SHIP below. Everything beyond that subset is in Explicitly Post-MVP. The earlier "no tier assigned" flag is closed.

**DECIDED: Zyno is MUST SHIP, fought after the father on Floor 16.** The session changelog briefly moved the father to Biome 1 and kept Zyno out of the MVP; the owner reversed that on 2026-08-15 — **the father is the Final Boss, fought before Zyno**, as `03-CONTENT_DESIGN.md` §5 describes. This keeps Floor 16's placeholder-stub safety valve removed: the MVP needs a real Zyno encounter, not a stub. Because Zyno's fight is genuinely new content (no moveset, stats, arena, or art exist yet) landing on a timeline already extended once for the 3-weapon system, the MUST SHIP entry below takes the cheapest version that still counts as real — reuse an existing Mini-Boss's moveset/arena, palette-swapped with new dialogue (the recontextualization technique already budgeted for Elites, ART_DIRECTION §4 / NARRATIVE §4a) — rather than a bespoke moveset. A fully custom Zyno fight is a genuine additional-timeline conversation, not something the 45-day plan absorbs for free.

## MUST SHIP

Non-negotiable. The MVP is not the MVP without these.

- [ ] All 3 weapons (Katana, Bow, Greatsword) fully functional: Basic Attack, Heavy Strike, Ultimate, all with distinct feel
- [ ] Ultimate Gauge system (fill-on-hit, drain-on-use, no cooldown)
- [ ] Attack State Machine (Windup/Active/Recovery) + Dash-Attack Cancel
- [ ] Dig-Dash with i-frames
- [ ] Biome 1 (Upper Caves) fully playable: all 6 Combat Rooms (incl. 1 Wave Room minimum), Mini-Boss (The Collapsed King) with weapon-check
- [ ] Upper Caves cracked tiles (collapse under standing weight) — the biome's environmental mechanic. ~~Rising Hazard system~~ **cut entirely (owner, 2026-08-15, CORE_SYSTEMS §7)**; the cracked tiles were bundled into that line and survive on their own
- [ ] Weighted-draw Upgrade system with the shared upgrade pool (can ship with a reduced subset — see CUT IF NECESSARY)
- [ ] Curse system (4th slot, at least 4–5 of the 8 curses)
- [ ] XP/Leveling system: enemies drop XP, level-up pauses game and opens a mixed-tier upgrade offer (single weighted draw across the full pool, not floor-gated)
- [ ] Room reshuffling-bag logic: 3–5 rooms/floor drawn from the per-biome pool without immediate repeats
- [ ] Shard run-end calculation (Levels Gained + Depth Reached) — no in-level currency pickups
- [ ] 1 Evolution Tier per weapon (3 total), mutually-exclusive kit-replacement choices at a level-5 milestone
- [ ] 1 Trapped Soul type (The Warden's Soul) + 1 Trapped Soul room in Biome 1
- [ ] Narrative identity pass on existing Biome 1 content: village framing on the existing enemy roster, minimal Whisper Layer lines, Refusal State on the father fight (Floor 16) only
- [ ] Memory Fragment pickup type + Hub Codex UI stub (small Biome-1 fragment set)
- [ ] Full Hub loop: weapon select, Descend, Death/Victory screen, run-end Shard award, return to Hub
- [ ] At least a minimal Hub Stat System (2–3 Core Stats purchasable) proving the meta-progression loop works end to end
- [ ] **Floor 16 Final Boss Sequence — no placeholder stub.** The Depth Warden/father fight can ship as a stub or a full fight (see SHOULD SHIP below), but **a real Zyno encounter must be reachable and beatable after him** — minimum version: an existing Mini-Boss moveset/arena, palette-swapped, with new Zyno dialogue and a distinct name/identity. This is the one item on this list that is genuinely new scope; see the flag above.

## SHOULD SHIP

Strongly desired, real quality-of-experience impact, but the game is still recognizably "Deeper" without them at MVP time.

- [ ] Biomes 2 & 3 (Flooded Tunnels, Molten Depths) fully playable
- [ ] Full Depth Warden/father fight with all 3 phases (the fight itself is unchanged from the original Final Boss design — just reidentified; it's SHOULD SHIP the same way it always was, independent of the Zyno requirement above)
- [ ] A bespoke Zyno moveset/arena, replacing the reused-Mini-Boss MVP version above
- [ ] Full weapon-specific upgrade sub-pools (all 45 entries across 3 weapons)
- [ ] All 8 Curses
- [ ] Alt Ultimates (all 3 weapons)
- [ ] Full Hub Stat System (all 6 Core Stats + all 8 Marks)
- [ ] Secret Floors (key-gated vault rooms)
- [ ] Relics (all 3, Legendary tier) + Relic Vault

## CUT IF NECESSARY

Safe scope valves if Phase 3/5 content authoring (the identified schedule risk in IMPLEMENTATION_PLAN.md) runs long. Cutting these does not threaten the core loop.

- Weapon Mastery (already scoped as stub-only for MVP per IMPLEMENTATION_PLAN Phase 6 — full node effects are explicitly post-MVP)
- Reduce weapon sub-pools from 15 to ~8–10 entries each for MVP, backfill post-MVP
- Reduce shared upgrade pool from 24 to ~12–15 entries for MVP
- Wave Rooms capped at 1 per biome instead of 1–2 for MVP (still proves the mechanic exists)
- Biome 2/3 unique enemy elites — ship the 3 base enemies + Mini-Boss per biome, defer the Elite/key-drop/Secret-Floor loop specifically for Biomes 2–3 if needed (Biome 1's Secret Floor stays as the proof-of-concept)

**Removed from this list:** *Gambler's Edge and other Epic-tier situational upgrades* — superseded by the mixed-tier single-draw system (rarity gating within one offer no longer applies the same way). Revisit this cut once the new draw system is implemented and playtested. Note the "reduce shared pool from 24" valve above is now **23** entries, since Lucky Find was deleted with the run currency.

---

## MVP Definition of Done

A single unbroken playthrough must be possible:

1. Launch → Main Menu → Hub
2. Select a weapon → Descend
3. Clear Biome 1 (floors 1–5) using core loop, leveling up and encountering the Upgrade/Curse screen on each level-up
4. Defeat Mini-Boss 1 (The Collapsed King)
5. Reach Floor 16 (full 3-biome run if Biomes 2/3 shipped, or a Biome 1-only run to Floor 16 if not) and beat the Floor 16 sequence — the Depth Warden/father (stub or full, per SHOULD SHIP) then Zyno (minimum: reused-content version, per MUST SHIP)
6. Win or die → see Death/Victory screen showing depth reached and Shards earned
7. Return to Hub with Shards, spend on at least one Core Stat, redescend

If this sequence is unbroken and the 3 weapons feel meaningfully different while doing it, the MVP has succeeded — everything else is depth to layer on post-MVP.

---

## Explicitly Post-MVP (not even CUT IF NECESSARY — out of scope entirely for this pass)

- Full Weapon Mastery node effects
- Daily/weekly seeded runs, ghost-run replay (both proposed in earlier design discussion, never formally approved for MVP scope)
- Audio implementation beyond placeholder/must-have SFX list (GDD Audio section)
- Full VFX polish pass (MVP ships with the "must-have" VFX list from ART_DIRECTION §6 only)
- Evolution Tiers beyond the first (levels 10, 15, 20+)
- Trapped Soul types beyond The Warden's Soul
- Full Whisper Layer script (Biomes 2–3, escalation/glitch behavior)
- Full Memory Fragment content set
- Refusal State on any encounter beyond the father fight
- Flicker Recognition
- Post-Completion Truth Pass
