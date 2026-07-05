type ToastListener = (message: string) => void;

let listener: ToastListener | null = null;

/**
 * Registered once by the mounted <ToastProvider> so non-React code (the
 * QueryClient's global query/mutation error handlers in src/queryClient.ts)
 * can publish a toast without needing a React context of its own.
 */
export function registerToastListener(handler: ToastListener | null): void {
  listener = handler;
}

export function publishErrorToast(message: string): void {
  listener?.(message);
}
