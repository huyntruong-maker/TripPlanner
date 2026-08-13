---
description: Pull the latest claude-fullstack-pack components from upstream into the project's .claude/ — show a diff, confirm, and never delete local-only files.
argument-hint: "[optional: specific component, e.g. skills or commands]"
---

Sync pack components from upstream into this project's `.claude/`. Optional scope: $ARGUMENTS

Hard rules: show a diff and get explicit confirmation before writing; NEVER delete files that exist only locally (project customizations); additive/update only.

Steps:
1. Identify the upstream pack source (configured remote/path or the pack repo). Determine target dirs under `.claude/` (e.g. `.claude/skills`, `.claude/commands`, `.claude/agents`), scoped to $ARGUMENTS if given.
2. Compute the diff between upstream components and the local copies. Classify each: new (add), changed (update), local-only (keep, never touch), conflicting (flag).
3. Present the diff summary: what would be added, what would be updated, and which local-only files are being preserved untouched.
4. Wait for explicit user confirmation. For conflicts, ask per-file how to resolve.
5. Apply only the confirmed additions/updates. Leave local-only files in place.
6. Report what changed and recommend committing the sync as a `chore(pack): sync upstream` commit.

Verification: a diff was shown and confirmed before any write, no local-only file was deleted, only confirmed changes were applied, and the result is reported as a clean Conventional Commit suggestion.
