import { QueryClientProvider, useQuery } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from './test/msw/server';
import { getTrips } from './api/trips';
import { ToastProvider } from './components/toast/ToastProvider';
import { createAppQueryClient } from './queryClient';

const BASE_URL = 'http://localhost:5080/api/v1';

/** A bare query with zero component-level error handling — proves the toast
 * comes from the QueryClient's global wiring, not a per-component catch. */
function TripsListProbe() {
  useQuery({ queryKey: ['trips'], queryFn: getTrips, retry: false });
  return <p>probe rendered</p>;
}

describe('createAppQueryClient — global error toast wiring', () => {
  it('shows a toast automatically when a query fails, with no component-level error handling', async () => {
    server.use(
      http.get(`${BASE_URL}/trips`, () =>
        HttpResponse.json(
          {
            success: false,
            errorCode: 'Trip.GetTrips.Exception',
            error: 'Could not load your trips.',
            validates: [],
          },
          { status: 500 },
        ),
      ),
    );

    const queryClient = createAppQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <TripsListProbe />
        </ToastProvider>
      </QueryClientProvider>,
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load your trips.');
  });

  it('falls back to a generic message when the backend response has no error text', async () => {
    server.use(http.get(`${BASE_URL}/trips`, () => new HttpResponse(null, { status: 500 })));

    const queryClient = createAppQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <TripsListProbe />
        </ToastProvider>
      </QueryClientProvider>,
    );

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Something went wrong. Please try again.',
    );
  });
});
