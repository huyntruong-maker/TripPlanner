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
  });

  describe('logged in — modal dialog (trip + day only, no Saved Places option)', () => {
    beforeEach(() => {
      signInForTest();
    });

    it('opens a labelled, modal dialog when "Add to Trip" is clicked', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));

      const dialog = await screen.findByRole('dialog', { name: 'Add to trip' });
      expect(dialog).toHaveAttribute('aria-modal', 'true');
    });

    it('never offers a "Saved Places" day option — trip + day selection only', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await user.selectOptions(await screen.findByLabelText('Trip'), EXISTING_TRIP_ID);

      expect(screen.queryByRole('option', { name: /Saved Places/ })).not.toBeInTheDocument();
    });

    it('adds the destination to a chosen trip and day', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await user.selectOptions(await screen.findByLabelText('Trip'), EXISTING_TRIP_ID);
      await user.selectOptions(await screen.findByLabelText('Day'), 'day-2');
      await user.click(screen.getByRole('button', { name: 'Add' }));

      expect(await screen.findByRole('status')).toHaveTextContent('Added to Paris 2026.');
      // The dialog closes once the destination is added.
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('blocks adding (no day step to skip to) when the chosen trip has no itinerary days yet', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await user.selectOptions(await screen.findByLabelText('Trip'), TRIP_WITHOUT_DATES_ID);

      expect(await screen.findByText(/This trip has no dates yet/)).toBeInTheDocument();
      expect(screen.queryByLabelText('Day')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Add' })).toBeDisabled();
    });

    it('shows a message when the user has no trips yet', async () => {
      clearTripsFixture();
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));

      expect(await screen.findByText(/You don't have any trips yet/)).toBeInTheDocument();
    });

    it('closes on Escape and returns focus to the trigger button', async () => {
      const user = userEvent.setup();
      renderControl();

      const trigger = screen.getByRole('button', { name: 'Add to Trip' });
      await user.click(trigger);
      await screen.findByRole('dialog');

      await user.keyboard('{Escape}');

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      expect(trigger).toHaveFocus();
    });

    it('closes when the backdrop (outside the dialog content) is clicked', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      const dialog = await screen.findByRole('dialog');

      // The backdrop is the dialog's own positioning parent — click it directly, not the content.
      await user.click(dialog.parentElement as HTMLElement);

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('closes via the dialog\'s own close (X) button', async () => {
      const user = userEvent.setup();
      renderControl();

      await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
      await screen.findByRole('dialog');

      await user.click(screen.getByRole('button', { name: 'Close dialog' }));

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
