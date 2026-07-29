import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../api/errors';
import { createTrip } from '../../api/trips';
import { useToast } from '../../components/toast/ToastProvider';
import { TRIPS_QUERY_KEY, useTrips } from './useTrips';
import { createTripSchema, type CreateTripFormValues } from './schemas';

const INPUT_CLASSES =
  'w-full border border-outline-variant rounded-lg px-4 py-3 text-body-md focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none';

export function TripsPage() {
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

  const tripCount = tripsQuery.data?.length ?? 0;

  return (
    <div className="max-w-[800px] mx-auto flex flex-col gap-8">
      <h1 className="text-headline-md font-headline-md text-on-surface">My trips</h1>

      <section className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 border border-outline-variant/30">
        <div className="flex items-center gap-3 mb-8">
          <div className="p-2 bg-surface-container rounded-lg">
            <span className="material-symbols-outlined text-primary" aria-hidden="true">
              add_location_alt
            </span>
          </div>
          <h2 className="text-headline-md font-headline-md text-on-surface">Create a trip</h2>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-6">
          <div className="space-y-2">
            <label
              htmlFor="create-trip-name"
              className="block text-label-md font-label-md text-on-surface-variant"
            >
              Trip name
            </label>
            <input
              id="create-trip-name"
              className={INPUT_CLASSES}
              placeholder="e.g., Summer in Japan 2026"
              {...register('name')}
              aria-invalid={Boolean(errors.name)}
              aria-describedby={errors.name ? 'create-trip-name-error' : undefined}
            />
            {errors.name && (
              <p className="text-error text-label-sm font-semibold" id="create-trip-name-error">
                {errors.name.message}
              </p>
            )}
          </div>
          {formError && (
            <p className="error text-error text-label-sm font-semibold" role="alert">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-primary text-on-primary py-4 rounded-full font-label-md hover:opacity-90 active:scale-[0.98] transition-all disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            <span className="material-symbols-outlined" aria-hidden="true">
              rocket_launch
            </span>
            {isSubmitting ? 'Creating…' : 'Create trip'}
          </button>
        </form>
      </section>

      <section className="flex flex-col gap-6">
        <div className="flex justify-between items-end px-2">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-surface-container rounded-lg text-primary">
              <span className="material-symbols-outlined" aria-hidden="true">
                map
              </span>
            </div>
            <h2 className="text-headline-md font-headline-md text-on-surface">Your trips</h2>
          </div>
          {tripsQuery.data && (
            <span className="text-label-sm font-label-sm text-outline uppercase tracking-wider">
              {tripCount} {tripCount === 1 ? 'trip' : 'trips'} found
            </span>
          )}
        </div>

        {tripsQuery.isLoading && (
          <p className="text-on-surface-variant text-body-md">Loading trips…</p>
        )}

        {tripsQuery.isError && (
          <p className="text-error text-label-sm font-semibold" role="alert">
            {getApiErrorMessage(tripsQuery.error, 'Could not load your trips.')}
          </p>
        )}

        {tripsQuery.data && tripsQuery.data.length === 0 && (
          <div className="border-2 border-dashed border-outline-variant/40 rounded-2xl p-12 text-center text-on-surface-variant">
            <span className="material-symbols-outlined text-4xl mb-4 block" aria-hidden="true">
              explore
            </span>
            <p className="text-body-md">
              You don&apos;t have any trips yet. Create one above to get started.
            </p>
          </div>
        )}

        {tripsQuery.data && tripsQuery.data.length > 0 && (
          <ul className="flex flex-col gap-4">
            {tripsQuery.data.map((trip) => (
              <li key={trip.id}>
                <Link
                  to={`/trips/${trip.id}`}
                  aria-label={trip.name}
                  className="group block bg-surface-container-lowest rounded-xl elevation-l1 hover:elevation-l2 border border-outline-variant/20 transition-all p-6"
                >
                  <div className="flex items-center gap-6">
                    <div
                      className="hidden sm:flex w-14 h-14 flex-shrink-0 items-center justify-center rounded-lg bg-surface-container text-primary"
                      aria-hidden="true"
                    >
                      <span className="material-symbols-outlined text-2xl">map</span>
                    </div>
                    <div className="flex-grow flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
                      <div>
                        <h3 className="text-headline-md font-headline-md text-on-surface group-hover:text-primary transition-colors">
                          {trip.name}
                        </h3>
                        {trip.startDate && trip.endDate && (
                          <div className="flex items-center gap-2 mt-1 text-on-surface-variant font-body-md">
                            <span className="material-symbols-outlined text-sm" aria-hidden="true">
                              calendar_month
                            </span>
                            <span className="trip-dates">
                              {trip.startDate} – {trip.endDate}
                            </span>
                          </div>
                        )}
                      </div>
                      <span className="text-label-md font-label-md text-primary flex items-center gap-1 flex-shrink-0">
                        View details
                        <span className="material-symbols-outlined text-sm" aria-hidden="true">
                          arrow_forward_ios
                        </span>
                      </span>
                    </div>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
