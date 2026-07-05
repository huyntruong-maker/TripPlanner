import { http, HttpResponse } from 'msw';
import { buildFakeJwt } from '../../buildFakeJwt';

// Matches apiClient's default baseURL (src/api/client.ts) when
// VITE_API_BASE_URL is unset, which is how tests run.
const BASE_URL = 'http://localhost:5080/api/v1';

export const EXISTING_EMAIL = 'taken@example.com';
export const VALID_CREDENTIALS = { email: 'jane@example.com', password: 'Secret123!' };
export const VALID_VERIFY_TOKEN = 'valid-verify-token';
export const ALREADY_VERIFIED_TOKEN = 'already-verified-token';

function issueTokensFor(email: string) {
  return {
    token: buildFakeJwt({ nameid: 'b3b1f5b0-1111-4a2b-9c3d-abcdef123456', unique_name: email }),
    refreshToken: 'refresh-token-for-tests',
  };
}

export const authHandlers = [
  http.post(`${BASE_URL}/auth/register`, async ({ request }) => {
    const body = (await request.json()) as { email?: string };

    if (body.email === EXISTING_EMAIL) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Auth.Register.EmailTaken',
          error: 'An account with this email already exists.',
          validates: [],
        },
        { status: 400 },
      );
    }

    return HttpResponse.json(
      { success: true, errorCode: null, error: null, validates: [], result: null },
      { status: 201 },
    );
  }),

  http.post(`${BASE_URL}/auth/login`, async ({ request }) => {
    const body = (await request.json()) as { username?: string; password?: string };

    if (body.username === VALID_CREDENTIALS.email && body.password === VALID_CREDENTIALS.password) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: issueTokensFor(body.username),
      });
    }

    return HttpResponse.json(
      {
        success: false,
        errorCode: 'Auth.Login.InvalidCredential',
        error: 'Invalid email or password.',
        validates: [],
      },
      { status: 400 },
    );
  }),

  http.put(`${BASE_URL}/auth/logout`, () =>
    HttpResponse.json({ success: true, errorCode: null, error: null, validates: [], result: null }),
  ),

  http.put(`${BASE_URL}/auth/refresh`, () =>
    HttpResponse.json(
      { success: false, errorCode: 'Auth.RefreshToken.Failed', error: 'Refresh token invalid.', validates: [] },
      { status: 401 },
    ),
  ),

  http.get(`${BASE_URL}/auth/verify-email`, ({ request }) => {
    const token = new URL(request.url).searchParams.get('token');

    if (token === VALID_VERIFY_TOKEN) {
      return HttpResponse.json({ success: true, errorCode: null, error: null, validates: [], result: null });
    }

    if (token === ALREADY_VERIFIED_TOKEN) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Auth.VerifyEmail.AlreadyVerified',
          error: 'This email is already verified.',
          validates: [],
        },
        { status: 400 },
      );
    }

    return HttpResponse.json(
      {
        success: false,
        errorCode: 'Auth.VerifyEmail.TokenInvalid',
        error: 'This verification link is invalid.',
        validates: [],
      },
      { status: 400 },
    );
  }),
];
