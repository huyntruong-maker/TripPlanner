---
description: Invoke the orchestrator agent to plan a multi-discipline task into waves, get approval, and run the right specialists in order.
argument-hint: "<feature or task to coordinate>"
---

Coordinate this work using the orchestrator agent: $ARGUMENTS

Steps:
1. Invoke the `orchestrator` agent with the task ($ARGUMENTS).
2. Have it restate scope and acceptance criteria, asking clarifying questions if ambiguous.
3. Have it map the task to specialists (systems-architect, backend-lead, frontend-lead, database-expert, qa-engineer, cicd-engineer, code-reviewer, security-auditor, docs-writer) and produce a WAVE plan — parallel where independent, sequential where one output feeds the next.
4. Create the feature branch before any specialist writes code.
5. Present the wave plan and WAIT for user approval before running anything.
6. Run waves in order. After each wave, summarize what changed and which docs updated.
7. If any wave fails (build break, failing tests, unmet criteria), STOP and ask how to proceed — do not auto-advance.
8. When all waves pass, summarize the full change set, branch name, and suggest opening a PR (`/ship`).

Verification: an approved wave plan exists, specialists ran in dependency order, each wave's output was verified, and a final summary with the branch name is reported.
