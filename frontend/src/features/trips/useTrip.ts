import { useQuery } from '@tanstack/react-query';
import { getTrip } from '../../api/trips';

export function tripQueryKey(tripId: string) {
  return ['trips', tripId] as const;
}

/** Shared mutationKey prefix for every trip-board mutation (remove, move); scopes `useIsMutating` for the F3-US9 saving indicator. */
export function tripMutationScopeKey(tripId: string) {
  return ['trips', tripId, 'mutation'] as const;
}

/** Full trip detail (itinerary days + destinations), loaded on demand. */
export function useTrip(tripId: string | undefined) {
  return useQuery({
    queryKey: tripQueryKey(tripId ?? ''),
    queryFn: () => getTrip(tripId as string),
    enabled: Boolean(tripId),
  });
}
