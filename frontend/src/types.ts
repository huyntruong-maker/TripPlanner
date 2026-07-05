// Shared types mirroring the backend DTOs (see docs/API.md).

/** The response envelope every API endpoint returns (docs/API.md line 5-9). */
export interface ApiEnvelope<TResult> {
  success: boolean;
  errorCode: string | null;
  error: string | null;
  validates: unknown[];
  result: TResult;
}

/** Identity decoded from the JWT's own claims — the API has no `/me` endpoint. */
export interface AuthenticatedUser {
  id: string;
  email: string;
}

/** Result shape of POST /auth/login and PUT /auth/refresh. */
export interface AuthTokens {
  token: string;
  refreshToken: string;
}

/** A paginated list result (docs/API.md GET /destinations/locations/search, /attractions). */
export interface PagedResult<TItem> {
  items: TItem[];
  totalCount: number;
}

/** Result of GET /destinations/locations/search. */
export interface LocationSuggestion {
  name: string;
  displayName: string;
  latitude: number;
  longitude: number;
  locationType: string;
  country: string;
}

/** A single item from GET /destinations/attractions. */
export interface AttractionSummary {
  providerPlaceId: string;
  name: string;
  category: string | null;
  tags: string[];
  rating: number | null;
  thumbnailUrl: string | null;
  latitude: number;
  longitude: number;
  address: string | null;
}

/** docs/API.md GET /destinations/{providerPlaceId} — null when the provider has no hours data. */
export interface OpeningHours {
  displayText: string | null;
  weekdayText: string[];
  isOpenNow: boolean | null;
}

/**
 * Full destination detail. Every optional field is null/empty when the
 * provider doesn't supply it — the view must still render (F2-US1).
 */
export interface DestinationDetail {
  providerPlaceId: string;
  name: string;
  category: string | null;
  tags: string[];
  description: string | null;
  photos: string[];
  address: string | null;
  website: string | null;
  openingHours: OpeningHours | null;
  rating: number | null;
  latitude: number;
  longitude: number;
}

/** One destination scheduled within a trip (docs/API.md GET /trips/{id}). */
export interface TripDestination {
  id: string;
  tripId: string;
  itineraryDayId: string | null;
  providerPlaceId: string;
  name: string;
  category: string | null;
  thumbnailUrl: string | null;
  lat: number;
  lng: number;
  position: number;
}

/** One calendar day of a trip, ordered by `dayIndex`. */
export interface ItineraryDay {
  id: string;
  date: string;
  dayIndex: number;
  tripDestinations: TripDestination[];
}

/**
 * A trip. `itineraryDays` is always `[]` from GET /trips (list) — only
 * GET /trips/{id} (detail) populates it.
 */
export interface Trip {
  id: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
  updatedAt: string;
  itineraryDays: ItineraryDay[];
}
