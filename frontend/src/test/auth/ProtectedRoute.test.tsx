import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { VALID_CREDENTIALS } from '../msw/handlers/auth';
import { buildFakeJwt } from '../buildFakeJwt';
import { renderAuthRoutes } from '../authTestRoutes';

describe('ProtectedRoute', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('redirects an anonymous visitor from /trips to /login', async () => {
    renderAuthRoutes(['/trips']);

    expect(await screen.findByRole('heading', { name: 'Log in' })).toBeInTheDocument();
  });

  it('sends the user back to /trips after logging in (F3-US8 returnTo)', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/trips']);

    await screen.findByRole('heading', { name: 'Log in' });

    await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), VALID_CREDENTIALS.password);
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    await waitFor(() => expect(screen.getByText('Trips page content')).toBeInTheDocument());
  });

  it('renders the protected content directly when a session is already persisted', () => {
    localStorage.setItem(
      'tripplanner.token',
      buildFakeJwt({ nameid: 'user-1', unique_name: VALID_CREDENTIALS.email }),
    );
    localStorage.setItem('tripplanner.refreshToken', 'refresh-1');

    renderAuthRoutes(['/trips']);

    expect(screen.getByText('Trips page content')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Log in' })).not.toBeInTheDocument();
  });
});
