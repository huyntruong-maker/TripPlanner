import type { Trip, TripDestination } from '../../types';

export interface MoveDestinationTarget {
  /** `null` targets the trip's Saved Places list. */
  itineraryDayId: string | null;
  /** 1-based; `null` appends at the end of the target list. */
  position: number | null;
}

/** Internal key used to address either Saved Places or a specific itinerary day's list. */
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

/** Renumbers a list's `position` field to 1-based, in its current order. */
function renumber(list: TripDestination[]): TripDestination[] {
  return list.map((item, index) => ({ ...item, position: index + 1 }));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Pure client-side projection of a move — used for the optimistic cache update
 * in `useMoveTripDestination` so the board reflects the change in well under
 * NFR-4's 100ms budget, before the server responds. Mirrors the backend's
 * position semantics: 1-based, `null` position appends at the end,
 * `itineraryDayId: null` means Saved Places.
 *
 * Returns `trip` unchanged if `tripDestinationId` isn't found anywhere in it.
 */
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
