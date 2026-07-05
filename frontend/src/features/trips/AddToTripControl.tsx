import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { buildLoginUrl } from '../../auth/returnTo';
import { getApiErrorMessage } from '../../api/errors';
import { addTripDestination, getTrip } from '../../api/trips';
import { useTrips } from './useTrips';
import { tripQueryKey } from './useTrip';
import { addToTripSchema, type AddToTripFormValues } from './schemas';

export interface AddableDestination {
  providerPlaceId: string;
  name: string;
  category: string | null;
  thumbnailUrl: string | null;
  lat: number;
  lng: number;
}

interface AddToTripControlProps {
  destination: AddableDestination;
}

/**
 * F3/US3, US8 — "Add to Trip" action reused by the results grid and the
 * destination detail page. Disabled (not just hidden) when logged out, with
 * a login link that returns here afterward (F3-US8 AC2, best-effort).
 */
export function AddToTripControl({ destination }: AddToTripControlProps) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  const [isOpen, setIsOpen] = useState(false);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  if (!isAuthenticated) {
    const loginUrl = buildLoginUrl(`${location.pathname}${location.search}`);
    return (
      <div className="add-to-trip">
        <button type="button" disabled title="Log in to add this to a trip">
          Add to Trip
        </button>
        <p className="hint">
          <Link to={loginUrl}>Log in</Link> to add this to a trip.
        </p>
      </div>
    );
  }

  return (
    <div className="add-to-trip">
      <button type="button" onClick={() => setIsOpen((open) => !open)} aria-expanded={isOpen}>
        {isOpen ? 'Cancel' : 'Add to Trip'}
      </button>
      {confirmation && !isOpen && (
        <p className="hint" role="status">
          {confirmation}
        </p>
      )}
      {isOpen && (
        <AddToTripForm
          destination={destination}
          onAdded={(tripName) => {
            setConfirmation(`Added to ${tripName}.`);
            setIsOpen(false);
          }}
        />
      )}
    </div>
  );
}

interface AddToTripFormProps {
  destination: AddableDestination;
  onAdded: (tripName: string) => void;
}

function AddToTripForm({ destination, onAdded }: AddToTripFormProps) {
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);
  const tripsQuery = useTrips();

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<AddToTripFormValues>({
    resolver: zodResolver(addToTripSchema),
    defaultValues: { tripId: '', itineraryDayId: '' },
  });

  const selectedTripId = watch('tripId');

  const selectedTripQuery = useQuery({
    queryKey: tripQueryKey(selectedTripId),
    queryFn: () => getTrip(selectedTripId),
    enabled: Boolean(selectedTripId),
  });

  const addMutation = useMutation({
    mutationFn: (values: AddToTripFormValues) =>
      addTripDestination(values.tripId, {
        itineraryDayId: values.itineraryDayId,
        providerPlaceId: destination.providerPlaceId,
        name: destination.name,
        category: destination.category,
        thumbnailUrl: destination.thumbnailUrl,
        lat: destination.lat,
        lng: destination.lng,
      }),
  });

  async function onSubmit(values: AddToTripFormValues) {
    setFormError(null);
    try {
      await addMutation.mutateAsync(values);
      await queryClient.invalidateQueries({ queryKey: tripQueryKey(values.tripId) });
      const tripName = tripsQuery.data?.find((trip) => trip.id === values.tripId)?.name ?? 'your trip';
      onAdded(tripName);
    } catch (err) {
      setFormError(getApiErrorMessage(err, 'Could not add this destination.'));
    }
  }

  if (tripsQuery.isLoading) {
    return <p className="state-message state-message--loading">Loading your trips…</p>;
  }

  if (tripsQuery.isError) {
    return (
      <p className="error" role="alert">
        {getApiErrorMessage(tripsQuery.error, 'Could not load your trips.')}
      </p>
    );
  }

  if (!tripsQuery.data || tripsQuery.data.length === 0) {
    return (
      <p>
        You don&apos;t have any trips yet. <Link to="/trips">Create one</Link> first.
      </p>
    );
  }

  const availableDays = selectedTripQuery.data?.itineraryDays ?? [];
  const hasSelectedTripWithNoDays =
    Boolean(selectedTripId) && selectedTripQuery.isSuccess && availableDays.length === 0;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="add-to-trip-form">
      <label>
        Trip
        <select
          {...register('tripId')}
          aria-invalid={Boolean(errors.tripId)}
          aria-describedby={errors.tripId ? 'add-to-trip-tripId-error' : undefined}
        >
          <option value="">Choose a trip…</option>
          {tripsQuery.data.map((trip) => (
            <option key={trip.id} value={trip.id}>
              {trip.name}
            </option>
          ))}
        </select>
        {errors.tripId && (
          <p className="error" id="add-to-trip-tripId-error">
            {errors.tripId.message}
          </p>
        )}
      </label>

      {selectedTripId && selectedTripQuery.isLoading && (
        <p className="state-message state-message--loading">Loading days…</p>
      )}

      {hasSelectedTripWithNoDays && (
        <p>
          This trip has no dates yet. <Link to={`/trips/${selectedTripId}`}>Set dates</Link> first.
        </p>
      )}

      {availableDays.length > 0 && (
        <label>
          Day
          <select
            {...register('itineraryDayId')}
            aria-invalid={Boolean(errors.itineraryDayId)}
            aria-describedby={errors.itineraryDayId ? 'add-to-trip-itineraryDayId-error' : undefined}
          >
            <option value="">Choose a day…</option>
            {availableDays.map((day) => (
              <option key={day.id} value={day.id}>
                Day {day.dayIndex} — {day.date}
              </option>
            ))}
          </select>
          {errors.itineraryDayId && (
            <p className="error" id="add-to-trip-itineraryDayId-error">
              {errors.itineraryDayId.message}
            </p>
          )}
        </label>
      )}

      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}

      <button type="submit" disabled={isSubmitting || availableDays.length === 0}>
        {isSubmitting ? 'Adding…' : 'Add'}
      </button>
    </form>
  );
}
