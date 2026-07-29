import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it } from 'vitest';
import { VALID_CREDENTIALS } from '../msw/handlers/auth';
import { buildFakeJwt } from '../buildFakeJwt';
import { AuthProvider, useAuth } from '../../auth/AuthContext';

function createWrapper() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  function wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    );
  }

  return { wrapper, queryClient };
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('logs in, decodes the user from the token, and persists the session', async () => {
    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper().wrapper });

    await act(async () => {
      await result.current.login(VALID_CREDENTIALS.email, VALID_CREDENTIALS.password);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.email).toBe(VALID_CREDENTIALS.email);
    expect(localStorage.getItem('tripplanner.token')).not.toBeNull();
    expect(localStorage.getItem('tripplanner.refreshToken')).not.toBeNull();
  });

  it('rejects invalid credentials and does not create a session', async () => {
    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper().wrapper });

    await expect(
      act(async () => {
        await result.current.login('jane@example.com', 'wrong-password');
      }),
    ).rejects.toBeTruthy();

    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem('tripplanner.token')).toBeNull();
  });

  it('stays signed in across a simulated page reload (tokens rehydrated from localStorage)', () => {
    const token = buildFakeJwt({ nameid: 'user-1', unique_name: VALID_CREDENTIALS.email });
    localStorage.setItem('tripplanner.token', token);
    localStorage.setItem('tripplanner.refreshToken', 'refresh-1');

    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper().wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.email).toBe(VALID_CREDENTIALS.email);
  });

  it('logs out, clears the persisted session, and clears the query cache (NFR-6)', async () => {
    const token = buildFakeJwt({ nameid: 'user-1', unique_name: VALID_CREDENTIALS.email });
    localStorage.setItem('tripplanner.token', token);
    localStorage.setItem('tripplanner.refreshToken', 'refresh-1');

    const { wrapper, queryClient } = createWrapper();
    queryClient.setQueryData(['trips'], [{ id: 'trip-1', name: "Jane's Paris Trip" }]);

    const { result } = renderHook(() => useAuth(), { wrapper });
    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      result.current.logout();
    });

    await waitFor(() => expect(result.current.isAuthenticated).toBe(false));
    expect(localStorage.getItem('tripplanner.token')).toBeNull();
    expect(localStorage.getItem('tripplanner.refreshToken')).toBeNull();
    expect(queryClient.getQueryData(['trips'])).toBeUndefined();
  });
});
