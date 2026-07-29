/** Router `state` shape attached to a destination card's Link — read back by `BackToSearchButton`. */
export interface DiscoverSearchLinkState {
  discoverSearch?: string;
}

const STORAGE_KEY = 'discover:lastSearch';

/** Restores Discover's search after a dev-reload resets `location.key`; private-browsing storage errors are swallowed as an acceptable degradation. */
export function saveLastDiscoverSearch(search: string): void {
  if (!search) return;
  try {
    sessionStorage.setItem(STORAGE_KEY, search);
  } catch {
    // swallow: acceptable degradation
  }
}

export function readLastDiscoverSearch(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}
