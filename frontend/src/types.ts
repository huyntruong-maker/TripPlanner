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

export interface PagedResult<TItem> {
  items: TItem[];
  totalCount: number;
}

export interface LocationSuggestion {
  name: string;
  displayName: string;
  latitude: number;
  longitude: number;
  locationType: string;
  country: string;
}

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

/** Null when the provider has no hours data. */
export interface OpeningHours {
  displayText: string | null;
  weekdayText: string[];
  isOpenNow: boolean | null;
}

/** Every optional field is null/empty when the provider doesn't supply it; the view must still render. */
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

export interface ItineraryDay {
  id: string;
  date: string;
  dayIndex: number;
  tripDestinations: TripDestination[];
}

/** `itineraryDays` and `savedPlaces` are always `[]` from the list endpoint; only the detail endpoint populates them. */
export interface Trip {
  id: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
  updatedAt: string;
  itineraryDays: ItineraryDay[];
  /** Destinations not yet scheduled to a day, ordered by `position`. */
  savedPlaces: TripDestination[];
}
