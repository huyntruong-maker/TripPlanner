import { QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../../../auth/AuthContext';
import { ToastProvider } from '../../../components/toast/ToastProvider';
import { createAppQueryClient } from '../../../queryClient';
import { server } from '../../msw/server';
import { EXISTING_TRIP_ID, clearTripsFixture } from '../../msw/handlers/trips';
import { signInForTest } from '../../tripsTestRoutes';
import type { AttractionSummary } from '../../../types';
import { AttractionCard } from '../../../features/destinations/components/AttractionCard';

const BASE_URL = 'http://localhost:5080/api/v1';

function renderCard(attraction: AttractionSummary) {
  // real app QueryClient: its mutationCache wires failures to the global error toast, which QuickSaveControl relies on
  const queryClient = createAppQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter>
          <AuthProvider>
            <ul>
              <AttractionCard attraction={attraction} discoverSearch="?q=Test&lat=1&lng=2" />
            </ul>
          </AuthProvider>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

const BASE_ATTRACTION: AttractionSummary = {
  providerPlaceId: 'W1',
  name: 'Grand Museum',
  category: 'other_buildings_and_structures',
  tags: ['other_buildings_and_structures'],
  rating: null,
  thumbnailUrl: null,
  latitude: 1,
  longitude: 1,
  address: null,
};

describe('AttractionCard — visual design (design pass)', () => {
  it('humanizes the category eyebrow instead of showing the raw provider slug', () => {
    renderCard(BASE_ATTRACTION);

    expect(screen.getByText('Other buildings and structures')).toBeInTheDocument();
    expect(screen.queryByText('other_buildings_and_structures')).not.toBeInTheDocument();
  });

  it('shows a rating badge with humanized "★ rating" text when a rating is present', () => {
    renderCard({ ...BASE_ATTRACTION, rating: 8.7 });

    const badge = screen.getByText('★ 8.7');
    expect(badge).toBeInTheDocument();
    expect(badge.closest('p')).toHaveAttribute('aria-label', 'Rating 8.7 out of 10');
  });

  it('shows no rating badge when the rating is missing', () => {
    renderCard(BASE_ATTRACTION);

    expect(screen.queryByText(/★/)).not.toBeInTheDocument();
  });

  it('shows a placeholder with an icon and "No photo" text when there is no thumbnail', () => {
    renderCard(BASE_ATTRACTION);

    expect(screen.getByText('No photo')).toBeInTheDocument();
    expect(document.querySelector('img.attraction-thumbnail')).not.toBeInTheDocument();
  });

  it('humanizes tag chips and shows at most 3, collapsing the rest into a "+N" chip', () => {
    renderCard({
      ...BASE_ATTRACTION,
      category: 'museum',
      tags: ['museum', 'Art_galleries', 'historic_site', 'gift_shop', 'guided_tours'],
    });

    expect(screen.getByText('Art galleries')).toBeInTheDocument();
    expect(screen.getByText('Historic site')).toBeInTheDocument();
    expect(screen.getByText('Gift shop')).toBeInTheDocument();
    expect(screen.queryByText('Guided tours')).not.toBeInTheDocument();
    expect(screen.getByText('+1')).toBeInTheDocument();
  });

  it('does not show a tag list at all when there are no additional tags beyond the category', () => {
    renderCard(BASE_ATTRACTION);

    expect(screen.queryByText(/^\+\d+$/)).not.toBeInTheDocument();
  });
});

describe('AttractionCard — "Add to Trip" footer control (trip + day picker)', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('is disabled with a login hint when logged out', () => {
    renderCard(BASE_ATTRACTION);

    expect(screen.getByRole('button', { name: 'Add to Trip' })).toBeDisabled();
    expect(screen.getByRole('link', { name: 'Log in' })).toBeInTheDocument();
  });

  it('opens the trip + day picker form and adds the destination to a specific day', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.click(screen.getByRole('button', { name: 'Add to Trip' }));
    await user.selectOptions(await screen.findByLabelText('Trip'), EXISTING_TRIP_ID);
    await user.selectOptions(await screen.findByLabelText('Day'), 'day-2');
    await user.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Added to Paris 2026.');
  });
});

