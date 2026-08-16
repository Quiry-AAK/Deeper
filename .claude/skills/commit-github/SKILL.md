---
name: commit-github
description: Commit everything done since the last commit in this Unity repo, with a subject and body in the house style, after a Unity-specific pre-flight (meta pairing, stray local output, design-doc guard, doc-sync check). Optionally push to origin/main. Use for "/commit-github", "commit this", "commit our work", "commit and push", "wrap up this session's changes", or any request to get the working tree into git.
---

# Deeper — Commit to GitHub

Take the working tree from wherever it is to a **clean, pushed-ready commit whose message a
reader can use a year from now**. The history in this repo is unusually good — messages record
the owner decision behind a change, the bug a fix prevents, and what was deliberately *not* done.
Match it. `git log -5` before writing anything; it is the style guide.

Do the pre-flight even when the change looks trivial. Three of the four checks below exist because
a Unity repo breaks for the *next* person, not for you — a missing `.meta` is invisible locally and
turns every prefab reference into a missing script on their clone.

## Step 0 — Orient, and get in sync first

```
git status --porcelain=v1        # the full surface, not the truncated snapshot
git diff --stat HEAD             # tracked changes by size
git log -5 --format='%s%n%n%b'   # the message style you are about to match
git fetch origin && git status -sb
```

**The designer pushes to `main` from their own machine.** If `origin/main` is ahead, pull before
committing — `git pull --rebase origin main` — so the commit lands on top of their work rather than
producing a merge you did not intend.

If that rebase conflicts inside a `.unity`, `.prefab` or `.asset` file: **stop and tell the owner.**
Unity YAML is merged by `unityyamlmerge` (see `.gitattributes`), and hand-resolving a conflict in a
scene or prefab produces a file that opens but is quietly wrong. Never hand-edit YAML conflict
markers.

## Step 1 — Pre-flight, four checks

Run all four. Each one is fast and each has already caught something real.

**1. Every asset has its `.meta`, every `.meta` has its asset.** Unity resolves script and sprite
references by the GUID in the `.meta`. A `.cs` committed without its `.cs.meta` breaks every prefab
pointing at it; an orphan `.meta` is a reference to a file nobody else has. New *folders* need their
folder `.meta` too — `Rooms/` and `Rooms.meta` are two separate entries in `git status`, and it is
easy to stage one.

```bash
git ls-files -co --exclude-standard -- 'Assets/**' | grep -v '\.meta$' | grep -v '/\.' \
  | while read -r f; do [ -e "$f.meta" ] || echo "MISSING META: $f"; done
git ls-files -co --exclude-standard -- 'Assets/**' | grep '\.meta$' \
  | while read -r m; do [ -e "${m%.meta}" ] || echo "ORPHAN META: $m"; done
```

Both must come back empty. (`.gitkeep` and other dotfiles are excluded deliberately — Unity does not
generate metas for them, so they are not findings.)

**2. Nothing local sweeps in.** `git add -A` takes everything not ignored, and this project generates
local output that is *not* in `.gitignore` — `Captures/` (verification screenshots) is the standing
example. List untracked paths outside the asset tree and decide each one explicitly:

```bash
git ls-files -o --exclude-standard --directory | grep -vE '^(Assets|ProjectSettings|Packages|\.claude)/'
```

Screenshots and scratch output stay out unless the owner asks for them. Anything under
`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln` is already ignored — if one shows
up, that is a `.gitignore` problem to raise, not a file to commit.

**3. `Assets/_Main/Docs/Design/*` must not be modified.** Those nine files belong to the designer.
If any shows as modified and this session did not have an explicit owner instruction to edit them,
**stop and ask** — the divergence should have gone into `Docs/00-DESIGN_CHANGE_BRIEF.md` instead
(Design Rules 11/12). Do not quietly commit it and do not revert it either; ask.

**4. The docs that *should* have moved, did.** Any change that diverges from locked design, or that
invents a number no doc specifies, is supposed to land in the same pass as the code:

| If the change… | This file should be in the diff |
|---|---|
| adds/finishes any system | `Docs/Engineering/00-IMPLEMENTATION_PLAN.md` (checkboxes + notes) |
| diverges from locked design, or invents numbers | `Docs/00-DESIGN_CHANGE_BRIEF.md` |
| touches `TestScene` or `Scripts/Testing/` | `Docs/Engineering/02-TEST_SCENE.md` |
| discovered an environment gotcha while verifying | `Docs/Engineering/01-VERIFICATION.md` |

