---
name: git-workflow-and-versioning
description: Guides agents through trunk-based development, Conventional Commits, atomic commits, branch naming, PR hygiene, and semantic versioning. Use when committing, branching, opening PRs, tagging releases, or deciding a version bump.
---

## Overview

Trunk-based development with short-lived branches, atomic Conventional Commits, clean PRs, and SemVer releases. The goal is a readable history, easy reverts, and an automatable changelog.

## When to Use

- Any time you commit, branch, open a PR, or cut a release.
- Deciding whether a change is a patch, minor, or major bump.
- Cleaning up history before merge.

## Process

1. Branch from up-to-date main, short-lived: `feature/<id>-short-desc` (also `fix/`, `chore/`, `docs/`). Checkpoint: branch named by convention.
2. Make atomic commits — one logical change each, building and passing tests. Use Conventional Commits:
   - `feat(scope): ...`, `fix(scope): ...`, `docs:`, `refactor:`, `test:`, `chore:`, `perf:`, `build:`, `ci:`.
   - Breaking change: `feat(scope)!: ...` or a `BREAKING CHANGE:` footer.
   - Reference task/FR IDs in the body when relevant.
3. Keep branches small and rebase/merge main frequently to avoid drift.
4. Before PR: rebase/squash noise into meaningful commits, run full build + tests, self-review the diff.
5. Open a PR with a clear title (Conventional Commit style), description linking spec/plan, and the test evidence.
6. Address review (see code-review-and-quality); resolve all blockers before merge.
7. Merge to trunk (squash or rebase per repo policy); delete the branch.
8. Version with SemVer: patch for `fix`, minor for `feat`, major for breaking changes. Tag and update the changelog from commit history.

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "I'll commit everything together." | Atomic commits keep history bisectable and revertible. |
| "Commit message format doesn't matter." | Conventional Commits drive changelog + version automation. |
| "Long-lived branch is fine." | It drifts from main and creates merge hell. |
| "Just bump the version manually." | SemVer rules map directly from commit types — follow them. |

## Red Flags

- Commit messages like "wip", "fix", "stuff".
- A branch open for weeks, far behind main.
- A breaking change released as a minor/patch bump.
- PR merged with unresolved blocker comments or no test evidence.

## Verification

- Branch follows the naming convention and is short-lived.
- Every commit is atomic and Conventional-Commit formatted; history is clean.
- Build + tests pass on the branch before merge, with evidence in the PR.
- Version bump matches SemVer rules; tag and changelog updated.
