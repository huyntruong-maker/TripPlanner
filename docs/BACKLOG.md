# Backlog & Implementation Plan — Travel Trip Planner

Pick work from here in wave order. `MVP` = "Selected US? = Yes" in the source sheet
("Feature List (for DotNet-fullstack team)"). Branch naming: `feature/<feature>-<us>-<slug>`
(e.g. `feature/auth-us1-signup`). Titles, acceptance criteria, and business rules below are
transcribed from the sheet — keep them in sync if the sheet changes.

## Story map (priority + scope + owner)

### Feature 1 — Destination Suggestion
| US | Title | Priority | MVP | Wave | Primary owner | Status |
|----|-------|----------|-----|------|---------------|--------|
| US1 | Use Autocomplete for search field | Medium | — | 5 | frontend-lead + backend-lead | Done 2026-07-18 |
| US2 | Search by city/country | High | Yes | 2 | backend-lead + frontend-lead | Done (geocoding now multi-result via Nominatim, switchable) |
| US3 | View recommended attractions list | High | Yes | 2 | backend-lead + frontend-lead | Done |
| US4 | Filter recommended attractions | Medium | — | 5 | frontend-lead | Done 2026-07-18 |
| US5 | Sort attractions | Low | — | 5 | frontend-lead | Done 2026-07-18 |

### Feature 2 — Destination Details
| US | Title | Priority | MVP | Wave | Primary owner | Status |
|----|-------|----------|-----|------|---------------|--------|
| US1 | Open a destination details view | Medium | Yes | 3 | backend-lead + frontend-lead | Backend done 2026-06-28 |
| US2 | View photos in destination details | Medium | Yes | 3 | frontend-lead | Backend done 2026-06-28 |
| US3 | View map and location info | Low | — | 5 | frontend-lead | Done 2026-07-18 (Leaflet map) |
| US4 | View opening hours when available | Low | Yes | 3 | backend-lead | Backend done 2026-06-28 |

### Feature 3 — Trip Planner
| US | Title | Priority | MVP | Wave | Primary owner | Status |
|----|-------|----------|-----|------|---------------|--------|
| US1 | Create a trip | High | Yes | 4 | backend-lead + database-expert | Done |
| US2 | Set trip start and end dates | High | Yes | 4 | backend-lead + database-expert | Done |
| US3 | Add a destination to a trip | High | Yes | 4 | backend-lead + frontend-lead | Done (day now optional → Saved Places) |
| US4 | Schedule destinations into a day in trip | Medium | — | 5 | frontend-lead | Done 2026-07-18 (drag-drop) |
| US5 | Reorder destinations within a day | High | — | 5 | frontend-lead + backend-lead | Done 2026-07-18 |
| US6 | Drag a destination from one day to another | High | — | 5 | frontend-lead + backend-lead | Done 2026-07-18 |
| US7 | Remove a destination from the itinerary | High | Yes | 4 | backend-lead + frontend-lead | Done |
| US8 | Require login to save trips and destinations | Medium | Yes | 4 | backend-lead + security-auditor | Done |
| US9 | Save trips and destinations automatically to my account | Medium | — | 5 | backend-lead + frontend-lead | Done 2026-07-18 (saving indicator + retry) |
| US10 | Load my saved trips and destinations when I return | Medium | Yes | 4 | backend-lead + frontend-lead | Done |

### Feature 4 — User Authentication
| US | Title | Priority | MVP | Wave | Primary owner |
|----|-------|----------|-----|------|---------------|
| US1 | Sign up with email and password | Medium | Yes | 1 | backend-lead + security-auditor |
| US2 | Verify email to activate account | Medium | Yes | 1 | backend-lead |
| US3 | Log in with email and password | Medium | Yes | 1 | backend-lead + security-auditor |
| US4 | Log out | Medium | Yes | 1 | backend-lead + frontend-lead |

### Non-functional requirements
| NFR | Area | Requirement | MVP |
|-----|------|-------------|-----|
| NFR1 | Search Performance | County/City search results returned within ≤ 500 ms for 95% of requests. | Yes |
| NFR2 | Search Performance | Attractions suggestion displayed within ≤ 1000 ms for 95% of requests. | — |
| NFR3 | Loading Time | Destination details popup displayed within ≤ 2 s after the user opens it. | — |
| NFR4 | Responsiveness | Drag-and-drop (assigning destinations to days) responds within ≤ 100 ms, no visible UI lag. | — |
| NFR5 | Data Scalability | System must handle millions of destination records from external APIs. | — |
| NFR6 | Authorization | Users can only view and modify their own trips and saved destinations. | Yes |

---

## Story details (acceptance criteria & business rules from the sheet)

### Feature 1 — Destination Suggestion

