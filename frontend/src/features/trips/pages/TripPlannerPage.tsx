import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import { sortableKeyboardCoordinates } from '@dnd-kit/sortable';
import { getApiErrorMessage } from '../../../api/errors';
import { removeTripDestination } from '../../../api/trips';
import { PlannerColumnView } from '../components/PlannerColumnView';
import { SavingIndicator } from '../components/SavingIndicator';
import { SetDatesForm } from '../components/SetDatesForm';
import { useMoveTripDestination } from '../hooks/useMoveTripDestination';
import { useTrip, tripMutationScopeKey, tripQueryKey } from '../hooks/useTrip';
import { buildPlannerColumns, resolveDropTarget } from '../lib/dragDrop';

const DESTINATIONS_UNSCHEDULED_WARNING = 'Trip.SetDates.DestinationsUnscheduled';
const BACK_LINK_CLASSES =
  'inline-flex items-center gap-2 text-primary font-label-md hover:underline';

export function TripPlannerPage() {
  const { tripId } = useParams<{ tripId: string }>();
  const queryClient = useQueryClient();
  const tripQuery = useTrip(tripId);
  const [datesWarning, setDatesWarning] = useState<string | null>(null);
  const [isSavedPlacesOpen, setIsSavedPlacesOpen] = useState(true);

  const removeMutation = useMutation({
    mutationKey: tripMutationScopeKey(tripId ?? ''),
    mutationFn: (tripDestinationId: string) =>
      removeTripDestination(tripId as string, tripDestinationId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tripQueryKey(tripId as string) }),
  });

  const moveMutation = useMoveTripDestination(tripId ?? '');

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  if (tripQuery.isLoading) {
    return (
      <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 border border-outline-variant/30">
        <p className="text-on-surface-variant text-body-md">Loading trip…</p>
      </div>
    );
  }

  if (tripQuery.isError || !tripQuery.data) {
    return (
      <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 border border-outline-variant/30 space-y-stack-md">
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(tripQuery.error, 'Could not load this trip.')}
        </p>
        <Link to="/trips" className={BACK_LINK_CLASSES}>
          <span className="material-symbols-outlined text-sm" aria-hidden="true">
            arrow_back
          </span>
          Back to my trips
        </Link>
      </div>
    );
  }

  const trip = tripQuery.data;
  const columns = buildPlannerColumns(trip);
  const [savedPlacesColumn, ...dayColumns] = columns;
  const removingId = removeMutation.isPending ? (removeMutation.variables ?? null) : null;

  function handleDatesSaved(warningErrorCode: string | null) {
    setDatesWarning(
      warningErrorCode === DESTINATIONS_UNSCHEDULED_WARNING
        ? 'Some destinations no longer fit in the new date range and were unscheduled.'
        : null,
    );
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    const variables = resolveDropTarget(columns, String(active.id), over ? String(over.id) : null);
    if (!variables) return;
    moveMutation.mutate(variables);
  }

  return (
    <div className="space-y-8">
      <Link to="/trips" className={BACK_LINK_CLASSES}>
        <span className="material-symbols-outlined text-sm" aria-hidden="true">
          arrow_back
        </span>
        Back to my trips
      </Link>

      <div className="bg-primary text-on-primary rounded-xl p-8 md:p-10 elevation-l1">
        <p className="flex items-center gap-2 text-label-sm font-label-sm uppercase tracking-wider opacity-80 mb-2">
          <span className="material-symbols-outlined text-sm" aria-hidden="true">
            flight_takeoff
          </span>
          Trip
        </p>
        <h1 className="text-display font-display leading-tight">{trip.name}</h1>
      </div>

      <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 border border-outline-variant/30 space-y-stack-lg">
        {datesWarning && (
          <p
            className="bg-[#FFF7E6] border border-[#F59E0B] text-[#92400E] rounded-lg px-4 py-3 text-label-md font-label-md"
            role="status"
          >
            {datesWarning}
          </p>
        )}
        <SetDatesForm trip={trip} onSaved={handleDatesSaved} />
      </div>

      <section>
        <div className="flex items-center justify-between mb-stack-lg">
          <h2 className="text-headline-lg font-headline-lg text-primary">Trip itinerary</h2>
          <SavingIndicator tripId={trip.id} />
        </div>

        {/* autoScroll (dnd-kit default, kept explicit) scrolls the page toward the pointer mid-drag since layout is normal vertical flow. */}
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
          autoScroll
        >
          {/* Two independent containers, not one grid — keeps the day grid from reserving Saved Places' cell and offsetting every row after it. */}
          <div className="lg:flex lg:items-start lg:gap-gutter">
            <aside className="mb-gutter lg:mb-0 lg:w-[240px] lg:flex-shrink-0 lg:sticky lg:top-6">
              <button
                type="button"
                onClick={() => setIsSavedPlacesOpen((open) => !open)}
                aria-expanded={isSavedPlacesOpen}
                aria-controls="saved-places-panel"
                className="lg:hidden w-full flex items-center justify-between gap-3 mb-3 bg-surface-container-lowest rounded-xl elevation-l1 border border-outline-variant/20 p-4"
              >
                <span className="flex items-center gap-2 font-label-md text-on-surface">
                  <span className="material-symbols-outlined text-primary" aria-hidden="true">
                    bookmark
                  </span>
                  Saved Places ({savedPlacesColumn.destinations.length})
                </span>
                <span className="material-symbols-outlined text-on-surface-variant" aria-hidden="true">
                  {isSavedPlacesOpen ? 'expand_less' : 'expand_more'}
                </span>
              </button>
              <div id="saved-places-panel" className={isSavedPlacesOpen ? undefined : 'hidden lg:block'}>
                <PlannerColumnView
                  column={savedPlacesColumn}
                  onRemove={(id) => removeMutation.mutate(id)}
                  removingId={removingId}
                  emptyMessage="No destinations saved yet."
                />
              </div>
            </aside>

            <div className="flex-1 min-w-0">
              {trip.itineraryDays.length === 0 ? (
                <div className="bg-surface-container-lowest rounded-xl elevation-l1 border border-outline-variant/20 p-10 text-center">
                  <p className="text-on-surface-variant text-body-md">
                    Set the trip dates above to generate your itinerary days.
                  </p>
                </div>
              ) : (
                <div className="grid gap-gutter grid-cols-[repeat(auto-fill,minmax(210px,1fr))] items-start">
                  {dayColumns.map((column) => (
                    <PlannerColumnView
                      key={column.id}
                      column={column}
                      onRemove={(id) => removeMutation.mutate(id)}
                      removingId={removingId}
                      emptyMessage="No destinations scheduled for this day yet."
                    />
                  ))}
                </div>
              )}
            </div>
          </div>
        </DndContext>
      </section>

      {removeMutation.isError && (
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(removeMutation.error, 'Could not remove this destination.')}
        </p>
      )}
    </div>
  );
}