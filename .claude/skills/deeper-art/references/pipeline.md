# Deeper — Art Generation Pipeline

How to actually drive the PixelLab MCP and land the result in Unity without breaking the rig.

**Check tool schemas at use time.** Load them with
`ToolSearch("select:mcp__pixellab__create_character,mcp__pixellab__animate_character,...")` before
calling. Parameter names and options change; do not generate from memory of this document.

---

## 1. Tool map

| Need | Tool |
|---|---|
| Player / enemy character with directions | `create_character` |
| Extra pose or state for an existing character | `create_character_state` |
| Animate a character | `animate_character` |
| Props, pickups, ore, chests | `create_map_object`, `create_1_direction_object`, `create_8_direction_object` |
| Floor / wall tilesets | `create_topdown_tileset` |
| One-off images, VFX frames, concepts | `create_image_pixflux`, `create_image_pixen`, `create_image_pro` |
| Inventory / HUD icons | `create_ui_asset` |
| Revise part of an existing image | `edit_image`, `inpaint_image` |
| Credit balance | `get_balance` |
| What already exists | `list_characters`, `list_objects`, `list_topdown_tilesets` |

Read `mcpforunity://custom-tools` and the PixelLab project list (`list_projects`) once per session
before assuming what's available.

---

## 2. Credit discipline

Generations cost real money and are not refundable when the output is wrong.

1. `get_balance` before starting any session of art work.
2. Never batch before one sample from that same prompt shape has been approved.
3. Prefer `edit_image` / `inpaint_image` over full regeneration when fixing a local defect.
4. If a prompt produces two rejects in a row, stop and re-read `style-guide.md` §10 rather than
   burning a third attempt on the same wording.
5. Never spend credits on an asset the user did not ask for.

---

## 3. Style consistency

Consistency drift is the primary failure mode of generated art sets — every asset looks fine alone
and wrong together, and the fix is regenerating the whole set.

- The **style anchor** (SKILL.md Phase 0) is generated once and approved by a human.
- Every subsequent generation reuses the anchor's prompt skeleton verbatim, changing only the
  subject noun. Do not re-word the style clauses per asset.
- Where a tool accepts a reference or init image, pass the anchor.
- Record the exact prompt and parameters alongside every accepted asset, so a later regeneration can
  reproduce it.
- Review new assets **side by side with the anchor**, never in isolation.

---

## 4. The equipment layering problem

This is the hard part of the project, and it deserves an explicit strategy.

**The tension.** Generative pixel art produces *whole characters*. The rig needs *separable layers*
— a helmet, a chest piece, greaves, boots and a weapon that each register pixel-perfectly against a
shared body across 5 directions × up to 6 frames. Asking a generator for "a helmet on transparency"
produces a helmet that fits no particular head.

**Approaches, in order of preference:**

**A. Base-body-first, then paint gear onto the real frames.** Generate and approve the base body for
every direction and frame. For each gear piece, use `inpaint_image` / `edit_image` on *those exact
frames*, masked to the slot region, so the piece is drawn onto the actual body it must fit. Then
isolate the piece by diffing against the clean base frame and keeping only changed pixels.

This preserves alignment by construction, because the gear was never drawn against a hypothetical
body. It is the recommended path.

> **Validate on one piece before committing.** Take a single helmet through the entire loop —
> generate, diff, import, composite over the body in all 5 directions and every frame — and confirm
> zero drift. Only then generate the rest. The diff-extraction step in particular is unproven and
> must be demonstrated, not assumed.

**B. Whole-character variants per loadout.** Rejected. Five slots with multiple items each is a
combinatorial explosion and it destroys the mix-and-match inventory that already works.

**C. Generate loosely, then hand-align.** The fallback if A fails. Reliable but slow, and it makes
every new gear piece a manual job rather than a generation.

If A proves unworkable, that is a real signal worth taking back to the user — it may argue for fewer
visually-distinct slots rather than for a slower pipeline.

---

## 5. Directions

The rig authors **5** directions — Down, Up, Side, DownDiagonal, UpDiagonal — all facing **right**,
and mirrors the three left-hand facings at runtime (`Facing.IsMirrored()`).

PixelLab may natively emit 4 or 8 directions. Reconcile before batching:

- If it emits 8 distinct directions, decide with the user whether to keep all 8 (drop mirroring;
  roughly doubles art volume and cost) or down-select to the 5 right-facing ones.
- If it emits 4, the diagonals still need generating separately.
- Whatever is chosen, **side and diagonal art must face right.** Left-facing source art will appear
  mirrored-backwards in game, and asymmetric details (a weapon on one hip, a shoulder pauldron) will
  swap sides as the player turns.

Asymmetric design is a trap in a mirrored rig. Prefer near-symmetric silhouettes, or accept that the
asymmetry flips.

---

## 6. Sheet assembly and import

Target sheet: **6 columns × 10 rows** of 32×48 cells → 192×480.

Row order (row 0 at the **top** of the image):

| Row | Clip | Direction | Columns used |
|---|---|---|---|
| 0 | Idle | Down | 0–3 |
| 1 | Idle | Up | 0–3 |
| 2 | Idle | Side | 0–3 |
| 3 | Idle | DownDiagonal | 0–3 |
| 4 | Idle | UpDiagonal | 0–3 |
| 5 | Move | Down | 0–5 |
| 6 | Move | Up | 0–5 |
| 7 | Move | Side | 0–5 |
| 8 | Move | DownDiagonal | 0–5 |
| 9 | Move | UpDiagonal | 0–5 |

Import settings (all mandatory):

```
textureType        = Sprite
spriteImportMode   = Multiple
spritePixelsPerUnit= 32
filterMode         = Point
textureCompression = Uncompressed
alphaIsTransparency= true
mipmapEnabled      = false
wrapMode           = Clamp
```

Slice to `<piece>_<row>_<col>`, pivot Center, then rebind the matching
`Assets/_Main/Data/Animation/Anim_<piece>.asset`. Re-slicing regenerates sub-sprites, so **always
rebind the animation set after a reimport** — otherwise references silently go null.

---

## 7. Verification

Automated, before showing the user:

1. Every `SpriteAnimationSet` resolves all 8 facings × every state × every frame — no nulls.
2. Mirrored pairs (E/W, NE/NW, SE/SW) return the identical sprite differing only by flip.
3. Body and every gear layer land on the same row and frame index.
4. Composite the layers to a PNG and **actually look at it** — this has already caught a helmet
   covering the face pixels, which made two directions indistinguishable, and diagonals too subtle
   to tell from cardinals. Neither showed up in any assertion.

Then in play mode: walk all 8 directions and confirm facing, mirroring and layer lockstep.

Note the Game/Scene views may be collapsed in the user's editor layout, and the editor freezes the
player loop while unfocused (`Application.runInBackground = true` works around it for automation).
Screenshot-based verification is unreliable there; composite-to-PNG is the dependable path.
