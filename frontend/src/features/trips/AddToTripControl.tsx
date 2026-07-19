import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { buildLoginUrl } from '../../auth/returnTo';
import { getApiErrorMessage } from '../../api/errors';
import { addTripDestination, getTrip } from '../../api/trips';
import { publishToast } from '../../components/toast/toastBus';
import { useTrips } from './useTrips';
import { tripQueryKey } from './useTrip';
import { addToTripSchema, type AddToTripFormValues } from './schemas';

const INPUT_CLASSES =
  'w-full border border-outline-variant rounded-lg px-4 py-3 text-body-md focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none';
const TOGGLE_BUTTON_BASE_CLASSES =
  'inline-flex items-center gap-2 px-6 py-2.5 rounded-full font-label-md transition-all';
/** Circular icon-only trigger (the card "quick-save" affordance); visibility/position come from `className`. */
const ICON_TRIGGER_BASE_CLASSES =
  'inline-flex items-center justify-center w-9 h-9 rounded-full bg-on-surface/70 text-on-primary hover:bg-on-surface/90 transition-all duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2';
/** Hidden until the card is hovered/keyboard-focused; always shown on small (touch-first) breakpoints as a fallback affordance. */
const ICON_TRIGGER_HIDDEN_CLASSES =
  'opacity-0 group-hover:opacity-100 focus-visible:opacity-100 max-sm:opacity-100';
/** Select value representing "Saved Places (schedule later)"; converted to `itineraryDayId: null` on submit. */
export const SAVED_PLACES_OPTION_VALUE = 'saved-places';

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
  /**
   * `button` (default): the full-width "Add to Trip" pill with an inline confirmation message —
   * used on the destination detail page. `icon`: a compact circular trigger meant to be
   * overlaid on an `AttractionCard`'s image (revealed on hover/focus, always shown on small
   * breakpoints); confirms via the global toast instead of inline text so it doesn't need
   * extra card real estate. Both variants drive the exact same add-to-trip form/mutation.
   */
  variant?: 'button' | 'icon';
  /** Positions the icon-variant trigger (e.g. `"absolute top-3 left-3 z-20"`); ignored for `variant="button"`. */
  className?: string;
}

