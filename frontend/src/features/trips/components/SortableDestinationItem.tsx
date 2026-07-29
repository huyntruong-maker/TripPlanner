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
      className="p-2.5 rounded-lg bg-surface border border-outline-variant/30 flex flex-col gap-2"
    >
      <div className="flex items-start gap-2">
        <button
          type="button"
          className="material-symbols-outlined text-outline cursor-grab active:cursor-grabbing touch-none flex-shrink-0"
          aria-label={`Reorder ${destination.name}`}
          {...attributes}
          {...listeners}
        >
          drag_indicator
        </button>
        {/* No truncation: the full name is the whole point of the list, and the column is narrow
            enough that ellipsis was hiding almost everything past the first word. */}
        <span className="font-label-md text-on-surface flex-grow min-w-0 break-words">
          {destination.name}
        </span>
      </div>
      <button
        type="button"
        className="self-end text-error bg-error-container/20 px-3 py-1 rounded-full text-label-sm hover:bg-error-container/40 transition-colors disabled:opacity-60 disabled:cursor-not-allowed flex-shrink-0"
        onClick={() => onRemove(destination.id)}
        disabled={isRemoving}
      >
        Remove
      </button>
    </li>
  );
}