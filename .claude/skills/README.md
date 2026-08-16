# Project skills

Claude skills committed to this repo, in `.claude/skills/`. Invoke one by name with the
Skill tool, or type its slash command.

**Generated file — do not edit by hand.** `.claude/hooks/update-skill-index.mjs` rewrites it from each skill's
frontmatter whenever a skill is written or edited, so hand-written changes are lost on the
next skill edit. Change a skill's `name` or `description` instead.

| Skill | Invoke | What it does |
|---|---|---|
| [`check-room-types`](check-room-types/SKILL.md) | `/check-room-types` | Audit every room type "Deeper" 's design defines against what is actually built, and report a per-type implementation status — including what is still outstanding on the ones that already work. |
| [`commit-github`](commit-github/SKILL.md) | `/commit-github` | Commit everything done since the last commit in this Unity repo, with a subject and body in the house style, after a Unity-specific pre-flight (meta pairing, stray local output, design-doc guard, doc-sync check). |
| [`deeper-art`](deeper-art/SKILL.md) | `/deeper-art` | Create or revise game art for "Deeper" through the PixelLab MCP. |
| [`implement-room-type`](implement-room-type/SKILL.md) | `/implement-room-type` | Build one room type for "Deeper" end to end — design read, reuse decision, code, ASCII layout, prefab, play-mode verification and docs. |

## check-room-types

`.claude/skills/check-room-types/SKILL.md`

Audit every room type "Deeper" 's design defines against what is actually built, and report a per-type implementation status — including what is still outstanding on the ones that already work. Use for "which room types are implemented", "room status", "what rooms are left", "is the Secret Vault built", "room type audit", or any question about how far the room system has got. Reports only; building a room type is the `implement-room-type` skill.

## commit-github

`.claude/skills/commit-github/SKILL.md`

Commit everything done since the last commit in this Unity repo, with a subject and body in the house style, after a Unity-specific pre-flight (meta pairing, stray local output, design-doc guard, doc-sync check). Optionally push to origin/main. Use for "/commit-github", "commit this", "commit our work", "commit and push", "wrap up this session's changes", or any request to get the working tree into git.

## deeper-art

`.claude/skills/deeper-art/SKILL.md`

Create or revise game art for "Deeper" through the PixelLab MCP. Use for any sprite, character, equipment layer, tileset, VFX sheet, icon or UI asset in this project — it locks the Modern Pixel Art style, the palette and lighting rules, the sheet/naming contract the animation rig depends on, and the credit-safe generation order. Triggers on "make art", "generate sprites", "pixellab", "character art", "tileset", "equipment art", "icon", "concept", or any request to replace placeholder art.

Supporting files:

- `references/pipeline.md`
- `references/style-guide.md`

## implement-room-type

`.claude/skills/implement-room-type/SKILL.md`

Build one room type for "Deeper" end to end — design read, reuse decision, code, ASCII layout, prefab, play-mode verification and docs. Takes the room type as an argument (Combat, Secret Vault, Trapped Soul, Mini-Boss, Final Boss, or a specific new layout). Use for "/implement-room-type <name>", "build the Secret Vault room", "add the Trapped Soul room", "author another combat room layout", or any request to implement or extend a room type.

Supporting files:

- `references/room-types.md`
