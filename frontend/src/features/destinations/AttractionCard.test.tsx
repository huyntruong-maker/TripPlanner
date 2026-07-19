import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import { signInForTest } from '../../test/tripsTestRoutes';
import type { AttractionSummary } from '../../types';
import { AttractionCard } from './AttractionCard';

function renderCard(attraction: AttractionSummary) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AuthProvider>
          <ul>
            <AttractionCard attraction={attraction} />
          </ul>
        </AuthProvider>
      </MemoryRouter>
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

describe('AttractionCard — quick-save hover icon', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('shows a login-linked save icon when logged out, separate from the destination link', () => {
    renderCard(BASE_ATTRACTION);

    const saveLink = screen.getByRole('link', { name: 'Save to trip' });
    expect(saveLink.tagName).toBe('A');
    expect(saveLink).toHaveAttribute('href', expect.stringContaining('/login'));

    // The destination link is a distinct element — the save affordance isn't nested inside it.
    const destinationLink = screen.getByRole('link', { name: /Grand Museum/ });
    expect(destinationLink).not.toBe(saveLink);
  });

  it('opens the same add-to-trip popover as a hover/focus icon button when logged in', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    const saveButton = screen.getByRole('button', { name: 'Save to trip' });
    expect(saveButton).toHaveAttribute('aria-expanded', 'false');
    expect(saveButton.closest('a')).toBeNull();

    await user.click(saveButton);

    expect(screen.getByRole('button', { name: 'Close save to trip' })).toHaveAttribute(
      'aria-expanded',
      'true',
    );
    expect(screen.getByLabelText('Trip')).toBeInTheDocument();
    // Confirms the click didn't navigate away from the card.
    expect(screen.getByRole('heading', { name: 'Grand Museum' })).toBeInTheDocument();
  });

  it('is keyboard-focusable (visible on focus, not just hover)', async () => {
    signInForTest();
    const user = userEvent.setup();
    renderCard(BASE_ATTRACTION);

    await user.tab();
    expect(screen.getByRole('button', { name: 'Save to trip' })).toHaveFocus();
  });
});
