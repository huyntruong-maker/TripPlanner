---
description: Prepare a change for shipping — apply git-workflow-and-versioning plus CI/CD checks, then prepare a PR or release.
argument-hint: "<feature/branch to ship; or 'release <version>'>"
---

Ship the work using the `git-workflow-and-versioning` skill: $ARGUMENTS

Steps:
1. Ensure the branch follows naming convention and is rebased on up-to-date main.
2. Confirm commits are atomic and Conventional-Commit formatted; squash/clean noise.
3. Run the full build, tests, lint, and any CI checks locally (invoke the `cicd-engineer` agent if CI config needs inspection). Capture passing output.
4. Prepare the PR: clear Conventional-style title, description linking spec/plan, and test evidence. Resolve any outstanding review blockers (`/review`).
5. For a release: compute the SemVer bump from commit types (patch=fix, minor=feat, major=breaking), tag, and update the changelog from commit history.

Verification: build + tests + CI checks pass with evidence, the PR has a clean title/description/test log, blockers are resolved, and any version bump matches SemVer with tag + changelog updated.
