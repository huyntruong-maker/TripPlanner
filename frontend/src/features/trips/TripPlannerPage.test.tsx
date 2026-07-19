import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import {
  EXISTING_TRIP_ID,
  PLANNER_TRIP_ID,
  SHORTEN_DATES_TRIP_ID,
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

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This trip no longer exists or you do not have access to it.',
    );
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

    expect(
      await screen.findByText(
        'Some destinations no longer fit in the new date range and were unscheduled.',
      ),
    ).toBeInTheDocument();
  });

  it('removes a destination immediately (F3-US7)', async () => {
    const user = userEvent.setup();
    renderTripsRoutes([`/trips/${EXISTING_TRIP_ID}`]);

    expect(await screen.findByText('Notre-Dame')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Remove' }));

    await waitFor(() => expect(screen.queryByText('Notre-Dame')).not.toBeInTheDocument());
    expect(screen.getAllByText('No destinations scheduled for this day yet.')).toHaveLength(2);
  });

  describe('planner board (F3-US4/US5/US6)', () => {
    it('renders a Saved Places column alongside the day columns', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      expect(await screen.findByRole('heading', { name: 'Saved Places' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'Day 1 — 2026-09-01' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'Day 2 — 2026-09-02' })).toBeInTheDocument();

      const savedPlaces = screen.getByRole('heading', { name: 'Saved Places' }).closest('div');
      expect(within(savedPlaces as HTMLElement).getByText('Louvre Museum')).toBeInTheDocument();

      const dayOne = screen.getByRole('heading', { name: 'Day 1 — 2026-09-01' }).closest('div');
      expect(within(dayOne as HTMLElement).getByText('Duplicate Trigger Place')).toBeInTheDocument();

      expect(
        screen.getByText('No destinations scheduled for this day yet.'),
      ).toBeInTheDocument();
    });

    it('every destination has a keyboard-operable drag handle', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');

      expect(screen.getByRole('button', { name: 'Reorder Louvre Museum' })).toBeInTheDocument();
      expect(
        screen.getByRole('button', { name: `Reorder Duplicate Trigger Place` }),
      ).toBeInTheDocument();
    });

    it('lays days out in a wrapping grid rather than a horizontal-scroll strip', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');

      const dayOneHeading = screen.getByRole('heading', { name: 'Day 1 — 2026-09-01' });
      const daysGrid = dayOneHeading.closest('div')?.parentElement;
      expect(daysGrid).not.toBeNull();
      expect(daysGrid).toHaveClass('grid');
      // The old layout put everything (Saved Places + every day) in one overflow-x-auto flex row.
      expect(document.querySelector('.overflow-x-auto')).not.toBeInTheDocument();
    });

    it('uses a narrower minimum column width so more days fit on one row before wrapping', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      const dayOneHeading = await screen.findByRole('heading', { name: 'Day 1 — 2026-09-01' });
      const daysGrid = dayOneHeading.closest('div')?.parentElement as HTMLElement;

      expect(daysGrid).toHaveClass('grid-cols-[repeat(auto-fill,minmax(210px,1fr))]');
    });

    it('slims the Saved Places sidebar down from its previous width', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      const savedPlacesHeading = await screen.findByRole('heading', { name: 'Saved Places' });
      const sidebar = savedPlacesHeading.closest('aside');

      expect(sidebar).toHaveClass('lg:w-[240px]');
    });

    it('keeps Saved Places out of the day-columns grid — two independent containers, not one grid with a hole', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');

      const dayOneHeading = screen.getByRole('heading', { name: 'Day 1 — 2026-09-01' });
      const daysGrid = dayOneHeading.closest('div')?.parentElement as HTMLElement;
      const savedPlacesHeading = screen.getByRole('heading', { name: 'Saved Places' });

      expect(daysGrid.contains(savedPlacesHeading)).toBe(false);
      // Saved Places sits in its own sticky sidebar, a sibling of (not inside) the days grid.
      expect(savedPlacesHeading.closest('aside')).not.toBeNull();
      expect(daysGrid.closest('aside')).toBeNull();
    });

    it('gives every day/Saved Places card the same compact minimum height, empty or not', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');

      const savedPlacesCard = screen.getByRole('heading', { name: 'Saved Places' }).closest('div');
      const dayOneCard = screen.getByRole('heading', { name: 'Day 1 — 2026-09-01' }).closest('div');
      const dayTwoCard = screen.getByRole('heading', { name: 'Day 2 — 2026-09-02' }).closest('div');
      // Day 2 is empty; it must share the same min-height class as the populated cards, not a
      // taller one from its own empty-state box.
      expect(savedPlacesCard).toHaveClass('min-h-[160px]');
      expect(dayOneCard).toHaveClass('min-h-[160px]');
      expect(dayTwoCard).toHaveClass('min-h-[160px]');
    });

    it('shows a compact single-line day heading (full date kept as the accessible name)', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      const heading = await screen.findByRole('heading', { name: 'Day 1 — 2026-09-01' });
      // Accessible name (aria-label) stays the full date; visible text is the compact form.
      expect(heading).toHaveTextContent('Day 1 · Sep 1');
      expect(heading).toHaveClass('truncate');
      expect(heading).toHaveAttribute('title', 'Day 1 — 2026-09-01');
    });

    it('truncates a long item name to a single line instead of wrapping/growing the row', async () => {
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      const itemName = await screen.findByText('Louvre Museum');
      expect(itemName).toHaveClass('truncate');
      expect(itemName).toHaveAttribute('title', 'Louvre Museum');
    });

    it('offers a mobile collapse toggle for Saved Places, expanded by default', async () => {
      const user = userEvent.setup();
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');

      const toggle = screen.getByRole('button', { name: /Saved Places/ });
      expect(toggle).toHaveAttribute('aria-expanded', 'true');
      expect(screen.getByText('Louvre Museum')).toBeVisible();

      await user.click(toggle);

      expect(toggle).toHaveAttribute('aria-expanded', 'false');
    });
  });

  describe('saving indicator (F3-US9)', () => {
    it('shows "Saving…" while a mutation is in flight, then "All changes saved"', async () => {
      const user = userEvent.setup();
      renderTripsRoutes([`/trips/${PLANNER_TRIP_ID}`]);

      await screen.findByText('Louvre Museum');
      expect(screen.getByText('All changes saved')).toBeInTheDocument();

      await user.click(
        within(
          screen.getByRole('button', { name: 'Reorder Louvre Museum' }).closest('li') as HTMLElement,
        ).getByRole('button', { name: 'Remove' }),
      );

      expect(await screen.findByText('Saving…')).toBeInTheDocument();
      await waitFor(() => expect(screen.getByText('All changes saved')).toBeInTheDocument());
    });
  });
});
