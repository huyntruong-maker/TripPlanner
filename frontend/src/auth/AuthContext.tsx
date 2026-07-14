import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import * as authApi from '../api/auth';
import {
  clearTokens,
  getStoredTokens,
  registerSessionExpiredHandler,
  storeTokens,
} from '../api/client';
import { publishErrorToast } from '../components/toast/toastBus';
import { decodeUserFromToken } from './jwt';

const SESSION_EXPIRED_MESSAGE = 'Your session has expired. Please log in again.';
import type { AuthenticatedUser, AuthTokens } from '../types';

interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string, rememberMe?: boolean) => Promise<void>;
  register: (email: string, password: string, firstName: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/** Persists tokens in localStorage to survive refresh; no `/me` endpoint, so identity is decoded from the JWT. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [tokens, setTokens] = useState<AuthTokens | null>(() => getStoredTokens());
  const queryClient = useQueryClient();

  // Clears local session and query cache on refresh-token failure (NFR-6).
  useEffect(() => {
    registerSessionExpiredHandler(() => {
      setTokens(null);
      queryClient.clear();
      publishErrorToast(SESSION_EXPIRED_MESSAGE);
    });
  }, [queryClient]);

  const user = useMemo(() => (tokens ? decodeUserFromToken(tokens.token) : null), [tokens]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: tokens !== null,
      async login(email, password, rememberMe = false) {
        const nextTokens = await authApi.login({ username: email, password, rememberMe });
        storeTokens(nextTokens);
        setTokens(nextTokens);
      },
      async register(email, password, firstName) {
        // Doesn't log the user in — account must be verified by email first.
        await authApi.register({ email, password, firstName });
      },
      logout() {
        const activeTokens = tokens;
        clearTokens();
        setTokens(null);
        // Drop cached queries so another user on this tab can't see them (NFR-6).
        queryClient.clear();
        if (activeTokens) {
          void authApi.logout(activeTokens).catch(() => {
            // Best-effort server-side session end; local state is already cleared.
          });
        }
      },
    }),
    [tokens, user, queryClient],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
