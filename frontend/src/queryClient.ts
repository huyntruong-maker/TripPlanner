import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import type { SessionExpiredError } from './api/client';
import { getApiErrorMessage } from './api/errors';
import { publishErrorToast } from './components/toast/toastBus';

const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';

function reportError(error: unknown): void {
  // AuthContext already shows a dedicated session-expired toast for these.
  if ((error as SessionExpiredError)?.isSessionExpired) return;
  publishErrorToast(getApiErrorMessage(error, GENERIC_ERROR_MESSAGE));
}

/** Global QueryClient — surfaces query/mutation failures as toasts; login, register, create-trip, and set-dates call the API directly and toast manually instead. */
export function createAppQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: 1,
        refetchOnWindowFocus: false,
      },
    },
    queryCache: new QueryCache({ onError: reportError }),
    mutationCache: new MutationCache({ onError: reportError }),
  });
}
