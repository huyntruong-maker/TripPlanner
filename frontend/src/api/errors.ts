import axios from 'axios';
import type { ApiEnvelope } from '../types';

/**
 * Extracts a user-facing message from the backend's custom envelope
 * (`{ success, errorCode, error, validates }` — docs/API.md), not RFC 7807
 * ProblemDetails. Falls back to a generic message when the shape is unexpected.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  const body = error.response?.data as Partial<ApiEnvelope<unknown>> | undefined;
  if (!body) {
    return fallback;
  }

  if (body.error) {
    return body.error;
  }

  const [firstValidationIssue] = body.validates ?? [];
  if (typeof firstValidationIssue === 'string') {
    return firstValidationIssue;
  }
  if (
    firstValidationIssue &&
    typeof firstValidationIssue === 'object' &&
    'message' in firstValidationIssue &&
    typeof (firstValidationIssue as { message?: unknown }).message === 'string'
  ) {
    return (firstValidationIssue as { message: string }).message;
  }

  return fallback;
}

/** Extracts the backend's `errorCode` (docs/API.md), e.g. `Auth.VerifyEmail.AlreadyVerified`. */
export function getApiErrorCode(error: unknown): string | null {
  if (!axios.isAxiosError(error)) {
    return null;
  }

  const body = error.response?.data as Partial<ApiEnvelope<unknown>> | undefined;
  return body?.errorCode ?? null;
}
