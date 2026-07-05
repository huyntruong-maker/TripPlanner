import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { VALID_CREDENTIALS } from '../../test/msw/handlers/auth';
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
      await screen.findByText('Invalid email or password.', { selector: 'p.error' }),
    ).toBeInTheDocument();
  });

  it('also surfaces invalid-credential errors as a global toast popup', async () => {
    const user = userEvent.setup();
    renderAuthRoutes(['/login']);

    await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
    await user.type(screen.getByLabelText('Password'), 'totally-wrong');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(
      await screen.findByText('Invalid email or password.', { selector: '.toast span' }),
    ).toBeInTheDocument();
  });
});