/** "Add to Trip" action, reused across pages; disabled (not hidden) when logged out, with a login link that returns here after. */
export function AddToTripControl({ destination, variant = 'button', className }: AddToTripControlProps) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  const [isOpen, setIsOpen] = useState(false);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  const loginUrl = buildLoginUrl(`${location.pathname}${location.search}`);

  if (!isAuthenticated) {
    if (variant === 'icon') {
      return (
        <Link
          to={loginUrl}
          aria-label="Save to trip"
          title="Log in to save this to a trip"
          className={`${ICON_TRIGGER_BASE_CLASSES} ${ICON_TRIGGER_HIDDEN_CLASSES} ${className ?? ''}`}
        >
          <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
            bookmark_border
          </span>
        </Link>
      );
    }

    return (
      <div className="space-y-2">
        <button
          type="button"
          disabled
          title="Log in to add this to a trip"
          className={`${TOGGLE_BUTTON_BASE_CLASSES} bg-surface-container text-outline cursor-not-allowed`}
        >
          <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
            add_circle
          </span>
          Add to Trip
        </button>
        <p className="text-label-sm font-label-sm text-on-surface-variant">
          <Link to={loginUrl} className="text-primary font-semibold hover:underline">
            Log in
          </Link>{' '}
          to add this to a trip.
        </p>
      </div>
    );
  }

  if (variant === 'icon') {
    return (
      <div className={`relative ${className ?? ''}`}>
        <button
          type="button"
          onClick={() => setIsOpen((open) => !open)}
          aria-expanded={isOpen}
          aria-label={isOpen ? 'Close save to trip' : 'Save to trip'}
          className={`${ICON_TRIGGER_BASE_CLASSES} ${isOpen ? 'opacity-100' : ICON_TRIGGER_HIDDEN_CLASSES}`}
        >
          <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
            {isOpen ? 'close' : 'bookmark_border'}
          </span>
        </button>
        {isOpen && (
          <div className="absolute z-20 top-full left-0 mt-2 w-72 max-w-[85vw]">
            <AddToTripForm
              destination={destination}
              onAdded={(tripName) => {
                publishToast(`Added to ${tripName}.`);
                setIsOpen(false);
              }}
            />
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-expanded={isOpen}
        className={
          isOpen
            ? `${TOGGLE_BUTTON_BASE_CLASSES} border border-outline-variant text-on-surface-variant hover:bg-surface-container`
            : `${TOGGLE_BUTTON_BASE_CLASSES} bg-primary text-on-primary hover:opacity-90 active:scale-95`
        }
      >
        <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
          {isOpen ? 'close' : 'add_circle'}
        </span>
        {isOpen ? 'Cancel' : 'Add to Trip'}
      </button>
      {confirmation && !isOpen && (
        <p className="flex items-center gap-2 text-label-md font-label-md text-primary" role="status">
          <span className="material-symbols-outlined text-[18px]" aria-hidden="true">
            check_circle
          </span>
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
        itineraryDayId:
          values.itineraryDayId === SAVED_PLACES_OPTION_VALUE ? null : values.itineraryDayId,
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
    return <p className="text-on-surface-variant text-body-md">Loading your trips…</p>;
  }

  if (tripsQuery.isError) {
    return (
      <p className="text-error text-label-sm font-semibold" role="alert">
        {getApiErrorMessage(tripsQuery.error, 'Could not load your trips.')}
      </p>
    );
  }

  if (!tripsQuery.data || tripsQuery.data.length === 0) {
    return (
      <p className="text-body-md text-on-surface-variant">
        You don&apos;t have any trips yet.{' '}
        <Link to="/trips" className="text-primary font-semibold hover:underline">
          Create one
        </Link>{' '}
        first.
      </p>
    );
  }

  const availableDays = selectedTripQuery.data?.itineraryDays ?? [];

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      noValidate
      className="space-y-4 bg-surface-container-low rounded-lg p-4 border border-outline-variant/30"
    >
      <div className="space-y-2">
        <label
          htmlFor="add-to-trip-tripId"
          className="block text-label-md font-label-md text-on-surface-variant"
        >
          Trip
        </label>
        <select
          id="add-to-trip-tripId"
          className={INPUT_CLASSES}
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
          <p className="text-error text-label-sm font-semibold" id="add-to-trip-tripId-error">
            {errors.tripId.message}
          </p>
        )}
      </div>

      {selectedTripId && selectedTripQuery.isLoading && (
        <p className="text-on-surface-variant text-body-md">Loading days…</p>
      )}

      {selectedTripId && selectedTripQuery.isSuccess && (
        <div className="space-y-2">
          <label
            htmlFor="add-to-trip-itineraryDayId"
            className="block text-label-md font-label-md text-on-surface-variant"
          >
            Day
          </label>
          <select
            id="add-to-trip-itineraryDayId"
            className={INPUT_CLASSES}
            {...register('itineraryDayId')}
            aria-invalid={Boolean(errors.itineraryDayId)}
            aria-describedby={errors.itineraryDayId ? 'add-to-trip-itineraryDayId-error' : undefined}
          >
            <option value="">Choose where to add it…</option>
            <option value={SAVED_PLACES_OPTION_VALUE}>Saved Places (schedule later)</option>
            {availableDays.map((day) => (
              <option key={day.id} value={day.id}>
                Day {day.dayIndex} — {day.date}
              </option>
            ))}
          </select>
          {errors.itineraryDayId && (
            <p
              className="text-error text-label-sm font-semibold"
              id="add-to-trip-itineraryDayId-error"
            >
              {errors.itineraryDayId.message}
            </p>
          )}
          {availableDays.length === 0 && (
            <p className="text-body-md text-on-surface-variant">
              This trip has no dates yet. Choose Saved Places to add it now, or{' '}
              <Link
                to={`/trips/${selectedTripId}`}
                className="text-primary font-semibold hover:underline"
              >
                set dates
              </Link>{' '}
              to schedule a specific day.
            </p>
          )}
        </div>
      )}

      {formError && (
        <p className="text-error text-label-sm font-semibold" role="alert">
          {formError}
        </p>
      )}

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full bg-primary text-on-primary py-3 rounded-lg font-label-md hover:opacity-90 active:scale-[0.98] transition-all disabled:opacity-60 disabled:cursor-not-allowed"
      >
        {isSubmitting ? 'Adding…' : 'Add'}
      </button>
    </form>
  );
}
