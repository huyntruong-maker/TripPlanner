import { http, HttpResponse } from 'msw';
import type { Trip } from '../../../types';

const BASE_URL = 'http://localhost:5080/api/v1';

export const EXISTING_TRIP_ID = 'trip-with-dates';
export const TRIP_WITHOUT_DATES_ID = 'trip-without-dates';
export const SHORTEN_DATES_TRIP_ID = 'trip-to-shorten';
export const SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID = 'trip-to-shorten-with-destination';
export const TRIP_NOT_FOUND_ID = 'trip-does-not-exist';
export const DUPLICATE_NAME_TRIGGER = 'DuplicateTripNameTrigger';
export const EXISTING_DESTINATION_ID = 'existing-destination';
export const DROPPED_DESTINATION_ID = 'dropped-destination';

const DAY_1_ID = 'day-1';
const DAY_2_ID = 'day-2';
const SHORTEN_DAY_ID = 'shorten-day-1';
const SHORTEN_WITH_DESTINATION_DAY_1_ID = 'shorten-with-destination-day-1';
const SHORTEN_WITH_DESTINATION_DAY_2_ID = 'shorten-with-destination-day-2';
const SHORTEN_WITH_DESTINATION_DAY_3_ID = 'shorten-with-destination-day-3';

function defaultTrips(): Trip[] {
  return [
    {
      id: EXISTING_TRIP_ID,
      name: 'Paris 2026',
      startDate: '2026-07-01',
      endDate: '2026-07-02',
      createdAt: '2026-06-01T10:00:00Z',
      updatedAt: '2026-06-01T10:00:00Z',
      itineraryDays: [
        { id: DAY_1_ID, date: '2026-07-01', dayIndex: 1, tripDestinations: [] },
        {
          id: DAY_2_ID,
          date: '2026-07-02',
          dayIndex: 2,
          tripDestinations: [
            {
              id: EXISTING_DESTINATION_ID,
              tripId: EXISTING_TRIP_ID,
              itineraryDayId: DAY_2_ID,
              providerPlaceId: 'W999999',
              name: 'Notre-Dame',
              category: 'cultural',
              thumbnailUrl: null,
              lat: 48.853,
              lng: 2.3499,
              position: 1,
            },
          ],
        },
      ],
    },
    {
      id: TRIP_WITHOUT_DATES_ID,
      name: 'Someday Trip',
      startDate: null,
      endDate: null,
      createdAt: '2026-06-02T10:00:00Z',
      updatedAt: '2026-06-02T10:00:00Z',
      itineraryDays: [],
    },
    {
      id: SHORTEN_DATES_TRIP_ID,
      name: 'Trip To Shorten',
      startDate: '2026-08-01',
      endDate: '2026-08-05',
      createdAt: '2026-06-03T10:00:00Z',
      updatedAt: '2026-06-03T10:00:00Z',
      itineraryDays: [{ id: SHORTEN_DAY_ID, date: '2026-08-01', dayIndex: 1, tripDestinations: [] }],
    },
    {
      id: SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID,
      name: 'Trip With Scheduled Day',
      startDate: '2026-09-01',
      endDate: '2026-09-03',
      createdAt: '2026-06-04T10:00:00Z',
      updatedAt: '2026-06-04T10:00:00Z',
      itineraryDays: [
        {
          id: SHORTEN_WITH_DESTINATION_DAY_1_ID,
          date: '2026-09-01',
          dayIndex: 1,
          tripDestinations: [],
        },
        {
          id: SHORTEN_WITH_DESTINATION_DAY_2_ID,
          date: '2026-09-02',
          dayIndex: 2,
          tripDestinations: [],
        },
        {
          id: SHORTEN_WITH_DESTINATION_DAY_3_ID,
          date: '2026-09-03',
          dayIndex: 3,
          tripDestinations: [
            {
              id: DROPPED_DESTINATION_ID,
              tripId: SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID,
              itineraryDayId: SHORTEN_WITH_DESTINATION_DAY_3_ID,
              providerPlaceId: 'W111111',
              name: 'Louvre Museum',
              category: 'cultural',
              thumbnailUrl: null,
              lat: 48.8606,
              lng: 2.3376,
              position: 1,
            },
          ],
        },
      ],
    },
  ];
}

let tripsState: Trip[] = defaultTrips();

/** Test-only helper: restore the default fixture between tests. */
export function resetTripsFixture() {
  tripsState = defaultTrips();
}

