# Deeper — Modern Pixel Art Style Guide

The visual law for this project. Every generated asset is held to this document, and anything that
fails it gets regenerated rather than patched.

---

## 1. Identity

**Modern pixel art.** Strict pixel grid, deliberate pixel placement, nearest-neighbour everywhere —
but authored with contemporary technique rather than 1980s hardware limits.

The distinction is not decorative. It determines every choice below:

| | Retro / 8-bit (**not this**) | Modern pixel art (**this**) |
|---|---|---|
| Palette | Rationed by hardware, a few colours per tile | Curated and harmonised, ramps per material |
| Shading | Flat fills, 1–2 tones | 3–5 tone ramps, ambient occlusion, bounce light |
| Shadow colour | The same hue, darkened | **Hue-shifted** toward cool/violet |
| Highlight colour | The same hue, lightened | **Hue-shifted** toward warm/yellow |
| Outlines | Uniform 1px black around everything | **Selective**, coloured, broken on lit edges |
| Gradients | Checkerboard dithering | Smooth ramps; dithering only as deliberate texture |
| Anti-aliasing | None | Sparing, hand-placed, on curves only |
| Character size | 16×16 | 32×48 here |

Two rejection tests, applied to everything:

> If it could pass for an NES sprite, it is wrong — under-shaded, flat, black-outlined.
> If it looks smoothed, blurred or filtered, it is also wrong — that is not pixel art at all.

---

## 2. Grid and resolution

- Characters **32×48**, pivot Center. Tiles **32×32**. Pixels per unit **32**.
- One art pixel equals one texture pixel equals one screen pixel at 1× zoom. Never scale a sprite
  to fit — regenerate at the correct size.
- No rotation of pixel content. Rotating produces off-grid fringe pixels that read as blur.
- Transparency is binary. No soft alpha edges; a pixel is either in the sprite or out of it. Partial
  alpha is only ever acceptable as deliberate hand-placed anti-aliasing (§6).

---

## 3. Palette architecture

Three layers, in priority order. See `ART_DIRECTION.md` §2 for the locked biome table.

**Global neutrals.** A shared stone-grey/near-black base runs through all three biomes so the player
character and UI read consistently regardless of location. The player is never re-palettised per
biome.

**Biome core.** Each biome owns a 6–8 colour core, expanded into ramps for shading (§4). The core is
the *identity*; the ramps are the *rendering* of it.

- Upper Caves — cool greys, muted browns, sparse warm torchlight
- Flooded Tunnels — deep blues, teal, slate
- Molten Depths — charcoal black, dark red, ember orange

**Reserved hazard accents.** Orange-red, pale cyan-white, and bright yellow-orange are reserved
*exclusively* for hazard telegraphs and the Hazard Front. They never appear decoratively, on gear,
on UI chrome, or on ambient props. This is a gameplay-readability rule, not an aesthetic preference:
the player must be able to read "that colour means danger" instantly, in any biome.

---

## 4. Shading model

**Ramps.** Each material gets a 3–5 step ramp: core tone, one or two shadow steps, one or two
highlight steps. Fewer than 3 reads as flat/retro; more than 5 at 32×48 is wasted — the pixels
aren't there to carry it.

**Hue shifting is mandatory.** This is the single technique that most separates modern pixel art
from retro, and the thing most likely to be lost if a prompt just says "pixel art":

- Shadows shift **toward cool** — blue/violet — as they darken, never just toward black.
- Highlights shift **toward warm** — yellow/orange — as they lighten, never just toward white.
- Pure black and pure white are effectively banned. Use the darkest neutral and the warmest
  off-white in the palette instead.

**Light direction is fixed: upper-left.** Every asset in the game is lit from the same direction.
Inconsistent light direction across a set is the fastest way to make a scene look assembled rather
than authored.

**Ambient occlusion.** Where forms meet — under a helmet brim, where an arm meets the torso, where a
prop meets the floor — darken the contact with a shadow-ramp step. This is what gives a 32×48 sprite
readable depth.

---

## 5. Outlines

**Selective outlining (selout), not uniform black.**

- Outline colour is a darkened, hue-shifted version of the adjacent fill — not black, and not one
  global outline colour.
- Outlines **break or lighten on lit edges** (upper-left) and **strengthen on shadowed edges**
  (lower-right). A fully enclosed uniform outline is the retro look this project is avoiding.
- Interior detail is separated by shading steps, not by drawing more outlines. Internal black lines
  eat the tiny pixel budget and flatten the form.
- The exception is silhouette safety: where a sprite would otherwise disappear against a same-value
  background, keep the outline continuous on that edge. Readability beats style.

---

## 6. Anti-aliasing

Permitted, sparing, hand-placed only.

- Use it to soften stair-stepping on shallow curves and diagonals, with one intermediate tone
  between the two colours it sits between.
- Never on the outer silhouette against transparency — that produces the halo fringe that makes
  pixel art look upscaled.
- Never automatic, never a filter, never a blur pass.

---

## 7. Readability rules

These come from the game design, not from style, and they override aesthetics:

- **Silhouette first.** Enemies, hazards and the player must be identifiable from silhouette alone
  at combat speed. Test by filling the sprite solid black.
- **Read at true scale.** All judgement happens at 1× game zoom. Detail visible only when zoomed in
  is budget spent on nothing — and at 32×48 with layered gear, the budget is severe.
