import { useQuery } from '@tanstack/react-query';
import { getTrip } from '../../api/trips';

export function tripQueryKey(tripId: string) {
  return ['trips', tripId] as const;
}

/** Full trip detail (itinerary days + destinations), loaded on demand. */
export function useTrip(tripId: string | undefined) {
  return useQuery({
    queryKey: tripQueryKey(tripId ?? ''),
    queryFn: () => getTrip(tripId as string),
    enabled: Boolean(tripId),
  });
}