/** Test-only helper: simulate a user with no saved trips (F3-US10 empty state). */
export function clearTripsFixture() {
  tripsState = [];
}

function envelope<T>(result: T, errorCode: string | null = null) {
  return { success: true, errorCode, error: null, validates: [], result };
}

function toListShape(trip: Trip): Trip {
  return { ...trip, itineraryDays: [] };
}

export const tripsHandlers = [
  http.get(`${BASE_URL}/trips`, () => HttpResponse.json(envelope(tripsState.map(toListShape)))),

  http.get(`${BASE_URL}/trips/:id`, ({ params }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(
        { success: false, errorCode: 'Trip.NotFound', error: 'Trip not found.', validates: [] },
        { status: 404 },
      );
    }
    return HttpResponse.json(envelope(trip));
  }),

  http.post(`${BASE_URL}/trips`, async ({ request }) => {
    const body = (await request.json()) as { name?: string };

    if (body.name === DUPLICATE_NAME_TRIGGER) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Trip.CreateTrip.Exception',
          error: 'Could not create trip.',
          validates: [],
        },
        { status: 500 },
      );
    }

    const newTrip: Trip = {
      id: `trip-${tripsState.length + 1}`,
      name: body.name ?? '',
      startDate: null,
      endDate: null,
      createdAt: '2026-07-05T10:00:00Z',
      updatedAt: '2026-07-05T10:00:00Z',
      itineraryDays: [],
    };
    tripsState = [...tripsState, newTrip];

    return HttpResponse.json(envelope(newTrip), { status: 201 });
  }),

  http.put(`${BASE_URL}/trips/:id/dates`, async ({ params, request }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(
        { success: false, errorCode: 'Trip.NotFound', error: 'Trip not found.', validates: [] },
        { status: 404 },
      );
    }

    const body = (await request.json()) as { startDate: string; endDate: string };
    const isShorten =
      params.id === SHORTEN_DATES_TRIP_ID ||
      params.id === SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID;

    const updatedTrip: Trip = {
      ...trip,
      startDate: body.startDate,
      endDate: body.endDate,
      itineraryDays: [{ id: DAY_1_ID, date: body.startDate, dayIndex: 1, tripDestinations: [] }],
    };
    tripsState = tripsState.map((item) => (item.id === trip.id ? updatedTrip : item));

    return HttpResponse.json(
      envelope(updatedTrip, isShorten ? 'Trip.SetDates.DestinationsUnscheduled' : null),
    );
  }),

  http.post(`${BASE_URL}/trips/:id/destinations`, async ({ params, request }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(
        { success: false, errorCode: 'Trip.NotFound', error: 'Trip not found.', validates: [] },
        { status: 404 },
      );
    }

    const body = (await request.json()) as {
      itineraryDayId: string;
      providerPlaceId: string;
      name: string;
      category: string | null;
      thumbnailUrl: string | null;
      lat: number;
      lng: number;
    };

    const newDestination = {
      id: `destination-${Date.now()}`,
      tripId: trip.id,
      itineraryDayId: body.itineraryDayId,
      providerPlaceId: body.providerPlaceId,
      name: body.name,
      category: body.category,
      thumbnailUrl: body.thumbnailUrl,
      lat: body.lat,
      lng: body.lng,
      position: 1,
    };

    const updatedTrip: Trip = {
      ...trip,
      itineraryDays: trip.itineraryDays.map((day) =>
        day.id === body.itineraryDayId
          ? { ...day, tripDestinations: [...day.tripDestinations, newDestination] }
          : day,
      ),
    };
    tripsState = tripsState.map((item) => (item.id === trip.id ? updatedTrip : item));

    return HttpResponse.json(envelope(newDestination), { status: 201 });
  }),

  http.delete(`${BASE_URL}/trips/:id/destinations/:tripDestinationId`, ({ params }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(
        { success: false, errorCode: 'Trip.NotFound', error: 'Trip not found.', validates: [] },
        { status: 404 },
      );
    }

    const updatedTrip: Trip = {
      ...trip,
      itineraryDays: trip.itineraryDays.map((day) => ({
        ...day,
        tripDestinations: day.tripDestinations.filter(
          (destination) => destination.id !== params.tripDestinationId,
        ),
      })),
    };
    tripsState = tripsState.map((item) => (item.id === trip.id ? updatedTrip : item));

    return HttpResponse.json(envelope(true));
  }),
];
