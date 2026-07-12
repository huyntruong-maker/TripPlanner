const STORAGE_KEY = 'tripplanner.pendingAddToTrip';

/**
 * Records the destination a logged-out user was trying to add to a trip,
 * just before sending them to /login (F3-US8 AC5 — best-effort resume).
 * Full auto-replay isn't possible — the user still has to pick a trip and a
 * day — so this only remembers *which* destination to re-open the picker
 * for once they're back.
 */
export function rememberPendingAddToTrip(providerPlaceId: string): void {
  sessionStorage.setItem(STORAGE_KEY, providerPlaceId);
}

/**
 * Reads the pending intent (if any) and always clears it, so a stale intent
 * never leaks onto an unrelated destination later. Returns true only when
 * the stored intent matches the given destination.
 */
export function consumePendingAddToTrip(providerPlaceId: string): boolean {
  const stored = sessionStorage.getItem(STORAGE_KEY);
  if (stored === null) {
    return false;
  }
  sessionStorage.removeItem(STORAGE_KEY);
  return stored === providerPlaceId;
}
