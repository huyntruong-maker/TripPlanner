import type { ToastOptions } from './ToastProvider';

type ToastListener = (message: string, options?: ToastOptions) => void;

let listener: ToastListener | null = null;

// lets non-React code (e.g. QueryClient's error handlers) publish a toast without the React context; no-op if no ToastProvider is mounted
export function registerToastListener(handler: ToastListener | null): void {
  listener = handler;
}

export function publishErrorToast(message: string): void {
  listener?.(message, { tone: 'error' });
}

export function publishToast(message: string, options?: ToastOptions): void {
  listener?.(message, options);
}
