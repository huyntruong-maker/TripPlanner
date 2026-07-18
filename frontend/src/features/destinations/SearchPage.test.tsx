import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import {
  ATTRACTIONS_ERROR_CITY_QUERY,
  CITY_WITH_ATTRACTIONS_QUERY,
  CITY_WITH_NO_ATTRACTIONS_QUERY,
  FILTER_SORT_CITY_QUERY,
  LOCATION_SEARCH_ERROR_QUERY,
  MANY_MATCHES_QUERY,
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
    expect(await screen.findByRole('option', { name: 'SlowCity, Testland' })).toBeInTheDocument();
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

    expect(await screen.findByRole('option', { name: 'Paris, France' })).toBeInTheDocument();
  });
});

describe('SearchPage — autocomplete combobox (F1-US1)', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not query until at least 2 characters are typed', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    await user.type(screen.getByLabelText('Search a city or country'), 'P');
    act(() => {
      vi.advanceTimersByTime(500);
    });

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('debounces ~300ms before fetching suggestions', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    await user.type(screen.getByLabelText('Search a city or country'), CITY_WITH_ATTRACTIONS_QUERY);

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(await screen.findByRole('option', { name: 'Paris, France' })).toBeInTheDocument();
  });

  it('shows at most 5 suggestions', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    await typeQuery(user, MANY_MATCHES_QUERY);
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(await screen.findAllByRole('option')).toHaveLength(5);
  });

  it('has proper combobox ARIA wiring while the listbox is open', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    const input = screen.getByRole('combobox', { name: 'Search a city or country' });
    expect(input).toHaveAttribute('aria-expanded', 'false');

    await user.type(input, CITY_WITH_ATTRACTIONS_QUERY);
    act(() => {
      vi.advanceTimersByTime(300);
    });

    const option = await screen.findByRole('option', { name: 'Paris, France' });
    expect(input).toHaveAttribute('aria-expanded', 'true');
    expect(input).toHaveAttribute('aria-controls', option.closest('ul')?.id);
  });

  it('selects a suggestion with the keyboard (ArrowDown + Enter)', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    const input = screen.getByLabelText('Search a city or country');
    await user.type(input, CITY_WITH_ATTRACTIONS_QUERY);
    act(() => {
      vi.advanceTimersByTime(300);
    });
    await screen.findByRole('option', { name: 'Paris, France' });

    await user.keyboard('{ArrowDown}');
    expect(input).toHaveAttribute('aria-activedescendant', 'location-option-0');

    await user.keyboard('{Enter}');

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(input).toHaveValue('Paris, France');
    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
  });

  it('selects a suggestion with the mouse', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    await typeQuery(user, CITY_WITH_ATTRACTIONS_QUERY);
    act(() => {
      vi.advanceTimersByTime(300);
    });

    await user.click(await screen.findByRole('option', { name: 'Paris, France' }));

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
  });

  it('closes the listbox on Escape without clearing the typed query', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime, delay: null });
    renderSearchPage();

    const input = screen.getByLabelText('Search a city or country');
    await user.type(input, CITY_WITH_ATTRACTIONS_QUERY);
    act(() => {
      vi.advanceTimersByTime(300);
    });
    await screen.findByRole('option', { name: 'Paris, France' });

    await user.keyboard('{Escape}');

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(input).toHaveValue(CITY_WITH_ATTRACTIONS_QUERY);
  });
});

