# Git Workflow (Common)

- Trunk-based: branch off `main`, keep branches short-lived, merge via PR.
- Branch names: `type/short-description` (e.g. `feat/order-pagination`, `fix/cors-headers`).
- Commit messages follow Conventional Commits: `type(scope): summary`.
  - Types: feat, fix, docs, refactor, test, chore, ci, build, perf.
- Make small, atomic commits that each leave the build green.
- Never commit secrets, credentials, `.env` files, or large binaries.
- Rebase or update from `main` before merging; resolve conflicts locally.
- Every PR: descriptive title, linked issue, passing CI, at least one review.
- Squash-merge feature branches to keep `main` history linear and readable.
- Tag releases with semver (`vMAJOR.MINOR.PATCH`); never reuse a tag.
- Do not force-push shared branches; never rewrite `main`.
- Prefer feature flags over long-lived branches for incomplete work.

Keep `main` always releasable.
