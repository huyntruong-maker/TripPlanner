import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../auth/AuthContext';

/**
 * Header avatar that reveals the signed-in email and a log-out action. Lives in the header
 * so logging out is reachable from every page, not just the trip list.
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
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label="Account menu"
        className="w-10 h-10 rounded-full bg-primary-container text-on-primary-container font-bold flex items-center justify-center hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-primary/40 transition-opacity"
      >
        <span aria-hidden="true">{avatarInitial}</span>
      </button>

      {isOpen && (
        <div
          role="menu"
          aria-label="Account"
          className="absolute right-0 mt-2 w-64 bg-surface-container-lowest rounded-xl elevation-l2 border border-outline-variant/30 py-2 z-50"
        >
          <p className="px-4 py-2 text-label-sm text-on-surface-variant break-words">
            Signed in as{' '}
            <strong className="block text-body-md text-on-surface font-semibold">{email}</strong>
          </p>
          <div className="h-px bg-outline-variant/40 my-1" aria-hidden="true" />
          <button
            role="menuitem"
            type="button"
            onClick={() => {
              setIsOpen(false);
              logout();
            }}
            className="w-full flex items-center gap-2 px-4 py-2.5 text-label-md text-on-surface hover:bg-surface-container transition-colors"
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
