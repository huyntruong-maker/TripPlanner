import { apiClient } from './client';
import type { ApiEnvelope, Trip, TripDestination } from '../types';

// Trips endpoints; JWT required. A trip not owned by the caller returns 404 (NFR-6).

/** itineraryDays is always [] in the list response. */
export async function getTrips(): Promise<Trip[]> {
  const { data } = await apiClient.get<ApiEnvelope<Trip[]>>('/trips');
  return data.result;
}

/** Includes itineraryDays and tripDestinations, unlike the list endpoint. */
export async function getTrip(tripId: string): Promise<Trip> {
  const { data } = await apiClient.get<ApiEnvelope<Trip>>(`/trips/${encodeURIComponent(tripId)}`);
  return data.result;
}

export interface CreateTripPayload {
  name: string;
}

/** Creates a trip without dates. */
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

/** Regenerates itineraryDays for the new date range. */
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

/** Adds a destination to a specific itinerary day. */
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

/** Soft-deletes immediately. */
export async function removeTripDestination(
  tripId: string,
  tripDestinationId: string,
): Promise<void> {
  await apiClient.delete<ApiEnvelope<boolean>>(
    `/trips/${encodeURIComponent(tripId)}/destinations/${encodeURIComponent(tripDestinationId)}`,
  );
}
