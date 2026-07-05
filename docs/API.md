# API Reference — Travel Trip Planner

Owner agent: backend-lead. Living doc — update when endpoints change.

All responses use the custom envelope (not RFC 7807 ProblemDetails):
```json
{ "success": true|false, "errorCode": "string|null", "error": "string|null", "validates": [] }
```
Data is always under the `result` field when `success: true`.

Base path: `/api/v1/`

---

## Authentication

### POST /api/v1/auth/register
Register a new user account.

**Body** `application/json`
```json
{ "email": "user@example.com", "password": "Secret123!", "firstName": "Jane" }
```

**Responses**
| Status | When |
|--------|------|
| 201 Created | Account created; verification email sent. |
| 400 Bad Request | `errorCode`: `Auth.Register.EmailRequired`, `Auth.Register.InvalidEmail`, `Auth.Register.PasswordRequired`, `Auth.Register.PasswordTooWeak`, `Auth.Register.EmailTaken` |

---

### GET /api/v1/auth/verify-email?token=\<token\>
Activate an account using the token from the verification email.

**Responses**
| Status | When |
|--------|------|
| 200 OK | Email verified. |
| 400 Bad Request | `errorCode`: `Auth.VerifyEmail.TokenRequired`, `Auth.VerifyEmail.TokenInvalid`, `Auth.VerifyEmail.TokenExpired`, `Auth.VerifyEmail.AlreadyVerified` |

---

### POST /api/v1/auth/login
Log in and obtain JWT access + refresh tokens.

**Body**
```json
{
  "username": "user@example.com",
  "password": "Secret123!",
  "rememberMe": false
}
```

**Success response** (`200 OK`) `result`:
```json
{ "token": "<JWT>", "refreshToken": "<refresh>" }
```

**Error codes**: `Auth.Login.InvalidCredential`, `Auth.Login.LockedOut`, `Auth.Login.InActive`

---

### PUT /api/v1/auth/refresh
Exchange a refresh token for a new access token.

**Body** `{ "token": "...", "refreshToken": "..." }`

**Error codes**: `Auth.RefreshToken.RequiredToken`, `Auth.RefreshToken.Failed`

---

### PUT /api/v1/auth/logout
End the user's current session (single session per user).

**Body** `{ "token": "...", "refreshToken": "..." }`

---

### PUT /api/v1/auth/change-password
Change password (authenticated).

**Body** `{ "oldPassword": "...", "newPassword": "..." }`

---

### POST /api/v1/auth/forgot-password
Request a password-reset email.

**Body** `{ "email": "user@example.com" }`

---

### POST /api/v1/auth/reset-password
Complete password reset using the token from the email.

**Body** `{ "token": "...", "newPassword": "...", "confirmPassword": "..." }`

---

## Destinations

All destination endpoints are **public** (`[AllowAnonymous]`) — no JWT required (F3-US8).

### GET /api/v1/destinations/locations/search

Search for cities or countries by name. Returns up to 5 ranked results; exact matches first; case-insensitive; partial matches allowed ("Lon" → "London").

**Query parameters**
| Param | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `query` | string | Yes | — | At least 1 character. |
| `maxResults` | int | No | 5 | Clamped to `[1, 5]`. |

**Success response** (`200 OK`)
```json
{
  "success": true,
  "result": {
    "items": [
      {
        "name": "London",
        "displayName": "London, United Kingdom",
        "latitude": 51.5074,
        "longitude": -0.1278,
        "locationType": "city",
        "country": "United Kingdom"
      }
    ],
    "totalCount": 1
  }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Destination.SearchLocations.QueryRequired` | `query` is null or whitespace. |
| 500 | `Destination.SearchLocations.Exception` | Unexpected server error. |

**NFR-1**: Results returned within ≤ 500 ms for 95% of requests via Redis caching (1-hour TTL on geocoding results).

---

### GET /api/v1/destinations/attractions

Returns a ranked, paginated list of attractions near the given coordinates. Returns an empty `items` array (not an error) when no attractions are found.

**Query parameters**
| Param | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `latitude` | double | Yes | — | Must be in `[-90, 90]`. |
| `longitude` | double | Yes | — | Must be in `[-180, 180]`. |
| `radiusMeters` | int | No | 20000 | Search radius. City default 20 km. |
| `page` | int | No | 1 | 1-based page index. |
| `pageSize` | int | No | 20 | Clamped to `[1, 20]`. |

