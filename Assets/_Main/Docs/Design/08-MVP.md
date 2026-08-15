# MVP — "Deeper"

Defines what must be playable at the end of the 45-day extended timeline (see 07-IMPLEMENTATION_PLAN.md). Priority tiers per PROJECT rules: **MUST SHIP → SHOULD SHIP → CUT IF NECESSARY**. The core loop — **Movement → Combat → Dig-Dash → Room Clear → Upgrade Pick → Descend** — is protected above everything below it.

---

✅ **RESOLVED: narrative now has tiers.** The session changelog gives the narrative layer an explicit MVP subset — minimal Whisper Layer lines (Biome 1 + the father fight), a Memory Fragment pickup with a Codex UI stub, and the Refusal State on the father fight only — all MUST SHIP below. Everything beyond that subset is in Explicitly Post-MVP. The earlier "no tier assigned" flag is closed.

⚠️ **SUPERSEDED, needs the owner's confirmation: "Zyno is MUST SHIP."** The previous pass made a real Zyno encounter MUST SHIP and removed Floor 16's placeholder-stub safety valve. **The session changelog overrides that** — it states Zyno *"never appears physically in the MVP"* and is present only through the Whisper Layer, and the owner directed the changelog to take precedence. The MUST SHIP list below now reflects the changelog. The previous pass's costing still stands if Zyno is later put back: the cheapest real version is a reused Mini-Boss moveset/arena, palette-swapped with new dialogue (the Elite recontextualization technique, ART_DIRECTION §4 / NARRATIVE §4a), and a bespoke fight is an additional-timeline conversation the 45-day plan does not absorb for free. **What Floor 16 is now — with the father moved to Biome 1 and Zyno absent — is undecided.** See `00-DESIGN_CHANGE_BRIEF.md` §11.

## MUST SHIP

Non-negotiable. The MVP is not the MVP without these.

- [ ] All 3 weapons (Katana, Bow, Greatsword) fully functional: Basic Attack, Heavy Strike, Ultimate, all with distinct feel
- [ ] Ultimate Gauge system (fill-on-hit, drain-on-use, no cooldown)
- [ ] Attack State Machine (Windup/Active/Recovery) + Dash-Attack Cancel
- [ ] Dig-Dash with i-frames
- [ ] Biome 1 (Upper Caves) fully playable: all 6 Combat Rooms (incl. 1 Wave Room minimum), Mini-Boss with weapon-check — the Mini-Boss is **the father**
- [ ] Rising Hazard system, at least the Upper Caves variant (rockfall + cracked tiles)
- [ ] Weighted-draw Upgrade system with the shared upgrade pool (can ship with a reduced subset — see CUT IF NECESSARY)
- [ ] Curse system (4th slot, at least 4–5 of the 8 curses)
- [ ] XP/Leveling system: enemies drop XP, level-up pauses game and opens a mixed-tier upgrade offer (single weighted draw across the full pool, not floor-gated)
- [ ] Room reshuffling-bag logic: 3–5 rooms/floor drawn from the per-biome pool without immediate repeats
- [ ] Shard run-end calculation (Levels Gained + Depth Reached) — no in-level currency pickups
- [ ] 1 Evolution Tier per weapon (3 total), mutually-exclusive kit-replacement choices at a level-5 milestone
- [ ] 1 Trapped Soul type (The Warden's Soul) + 1 Trapped Soul room in Biome 1
- [ ] Narrative identity pass on existing Biome 1 content: father as Mini-Boss, minimal Whisper Layer lines, Refusal State on the father fight only
- [ ] Memory Fragment pickup type + Hub Codex UI stub (small Biome-1 fragment set)
- [ ] Full Hub loop: weapon select, Descend, Death/Victory screen, run-end Shard award, return to Hub
- [ ] At least a minimal Hub Stat System (2–3 Core Stats purchasable) proving the meta-progression loop works end to end
- [ ] **Floor 16 — undecided, blocked on the conflict flagged above.** The session changelog keeps Zyno out of the MVP entirely and moves the father to Biome 1, which leaves Floor 16 without an identity. Until that is settled, treat the old placeholder "win condition" room as the working assumption for closing the loop, and do not schedule a Zyno fight.

## SHOULD SHIP

Strongly desired, real quality-of-experience impact, but the game is still recognizably "Deeper" without them at MVP time.

- [ ] Biomes 2 & 3 (Flooded Tunnels, Molten Depths) fully playable
- [ ] Full multi-phase Floor 16 boss fight, whatever its final identity turns out to be (the encounter design is unchanged from the original Depth Warden; only *who it is* is contested)
- [ ] A physical Zyno encounter of any kind — moved out of MUST SHIP by the session changelog, which keeps him to the Whisper Layer for the MVP
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
4. Defeat Mini-Boss 1 — the father
5. Reach Floor 16 (full 3-biome run if Biomes 2/3 shipped, or a Biome 1-only run to Floor 16 if not) and beat whatever the Floor 16 encounter resolves to — see the contested-identity flag at the top of this file
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
