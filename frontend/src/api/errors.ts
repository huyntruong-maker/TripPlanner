import axios from 'axios';
import type { ApiEnvelope } from '../types';

/** User-facing text per backend errorCode (docs/API.md); codes not listed fall through to the caller's fallback. */
const errorCodeMessages: Record<string, string> = {
  'Trip.NotFound': 'This trip no longer exists or you do not have access to it.',
  'Trip.AddDestination.ItineraryDayNotFound': 'That itinerary day is not part of this trip.',
  'Trip.SetDates.InvalidDateRange': 'The start date must be on or before the end date.',
};

/** Reads a message from the backend's custom error envelope (not RFC 7807); falls back when the shape is unexpected. */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  const body = error.response?.data as Partial<ApiEnvelope<unknown>> | undefined;
  if (!body) {
    return fallback;
  }

  if (body.errorCode && errorCodeMessages[body.errorCode]) {
    return errorCodeMessages[body.errorCode];
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
