# TODO Backlog — Travel Trip Planner

Generated: 2026-06-17. Pick work in wave order (Phase 0 → 1 → 2 → 3). See `docs/BACKLOG.md` for
full acceptance criteria and `docs/BUILD-PLAYBOOK.md` for `/orchestrate` prompts.

---

## Phase 0 — Foundation gaps (done or in-progress)

| ID | Task | Status | Notes |
|----|------|--------|-------|
| P0-1 | Scaffold 4-layer solution (WebApi, Application, Domain, Infrastructure) | ✅ Done | |
| P0-2 | EF Core + PostgreSQL wired, initial migration runs | ✅ Done | Identity tables exist |
| P0-3 | Health check endpoint `/healthz` | ✅ Done | Returns JSON |
| P0-4 | Serilog structured logging | ✅ Done | Console + rolling file |
| P0-5 | Docker-compose (API + PostgreSQL + Redis + MinIO) | ✅ Done | |
| P0-6 | GitHub Actions CI (build + test + lint) | ✅ Done | `.github/workflows/ci.yml` added |
| P0-7 | Any test project (unit or integration) | ❌ Missing | No test projects in solution |
| P0-8 | Swagger/OpenAPI reachable in dev | ✅ Done | `/swagger` when dev or `EnableSwagger=true` |

---

## Phase 1 — Backend & Database (MVP features)

### 1.1 Auth API — F4 US1–US4  `feature/auth-us1-signup`

| ID | Task | AC ref | Status |
|----|------|--------|--------|
| T1 | `POST /api/v1/auth/register` — unique email, password ≥8 chars, bcrypt hash, generic errors | F4-US1 | ❌ |
| T2 | `EmailVerificationTokens` table + migration | F4-US2 | ❌ |
| T3 | `GET /api/v1/auth/verify-email?token=` — activate `EmailConfirmed`, handle expired/invalid | F4-US2 | ❌ |
| T4 | Send verification email on register (reuse existing `EmailService`) | F4-US2 | ❌ |
| T5 | `POST /api/v1/auth/login` — align to PRD: accept `email` not just `username` | F4-US3 | ⚠️ Partial (username-based exists; needs email alias) |
| T6 | Logout revokes token (currently PUT, verify it meets F4-US4 AC) | F4-US4 | ⚠️ Exists, needs AC verification |
| T7 | Integration tests: register, verify, login, refresh, logout flows | Phase 1 exit | ❌ |
| T8 | Security review: no account enumeration on register/forgot-password | F4-US1 BR | ❌ |
| T9 | Update `docs/API.md` & `docs/DATABASE.md` with final auth contract | Phase 1 exit | ❌ |

### 1.2 Destination Suggestion API — F1 US2, US3  `feature/suggestion-us2-search`

| ID | Task | AC ref | Status |
|----|------|--------|--------|
| T10 | Define `IGeocodingProvider` + `IDestinationProvider` interfaces | Architecture | ❌ |
| T11 | OpenTripMap client: geocoding (name → lat/lng, ranked, max 5, dedupe) | F1-US2 | ❌ |
| T12 | `GET /api/v1/locations/search?q=` endpoint (≤500 ms NFR-1) | F1-US2 | ❌ |
| T13 | OpenTripMap client: POIs near coordinates (radius, max 20/page) | F1-US3 | ❌ |
| T14 | Foursquare client: enrich POIs with categories + reviews | F1-US3 | ❌ |
| T15 | `GET /api/v1/attractions?lat=&lng=&radius=&page=` endpoint | F1-US3 | ❌ |
| T16 | Redis caching for geocoding + attractions responses (meet NFR-1/NFR-2) | NFR-1/2 | ❌ |
| T17 | Optional `DestinationCache` table for popular places | NFR-2/5 | ❌ |
| T18 | Integration tests for search + attractions endpoints | Phase 1 exit | ❌ |

### 1.3 Destination Details API — F2 US1, US2, US4  `feature/details-us1-detail-view`

| ID | Task | AC ref | Status |
|----|------|--------|--------|
| T19 | `GET /api/v1/destinations/{providerPlaceId}` (name, category, description, photos, address?, website?, openingHours?) | F2-US1/US2/US4 | ❌ |
| T20 | Detail view opens even if some fields are missing (graceful partial response) | F2-US1 BR | ❌ |
| T21 | NFR-3 ≤2s — cache destination details by providerPlaceId | NFR-3 | ❌ |
| T22 | Integration tests for details endpoint | Phase 1 exit | ❌ |

