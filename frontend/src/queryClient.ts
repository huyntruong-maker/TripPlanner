import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import type { SessionExpiredError } from './api/client';
import { getApiErrorMessage } from './api/errors';
import { publishErrorToast } from './components/toast/toastBus';

const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';

function reportError(error: unknown): void {
  // AuthContext already shows a dedicated "session expired" toast for these
  // (see registerSessionExpiredHandler) — showing the raw 401/400 too would
  // be a confusing second toast for the same event.
  if ((error as SessionExpiredError)?.isSessionExpired) return;
  publishErrorToast(getApiErrorMessage(error, GENERIC_ERROR_MESSAGE));
}

/**
 * The single QueryClient for the whole app. Every query/mutation failure
 * (search, attractions, destination detail, trips, add/remove destination,
 * etc.) is automatically surfaced as a global error toast, on top of any
 * page-level inline error message — see components/toast/ToastProvider.tsx.
 *
 * A handful of call sites (login, register, create-trip, set-dates) call the
 * API directly instead of through useMutation, so they aren't covered by
 * this global wiring; those call `useToast().showToast(...)` explicitly in
 * their catch blocks instead.
 */
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