describe('AttractionCard — "Save place" hover/focus icon (trip-only quick save)', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('is a distinct action from the footer Add to Trip control', () => {
    signInForTest();
    renderCard(BASE_ATTRACTION);

    expect(screen.getByRole('button', { name: 'Save place' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add to Trip' })).toBeInTheDocument();
  });

  it('shows a login-linked icon when logged out, separate from the destination link', () => {
    renderCard(BASE_ATTRACTION);

    const saveLink = screen.getByRole('link', { name: 'Save place' });
    expect(saveLink.tagName).toBe('A');
    expect(saveLink).toHaveAttribute('href', expect.stringContaining('/login'));

    const destinationLink = screen.getByRole('link', { name: /Grand Museum/ });
    expect(destinationLink).not.toBe(saveLink);
  });

  it('is keyboard-focusable (visible on focus, not just hover) and not nested in the destination link', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.tab();
    const saveButton = screen.getByRole('button', { name: 'Save place' });
    expect(saveButton).toHaveFocus();
    expect(saveButton.closest('a')).toBeNull();
  });

  it('opens a popover listing only trips (no day step) and saves straight to Saved Places', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    const saveButton = screen.getByRole('button', { name: 'Save place' });
    expect(saveButton).toHaveAttribute('aria-expanded', 'false');

    await user.click(saveButton);

    expect(screen.getByRole('button', { name: 'Close save place' })).toHaveAttribute(
      'aria-expanded',
      'true',
    );
    expect(screen.queryByLabelText('Day')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Trip')).not.toBeInTheDocument();

    const tripButton = await screen.findByRole('button', { name: 'Paris 2026' });
    await user.click(tripButton);

    expect(await screen.findByRole('status')).toHaveTextContent('Saved to Paris 2026.');
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Paris 2026' })).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Save place' })).toHaveFocus();
  });

  it('moves focus into the trip list when the popover opens (keyboard accessibility)', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.click(screen.getByRole('button', { name: 'Save place' }));

    const firstTripButton = await screen.findByRole('button', { name: 'Paris 2026' });
    await waitFor(() => expect(firstTripButton).toHaveFocus());
  });

  it('closes on Escape and returns focus to the trigger', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    const saveButton = screen.getByRole('button', { name: 'Save place' });
    await user.click(saveButton);
    await screen.findByRole('button', { name: 'Paris 2026' });

    await user.keyboard('{Escape}');

    expect(screen.queryByRole('button', { name: 'Paris 2026' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save place' })).toHaveFocus();
  });

  it('shows an empty state with a link to create a trip when the user has none yet', async () => {
    clearTripsFixture();
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.click(screen.getByRole('button', { name: 'Save place' }));

    expect(await screen.findByText(/You don't have any trips yet/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Create one' })).toHaveAttribute('href', '/trips');
  });

  it('surfaces API failures via the existing global error-toast mapping', async () => {
    server.use(
      http.post(`${BASE_URL}/trips/:id/destinations`, () =>
        HttpResponse.json(
          {
            success: false,
            errorCode: 'Trip.AddDestination.Exception',
            error: 'Could not save this destination.',
            validates: [],
          },
          { status: 500 },
        ),
      ),
    );
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.click(screen.getByRole('button', { name: 'Save place' }));
    await user.click(await screen.findByRole('button', { name: 'Paris 2026' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save this destination.');
    expect(screen.getByRole('button', { name: 'Paris 2026' })).toBeInTheDocument();
  });

  it('offers Saved Places even for a trip with no dates yet (no day step to block on)', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.click(screen.getByRole('button', { name: 'Save place' }));
    await user.click(await screen.findByRole('button', { name: 'Someday Trip' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Saved to Someday Trip.');
  });
});
