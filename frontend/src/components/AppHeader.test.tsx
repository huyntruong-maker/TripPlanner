import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '../auth/AuthContext';
import { signInForTest } from '../test/tripsTestRoutes';
import { AppHeader } from './AppHeader';

function renderHeader() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <AppHeader />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AppHeader', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('when logged out', () => {
    it('offers a log-in link and no account menu', () => {
      renderHeader();

      expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login');
      expect(screen.queryByRole('button', { name: 'Account menu' })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: 'My trips' })).not.toBeInTheDocument();
    });
  });

  describe('when logged in', () => {
    it('links to the trip list instead of log in', () => {
      signInForTest();
      renderHeader();

      expect(screen.getByRole('link', { name: 'My trips' })).toHaveAttribute('href', '/trips');
      expect(screen.queryByRole('link', { name: 'Log in' })).not.toBeInTheDocument();
    });

    it('reveals the signed-in email and a log-out item only after opening the menu', async () => {
      const user = userEvent.setup();
      signInForTest('jane@example.com');
      renderHeader();

      expect(screen.queryByRole('menuitem', { name: 'Log out' })).not.toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Account menu' }));

      expect(screen.getByText('jane@example.com')).toBeInTheDocument();
      expect(screen.getByRole('menuitem', { name: 'Log out' })).toBeInTheDocument();
    });

    it('closes the account menu on Escape', async () => {
      const user = userEvent.setup();
      signInForTest();
      renderHeader();

      const trigger = screen.getByRole('button', { name: 'Account menu' });
      await user.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      await user.keyboard('{Escape}');

      expect(trigger).toHaveAttribute('aria-expanded', 'false');
      expect(screen.queryByRole('menuitem', { name: 'Log out' })).not.toBeInTheDocument();
    });
  });

  describe('mobile navigation', () => {
    it('toggles the nav panel so links stay reachable on small screens', async () => {
      const user = userEvent.setup();
      renderHeader();

      const toggle = screen.getByRole('button', { name: 'Open navigation menu' });
      expect(toggle).toHaveAttribute('aria-expanded', 'false');

      await user.click(toggle);

      expect(screen.getByRole('button', { name: 'Close navigation menu' })).toHaveAttribute(
        'aria-expanded',
        'true',
      );
    });

    it('collapses the nav panel after following a link', async () => {
      const user = userEvent.setup();
      renderHeader();

      await user.click(screen.getByRole('button', { name: 'Open navigation menu' }));
      await user.click(screen.getByRole('link', { name: 'Log in' }));

      expect(screen.getByRole('button', { name: 'Open navigation menu' })).toHaveAttribute(
        'aria-expanded',
        'false',
      );
    });
  });
});
