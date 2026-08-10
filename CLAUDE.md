# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Deeper" — a 2D pixel-art top-down action roguelike (vertical mine descent) built in Unity 6000.0.58f1 (URP 2D Renderer, new Input System). The codebase is in an early state: project scaffolding, full design/engineering documentation, and a first slice of gameplay code — player prefab, equipment/inventory system, 8-directional movement, a pose-driven animation rig, and a single test room. **No combat exists yet** — no attacks, no Attack State Machine, no Dig-Dash, no enemies, no damage pipeline.

## Source of truth — read before touching gameplay code or design

- `Assets/_Main/Docs/Design/01-GDD.md` through `09-DESIGN_RULES.md` are the **locked design source of truth** (game overview, core systems, content tables, balance numbers, art budgets, level design, day-by-day plan, MVP scope tiers, and the 14 process rules in `09-DESIGN_RULES.md`). Do not invent mechanics, numbers, or systems that contradict these docs — flag conflicts instead of silently resolving them (Design Rule 9, 11, 12).
- `Assets/_Main/Docs/Engineering/00-IMPLEMENTATION_PLAN.md` is the **engineering checklist**, mirroring the design plan's 7 milestones (M0 scaffolding → M6 Final Boss/Hub) with per-milestone Goal/Systems/Dependencies/Files/Order/Definition-of-Done/Risks. Check it before starting work to see what milestone is current and what's already done; update its checkboxes and notes in the same pass as any implementation change.
- `Assets/_Main/Docs/Engineering/01-VERIFICATION.md` records **environment-specific gotchas for verifying work** — the editor freezing the player loop when unfocused, simulated key input never reaching play mode, screenshots being unusable in this window layout, and the `execute_code` constraints. Read it before trying to verify anything in play mode or visually; re-deriving it wastes a lot of calls.
- Keep design and engineering strictly separate: design docs define *what* the game does, engineering docs define *how* it's built. An engineering limitation that seems to require a design change should be raised, not silently implemented as a design change.
- **Milestone order is not binding.** The project owner directs what gets built next, in whatever order they choose; the engineering plan is a running checklist of what exists, not a gate sequence. Mark items resolved as they're completed, and give owner-directed work that falls outside the milestone plan its own tracked section there. Still build one concrete objective at a time rather than speculatively scaffolding ahead.
- **Known unresolved design conflict:** the equipment/inventory system (armor slots + runtime weapon switching) contradicts locked design decisions — the design docs define no armor gear, and GDD/CORE_SYSTEMS §1 lock the weapon for a full run. It was built as owner-directed engineering work and the design docs were deliberately left unamended. Don't "fix" the docs to match the code, or the code to match the docs, without an explicit decision. See the Equipment & Inventory section of the engineering plan.

## Art

**All art work goes through the `deeper-art` skill** (`.claude/skills/deeper-art/`). Invoke it before generating or revising any sprite, character, equipment layer, tileset, icon or UI asset. It carries the locked art style (**Modern pixel art — not 8-bit/retro**), the palette and lighting rules, the sheet/naming contract the animation rig depends on, and the credit-safe generation order. Art generated without it will misalign against the rig rather than error.

## Commands

This is a Unity project, not a CLI-driven build. There is no package.json/Makefile — building, running, and testing all happen through the Unity Editor:

- **Open/run:** Open the project root in Unity Hub with Editor version `6000.0.58f1` (see `ProjectSettings/ProjectVersion.txt`), then Play from the Editor.
- **Build:** Unity Editor → `File > Build Settings` (no CLI build pipeline configured yet).
- **Tests:** `com.unity.test-framework` is installed but no tests exist yet. Once added, run via Unity's Test Runner window (`Window > General > Test Runner`), which supports running a single test by right-click → Run.
- **IDE:** `Deeper.sln` is the generated solution (opened via Rider or Visual Studio, both configured in `Packages/manifest.json` via `com.unity.ide.rider` / `com.unity.ide.visualstudio`). Don't hand-edit `.csproj`/`.sln` files — Unity regenerates them.

## Architecture (as specified by the design docs — not yet implemented)

The intended architecture, per `Docs/Design/02-CORE_SYSTEMS.md`, centers on a small number of generic systems that get reskinned/reused rather than rebuilt per weapon/biome/enemy (Design Rule 2):

- **`IWeapon` interface** — all 3 weapons (Katana, Bow, Greatsword) implement `BasicAttack()`, `HeavyStrike()`, `Ultimate()`, `OnHitLanded(target)`, `GetAttackTiming()`, so the rest of the game (animation, upgrades, HUD, Ultimate Gauge) never branches on which weapon is equipped. Per the engineering plan, Katana is built as a concrete implementation first (Milestone 1) and only generalized into the interface once one weapon proves the shape (Milestone 2) — deliberately not designed for 3 unknowns simultaneously.
- **Attack State Machine** — every weapon action runs `IDLE → WINDUP → ACTIVE → RECOVERY → IDLE`, with a Dash-Attack Cancel that's only legal during Recovery. Per-weapon timing/damage values are data, not hardcoded, so the same state machine drives all 3 weapons.
- **Single damage event pipeline** — `OnDamageDealt(source, target, amount)` is the one event that HUD, Combo Counter, and Ultimate Gauge all subscribe to, avoiding duplicated hit-handling logic.
- **Ultimate Gauge** — resource-gated (not cooldown-gated); fills per `OnHitLanded`, drains fully on use. Alt Ultimates are a swappable strategy on the `IWeapon` instance, not a separate system.
- **`HazardFront`** — one timer-driven system reskinned per biome (rockfall/cracked tiles, rising water/room geometry, lava flow/scorched ground). Build this generic in Milestone 3 (Biome 1) since Milestone 5 (Biomes 2–3) depends on it being a pure reskin, not a rebuild.
- **Room/Wave system** — floors pull 1–3 hand-authored rooms from a per-biome pool via deterministic shuffle (no live procgen, no branching paths). Wave Rooms are a boolean flag on Combat Room prefabs, not a separate room type.
- **Weighted-draw upgrade/curse pools** — shared upgrade pool + weapon-specific sub-pool + a separately-drawn Curse pool, all through one weighted-draw function with per-biome weight tables. Content (upgrades, curses, enemy stats) is intended to be data-driven (e.g., `ScriptableObject` per entry) so BALANCE.md's placeholder numbers can be retuned without code changes.
- **Boss weapon-check mechanics** — not a separate system; existing boss phase/state logic reads the equipped weapon type at phase transitions.

