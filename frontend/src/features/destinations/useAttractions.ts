import { useQuery } from '@tanstack/react-query';
import { getAttractions } from '../../api/destinations';

export interface Coordinates {
  latitude: number;
  longitude: number;
}

// Generous windows so browsing detail pages and coming Back re-shows the same list instantly
// (and keeps showing it even if a background refetch fails); the backend's own Redis cache is
// the source of freshness, so a stale-ish client copy is fine.
const ATTRACTIONS_STALE_TIME_MS = 5 * 60_000;
const ATTRACTIONS_GC_TIME_MS = 30 * 60_000;

/** `null` coords means "no location chosen yet". */
export function useAttractions(coordinates: Coordinates | null) {
  return useQuery({
    queryKey: ['destinations', 'attractions', coordinates?.latitude, coordinates?.longitude],
    queryFn: () => getAttractions(coordinates as Coordinates),
    enabled: coordinates !== null,
    staleTime: ATTRACTIONS_STALE_TIME_MS,
    gcTime: ATTRACTIONS_GC_TIME_MS,
  });
}