### 1.4 Trips API + DB — F3 US1, US2, US3, US7, US8, US10  `feature/trips-us1-create-trip`

| ID | Task | AC ref | Status |
|----|------|--------|--------|
| T23 | `Trips` table + migration (UserId FK, Name, StartDate, EndDate, CreatedAt, UpdatedAt) | F3-US1/US2 | ❌ |
| T24 | `ItineraryDays` table + migration (TripId FK, Date, DayIndex) | F3-US2 | ❌ |
| T25 | `TripDestinations` table + migration (TripId, ItineraryDayId nullable, ProviderPlaceId, …) | F3-US3 | ❌ |
| T26 | `POST /api/v1/trips` — create trip (name required) | F3-US1 | ❌ |
| T27 | `PUT /api/v1/trips/{id}` — set name/dates; generate one ItineraryDay per date; warn when reducing drops items | F3-US2 | ❌ |
| T28 | `POST /api/v1/trips/{id}/destinations` — add destination; `itineraryDayId` required (MVP) | F3-US3 | ❌ |
| T29 | `DELETE /api/v1/trips/{id}/destinations/{destinationId}` | F3-US7 | ❌ |
| T30 | `GET /api/v1/trips` — list user's trips (empty state allowed) | F3-US10 | ❌ |
| T31 | `GET /api/v1/trips/{id}` — trip with itinerary days + destinations | F3-US10 | ❌ |
| T32 | Auth-gate all trip writes; enforce NFR-6 (own-data-only) on every query | F3-US8/NFR-6 | ❌ |
| T33 | Integration tests: trip CRUD, add/remove destination, load saved trips | Phase 1 exit | ❌ |

---

## Phase 2 — Frontend (after Phase 1 API contracts are stable)

| ID | Area | Task | Status |
|----|------|------|--------|
| T34 | Auth UI | Sign-up, email verification, login, logout pages + route guards | ❌ |
| T35 | Search UI | Search box + attractions results grid (thumbnails/placeholders) | ❌ |
| T36 | Details UI | Destination detail view + photo carousel + "Opening hours not available" fallback | ❌ |
| T37 | Trip Planner UI | Trip list + planner board (create, set dates, add to day, remove, load saved) | ❌ |

---

## Phase 3 — Enhancements (post-MVP)

| ID | Area | Task | Status |
|----|------|------|--------|
| T38 | Search | F1-US1 Autocomplete (≥2 chars, up to 5 suggestions) | ❌ |
| T39 | Search | F1-US4 Filter + F1-US5 Sort attractions | ❌ |
| T40 | Details | F2-US3 Map/location info (Leaflet) | ❌ |
| T41 | Trip Planner | F3-US4/US5/US6 Drag-drop + reorder + move between days (NFR-4 ≤100ms) | ❌ |
| T42 | Trip Planner | F3-US9 Autosave (saving indicator, retry on failure) | ❌ |

---

## Technical debt / infrastructure gaps

| ID | Task | Priority |
|----|------|----------|
| TD1 | Test projects — **Deferred** (unit tests not needed yet; CI `dotnet test` no-ops until added) | Deferred |
| TD2 | CI/CD: `.github/workflows/ci.yml` added (build + format; test no-op) — ✅ Done | Done |
| TD3 | CORS: `SetIsOriginAllowed(_ => true)` — restrict to explicit allow-list before prod | High |
| TD4 | Secrets in `appsettings.json` (JWT secret, MinIO key) — move to `dotnet user-secrets` dev / env vars prod | High |
| TD5 | SMTP credentials are empty — configure before testing email flows | Medium |
| TD6 | Dockerfile targets .NET 8 runtime (`mcr.microsoft.com/dotnet/runtime:8.0`) but code targets .NET 10 — update | Medium |
| TD7 | MinIO and Quartz infrastructure wired but no feature uses them yet — verify they don't fail startup in CI | Low |
| TD8 | `Program.cs` has duplicate `builder.Host.UseSerilog(...)` call | Low |
