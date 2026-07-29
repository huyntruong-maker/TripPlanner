import type { Trip, TripDestination } from '../../../types';

/** Column id for the Saved Places board column; distinct from any backend id (those are GUIDs). */
export const SAVED_PLACES_COLUMN_ID = 'saved-places';

export interface PlannerColumn {
  id: string;
  /** Full text, e.g. "Day 1 — 2026-08-01"; used as the column's `aria-label` and `title` so the shortened heading never loses information. */
  title: string;
  /** Compact one-line visible heading, e.g. "Day 1 · Aug 1" (same as `title` for Saved Places). */
  shortTitle: string;
  /** `null` for the Saved Places column. */
  itineraryDayId: string | null;
  destinations: TripDestination[];
}

/** Renders an ISO date (`"2026-08-01"`) as `"Aug 1"`; falls back to the raw string if unparsable. */
function formatShortDate(isoDate: string): string {
  // Append a local (non-UTC) time so the date doesn't shift a day back in negative-offset zones.
  const date = new Date(`${isoDate}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return isoDate;
  }
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

export function buildPlannerColumns(trip: Trip): PlannerColumn[] {
  return [
    {
      id: SAVED_PLACES_COLUMN_ID,
      title: 'Saved Places',
      shortTitle: 'Saved Places',
      itineraryDayId: null,
      destinations: trip.savedPlaces,
    },
    ...trip.itineraryDays.map((day) => ({
      id: day.id,
      title: `Day ${day.dayIndex} — ${day.date}`,
      shortTitle: `Day ${day.dayIndex} · ${formatShortDate(day.date)}`,
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

/** Maps dnd-kit's active/over ids to move variables (over a column id appends, over an item id inserts before it); null if not actionable. */
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
