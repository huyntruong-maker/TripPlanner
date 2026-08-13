# Product Requirements — Travel Trip Planner

> Source of truth for what we build. Agents read this; they do not edit it without explicit human instruction.
> Derived from "Feature List (for DotNet-fullstack team)".

## Objective
Let users discover destinations (cities/countries and their attractions) from external travel data,
view destination details, and plan a multi-day trip itinerary saved to their account.

## Users & primary use cases
- Traveler — search a place, browse recommended attractions, open details, build and save a day-by-day trip.

## Scope (MVP = "Selected US? = Yes" in the sheet)
Feature 1 — Destination Suggestion: search by city/country (FR-F1-US2), recommended attractions list (FR-F1-US3).
Feature 2 — Destination Details: open details (FR-F2-US1), photos (FR-F2-US2), opening hours (FR-F2-US4).
Feature 3 — Trip Planner: create trip (US1), set dates (US2), add destination (US3), remove destination (US7),
require login to save (US8), load saved trips (US10).
Feature 4 — User Authentication: sign up (US1), verify email (US2), log in (US3), log out (US4).

## Out of MVP (backlog — build after MVP)
F1: autocomplete (US1), filter (US4), sort (US5). F2: map/location info (US3).
F3: drag-drop schedule into day (US4), reorder within day (US5), move between days (US6), autosave (US9).

## External data providers
- OpenTripMap — geocoding (name → lat/lng) and POIs near coordinates.
- Foursquare — enrich attractions with categories and reviews.
- UI fields are confirmed against the actual provider responses before finalizing.

## Non-functional requirements
- NFR-1 (MVP): city/country search results returned within ≤ 500 ms for 95% of requests.
- NFR-2: attractions suggestion displayed within ≤ 1000 ms (p95).
- NFR-3: destination details popup within ≤ 2 s.
- NFR-4: drag-and-drop responds within ≤ 100 ms (no visible lag).
- NFR-5: handle millions of destination records from external APIs (caching/pagination strategy).
- NFR-6 (MVP): authorization — users can only view/modify their own trips and saved destinations.

## Key business rules (from sheet)
- Search: include cities + countries, rank by relevance (exact first), max 5 results, no duplicates,
  case-insensitive, partial match allowed.
- Attractions: fetch by coordinates + radius (city default 20 km), max 20 per page, provider/internal ranking.
- Trip: name required; start date ≤ end date; one itinerary day per date in range; reducing days warns if it removes items.
- Add-to-trip / create-trip require login; after login, resume the original action.
- Auth: unique email, password min 8 chars, strong hashing (bcrypt/argon2), generic errors (no account enumeration).

## Success metrics
- MVP feature set implemented with acceptance criteria met; NFR-1 and NFR-6 verified.