describe('SearchPage — attractions grid (F1/US3)', () => {
  it('shows a loading state while fetching attractions', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, SLOW_CITY_QUERY);
    await user.click(await screen.findByRole('option', { name: 'SlowCity, Testland' }));

    expect(await screen.findByText('Loading attractions…')).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
  });

  it('renders the results grid with thumbnail, category/tags, and rating when available', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, CITY_WITH_ATTRACTIONS_QUERY);
    await user.click(await screen.findByRole('option', { name: 'Paris, France' }));

    expect(await screen.findByRole('heading', { name: 'Eiffel Tower' })).toBeInTheDocument();
    // Thumbnail is decorative (alt=""); assert the image directly instead of by accessible name.
    const thumbnail = document.querySelector('img.attraction-thumbnail');
    expect(thumbnail).toHaveAttribute('src', 'https://example.test/eiffel.jpg');
    expect(thumbnail).toHaveAttribute('alt', '');
    // "cultural"/"landmark" also appear as filter-checkbox labels (F1-US4), so there may be more than one match.
    expect(screen.getAllByText('cultural').length).toBeGreaterThan(0);
    expect(screen.getAllByText('landmark').length).toBeGreaterThan(0);
    expect(screen.getByText('Rating 9.5')).toBeInTheDocument();

    // Louvre has no thumbnail/rating — placeholder + no rating line, not a crash.
    expect(screen.getByRole('heading', { name: 'Louvre Museum' })).toBeInTheDocument();
    expect(screen.getByText('No photo')).toBeInTheDocument();
  });

  it('shows the "No attractions found" empty state', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, CITY_WITH_NO_ATTRACTIONS_QUERY);
    await user.click(await screen.findByRole('option', { name: 'Nowhereville, Nowhere' }));

    expect(await screen.findByText('No attractions found.')).toBeInTheDocument();
  });

  it('shows an error state when the attractions request fails', async () => {
    const user = userEvent.setup();
    renderSearchPage();

    await typeQuery(user, ATTRACTIONS_ERROR_CITY_QUERY);
    await user.click(await screen.findByRole('option', { name: 'AttractionsErrorCity, Testland' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load attractions.');
  });
});

describe('SearchPage — filter and sort attractions (F1-US4/US5)', () => {
  async function selectFilterSortCity(user: ReturnType<typeof userEvent.setup>) {
    renderSearchPage();
    await typeQuery(user, FILTER_SORT_CITY_QUERY);
    await user.click(await screen.findByRole('option', { name: 'FilterSortCity, Testland' }));
    await screen.findByRole('heading', { name: 'Museum Alpha' });
  }

  it('renders a checkbox per distinct category and filters by selection', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    expect(screen.getAllByRole('heading', { name: /Museum|Park|Landmark/ })).toHaveLength(4);

    await user.click(screen.getByRole('checkbox', { name: 'museum' }));

    expect(screen.getByRole('heading', { name: 'Museum Alpha' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Museum Delta' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Park Beta' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Landmark Gamma' })).not.toBeInTheDocument();
  });

  it('filters by minimum rating, excluding attractions with no rating', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    await user.selectOptions(screen.getByLabelText('Minimum rating'), '8');

    expect(screen.getByRole('heading', { name: 'Museum Alpha' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Museum Delta' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Park Beta' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Landmark Gamma' })).not.toBeInTheDocument();
  });

  it('combines category and rating filters (AND)', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    await user.click(screen.getByRole('checkbox', { name: 'museum' }));
    await user.selectOptions(screen.getByLabelText('Minimum rating'), '9');

    expect(screen.getByRole('heading', { name: 'Museum Delta' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Museum Alpha' })).not.toBeInTheDocument();
  });

  it('shows a friendly empty state when filters exclude everything', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    await user.selectOptions(screen.getByLabelText('Minimum rating'), '9');
    await user.click(screen.getByRole('checkbox', { name: 'park' }));

    expect(
      await screen.findByText('No attractions match the selected filters.'),
    ).toBeInTheDocument();
  });

  it('restores the full list when filters are cleared', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    await user.click(screen.getByRole('checkbox', { name: 'museum' }));
    expect(screen.queryByRole('heading', { name: 'Park Beta' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Clear filters' }));

    expect(screen.getByRole('heading', { name: 'Park Beta' })).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'museum' })).not.toBeChecked();
  });

  it('sorts by highest rating, with missing ratings last', async () => {
    const user = userEvent.setup();
    await selectFilterSortCity(user);

    await user.selectOptions(screen.getByLabelText('Sort by'), 'rating');

    const headings = screen.getAllByRole('heading', { name: /Museum|Park|Landmark/ });
    expect(headings.map((heading) => heading.textContent)).toEqual([
      'Museum Delta',
      'Museum Alpha',
      'Park Beta',
      'Landmark Gamma',
    ]);
  });
});
