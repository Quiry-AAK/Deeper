# ENGINEERING IMPLEMENTATION PLAN — "Deeper"

Technical checklist for building the systems defined in the design docs (`Assets/_Main/Docs/Design/`). This document tracks **engineering status only** — it does not redefine design (see `01-GDD.md` through `09-DESIGN_RULES.md` for that) and does not restate numeric values (see `04-BALANCE.md`).

Mirrors the milestones in `Design/07-IMPLEMENTATION_PLAN.md`, but **milestone order is not binding**. The project owner decides what gets built next; this document is the running inventory of what exists and what doesn't, not a gate sequence. Work items are checked off as they're completed, whichever milestone they belong to, and owner-directed work that sits outside the milestone plan gets its own tracked section rather than being squeezed into a milestone it doesn't belong to.

Update this file (check boxes, add notes) at the end of every implementation task.

**Engine:** Unity 6000.0.58f1, URP (2D Renderer), new Input System, no additional third-party packages unless a milestone below calls one out explicitly.

**Current status:** Milestone 0 done. Owner-directed work has landed outside the milestone plan — see **Equipment & Inventory System** and **Player Movement, Animation & Test Level** below. Milestone 1 is partially covered by that work (movement, animation rig, test room); its combat half — Attack State Machine, Katana kit, Dig-Dash, damage pipeline, Ultimate Gauge, first enemy — has not been started.

---

## Milestone 0 — Project Scaffolding

**Goal:** A clean, empty Unity project that's ready to receive Phase 1 code.

**Status:** ✅ Done (this pass).

- [x] Folder structure under `Assets/_Main/` (`Art`, `Audio`, `Docs`, `Input`, `Materials`, `Prefabs`, `Scenes`, `Scripts`)
- [x] URP 2D Renderer + Input System packages installed
- [x] Design docs in place (`Docs/Design/`)
- [x] Engineering docs folder created (`Docs/Engineering/`, this file)

**Note:** `Scripts/` subfolders (Player, Weapons, Combat, etc.) are created as work actually needs them, not pre-built — see Design Rule 2 (reuse before building) and the project's "don't overbuild" directive. A `Deeper.Runtime` assembly definition can be added once there's enough code to justify separate compilation; not needed yet.

---

## Equipment & Inventory System — owner-directed, outside the milestone plan

**Status:** ✅ Built and verified in play mode (this pass).

**⚠️ Design status: unresolved conflict with locked design decisions. Design docs deliberately NOT amended.**

This system was requested directly and built as engineering work. Two things about it contradict the locked design source of truth, flagged here per Design Rules 11/12 rather than silently written into the design docs:

1. **Armor equipment does not exist in the design.** GDD §Player defines mitigation as avoidance (Dig-Dash i-frames), Hyper Armor (Greatsword only) and "HP/armor upgrades" — where *armor* means the run upgrade pool (Iron Skin) and Hub Core Stats (Second Skin), not worn gear. Four armor slots are a new system, a new content pool, and a real cost against `ART_DIRECTION.md` §3's frame ceilings, since every piece needs coverage across every animation state and direction.
2. **Runtime weapon switching contradicts a locked decision.** GDD §Player: weapon is *"Locked for the full run"*; CORE_SYSTEMS §1: *"No runtime weapon-swapping"*; this document's own Cross-Cutting notes say not to build for it.

Neither is resolved. If armor equipment becomes real design, that's an explicit reopen (Rule 11) requiring GDD, CORE_SYSTEMS, CONTENT_DESIGN and ART_DIRECTION to be updated together (Rule 14). Until then the code exists and the design docs describe a different game.

