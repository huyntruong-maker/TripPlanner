---
description: Decompose a spec into small verifiable tasks with acceptance criteria and dependency-ordered waves using the planning-and-task-breakdown skill.
argument-hint: "<spec path or feature to plan>"
---

Apply the `planning-and-task-breakdown` skill to: $ARGUMENTS

Read the relevant spec (e.g. `docs/specs/<feature>.md`). Produce `docs/plans/<feature>.md` with tasks (`T-01`...), each linked to an FR, with concrete acceptance criteria, dependencies, and parallel-safe flags. Order tasks acyclically (foundations first) and group them into waves for the orchestrator. Confirm the plan with the user before building.

Verification: every FR maps to at least one task, all tasks have acceptance criteria and dependencies, the order is acyclic, and waves are explicit.