**Success response** (`200 OK`)
```json
{
  "success": true,
  "result": {
    "items": [
      {
        "providerPlaceId": "W214242",
        "name": "Eiffel Tower",
        "category": "cultural",
        "tags": ["cultural", "landmark"],
        "rating": 9.5,
        "thumbnailUrl": "https://...",
        "latitude": 48.8584,
        "longitude": 2.2945,
        "address": "Champ de Mars, Paris, France"
      }
    ],
    "totalCount": 1
  }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Destination.GetAttractions.LatitudeRequired` | `latitude` is missing. |
| 400 | `Destination.GetAttractions.LongitudeRequired` | `longitude` is missing. |
| 400 | `Destination.GetAttractions.InvalidCoordinates` | Coordinates out of valid range. |
| 500 | `Destination.GetAttractions.Exception` | Unexpected server error. |

**NFR-2**: Attractions list returned within ≤ 1000 ms via Redis caching (30-minute TTL on attraction list results; 24-hour TTL on detail records).

**Provider note**: Attractions are fetched from OpenTripMap (POIs by radius, minimum `rate=3` for quality). Foursquare is available as an alternative/enrichment provider and is registered in DI under `FoursquareDestinationProvider`.

---

### GET /api/v1/destinations/{providerPlaceId}

Returns full detail for a single destination. All optional fields (`description`, `photos`, `address`, `website`, `openingHours`) are `null` or empty when the provider does not supply them — the response is always returned (graceful partial data, F2-US1 business rule). Implements F2-US1, F2-US2, F2-US4.

**Path parameter**
| Param | Type | Required | Notes |
|-------|------|----------|-------|
| `providerPlaceId` | string | Yes | Provider-specific place ID: OpenTripMap `xid` (e.g. `W214242`) or Foursquare `fsq_id`. |

**Success response** (`200 OK`)
```json
{
  "success": true,
  "result": {
    "providerPlaceId": "W214242",
    "name": "Eiffel Tower",
    "category": "cultural",
    "tags": ["cultural", "landmark"],
    "description": "Famous iron lattice tower on the Champ de Mars in Paris.",
    "photos": [
      "https://cdn.example.com/photo1.jpg",
      "https://cdn.example.com/photo2.jpg"
    ],
    "address": "Champ de Mars, Paris, France",
    "website": "https://toureiffel.paris",
    "openingHours": {
      "displayText": "Daily 09:00–23:00",
      "weekdayText": [
        "Monday: 09:00 – 23:00",
        "Tuesday: 09:00 – 23:00"
      ],
      "isOpenNow": true
    },
    "rating": 9.5,
    "latitude": 48.8584,
    "longitude": 2.2945
  }
}
```

**Partial data example** — when optional fields are absent the view still opens:
```json
{
  "success": true,
  "result": {
    "providerPlaceId": "W000001",
    "name": "Mystery Ruin",
    "category": null,
    "tags": [],
    "description": null,
    "photos": [],
    "address": null,
    "website": null,
    "openingHours": null,
    "rating": null,
    "latitude": 10.0,
    "longitude": 20.0
  }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Destination.GetDetail.ProviderPlaceIdRequired` | `providerPlaceId` is null or whitespace. |
| 404 | `Destination.GetDetail.NotFound` | Provider does not recognise the given ID. |
| 500 | `Destination.GetDetail.Exception` | Unexpected server error. |

**NFR-3**: Response within ≤ 2 s. Repeated requests are served from the Redis 24-hour TTL cache (keyed on `providerPlaceId`).

**Provider mapping**
| Field | OpenTripMap source | Foursquare source |
|-------|--------------------|-------------------|
| `name` | `name` | `name` |
| `category` | first kind from `kinds` | first `categories[].name` |
| `tags` | all kinds from `kinds` | all `categories[].name` |
| `description` | `wikipedia_extracts.text` | `description` |
| `photos` | `preview.source` (single) | `photos[].prefix+300x300+suffix` (all) |
| `address` | `address.road + city + country` | `location.address + locality + country` |
| `website` | `url` | `website` |
| `openingHours` | not available (null) | `hours.display`, `hours.regular`, `hours.open_now` |
| `rating` | `rate` | `rating` |

---

## Trips

All trip endpoints require a valid JWT (`Authorization: Bearer <token>`). Unauthenticated requests receive `401 Unauthorized`. Requests that target a trip not owned by the caller receive `404 Not Found` (same code as not-found — prevents trip-ID enumeration, NFR-6).

### GET /api/v1/trips

Returns the authenticated user's trip list. Empty array when the user has no trips (F3-US10).

