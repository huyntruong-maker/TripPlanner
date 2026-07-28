import { useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiErrorMessage } from '../../api/errors';
import { moveTripDestination } from '../../api/trips';
import { useToast } from '../../components/toast/ToastProvider';
import type { Trip } from '../../types';
import { moveDestinationInTrip } from './moveDestination';
import { tripMutationScopeKey, tripQueryKey } from './useTrip';

export interface MoveTripDestinationVariables {
  tripDestinationId: string;
  /** `null` moves the destination to Saved Places. */
  itineraryDayId: string | null;
  /** 1-based; `null` appends at the end of the target list. */
  position: number | null;
}

interface MoveMutationContext {
  previousTrip: Trip | undefined;
}

const MOVE_FALLBACK_MESSAGE = 'Could not move this destination. Please try again.';

/** Optimistically updates the cache in `onMutate` (NFR-4), rolls back on failure, and shows its own error toast with Retry via `meta.suppressGlobalToast`. */
export function useMoveTripDestination(tripId: string) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const mutation = useMutation<Trip, unknown, MoveTripDestinationVariables, MoveMutationContext>({
    mutationKey: tripMutationScopeKey(tripId),
    meta: { suppressGlobalToast: true },
    mutationFn: (variables) =>
      moveTripDestination(tripId, variables.tripDestinationId, {
        itineraryDayId: variables.itineraryDayId,
        position: variables.position,
      }),
    onMutate: async (variables) => {
      const queryKey = tripQueryKey(tripId);
      await queryClient.cancelQueries({ queryKey });
      const previousTrip = queryClient.getQueryData<Trip>(queryKey);

      if (previousTrip) {
        queryClient.setQueryData(
          queryKey,
          moveDestinationInTrip(previousTrip, variables.tripDestinationId, {
            itineraryDayId: variables.itineraryDayId,
            position: variables.position,
          }),
        );
      }

      return { previousTrip };
    },
    onError: (error, variables, context) => {
      if (context?.previousTrip) {
        queryClient.setQueryData(tripQueryKey(tripId), context.previousTrip);
      }
      const message = getApiErrorMessage(error, MOVE_FALLBACK_MESSAGE);
      showToast(message, {
        tone: 'error',
        action: { label: 'Retry', onAction: () => mutation.mutate(variables) },
      });
    },
    onSuccess: (trip) => {
      queryClient.setQueryData(tripQueryKey(tripId), trip);
    },
  });

  return mutation;
}
