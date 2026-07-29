import type { Trip, TripDestination } from '../../types';

export interface MoveDestinationTarget {
  /** `null` targets the trip's Saved Places list. */
  itineraryDayId: string | null;
  /** 1-based; `null` appends at the end of the target list. */
  position: number | null;
}

const SAVED_PLACES_KEY = '__saved-places__';

function keyFor(itineraryDayId: string | null): string {
  return itineraryDayId ?? SAVED_PLACES_KEY;
}

function getListByKey(trip: Trip, key: string): TripDestination[] {
  if (key === SAVED_PLACES_KEY) return trip.savedPlaces;
  return trip.itineraryDays.find((day) => day.id === key)?.tripDestinations ?? [];
}

function findOwnerKey(trip: Trip, tripDestinationId: string): string | null {
  if (trip.savedPlaces.some((item) => item.id === tripDestinationId)) {
    return SAVED_PLACES_KEY;
  }
  const owningDay = trip.itineraryDays.find((day) =>
    day.tripDestinations.some((item) => item.id === tripDestinationId),
  );
  return owningDay ? owningDay.id : null;
}

function renumber(list: TripDestination[]): TripDestination[] {
  return list.map((item, index) => ({ ...item, position: index + 1 }));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/** Optimistic-cache projection of a move for useMoveTripDestination (NFR-4: under 100ms); returns `trip` unchanged if not found. */
export function moveDestinationInTrip(
  trip: Trip,
  tripDestinationId: string,
  target: MoveDestinationTarget,
): Trip {
  const sourceKey = findOwnerKey(trip, tripDestinationId);
  if (sourceKey === null) {
    return trip;
  }

  const targetKey = keyFor(target.itineraryDayId);
  const sourceList = getListByKey(trip, sourceKey);
  const movedItem = sourceList.find((item) => item.id === tripDestinationId);
  if (!movedItem) {
    return trip;
  }

  const sourceRemaining = sourceList.filter((item) => item.id !== tripDestinationId);
  const isSameList = sourceKey === targetKey;
  const targetBaseList = isSameList ? sourceRemaining : getListByKey(trip, targetKey);

  const insertionIndex =
    target.position === null
      ? targetBaseList.length
      : clamp(target.position - 1, 0, targetBaseList.length);

  const updatedItem: TripDestination = { ...movedItem, itineraryDayId: target.itineraryDayId };
  const nextTargetList = renumber([
    ...targetBaseList.slice(0, insertionIndex),
    updatedItem,
    ...targetBaseList.slice(insertionIndex),
  ]);
  const nextSourceList = isSameList ? nextTargetList : renumber(sourceRemaining);

  function resolveList(key: string): TripDestination[] {
    if (key === targetKey) return nextTargetList;
    if (key === sourceKey) return nextSourceList;
    return getListByKey(trip, key);
  }

  return {
    ...trip,
    savedPlaces: resolveList(SAVED_PLACES_KEY),
    itineraryDays: trip.itineraryDays.map((day) => ({
      ...day,
      tripDestinations: resolveList(day.id),
    })),
  };
}
