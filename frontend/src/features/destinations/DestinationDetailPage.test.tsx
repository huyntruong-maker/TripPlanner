import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../../auth/AuthContext';
import { buildFakeJwt } from '../../test/buildFakeJwt';
import {
  FULL_DETAIL_ID,
  NOT_FOUND_ID,
  PARTIAL_DETAIL_ID,
} from '../../test/msw/handlers/destinationDetail';
import { DestinationDetailPage } from './DestinationDetailPage';

function renderDetailPage(providerPlaceId: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/destinations/${providerPlaceId}`]}>
        <AuthProvider>
          <Routes>
            <Route path="/destinations/:providerPlaceId" element={<DestinationDetailPage />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DestinationDetailPage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('renders full detail data when every field is present', async () => {
    renderDetailPage(FULL_DETAIL_ID);

    expect(await screen.findByRole('heading', { name: 'Eiffel Tower', level: 1 })).toBeInTheDocument();
    expect(screen.getAllByText('cultural').length).toBeGreaterThan(0);
    expect(screen.getByText('landmark')).toBeInTheDocument();
    expect(screen.getByText('Rating 9.5')).toBeInTheDocument();
    expect(screen.getByText(/Famous iron lattice tower/)).toBeInTheDocument();
    expect(screen.getByText('Champ de Mars, Paris, France')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Visit website' })).toHaveAttribute(
      'href',
      'https://toureiffel.paris',
    );
  });

  it('renders with every optional field null/empty without crashing (F2-US1 partial data)', async () => {
    renderDetailPage(PARTIAL_DETAIL_ID);

    expect(await screen.findByRole('heading', { name: 'Mystery Ruin', level: 1 })).toBeInTheDocument();
    expect(screen.queryByText('cultural')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Visit website' })).not.toBeInTheDocument();
    expect(screen.getByText('No photos available')).toBeInTheDocument();
    expect(screen.getByText('Opening hours not available.')).toBeInTheDocument();
  });

  it('shows an error state for an unrecognized destination id', async () => {
    renderDetailPage(NOT_FOUND_ID);

    expect(await screen.findByRole('alert')).toHaveTextContent('This destination could not be found.');
  });

  describe('opening hours', () => {
    it('shows the "Opening hours not available" fallback when null', async () => {
      renderDetailPage(PARTIAL_DETAIL_ID);

      expect(await screen.findByText('Opening hours not available.')).toBeInTheDocument();
    });

    it('shows display text, open-now status, and weekday hours when present', async () => {
      renderDetailPage(FULL_DETAIL_ID);

      expect(await screen.findByText('Daily 09:00–23:00')).toBeInTheDocument();
      expect(screen.getByText('Open now')).toBeInTheDocument();
      expect(screen.getByText('Monday: 09:00 – 23:00')).toBeInTheDocument();
    });
  });

  describe('photo carousel', () => {
    it('shows a placeholder when there are no photos', async () => {
      renderDetailPage(PARTIAL_DETAIL_ID);

      expect(await screen.findByText('No photos available')).toBeInTheDocument();
    });

    it('navigates between photos with Previous/Next controls', async () => {
      const user = userEvent.setup();
      renderDetailPage(FULL_DETAIL_ID);

      expect(await screen.findByAltText('Eiffel Tower photo 1 of 2')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Next photo' }));
      expect(screen.getByAltText('Eiffel Tower photo 2 of 2')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Previous photo' }));
      expect(screen.getByAltText('Eiffel Tower photo 1 of 2')).toBeInTheDocument();
    });
  });

  describe('Add to Trip (F3-US8 AC2 — disabled when logged out)', () => {
    it('is disabled and shows a login hint when logged out', async () => {
      renderDetailPage(FULL_DETAIL_ID);

      const addToTripButton = await screen.findByRole('button', { name: 'Add to Trip' });
      expect(addToTripButton).toBeDisabled();
      expect(screen.getByRole('link', { name: 'Log in' })).toBeInTheDocument();
    });

    it('is enabled when logged in', async () => {
      localStorage.setItem(
        'tripplanner.token',
        buildFakeJwt({ nameid: 'user-1', unique_name: 'jane@example.com' }),
      );
      localStorage.setItem('tripplanner.refreshToken', 'refresh-1');

      renderDetailPage(FULL_DETAIL_ID);

      const addToTripButton = await screen.findByRole('button', { name: 'Add to Trip' });
      await waitFor(() => expect(addToTripButton).toBeEnabled());
    });
  });
});
