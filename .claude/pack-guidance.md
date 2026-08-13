# fullstack-pack — operating guidance (drop-in)

Imported into the project's `CLAUDE.md` via `@.claude/pack-guidance.md`.

## Available in `.claude/`
- agents/ — orchestrator + backend-lead (.NET), frontend-lead (React), database-expert (EF Core),
  qa-engineer, cicd-engineer, deploy-engineer, systems-architect, code-reviewer, security-auditor, docs-writer.
- skills/ — lifecycle (spec→plan→build→test→review→ship) + stack (.NET Web API, React, EF Core, API, security, ci-cd, deploy).
- commands/ — /start /orchestrate /spec /plan /build /test /review /ship /sync-pack.
- rules/, hooks/, mcp/.

## Source of truth for THIS project
- `docs/PRD.md` — product requirements (travel trip-planner).
- `docs/BACKLOG.md` — feature → user-story plan, MVP scope, implementation waves. Pick work from here.
- `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/DATABASE.md` — living technical docs (owning agent updates them).

## Default workflow
1. Pick a story from `docs/BACKLOG.md` (respect the MVP / wave order).
2. `/spec` (if unclear) → `/plan` → `/orchestrate "<story>"` or `/build`.
3. Verify before done: tests pass, build succeeds, lint clean.
4. `/review` (code-reviewer + security-auditor) → `/ship`.

## Hard rules
- Conventional Commits; branches `feature/<feature>-<us>-desc` (e.g. `feature/auth-us1-signup`).
- Small atomic, rollback-friendly slices.
- Enforce NFRs: search ≤500ms (NFR1), authorization = users touch only their own data (NFR6).
- Be additive in an existing codebase; match its conventions over pack defaults.

## Stack defaults
Backend: ASP.NET Core Web API (.NET 8+), EF Core, FluentValidation, ProblemDetails, JWT auth.
Frontend: React + TypeScript (Vite), TanStack Query, react-hook-form + zod.
Tests: xUnit, Vitest/Testing Library, Playwright. External data: OpenTripMap + Foursquare.
