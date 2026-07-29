import { useQuery } from '@tanstack/react-query';
import { getTrip } from '../../../api/trips';

export function tripQueryKey(tripId: string) {
  return ['trips', tripId] as const;
}

/** Shared mutationKey prefix for every trip-board mutation (remove, move); scopes the `useIsMutating` saving indicator. */
export function tripMutationScopeKey(tripId: string) {
  return ['trips', tripId, 'mutation'] as const;
}

export function useTrip(tripId: string | undefined) {
  return useQuery({
    queryKey: tripQueryKey(tripId ?? ''),
    queryFn: () => getTrip(tripId as string),
    enabled: Boolean(tripId),
  });
}
