import type { Trip, TripDestination } from '../../types';

/** Column id for the Saved Places board column; distinct from any backend id (those are GUIDs). */
export const SAVED_PLACES_COLUMN_ID = 'saved-places';

export interface PlannerColumn {
  id: string;
  title: string;
  /** `null` for the Saved Places column. */
  itineraryDayId: string | null;
  destinations: TripDestination[];
}

/** Projects a trip into the planner board's columns: Saved Places, then one column per itinerary day. */
export function buildPlannerColumns(trip: Trip): PlannerColumn[] {
  return [
    {
      id: SAVED_PLACES_COLUMN_ID,
      title: 'Saved Places',
      itineraryDayId: null,
      destinations: trip.savedPlaces,
    },
    ...trip.itineraryDays.map((day) => ({
      id: day.id,
      title: `Day ${day.dayIndex} — ${day.date}`,
      itineraryDayId: day.id,
      destinations: day.tripDestinations,
    })),
  ];
}

export interface MoveVariables {
  tripDestinationId: string;
  itineraryDayId: string | null;
  position: number | null;
}

/**
 * Maps a dnd-kit drag-end (`active`/`over` ids) onto move-mutation variables,
 * given the board's current columns. `overId` may be either a column's own id
 * (dropped on empty column space — appends at the end) or another
 * destination's id (dropped near an item — inserts before it).
 *
 * Returns `null` when the drop isn't actionable (no `over`, unknown ids, or a
 * no-op drop back onto its original slot).
 */
export function resolveDropTarget(
  columns: PlannerColumn[],
  activeId: string,
  overId: string | null,
): MoveVariables | null {
  if (overId === null || overId === activeId) {
    return null;
  }

  const sourceColumn = columns.find((column) =>
    column.destinations.some((destination) => destination.id === activeId),
  );
  if (!sourceColumn) {
    return null;
  }

  const overIsColumn = columns.some((column) => column.id === overId);
  const targetColumn = overIsColumn
    ? (columns.find((column) => column.id === overId) ?? null)
    : (columns.find((column) => column.destinations.some((destination) => destination.id === overId)) ??
      null);
  if (!targetColumn) {
    return null;
  }

  const isSameColumn = sourceColumn.id === targetColumn.id;
  const targetDestinations = isSameColumn
    ? targetColumn.destinations.filter((destination) => destination.id !== activeId)
    : targetColumn.destinations;

  const overIndex = overIsColumn
    ? -1
    : targetDestinations.findIndex((destination) => destination.id === overId);
  const targetIndex = overIndex === -1 ? targetDestinations.length : overIndex;

  if (isSameColumn) {
    const originalIndex = sourceColumn.destinations.findIndex(
      (destination) => destination.id === activeId,
    );
    if (originalIndex === targetIndex) {
      return null;
    }
  }

  return {
    tripDestinationId: activeId,
    itineraryDayId: targetColumn.itineraryDayId,
    position: targetIndex + 1,
  };
}
