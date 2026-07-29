import { apiClient } from './client';
import type {
  ApiEnvelope,
  AttractionSummary,
  DestinationDetail,
  LocationSuggestion,
  PagedResult,
} from '../types';

// Destinations endpoints; all public (AllowAnonymous), no JWT required.

export interface SearchLocationsParams {
  query: string;
  maxResults?: number;
}

/** Returns up to 5 ranked matches. */
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

/** Returns 200 with partial data when optional fields are unavailable; 404 only if the provider doesn't recognize the id. */
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

export async function getAttractions(
  params: GetAttractionsParams,
): Promise<PagedResult<AttractionSummary>> {
  const { data } = await apiClient.get<ApiEnvelope<PagedResult<AttractionSummary>>>(
    '/destinations/attractions',
    { params },
  );
  return data.result;
}
