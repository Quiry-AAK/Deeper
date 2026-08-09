# MVP — "Deeper"

Defines what must be playable at the end of the 45-day extended timeline (see 07-IMPLEMENTATION_PLAN.md). Priority tiers per PROJECT rules: **MUST SHIP → SHOULD SHIP → CUT IF NECESSARY**. The core loop — **Movement → Combat → Dig-Dash → Room Clear → Upgrade Pick → Descend** — is protected above everything below it.

---

## MUST SHIP

Non-negotiable. The MVP is not the MVP without these.

- [ ] All 3 weapons (Katana, Bow, Greatsword) fully functional: Basic Attack, Heavy Strike, Ultimate, all with distinct feel
- [ ] Ultimate Gauge system (fill-on-hit, drain-on-use, no cooldown)
- [ ] Attack State Machine (Windup/Active/Recovery) + Dash-Attack Cancel
- [ ] Dig-Dash with i-frames
- [ ] Biome 1 (Upper Caves) fully playable: all 6 Combat Rooms (incl. 1 Wave Room minimum), 2 Reward Rooms, Mini-Boss with weapon-check
- [ ] Rising Hazard system, at least the Upper Caves variant (rockfall + cracked tiles)
- [ ] Weighted-draw Upgrade system with the shared upgrade pool (can ship with a reduced subset — see CUT IF NECESSARY)
- [ ] Curse system (4th slot, at least 4–5 of the 8 curses)
- [ ] Full Hub loop: weapon select, Descend, Death/Victory screen, Ore→Ore Shard conversion, return to Hub
- [ ] At least a minimal Hub Stat System (2–3 Core Stats purchasable) proving the meta-progression loop works end to end
- [ ] Floor 16 stub: does not need to be the full Depth Warden fight — a placeholder "win condition" room is acceptable to prove the loop closes (full Final Boss is SHOULD SHIP, see below)

## SHOULD SHIP

Strongly desired, real quality-of-experience impact, but the game is still recognizably "Deeper" without them at MVP time.

- [ ] Biomes 2 & 3 (Flooded Tunnels, Molten Depths) fully playable
- [ ] Full Final Boss (The Depth Warden) with all 3 phases
- [ ] Full weapon-specific upgrade sub-pools (all 45 entries across 3 weapons)
- [ ] All 8 Curses
- [ ] Alt Ultimates (all 3 weapons)
- [ ] Full Hub Stat System (all 6 Core Stats + all 8 Miner's Traits)
- [ ] Secret Floors (key-gated vault rooms)
- [ ] Relics (all 3, Legendary tier) + Relic Vault

## CUT IF NECESSARY

Safe scope valves if Phase 3/5 content authoring (the identified schedule risk in IMPLEMENTATION_PLAN.md) runs long. Cutting these does not threaten the core loop.

- Weapon Mastery (already scoped as stub-only for MVP per IMPLEMENTATION_PLAN Phase 6 — full node effects are explicitly post-MVP)
- Reduce weapon sub-pools from 15 to ~8–10 entries each for MVP, backfill post-MVP
- Reduce shared upgrade pool from 24 to ~12–15 entries for MVP
- Wave Rooms capped at 1 per biome instead of 1–2 for MVP (still proves the mechanic exists)
- Gambler's Edge and other Epic-tier situational upgrades (low playtest priority — build variety generators, not core-loop-critical)
- Biome 2/3 unique enemy elites — ship the 3 base enemies + Mini-Boss per biome, defer the Elite/key-drop/Secret-Floor loop specifically for Biomes 2–3 if needed (Biome 1's Secret Floor stays as the proof-of-concept)

---

## MVP Definition of Done

A single unbroken playthrough must be possible:

1. Launch → Main Menu → Hub
2. Select a weapon → Descend
3. Clear Biome 1 (floors 1–5) using core loop, encountering the Upgrade/Curse screen each floor
4. Defeat Mini-Boss 1
5. Reach Floor 16 (full 3-biome run if Biomes 2/3 shipped, or the Floor 16 stub if not)
6. Win or die → see Death/Victory screen showing depth reached and Ore Shards earned
7. Return to Hub with Ore Shards, spend on at least one Core Stat, redescend

If this sequence is unbroken and the 3 weapons feel meaningfully different while doing it, the MVP has succeeded — everything else is depth to layer on post-MVP.

---

## Explicitly Post-MVP (not even CUT IF NECESSARY — out of scope entirely for this pass)

- Full Weapon Mastery node effects
- Daily/weekly seeded runs, ghost-run replay (both proposed in earlier design discussion, never formally approved for MVP scope)
- Audio implementation beyond placeholder/must-have SFX list (GDD Audio section)
- Full VFX polish pass (MVP ships with the "must-have" VFX list from ART_DIRECTION §6 only)
