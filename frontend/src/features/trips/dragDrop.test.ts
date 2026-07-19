import { describe, expect, it } from 'vitest';
import type { Trip, TripDestination } from '../../types';
import { SAVED_PLACES_COLUMN_ID, buildPlannerColumns, resolveDropTarget } from './dragDrop';

function destination(id: string, itineraryDayId: string | null): TripDestination {
  return {
    id,
    tripId: 'trip-1',
    itineraryDayId,
    providerPlaceId: `provider-${id}`,
    name: id,
    category: null,
    thumbnailUrl: null,
    lat: 0,
    lng: 0,
    position: 1,
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
    savedPlaces: [destination('saved-a', null)],
    itineraryDays: [
      {
        id: 'day-1',
        date: '2026-01-01',
        dayIndex: 1,
        tripDestinations: [destination('day1-a', 'day-1'), destination('day1-b', 'day-1')],
      },
      { id: 'day-2', date: '2026-01-02', dayIndex: 2, tripDestinations: [] },
    ],
  };
}

describe('buildPlannerColumns', () => {
  it('projects Saved Places first, then one column per itinerary day', () => {
    const columns = buildPlannerColumns(buildTrip());

    expect(columns.map((column) => column.id)).toEqual([SAVED_PLACES_COLUMN_ID, 'day-1', 'day-2']);
    expect(columns[0].itineraryDayId).toBeNull();
    expect(columns[1].title).toBe('Day 1 — 2026-01-01');
  });

  it('gives each day column a compact single-line shortTitle, keeping the full date in title', () => {
    const columns = buildPlannerColumns(buildTrip());

    expect(columns[1].shortTitle).toBe('Day 1 · Jan 1');
    expect(columns[1].title).toBe('Day 1 — 2026-01-01');
    expect(columns[2].shortTitle).toBe('Day 2 · Jan 2');
  });

  it('uses the same text for Saved Places\' title and shortTitle (no date to shorten)', () => {
    const columns = buildPlannerColumns(buildTrip());

    expect(columns[0].title).toBe('Saved Places');
    expect(columns[0].shortTitle).toBe('Saved Places');
  });
});

describe('resolveDropTarget', () => {
  it('returns null when there is no drop target', () => {
    const columns = buildPlannerColumns(buildTrip());
    expect(resolveDropTarget(columns, 'saved-a', null)).toBeNull();
  });

  it('returns null for an unknown active id', () => {
    const columns = buildPlannerColumns(buildTrip());
    expect(resolveDropTarget(columns, 'does-not-exist', 'day-1')).toBeNull();
  });

  it('moves a saved place onto a day column (dropped on empty column space) — appends at the end', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'saved-a', 'day-1');

    expect(result).toEqual({ tripDestinationId: 'saved-a', itineraryDayId: 'day-1', position: 3 });
  });

  it('moves a saved place to just before a specific item in a day', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'saved-a', 'day1-b');

    expect(result).toEqual({ tripDestinationId: 'saved-a', itineraryDayId: 'day-1', position: 2 });
  });

  it('reorders within the same column relative to another item', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'day1-b', 'day1-a');

    expect(result).toEqual({ tripDestinationId: 'day1-b', itineraryDayId: 'day-1', position: 1 });
  });

  it('drops onto an empty day column by its column id', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'day1-a', 'day-2');

    expect(result).toEqual({ tripDestinationId: 'day1-a', itineraryDayId: 'day-2', position: 1 });
  });

  it('moves a destination back to Saved Places via the column id', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'day1-a', SAVED_PLACES_COLUMN_ID);

    expect(result).toEqual({ tripDestinationId: 'day1-a', itineraryDayId: null, position: 2 });
  });

  it('returns null for a no-op drop back onto its original slot', () => {
    const columns = buildPlannerColumns(buildTrip());

    const result = resolveDropTarget(columns, 'day1-a', 'day1-a');

    expect(result).toBeNull();
  });
});
