# Architecture — Travel Trip Planner

Owner agent: systems-architect. Living doc — update when structure changes.

## Overview
React SPA → ASP.NET Core Web API → (EF Core) SQL database + cache + external providers (OpenTripMap, Foursquare).

```
[React SPA] --HTTPS/JSON--> [ASP.NET Core Web API] --EF Core--> [SQL Server / PostgreSQL]
                                   |  \--> [Cache: IMemoryCache / Redis]
                                   \--> [Provider clients: OpenTripMap, Foursquare]
```

## Backend (ASP.NET Core Web API, .NET 8+)
Layered solution:
- `Api` — controllers/minimal endpoints, auth middleware, ProblemDetails, request validation (FluentValidation).
- `Application` — use-case services (search, attractions, details, trips), DTOs.
- `Domain` — entities (User, Trip, ItineraryDay, TripDestination), rules.
- `Infrastructure` — EF Core DbContext + migrations, provider clients, caching, email sender.

Cross-cutting: JWT auth (access + refresh), global exception → ProblemDetails, structured logging, options pattern for provider keys.

### External providers
Wrap behind `IDestinationProvider` and `IGeocodingProvider` so providers are swappable and mockable in tests.
- OpenTripMap: geocoding (name → lat/lng), POIs by radius (city default 20 km).
- Foursquare: enrich POIs with categories/reviews.
Responses normalized to internal `DestinationDto`. Cache normalized results (see NFRs).

### Performance / scalability (NFR-1,2,5)
- Cache geocoding + attractions responses (key = normalized query / placeId), short TTL.
- Pagination (max 20/page) and provider-side filtering; never load all records.
- Optional `DestinationCache` table for popular places to cut provider latency.

## Frontend (React + TypeScript, Vite)
- Server state: TanStack Query (search, attractions, details, trips) with caching/optimistic updates.
- Client state: lightweight store (Zustand) for trip-board UI; forms via react-hook-form + zod.
- Routing with guarded routes (auth required for trip pages). Accessibility: WCAG 2.1 AA.
- Map (Leaflet) and drag-drop (dnd-kit) are deferred (Wave 5).

## Auth & authorization
- JWT access token (short-lived) + refresh token; "stay signed in after refresh".
- Email verification token before activation.
- NFR-6: every trip/destination query is scoped by the authenticated `UserId`; no cross-user access.

## Environments
local (docker-compose: api + web + db) → staging → production. Health check endpoint for probes.

---

## Reconciliation against actual code (2026-06-17)

The section above is the original design intent. This section records where the live codebase
currently differs or adds detail. **This section wins when there is a conflict.**

### Actual runtime: .NET 10 (not .NET 8+)
`global.json` pins SDK `10.0.0` (rollForward: latestMinor); all `.csproj` files target `net10.0`.

### Actual project/layer names
| Design name | Actual project |
|-------------|---------------|
| `Api` | `WebApi` |
| `Application` | `Application` |
| `Domain` | `Domain` |
| `Infrastructure` | `Infrastructure` |

### Solution structure & key dependencies
- **API versioning**: Asp.Versioning.Mvc v8.1.0 — all controllers under `api/v{version}/`.
- **AutoMapper**: request models → commands; commands → response DTOs (profiles in `WebApi/Mappers/`).
- **MediatR v14**: CQRS. Commands implement `ICommand<T>`, queries `IQuery<T>`. `ISender` is injected as an **action-method parameter** (not constructor) via ASP.NET endpoint DI.
- **Serilog**: structured logging to console + rolling file (configured via `appsettings.Development.json`).
- **Quartz v3** (+ PostgreSQL persistence): background job scheduler. Migrations handled by `AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL`.
- **SMTP email**: `EmailService` sends HTML template emails (new-device alert, password reset). Templates in `WebApi/wwwroot/emails/`.
- **MinIO**: S3-compatible object storage (`StorageService`). Not yet used by any feature endpoint.
- **Redis** (StackExchange.Redis): caching via `CacheManager` (get/set/remove/pattern-remove). Used at the infrastructure level; not yet wired into any feature query.
- **ASP.NET Core Identity**: `User` extends `IdentityUser<Guid>`. `WriteDbContext` extends `IdentityDbContext<User, Role, Guid, …>`.