**F1-US1 — Use Autocomplete for search field** · Medium · not MVP
*As a user, I want location autocomplete suggestions while typing so I can quickly select the correct city or country.*
- AC: (1) Type at least 2 characters to trigger location suggestions. (2) See up to 5 matching city/country suggestions while typing. (3) Select a suggestion to confirm the intended location. (4) See the selected location displayed in the search box after selection.
- Note: Use OpenTripMap Geocoding API → lat/lng.

**F1-US2 — Search by city/country** · High · MVP
*As a user, I want to search for a city or country by name so I can discover destinations to visit.*
- Precondition: user completes location selection in US1.
- AC: (1) Enter at least 1 character into the search input to start searching. (2) See a list of matching city and country results based on the entered text. (3) See both city and country names clearly labeled in the results. (4) Select a city or country from the search results. (5) View the selected city or country as the active search value. (6) Receive a message "No attractions found" when no matching locations are found. (7) Clear the search input to start a new search.
- Business rules: results must include cities and countries; ranked by relevance (exact matches first); max 5 results displayed; no duplicate locations; case-insensitive; partial matches allowed (e.g. "Lon" → "London").

**F1-US3 — View recommended attractions list** · High · MVP
*As a user, I want to see a recommended list of attractions so I can decide what to explore.*
- AC: (1) View a list of attractions after submitting a valid location search. (2) See each attraction name. (3) See each attraction category/tags when available. (4) See each attraction rating/popularity indicator when available. (5) See a thumbnail image when available. (6) See a placeholder when image or rating is unavailable. (7) See pagination and load more attractions when reaching 20 items. *(optional)*
- Business rules: fetch attractions using coordinates + radius/bounds; default radius — City: 20 km, Country: broader approach (MVP option: show "top attractions" by major cities or require city selection); return max 20 items per page; use provider ranking and/or internal scoring (e.g. popularity/rate first).
- Note: Call OpenTripMap for POIs near the city center; call Foursquare to enrich with categories and reviews. What to show on the UI is reviewed against the actual response.

**F1-US4 — Filter recommended attractions** · Medium · not MVP
*As a user, I want to filter attractions so I can find places matching my interests.*
- AC: (1) Apply category filters to narrow the list. (2) Apply rating/popularity filters to narrow the list. (3) Combine multiple filters at the same time. (4) Clear filters to return to the full list.
- Business rules (MVP, will review): filters by Category (from provider tags/kinds) and Rating/popularity (if available); allow multiple filters at once; keep filters applied when navigating across pages.

**F1-US5 — Sort attractions** · Low · not MVP
*As a user, I want to sort attractions so I can prioritize what matters to me.*
- AC: (1) Sort by recommended order by default. (2) Sort by highest rating/popularity when selected. (3) Keep applied filters when changing sort order. (4) See results update immediately after changing sort.
- Business rules (MVP, will review): Recommended (default) and Highest rating/popularity (if available).

### Feature 2 — Destination Details

**F2-US1 — Open a destination details view** · Medium · MVP
*As a user, I want to open destination details so I can see more information before adding it to my trip.*
- AC: (1) Select a destination from the list to open its detail view. (2) View the name, category, and short description. (3) View images when available. (4) View the location on a map. *(optional)* (5) View additional information such as address, opening hours, and website when available. (6) Identify an option to add the destination to a trip. (7) Close the detail view and return to the previous list.
- Business rules: details must match the selected destination exactly; if some fields are missing the detail view must still open; images and maps are optional and shown only when available; the "Add to Trip" action must be unavailable/unclickable if the user is not logged in.
- Note: fetch details by provider place ID (e.g. OpenTripMap `xid` or Foursquare).

**F2-US2 — View photos in destination details** · Medium · MVP
*As a user, I want to see photos of a destination so I can judge if it's worth visiting.*
- AC: (1) View destination photos when available. (2) Swipe through multiple photos when available. (3) See a placeholder when no photos are available.
- Business rules: show at least 1 image if available; otherwise show placeholder.

**F2-US3 — View map and location info** · Low · not MVP
*As a user, I want to see the destination on a map so I understand where it is.*
- AC: (1) View the destination on a map with a marker. (2) Zoom and pan the map to explore nearby areas. (3) View an address/area label when available.

**F2-US4 — View opening hours when available** · Low · MVP
*As a user, I want to see opening hours (when available) so I can plan the right day/time.*
- AC: (1) View opening hours when available. (2) See "Opening hours not available" when the data is missing.

### Feature 3 — Trip Planner

**F3-US1 — Create a trip** · High · MVP
*As a user, I want to create a trip so I can start planning an itinerary.*
- AC: (1) Create a new trip by entering a trip name. (2) View the new trip in my trip list. (3) Open the trip planner after creating the trip.
- Business rules: trip name is required.

