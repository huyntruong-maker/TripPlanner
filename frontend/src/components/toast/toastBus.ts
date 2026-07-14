type ToastListener = (message: string) => void;

let listener: ToastListener | null = null;

/** Lets non-React code (QueryClient's error handlers) publish a toast without a React context. */
export function registerToastListener(handler: ToastListener | null): void {
  listener = handler;
}

export function publishErrorToast(message: string): void {
  listener?.(message);
}
