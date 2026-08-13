---
name: spec-driven-development
description: Guides agents through writing a short PRD/spec before code. Use when a feature, change, or task lacks a written definition of objectives, functional requirements, non-goals, and constraints — i.e. before planning or implementing anything non-trivial.
---

## Overview

A spec turns a vague request into a small, testable contract. It is short by design — objectives, numbered functional requirements, explicit non-goals, and constraints. The goal is shared understanding before code, not a heavy document.

Output lives at `docs/specs/<feature>.md` (or appended to `docs/PRD.md`).

## When to Use

- Before `/plan` or `/build` on any feature larger than a trivial fix.
- When the request is ambiguous, has hidden scope, or stakeholders disagree.
- When a change affects an API contract, data model, or user-facing behavior.

Skip only for true one-liners (typo, version bump) — and note the skip.

## Process

1. Capture the objective in 1-3 sentences: who needs this and why. Checkpoint: a reader can restate the goal.
2. List functional requirements as testable statements, IDs `FR-001`, `FR-002`, ... Each says what the system shall do, not how.
3. List explicit non-goals — what this work will NOT do. This is the main scope-creep brake.
4. List constraints: stack, performance budgets, security/compliance, deadlines, dependencies.
5. Record open questions and assumptions; resolve blocking ones before proceeding. Checkpoint: no blocking unknowns remain.
6. Define acceptance per FR — how we will know it is met (link to future tests).
7. Get a thumbs-up (user or orchestrator) before moving to planning.

### Spec template

```
# Spec: <feature>
## Objective
<who + why, 1-3 sentences>
## Functional Requirements
- FR-001: The system shall ...
- FR-002: The system shall ...
## Non-Goals
- This will not ...
## Constraints
- Stack / perf / security / deadline / deps
## Open Questions & Assumptions
- Q: ...  (assumption: ...)
## Acceptance
- FR-001 verified by <test/behavior>
```

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "I already know what to build." | Then writing it costs 5 minutes and exposes gaps. |
| "Specs slow us down." | Rework from missed scope is far slower. |
| "Requirements will change." | Numbered FRs make change tracking trivial. |
| "Non-goals are obvious." | Unwritten non-goals are the top source of scope creep. |

## Red Flags

- You cannot list 2+ concrete functional requirements.
- "It depends" answers with no recorded assumption.
- The spec describes implementation (how) instead of behavior (what).
- Missing or empty non-goals section.

## Verification

- `docs/specs/<feature>.md` exists with Objective, FR-IDs, Non-Goals, Constraints, Acceptance.
- Every FR is a uniquely numbered, testable statement.
- Open questions are resolved or explicitly accepted as assumptions.
- A reviewer signed off before planning began.