**F3-US2 — Set trip start and end dates** · High · MVP
*As a user, I want to set trip dates so the planner creates the correct number of days.*
- AC: (1) Set a trip start date. (2) Set a trip end date. (3) View one itinerary day created for each date in the selected range. (4) See itinerary days update when changing trip dates. (5) Confirm date changes when reducing days would remove planned items.
- Business rules: require start date ≤ end date.

**F3-US3 — Add a destination to a trip** · High · MVP
*As a user, I want to add one or more destinations to a selected trip so I can collect places for that trip before scheduling them by day.*
- AC: (1) Add a destination to a trip from the attraction list. (2) Add a destination to a trip from the destination details page. (3) Select a day in the trip. (4) See the destination appear immediately under the selected trip.
- Note (from sheet): include "select a day in the trip" like the node in US4. Because drag-drop scheduling (US4) is **not** in MVP, the MVP add-to-trip flow requires the user to pick a day.

**F3-US4 — Schedule destinations into a day in trip** · Medium · not MVP
*As a user, I want to drag and drop destinations into a specific day so I can quickly build a day-by-day itinerary.*
- Preconditions: a trip is created; trip start/end dates set (days exist); at least one destination in Saved Places.
- AC: (1) Drag a destination from Saved Places and drop it into a selected day. (2) See it removed from Saved Places after dropping. (3) See it appear immediately in the selected day. (4) See a message when dropping into a day where it already exists. (5) See it return to Saved Places when the drop action is invalid.
- Business rules: remove destination from Saved Places after a successful drop; prevent the same destination being scheduled twice in the same day.
- Note: if this feature is not implemented, US3 must let users select a day in the trip.

**F3-US5 — Reorder destinations within a day** · High · not MVP
*As a user, I want to reorder destinations within a day so I can control the visit sequence.*
- AC: (1) Drag a destination within a day to change its position. (2) See the new order saved automatically. (3) See the updated order preserved after refreshing the page.

**F3-US6 — Drag a destination from one day to another** · High · not MVP
*As a user, I want to move a destination from one day to another so I can reschedule easily.*
- AC: (1) Drag a destination from one day and drop it into another. (2) See it removed from the original day. (3) See it appear in the new day. (4) See a message when dropping into a day where it already exists. (5) See it remain in the original day when the drop is invalid.

**F3-US7 — Remove a destination from the itinerary** · High · MVP
*As a user, I want to remove a destination so I can keep my itinerary accurate.*
- AC: (1) Remove a destination from a trip day. (2) See it removed immediately after confirmation.

**F3-US8 — Require login to save trips and destinations** · Medium · MVP
*As a user, I want to be prompted to log in when saving trips so my destinations are stored in my account.*
- AC: (1) Browse destinations without logging in. (2) Prompt me to log in when I try to create a trip while logged out. (3) Prompt me to log in when I try to add a destination to a trip while logged out. (4) Return me to the same trip/destination after logging in. (5) Complete the original save action after successful login.

**F3-US9 — Save trips and destinations automatically to my account** · Medium · not MVP
*As a user, I want my trips and selected destinations saved automatically so I can come back later and continue planning.*
- AC: (1) Create a trip and see it remain available after closing and reopening the app. (2) Add a destination and see it remain after closing and reopening. (3) Remove a destination and see it remain removed after closing and reopening. (4) Edit trip details (name/dates) and see updates persist after closing and reopening. (5) See a saving indicator when changes are being saved. (6) See an error message when saving fails and keep unsaved changes locally until retry.

**F3-US10 — Load my saved trips and destinations when I return** · Medium · MVP
*As a user, I want the app to load my saved trips and destinations when I return so I can continue where I left off.*
- AC: (1) Log in and see my previously created trips. (2) Open a trip and see previously saved destinations and itinerary days. (3) See an empty state when I have no saved trips.

### Feature 4 — User Authentication

**F4-US1 — Sign up with email and password** · Medium · MVP
*As a user, I want to create an account so I can access the app with my own identity.*
- AC: (1) Open the sign-up screen. (2) Enter an email address and password. (3) Create an account when the email is not already registered. (4) See a message when the email is already registered. (5) See a message when the password does not meet requirements. (6) Continue to the app in a signed-in state after successful sign-up.
- Business rules: require unique email per account; enforce password policy (MVP: min 8 chars); store passwords using strong hashing (e.g. bcrypt/argon2); send generic error messages to avoid account enumeration.

