# Build Playbook — Travel Trip Planner

Copy-paste prompts for building the app **backend-first**, following the phases in
`docs/BACKLOG.md` ("Implementation plan"). Order is strict: finish Phase 0 → Phase 1
(all MVP DB + API) → Phase 2 (frontend) → Phase 3 (enhancements). Do not start a feature's
UI until its API contract is implemented and integration-tested.

Per feature, the flow is: contract → endpoint → docs, then `/review` → `/ship`. The MVP DB schema is
designed and migrated once up front in **Phase 1.0** (before any feature endpoints). **Unit tests are
deferred for now**; integration tests are optional (recommended only for security-sensitive flows like auth).
Branch per story: `feature/<feature>-<us>-slug` (e.g. `feature/auth-us1-signup`).

---

## Local environment — Docker Engine (no Docker Desktop)

Backing services (PostgreSQL, Redis, MinIO) run via `docker compose`. **Docker Desktop is NOT used
(company policy)** — use **Docker Engine** instead (e.g. on WSL2 or a Linux host). The `docker compose`
commands are identical; only the daemon differs.

- Start ONLY the database (enough for Phase 1.0 migrations and most backend work):
  ```
  docker compose up -d postgres
  ```
- Start everything: `docker compose up -d`   ·   stop: `docker compose down`
- DB is reachable at `localhost:5432` (user `postgres`, db `trip-planner`).
- On WSL2, run `docker compose` and `dotnet` from the same WSL shell so they share the Engine daemon. If a
  tool can't find Docker, set the context: `docker context use default` (or `export DOCKER_HOST=unix:///var/run/docker.sock`).
- Testcontainers-based tests (only if/when added) also need a reachable Docker Engine daemon — same setup.

---

## Kickoff (first time only)

If this pack was dropped into an **existing** repo, first reconcile it with the codebase
(see `INTEGRATION.md` → "After attaching"): read the existing code base, update `.claude/rules/` to
match the real style/conventions, fix "Stack defaults", and **reconcile** `docs/ARCHITECTURE.md` /
`API.md` / `DATABASE.md` with the existing code — compare what already exists and update both sides to
agree, **do not** overwrite or rewrite from scratch. For style/conventions specifically, the repo wins
over pack defaults.

```
/start Read the entire existing codebase and report the stack, structure, and conventions. Then update .claude/rules to match the real style, and reconcile docs/ARCHITECTURE.md, API.md, DATABASE.md with the existing code (compare and update both sides to agree, don't rewrite from scratch). Style: the repo wins over pack defaults.
```

Once reconciliation is done, run the kickoff against the plan:

```
/start Onboard the trip planner project per docs/PRD.md and docs/BACKLOG.md. Confirm the stack and build a TODO from the backend-first Implementation plan.
```

---

## Phase 0 — Foundation

**Current state (this repo) — mostly DONE, do NOT re-scaffold.** Already present: 4-layer solution
(WebApi/Application/Domain/Infrastructure), EF Core + PostgreSQL with migrations, Identity tables,
`/healthz` health check, Serilog, docker-compose (API + PostgreSQL + Redis + MinIO), Swagger.
**Remaining gaps (see `docs/TODO.md`): CI and test projects.**

```
/orchestrate "Finish Phase 0 gaps ONLY (do not re-scaffold): (P0-6) add .github/workflows/ci.yml triggering on push + pull_request to main and workflow_dispatch, running dotnet restore/build/test + dotnet format --verify-no-changes on ubuntu-latest with the SDK from global.json (.NET 10); (P0-7/TD1) add Tests.Unit and Tests.Integration projects to the solution with one smoke test each (integration via WebApplicationFactory). Backend/infra only. Update docs/TODO.md statuses and stop for my review."
```

Optional quick fixes while you're here (from `docs/TODO.md` tech-debt): TD6 Dockerfile targets .NET 8 but
code is .NET 10; TD8 duplicate `UseSerilog` in `Program.cs`; TD3 CORS allows any origin; TD4 secrets in
`appsettings.json`.

Gate:

```
/review Confirm CI runs on push/PR, build + format pass, and docs/TODO.md statuses are updated. (Test projects are deferred — unit tests not needed yet; the CI test step no-ops until tests exist.)
```

---

## Phase 1 — Backend & Database (build before any UI)

Do one feature at a time. After each: run the gate, then move on. **Unit tests are deferred** — focus on
DB + API; the CI `dotnet test` step no-ops until test projects are added later.

### 1.0 Database schema & migrations for MVP (design + migrate FIRST)

Design and migrate ALL MVP business tables before building any feature endpoints, so each feature is built
on a stable schema. The existing migrations cover Identity/Quartz only — the MVP business tables do not
exist yet.

```
/orchestrate "Phase 1.0 DB schema for MVP (design + migrate first, NO feature endpoints yet): per docs/DATABASE.md add entities + EF Core migrations for EmailVerificationTokens, Trips, ItineraryDays, TripDestinations, and optional DestinationCache. Follow the repo's dual-DbContext (Read/Write) + UnitOfWork conventions. Run the migration locally, update docs/DATABASE.md, and stop for my review."
```

