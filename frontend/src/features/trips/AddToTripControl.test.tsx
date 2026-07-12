import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import {
  EXISTING_TRIP_ID,
  TRIP_WITHOUT_DATES_ID,
  clearTripsFixture,
} from '../../test/msw/handlers/trips';
import { signInForTest } from '../../test/tripsTestRoutes';
import { AddToTripControl, type AddableDestination } from './AddToTripControl';
import { consumePendingAddToTrip, rememberPendingAddToTrip } from './pendingAddToTrip';

const SAMPLE_DESTINATION: AddableDestination = {
  providerPlaceId: 'W214242',
  name: 'Eiffel Tower',
  category: 'cultural',
  thumbnailUrl: 'https://example.test/eiffel.jpg',
  lat: 48.8584,
  lng: 2.2945,
};

function renderControl(initialEntry = '/destinations/W214242') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <AuthProvider>
          <AddToTripControl destination={SAMPLE_DESTINATION} />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AddToTripControl', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  describe('logged out (F3-US8 AC2 — disabled + redirect-back)', () => {
    it('shows a disabled button and a login link that returns to the current page', () => {
      renderControl('/destinations/W214242');

      expect(screen.getByRole('button', { name: 'Add to Trip' })).toBeDisabled();
      expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
        'href',
        '/login?returnTo=%2Fdestinations%2FW214242',
      );
    });

    it('remembers this destination as the pending add-to-trip intent when the login link is clicked (F3-US8 AC5)', async () => {
      const user = userEvent.setup();
      renderControl('/destinations/W214242');

      await user.click(screen.getByRole('link', { name: 'Log in' }));

      expect(consumePendingAddToTrip(SAMPLE_DESTINATION.providerPlaceId)).toBe(true);
    });
  });

  describe('logged in', () => {
    beforeEach(() => {
      signInForTest();
    });

    it('adds the destination to a chosen trip and day', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await user.selectOptions(await screen.findByLabelText('Trip'), EXISTING_TRIP_ID);
      await user.selectOptions(await screen.findByLabelText('Day'), 'day-2');
      await user.click(screen.getByRole('button', { name: 'Add' }));

      expect(await screen.findByRole('status')).toHaveTextContent('Added to Paris 2026.');
    });

    it('prompts to set dates first when the chosen trip has no itinerary days', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await user.selectOptions(await screen.findByLabelText('Trip'), TRIP_WITHOUT_DATES_ID);

      expect(await screen.findByText(/This trip has no dates yet/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Add' })).toBeDisabled();
    });

    it('shows a message when the user has no trips yet', async () => {
      clearTripsFixture();
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));

      expect(await screen.findByText(/You don't have any trips yet/)).toBeInTheDocument();
    });

    describe('resuming after login (F3-US8 AC5 — best-effort)', () => {
      it('auto-opens and focuses the trip picker for the destination that had a pending intent', async () => {
        rememberPendingAddToTrip(SAMPLE_DESTINATION.providerPlaceId);

        renderControl();

        const tripSelect = await screen.findByLabelText('Trip');
        expect(tripSelect).toHaveFocus();
        expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
      });

      it('clears the pending intent so it does not resume again on a later render', async () => {
        rememberPendingAddToTrip(SAMPLE_DESTINATION.providerPlaceId);

        renderControl();

        await screen.findByLabelText('Trip');
        expect(consumePendingAddToTrip(SAMPLE_DESTINATION.providerPlaceId)).toBe(false);
      });

      it('does not resume when the pending intent is for a different destination', () => {
        rememberPendingAddToTrip('a-different-place-id');

        renderControl();

        expect(screen.getByRole('button', { name: 'Add to Trip' })).toBeInTheDocument();
        expect(screen.queryByLabelText('Trip')).not.toBeInTheDocument();
      });
    });
  });
});
