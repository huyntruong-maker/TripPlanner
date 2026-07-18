import { describe, expect, it } from 'vitest';
import type { Trip, TripDestination } from '../../types';
import { moveDestinationInTrip } from './moveDestination';

function destination(overrides: Partial<TripDestination> & { id: string }): TripDestination {
  return {
    tripId: 'trip-1',
    itineraryDayId: null,
    providerPlaceId: `provider-${overrides.id}`,
    name: overrides.id,
    category: null,
    thumbnailUrl: null,
    lat: 0,
    lng: 0,
    position: 1,
    ...overrides,
  };
}

function buildTrip(): Trip {
  return {
    id: 'trip-1',
    name: 'Test Trip',
    startDate: '2026-01-01',
    endDate: '2026-01-02',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    savedPlaces: [
      destination({ id: 'saved-a', itineraryDayId: null, position: 1 }),
      destination({ id: 'saved-b', itineraryDayId: null, position: 2 }),
    ],
    itineraryDays: [
      {
        id: 'day-1',
        date: '2026-01-01',
        dayIndex: 1,
        tripDestinations: [
          destination({ id: 'day1-a', itineraryDayId: 'day-1', position: 1 }),
          destination({ id: 'day1-b', itineraryDayId: 'day-1', position: 2 }),
        ],
      },
      { id: 'day-2', date: '2026-01-02', dayIndex: 2, tripDestinations: [] },
    ],
  };
}

describe('moveDestinationInTrip', () => {
  it('moves a saved place into a day at the requested position (F3-US4)', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'saved-a', { itineraryDayId: 'day-1', position: 1 });

    expect(result.savedPlaces.map((item) => item.id)).toEqual(['saved-b']);
    expect(result.savedPlaces[0].position).toBe(1);

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['saved-a', 'day1-a', 'day1-b']);
    expect(day1.tripDestinations.map((item) => item.position)).toEqual([1, 2, 3]);
    expect(day1.tripDestinations[0].itineraryDayId).toBe('day-1');
  });

  it('reorders within the same day, renumbering positions (F3-US5)', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'day1-b', { itineraryDayId: 'day-1', position: 1 });

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['day1-b', 'day1-a']);
    expect(day1.tripDestinations.map((item) => item.position)).toEqual([1, 2]);
  });

  it('moves a destination between two days (F3-US6)', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'day1-a', { itineraryDayId: 'day-2', position: 1 });

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    const day2 = result.itineraryDays.find((day) => day.id === 'day-2')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['day1-b']);
    expect(day1.tripDestinations[0].position).toBe(1);
    expect(day2.tripDestinations.map((item) => item.id)).toEqual(['day1-a']);
    expect(day2.tripDestinations[0].itineraryDayId).toBe('day-2');
  });

  it('moves a destination out of a day back to Saved Places', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'day1-a', { itineraryDayId: null, position: null });

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['day1-b']);
    expect(result.savedPlaces.map((item) => item.id)).toEqual(['saved-a', 'saved-b', 'day1-a']);
    expect(result.savedPlaces[2].itineraryDayId).toBeNull();
  });

  it('appends at the end when position is null', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'saved-a', { itineraryDayId: 'day-1', position: null });

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['day1-a', 'day1-b', 'saved-a']);
  });

  it('clamps an out-of-range position to the end of the target list', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'saved-a', { itineraryDayId: 'day-1', position: 99 });

    const day1 = result.itineraryDays.find((day) => day.id === 'day-1')!;
    expect(day1.tripDestinations.map((item) => item.id)).toEqual(['day1-a', 'day1-b', 'saved-a']);
  });

  it('returns the trip unchanged when the destination id is not found', () => {
    const trip = buildTrip();

    const result = moveDestinationInTrip(trip, 'does-not-exist', {
      itineraryDayId: 'day-1',
      position: 1,
    });

    expect(result).toBe(trip);
  });
});
