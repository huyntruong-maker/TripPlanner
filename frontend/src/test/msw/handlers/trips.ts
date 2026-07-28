import { delay, http, HttpResponse } from 'msw';
import type { Trip, TripDestination } from '../../../types';

const BASE_URL = 'http://localhost:5080/api/v1';
// Small but nonzero so the "Saving…" indicator has an observable in-flight window.
const MUTATION_DELAY_MS = 30;

export const EXISTING_TRIP_ID = 'trip-with-dates';
export const TRIP_WITHOUT_DATES_ID = 'trip-without-dates';
export const SHORTEN_DATES_TRIP_ID = 'trip-to-shorten';
export const TRIP_NOT_FOUND_ID = 'trip-does-not-exist';
export const DUPLICATE_NAME_TRIGGER = 'DuplicateTripNameTrigger';
export const EXISTING_DESTINATION_ID = 'existing-destination';

export const PLANNER_TRIP_ID = 'planner-trip';
export const PLANNER_DAY_1_ID = 'planner-day-1';
export const PLANNER_DAY_2_ID = 'planner-day-2';
export const PLANNER_SAVED_PLACE_ID = 'planner-saved-1';
export const PLANNER_DUPLICATE_SAVED_PLACE_ID = 'planner-saved-duplicate-source';
export const PLANNER_DAY1_DESTINATION_ID = 'planner-day1-destination-1';
export const PLANNER_DUPLICATE_PROVIDER_PLACE_ID = 'W-duplicate-trigger';
export const MOVE_EXCEPTION_TRIGGER_ID = 'move-exception-trigger';
export const RETRY_SUCCEEDS_DESTINATION_ID = 'retry-succeeds-destination';

const DAY_1_ID = 'day-1';
const DAY_2_ID = 'day-2';
const SHORTEN_DAY_ID = 'shorten-day-1';

function defaultTrips(): Trip[] {
  return [
    {
      id: EXISTING_TRIP_ID,
      name: 'Paris 2026',
      startDate: '2026-07-01',
      endDate: '2026-07-02',
      createdAt: '2026-06-01T10:00:00Z',
      updatedAt: '2026-06-01T10:00:00Z',
      savedPlaces: [],
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
      savedPlaces: [],
      itineraryDays: [],
    },
    {
      id: SHORTEN_DATES_TRIP_ID,
      name: 'Trip To Shorten',
      startDate: '2026-08-01',
      endDate: '2026-08-05',
      createdAt: '2026-06-03T10:00:00Z',
      updatedAt: '2026-06-03T10:00:00Z',
      savedPlaces: [],
      itineraryDays: [{ id: SHORTEN_DAY_ID, date: '2026-08-01', dayIndex: 1, tripDestinations: [] }],
    },
    {
      id: PLANNER_TRIP_ID,
      name: 'Planner Board Trip',
      startDate: '2026-09-01',
      endDate: '2026-09-02',
      createdAt: '2026-06-04T10:00:00Z',
      updatedAt: '2026-06-04T10:00:00Z',
      savedPlaces: [
        {
          id: PLANNER_SAVED_PLACE_ID,
          tripId: PLANNER_TRIP_ID,
          itineraryDayId: null,
          providerPlaceId: 'W-louvre',
          name: 'Louvre Museum',
          category: 'museum',
          thumbnailUrl: null,
          lat: 48.8606,
          lng: 2.3376,
          position: 1,
        },
        {
          id: RETRY_SUCCEEDS_DESTINATION_ID,
          tripId: PLANNER_TRIP_ID,
          itineraryDayId: null,
          providerPlaceId: 'W-retry',
          name: 'Sacré-Cœur',
          category: 'cultural',
          thumbnailUrl: null,
          lat: 48.8867,
          lng: 2.3431,
          position: 2,
        },
        {
          id: PLANNER_DUPLICATE_SAVED_PLACE_ID,
          tripId: PLANNER_TRIP_ID,
          itineraryDayId: null,
          // Same providerPlaceId as PLANNER_DAY1_DESTINATION_ID — moving this into Day 1 triggers DuplicateInDay.
          providerPlaceId: PLANNER_DUPLICATE_PROVIDER_PLACE_ID,
          name: 'Duplicate Trigger Place (saved copy)',
          category: 'cultural',
          thumbnailUrl: null,
          lat: 48.86,
          lng: 2.36,
          position: 3,
        },
      ],
      itineraryDays: [
        {
          id: PLANNER_DAY_1_ID,
          date: '2026-09-01',
          dayIndex: 1,
          tripDestinations: [
            {
              id: PLANNER_DAY1_DESTINATION_ID,
              tripId: PLANNER_TRIP_ID,
              itineraryDayId: PLANNER_DAY_1_ID,
              providerPlaceId: PLANNER_DUPLICATE_PROVIDER_PLACE_ID,
              name: 'Duplicate Trigger Place',
              category: 'cultural',
              thumbnailUrl: null,
              lat: 48.85,
              lng: 2.35,
              position: 1,
            },
          ],
        },
        { id: PLANNER_DAY_2_ID, date: '2026-09-02', dayIndex: 2, tripDestinations: [] },
      ],
    },
  ];
}

