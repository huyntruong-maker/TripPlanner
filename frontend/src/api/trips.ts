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
  /** `null` adds the destination to the trip's unscheduled "Saved Places" list. */
  itineraryDayId: string | null;
  providerPlaceId: string;
  name: string;
  category: string | null;
  thumbnailUrl: string | null;
  lat: number;
  lng: number;
}

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

export interface MoveTripDestinationPayload {
  /** `null` moves the destination to Saved Places. */
  itineraryDayId: string | null;
  /** 1-based; `null` appends at the end of the target list. */
  position: number | null;
}

/** Returns the full updated trip, not just the moved destination. */
export async function moveTripDestination(
  tripId: string,
  tripDestinationId: string,
  payload: MoveTripDestinationPayload,
): Promise<Trip> {
  const { data } = await apiClient.put<ApiEnvelope<Trip>>(
    `/trips/${encodeURIComponent(tripId)}/destinations/${encodeURIComponent(tripDestinationId)}`,
    payload,
  );
  return data.result;
}
