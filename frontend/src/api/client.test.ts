import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { server } from '../test/msw/server';
import { buildFakeJwt } from '../test/buildFakeJwt';
import {
  apiClient,
  getRefreshToken,
  getToken,
  registerSessionExpiredHandler,
  storeTokens,
} from './client';

const BASE_URL = 'http://localhost:5080/api/v1';

function accessToken() {
  return buildFakeJwt({ nameid: 'user-1', unique_name: 'jane@example.com' });
}

describe('apiClient response interceptor — silent token refresh', () => {
  beforeEach(() => {
    localStorage.clear();
    registerSessionExpiredHandler(() => {
      // no-op default; individual tests override when they need to observe it
    });
  });

  it('retries the original request once after a successful silent refresh', async () => {
    storeTokens({ token: accessToken(), refreshToken: 'old-refresh-token' });

    let attempts = 0;
    server.use(
      http.get(`${BASE_URL}/trips`, () => {
        attempts += 1;
        if (attempts === 1) {
          return HttpResponse.json(
            { success: false, errorCode: null, error: 'Unauthorized', validates: [] },
            { status: 401 },
          );
        }
        return HttpResponse.json({ success: true, errorCode: null, error: null, validates: [], result: [] });
      }),
      http.put(`${BASE_URL}/auth/refresh`, () =>
        HttpResponse.json({
          success: true,
          errorCode: null,
          error: null,
          validates: [],
          result: { token: accessToken(), refreshToken: 'new-refresh-token' },
        }),
      ),
    );

    const response = await apiClient.get('/trips');

    expect(response.status).toBe(200);
    expect(attempts).toBe(2);
    expect(getRefreshToken()).toBe('new-refresh-token');
  });

  it('does not retry forever — a second 401 after a successful refresh still fails', async () => {
    storeTokens({ token: accessToken(), refreshToken: 'old-refresh-token' });

    server.use(
      http.get(`${BASE_URL}/trips`, () =>
        HttpResponse.json(
          { success: false, errorCode: null, error: 'Unauthorized', validates: [] },
          { status: 401 },
        ),
      ),
      http.put(`${BASE_URL}/auth/refresh`, () =>
        HttpResponse.json({
          success: true,
          errorCode: null,
          error: null,
          validates: [],
          result: { token: accessToken(), refreshToken: 'new-refresh-token' },
        }),
      ),
    );

    await expect(apiClient.get('/trips')).rejects.toMatchObject({
      response: { status: 401 },
    });
  });

  it('clears the session when the refresh token itself is invalid/expired', async () => {
    storeTokens({ token: accessToken(), refreshToken: 'expired-refresh-token' });

    let sessionExpired = false;
    registerSessionExpiredHandler(() => {
      sessionExpired = true;
    });

    server.use(
      http.get(`${BASE_URL}/trips`, () =>
        HttpResponse.json(
          { success: false, errorCode: null, error: 'Unauthorized', validates: [] },
          { status: 401 },
        ),
      ),
      // No override for PUT /auth/refresh — the default handler always fails, matching an expired refresh token.
    );

    await expect(apiClient.get('/trips')).rejects.toBeTruthy();

    expect(sessionExpired).toBe(true);
    expect(getToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
  });

  it('does not attempt a refresh for a 401 from an auth endpoint itself', async () => {
    storeTokens({ token: accessToken(), refreshToken: 'old-refresh-token' });

    let refreshCalls = 0;
    server.use(
      http.put(`${BASE_URL}/auth/logout`, () =>
        HttpResponse.json(
          { success: false, errorCode: null, error: 'Unauthorized', validates: [] },
          { status: 401 },
        ),
      ),
      http.put(`${BASE_URL}/auth/refresh`, () => {
        refreshCalls += 1;
        return HttpResponse.json(
          { success: false, errorCode: 'Auth.RefreshToken.Failed', error: 'Refresh token invalid.', validates: [] },
          { status: 401 },
        );
      }),
    );

    await expect(apiClient.put('/auth/logout', {})).rejects.toMatchObject({
      response: { status: 401 },
    });

    expect(refreshCalls).toBe(0);
  });
});
