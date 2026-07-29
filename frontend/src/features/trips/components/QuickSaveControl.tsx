import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../../auth/AuthContext';
import { buildLoginUrl } from '../../../auth/returnTo';
import { getApiErrorMessage } from '../../../api/errors';
import { addTripDestination } from '../../../api/trips';
import { publishToast } from '../../../components/toast/toastBus';
import type { Trip } from '../../../types';
import type { AddableDestination } from './AddToTripControl';
import { useTrips } from '../hooks/useTrips';
import { tripQueryKey } from '../hooks/useTrip';

const ICON_TRIGGER_BASE_CLASSES =
  'inline-flex items-center justify-center w-9 h-9 rounded-full bg-on-surface/70 text-on-primary hover:bg-on-surface/90 transition-all duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2';
// hidden until hover/keyboard-focus; always shown on touch-first breakpoints as a fallback affordance
const ICON_TRIGGER_HIDDEN_CLASSES =
  'opacity-0 group-hover:opacity-100 focus-visible:opacity-100 max-sm:opacity-100';

interface QuickSaveControlProps {
  destination: AddableDestination;
  className?: string;
}

// picks only a trip (no day step), saves straight to Saved Places; use AddToTripControl for the day picker
export function QuickSaveControl({ destination, className }: QuickSaveControlProps) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  const [isOpen, setIsOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  if (!isAuthenticated) {
    const loginUrl = buildLoginUrl(`${location.pathname}${location.search}`);
    return (
      <Link
        to={loginUrl}
        aria-label="Save place"
        title="Log in to save this place"
        className={`${ICON_TRIGGER_BASE_CLASSES} ${ICON_TRIGGER_HIDDEN_CLASSES} ${className ?? ''}`}
      >
        <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
          bookmark_border
        </span>
      </Link>
    );
  }

  function close() {
    setIsOpen(false);
    triggerRef.current?.focus();
  }

  return (
    <div className={`relative ${className ?? ''}`}>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-expanded={isOpen}
        aria-label={isOpen ? 'Close save place' : 'Save place'}
        className={`${ICON_TRIGGER_BASE_CLASSES} ${isOpen ? 'opacity-100' : ICON_TRIGGER_HIDDEN_CLASSES}`}
      >
        <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
          {isOpen ? 'close' : 'bookmark_border'}
        </span>
      </button>
      {isOpen && <QuickSavePopover destination={destination} onClose={close} />}
    </div>
  );
}

interface QuickSavePopoverProps {
  destination: AddableDestination;
  onClose: () => void;
}

function QuickSavePopover({ destination, onClose }: QuickSavePopoverProps) {
  const queryClient = useQueryClient();
  const tripsQuery = useTrips();
  const firstTripButtonRef = useRef<HTMLButtonElement>(null);

  // move focus into the list as soon as it's available (keyboard accessibility)
  useEffect(() => {
    firstTripButtonRef.current?.focus();
  }, [tripsQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (tripId: string) =>
      addTripDestination(tripId, {
        itineraryDayId: null,
        providerPlaceId: destination.providerPlaceId,
        name: destination.name,
        category: destination.category,
        thumbnailUrl: destination.thumbnailUrl,
        lat: destination.lat,
        lng: destination.lng,
      }),
  });

  async function handleSave(trip: Trip) {
    try {
      await saveMutation.mutateAsync(trip.id);
      await queryClient.invalidateQueries({ queryKey: tripQueryKey(trip.id) });
      publishToast(`Saved to ${trip.name}.`, { tone: 'success' });
      onClose();
    } catch {
      // The global mutation-error toast (queryClient.ts) already surfaced this failure.
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      onClose();
    }
  }

  return (
    <div
      onKeyDown={handleKeyDown}
      className="absolute z-20 top-full left-0 mt-2 w-64 max-w-[85vw] bg-surface-container-lowest rounded-lg elevation-l1 border border-outline-variant/30 p-3 space-y-2"
    >
      {tripsQuery.isLoading && (
        <p className="text-on-surface-variant text-body-md px-1 py-1">Loading your trips…</p>
      )}

      {tripsQuery.isError && (
        <p className="text-error text-label-sm font-semibold px-1" role="alert">
          {getApiErrorMessage(tripsQuery.error, 'Could not load your trips.')}
        </p>
      )}

      {tripsQuery.data && tripsQuery.data.length === 0 && (
        <p className="text-body-md text-on-surface-variant px-1">
          You don&apos;t have any trips yet.{' '}
          <Link to="/trips" className="text-primary font-semibold hover:underline">
            Create one
          </Link>{' '}
          first.
        </p>
      )}

      {tripsQuery.data && tripsQuery.data.length > 0 && (
        <ul className="space-y-1">
          {tripsQuery.data.map((trip, index) => (
            <li key={trip.id}>
              <button
                ref={index === 0 ? firstTripButtonRef : undefined}
                type="button"
                onClick={() => handleSave(trip)}
                disabled={saveMutation.isPending}
                className="w-full text-left px-3 py-2 rounded-lg text-body-md text-on-surface hover:bg-surface-container transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {trip.name}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