let tripsState: Trip[] = defaultTrips();
let retriedOnceIds = new Set<string>();

export function resetTripsFixture() {
  tripsState = defaultTrips();
  retriedOnceIds = new Set<string>();
}

export function clearTripsFixture() {
  tripsState = [];
}

function envelope<T>(result: T, errorCode: string | null = null) {
  return { success: true, errorCode, error: null, validates: [], result };
}

function errorEnvelope(errorCode: string, error: string) {
  return { success: false, errorCode, error, validates: [] };
}

function toListShape(trip: Trip): Trip {
  return { ...trip, itineraryDays: [], savedPlaces: [] };
}

function findDestination(
  trip: Trip,
  tripDestinationId: string,
): { destination: TripDestination; ownerDayId: string | null } | null {
  const savedPlace = trip.savedPlaces.find((item) => item.id === tripDestinationId);
  if (savedPlace) return { destination: savedPlace, ownerDayId: null };

  for (const day of trip.itineraryDays) {
    const found = day.tripDestinations.find((item) => item.id === tripDestinationId);
    if (found) return { destination: found, ownerDayId: day.id };
  }
  return null;
}

function listByDayId(trip: Trip, itineraryDayId: string | null): TripDestination[] {
  if (itineraryDayId === null) return trip.savedPlaces;
  return trip.itineraryDays.find((day) => day.id === itineraryDayId)?.tripDestinations ?? [];
}

