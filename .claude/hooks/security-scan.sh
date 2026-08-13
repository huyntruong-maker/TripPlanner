#!/usr/bin/env bash
set -euo pipefail

# Lightweight secret grep + dependency audit for the .NET + React stack.
# --quick : only the fast secret grep (used in PreToolUse).
# Degrades gracefully when tools are absent.

ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"
cd "$ROOT"
QUICK=0
[ "${1:-}" = "--quick" ] && QUICK=1

echo "[security-scan] starting in $ROOT (quick=$QUICK)"
FINDINGS=0

# --- Secret grep ---
PATTERNS='(AKIA[0-9A-Z]{16}|-----BEGIN [A-Z ]*PRIVATE KEY-----|password\s*=\s*["'"'"'][^"'"'"']+|api[_-]?key\s*[:=]\s*["'"'"'][^"'"'"']+|secret\s*[:=]\s*["'"'"'][^"'"'"']+|Bearer\s+[A-Za-z0-9._-]{20,})'
SEARCH_CMD=""
if command -v rg >/dev/null 2>&1; then
  SEARCH_CMD="rg -n -i --hidden -g '!.git' -g '!node_modules' -g '!bin' -g '!obj' -e"
elif command -v grep >/dev/null 2>&1; then
  SEARCH_CMD="grep -RniE --exclude-dir=.git --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj -e"
fi

if [ -n "$SEARCH_CMD" ]; then
  echo "[security-scan] scanning for secrets"
  if eval "$SEARCH_CMD \"$PATTERNS\" ." 2>/dev/null; then
    echo "[security-scan] WARNING: potential secrets above"
    FINDINGS=1
  else
    echo "[security-scan] no obvious secrets found"
  fi
else
  echo "[security-scan] no grep/rg available, skipping secret scan"
fi

if [ "$QUICK" -eq 1 ]; then
  echo "[security-scan] quick mode done"
  exit 0
fi

# --- .NET dependency audit ---
if command -v dotnet >/dev/null 2>&1 && find . -name '*.csproj' -print -quit | grep -q .; then
  echo "[security-scan] dotnet list package --vulnerable"
  dotnet list package --vulnerable --include-transitive 2>/dev/null || echo "[security-scan] dotnet audit skipped"
else
  echo "[security-scan] dotnet or .csproj missing, skipping .NET audit"
fi

# --- npm dependency audit ---
if [ -f package.json ] && command -v npm >/dev/null 2>&1; then
  echo "[security-scan] npm audit"
  npm audit --audit-level=high || echo "[security-scan] npm audit reported issues"
else
  echo "[security-scan] package.json or npm missing, skipping npm audit"
fi

echo "[security-scan] done (findings=$FINDINGS)"
# Do not hard-fail the session on findings; surface them for review.
exit 0