### Database: PostgreSQL only
The codebase targets **PostgreSQL 16** exclusively (Npgsql.EntityFrameworkCore.PostgreSQL v10).
The design doc says "SQL Server or PostgreSQL" — ignore SQL Server; it is not configured.

### Dual DbContext (Read/Write split)
- `WriteDbContext` — all mutations; injected via `IWriteUnitOfWork`.
- `ReadDbContext` — all queries; injected via `IReadUnitOfWork`.
- Both connect to the same physical database. `Replication.ReadDelay` (1000 ms) is a config stub for
  a future read-replica; it does not currently change routing.

### Validation & error response (not ProblemDetails)
Controllers validate inputs manually with guard clauses. All responses use the custom envelope:
```json
{ "success": bool, "errorCode": "string|null", "error": "string|null", "validates": [] }
```
Error-code string constants live in `Domain/Messages/*ControllerMsg.cs`. **FluentValidation is not used.**
RFC 7807 ProblemDetails is **not** used in any existing controller.

### Auth system (implemented vs. designed)
The implemented auth system is richer than the PRD design:
- Login uses **username** (not email) + password.
- Single-session-per-user token storage in `UserTokens` table (one row per user). Device
  fingerprinting/tracking (`DeviceUuid`, `DeviceInfo`, `LocationInfo`) and the "new device login"
  email notification were removed 2026-07-05 as unused scope — a new login now simply replaces the
  user's existing session.
- Account lockout: 5 failed attempts → 60-minute lockout.
- JWT access token (15 min) + refresh token (30 days / 7 days short session), stored in `UserTokens`.
- `POST /register` and `GET /verify-email` are implemented (F4-US1/US2 per the PRD).

### Destination Suggestion API (F1-US2, F1-US3 — implemented 2026-06-20)
External provider integration for location search and attraction discovery.

**Provider abstraction layer** (`Application/Interfaces/Providers/`)
- `IGeocodingProvider` — geocodes a free-text city/country query → `IReadOnlyList<LocationDto>`.
- `IDestinationProvider` — fetches attractions by lat/lon/radius → `AttractionSearchResultDto`; fetches detail by provider ID → `AttractionDto?`.

**Concrete implementations** (`Infrastructure/Providers/`)
- `OpenTripMapGeocodingProvider` — calls the OpenTripMap `/geoname` endpoint for the primary
  best-match lookup, plus a follow-up `/geoname` call per matching country name (from .NET's
  built-in `RegionInfo` table) so both cities and countries can be surfaced for a partial query
  (F1-US2). `/geoname` cannot return several candidates in one call, so this provider returns raw,
  unranked, possibly-duplicate candidates — deduping/ranking/clamping is applied afterward by
  `Application.Common.Utils.LocationResultRanker` in `SearchLocationsQueryHandler`, keeping that
  business rule provider-agnostic.
- `OpenTripMapDestinationProvider` — calls the OpenTripMap `/radius` endpoint (`rate≥3` quality filter) and the `/xid/{id}` detail endpoint.
- `FoursquareDestinationProvider` — calls Foursquare Places API v3 `/places/search` and `/places/{id}`.
- `FoursquareEnrichedDestinationProvider` (`Infrastructure/Providers/Foursquare/`) — composite
  `IDestinationProvider`: OpenTripMap is the primary POI source; Foursquare enriches
  category/rating/thumbnail/hours by matching each attraction to a Foursquare place (name +
  coordinates, ≤ 300 m), best-effort per item. Degrades to unenriched OpenTripMap results (with a
  logged warning per item) if Foursquare is unconfigured, disabled, fails, or a match isn't found —
  the attractions request never fails because of Foursquare.

