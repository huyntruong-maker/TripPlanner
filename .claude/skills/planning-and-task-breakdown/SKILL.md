---
name: planning-and-task-breakdown
description: Guides agents through decomposing a spec into small verifiable tasks with acceptance criteria and dependency ordering. Use after a spec exists and before implementation, when you need a task backlog, sequencing, or a wave plan.
---

## Overview

Planning turns FRs from a spec into an ordered backlog of small tasks, each independently verifiable, each with explicit acceptance criteria and dependencies. A good plan lets work proceed in thin slices and lets the orchestrator schedule parallel waves.

Output lives at `docs/plans/<feature>.md` or the project TODO backlog.

## When to Use

- Right after `/spec`, before any code.
- When a feature is large enough to need ordering or parallelization.
- When handing work to multiple specialists (backend, frontend, DB, QA).

## Process

1. List every FR from the spec. Checkpoint: each FR maps to at least one task.
2. Decompose each FR into tasks small enough to finish and verify in one sitting (a few hours max). Split anything bigger.
3. Give each task an ID (`T-01`), a one-line goal, and concrete acceptance criteria (observable, testable).
4. Map dependencies: which task must finish before another can start. Mark independent tasks as parallelizable.
5. Order tasks: foundational/shared first (schema, contracts), then dependents, then polish. Checkpoint: no task depends on a later one.
6. Group into waves for the orchestrator: parallel where independent, sequential where one feeds the next.
7. Flag risks and unknowns per task; spike them first if they block estimates.
8. Confirm the plan with the user/orchestrator before building.

### Task template

```
- T-01 [FR-001] <goal>
  Acceptance: <observable, testable outcome>
  Depends on: none | T-xx
  Parallel-safe: yes/no  Owner: <agent>
```

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "I'll figure out order as I go." | Hidden dependencies cause rework and merge pain. |
| "Tasks don't need acceptance criteria." | Without them you cannot tell when a task is done. |
| "It's one big task." | If it can't be verified in one sitting, it's not one task. |
| "Skip waves, just do it." | Waves expose what can run in parallel and what blocks. |

## Red Flags

- Tasks with no acceptance criteria.
- A task that takes days — it is really several tasks.
- Circular or implicit dependencies.
- A plan with no relation back to FR IDs.

## Verification

- `docs/plans/<feature>.md` lists tasks with IDs, FR links, acceptance criteria, and dependencies.
- Every FR is covered by at least one task; no orphan tasks.
- Dependency order is acyclic and waves are explicit.
- Plan approved before implementation started.
