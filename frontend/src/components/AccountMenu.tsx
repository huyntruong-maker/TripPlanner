import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../auth/AuthContext';

const PANEL_ID = 'account-panel';

/**
 * Header avatar with a compact popover holding the signed-in address and a log-out action.
 *
 * Deliberately a small popover anchored to the avatar rather than a full-height sheet: there are
 * two lines of content, and a sheet both dwarfs them and hides the nav. It does briefly cover the
 * page beneath — that is what a popover does — so the solid border and overlay shadow are what
 * make it read as a deliberate layer instead of one white card bleeding into another.
 */
export function AccountMenu() {
  const { user, logout } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    function handlePointerDown(event: PointerEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsOpen(false);
      }
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
    <div className="relative" ref={containerRef}>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-expanded={isOpen}
        aria-controls={PANEL_ID}
        aria-label="Account menu"
        className="w-10 h-10 rounded-full bg-primary-container text-on-primary-container font-bold flex items-center justify-center hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-primary/40 transition-opacity"
      >
        <span aria-hidden="true">{avatarInitial}</span>
      </button>

      {isOpen && (
        // No ARIA role on the panel: its contents are a line of text and a single button, so a
        // menu or dialog role would describe a structure that is not there. aria-expanded and
        // aria-controls on the trigger are the whole contract.
        // mt-4 (not mt-2) clears the header's bottom padding — the trigger sits 16px above the
        // header's edge, so a smaller offset leaves the panel overlapping the header band.
        <div
          id={PANEL_ID}
          className="absolute right-0 mt-4 w-64 bg-surface-container-lowest rounded-xl elevation-overlay border border-outline-variant py-2 z-50"
        >
          <p className="px-4 py-2 text-label-sm text-on-surface-variant break-words">
            Signed in as{' '}
            <strong className="block text-body-md text-on-surface font-semibold">{email}</strong>
          </p>

          <div className="h-px bg-outline-variant/40 my-1" aria-hidden="true" />

          <button
            type="button"
            onClick={() => {
              setIsOpen(false);
              logout();
            }}
            className="w-full flex items-center gap-2 px-4 py-2.5 text-label-md text-on-surface hover:bg-surface-container focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary/40 transition-colors"
          >
            <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
              logout
            </span>
            <span>Log out</span>
          </button>
        </div>
      )}
    </div>
  );
}
