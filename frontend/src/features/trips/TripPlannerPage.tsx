import { useState, type CSSProperties } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useIsMutating, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { getApiErrorMessage } from '../../api/errors';
import { removeTripDestination, setTripDates } from '../../api/trips';
import { useToast } from '../../components/toast/ToastProvider';
import type { Trip, TripDestination } from '../../types';
import { buildPlannerColumns, resolveDropTarget, type PlannerColumn } from './dragDrop';
import { useMoveTripDestination } from './useMoveTripDestination';
import { useTrip, tripMutationScopeKey, tripQueryKey } from './useTrip';
import { setDatesSchema, type SetDatesFormValues } from './schemas';

const DESTINATIONS_UNSCHEDULED_WARNING = 'Trip.SetDates.DestinationsUnscheduled';
const BACK_LINK_CLASSES =
  'inline-flex items-center gap-2 text-primary font-label-md hover:underline';
const INPUT_CLASSES =
  'w-full h-12 px-4 rounded-lg border border-outline-variant focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-body-md outline-none';

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

function SavingIndicator({ tripId }: { tripId: string }) {
  const mutatingCount = useIsMutating({ mutationKey: tripMutationScopeKey(tripId) });

  return (
    <p
      className="flex items-center gap-2 text-label-sm font-label-sm text-on-surface-variant"
      role="status"
      aria-live="polite"
    >
      <span className="material-symbols-outlined text-[16px]" aria-hidden="true">
        {mutatingCount > 0 ? 'sync' : 'cloud_done'}
      </span>
      {mutatingCount > 0 ? 'Saving…' : 'All changes saved'}
    </p>
  );
}

interface PlannerColumnViewProps {
  column: PlannerColumn;
  onRemove: (tripDestinationId: string) => void;
  removingId: string | null;
  emptyMessage: string;
}

function PlannerColumnView({ column, onRemove, removingId, emptyMessage }: PlannerColumnViewProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.id });

  return (
    <div className="min-h-[160px] bg-surface-container-lowest rounded-xl elevation-l1 border border-outline-variant/20 p-4">
      <header className="flex justify-between items-center gap-2 mb-3">
        {/* Accessible name comes from aria-label (full text), not the truncated visible text. */}
        <h3
          className="min-w-0 truncate whitespace-nowrap text-label-md font-label-md text-on-surface"
          title={column.title}
          aria-label={column.title}
        >
          {column.shortTitle}
        </h3>
        <span
          className="bg-surface-container text-primary p-1 rounded-lg flex-shrink-0"
          aria-hidden="true"
        >
          <span className="material-symbols-outlined text-[18px]">
            {column.itineraryDayId === null ? 'bookmark' : 'event'}
          </span>
        </span>
      </header>

      <SortableContext
        items={column.destinations.map((destination) => destination.id)}
        strategy={verticalListSortingStrategy}
      >
        <div
          ref={setNodeRef}
          className={`rounded-lg transition-colors ${
            isOver ? 'bg-primary/10 ring-2 ring-inset ring-primary' : ''
          }`}
        >
          {column.destinations.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 py-3 rounded-lg border-2 border-dashed border-outline-variant/40 text-center">
              <span className="material-symbols-outlined text-lg text-outline" aria-hidden="true">
                {column.itineraryDayId === null ? 'bookmark_border' : 'add_location_alt'}
              </span>
              <p className="text-on-surface-variant text-label-sm px-2">{emptyMessage}</p>
            </div>
          ) : (
            <ul className="space-y-2">
              {column.destinations.map((destination) => (
                <SortableDestinationItem
                  key={destination.id}
                  destination={destination}
                  onRemove={onRemove}
                  isRemoving={removingId === destination.id}
                />
              ))}
            </ul>
          )}
        </div>
      </SortableContext>
    </div>
  );
}

interface SortableDestinationItemProps {
  destination: TripDestination;
  onRemove: (tripDestinationId: string) => void;
  isRemoving: boolean;
}

