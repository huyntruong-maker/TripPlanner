import { apiClient } from './client';
import type { ApiEnvelope, Trip, TripDestination } from '../types';

// Calls to the Trips endpoints (docs/API.md "Trips"). All require a JWT;
// apiClient's request interceptor attaches it automatically. A trip not
// owned by the caller returns 404 (NFR-6 — no ID-enumeration signal).

/** GET /trips — the caller's trip list; itineraryDays is always [] here. */
export async function getTrips(): Promise<Trip[]> {
  const { data } = await apiClient.get<ApiEnvelope<Trip[]>>('/trips');
  return data.result;
}

/** GET /trips/{id} — full detail including itineraryDays + tripDestinations. */
export async function getTrip(tripId: string): Promise<Trip> {
  const { data } = await apiClient.get<ApiEnvelope<Trip>>(`/trips/${encodeURIComponent(tripId)}`);
  return data.result;
}

export interface CreateTripPayload {
  name: string;
}

/** POST /trips — creates a trip without dates (F3-US1). */
export async function createTrip(payload: CreateTripPayload): Promise<Trip> {
  const { data } = await apiClient.post<ApiEnvelope<Trip>>('/trips', payload);
  return data.result;
}

export interface SetTripDatesPayload {
  startDate: string;
  endDate: string;
}

export interface SetTripDatesResult {
  trip: Trip;
  /** Set to `Trip.SetDates.DestinationsUnscheduled` when success is still true but items were unscheduled. */
  warningErrorCode: string | null;
}

/** PUT /trips/{id}/dates — regenerates itineraryDays for the new range (F3-US2). */
export async function setTripDates(
  tripId: string,
  payload: SetTripDatesPayload,
): Promise<SetTripDatesResult> {
  const { data } = await apiClient.put<ApiEnvelope<Trip>>(
    `/trips/${encodeURIComponent(tripId)}/dates`,
    payload,
  );
  return { trip: data.result, warningErrorCode: data.errorCode };
}

export interface AddTripDestinationPayload {
  itineraryDayId: string;
  providerPlaceId: string;
  name: string;
  category: string | null;
  thumbnailUrl: string | null;
  lat: number;
  lng: number;
}

/** POST /trips/{id}/destinations — adds a destination to a specific day (F3-US3). */
export async function addTripDestination(
  tripId: string,
  payload: AddTripDestinationPayload,
): Promise<TripDestination> {
  const { data } = await apiClient.post<ApiEnvelope<TripDestination>>(
    `/trips/${encodeURIComponent(tripId)}/destinations`,
    payload,
  );
  return data.result;
}

/** DELETE /trips/{id}/destinations/{tripDestinationId} — soft-delete, immediate (F3-US7). */
export async function removeTripDestination(
  tripId: string,
  tripDestinationId: string,
): Promise<void> {
  await apiClient.delete<ApiEnvelope<boolean>>(
    `/trips/${encodeURIComponent(tripId)}/destinations/${encodeURIComponent(tripDestinationId)}`,
  );
}
