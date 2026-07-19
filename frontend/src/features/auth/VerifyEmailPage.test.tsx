import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ALREADY_VERIFIED_TOKEN, VALID_VERIFY_TOKEN } from '../../test/msw/handlers/auth';
import { VerifyEmailPage } from './VerifyEmailPage';

function LoginRouteStub() {
  const location = useLocation();
  const state = location.state as { justVerified?: boolean } | null;
  return <p>Login page{state?.justVerified ? ' (justVerified)' : ''}</p>;
}

function renderVerifyEmail(route: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <Routes>
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/login" element={<LoginRouteStub />} />
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

  it('shows an already-verified state distinctly from a generic failure', async () => {
    renderVerifyEmail(`/verify-email?token=${ALREADY_VERIFIED_TOKEN}`);

    expect(await screen.findByRole('heading', { name: 'Already verified' })).toBeInTheDocument();
  });

  it('shows an invalid/expired state for an unrecognized token', async () => {
    renderVerifyEmail('/verify-email?token=garbage');

    expect(await screen.findByRole('alert')).toHaveTextContent('The verification link is invalid.');
  });

  describe('success — auto-redirect to /login (industry-standard flow)', () => {
    beforeEach(() => {
      vi.useFakeTimers({ shouldAdvanceTime: true });
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('shows a success state with a visible countdown and an immediate "Go to log in" button', async () => {
      renderVerifyEmail(`/verify-email?token=${VALID_VERIFY_TOKEN}`);

      expect(await screen.findByRole('heading', { name: 'Email verified' })).toBeInTheDocument();
      expect(screen.getByText(/Redirecting to log in/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Go to log in' })).toBeInTheDocument();
    });

    it('auto-redirects to /login (with a justVerified flag) after the countdown', async () => {
      renderVerifyEmail(`/verify-email?token=${VALID_VERIFY_TOKEN}`);
      await screen.findByRole('heading', { name: 'Email verified' });

      // Each tick reschedules the next one from a React effect, so advance (and let React flush)
      // one second at a time rather than jumping the whole 3000ms in one go.
      for (let tick = 0; tick < 3; tick += 1) {
        await act(async () => {
          vi.advanceTimersByTime(1000);
        });
      }

      expect(await screen.findByText('Login page (justVerified)')).toBeInTheDocument();
    });

    it('navigates immediately when "Go to log in" is clicked, without waiting for the countdown', async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      renderVerifyEmail(`/verify-email?token=${VALID_VERIFY_TOKEN}`);

      const goButton = await screen.findByRole('button', { name: 'Go to log in' });
      await user.click(goButton);

      expect(await screen.findByText('Login page (justVerified)')).toBeInTheDocument();
    });
  });
});
