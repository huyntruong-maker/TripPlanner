---
name: using-the-pack
description: Meta router for the claude-fullstack-pack. Maps incoming work to the right skill, agent, or command and defines shared operating rules. Use at the start of any non-trivial task to decide which lifecycle skill applies, or when unsure how the pack fits together.
---

## Overview

This is the entry point for the claude-fullstack-pack. It routes a unit of work to the correct skill/command and enforces operating rules that every other skill assumes. Read this first, route, then hand off.

The pack covers the full lifecycle: spec to plan to build to test to review to ship. It is stack-agnostic but the concrete examples target ASP.NET Core Web API + React/TypeScript.

## When to Use

- At the start of any task larger than a one-line fix.
- When you are unsure which skill or command applies.
- When a request bundles several phases ("build feature X") and needs decomposition.
- To confirm the shared rules (verification, commits, atomic changes) before acting.

## Process

1. Classify the request into a lifecycle phase using the routing table below.
2. If the request spans multiple phases, do not freelance — route to `/orchestrate` to plan waves.
3. Load the matched skill and follow its Process section as written. Checkpoint: name the skill out loud.
4. Apply the shared operating rules throughout, regardless of skill.
5. Finish only when that skill's Verification section is satisfied with evidence.

### Routing table

| Incoming work | Skill | Command | Agent |
|---|---|---|---|
| Onboard an existing project, generate docs | (bootstrap) | `/start` | systems-architect |
| "Coordinate this / who does what" | (n/a) | `/orchestrate` | orchestrator |
| Define what to build, requirements unclear | spec-driven-development | `/spec` | systems-architect |
| Have a spec, need tasks + ordering | planning-and-task-breakdown | `/plan` | orchestrator |
| Write code for a planned task | incremental-implementation | `/build` | backend-lead |
| Add/fix tests, QA a change | test-driven-development | `/test` | qa-engineer |
| Review a diff/branch (quality + security) | code-review-and-quality | `/review` | code-reviewer, security-auditor |
| Commit, branch, PR, release, versioning | git-workflow-and-versioning | `/ship` | cicd-engineer |
| Refresh pack files from upstream | (maintenance) | `/sync-pack` | (n/a) |

## Shared operating rules

1. Always run verification. No task is done without passing build + tests as evidence.
2. Follow Conventional Commits (`type(scope): subject`) for every commit.
3. Prefer small atomic changes — one logical change per commit, ~100-400 lines per review unit.
4. Thin vertical slices over big-bang changes; keep the tree green between commits.
5. Never overwrite user code silently — additive changes, explicit diffs, confirm destructive ops.
6. State assumptions out loud; when ambiguous, ask or record them in the spec.

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "It's small, skip routing." | Routing costs seconds and stops you skipping verification. |
| "I'll just build it, the spec is obvious." | Obvious specs take 5 lines and catch hidden scope. |
| "Tests later." | Later never comes; unverified work is not done. |
| "One big commit is faster." | Big commits are unreviewable and unrevertible. |

## Red Flags

- You started editing code without knowing which skill governs the task.
- Multiple phases are happening in one undifferentiated blob of work.
- You are about to mark something complete with no test/build output.
- A change touches unrelated files "while you're in there."

## Verification

- The chosen skill is named explicitly before work begins.
- Shared rules were applied (Conventional Commits, atomic changes visible in history).
- The downstream skill's own Verification section passed with evidence (test/build logs).
