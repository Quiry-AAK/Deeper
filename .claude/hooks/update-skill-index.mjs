#!/usr/bin/env node
// Regenerates .claude/skills/README.md — the index of this project's Claude skills.
//
// Run as a PostToolUse hook on Write|Edit (see .claude/settings.json). It reads the hook's
// stdin JSON, ignores anything outside .claude/skills/, and otherwise rewrites the index from
// the frontmatter of every SKILL.md on disk.
//
// Why regenerate the whole file rather than append the one skill that just changed: appending
// duplicates a skill every time it is edited, and never notices a rename or a deletion. A full
// rebuild is idempotent, so the hook can fire as often as it likes and the index still matches
// the directory. It costs a directory scan of a folder with a handful of entries.
//
// Deleting a skill folder does not fire a Write or Edit, so the index drops it on the next
// skill write rather than immediately. Run `node .claude/hooks/update-skill-index.mjs --force`
// to rebuild on demand.

import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join, dirname, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

// Resolved from this file's own location, not from cwd — a hook's working directory is not
// something to bet the write path on.
const HOOKS_DIR = dirname(fileURLToPath(import.meta.url));
const PROJECT_ROOT = dirname(dirname(HOOKS_DIR));
const SKILLS_DIR = join(PROJECT_ROOT, ".claude", "skills");
const INDEX_PATH = join(SKILLS_DIR, "README.md");

const force = process.argv.includes("--force");

main();

function main() {
  if (!force && !touchesSkills(readStdin())) return; // Not a skill edit. Do nothing, say nothing.

  if (!existsSync(SKILLS_DIR)) return;

  const skills = readdirSync(SKILLS_DIR, { withFileTypes: true })
    .filter((e) => e.isDirectory())
    .map((e) => readSkill(join(SKILLS_DIR, e.name), e.name))
    .filter(Boolean)
    .sort((a, b) => a.name.localeCompare(b.name));

  const markdown = render(skills);
  const previous = existsSync(INDEX_PATH) ? readFileSync(INDEX_PATH, "utf8") : "";

  // Only write — and only speak — when something actually changed, or every edit to any skill
  // reports an update that isn't one.
  if (markdown === previous) return;

  writeFileSync(INDEX_PATH, markdown, "utf8");
  process.stdout.write(
    JSON.stringify({
      systemMessage: `Skill index updated — ${skills.length} skill${
        skills.length === 1 ? "" : "s"
      } in .claude/skills/README.md`,
    })
  );
}

/**
 * Reads fd 0 synchronously. The streaming form (`on("data")` / `on("end")` behind a promise)
 * exited silently here without ever resolving, which looks exactly like "the path didn't match"
 * — the process just ends with the await still pending. Blocking on the fd removes the question.
 */
function readStdin() {
  try {
    return JSON.parse(readFileSync(0, "utf8"));
  } catch {
    return null; // No payload, or not JSON. Treated as "not a skill edit".
  }
}

/** True when the tool call that fired this hook wrote something inside .claude/skills/. */
function touchesSkills(payload) {
  if (!payload) return false;

  const path =
    payload?.tool_input?.file_path ??
    payload?.tool_response?.filePath ??
    payload?.tool_input?.path ??
    "";

  if (!path) return false;

  // Anchored to a path segment so `notes.claude/skills/` can't match, but without demanding a
  // leading slash — the tool reports a relative path as often as an absolute one.
  const normalized = String(path).replace(/\\/g, "/");
  if (!/(^|\/)\.claude\/skills\//.test(normalized)) return false;

  // The index is this script's own output. Rebuilding because it changed is a wasted scan.
  return !normalized.endsWith("/.claude/skills/README.md");
}

function readSkill(dir, folder) {
  const skillFile = join(dir, "SKILL.md");
  if (!existsSync(skillFile)) return null;

  const front = parseFrontmatter(readFileSync(skillFile, "utf8"));

  return {
    folder,
    // The frontmatter name is what the Skill tool resolves; fall back to the folder so a skill
    // with malformed frontmatter still appears rather than silently vanishing from the index.
    name: front.name || folder,
    description: front.description || "",
    extras: listExtras(dir),
  };
}

/** Reads the leading `---` block. Continuation lines fold into the previous key. */
function parseFrontmatter(text) {
  const lines = text.split(/\r?\n/);
  if (lines[0]?.trim() !== "---") return {};

  const out = {};
  let key = null;

  for (let i = 1; i < lines.length; i++) {
    const line = lines[i];
    if (line.trim() === "---") break;

    const match = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(line);
    if (match) {
      key = match[1];
      out[key] = unquote(match[2].trim());
    } else if (key && line.trim()) {
      out[key] = `${out[key]} ${line.trim()}`.trim();
    }
  }
  return out;
}

function unquote(value) {
  const match = /^(['"])([\s\S]*)\1$/.exec(value);
  return match ? match[2] : value;
}

/** Everything in the skill folder that isn't SKILL.md — reference files, scripts, assets. */
function listExtras(dir) {
  const found = [];

  const walk = (current, prefix) => {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const rel = prefix ? `${prefix}/${entry.name}` : entry.name;
      if (entry.isDirectory()) walk(join(current, entry.name), rel);
      else if (rel !== "SKILL.md") found.push(rel);
    }
  };

  walk(dir, "");
  return found.sort();
}

function render(skills) {
  const generator = relative(PROJECT_ROOT, fileURLToPath(import.meta.url)).split(sep).join("/");

  const lines = [
    "# Project skills",
    "",
    "Claude skills committed to this repo, in `.claude/skills/`. Invoke one by name with the",
    "Skill tool, or type its slash command.",
    "",
    `**Generated file — do not edit by hand.** \`${generator}\` rewrites it from each skill's`,
    "frontmatter whenever a skill is written or edited, so hand-written changes are lost on the",
    "next skill edit. Change a skill's `name` or `description` instead.",
    "",
  ];

  if (skills.length === 0) {
    lines.push("_No skills yet._", "");
    return lines.join("\n");
  }

  lines.push("| Skill | Invoke | What it does |", "|---|---|---|");
  for (const skill of skills) {
    lines.push(
      `| [\`${skill.name}\`](${skill.folder}/SKILL.md) | \`/${skill.name}\` | ${summarize(
        skill.description
      )} |`
    );
  }
  lines.push("");

  for (const skill of skills) {
    lines.push(`## ${skill.name}`, "");
    lines.push(`\`.claude/skills/${skill.folder}/SKILL.md\``, "");
    if (skill.description) lines.push(skill.description, "");
    if (skill.extras.length > 0) {
      lines.push("Supporting files:", "");
      for (const extra of skill.extras) lines.push(`- \`${extra}\``);
      lines.push("");
    }
  }

  return lines.join("\n");
}

/** First sentence of the description, so the table stays scannable. */
function summarize(description) {
  if (!description) return "_(no description)_";

  const firstSentence = /^(.*?[.!?])(\s|$)/s.exec(description);
  const text = (firstSentence ? firstSentence[1] : description).trim();

  // Pipes would break the markdown table; newlines already collapsed by the frontmatter parser.
  return text.replace(/\|/g, "\\|");
}
