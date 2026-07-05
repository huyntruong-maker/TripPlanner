import { useQuery } from '@tanstack/react-query';
import { getTrips } from '../../api/trips';

export const TRIPS_QUERY_KEY = ['trips'] as const;

/** F3/US1, US10 — the signed-in user's trip list. */
export function useTrips() {
  return useQuery({ queryKey: TRIPS_QUERY_KEY, queryFn: getTrips });
}
