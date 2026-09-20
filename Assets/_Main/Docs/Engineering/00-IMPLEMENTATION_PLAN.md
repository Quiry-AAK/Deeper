# ENGINEERING IMPLEMENTATION PLAN — "Deeper"

Technical checklist for building the systems defined in the design docs (`Assets/_Main/Docs/Design/`). This document tracks **engineering status only** — it does not redefine design (see `01-GDD.md` through `09-DESIGN_RULES.md` for that) and does not restate numeric values (see `04-BALANCE.md`).

Mirrors the milestones in `Design/07-IMPLEMENTATION_PLAN.md`, but **milestone order is not binding**. The project owner decides what gets built next; this document is the running inventory of what exists and what doesn't, not a gate sequence. Work items are checked off as they're completed, whichever milestone they belong to, and owner-directed work that sits outside the milestone plan gets its own tracked section rather than being squeezed into a milestone it doesn't belong to.

Update this file (check boxes, add notes) at the end of every implementation task.

**Engine:** Unity 6000.0.58f1, URP (2D Renderer), new Input System, no additional third-party packages unless a milestone below calls one out explicitly.

**Current status:** Milestone 0 done. Owner-directed work has landed outside the milestone plan — see **Run Loadout**, **Player Movement, Animation & Test Level**, **Real Character Art**, **Katana attacks**, **Player prefab structure**, **Damage pipeline & training dummy**, **Biome 1 basic enemies** and **TestScene** below. Milestone 1 is largely covered by that work (movement, animation rig, test room, Attack State Machine, Katana Basic/Heavy/Ultimate, Ultimate Gauge, Combo Counter, hitbox + damage pipeline), and its "real enemy with AI" gap is now closed: the whole Upper Caves basic roster — Cave Crawler, Rock Slinger, Tunnel Brute and the Elite Deep Warden — chases, telegraphs, attacks and dies, on placeholder art. Milestone 1's remaining gaps are now closed too: the Dig-Dash and its Dash-Attack Cancel, and — as of 2026-09-08 — player death and run-end, alongside a sixteen-floor run in its own scene (see **The run** at the end of this document).

**The Rising Hazard was cut on 2026-08-15 (owner).** No `HazardFront`, no per-biome timer, no chase — see `Design/02-CORE_SYSTEMS.md` §7, now a removal notice. Per-biome environmental mechanics (cracked tiles, water/currents, geysers) survive as room-authored components. This is no longer breaking news: the GDD's 2026-09-18 rewrite states the post-hazard world as settled fact and has dropped its inline ⚠️ hole callouts, promoting the knock-ons into numbered questions — `GDD §Open Decisions 1` (is there a clock at all), `3` (what a Trapped Soul costs), `5` (does the escape sequence survive) and `8` (Greed's Toll has no downside). They remain design calls, not engineering ones.

**The GDD was rewritten by the design owner on 2026-09-18, and the milestone bodies below have been reconciled against it** — see *GDD reconciliation* at the end of this document for the full list of what moved. Three things changed at once: divergences this file and the change brief carried as deviations are now **locked design** (the Dash Attack, the chargeable Heavy Strike, the dash travelling along the movement keys, the HUD's dash pip and upgrade strip, the icon-led offer card, one Death/Victory screen, the Secret Vault's cost and payout); the eleven `GDD §Open Decisions` replace the scattered ⚠️ callouts and are now mapped item-by-item in *Open Engineering Questions* below; and the GDD has stopped carrying hard numbers this document's serialized fields own (movement ramp, chain window, lunge distances). The earlier **2026-08-14** amendment (the designer's session changelog, applied from this side — `Docs/00-DESIGN_CHANGE_BRIEF.md` §11) is folded into that reconciliation rather than still being listed as pending: Reward Rooms are gone, upgrades are level-up triggered, floors draw 3–5 rooms via a reshuffling bag, Shards are awarded once at run end, and XP/Leveling, Evolution Tiers, Trapped Souls and the narrative subset (Whisper Layer, Memory Fragments, Refusal State) are MUST SHIP.

The inventory/armor system has been **removed** at the owner's direction: a run is now one weapon chosen in the Hub, and the protagonist is a single fixed character — a woman in a hooded cape and light armour. A narrative layer was introduced in the same pass and is recorded in `Design/10-NARRATIVE.md`; a handoff for the designer listing every change and conflict is in `Docs/00-DESIGN_CHANGE_BRIEF.md`. Both are owner-directed and not locked. The player has real animated art — Idle and Move, 4 frames × 5 directions, plus a sheathed Katana layer.

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

## Run Loadout — owner-directed, replaced the Equipment & Inventory System

**Status:** ✅ Inventory removed. Weapon-only loadout in place. **Not yet verified in the editor** — the Unity MCP connection dropped mid-change, so the prefab/scene YAML edits below were made by hand and still need one play-mode pass.

**Design status: the standing conflict is RESOLVED, in favour of the locked design.** The owner decided to delete the inventory system outright. The player now picks a weapon in the Hub before the run and nothing else; the weapon is locked for the descent, exactly as GDD §Player and CORE_SYSTEMS §1 always specified. Armor is gone. Body/gender selection was considered and dropped in the same pass — the protagonist is a single fixed character (see `Design/10-NARRATIVE.md`).

Armor and helmets return **post-launch as cosmetics only**, carrying no stats and no mid-run swapping. That is why the rig is still layered rather than collapsed into a single sprite.

**What replaced it:**
- [x] `RunLoadout` — the run's single choice, a `WeaponDefinition`, plus `SetWeapon` for the Hub to call. Applies the weapon's `StatModifier`s through `PlayerStats.SetSource` and raises `LoadoutChanged`.
- [x] `WeaponDefinition` — now a standalone `ScriptableObject` rather than a subclass of the deleted `EquipmentDefinition`. Field names were kept identical so the three existing weapon assets deserialize unchanged.
- [x] `CharacterLayerView` / `CharacterVisuals` — draw two layers, body and weapon, both resolved from the one `CharacterAnimator` pose. The four ex-armor `SpriteRenderer`s stay wired as `cosmeticRenderers` and are force-disabled on enable, so no stale gear art survives and post-launch cosmetics have somewhere to draw without rebuilding the prefab.

**Deleted:** `EquipmentInventory`, `EquipmentDefinition`, `EquipmentSlot`, `EquipmentVisuals`, `EquipmentLayerView`, `EquipmentPreview`, `InventoryUI`, `InventoryItemButton`, `TabGroup`, `InventoryCanvas.prefab`, and the 5 armor `.asset` files. `Data/Equipment/` is now `Data/Weapons/`.

**Kept deliberately:** every art PNG and every `Anim_*.asset`. The animation sets hold 40 sub-sprite bindings each and are the expensive part to rebuild; the helmet and vest art stays on disk for the post-launch cosmetic pass even though nothing references it now.

**Technique worth reusing — renaming a script without breaking prefabs.** Unity resolves `m_Script` by the GUID in the `.meta`, so renaming a `.cs` **and its `.cs.meta` together** (`git mv` both) preserves every prefab and scene reference through a rename. That is how `EquipmentInventory → RunLoadout` and `EquipmentVisuals → CharacterVisuals` kept their component bindings on `Player.prefab` instead of turning into missing scripts. Only the serialized *field names* had to be hand-edited.

**Hand-edited YAML (needs an editor pass to confirm):**
- `Player.prefab` — `RunLoadout` now has `weapon: Weapon_Katana`; `CharacterVisuals` has `loadout` / `bodyAnimation` / `bodyRenderer` / `weaponRenderer` / `cosmeticRenderers[4]`
- `SampleScene.unity` — removed the `InventoryCanvas` prefab instance (468 lines), its stripped component reference, and its `SceneRoots` entry

**Verified by inspection, not by Unity:** no dangling references to any deleted armor asset remain in `Player.prefab`; every other GUID it references resolves to a file on disk; no script references any deleted type.

**Still missing:** the Hub weapon-select screen. `RunLoadout.SetWeapon` is the seam it will call — the prefab currently ships with Katana assigned so the test scene plays without a Hub.

## Real character art — owner-directed, in progress

The protagonist is a **single fixed character**: a woman in a hooded short cape over light leather armour (PixelLab character `ad2c77cf`). There is no body selection. The earlier bald-miner body and its helmet/vest layers are superseded — that art is still on disk but fits the old body, not her, and would need redoing for the post-launch cosmetic pass.

- [x] Protagonist body — `create_character` v3, size 48, 8 directions down-selected to the 5 authored ones
- [x] **Idle** — 4 frames × 5 directions
- [x] **Move** — 4 frames × 5 directions
- [x] `Weapon_Katana` — sheathed katana layer, static per direction
- [x] **Katana attack states** — Basic, Heavy Strike, Ultimate, **4 frames each** × 5 directions, on their own sheet
- [x] **Basic chain hit 2** — 4 frames × 5 directions on `Sheets/Katana_Chain2.png`, bound to `BasicAttack2`
- [x] ~~Basic chain hit 3~~ — **cut, owner-directed.** The chain loops, so hit 1 following hit 2 already reads as a third distinct cut in play. `basicChainLength` is 2. `CharacterState.BasicAttack3` and its fallback stay in place, so a third clip is a data change if it is ever wanted
- [ ] Bow and Greatsword layers and attack sets
- [ ] Dig-Dash / hit / death states — none exist

### Katana attacks

Frame counts sit inside ART_DIRECTION §3's per-weapon ceilings (Basic 5, Heavy 6, Ultimate 8). The sword is **baked into the pose**, not layered — with armor gone and one weapon locked per run there is no combinatorial explosion, so three weapons means three sheets rather than 3ⁿ. Layering the blade per attack frame would have cost ~1,900 generations; baking cost 40.

**Attacks live on their own sheet** (`Sheets/Katana_Attacks.png`, 432×1410, 4 cols × 15 rows, **108×94 cells**) because a swung blade leaves the body's footprint — measured 36–51px wide and up to 58px above the feet, against a 32×48 cell. Giving attacks a separate sheet means Idle/Move never re-slice and keep their bindings.

**Alignment rule:** every attack frame is cropped so her *resting-pose body* sits at the same offset from the cell centre as it does in the 32×48 idle cell (feet at centre +23). With both sprites pivoted Center she occupies the identical world position, so she does not jump when an attack starts. Measured clipping across all 60 attack frames: **0 pixels**.

**The weapon layer disables itself during attacks, by construction.** `Anim_Weapon_Katana` has no clip for the attack states, so `Resolve` returns null and `CharacterVisuals` disables the renderer — the sheathed hip sword vanishes exactly as the drawn sword appears in the body art. No double-sword, and no code needed.

**`end_frame_url` is for looping clips only — never for one-shot attacks.** Forcing an attack to return to the exact resting pose squeezed the motion out of it: the first Heavy Strike pass produced no overhead cut at all and a white artifact where the blade should be. Dropping `end_frame` (keeping `custom_start_frame_url`) restored a real draw → raise → strike → crouched follow-through. Idle/Move still need both frames; attacks need only the start.

**Verified:** `Anim_Body_Base` resolves every state at **4 frames × 5 authored directions**, zero nulls (`Katana_Attacks` rows 0–4 Basic, 5–9 Heavy, 10–14 Ultimate). Mirrored pairs return identical sprites on attack states. No console errors.

**Two bugs came from guessing frame counts instead of reading them.** Every attack clip is 4 frames, but the state machine assumed the ART_DIRECTION ceilings (Basic 4 / Heavy 6 / Ultimate 8) and asked the *weapon's* animation set, which has no attack clips at all because the sword is baked into the body art. `Resolve` wraps `frame % length`, so Heavy stepped 0–5 against a 4-frame clip and visibly restarted into frames 0 and 1 before being cut off — it read as the attack trying to fire twice. `CharacterLayerView.BodyAnimation` now exposes the set that actually draws her and `StartHit` reads the real count, warning instead of wrapping if it is ever zero.

**The five directions are five different swings, and that is the root cause of most attack-feel bugs.** The generator did not draw one arc rotated five ways — the frame on which the blade actually connects differs per direction (Side cuts on frame 2; UpDiagonal is still winding up on frame 2 and only sweeps down on frame 3), and some directions finish with the blade carried around behind her back. Three separate reported bugs — camera shake firing early on NE/NW, the character freezing on E/W, and S/SE "not matching" — were all this one fact. Two things follow, and both are needed:

- **`SpriteAnimationSet.StrikeFrames`** — a per-authored-direction strike index stored *with the art*, since that is what it is a property of. `Anim_Body_Base` authors HeavyStrike as `[2,2,2,2,3]` in FacingArt order (Down, Up, Side, DownDiagonal, UpDiagonal); mirroring gives NW the same 3 as NE for free. Empty or 0 falls back to the middle frame.
- **Recovery settles back to the stance frame.** Recovery is the longest phase (0.35s of a 0.77s Heavy), so whatever frame it parks on defines what the attack looks like. Parking on the follow-through held a blade-behind pose for a third of a second, which read as a different move; the follow-through now plays for the first half of Recovery and frame 0 for the rest. Every direction finishes the same way, with no extra art.

**Do not duplicate a frame to hide an unwanted pose.** Rebuilding east as `[0,1,2,2]` to keep the blade forward looked correct in the sheet and read in motion as the character freezing mid-attack, because a duplicated final frame is held for the whole Recovery. All five directions now use all four authored frames (verified: no two frames in any Heavy row are identical).

**Chain hits go on their own sheet, never appended to `Katana_Attacks.png`.** Adding rows changes the texture height, which forces a reslice, which regenerates every sub-sprite and silently nulls all 60 existing references. `Katana_Chain2.png` is 440×360, 4×5 of **110×72** cells — a different cell size to the attack sheet, which is fine because the contract that actually aligns the two is her **feet sitting `FEET_BELOW_CENTRE` (23px) below the cell centre** with both sheets pivoted Center. That holds at any cell size, so she does not shift between chain hits. `build_chain.py <group> <out> <zip>` packs any future hit the same way.

**Chain 2 generated cleanly on the first attempt** (pro, 5 directions, 100 generations) — hood and cape intact everywhere, katana a proper blade everywhere, and baked arcs in all five directions **including north**, which needed its trail hand-drawn for chain 1. Its strike frames are `[1,2,1,2,1]`, derived by measuring which frame each direction's arc peaks on rather than assumed. Weapon-layer `Anim_Weapon_Katana` has no BasicAttack2 clip, so the sheathed hip sword hides itself during the hit exactly as it does for the other attack states.

**Heavy Strike art is a known-bad area — five generation attempts failed.** Recorded so it is not retried blindly: 2× v3 (blade dissolved into white smears; then a clip with no swing at all) and 3× pro (lost the hood and cape entirely; then a full 5-direction batch that turned the katana into a **gold staff** in east and a **scythe** in north with almost no blade travel). ~142 generations spent, none shipped except the first south regeneration. The shipped Heavy is the original pro batch plus code-side fixes. The five arcs still differ in shape from each other; the remaining options are reusing the Basic swing art for Heavy (guaranteed consistent, differentiated by timing/trail/impact) or hand-authored art.

**Heavy's slash trail is drawn in code, not generated.** The Basic chain came back from the generator with bright arcs; Heavy came back with none, so the slower, 20-damage attack read as *weaker* than the 8-damage one. The trail is drawn by locating the blade tip in each frame (brightest pixels furthest from her chest) and sweeping between consecutive tips, so it re-derives itself whenever a direction is regenerated instead of needing hand-tuned angles per direction — which matters, because south has now been regenerated three times. Three things it needs, each found by looking at the result:
- **Cap the sweep** (~102°). The blade travels ~180° from wind-up to impact; drawing all of it produces a giant crescent floating free of the character, the same failure as the abandoned `SlashVFX` overlay. A trail only reads as motion when it stays attached to the blade.
- **Measure the cell without the trail.** The trail is clipped into the cell, so letting it drive the cell measurement is circular — it grew the cell from 94px to 106px, which would have forced a reslice and nulled all 60 bindings.
- **Fade at the cell edge.** A straight clip cut the bright core off square and left a hard vertical bar hanging in the frame, which reads as a rendering glitch rather than a slash.

Weighted across three frames around the strike, and shifted backwards when the strike *is* the last frame (the up-diagonal), which otherwise gets half the trail of every other direction.

**South Heavy was regenerated.** The batch prompt said "raising the katana high overhead then *slamming it down*", which foreshortens on a front-on view into planting the blade in the ground while crouching — a visibly different action from the sweep every other direction performs. Regenerated in **pro** mode as a diagonal cut across the body finishing with the blade level and forward. **v3 is not usable for this**: two attempts at 1 generation each produced first a blade dissolved into white smears, then a clip with no overhead raise at all. Pro (20/direction) is the only mode that has produced usable Katana attack art.

**Anchor frames on the legs, not the whole bounding box.** Cell centring used frame 0's full bbox, which worked only while frame 0 happened to be a compact guard pose. The regenerated south starts with the blade already extended, dragging the bbox centre **7px** right — enough to draw her body off-centre and make her jump the moment a south Heavy began. The bottom 8 rows are legs and feet in every frame of every direction and no blade reaches them, so they are the stable anchor; it reproduces every existing direction within 1px. The build also **pins the cell to the sliced 108×94 grid** instead of shrinking to fit (the leg anchor would have produced 102×94), because any cell-size change forces a reslice that silently nulls every `SpriteAnimationSet` reference.

**The follow-through frame is not optional.** Heavy was briefly rebuilt as frames `[0,1,2,2]` on the theory that its last pose read as a second attack starting — that was the wrong fix for the wrapping bug above, and it broke the swing itself. North-east's strike happens entirely between frames 2 and 3, so dropping frame 3 left the sword hanging in the air and the clip ended before it landed; south ended on a crouched vertical plunge instead of its sweep. Restored to `[0,1,2,3]`. The rebuild is pixel-identical in size (432×1410, 108×94 cells, `clipped=0`), so no reslice and no rebinding was needed.

### Attack State Machine — built

`Scripts/Combat/AttackStateMachine.cs` runs IDLE → WINDUP → ACTIVE → RECOVERY → IDLE and is on `Player.prefab`. It never branches on weapon type: phase timings are authored data on `WeaponDefinition.GetAttackTiming(action)`, which is the seam `IWeapon.GetAttackTiming()` will sit on in Milestone 2.

| Action | Input | Windup | Active | Recovery | Total | Damage |
|---|---|---|---|---|---|---|
| Basic | LMB / gamepad west | 0.10 | 0.08 | 0.18 | 0.36 | 8 |
| Heavy Strike | RMB / gamepad north | 0.30 | 0.12 | 0.35 | 0.77 | 20 |
| Ultimate | R / gamepad LB | 0.25 | 0.40 | 0.45 | 1.10 | 40 |

Basic and Heavy are BALANCE §2 verbatim. **BALANCE has no Windup/Active/Recovery row for Ultimates** — those three numbers are placeholders and need a design answer.

### Ultimate is a buff — owner-directed, contradicts locked docs

`UltimateBuff` replaces the Ultimate's damage with a timed self-buff: the cast raises the katana, an aura comes up on her and much harder on the blade, and every attack trails it while it lasts. **CORE_SYSTEMS §4 and BALANCE §2 (40 damage) both disagree with this** — recorded in `Docs/00-DESIGN_CHANGE_BRIEF.md` §7f, including the open question of whether `IWeapon.Ultimate()` can still assume every Ultimate is an attack. That one wants deciding **before Milestone 2 generalises the interface**. Gauge behaviour is unchanged: still resource-gated, still drains fully on use.

Duration 8s / +50% damage / **+40% attack speed** / +15% move speed are **placeholders** — BALANCE has no row for a buff Ultimate. The buff goes through `PlayerStats.SetSource`, the same pipeline as run upgrades and Hub Core Stats, so it composes with them and cannot leak; re-casting refreshes rather than stacks.

**The Ultimate's shape is weapon data, so `IWeapon` can cover both.** `WeaponDefinition.UltimateShape` is `Attack` or `Buff` with a `UltimateBuffSpec` payload; the state machine branches on it rather than assuming. Katana is `Buff`, Bow and Greatsword remain `Attack`. This is what lets `IWeapon.Ultimate()` express both shapes in Milestone 2 instead of hardcoding one.

**`StatType.AttackSpeed` (new, value 8)** scales all three attack phases together — never Recovery alone, because the strike frame is pinned to the Active window and moving one phase without the others slides the blade out of its own damage window. Appended rather than inserted, since `StatType` serialises by integer and renumbering would silently repoint saved modifiers. Buffed: Basic 0.36s → 0.26s, Heavy 0.77s → 0.55s.

**`DamageBonus` modifiers must be Flat, not Percent.** `PlayerStats` computes `(base + flat) * (1 + percent)` and `baseDamageBonus` is **0**, so a percent modifier multiplies zero and the buff silently does nothing — it applied cleanly and changed no numbers. `DamageBonus` is itself a fraction, so Flat 0.5 *is* the +50%. `AttackSpeed` (base 1) and `MoveSpeed` (base 5) have non-zero bases and correctly use Percent.

**The Ultimate cast needed no new art.** "Raise the katana" is exactly Heavy's frame 1, so the Ultimate rows are rebuilt from Heavy as `[0,1,1,1]` — she draws, raises, holds while the aura comes up, then settles. The old "rapid flurry of slashes" art it replaced was wrong for a buff anyway.

**The aura is a scaled-up silhouette of her own sprite, drawn behind her through `Materials/AuraSilhouette.shader`.** The shader keeps the sprite's shape and throws away its colours, outputting a flat additive tint against the texture's alpha — without that it reads as a second character showing round the edges rather than an aura. What sticks out past her *is* the aura, and it follows her outline exactly because it is her outline. One renderer per layer, correct for every pose, frame and facing at no cost, and it will stay correct for any art added later.

The katana's silhouette scales **1.45× against the body's 1.14×** and is `weaponAuraBoost` brighter, which is what makes the blade's aura read as exaggerated.

