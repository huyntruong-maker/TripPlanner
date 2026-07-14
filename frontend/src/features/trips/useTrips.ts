import { useQuery } from '@tanstack/react-query';
import { getTrips } from '../../api/trips';

export const TRIPS_QUERY_KEY = ['trips'] as const;

export function useTrips() {
  return useQuery({ queryKey: TRIPS_QUERY_KEY, queryFn: getTrips });
}