**Primary provider selection** — config-driven, resolved in `ProvidersInjection.cs`:
- `Providers:Geocoding:Provider` — `"Nominatim"` (default, up to 5 ranked results) or `"OpenTripMap"` (single best match).
- `Providers:Destination:Provider` — `"OpenTripMap"` (default; optionally enriched by Foursquare, see below) or `"Foursquare"` (used directly as a full standalone source — it already implements `IDestinationProvider` on its own, so no enrichment step runs).
- `Providers:Foursquare:EnableEnrichment` (default `true`) — master on/off switch for the enrichment step, independent of whether an API key is configured (both must hold for enrichment to run).
Unknown values fall back to the default and log a warning.

**Caching decorators** (`Infrastructure/Providers/Caching/`)
Both providers are wrapped by Redis-backed caching decorators that satisfy the NFRs. TTLs are config-driven, defaulting to:
- `CachedGeocodingProvider` — `Caching:Locations:TtlHours` (default 1h; NFR-1: ≤ 500 ms for 95% of location searches).
- `CachedDestinationProvider` — `Caching:Destinations:AttractionListTtlMinutes` (default 30 min; NFR-2: ≤ 1000 ms) and `Caching:Destinations:AttractionDetailTtlHours` (default 24h).
Cache keys are content-based (query text / lat+lon+radius+page); API keys are never included in cache keys or log output.

**DI wiring** — `ProvidersInjection.AddProviders()` registered in `CoreDependencyConfiguration`.

**Controller** (`WebApi/Controllers/v1/DestinationController`)
- `GET /api/v1/destinations/locations/search?query=&maxResults=` — `[AllowAnonymous]`
- `GET /api/v1/destinations/attractions?latitude=&longitude=&radiusMeters=&page=&pageSize=` — `[AllowAnonymous]`
- `GET /api/v1/destinations/{providerPlaceId}` — `[AllowAnonymous]` (F2-US1, US2, US4)
Guard clauses at top of each action; `ResultRes<T>` response envelope; `ISender` injected as action-method parameter.

**Config keys** (`Domain/Constants/ConfigKeys.Providers`)
```
Providers:OpenTripMap:BaseUrl
Providers:OpenTripMap:ApiKey
Providers:Foursquare:BaseUrl
Providers:Foursquare:ApiKey
```
Keys are read from `appsettings.json` (stub values; set real keys in user-secrets or env vars).

### Destination Details API (F2-US1, F2-US2, F2-US4 — implemented 2026-06-28)
`GET /api/v1/destinations/{providerPlaceId}` fetches full destination detail via the existing
`IDestinationProvider.GetAttractionDetailAsync` method.

**New types added**
- `Application/Dtos/Destinations/OpeningHoursDto` — structured opening-hours value: `DisplayText`, `WeekdayText`, `IsOpenNow`.
- `Application/Dtos/Destinations/DestinationDetailDto` — full detail DTO: all `AttractionDto` fields plus `Description`, `Photos` (list), `Website`, `OpeningHours`.
- `Application/Dtos/Destinations/AttractionDto` — extended with `Description`, `Photos`, `Website`, `OpeningHours` (null/empty for list calls).
- `Application/Features/Destinations/Queries/GetAttractionDetailQuery/` — CQRS query + handler.
- `Domain/Messages/DestinationControllerMsg.GetDetail` — error code struct.

**Provider mapping extended**
- `OpenTripMapDestinationProvider.MapDetail()`: now populates `Description` (`wikipedia_extracts.text`), `Photos` (from `preview.source`), `Website` (`url`). Opening hours not available from OpenTripMap.
- `FoursquareDestinationProvider.MapPlace()`: now populates `Description`, `Photos` (all photos, not just thumbnail), `Website`, `OpeningHours` (from `hours.display`, `hours.regular`, `hours.open_now`).

**Caching**: existing `CachedDestinationProvider` already caches `GetAttractionDetailAsync` at 24-hour TTL — satisfies NFR-3 (≤ 2 s) for repeated requests.

**Tests**: `TripPlanner.Tests` xUnit project added to the solution. 10 tests cover the query handler and controller action (full data, partial data, null/provider-not-found, exception, missing path param).

### Frontend (React + TypeScript, Vite) — implemented (Waves 0-4, 2026-07-05)

