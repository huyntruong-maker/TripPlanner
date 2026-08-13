#!/usr/bin/env bash
set -euo pipefail

# Format staged/changed code for the .NET + React stack.
# Degrades gracefully when tools are not installed.

ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"
cd "$ROOT"

echo "[format-code] starting in $ROOT"

# --- .NET ---
if command -v dotnet >/dev/null 2>&1; then
  if ls ./**/*.csproj >/dev/null 2>&1 || find . -name '*.csproj' -print -quit | grep -q .; then
    echo "[format-code] dotnet format"
    dotnet format --no-restore || echo "[format-code] dotnet format reported issues"
  else
    echo "[format-code] no .csproj found, skipping dotnet format"
  fi
else
  echo "[format-code] dotnet not found, skipping"
fi

# --- React / TypeScript ---
if [ -f package.json ]; then
  if command -v npx >/dev/null 2>&1; then
    if npx --no-install prettier --version >/dev/null 2>&1; then
      echo "[format-code] prettier --write"
      npx --no-install prettier --write "**/*.{ts,tsx,js,jsx,css,json,md}" || true
    else
      echo "[format-code] prettier not installed, skipping"
    fi
    if npx --no-install eslint --version >/dev/null 2>&1; then
      echo "[format-code] eslint --fix"
      npx --no-install eslint . --fix || echo "[format-code] eslint reported issues"
    else
      echo "[format-code] eslint not installed, skipping"
    fi
  else
    echo "[format-code] npx not found, skipping JS/TS formatting"
  fi
else
  echo "[format-code] no package.json, skipping JS/TS formatting"
fi

echo "[format-code] done"
