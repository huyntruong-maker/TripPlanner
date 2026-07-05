import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { EXISTING_EMAIL } from '../../test/msw/handlers/auth';
import { renderAuthRoutes } from '../../test/authTestRoutes';

async function fillAndSubmit(
  user: ReturnType<typeof userEvent.setup>,
  { firstName, email, password }: { firstName: string; email: string; password: string },
) {
  if (firstName) await user.type(screen.getByLabelText('First name'), firstName);
  if (email) await user.type(screen.getByLabelText('Email'), email);
  if (password) await user.type(screen.getByLabelText(/Password/), password);
  await user.click(screen.getByRole('button', { name: 'Sign up' }));
}

describe('RegisterPage', () => {
  it('shows validation errors for an empty submission', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/register']);

    await user.click(screen.getByRole('button', { name: 'Sign up' }));

    expect(await screen.findByText('First name is required')).toBeInTheDocument();
    expect(screen.getByText('Email is required')).toBeInTheDocument();
    expect(screen.getByText('Password must be at least 8 characters')).toBeInTheDocument();
  });

  it('registers successfully and shows a check-your-email message (no auto-login)', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/register']);

    await fillAndSubmit(user, { firstName: 'Jane', email: 'new.user@example.com', password: 'Secret123!' });

    expect(await screen.findByRole('heading', { name: 'Check your email' })).toBeInTheDocument();
    expect(screen.getByText(/new\.user@example\.com/)).toBeInTheDocument();
  });

  it('shows the server error message for a duplicate email', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/register']);

    await fillAndSubmit(user, { firstName: 'Jane', email: EXISTING_EMAIL, password: 'Secret123!' });

    expect(
      await screen.findByText('An account with this email already exists.', { selector: 'p.error' }),
    ).toBeInTheDocument();
  });

  it('also surfaces duplicate-email errors as a global toast popup', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/register']);

    await fillAndSubmit(user, { firstName: 'Jane', email: EXISTING_EMAIL, password: 'Secret123!' });

    expect(
      await screen.findByText('An account with this email already exists.', {
        selector: '.toast span',
      }),
    ).toBeInTheDocument();
  });
});