The "Frontend" section above stated the original design intent, written before any frontend code
existed. Two of its claims did not hold once the MVP was actually built (F4 auth, F1 search/results,
F2 detail view, F3 trip planner) — corrected here; **this section wins where it conflicts with the one
above**.

- **No Zustand (or any client-state library) was added.** All server data flows through TanStack Query;
  the small amount of local UI state (form inputs, disclosure toggles, the add-to-trip trip/day
  selections) is plain React `useState` / `useForm` state co-located with the component that needs it.
  A dedicated client-state store was never needed at this scope.
- **No optimistic updates.** Mutations (create trip, set dates, add/remove destination) call
  `queryClient.invalidateQueries()` on success and let the UI reflect the server's authoritative
  response; none use `onMutate`-based optimistic cache writes.

What was actually built:
- **Structure**: `frontend/src/{api,auth,features,hooks,test}`. `api/` holds one module per backend
  resource (`auth.ts`, `destinations.ts`, `trips.ts`) that unwraps the `{ success, errorCode, error,
  validates, result }` envelope; `features/<domain>/` holds pages plus domain-local hooks/components
  (`auth`, `destinations`, `trips`); `auth/` holds `AuthContext`, `ProtectedRoute`, JWT-claims decoding
  (no `/me` endpoint exists), and the `returnTo` redirect-back helper.
- **Server state**: TanStack Query for every network read (`useQuery`) and write (`useMutation`); one
  `QueryClient` is created in `main.tsx`. `AuthContext.logout()` and the silent-refresh-failure path both
  call `queryClient.clear()` so a different user signing into the same tab never sees a previous user's
  cached trips/destinations (NFR-6 enforced at the client cache layer, in addition to the server already
  scoping every query by `UserId`).
- **Auth**: JWT access + refresh tokens stored in `localStorage` (accepted risk — see below). A response
  interceptor in `api/client.ts` retries a request once after a silent `PUT /auth/refresh` on a 401, and
  clears the session if the refresh itself fails.
- **Forms**: react-hook-form + zod on every form (login, register, create-trip, set-dates, add-to-trip
  day picker) — this part of the original design intent held as planned.
- **Routing / guards**: react-router-dom; `/trips` and `/trips/:tripId` are wrapped in a `ProtectedRoute`
  that redirects an anonymous visitor to `/login?returnTo=<path>` and back after sign-in. Public pages
  (search, destination detail) instead disable the "Add to Trip" action and show an inline login link
  using the same `returnTo` mechanism, since browsing must stay anonymous-friendly while saving requires
  auth (F3-US8).
- **Accessibility**: form inputs pair `aria-invalid` with `aria-describedby` pointing at the field's own
  error message; decorative thumbnails use `alt=""` so card links don't get a cluttered accessible name;
  disabled controls use native `disabled`, not just styling.
- **Testing**: Vitest + React Testing Library + MSW (`frontend/src/test/`), one handler module per
  backend resource composed into a shared handler list. 115 tests (as of Phase 3) spanning auth,
  search/results (incl. autocomplete + filter/sort), the detail view (incl. the map render-guard), and
  the trip planner (incl. the pure move/drag-projection functions and the optimistic move-mutation hook)
  — covering loading/empty/error/success states plus the token-refresh interceptor.
- **Previously deferred, now implemented** (Phase 3 / post-MVP, see below): map view (Leaflet),
  drag-drop scheduling (dnd-kit), autocomplete, filters/sort, autosave.

### Frontend (Phase 3 post-MVP) — implemented

- **F1-US1 autocomplete**: `SearchPage`'s location input is a full ARIA combobox (`role="combobox"`,
  `aria-expanded`, `aria-controls`, `aria-activedescendant`, `role="listbox"`/`option"`). Debounces the
  typed query 300ms (`useDebouncedValue`) once ≥ 2 characters are typed, via the existing
  `useLocationSearch` TanStack Query hook; shows at most 5 suggestions client-side. Full keyboard
  support (ArrowUp/Down, Enter, Escape) plus mouse selection.
