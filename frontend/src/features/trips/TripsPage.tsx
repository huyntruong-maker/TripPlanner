import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { getApiErrorMessage } from '../../api/errors';
import { createTrip } from '../../api/trips';
import { useToast } from '../../components/toast/ToastProvider';
import { TRIPS_QUERY_KEY, useTrips } from './useTrips';
import { createTripSchema, type CreateTripFormValues } from './schemas';

/** F3/US1 — create a trip, F3/US10 — list saved trips with an empty state. */
export function TripsPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);
  const tripsQuery = useTrips();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateTripFormValues>({
    resolver: zodResolver(createTripSchema),
    defaultValues: { name: '' },
  });

  async function onSubmit(values: CreateTripFormValues) {
    setFormError(null);
    try {
      const trip = await createTrip(values);
      await queryClient.invalidateQueries({ queryKey: TRIPS_QUERY_KEY });
      reset();
      navigate(`/trips/${trip.id}`);
    } catch (err) {
      const message = getApiErrorMessage(err, 'Could not create trip.');
      setFormError(message);
      showToast(message);
    }
  }

  return (
    <div className="trips-page">
      <div className="card">
        <header className="row">
          <h1>My trips</h1>
          <button type="button" onClick={logout}>
            Log out
          </button>
        </header>
        <p>
          Signed in as <strong>{user?.email}</strong>.
        </p>
      </div>

      <div className="card">
        <h2>Create a trip</h2>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <label>
            Trip name
            <input
              {...register('name')}
              aria-invalid={Boolean(errors.name)}
              aria-describedby={errors.name ? 'create-trip-name-error' : undefined}
            />
            {errors.name && (
              <p className="error" id="create-trip-name-error">
                {errors.name.message}
              </p>
            )}
          </label>
          {formError && (
            <p className="error" role="alert">
              {formError}
            </p>
          )}
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creating…' : 'Create trip'}
          </button>
        </form>
      </div>

      <div className="card">
        <h2>Your trips</h2>

        {tripsQuery.isLoading && <p>Loading trips…</p>}

        {tripsQuery.isError && (
          <p className="error" role="alert">
            {getApiErrorMessage(tripsQuery.error, 'Could not load your trips.')}
          </p>
        )}

        {tripsQuery.data && tripsQuery.data.length === 0 && (
          <p>You don&apos;t have any trips yet. Create one above to get started.</p>
        )}

        {tripsQuery.data && tripsQuery.data.length > 0 && (
          <ul className="trip-list">
            {tripsQuery.data.map((trip) => (
              <li key={trip.id}>
                <Link to={`/trips/${trip.id}`}>{trip.name}</Link>
                {trip.startDate && trip.endDate && (
                  <span className="trip-dates">
                    {trip.startDate} – {trip.endDate}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
