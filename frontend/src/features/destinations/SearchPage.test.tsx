import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import {
  ATTRACTIONS_ERROR_CITY_QUERY,
  CITY_WITH_ATTRACTIONS_QUERY,
  CITY_WITH_NO_ATTRACTIONS_QUERY,
  LOCATION_SEARCH_ERROR_QUERY,
  NO_MATCHING_LOCATIONS_QUERY,
  SLOW_CITY_QUERY,
} from '../../test/msw/handlers/destinations';
import { SearchPage } from './SearchPage';

function renderSearchPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AuthProvider>
          <SearchPage />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function typeQuery(user: ReturnType<typeof userEvent.setup>, value: string) {
  await user.type(screen.getByLabelText('Search a city or country'), value);
}

describe('SearchPage — location search (F1/US2)', () => {
  it('shows a loading state while searching', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, SLOW_CITY_QUERY);

    expect(await screen.findByText('Searching…')).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: 'SlowCity, Testland' })).toBeInTheDocument();
  });

  it('shows an empty state when no city or country matches', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, NO_MATCHING_LOCATIONS_QUERY);

    expect(await screen.findByText('No matching cities or countries.')).toBeInTheDocument();
  });

  it('shows an error state when the location search fails', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, LOCATION_SEARCH_ERROR_QUERY);

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not search locations.');
  });

  it('lists matching locations and lets the user pick one', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, CITY_WITH_ATTRACTIONS_QUERY);

    expect(await screen.findByRole('button', { name: 'Paris, France' })).toBeInTheDocument();
  });
});

describe('SearchPage — attractions grid (F1/US3)', () => {
  it('shows a loading state while fetching attractions', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, SLOW_CITY_QUERY);
    await user.click(await screen.findByRole('button', { name: 'SlowCity, Testland' }));

    expect(await screen.findByText('Loading attractions…')).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
  });

  it('renders the results grid with thumbnail, category/tags, and rating when available', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, CITY_WITH_ATTRACTIONS_QUERY);
    await user.click(await screen.findByRole('button', { name: 'Paris, France' }));

    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
    // Thumbnail is decorative (alt=""); assert the image directly instead of by accessible name.
    const thumbnail = document.querySelector('img.attraction-thumbnail');
    expect(thumbnail).toHaveAttribute('src', 'https://example.test/eiffel.jpg');
    expect(thumbnail).toHaveAttribute('alt', '');
    expect(screen.getAllByText('cultural').length).toBeGreaterThan(0);
    expect(screen.getByText('landmark')).toBeInTheDocument();
    expect(screen.getByText('Rating 9.5')).toBeInTheDocument();

    // Louvre has no thumbnail/rating — placeholder + no rating line, not a crash.
    expect(screen.getByRole('heading', { name: 'Louvre Museum' })).toBeInTheDocument();
    expect(screen.getByText('No photo')).toBeInTheDocument();
  });

  it('shows the "No attractions found" empty state', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, CITY_WITH_NO_ATTRACTIONS_QUERY);
    await user.click(await screen.findByRole('button', { name: 'Nowhereville, Nowhere' }));

    expect(await screen.findByText('No attractions found.')).toBeInTheDocument();
  });

  it('shows an error state when the attractions request fails', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, ATTRACTIONS_ERROR_CITY_QUERY);
    await user.click(await screen.findByRole('button', { name: 'AttractionsErrorCity, Testland' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load attractions.');
  });
});
