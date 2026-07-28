import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { getTrip } from '../../api/trips';
import { ToastProvider } from '../../components/toast/ToastProvider';
import type { Trip } from '../../types';
import {
  MOVE_EXCEPTION_TRIGGER_ID,
  PLANNER_DAY_1_ID,
  PLANNER_DAY_2_ID,
  PLANNER_DUPLICATE_SAVED_PLACE_ID,
  PLANNER_SAVED_PLACE_ID,
  PLANNER_TRIP_ID,
  RETRY_SUCCEEDS_DESTINATION_ID,
} from '../../test/msw/handlers/trips';
import { tripQueryKey } from './useTrip';
import { useMoveTripDestination } from './useMoveTripDestination';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  function wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <ToastProvider>{children}</ToastProvider>
      </QueryClientProvider>
    );
  }
  return { queryClient, wrapper };
}

// loads the real fixture trip from the MSW-backed API, so this test can't drift from the fixture
async function seedTripInCache(queryClient: QueryClient) {
  const trip = await getTrip(PLANNER_TRIP_ID);
  queryClient.setQueryData(tripQueryKey(PLANNER_TRIP_ID), trip);
  return trip;
}

describe('useMoveTripDestination', () => {
  it('optimistically updates the cache, then replaces it with the server response on success', async () => {
    const { queryClient, wrapper } = createWrapper();
    const trip = await seedTripInCache(queryClient);

    const { result } = renderHook(() => useMoveTripDestination(PLANNER_TRIP_ID), { wrapper });

    act(() => {
      result.current.mutate({
        tripDestinationId: PLANNER_SAVED_PLACE_ID,
        itineraryDayId: PLANNER_DAY_2_ID,
        position: 1,
      });
    });

    // applied before the (artificially delayed) server responds — this is the onMutate projection
    await waitFor(() => {
      const currentTrip = queryClient.getQueryData<Trip>(tripQueryKey(PLANNER_TRIP_ID));
      const day2 = currentTrip?.itineraryDays.find((day) => day.id === PLANNER_DAY_2_ID);
      expect(day2?.tripDestinations[0]?.id).toBe(PLANNER_SAVED_PLACE_ID);
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    const finalTrip = queryClient.getQueryData<Trip>(tripQueryKey(PLANNER_TRIP_ID));
    expect(finalTrip).toEqual(result.current.data);
    const finalDay2 = finalTrip?.itineraryDays.find((day) => day.id === PLANNER_DAY_2_ID);
    expect(finalDay2?.tripDestinations.map((item) => item.id)).toEqual([PLANNER_SAVED_PLACE_ID]);
    expect(trip.id).toBe(PLANNER_TRIP_ID);
  });

  it('rolls back the optimistic update and toasts a friendly message on Trip.MoveDestination.DuplicateInDay', async () => {
    const { queryClient, wrapper } = createWrapper();
    const trip = await seedTripInCache(queryClient);

    const { result } = renderHook(() => useMoveTripDestination(PLANNER_TRIP_ID), { wrapper });

    act(() => {
      // Day 1 already has a destination with the same providerPlaceId as this saved "duplicate source"
      result.current.mutate({
        tripDestinationId: PLANNER_DUPLICATE_SAVED_PLACE_ID,
        itineraryDayId: PLANNER_DAY_1_ID,
        position: 1,
      });
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    const rolledBackTrip = queryClient.getQueryData<Trip>(tripQueryKey(PLANNER_TRIP_ID));
    expect(rolledBackTrip).toEqual(trip);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This destination is already in that day.',
    );
  });

  it('retries the same mutation with the same variables from the toast Retry action, succeeding the second time', async () => {
    const { queryClient, wrapper } = createWrapper();
    await seedTripInCache(queryClient);

    const { result } = renderHook(() => useMoveTripDestination(PLANNER_TRIP_ID), { wrapper });
    const user = userEvent.setup();

    act(() => {
      result.current.mutate({
        tripDestinationId: RETRY_SUCCEEDS_DESTINATION_ID,
        itineraryDayId: PLANNER_DAY_2_ID,
        position: 1,
      });
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    await user.click(await screen.findByRole('button', { name: 'Retry' }));

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    const finalTrip = queryClient.getQueryData<Trip>(tripQueryKey(PLANNER_TRIP_ID));
    const finalDay2 = finalTrip?.itineraryDays.find((day) => day.id === PLANNER_DAY_2_ID);
    expect(finalDay2?.tripDestinations.map((item) => item.id)).toEqual([RETRY_SUCCEEDS_DESTINATION_ID]);
  });

  it('shows the generic fallback message and still rolls back for an unmapped exception code', async () => {
    const { queryClient, wrapper } = createWrapper();
    const trip = await seedTripInCache(queryClient);

    const { result } = renderHook(() => useMoveTripDestination(PLANNER_TRIP_ID), { wrapper });

    act(() => {
      result.current.mutate({
        tripDestinationId: MOVE_EXCEPTION_TRIGGER_ID,
        itineraryDayId: PLANNER_DAY_2_ID,
        position: 1,
      });
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    const rolledBackTrip = queryClient.getQueryData<Trip>(tripQueryKey(PLANNER_TRIP_ID));
    expect(rolledBackTrip).toEqual(trip);
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not move this destination.');
  });
});
