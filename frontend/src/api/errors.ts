import axios from 'axios';
import type { ApiEnvelope } from '../types';
import { apiErrorMessages } from './errorMessages';

/** Reads a message from the backend's custom error envelope (not RFC 7807); falls back when the shape is unexpected. */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  const body = error.response?.data as Partial<ApiEnvelope<unknown>> | undefined;
  if (!body) {
    return fallback;
  }

  if (body.errorCode && apiErrorMessages[body.errorCode]) {
    return apiErrorMessages[body.errorCode];
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