A missing one is not a reason to block the commit — it is a reason to say so before committing, and
offer to write it. Committing code whose divergence is recorded nowhere is how the change brief went
stale twice.

## Step 2 — Decide the split

Default to **one commit for one coherent body of work.** Prefer several when the tree holds
genuinely separate passes that touch disjoint paths — they review better and revert independently.

Collapse into one commit when the passes *overlap in the same files*, and then **say so in the body**.
`be03737` is the precedent: two bodies of work in one commit because separating them meant
reconstructing a tree state that never existed on disk. That sentence is worth more to the next
reader than a clean-looking pair of commits that lie about the order things happened.

Do not split for its own sake. A commit whose message is "part 2 of 3" tells nobody anything.

## Step 3 — Write the message

House style, from the existing log:

- **Subject:** imperative mood, sentence case, no trailing period, ≤72 chars. No `feat:`/`fix:`
  prefixes, no scope parens, no emoji. *"Add Biome 1 enemy roster, object pooling, and the staged
  combat pass"*, not *"feat(enemies): add roster"*.
- **Body:** hard-wrapped at 72 columns. Lead with a paragraph of *why* — the owner decision and its
  date, the bug, the hole being closed. Then grouped bullets under short headings when the change
  spans subsystems.
- **Record the things a diff cannot show:** what was deliberately left alone and why, numbers that
  were measured rather than chosen, bugs found during verification (with the symptom, not just the
  fix), and anything now flagged for a design decision. Cite doc sections.
- **Footer:** one blank line, then exactly

  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  ```

Shape:

```
<Imperative subject, <=72 chars>

<Why this exists: the owner decision (with date), the bug, or the goal.>

<Subsystem heading>
- <what changed, and the constraint or measurement behind it>

Deliberately not done:
- <what was left, and why it is not a bug>
```

### Write the message to a file — never pass a body with `-m`

**Backticks in a double-quoted string are command substitution, and they silently delete the word.**
This is not hypothetical: commit `881a589` in this repo reads *"OnDamageDealt gains a  param"* — the
backticked `source` was executed as a command, produced nothing, and left the double space behind.
Nobody noticed until it was permanent. This repo's prose is full of backticked identifiers, so the
hazard is live in almost every message you will write.

```bash
# write the message with the Write tool, to the session scratchpad, then:
git add -A                                    # after Step 1 cleared it
git commit -F "<scratchpad>/commit-msg.txt"
```

Use the Write tool for the file (UTF-8, no shell quoting involved at all). Do not use a heredoc, do
not use `-m` for anything longer than a subject, and never build the message inside a PowerShell
string — backticks are the escape character there.

## Step 4 — Verify the commit

```bash
git show --stat HEAD | head -40
git status --porcelain=v1        # expect empty, or only the paths you chose to leave
git log -1 --format=%B           # read it back; confirm no word was eaten
```

Check the file count against what Step 0 showed. A commit that swept in 40 more files than you
reviewed is the failure this whole skill is arranged to prevent.

## Step 5 — Push only when asked

`git push origin main` is outward-facing and shared with the designer, so it is not part of the
default flow. Push when the invocation says to (`/commit-github push`, "commit and push"); otherwise
commit, report the SHA and subject, and ask.

If the push is rejected as non-fast-forward, the designer pushed while you worked: `git pull --rebase
origin main`, re-run Step 1's checks, push again. Never `--force`, and never `--amend` a commit that
is already on `origin`.

## Arguments

| Invocation | Behaviour |
|---|---|
| `/commit-github` | Pre-flight, one or more commits, report, then ask about pushing. |
| `/commit-github push` | Same, and push to `origin/main` when the commit is verified. |
| `/commit-github <hint>` | The hint scopes the commit — a path, a subsystem, or the framing for the subject line. |

## Never

- Never commit without running Step 1. All four checks are seconds; the failures they catch are
  invisible on this machine and only appear on someone else's clone.
- Never commit a modified file under `Assets/_Main/Docs/Design/` without an explicit owner
  instruction in this session.
- Never hand-resolve a conflict in a `.unity`, `.prefab` or `.asset` file, and never hand-normalize
  line endings in one — `.gitattributes` owns both.
- Never pass a multi-line body with `-m`, and never put backticks in a shell-quoted message.
- Never `--force`, never `--amend` anything already pushed, never `--no-verify`.
- Never write a message that only restates the diff. If the body could be regenerated from
  `git show --stat`, it is not finished — the *why*, the rejected alternative and the open question
  are the parts only you can write.
- Never claim in a message that something was verified in play mode unless it actually was.