Gate:

```
/review Phase 1.0: migrations created and run cleanly, schema matches docs/DATABASE.md, dual-DbContext/UnitOfWork conventions followed.
```

### 1.1 Auth API — F4 US1–US4

**Current state — auth PARTIALLY exists, augment it, don't rebuild.** Already present: login (uses
`Username`, not email), logout (PUT), change/forgot/reset-password, JWT access+refresh, Identity tables.
Missing/needs work (see `docs/TODO.md` T1–T9): public **register** (F4-US1), **EmailVerificationTokens** +
**verify-email** (F4-US2), switch login to accept **email** (T5), verify logout meets AC (T6), and
**integration tests** for the whole flow.

```
/plan "Phase 1.1 Auth (F4 US1–US4), AUGMENT existing auth (do not rebuild): add register (unique email, password ≥8, bcrypt, generic errors), verify-email endpoint reusing the existing EmailService (EmailVerificationTokens table comes from Phase 1.0), make login accept email (T5), verify logout AC (T6). Follow the repo's CQRS/MediatR + ResultRes conventions."
```
```
/orchestrate "Build Phase 1.1 Auth per the approved plan. Update docs/API.md & docs/DATABASE.md. Backend only. Unit tests deferred; optionally add ONE happy-path integration test for register→verify→login if quick."
```

### 1.2 Destination Suggestion API — F1 US2, US3

```
/orchestrate "Phase 1.2 Destination Suggestion API (F1 US2,US3): IDestinationProvider/IGeocodingProvider (OpenTripMap+Foursquare), GET /locations/search (cities+countries, exact-first, max 5, dedupe, case-insensitive, partial) and GET /attractions (coords+radius, city 20km, max 20/page, empty 'No attractions found'), caching to meet NFR-1 ≤500ms & NFR-2 ≤1000ms. Include tests + docs."
```

### 1.3 Destination Details API — F2 US1, US2, US4

```
/orchestrate "Phase 1.3 Destination Details API (F2 US1,US2,US4): GET /destinations/{providerPlaceId} returning name/category/description/photos/address?/website?/openingHours, opens even when fields are missing, NFR-3 ≤2s. Include tests + docs."
```

### 1.4 Trips API + DB — F3 US1, US2, US3, US7, US8, US10

```
/orchestrate "Phase 1.4 Trips API (F3 US1,US2,US3,US7,US8,US10) — tables from Phase 1.0: trip CRUD (name required); set dates → generate one ItineraryDay per date (start ≤ end, warn when reducing days drops items); add-destination requires itineraryDayId; remove; load saved trips (empty state). Auth-gate + NFR-6 on every query. Update docs. Unit tests deferred."
```

### Gate per feature (and exit gate for Phase 1)

```
/review Current branch: all of the feature's endpoints present and working, relevant NFRs met (NFR-1/NFR-6), docs/API.md & docs/DATABASE.md updated. (Unit tests deferred; integration tests optional for now.)
```
```
/ship feature/<feature>-<us>-slug
```

Phase 1 is done when: all MVP endpoints are implemented, integration tests are green, NFR-1 and NFR-6 are verified, and docs are updated.

---

## Phase 2 — Frontend (only after Phase 1 is done)

```
/orchestrate "Phase 2 Frontend, wired to the stable APIs, in feature order: auth (pages + route guards) → search box + results grid (thumbnails/placeholders) → detail view + photo carousel + 'Opening hours not available' fallback (Add to Trip disabled when logged out) → trip list + planner board (create trip, set dates, add to a selected day, remove, load saved). Use TanStack Query + react-hook-form/zod."
```

Gate:

```
/review UI: renders loading/empty/error/success states, basic accessibility (roles/labels/focus), RTL/MSW tests green, typecheck + lint clean.
```

---

## Phase 3 — Enhancements (post-MVP, backend-then-frontend per item)

Pick one at a time:

```
/orchestrate "F1-US1 Autocomplete for the search field (≥2 chars, up to 5 suggestions). Backend first, then frontend."
```
```
/orchestrate "F1-US4 Filter + F1-US5 Sort attractions (category, rating/popularity; sort recommended/highest). Backend first, then frontend."
```
```
/orchestrate "F2-US3 View map + location info (marker, zoom/pan, address label)."
```
```
/orchestrate "F3-US4/US5/US6 Drag-drop scheduling, reorder within a day, drag between days — meet NFR-4 ≤100ms. Backend (position/itineraryDayId) first, then frontend with dnd-kit."
```
```
/orchestrate "F3-US9 Autosave trips/destinations (saving indicator, retry on failure)."
```

---

## Tips to stay backend-first
- Phase 0 and Phase 1: DB + API only, no frontend. **Unit tests deferred for now.**
- Design + migrate the full MVP DB once in **Phase 1.0** before building feature endpoints.
- Do one feature at a time; finish `/review` + `/ship` before moving to the next.
- One branch per story (`feature/...`), commit with Conventional Commits.
- Always enforce NFR-1 (search ≤500ms) and NFR-6 (own-data-only) on the relevant stories.
