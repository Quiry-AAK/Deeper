# Upgrade and Curse icons

Generated through the `deeper-art` skill with **PixelLab `create_image_pixflux`**, 1 generation each.

## The prompt skeleton

Reused verbatim for every icon, changing only the leading subject noun (pipeline.md §3):

```
<subject>, single centred game item icon, front view, modern pixel art, hue-shifted shading,
selective outline, limited curated palette, 3-tone shading ramp, top-left light source,
ambient occlusion, readable silhouette, clean pixel clusters, 128x128 canvas, transparent background
```

Parameters, identical across the set:

| Parameter | Value |
|---|---|
| `width` / `height` | 128 / 128 |
| `no_background` | true |
| `outline` | `selective outline` |
| `shading` | `medium shading` |
| `detail` | `medium detail` |
| `view` | `side` |
| `text_guidance_scale` | 9 |
| `color_image_base64` | `Assets/_Main/Art/StyleAnchor/UI_IconPalette.png` |

**The forced palette is the whole reason these came back usable.** `UI_IconPalette.png` is written by
`Deeper/Generate Icon Palette` from 25 colours the game already draws — the Upper Caves greys and
browns, the HUD's steel ramp, the four tier colours and the bar fills. Without it PixelLab returns
its own palette; the note in the engineering plan about "a heart, a money bag and a lightning bolt in
a palette the game does not use" is what that looks like. **No hazard accent is in the palette**
(ART_DIRECTION §2 reserves orange-red, pale cyan-white and bright yellow-orange for danger
telegraphs), so no icon can use one decoratively even by accident.

The style anchor for the set is `Upg_Vitality` — the first generated, approved by the owner before
the other 45 were batched.

## Why 128

`BuildUpgradePanel` draws each icon in a **64-unit** box inside `HUD_SlotIcon` (72 units, 4-unit
border). The HUD canvas scales by a whole number from a 540 reference, so that box is 128 screen px
at 1080p — a 1:1 draw — and 64 px in a short window, an exact half. The run HUD's upgrade strip uses
a 16-unit box, which is an exact quarter. Every draw of these files is an integer ratio; nothing here
is ever resampled off its grid.

## Import settings

Sprite / Single, 32 PPU, Point filter, uncompressed, no mipmaps, `alphaIsTransparency`, Center pivot
— the same contract as the weapon icons next door in `Art/UI/`. Applied by
`Deeper/Import Upgrade Icons`.

## `Cat_*` — the effect-category glyphs (icon-led card pass, 2026-09-17)

Twelve small glyphs — `Cat_Health.png` through `Cat_Utility.png` — one per `EffectSummary`'s
`UpgradeCategory` (`Scripts/Upgrades/EffectSummary.cs`), drawn on each card as a corner badge on the
unique icon rather than its own full-width band. Same folder, same import contract, same
`Deeper/Import Upgrade Icons` menu item as the `Upg_*`/`Curse_*` set above — the only things that
change are the canvas size and the subject noun.

**Authored at 64×64, not 128×128.** `BuildUpgradePanel` draws each glyph in a **32-unit** box, half
the unique icon's 64-unit one, so the same "authored size = 2× the on-screen unit box at the 1080p
reference" rule above holds one size down: 64px at the canvas's 2× factor is a 1:1 draw, 32px in a
short window, an exact half. Authoring these at 128 (matching the unique icons) would make the corner
badge read as large as the icon it sits on.

**Same prompt skeleton, same forced palette**, subject noun and canvas size changed only — reused
verbatim from the section above. `Cat_Damage` was the style-anchor sample for this sub-set, compared
side by side against `Upg_HeavyHands` before the other 11 were batched. One regeneration was needed:
`Cat_Dash`'s first pass ("triple speed-chevron and dust puff") came back as a cartoon rally car —
PixelLab reading "speed" and "dust puff" as a racing motif — and a second pass with an explicit
"abstract icon, no vehicle, no car" clause returned a clean angular speed-streak instead.

**The naming coupling is real, not just convention.** `UpgradeCardArt.CategoryGlyphs.Of` switches on
the `UpgradeCategory` enum name, and `BuildUpgradePanel.WireCardArt` loads `Cat_<enum name>.png` for
each field — renaming a category means renaming its file in the same pass, or the wiring call logs a
missing-glyph warning and the card draws that corner blank.

## Regenerating one

Re-run the same call with that icon's subject noun. Prefer `edit_image` / `inpaint_image` over a full
regeneration when fixing a local defect (pipeline.md §2).
