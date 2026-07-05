import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider, useToast } from './ToastProvider';

function TriggerButton({ message }: { message: string }) {
  const { showToast } = useToast();
  return (
    <button type="button" onClick={() => showToast(message)}>
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

  it('shows a toast with role="alert" when showToast is called', async () => {
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

  it('auto-dismisses a toast after the timeout', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ToastProvider>
        <TriggerButton message="Something failed." />
      </ToastProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Trigger' }));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    vi.advanceTimersByTime(8000);

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
});
