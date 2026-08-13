# Hooks

Claude Code hooks for the `claude-fullstack-pack` (.NET + React) stack. They automate
formatting, quick tests, and security scanning around the agent's work.

## What each hook does

| File | Event | Purpose |
| --- | --- | --- |
| `hooks.json` | config | Wires the events below to the scripts. |
| `format-code.sh` | PostToolUse (Edit/Write) | Runs `dotnet format`, then Prettier + ESLint `--fix`. |
| `run-tests.sh` | Stop | Runs the quick test suite (`dotnet test`, `npm test`). |
| `security-scan.sh` | PreToolUse (Bash) `--quick`; full on demand | Secret grep, then `dotnet list package --vulnerable` + `npm audit`. |
| SessionStart | config | Prints a short orientation message. |

All scripts use `#!/usr/bin/env bash`, `set -euo pipefail`, and check `command -v` before
using a tool, so they degrade gracefully when `dotnet`, `npm`, or `rg` are missing (and when
there is no test project or `package.json`). `format-code.sh` and `run-tests.sh` resolve the
repo via `${CLAUDE_PROJECT_DIR}`.

## How to enable

1. Copy the hooks into your project's Claude config:
   ```bash
   mkdir -p .claude/hooks
   cp hooks/*.sh .claude/hooks/
   chmod +x .claude/hooks/*.sh
   ```
2. Merge `hooks/hooks.json` into your settings. Either:
   - Copy its `hooks` object into `.claude/settings.json` (project) or
     `~/.claude/settings.json` (user), or
   - Reference the scripts from your existing settings using the same matchers.
3. The configured commands call the scripts via
   `bash "${CLAUDE_PROJECT_DIR}/.claude/hooks/<script>.sh"`, so keep the scripts under
   `.claude/hooks/`.

## Run manually

```bash
bash .claude/hooks/format-code.sh
bash .claude/hooks/run-tests.sh
bash .claude/hooks/security-scan.sh           # full scan + audits
bash .claude/hooks/security-scan.sh --quick   # secret grep only
```

## Notes

- `security-scan.sh` surfaces findings but exits 0 so it does not block the session; review
  any warnings before pushing. Tighten to a non-zero exit if you want a hard gate.
- `run-tests.sh` exits non-zero on test failure so the Stop hook flags broken work.
