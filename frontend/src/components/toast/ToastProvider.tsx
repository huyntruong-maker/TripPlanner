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

const TOAST_AUTO_DISMISS_MS = 8000;

export interface ToastAction {
  /** Button label, e.g. "Retry". */
  label: string;
  onAction: () => void;
}

interface ToastItem {
  id: number;
  message: string;
  action?: ToastAction;
}

interface ToastContextValue {
  /** Shows an error popup with the given message, optionally with an action button (e.g. Retry). */
  showToast: (message: string, action?: ToastAction) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

/** App-wide error popup: shown automatically for query/mutation failures, or explicitly via useToast().showToast() for direct API calls (login, register, create-trip, set-dates). */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);

  const dismissToast = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const showToast = useCallback(
    (message: string, action?: ToastAction) => {
      const id = nextId.current++;
      setToasts((current) => [...current, { id, message, action }]);
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
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className="toast bg-error-container border border-error/30 text-on-error-container rounded-lg px-4 py-3 flex items-start justify-between gap-3 shadow-lg"
            role="alert"
          >
            <span className="text-label-md font-label-md">{toast.message}</span>
            <div className="flex items-center gap-2 flex-shrink-0">
              {toast.action && (
                <button
                  type="button"
                  className="text-on-error-container font-label-md underline hover:no-underline"
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
                className="text-on-error-container/70 hover:text-on-error-container transition-colors"
                onClick={() => dismissToast(toast.id)}
                aria-label="Dismiss notification"
              >
                <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                  close
                </span>
              </button>
            </div>
          </div>
        ))}
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
