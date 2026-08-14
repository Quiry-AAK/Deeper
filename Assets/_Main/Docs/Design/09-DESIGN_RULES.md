# DESIGN RULES — "Deeper"

The operating ruleset for this project. These rules govern how new ideas get evaluated and folded into the design docs — they're descriptive of the process already used throughout GDD/CORE_SYSTEMS/CONTENT_DESIGN/BALANCE, formalized here so future decisions stay consistent.

---

## Rule 1 — Scope increases need a cut or a timeline decision, not silence

If a new feature adds meaningful development time, it must be paired with one of: (a) an explicit cut elsewhere, (b) an explicit timeline extension, or (c) a demotion to post-MVP. A scope increase that isn't paired with one of these is a scheduling risk masquerading as a design decision.

## Rule 2 — Reuse existing systems before building new ones

Before proposing a new system, check whether an existing one (Attack State Machine, Hazard timer, weighted-draw pool, buff/modifier stack, etc.) can be extended or reskinned to cover the need. Mini-Boss Overcharge, Wave Rooms, and all 3 biomes' hazard variants are built this way — one underlying system, different presentation layers. New architecture is the expensive option; only reach for it when reuse genuinely doesn't fit.

## Rule 3 — Prefer systemic replayability over hand-authored content

When choosing between "add more static content" and "add a system that generates variety from existing content," prefer the system. The Curse pool, weighted upgrade draws, and Ultimate Gauge's resource-pacing model all exist because they multiply the value of content that already exists, rather than requiring linear content growth to keep runs feeling different.

## Rule 4 — Each weapon/enemy/biome needs a clearly different function, not just different numbers

Stat variation alone (higher damage, lower HP) is not sufficient differentiation. Katana/Bow/Greatsword each have a distinct mechanical hook (Combo Counter/Charge Shot/Hyper Armor) precisely because "faster but weaker" isn't enough to justify tripling the animation and upgrade-pool budget. Applies equally to enemy roles and biome hazard identities.

## Rule 5 — New mechanics must serve one of the four core verbs

Movement, Combat, Collect, or Descend (the core loop verbs from the GDD). A proposed mechanic that doesn't clearly strengthen one of these needs a strong justification to be included — "it would be cool" is not sufficient on its own; "it makes Collect a real decision" (e.g., Ore-banking risk) is.

## Rule 6 — Animation/art budgets are ceilings, not targets

Per-state frame counts in ART_DIRECTION.md are hard limits agreed on before content is authored against them. If a feature would require exceeding them, that's a Rule 1 scope conversation, not a quiet budget increase.

## Rule 7 — No feature exists "because roguelikes usually have it"

Every mechanic in these docs should trace back to a specific reason it belongs in *this* game (see Rule 5). Genre convention is a reasonable source of inspiration, not a justification on its own — this is why generic Crit Chance/Crit Damage was cut in favor of Miner's Traits, which are specific to this project's identity rather than a default RPG stat block.

## Rule 8 — Numbers are placeholders until playtested

Every value in BALANCE.md is a first-pass placeholder, explicitly labeled as such. Design docs should never present numeric tuning as final — internal consistency (weapons reaching similar power levels via different paths) matters more at this stage than correctness, since correctness only comes from playtesting.

## Rule 9 — Undefined terms get resolved, not left ambiguous

If a doc references a concept that isn't defined anywhere (the "relic" gap that existed before CONTENT_DESIGN.md formalized it), that gets flagged and resolved before more content is built on top of the ambiguity, not left as implied flavor text.

## Rule 10 — Redundancy check before adding upgrades/content

Before adding a new upgrade, curse, enemy, or room type, check it against the existing pool for near-duplicate effects (e.g., the original duplicate "Grapple Pull" on two weapons was caught and differentiated this way). Content that's mechanically identical to something that already exists adds pool bloat without adding real build variety.

## Rule 11 — Locked decisions can be revisited, but only explicitly

A design decision documented in these files (e.g., "no crit system," "Pickaxe is the only weapon") is treated as locked until the project owner explicitly reopens it. Quietly drifting away from a documented decision without flagging the conflict is not allowed — see the multi-weapon and crit-system conversations in this project's history for the pattern: flag the conflict, get an explicit decision, then update every doc that referenced the old decision.

## Rule 12 — Flag before implementing, not after

When a request would significantly increase scope, change a locked decision, or conflict with an existing system, that gets raised *before* writing it into the docs — not implemented first and explained afterward. The person making the call should always have the tradeoff in front of them at decision time.

**Working sub-rule, added once this started happening in practice:** when the project owner directs something in person (outside a written doc) that contradicts a locked doc, the change gets built as directed — a locked value is a record of what was true when written, not a blocker on the owner's own instruction. The divergence still gets flagged in `00-DESIGN_CHANGE_BRIEF.md` for the designer to reconcile into the locked docs afterward; it just doesn't block the owner in the moment.

## Rule 13 — The core loop is protected above all optional content

Per MVP.md: Movement → Combat → Dig-Dash → Room Clear → Upgrade Pick → Descend must work end-to-end before any optional content (extra biomes, full upgrade pools, Weapon Mastery, etc.) is prioritized. Every CUT IF NECESSARY item in MVP.md is chosen specifically because cutting it doesn't touch this loop.

## Rule 14 — Downstream docs must be updated in the same pass as the decision that changed them

When a decision changes something referenced across multiple docs (e.g., retiring the Active Ability system touched GDD, CORE_SYSTEMS, and CONTENT_DESIGN), all affected docs get updated together, not left inconsistent until someone notices later. A grep-style check for the old terminology before considering the change "done" is standard practice.

---

## How These Rules Get Applied

Each new idea proposed for this project should pass through:

1. **Does it serve a core verb?** (Rule 5) If not, strong justification needed.
2. **Does something like it already exist?** (Rules 2, 10) If yes, extend/reskin instead of building new.
3. **What does it cost?** (Rules 1, 6) Name the cost explicitly.
4. **Does it conflict with a locked decision?** (Rule 11) If yes, flag before proceeding (Rule 12).
5. **Is it MUST SHIP, SHOULD SHIP, or CUT IF NECESSARY?** (Rule 13) Every new feature gets sorted into MVP.md's tiers, not left ambiguous.
