---
name: ci-cd-and-automation
description: Guides agents through GitHub Actions CI/CD for .NET + React — build, test, lint gates, caching, matrices, and quality gates. Use when creating or improving CI workflows for the full-stack pack.
---

# CI/CD and Automation

## Overview
GitHub Actions pipelines that gate every PR for an ASP.NET Core API and a React app:
build, test, lint, and security shift-left, with caching and matrix builds for speed.

## When to Use
- Creating or editing `.github/workflows`.
- Adding quality gates or speeding up CI.
- Wiring feature flags or release automation triggers.

## Process

1. Run separate jobs for backend and frontend so failures are isolated and parallel.
   ```yaml
   jobs:
     dotnet: { runs-on: ubuntu-latest }
     web:    { runs-on: ubuntu-latest }
   ```

2. Cache aggressively to cut build time.
   ```yaml
   - uses: actions/setup-dotnet@v4
     with: { dotnet-version: '8.0.x', cache: true }
   - uses: actions/setup-node@v4
     with: { node-version: '20', cache: 'npm' }
   ```

3. Gate the .NET job: restore, build (warnings as errors), format check, test + coverage.
   ```yaml
   - run: dotnet format --verify-no-changes
   - run: dotnet build -warnaserror
   - run: dotnet test --collect:"XPlat Code Coverage"
   ```

4. Gate the web job: install, typecheck, lint, test, build.
   ```yaml
   - run: npm ci
   - run: npm run typecheck && npm run lint
   - run: npm test -- --run && npm run build
   ```

5. Use a matrix where it adds value (e.g., multiple .NET or Node versions, OSes).
   ```yaml
   strategy: { matrix: { dotnet: ['8.0.x'], os: [ubuntu-latest] } }
   ```

6. Shift security left: run dependency audits in CI and fail on high severity.
   ```yaml
   - run: dotnet list package --vulnerable --include-transitive
   - run: npm audit --audit-level=high
   ```

7. Make all gates required branch-protection checks so PRs cannot merge while red.

8. Keep workflows fast (<10 min target): cache, parallelize, run only affected scopes via
   `paths`/`paths-filter` when the repo is large.

9. Use feature flags (config or a flag service) to merge incomplete work behind a switch
   instead of long-lived branches; CI stays green on `main`.

10. Pin action versions and least-privilege `permissions:` per job.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Run tests after merge." | Gate before merge; broken main blocks everyone. |
| "Caching is premature optimization." | Cold installs dominate CI time; cache early. |
| "Audits slow the pipeline." | A few seconds vs shipping known CVEs. |
| "One giant job is simpler." | Split jobs surface the real failure faster. |
| "Long feature branches are fine." | They rot and conflict; use flags + trunk. |

## Red Flags
- No required status checks on the default branch.
- Tests or lint not run in CI, only locally.
- No dependency caching; multi-minute installs every run.
- `permissions: write-all` or unpinned `@master` actions.
- Security audits absent from the pipeline.

## Verification
- PRs cannot merge unless build, test, lint, typecheck pass.
- Cache hit rates visible; CI runtime within target.
- Dependency audit step present and enforced.
- Workflow uses pinned actions and scoped permissions.
- `main` stays green; incomplete work sits behind flags.
