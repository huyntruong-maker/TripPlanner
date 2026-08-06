import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigationType, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../../auth/AuthContext';
import { AttractionsGrid } from '../components/AttractionsGrid';
import {
  LOCATION_LISTBOX_ID,
  LocationSuggestionList,
  locationOptionId,
} from '../components/LocationSuggestionList';
import { useDebouncedValue } from '../hooks/useDebouncedValue';
import { useLocationSearch } from '../hooks/useLocationSearch';
import { saveLastDiscoverSearch } from '../lib/discoverSearchStorage';
import {
  locationFromSearchParams,
  locationToSearchParams,
} from '../lib/locationSearchParams';
import type { LocationSuggestion } from '../../../types';

const SEARCH_DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 2;
const MAX_SUGGESTIONS = 5;

/** Public page — users can browse destinations and attractions before logging in. */
export function SearchPage() {
  const { isAuthenticated } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigationType = useNavigationType();

  const [selectedLocation, setSelectedLocation] = useState<LocationSuggestion | null>(() =>
    locationFromSearchParams(searchParams),
  );
  const [query, setQuery] = useState(() => selectedLocation?.displayName ?? '');
  const [isDropdownDismissed, setIsDropdownDismissed] = useState(() => Boolean(selectedLocation));
  const [activeIndex, setActiveIndex] = useState(-1);
  const debouncedQuery = useDebouncedValue(query, SEARCH_DEBOUNCE_MS);

  const locationsQuery = useLocationSearch(debouncedQuery);

  const suggestions = useMemo(
    () => (locationsQuery.data?.items ?? []).slice(0, MAX_SUGGESTIONS),
    [locationsQuery.data],
  );

  // SearchPage stays mounted across same-route history entries, so re-hydrate from the URL on POP.
  useEffect(() => {
    if (navigationType !== 'POP') return;
    const restored = locationFromSearchParams(searchParams);
    setSelectedLocation(restored);
    setQuery(restored?.displayName ?? '');
    setIsDropdownDismissed(Boolean(restored));
    setActiveIndex(-1);
  }, [navigationType, searchParams]);

  const discoverSearch = searchParams.toString() ? `?${searchParams.toString()}` : '';

  // Fallback for BackToSearchButton when history/Link state is lost (e.g. a dev-server reload).
  useEffect(() => {
    if (discoverSearch) {
      saveLastDiscoverSearch(discoverSearch);
    }
  }, [discoverSearch]);

  function handleQueryChange(nextQuery: string) {
    setQuery(nextQuery);
    setIsDropdownDismissed(false);
    setActiveIndex(-1);
    if (selectedLocation) {
      setSelectedLocation(null);
      setSearchParams(new URLSearchParams(), { replace: true });
    }
  }

  function handleSelectLocation(location: LocationSuggestion) {
    setSelectedLocation(location);
    setQuery(location.displayName);
    setIsDropdownDismissed(true);
    setActiveIndex(-1);
    // Pushes (not replaces) so a new search gets its own Back stop, dropping stale filter params.
    setSearchParams(locationToSearchParams(location));
  }

  const showDropdown =
    !selectedLocation && !isDropdownDismissed && debouncedQuery.trim().length >= MIN_QUERY_LENGTH;
  const isListboxOpen = showDropdown && suggestions.length > 0;

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (!isListboxOpen) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setActiveIndex((current) => (current + 1) % suggestions.length);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setActiveIndex((current) => (current - 1 + suggestions.length) % suggestions.length);
    } else if (event.key === 'Enter') {
      if (activeIndex >= 0 && activeIndex < suggestions.length) {
        event.preventDefault();
        handleSelectLocation(suggestions[activeIndex]);
      }
    } else if (event.key === 'Escape') {
      setIsDropdownDismissed(true);
      setActiveIndex(-1);
    }
  }

  // Once a location is picked the page is about browsing its attractions, so the hero collapses to
  // a slim bar pinned under the header: searching another city no longer needs a scroll back to
  // the top, and the reclaimed height brings results above the fold. One input instance either
  // way, so switching modes can't remount it or drop focus.
  const isBrowsing = selectedLocation !== null;

  return (
    <div className="space-y-section-gap">
      <section
        className={
          isBrowsing ? 'sticky top-[var(--app-header-height)] z-40 py-3 bg-background' : undefined
        }
      >
        <div
          className={
            isBrowsing
              ? 'bg-surface-container-lowest rounded-xl px-4 py-3 elevation-l1 border border-outline-variant/30'
              : 'bg-surface-container-lowest rounded-xl p-8 elevation-l1 max-w-3xl mx-auto border border-outline-variant/30'
          }
        >
          {!isBrowsing && (
            <h1 className="text-display font-display mb-stack-lg text-primary">
              Discover destinations
            </h1>
          )}
          <div className="relative space-y-2">
            <label
              className={
                isBrowsing
                  ? 'sr-only'
                  : 'block text-label-md font-label-md text-on-surface-variant ml-1'
              }
              htmlFor="destination-search"
            >
              Search a city or country
            </label>
            <div className="relative group">
              <span
                className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-outline group-focus-within:text-primary transition-colors"
                aria-hidden="true"
              >
                search
              </span>
              <input
                id="destination-search"
                type="search"
                role="combobox"
                aria-expanded={showDropdown}
                aria-controls={LOCATION_LISTBOX_ID}
                aria-autocomplete="list"
                aria-activedescendant={
                  isListboxOpen && activeIndex >= 0 ? locationOptionId(activeIndex) : undefined
                }
                value={query}
                onChange={(event) => handleQueryChange(event.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="e.g. Paris"
                autoComplete="off"
                className={`w-full pl-12 pr-4 bg-surface border border-outline-variant rounded-xl focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary text-body-md transition-all ${
                  isBrowsing ? 'py-2.5' : 'py-4'
                }`}
              />
            </div>

            {showDropdown && (
              <LocationSuggestionList
                query={locationsQuery}
                suggestions={suggestions}
                activeIndex={activeIndex}
                onSelect={handleSelectLocation}
                onHover={setActiveIndex}
              />
            )}
          </div>

          {!isBrowsing && !isAuthenticated && (
            <p className="mt-stack-lg text-on-surface-variant text-body-md">
              <Link to="/login" className="text-primary font-bold hover:underline">
                Log in
              </Link>{' '}
              to start planning a trip.
            </p>
          )}
        </div>
      </section>

      {selectedLocation && (
        // Keyed by coordinates so switching locations remounts the grid with fresh filters/sort.
        <AttractionsGrid
          key={`${selectedLocation.latitude},${selectedLocation.longitude}`}
          location={selectedLocation}
          discoverSearch={discoverSearch}
        />
      )}
    </div>
  );
}
