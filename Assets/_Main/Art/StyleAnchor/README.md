# Upper Caves style anchor

`UpperCaves_Palette.png` is the forced palette passed to PixelLab as `color_image_base64`.
Its colours are the project's own shipped values, not invented ones:

- Cool grey ramp, from `Tile_Floor.png` (51,48,56) and `Tile_Wall.png` (87,82,92 / 117,110,124),
  extended one step darker and one lighter.
- Muted browns and a sparse warm ochre, per ART_DIRECTION section 2's Upper Caves row.
- **No orange-red.** Section 2 reserves it cross-biome for crack glow and rockfall dust telegraphs,
  so it must never appear decoratively.

Passing this as a forced palette works: every colour in every generation came back from this set.
What it does not fix is *form* - see the engineering plan's floor-loading section for the two
tools' opposite failure modes and the recolour step that resolves them.
