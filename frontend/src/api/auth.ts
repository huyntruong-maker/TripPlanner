import { apiClient } from './client';
import type { ApiEnvelope, AuthTokens } from '../types';

// Auth endpoints; base path is baked into apiClient's baseURL.

export interface RegisterPayload {
  email: string;
  password: string;
  firstName: string;
}

/** POST /auth/register — 201 on success; the API sends a verification email. */
export async function register(payload: RegisterPayload): Promise<void> {
  await apiClient.post<ApiEnvelope<null>>('/auth/register', payload);
}

/** GET /auth/verify-email?token= — activates the account. */
export async function verifyEmail(token: string): Promise<void> {
  await apiClient.get<ApiEnvelope<null>>('/auth/verify-email', { params: { token } });
}

export interface LoginPayload {
  username: string;
  password: string;
  rememberMe: boolean;
}

/** POST /auth/login — returns `{ token, refreshToken }`, no user object. */
export async function login(payload: LoginPayload): Promise<AuthTokens> {
  const { data } = await apiClient.post<ApiEnvelope<AuthTokens>>('/auth/login', payload);
  return data.result;
}

/** PUT /auth/logout — called after local tokens are cleared, so pass the token explicitly (the interceptor would find nothing to attach). */
export async function logout(tokens: AuthTokens): Promise<void> {
  await apiClient.put<ApiEnvelope<null>>('/auth/logout', tokens, {
    headers: { Authorization: `Bearer ${tokens.token}` },
  });
}