export const tripsHandlers = [
  http.get(`${BASE_URL}/trips`, () => HttpResponse.json(envelope(tripsState.map(toListShape)))),

  http.get(`${BASE_URL}/trips/:id`, ({ params }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(errorEnvelope('Trip.NotFound', 'Trip not found.'), { status: 404 });
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
      savedPlaces: [],
      itineraryDays: [],
    };
    tripsState = [...tripsState, newTrip];

    return HttpResponse.json(envelope(newTrip), { status: 201 });
  }),

  http.put(`${BASE_URL}/trips/:id/dates`, async ({ params, request }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(errorEnvelope('Trip.NotFound', 'Trip not found.'), { status: 404 });
    }

    const body = (await request.json()) as { startDate: string; endDate: string };
    const isShorten = params.id === SHORTEN_DATES_TRIP_ID;

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
      return HttpResponse.json(errorEnvelope('Trip.NotFound', 'Trip not found.'), { status: 404 });
    }

    const body = (await request.json()) as {
      itineraryDayId: string | null;
      providerPlaceId: string;
      name: string;
      category: string | null;
      thumbnailUrl: string | null;
      lat: number;
      lng: number;
    };
    const itineraryDayId = body.itineraryDayId ?? null;

    if (itineraryDayId !== null) {
      const day = trip.itineraryDays.find((item) => item.id === itineraryDayId);
      if (!day) {
        return HttpResponse.json(
          errorEnvelope('Trip.AddDestination.ItineraryDayNotFound', 'Itinerary day not found.'),
          { status: 404 },
        );
      }
      if (day.tripDestinations.some((item) => item.providerPlaceId === body.providerPlaceId)) {
        return HttpResponse.json(
          errorEnvelope(
            'Trip.AddDestination.DuplicateInDay',
            'This destination is already in that day.',
          ),
          { status: 400 },
        );
      }
    }

    const newDestination: TripDestination = {
      id: `destination-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      tripId: trip.id,
      itineraryDayId,
      providerPlaceId: body.providerPlaceId,
      name: body.name,
      category: body.category,
      thumbnailUrl: body.thumbnailUrl,
      lat: body.lat,
      lng: body.lng,
      position: listByDayId(trip, itineraryDayId).length + 1,
    };

    const updatedTrip: Trip =
      itineraryDayId === null
        ? { ...trip, savedPlaces: [...trip.savedPlaces, newDestination] }
        : {
            ...trip,
            itineraryDays: trip.itineraryDays.map((day) =>
              day.id === itineraryDayId
                ? { ...day, tripDestinations: [...day.tripDestinations, newDestination] }
                : day,
            ),
          };
    tripsState = tripsState.map((item) => (item.id === trip.id ? updatedTrip : item));

    return HttpResponse.json(envelope(newDestination), { status: 201 });
  }),

  http.delete(`${BASE_URL}/trips/:id/destinations/:tripDestinationId`, async ({ params }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(errorEnvelope('Trip.NotFound', 'Trip not found.'), { status: 404 });
    }

    await delay(MUTATION_DELAY_MS);

    const updatedTrip: Trip = {
      ...trip,
      savedPlaces: trip.savedPlaces.filter((item) => item.id !== params.tripDestinationId),
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

  http.put(`${BASE_URL}/trips/:id/destinations/:tripDestinationId`, async ({ params, request }) => {
    const trip = tripsState.find((item) => item.id === params.id);
    if (!trip) {
      return HttpResponse.json(errorEnvelope('Trip.NotFound', 'Trip not found.'), { status: 404 });
    }

    const tripDestinationId = params.tripDestinationId as string;

    if (
      tripDestinationId === MOVE_EXCEPTION_TRIGGER_ID ||
      (tripDestinationId === RETRY_SUCCEEDS_DESTINATION_ID && !retriedOnceIds.has(tripDestinationId))
    ) {
      retriedOnceIds.add(tripDestinationId);
      return HttpResponse.json(
        errorEnvelope('Trip.MoveDestination.Exception', 'Could not move this destination.'),
        { status: 500 },
      );
    }

    await delay(MUTATION_DELAY_MS);

    const body = (await request.json()) as { itineraryDayId: string | null; position: number | null };
    const located = findDestination(trip, tripDestinationId);
    if (!located) {
      return HttpResponse.json(
        errorEnvelope('Trip.MoveDestination.DestinationNotFound', 'Destination not found.'),
        { status: 404 },
      );
    }

    if (body.itineraryDayId !== null && !trip.itineraryDays.some((day) => day.id === body.itineraryDayId)) {
      return HttpResponse.json(
        errorEnvelope('Trip.MoveDestination.ItineraryDayNotFound', 'Itinerary day not found.'),
        { status: 404 },
      );
    }

    const targetList = listByDayId(trip, body.itineraryDayId);
    const isDuplicate = targetList.some(
      (item) => item.id !== tripDestinationId && item.providerPlaceId === located.destination.providerPlaceId,
    );
    if (isDuplicate) {
      return HttpResponse.json(
        errorEnvelope(
          'Trip.MoveDestination.DuplicateInDay',
          'This destination is already in that day.',
        ),
        { status: 400 },
      );
    }

    const withoutItem: Trip = {
      ...trip,
      savedPlaces: trip.savedPlaces.filter((item) => item.id !== tripDestinationId),
      itineraryDays: trip.itineraryDays.map((day) => ({
        ...day,
        tripDestinations: day.tripDestinations.filter((item) => item.id !== tripDestinationId),
      })),
    };

    const updatedDestination: TripDestination = {
      ...located.destination,
      itineraryDayId: body.itineraryDayId,
    };
    const baseTargetList = listByDayId(withoutItem, body.itineraryDayId);
    const insertionIndex =
      body.position === null
        ? baseTargetList.length
        : Math.min(Math.max(body.position - 1, 0), baseTargetList.length);
    const nextTargetList = [
      ...baseTargetList.slice(0, insertionIndex),
      updatedDestination,
      ...baseTargetList.slice(insertionIndex),
    ].map((item, index) => ({ ...item, position: index + 1 }));

    const updatedTrip: Trip = {
      ...withoutItem,
      savedPlaces: body.itineraryDayId === null ? nextTargetList : withoutItem.savedPlaces,
      itineraryDays: withoutItem.itineraryDays.map((day) =>
        day.id === body.itineraryDayId ? { ...day, tripDestinations: nextTargetList } : day,
      ),
    };
    tripsState = tripsState.map((item) => (item.id === trip.id ? updatedTrip : item));

    return HttpResponse.json(envelope(updatedTrip));
  }),
];
