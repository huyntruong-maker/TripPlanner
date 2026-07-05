import { apiClient } from './client';
import type {
  ApiEnvelope,
  AttractionSummary,
  DestinationDetail,
  LocationSuggestion,
  PagedResult,
} from '../types';

// Calls to the Destinations endpoints (docs/API.md "Destinations"). All are
// public (AllowAnonymous) — no JWT required.

export interface SearchLocationsParams {
  query: string;
  maxResults?: number;
}

/** GET /destinations/locations/search — up to 5 ranked city/country matches. */
export async function searchLocations({
  query,
  maxResults,
}: SearchLocationsParams): Promise<PagedResult<LocationSuggestion>> {
  const { data } = await apiClient.get<ApiEnvelope<PagedResult<LocationSuggestion>>>(
    '/destinations/locations/search',
    { params: { query, maxResults } },
  );
  return data.result;
}

/**
 * GET /destinations/{providerPlaceId} — always returns 200 with partial data
 * when optional fields are unavailable (F2-US1); only 404 when the provider
 * doesn't recognize the id at all.
 */
export async function getDestinationDetail(providerPlaceId: string): Promise<DestinationDetail> {
  const { data } = await apiClient.get<ApiEnvelope<DestinationDetail>>(
    `/destinations/${encodeURIComponent(providerPlaceId)}`,
  );
  return data.result;
}

export interface GetAttractionsParams {
  latitude: number;
  longitude: number;
  radiusMeters?: number;
  page?: number;
  pageSize?: number;
}

/** GET /destinations/attractions — paginated list; empty `items` when none found. */
export async function getAttractions(
  params: GetAttractionsParams,
): Promise<PagedResult<AttractionSummary>> {
  const { data } = await apiClient.get<ApiEnvelope<PagedResult<AttractionSummary>>>(
    '/destinations/attractions',
    { params },
  );
  return data.result;
}
