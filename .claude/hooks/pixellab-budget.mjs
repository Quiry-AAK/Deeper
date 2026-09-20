#!/usr/bin/env node
/**
 * PixelLab generation budget gate.
 *
 * Fires before every mcp__pixellab__* tool call, adds up what the session has spent, and asks the
 * owner to confirm once the running total would pass THRESHOLD generations. Under the threshold it
 * is silent — the point is to stop deliberating about credits on every single call, not to add a
 * prompt to each one.
 *
 * Read-only calls (get_*, list_*, delete_*) cost nothing and never count.
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";

const THRESHOLD = 100;

/**
 * Generation cost per tool. PixelLab bills per generated frame/direction, so a character or an
 * animation is one generation PER DIRECTION, and a Wang tileset is one per tile. These are the
 * documented worst cases — over-estimating is the safe direction for a spend gate.
 */
const COST = {
  create_topdown_tileset: 25,
  create_sidescroller_tileset: 25,
  create_tiles_pro: 25,
  create_isometric_tile: 8,
  create_building_kit: 16,
  create_map: 16,
  create_character: 5,
  create_portrait_character: 5,
  create_character_state: 5,
  create_8_direction_object: 8,
  // Documented as 20-40; the upper bound is the safe one for a spend gate.
  create_1_direction_object: 40,
  create_map_object: 40,
  create_object_state: 40,
  create_image_pro: 40,
  animate_character: 5,
  animate_object: 5,
  animate_image: 5,
  create_vocal_animation: 5,
  create_talking_gif: 5,
  create_font: 5,
};

const FREE = /^(get_|list_|delete_|agent_|search_|cancel_|dismiss_|view_|move_|remove_|place_|select_|set_|update_|add_to_)/;

function readStdin() {
  try {
    return JSON.parse(readFileSync(0, "utf8") || "{}");
  } catch {
    return {};
  }
}

function costOf(shortName) {
  if (FREE.test(shortName)) return 0;
  return COST[shortName] ?? 1;
}

function statePath(sessionId) {
  const dir = join(tmpdir(), "claude-pixellab-budget");
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  return join(dir, `${(sessionId || "default").replace(/[^\w-]/g, "_")}.json`);
}

function main() {
  const input = readStdin();
  const tool = input.tool_name || "";
  if (!tool.startsWith("mcp__pixellab__")) process.exit(0);

  const shortName = tool.replace("mcp__pixellab__", "");
  const cost = costOf(shortName);
  if (cost === 0) process.exit(0);

  const file = statePath(input.session_id);
  let spent = 0;
  let approvedUpTo = THRESHOLD;

  if (existsSync(file)) {
    try {
      const saved = JSON.parse(readFileSync(file, "utf8"));
      spent = saved.spent || 0;
      approvedUpTo = saved.approvedUpTo ?? THRESHOLD;
    } catch {
      /* corrupt state just resets the counter; never block on it */
    }
  }

  const after = spent + cost;

  if (after > approvedUpTo) {
    // Raise the ceiling now so approving once covers the next block rather than prompting
    // on every call past the line.
    writeFileSync(
      file,
      JSON.stringify({ spent: after, approvedUpTo: after + THRESHOLD }),
      "utf8",
    );

    console.log(
      JSON.stringify({
        hookSpecificOutput: {
          hookEventName: "PreToolUse",
          permissionDecision: "ask",
          permissionDecisionReason:
            `PixelLab budget: this ${shortName} call costs ~${cost} generation(s), ` +
            `taking the session to ~${after} — past the ${approvedUpTo} agreed so far. ` +
            `Approve to continue and raise the ceiling to ~${after + THRESHOLD}, or deny to ` +
            `stop and reduce the batch.`,
        },
      }),
    );
    process.exit(0);
  }

  writeFileSync(file, JSON.stringify({ spent: after, approvedUpTo }), "utf8");
  process.exit(0);
}

main();
