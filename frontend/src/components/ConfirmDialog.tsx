import { useEffect, useRef, type KeyboardEvent, type ReactNode } from 'react';

const PANEL_CLASSES =
  'bg-[#FFF7E6] border border-[#F59E0B] text-[#92400E] rounded-lg p-4 space-y-3';
const CONFIRM_BUTTON_CLASSES =
  'bg-error text-on-error px-4 py-2 rounded-lg font-label-md hover:opacity-90 active:scale-95 transition-all';
const CANCEL_BUTTON_CLASSES =
  'border border-outline-variant text-on-surface-variant px-4 py-2 rounded-lg font-label-md hover:bg-surface-container transition-all';

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: ReactNode;
  confirmLabel: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Small inline confirmation panel shown before an action that would lose
 * data (F3-US2 AC5 — shortening trip dates unschedules items; F3-US7 AC2 —
 * removing a destination). Deliberately not a modal overlay: it stays in the
 * document flow next to the action that triggered it, moves focus to
 * "Cancel" when it appears, and Escape cancels — enough for keyboard/screen
 * reader users without the complexity of a full focus-trapped dialog.
 */
export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const cancelButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (open) {
      cancelButtonRef.current?.focus();
    }
  }, [open]);

  if (!open) {
    return null;
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.stopPropagation();
      onCancel();
    }
  }

  return (
    <div role="group" aria-label={title} onKeyDown={handleKeyDown} className={PANEL_CLASSES}>
      <p className="font-label-md font-semibold">{title}</p>
      <div className="text-label-md">{description}</div>
      <div className="flex gap-3">
        <button
          ref={cancelButtonRef}
          type="button"
          onClick={onCancel}
          className={CANCEL_BUTTON_CLASSES}
        >
          {cancelLabel}
        </button>
        <button type="button" onClick={onConfirm} className={CONFIRM_BUTTON_CLASSES}>
          {confirmLabel}
        </button>
      </div>
    </div>
  );
}
