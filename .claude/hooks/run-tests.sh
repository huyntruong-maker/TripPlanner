#!/usr/bin/env bash
set -euo pipefail

# Run a quick test suite for the .NET + React stack.
# Degrades gracefully when no test project / tooling is present.

ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"
cd "$ROOT"

echo "[run-tests] starting in $ROOT"
STATUS=0

# --- .NET tests ---
if command -v dotnet >/dev/null 2>&1; then
  if find . -name '*Tests*.csproj' -print -quit | grep -q . || find . -name '*.Tests.csproj' -print -quit | grep -q .; then
    echo "[run-tests] dotnet test"
    dotnet test --nologo --no-restore || STATUS=1
  else
    echo "[run-tests] no .NET test project found, skipping"
  fi
else
  echo "[run-tests] dotnet not found, skipping .NET tests"
fi

# --- React / JS tests ---
if [ -f package.json ]; then
  if grep -q '"test"' package.json; then
    if command -v npm >/dev/null 2>&1; then
      echo "[run-tests] npm test"
      # --run keeps Vitest non-watch; harmless flag for most runners via --
      npm test --silent -- --run >/dev/null 2>&1 || npm test --silent || STATUS=1
    else
      echo "[run-tests] npm not found, skipping JS tests"
    fi
  else
    echo "[run-tests] no test script in package.json, skipping"
  fi
else
  echo "[run-tests] no package.json, skipping JS tests"
fi

if [ "$STATUS" -ne 0 ]; then
  echo "[run-tests] FAILED"
  exit "$STATUS"
fi
echo "[run-tests] passed"
