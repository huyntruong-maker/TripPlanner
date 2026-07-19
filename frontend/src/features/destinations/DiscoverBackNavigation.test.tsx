import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useNavigate } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import { CITY_WITH_ATTRACTIONS_QUERY } from '../../test/msw/handlers/destinations';
import { DestinationDetailPage } from './DestinationDetailPage';
import { SearchPage } from './SearchPage';

/** Stands in for the browser's own Back button/gesture — same mechanism react-router uses for it (a POP navigation), without depending on a real browser. */
function SimulateBrowserBack() {
  const navigate = useNavigate();
  return (
    <button type="button" onClick={() => navigate(-1)}>
      Simulate browser Back
    </button>
  );
}

function renderApp(initialEntries: string[] = ['/']) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <AuthProvider>
          <SimulateBrowserBack />
          {/* Mirrors App.tsx's real route config for these two routes. */}
          <Routes>
            <Route path="/" element={<SearchPage />} />
            <Route path="/destinations/:providerPlaceId" element={<DestinationDetailPage />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Discover -> destination detail -> Back preserves search state (real navigation, F1 bug report)', () => {
  async function searchSelectAndOpenDetail(user: ReturnType<typeof userEvent.setup>) {
    renderApp(['/']);

    await user.type(screen.getByLabelText('Search a city or country'), CITY_WITH_ATTRACTIONS_QUERY);
    await user.click(await screen.findByRole('option', { name: 'Paris, France' }));
    await screen.findByRole('heading', { name: 'Eiffel Tower' });

    // Real navigation via the card's own <Link>, not a manually-typed URL.
    await user.click(screen.getByRole('link', { name: /Eiffel Tower/ }));
    expect(
      await screen.findByRole('heading', { name: 'Eiffel Tower', level: 1 }),
    ).toBeInTheDocument();
  }

  function expectDiscoverStateRestored() {
    return Promise.all([
      screen.findByRole('heading', { name: 'Attractions near Paris, France' }),
      screen.findByRole('heading', { name: 'Eiffel Tower' }),
    ]).then(() => {
      expect(screen.getByDisplayValue('Paris, France')).toBeInTheDocument();
    });
  }

  it('restores the selected location and attraction list via the detail page\'s own "Back to search" control', async () => {
    const user = userEvent.setup();
    await searchSelectAndOpenDetail(user);

    // What a user actually clicks — this was the bug: it pushed a fresh, param-less "/".
    await user.click(screen.getByRole('button', { name: 'Back to search' }));

    await expectDiscoverStateRestored();
  });

  it('also restores it via a genuine browser Back gesture', async () => {
    const user = userEvent.setup();
    await searchSelectAndOpenDetail(user);

    await user.click(screen.getByRole('button', { name: 'Simulate browser Back' }));

    await expectDiscoverStateRestored();
  });

  describe('when the detail page has no in-app history (e.g. after a dev-server full reload)', () => {
    afterEach(() => {
      sessionStorage.clear();
    });

    it('restores the last search from sessionStorage instead of landing on a bare "/"', async () => {
      const user = userEvent.setup();
      sessionStorage.setItem(
        'discover:lastSearch',
        '?q=Paris%2C+France&lat=48.8566&lng=2.3522&name=Paris&locationType=city&country=France',
      );

      // The detail URL is the FIRST history entry — location.key is 'default', navigate(-1) has
      // nowhere to go. This is exactly the post-reload state the user reported.
      renderApp(['/destinations/W214242']);
      await screen.findByRole('heading', { name: 'Eiffel Tower', level: 1 });

      await user.click(screen.getByRole('button', { name: 'Back to search' }));

      await expectDiscoverStateRestored();
    });
  });
});
