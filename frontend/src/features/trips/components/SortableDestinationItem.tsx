import { type CSSProperties } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { TripDestination } from '../../../types';

interface SortableDestinationItemProps {
  destination: TripDestination;
  onRemove: (tripDestinationId: string) => void;
  isRemoving: boolean;
}

export function SortableDestinationItem({ destination, onRemove, isRemoving }: SortableDestinationItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: destination.id,
  });

  const style: CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      className="min-h-[48px] p-2.5 rounded-lg bg-surface border border-outline-variant/30 flex items-center gap-2"
    >
      <button
        type="button"
        className="material-symbols-outlined text-outline cursor-grab active:cursor-grabbing touch-none flex-shrink-0"
        aria-label={`Reorder ${destination.name}`}
        {...attributes}
        {...listeners}
      >
        drag_indicator
      </button>
      <span
        className="font-label-md text-on-surface flex-grow min-w-0 truncate"
        title={destination.name}
      >
        {destination.name}
      </span>
      <button
        type="button"
        className="text-error bg-error-container/20 px-3 py-1 rounded-full text-label-sm hover:bg-error-container/40 transition-colors disabled:opacity-60 disabled:cursor-not-allowed flex-shrink-0"
        onClick={() => onRemove(destination.id)}
        disabled={isRemoving}
      >
        Remove
      </button>
    </li>
  );
}