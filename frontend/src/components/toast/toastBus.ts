import type { ToastOptions } from './ToastProvider';

type ToastListener = (message: string, options?: ToastOptions) => void;

let listener: ToastListener | null = null;

/** Lets non-React code (QueryClient's error handlers, or components that may render without a ToastProvider ancestor in tests) publish a toast without needing the React context. A no-op when no ToastProvider is mounted. */
export function registerToastListener(handler: ToastListener | null): void {
  listener = handler;
}

/** Always the (red, `role="alert"`) error tone. */
export function publishErrorToast(message: string): void {
  listener?.(message, { tone: 'error' });
}

/** Same delivery mechanism as `publishErrorToast`, for any tone (e.g. the green "Saved to {trip}" quick-save confirmation). */
export function publishToast(message: string, options?: ToastOptions): void {
  listener?.(message, options);
}