**Built:**
- [x] `StatType` / `StatModifier` — shared stat vocabulary, deliberately matching the Hub Core Stats table (BALANCE §15) plus flat damage reduction
- [x] `PlayerStats` — base line + registered modifier sources; flat modifiers sum into the base, percent modifiers sum with each other and apply once (BALANCE's "percentages are additive")
- [x] `EquipmentSlot` — Head / Chest / Legs / Feet / Weapon, stable explicit indices
- [x] `EquipmentDefinition` / `WeaponDefinition` — `ScriptableObject` per gear entry; the Weapon slot is type-guarded so only weapon assets can occupy it
- [x] `EquipmentInventory` — 5 slots + carried list; equipping into an occupied slot returns the displaced piece to carried instead of destroying it
- [x] `EquipmentLayerView` — shared subscribe/refresh/apply-sprite base for anything drawing one art layer per slot; refreshes on enable so a view inside a hidden tab catches up when shown
- [x] `EquipmentVisuals` — `SpriteRenderer` layers on the in-world player rig
- [x] `EquipmentPreview` — UI `Image` layers for the inventory paper-doll, same base class, no second camera or render texture
- [x] `InventoryUI` / `InventoryItemButton` — equipped column, carried column, live stat readout; one row component backs both lists
- [x] `TabGroup` — mutually exclusive content panels; GEAR and PREVIEW tabs
- [x] `Player.prefab` — Rigidbody2D (dynamic, zero gravity, rotation frozen), feet-footprint CapsuleCollider2D, 6-layer sprite rig (Body + one per slot, sorting orders 0–5)
- [x] `InventoryCanvas.prefab` + EventSystem (`InputSystemUIInputModule`)
- [x] 8 placeholder gear assets under `Assets/_Main/Data/Equipment/`
- [x] 17 placeholder sprites under `Assets/_Main/Art/Placeholder/` (9 body layers + 8 icons), imported at 32 PPU / Point filter / uncompressed per ART_DIRECTION §1
- [x] Verified in play mode: swaps propagate to slots, carried list, stat aggregation, the world rig and the preview doll simultaneously, with no console errors

**Sprite layering contract:** every body-layer sprite is authored on the same 32×48 canvas with the same pivot, so pieces align by construction rather than by per-piece offsets. Draw order is sorting order 0–5 on the world rig (Body, Legs, Feet, Chest, Head, Weapon) and the matching sibling order in the preview doll. Real art must keep that canvas and that order — it's the only thing making the layers line up.

**Placeholder art:** everything under `Art/Placeholder/` is programmer art — flat colour blocks with a darker outline, generated procedurally, not a style reference. It exists so the layering is verifiable before real art lands. Replacing a piece is a sprite swap on the gear asset's `bodyLayer`/`icon` fields; no code changes. Directional variants (the rig currently assumes one facing) are an open question for whenever real art starts.

**Deliberately not built:**
- Player movement — that's Milestone 1 and was not requested; the prefab is shaped to receive it
- Directional / animated equipment art — the layer rig is direction-agnostic, but nothing drives per-direction sprite sets yet
- Any combat behaviour on `WeaponDefinition` — equipping a weapon changes stats and sprite layer only. The asset is the intended home for BALANCE §2 Windup/Active/Recovery data when the Attack State Machine exists
- Weapon stat modifiers — per-attack damage is timing-table data (BALANCE §2), not a stat block, so weapon assets carry no modifiers and swapping weapons correctly shows no stat change

**Known constraints:**
- The `ScriptableObject` asset *is* the item identity, and `PlayerStats` keys modifier bundles off that reference. One asset can therefore be equipped/carried only once — no duplicate stacks. Supporting duplicates needs a runtime instance wrapper around the definition; deferred until something actually requires it.
- The UI uses legacy `UnityEngine.UI.Text` with Unity's built-in font, because TextMeshPro's essential resources aren't imported. It's a dev-facing screen and will be replaced by the real UI art pass (ART_DIRECTION §5).
- `InputSystem_Actions.inputactions` is still Unity's stock template (Move/Look/Jump/Sprint…), untouched for Deeper. The inventory toggle reads `Keyboard.current` directly rather than adding actions to an asset that needs a proper pass first.

---

## Player Movement, Animation & Test Level — owner-directed

**Status:** ✅ Built and verified in play mode (this pass). Overlaps the movement half of Milestone 1.

**Built:**
- [x] `PlayerController` — 8-directional, fixed speed, no acceleration curve (GDD §Player). Speed is read from `PlayerStats` every frame, so equipment/upgrades/Hub stats change it without touching the controller. Input comes from the `.inputactions` asset via `FindActionMap`/`FindAction`, not hardcoded keys.
- [x] `CharacterState` / `Facing` enums — Idle and Move only; Attack/Dash/Hit/Death join when the Attack State Machine lands, deliberately not stubbed
- [x] **8-way facing** — `CharacterAnimator.FromDirection` snaps a movement vector to one of eight compass directions by 45° octant, so diagonals read as diagonals instead of collapsing onto the dominant axis
- [x] `SpriteAnimationSet` — per-piece art table: frames per state per authored direction. **Five directions are authored (Down, Up, Side, DownDiagonal, UpDiagonal), mirrored to cover all eight facings** via `Facing.IsMirrored()`, matching ART_DIRECTION §3's "8-directional, can mirror for 4 base directions". Authoring eight sets would nearly double the character art budget for no visual gain.
- [x] `CharacterAnimator` — owns state, facing and a free-running frame counter, and raises `PoseChanged`. Not a Unity `Animator`: a paper-doll rig would need one controller per layer kept in sync, this needs one integer
- [x] `EquipmentLayerView` reworked to resolve every layer from that single shared pose, so body and gear are frame-locked by construction
- [x] Test room: 28×16 tile room, wall ring + 5 interior pillars, `TilemapCollider2D`; floor/wall placeholder tiles; camera framed on the room
- [x] 9 animated sheets (4 cols × 10 rows, sliced to 40 sub-sprites each) + 9 `SpriteAnimationSet` assets

**Verified:** all eight facings resolve the correct art row (E→Side, NE→UpDiagonal, N→Up, NW→UpDiagonal mirrored, W→Side mirrored, SW/SE→DownDiagonal, S→Down); mirrored pairs return the identical sprite differing only in `flipX`; facing persists when movement stops; body and all gear layers land on identical row/frame; collision stops the player at exactly wall-face minus collider half-width (x=26.70 against the wall at x=27); all 9 sets resolve every state × 8 facings × 4 frames.

**Camera is static**, framed so the room is exactly one screen tall. Fine for a single test room; rooms larger than one screen will need a follow camera, which is not a design decision the docs have made yet.

**Known gap — input could not be verified end to end.** The action resolves correctly (8 controls: WASD + arrows, enabled, bound through the asset), but synthetic device events queued from an editor-context script never reach play mode's input buffer, so *actual key presses moving the character* is unverified by automation and needs a human at a focused Game view. Everything downstream of the input value is verified.

**Deliberately not built:** Dig-Dash, attacks, i-frames, enemies, any combat — all Milestone 1 combat scope, none of it requested yet.

---

## Milestone 1 — Foundation & Single-Weapon Combat Loop
*(maps to Design/07 Phase 1, Days 1–10)*

**Goal:** Katana fully functional in one test room — proves the Attack State Machine and Ultimate Gauge before multiplying by 3 weapons.

**Systems/features involved:**
- Player movement (8-directional, fixed speed, per GDD §Player)
- Attack State Machine: `IDLE → WINDUP → ACTIVE → RECOVERY → IDLE` (CORE_SYSTEMS §2)
- Dig-Dash + i-frames (GDD §Player, BALANCE §1)
- Katana Basic Attack + Heavy Strike, hitbox/damage pipeline
- `OnDamageDealt(source, target, amount)` event (CORE_SYSTEMS §6)
- Ultimate Gauge (fill-on-hit, drain-on-use) + Katana Combo Counter + Combo Finisher (CORE_SYSTEMS §4, §5a)
- Dash-Attack Cancel (CORE_SYSTEMS §2)
- One test enemy (Cave Crawler) with basic AI, player HP/damage-taken loop

**Dependencies:** None (first gameplay milestone).

**Files/systems likely to be created:**
- `Scripts/Player/PlayerController.cs` (movement, input read via new Input System)
- `Scripts/Combat/AttackStateMachine.cs` (or per-weapon component using a shared state enum/struct)
- `Scripts/Combat/IWeapon.cs` (interface stub — full generalization deferred to Milestone 2 per Design Rule "don't abstract for 3 unknowns at once"; Katana can start as a concrete class)
- `Scripts/Combat/KatanaWeapon.cs`
- `Scripts/Combat/Hitbox.cs`, `Scripts/Combat/DamageEvents.cs` (`OnDamageDealt`)
- `Scripts/Combat/UltimateGauge.cs`
- `Scripts/Combat/ComboCounter.cs`
- `Scripts/Player/DigDash.cs`
- `Scripts/Enemies/CaveCrawler.cs`, `Scripts/Enemies/EnemyHealth.cs`
- `Assets/_Main/Input/InputSystem_Actions.inputactions` (already exists — extend with attack/dash/ultimate actions)
- A test scene (reuse or replace `SampleScene.unity`)

**Implementation order:** movement → state machine skeleton → Katana Basic/Heavy hitboxes → damage event → Ultimate Gauge → Combo Counter → Katana Ultimate → Dash-Attack Cancel → test enemy → playtest pass.

**Definition of Done:** Katana's full kit (Basic/Heavy/Ultimate/Dash, incl. Dash-Attack Cancel) feels good against Cave Crawler in a single test room. Matches Design/07 Phase 1 exit criteria.

**Potential technical risks:**
- Designing the Attack State Machine too Katana-specific, making Milestone 2's generalization painful — mitigate by keeping windup/active/recovery timing data-driven (per-weapon values, not hardcoded) even though only one weapon exists yet.
- Input System action map churn as more actions (Heavy Strike, Ultimate, Dash) get added — keep the `.inputactions` asset as the single source of truth, don't hardcode `KeyCode` checks.

---

## Milestone 2 — Bow & Greatsword, `IWeapon` Generalization
*(maps to Design/07 Phase 2, Days 11–18)*

**Goal:** Generalize Katana's concrete implementation behind the shared `IWeapon` interface (CORE_SYSTEMS §1), then implement Bow and Greatsword against it.

**Systems/features involved:**
- `IWeapon` interface: `BasicAttack()`, `HeavyStrike()`, `Ultimate()`, `OnHitLanded(target)`, `GetAttackTiming()`
- Bow: projectile hit-detection (reused for enemy ranged attacks too), Charge Shot variable windup, Piercing Shot Ultimate
- Greatsword: wide-arc hitbox, Hyper Armor state, Ground Slam Ultimate
- Weapon select screen (Hub stub only — full Hub is Milestone 6)

**Dependencies:** Milestone 1 (Attack State Machine, Ultimate Gauge, damage pipeline must exist and be proven on Katana first).

**Files/systems likely to be created:**
- `Scripts/Combat/IWeapon.cs` (promoted from stub to real interface)
- `Scripts/Combat/BowWeapon.cs`, `Scripts/Combat/GreatswordWeapon.cs`
- `Scripts/Combat/Projectile.cs` (shared by Bow and future enemy ranged attacks)
- `Scripts/Combat/HyperArmor.cs` (or a state flag read by the damage pipeline)
- `Scripts/UI/WeaponSelectStub.cs`

**Implementation order:** refactor Katana behind `IWeapon` (confirms the abstraction holds) → Bow → Greatsword → weapon select stub → back-to-back playtest.

**Definition of Done:** All 3 weapons feel distinctly different in the same test room. Matches Design/07 Phase 2 exit criteria.

**Potential technical risks:**
- `IWeapon` shaped wrong for Bow's variable-length windup (Charge Shot) if it was designed purely around Katana/Greatsword's fixed timing — this is exactly why Design/07 sequences generalization *after* one concrete implementation, not before.
- Projectile hit-detection needs to be reusable by enemies later (Rock Slinger, Current Wisp, etc.) — build it weapon-agnostic from the start, not Bow-specific.

---

## Milestone 3 — Biome 1 Content: Rooms, Enemies, Hazard
*(maps to Design/07 Phase 3, Days 19–26)*

**Goal:** First full playable biome, start to finish.

**Systems/features involved:**
- Room system: Combat/Reward room loading, room-lock logic (CORE_SYSTEMS §8)
- Hazard Front system: timer-driven, Upper Caves variant (rockfall + cracked tiles) (CORE_SYSTEMS §7)
- Upper Caves enemy roster: Cave Crawler (exists from M1), Rock Slinger, Tunnel Brute, Elite: Deep Warden
- Upper Caves room layouts: 6 Combat Rooms (1–2 flagged `IsWaveRoom`), 2 Reward Rooms (LEVEL_DESIGN §2–3)
- Mini-Boss: The Collapsed King, with weapon-check mechanic (CORE_SYSTEMS §11)
- Secret Vault room + key-drop logic (CORE_SYSTEMS §8)

**Dependencies:** Milestone 2 (all 3 weapons must exist — room layouts need to accommodate all 3, per LEVEL_DESIGN §2 positioning-zone requirement).

**Files/systems likely to be created:**
- `Scripts/Rooms/Room.cs`, `Scripts/Rooms/RoomManager.cs` (per-floor room sequencing, deterministic shuffle)
- `Scripts/Rooms/CombatRoom.cs` (room-lock logic, `IsWaveRoom` flag + wave-batch trigger)
- `Scripts/Hazards/HazardFront.cs` (core timer-driven system)
- `Scripts/Hazards/UpperCavesHazard.cs` (rockfall presentation + cracked-tile collapse micro-system)
- `Scripts/Enemies/RockSlinger.cs`, `Scripts/Enemies/TunnelBrute.cs`, `Scripts/Enemies/DeepWarden.cs`
- `Scripts/Enemies/BossPhaseController.cs` (weapon-check read, reused by all bosses per CORE_SYSTEMS §11)
- `Scripts/Rooms/SecretVault.cs`, `Scripts/Player/Inventory.cs` (SecretKey flag)
- Room prefabs/scenes under `Assets/_Main/Scenes/` or `Prefabs/Rooms/`

**Implementation order:** room loading + lock logic → Hazard Front (generic) → Upper Caves hazard skin → 3 base enemies → 6 Combat Room layouts + 2 Reward Rooms → Mini-Boss + weapon-check → Secret Vault + key drop → full-biome playtest.

**Definition of Done:** Full Biome 1 clear is playable start to finish with all 3 weapons. Matches Design/07 Phase 3 exit criteria (MVP.md's largest content-authoring risk — see below).

**Potential technical risks:**
- Room-layout authoring (18 total Combat Rooms across all biomes eventually) is explicitly flagged in Design/07 and LEVEL_DESIGN.md as the single biggest schedule risk — this milestone alone authors 6 of them plus 2 Reward Rooms.
- `HazardFront` must be built generic enough that Biome 2/3 variants (rising water + geometry change, lava + scorched ground) are pure reskins per Design Rule 2 — verify this before considering the milestone done, not after Milestone 5 discovers it doesn't generalize.

---

## Milestone 4 — Upgrade & Curse System
*(maps to Design/07 Phase 4, Days 27–32)*

**Goal:** The run-to-run build-variety layer.

**Systems/features involved:**
- Weighted-draw upgrade system (CONTENT_DESIGN §1, BALANCE §13)
- Shared upgrade pool (24 entries, CONTENT_DESIGN §1 / BALANCE §9)
- Weapon-specific sub-pools (45 entries total, CONTENT_DESIGN §2 / BALANCE §10)
- Curse pool (8 entries, CONTENT_DESIGN §3 / BALANCE §11) + always-visible 4th slot
- Upgrade screen UI (ART_DIRECTION §5)

**Dependencies:** Milestone 3 (upgrades are offered at floor-end, needs the room/floor loop to exist).

**Files/systems likely to be created:**
- `Scripts/Upgrades/UpgradeDefinition.cs` (data-driven — likely a `ScriptableObject` per upgrade so content authoring doesn't require code changes per entry)
- `Scripts/Upgrades/UpgradePool.cs`, `Scripts/Upgrades/WeightedDraw.cs`
- `Scripts/Upgrades/CurseDefinition.cs`, `Scripts/Upgrades/CursePool.cs`
- `Scripts/UI/UpgradeScreen.cs`
- Modifier application hooks on `PlayerController`/`IWeapon` implementations (many upgrades modify existing systems rather than adding new ones — per Design Rule 2, implement as parameter changes on existing components, not new systems per upgrade)

**Implementation order:** weighted-draw core (generic, tier-aware per BALANCE §13) → shared pool wired to effects → weapon sub-pools → Curse pool + 4th slot → upgrade screen UI → full-run playtest for build variety.

**Definition of Done:** A full Biome 1 run generates genuinely different builds run-to-run. Matches Design/07 Phase 4 exit criteria.

**Potential technical risks:**
- 69 total upgrade entries (24 shared + 45 weapon-specific) + 8 curses is a lot of individual effect implementations — MVP.md explicitly allows shipping a reduced subset (~12–15 shared, ~8–10 per weapon) if this runs long; flag early if it's trending that way rather than discovering it on Day 32.
- `ScriptableObject`-per-upgrade only pays off if the effect-application code is genuinely data-driven (e.g., a small set of effect "kinds" with numeric parameters) — if every upgrade needs bespoke code anyway, the `ScriptableObject` layer is just overhead. Decide this after the first ~5 upgrades are implemented, not upfront.

---

## Milestone 5 — Biomes 2 & 3
*(maps to Design/07 Phase 5, Days 33–40)*

**Goal:** Content-scale the Biome 1 pattern across the remaining two biomes — pure content authoring, no new core systems if Milestone 3 generalized correctly.

**Systems/features involved:**
- Flooded Tunnels: enemies (Eel Diver, Current Wisp, Bloated Drifter, Elite: Tideheart), rooms with low/high water-tile data, Mini-Boss (Drowned Custodian), hazard variant (rising water + room geometry change)
- Molten Depths: enemies (Ember Wisp, Magma Crawler, Forge Golem, Elite: Cinder Warden), rooms with geyser tiles + scorched ground, Mini-Boss (Molten Sentinel), hazard variant (lava flow)

**Dependencies:** Milestone 3 (`HazardFront`, `Room`, `BossPhaseController` must already be generic/reusable — this milestone is the test of that).

**Files/systems likely to be created:**
- `Scripts/Hazards/FloodedTunnelsHazard.cs`, `Scripts/Hazards/MoltenDepthsHazard.cs` (reskins of `HazardFront`)
- `Scripts/Enemies/EelDiver.cs`, `CurrentWisp.cs`, `BloatedDrifter.cs`, `Tideheart.cs`
- `Scripts/Enemies/EmberWisp.cs`, `MagmaCrawler.cs`, `ForgeGolem.cs`, `CinderWarden.cs`
- `Scripts/Enemies/DrownedCustodian.cs`, `Scripts/Enemies/MoltenSentinel.cs`
- Room prefabs for both biomes (12 more Combat Rooms + 4 Reward Rooms + 2 Mini-Boss arenas)

**Implementation order:** Flooded Tunnels (enemies → rooms → hazard → Mini-Boss) → Molten Depths (same order) → cross-biome playtest.

**Definition of Done:** A full 3-biome run (floors 1–15) is completable. Matches Design/07 Phase 5 exit criteria.

**Potential technical risks:**
- If this phase does *not* run faster than Milestone 3 per-biome, it means Milestone 3's systems weren't actually generalized — that's a signal to stop and fix the abstraction rather than push through with biome-specific hacks (Design Rule 2).
- Water/lava room-geometry changes (CORE_SYSTEMS §7) require rooms to define low/high tile zones in layout data — confirm this data format was actually built into the Milestone 3 room system, not bolted on ad hoc here.

---

## Milestone 6 — Final Boss, Hub, Meta-Progression
*(maps to Design/07 Phase 6, Days 41–45)*

**Goal:** Close the loop — death/victory return the player to a Hub that actually matters.

**Systems/features involved:**
- Final Boss: The Depth Warden, multi-phase, reuses all 3 hazard themes (BALANCE §6)
- Escape sequence (post-boss countdown, reuses Dig-Dash + Hazard systems)
- Hub Stat System: Core Stats + Miner's Traits (CONTENT_DESIGN §7, BALANCE §15)
- Ore → Ore Shard conversion (BALANCE §14)
- Death/Victory screens (GDD §UI)
- Relic Vault, Weapon Mastery stub (tracking only, per MVP.md)

**Dependencies:** Milestone 5 (Final Boss arena "incorporates all 3 biome hazard types in sequence" — needs all 3 hazard variants to exist first, per LEVEL_DESIGN §6).

**Files/systems likely to be created:**
- `Scripts/Enemies/DepthWarden.cs` (multi-phase, own dedicated arena logic per LEVEL_DESIGN §6)
- `Scripts/Meta/SaveData.cs` (persistent Ore Shards, Hub Stat ranks, Weapon Mastery counters, discovered Relics)
- `Scripts/Meta/HubStatSystem.cs`, `Scripts/Meta/CoreStat.cs`, `Scripts/Meta/MinersTrait.cs`
- `Scripts/Meta/OreConversion.cs`
- `Scripts/UI/HubScreen.cs`, `Scripts/UI/DeathVictoryScreen.cs`
- `Scripts/Meta/RelicVault.cs`, `Scripts/Meta/WeaponMastery.cs` (stub: counter tracking only)

**Implementation order:** Final Boss → escape sequence → Hub Stat System → Ore→Shard conversion + Death/Victory screens → Relic Vault + Weapon Mastery stub → full end-to-end playtest.

**Definition of Done:** Hub → Run → Death/Victory → Hub loop works end to end. Matches 08-MVP.md's MUST SHIP list, all items checked.

**Potential technical risks:**
- Save data format needs to be settled once and be forward-compatible-ish, since it's the first thing touched by every subsequent post-MVP content pass — avoid a schema that requires a migration for every new Miner's Trait added later.
- Final Boss arena is explicitly a one-off, non-reused layout (LEVEL_DESIGN §6) — don't try to force it through the generic `Room` system if it doesn't fit; a bespoke controller is the correct call here, not a violation of Design Rule 2 (reuse applies to systems, not to a deliberately unique set-piece).

---

## Cross-Cutting Engineering Notes

- **Data-driven content over hardcoded content**, wherever the design doc describes a *table* (upgrades, curses, enemy stats, Hub Stat costs) — favor `ScriptableObject` or serialized data assets so BALANCE.md's "numbers are placeholders" (Design Rule 8) can be iterated without code changes. Don't over-engineer this into a generic data-editor tool; a `ScriptableObject` per entry is enough.
- **One `OnDamageDealt` / `OnHitLanded` event pipeline**, per CORE_SYSTEMS §6 — Combo Counter, Ultimate Gauge, HUD damage numbers, and on-hit upgrade procs all subscribe to the same event rather than each system re-detecting hits independently.
- **No runtime weapon-swapping, no branching room paths** — these are locked design decisions (GDD, LEVEL_DESIGN §1); don't build generalized systems that accommodate them "just in case."
- **Assembly definitions:** not needed until compile times become a problem or an editor-only/runtime split is needed (e.g., editor tooling for authoring `ScriptableObject` content). Revisit if Milestone 4's content volume makes iteration slow.

---

## Open Engineering Questions

Carried from design docs' own "Open Items" sections — these affect implementation but are design calls, not engineering ones. Flagged here so they're not missed, per Design Rule 9 (undefined terms/decisions get resolved, not left ambiguous):

- Hazard-touch: confirmed instant-kill (BALANCE §7) — implement as such, no lingering "heavy damage" branch needed.
- Mini-Boss Overcharge exact clear-trigger condition (CORE_SYSTEMS §12) — needs a design decision before Milestone 3/5 boss work locks it in.
- Weapon Mastery node effects (3–5 per weapon) — explicitly deferred past MVP; Milestone 6 only needs the counter, not the effects.
