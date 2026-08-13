---
name: incremental-implementation
description: Guides agents through implementing planned tasks as thin vertical slices — implement, test, verify, commit — with feature flags and rollback-friendly changes. Use when writing code for a planned task and you want to keep the tree green and changes safe to revert.
---

## Overview

Build in thin vertical slices: each slice delivers a small, working, end-to-end increment (DB to API to UI where relevant), is tested, verified, and committed before the next begins. The tree stays green; every commit is revertible.

## When to Use

- Implementing any task from a `/plan` backlog.
- When a change is large enough that a single commit would be risky or unreviewable.
- When shipping behind a flag while work is in progress.

## Process

1. Pick the next ready task (dependencies satisfied). Restate its acceptance criteria.
2. Cut the smallest slice that moves toward acceptance and is independently testable. Checkpoint: the slice compiles and runs on its own.
3. If the feature is incomplete or risky, put it behind a feature flag (config/env toggle) so main stays shippable.
4. Implement the slice. Keep edits scoped — no drive-by refactors in the same commit.
5. Write/extend tests for the slice (see test-driven-development). Run them.
6. Verify: run the build and the relevant test suite. Checkpoint: build passes, tests green.
   - .NET: `dotnet build` and `dotnet test`.
   - React/TS: `npm run build`, `npm test` / `npx vitest run`, and typecheck (`tsc --noEmit`).
7. Commit atomically with a Conventional Commit message referencing the task/FR.
8. Repeat for the next slice. Keep changes rollback-friendly (additive migrations, reversible toggles).

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "I'll wire it all up, then test once." | A broken big change is hard to bisect; slices are not. |
| "Feature flags are overkill." | They let you merge half-done work without breaking main. |
| "I'll commit everything at the end." | One giant commit is unreviewable and unrevertible. |
| "Quick refactor while I'm here." | Mix refactor + feature and reverting either is painful. |

## Red Flags

- Uncommitted changes spanning many unrelated files.
- A slice that cannot be tested until "the rest is done."
- Destructive DB migrations with no rollback path.
- Skipping the build/test step "because it's a small change."

## Verification

- Each slice has a passing build and green tests, with output as evidence.
- History shows small atomic commits, each in Conventional Commit format.
- Incomplete work is behind a flag; main remains shippable.
- Migrations/changes are reversible or have a documented rollback.