- **Value separation over colour separation.** Threats must separate from the background by
  *brightness*, not only by hue. A mine is dark; hue contrast alone will not carry.

---

## 8. Lighting integration (URP 2D)

The scene runs the URP 2D Renderer with a `Light2D` present. Sprites therefore get lit twice unless
this is settled deliberately:

**Recommended model** — bake soft ambient form-shading into the sprite (§4), and reserve `Light2D`
for mood wash, biome colour temperature, and hazard glow. Dynamic lights should *tint and atmosphere*
the scene, not define form.

Baking full dramatic directional shading and then lighting it dynamically produces muddy,
double-shaded results. This decision is flagged as open in `SKILL.md` — confirm it before generating
a full set, because it changes how every asset is authored.

---

## 9. Animation feel

- Frame budgets: **Idle 4, Move 6** per direction. These are ceilings from `ART_DIRECTION.md` §3,
  not targets — fewer frames that read cleanly beat more frames that wobble.
- Animate with **sub-pixel intent**: 1px offsets, weight shifts, and secondary motion. A modern walk
  cycle carries a body bob and an opposed limb swing, not just alternating legs.
- **Layer lockstep.** Every equipment layer animates on the same frame timeline as the body. A
  helmet that bobs one frame out of sync with the head is the most visible possible defect in a
  paper-doll rig.
- Loops must be seamless: the last frame flows into the first with no pop.

---

## 10. Prompt vocabulary

What to put in PixelLab prompts to actually land this style.

**Use:** `modern pixel art`, `hue-shifted shading`, `selective outline`, `limited curated palette`,
`3-tone shading ramp`, `top-left light source`, `ambient occlusion`, `readable silhouette`,
`clean pixel clusters`, `top-down game sprite`.

**Avoid:** `8-bit`, `NES`, `retro`, `SNES`, `16-bit` (all pull toward flat, black-outlined, dithered
output); `anti-aliased`, `smooth`, `soft shading`, `airbrushed`, `HD`, `realistic`, `detailed`
(these pull toward filtered non-pixel-art output); `vibrant`/`colourful` for anything in a biome
palette, which will fight the reserved hazard accents.

**Always state explicitly:** the exact canvas size, the facing direction, the view angle (top-down),
and that the background is transparent.

---

## 11. Rejection criteria

Regenerate — do not patch — when any of these are true:

- Flat single-tone fills, or shadows that are just a darker version of the same hue
- Uniform black outline enclosing the whole sprite
- Blurred, anti-aliased or upscaled-looking edges; semi-transparent fringe pixels
- Light direction inconsistent with upper-left, or inconsistent with the style anchor
- Hazard accent colours used anywhere other than a hazard
- Illegible silhouette, or detail that only resolves when zoomed in
- Equipment that drifts by even one pixel against the base body in any direction or frame

---

## 12. Isometric tiles — the shape rule, learned the hard way

Isometric tiles are the one asset class that does not follow §2's 32×32. They are **64×64 canvases
holding a 2:1 diamond** (64 wide, 32 tall), placed on a Unity isometric `Grid` with `cellSize (2, 1)`
at 32 PPU.

**Generate floors with `tile_shape: "thin tile"`, and strip the side wall afterwards.**

PixelLab's isometric tiles are little 3D blocks: a diamond top face with a side wall hanging below
it. That wall lands in exactly the pixels the neighbouring tile's top face occupies, and this
project's URP `Renderer2D` runs `TransparencySortMode.Default` under an orthographic camera — which
sorts by Z, so every tile ties at z = 0 and the per-tile order inside a Tilemap is undefined. A floor
built from block tiles renders as **a field of raised blocks with every slab edge showing**.

- `"thick tile"` / `"block"`: a ~20-row side wall, and a top face ~35 rows tall whose slope measures
  an irregular ~3.5px/row. A true 2:1 diamond is exactly 4px/row. These cannot tessellate.
- `"thin tile"`: measured as an exact 2:1 diamond. Use it for floors.
- Walls keep their block — a wall is *meant* to read as raised.

`Deeper/Generate Hub Art` crops each `Iso/Floor_*.png` down to its bare diamond, with a two-pixel
skirt of the diamond's **own edge colour** (not the source's wall pixels, which would paint a dark
fringe over the neighbour when the undefined order goes the other way). It is idempotent.

**Measure, do not trust.** Profile a new tile's opaque width row by row before generating a set. The
shipped `Art/Environment/UpperCaves/Iso/*.png` profile identically to the rejected tiles, so the
existing biome floors carry this defect too.

## 13. A floor tile is stamped hundreds of times — it is wallpaper, not a picture

Per-tile detail becomes a repeating motif across a whole room. The camp's first dirt tile had a small
dark mark in it, and a floor of it read as printed wallpaper rather than ground.

- Ask for **"almost featureless"**, "no marks, no rocks, no cracks", `low detail`, flat or basic
  shading. `highly detailed` produces speckle, not texture.
- Variety comes from **several variants picked per cell, weighted** — one dominant base with the
  others sparse. An *even* mix is its own defect: variants differ in value as well as texture, so
  equal weights read as mismatched floor tiles rather than as ground.
- **Composite before committing.** Stamping the candidate tiles into an isometric patch and looking
  at it caught every one of these; none was visible on a single tile.
