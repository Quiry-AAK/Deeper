---
name: deeper-art
description: Create or revise game art for "Deeper" through the PixelLab MCP. Use for any sprite, character, equipment layer, tileset, VFX sheet, icon or UI asset in this project — it locks the Modern Pixel Art style, the palette and lighting rules, the sheet/naming contract the animation rig depends on, and the credit-safe generation order. Triggers on "make art", "generate sprites", "pixellab", "character art", "tileset", "equipment art", "icon", "concept", or any request to replace placeholder art.
---

# Deeper — Art Creation

Every asset in this game is generated through the PixelLab MCP and then imported against a rig that
makes strict assumptions. Art that ignores those assumptions doesn't look slightly wrong — it
silently misaligns, and the failure only shows up once a full set has been generated and paid for.

This skill exists so that never happens: style first, contract second, generation last.

## The style is locked

**Modern pixel art — not retro.** The pixel grid is strict and every pixel is placed deliberately,
but nothing here is constrained by 1980s hardware. Palettes are *curated*, not rationed. Form is
built from 3–5 shade ramps with hue-shifted shadows and highlights. Outlines are selective and
coloured, never uniform black. Anti-aliasing is applied by hand, on curves only.

> If a piece could pass for an NES sprite, it is wrong.
> If it looks like a filtered or smoothed illustration, it is also wrong.

The full specification — palette architecture, shade ramps, outline rules, lighting model, and the
prompt vocabulary that actually produces this look — is in **`references/style-guide.md`**. Read it
before writing a single generation prompt.

## Non-negotiable technical contract

These come from the shipping code (`SpriteAnimationSet`, `EquipmentLayerView`), not from taste.
Breaking any of them misaligns art rather than erroring.

| Rule | Value |
|---|---|
| Character canvas | **32×48**, pivot **Center** |
| Tile canvas | **32×32** |
| Pixels per unit | **32** |
| Import | Point filter, uncompressed, no mipmaps, alphaIsTransparency |
| Authored directions | **5** — Down, Up, Side, DownDiagonal, UpDiagonal |
| Direction handedness | All side/diagonal art faces **right**; left facings are mirrored at runtime |
| Sheet grid | **6 columns × 10 rows** |
| Row order | Idle: Down, Up, Side, DownDiag, UpDiag → then Move in the same order |
| Frames | Idle uses columns 0–3, Move uses columns 0–5 |
| Sub-sprite name | `<piece>_<row>_<col>` |

Clip lengths may differ per row — `SpriteAnimationSet.Resolve` wraps against each clip's own array
length, so Idle-4 and Move-6 coexist with no code change.

**Equipment layers** are drawn on that same 32×48 canvas with that same pivot, and must register
pixel-perfectly against the base body in every direction and frame. Alignment is the whole ballgame;
see `references/pipeline.md`.

## Workflow

### Phase 0 — Style anchor (once, before any production art)

Nothing gets generated in bulk until one canonical asset exists and a human has approved it.

1. Generate a single front-facing idle character at 32×48.
2. Present it to the user. Iterate until they sign off.
3. Save it under `Assets/_Main/Art/StyleAnchor/` and record the exact prompt and tool parameters
   next to it.

That anchor is the reference for every later generation. Consistency drift across a set is the
single most common way generated art becomes unusable, and it is unrecoverable without regenerating
everything.

### Phase 1 — Spec before generating

Write down, and check against the design docs, before touching a tool:

- Asset type, canvas size, how many directions, how many frames
- Which biome palette applies (`ART_DIRECTION.md` §2) — and confirm no hazard accent colour is being
  used decoratively
- What the asset must read as **at 100% game zoom**, not zoomed in
- Whether it layers over something else (equipment) or stands alone

### Phase 2 — Generate one, review, then batch

1. Check `mcp__pixellab__get_balance` first. Generations cost real credits.
2. Generate **one** asset. Never open with a batch.
3. Run the acceptance checklist below.
4. Only after it passes, generate the rest of the set.

### Phase 3 — Acceptance checklist

Reject and regenerate if any of these fail:

- **Silhouette** — fill the sprite solid black. Still identifiable? (ART_DIRECTION §1 requires
  silhouette-readability at combat speed.)
- **Scale** — viewed at true in-game size, does it read? Detail that only exists when zoomed is
  wasted budget.
- **Palette** — colours belong to the declared ramps; hazard accents appear only on hazards.
- **Grid** — every pixel on-grid. No semi-transparent fringe pixels from resampling or rotation.
- **Consistency** — side by side with the style anchor: same light direction, same outline
  treatment, same shading logic.
- **Alignment** (equipment only) — composite over the base body across all 5 directions and every
  frame. Any drift at all is a reject.

Composite and inspect the result rather than trusting it — render the layers to a PNG and look at
it. This has already caught two real defects in placeholder art.

### Phase 4 — Integrate

1. Write the sheet into `Assets/_Main/Art/<area>/`.
2. Apply the import settings from the contract table, and slice to the 6×10 grid with
   `<piece>_<row>_<col>` names.
3. Rebind the matching `SpriteAnimationSet` in `Assets/_Main/Data/Animation/`.
4. Enter play mode and verify every facing resolves and mirrored pairs differ only by flip.
5. Update the engineering plan checklist in the same pass.

## Open decisions that block bulk generation

Raise these with the user before generating a full set. Each is far cheaper to settle now than after
hundreds of assets exist:

- **Detail budget vs. slot count.** Five equipment slots on a 32×48 character leaves each piece only
  a handful of visible pixels. Either gear reads as colour-blocking rather than detailed equipment,
  or the canvas grows (which breaks the 32px tile alignment the docs lock). This needs a decision.
- **Baked shading vs. URP 2D lights.** The scene has a `Light2D`. If sprites ship with baked
  directional shading *and* dynamic lights also shade them, the result is muddy. Recommended: bake
  soft ambient form-shading, and reserve `Light2D` for mood wash and hazard glow only — not as the
  primary form-defining light.
- **PixelLab's native direction count** vs. our 5-authored-plus-mirroring scheme. If the tool emits
  8 genuinely distinct directions, decide whether to keep all 8 (drop mirroring, roughly double the
  art) or down-select to 5.

## Never

- Never generate a batch before one approved sample exists.
- Never invent palettes, canvas sizes or direction schemes that contradict `ART_DIRECTION.md` —
  flag the conflict instead (Design Rules 11/12).
- Never accept art that needs runtime scaling or rotation to fit; regenerate it at the right size.
- Never spend credits without the user having asked for that specific asset.
