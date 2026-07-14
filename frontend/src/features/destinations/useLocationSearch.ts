import { useQuery } from '@tanstack/react-query';
import { searchLocations } from '../../api/destinations';

const MIN_QUERY_LENGTH = 1;
const LOCATIONS_STALE_TIME_MS = 60_000;

/** Requires at least 1 character before querying. */
export function useLocationSearch(query: string) {
  const trimmedQuery = query.trim();

  return useQuery({
    queryKey: ['destinations', 'locations', trimmedQuery],
    queryFn: () => searchLocations({ query: trimmedQuery }),
    enabled: trimmedQuery.length >= MIN_QUERY_LENGTH,
    staleTime: LOCATIONS_STALE_TIME_MS,
  });
}
