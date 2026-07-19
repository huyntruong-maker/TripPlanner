/** Router `state` shape attached to a destination card's Link — read back by `BackToSearchButton`. */
export interface DiscoverSearchLinkState {
  discoverSearch?: string;
}

const STORAGE_KEY = 'discover:lastSearch';

/**
 * Persists Discover's current search string (e.g. `"?q=Paris...&lat=...&lng=..."`) so it can be
 * restored even when there's no usable in-app history to go back to — notably after a Vite-dev
 * full reload while sitting on a destination detail page, which resets `location.key` to
 * `'default'` (see `BackToSearchButton` in `DestinationDetailPage.tsx`). Wrapped in try/catch:
 * some browsers throw on storage access in private-browsing modes, and losing this affordance
 * there is an acceptable degradation, not worth crashing the page over.
 */
export function saveLastDiscoverSearch(search: string): void {
  if (!search) return;
  try {
    sessionStorage.setItem(STORAGE_KEY, search);
  } catch {
    // Ignore — see above.
  }
}

export function readLastDiscoverSearch(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}
