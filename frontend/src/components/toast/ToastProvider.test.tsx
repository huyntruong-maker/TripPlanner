import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider, useToast, type ToastOptions } from './ToastProvider';

function TriggerButton({ message, options }: { message: string; options?: ToastOptions }) {
  const { showToast } = useToast();
  return (
    <button type="button" onClick={() => showToast(message, options)}>
      Trigger
    </button>
  );
}

describe('ToastProvider', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows a toast with role="alert" when showToast is called (default tone is error)', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ToastProvider>
        <TriggerButton message="Something failed." />
      </ToastProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Trigger' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Something failed.');
  });

  it('dismisses a toast when its close button is clicked', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ToastProvider>
        <TriggerButton message="Something failed." />
      </ToastProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Trigger' }));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Dismiss notification' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('auto-dismisses a toast after 3 seconds', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ToastProvider>
        <TriggerButton message="Something failed." />
      </ToastProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Trigger' }));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    vi.advanceTimersByTime(2999);
    expect(screen.getByRole('alert')).toBeInTheDocument();

    vi.advanceTimersByTime(1);
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
  });

  it('shows multiple toasts independently', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ToastProvider>
        <TriggerButton message="First error." />
        <TriggerButton message="Second error." />
      </ToastProvider>,
    );

    const [first, second] = screen.getAllByRole('button', { name: 'Trigger' });
    await user.click(first);
    await user.click(second);

    expect(screen.getAllByRole('alert')).toHaveLength(2);
  });

  describe('success tone', () => {
    it('shows a green, role="status" toast (not role="alert") and also auto-dismisses after 3s', async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      render(
        <ToastProvider>
          <TriggerButton message="Saved to Paris 2026." options={{ tone: 'success' }} />
        </ToastProvider>,
      );

      await user.click(screen.getByRole('button', { name: 'Trigger' }));

      const toast = screen.getByRole('status');
      expect(toast).toHaveTextContent('Saved to Paris 2026.');
      expect(toast.className).toMatch(/green/);
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();

      vi.advanceTimersByTime(3000);
      await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument());
    });
  });

  describe('error tone', () => {
    it('is styled distinctly (red) from the success tone', async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      render(
        <ToastProvider>
          <TriggerButton message="Something failed." options={{ tone: 'error' }} />
        </ToastProvider>,
      );

      await user.click(screen.getByRole('button', { name: 'Trigger' }));

      const toast = screen.getByRole('alert');
      expect(toast.className).toMatch(/error/);
      expect(toast.className).not.toMatch(/green/);
    });
  });

  describe('action button (e.g. Retry)', () => {
    it('runs the action and dismisses the toast when clicked', async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      const onAction = vi.fn();
      render(
        <ToastProvider>
          <TriggerButton
            message="Could not move this destination."
            options={{ tone: 'error', action: { label: 'Retry', onAction } }}
          />
        </ToastProvider>,
      );

      await user.click(screen.getByRole('button', { name: 'Trigger' }));
      await user.click(screen.getByRole('button', { name: 'Retry' }));

      expect(onAction).toHaveBeenCalledTimes(1);
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });
});
