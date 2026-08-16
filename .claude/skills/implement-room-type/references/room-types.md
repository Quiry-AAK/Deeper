# Room types — where to read, what to reuse, what blocks each

Pointers, not copies. Every requirement below lives in a design doc; read the cited section rather
than trusting this file, which lags by design. Statuses are hints for planning — confirm them with
`check-room-types` before building.

The canonical type list is one line: **`Design/02-CORE_SYSTEMS.md` §8**. If it disagrees with
anything here, it wins.

---

## Combat Room — built

**Read:** CORE_SYSTEMS §8 · LEVEL_DESIGN §2, §3, §4 · BALANCE §8 (30–60 s clear, 60–100 s for waves)

**Exists:** `CombatRoom` / `WaveSpawner` / `RoomDoor` / `RoomEntry`, `RoomLayout.cs`,
`CombatRoom_UpperCaves_01.prefab`. Verified in play mode.

**The job now is layouts and gaps**, not classes. LEVEL_DESIGN §2 wants **6 per biome** with 1–2
flagged as Wave Rooms; the room also is not §3-compliant until the Upper Caves' **cracked tiles**
and its **breakable-wall Dig-Dash shortcut** exist. The first layout reserves a 2×2 zone for the
cracked tiles. `CameraRig` also has no bounds clamp, so standing at a door shows past the room edge.

**Wave Rooms are a flag, not a type.** `WaveSpawner` with 2–3 waves already does it and
`CombatRoom.IsWaveRoom` derives from the array length — deliberately not a serialized bool, which
would be a second source of truth that can disagree. Building one is purely an authoring job.

---

## Secret Vault Room — not started

**Read:** CORE_SYSTEMS §8 ("Secret Floors") · LEVEL_DESIGN §2 · CORE_SYSTEMS §10 (Relics)

**Shape:** 1 layout reused across all biomes with biome-specific tile dressing — the function
matters more than layout novelty. A locked door gated on a `SecretKey` flag granted by defeating a
rare elite earlier in the biome. Payout is a large XP grant **or** a guaranteed Legendary-tier
upgrade offer.

**Reuse:** `RoomDoor` for the door itself (it already is a collider + sprite toggle); `RoomEntry` if
the vault needs a trigger; `PlayerXP` for the XP half.

**New:** the key flag and its check. The engineering plan sketches `Scripts/Rooms/SecretVault.cs` +
a player-side flag — note there is **no player inventory**, so the flag needs a home; the smallest
honest one is a field on the run's own state, not a new inventory system.

**Blocked / open:**
- The **Legendary-offer half needs the upgrade offer system** (Milestone 4), which does not exist.
  The XP half is buildable today; say so rather than half-building both.
- CORE_SYSTEMS §8 flags that **the risk half is gone** — entry used to cost time against the Rising
  Hazard. It is now pure upside and needs a new cost (a fight, a resource, a one-per-run limit).
  Owner's call, not engineering's.

---

## Trapped Soul Room — not started

**Read:** CORE_SYSTEMS §14 · LEVEL_DESIGN §2 · BALANCE §17 (soul effect values) · NARRATIVE
(unlocked — do not treat as binding)

**Shape:** small footprint, Secret-Vault-style layout. One bound soul as a short interactable.
2–3 soul slots per run; freeing one grants a persistent in-run effect; a freed soul can be lost
permanently for the run if it dies. **MVP scope is 1 soul type and 1 room in Biome 1.**

**Reuse:** `PlayerStats.SetSource(key, modifiers)` is the correct home for a soul's persistent
effect — add and remove by key, never mutate player numbers directly.

**New:** the interactable, the soul actor, the slot bookkeeping.

**Blocked / open:**
- §14 says outright that **freeing a soul currently costs nothing** and needs a new price, for the
  same reason as the vault.
- **How a freed soul is lost** is an open item in both CORE_SYSTEMS and BALANCE.
- A freed soul that follows the player is an escort actor with `EnemyChase`-grade steering and **no
  pathfinding** — check that against the layout constraint before promising it.

---

## Mini-Boss Room — not started, and the biggest

**Read:** LEVEL_DESIGN §2, §6 · CORE_SYSTEMS §11 (weapon-check), §16 (Overcharge) ·
CONTENT_DESIGN (the boss roster)

**Shape:** one large open arena per biome, no feeder rooms — the floor is Combat rooms (plus any
detour) → Mini-Boss room, full stop. It must fit the boss's full attack radius plus kiting space,
and runs the biome's environmental mechanic during the fight. Every 5th floor. Biome 1's is **The
Collapsed King** — note the narrative pass recast this as the father, which is an unlocked conflict,
not a decision.

**Blocked:** almost everything the room needs is upstream of the room.
- **No boss actor exists**, and no phase/state logic to read the equipped weapon at phase
  transitions (§11 is explicit that this is not a separate system).
- **No environmental mechanics exist** — cracked tiles, water, geysers are all unbuilt.
- **Overcharge** (§16) is a scoped buff on the upgrade-modifier stack, which is Milestone 4.

If asked for this, propose splitting it: the arena and its lifecycle are buildable now; the boss is
its own objective. Do not scaffold a boss framework speculatively.

---

## Final Boss Arena — not started, bespoke

**Read:** LEVEL_DESIGN §6 · CORE_SYSTEMS §8 · CONTENT_DESIGN (Zyno) · NARRATIVE (unlocked)

**Shape:** Floor 16, the only room that changes its own geometry mid-fight, and explicitly a one-off
non-reused layout. LEVEL_DESIGN §6 says a bespoke controller is the correct call here — forcing it
through the generic room system is **not** required by Design Rule 2, which governs systems, not
deliberate set-pieces.

**Blocked / open:**
- The degrading arena was specced as "all 3 biome hazard types in sequence", **which no longer
  exists** after the Rising Hazard was cut. LEVEL_DESIGN §6 calls the rebuild on surviving
  mechanics a re-spec, not a translation — designer work, not engineering's to invent.
- The floor hosts **two fights** (the father, then Zyno), and the Final Boss identity is an open
  CONFLICT in the change brief.

---

## Reward Room — cut, do not build

Removed in CORE_SYSTEMS §8 and LEVEL_DESIGN §2 when Shards became run-end only, so its function
(currency payout) no longer exists. `CONTENT_DESIGN` §6 still lists 2 per biome — that is a known
stale copy, already recorded in the change brief. If asked for one, say it is cut and cite §8.

---

## Shared infrastructure every type is waiting on

Not a room type, and worth naming whenever one is planned:

- **Room loading / `RoomManager` / the reshuffling-bag draw** of 3–5 rooms per floor (CORE_SYSTEMS
  §8) is unbuilt. `CombatRoom.Cleared` is the event written for it. Until it exists, every room type
  is `TestScene`-only.
- **No player death / run-end.** She can die inside a locked room whose doors only open when the
  enemies do. This bites hardest on exactly the room types that lock.
- **`RoomTrigger` is layer 8.** Any new room-scoped volume goes there, always.