`Scripts/` under `Assets/_Main/` currently holds only what's been built: `Stats/` (shared `StatType`/`StatModifier`), `Player/PlayerStats.cs`, `Equipment/` (slot enum, gear `ScriptableObject`s, inventory, layered visuals) and `UI/` (inventory screen with GEAR/PREVIEW tabs). The remaining subfolders (Weapons, Combat, Enemies, Hazards, Rooms, Upgrades, Meta) are created as work actually needs them, not scaffolded upfront.

**Equipment sprite layering.** `EquipmentLayerView` is the shared base for anything drawing one art layer per gear slot; `EquipmentVisuals` (world rig, `SpriteRenderer`s) and `EquipmentPreview` (inventory paper-doll, UI `Image`s) differ only in renderer type. It refreshes on enable, so a view inside a hidden tab catches up when shown rather than waiting for the next equip. **Every body-layer sprite shares one 32×48 canvas and pivot** — that, plus a fixed draw order (Body, Legs, Feet, Chest, Head, Weapon = sorting order 0–5, and the same sibling order in the doll), is the only thing aligning the pieces. Real art must preserve both.

**Animation is pose-driven, not Unity `Animator`-driven.** `CharacterAnimator` owns one (state, facing, frame) pose and raises `PoseChanged`; every layer resolves its sprite from that same pose through the piece's `SpriteAnimationSet`. This is what keeps body and gear frame-locked — a paper-doll rig driven by Unity `Animator`s would need one controller per layer kept in sync. Don't add per-layer animators.

**Facing is 8-way, drawn with 5 authored directions.** `Facing` has all eight compass directions (`CharacterAnimator.FromDirection` snaps a vector by 45° octant). `Facing.ToArt()` collapses those onto five authored rows — Down, Up, Side, DownDiagonal, UpDiagonal — and the three left-hand facings reuse their right-hand art mirrored (`Facing.IsMirrored()` → `flipX` on renderers, negative X scale on UI Images). This is ART_DIRECTION §3's "8-directional, can mirror for 4 base directions": authoring eight separate sets would nearly double the character art budget for no visual gain. Add new directions by extending `FacingArt` and the `Clip` struct together, never by adding art rows alone.

**Sprite sheets.** Rows are always `Idle Down/Up/Side/DownDiag/UpDiag` then the same five for `Move` (10 rows); columns are frames; sub-sprites are named `<piece>_<row>_<col>` and referenced by the `SpriteAnimationSet` assets in `Data/Animation/`. Current **placeholder** sheets in `Art/Placeholder/Sheets/` are 4 columns (4 frames on every clip). **Real art targets 6 columns** — Idle uses columns 0–3, Move uses 0–5 — per the `deeper-art` skill. Differing clip lengths need no code change, because `SpriteAnimationSet.Resolve` wraps against each clip's own array length. Any reslice regenerates sub-sprites and silently nulls existing references, so **rebind the animation sets after every reimport**.

**`PlayerStats` is the shared stat pipeline.** Anything that changes a player number — equipment now, run upgrades (Milestone 4) and Hub Core Stats (Milestone 6) later — registers a named bundle of `StatModifier`s via `SetSource(key, modifiers)` and removes it by the same key, rather than mutating player numbers directly. Flat modifiers sum into the base value; percent modifiers sum with each other and apply once, matching BALANCE.md's "percentages are additive unless noted". Its `StatType` vocabulary intentionally mirrors the Hub Core Stats table so all three systems speak the same language.

## Repo conventions

- All gameplay assets live under `Assets/_Main/` (`Art`, `Audio`, `Data`, `Docs`, `Input`, `Materials`, `Prefabs`, `Scenes`, `Scripts`); `Assets/Settings` holds URP/render-pipeline config generated by Unity's project template and is left alone.
- `Assets/_Main/Data/` holds authored `ScriptableObject` content assets (currently `Data/Equipment/`). This is where the data-driven content the design docs describe as *tables* — upgrades, curses, enemy stats, Hub Stat costs — should go, so BALANCE.md's placeholder numbers can be retuned without code changes.
- `Assets/_Main/Art/Placeholder/` is procedurally-generated programmer art (flat colour blocks), not a style reference — it exists only to make layering verifiable before real art lands. Replacing a piece is a sprite swap on the gear asset's `bodyLayer`/`icon` fields; no code changes. Sprites import at 32 PPU, Point filter, uncompressed (ART_DIRECTION §1).
- `.gitattributes` treats Unity YAML assets (`.mat`, `.anim`, `.unity`, `.prefab`, `.asset`, `.meta`, `.controller`) with `unityyamlmerge` and LF line endings — don't hand-normalize these.
