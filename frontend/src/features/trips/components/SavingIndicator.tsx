import { useIsMutating } from '@tanstack/react-query';
import { tripMutationScopeKey } from '../hooks/useTrip';

/** Live save state for the planner: any in-flight mutation scoped to this trip flips it to "Saving…". */
export function SavingIndicator({ tripId }: { tripId: string }) {
  const mutatingCount = useIsMutating({ mutationKey: tripMutationScopeKey(tripId) });

  return (
    <p
      className="flex items-center gap-2 text-label-sm font-label-sm text-on-surface-variant"
      role="status"
      aria-live="polite"
    >
      <span className="material-symbols-outlined text-[16px]" aria-hidden="true">
        {mutatingCount > 0 ? 'sync' : 'cloud_done'}
      </span>
      {mutatingCount > 0 ? 'Saving…' : 'All changes saved'}
    </p>
  );
}
