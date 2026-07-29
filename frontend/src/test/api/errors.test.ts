import { describe, expect, it } from 'vitest';
import { AxiosError } from 'axios';
import { getApiErrorMessage } from '../../api/errors';

function apiError(data: unknown, status = 400): AxiosError {
  return new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
    data,
    status,
    statusText: '',
    headers: {},
    config: {} as never,
  });
}

describe('getApiErrorMessage', () => {
  it('returns the mapped message for a known errorCode', () => {
    const error = apiError({ success: false, errorCode: 'Trip.NotFound', error: null }, 404);

    expect(getApiErrorMessage(error, 'fallback')).toBe(
      'This trip no longer exists or you do not have access to it.',
    );
  });

  it('prefers the mapped message over the envelope error text', () => {
    const error = apiError({
      success: false,
      errorCode: 'Trip.AddDestination.ItineraryDayNotFound',
      error: 'server text',
    });

    expect(getApiErrorMessage(error, 'fallback')).toBe(
      'That itinerary day is not part of this trip.',
    );
  });

  it('falls back to the envelope error text for an unmapped errorCode', () => {
    const error = apiError({ success: false, errorCode: 'Trip.Unknown', error: 'server text' });

    expect(getApiErrorMessage(error, 'fallback')).toBe('server text');
  });

  it('returns the fallback when the error is not an axios error', () => {
    expect(getApiErrorMessage(new Error('boom'), 'fallback')).toBe('fallback');
  });
});
