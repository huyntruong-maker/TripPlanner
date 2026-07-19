import type { ToastAction } from './ToastProvider';

type ToastListener = (message: string, action?: ToastAction) => void;

let listener: ToastListener | null = null;

/** Lets non-React code (QueryClient's error handlers, or components that may render without a ToastProvider ancestor in tests) publish a toast without needing the React context. A no-op when no ToastProvider is mounted. */
export function registerToastListener(handler: ToastListener | null): void {
  listener = handler;
}

export function publishErrorToast(message: string): void {
  listener?.(message);
}

/** Same delivery mechanism as `publishErrorToast`, for non-error confirmations (e.g. the AttractionCard quick-save toast). */
export function publishToast(message: string, action?: ToastAction): void {
  listener?.(message, action);
}
