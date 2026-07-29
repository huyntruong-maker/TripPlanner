import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../msw/server';
import { VALID_CREDENTIALS } from '../../msw/handlers/auth';
import {
  DUPLICATE_NAME_TRIGGER,
  EXISTING_TRIP_ID,
  clearTripsFixture,
} from '../../msw/handlers/trips';
import { renderTripsRoutes, signInForTest } from '../../tripsTestRoutes';

describe('TripsPage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('login-redirect-and-back (F3-US8 AC2)', () => {
    it('redirects to /login when trying to view/create trips while logged out, then returns to /trips', async () => {
      const user = userEvent.setup();
      renderTripsRoutes(['/trips']);

      expect(await screen.findByRole('heading', { name: 'Log in' })).toBeInTheDocument();

      await user.type(screen.getByLabelText('Email'), VALID_CREDENTIALS.email);
      await user.type(screen.getByLabelText('Password'), VALID_CREDENTIALS.password);
      await user.click(screen.getByRole('button', { name: 'Log in' }));

      expect(await screen.findByRole('heading', { name: 'My trips' })).toBeInTheDocument();
    });
  });

  describe('trip list states (F3-US1, US10)', () => {
    it('shows an empty state when the user has no trips', async () => {
      clearTripsFixture();
      signInForTest();
      renderTripsRoutes(['/trips']);

      expect(
        await screen.findByText("You don't have any trips yet. Create one above to get started."),
      ).toBeInTheDocument();
    });

    it('lists existing trips with their dates', async () => {
      signInForTest();
      renderTripsRoutes(['/trips']);

      expect(await screen.findByRole('link', { name: 'Paris 2026' })).toBeInTheDocument();
      expect(screen.getByText('2026-07-01 – 2026-07-02')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Someday Trip' })).toBeInTheDocument();
    });

    it('shows an error state when the trip list fails to load', async () => {
      server.use(
        http.get('http://localhost:5080/api/v1/trips', () =>
          HttpResponse.json(
            { success: false, errorCode: 'Trip.GetTrips.Exception', error: 'Could not load your trips.', validates: [] },
            { status: 500 },
          ),
        ),
      );
      signInForTest();
      renderTripsRoutes(['/trips']);

      expect(await screen.findByRole('alert')).toHaveTextContent('Could not load your trips.');
    });
  });

  describe('create trip (F3-US1)', () => {
    it('shows a validation error when the name is empty', async () => {
      const user = userEvent.setup();
      signInForTest();
      renderTripsRoutes(['/trips']);

      await screen.findByRole('heading', { name: 'My trips' });
      await user.click(screen.getByRole('button', { name: 'Create trip' }));

      expect(await screen.findByText('Trip name is required')).toBeInTheDocument();
    });

    it('creates a trip and opens its planner page', async () => {
      const user = userEvent.setup();
      signInForTest();
      renderTripsRoutes(['/trips']);

      await user.type(screen.getByLabelText('Trip name'), 'Tokyo Adventure');
      await user.click(screen.getByRole('button', { name: 'Create trip' }));

      expect(await screen.findByRole('heading', { name: 'Tokyo Adventure' })).toBeInTheDocument();
    });

    it('shows the server error message when creation fails', async () => {
      const user = userEvent.setup();
      signInForTest();
      renderTripsRoutes(['/trips']);

      await user.type(screen.getByLabelText('Trip name'), DUPLICATE_NAME_TRIGGER);
      await user.click(screen.getByRole('button', { name: 'Create trip' }));

      expect(
        await screen.findByText('Could not create trip.', { selector: 'p.error' }),
      ).toBeInTheDocument();
    });

    it('also surfaces creation errors as a global toast popup', async () => {
      const user = userEvent.setup();
      signInForTest();
      renderTripsRoutes(['/trips']);

      await user.type(screen.getByLabelText('Trip name'), DUPLICATE_NAME_TRIGGER);
      await user.click(screen.getByRole('button', { name: 'Create trip' }));

      expect(
        await screen.findByText('Could not create trip.', { selector: '.toast span' }),
      ).toBeInTheDocument();
    });
  });

  it('logs out via the header account menu', async () => {
    const user = userEvent.setup();
    signInForTest();
    renderTripsRoutes(['/trips']);

    await screen.findByRole('heading', { name: 'My trips' });
    await user.click(screen.getByRole('button', { name: 'Account menu' }));
    await user.click(screen.getByRole('button', { name: 'Log out' }));

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Log in' })).toBeInTheDocument());
  });

  it('loads a saved trip already in the fixture (F3-US10 load saved trips)', async () => {
    signInForTest();
    renderTripsRoutes([`/trips/${EXISTING_TRIP_ID}`]);

    expect(await screen.findByRole('heading', { name: 'Paris 2026' })).toBeInTheDocument();
    expect(screen.getByText('Notre-Dame')).toBeInTheDocument();
  });
});
