import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { registerToastListener } from './toastBus';

const TOAST_AUTO_DISMISS_MS = 3000;

export type ToastTone = 'success' | 'error';

export interface ToastAction {
  /** Button label, e.g. "Retry". */
  label: string;
  onAction: () => void;
}

export interface ToastOptions {
  /** Defaults to `'error'` — most toasts are failure notices; opt into `'success'` explicitly. */
  tone?: ToastTone;
  action?: ToastAction;
}

interface ToastItem {
  id: number;
  message: string;
  tone: ToastTone;
  action?: ToastAction;
}

interface ToastContextValue {
  /** Shows a popup with the given message; defaults to the error tone, optionally with an action button (e.g. Retry). */
  showToast: (message: string, options?: ToastOptions) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

/** Tone-specific styling — success is green with `role="status"`, error is red with `role="alert"` (WCAG: errors need assertive announcement, confirmations don't). */
const TONE_STYLES: Record<
  ToastTone,
  { container: string; text: string; icon: string; iconColor: string; role: 'status' | 'alert' }
> = {
  success: {
    container: 'bg-green-50 border border-green-300',
    text: 'text-green-900',
    icon: 'check_circle',
    iconColor: 'text-green-600',
    role: 'status',
  },
  error: {
    container: 'bg-error-container border border-error/30',
    text: 'text-on-error-container',
    icon: 'error',
    iconColor: 'text-error',
    role: 'alert',
  },
};

/** App-wide popup: shown automatically for query/mutation failures, or explicitly via useToast().showToast() for direct API calls (login, register, create-trip, set-dates, quick-save). */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);

  const dismissToast = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const showToast = useCallback(
    (message: string, options?: ToastOptions) => {
      const id = nextId.current++;
      const tone = options?.tone ?? 'error';
      setToasts((current) => [...current, { id, message, tone, action: options?.action }]);
      setTimeout(() => dismissToast(id), TOAST_AUTO_DISMISS_MS);
    },
    [dismissToast],
  );

  useEffect(() => {
    registerToastListener(showToast);
    return () => registerToastListener(null);
  }, [showToast]);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="fixed top-4 right-4 z-[1000] flex flex-col gap-2 max-w-[360px]">
        {toasts.map((toast) => {
          const styles = TONE_STYLES[toast.tone];
          return (
            <div
              key={toast.id}
              className={`toast ${styles.container} ${styles.text} rounded-lg px-4 py-3 flex items-start gap-3 shadow-lg`}
              role={styles.role}
            >
              <span
                className={`material-symbols-outlined text-[20px] flex-shrink-0 ${styles.iconColor}`}
                aria-hidden="true"
              >
                {styles.icon}
              </span>
              <span className="text-label-md font-label-md flex-grow">{toast.message}</span>
              <div className="flex items-center gap-2 flex-shrink-0">
                {toast.action && (
                  <button
                    type="button"
                    className="font-label-md underline hover:no-underline"
                    onClick={() => {
                      toast.action?.onAction();
                      dismissToast(toast.id);
                    }}
                  >
                    {toast.action.label}
                  </button>
                )}
                <button
                  type="button"
                  className="opacity-70 hover:opacity-100 transition-opacity"
                  onClick={() => dismissToast(toast.id)}
                  aria-label="Dismiss notification"
                >
                  <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                    close
                  </span>
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return ctx;
}
