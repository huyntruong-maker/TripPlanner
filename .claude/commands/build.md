---
description: Implement a planned task as thin vertical slices using the incremental-implementation skill — implement, test, verify, commit.
argument-hint: "<task ID or description to build>"
---

Apply the `incremental-implementation` skill to: $ARGUMENTS

For each thin slice: implement the smallest end-to-end increment, write/extend tests, run the build and tests (`dotnet build`/`dotnet test` for .NET; `npm run build`/`vitest run`/`tsc --noEmit` for React/TS), then commit atomically with a Conventional Commit message referencing the task/FR. Put incomplete or risky work behind a feature flag so main stays shippable. Keep changes rollback-friendly.

Verification: each slice has a passing build and green tests (output shown), history shows small atomic Conventional Commits, and incomplete work is flagged off.
