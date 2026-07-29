import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import type { PlannerColumn } from '../lib/dragDrop';
import { SortableDestinationItem } from './SortableDestinationItem';

interface PlannerColumnViewProps {
  column: PlannerColumn;
  onRemove: (tripDestinationId: string) => void;
  removingId: string | null;
  emptyMessage: string;
}

export function PlannerColumnView({ column, onRemove, removingId, emptyMessage }: PlannerColumnViewProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.id });

  return (
    <div className="min-h-[160px] bg-surface-container-lowest rounded-xl elevation-l1 border border-outline-variant/20 p-4">
      <header className="flex justify-between items-center gap-2 mb-3">
        {/* Accessible name comes from aria-label (full text), not the truncated visible text. */}
        <h3
          className="min-w-0 truncate whitespace-nowrap text-label-md font-label-md text-on-surface"
          title={column.title}
          aria-label={column.title}
        >
          {column.shortTitle}
        </h3>
        <span
          className="bg-surface-container text-primary p-1 rounded-lg flex-shrink-0"
          aria-hidden="true"
        >
          <span className="material-symbols-outlined text-[18px]">
            {column.itineraryDayId === null ? 'bookmark' : 'event'}
          </span>
        </span>
      </header>

      <SortableContext
        items={column.destinations.map((destination) => destination.id)}
        strategy={verticalListSortingStrategy}
      >
        <div
          ref={setNodeRef}
          className={`rounded-lg transition-colors ${
            isOver ? 'bg-primary/10 ring-2 ring-inset ring-primary' : ''
          }`}
        >
          {column.destinations.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 py-3 rounded-lg border-2 border-dashed border-outline-variant/40 text-center">
              <span className="material-symbols-outlined text-lg text-outline" aria-hidden="true">
                {column.itineraryDayId === null ? 'bookmark_border' : 'add_location_alt'}
              </span>
              <p className="text-on-surface-variant text-label-sm px-2">{emptyMessage}</p>
            </div>
          ) : (
            <ul className="space-y-2">
              {column.destinations.map((destination) => (
                <SortableDestinationItem
                  key={destination.id}
                  destination={destination}
                  onRemove={onRemove}
                  isRemoving={removingId === destination.id}
                />
              ))}
            </ul>
          )}
        </div>
      </SortableContext>
    </div>
  );
}