**The aura is generated in the shader, from one extra sprite per layer** — one for her body, one for the katana, each a copy of the sprite the rig is already drawing, scaled up for reach. `AuraSilhouette.shader` builds the fire in this order: snap to a pixel grid sized to **one game pixel** (`rect × scale`, derived — a fixed number would make the aura's pixels grow with the reach and stop matching the art), sample the sprite's alpha as a shape mask, apply a **radial falloff** (the soft round shape of a default particle texture), threshold **fbm noise scrolling along −Y**, then quantise the heat ramp to a few flat steps so it bands like hand-drawn fire.

**Threshold the noise against a radially-varying cut; do not multiply then threshold.** Multiplying noise by the falloff scales every pixel down together, so one global cut just carves an ellipse — solid inside, nothing outside. Driving the *cut* by the falloff (`cut = 1 - radial × _Coverage + _Erode`) keeps almost everything at the core and only the brightest noise at the rim, which is what tatters the edge into tongues. Getting this backwards cost two rebuilds: the first drew a solid blob, and with the old `_Erode` still tuned for the pre-multiply formula the aura vanished almost entirely.

**The radial falloff also removes the sprite-border seam** that a scaled silhouette otherwise shows — her art runs flush to the cell edges with zero margin, so the silhouette ends in a straight cut, and a radial mask simply cannot produce one. An explicit `_EdgeFade` was tried first and is no longer needed.

**Tongue size is capped by resolution, not taste.** The aura is only ~62×125 game pixels, so above `_FlameSize` ≈ 2.5 there are too few noise cells across it to form separate tongues and it collapses back into one blob. 1.6 was chosen by rendering a sweep and looking.

Superseded: a three-layer stack of eroded silhouettes (the radial falloff does the same core-to-tips job in one pass, 6 renderers → 2) and per-quad wobble (all motion now comes from the scrolling noise; scaling the quad on top only shook the flames around).

**An earlier iteration burned the silhouette without the radial mask.** `AuraSilhouette.shader` eats the shape away with three octaves of value noise scrolling upward through it, so the band between her real outline and the enlarged copy breaks into licking tongues instead of staying a smooth halo. Erosion is biased by height (`_TopBias`), so it stays solid near her body and tatters toward the top the way fire does, and the surviving thickness drives a colour ramp from a white-hot core to the magenta fringe. Sampling is never offset, so it cannot bleed into neighbouring frames of a packed sheet — the trap that ruled out a neighbour-sampling outline shader earlier.

**It is drawn as a stack of layers, because one layer can only ever be a band.** Three concentric copies at increasing scale and increasing erosion (`innerErode` 0.14 → `outerErode` 0.62), each with its own noise seed — identical seeds erode identically and collapse the stack back into a single band. Outer layers sort furthest back so the hot core sits in front. `_Erode` and `_Seed` go through a `MaterialPropertyBlock`, so all layers still share one material.

**Wobble has to grow outward, not apply evenly.** At `wobbleAmount` 0.16 the amplitude is larger than the 0.14 gap between layers, so an even wobble lets an inner layer swell past an outer one and inverts the core-to-tongues ordering the effect depends on — measured, and it happened on the first attempt. Scaling wobble by the layer's distance from her fixes it and matches how fire behaves: steady at the base, chaotic at the tips. Verified **0 ordering inversions across 24 samples**.

**The wobble is per-axis, not a uniform pulse.** Width and height are driven by two independent Perlin channels, so the aura squashes and stretches rather than inflating and deflating — a uniform scale pulse reads as a breathing balloon. Perlin rather than a sine keeps it off an audible-looking beat; a third and fourth channel drive positional drift and an opacity flicker. The katana wobbles `weaponWobbleBoost` (1.7×) harder on top of its larger scale. Measured over ten samples at `wobbleAmount` 0.16: scaleX 1.05–1.19, scaleY 1.02–1.23, and **x ≠ y on 10/10 samples**, which is the property that distinguishes squash from pulse.

**Scaling happens about the sprite's pivot, not about her art**, and she is not drawn at her pivot. Left uncorrected the copy slides away from her as it grows, which looks like the aura drifting off to one side. `DrawSilhouette` places it at `pos + d(1 - scale)` so her art stays centred on itself at any scale — **per axis**, since width and height wobble independently and one shared correction would reintroduce the drift.

**Three earlier approaches were built and removed** — recorded so none is retried:

- **Offset sprite copies** (a ring of the whole silhouette pushed outward). Dilates rather than outlines: the copies fill in, so it reads as a soft blob around her. Also cost 84 renderers.
- **Baked silhouette-edge sheets** (`dilate minus original`, per-cell so it could not bleed across frames). Geometrically correct and cheap, but **glitchy in motion** — a hard 2px rim on 4-frame pixel animation pops frame to frame rather than flowing. Removed along with its sheets.
- **A procedural magenta flame loop** drawn behind her and on the blade. Looked good in isolation but buried the silhouette it was meant to sit under. `auraflame.py` and `VFX/Aura_Flame.png` are kept, so it is a few lines away if wanted again.

**She is not centred in her cell, and the flame has to follow where she is drawn.** Measured on the Idle/Move sheet, her legs sit 3–4.5px right of centre on the side and diagonal rows; mirroring the left-hand facings flips that the other way. A flame pinned to the transform therefore sits off to one side facing right and off to the other facing left, which is exactly how it looked. **The art cannot simply be re-centred — the cells have zero spare margin on both edges**, so shifting the pixels would clip her. `AuraVisuals.Measure` instead scans each sprite once and caches **two** anchors, because the two effects need different ones: the **bounding-box centre** (what a scaled copy must grow about) and the **horizontal centre of the bottom rows** — her legs — which is where fire is planted. They genuinely disagree: the sheathed katana juts out to one side and drags the bounding box back toward the middle while her legs sit up to 3px off it. Using one for both silently reintroduces the drift.

This requires the character sheets to be **Read/Write enabled**. The scan degrades to no correction rather than throwing if that is ever turned off — and note Unity raises `ArgumentException`, not `UnityException`, for an unreadable texture: catching only the latter let it escape `LateUpdate` and took the flame down with it.

**The flame loop is procedurally generated magenta fire** (`Art/Placeholder/VFX/Aura_Flame.png`, 8 frames of 96×128), matching an anime-style reference the owner supplied: turbulent magenta tongues with a white-hot base, engulfing her so she reads as a dark silhouette against it. Drawn behind the character at 3 world units tall.

Two reasons it is procedural rather than generated through PixelLab: **fire has no facing**, so one loop serves all eight directions and no directional set is needed; and it can be retuned in seconds instead of costing 20 generations per attempt. `auraflame.py` builds it. It burns on **both** her and the katana, with the blade's at 0.62× the size but `weaponAuraBoost` (2.4×) the brightness — smaller and hotter, so the weapon reads as the source of the power rather than a second bonfire. The katana's flame follows the sheathed-weapon layer and hides itself during attacks, when the blade is baked into the body art and the body flame already covers it.

The loop is seamless because the noise field **tiles vertically** — the lattice wraps at `H`, so scrolling it by exactly `H / frames` per frame returns to the start with no crossfade and no seam. This requires every octave's cell size to divide `W` and `H` exactly; breaking that silently reintroduces the seam. Two details that decide whether it reads as fire or smoke: the noise cells are **stretched along y** (12×32), which turns blobs into licking tongues, and the field is **hard-thresholded then re-expanded**, which gives the tongues crisp edges. An earlier pass had the vertical gradient inverted — `y = 0` is the *top* — which put a soft blob above her head instead of fire at her feet.

**The aura also needs no art on the character itself, and the katana out-glows her for free.** `AuraVisuals` draws an **additive** copy of whatever sprite the rig is already showing. Additive blending adds each pixel in proportion to its own brightness, so the near-white blade blows out hard while dark armour barely lifts — the requested "katana has a lot more" falls out of the blend mode instead of needing the blade masked by hand. Because it copies the live sprite it stays correct for every pose, frame and facing, including animations added later. The sheathed weapon layer gets its own aura at `weaponAuraBoost` (2.4×) since it is a separate renderer during Idle/Move, and follows that renderer's enabled state — the layer hides itself during attacks, when the blade is baked into the body art instead. Attack trails are a fixed pool of fading afterimages, so a long combo cannot allocate.

**Input bindings were missing and had to be added.** The Player map had only `Attack`; `RightClick` exists but lives in the **UI** map, so binding Heavy to it would have silently failed. Added `HeavyStrike` (RMB, matching CONTENT_DESIGN's "RClick") and `Ultimate` (**R**, matching GDD §Controls and CORE_SYSTEMS §4, which both specify it — an earlier note here claimed no design doc did, and was wrong) to the Player map.

**Attack clips cannot use the animator's shared frame rate.** Katana Basic totals 0.36s, shorter than its four frames take at 8fps. `CharacterAnimator.PlayAction(state, duration, frameCount)` therefore stretches an action's clip across the action's own duration; the free-running counter still drives Idle/Move. `SetMotion` keeps updating **facing** during an action but no longer overrides **state**, so the player can aim mid-swing without cutting the animation short.

**Frames are pinned to phases, not spread evenly.** An attack clip's frames are not equal in meaning — the authored order is guard, wind-up, strike, follow-through — so spreading four frames evenly across a duration puts the blade wherever the arithmetic lands. Measured on Heavy (0.30/0.12/0.35, 0.19s per frame): the Active window ran 0.30–0.42s, entirely inside the frame where the sword is still raised overhead, while the actual sweep happened during Recovery. `PlayAction(state, windup, active, recovery, frameCount, strikeFrame)` maps frames `0..strike-1` across Windup, holds `strike` for the whole Active window, and plays the rest across Recovery, so the blade and the hitbox occupy the same instant for every direction and every weapon **without touching BALANCE's timings**. `AttackStateMachine.alignFramesToPhases` toggles it back to even spreading for comparison.

**Since built:** hitboxes and the damage pipeline now attach to `ActivePhaseOpened` — see **Damage pipeline & training dummy** below. `CanCancel` still exposes the Dash-Attack Cancel window (legal for the whole Recovery phase, BALANCE §2) ahead of the Dig-Dash existing.

**Inferred, not stated — flagged for design:** attacks root the player (`PlayerController.rootDuringAttack`, default on). BALANCE §2 singles out the alt Ultimate "Thousand Cuts" as *"player-mobile"*, which only distinguishes it if attacks are normally rooted. That is an inference from one adjective and should be confirmed.

**Known gaps:**
- The Ultimate has **cyan slash arcs baked into the character frames**. ART_DIRECTION reserves cyan-white as a hazard accent, so this is a style conflict to resolve; it also means the arcs are not separable as VFX.
- ART_DIRECTION §6's **weapon hit-flash VFX has not been made** — it is still outstanding as its own asset.
- Idle/Move art does not yet have the katana baked in; it still uses the layered hip sword. Completing the per-weapon bake means regenerating those two clips with the sword visible (~10 generations).

Frame counts sit inside `ART_DIRECTION.md` §3's ceilings (Idle 4, Move 6). `walking-4-frames` was chosen over the 6-frame template deliberately: it matches the existing 4-column sheet grid, so Unity never re-slices and the `SpriteAnimationSet` bindings survive. Six frames would force a 192×480 sheet, a re-slice, and a full rebind (VERIFICATION.md §6).

### Findings — all learned expensively, do not re-derive

**`edit_image` cannot feed a layer pipeline.** It preserves the character semantically but redraws every pixel — 63% of a frame changed on a single helmet edit — so nothing clean remains to subtract. Only `inpaint_image` freezes the area outside the mask. Measured drift outside the mask, across every piece and direction attempted: **0 pixels**.

**Masks must be measured per direction, not guessed.** One generous 20px-wide head mask produced five different helmets, worst on the side view where the mask was wider than the entire 15px-wide body. Measuring each direction's actual extent and padding by 2px fixed it. Mask sizing is the single biggest lever on cross-direction consistency.

**Template animations destroy costume silhouette.** `breathing-idle` and `walking-4-frames` rebuild the character from a skeleton that knows nothing about a cape, and dropped it entirely on back-facing views — **−27% opaque pixels on north, −22% on north-east**. Fix: `mode="v3"` with `custom_start_frame_url` pointing at the *rotation* image, which animates from art that already has the cape. Anything with a distinctive silhouette (cape, cloak, tail, long hair) must use v3 with a start frame, never a bare template.

**v3 animations drift outward without an end frame.** Frames progressively billowed the cape past the 32px cell (frame 3 hit 34–37px). Passing `end_frame_url` = the same rotation bounds the drift *and* produces the seamless loop the style guide requires. **Always pass both `custom_start_frame_url` and `end_frame_url`** — a start frame alone is not enough. An Idle generated without an end frame drifted so far it read as *running*: the cape flared sideways and an arm lifted, while the Move clip generated *with* an end frame was visibly calmer. Getting this wrong inverts idle and move.

**Frame 4 of a 5-frame v3 clip is the loop closing.** With `keep_first_frame=true` and `frame_count=4` you get 5 frames where frame 4 ≈ frame 0 (measured motion ~0). Use frames 0–3 as the cycle and discard frame 4.

**She is exactly 32px wide at rest, so the cell has no margin for cape sway.** Once the cape is correctly present, `Move south` reaches 36px on its widest frame. Dropping the offending frames would leave a 1-frame walk, so the sheet instead centres each row's window on its *resting* pose and accepts the overflow — measured at 47 clipped pixels across four frames, worst case 29 of 845 (3.4%) at the outermost hem. Vertically the window is anchored to the feet and shared across all rows, costing 1px of hood on one frame. Both are far cheaper than the alternatives; do not "fix" this by re-centring per frame, which cancels the animation.

**One crop window per row, never per frame.** Aligning each frame to its own bounding box silently cancels the animation's motion — the character slides instead of steps. The window is shared horizontally within a row and **vertically across all rows**, so she never jumps when turning. Rows use different x-origins because directions sit at different canvas positions and no single window covers all of them.

**Packed multi-sprite inpainting is blocked by transport, not by the API.** Batching many cells into one `inpaint_image` call would cut cost ~5×, and the image fits the API's 512×512 limit — but a 160×48 strip is ~7,700 base64 chars and the MCP client truncates arguments at roughly 4,200. Quantizing barely helps (the payload is structural, not palette). **Use `image_url` against the already-hosted PixelLab frames instead of uploading** — that avoids base64 entirely, at the cost of one call per image.

### Cost model

| Operation | Generations |
|---|---|
| `create_character` v3, 8 directions | 2 |
| `animate_character` template or v3 | ~1 per direction |
| `inpaint_image` | ~20 per call, one image per call |
| `edit_image` | ~20–40, unusable here |

Enemies and bosses are **whole characters** and never need inpainting, so they use the 2-generation path — roughly 17 each fully animated. Only the player's weapon and cosmetic layers need the expensive tool.

### Game feel — owner-directed ("make it feel like Hades")

**The up/down-attack complaint was a contrast bug, not a 2D/perspective one.** The natural diagnosis is foreshortening, and it was wrong: measured horizontal sweep is actually *wider* facing up/down (91px) than facing side (69px). The real cause was brightness — in the peak swing frame the baked arc had **197 bright pixels facing side but 14 facing up**. Facing up was worst because the character turns away, her pale cloak fills the frame, and a dark arc on a light cloak disappears. Measure luminance before blaming projection.

**Fix: the slash arc is now its own sprite layer** (`SlashVFX`), not baked into the character frames. One arc per authored direction at a normalised palette (574–698 bright pixels each — consistent by construction), drawn at sorting order 20 above every character layer, offset forward along the facing vector, fading out over 0.12s on unscaled time so it stays visible through hitstop. Left-hand facings mirror via `flipX` like the character art. This simultaneously fixes contrast, cloak occlusion, the source-canvas truncation that was clipping the Ultimate's crescent, and finally delivers ART_DIRECTION §6's "weapon hit-flash".

**Other feel systems, all code:**
- `HitStop` — 45/85/110ms per action. Refuses to stack so chains cannot compound into a stall, and restores timescale on disable so the game can never be left frozen.
- Attack **lunge** — 0.75/1.15/0.9 world units on an ease-out curve, direction locked per hit. Replaced an earlier root-in-place behaviour that was inferred from one adjective in BALANCE and was the main reason attacks felt weightless.
- `CameraRig` — smooth follow (0.15s), look-ahead (1.1 units) leading the player's input, and Perlin-based impact shake on **unscaled** time so shake and hitstop overlap rather than cancelling. The camera was previously static, which is a large part of why the game felt stiff.

**Movement acceleration — later built, owner-directed.** This section previously recorded it as deliberately *not* done, because GDD §Movement locks *"fixed speed, no acceleration curve (keeps controls crisp and easy to tune)"*. The owner subsequently directed it: `PlayerController.accelerationTime` is **0.055s** and `decelerationTime` **0.085s**, the asymmetry being what reads as weight rather than float. The intent behind the locked rule is preserved — 55ms is about three frames at 60fps, imperceptible as lag — and both values tune to zero to restore the documented behaviour exactly. Recorded in `00-DESIGN_CHANGE_BRIEF.md` §7d, pending a Rule 14 pass. `EnemyChase` uses the same ramp at 0.12 / 0.16.

**No longer temporary:** hitstop, shake and gauge fill used to fire on the Active phase rather than on contact, gated behind `UltimateGauge.fillOnSwingUntilEnemiesExist`, so whiffs stuttered. All three now hang off `AttackHitbox`'s contact reports and the flag is gone — see **Damage pipeline & training dummy** below.

### Verified in the editor

`Anim_Body_Base` and `Anim_Weapon_Katana` each resolve **64/64** (2 states × 8 facings × 4 frames) with zero nulls. `Player.prefab` loads with **0 missing scripts** and carries PlayerStats, RunLoadout, CharacterVisuals, CharacterAnimator and PlayerController. Scripts compile with no errors or warnings. No `.meta` changed through any art pass, so nothing has ever needed rebinding.

### Known quality gaps

- The katana is worn at the hip, so from **directly behind** only the scabbard tip shows under the cape. That is deliberate and physically right, not a defect — an earlier attempt to force a full sword into the north view produced 498px of mangled cape.
- The weapon layer is **static per direction** — it does not swing with the walk cycle. Acceptable for a sheathed weapon; an attack animation would need per-frame art.
- `Idle north-east` still carries slightly more motion than its Move counterpart (measured ratio 1.09). Below the threshold that reads as wrong, but it is the one direction that did not fully settle.

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

## Player prefab structure — owner-directed

**Status:** ✅ Built and verified in play mode. Reason: every script sat on the single root object, so finding one in the Inspector meant scrolling sixteen components.

**Built:**
- [x] `Player.prefab` split into subsystem groups — root (`Rigidbody2D`, `CapsuleCollider2D`, `PlayerStats`, `RunLoadout`, `PlayerController`) plus `Visual` (`CharacterAnimator`, `CharacterVisuals`, `AuraVisuals`), `Combat` (`AttackStateMachine`, `ComboCounter`, `UltimateGauge`, `UltimateBuff`, `HitStop`, `SlashVFX`) and `Aim` (`PlayerAim`, with the `Reticle` under it). Layout and the rules for extending it are recorded in `CLAUDE.md`.
- [x] `Core/RigRefs.cs` — resolves a reference across the whole rig when its Inspector slot is empty, since `GetComponent` now only sees one group. All references remain wired in the prefab; this is the fallback.
- [x] `AttackStateMachine.characterView` — was a hard `GetComponent<CharacterLayerView>()`, which the split would have silently broken. Losing it costs the real clip length and strike frame, i.e. attacks stop animating.
- [x] `RequireComponent(typeof(RunLoadout))` dropped from `AttackStateMachine` and `CharacterVisuals` — it was forcing three unrelated subsystems onto one object. The genuine same-object requirements (`PlayerController`→`Rigidbody2D`, `RunLoadout`→`PlayerStats`) are kept.
- [x] `CameraRig` now finds the player controller with `GetComponentInChildren`, so it keeps working regardless of which group the controller sits in.

**Done by editing the prefab YAML, deliberately.** Unity's copy-component/paste-as-new gives the component a new fileID and nulls every serialized reference to it; changing `m_GameObject` and moving the `m_Component` entry preserves the fileID, so all 30+ wired references survived untouched. Verified by dumping every object-reference field on the reimported prefab: zero nulls.

**Verified in play mode:** `Begin(Basic)` runs Windup→Active→Recovery→Idle, the body sprite animates, the Ultimate Gauge fills +8 and the Combo Counter reaches 1 stack — i.e. the Combat group drives the animator on Visual and reads the stats and loadout on the root. Zero console errors or warnings.

**Found while verifying, later fixed:** a frame longer than `windup + active` (0.18 s on the Katana's Basic) skipped the Active phase entirely — `Update` sampled the phase once per frame, so the transition was missed and `ActivePhaseOpened` never fired. Unreachable at a normal frame rate; reproducible whenever a hitch or an editor tool call stalls the main thread past that window, which is how it was found. **Fixed in the damage-pipeline pass** (see below) by firing on *crossing* the windup rather than on sampling into Active — once a swing can actually hit something, a skipped Active is a lost damage window.

---

## Damage pipeline & training dummy — owner-directed

**Goal:** something to hit, so the feel layer can stop lying. Attacks previously ran their whole state machine with nothing to connect with, so gauge fill, combo stacks, hitstop and camera shake all fired off the Active phase opening — meaning **whiffs stuttered the screen and filled the gauge**. That is now gone.

- [x] `Scripts/Combat/Damageable.cs` — HP for anything hittable, player and enemies alike. `TakeDamage(float)` returns whether the hit actually landed; raises `Damaged(appliedAmount)` and `Died`. Optional invulnerability window on **unscaled** time.
- [x] `Scripts/Combat/AttackHitbox.cs` — on the player's `Combat` group. Opens on `ActivePhaseOpened`, stays live for the action's authored `Active` duration, sweeps with `Physics2D.OverlapCircle` each frame, and raises `Landed(action, target, amount)` / `Missed(action)`.
- [x] `Scripts/Combat/HitFlash.cs` — tints wired `SpriteRenderer`s on `Damaged`, on unscaled time so it survives the hitstop that fires with it.
- [x] `Scripts/Combat/ContactDamage.cs` — damages what it touches. Carries no cooldown of its own; the target's own invulnerability window is the rate limit, so two enemies leaning on the player cannot stack into instant death.
- [x] `Scripts/Enemies/TrainingDummy.cs` + `Prefabs/TrainingDummy.prefab` — **test-only.** 20 HP, deals 8 contact damage, stands back up after 2 s instead of dying. `Damageable` and `ContactDamage` are the reusable halves; the respawn behaviour is not.
- [x] Three dummies placed in `SampleScene` at (11.5, 8.5), (17.5, 8.5), (14.5, 5.5) — open floor, clear of the wall ring and all five 2×2 pillars.
- [x] `Art/Placeholder/Enemies/TrainingDummy.png` — procedurally generated flat-colour block on the character's 32×48 canvas, 32 PPU / Point / uncompressed. Programmer art, deliberately **not** through the `deeper-art` skill.
- [x] Layers **6 = Player**, **7 = Enemy** added to `TagManager.asset` (both slots were empty; nothing in the project used a non-zero layer before). No `Physics2DSettings` change needed — the collision matrix is fully permissive, so both new layers still collide with the Default-layer `Walls` tilemap. `CameraRig` finds the player by **tag**, so it is unaffected.

**The feel layer moved from the swing to contact.** `AttackStateMachine.OpenActivePhase()` now only raises `ActivePhaseOpened`; `HandleLanded`/`HandleMissed` do the rest. `UltimateGauge.fillOnSwingUntilEnemiesExist` and `FillsOnSwing` are **deleted** — that one flag was gating three different things (gauge, hitstop, shake) while combo fill was ungated, which is why turning it off was never a clean switch.

**Only the first target of a swing counts as a landed hit.** An attack that catches three dummies gives one combo stack, one gauge tick and one hitstop freeze. Without that guard a crowd would fill the gauge several times off one press and chain a freeze per body.

**A buff-shaped Ultimate opens no hitbox at all.** The Katana's Ultimate grants a timed buff, so it counts as neither a hit nor a whiff — and without the `UltimateShape == Buff` check it would also have dealt its authored 40 damage on top of the buff.

**Dangling ends now wired.** `ComboCounter.OnMissed()` and `OnDamageTaken()` had zero callers; both are live. `PlayerStats.MaxHP`, `DamageReduction` and `UltimateGaugeGain` were written-but-never-read; all three are now consumed (`MaxHP`/`DamageReduction` by `Damageable` when the rig has a `PlayerStats`, `UltimateGaugeGain` by every gauge gain). `AttackTiming.Damage` was read by nothing; it is the hitbox's damage now, as `Damage × ComboCounter.DamageMultiplier × (1 + PlayerStats.DamageBonus)` — and note `DamageBonus` is a **Flat** modifier carrying a *fraction*, per the comment where `UltimateBuff` registers it.

**`SlashVFX` is DELETED — owner-directed.** The attack frames already draw their own arc, so the separate sprite layer put **two** arcs on every swing. `Scripts/Combat/SlashVFX.cs` and its component on `Player.prefab` are gone. The five `Art/Placeholder/VFX/Slash_*.png` sprites are **kept but now unreferenced** — they cost PixelLab credits and are the obvious starting point if a separate arc layer is ever wanted again, so they were not deleted along with the code.

⚠️ **This re-opens a measured defect — flagged, not silently accepted.** The Game-feel section below records why the arc was pulled out of the character frames in the first place: the baked arcs measured **197 bright pixels facing side but 14 facing up**, because she turns away and a dark arc vanishes against her pale cloak. That measurement predates the latest art pass, so it may be stale — but if it still holds, up-facing and up-diagonal attacks will read weakly. **Re-measure the current `Katana_Attacks` / `Katana_Chain2` sheets before treating this as settled**, and fix it in the art via the `deeper-art` skill rather than by re-adding a second arc layer.

### Depth sorting — owner-directed ("it looks unnatural")

**The rule, as the owner stated it:** whoever is lower on screen draws on top — not always the player — and when two actors are on the same level, the player wins.

Actors used to overlap flatly, and which one won could flip as they moved. Four separate causes, each found by looking at a render rather than by reasoning:

1. **No depth ordering existed at all.** Transparency Sort Mode was `Default` with axis `(0,0,1)` — a Z-distance sort in a game where every sprite sits at z=0. Set to **Custom Axis `(0,1,0)`** in `GraphicsSettings.asset`.
2. **The rig's paper-doll orders were fighting the world.** `Player/Body` is order 0 and an enemy is order 0 — a tie — while her **Head is order 4 and Weapon 5**, so her head beat every enemy no matter where she stood. Fixed with a **`SortingGroup` on each actor root**, so a rig sorts against the world as one unit and its 0–5 orders apply only *inside* the group. The layering contract in CLAUDE.md is untouched.
3. **A `SortingGroup` sorts by its renderers' bounds, not by its transform.** Measured: with her root at y=9.5, a full unit *behind* a dummy at y=8.5, she still drew in front — because her artwork's centre sat lower than the dummy's. This is invisible today only because every rig happens to offset its art by the same +0.75; the first enemy drawn at a different height would sort as if standing somewhere it is not. Fixed with **`Scripts/Core/YDepthSort.cs`**, which drives `SortingGroup.sortingOrder` from the actor's **root** (its feet) in `LateUpdate`. Sorting order beats the sort axis, so the result is exact and inspectable rather than approximate.
4. **No tie-break.** `YDepthSort` quantises Y into `step` bands (0.1 units) and doubles the resulting order so there is a free slot between adjacent levels for a `priority` to occupy. **Player priority 1, enemies 0.** The band is deliberate: two actors standing side by side will never share a Y to the last decimal, but they should still read as level.

**Two bugs this fix itself introduced, both caught by looking:**

- **Actors vanished behind the floor.** Y-driven orders are large negatives (about −170 at y=8.5) and the tilemaps sit on `Default` at order 0/−10, so every actor sank beneath the ground. Fixed with a **`Actors` sorting layer** between `Default` and `Overlay` — actor orders no longer compete with environment orders at all.
- **Actors then rendered as pure black silhouettes.** URP's `Light2D` applies per sorting layer, and the scene's Global Light targeted `Default` only. Its target list now includes all three layers. **Any new sorting layer must be added to the Global Light 2D or everything on it renders unlit.**

**Sorting layer stack (render order, bottom to top):** `Default` (tilemaps) → `Actors` (characters, Y-sorted) → `Overlay` (hit flashes at 25, aim reticle at 50). The reticle keeps a `SortingGroup` with **`sortAtRoot = true`** so it escapes the player's group instead of inheriting its Y-driven order.

**Verified visually in all four cases:** behind → the dummy's crossbar occludes her boots; in front → she occludes its body; same level → she wins; and a landed hit → the flash draws above both.

### Hit VFX — owner-directed, replaces the slash arc as the contact cue

- [x] `Scripts/Combat/HitVFX.cs` — flashes a burst on `AttackHitbox.Landed`, so a whiff shows nothing, matching hitstop, shake and gauge fill. **Not** deduped per swing (unlike the gauge and combo): `Landed` is raised once per target and every target should flash.
- [x] `Art/Placeholder/VFX/HitFlash.png` — 64×64, 32 PPU, Point, uncompressed, centre pivot.

**One base sprite, scaled per action, per ART_DIRECTION §6** — which explicitly allows "a base flash effect tinted per weapon color" rather than an asset per weapon per action. A `tint` field is the per-weapon hook; white leaves the art as authored. Sizes are `0.5 / 0.85 / 1.05` for Basic/Heavy/Ultimate (the sprite is 2 world units at scale 1), and lifetimes `0.10 / 0.16 / 0.22 s` — heavier hits read bigger **and** linger, which is the owner's "more visible if it's heavy".

**Two art generations, one rejected.** The first burst failed the skill's acceptance checklist on palette, measured not eyeballed: ~8% dark red-brown pixels (an emissive flash must contain no shadow pixels), off-palette pink, and part of its chroma sitting in the **15–40° orange-red band that ART_DIRECTION §2 reserves exclusively for hazard telegraphs** — a warm-red impact would have read as *danger* rather than as a landed hit. The accepted burst measures **0 dark pixels, 0 partial-alpha pixels, all chroma at hue 45–60°** (gold), 42% pure-white core.

**Anchored to the drawn body, not the collider.** The first composite put the flash on the dummy's shins: in a top-down game the collider is a foot-level footprint, and on the training dummy its centre lands 11px up a 48px sprite. `HitVFX` uses the `SpriteRenderer` bounds centre and falls back to the collider only when there is no renderer. Verified in play mode against a real scene dummy: flash at y=9.25 (renderer centre) rather than 8.85 (collider centre).

**The pool is deliberately outside the player rig.** Parented to the Combat group, flashes would ride along as she lunges away and drag the impact off the enemy it belongs to. `HitVFX` builds its own `HitVFX Pool` container at the scene root and destroys it in `OnDestroy`.

**Two other leftovers cleared in the same pass.** `Weapon_Katana.asset` carried `ultimateBuff.Duration: 90` against the authored default of 8 in both `WeaponDefinition` and `UltimateBuff.fallback` — a debug value that made the buff outlast most of a floor; set back to 8. And the whole of the previous session's work (`Scripts/Camera`, `Character/AuraVisuals`, `Core`, `Combat`, `PlayerAim`, `UltimateGaugeHUD`, the VFX placeholder art, the aura material/shader, `10-NARRATIVE.md`, `00-DESIGN_CHANGE_BRIEF.md`) was **untracked in git** — 69 files now staged, still uncommitted.

### Two divergences from locked design — recorded, not silently applied

| Locked doc | Says | Built | Note |
|---|---|---|---|
| `Design/04-BALANCE.md` §4 | Katana +8% Basic / +15% Heavy; Bow +6%/+15%; Greatsword +10%/+20% per landed hit | **1% flat, every weapon, every action** | Owner-directed. 100 landed hits to fill. Values stay serialized on `UltimateGauge.fillRates`, retunable without a recompile (Design Rule 8). |
| `Design/02-CORE_SYSTEMS.md` §4 + `04-BALANCE.md` §10 | Gain-on-taking-damage is the **"Gauge: Vengeance"** upgrade (+5%), not base behaviour | **+1% on taking damage at base** (`gainOnDamageTaken`) | Owner-directed. A flat percentage, deliberately not scaled by how hard the hit was. |

Both need a Rule 14 reopen if they are to become design; until then BALANCE §4 remains the locked table and this section is the record of the deviation. **Also in `00-DESIGN_CHANGE_BRIEF.md` §7g**, with the two consequences that need deciding rather than just recording: a flat rate deletes the gauge as a weapon-differentiating knob, and gain-on-damage at base leaves the "Gauge: Vengeance" upgrade with no job.

**A third divergence, and the only one that is a live gameplay hole:** the Ultimate still calls `ComboCounter.Consume()` and **discards the result**. CORE_SYSTEMS §4 and BALANCE §4 convert those stacks into bonus damage, and a buff Ultimate has no damage to convert them into — so casting at 10 stacks silently throws away −20% damage at the exact moment a +50% damage buff starts, making the optimal play to cast at *zero* stacks. That inverts what a "Combo Finisher" is for. Brief §7h lays out the three ways out; **this wants a decision before any further Ultimate work.**

### Still missing from Milestone 1

- **Player death / run-end.** `Damageable.Died` fires on the player with no subscriber: she stops taking damage and sits at 0 HP. Deliberate and documented, not an oversight — there is no run-end, no death screen and no respawn yet. `Damageable` has a `Refill` context-menu item for testing.
- **No HP bar and no damage numbers.** The 1% gauge tick and the hit flash are currently the only feedback that a hit was taken. CORE_SYSTEMS §6 lists damage numbers as optional.
- **Dig-Dash** and therefore the Dash-Attack Cancel (`CanCancel` is still exposed and unused).
- ~~**Real enemies.**~~ **Resolved** — the whole Biome 1 basic roster now exists with AI, telegraphed attacks and death. See *Biome 1 basic enemies* below. The training dummy stays, unchanged, as the thing that never dies.
- **Per-weapon hitbox reach.** Radius and offset are serialized on `AttackHitbox` (0.8 / 1.0 / 1.3, offsets 0.55 forward and 1.05 vertical), not on `WeaponDefinition`. They must move onto the weapon data in Milestone 2, when the Bow and Greatsword need different shapes. The offsets duplicate `SlashVFX`'s on purpose, with tooltips saying so — if the two drift apart, hits land where no arc is visible.

### Two bugs the play-mode probe caught — both fixed

**`ContactDamage` ignored its own enabled checkbox.** Switching the component off did not stop the collision callbacks: a disabled `ContactDamage` still dealt full damage on contact, which is how a "disabled" dummy silently chipped the player mid-test and reset the combo. Unity only honours the `enabled` flag for components that have one of the standard enableable callbacks (`Start`/`Update`/…), and this class has none — it is all `OnCollision*`. Fixed with an explicit `isActiveAndEnabled` guard, so the checkbox now means what any reader assumes. Confirmed: 1197 frames in contact, 0 damage.

**Contact damage stopped whenever the player stood still.** `Player.prefab`'s `Rigidbody2D` was on Sleeping Mode *Start Awake*, and a motionless body sleeps in about a second — at which point Unity stops delivering `OnCollisionStay2D` and contact damage silently ceases. Measured: **0 damage over 1372 frames while `Collider2D.IsTouching` was still true and the overlap distance was −0.005**, then 8 damage the instant the body was woken by hand. It presents as contact damage working while you walk and randomly stopping when you stand — an engine glitch, not a sleep rule. Fixed by setting Sleeping Mode to **Never Sleep** on the player; the reasoning is recorded in `ContactDamage`'s class comment, since the prefab field cannot carry one. Re-measured after the fix: she takes exactly one 8-damage tick per 0.6 s invulnerability window while standing against an enemy.

### Verified

Compiled all 27 gameplay scripts against the real Unity 6000.0.58f1 assemblies (`dotnet build` on a generated project mirroring `Assembly-CSharp.csproj`'s references): **0 errors, 0 warnings.** Static pass over the hand-authored YAML: no dangling local `fileID`s in either prefab, every `m_Script` GUID resolves to an existing `.cs`, no component/GameObject ownership mismatches, all three dummy instances registered in `SceneRoots`.

**In the editor** — forced reimport of the hand-written `.meta` files: console clean, both prefabs load with **0 missing scripts**, `TrainingDummy.png` imports as a 32×48 Single sprite at 32 PPU / Point / centre pivot, layers read back as `6=Player` / `7=Enemy`, and all three dummies sit on layer 7 at their authored positions. Inspector wiring confirmed by `SerializedObject`: `hitbox` bound, `fillRates` all 1/1, the old `fillOnSwingUntilEnemiesExist` field gone, `owner`/`stats` resolved on gauge, combo and hitbox.

**In play mode** (`Application.runInBackground = true` first — `frameCount` was stuck at 2 until it was set, exactly as `01-VERIFICATION.md` §1 warns; every probe object destroyed afterwards, 0 stray roots, `timeScale` back to 1):

| Assertion | Result |
|---|---|
| Basic connects | target 20 → **12** (8 damage) |
| Gauge on a connecting swing | 0 → **1** |
| Combo on a connecting swing | 0 → **1** |
| **Whiff** — gauge | **unchanged at 1** |
| **Whiff** — combo | **reset to 0**, `timeScale` stays 1 (no hitstop) |
| One swing, two targets | both 20 → **12**, gauge **+1 only**, combo **+1 only** |
| Hit taken | HP −8, gauge **+1**, combo **reset to 0** |
| i-frames | second `TakeDamage` in the same frame returns **false**, HP and gauge unchanged |
| Buff Ultimate | target **untouched at 20**, gauge drained to 0, buff active (DamageBonus 0.5 / AttackSpeed 1.4 / MoveSpeed 5.75) |
| Death | at 0 HP `TakeDamage` returns false and the gauge stops moving |

One false alarm worth not re-deriving: `UltimateBuff.IsActive` reads `False` if checked in a *later* MCP call than the one that fired the Ultimate — the buff is 8 s and a round trip is longer than that. Check it in the same call.

**Still not verified — needs your eyes** (`01-VERIFICATION.md` §4: every visual defect so far passed every assertion, and §3: screenshots are unusable in this window layout). Nobody has *looked* at a hit yet — the slash arc lining up with where damage actually lands, the dummy's flash, the respawn, the HUD ticking.

---

## Biome 1 basic enemies — owner-directed

**Status:** ✅ Built and verified in play mode. Four enemies that chase, telegraph, attack and die.

This is Milestone 3's Upper Caves roster pulled forward at the owner's direction, minus the
Collapsed King mini-boss. It resolves Milestone 1's "real enemies" gap outright.

| Enemy | HP | Dmg | Speed | Damage delivery | Prefab |
|---|---|---|---|---|---|
| Cave Crawler | 20 | 8 | 3.5 | `ContactDamage`, plus a lunge that closes the gap | `Prefabs/Enemies/CaveCrawler.prefab` |
| Rock Slinger | 15 | 6 | 2.5 | `ThrownRock` projectile only | `Prefabs/Enemies/RockSlinger.prefab` |
| Tunnel Brute | 60 | 15 | 2.0 | `OverheadSlam` radial hit + knockback | `Prefabs/Enemies/TunnelBrute.prefab` |
| Elite: Deep Warden | 100 | 18 | 2.0 | Same as the Brute | `Prefabs/Enemies/DeepWarden.prefab` — **variant of TunnelBrute** |

HP / damage / speed are BALANCE §5, locked. Everything else on this page is invented — see below.

### Files

`Scripts/Enemies/`: `EnemyDefinition.cs` (ScriptableObject), `Enemy.cs`, `EnemyTarget.cs`,
`EnemyChase.cs`, `TelegraphedAttack.cs`, `LungeAttack.cs`, `RockThrow.cs`, `ThrownRock.cs`,
`OverheadSlam.cs`, `EnemyDeath.cs`.
`Scripts/Animation/SpriteAnimationView.cs`. `Scripts/Core/ActorPool.cs` and `PooledActor.cs`.
`Scripts/Editor/PlaceholderEnemySheets.cs` and `EnemyAnimationSets.cs`. `Data/Enemies/Enemy_*.asset` ×4, `Data/Animation/Anim_Enemy_*.asset` ×3,
`Art/Placeholder/Enemies/{CaveCrawler,RockSlinger,TunnelBrute,ThrownRock}.png`.

**No `CaveCrawler.cs` / `RockSlinger.cs` / `TunnelBrute.cs` / `DeepWarden.cs`**, which Milestone 3's
file list names. Each would have been an empty shell: an enemy *is* its definition asset plus which
attack component its prefab carries. The differences that matter are data (`retreatDistance` 3.5 is
the entire Rock Slinger kiting behaviour) or one small component. Correct that file list rather than
adding classes to match it.

### Stats live in `EnemyDefinition`, not on the prefabs

Owner-directed, and the case CLAUDE.md names for data assets over hardcoded values. `Damageable` and
`ContactDamage` are shared with the player, so they cannot read an enemy asset without inverting the
dependency; `Enemy.Awake` **pushes** into them through two new setters, and everything written for
enemies **pulls** from `Enemy.Definition` directly and needs no setter.

**`Damageable.SetMaxHealth` refuses when a `PlayerStats` is present** (warns, changes nothing). That
one guard is what makes it impossible for enemy data to reach the player's HP, which is the output of
the stat pipeline. Verified: `SetMaxHealth(7)` on the player left her at 100.

Consequence to know: `Damageable.maxHealth` on an enemy prefab is now vestigial — the tooltip says so.
The proof it is really the asset driving is that `TunnelBrute.prefab` still serializes 20 while the
spawned Brute reads 60.

**The Deep Warden is the acceptance test for this split, and it passed trivially**: a prefab variant
with exactly two overrides — the definition, and a `SpriteRenderer.color` tint. Tint is authored on the
prefab rather than carried in data because `HitFlash.Awake` captures each renderer's colour; a tint
written at runtime by another component would be captured or clobbered depending on undefined
component order, and the Warden would flash back to untinted on its first hit.

### The enemy sheet contract — NEW, and not in any design doc

`ART_DIRECTION` §4 gives enemies a frame budget but defines **no row order and no direction count**
for them (§3's 8-way-mirrored rule sits under *Player* Animation Budget). This pass had to invent one.
**Recorded here, deliberately not written into the locked design doc** — Design Rules 11/12. A Rule 14
pass should fold it into ART_DIRECTION properly; it is on the designer's list as
`00-DESIGN_CHANGE_BRIEF.md` §7i, along with the two other things this pass invented that BALANCE and
CONTENT_DESIGN never specified — how each enemy delivers its damage (§7j) and roughly thirty
behaviour numbers with no design source (§7k).

Sheet is **128 × 576** — 4 columns × 12 rows of 32×48 cells, 32 PPU, Point, uncompressed, pivot Center.
Sub-sprites are `<Enemy>_<row>_<col>`, matching the existing player convention. Row 0 is the top row.

| Rows | State | Columns | Bound to |
|---|---|---|---|
| 0–2 | Idle/Move — Down, Up, Side | 0–3 | `Idle` **and** `Move` |
| 3–5 | Telegraph — Down, Up, Side | 0–2 | `Telegraph` (new) |
| 6–8 | Attack — Down, Up, Side | 0–2 | `BasicAttack` |
| 9–11 | Death — Down, Up, Side | 0–2 | `Death` (new) |

Only cells carrying a frame are sliced — **39 sub-sprites per enemy, not 48**. Slicing the unused
column 3 of the 3-frame rows would produce sprites that exist only to be skipped, and binding one by
mistake shows a one-frame disappearance mid-attack.

**Enemies author three directions, not the player's five.** Owner-directed: basics are 4-directional
(Down/Up/Side mirrored to all eight facings), and the full 5-row 8-directional set is reserved for
bosses. The `DownDiagonal` and `UpDiagonal` arrays in each clip point at the *same sprites* as `Side`,
so `Facing.ToArt()` and `IsMirrored()` cover all eight facings with **no code change**. The reasoning:
2-directional art cannot show whether an enemy is facing you, and in a game where every attack is
telegraphed that is the information the fight is built on; the diagonals are the rows that earn the
least per cell, so they are the right thing to cut. Collapsing 3 rows to 1 later is re-pointing arrays;
going the other way needs art that does not exist.

**`Idle` binds one frame, `Move` binds four**, from the same rows. §4 budgets Idle/Move as a single
4-frame line so they share art — but a standing enemy playing the whole cycle walks on the spot.
`Resolve` wraps `frame % length`, so a one-entry array is a still pose for free.

### `CharacterState` gained `Telegraph = 7` and `Death = 8`

Appended, never inserted — the enum serialises by integer and every `Anim_*` clip keys off it.
The enum's comment said Death was *deliberately* absent; that is now corrected in place. Both are
**enemy-only**: the player has no art for either, and `Resolve` returning null for her is correct.
**Player death / run-end is still unbuilt** and this pass makes it louder — four enemies that really
kill her now exist, and she still sits at 0 HP with no subscriber on `Damageable.Died`.

### Damage delivery per enemy — a design interpretation, flag it

BALANCE §5 gives one Damage number per enemy and never says how it is delivered. Chosen so the three
differ in *function*, not only in numbers (Design Rule 4):

- **Cave Crawler** carries `ContactDamage` — touching it hurts. It is the pressure enemy.
- **Rock Slinger** carries none. All 6 is the rock, so it is safe to body and punishes standing still
  at range instead of proximity.
- **Tunnel Brute / Deep Warden** carry none. All 15/18 is the slam, so there is a safe window beside
  them between slams — the whiff-punish space `LEVEL_DESIGN` §2 asks Combat Rooms to preserve for the
  Greatsword. Giving the Brute both contact *and* slam would double-dip and delete it.

### Invented numbers — no design doc specifies any of these

All are serialized with `[Tooltip]`s saying so, retunable without a recompile (Design Rule 8).

| | Crawler | Slinger | Brute | Warden |
|---|---|---|---|---|
| Windup (telegraph) | 0.35 | 0.50 | 0.75 | 0.70 |
| Active | 0.18 | 0.06 | 0.12 | 0.12 |
| Recovery | 0.45 | 0.60 | 0.90 | 0.85 |
| Cooldown | 1.20 | 2.20 | 2.50 | 2.20 |
| Aggro radius | 10 | 12 | 12 | 14 |
| Attack range | 1.6 | 7.0 | 2.0 | 2.2 |
| Stop distance | 0.9 | 5.5 | 1.2 | 1.2 |
| Retreat distance | 0 | 3.5 | 0 | 0 |

Per-move geometry stays on the components, not the definition, so a Rock Slinger is never shown a
`slamRadius` field it does not use: `LungeAttack.lungeDistance` 1.8, `OverheadSlam.radius` 2.2 /
`knockbackSpeed` 12 / `knockbackTime` 0.30, `RockThrow.projectileSpeed` 4.5,
`ThrownRock.lifetime` 4.0. (This mirrors the outstanding Milestone 2 item to move `AttackHitbox`'s
per-weapon reach onto `WeaponDefinition` — move both together when that happens.)

Two anchors so the durations are not arbitrary: **ART_DIRECTION §4 caps Telegraph at 3 frames**, which
at the animator's 8 fps is 0.375 s — that is where the Crawler's 0.35 came from. The 3-frame cap is an
*art* budget, not a duration cap: the Brute's 0.75 s telegraph is still 3 frames, played slower by
`PlayAction`. And the player moves at 5.0, so the rock at 4.5 is outrunnable by design.

### Knockback required a change to `PlayerController`

`FixedUpdate` writes `linearVelocity` outright every tick, so an `AddForce` from the slam is erased on
the same physics step — the knockback would simply not happen. Added `ApplyKnockback(velocity,
duration)` as a decaying branch checked **before** the attack-lunge branch: being slammed should
interrupt a swing, not lose a tug-of-war with it.

### Three bugs the play-mode probe caught — all fixed

**One enemy silently rewrote another's contact damage.** `Enemy.Awake` resolved its optional
`contact` field through `RigRefs.Find`, which searches from `transform.root` — and for anything
`TestSpawner` creates, root is the **spawner**, not the enemy. That field is legitimately empty on
three of the four enemies, so their Awake reached across and found the *Cave Crawler's*
`ContactDamage`, then wrote their own damage into it. Measured: Crawler alone dealt BALANCE §5's 8;
spawning a Slinger made it **6**; a Brute made it **15**. Fixed by resolving `ContactDamage` (and
`Damageable`) with a same-object `GetComponent` — CLAUDE.md's prefab rule already puts both on the
actor root beside the collider, so the rig-wide search bought nothing and could only find the wrong
rig. **This is the general hazard below, live.**

**The thrown rock could never hit anything.** `RockThrow` spawned it 0.85 units "up" to match the
Slinger's raised arm — but in a top-down game Y is a *ground* axis, not height. Measured: the rock
travelled 0.85 north of the player for its whole flight and passed cleanly over her collider every
time, dealing 0 damage. Fixed by launching from the ground line and lifting the **art** on the
projectile's own `Visual` child instead — the same split `Player.prefab` uses for its +0.75 Visual.
Re-measured: 6 damage per hit.

**`LungeAttack` could not stop what it started.** It set a velocity that only `EnemyChase` ever
cleared. Measured with chase switched off: a lunging Crawler kept the velocity and was still
travelling **80+ units** away, sampled twice to confirm it never stopped. This was **reachable in
ordinary play, not only in a probe** — `EnemyDeath.HandleDied` disables `EnemyChase`, so a Crawler
killed inside its 0.18 s lunge window lost the only thing clearing its velocity and its corpse slid
for the whole despawn delay. Fixed by giving the lunge its own timer; `EnemyDeath` deliberately does
**not** disable `LungeAttack`, so that timer still runs on a corpse. A component that is only correct
by the grace of its neighbour is a bug waiting for the next configuration.

### The general hazard: `RigRefs` resolves against the spawner, not the actor

`RigRefs.Find` searches from `transform.root`. Anything spawned under a parent — every pooled enemy —
has the **spawner** as its root, so an unwired field can resolve against a sibling actor. The
`ContactDamage` bug above is exactly this, and it was live because the field is *meant* to be empty
on most enemies.

The rule that follows: **`RigRefs` is only safe for a field that is always populated on the rig.**
For anything optional, or anything the prefab layout already pins to a known object, use
`GetComponent`/`GetComponentInParent` instead. Every remaining reference on the enemy prefabs is
wired in the Inspector, so no other fallback runs.

### Object pooling — owner-directed

**Status:** ✅ Built and verified. `Scripts/Core/ActorPool.cs` + `Scripts/Core/PooledActor.cs`.

Enemies and thrown rocks are reused instead of instantiated and destroyed. A Wave Room spawns
enemies in batches for a whole floor and a Slinger throws every couple of seconds; doing that with
`Instantiate`/`Destroy` allocates a rig per spawn and hands the GC a steady drip of dead
GameObjects — and in a game whose feel layer is measured in hitstop frames, a GC spike lands exactly
where it is most visible.

- **`ActorPool`** — a plain C# class over `UnityEngine.Pool.ObjectPool<GameObject>` (the obvious
  Unity shape, per CLAUDE.md). **One pool per prefab, owned by whatever spawns it** — the shape
  `TestSpawner` already had. There is deliberately **no global registry**: a pool nobody owns is a
  pool nobody clears.
- **`PooledActor`** — lets an instance put itself away without knowing which pool it came from, or
  whether it came from one at all. **Added by the pool at runtime, not authored on prefabs**: the
  pool is the only thing that can bind it, and a prefab carrying an unbound one would look like it
  pools when it does not. Everything falls back to `Destroy` when unpooled, so pooling is an
  optimisation you cannot forget and leak from.

**Death now returns to the pool.** `EnemyDeath` plays the Death clip, switches off the collider,
chase and attack, then releases after `despawnDelay` (unscaled — a scaled corpse timer would linger
for real seconds whenever a kill triggered hitstop).

**The trap pooling always sets, handled explicitly:** a recycled instance never runs `Awake` again.
Release deactivates and Get reactivates, which makes `OnEnable` the correct reset hook, and each
component resets its **own** state there:

| Component | Reset on enable | What it prevented |
|---|---|---|
| `Enemy` | `Damageable.Refill()` | Reuse came back holding the 0 HP that killed it — dead on arrival |
| `EnemyDeath` | re-enable collider / chase / attack | Second use was an inert prop that could not be hit, move or attack |
| `TelegraphedAttack` | clear `_readyAt` | An absolute `Time.time` cooldown survived death, so a respawn could not attack for up to 2.5 s |
| `LungeAttack` | clear the lunge timer | A respawn had its velocity zeroed mid-stride by the previous life's timer |
| `CharacterAnimator` | `CancelAction()` | An enemy released mid-Death clip came back still holding the Death pose |
| `ThrownRock` | clear velocity and damage | A recycled rock travelled for a frame before `Launch` was called |

**`TestSpawner.Alive` counts active instances, not non-null ones.** Pooled instances are never null —
they are deactivated children — so the old null-pruning count reported a room full of enemies nobody
could see. `Start`'s adoption of hand-placed actors also skips inactive children for the same reason.

Measured: **100 spawns served by 7 rigs**; `maxAlive` still enforced (40 presses against a cap of 20
gave 20 alive); `Clear` returns everything and keeps the rigs; a Slinger firing repeatedly used
**2 rock instances total**.

### Verified

Console clean, 0 errors / 0 warnings. Play mode with `Application.runInBackground = true`, driven
through public methods and `[ContextMenu]` (simulated keys never arrive — `01-VERIFICATION.md` §2).

| Assertion | Result |
|---|---|
| Animation sets resolve | 960 checks across 3 enemies × 5 states × 8 facings × 8 frames — **0 nulls** |
| Diagonals alias Side | **True** on all three (the 4-direction contract) |
| Definition drives HP | spawned 20 / 15 / 60 / 100 while `TunnelBrute.prefab` still serializes **20** |
| Player HP path protected | `SetMaxHealth(7)` on the player **refused**, still 100 |
| Chase closes | melee converged to ~1.2 from a 5-unit ring |
| Slinger holds its line | converged to **5.39** against `stopDistance` 5.5 — it kites, it does not close |
| Aim locks at windup | `AimDirection` exactly (−1, 0) with the target due west |
| Brute slam | player 100 → **85** (exactly 15) |
| Knockback | player pushed **1.68 units** directly away from the slam |
| Rock | **6** damage per hit; travels on the ground line, art lifted to +0.5 |
| Death | `Death` clip plays, collider off, chase off, `IsAlive` false, leaves the scene |
| Player's swing still lands | Crawler 20 → **12**, combo 0 → **1**, gauge **+1%**, `timeScale` back to 1 |
| Depth sorting | Brute below → order −148 (front), player −159, level enemies −160, Warden above −172 (behind) |
| Contact damage isolation | Crawler holds **8** with a Slinger, Brute and Warden all spawned (was 6 / 15 before the fix) |
| Pooled reuse | recycled Crawler comes back **20/20 HP**, collider on, chase on, attack on, pose `Idle` not `Death` |
| Pool bounded | 100 spawns → **7 rigs**; 40 presses against `maxAlive` 20 → 20 alive; rocks capped at **2 instances** |

**Looked at, not just asserted** (§4 — every visual defect so far passed every assertion). Two
named-camera screenshots: all four enemies beside the player read as three distinct silhouettes at
true game zoom, nothing renders black (the `Actors` layer is in the Global Light 2D target list),
feet sit on the floor, and the Warden is visibly the Brute in violet. A second, closer shot with
enemies deliberately overlapping her confirms the sort order is what the numbers claimed.

### Outstanding

- **The Deep Warden has no aura layer.** ART_DIRECTION §4 specifies "palette-swap + 1 aura VFX layer";
  the tint ships, the aura does not. `AuraVisuals` resolves `UltimateBuff` and `AttackStateMachine`,
  so it is player-coupled and cannot be pointed at an enemy without being rewritten.
- ~~**No key drop.**~~ **Built** — `KeyReward` is on `DeepWarden.prefab` and grants 1 `SecretKey` on
  death. See *Secret Vault* below. The gate is still untested as a player experience, because no room
  places a Warden and there is no floor sequencing to put one on the way.
- **Enemy-vs-enemy collision is unfiltered.** Layer 7 collides with itself, so a crowd will jostle.
  If it reads badly the fix is the collision matrix, not code.
- **Feel is unjudged.** Whether a 0.35 s Crawler telegraph is actually dodgeable at speed needs a human
  at a focused Game view — synthetic input never reaches play mode.

---

## TestScene — owner-directed development sandbox

**Status:** ✅ Built and verified in play mode (this pass). Full record in `02-TEST_SCENE.md`.

**Owner-directed working order for the whole project:** every system gets built and tuned in
`TestScene` first — enemies, hazards, rooms, upgrades, curses, the remaining weapons — and only then
gets assembled into the real game scenes. This plan's milestones still say *what* gets built; this
says *where* it gets built first.

**Built:**
- [x] `SampleScene` renamed to `TestScene` through `AssetDatabase.RenameAsset` — GUID preserved, Build Settings path updated automatically. Still the only scene in Build Settings.
- [x] `Scripts/Testing/` — `TestSpawner` (one object per prefab, own spawn/clear keys, ring placement, alive cap), `TestControls` (fill Ultimate / heal / reset player), `TestOverlay` (key legend + live `HP / ULT / COMBO / Dummies` readout). All test-only.
- [x] `TestHarness` group in the scene holding all three, so everything test-only is deletable in one click.
- [x] **The room starts empty — owner-directed.** The three hand-placed training dummies were removed; `F4` spawns them on demand and `spawnOnStart` is the knob for a room that comes up populated. Anything hand-placed under the spawner is still adopted at `Start`, so the readout and Clear stay honest.
- [x] Keys are function keys read straight off `Keyboard.current`, deliberately **not** added to `InputSystem_Actions` — that asset is shipped content and is what the rebinding UI will read.

**One decision worth not re-litigating:** a scene per mechanic was considered and rejected — rooms
are prefabs by design (so "test every room type" is a prefab drop, not a scene), roguelike bugs live
in system *interactions* that an isolated scene hides, and duplicated scenes duplicate the camera,
lighting, HUD and player wiring, which then drift. The discipline that makes one sandbox work:
**everything testable lives in a prefab or a component, never authored only in the scene.** Reasoning
in full in `02-TEST_SCENE.md` §1.

**Deliberately not built:** a slow-motion key — `HitStop` restores `Time.timeScale` to a fixed normal
after every landed hit, so it would snap back on the next connect and read as broken.

---

## In-run HUD — owner-directed

**Status:** ✅ Built, wired and verified in play mode. It was written blind (no Unity connection) and
integrated in a later pass; that pass found two defects in it, both below. Divergences in
`Docs/00-DESIGN_CHANGE_BRIEF.md` §15 and §16.

ART_DIRECTION §5's HUD, with real generated art: HP top-left, XP + level badge top-right, Ultimate
Gauge and weapon icon bottom-centre, plus the wave indicator and a depth readout.

**Built:**
- [x] `Scripts/UI/StatBar.cs` — reusable framed bar (socket, fill, frame, label). Pure view.
- [x] `Scripts/UI/HealthBarHUD.cs`, `ExperienceBarHUD.cs`, `DashHUD.cs`, `WeaponIconHUD.cs`,
      `WaveIndicatorHUD.cs`, `DepthIndicatorHUD.cs` — one element, one class.
- [x] `Scripts/Player/PlayerXP.cs` + `Scripts/Enemies/XPReward.cs` — a minimal XP source, so the bar
      is real rather than decorative. The level-up **offer** is Milestone 4 and is not built.
- [x] `Scripts/Editor/BuildRunHUD.cs` — builds and wires the entire canvas from one menu item.
- [x] `Art/UI/` — six pieces sliced from one generated kit: `HUD_BarLarge`, `HUD_BarMedium`,
      `HUD_BarThin`, `HUD_IconFrame`, `HUD_DashPip`, `HUD_LevelBadge`.

### The layout is a committed tool, not dragged rects

Same reasoning as `RoomLayout`: a HUD assembled by dragging is a layout nobody can reproduce, review
or diff, and every retune means finding the same six anchors again. `BuildRunHUD` holds the reference
resolution, margins, insets and anchoring, is idempotent, and rebuilds the whole thing on one click.

### Three ordering bugs caught before they shipped

All three are UGUI sibling-order traps, and none would have thrown:

1. **`Text` and `Image` cannot share a GameObject** — both derive from `Graphic` and they fight over
   the same canvas renderer, so one silently does not draw. Every label is its own child.
2. **The wave indicator hid the object its own `Update` runs on.** A component that polls to decide
   whether to show itself cannot live on the thing it hides — it would never come back. Split into an
   always-active host and a toggled panel.
3. **`StatBar` and `UltimateGaugeHUD` would both have written the same `fillAmount`.** The Ultimate
   keeps its own driver (it owns ART_DIRECTION §6's must-have full-pulse) and gets the bar *visuals*
   without a `StatBar`.

### And one that only a picture caught

The generated frames ship with a **filled** interior: a fill under them is invisible, a fill over them
hides the rivets and leather banding. Rendering the layout to a PNG from `BuildRunHUD`'s own constants
is what showed it — Unity was down, so that mock was the only way to look at it at all. Fixed by
flood-filling each bar frame's interior to transparency so the fill reads *through* the frame.
**Any future bar frame must be hollowed the same way.**

### Integration, done

1. [x] Compiled clean — all ten files, first pass, nothing to fix.
2. [x] `Art/UI/*.png` imported at 32 PPU, Point, Uncompressed, `alphaIsTransparency`. No 9-slice
       borders: every frame is drawn at its native sprite size, so they are never stretched.
3. [x] `Deeper/Build Run HUD` run; the old hand-built `HUDCanvas/UltimateGauge` **and** the
       `UltimateGaugeHUD` that drove it from the canvas root are both deleted.
4. [x] `PlayerXP` on the Player root; `XPReward` on all four enemy prefabs, with per-enemy values.
5. [x] Looked at in play mode, over the real room.

### Two defects the integration found, both fixed

**1. Every bar drew at full width forever.** `Image` ignores `type` and `fillAmount` entirely when it
has no sprite and falls back to a plain quad, and `BuildBarVisuals` created each fill with a null
sprite. Health read 62% and 20% as the *same 182px bar* — only the colour changed, which is why it
looked plausible. Fixed with `Art/UI/HUD_Fill.png`, a flat 4×4 white sprite that multiplies by the
fill colour set in code. **Any future `Type.Filled` image needs a sprite.**

**2. The authored fill insets did not match where the frame art's hole is.** They were estimated at
20 / 16 / 12; measured, the holes are 27/23/33/30, 53/17/52/17 and — the Ultimate frame, whose chunky
end caps the eye reads as part of the bar — **119**/26/118/23. A fill sized to the estimate spills
under the frame, so the visible portion stops being proportional to the value. `BuildRunHUD` now
**measures the knocked-out interior from the PNG's alpha at build time** (`MeasureHole`), so
re-generating a frame can never desync the layout from it again. This also retires the three inset
constants.

Verified by measuring the rendered pixels: fill widths came out at 0.607 / 0.702 / 0.755 of their
sockets against `fillAmount`s of 0.610 / 0.700 / 0.760 — within a pixel on all three.

### HUD restyle and the upgrade strip — owner-directed, second pass

The owner's brief: swap the dash and weapon positions, simplify the weapon slot and give it a real
weapon icon rather than the character-holding-a-weapon frame, add a dash icon, make the health bar
smaller and simpler, and add a transparent strip of the run's upgrades down the left.

- [x] **Dash and weapon swapped.** Dash left of the gauge, weapon right (`anchoredPosition` ±204).
- [x] **`Scripts/Editor/HUDFrameArt.cs`** — draws the frame chrome (`Deeper/Generate HUD Frames`):
      `HUD_BarSlim` 320×36, `HUD_BarSlimUltimate` 300×30, `HUD_BarSlimXP` 240×22, `HUD_SlotSquare`,
      `HUD_SlotRound`, `HUD_SlotHex`, `HUD_SlotUpgrade`, `HUD_Disc`, `HUD_IconDash`.
- [x] **Real weapon icons** — `HUD_IconKatana/Bow/Greatsword`, generated, and `WeaponDefinition.icon`
      repointed at them.
- [x] **`Scripts/Upgrades/`** — `UpgradeDefinition` + `RunUpgrades`, and `Scripts/UI/UpgradeListHUD.cs`.
- [x] **`Data/Upgrades/`** — seven Common entries at BALANCE §9's exact values.

**Why the chrome is drawn and the icons are generated.** These frames are geometry: what matters is
an exact border width, a chamfer on whole pixels, and an interior that is *fully transparent*, because
`BuildRunHUD.MeasureHole` reads that hole to place the fill. Asked for empty slim frames, the UI
generator returned a heart, a money bag and a lightning bolt in a palette the game does not use — 40
generations, rejected. A katana has material and form to interpret and is the opposite case; those
came back right. **The split to keep: generate things with material, draw things with geometry.** The
dash glyph moved to the drawn side for the same reason, after two generated attempts (one with a
smear across it, one where the three chevrons merged into a blob).

**Weapon icons were the character's weapon *layer*, not an icon.** `WeaponDefinition.icon` and
`bodyLayer` both pointed at the same sprite — a frame of the paper-doll sheet, which draws the weapon
as held, at the arm position. At icon size that reads as a tiny figure. They are separate concerns and
are now separate assets.

**The restyle went wider than the brief, deliberately.** The Ultimate bar, the XP bar and the level
badge were not named, but they are the same heavy generated family the health bar was pulled out of,
and a HUD carrying two frame styles at once reads as unfinished rather than as either. They now use
the slim frames. Reverting is three `Load(...)` lines in `BuildRunHUD`. The superseded pieces —
`HUD_BarLarge`, `HUD_BarMedium`, `HUD_BarThin`, `HUD_IconFrame`, `HUD_DashPip`, `HUD_LevelBadge` — are
still in `Art/UI/`, referenced by nothing, kept rather than deleted because they are paid generations.

**Two contrast defects only a picture caught**, both fixed: the upgrade slots were hollow outlines that
vanished against the world and needed a translucent socket behind them; and the dash glyph sat on top
of its own charge disc at values close enough to disappear into it when the dash was full.

**The strip is real, not a mock.** `RunUpgrades.Add` applies each upgrade's modifiers through
`PlayerStats.SetSource`, verified in play mode: MaxHP 100 → 115, MoveSpeed 5 → 5.5, DashCooldown
1.2 → 0.96, DashDistance 3 → 3.75, DamageBonus 0 → 3, and a duplicate pick refused.

**A finding for Milestone 4:** of BALANCE §9's shared pool, only the seven Commons authored here are
expressible as `StatModifier`s. **Every Rare and Epic in that pool is behavioural** — Thorns,
Executioner, Explosive Finish, Blink Strike, Last Stand — and needs hooks in the damage pipeline, which
is exactly what CORE_SYSTEMS §5's "add `source` to the damage events" decision exists for. That work
gates the upgrade pool, not the draw logic.

### The HUD now wires its own sources

`BuildRunHUD` points each element at the open scene's player (`PlayerPart<T>()`) instead of leaving
every class to find one at runtime. The runtime fallbacks stay and still work; the wiring is what makes
the connections visible in the Inspector, which is the house rule. It lives in the tool because a
rebuild would silently discard hand-dragged references.

---

## Dig-Dash, spawn telegraphs and first environment art — owner-directed

**Status:** ✅ Built, wired and verified in play mode. It was written blind (no Unity connection) and
integrated in a later pass; that pass found two defects in it, both below. Divergences in
`Docs/00-DESIGN_CHANGE_BRIEF.md` §14 and §16.

**Built:**
- [x] `Scripts/Player/DigDash.cs` — BALANCE §1's 3.0 units / 1.2 s / 0.25 s i-frames, all read from
      `PlayerStats` so the five dash upgrades and the Hub stat land without touching it. Carries the
      Dash-Attack Cancel.
- [x] `Scripts/Player/DashTrail.cs` — ART_DIRECTION §6's "Dig-Dash trail", pooled afterimages.
- [x] `Scripts/Rooms/SpawnTelegraph.cs` — a pooled ground decal, played before an enemy arrives.
- [x] `Combat/Damageable.cs` — gained `GrantInvulnerability(float)` and `IsInvulnerable`.
- [x] `Animation/CharacterPose.cs` — `CharacterState.Dash = 9`, appended.
- [x] `Character/CharacterLayerView.cs` — Dash falls back to Move rather than drawing nothing.
- [x] `Player/PlayerController.cs` — the dash branch, and `SetMotion` skipped while dashing.
- [x] `Player/PlayerAim.cs` — facing frozen while dashing.
- [x] `Rooms/WaveSpawner.cs` — `spawnDelay` + the `_pending` guard.
- [x] `Testing/TestOverlay.cs` — a `DASH GO / i-FRAME / ready / n%` readout.
- [x] `Input/InputSystem_Actions.inputactions` — `Sprint`→`Dash`; the dead `Jump`/`Crouch`/`Previous`/
      `Next` template actions deleted; the `buttonNorth` double-bind on `Interact`+`HeavyStrike` fixed.
      8 actions, 32 bindings, **zero duplicate binding paths** (validated by parsing the JSON).
- [x] `Art/Placeholder/Sheets/Dash.png` — 432×470, **4 cols × 5 rows of 108×94**, 20 frames.
- [x] `Art/Placeholder/VFX/SpawnBurst.png` — 48×48, transparent.
- [x] `Art/Placeholder/Tiles/UpperCaves_Wang16.png` — 128×128, 16-tile Wang sheet. **Candidate only,
      referenced by nothing** — see the art note below.

### Three integration points, and why each is where it is

**`PlayerController.FixedUpdate` gained a branch between knockback and the attack lunge.** Both halves
are forced: *below* knockback because that branch is documented as outranking everything (a dash that
shrugged off a Brute slam would be i-frames **plus** immunity to displacement, which no doc grants);
*above* the lunge because CORE_SYSTEMS §2 requires the dash to cancel an attack in Recovery, and if the
lunge branch ran first it would still own velocity on the frame the dash starts.

**The dash exposes velocity, it does not move the body.** Same contract as `LungeVelocity`. An
`AddForce` or a transform write is erased on the same physics step, because `FixedUpdate` writes
`linearVelocity` outright every tick.

**The Dash-Attack Cancel turned out to be three lines.** `CanCancel` already implemented BALANCE §2
correctly and `Stop()`'s own doc comment already said the cancel would call it. The seams were left
open on purpose and they fit.

### The spawn delay nearly broke the room

`CheckProgress()` runs synchronously at the end of `SpawnNextWave()`. With every spawn deferred behind
a telegraph, the alive list is empty at that moment — so the room would have advanced the wave, or
**declared itself cleared and opened the doors, before a single enemy existed.** Guarded with a
`_pending` count incremented *before* the wait, and `CheckProgress` returns early while it is non-zero.
`Clear()` also had to gain `StopAllCoroutines()`, or re-arming mid-delay lands an enemy into a room
that has already reopened.

`WaveSpawner` is now **384 lines**, past CLAUDE.md's ~300-line prompt to check whether it is doing two
jobs. It is arguably doing three (pooling, wave sequencing, arrival timing). Worth a split next time it
is touched.

### The art finding — do Phase 0 before any more environment art

The character tools anchor to an existing character and hold style perfectly: the dash came back
indistinguishable from the shipped idle on the first try. **The freeform tools have no anchor**, and
two environment generations were rejected before one passed — the first tileset returned a pale
cyan-white wall, which ART_DIRECTION §2 reserves cross-biome as the *Flooded Tunnels hazard accent*.

The fix that worked, and should be the standard from here: **build a forced palette from the project's
own shipped colours and pass it as `color_image_base64`.** That took the spawn burst from an opaque
navy-and-purple square to a correct transparent decal in one attempt.

`.claude/skills/deeper-art` Phase 0 (generate and approve one canonical asset, then reference it
everywhere) has never been done, and this is what it exists to prevent.

### Integration, done

1. [x] Compiled clean — all ten changed files, first pass.
2. [x] `Dash.png` resliced as a 108×94 grid, 4 cols × 5 rows, Center pivot, 32 PPU, Point,
       Uncompressed, `Dash_<row>_<col>` with row 0 at the top. Unity's auto-import had guessed a
       20-sprite variable-rect slice at 100 PPU with a bottom-left pivot; that had to be replaced.
       Same cell size as the shipped `Katana_Attacks` sheet, so the two align on one pivot.
3. [x] **`Dash` clip added to `Anim_Body_Base.asset`** in FacingArt row order, `StrikeFrames` empty.
       `Resolve(Dash, …)` returns a sprite for all 8 facings × 4 frames, 0 null. The rig draws
       `Dash_2_0` for a right-facing dash — Side row, unmirrored, correct.
4. [x] `SpawnBurst.png` imported (32 PPU, Point, Uncompressed, Center pivot).
5. [x] `DigDash` and `PlayerXP` on `Player.prefab`'s root, every reference wired.
       **`DashTrail` went on `Visual`, not the root** — it is a character visual and belongs beside
       `AuraVisuals`, the other afterimage system, per the one-group-per-subsystem rule.
       `PlayerController.dash`, `PlayerAim.dash` and `TestOverlay.dash` all wired.
6. [x] `SpawnTelegraph` on the room's `Encounter`, beside the `WaveSpawner` it serves;
       `WaveSpawner.telegraph` wired, and the mark's `lifetime` set from the spawner's own
       `spawnDelay` so the two cannot drift.
7. [x] Verification pass run in `01-VERIFICATION.md` order.
8. [ ] **The Wang tileset is rejected, not pending.** See below — nothing to slice.

### Two defects the integration found, both fixed

**1. The dash covered 13.2 units for an authored 3.0, and the error scaled with frame rate.**
`_remaining` ticked in `Update` while `PlayerController` integrates `DashVelocity` in `FixedUpdate`.
Every physics step inside a catch-up burst therefore read the *same* `_remaining` and ran at full dash
speed — one stalled frame is 16 steps at peak velocity. Demonstrated by changing nothing but
`Time.maximumDeltaTime`: **13.164 units at the 0.3333 default, 3.298 clamped to one step per frame.**
Fixed by ticking in `FixedUpdate`, and sampling `DashVelocity` half a step back so the discrete sum
lands on the authored distance rather than ~11% short of it. Re-measured under the original
conditions: **3.139 units.**

**The same shape exists in `AttackStateMachine`** — `_elapsed += Time.deltaTime` in `Update` drives
`LungeVelocity`, which `PlayerController` also integrates in `FixedUpdate`, so the attack lunge
overshoots the same way. **Deliberately not changed here:** that timer also drives phase transitions,
the Active-window opening, chain buffering and animation frame alignment, all of them tuned against
the current behaviour. Moving it is a feel change, not a bug fix, and it should be its own pass.

**2. The dash trail drew on the wrong sorting layer.** `DashTrail.Stamp` copied `bodyRenderer`'s
layer and order — but the body renderer sits *inside* the rig's `SortingGroup`, so its authored
`Default/0` is never what draws; the group's `Actors` at `YDepthSort`'s order is. The trail landed on
`Default/-1`: above the Walls tilemap at `-10`, so a dash past a wall drew the trail on top of it, and
below every enemy instead of interleaved with them by depth. Now copied from the `SortingGroup`, one
order behind it — verified as `Actors/-170` against a group at `Actors/-169`.

This is the general trap the layout rule already warns about, in a new place: **inside a
`SortingGroup`, a renderer's own sorting layer is authored data that never reaches the screen.** Read
the group.

### The tileset is rejected

`UpperCaves_Wang16.png` shipped as a candidate. On its own 32×32 grid its stone-and-moss edge runs
**cross the cell boundaries instead of sitting inside them**, so it is not a valid 16-tile Wang set and
a `RuleTile` built from it would seam at every join; its olive-green also sits outside the cool
grey-purple the built rooms use. It stays in `Art/Placeholder/Tiles/` referenced by nothing. The fix is
the Phase 0 anchor pass below, not another freeform generation.

---

## First Combat Room — owner-directed, Milestone 3 pulled forward

**Status:** ✅ Built and verified in play mode. Divergences and invented numbers are recorded in
`Docs/00-DESIGN_CHANGE_BRIEF.md` §13.

The first room type. `Armed → Fighting → Cleared`: she walks into the room, both doors shut behind
her, six enemies spawn, and the doors open again on the killing blow. This is the first thing in the
project that is a game loop rather than a sandbox.

**Built:**
- [x] `Scripts/Rooms/CombatRoom.cs` — the lifecycle only. Opens and shuts doors, raises `Cleared`.
- [x] `Scripts/Rooms/WaveSpawner.cs` — the encounter as data. One `ActorPool` per distinct prefab,
      sized and prewarmed from the wave data itself. 1 wave is a standard Combat Room; 2–3 make it a
      Wave Room (CORE_SYSTEMS §8) with no other change.
- [x] `Scripts/Rooms/RoomDoor.cs` — one door: collider + sprite on or off. No tween.
- [x] `Scripts/Rooms/RoomEntry.cs` — the trigger volume, on the new layer 8 `RoomTrigger`.
- [x] `Prefabs/Rooms/CombatRoom_UpperCaves_01.prefab` — 28×16, 6 posts, 2 doors, 6 spawn markers,
      mounted under `Level` in `TestScene` with the scene's own tilemaps switched off.
- [x] `Scripts/Editor/RoomLayout.cs` — the layout as an ASCII map, plus the two things derived from it
      (the painted tilemaps and the marker positions), so the map and the prefab cannot drift apart.
- [x] `Scripts/Editor/PlaceholderRoomArt.cs` + `Art/Placeholder/Rooms/Door.png` — 32×64 programmer art.
- [x] `Scripts/Testing/TestRoomControls.cs` — `F12` re-arms the room. Plus four additions to
      `TestOverlay` for its legend and status lines.

### Why four classes and not one

Same reasoning as the enemy split (`Enemy` / `EnemyChase` / `TelegraphedAttack` / `EnemyDeath`): one
job each, and a reader looking for "what shuts the door" opens the file called `RoomDoor`. `RoomEntry`
in particular is not over-split — Unity delivers trigger callbacks to the GameObject carrying the
collider, and the volume has to be positioned on the room's half-way line rather than at the room
origin, so it needs its own object either way.

### The one that would have bitten: `Damageable.Died` survives pooling

`Died` is a plain C# event with no sender, and a pooled instance never runs `Awake` again — so a
subscription made at spawn lives into the enemy's next life. The room's alive-tracking is therefore
**state-driven, not a counter**: a list of `Damageable`s, pruned by `IsAlive`, unsubscribing as it
goes. That is idempotent, so even a leaked double-subscription changes nothing, where a `--count`
would have opened the doors a kill early. Subscribe on exactly one line (right after `pool.Get`),
unsubscribe on exactly two (`Prune` and `Clear`), and `Clear` unsubscribes **before** releasing —
releasing does not fire `Died`, so the order is what stops a re-armed room double-counting.

### Two engine facts that decided the layout

- **`EnemyChase` has no pathfinding** — straight-line steering with a stop/retreat band. Interior cover
  must be isolated convex posts with clearance; any concave pocket traps an enemy and the room never
  unlocks. This constrains all 18 Combat Rooms and is written in no design doc (brief §13.2).
- **Aggro radius binds spawn placement.** `EnemyTarget.Acquired` gates all movement and attacking, and
  the radii are 10–12. LEVEL_DESIGN §4's "spawn points at room edges" would put spawns 15–23 units from
  the lock line in a 28-wide room, where they stand completely still (brief §13.1).

### The layer-8 decision, demonstrated rather than assumed

`RoomEntry` sits on a new layer **8 `RoomTrigger`**, not Default. `ThrownRock.blockingLayers` is
Default and the rock is itself a trigger carrying a kinematic rigidbody, so a Default-layer volume
across the room's middle destroys every Rock Slinger projectile that crosses it. Proven three ways in
one probe: rock vs wall → despawns (the mechanism works), rock vs Entry on layer 8 → survives, rock vs
Entry on layer 0 → despawns. **Any future room-scoped trigger volume belongs on layer 8.**

Two probes that looked conclusive and were not, recorded so they are not repeated: `Physics2D.Simulate`
does **not** fire `OnTriggerEnter2D` for a collider that starts already overlapping, and `Destroy` is
deferred to end of frame — so a freshly-instantiated rock always reads as "survived" no matter what.
Testing through a real `ActorPool` (where `Release` deactivates synchronously) is what made it
measurable.

### Verified in play mode

`Application.runInBackground = true` first; everything driven through public methods, since simulated
key presses never reach play mode.

| Check | Result |
|---|---|
| Fresh play | `Armed`, both doors open, 0 alive, wave 0/1 |
| Spring the room | `Fighting`, both doors shut, **6** spawned, one per marker, each at its definition's max HP |
| Kill one at a time | alive steps 6→5→4→3→2→1→0, **exactly one per kill**; `Cleared` raised once; doors open |
| Clear is `Died`-driven | `pool.Live` still **6** at the moment the doors opened — the 0.45 s corpse delay does not gate it |
| Pooled reuse | 3 full encounters, always 6 spawned, counting never double-steps, `Encounter` child count stays **7** (6 rigs + markers) — reuse, not re-instantiation |
| Re-arm mid-fight | 3 alive → `Arm()` → 0 alive, 0 active, doors open, `Armed`; re-entering then counts cleanly, so the aborted fight left no subscription behind |
| The trigger itself | moving the player's rigidbody into the band → `Fighting` (the trigger-stay path) |
| Wave Room path | 2 waves, `IsWaveRoom` true; killing to 1 remaining spawns wave 2 while the straggler lives; `Cleared` only once every wave is dead |
| Rock vs entry volume | survives on layer 8, despawns on layer 0, despawns on a wall |
| Left behind | console clean, `timeScale` 1, 0 stray `(Clone)` roots, scene not dirty, `CameraRig` re-enabled |

**Looked at, not just asserted** — three named-camera screenshots at the room's true framing: Armed
(door gaps open in both side walls), Fighting (both doors visibly shut, six enemies on their markers,
the Brute behind her at the entry door), Cleared (open again, empty). Nothing renders black.

### Outstanding

- **Player death is now a dead end in the literal sense.** She can die inside a locked room whose doors
  only open when the enemies are dead. `Damageable.Died` still has no subscriber on the player.
- **No room loading, no floor sequencing.** `RoomManager` and the reshuffling bag are unbuilt;
  `CombatRoom.Cleared` is deliberately the one event written for something that does not exist yet.
- **No cracked tiles and no breakable wall** — the room is not LEVEL_DESIGN §3-compliant until both
  land. The layout reserves a 2×2 zone for the first.
- **`CameraRig` has no bounds clamp**, so standing at a door shows past the room edge into void.
- **Feel is unjudged** — whether the fight actually clears in BALANCE §8's 30–60 s needs a human.
- ~~**`F12` was the last free function key.**~~ **Resolved 2026-08-16** — the sandbox has a test config
  HUD (a toggled clickable panel on the backquote key), so harness additions now cost a button rather
  than a key. See *Wave Room + test config HUD* below.
- **Its spawn markers are no longer consumed in authored order.** `WaveSpawner` now chooses each
  arrival against the player's position, so this room's original "one enemy per marker" result only
  holds when she springs it from the lock line. Re-verified; see below.

---

## Dash rework, Dash Attack and the chargeable Heavy — owner-directed, 2026-08-16

Five items in one owner brief. Two were presentation fixes, three were new mechanics. Recorded in
`Docs/00-DESIGN_CHANGE_BRIEF.md` §17, which is where the design consequences live — this section is
the engineering half.

- [x] **Dash direction comes from the movement keys, not facing** (`DigDash.ResolveDirection`).
- [x] **Dash Attack** — a fourth `AttackAction`, its own `CharacterState`, its own timing row.
- [x] **Heavy Strike charges** — a fifth `AttackPhase`, with chargeability as weapon data.
- [x] **Dash HUD slot back to a square**, matching the weapon slot beside it.
- [x] **New dash icon**, generated rather than drawn.

### `DigDash` reads its own Move action, and that is not duplication

The obvious implementation is `PlayerController.MoveInput`. It is wrong here: `DigDash` carries
`[DefaultExecutionOrder(-10)]` so that a dash beats the attack chain buffer, which means it runs
*before* the controller polls input and would read the **previous** frame's direction. A direction
tapped on the same frame as the dash key would be dropped. Reading the action directly costs three
lines and makes the ordering irrelevant.

Normalized rather than raw, so an analog stick at half deflection still dashes BALANCE §1's full 3.0
units — `DashVelocity` multiplies this vector by the distance directly.

### The Dash Attack is a fourth `AttackAction`, and that exposed a live trap

Adding `AttackAction.DashAttack = 3` broke five lookups at once, all of the same shape:

```csharp
action == AttackAction.Basic ? basicX : action == AttackAction.Heavy ? heavyX : ultimateX
```

A two-step ternary over a three-value enum silently treats **every future value** as the last one, so
a Dash Attack would have inherited the Ultimate's lunge, hitstop, camera shake, hitbox radius and hit
VFX. All five are now `switch` statements with an explicit `default`, in `AttackStateMachine`,
`AttackHitbox` and `HitVFX`. `ActionFor` had the same bug with a worse symptom — it returned the
*Ultimate's* InputAction for any unknown action, so R would have buffered a chain into a Dash Attack.

`UltimateGauge` was the one site left alone: its `action == Heavy ? Heavy : Basic` is correct by
construction, and BALANCE §4's table has two columns. Inventing a third there would have been a
design decision made inside a lookup.

### Charging is a phase, not a component

`AttackPhase.Charging` sits in front of Windup. The alternative — a separate `HeavyCharge` component
owning the hold and calling into the state machine — was rejected because it needs the Heavy
InputAction, the weapon data and the animator, all of which this class already owns, and it would
have had to take the heavy button *away* from the machine that polls it.

The phase is deliberately **not committed** (`IsCommitted`, which is what `PlayerController` and
`PlayerAim` now read instead of `IsAttacking`). While charging she keeps turning to the cursor and
can dash out. ~~She walks at 0.45x.~~ **Reversed the same day: a charge roots her** (owner,
2026-08-16, brief §20) — `chargeMoveScale` is **0** on `Player.prefab`, and `GDD §Player` now locks
the rooting. It is a speed of zero, never a change to `IsCommitted`, which would freeze her aim,
make the charge undashable and hand movement to `LungeVelocity`. Two consequences fell out of the
phase being uncommitted, and both are load-bearing:

- `LungeVelocity` had to switch to `IsCommitted` too. During Charging `_elapsed` is still zero, so its
  ease-out curve reports **peak** lunge speed for the entire hold — a bug that would only have
  appeared if anything read it, which `PlayerController` no longer does. Guarded at the source.
- The Dash-Attack Cancel was extended to break out of a charge. A hold she cannot escape is a trap,
  and the dash is the only escape she has.

### One looping animation clip, the first on the rig

Every other action clip has a length the state machine knows in advance. A charge lasts exactly as
long as the button is held, so `CharacterAnimator.PlayLoop` wraps `_actionElapsed` instead of handing
the pose back. It subtracts one cycle rather than zeroing, so a long hold does not stutter once per
pass.

### The art-fallback map moved onto `CharacterState`

Two places need the same answer to "what do I draw when this state has no art yet" —
`CharacterLayerView` resolves the *sprite*, `AttackStateMachine` reads the *frame count* to size the
clip. They already had two hand-maintained copies covering different sets of states, which is a
defect waiting to happen: the frame count would come from one clip while a different one was drawn.
There is now one `CharacterState.FallbackArt()` extension and both call it. It also covers
`Dash -> Move`, which `CharacterLayerView` had and `AttackStateMachine` did not.

This is what lets all three new moves ship as working code independent of their art, and what will
let the Bow and Greatsword have them for no frames at all.

### Art: three clips, five directions, and a boot

Generated through the `deeper-art` skill on the existing `Deeper Protagonist Katana` PixelLab
character, v3 mode, one generation per direction.

**The technique that made the difference was seeding each generation with a start frame rather than
describing the pose.** Written as prose, "holding the katana raised overhead" produced four frames of
her standing with no sword visible, and the dash attack came back with an orange glowing blade that
matches nothing else in the game. Seeded from the raised-blade frame of the shipped Heavy Cleave and
from the last frame of the shipped Dash, both came back correct first time — and as a bonus the Dash
Attack now visually continues out of the dash pose, because it literally starts on it.

Sheet layout was **measured off the shipped sheets, not assumed**: 108x94 cells with each 92x92
PixelLab frame centred at (8,1). The sprites use a Center pivot, so packing at the frames' native
92x92 would have shifted every new clip relative to the existing ones.

The packer strips small dark blobs detached from the figure and **reports each removal** — one 28px
stray appeared below her feet in a dash-attack frame. A cleanup pass that guessed silently would
eventually delete part of a swing.

The dash icon is generated (a winged boot) where the chevrons it replaces were drawn. This is the
same generate-things-with-material / draw-things-with-geometry split as before, landing the other way
this time — and `HUDFrameArt` no longer writes `HUD_IconDash`, because leaving that `Write` in place
would have silently clobbered the generated PNG the next time anyone ran the menu item.

### Outstanding

- **`AttackStateMachine` is now ~640 lines and is doing two jobs** — phase/input state machine, and
  the contact feel layer (hitstop, shake, gauge, combo, VFX dispatch). It was already over the
  ~300-line guidance before this pass. The split worth doing is `AttackStateMachine` +
  `AttackFeedback`; it was not done here because it moves components between prefab groups, and that
  is its own pass with its own verification.
- **The lunge timer bug is still unfixed** and now has a sibling: `_elapsed` ticks in `Update` while
  `LungeVelocity` is integrated in `FixedUpdate`, exactly the shape that made the dash travel 13.2
  units for an authored 3.0. Still deliberately left alone — that timer also drives phase transitions
  and frame alignment, so changing it is a feel pass.
- **The Bow and Greatsword default to chargeable.** That is a design call, flagged in change brief
  §17c, and switching it off is a checkbox on the asset.
- **Player death is still unbuilt**, and the Dash Attack makes the dash a more attractive thing to
  spend offensively, which means dying with the dash on cooldown is now easier to arrange.

---

## HUD restyled as pixel art — owner-directed, 2026-08-16

**Owner-directed:** *"Look at HUD. It's too simple for pixel game."* The previous pass had stripped
the chrome down to a four-pixel band of one flat grey after the owner called the generated kit "too
much" — that earlier note was about **size** (a 448×129 health bar, a fifth of the screen wide), and
the fix overshot into a wireframe. This pass keeps every footprint from that correction and puts the
craft back as material rather than as area. Nothing here is larger than what it replaced.

- [x] **`Scripts/Editor/HUDFrameArt.cs`** rewritten (`Deeper/Generate HUD Frames`). A real plate
      profile — outline, one lit pixel on the top and left faces, near-black on the bottom and
      right, a dark body, and a single dithered ring fading the highlight in. Bars gained solid
      riveted end caps and an **inverted** bevel around the channel, which is what makes a bar read
      as cut into the plate rather than punched through it. HP gained 8 segment ticks. New pieces:
      `HUD_Banner` (the wave plaque) and one fill column per bar.
- [x] **`Scripts/Editor/PixelFontGlyphs.cs` + `PixelFontArt.cs`** (`Deeper/Generate HUD Font`) — a
      5×7 bitmap face packed into `HUD_Font.png` and built into `HUD_Font.fontsettings`. The glyph
      table is its own file because it is data, not logic.
- [x] **`Scripts/UI/StatBar.cs`** — a chase bar: it holds where the fill was, then drains to it, so
      the *size* of a hit reads as a block. Wired on HP only; XP and the Ultimate never fall.
- [x] **`Scripts/Editor/BuildRunHUD.cs`** — assigns the font, adds a 1px hard drop shadow to every
      label, wires the chase bar, loads each bar's own fill column, and puts the plaque behind the
      wave text. Slot insets are now named constants tied to the border widths `HUDFrameArt` draws.
- [x] **`Scripts/UI/ExperienceBarHUD.cs`** — the level badge shows the number alone.

### Three couplings this created, all of them load-bearing

1. **A slot's border width is duplicated in two files and must agree.** `HUDFrameArt` draws
   `HUD_SlotSquare` at a 6px border precisely so 76 − 12 lands on **64**, the authored icon size;
   `BuildRunHUD.SlotBorder` insets the socket, the cooldown sweep and the icon to match. They are
   named constants on both sides rather than a shared one because the art tool must not depend on
   the layout tool. Change one, change the other.
2. **A segment tick may never land on a bar's exact centre column.** `MeasureHole` reads each
   channel's *height* down that column, and an opaque tick there reports a channel 4px short, which
   would sit every fill high inside its frame. An even segment count always puts one there, so the
   whole tick set is nudged 2px clear — dropping that divider instead leaves a visible gap in the
   middle of eight marks.
3. **Each bar's fill column is generated at that bar's channel height** (26 / 20 / 14) rather than
   shared. A shared column stretched to three heights is the one place point-filtered art gets
   resampled by a non-integer factor, and the fill's bright top row is exactly what smears.

### Verified by rendering, not by assertion

`01-VERIFICATION.md` §3's "read the PNGs off disk, composite them in code, look at the image" path —
the whole kit was drawn in a throwaway mirror of the algorithm first, composited over a cave-dark
ground at 1×, 2× and 4×, and iterated on until it read right. Three things were wrong on the first
render and were only visible in the picture: the body dither ran end to end and read as **woven
mesh** at 4×, the whole ramp sat too bright and competed with the bars it frames, and the chase bar
was louder than the health still standing behind it. A scripted check confirms the contract that
matters — `MeasureHole` reports 292×26 / 276×20 / 222×14, each `_Fill` column matches its channel
height exactly, and the health bar carries 7 dividers with none on the sampled centre column. The
ported glyph table was diffed against the rendered prototype and is identical, glyph for glyph.

**And it compiles.** All 72 project scripts, 0 errors, against Unity's own Roslyn and reference set
with no editor open — the method is now `01-VERIFICATION.md` §10. That is what makes the API surface
this pass leans on (`Font.characterInfo`, `CharacterInfo`'s corner UVs, `TextureImporterNPOTScale`,
`Shadow`) a checked claim rather than a remembered one.

### Then run for real in the editor, which found one more

The three menu items were run in `TestScene` and the built HUD rendered to
`Captures/HUD_ingame.png` (Overlay canvases need the camera trick in `01-VERIFICATION.md` §3).

The font asset builds correctly and needs no caveat: `dynamic=False`, `fontSize` 14, `lineHeight`
18, `ascent` 14, **76** `characterInfo` entries (51 authored + 25 lowercase aliases — 'x' has its
own glyph), material on `UI/Default` with the 192×64 point-filtered atlas, and every glyph spot-check
resolving to advance 12 and box (0,0)–(10,14). All three menu items logged **zero warnings**, which
is itself the check that every sprite loaded, every `Wire` found its serialized field — including
the new `ghost` and `ghostColor` — and `MeasureHole` found a hole in every frame. In the rebuilt
HUD all 9 labels are on `HUD_Font` and all 9 carry a `Shadow`.

**And the render caught a defect nothing else would have: the weapon slot drew as a solid white
box.** `BuildRunHUD` created the icon `Image` enabled with a null sprite, which UGUI draws as a white
quad — 64px square in the middle of the screen. It is not only an editor artefact: `WeaponIconHUD`
is what hides an empty icon, and its `OnEnable` **returned early when `loadout` was null**, so with
no tagged player in the scene nothing ever switched it off. Both halves are fixed — the icon is now
built disabled (as the upgrade slots already were) and `OnEnable` always refreshes, subscribing only
when there is a loadout. This is `01-VERIFICATION.md` §4 again: it passed every assertion.

### The defect that mattered most was invisible to every check above

The owner's first look at the finished HUD was **"it's the same UI, what is changed?"** — and every
verification in this section had already passed. The art was correct, the font asset was correct,
the layout was correct, the render I had taken was correct. What was wrong sat outside all of it:

- [x] **`Scripts/UI/PixelPerfectHUDScale.cs`** — pins the canvas to `ConstantPixelSize` with a
      whole-number `scaleFactor` from `Screen.height / 1080`, floor 1. `BuildRunHUD` adds it.

`CanvasScaler` was on `ScaleWithScreenSize`, which produces a **fractional** factor at any window
that is not exactly the reference. The editor Game view is 906×463, so the factor was **0.4498**, and
at that factor every detail this pass added is finer than the resampling error — bevel, rivets,
segment ticks, and the font, which drew `74 / 128` as `r4 / 128` because the 7's top stroke fell
between two screen pixels. Rendered side by side at the same 906×463, 0.45 versus integer 1× is not
a subtle difference; it is the difference between the old flat chrome and the new one.

**The lesson is about where I rendered, not whether I rendered.** I verified at 1920×1080, the one
resolution where the bug cannot appear, and the art is authored 1:1. Verifying a resolution-dependent
thing at its reference resolution proves nothing about any other. Check `canvas.scaleFactor` is a
whole number *before* concluding anything about HUD art.

### Then the fix was the wrong size, and that exposed a worse bug

Whole-number scaling with the art still authored 1:1 forces the factor to **1** at every window below
1080 — the owner's next note was *"you made UI bigger in low resolutions"*. The health bar spanned a
third of a 906px Game view instead of a sixth.

- [x] **The whole kit is re-authored at half its on-screen size** and `ReferenceHeight` is 540, so the
      normal factor at 1080p is **2**. Bars 160×18 / 150×15 / 120×11, slots 40 and 22, hex 28, banner
      134×19, fill columns 12/9/7, every layout constant in `BuildRunHUD` halved. On-screen at 1080p
      this is pixel-for-pixel what it was; on a small window it is half the footprint.
- [x] **The font packs at 1× now** (`PixelFontArt.Scale`), native size 7 rendering at 14 on screen.
      That also closes the two-pixel-grids divergence: chrome and text finally share one grid.
- [x] The 64px weapon icons need no re-authoring — a 32-logical-px rect at factor 2 is 64 screen px,
      so they land 1:1 exactly as before. This is why `HUD_SlotSquare`'s border is 4 (40 − 8 = 32).

**The bug this uncovered is the important one.** After regenerating, the HUD rendered *completely
wordless* — chrome perfect, not one letter. `font.characterInfo` read back flawless (76 entries,
correct advances and UVs) and `Text` even generated the right vertex count, but
`GetCharacterInfo` returned **false for every character**: assigning `characterInfo` to a Font that is
already loaded does not rebuild Unity's internal glyph map. Fixed with an
`AssetDatabase.ImportAsset(..., ForceUpdate)` after `SaveAssets`, and written up as
`01-VERIFICATION.md` §5b.

It is worth being precise about how bad this one was. It **only bites on a re-run** — the first
generation calls `CreateAsset`, and creating the asset builds the lookup — so the tool worked
perfectly the first time and would have silently produced an invisible font every time after. Every
inspectable value stayed correct throughout. Nothing but rendering it would have caught it.

### The dash cooldown was drawn inside-out

Owner: *"there is blue background behind dash icon and it looks terrible"*. It was not a background —
it was the cooldown wipe, and it was inverted on both axes at once.

`DigDash.CooldownNormalized` returns **1 when ready**, and `DashHUD` assigned it straight to the
sweep's `fillAmount`. So a ready dash — which is nearly all the time — drew a *full* translucent blue
disc across the whole slot, and the disc **emptied** exactly while the cooldown was running. The one
moment the element had something to say was the moment it went blank, and the rest of the time it was
a permanent blue wash that read as a background.

- [x] `DashHUD` fills `1f - charge` (the cooldown *remaining*) and sets `sweep.enabled = charge < 1f`,
      so a ready dash draws nothing at all.
- [x] The scrim is dark (`0.05, 0.05, 0.07, 0.72`), not blue — a cooldown scrim, not a tint.
- [x] `BuildRunHUD` builds the Sweep **after** the Glyph. Sibling order is draw order, and underneath
      the icon the scrim only darkened the socket showing through the icon's transparent pixels.

Verified by rendering the slot at charge 1.0, 0.55 and 0.0: bright icon on a plain dark socket, a
partial wedge, and a fully darkened slot.

### Outstanding

- **Between 1080 and 2160 the factor stays at 2**, so a 1440p display gets a proportionally smaller
  HUD than 1080p. Inherent to whole-number scaling; noted in change brief §18f, no action planned.
- **`HUD_BarLarge`, `HUD_BarMedium`, `HUD_BarThin`, `HUD_IconFrame`, `HUD_DashPip` and
  `HUD_LevelBadge` are still on disk and still unreferenced**, now two restyles out of date.
- **`HUD_SlotRound` and `HUD_Disc` are regenerated but unused** — kept only so the kit stays one
  style if the round dash slot is ever revisited. The owner has rejected it twice.
- **`Captures/` holds the preview PNGs** and is untracked throwaway; delete it whenever.
- **The HUD now contains two pixel grids.** The chrome is 1:1 at the 1920×1080 reference; the font
  is authored at 5×7 and packed at 2×, so text has a 2px grid. Deliberate — a 7px face is
  unreadable at 1080p — but it is a second scale inside one HUD, and it compounds change brief
  §15.5's open question about HUD-vs-world pixel density.
- **`HUD_BarLarge`, `HUD_BarMedium`, `HUD_BarThin`, `HUD_IconFrame`, `HUD_DashPip` and
  `HUD_LevelBadge` are still on disk and still unreferenced**, now two restyles out of date.
- **`HUD_SlotRound` and `HUD_Disc` are regenerated but unused** — kept only so the kit stays one
  style if the round dash slot is ever revisited. The owner has rejected it twice.

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
- Ultimate Gauge (fill-on-hit, drain-on-use) + Katana Combo Counter + ~~Combo Finisher (CORE_SYSTEMS §4, §5a)~~ — **no finisher is owed.** The GDD now states both chain hits deal equal damage, and whether a later hit should scale as a finisher is `GDD §Open Decisions 11`. Nothing here is unbuilt against locked design; building one would be inventing the ruling
- Dash-Attack Cancel (CORE_SYSTEMS §2), and the **Dash Attack** — a fourth weapon action, now GDD-locked (`GDD §Player`, Controls + Dodge/Mobility). Built 2026-08-16, see *Dash rework* below
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

**Definition of Done:** Katana's full kit feels good against Cave Crawler in a single test room. Matches Design/07 Phase 1 exit criteria. **The kit is now five moves, not four** — Basic (a looping 2-hit chain), Heavy (chargeable), Ultimate, Dig-Dash with its Dash-Attack Cancel, and the Dash Attack — all of them built and all of them now locked in `GDD §Player`. What this milestone still owes is feel, not code.

**Potential technical risks:**
- Designing the Attack State Machine too Katana-specific, making Milestone 2's generalization painful — mitigate by keeping windup/active/recovery timing data-driven (per-weapon values, not hardcoded) even though only one weapon exists yet.
- Input System action map churn as more actions (Heavy Strike, Ultimate, Dash) get added — keep the `.inputactions` asset as the single source of truth, don't hardcode `KeyCode` checks.

---

## Milestone 2 — Bow & Greatsword, `IWeapon` Generalization
*(maps to Design/07 Phase 2, Days 11–18)*

**Goal:** Generalize Katana's concrete implementation behind the shared `IWeapon` interface (CORE_SYSTEMS §1), then implement Bow and Greatsword against it.

**Systems/features involved:**
- `IWeapon` interface: `BasicAttack()`, `HeavyStrike()`, `Ultimate()`, `OnHitLanded(target)`, `GetAttackTiming()` — ⚠️ **this list is one action short.** `GDD §Player` now says the weapon determines a weapon-flavored **Dash Attack** as well, and `AttackAction` has carried a fourth value since 2026-08-16. Whatever shape the interface takes must cover four actions, not three, or the Bow and Greatsword ship with the Katana's dash lunge — the same trap the two-step ternary sprang when the action was added (see *Dash rework* below)
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
- **The Bow's signature trait now collides with a universal mechanic, and that is a risk to the abstraction, not just to the Bow.** `GDD §Player` locks a chargeable Heavy Strike on all three weapons while keeping Charge Shot as the Bow's locked signature; as built, `ChargeSpec` is data on `WeaponDefinition` and both the Bow and Greatsword default to chargeable. If the resolution is "Katana-only" (`GDD §Open Decisions 10`, brief §17c/§20), charging stops being weapon data and becomes a per-weapon branch — exactly what `IWeapon` exists to prevent. **Get the decision before generalizing**, because the answer changes whether `Charging` stays a phase on the shared state machine.
- Projectile hit-detection needs to be reusable by enemies later (Rock Slinger, Current Wisp, etc.) — build it weapon-agnostic from the start, not Bow-specific.

---

## Milestone 3 — Biome 1 Content: Rooms, Enemies
*(maps to Design/07 Phase 3, Days 19–26)*

**Goal:** First full playable biome, start to finish.

> **The Rising Hazard is cut (owner, 2026-08-15).** `HazardFront` and its per-biome reskins are **not to be built** — see CORE_SYSTEMS §7, now a removal notice. The cracked-tile collapse micro-system survives on its own; it was always separate. This deletes the largest reusable system Milestone 5 was going to inherit, so the "Biome 2/3 are pure reskins" assumption below is weaker than it was. Rooms also no longer need low/high flood-zone data.

**Systems/features involved:**
- ~~Room-lock logic and the Wave Room batch trigger~~ — **done ahead of this milestone**, see *First Combat Room* below
- ~~Room system: room loading, reshuffling-bag draw of 3–5 rooms per floor (CORE_SYSTEMS §8)~~ — **built**, see *Floor loading* (2026-08-24) and *The run* (2026-09-08). `Scripts/Run/` sequences sixteen floors and the bag refills only on a biome change, because refilling per floor resets the shuffle §8 depends on
- Cracked tiles: collapse-under-standing-weight micro-system, Upper Caves (`GDD §Biome Identity`). **Still the only unbuilt item Biome 1's identity rests on** — and `GDD §Open Decisions 4` treats it as the benchmark Biomes 2–3 are measured against, so it is worth building before that question is answered rather than after
- ~~Upper Caves enemy roster~~ — **done ahead of this milestone**, see *Biome 1 basic enemies*. All four exist on placeholder art. Only the Collapsed King is left.
- ~~Upper Caves room layouts: 6 Combat Rooms (1–2 flagged `IsWaveRoom`), 2 Reward Rooms (LEVEL_DESIGN §2–3)~~ — **the 6 Combat Rooms and the Wave Room are authored** (`Layout_UpperCaves_01..07`), plus the Secret Vault, a Mini-Boss arena and Floor 16's two arenas: eleven prefabs. **Reward Rooms do not exist and are not to be built** — cut on 2026-08-14, and the GDD's 2026-09-18 rewrite carries no trace of them
- Mini-Boss: The Collapsed King, with weapon-check mechanic (CORE_SYSTEMS §11). The **arena** exists; the boss in it is a scaled, tinted `TunnelBrute` variant with BALANCE §6's HP and nothing else — `GDD §Open Decisions 7`
- ~~Secret Vault room + key-drop logic (CORE_SYSTEMS §8)~~ — **built ahead of this milestone**, see
  *Secret Vault* below. Its cost and payout are no longer invented: `GDD §Roguelike Structure` now locks one guarded fight tougher than a standard Combat Room, a guaranteed Legendary-tier payout, and **the key consumed on opening** — which is exactly what `VaultDoor.consumeKey` was carrying as an invention, so that tooltip's "INVENTED" is now out of date. **Unverified: whether the built encounter is actually *tougher* than a standard room's.** Both are six enemies; the vault's is authored on the prefab and the standard is `Encounter_UpperCaves_Standard`, and the two have not been compared on total HP or composition. Whether it can sit on the route is still open (`GDD §Open Decisions 2`)

**Dependencies:** Milestone 2 (all 3 weapons must exist — room layouts need to accommodate all 3, per LEVEL_DESIGN §2 positioning-zone requirement).

**Files/systems likely to be created:**
- ~~`Scripts/Rooms/Room.cs`, `Scripts/Rooms/RoomManager.cs` (per-floor room sequencing, deterministic shuffle)~~ — **built, and neither has that name.** Sequencing is `Scripts/Run/` (`FloorLoader`, `RoomBag`, `BiomeRoomPool`, `RunPlan`, `RoomRole`, `RunSeed`), not a `Manager` on a room; which room goes where is one method, `FloorLoader.RoleFor`
- ~~`Scripts/Rooms/CombatRoom.cs` (room-lock logic, `IsWaveRoom` flag + wave-batch trigger)~~ — **built**, and split four ways rather than one: `CombatRoom` / `WaveSpawner` / `RoomDoor` / `RoomEntry`. `IsWaveRoom` is a derived property, not a serialized flag. See *First Combat Room* below
- ~~`Scripts/Hazards/HazardFront.cs`~~, ~~`Scripts/Hazards/UpperCavesHazard.cs`~~ — **not to be written; the Rising Hazard is cut.** The cracked-tile collapse survives as a small room-authored component (`Scripts/Rooms/CrackedTile.cs` or similar), not as a hazard skin
- ~~`Scripts/Enemies/RockSlinger.cs`, `TunnelBrute.cs`, `DeepWarden.cs`~~ — **these were never written, on purpose.** The roster shipped as composed components plus one `EnemyDefinition` asset each; a per-enemy class would have held nothing. See *Biome 1 basic enemies*.
- `Scripts/Enemies/BossPhaseController.cs` (weapon-check read, reused by all bosses per CORE_SYSTEMS §11)
- ~~`Scripts/Rooms/SecretVault.cs`, `Scripts/Player/Inventory.cs` (SecretKey flag)~~ — **built, and
  neither file has that name.** There is no `SecretVault` room class at all: a vault is a `CombatRoom`
  plus `VaultDoor` (the lock) and `VaultReward` (the payout), because a second room class would have
  re-implemented the lifecycle it already has. And `Inventory` was the wrong name to revive — the
  inventory system was deleted at the owner's direction, so the key count lives in
  `Scripts/Player/RunKeys.cs`. See *Secret Vault* below
- Room prefabs/scenes under `Assets/_Main/Scenes/` or `Prefabs/Rooms/`

**Implementation order:** ~~room loading + lock logic → reshuffling-bag draw~~ → cracked tiles → ~~6 Combat Room layouts~~ → Mini-Boss + weapon-check → ~~Secret Vault + key drop~~ → full-biome playtest. **What is actually left in this milestone is three things:** the cracked-tile micro-system, **the Collapsed King as a real boss** (the arena and a placeholder with BALANCE §6's HP exist; no phase system, no weapon-check — `GDD §Open Decisions 7`), and the layout work LEVEL_DESIGN §3 asks for on top of the six that exist — cracked tiles and a breakable wall, which no room has.

**Definition of Done:** Full Biome 1 clear is playable start to finish with all 3 weapons. Matches Design/07 Phase 3 exit criteria (MVP.md's largest content-authoring risk — see below).

**Potential technical risks:**
- Room-layout authoring (18 total Combat Rooms across all biomes eventually) is explicitly flagged in Design/07 and LEVEL_DESIGN.md as the single biggest schedule risk — this milestone alone authors 6 of them. **Retired for Biome 1, proven for the rest:** the six exist, and authoring them is now an ASCII map in a `Layout_*.cs` plus one press of `Deeper/Build All Room Prefabs`. The live version of the risk is repetition, not authoring — floors draw **3–5** rooms from a pool of 7, verified at 61 rooms over a sixteen-floor run.
- ~~`HazardFront` must generalize to Biome 2/3 variants~~ — moot, the system is cut. **The residual risk is the opposite one:** with no hazard to reskin, Biomes 2 and 3 have much less to inherit from this milestone, and what differentiates them is `GDD §Open Decisions 4` (DESIGN_RULES Rule 4/5).

---

## Milestone 4 — Upgrade & Curse System
*(maps to Design/07 Phase 4, Days 27–32)*

**Goal:** The run-to-run build-variety layer.

**Systems/features involved:**
- ~~Weighted-draw upgrade system (CONTENT_DESIGN §1, BALANCE §13)~~ — **built**, see *Milestone 4, first half* below
- ~~Shared upgrade pool (24 entries, CONTENT_DESIGN §1 / BALANCE §9)~~, ~~Curse pool (8 entries, CONTENT_DESIGN §3 / BALANCE §11) + always-visible 4th slot~~ — **authored as data**: 38 upgrades and 8 Curses with generated icons
- Weapon-specific sub-pools (45 entries total, CONTENT_DESIGN §2 / BALANCE §10) — the Katana's 13 live on `WeaponDefinition`; the Bow's and Greatsword's are blocked on Milestone 2
- ~~Upgrade screen UI (ART_DIRECTION §5)~~ — **built, and the GDD now describes what was built**: `GDD §UI` has stopped saying "icon, name, short description" and specifies the icon-led card (tier-colored border, unique icon, effect-category glyph, compressed effect line). No engineering change; it stops being a recorded divergence
- **The effects themselves — the second half of this milestone, and the bulk of it.** `Docs/Engineering/03-UPGRADE_STATUS.md` is the generated inventory: only what is expressible as a `StatModifier` runs, and every Curse is data only. This is what gates the milestone, not the UI
- **Evolution offers every 5th level** (`GDD §Core Gameplay Loop` 4, `GDD §UI`, CORE_SYSTEMS §13) — GDD-locked, **unbuilt**, and blocked on content rather than code: §13's two Evolutions per weapon are an open item in the design doc itself, so `UpgradeOffer` presents an ordinary draw on every level including the fifth

**Dependencies:** ~~Milestone 3 (upgrades are offered at floor-end, needs the room/floor loop to exist).~~ **Wrong since 2026-08-14 and still wrong when the panel was built: upgrades are triggered by level-up, not floor end** (`GDD §Core Gameplay Loop` 4). The dependency is therefore XP — `PlayerXP` and `XPReward` — not the floor loop, which is why the offer screen shipped before a run existed. What it does still need from Milestone 3 is a *reason to level*, i.e. enemies worth XP.

**Files/systems likely to be created:**
- `Scripts/Upgrades/UpgradeDefinition.cs` (data-driven — likely a `ScriptableObject` per upgrade so content authoring doesn't require code changes per entry)
- `Scripts/Upgrades/UpgradePool.cs`, `Scripts/Upgrades/WeightedDraw.cs`
- `Scripts/Upgrades/CurseDefinition.cs`, `Scripts/Upgrades/CursePool.cs`
- `Scripts/UI/UpgradeScreen.cs`
- Modifier application hooks on `PlayerController`/`IWeapon` implementations (many upgrades modify existing systems rather than adding new ones — per Design Rule 2, implement as parameter changes on existing components, not new systems per upgrade)

**Implementation order:** ~~weighted-draw core (generic, tier-aware per BALANCE §13)~~ → **shared pool wired to effects** → weapon sub-pools → ~~Curse pool + 4th slot~~ → ~~upgrade screen UI~~ → Evolution offer → full-run playtest for build variety. The order ran UI-first at the owner's direction; what is left is the middle of it.

**Definition of Done:** A full Biome 1 run generates genuinely different builds run-to-run. Matches Design/07 Phase 4 exit criteria. **Not met, and the gap is not the screen:** picking a card records it and draws it on the HUD strip, but only `StatModifier` upgrades change how the run plays, so two runs with different cards currently play the same. `03-UPGRADE_STATUS.md` is the live count.

**Potential technical risks:**
- 69 total upgrade entries (24 shared + 45 weapon-specific) + 8 curses is a lot of individual effect implementations — MVP.md explicitly allows shipping a reduced subset (~12–15 shared, ~8–10 per weapon) if this runs long; flag early if it's trending that way rather than discovering it on Day 32.
- `ScriptableObject`-per-upgrade only pays off if the effect-application code is genuinely data-driven (e.g., a small set of effect "kinds" with numeric parameters) — if every upgrade needs bespoke code anyway, the `ScriptableObject` layer is just overhead. **Half-answered by the first pass:** seven of 38 are pure `StatModifier` bundles and need no code at all, which is the data-driven case working; the other 31 and all 8 Curses each need a hook in the damage pipeline that does not exist. The decision still owed is which hooks to add, not whether to keep the assets.

---

## Milestone 5 — Biomes 2 & 3
*(maps to Design/07 Phase 5, Days 33–40)*

**Goal:** Content-scale the Biome 1 pattern across the remaining two biomes — pure content authoring, no new core systems if Milestone 3 generalized correctly.

> **`GDD §Open Decisions 4` governs this whole milestone and should be answered before any of it is authored.** As built, `Pool_FloodedTunnels` and `Pool_MoltenDepths` hold Biome 1's layouts and roster under their own themes — floors 6–16 are Upper Caves in a different colour, which is the palette-swap DESIGN_RULES Rule 4/5 rejects. The seams are two pool assets and no code, so the answer decides how much of the list below is real: new rooms, new enemies, a stronger environmental hook, or all three.

**Systems/features involved:**
- Flooded Tunnels: enemies (Eel Diver, Current Wisp, Bloated Drifter, Elite: Tideheart), rooms with water patches and currents, Mini-Boss (Drowned Custodian). ~~Low/high water-tile data, hazard variant (rising water + room geometry change)~~ — cut with the Rising Hazard
- Molten Depths: enemies (Ember Wisp, Magma Crawler, Forge Golem, Elite: Cinder Warden), rooms with geyser tiles, Mini-Boss (Molten Sentinel). ~~Scorched ground, hazard variant (lava flow)~~ — cut with the Rising Hazard

**Dependencies:** Milestone 3 (`Room` and `BossPhaseController` must already be generic/reusable — this milestone is the test of that). `HazardFront` is no longer part of that contract; it is cut.

**Files/systems likely to be created:**
- ~~`Scripts/Hazards/FloodedTunnelsHazard.cs`, `Scripts/Hazards/MoltenDepthsHazard.cs`~~ — not to be written; the Rising Hazard is cut. Water/current and geyser tiles remain as room-authored components
- `Scripts/Enemies/EelDiver.cs`, `CurrentWisp.cs`, `BloatedDrifter.cs`, `Tideheart.cs`
- `Scripts/Enemies/EmberWisp.cs`, `MagmaCrawler.cs`, `ForgeGolem.cs`, `CinderWarden.cs`
- `Scripts/Enemies/DrownedCustodian.cs`, `Scripts/Enemies/MoltenSentinel.cs`
- Room prefabs for both biomes (12 more Combat Rooms + 2 Mini-Boss arenas; Reward Rooms are cut)

**Implementation order:** Flooded Tunnels (enemies → rooms → Mini-Boss) → Molten Depths (same order) → cross-biome playtest.

**Definition of Done:** A full 3-biome run (floors 1–15) is completable. Matches Design/07 Phase 5 exit criteria. **Completable already** — sixteen floors were driven end to end on 2026-09-08 — so this criterion no longer separates done from not done. The honest one is `GDD §Open Decisions 4`'s: a Flooded Tunnels floor plays differently from an Upper Caves floor, rather than looking different.

**Potential technical risks:**
- If this phase does *not* run faster than Milestone 3 per-biome, it means Milestone 3's systems weren't actually generalized — that's a signal to stop and fix the abstraction rather than push through with biome-specific hacks (Design Rule 2). **The room half already passes:** a biome is a `BiomeRoomPool` asset and a theme, and standing up two placeholder biomes cost no code.
- ~~Water/lava room-geometry changes require low/high tile-zone data~~ — moot, cut with the Rising Hazard. **The replacement risk is design-side and now numbered:** `GDD §Open Decisions 4`. Raise it before authoring 12 rooms against it.

---

## Milestone 6 — Final Boss, Hub, Meta-Progression
*(maps to Design/07 Phase 6, Days 41–45)*

**Goal:** Close the loop — death/victory return the player to a Hub that actually matters.

> **Half of this milestone is already built, out of order and at the owner's direction** — see *The Hub* (2026-09-07) and *The run* (2026-09-08). The loop itself closes: Hub → weapon select → descend → sixteen floors → death or victory → summary → back to the Hub with Shards, verified in play mode. What is left is what makes the Hub *matter*: the bosses being bosses, and everything the player spends Shards on.

**Systems/features involved:**
- Final Boss: The Depth Warden — **her father** — multi-phase (BALANCE §6). ⚠️ Its phases were themed on the 3 biome hazards, which are cut; they need re-theming before this is buildable. A placeholder exists with §6's 1200 HP and nothing else (`GDD §Open Decisions 7`)
- **Zyno, fought immediately after the father — MUST SHIP** (CONTENT_DESIGN §5). MVP version reuses an existing Mini-Boss's moveset/arena, palette-swapped, with his own dialogue. Which Mini-Boss is undecided. This is unscheduled work: Design/07 Day 41 budgets one boss, not two. Floor 16 **is** two fights in two arenas already; both are placeholders
- Escape sequence (post-boss 45s countdown). ~~It was specified as reusing the Hazard system, which no longer exists~~ — `GDD §Victory` now states it plainly as the only timer left, and `GDD §Open Decisions 5` is the call on whether it survives at all. **Do not build it until that is answered**; victory currently ends on Zyno's arena clearing
- Hub Stat System: Core Stats + **Marks** (CONTENT_DESIGN §7, BALANCE §15). Unbuilt — the stat shrine is placed in the Hub and answers "not yet" via `HubNotice`
- ~~Ore → Ore Shard conversion (BALANCE §14)~~ — **stale naming and a stale system: there is no Ore, and no conversion.** Shards are computed once at run end from Levels Gained and Depth Reached (`GDD §Progression`), which `Scripts/Run/RunEnd.cs` does and `Meta/ShardBank` banks. **Built**
- ~~Death/Victory screens (GDD §UI)~~ — **built as one screen**, `Scripts/UI/RunSummaryPanel.cs`, which is what `GDD §UI` now specifies: one screen for both outcomes, distinguished by title and accent colour
- Relic Vault, Weapon Mastery stub (tracking only, per MVP.md). Unbuilt. The Relic itself exists (the Katana's, on `WeaponDefinition.RelicSpec`), so the Vault's job is purchase and guarantee, not content
- **Persistence.** `Meta/ShardBank` is a `ScriptableObject` seam, not a save file: a runtime write sticks for the editor session and is discarded in a build, so a player who closes a build loses their Shards. `SaveData` below is what fixes that, and every caller already speaks to `Add`/`TrySpend`

**Dependencies:** Milestone 5 — but weakened: the Final Boss arena was specified as incorporating "all 3 biome hazard types in sequence," and those no longer exist. What the degrading arena is built from is now an open design question (LEVEL_DESIGN §6).

**Files/systems likely to be created:**
- `Scripts/Enemies/DepthWarden.cs` (multi-phase, own dedicated arena logic per LEVEL_DESIGN §6). **The shared piece Milestone 3 also needs is `BossPhaseController`** — no boss of the five has a phase, a transition or a weapon-check, and there is no system for one
- `Scripts/Meta/SaveData.cs` (persistent Shards, Hub Stat ranks, Weapon Mastery counters, discovered Relics) — **the one unbuilt thing the built half already depends on**, per the `ShardBank` note above
- `Scripts/Meta/HubStatSystem.cs`, `Scripts/Meta/CoreStat.cs`, `Scripts/Meta/Mark.cs` (renamed from Miner's Trait on 2026-08-14). `PlayerStats.SetSource` is the seam these apply through — its `StatType` vocabulary was built to mirror the Core Stats table for exactly this
- ~~`Scripts/Meta/OreConversion.cs`~~ — not to be written; there is no Ore. `Scripts/Run/RunEnd.cs` computes the award, `Scripts/Meta/ShardBank.cs` holds it
- ~~`Scripts/UI/HubScreen.cs`, `Scripts/UI/DeathVictoryScreen.cs`~~ — **built, and neither has that name.** The Hub is a walkable scene rather than a screen (`Scripts/Hub/` + `WeaponSelectPanel`, `ShardCounterHUD`, `HubPromptHUD`); the summary is `Scripts/UI/RunSummaryPanel.cs`, one screen for both outcomes
- `Scripts/Meta/RelicVault.cs`, `Scripts/Meta/WeaponMastery.cs` (stub: counter tracking only)

**Implementation order:** ~~Final Boss → escape sequence~~ (both blocked: the phases need re-theming, and `GDD §Open Decisions 5` decides whether the escape exists) → **boss phases + weapon-check, the thing all five bosses share** → Hub Stat System → ~~Shard award + Death/Victory screen~~ (built) → `SaveData` → Relic Vault + Weapon Mastery stub → full end-to-end playtest.

**Definition of Done:** ~~Hub → Run → Death/Victory → Hub loop works end to end.~~ **Met on 2026-09-08.** The remaining criterion is 08-MVP.md's MUST SHIP list, which this loop does not yet satisfy: no boss has a phase or a weapon-check, nothing persists across a build, and there is nothing to spend Shards on.

**Potential technical risks:**
- Save data format needs to be settled once and be forward-compatible-ish, since it's the first thing touched by every subsequent post-MVP content pass — avoid a schema that requires a migration for every new Mark added later.
- Final Boss arena is explicitly a one-off, non-reused layout (LEVEL_DESIGN §6) — don't try to force it through the generic `Room` system if it doesn't fit; a bespoke controller is the correct call here, not a violation of Design Rule 2 (reuse applies to systems, not to a deliberately unique set-piece).

---

## Cross-Cutting Engineering Notes

- **Data-driven content over hardcoded content**, wherever the design doc describes a *table* (upgrades, curses, enemy stats, Hub Stat costs) — favor `ScriptableObject` or serialized data assets so BALANCE.md's "numbers are placeholders" (Design Rule 8) can be iterated without code changes. Don't over-engineer this into a generic data-editor tool; a `ScriptableObject` per entry is enough.
- **One `OnDamageDealt` / `OnHitLanded` event pipeline**, per CORE_SYSTEMS §6 — Combo Counter, Ultimate Gauge, HUD damage numbers, and on-hit upgrade procs all subscribe to the same event rather than each system re-detecting hits independently.
- **No runtime weapon-swapping, no branching room paths** — these are locked design decisions (GDD, LEVEL_DESIGN §1); don't build generalized systems that accommodate them "just in case."
- **Assembly definitions:** not needed until compile times become a problem or an editor-only/runtime split is needed (e.g., editor tooling for authoring `ScriptableObject` content). Revisit if Milestone 4's content volume makes iteration slow.

---

## Open Engineering Questions

Two lists, because they have two different owners. The first is **`GDD §Open Decisions`** — the eleven numbered design questions the 2026-09-18 rewrite collected out of the scattered ⚠️ callouts. They are design calls, not engineering ones (Design Rule 9); what this file adds is *what each one blocks here and in which milestone*, so the two documents can be read against each other by number. The second list is what engineering is still missing that the eleven do not cover.

### Blocked on `GDD §Open Decisions`

| # | The question | What it blocks here | Milestone |
|---|---|---|---|
| 1 | Is there a clock at all? | Nothing in flight — but it is the **only** open item that would add a new system rather than fill an existing seam, and #3, #5 and #8 all resolve differently depending on it. Engineering should not invent a substitute clock. | — (new system) |
| 2 | Can the Secret Vault sit on a floor's route? | `RoomRole.SecretVault` is declared on all three pools and **never drawn**; `FloorLoader.RoleFor` has no line for it. On-route needs a second doorway on the layout — `RoomConnection.HasExit` reads doorway *columns*, so the prefab changes, not the loader. | 3 |
| 3 | What does freeing a Trapped Soul cost? | The whole room type. `RoomRole.TrappedSoul` is declared and unfilled; a cost of "a fight" is a Combat Room variant, a cost of "HP" or "an upgrade slot" is not. | 3 |
| 4 | Are Biomes 2 and 3 mechanically distinct enough? | **Governs Milestone 5 entirely** — see the note at the top of it. Both pools currently hold Biome 1's content under a different theme. | 5 |
| 5 | Does the Floor 16 escape sequence survive? | The escape timer, and with it whether `RunEnd` gains a third outcome (escaped / caught) or stays two. | 6 |
| 6 | "Descend" plays as "walk east." | Floor-transition presentation in `FloorLoader`, which butts each room east of the last and increments a number silently. A stairwell room is a room type; a fade is not — the answer decides which. | 3 |
| 7 | No boss has a phase or a weapon-check. | `BossPhaseController`, the one system all five bosses share, and therefore the Collapsed King in Milestone 3 as much as the Depth Warden in Milestone 6. All five are scaled `TunnelBrute` variants carrying BALANCE §6's HP. | 3 + 6 |
| 8 | Greed's Toll has no downside. | That Curse's effect, in Milestone 4's behavioural half. It is the one entry whose *upside* can be implemented without knowing the answer, which is the trap. | 4 |
| 9 | What is "Deeper" once the story resolves? | Nothing buildable — it decides whether post-run-1 content exists at all. Named here so it is not mistaken for an engineering gap. | post-MVP |
| 10 | Does charging belong on every weapon's Heavy Strike? | **The shape of `IWeapon`.** Charging is `ChargeSpec` data on `WeaponDefinition` today, which is only correct if it stays universal; "Katana-only" makes it a per-weapon branch. See Milestone 2's risks. | 2 |
| 11 | Does the Basic chain need a finisher, and does it touch the Combo Counter? | Per-hit chain damage in `AttackStateMachine` (equal today) and whether `ComboCounter` grows a second entry point. Cheap either way — one timing row and one call — which is why it has stayed open harmlessly. | 1 (+4 hooks) |

### Engineering-owned, or design-open but outside those eleven

- **XP level-threshold curve and per-enemy XP drop values** (BALANCE §16) — explicitly unresolved in design, and now load-bearing: levelling is what triggers every upgrade offer, so the curve is the pacing of Milestone 4's entire content layer. `PlayerXP` and `XPReward` carry invented placeholders.
- **Mini-Boss Overcharge exact clear-trigger condition** (CORE_SYSTEMS §16, renumbered from §12 when the XP/Evolution/Souls/Narrative sections were added) — needs a design decision before Milestone 3/5 boss work locks it in. Not covered by `GDD §Open Decisions 7`, which is about phases, not about Overcharge.
- **Weapon Mastery node effects** (3–5 per weapon) — explicitly deferred past MVP; Milestone 6 only needs the counter, not the effects.
- **Which damage-pipeline hooks the 31 non-`StatModifier` upgrades need**, and in what order — engineering's own call, gating Milestone 4's second half. `03-UPGRADE_STATUS.md` is the inventory to work from.
- **The Ultimate still consumes the Combo Counter and discards it** (brief §7h) — a live gameplay hole rather than an open question, carried since the Ultimate became a buff. It makes casting at zero stacks optimal. Unlike the eleven above, this one has a wrong answer running in the build right now.

---

## Upper Caves Wave Room + test config HUD — owner-directed, 2026-08-16

**Status:** ✅ Built and verified in play mode. Design consequences and invented numbers are in
`Docs/00-DESIGN_CHANGE_BRIEF.md` §19.

`/implement-room-type Wave Room` is a **layout** job, not a new type. CORE_SYSTEMS §8 calls a Wave Room
"a variant flag on Combat Room prefabs, not a new room type", and the code already had it: `WaveSpawner`
takes 2–3 batches, `nextWaveAtRemaining` is §8's "~1 remaining" threshold, `CombatRoom.IsWaveRoom` is
derived from the wave count, and the *First Combat Room* pass above already verified that path. **No new
room class was written.**

**Built:**
- [x] `Prefabs/Rooms/WaveRoom_UpperCaves_02.prefab` — 32×18, 9 posts, 2 doors, 10 spawn markers,
      3 waves / 12 enemies. Layout 2 of the 6 Upper Caves Combat Rooms, and the 1 flagged Wave Room.
- [x] `Scripts/Editor/Layout_UpperCaves_02.cs` — its ASCII map.
- [x] `Scripts/Editor/Layout_UpperCaves_01.cs` — room 01's map, moved out of `RoomLayout` verbatim.
- [x] `Scripts/Editor/RoomLayout.cs` — **refactored into the shared painter.** Every function now takes
      a `string[] map`; added `Validate` (ragged rows and stray characters, since a map is hand-edited
      and a short row silently shifts a whole line rather than throwing).
- [x] `Scripts/Editor/BuildRoomPrefab.cs` — **builds a whole room prefab from its map**: hierarchy,
      tilemaps, doors, entry volume, markers, components, wiring, save. One menu item per room.
- [x] `Scripts/Rooms/WaveSpawner.cs` — spawn placement is now chosen at runtime (below).
- [x] `Scripts/Testing/TestRoomSelector.cs` — loads one room prefab at a time, moves the player to its
      `PlayerStart`, re-points `TestRoomControls`.
- [x] `Scripts/Testing/TestConfigHUD.cs` + `Scripts/Editor/BuildTestConfigHUD.cs` — the debug menu.
- [x] `TestRoomControls.Bind`, `TestOverlay.SetLegendVisible` — small hooks for the two above.

### Why the room prefab got a builder

Room 01 was assembled by hand, so every fact about it — that the floor draws at sorting order −20, that
the entry volume is on layer 8, that a door collider is 1×2 — lived only inside 6,500 lines of YAML.
With a second layout due and four more after it, that is four more chances to get one of them subtly
wrong. `BuildRoomPrefab` makes those facts a dozen readable lines and derives everything positional from
the map, so a room and its map cannot disagree. Same argument `BuildRunHUD` and `RoomLayout` already
make. **Rooms 03–06 now cost a map and a menu item.**

### The change with real reach: arrivals are placed against the player, not by authored order

`WaveSpawner.NextPosition` used to cycle `spawnPoints` by an index. It now takes the marker **farthest
from the player that is still inside the spawning enemy's aggro radius** (read off the prefab's
`EnemyDefinition`), falling back to the old cycling order when nothing is in range.

This is the synthesis of a design rule and an engine fact that had been recorded as irreconcilable
(brief §13.1): LEVEL_DESIGN §4 wants arrivals at the room edges, `EnemyTarget.Acquired` makes anything
beyond 10–12 units inert. Taking the farthest *in-range* marker satisfies both, and it is what lets a
32-wide room have edge markers at all.

Two details that are load-bearing:

- **Markers taken inside a batch are tracked and excluded.** Every enemy in a wave resolves its position
  on the same frame, so without this "farthest in range" returns the same winner every time and **the
  whole batch stacks on one tile.** Cleared per wave, not per encounter. When a batch outruns the
  in-range markers the list resets and rotates rather than stacking the remainder.
- **The player is found by tag and cached**, the way `CameraRig` and `EnemyTarget` do it — a room prefab
  cannot hold an Inspector reference to a player who lives outside it, and `RigRefs.Find` is wrong here
  for the reason it is always wrong in room code (it searches from `transform.root`, which for anything
  a spawner made is the *spawner*).

### The debug menu, and the one hazard it introduced

Every player system reads its `InputAction` straight off the shared `InputActionAsset`
(`AttackStateMachine.cs`, `DigDash`, `PlayerController`, `PlayerAim`) — UGUI's `EventSystem` is nowhere
in that path and cannot swallow a click for them. **A click on a debug button would also swing the
katana.** `TestConfigHUD` therefore disables the whole `Player` action map while the panel is open and
re-enables it on close *and* in `OnDisable` — a leaked disable looks exactly like an input-system bug.
It also restores the hardware cursor, which `PlayerAim` hides, or the panel is there but unclickable.

The panel opens on **backquote/tilde**, not a function key: F1–F3 are player cheats, F4–F9 spawners,
F10 the overlay, F12 the room re-arm. The keys all still work — every button calls the same public
method its key calls, so there is one implementation per cheat.

### Verified in play mode

`Application.runInBackground = true` first; everything driven through public methods.

| Check | Result |
|---|---|
| Map integrity | 18 rows × 32, all legend characters, 9 posts each ≥4 tiles from another and ≥2 from the border |
| Painted output | 576 floor cells, 101 wall cells (92 perimeter after 4 door gaps + 9 posts); **walls read back identical to the authored map** |
| Marker coverage | worst floor tile is **7.21** units from its nearest marker — inside the tightest radius (10), so the cycling fallback is unreachable here |
| Fresh load | `Armed`, `IsWaveRoom` **true**, `WaveCount` **3**, 0 alive, doors open |
| Spring | `Fighting`, both doors shut, wave 1/3, **4** spawned |
| Telegraph is real | at the instant of springing: `alive` **4**, active enemy objects **0** — and the room did *not* clear itself (the `_pending` guard) |
| **Smart placement, measured** | from (6.5, 9.5): 6 of 10 markers in range at radius 10; the 4 crawlers took the **4 farthest** (8.06, 7.07, 7.07, 6.32), **each a distinct marker**; the two nearer and the four out-of-range were passed over |
| Wave progression | 4 → kill to 1 → wave 2 spawns 5 while the straggler lives → kill to 1 → wave 3 spawns 3 → `Cleared` raised **once**, doors open. 12 kills for 12 enemies |
| Pooled reuse | 3 full encounters, `Encounter` child count stays **13** (1 markers + 12 pooled) — reuse, not re-instantiation |
| Re-arm mid-fight | wave 2 with 6 alive → `Arm()` → 0 alive, 0 active objects, doors open, `Armed`; re-entering counts cleanly at 1/3 with 4 |
| **Room 01 regression** | unchanged: 6 spawned, alive steps 6→5→4→3→2→1→0 one per kill, `Cleared` once, doors open, all 6 arrivals inside their aggro radius |
| Clear is `Died`-driven | 6 enemy objects still active at the moment the doors opened — the 0.45 s corpse delay does not gate it |
| Room selector | load 0 → 1 → 0; player lands on each `PlayerStart` (4.5, 8.5 / 7.5, 9.5); overlay status follows (`WAVE 0/1` vs `WAVE 0/3`); outgoing room gone next frame |
| Config HUD input gate | closed: map **enabled**, cursor hidden. open: map **disabled**, Move/Attack off, cursor shown, and a Heal click healed without touching the attack state. Closed again: fully restored |
| Left behind | console clean, `timeScale` 1, 0 stray `(Clone)` roots, 0 active enemies, `CameraRig` re-enabled, scene saved |

**Looked at, not just asserted** — four screenshots in `Captures/`: the room Armed (door gaps open in
both side walls, 2 posts west / 7 east), mid-wave-2 (both doors visibly shut, crawlers swarming her at
d≈0.6 while both Slingers hold at d≈5), Cleared (open again, empty), and the config HUD open. Nothing
renders black. The first HUD capture found three real layout defects — buttons stretched by
`childForceExpandWidth`, labels clipped to "CombatRoom_Upp", and the panel overlapping `TestOverlay`'s
legend — all fixed in the builder rather than in the output, then re-shot.

### Outstanding

- **Still no player death / run-end**, and a 3-wave room lengthens the exposure: 12 enemies across 3
  batches with no death state and no way out of a locked room at 0 HP. This is the oldest item here.
- **2 of 6 Upper Caves layouts exist.** Four Combat Rooms still to author — now cheap, one map plus a
  menu item each.
- **Neither room has cracked tiles or a breakable wall**, so neither is LEVEL_DESIGN §3 compliant. The
  breakable wall is now *possible* (Dig-Dash exists) and no room has one.
- **Feel is still unjudged for both rooms.** The Wave Room's 260 HP is derived from room 01's untested
  150, so if room 01 is mistuned this inherits it. BALANCE §8 wants 60–100 s; nobody has played it.
- **`CombatRoom.Start` arms the room the frame after it is instantiated**, so anything that springs a
  freshly-loaded room in the *same* frame gets wiped by that `Arm()`. Harmless in play (she walks in
  later) and caught during verification, but it is a trap for the floor loader — load and spring must
  not share a frame.
- **The room selector is not the floor loader.** No sequencing, no reshuffling bag. `CombatRoom.Cleared`
  is still the untouched hook.
- **`AttackStateMachine`'s lunge still ticks its timer on `Update`** — unrelated to this pass, still the
  same shape as the dash bug fixed earlier, still deliberately left alone.

---

## Secret Vault — owner-directed, 2026-08-16

**Status:** 🟡 Built and imported, **not verified in play mode.** Design consequences and invented
numbers are in `Docs/00-DESIGN_CHANGE_BRIEF.md` §21.

`/implement-room-type Secret Vault`. Unlike the Wave Room above, this **is** a new type: CORE_SYSTEMS §8
gives a Secret Floor a locked door, a `SecretKey` from a rare elite, and a payout, and none of the three
existed. It is also the first room in the project whose gate is a *player resource* rather than a
trigger volume.

**Built:**
- [x] `Scripts/Player/RunKeys.cs` — the run's key count. Run-scoped, reset in `OnEnable`.
- [x] `Scripts/Enemies/KeyReward.cs` — grants a key on death. On `DeepWarden.prefab`, 1 key.
- [x] `Scripts/Rooms/VaultDoor.cs` — the lock. Reads the key, seals for the fight, reopens on the clear.
- [x] `Scripts/Rooms/VaultReward.cs` — the payout. Hands over the equipped weapon's relic on the clear.
- [x] `Scripts/Character/WeaponDefinition.cs` — `RelicSpec`, beside the existing `ChargeSpec` and
      `UltimateBuffSpec`. Authored on `Weapon_Katana.asset`.
- [x] `Data/Upgrades/Upgrade_EndlessEdge.asset` — Legendary tier, BALANCE §12's numbers.
- [x] `Scripts/Combat/ComboCounter.cs` — reads the relic: no cap, per-stack bonus 2% → 1%.
- [x] `Scripts/Rooms/CombatRoom.cs` — gained `StateChanged`.
- [x] `Scripts/Editor/Layout_SecretVault_01.cs` + `BuildRoomPrefab.BuildSecretVault` +
      `Prefabs/Rooms/SecretVault_UpperCaves_01.prefab` — 22×16, 4 posts, 1 floor door, 1 vault door,
      6 markers, one wave of 6 for 190 HP.
- [x] `Scripts/Editor/PlaceholderRoomArt.cs` — vault door (iron, brass lock plate) and pedestal.
- [x] `Scripts/Editor/RoomLayout.cs` — legend gained `V` and `T`.
- [x] Harness: `TestControls.GrantSecretKey` + its config-HUD button, `TestRoomControls` vault readout,
      the vault added to the room selector's list.

### There is no `SecretVault` room class, on purpose

The plan's Milestone 3 file list named one. A vault is a **`CombatRoom` plus two small components**: the
lifecycle, the doors, the wave and the clear event already exist and already work, and the vault needs
exactly two things on top — a lock that decides when one door opens, and a prize that fires on the
clear. A `SecretVault` subclass or sibling would have re-implemented the collider-and-sprite toggle that
`RoomDoor` *is*, giving two answers to "what does a shut door look like". Same reason `RoomDoor` and
`CombatRoom` were split in the first place, one level down.

`Inventory` was the other name in that list, and reviving it would have read as the deleted inventory
system coming back. `RunKeys` holds one count and nothing else; a second key type gets a second field,
not a bag.

### `CombatRoom` needed a second event, not a bigger one

`Cleared` cannot express "the room just locked", and the vault door has to seal on that transition —
because `EnemyChase` has no pathfinding, so a guard following her back through the 1-wide inner doorway
jams on the interior wall and the room never unlocks. `StateChanged` is a separate event rather than
`Cleared` generalised: `Cleared` is a named contract the floor loader is already written against, and
collapsing the two would make every future subscriber filter for the one transition it wants. Polling
`State` was the other option and it hides the transition rather than reporting it.

### The relic lives on the weapon asset

`RelicSpec` sits beside `ChargeSpec` and `UltimateBuffSpec` for the reason those do: CONTENT_DESIGN §4's
"only offered when that weapon is equipped" becomes *literally true* when the relic is weapon data, and
`VaultReward` can then pay out without ever naming a weapon or a relic. A registry keyed by
`WeaponType` would be the same data plus a way to disagree with itself.

Two consequences worth knowing:
- **The trait asks the run, the vault does not reach into the trait.** `ComboCounter` checks whether
  `RunUpgrades` carries the weapon's relic offer, so the vault stays ignorant of what any relic *does* —
  and the same relic arriving from a Mini-Boss drop or a Hub guarantee needs no second wiring. The
  Greatsword's Mountain's Fall will read its own field from `UltimateGauge` the same way.
- **Only the Katana's relic is authored.** The Bow's needs the Charge Shot and the Greatsword's needs its
  Ultimate; neither system is written. A vault entered with either weapon logs a warning and pays
  nothing, which is loud on purpose — a vault that silently pays nothing is indistinguishable from one
  that has not been reached.

### The lock is a child of the door, and that placement is the layer-8 rule again

`VaultDoor` is a trigger volume on layer **8 `RoomTrigger`**, parented to the door so the door keeps its
solid barrier on Default. Putting the trigger on the door object itself would put it on Default, where
`ThrownRock.blockingLayers` despawns every rock thrown through the doorway — the same finding the first
Combat Room demonstrated. The volume is also **2 × 2.4 units against a 1×2 doorway**, deliberately
oversized: the door's barrier stops her *before* the gap, so a volume the size of the gap is one she can
never reach.

### One builder for every room, not a room-type branch

`BuildRoomPrefab` builds vault doors and the pedestal from the same pass it builds everything else — a
map with no `V` or `T` cells simply produces neither. A `if (isSecretVault)` branch is the thing that
drifts. The one asymmetry is deliberate and commented at the call site: **the vault door is not in the
array wired to `CombatRoom.doors`**, because `Arm()` opens everything in that list and would unlock the
vault every time the room re-armed.

### Not verified — what has to be checked

Written and imported blind, exactly like the HUD and Dig-Dash passes, and those cost four defects
between them. Nothing below has been observed:

| Check | Why it is the risky one |
|---|---|
| Key drop | `KeyReward` resolves `RunKeys` in `OnEnable` and the Warden is pooled — the resolve-per-life path is the one pooling breaks |
| Door consumes exactly one key | `TryConsumeSecretKey` is called from both `OnTriggerEnter2D` and `OnTriggerStay2D`; the `_unlocked` guard is what stops Stay spending a second key on the next frame |
| Refuses to open on zero keys | Including that walking into it does not silently consume anything |
| Seal holds for the whole fight | `StateChanged(Fighting)` → `door.Close()`, and the guards cannot be kited into the antechamber |
| Payout fires once | On `Cleared`, and **not again** on a re-arm, and not twice if `Cleared` and `StateChanged` both land |
| `ComboCounter` re-values live stacks | Taking Endless Edge mid-combo should re-price banked stacks, not clear them |
| 190 HP in 22×16 | Feel. BALANCE §8's 30–60 s has now gone unmeasured for three rooms |

### Outstanding

- **Not run.** This is the first room type to be committed unverified; the brief's §21 says so too.
- **The key has no source in a real run.** `KeyReward` is on the Warden, but no room places one and
  there is no floor sequencing — the key is reachable only from the sandbox spawner or its debug button.
  Needs the floor loader, not a design ruling.
- **Two of three relics unauthored**, blocked on the Bow and Greatsword existing.
- **Three rooms now lack cracked tiles and a breakable wall** (LEVEL_DESIGN §3). A vault is the most
  obvious place in the game for a breakable-wall alternate entrance; that is a design call.
- **Still no player death / run-end** — and a vault is the worst room yet for it: locked in 22×16 with
  two Brutes.
- **`AttackStateMachine`'s lunge still ticks its timer on `Update`.** Untouched again.

---

## Floor loading, room selection and room dressing — owner-directed, 2026-08-24

**Status:** 🟢 System built and **verified in play mode**. Art is started, not finished — see
*Outstanding*. Design consequences are in `Docs/00-DESIGN_CHANGE_BRIEF.md` §22.

The owner asked for the Hades model: handcrafted room templates, procedurally selected and
procedurally dressed, plus a new scene that runs it. That is not a departure from locked design —
`LEVEL_DESIGN` §1 and `CORE_SYSTEMS` §8 already specify exactly this, and Design Rule 3 asks for it.
What is genuinely new is the **dressing** layer, which appears in no design doc.

This closes the longest-standing gap in the project: `CombatRoom.Cleared` had been written for a
floor loader that did not exist since the first room shipped. It finally has a subscriber.

**Built:**
- [x] `Scripts/Rooms/Wave.cs` + `SpawnGroup.cs` — extracted from `WaveSpawner`'s private nested types
- [x] `Scripts/Rooms/EncounterDefinition.cs` — a fight as content; budgets **derived**, never typed
- [x] `Scripts/Rooms/RoomConnection.cs` — west door, east door, arrival marker
- [x] `Scripts/Rooms/RoomTheme.cs` + `RoomDressing.cs` — the look, and applying it
- [x] `Scripts/Run/` — `RunSeed`, `RoomBag`, `RoomOption`, `BiomeRoomPool`, `FloorLoader`
- [x] `Scripts/Editor/BuildRoomLabScene.cs` + `Scenes/RoomLabScene.unity` +
      `Scripts/Testing/RoomLab.cs` / `RoomLabHUD.cs`
- [x] `Scripts/Editor/BuildRoomTiles.cs` — `Data/Tiles/` had no generator at all before this
- [x] `Prefabs/Rig/Main Camera.prefab` + `Global Light 2D.prefab`, extracted from `TestScene`
- [x] `Deeper/Build Combat Room Prefab` — room 01 predated the builder and could not be rebuilt
- [x] `Data/Encounters/`, `Data/Themes/`, `Data/Rooms/Pool_UpperCaves.asset`, `Art/Environment/`

### Rooms are placed adjacent, not teleported between

The decision the whole design rests on. Each room is mounted east of the last and the one behind is
destroyed. It deletes four problems at once: the next room's own `RoomEntry` band is already the
arrival trigger, so **no exit volume, no arrival marker and no new legend character were needed**;
`CameraRig` needs no change; and the `Start`-arms-the-room trap recorded above under *Wave Room*
cannot fire, because she has to walk the length of a room to reach the next band.

Alignment is `next = behind.EastAnchor.position + right − next.WestAnchor.localPosition`. Expressed
in door positions rather than room widths, so the 32×18 Wave Room — whose west door sits a tile
higher than a 28×16 Combat Room's — comes out shifted down by exactly that tile with no special
case. Verified across 9 mounts: 0 → 28 → 60 → 88 → 120 → 148 → 180 → 208 → 240, Wave Rooms at y −1.

### The encounter must be swapped while the room instance is inactive

`WaveSpawner.BuildPools` runs in `Awake` exactly once and `ActorPool` has no dispose, so a second
build would overwrite the pool dictionary for any prefab common to both encounters and strand the
first set of prewarmed instances under the `Encounter` transform, where `Clear()` can no longer
reach them — a leak that looks like nothing at all. `FloorLoader` therefore instantiates into an
**inactive staging root**, where Unity defers `Awake`, configures, then reparents. `SetEncounter`
latches `_poolsBuilt` and refuses loudly if called later, so the failure can never be silent.

### Extracting Wave/SpawnGroup did not change a single prefab byte

They were `private sealed class` nested types, so an encounter could not be authored from outside
the component that owned it. Promoting them to public top-level types with **identical field names**
left all three room prefabs' YAML byte-identical, because Unity serialises a by-value
`[Serializable]` class as an untyped nested mapping keyed by field name — no type token is written.
Confirmed both ways: the prefabs still read back 1/3/1 waves and 6/12/6 enemies through the editor,
and `EncounterDefinition` serialises to the same shape. The one thing that would break it is
`[SerializeReference]`, which *does* write a type token; there is a comment on the class saying so.

### Two door hooks, and why one is not enough

Once the room behind is destroyed, a cleared room's west door opens onto void. Closing it needs
**both** of `CombatRoom`'s events, because of ordering: `Arm()` runs `SetDoors(false)` *then*
`SetState(Armed)`, so `StateChanged(Armed)` lands after the open; but the clear runs
`SetState(Cleared)` → `SetDoors(false)` → `Cleared()`, so `StateChanged(Cleared)` lands *before* it
and only the `Cleared` event lands after. Subscribing to `StateChanged` alone is right in one
ordering and wrong in the other. `CombatRoom` itself needed no change at all.

### Verification

| Check | Result |
|---|---|
| Camera/light extraction | TestScene plays identically; light kept all 3 sorting layers; scene keeps its `target` override while the prefab falls back to the tag |
| Wave/SpawnGroup promotion | Prefab YAML byte-identical; Unity reads back 3 waves / 12 enemies and 1 wave / 6 |
| `EncounterDefinition` budget | `Encounter_UpperCaves_Standard` derives **150 HP**, matching room 01's authored number |
| `RoomConnection` wiring | Room 01 west (0.5, 8.0) / east (27.5, 8.0); vault correctly `hasExit=False` |
| Rebuilt room 01 | Renders pixel-identical to the hand-built original |
| Floor sequencing | 9 rooms over 2 floors; floor 1 rolled 4 rooms, floor 2 rolled 5; no immediate repeats |
| Adjacency | Exact, including the y-offset for the taller Wave Room |
| Dressing varies | Every room reported a different `RoomDressing.Signature` |
| Live room count | **2** in steady state — the climb to 9 was the deferred-`Destroy` artifact of driving 8 mounts inside one frame, confirmed by re-reading on a later frame |
| Real art through the pipeline | Two floor and two wall variants visibly mixed per cell |
| Left behind | Console clean — 0 errors, 0 warnings throughout; no stray `(Clone)` roots |

### The two art tools fail in opposite directions, and the fix is a recolour

Worth writing down, because it cost four rejected generations to find and it will recur for Biomes
2 and 3:

- **`create_image_pixflux` gets the palette right and the form wrong.** With the project's own
  colours passed as `color_image_base64`, *every returned colour* came from that set — the forced
  palette works exactly as the earlier spawn-burst finding claimed. But it shades the image as a
  **picture**, applying a scene-level top-left shadow: measured on a 32×32 floor candidate, edge rows
  averaged 51–58 against an interior of 75. A tile with a dark top-left border cannot repeat, and
  generating larger and slicing does not help — at 128×128 it draws a cave floor *scene*, with
  boulders and sweeping arcs, so the slices disagree wildly in brightness.
- **`create_topdown_tileset` gets the form right and the palette wrong.** It is the tool
  `references/pipeline.md` actually prescribes for tilesets, and its base tiles repeat **perfectly
  seamlessly**. But it takes no palette parameter, and it returned navy blue — which is the *Flooded
  Tunnels* family, the same class of mistake the change brief already records for the first tileset.
- **The fix is a luminance remap onto the shipped ramp**, applied after generation. It is pixel-exact
  — no resampling, so no off-grid fringe — and it is the same palette-swap-as-variant technique
  ART_DIRECTION §68 already uses for the Deep Warden. The floor came out on (51,48,56), which *is*
  the shipped floor base, plus one lighter step.

`Art/StyleAnchor/UpperCaves_Palette.png` holds the forced palette and its README records why each
colour is in it. `Art/Environment/UpperCaves/` holds the accepted output, including the full
recoloured 16-tile Wang sheet for a future `RuleTile` pass.

### Outstanding

- **Biomes 2 and 3 have one theme each and no enemies**, so they cannot be played — no rooms, no
  roster, no mini-boss. Their tiles exist so the theme system is proven across all three palettes.
- **Upper Caves has 3 themes and 62 decals**; more layouts would help more than more art now.
- ~~**Only 2 layouts in the pool.**~~ **Resolved 2026-09-08** — seven, which is `LEVEL_DESIGN` §2's
  full 6 Combat + 1 Wave for the biome.
- **The Secret Vault is not in the floor pool.** In an eastward corridor every room is walked out of
  and the vault is authored with one door. §8 calls a Secret Floor a *detour*, and a detour needs the
  branch §1 rules out. Change brief §22.2 — a design question, not a bug.
- ~~**No Mini-Boss room exists.**~~ **Resolved 2026-09-08** — `MiniBossArena_01` plus both Floor 16
  arenas, on placeholder bosses. The seam is now `BiomeRoomPool.RoomFor(RoomRole)`, and a role with
  no room still warns and draws a Combat Room rather than failing.
- ~~**`FloorLoader` has no scene.**~~ **Resolved 2026-09-08** — `RunScene` is back, built by
  `Deeper/Build Run Scene`, and the Hub's shaft leads to it. See *The run* below.
- **No floor-transition presentation**, whenever a run scene does come back. GDD calls a descent
  "linearly downward"; the loader walks east with the floor number incrementing silently. Brief §22.4.
- ~~**The camera lags on the first frame of a run.**~~ **Resolved 2026-09-08** by the one-line snap
  this entry proposed: `CameraRig.SnapToTarget()`, called from `FloorLoader.PlacePlayerAt`.
- ~~**Still no player death / run-end.**~~ **Resolved 2026-09-08**, and it was the oldest item in
  this document — `PlayerDeath` on the rig, `RunEnd` for the award, `RunSummaryPanel` for the screen.
- **`execute_code` over MCP is broken in this environment** — every snippet, down to
  `return 1 + 1;`, fails with a line-1 BOM error, in and out of play mode and after a forced
  refresh. Verification used temporary `[MenuItem]` probes driven by `execute_menu_item` and read
  back through `read_console`. Still true on 2026-09-08. Worth adding to `01-VERIFICATION.md`.
- **`AttackStateMachine`'s lunge still ticks its timer on `Update`.** Untouched again.

---

## Room art and the Room Lab — owner-directed, 2026-08-24

**Status:** 🟢 Built and verified. Replaces the run scene, which the owner did not want.

Three owner corrections after the first pass: the new scene should be a **room-visual sandbox with a
re-roll button**, not a run scene; the art set was far too small; and the credit budget should be a
gate that asks once, not something deliberated over on every call.

### The Room Lab replaces the run scene

`Scenes/RoomLabScene.unity`, generated by `Deeper/Build Room Lab Scene`. One button mounts a fresh
room: a layout drawn from the bag, a theme drawn from the biome, an encounter drawn from the
layout's list. A readout names all three so what you are looking at is never a guess.

**There is no player in it** (owner, 2026-08-24), and that is what makes it a lab rather than a
level. Three consequences, each of which needed handling:

- **The camera frames the whole room** instead of following anyone, measured from the room's tilemap
  renderers rather than a stored size — the layouts are 28×16, 32×18 and 22×16, and a lab that only
  fitted the first would crop the others. `CameraRig` is disabled rather than removed, so the prefab
  is untouched and the reason is visible in the Inspector.
- **Nothing crosses the entry band**, so the encounter is sprung directly. It is started on the
  room's `StateChanged(Armed)`, **not** inline at mount: `CombatRoom.Start` runs `Arm()` at the end
  of that same frame and `Arm` calls `encounter.Clear()`, so anything spawned inline is released a
  moment later. That is precisely the "load and spring must not share a frame" trap recorded when
  the floor loader was built — walked into once here, and caught by a screenshot with no enemies in
  it rather than by an assertion.
- **`Begin()` rather than `PlayerEntered()`**, so the room stays Armed with its doors open. Entering
  would shut them and hide the doorways; a lab wants the arrivals and the door gaps visible at once.
  With no player to measure against, `WaveSpawner` falls back to cycling the authored markers in
  order, which is exactly the right behaviour for judging where a layout puts its enemies.

`RoomLab` is test-only and deliberately **not** `FloorLoader`: sequencing a floor, destroying what
is behind her and aligning doorways are all irrelevant when the question is "does this room look
right". It reuses the shipped content pipeline rather than a parallel one — same `BiomeRoomPool`,
`RoomBag`, `RoomTheme` and `RoomDressing` — so what is judged here is what ships.

`RunScene` and `BuildRunScene` were deleted. `FloorLoader` is kept: it is CORE_SYSTEMS §8's
reshuffling bag, which MVP lists as MUST SHIP, and it is verified. It simply has no scene until the
run flow is the objective.

### The art pipeline: generate for form, recolour for palette

The two PixelLab tools fail in exactly opposite directions, and knowing which to use for what is the
whole finding:

| Tool | Form | Palette | Use for |
|---|---|---|---|
| `create_topdown_tileset` | ✅ base tiles repeat **seamlessly** | ❌ no palette parameter — returned navy, then green | floors and walls |
| `create_image_pixflux` | ❌ shades the image as a *picture* | ✅ `color_image_base64` is obeyed exactly | nothing tiling |
| `create_1_direction_object` | ✅ real transparency, 64 candidates per call | ❌ no palette parameter | decals and props |

Measured, not guessed: a 32×32 floor from `pixflux` came back with edge rows averaging 51–58 luma
against an interior of 75 — a dark top-left border that cannot repeat. Generating larger and slicing
does not help; at 128×128 it draws a cave floor *scene*, with boulders, so the slices disagree. And
`no_background: true` was ignored outright (1024 of 1024 pixels opaque), which is the **same defect
the change brief already recorded** for the spawn-burst VFX.

So: generate with the tool that gets the form right, then **remap luminance onto the biome's locked
ramp**. Pixel-exact, no resampling, no off-grid fringe — the same palette-swap-as-variant technique
ART_DIRECTION §68 already uses for the Deep Warden. The recipe lives in the change brief §22.3; the
ramps are in the harvest script's `RAMPS` table and none of them contains its biome's reserved
hazard accent.

**Floors and walls must take different bands of that ramp.** A plain luminance remap put
`Floor_Collapsed` at 78 luma and `Wall_Collapsed` at 83 — two obviously different source materials
landing five steps apart, and the wall ring vanished into the floor in play mode. Floors now map to
the lower band and walls to the upper, and walls additionally **stretch** their own range across it,
because one generated wall was so low-contrast that an absolute map parked all of it at the bottom
of the band (11 luma of separation). All five pairs now clear 19+.

### What was generated

| Set | Count | Cost |
|---|---|---|
| Wang tilesets → 1 floor + 1 wall each | 5 (3 Upper Caves, 1 Flooded, 1 Molten) | ~5 × 16–25 |
| Ground decals and debris, Upper Caves | **62 usable** from one 64-candidate pack | 20 |

62 decals for 20 generations is why `create_1_direction_object` at size ≤42 is the right tool for
these: it returns 64 candidates in one call. Two were rejected automatically — one blank, one at 86%
coverage, which would have hidden the tile under it rather than decorating it.

New committed tools, both reproducible from the art on disk rather than assembled by dragging:
- `Deeper/Generate Room Tiles` — imports every PNG under `Art/Environment/<biome>/` with
  ART_DIRECTION §1's settings and writes a matching `Tile` asset. **Collider type comes from the
  file name**: `Wall_*` collides, everything else does not, so the dangerous option has to be asked
  for. `Data/Tiles/` had no generator at all before this.
- `Deeper/Generate Room Themes` — one `RoomTheme` per floor/wall pair found. Each theme keeps its
  **own** floor and wall; mixing every tile into every theme would average them into one look, which
  is the opposite of the point.

### Verification

Played in `RoomLabScene`. Two rolls produced visibly different rooms: different wall texture
(stippled vs layered rock), different floor, different debris scatter. The wall ring reads as a wall
with its door gap; interior posts read as posts; decals sit on open floor and never on a doorway,
the entry band or a spawn marker. Console clean — 0 errors, 0 warnings. `TestScene` still plays
identically.

### The PixelLab budget gate

`.claude/hooks/pixellab-budget.mjs`, wired as a `PreToolUse` hook on `mcp__pixellab__.*` in
`.claude/settings.json`. It sums the generation cost of every call and asks for confirmation once the
session would pass 100, then raises its own ceiling by another 100 so approving once does not mean
being asked on every call afterwards. Read-only calls cost nothing. A hook rather than a skill
because it must fire automatically with nothing typed — a skill only loads when invoked.

Cost estimates are deliberately the documented **worst case** (a tileset counts 25, a 1-direction
object 40): over-counting is the safe direction for a spend gate. Verified by pipe-test — silent at
99, asks at 101 — and confirmed firing in the live session.

---

## Milestone 4, first half — the upgrade offer screen (owner-directed, 2026-08-25)

The owner asked for **the UI and the content, not the effects**: the offer screen, every upgrade and
Curse the docs name, authored with real icons, with no behavioural implementation behind them.

### Built

- [x] **`Scripts/Upgrades/UpgradePool.cs`** — the weighted draw. BALANCE §13's three biome rows as
      authored data; Legendary filtered out; already-taken excluded; prerequisites and mutual
      exclusions honoured.
- [x] **`Scripts/Upgrades/CursePool.cs`** — flat uniform draw, its own pool per §13.
- [x] **`Scripts/Upgrades/CurseDefinition.cs`** — upside and downside as two fields.
- [x] **`Scripts/Upgrades/RunCurses.cs`** — the run's Curses. Records only; applies nothing.
- [x] **`Scripts/UI/UpgradeCard.cs`** — the view. Draws an upgrade or a Curse.
- [x] **`Scripts/UI/UpgradeOffer.cs`** — the binder: queue, draw, present, pick, flash.
- [x] **`Scripts/UI/TierPalette.cs`** — ART_DIRECTION §5's colours, shared by the card and the strip.
- [x] **`Scripts/Core/RunPause.cs`** — refcounted `Time.timeScale` + action map + cursor.
- [x] **`Scripts/Editor/UpgradeCatalog.cs`** + **`BuildUpgradeAssets.cs`** — 38 upgrades and 8 Curses
      as a committed table, written to assets **in place** so GUIDs survive.
- [x] **`Scripts/Editor/HUDLayout.cs`** — `BuildRunHUD`'s primitives, extracted so two layout tools
      share one canvas contract.
- [x] **`Scripts/Editor/BuildUpgradePanel.cs`** — `Deeper/Build Upgrade Panel`.
- [x] **`HUD_Card` + `HUD_SlotIcon`** in `HUDFrameArt`, and **46 generated 128×128 icons**.
- [x] **The Secret Vault presents its Relic** through the panel (change brief §21.3.1's seam).
- [x] `WeaponDefinition.weaponUpgrades` — the Katana's 13 entries live on the weapon asset.

### Not built, and why

- **Every effect that is not a `StatModifier`.** Seven of 38 upgrades are expressible today; the rest
  and all eight Curses need damage-pipeline hooks. This was the finding recorded when the strip was
  built, and it is still the thing gating the second half of Milestone 4.
- **The Evolution milestone** (CORE_SYSTEMS §13). Its content is an open item in the design doc.
- **Reroll and skip.** In no doc.
- **Bow and Greatsword sub-pools.** Neither weapon exists.

### The draw picks a tier first, then an entry — not one weighted list

The obvious implementation weights every candidate by its tier's number and draws from that. It gets
BALANCE §13's published table wrong, and increasingly wrong as content lands: with 14 Commons and 2
Epics in the candidate set, per-entry weighting turns "Epic 5%" into 5×2 / (65×14 + 30×7 + 5×2) —
about **1%**. Choosing the tier first makes the published percentage the actual percentage regardless
of how many entries each tier happens to hold. A tier with no candidates left is skipped rather than
rolled and discarded, so a late run that has taken every Common still gets three cards.

### `RunPause` exists because three things must move together

Zeroing `Time.timeScale` is not a pause. Every player system reads its `InputAction` straight off the
shared asset and UGUI's EventSystem is nowhere in that path, so without disabling the Player map a
click on a card **also swings the katana** — `TestConfigHUD` found that first and this is its recipe.
And `PlayerAim` hides the hardware cursor, so a modal panel under it has buttons and nothing to click
them with.

It is **refcounted**, and that is why it is one object rather than a field on each panel.
`TestConfigHUD` had no refcount: with two panels open, the first to close re-enabled input underneath
the second. `TestConfigHUD` now routes through `RunPause` and that class of bug is gone.

### Two `HitStop` defects the pause exposed, both fixed

1. **An in-flight freeze un-paused the panel.** `HitStop.Run()` runs on *unscaled* time and
   unconditionally writes `Time.timeScale = normalScale` when it expires — about 50ms after any
   landed hit. This is not a corner case: **the killing blow that pays the XP is exactly the hit that
   freezes.** `RunPause.Push` now calls a new `HitStop.Cancel()` first. Verified in play mode: a
   0.5s freeze, a level-up in the same call, and `timeScale` still 0 long after the freeze would have
   ended.
2. **`HitStop.OnEnable`'s self-heal stomped a hard pause.** Its domain-reload guard was
   `timeScale <= frozenScale + ε`, and `0 <= 0.0201` is true, so any re-enable during a pause resumed
   the game. Now guarded with `timeScale > 0f`.
3. **A freeze starting AFTER the pause, from the same hit, was not covered by either fix above —
   found live, 2026-09-17.** The owner reported the level-up panel not actually pausing; two static
   reviews of `RunPause`/`HitStop`/`UpgradeOffer` found nothing wrong, because nothing *was* wrong in
   either class read alone — the bug is in the order two independent event chains run from one hit.
   `AttackHitbox.Sweep()` calls `target.TakeDamage()` before it fires `Landed`. On a killing blow,
   `TakeDamage` synchronously cascades `Damageable.Died` → `XPReward` → `PlayerXP.Add` →
   `UpgradeOffer.Open()` → `RunPause.Push()` (`Time.timeScale = 0`) — all before `TakeDamage` returns.
   Only afterward does `Sweep()` fire `Landed`, which reaches `AttackStateMachine.HandleLanded` →
   `HitStop.Freeze()` **for that same hit**. `Freeze()` unconditionally set `Time.timeScale =
   frozenScale` on the spot, and ~50ms later its coroutine unconditionally restored `normalScale = 1`
   — both writes landing *after* `Push()`, so the pause was overwritten and then, moments later, fully
   released while the panel sat on screen with `RunPause` still holding. `Push`'s existing
   `hitStop.Cancel()` call (item 1 above) does not help: it guards a freeze already in flight when the
   pause starts, not one that starts a moment later in the same call chain. Confirmed with a live
   repro that reordered the two calls exactly as combat does (`PlayerXP.Add` then `HitStop.Freeze()`)
   and read `Time.timeScale` back at each step: `0` → `0.02` → `1`, with `RunPause._holds` still `1`
   throughout. Fixed by giving `HitStop` a `RunPause` reference (found by `FindFirstObjectByType`,
   the same fallback `UpgradeOffer`/`TestConfigHUD` already use, since `RunPause` is a scene object
   and `HitStop` lives on the player prefab) and refusing to start a freeze at all while
   `pause.IsPaused` — re-verified with the same repro: `Time.timeScale` now reads `0` → `0` → `0`
   through the whole panel lifetime, including with the sandbox's debug menu stacked on top
   (`_holds` 2 → 1 → 0, released exactly once on the final pick), and ordinary (unpaused) hitstop is
   unaffected.

### Icon size is arithmetic, not preference

Icons are authored at **128** and drawn in a **64-unit** box inside `HUD_SlotIcon` (72 units, 4-unit
border). The canvas scales by a whole number from a 540 reference, so that box is 128 screen px at
1080p — a 1:1 draw — and 64 px in a short window, an exact half. The run HUD's strip draws the same
file in a **16-unit** box, an exact quarter. Every draw of these files is an integer ratio.

That last number moved: the strip's icon rect was 18 units, which would have resampled a 128px icon
by 0.5625 — precisely the non-integer resample the whole HUD contract exists to prevent. It is inset
one unit further now.

### The card is a fifth bigger than the first attempt, and the picture is why

The first `Card(152, 164, 4, 6)` composited over the real chrome read as **a thin outline with
nothing in it**: a 4-unit plate band around a 152-unit shape is a hairline, and `Field` — tuned to sit
over the play area — composited against the offer's own scrim to within a shade of it. Border 6, a
new `CardField` that is lighter and more opaque, and 168 × 196. The rivets moved from inset 2 to
inset 4 as well: the chamfer eats the outer corner pixels and `Rivet` only draws over existing ink,
so the studs were being silently dropped.

The icon was half the size it should have been in the same picture. Neither fault would have shown up
in any assertion.

### Text is vertically centred between the name and the cost line

Every card in the row is the same height, and that height is set by the worst case — a Curse with a
three-line cost. Top-aligning a two-line upgrade in that box left a third of the card visibly empty.
The body box is now much taller than its text and middle-aligned; the cost box hugs the bottom.

### Verification

Compiled clean, then run — twice, because the first pass's card sizing was wrong and only the render
showed it.

- Panel opens on `PlayerXP.LeveledUp`; `timeScale` 0, Player map disabled, cursor visible.
- **One `Add(400)` took level 2 → 7 and produced five offers in sequence, not one.** `PlayerXP.Add`
  loops its level check and raises `LeveledUp` five times synchronously; an unqueued panel would have
  opened five times in one frame and thrown four offers away.
- Picking closes the panel and returns `timeScale` to exactly 1 — never a sampled value.
- Stats move through the real panel: Vitality 100 → 115 Max HP, Fleet Foot 5 → 5.5 move speed, Heavy
  Hands 0 → 3 damage bonus. Behavioural picks change nothing, as intended.
- Draws are mixed-tier, exclude what the run already holds, and include Katana entries.
- Curse picks land in `RunCurses` and appear in the run's strip.
- Secret Vault: cleared, and the panel presented Endless Edge alone on a gold card.
- **Rendered at 1920×1080 (`scaleFactor` 2) and at the owner's 906×463 (`scaleFactor` 1)** — both
  whole numbers, both legible, the 726-unit card row fits the short window.

### Outstanding

- The behavioural half of the pool — the second half of Milestone 4, and what gates it.
- Evolution content (CORE_SYSTEMS §13 open item).
- Whether the Curse should appear on every level or every floor (change brief §11, still open).
- `Greed's Toll` has no downside and its card says so.
- The Bow's and Greatsword's sub-pools and relics.
- **Still no player death or run-end.** A pause now exists, but there is still no way for a run to
  finish.

### Two verification gotchas found this session

Both are in `01-VERIFICATION.md` now. In short: `Camera.Render()` does not draw the canvas under URP,
and `FindFirstObjectByType<Canvas>()` finds the debug menu's canvas as readily as the HUD's.

---

## Milestone 4 — the offer card goes icon-led (owner, 2026-09-17)

**Goal:** compress the card's tier word and full prose description into a tier pip badge, an
effect-category glyph and a short value/detail line, without touching a single mechanic or number —
presentation only, over content already authored for the prose pass above.

### Built

- [x] **`Scripts/Upgrades/EffectSummary.cs`** — the 12-value `UpgradeCategory` enum and the
      `EffectSummary` struct (`Category`, `Value`, `Detail`) every upgrade and Curse now carries.
- [x] **`UpgradeDefinition.summary`** / **`CurseDefinition.upsideSummary` + `.costLine`** — the
      Curse keeps its downside as a plain string rather than a second `EffectSummary`, so a Curse
      card never shows two competing numeric callouts.
- [x] **`UpgradeCatalog.cs`** tagged all 46 entries (Category/Value/Detail, plus a `CostLine` per
      Curse) and **`BuildUpgradeAssets.cs`** gained a text guard — every character checked against
      `PixelFontGlyphs.Order`, `Value` capped at 10 chars, the longest word in `Detail`/`CostLine`
      capped at 21, the line at 42 — reported alongside the existing `missingIcons` count.
- [x] **`Scripts/UI/UpgradeCardArt.cs`** — `CategoryGlyphs`/`TierBadges`, the two sprite lookups the
      card draws from, each an explicit `switch` with a commented `default` per the project's
      enum-ternary rule.
- [x] **`UpgradeCard.cs`** rebuilt: `tierLabel`/`bodyLabel` (`Text`) are gone, replaced by
      `tierBadge`/`categoryBadge` (`Image`) and `valueLabel`/`detailLabel` (`Text`), plus a
      Curse-only `costMark`. Both `Bind` overloads route through one `DrawSummary(EffectSummary)`.
      `TierPalette.NameOf` deleted — its only caller was the removed `tierLabel`.
- [x] **`HUDFrameArt.cs`** gained `HUD_TierCommon/Rare/Epic/Legendary/Curse` (32×8 pip badges, a
      crossed bar for Curse) and `HUD_CostMark` (24×8, rule + triangle) — drawn near-white so
      `UpgradeCard` tints each one through `TierPalette`, the same trick the bar fills use.
- [x] **12 generated `Cat_*` glyphs** in `Art/UI/Icons/`, 64×64, through `deeper-art` with the same
      forced palette as `Upg_*`/`Curse_*`. One reject on the first batch: "triple speed-chevron and
      dust puff" for `Cat_Dash` came back as a cartoon rally car — regenerated with an explicit
      "no vehicle, no car" clause, which returned a clean speed-streak glyph.
- [x] **`BuildUpgradePanel.cs`** lays out the new widgets per a fixed table (below) and wires both
      lookups via `WireCardArt`, keyed by field name == enum name == file name.
- [x] **`UpgradeStatusReport.cs`** gained a Category column on both the upgrade and Curse tables, so
      a mistagged entry (left at the enum's default) is something a reader can actually spot.

### The new layout, top edge down, 10-unit pad

**Superseded twice — see "the card's layout, to the owner's mockup" below for what is actually
built.** Kept because the arithmetic under it (why 196, why 32, why 64) is still the arithmetic.

| y | h | Object | Notes |
|---|---|---|---|
| 10 | 8 | `TierBadge` | 32×8, tier-tinted |
| 22 | 72 | icon slot | unchanged |
| 66 | 32 | `CategoryBadge` | 32×32, untinted, corner-pinned to the icon slot (below) |
| 98 | 11 | `Name` | unchanged, font 7 |
| 112 | 20 | `Value` | font 14 — 2× native, the only other size that stays on the pixel grid |
| 134 | 22 | `Detail` | font 7, wraps |
| 156 | 8 | `CostMark` | Curse only |
| 166 | 20 | `Cost` | Curse only, wraps |

186 + 10 bottom pad = 196 — the card's height is unchanged from the prose pass; nothing here made it
taller or shorter, only rearranged what fills it.

### Icon size is arithmetic, not preference — again, one size down

The same rule the unique icon follows (128px in a 64-unit box, an exact 2× at the canvas's own 2×
factor) governs the category glyph: authored at **64px** in a **32-unit** box, half the unique icon's
ratio in both dimensions. The glyph's box is not centred under the icon socket — it is pinned to the
socket's bottom-right corner, offset +50 from the card's own centre, which lands its left edge 2 units
into the socket's 4-unit steel frame and clear of the 64-unit hole. That 2-unit figure is checked
against `BuildUpgradePanel`'s own `SlotBorder`/`SlotSize` constants in the same file, the same
cross-tool-constant discipline `HUD_SlotIcon`'s border already uses against the unique icon.

### Verification

Compiled clean throughout (0 errors at every stage: after the data-model pass, after the 46-entry
catalog pass, after the layout/wiring pass). `Deeper/Build Upgrade Assets` logged **0 card-text
problems** across all 46 entries on the first run — the glyph-set and length guard caught nothing,
which means the catalog pass respected the HUD face's character set and the length budgets on the
first attempt. Rendered at both **1920×1080 (`scaleFactor` 2)** and the owner's **906×463
(`scaleFactor` 1)** via the corrected URP two-call recipe (`01-VERIFICATION.md` §11): tier pips read
as a count, the Curse card's crossed bar and cost mark are visibly distinct, no white boxes anywhere
(the null-sprite guard covers both new `Image`s), and the longest strings in the set — Overwhelm's
32-char detail, Greed's Toll's 41-char cost line, Gauge: Adrenal Rush's 19-char name — all wrap or fit
as measured. One render-harness-only defect found and fixed in the throwaway verify script, not the
product: `GrantCard` is active and unbound at build time until something calls `Bind`/`Clear` on it,
and a probe that binds the row's four cards directly (skipping `UpgradeOffer`) has to clear it too, or
it sits centred over the row as an empty frame. `UpgradeOffer.ShowOffer` already does this in the real
flow. Play-mode pick flow confirmed end to end: `TestControls.GrantLevel` → real cards drawn → an
upgrade pick lands in `RunUpgrades` (`Count` 0→1), a Curse pick lands in `RunCurses` (`Count` 0→1),
and `UpgradeOffer.PresentGrant` — the exact call `VaultReward.Grant` makes — presents and picks
cleanly too (`RunUpgrades.Count` 1→2). `RunPause.IsPaused` and `Time.timeScale` traced `true`/`0` →
`false`/`1` correctly across all three picks, confirming the pause fix documented above survives this
rebuild. Console stayed clear of `HUDLayout.Wire`/`Load` warnings throughout both scenes' rebuilds.

### The card's layout, to the owner's mockup (owner, 2026-09-18)

The owner supplied a card mockup and mapped its boxes onto the existing widgets. Presentation only —
no new widget, no content change, the card still 168×196.

- [x] **The category glyph and the tier badge now share one left-aligned top row.** The glyph heads
      it at the card's top-left corner; the badge sits beside it, vertically centred on the glyph's
      taller band. Both positions are derived in `BuildUpgradePanel` (`TierBadgeX`/`TierBadgeY`) from
      the two sizes and one `BadgeGap`, so resizing either badge keeps the pair on one row.
- [x] **`HUD_TierCommon/Rare/Epic/Legendary/Curse` re-emitted at 64×16**, twice the strip they were —
      at 32×8 the badge read as a sliver of chrome beside a shape four times its area. An `Image`
      stretches a sprite to its rect, so the size had to move in `HUDFrameArt` as well as in
      `BuildUpgradePanel`; the two are commented as one pair. `TierCross` gained a full-width rule
      through its X, because in the wider box the bare X was a small mark floating in empty space
      while Common's single pip filled the same box edge to edge.
- [x] **The description block is now the card's whole lower half and it wraps** — `Summary` grew from
      148×20 at y 136 to 148×48 at y 138, `HorizontalWrapMode.Wrap`. **This fixed a live defect:** the
      line did not wrap, and at 21 characters per line the seven authored entries over 30 characters
      were drawing roughly 30 units past *each* edge of the card. `CostMark` and `Cost` did not move —
      they sit inside that block, and the three rects overlap on purpose.
- [x] ~~**`UpgradeCard` chooses the font size against the block, not against one line.**~~
      **Superseded the same day — the card now has one size; see "One description size" below.**
      `titleCharBudget`
      / `titleWordBudget` were replaced by `titleLineChars` (10) and `titleLines` (2), both wired from
      `BuildUpgradePanel` so they cannot go stale, and `FitsAtTitleSize` runs two tests: the string
      must fit the lines it is allowed, **and** no single word may be wider than one of them — the
      block wraps at spaces only, so a word with no break in it overflows sideways however short the
      whole string is. An upgrade gets both lines and is centred in the block; a Curse gets the first
      line only and is pinned to the top, because the cost mark and cost line take the rest.
- [x] This **largely answers the flag the 2026-09-17 addendum in change brief §26 raised** — "nearly
      every entry renders at 7pt". With two title lines instead of one, "MAX HP +15" and
      "ALL ATTACKS +3" now render at 14. The long entries still drop to 7, which is what that size is
      for. All eight Curse upsides are 16 characters or more, so in practice every Curse renders small.

| y | h | Object | Notes |
|---|---|---|---|
| 8 | 38 | `CategorySlot` | 38×38 `HUD_SlotGlyph` socket, top-LEFT at x 8 (added later — see "A socket for the category glyph") |
| 11 | 32 | `CategoryBadge` | 32×32, untinted, inside the socket at x 11 (was a bare child of the card at 10, 10) |
| 19 | 16 | `TierBadge` | 64×16, tier-tinted, top-left at x 54 (was 50, 18) |
| 46 | 72 | icon slot | unchanged |
| 122 | 11 | `Name` | unchanged, font 7 |
| 138 | 48 | `Summary` | font 7 (was 14 or 7 — see "One description size"), **wraps**; centred on an upgrade, top-aligned on a Curse |
| 156 | 8 | `CostMark` | Curse only, inside the block above |
| 166 | 20 | `Cost` | Curse only, wraps, inside the block above |

**Verification.** Compiled clean at every stage. `execute_code` was BOM-broken for the whole session
(`01-VERIFICATION.md` §9), so the probe was a throwaway `Scripts/Editor/VerifyUpgradeCard.cs` driven by
menu items (§12) and deleted afterwards. Every rect read back off the built `Card0` matches the table
above. Rendered through §11's two-call URP recipe at the owner's **906×463 (`scaleFactor` 1)** and at
**1920×1080 (`scaleFactor` 2)**, with a sample offer bound deliberately to cover each case: Vitality
(10 chars, one title line), Heavy Hands (14, two title lines), Overwhelm (36, drops to 7 and wraps)
and the Curse Starving Blade (33, drops to 7, two lines, clear of its cost mark). Nothing overflows
the card at either size.

**Two harness notes worth keeping**, both of which produced a capture that looked like a broken
layout. §11's recipe leaves the canvas at its authored sorting, and a Screen Space - Camera canvas
sorts against sprites by **sorting layer**, not by z — at the default `Default/0` the room's walls
drew straight through the cards even with `planeDistance` at 1. Force `Overlay/999` for the capture
and restore it. And `planeDistance` itself has to be small: `Camera.main` sits at z −10, so the
recipe's 10 puts the canvas at exactly z 0, in the plane of the room sprites.

**Flagged, not fixed — a font quirk now visible on the card.** `PixelFontGlyphs` authors a real
lowercase `x` (for "2x damage taken", "3 x 3") while every other lowercase letter aliases onto its
capital. So `Max HP` renders as `MAx HP`, and at 14pt on the description block it reads as a typo.
Four authored strings hit it: Vitality's `Max HP`, Blood Debt's cost line, and the names Executioner
and Explosive Finish. Fixing it is a content edit (author those four with a capital X) rather than a
font change, since the lowercase glyph is deliberate — left for the owner to call. **Fixed the next
day, a better way — see below.**

### All 46 card lines re-authored (owner, 2026-09-18)

Owner-reported off the screenshots above: about twenty of the compressed lines did not read as
English. **The cause is the 2026-09-17 collapse, not the writing.** `UpgradeCard` draws
`Detail + " " + Value`, so the number always lands last; the pairs were authored for the two-line card
where a big `Value` sat above a small `Detail` and two numbers stayed apart. On one line they became
`Arcs within 3.0 4`, `To knockback Immune`, `Per hit, cap 5. Resets on a miss +2%`.

- [x] **One rule, no code change, recorded in `UpgradeCatalog`'s class doc.** Because the join can
      only put `Value` last: **one trailing number** keeps the split (`"Move speed"` + `"+10%"`);
      **two numbers, a word-shaped value, or a whole sentence** goes into `Detail` with `Value` left
      empty, which `DrawSummary` already handled and Flow State already shipped. That took the
      empty-`Value` entries from 1 to 22. The trailing number stays preferred wherever the line reads
      as English that way — leading with it is the fallback, not a new house style, because the
      owner's 2026-09-17 objection to "a number, then what it modified" still stands.
- [x] **21 entries rewritten, 25 confirmed and left byte-identical.** No number, mechanic, tier,
      category or pool changed; each rewrite was checked against that entry's own
      `description`/`upside`, which are untouched.
- [x] **Momentum Edge reads `Stack cap 10 to 14`**, not `10-14`, which was being read as a range
      rather than a cap raised from one value to the other. Still no arrow — the glyph set has none.
- [x] **`UpgradeCard.Caps`** uppercases the name, summary and cost labels at draw time, which closes
      the lowercase-`x` flag above in one place instead of four authored strings — authoring
      `EXecutioner` would have corrupted the string for every prose reader. **`ToUpperInvariant`,
      never `ToUpper`:** this machine's locale is Turkish, where `ToUpper` maps `i` to the dotted `İ`,
      which is not in `PixelFontGlyphs.Order` and would render as a hole in every word with an i.

**Verification.** `Deeper/Build Upgrade Assets` rebuilt all 46 with **0 card-text problems** — that is
the glyph-set and length guard, and it appends its count to the build log only when non-zero. A
read-only dump over the regenerated `.asset` files reproduced every rendered line with the font's
uppercasing and the 14pt/7pt decision, and all 46 were read against their source prose: nothing over
the 42-char `Detail` limit, no character outside the face's set, **12 entries now render at 14pt**
(up from almost none before the two-line block). Rendered through §11's two-call URP recipe at the
owner's **906×463** and at **1920×1080** with two sample bindings — one covering the capital-X fix
(`MAX HP +15`), the longest rewritten line (`4 DAMAGE ARCS TO ONE ENEMY WITHIN 3.0`) and a Curse cost
line carrying the multiplier (`2X DAMAGE TAKEN`); the other covering the worst case for the Curse
card, Starving Blade's 38-character upside, which wraps to two 7pt lines and clears the cost mark with
room to spare.

**Found while verifying, not fixed — the Curse cost mark has never drawn its art.** `HUDFrameArt`
generates `HUD_CostMark` (24×8, a rule with a triangle head), but `BuildUpgradePanel.cs:311` builds
that `Image` with a **null sprite** and nothing assigns one — `UpgradeCard.Bind(CurseDefinition)` only
sets `enabled` and `color`. UGUI draws a null-sprite `Image` as a solid quad, so every Curse card has
been showing a plain crimson bar. Same defect class as the white weapon slot in `01-VERIFICATION.md`
§4, and the same reason it went unnoticed: nothing is null, nothing errors, no assertion fails. The
fix is one argument — `HUDLayout.Load("HUD_CostMark")` in place of `null` — plus a panel rebuild in
`TestScene` and `RunScene`. Left for the owner, being outside this pass's scope.

### One description size, and a fit check that counts lines (owner, 2026-09-18)

Owner-reported off a screenshot of a live offer: Executioner's line drew at 14 beside three cards at
7 ("the font sizes are changing"), and Greed's Toll's cost line ran across the card's bottom border.

- [x] **One size.** `UpgradeCard` no longer touches `fontSize`; `titleFontSize`, `bodyFontSize`,
      `titleLineChars`, `titleLines` and `FitsAtTitleSize` are deleted, and `BuildUpgradePanel.Label`
      no longer takes a size, so every card text box is `HUDLayout.BodyText` by construction. 7, not
      14, because 14 is the only other on-grid size and holds 10 characters a line. The header
      ("LEVEL 2") stays at `TitleText` — it is not card text.
- [x] **`EffectSummary.Line`** is the Detail-then-Value join, moved off the card so the fit check
      measures the exact string that draws.
- [x] **The fit check counts wrapped lines, not characters.** `BuildUpgradeAssets.CheckCardText` used
      a flat 42-char limit as "two lines of 21"; Greed's Toll's 41-char cost line passed it and wrapped
      to three, because a word that misses the end of one line carries its whole length to the next.
      It now word-wraps at `BuildUpgradePanel.LineChars` (21) and compares against each box's budget —
      `SummaryLines` 5, `CurseUpsideLines` 2, `CostLines` 2, and one line for a name. Those budgets are
      derived from the card's rects and from `PixelFontArt.Advance`/`LineSpacing`/`LineInk` (now
      internal, and `m_LineSpacing` reads the same property), so resizing a box moves its budget.
- [x] **Greed's Toll's cost line** is `Cost pending: its Hazard was cut` (was
      `Cost pending - the Hazard it paid was cut`). Change brief §26, last addendum.
- [x] Panel rebuilt and saved in `TestScene` and `RunScene`; the stale serialized fields are gone.

**Verification.** Compiled clean. `Deeper/Build Upgrade Assets`: 0 problems. **Negative test:** with
the old Greed's Toll line put back, the same build reported `1 card-text problem(s)` — the check
catches the defect it exists for (the warning text itself did not come through `read_console`,
presumably the console's warning filter; the count is appended to the build log line either way).
A throwaway probe (§12 fallback — `execute_code` hit the BOM failure again) then ran all 46 entries
through **UGUI's own `TextGenerator`** on the real built card in play mode: every line at size 7,
**0 over budget**, and each line count matched the fit check's arithmetic exactly. Rendered at the
owner's **906×463 (`scaleFactor` 1)** and **1920×1080 (`scaleFactor` 2)**: the screenshot's own offer
(Executioner, Windcutter, Momentum, Greed's Toll), the longest upgrade line (Blink Strike, 3 lines),
the longest name (Gauge: Adrenal Rush), 2-line cost lines (Blood Debt) and a 2-line Curse upside
(Starving Blade, which clears its cost mark). All text inside the frames, one size throughout.

### A socket for the category glyph (owner, 2026-09-18)

Owner, off a screenshot of the Quickstep card: the top-left glyph is "a bit naked" — it sat bare on
the card field while the icon under it has a steel slot.

- [x] **`HUD_SlotGlyph`, drawn by `HUDFrameArt`: `Slot(38, 3, 2, false)`.** Border 3 makes the hole
      exactly 32, the `Cat_*` glyphs' 64px at the canvas's 2x — `HUD_SlotIcon`'s arithmetic at half
      size. Drawn rather than generated because it is chrome (CLAUDE.md's material/geometry split).
      Not `HUD_SlotSquare`, which has the same 32 hole but at 40 units touches the card's border or
      the icon slot wherever it goes. No rivets: at border 3 a stud lands on the channel wall.
- [x] **`BuildUpgradePanel` builds it the way it builds the icon slot** — a `CategorySlot` holding a
      `Socket` plate, the `CategoryBadge` glyph inset by the border, and a `SlotFrame` over both — at
      `CategorySlotInset` 8, which leaves a unit of field inside the card's border and ends 2 units
      short of the icon slot at x 48. The tier badge's position is still derived, now off the socket
      (54, 19). The two sockets share one `SocketColour`. `UpgradeCard` is untouched: the socket is
      static chrome, and the `categoryBadge` wiring still points at the glyph.

**Verification.** Offline mockup first, off the real card PNGs: a 36-unit, 2-border variant was too
faint to count as a background and was dropped. Then compiled clean; `Deeper/Generate HUD Frames`
left all 33 existing frame PNGs byte-identical (hashed before and after) and added only
`HUD_SlotGlyph`; the panel was rebuilt and saved in `TestScene` and `RunScene` (the frame is referenced
5 times in each, one per card). A throwaway probe read `Card0`'s rects back as designed — socket
(8, 8) 38×38, glyph 32×32, tier badge (54, 19) — and rendered Quickstep, Vitality, Gauge: Adrenal
Rush and Greed's Toll at **906×463 (`scaleFactor` 1)** and **1920×1080 (`scaleFactor` 2)**: crisp on
the pixel grid, clear of the card border and the icon slot on every card, the Curse card included.

---

## The Hub — surface camp, weapon select and descend (owner-directed, 2026-09-07)

**Goal:** the place a run starts from. GDD §Game Loop 1 calls it "a small surface camp"; this builds
it as a walkable isometric scene rather than a menu, because the owner asked for one and because a
menu would make the tile and prop work pointless.

**Scope built:** the camp, the weapon rack (working), the mine shaft (working), the stat shrine
(placed, stubbed). Shards, the Hub Stat System and the Relic Vault are still Milestone 6.

### Files

- `Scripts/Hub/HubStation.cs` — "she is standing here and pressed E". One class for every fixture;
  what a station *does* belongs to whatever subscribes to its `Used` event.
- `Scripts/Hub/HubDescent.cs` — the shaft. Refuses to descend with no weapon and says so.
- `Scripts/Hub/HubNotice.cs` — a fixture that is placed but unbuilt, answering "not yet".
- `Scripts/Run/RunConfig.cs` — the chosen weapon, as an asset, so it survives the load into the run.
- `Scripts/UI/HubPromptHUD.cs`, `WeaponSelectPanel.cs`, `WeaponCard.cs`.
- `Scripts/Editor/Layout_Hub_01.cs` — the camp's authored ASCII map.
- `Scripts/Editor/BuildHubScene.cs` — builds `Scenes/HubScene.unity` (`Deeper/Build Hub Scene`).
- `Scripts/Editor/BuildHubArt.cs` — imports the camp art (`Deeper/Generate Hub Art`).
- `Scripts/Editor/BuildRunConfig.cs` — writes `Data/Run/RunConfig.asset` (`Deeper/Build Run Config`).
- `Art/Environment/SurfaceCamp/` — 5 floor tiles, 1 wall block, 7 fixture sprites, via PixelLab.

**Order:** Generate Hub Art → Generate Isometric Tiles → Generate Hub Markers → Import HUD Icons →
Build Run Config → Build Shard Bank → Build Hub Scene. Only the last has to be last; it reads what
all the others write, and warns rather than failing when one has not been run.

### What was reused, and the one thing that was deliberately not

`RoomLayout` supplies the isometric projection, the wall painting and the marker lookup, so the
camp's fixtures land on the same diamonds its tilemap draws. `Validate` grew an `extraLegend`
overload rather than having the camp's characters added to the shared legend — sharing the *rules*,
not the vocabulary of one map.

`IsometricRoomView` is **not** used. Its whole purpose is re-rolling a pooled room's visuals on every
mount, and the camp is the one place in the game that must look identical every visit. It is also
load bearing: a station's trigger volume has to sit exactly where its art is drawn, which a random
scatter cannot promise.

### An isometric FLOOR tile must carry no side wall — binding on all future tile art

The camp's first three builds came out as a **field of raised blocks with every slab edge showing**.
This is worth reading before generating any isometric tile, because two plausible causes were
investigated and both were wrong:

1. *Not* `TilemapRenderer.mode`. The built renderer read back `mode: 1` (Individual) — the value the
   room builders' comments blame this symptom on.
2. *Not* `TilemapRenderer.sortOrder`. It read back `TopRight`, and flipping it to `BottomLeft`
   changed **nothing on screen**. It was never being applied: `Assets/Settings/Renderer2D.asset` has
   `m_TransparencySortMode: 0` (Default), which under an orthographic camera sorts by Z. Every tile
   sits at z = 0, so they all tie and per-tile order inside a Tilemap is undefined.

The cause was the art. Profiling the PNG row by row: a generated tile is a small 3D block — a
diamond top face followed by a **20-row full-width side wall**. Cells step 32px apart vertically, so
the tile in front covers only ~6 of those rows and 14px of wall shows on every cell. PixelLab's
`tile_shape` is the control:

- **Floors: `"thin tile"`, then strip the wall entirely.** `BuildHubArt.FlattenFloorTiles` crops each
  `Iso/Floor_*.png` to its bare top-face diamond. Nothing ever sees the underside of the ground, and
  with no wall there is nothing to reveal — a far more durable fix than persuading the renderer to
  sort. It is idempotent, so it is safe to re-run.
- **Walls: keep the block.** A wall is *meant* to read as raised, and `Wall_Stone` renders correctly.
- `"thick tile"` and `"block"` also produce a top face that is ~35 rows tall against a 32px lattice —
  an irregular ~3.5px/row slope where a true 2:1 diamond is exactly 4px/row. `"thin tile"` measured
  as an exact 2:1. **Measure a new tile's row profile before trusting it.**

The crop leaves a **two-pixel skirt of the diamond's own edge colour**. An exact diamond tapers to
nothing at each vertex, so neighbours meet at a point rather than overlapping and the background
showed through as a speck on every tile. The skirt must copy edge colour, not the source's wall
pixels — with the draw order undefined, a wall-coloured skirt would sometimes paint a dark fringe
over the neighbour.

### The shipped biome tiles have the same defect

`Art/Environment/UpperCaves/Iso/*.png` profile identically to the rejected camp tiles (opaque rows
12–63, widest row ~32, full-width band below it). Every room in the game is drawing floors with a
20px side wall on a 32px lattice. This was not touched — it is a re-generate of five shipped tiles
plus a re-run of the flatten, and it is the owner's call.

### The world has no pixel-perfect camera

`Prefabs/Rig/Main Camera.prefab` is orthographic size 8 with no `PixelPerfectCamera` anywhere in the
project. At a 768px viewport that renders the world at **1.5×**, and at 1080p at **2.109×** — a
fractional zoom, which resamples point-filtered art off its own grid exactly as
`PixelPerfectHUDScale` exists to prevent for the HUD. This is the world-space twin of a lesson the
project has already learned once and is a plausible root cause of "the game looks blurry". Not
changed here: it alters framing in every scene, so it is a project-wide decision.

### Descend goes to `TestScene`, because there is no run scene

`Scripts/Run/FloorLoader.cs` is written and `Data/Rooms/Pool_UpperCaves.asset` exists, but the loader
is **mounted in no scene** — there is no scene that plays a run. `HubDescent.runScene` is a serialized
scene *name* (never a build index, which renumbers silently), so pointing it at a real run scene when
one exists is a field change, not a code change.

### Verified

Built clean with no wiring warnings — every `HUDLayout.Wire` call found its field, so the panel, the
prompt, the shaft and the shrine are all wired rather than falling back at runtime. Play mode enters
with **zero errors and zero warnings** and the camp renders with the player in it and the night
ambient applied.

**`HubStation`'s detection is verified**, by the trick below. **The keypress is not.** E cannot be
pressed from here — simulated key input never reaches play mode (`01-VERIFICATION.md` §2) — and
`execute_code` was broken this session, so `WeaponSelectPanel.Open()` and `HubDescent.Descend()`
could not be driven either. Both carry `[ContextMenu]` attributes for exactly this and can be
right-clicked in the Inspector. **Treat what happens after the key as unverified until someone plays
it**; the project's standing lesson is that structurally-checked code is not working code.

### Interaction markers — indicating which fixtures are usable (owner-directed, same pass)

The camp answered "press E" but never "this is usable at all", so the rack was indistinguishable
from the crates until you walked into it. Two layers were added, both scoped to that one question:

- `Scripts/Hub/StationMarker.cs` — a floating mark above each usable fixture: a dim chevron from
  across the camp, brightening into an **E keycap** in range. A world `SpriteRenderer`, not a
  world-space canvas: the HUD is authored at half size under `PixelPerfectHUDScale`'s whole-number
  factor, and a UGUI element out here would fight that scale for nothing.
- `Scripts/Editor/HubMarkerArt.cs` — draws both sprites (`Deeper/Generate Hub Markers`). **Drawn, not
  generated**, by CLAUDE.md's own split: a chevron and a keycap are pure symbols with no material to
  interpret. The E is read straight out of `PixelFontGlyphs`, so the key on screen is the same
  letterform as the prompt line rather than two people's idea of an E.
- The lantern posts moved in `Layout_Hub_01`. They used to be free scenery, which made the camp lie —
  lit things you could use, lit things you could not. One now stands beside each station and nowhere
  else, so "lit means usable" is true by construction. Costs no new art.

The marker is **unlit** (`Sprites-Default`), so the night ambient does not tint it blue along with
the world; it is an interface element that happens to live in the world.

**Its height is measured from the art's opaque top, not its canvas** — `BuildHubScene.ContentHeight`
reads the PNG's bytes. The first build used the canvas height and the shaft's marker floated clean
out of frame, because generated props are centred on a square canvas with whatever transparent
padding that leaves. Measuring means a new fixture needs no new number.

**Verification, and the trick worth reusing.** Simulated input is unusable and `execute_code` was
down, but the *spawn point is authored data* — moving `P` next to the rack in `Layout_Hub_01`,
rebuilding and playing put her in range deterministically, through the real spawn path. The marker
swapped to the keycap while the shaft's stayed a chevron. That confirms `HubStation`'s
`OnTriggerStay2D` filter and its unscaled presence window both work in play mode. `P` was moved back
afterwards. **Editing authored layout data is a usable substitute for a probe in a session with no
scripting.**

### The Shard counter — GDD §UI's first Hub Screen item (owner-directed, 2026-09-08)

The camp had no Shard readout at all. Added:

- `Scripts/Meta/ShardBank.cs` — the balance, as a `ScriptableObject`, with `Add` / `TrySpend` and a
  `BalanceChanged` event. **The first file in `Scripts/Meta/`**, which until now was one of the
  folders deliberately not scaffolded ahead of need.
- `Scripts/UI/ShardCounterHUD.cs` — the readout. Subscribes rather than polling in Update, unlike the
  run HUD's bars: those read values that change every frame, this reads one that changes a handful of
  times per session.
- `Scripts/Editor/BuildShardBank.cs` — writes `Data/Meta/ShardBank.asset` (`Deeper/Build Shard Bank`).
  **Re-running never touches the balance**; it is one of only two assets a running game writes to.
- `Art/UI/HUD_IconShard.png` — a faceted violet crystal, generated through the documented icon recipe
  (`create_image_pixflux`, 64×64, `UI_IconPalette.png` forced as `color_image_base64`). The violet is
  `168,115,219` — the Epic tier colour the game already ships, chosen because it is emphatically not
  one of ART_DIRECTION §2's three reserved hazard accents.
- `Scripts/Editor/ImportHUDIcons.cs` — applies the sprite contract to every `Art/UI/HUD_Icon*.png`
  (`Deeper/Import HUD Icons`).

**`Art/UI/` had no importer, and that was a live trap.** `HUDFrameArt` sets the contract on the chrome
it draws and `ImportUpgradeIcons` covers `Art/UI/Icons/`, but the weapon and dash icons between them
were imported by hand in some earlier pass. Any new icon dropped in that folder would have arrived at
Unity's defaults — 100 PPU, bilinear — and read as a blurry half-size smudge in its slot. The new tool
covers the whole `HUD_Icon*` set rather than the one file that prompted it, and warns (rather than
rescaling) when an icon is not 64×64, because rescaling point-filtered art off its grid is the defect
and not the cure.

**Why 64×64 specifically:** the HUD draws an icon into a 32-unit authored square and the canvas scales
by 2 at the 1080p reference, so a 64px source lands 1:1 on screen. The 128px upgrade icons are a
different case — they are drawn much larger on the offer cards.

**Hub-only, deliberately.** ART_DIRECTION §5's in-run HUD has no Shard row, and rightly: Shards are
awarded once at run end and spent between runs, so a counter during a descent would be a number that
cannot change, watched by a player who cannot spend it. It is built by `BuildHubScene` onto the Hub's
canvas and exists nowhere else.

**What this is not.** It is a seam, not a save file. Milestone 6 owns `Scripts/Meta/SaveData.cs`,
which will hold Shards alongside Hub stat ranks, Weapon Mastery counters and discovered Relics, and
will actually persist; when it arrives it should *back* `ShardBank` rather than replace it, since the
run-end award and the shrine's purchases will already be calling `Add` and `TrySpend`. Today the
balance survives an editor session and is discarded in a build, the same ScriptableObject behaviour
`RunConfig` relies on. **Nothing awards Shards** — BALANCE §14's formula has no run end to run at — so
the asset ships at 0 and carries a `Grant 250 Shards` context menu so the counter can be seen working.

**Verified — and the method is worth reusing, because a Screen Space Overlay canvas cannot be
screenshotted here.** `manage_camera` renders through a camera, which by definition excludes Overlay
canvases; switching the canvas to Screen Space - Camera to work around that failed too, because the
bridge cannot set an object-reference property (`worldCamera`) at all. And `execute_code` is still
broken, so the documented RenderTexture probe was unavailable.

What worked instead: **read the label's text back out of the live scene.** With the bank's balance
temporarily set to 1250 by editing `ShardBank.asset` directly, play mode, then
`mcpforunity://scene/gameobject/{id}/components` on the `Amount` object, which reports
`m_Text: "1,250"`. That single value proves four things at once — the counter is genuinely wired to
the asset rather than falling back, `OnEnable` draws on the way in and not only on a change, the `N0`
invariant format produces the comma, and **the hand-authored 5×7 bitmap face actually has a comma
glyph and renders it** (`characterCountVisible: 5`, 20 verts = 5 quads), which was a real risk worth
closing. The reported glyph positions also place the number at group-x 54.5–89 against a slot ending
at 40, so the layout does not collide. The balance was set back to 0 afterwards.

Generalising: **reading a component's properties back is the substitute for a screenshot when the
thing you need to check is a value rather than an appearance**, and it is far cheaper than a picture.
Pair it with an offline composite (the icon was checked against `HUD_SlotSquare` at true 1:1 scale in
the scratchpad) when appearance matters too.

### Hub polish pass — walls, colliders, sky, and the shaft split (owner-directed, 2026-09-08)

Five owner-reported defects, and what each turned out to be.

**Walls were half buried.** `Wall_Stone.png` is a 64px block whose top face is a diamond centred on
row 19, with 28px of body below it — so its *bottom* face centres on row 47. The sprite imports with
a Center pivot (row 32), so drawing it at the plain cell centre sank 15px of wall under the floor and
left 13px standing. Fixed by lifting every block `15/32` of a unit and stacking `WallCourses = 2`,
which puts the top at 1.75 units against a 1.5-unit player. The second course is the same sprite
offset by its own height, so no second piece of art was needed. **Both courses sort from the cell,
not from where they are drawn** — a raised course is not further back, and sorting it by its own y
would let the upper course of a near wall lose to the lower course of the wall behind it.

That in turn made a new layout rule: **nothing stands within two cells of the wall.** At 1.75 units
the wall is now tall enough to swallow a fixture placed against it, and the stat shrine — one cell
from the east wall — was half buried in stonework. It moved inboard.

**Colliders were all one size.** `AddBlocker` used a shared 1.4 x 0.7 box, argued for on the grounds
that a per-prop number is a per-prop thing to get wrong. That was simply wrong: measured footprints
run from a 1.0-unit lantern post to a 4.75-unit mine shaft, so the big fixtures were walk-through and
the small ones were surrounded by invisible wall. Both the blocker and the station's reach are now
derived from `ContentWidth` of the fixture's own art, so they cannot drift apart — **and that
coupling is load bearing**: with reach left at a fixed 1.6 while the blocker grew to match the art,
the shaft's blocker would have held her further away than she could reach and pressing E at the
shaft would never have worked.

**The background was black.** Now a deep desaturated indigo on the Hub's *camera instance* — not the
shared rig prefab, because a cave has no sky. Note for future screenshots: `manage_camera` with
`view_position` renders through a throwaway camera that does **not** inherit the clear colour, so a
positioned capture shows white and looks like the setting failed. Capture the real camera to check it.

**The tent was inert and oversized.** At 3.6 x 3.5 units it was the largest thing in the camp and did
nothing. It is now the **Codex** station — CORE_SYSTEMS §15 banks Memory Fragments into a Hub Codex
and MVP lists a Codex UI stub — carrying the same `HubNotice` treatment as the shrine. Recorded in
the change brief as a placement decision.

**One sprite could not express the mine shaft.** A sprite gets one sorting order, so the player was
always entirely in front of the headframe or entirely behind it, and descending requires being both
at once. `BuildHubArt.SplitShaft` cuts `Prop_Headframe.png` at row 128 into `Prop_ShaftBack` (tower,
platform, pit) and `Prop_ShaftFront` (the brick lip), **both keeping the full 160x160 canvas** so they
align by construction with no offsets to maintain. The lip sorts 1.6 units nearer the camera than its
own cell, which is what puts her inside the structure.

**A false start worth recording:** the first attempt offset the shaft's *art* back by 1.6 units so
the marked cell would be the pit. That pushed the whole structure into the south wall. The art was
already correct — only the point she climbs down at was wrong — so the offset moved to a `Mouth`
child object (verified at world (5, 6.6) against a cell centre of (5, 5.0)). **Move the target, not
the art.**

#### A bug in the flatten tool, found by its own log

`Generate Hub Art` re-flattened tiles that were already flat, cropping them a second time. The
idempotence check asked whether the row below the diamond's axis was full width — but the 2px skirt
the tool itself adds makes that row full width too, so a finished tile was indistinguishable from an
unprocessed block. It now measures the silhouette's **height** instead, which the skirt cannot fool:
a bare diamond is ~32 rows, a block 50+. The affected tiles were restored from source and reflattened
once. **Symptom to recognise: a "generated" log line naming a diamond axis that has moved since the
last run.**

**Verified:** compiles clean, builds clean with 4 stations and no wiring warnings, play mode raises
no errors, and the camp was composited offline at full size to judge the art — the editor's Game view
is currently 900x239, which is far too short to review an isometric scene in.

**Not verified: the descent animation itself.** It cannot be triggered without pressing E, and
`execute_code` is still down. `HubDescent.Descend()` carries a `[ContextMenu]` for exactly this.

#### The background, second attempt — a flat colour was never going to work

The first pass set the camera's clear colour to a near-black indigo and called it done. The owner's
reply was that it looked the same, and they were right twice over: (0.055, 0.062, 0.11) is
indistinguishable from black, **and a flat colour is the wrong answer regardless**. The camp is a
14-unit diamond and the Game view is currently ~60 world units across, so most of the screen is that
colour and the camp reads as a cut-out pasted onto nothing.

`PaintSurround` fixes it with ground instead: 22 cells of unlit moorland past the walls, fading out
so the camp sits on a hillside at night. No new art — it reuses the camp's own grass tiles, tinted
dark. The sky colour was also lifted to (0.09, 0.105, 0.185) so it reads as sky rather than as void.

**The fade is one tilemap per band, and that shape is forced by a Unity behaviour worth knowing.**
The obvious implementation is one tilemap with `SetTileFlags(pos, TileFlags.None)` followed by
`SetColor` per cell. It looks perfect in the editor and **silently loses every colour the moment the
scene reloads into play mode**, because a `Tile` asset rewrites both colour and flags from itself in
`GetTileData` on every refresh. What that looked like was a full-bright green field covering the
screen. `Tilemap.color` is a serialized property of the component and survives, so the bands are six
real tilemaps instead.

**A wrong diagnosis, recorded because it nearly cost a lot of time.** That full-bright field made the
whole frame look as though 2D lighting had stopped working in play mode, and the next move would have
been to go digging through `Renderer2D.asset` and the light's target sorting layers. It was measured
instead: raw grass albedo is R156 G162 B57 and the same grass in play mode reads R133 G141 B65 — a
blue shift, so the ambient *is* applied. Naive gamma maths predicts R86, which is what made it look
unlit; the project renders in **linear colour space**, where a 0.55 multiply lands far brighter than
0.55 of the 8-bit value. **Sample the pixels before concluding a light is off** — comparing two
screenshots by eye is worthless when one of them has a bright new object filling half the frame.

#### Colliders and the mine shaft, second pass (owner-directed, 2026-09-08)

**The colliders were wrong twice over, and deriving them from the art's width only fixed half of it.**

1. *Vertical placement.* A prop pivots at its **canvas** bottom, but a generated sprite is centred on
   a square canvas and carries transparent padding underneath — 13px on the shrine, which is 0.4
   world units of daylight under a stone obelisk. Every fixture is now dropped by its own measured
   `Bottom` so the drawn pixels touch the cell.
2. *Where the footprint actually is.* For an isometric prop the lowest drawn pixel is the **bottom
   vertex** of its base diamond, not the diamond's centre. A collider centred on the pivot therefore
   sat half a diamond in front of the thing it was meant to be inside. `FootprintCentre` puts it at
   `Bottom + Width/4`, and the station's trigger uses the same point.

`Measure` now returns Top/Bottom/Width in one pass and **everything positional derives from it** —
planting, collision, reach and marker height — so those four cannot drift and a new prop still needs
no hand-tuned numbers. Verified numerically: the shaft's blocker lands at world (5, 5.906) against a
cell centre of (5, 5.0), i.e. centred on the hole rather than in front of it.

**The mine shaft is two pieces of art now, not one sprite cut in half.** The previous attempt sliced
the shipped headframe at row 128 into "back" and "front lip". That could not work and the owner
called it: the tower's legs run down *through* the platform, so no horizontal cut separates the
structure from the ground it stands on — the split has to be **semantic**, ground and top.

- `Prop_ShaftPit` — a flat hole with a stone rim, drawn on the **Default** layer at
  `GroundPieceSortingOrder`, under every actor. She walks over it.
- `Prop_ShaftTower` — the open timber headframe, drawn on **Actors** and Y-sorted at the same cell,
  so she passes between its legs: in front from the south, behind from the north.

The `Fixture` table gained `Ground` and `Companion` to express this generally rather than
special-casing the shaft in code. The pit owns the footprint — collider, reach and the descent
`Mouth` all derive from its bounds — because a hole is what stops you and what you climb into; the
tower carries no collider, since two colliders on one fixture would disagree.

**Prompt note for regenerating these:** `view: "low top-down"` gave the pit raised brick walls, a box
rather than a hole. `view: "high top-down"` produced the flat, flush opening the ground layer needs.
Standing structures want `low top-down`; ground features want `high top-down`.

**Two false trails, both from positioned screenshots.** `manage_camera` with `view_position` renders
through a throwaway camera that inherits neither the clear colour nor the 2D lighting, so the moor
appeared as bright grey slabs and the sky as white. Both were correct all along — confirmed by
capturing the real camera in play mode. **Judge lighting and background only from a play-mode capture
of the actual camera.** Also worth knowing: the editor can be left in play mode by an earlier call,
and menu items then run inside it and log nothing useful — check `mcpforunity://editor/state` for
`is_playing` when a menu item produces no output.

#### Shaft alignment, and "beside" in an isometric map (owner-directed, 2026-09-08)

**The tower stood at the mouth of the hole, not over it.** Both shaft pieces bottom-pivot on their
own lowest drawn pixel, and for the pit that pixel is the *front vertex* of the opening — so aligning
the two by their baselines planted the tower on the near lip. `AddCompanion` now stands it at the
ground piece's footprint centre (`Width/4`, the same measurement the colliders use), which is what
puts a tower over a hole rather than in front of one.

The mark above it is measured from the companion too. Taking it from the ground piece alone put the
shaft's keycap inside the tower's roof; it now clears the tower's real top by the authored gap
(verified: marker at world y 11.47 against a tower top of 11.16).

**"Beside" is a diagonal step in this map, not an adjacent character.** Screen position is `x - y`
across and `x + y` back, so the cell level with and one step to the side of `(x, y)` is
`(x + 1, y - 1)`. A neighbour in the map *string* is diagonal on screen and reads as standing in
front — which is exactly how the shrine's lantern looked. Every lantern is now on that diagonal from
the station it lights.

**Spawn placement is now a constraint, not a free choice.** Station reach is derived per fixture, so
the shaft's is 2.65 units and the Codex tent's 2.6 where everything used to be 1.6. Two successive
spawns landed inside one of those and she arrived with a keycap already showing. `P` is at (6, 7),
whose nearest station is the rack at 3.2 units. **Anything moved in this layout has to be re-checked
against all four reaches**, because they are no longer the same size as each other.

**Shaft moved to (7, 2), against the south wall (owner-directed).** Safe to sit there now only
because the art offset is gone: the earlier version placed the sprite 1.6 units back, which is what
drove the structure into the wall and prompted moving it inboard in the first place. With the tower
standing on the pit's footprint centre instead, the pit's own bottom edge lands on its cell and the
wall tops are ~2.3 units clear.

**A stale character found while doing it.** `GroundFor` still tested `symbol == 't'` for "this cell is
grass" — the tent's old scenery letter, left behind when it became the Codex station `C`. The tent had
therefore been standing on bare dirt instead of its grass shelf ever since. Nothing errored and no
warning fired; the map simply painted the wrong ground under one fixture. **When a legend character is
renamed, grep for the old letter** — the layout file validates its own map, but code that switches on
those characters is not covered by anything.

---

## The run — right-sized rooms, a sixteen-floor descent and an ending (owner-directed, 2026-09-08)

**Status:** 🟢 Built and **verified in play mode**. Console clean throughout — 0 errors, 0 warnings.

The owner's two notes were "rooms are a lot big… we only use first half of the room" and "there is
only one room now — when that room is cleared the game doesn't mean anything; we want a fully
functional run". Placeholders for all bosses, and the floor system built so the room types that do
not exist yet drop in rather than forcing a rewrite.

**Closes four items this document has carried, one of them the oldest in it:**

- ~~**`FloorLoader` has no scene.**~~ `RunScene` exists and `Deeper/Build Run Scene` builds it.
- ~~**No Mini-Boss room exists.**~~ `MiniBossArena_01`, plus both Floor 16 arenas.
- ~~**Only 2 layouts in the pool.**~~ Seven: LEVEL_DESIGN §2's full 6 Combat + 1 Wave.
- ~~**Still no player death / run-end.**~~ `PlayerDeath`, `RunEnd` and `RunSummaryPanel`.

### Built

- [x] `Scripts/Run/RoomRole.cs` — Combat / MiniBoss / FinalBoss / TrueFinalBoss / SecretVault / TrappedSoul
- [x] `Scripts/Run/RunPlan.cs` + `Data/Run/RunPlan.asset` — which biome each floor draws from
- [x] `Scripts/Run/RunEnd.cs` — both endings, BALANCE §14's award, the Shard payout
- [x] `Scripts/Player/PlayerDeath.cs` — on `Player.prefab/Combat`
- [x] `Scripts/UI/RunSummaryPanel.cs` + `Scripts/Editor/BuildRunSummaryPanel.cs`
- [x] `Scripts/Editor/BuildRunScene.cs` + `Scenes/RunScene.unity`
- [x] `Scripts/Editor/BuildRunContent.cs` — 3 biome pools, the run plan, 5 placeholder bosses
- [x] `Layout_UpperCaves_03..07`, `Layout_MiniBossArena_01`, `Layout_FinalBossArena_01/_02`
- [x] `Layout_UpperCaves_01`, `_02` and `Layout_SecretVault_01` resized
- [x] `Deeper/Build All Room Prefabs` — eleven rooms in one press
- [x] `CameraRig.SnapToTarget()`; `CharacterState.Death` art fallback; `HubDescent` → `RunScene`

### Room size is arithmetic, not taste

An isometric `w × h` map draws a diamond **`(w+h)` wide by `(w+h)/2` tall** — the footprint depends
only on the *sum*. The camera is ortho 8, so it shows **28.4 × 16** world units at 16:9. Combat
Room 01 was 28×16 cells = **44 × 22 units**, a room and a half wide; the Wave Room was 50 × 25.

Sizes are now chosen per room from what is in it. Measured off the built prefabs:

| Room | cells | world | |
|---|---|---|---|
| Combat ×6 | 16 × 10 | 26.0 × 13.0 | fits the view |
| Wave Room | 20 × 12 | 32.0 × 16.0 | overflows, on purpose (LEVEL_DESIGN §4) |
| Secret Vault | 16 × 10 | 26.0 × 13.0 | fits |
| Mini-Boss arena | 20 × 14 | 34.0 × 17.0 | §6's "large open arena" |
| Final Boss arenas | 22 × 16 | 38.0 × 19.0 | the largest rooms in the game |

**No design doc specifies a room dimension**, so this is tuning rather than a design change. Change
brief §25.

The entry band also moved. It used to sit on the map's half-way line, because aggro radii are 10–12
and a lock sprung at the doorway left the far half standing still. At 26 units across the whole room
is inside that radius, so the band is now four to five cells inside the west door — leaving it at the
middle of a room this size would spring the fight with two thirds of the floor behind her.

### Three defects the pass found, all pre-existing

**1. A diagonal band's bounding box is not the band.** `BuildEntry` sized the entry trigger to the
`=` cells' *axis-aligned* bounds. On a diamond grid a column of cells is a 2:1 diagonal, so that box
is a rectangle roughly twice the band's area reaching into both halves of the room. At 16×10 it
swallowed the player start: the run mounted its first room, `RoomEntry.OnTriggerStay2D` fired on the
frame she was placed on the arrival marker, six enemies arrived on top of her, and **she died
without a key being pressed**. The old 28×16 layout cleared it by half a tile, which is the only
reason it had never shown up — and is also why the fight there sprang earlier than the map looked
like it should, which is a large part of what "we only use first half of the room" was describing.
Now a `PolygonCollider2D` on the band's true parallelogram, derived from the same basis vectors
`RoomLayout.CellCentre` uses.

**2. A single-doorway room reported an exit.** `BuildConnection` split doors on the room's projected
mid-line and then fell back to "if nothing landed east of centre, use the far door anyway, or the bag
would treat this as a dead end it is not" — exactly backwards for a room that *is* one. The Secret
Vault has one west doorway of two cells; both sit left of the mid-line, the fallback fired, and one
of its own west doors came back as its exit. `RoomConnection.HasExit` was therefore **true for the one
room in the game authored to be a detour**, so `RoomBag.Draw(needsExit)` — the only guard against
mounting an unwalkable room mid-floor — would have drawn it onto the route. Zyno's arena is authored
the same way and would have hit it too. Now read off the map's `D` **columns**: one distinct column is
a dead end, two are a through-room, and no projection is involved.

**3. `FloorLoader` refilled the bag every floor.** `AdvanceFrom` called `_bag.Fill(pool.Options)` on
each floor boundary, which resets the shuffle — CORE_SYSTEMS §8's "drawn through without immediate
repeats" only holds if the draw survives a floor. It now refills **only when the biome changes**.

### The trapped-enemy rule is now checked, not trusted

`EnemyChase` has no pathfinding, so an enemy that walks into a concave pocket stays in it — and a
Combat Room only unlocks when every enemy is dead, so the player is sealed in a room with an enemy
she cannot reach and no door. Nothing errors and nothing warns; the only symptom is a fight that
never ends. With six new layouts authored in one pass that stopped being an acceptable thing to eyeball.

`RoomLayout.ValidatePosts` now runs inside every room build: no post touching a wall or another post
8-way, two clear cells to any solid on each axis, three cells between posts. A single isolated post
is convex and can never form a pocket, which is why isolation is the whole rule. All eleven layouts
pass. It reports rather than refuses — a post one cell too close is a map somebody is mid-edit on,
and refusing to build would hide everything else that changed.

Worth stating because it is invisible in the map string: **two posts that look comfortably apart on
adjacent rows are one cell apart on the grid.**

### Roles are the seam the unbuilt room types plug into

`FloorLoader` used to hardcode `if (isLastOfFloor && _floor % 5 == 0)` for the Mini-Boss and nothing
else. Every remaining room type would have added another branch beside it. Now every design rule
about ordering is in one method:

```csharp
private RoomRole RoleFor(int floor, int roomIndex, bool isLast)
{
    if (final > 0 && floor >= final) return roomIndex == 0 ? FinalBoss : TrueFinalBoss;
    if (isLast && floor % 5 == 0)    return MiniBoss;
    return Combat;
}
```

`BiomeRoomPool.RoomFor(role)` returns the biome's room for a role, or **null**, and null is a
supported answer: the loader warns and draws an ordinary Combat Room. That is what lets a role be
declared before its room exists — `SecretVault` and `TrappedSoul` are both in that state. Adding the
Trapped Soul room later is a prefab, a `RoleRoom` entry and one line in `RoleFor`. **A role is a
property of the floor plan, not of the room prefab**, which is why one `MiniBossArena_01` serves all
three biomes with only its encounter swapped.

### Biomes 2 and 3 are a placeholder expressed in data, not a fallback in code

`Pool_FloodedTunnels` and `Pool_MoltenDepths` hold the **same Upper Caves layouts and roster** under
their own themes, so the biome change reads visually while the content stays Biome 1's. When Flooded
Tunnels gets its own rooms and enemies, those arrays change and no code does. Owner's call
(2026-09-08); change brief §25.

### Placeholder bosses are prefab variants, and only their HP is design

Each is a **variant of `TunnelBrute`** — CONTENT_DESIGN §5 describes every Mini-Boss and the Depth
Warden as a slow, high-HP, telegraphed slammer, which is what the Brute already is, and the project
has done this once before: the Deep Warden Elite is this prefab with a different `EnemyDefinition`
and a tint (ART_DIRECTION §4). **HP is BALANCE §6 verbatim** — 350 / 450 / 600 / 1200, and 600 for
Zyno, whose row leaves the choice TBD. Everything else is invented and is in the change brief.

**None of them has phases or a weapon-check moment**, which §6 and CONTENT_DESIGN §5 both specify
for every one. There is no boss phase system; these are single-wave encounters against a large
enemy. That is the gap this pass knowingly leaves.

### Verification

Driven by `[MenuItem]` probes through `execute_menu_item` and read back through `read_console` —
`execute_code` is still broken in this project and simulated key presses never reach play mode
(01-VERIFICATION.md). The probe file was deleted afterwards.

| Check | Result |
|---|---|
| Room footprints | Measured off the built prefabs; all six Combat Rooms 26.0 × 13.0, inside the 28.4 × 16 view |
| Dead ends | `HasExit` **false** for `SecretVault_UpperCaves_01` and `FinalBossArena_02`, true for the other nine |
| Fight does not spring on arrival | First room stays `Armed` after the player is placed — defect 1, fixed |
| Whole run | **61 rooms over 16 floors**, driven twice with different seeds |
| Rooms per floor | 3–5 every floor (`4 5 3 4 4 5 5 5 4 3 4 3 4 3 3`), and exactly **2** on floor 16 |
| Biome switch | Pool changes at floor 6 and floor 11, theme and ambient light with it |
| Roles | Mini-Boss arena is the last room of floors 5, 10 and 15; floor 16 is `FinalBossArena_01` then `_02` |
| Bag | All seven layouts drawn; no immediate repeats within a floor |
| Real clear chain | Sprang room 03, killed its 5 enemies → doors opened → next room mounted |
| Post placement | All 11 layouts pass `RoomLayout.ValidatePosts` — no pocket an enemy can be trapped in |
| Live room count | **2** in steady state after 61 mounts; **0** stray `(Clone)` roots |
| Death | Killed at floor 16 → death state, summary opens, `timeScale` 0 |
| Shards, death | Depth 16, +0 levels → **160** = (0 × 15) + (16 × 10), banked |
| Victory | Cleared Zyno's arena → `RunCompleted` → summary reads **Victory** |
| Shards, victory | Depth 16, +2 levels → **190** = (2 × 15) + (16 × 10), banked |
| Return to Hub | Loads `HubScene`, `timeScale` back to 1, Shard counter present and updated |
| Hub → run | `HubDescent` descends into `RunScene`; the run starts and logs its seed |
| Left behind | 0 errors, 0 warnings across every pass; `ShardBank` reset to 0 after testing |

### Outstanding

- **No boss has phases, a phase transition or a weapon-check.** BALANCE §6 and CONTENT_DESIGN §5
  specify all three for all five. The placeholders are one large enemy in a room.
- **Biomes 2 and 3 still have no rooms and no roster.** Floors 6–16 are Upper Caves content in a
  different colour. The seams are the two pool assets.
- **No floor-transition presentation.** GDD calls a descent "linearly downward"; the loader still
  walks east with the floor number incrementing silently. Brief §22.4, unchanged.
- **The Secret Vault is still not on the route.** It is now declared as `RoomRole.SecretVault` on all
  three pools and **never drawn** — §8's "detour" needs the branch LEVEL_DESIGN §1 rules out. The
  role existing is the seam; the design question is untouched. Brief §22.2.
- **The Trapped Soul room does not exist.** Its role is declared, nothing fills it.
- **Floor 16's arena does not change its own geometry**, which LEVEL_DESIGN §6 calls the one thing
  that makes it Floor 16.
- **A death still draws her standing up.** `CharacterState.Death` falls back to `Idle` because
  ART_DIRECTION §3's death frames do not exist. She stops moving and the screen opens over her;
  she does not fall over.
- **Nothing has been played by hand at the new room size.** Every check above is a probe measurement
  or a driven mount. Whether a 26 × 13 room *feels* right is the owner's call and one number to
  retune — the map string in `Layout_UpperCaves_01.cs`. Relatedly, only room 03 has had a real fight
  played out in it; the other five pass the geometric post check but have not been walked.
- **`AttackStateMachine`'s lunge still ticks its timer on `Update`.** Untouched again.

---

## GDD reconciliation — the 2026-09-18 rewrite

**Documentation pass. No code was written and no behaviour changed** — this is the engineering plan
being made accurate against a GDD the design owner rewrote, nothing more. `Design/` was not touched
from this side (it has one owner, and this is not it).

### Divergences that are now locked design

Each of these was carried above, or in `00-DESIGN_CHANGE_BRIEF.md`, as *built but not design*. The
GDD now describes them, so the engineering status is unchanged and the **divergence** is closed. They
are listed rather than deleted because the build-log sections above still describe them as deviations
and a reader needs to know the ground moved.

| Built | Recorded as | Now locked in |
|---|---|---|
| **Dash Attack** — a fourth `AttackAction` on Basic during/just after a dash | brief §17, *Dash rework* | `GDD §Player` (Controls, Dodge/Mobility), and named as something **the weapon determines** |
| **Chargeable Heavy Strike**, rooting her, cancellable by dash | brief §17c, §20 | `GDD §Player` (Heavy Strike), `GDD §Combat` (Attack Timing) — on all three weapons, with the Katana-only option kept open as #10 |
| **Dig-Dash travels along the held movement keys**, falling back to facing | brief §17 | `GDD §Player` (Dodge/Mobility) |
| **Dash cooldown pip** and **the run's upgrade strip** on the HUD; no Heavy cooldown icon | brief §15, §18 | `GDD §UI` (HUD) |
| **Icon-led offer card** — tier border, unique icon, category glyph, compressed effect line | brief §26, *Milestone 4 — the offer card goes icon-led* | `GDD §UI` (Upgrade Screen) |
| **One Death/Victory screen**, distinguished by title and accent | *The run* | `GDD §UI` (Death/Victory Screen) |
| **Secret Vault**: elite-dropped key, locked door, a guarded fight, a guaranteed Legendary payout (the weapon's Relic), **key consumed on opening** | brief §21, `VaultDoor.consumeKey`'s "INVENTED" tooltip | `GDD §Roguelike Structure` (Secret Floors). Its "tougher than a standard Combat Room" is the one clause the build has not been checked against — see Milestone 3 |
| **Attacks lunge rather than root**, with no BALANCE row for the distances | brief §7m | `GDD §Combat` (Attack Movement), which now says so explicitly |

**One consequence worth stating plainly, because it is the only one that costs work:** the `IWeapon`
contract in Milestone 2 is written for three actions and the GDD now specifies four. Corrected there.

**Two class comments now say the opposite of the GDD** and are worth a line each on the next pass
through those files — not now, because this was a documentation pass: `DashHUD` opens with "**This
element is in no design doc**" (`GDD §UI` now names the dash pip), and `VaultDoor.consumeKey`'s
tooltip calls key consumption "INVENTED" (`GDD §Roguelike Structure` now locks it). Both are
comments, so nothing behaves wrongly; both would mislead the next reader.

### Numbers the GDD handed back

The rewrite dropped the hard numbers it used to carry — the movement ramp, the chain window and the
per-action lunge distances. They now live **only** as serialized fields, which is where Design Rule 8
wants placeholders, but it also means nothing outside the prefab records them:

- `PlayerController.accelerationTime` 0.055 / `.decelerationTime` 0.085
- `AttackStateMachine.chainWindow` 0.25
- `AttackStateMachine.basicLunge` 0.75 / `.heavyLunge` 1.15 / `.ultimateLunge` 0.9 / `.dashAttackLunge` 1.4, front-loaded over `lungeFraction` 0.45

If any of these is ever meant to be balanced rather than tuned by feel, it needs a BALANCE row; until
then the prefab is the source of truth and `04-BALANCE.md` should not be read as disagreeing.

### Newly tracked — the Whisper Layer HUD line area

`GDD §UI` lists it as a HUD element (CORE_SYSTEMS §15) and has since 2026-08-14, when the narrative
subset joined MUST SHIP. **Nothing in this document tracked it and nothing implements it**: there is
no class in `Scripts/UI/` for it, `BuildRunHUD` lays out no such element, and its only appearance in
the codebase is inside `DashHUD`'s comment, where it is quoted as one of the elements `GDD §UI` asks
for. Tracking it here so it stops falling between the milestone plan (where the narrative systems
have no milestone at all) and the HUD passes (which built every *other* element `GDD §UI` names).

- [ ] **Whisper line area** — a HUD text line, driven by whatever raises Zyno's lines. The HUD half is
      small and well-understood (`HUDLayout` + one class, the same shape as `WaveIndicatorHUD`, which
      is the existing precedent for an element that shows itself only sometimes). **What does not
      exist is the source**: no Whisper system, no dialogue data, no trigger points. Do not build the
      label first — an element with nothing to say is indistinguishable from a broken one.
- [ ] **Memory Fragments and the Refusal State** (CORE_SYSTEMS §15) are in the same position — MUST
      SHIP, no milestone, no code. The Hub's Codex station is placed and stubbed (`HubNotice`), which
      is the only seam any of it has.

### Still to check elsewhere

`00-DESIGN_CHANGE_BRIEF.md` has not been re-read against this rewrite. Several of its entries now
describe behaviour the GDD has absorbed (the rows in the first table above) and should be retired or
re-tagged in a pass of its own — that is the designer's document, not this one, and editing it from
here is exactly the split this project keeps.

---

## The level-up beat — slow-mo, a burst, and a dealt-in offer (owner-directed, 2026-09-18)

**Owner's note:** the upgrade offer appearing on the frame of the killing blow was "a bit shocking".
The owner chose an eased slow-down into the pause (over an instant freeze) and PixelLab art (over
code-drawn geometry). Change brief §27 carries the design side and every invented number.

### Built

- [x] **`Core/RunPause.cs` — `Push(float easeIn)`.** The first hold can ramp `Time.timeScale` to 0
      over real seconds (`(1 − p)^easeExponent`, exponent 2) instead of cutting to it. Input, cursor and
      the HitStop cancel all happen on the first frame, and **`IsPaused` is true from the first frame**
      — which is what keeps `HitStop.Freeze` out of the ramp, since it already refuses while paused.
      An instant `Push()` mid-ease snaps to 0; the last `Pop()` lands on the fixed `normalScale`.
      `fixedDeltaTime` is left alone: every Rigidbody2D in the game already interpolates, so slow
      motion stays smooth without a second value to restore.
- [x] **`UI/UpgradeOffer.cs` — the beat.** A level-up takes the eased hold immediately and opens the
      panel `levelUpRevealDelay` (0.6s) later. Level-ups arriving during the beat only queue, so a
      multi-level drop gets one slow-down and one burst. The Secret Vault grant and the `Enqueue`
      probe still open at once. Picks are refused until the entrance has settled.
- [x] **`UI/OfferReveal.cs`** — the entrance: scrim fade, heading fade, cards rising 10 units with a
      stagger. **No scaling**: positions snap to whole canvas units and fades go through
      `CanvasGroup.alpha`, so nothing resamples off the pixel grid. The first offer of a hold gets
      the full entrance; the next queued one re-deals only its cards and heading.
- [x] **`Player/LevelUpVFX.cs`** on `Visual`, drawing on a new `Visual/LevelUpBurst` child —
      `Sprite-Unlit-Default`, order −1, **behind** her body inside her SortingGroup, so it
      depth-sorts with her and never buries her silhouette (the aura flame's failure). Unscaled
      flipbook; a second raise mid-clip does not restart it.
- [x] **`Editor/ImportLevelUpVFX.cs`** — `Deeper/Import Level-Up VFX`: import contract, slice
      `Art/VFX/LevelUp.png` as one row of square cells (`LevelUp_0_<col>`, ids kept by name across
      re-runs), and create-or-find the renderer and component on `Player.prefab`. Runs without the
      sheet, wiring an inert component — that is how the beat was verified before the art existed.
- [x] **`Editor/BuildUpgradePanel.cs`** — CanvasGroups on the scrim, a new `Heading` wrapper and
      every card; `OfferReveal` built and wired. Rebuilt in **TestScene** and **RunScene**.
- [x] **Pick-flash defect fixed.** The flash was built inside `Panel`, which `UpgradeOffer` switches
      off on the pick — so ART_DIRECTION §6's pick flash never showed after the last offer of a
      level-up. Confirmed on disk in both scenes before the change (`Flash <- Panel <- UpgradePanel`);
      it is now a sibling of `Panel`.

### Verification

`execute_code` failed every call on the BOM (01-VERIFICATION §9) and Roslyn is not installed, so this
ran through a throwaway `Editor/VerifyLevelUpBeat.cs` of menu items (§12), deleted afterwards. Timings
were stretched on the live instance (4s ease, 6s delay, and a slowed entrance) so MCP round trips
could sample mid-beat.

- **The ramp follows its curve**: 0.517 at 1.13s and 0.176 at 2.32s into a 4s ease ((1−p)² gives
  0.517 and 0.176), then exactly 0. `IsPaused` true and the Player map disabled from the first sample.
- Panel inactive until the delay, then open. Cards at rest at (−279, −99, 81, 279) — whole units — and
  mid-deal at whole units too (y −6, −7, −2).
- **A pick during the entrance is refused** (panel stays open, pending count unchanged); after it
  settles the pick lands, and `timeScale` returns to **exactly 1** with input re-enabled.
- **The flash now shows after the last pick** — active, enabled, alpha 1, parent `UpgradePanel`.
- **`Add(400)`: level 2 → 7, one beat, five offers.** The second offer re-dealt with the scrim held at
  1.00 and the heading faded back in from 0; the pause held at 0 across the hand-off.
- **Killing blow, in its real order**: a Cave Crawler killed through `TakeDamage` levelled her 1 → 2
  inside the call, and the `HitStop.Freeze` issued straight after — what `AttackHitbox.Landed` does —
  was refused (`_running` null). The ease carried on along its curve.
- **Debug menu mid-ease** snapped the scale to 0; closing it did **not** resume the game while the
  offer still held.
- **Vault grant**: no beat, instant pause, full entrance, card at its authored (0, −20).

- **The burst, with the real art**: a `GrantLevel` fires it in the same frame as the pause (frame
  `LevelUp_0_0`, playing); 1.3s later it has run out (`LevelUp_0_8`, renderer off) and the panel is
  up. Seen in the game at 1 fps (`Play` context menu) through `Main Camera`: ring round her feet,
  beam and sparkles behind her body, her silhouette intact, depth-sorted against the training dummy.
- **RunScene smoke test**: `Add(400)` took 1 → 7 — one beat, one burst, six offers queued; the pick
  flashed and the next offer re-dealt. No console errors in either scene.

### Art — `Art/VFX/LevelUp.png`, 9 frames of 96×96

Generated through the `deeper-art` skill, both approvals by the owner. **Record for regeneration:**

- **Forced palette** (pixflux `color_image_base64`), every colour already shipped: warm off-whites
  (253,252,246) and (252,252,232); the level badge's level-up flash (255,240,184); Legendary gold
  (230,189,92); Upper Caves ochres (176,152,106), (150,126,84); Epic violet (168,115,219);
  violet-greys (140,133,148), (117,110,124), (87,82,92). **No hazard yellow-orange.**
- **Key frame**: `create_image_pixflux`, 96×96, `no_background`, `outline: lineless`,
  `shading: medium shading`, `detail: medium detail`, `view: low top-down`, seed 1807 — "magical
  level-up aura VFX sprite… a flat glowing ring on the floor… a tall tapering beam of light… 3-tone
  ramp: bright warm off-white core, pale gold middle, cool violet outer edges; a few tiny square
  sparkles…" plus the style guide §10 vocabulary.
- **It took four generations and a hand edit, and the reason matters for next time.** Attempt 1
  (seed 4242, "narrow vertical column") drew a flat single-tone bar — style guide §11 reject. Attempt
  2 (above) was right in everything but the floor: a **solid violet disc**, 1,281 px of one colour.
  Two img2img passes asking for a thin ring (strength 150, then 60) kept the disc; the second turned
  it into a stone pedestal. PixelLab reads "ring on the floor" as a platform. **The disc was hollowed
  by a direct pixel edit** — the outer three pixels kept as a violet → gold → cream ramp, the beam's
  columns untouched, the rest cleared — owner-approved over a 20–40-generation `inpaint_image`.
- **Clip**: `animate_image` from the edited key frame, 8 frames (+ the seed as frame 0), seed 1807 —
  "a burst of light fading out: the tall beam of light thins and dissolves upward…, the thin ring on
  the floor spreads slightly outward while fading away, the small gold sparkles drift upward and wink
  out". `animate_image` takes no forced palette: 418 off-palette pixels across 8 frames, some
  drifting toward saturated yellow, **snapped to the nearest forced-palette colour**. The pixel grid
  held (binary alpha on every frame).
- **The generated ring never fades** — frames 4–8 hold it at full strength — so `LevelUpVFX` fades
  alpha over the last `fadeOutFrames` (4) rather than letting it blink off. Total cost: 6 generations.

### Outstanding

- **No ease back out** on the pick — deliberate, combat resumes on that frame (brief §27).
- **Every queued offer's header reads the final level** ("LEVEL 7" on all five after a 2 → 7 drop).
  Pre-existing: `ShowOffer` reads `experience.Level` at show time. Not changed in this pass.
- `UpgradeOffer.cs` is now ~430 lines, past the ~300-line prompt. The entrance was split out into
  `OfferReveal` for exactly that reason; what is left is the binder plus its queue.
