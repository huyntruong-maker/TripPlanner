import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { ApiEnvelope, AuthTokens } from '../types';

const TOKEN_STORAGE_KEY = 'tripplanner.token';
const REFRESH_TOKEN_STORAGE_KEY = 'tripplanner.refreshToken';

/**
 * A single configured axios instance used by the whole app.
 * A request interceptor attaches the JWT (when present); a response
 * interceptor transparently refreshes an expired access token once via
 * PUT /auth/refresh (docs/API.md) and retries the original request.
 */
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080/api/v1',
  headers: { 'Content-Type': 'application/json' },
});

export function storeTokens(tokens: AuthTokens): void {
  localStorage.setItem(TOKEN_STORAGE_KEY, tokens.token);
  localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, tokens.refreshToken);
}

export function clearTokens(): void {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
  localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
}

export function getStoredTokens(): AuthTokens | null {
  const token = getToken();
  const refreshToken = getRefreshToken();
  return token && refreshToken ? { token, refreshToken } : null;
}

/** Registered by AuthProvider so the refresh-failure path can clear session state. */
let onSessionExpired: (() => void) | null = null;

export function registerSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler;
}

apiClient.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

interface RetriableRequestConfig extends InternalAxiosRequestConfig {
  _retriedAfterRefresh?: boolean;
}

/** Marks an error as already handled by the session-expiry flow, so the
 * global query-error toast (src/queryClient.ts) doesn't also show the raw
 * 401/400 for it — the caller shows one friendly "session expired" toast instead. */
export interface SessionExpiredError {
  isSessionExpired?: boolean;
}

let refreshInFlight: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const currentTokens = getStoredTokens();
  if (!currentTokens) {
    return null;
  }

  try {
    const { data } = await axios.put<ApiEnvelope<AuthTokens>>(
      `${apiClient.defaults.baseURL}/auth/refresh`,
      currentTokens,
    );
    storeTokens(data.result);
    return data.result.token;
  } catch {
    return null;
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined;
    const isAuthEndpoint = originalRequest?.url?.includes('/auth/');

    const shouldAttemptRefresh =
      error.response?.status === 401 &&
      originalRequest !== undefined &&
      !originalRequest._retriedAfterRefresh &&
      !isAuthEndpoint;

    if (!shouldAttemptRefresh) {
      return Promise.reject(error);
    }

    originalRequest._retriedAfterRefresh = true;
    refreshInFlight ??= refreshAccessToken().finally(() => {
      refreshInFlight = null;
    });
    const refreshedToken = await refreshInFlight;

    if (!refreshedToken) {
      clearTokens();
      onSessionExpired?.();
      (error as AxiosError & SessionExpiredError).isSessionExpired = true;
      return Promise.reject(error);
    }

    originalRequest.headers.set('Authorization', `Bearer ${refreshedToken}`);
    return apiClient(originalRequest);
  },
);