function SortableDestinationItem({ destination, onRemove, isRemoving }: SortableDestinationItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: destination.id,
  });

  const style: CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      className="min-h-[48px] p-2.5 rounded-lg bg-surface border border-outline-variant/30 flex items-center gap-2"
    >
      <button
        type="button"
        className="material-symbols-outlined text-outline cursor-grab active:cursor-grabbing touch-none flex-shrink-0"
        aria-label={`Reorder ${destination.name}`}
        {...attributes}
        {...listeners}
      >
        drag_indicator
      </button>
      <span
        className="font-label-md text-on-surface flex-grow min-w-0 truncate"
        title={destination.name}
      >
        {destination.name}
      </span>
      <button
        type="button"
        className="text-error bg-error-container/20 px-3 py-1 rounded-full text-label-sm hover:bg-error-container/40 transition-colors disabled:opacity-60 disabled:cursor-not-allowed flex-shrink-0"
        onClick={() => onRemove(destination.id)}
        disabled={isRemoving}
      >
        Remove
      </button>
    </li>
  );
}

interface SetDatesFormProps {
  trip: Trip;
  onSaved: (warningErrorCode: string | null) => void;
}

function SetDatesForm({ trip, onSaved }: SetDatesFormProps) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SetDatesFormValues>({
    resolver: zodResolver(setDatesSchema),
    defaultValues: { startDate: trip.startDate ?? '', endDate: trip.endDate ?? '' },
  });

  async function onSubmit(values: SetDatesFormValues) {
    setFormError(null);
    try {
      const { warningErrorCode } = await setTripDates(trip.id, values);
      await queryClient.invalidateQueries({ queryKey: tripQueryKey(trip.id) });
      onSaved(warningErrorCode);
    } catch (err) {
      const message = getApiErrorMessage(err, 'Could not save the trip dates.');
      setFormError(message);
      showToast(message);
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      noValidate
      className="flex flex-col md:flex-row md:items-end gap-stack-lg"
    >
      <div className="w-full md:w-1/3 space-y-2">
        <label
          htmlFor="set-dates-startDate"
          className="block text-label-md font-label-md text-on-surface-variant"
        >
          Start date
        </label>
        <input
          id="set-dates-startDate"
          type="date"
          className={INPUT_CLASSES}
          {...register('startDate')}
          aria-invalid={Boolean(errors.startDate)}
          aria-describedby={errors.startDate ? 'set-dates-startDate-error' : undefined}
        />
        {errors.startDate && (
          <p className="text-error text-label-sm font-semibold" id="set-dates-startDate-error">
            {errors.startDate.message}
          </p>
        )}
      </div>
      <div className="w-full md:w-1/3 space-y-2">
        <label
          htmlFor="set-dates-endDate"
          className="block text-label-md font-label-md text-on-surface-variant"
        >
          End date
        </label>
        <input
          id="set-dates-endDate"
          type="date"
          className={INPUT_CLASSES}
          {...register('endDate')}
          aria-invalid={Boolean(errors.endDate)}
          aria-describedby={errors.endDate ? 'set-dates-endDate-error' : undefined}
        />
        {errors.endDate && (
          <p className="text-error text-label-sm font-semibold" id="set-dates-endDate-error">
            {errors.endDate.message}
          </p>
        )}
      </div>
      {formError && (
        <p className="text-error text-label-sm font-semibold w-full" role="alert">
          {formError}
        </p>
      )}
      <div className="w-full md:w-auto">
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full md:w-auto bg-primary text-on-primary h-12 px-8 rounded-lg font-label-md hover:opacity-90 active:scale-95 transition-all disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
            {trip.startDate ? 'sync' : 'event_available'}
          </span>
          {isSubmitting ? 'Saving…' : trip.startDate ? 'Update dates' : 'Set dates'}
        </button>
      </div>
    </form>
  );
}
