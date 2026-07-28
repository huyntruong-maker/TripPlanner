import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useAuth } from '../auth/AuthContext';

/**
 * Header avatar that opens an account panel as a sheet against the right edge of the viewport,
 * so it sits in the empty margin beside the centred content instead of landing on top of a card.
 *
 * The sheet is portalled to `document.body` on purpose: the header sets `backdrop-filter`, which
 * makes it the containing block for fixed-position descendants, so a sheet rendered inline would
 * be sized and placed against the 72px header rather than the viewport.
 */
export function AccountMenu() {
  const { user, logout } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const sheetRef = useRef<HTMLDivElement>(null);

  function close() {
    setIsOpen(false);
    triggerRef.current?.focus();
  }

  useEffect(() => {
    if (!isOpen) return;

    function handlePointerDown(event: PointerEvent) {
      const target = event.target as Node;
      // The sheet is portalled, so it is not inside the trigger's subtree — both must be checked.
      if (sheetRef.current?.contains(target) || triggerRef.current?.contains(target)) return;
      setIsOpen(false);
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape') return;
      setIsOpen(false);
      triggerRef.current?.focus();
    }

    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen]);

  const email = user?.email ?? '';
  const avatarInitial = email[0]?.toUpperCase() ?? '?';

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-haspopup="dialog"
        aria-expanded={isOpen}
        aria-label="Account menu"
        className="w-10 h-10 rounded-full bg-primary-container text-on-primary-container font-bold flex items-center justify-center hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-primary/40 transition-opacity"
      >
        <span aria-hidden="true">{avatarInitial}</span>
      </button>

      {isOpen &&
        createPortal(
          <div
            ref={sheetRef}
            role="dialog"
            aria-labelledby="account-sheet-title"
            className="fixed top-0 right-0 z-[1000] h-full w-80 max-w-[85vw] bg-surface-container-lowest border-l border-outline-variant elevation-overlay p-4 flex flex-col gap-3"
          >
            <div className="flex items-center justify-between gap-2">
              <span
                id="account-sheet-title"
                className="text-label-sm font-label-sm text-on-surface-variant"
              >
                Account
              </span>
              <button
                type="button"
                onClick={close}
                aria-label="Close account menu"
                className="w-9 h-9 rounded-full flex items-center justify-center text-on-surface-variant hover:text-on-surface hover:bg-surface-container focus:outline-none focus:ring-2 focus:ring-primary/40 transition-colors"
              >
                <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                  close
                </span>
              </button>
            </div>

            <p className="text-label-sm text-on-surface-variant break-words">
              Signed in as{' '}
              <strong className="block text-body-md text-on-surface font-semibold">{email}</strong>
            </p>

            <div className="h-px bg-outline-variant/40" aria-hidden="true" />

            <button
              type="button"
              onClick={() => {
                setIsOpen(false);
                logout();
              }}
              className="w-full flex items-center gap-2 px-3 py-2.5 rounded-lg text-label-md text-on-surface hover:bg-surface-container focus:outline-none focus:ring-2 focus:ring-primary/40 transition-colors"
            >
              <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                logout
              </span>
              <span>Log out</span>
            </button>
          </div>,
          document.body,
        )}
    </>
  );
}