- **F1-US4/US5 filter + sort**: client-side only, over the already-loaded attraction list in
  `SearchPage`'s `AttractionsGrid` — a category multi-select (checkboxes, derived from the union of each
  attraction's `category` + `tags`) AND a minimum-rating select, combined with AND; a "Highest rating"
  sort (missing ratings last); "Clear filters" resets category/rating (not sort). No pagination exists
  yet in this app, so the "stays applied across paginating" AC is trivially satisfied.
- **F2-US3 map**: `features/destinations/MapView.tsx` — plain Leaflet (not react-leaflet, for React 19
  peer-dep safety), a single `L.divIcon` marker (sidesteps the classic bundler-breaks-marker-icon-URLs
  issue), rendered only when `Number.isFinite(latitude/longitude)`. Leaflet's CSS is imported once in
  `main.tsx` (not inside the component) so component tests never need to process it under jsdom; the
  `leaflet` module itself is mocked in `DestinationDetailPage.test.tsx` since jsdom can't run real
  Leaflet layout.
- **F3-US4/US5/US6 drag-and-drop planner board**: `TripPlannerPage` now renders a "Saved Places" column
  (`trip.savedPlaces`) plus one sortable column per itinerary day, using `@dnd-kit/core` +
  `@dnd-kit/sortable` (`PointerSensor` + `KeyboardSensor`, so reordering stays keyboard-accessible).
  **This supersedes the "No optimistic updates" claim above**: moving/reordering a destination now goes
  through `useMoveTripDestination` (`PUT /trips/{id}/destinations/{tripDestinationId}`), which writes an
  optimistic projection into the TanStack Query cache in `onMutate` (pure logic factored into
  `features/trips/moveDestination.ts`, unit-tested independent of React/dnd-kit) and rolls back on
  error. The dnd-kit event → move-mutation-variables mapping is likewise factored into a pure,
  independently-tested function (`features/trips/dragDrop.ts`'s `resolveDropTarget`), since simulating
  real pointer drags under jsdom is impractical.
- **F3-US9 autosave indicator**: a `useIsMutating({ mutationKey: tripMutationScopeKey(tripId) })` badge
  in the planner header shows "Saving…"/"All changes saved"; the remove and move mutations share that
  `mutationKey` scope. A failed move shows an error toast with a "Retry" action that re-fires the same
  mutation with the same variables (`ToastProvider.showToast` now accepts an optional `{ label,
  onAction }`); this mutation opts out of the global auto-toast via `meta: { suppressGlobalToast: true }`
  (checked in `queryClient.ts`'s `MutationCache.onError`) so the user doesn't see two toasts for the
  same failure.
- **`AddToTripControl`** gained a "Saved Places (schedule later)" option in its day picker (submits
  `itineraryDayId: null`), so a destination can be saved before a trip has any dates at all — no longer
  blocked.

**Known limitation carried over from the API contract, now resolved**: the previous note about
destinations unscheduled by a date-range reduction being unretrievable no longer applies — the trip
detail DTO's `savedPlaces` field (this phase's contract addition) is exactly that list, and the planner
board renders and lets the user re-schedule them.

**Accepted risk, documented not blocked**: tokens live in `localStorage`, not an httpOnly cookie. The
API only issues bearer tokens (no cookie-session option exists in the current contract), and the
codebase has no `dangerouslySetInnerHTML` or other known XSS vector today, so this is a reasonable
trade-off given the current contract — revisit if the backend ever offers a cookie-based session.

### CORS (current)
All origins allowed (`SetIsOriginAllowed(_ => true)`). Restrict to an explicit allow-list before production.

### How to build / run locally
```bash
# Infrastructure runs via Docker Engine — Docker Desktop is NOT used (company policy).
# Use the Docker Engine daemon (e.g. on WSL2 or a Linux host); the compose commands are identical.
# Start only the database (enough for EF migrations and most backend work):
docker compose up -d postgres
# Or start everything (PostgreSQL, Redis, MinIO):
docker compose up -d

# Run the API (auto-migrates on startup)
cd WebApi && dotnet run

# Health check
curl http://localhost:5000/healthz

# Swagger UI (dev or EnableSwagger=true)
http://localhost:5000/swagger
```

### How to add a migration
```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```
