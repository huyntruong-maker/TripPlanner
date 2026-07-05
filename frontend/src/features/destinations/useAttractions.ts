import { useQuery } from '@tanstack/react-query';
import { getAttractions } from '../../api/destinations';

export interface Coordinates {
  latitude: number;
  longitude: number;
}

const ATTRACTIONS_STALE_TIME_MS = 30_000;

/** F1/US3 — attractions near a selected location; `null` coords means "no location chosen yet". */
export function useAttractions(coordinates: Coordinates | null) {
  return useQuery({
    queryKey: ['destinations', 'attractions', coordinates?.latitude, coordinates?.longitude],
    queryFn: () => getAttractions(coordinates as Coordinates),
    enabled: coordinates !== null,
    staleTime: ATTRACTIONS_STALE_TIME_MS,
  });
}
