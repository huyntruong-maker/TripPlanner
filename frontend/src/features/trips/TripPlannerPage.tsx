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
      <div className="card">
        <p className="state-message state-message--loading">Loading trip…</p>
      </div>
    );
  }

  if (tripQuery.isError || !tripQuery.data) {
    return (
      <div className="card">
        <p className="error" role="alert">
          {getApiErrorMessage(tripQuery.error, 'Could not load this trip.')}
        </p>
        <p>
          <Link to="/trips" className="back-link">
            Back to my trips
          </Link>
        </p>
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
    <div className="trip-planner">
      <p>
        <Link to="/trips" className="back-link">
          Back to my trips
        </Link>
      </p>

      <div className="card">
        <h1>{trip.name}</h1>
        {datesWarning && (
          <p className="warning" role="status">
            {datesWarning}
          </p>
        )}
        <SetDatesForm trip={trip} onSaved={handleDatesSaved} />
      </div>

      {trip.itineraryDays.length === 0 ? (
        <div className="card">
          <p className="state-message state-message--empty">
            Set the trip dates above to generate your itinerary days.
          </p>
        </div>
      ) : (
        <div className="itinerary-board">
          {trip.itineraryDays.map((day) => (
            <div key={day.id} className="itinerary-day card">
              <h2>
                Day {day.dayIndex} — {day.date}
              </h2>
              {day.tripDestinations.length === 0 ? (
                <p className="state-message state-message--empty">
                  No destinations scheduled for this day yet.
                </p>
              ) : (
                <ul className="itinerary-destinations">
                  {day.tripDestinations.map((destination) => (
                    <li key={destination.id} className="itinerary-destination">
                      <span>{destination.name}</span>
                      <button
                        type="button"
                        className="btn-danger"
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
      )}

      {removeMutation.isError && (
        <p className="error" role="alert">
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
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="set-dates-form">
      <label>
        Start date
        <input
          type="date"
          {...register('startDate')}
          aria-invalid={Boolean(errors.startDate)}
          aria-describedby={errors.startDate ? 'set-dates-startDate-error' : undefined}
        />
        {errors.startDate && (
          <p className="error" id="set-dates-startDate-error">
            {errors.startDate.message}
          </p>
        )}
      </label>
      <label>
        End date
        <input
          type="date"
          {...register('endDate')}
          aria-invalid={Boolean(errors.endDate)}
          aria-describedby={errors.endDate ? 'set-dates-endDate-error' : undefined}
        />
        {errors.endDate && (
          <p className="error" id="set-dates-endDate-error">
            {errors.endDate.message}
          </p>
        )}
      </label>
      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}
      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving…' : trip.startDate ? 'Update dates' : 'Set dates'}
      </button>
    </form>
  );
}