**Success response** (`200 OK`)
```json
{
  "success": true,
  "result": [
    {
      "id": "3fa85f64-...",
      "name": "Paris 2026",
      "startDate": "2026-07-01",
      "endDate": "2026-07-05",
      "createdAt": "2026-07-01T10:00:00Z",
      "updatedAt": "2026-07-01T10:00:00Z",
      "itineraryDays": []
    }
  ]
}
```

Note: `itineraryDays` is always an empty array in list responses. Use `GET /trips/{id}` for full detail.

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 401 | — | No valid JWT. |
| 500 | `Trip.GetTrips.Exception` | Unexpected server error. |

---

### GET /api/v1/trips/{id}

Returns full detail for a single trip, including itinerary days (ordered by `dayIndex`) and their destinations (ordered by `position`). Implements F3-US10.

> **Known gap**: destinations are only ever returned nested under `itineraryDays[].tripDestinations`.
> There is currently no way to retrieve a trip's "Saved Places" — destinations whose `itineraryDayId`
> was set to `null` by a date-range reduction (see `PUT /trips/{id}/dates` below). See that section for
> detail; tracked as a backend/contract follow-up, not a frontend defect.

**Path parameter**: `id` (Guid) — the trip ID.

**Success response** (`200 OK`)
```json
{
  "success": true,
  "result": {
    "id": "3fa85f64-...",
    "name": "Paris 2026",
    "startDate": "2026-07-01",
    "endDate": "2026-07-05",
    "createdAt": "2026-07-01T10:00:00Z",
    "updatedAt": "2026-07-01T10:00:00Z",
    "itineraryDays": [
      {
        "id": "a1b2c3d4-...",
        "date": "2026-07-01",
        "dayIndex": 1,
        "tripDestinations": [
          {
            "id": "e5f6...",
            "tripId": "3fa85f64-...",
            "itineraryDayId": "a1b2c3d4-...",
            "providerPlaceId": "W214242",
            "name": "Eiffel Tower",
            "category": "cultural",
            "thumbnailUrl": "https://...",
            "lat": 48.8584,
            "lng": 2.2945,
            "position": 1
          }
        ]
      }
    ]
  }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 401 | — | No valid JWT. |
| 404 | `Trip.NotFound` | Trip does not exist or does not belong to the caller. |
| 500 | `Trip.GetDetail.Exception` | Unexpected server error. |

---

### POST /api/v1/trips

Creates a new trip for the authenticated user (F3-US1). The trip is created without dates; use `PUT /trips/{id}/dates` to set the date range.

**Body** `application/json`
```json
{ "name": "Paris 2026" }
```

**Success response** (`201 Created`)
```json
{
  "success": true,
  "result": {
    "id": "3fa85f64-...",
    "name": "Paris 2026",
    "startDate": null,
    "endDate": null,
    "createdAt": "2026-07-01T10:00:00Z",
    "updatedAt": "2026-07-01T10:00:00Z",
    "itineraryDays": []
  }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Trip.CreateTrip.NameRequired` | `name` is null or whitespace. |
| 401 | — | No valid JWT. |
| 500 | `Trip.CreateTrip.Exception` | Unexpected server error. |

---

### PUT /api/v1/trips/{id}/dates

Sets or updates the trip's date range (F3-US2). Generates exactly one `ItineraryDay` per calendar date in `[startDate, endDate]`. When the range is shortened, itinerary days outside the new range are removed; any destinations scheduled to those days are moved to "Saved Places" (`itineraryDayId` becomes `null`) via the database cascade rule (not deleted). A warning code is included in the response when destinations were unscheduled.

**Path parameter**: `id` (Guid) — the trip ID.

**Body** `application/json`
```json
{ "startDate": "2026-07-01", "endDate": "2026-07-05" }
```

**Success response** (`200 OK`) — standard trip detail with regenerated `itineraryDays`:
```json
{
  "success": true,
  "errorCode": null,
  "result": { ... }
}
```

When destinations were unscheduled by the date reduction, `success` is still `true` and `errorCode` is set to the warning code:
```json
{
  "success": true,
  "errorCode": "Trip.SetDates.DestinationsUnscheduled",
  "result": { ... }
}
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Trip.SetDates.StartDateRequired` | `startDate` is missing. |
| 400 | `Trip.SetDates.EndDateRequired` | `endDate` is missing. |
| 400 | `Trip.SetDates.InvalidDateRange` | `startDate` is after `endDate`. |
| 401 | — | No valid JWT. |
| 404 | `Trip.NotFound` | Trip does not exist or does not belong to the caller. |
| 500 | `Trip.SetDates.Exception` | Unexpected server error. |

> **Known limitation — "Saved Places" are unreachable via the API.** When a date-range reduction
> unschedules a destination (`itineraryDayId` → `null`), the row survives in the database (per the
> cascade rule described above) but no documented endpoint returns it: `GET /trips` never nests
> destinations, and `GET /trips/{id}` only nests destinations under `itineraryDays[].tripDestinations`,
> which by definition excludes anything with a `null` `itineraryDayId`. The frontend can and does surface
> the `Trip.SetDates.DestinationsUnscheduled` warning at the moment it happens, but there is no way for a
> returning user to see or re-schedule those destinations later. Fixing this needs either a new endpoint
> (e.g. `GET /trips/{id}/saved-places`) or including unscheduled destinations in the `GET /trips/{id}`
> response outside of `itineraryDays`. Flagged during Phase 2 frontend work (2026-07-05); not addressed
> in this PR — backend follow-up needed.

---

### POST /api/v1/trips/{id}/destinations

Adds a destination to a specific itinerary day within the trip (F3-US3). Because drag-drop scheduling (F3-US4) is out of MVP, the caller must supply `itineraryDayId` explicitly. The itinerary day must belong to this trip (prevents cross-trip injection).

**Path parameter**: `id` (Guid) — the trip ID.

**Body** `application/json`
```json
{
  "itineraryDayId": "a1b2c3d4-...",
  "providerPlaceId": "W214242",
  "name": "Eiffel Tower",
  "category": "cultural",
  "thumbnailUrl": "https://cdn.example.com/thumb.jpg",
  "lat": 48.8584,
  "lng": 2.2945
}
```

`category` and `thumbnailUrl` are optional (nullable).

**Success response** (`201 Created`)
```json
{
  "success": true,
  "result": {
    "id": "e5f6...",
    "tripId": "3fa85f64-...",
    "itineraryDayId": "a1b2c3d4-...",
    "providerPlaceId": "W214242",
    "name": "Eiffel Tower",
    "category": "cultural",
    "thumbnailUrl": "https://...",
    "lat": 48.8584,
    "lng": 2.2945,
    "position": 1
  }
}
```

`position` is auto-assigned as the next slot within the day (max existing + 1; starts at 1).

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 400 | `Trip.AddDestination.ItineraryDayIdRequired` | `itineraryDayId` is missing. |
| 400 | `Trip.AddDestination.ProviderPlaceIdRequired` | `providerPlaceId` is null or whitespace. |
| 400 | `Trip.AddDestination.NameRequired` | `name` is null or whitespace. |
| 401 | — | No valid JWT. |
| 404 | `Trip.NotFound` | Trip not found/not owned, or `itineraryDayId` does not belong to this trip. |
| 500 | `Trip.AddDestination.Exception` | Unexpected server error. |

---

### DELETE /api/v1/trips/{id}/destinations/{tripDestinationId}

Removes a destination from the trip (F3-US7). Soft-deletes the `TripDestination` row. Both the trip and the destination must belong to the authenticated user (NFR-6).

**Path parameters**
| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Trip ID. |
| `tripDestinationId` | Guid | ID of the `TripDestination` to remove. |

**Success response** (`200 OK`)
```json
{ "success": true, "result": true }
```

**Error responses**
| Status | errorCode | When |
|--------|-----------|------|
| 401 | — | No valid JWT. |
| 404 | `Trip.NotFound` | Trip not found/not owned, or destination does not belong to this trip. |
| 500 | `Trip.RemoveDestination.Exception` | Unexpected server error. |

---

## Error codes reference

Error code string constants are defined in `Domain/Messages/*ControllerMsg.cs`. All codes follow the pattern `<Feature>.<Action>.<Reason>`.

| Prefix | File |
|--------|------|
| `Auth.*` | `Domain/Messages/AuthControllerMsg.cs` |
| `Destination.*` | `Domain/Messages/DestinationControllerMsg.cs` |
| `Trip.*` | `Domain/Messages/TripControllerMsg.cs` |

**Warning codes** (returned with `success: true` to indicate a non-fatal condition):
| Code | Endpoint | Meaning |
|------|----------|---------|
| `Trip.SetDates.DestinationsUnscheduled` | `PUT /trips/{id}/dates` | One or more scheduled destinations were moved to Saved Places because their itinerary day was removed by the date reduction. |

---

## Versioning

All routes are versioned (`/api/v{version}/`). Current version: `1.0`. The `Asp.Versioning.Mvc` package handles version negotiation; version is required in the URL segment.
