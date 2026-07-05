import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getApiErrorMessage } from '../../api/errors';
import { removeTripDestination, setTripDates } from '../../api/trips';
import { useToast } from '../../components/toast/ToastProvider';
import type { Trip } from '../../types';
import { useTrip, tripQueryKey } from './useTrip';
import { setDatesSchema, type SetDatesFormValues } from './schemas';

const DESTINATIONS_UNSCHEDULED_WARNING = 'Trip.SetDates.DestinationsUnscheduled';
const BACK_LINK_CLASSES =
  'inline-flex items-center gap-2 text-primary font-label-md hover:underline';
const INPUT_CLASSES =
  'w-full h-12 px-4 rounded-lg border border-outline-variant focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-body-md outline-none';

/** F3/US2, US3, US7, US10 — set dates, view the itinerary board, add/remove destinations. */
export function TripPlannerPage() {
  const { tripId } = useParams<{ tripId: string }>();
  const queryClient = useQueryClient();
  const tripQuery = useTrip(tripId);
  const [datesWarning, setDatesWarning] = useState<string | null>(null);

  const removeMutation = useMutation({
    mutationFn: (tripDestinationId: string) =>
      removeTripDestination(tripId as string, tripDestinationId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tripQueryKey(tripId as string) }),
  });

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

  function handleDatesSaved(warningErrorCode: string | null) {
    setDatesWarning(
      warningErrorCode === DESTINATIONS_UNSCHEDULED_WARNING
        ? 'Some destinations no longer fit in the new date range and were unscheduled.'
        : null,
    );
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

      {trip.itineraryDays.length === 0 ? (
        <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 border border-outline-variant/30 text-center">
          <p className="text-on-surface-variant text-body-md">
            Set the trip dates above to generate your itinerary days.
          </p>
        </div>
      ) : (
        <section>
          <h2 className="text-headline-lg font-headline-lg text-primary mb-stack-lg">
            Trip itinerary
          </h2>
          <div className="overflow-x-auto flex gap-gutter pb-4">
            {trip.itineraryDays.map((day) => (
              <div
                key={day.id}
                className="min-w-[320px] flex-shrink-0 bg-surface-container-lowest rounded-xl elevation-l1 border border-outline-variant/20 p-6"
              >
                <header className="flex justify-between items-center mb-6">
                  <h3 className="text-headline-md font-headline-md text-on-surface">
                    Day {day.dayIndex} — {day.date}
                  </h3>
                  <span
                    className="bg-surface-container text-primary p-2 rounded-lg flex-shrink-0"
                    aria-hidden="true"
                  >
                    <span className="material-symbols-outlined">event</span>
                  </span>
                </header>
                {day.tripDestinations.length === 0 ? (
                  <p className="text-on-surface-variant text-body-md text-center py-8">
                    No destinations scheduled for this day yet.
                  </p>
                ) : (
                  <ul className="space-y-4">
                    {day.tripDestinations.map((destination) => (
                      <li
                        key={destination.id}
                        className="p-4 rounded-lg bg-surface border border-outline-variant/30 flex justify-between items-center gap-3"
                      >
                        <span className="font-label-md text-on-surface">{destination.name}</span>
                        <button
                          type="button"
                          className="text-error bg-error-container/20 px-3 py-1 rounded-full text-label-sm hover:bg-error-container/40 transition-colors disabled:opacity-60 disabled:cursor-not-allowed flex-shrink-0"
                          onClick={() => removeMutation.mutate(destination.id)}
                          disabled={removeMutation.isPending && removeMutation.variables === destination.id}
                        >
                          Remove
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            ))}
          </div>
        </section>
      )}

      {removeMutation.isError && (
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(removeMutation.error, 'Could not remove this destination.')}
        </p>
      )}
    </div>
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
