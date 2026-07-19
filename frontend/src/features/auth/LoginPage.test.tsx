import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { UNVERIFIED_CREDENTIALS, VALID_CREDENTIALS } from '../../test/msw/handlers/auth';
import { renderAuthRoutes } from '../../test/authTestRoutes';

describe('LoginPage', () => {
  it('shows validation errors for an empty submission instead of calling the API', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText('Email is required')).toBeInTheDocument();
    expect(screen.getByText('Password is required')).toBeInTheDocument();
  });

  it('logs in successfully and navigates to the default destination', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), VALID_CREDENTIALS.password);
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    await waitFor(() => expect(screen.getByText('Trips page content')).toBeInTheDocument());
  });

  it('shows the server error message for invalid credentials', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), 'totally-wrong');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(
      await screen.findByText('Incorrect username or password.', { selector: 'p[role="alert"]' }),
    ).toBeInTheDocument();
  });

  it('also surfaces invalid-credential errors as a global toast popup', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), 'totally-wrong');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(
      await screen.findByText('Incorrect username or password.', { selector: '.toast span' }),
    ).toBeInTheDocument();
  });

  it('shows a clear message hinting to check the inbox when the account is not verified yet', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.type(screen.getByLabelText('Email'), UNVERIFIED_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), UNVERIFIED_CREDENTIALS.password);
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(
      await screen.findByText(
        'Please verify your email before logging in — check your inbox for the verification link.',
        { selector: 'p[role="alert"]' },
      ),
    ).toBeInTheDocument();
  });

  describe('arriving after email verification (VerifyEmailPage redirect)', () => {
    it('shows a green success banner (no dismiss button) when the justVerified flag is set', async () => {
      renderAuthRoutes([{ pathname: '/login', state: { justVerified: true } }]);

      const banner = await screen.findByRole('status');
      expect(banner).toHaveTextContent('Email verified successfully — please log in.');
      expect(banner.className).toContain('bg-green-50');

      // A success notice needs no dismiss affordance — logging in navigates away anyway.
      expect(screen.queryByRole('button', { name: 'Dismiss notification' })).not.toBeInTheDocument();
    });

    it('does not show the banner on a normal visit to /login', () => {
      renderAuthRoutes(['/login']);

      expect(
        screen.queryByText('Email verified successfully — please log in.'),
      ).not.toBeInTheDocument();
    });
  });
});
