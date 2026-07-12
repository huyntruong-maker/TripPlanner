import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import {
  EXISTING_TRIP_ID,
  SHORTEN_DATES_TRIP_ID,
  SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID,
  TRIP_WITHOUT_DATES_ID,
} from '../../test/msw/handlers/trips';
import { renderTripsRoutes, signInForTest } from '../../test/tripsTestRoutes';

describe('TripPlannerPage', () => {
  beforeEach(() => {
    localStorage.clear();
    signInForTest();
  });

  it('shows an error state for a trip that does not exist / is not owned by the caller', async () => {
    renderTripsRoutes(['/trips/does-not-exist']);

    expect(await screen.findByRole('alert')).toHaveTextContent('Trip not found.');
  });

  it('renders itinerary days and their scheduled destinations (F3-US10)', async () => {
    renderTripsRoutes([`/trips/${EXISTING_TRIP_ID}`]);

    expect(await screen.findByRole('heading', { name: 'Paris 2026' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Day 1 — 2026-07-01' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Day 2 — 2026-07-02' })).toBeInTheDocument();

    const dayTwo = screen.getByRole('heading', { name: 'Day 2 — 2026-07-02' }).closest('div');
    expect(dayTwo).not.toBeNull();
    expect(within(dayTwo as HTMLElement).getByText('Notre-Dame')).toBeInTheDocument();
  });

  it('prompts to set dates before any itinerary days exist', async () => {
    renderTripsRoutes([`/trips/${TRIP_WITHOUT_DATES_ID}`]);

    expect(await screen.findByRole('heading', { name: 'Someday Trip' })).toBeInTheDocument();
    expect(
      screen.getByText('Set the trip dates above to generate your itinerary days.'),
    ).toBeInTheDocument();
  });

  it('sets dates and generates itinerary days (F3-US2)', async () => {
    const user = userEvent.setup();
    renderTripsRoutes([`/trips/${TRIP_WITHOUT_DATES_ID}`]);

    await screen.findByRole('heading', { name: 'Someday Trip' });

    const startDateInput = screen.getByLabelText('Start date');
    const endDateInput = screen.getByLabelText('End date');
    await user.type(startDateInput, '2026-09-01');
    await user.type(endDateInput, '2026-09-01');
    await user.click(screen.getByRole('button', { name: 'Set dates' }));

    expect(await screen.findByRole('heading', { name: /Day 1/ })).toBeInTheDocument();
  });

  it('surfaces the DestinationsUnscheduled warning when dates are shortened', async () => {
    const user = userEvent.setup();
    renderTripsRoutes([`/trips/${SHORTEN_DATES_TRIP_ID}`]);

    await screen.findByRole('heading', { name: 'Trip To Shorten' });

    const endDateInput = screen.getByLabelText('End date');
    await user.clear(endDateInput);
    await user.type(endDateInput, '2026-08-01');
    await user.click(screen.getByRole('button', { name: 'Update dates' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Some destinations no longer fit in the new date range and were unscheduled.',
    );
  });

  describe('shortening dates that would drop a scheduled destination (F3-US2 AC5)', () => {
    async function shortenEndDate(user: ReturnType<typeof userEvent.setup>) {
      renderTripsRoutes([`/trips/${SHORTEN_WITH_SCHEDULED_DESTINATION_TRIP_ID}`]);

      await screen.findByRole('heading', { name: 'Trip With Scheduled Day' });
      expect(screen.getByText('Louvre Museum')).toBeInTheDocument();

      const endDateInput = screen.getByLabelText('End date');
      await user.clear(endDateInput);
      await user.type(endDateInput, '2026-09-02');
      await user.click(screen.getByRole('button', { name: 'Update dates' }));
    }

    it('asks for confirmation instead of submitting immediately', async () => {
      const user = userEvent.setup();
      await shortenEndDate(user);

      expect(
        await screen.findByText('Shortening these dates will remove scheduled destinations'),
      ).toBeInTheDocument();
      // Not yet submitted — the destination is still there and no warning shown.
      expect(screen.getByText('Louvre Museum')).toBeInTheDocument();
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('keeps the current dates and the destination when the user cancels', async () => {
      const user = userEvent.setup();
      await shortenEndDate(user);

      await user.click(await screen.findByRole('button', { name: 'Keep current dates' }));

      expect(
        screen.queryByText('Shortening these dates will remove scheduled destinations'),
      ).not.toBeInTheDocument();
      expect(screen.getByText('Louvre Museum')).toBeInTheDocument();
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('submits and unschedules the destination once the user confirms', async () => {
      const user = userEvent.setup();
      await shortenEndDate(user);

      await user.click(await screen.findByRole('button', { name: 'Update dates anyway' }));

      expect(await screen.findByRole('status')).toHaveTextContent(
        'Some destinations no longer fit in the new date range and were unscheduled.',
      );
    });
  });

  describe('removing a destination (F3-US7 AC2)', () => {
    it('asks for confirmation and keeps the destination when the user cancels', async () => {
      const user = userEvent.setup();
      renderTripsRoutes([`/trips/${EXISTING_TRIP_ID}`]);

      expect(await screen.findByText('Notre-Dame')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Remove' }));
      expect(
        await screen.findByText('Remove Notre-Dame from this day?'),
      ).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Cancel' }));

      expect(
        screen.queryByText('Remove Notre-Dame from this day?'),
      ).not.toBeInTheDocument();
      expect(screen.getByText('Notre-Dame')).toBeInTheDocument();
    });

    it('removes the destination only after the user confirms', async () => {
      const user = userEvent.setup();
      renderTripsRoutes([`/trips/${EXISTING_TRIP_ID}`]);

      expect(await screen.findByText('Notre-Dame')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Remove' }));
      await user.click(await screen.findByRole('button', { name: 'Yes, remove' }));

      await waitFor(() => expect(screen.queryByText('Notre-Dame')).not.toBeInTheDocument());
      expect(screen.getAllByText('No destinations scheduled for this day yet.')).toHaveLength(2);
    });
  });
});