**F4-US2 — Verify email to activate account** · Medium · MVP
*Activate the account through an email verification step before full access.*
- AC: (the sheet leaves this row's AC/user-story text blank) — implement an email verification token flow: issue a token on sign-up, activate the account when a valid token is consumed, handle expired/invalid tokens. *Confirm exact criteria with the product owner; the sheet row has no AC text.*

**F4-US3 — Log in with email and password** · Medium · MVP
*As a user, I want to log in so I can access my account.*
- AC: (1) Open the login screen. (2) Enter an email address and password. (3) Sign in when credentials are valid. (4) See a message when credentials are invalid. (5) Stay signed in after refreshing the page.

**F4-US4 — Log out** · Medium · MVP
*As a user, I want to log out so I can end my session.*
- AC: (1) Click the "Log out" option. (2) End the session immediately. (3) See the app return to a logged-out state. (4) Prevent access to pages after logging out.

---

## Implementation plan (backend-first)

Delivery order is **backend-first**: design/integrate the database and build the API for all MVP
features before any UI work. The frontend is built afterward against stable API contracts. The
`Wave` column in the story map above is the **feature build order** (1 = Auth … 5 = enhancements);
each MVP feature is delivered backend-first within that order. `/orchestrate` should sequence
contract → DB → backend → tests → (then) frontend.

### Phase 0 — Foundation (systems-architect, cicd-engineer, database-expert)
Scaffold the ASP.NET Core Web API solution, wire EF Core, design the DB schema for all MVP entities
with the initial migration, add a health check, and set up GitHub Actions CI (build + test + lint for
the .NET API). Acceptance: pipeline green on an empty API slice; `docs/DATABASE.md` and `docs/API.md`
reflect the schema/contract.

### Phase 1 — Backend & Database (build before any UI)
For each MVP feature in build order: design/integrate DB → implement endpoints → integration-test → update API/DB docs.

1. **Auth API — F4 US1–US4** (backend-lead, security-auditor, database-expert). Gates everything that saves data.
   DB: Users, EmailVerificationTokens, RefreshTokens. Endpoints: register (unique email, password ≥8,
   bcrypt/argon2, generic errors), email verification token flow, login (JWT access + refresh, stay signed
   in after refresh), refresh, logout (end session, block protected routes).
2. **Destination Suggestion API — F1 US2, US3** (backend-lead). Provider integration behind
   `IDestinationProvider` / `IGeocodingProvider` (OpenTripMap geocoding + POIs, Foursquare enrich).
   `GET /locations/search` (cities+countries, rank exact-first, max 5, dedupe, case-insensitive, partial);
   `GET /attractions` (coords+radius, city default 20 km, max 20/page, ranking, "No attractions found" empty).
   Caching to meet NFR-1 (≤500 ms) and NFR-2 (≤1000 ms). DB: optional `DestinationCache`.
3. **Destination Details API — F2 US1, US2, US4** (backend-lead). **DONE 2026-06-28.**
   `GET /destinations/{providerPlaceId}` (name, category, description, photos, address?, website?, opening hours;
   opens even with missing fields). NFR-3 ≤2 s. Branch: `feature/details-us1-us2-us4-destination-details`.
4. **Trips API + DB — F3 US1, US2, US3, US7, US8, US10** (backend-lead, database-expert, security-auditor).
   DB: Trips, ItineraryDays, TripDestinations (+ migration). Endpoints: trips CRUD (name required),
   set dates → generate one ItineraryDay per date (start ≤ end; warn when reducing days drops items),
   add destination (**select a day** — `itineraryDayId` required since drag-drop US4 is out of MVP),
   remove destination, load saved trips with empty state. Auth-gate every write; enforce NFR-6 (own-data-only) on every query.

Exit criteria: all MVP endpoints implemented, documented in `docs/API.md` / `docs/DATABASE.md`, and NFR-1 and
NFR-6 verified. (Unit tests are deferred for now; integration tests are optional. Any Testcontainers/integration
test runs against **Docker Engine**, not Docker Desktop — see the playbook "Local environment".)

### Phase 2 — Frontend (after API contracts are stable)
Built against the Phase 1 APIs, in the same feature order: auth (sign-up/login/logout pages + route guards) →
search box + results grid (thumbnails/placeholders) → destination detail view + photo carousel +
"Opening hours not available" fallback ("Add to Trip" disabled when logged out) → trip list + planner board
(create trip, set dates, add destination to a selected day, remove, load saved trips).

### Phase 3 — Backlog / enhancements (post-MVP, backend-then-frontend per item)
F1 autocomplete (US1), filters (US4), sort (US5); F2 map (US3);
F3 drag-drop scheduling (US4), reorder within a day (US5), drag between days (US6) — meet NFR-4 (≤100 ms); F3 autosave (US9).

## Definition of done (every story)
- Acceptance criteria from the sheet are met and demoed.
- Unit/integration tests cover the logic; relevant E2E for the user flow.
- Build + lint clean; `docs/API.md` / `docs/DATABASE.md` updated by the owning agent.
- For auth/data stories: security-auditor checks authorization (NFR-6) and secrets handling.
