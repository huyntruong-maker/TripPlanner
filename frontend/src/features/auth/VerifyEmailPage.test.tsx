import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { ALREADY_VERIFIED_TOKEN, VALID_VERIFY_TOKEN } from '../../test/msw/handlers/auth';
import { VerifyEmailPage } from './VerifyEmailPage';

function renderVerifyEmail(route: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <Routes>
          <Route path="/verify-email" element={<VerifyEmailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('VerifyEmailPage', () => {
  it('shows a missing-token state when the link has no token', () => {
    renderVerifyEmail('/verify-email');

    expect(screen.getByRole('alert')).toHaveTextContent('missing a verification token');
  });

  it('shows a success state for a valid token', async () => {
    renderVerifyEmail(`/verify-email?token=${VALID_VERIFY_TOKEN}`);

    expect(await screen.findByRole('heading', { name: 'Email verified' })).toBeInTheDocument();
  });

  it('shows an already-verified state distinctly from a generic failure', async () => {
    renderVerifyEmail(`/verify-email?token=${ALREADY_VERIFIED_TOKEN}`);

    expect(await screen.findByRole('heading', { name: 'Already verified' })).toBeInTheDocument();
  });

  it('shows an invalid/expired state for an unrecognized token', async () => {
    renderVerifyEmail('/verify-email?token=garbage');

    expect(await screen.findByRole('alert')).toHaveTextContent('This verification link is invalid.');
  });
});
