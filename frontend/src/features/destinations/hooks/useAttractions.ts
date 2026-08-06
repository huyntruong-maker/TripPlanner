import { useQuery } from '@tanstack/react-query';
import { getAttractions } from '../../../api/destinations';

export interface Coordinates {
  latitude: number;
  longitude: number;
}

/** Small enough that FoursquareEnrichedDestinationProvider's per-item enrichment reliably finishes without hitting Foursquare's rate limit. */
export const ATTRACTIONS_PAGE_SIZE = 9;

// Generous windows so Back re-shows the same list instantly; the backend's own Redis cache is the source of freshness.
const ATTRACTIONS_STALE_TIME_MS = 5 * 60_000;
const ATTRACTIONS_GC_TIME_MS = 30 * 60_000;

/** `null` coords means "no location chosen yet". */
export function useAttractions(coordinates: Coordinates | null, page: number) {
  return useQuery({
    queryKey: ['destinations', 'attractions', coordinates?.latitude, coordinates?.longitude, page],
    queryFn: () =>
      getAttractions({ ...(coordinates as Coordinates), page, pageSize: ATTRACTIONS_PAGE_SIZE }),
    enabled: coordinates !== null,
    staleTime: ATTRACTIONS_STALE_TIME_MS,
    gcTime: ATTRACTIONS_GC_TIME_MS,
  });
}
