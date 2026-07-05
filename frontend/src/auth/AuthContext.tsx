import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import * as authApi from '../api/auth';
import {
  clearTokens,
  getStoredTokens,
  registerSessionExpiredHandler,
  storeTokens,
} from '../api/client';
import { decodeUserFromToken } from './jwt';
import type { AuthenticatedUser, AuthTokens } from '../types';

interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string, rememberMe?: boolean) => Promise<void>;
  register: (email: string, password: string, firstName: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Holds the signed-in user's tokens in localStorage so a page refresh keeps
 * the session (Feature 4 / US3: "Stay signed in after refreshing the page").
 * There is no `/me` endpoint, so the display identity is decoded from the
 * access token's own claims (see auth/jwt.ts).
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [tokens, setTokens] = useState<AuthTokens | null>(() => getStoredTokens());
  const queryClient = useQueryClient();

  // If a background token refresh ultimately fails (refresh token itself
  // expired/invalid), the api client calls this to drop the local session.
  // Also clear the query cache — otherwise a different user signing in on the
  // same tab could briefly see the previous user's cached data (NFR-6).
  useEffect(() => {
    registerSessionExpiredHandler(() => {
      setTokens(null);
      queryClient.clear();
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
        // Registration does not log the user in — the account must be
        // verified by email first (backend returns Auth.Login.InActive
        // otherwise). Callers should show a "check your email" state.
        await authApi.register({ email, password, firstName });
      },
      logout() {
        const activeTokens = tokens;
        clearTokens();
        setTokens(null);
        // Drop every cached query — a different user signing in on the same
        // tab must never see this user's cached trips/destinations (NFR-6).
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
