import { useEffect, useMemo, useRef, useState } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Link, useNavigationType, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { getApiErrorMessage } from '../../api/errors';
import { useLocationSearch } from './useLocationSearch';
import { useAttractions } from './useAttractions';
import { AttractionCard } from './AttractionCard';
import { saveLastDiscoverSearch } from './discoverSearchStorage';
import { humanizeKind, kindKey } from './humanizeKind';
import type { AttractionSummary, LocationSuggestion, PagedResult } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 2;
const MAX_SUGGESTIONS = 5;
const LOCATION_LISTBOX_ID = 'location-suggestions-listbox';

const DROPDOWN_MESSAGE_CLASSES =
  'absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 px-4 py-3 text-body-md';

// URL search-param keys — keep the selected location (and, in AttractionsGrid, the active
// filters/sort) shareable and restorable across navigation (e.g. Back from the detail page).
const QUERY_PARAM = 'q';
const LAT_PARAM = 'lat';
const LNG_PARAM = 'lng';
const NAME_PARAM = 'name';
const LOCATION_TYPE_PARAM = 'locationType';
const COUNTRY_PARAM = 'country';

function optionId(index: number): string {
  return `location-option-${index}`;
}

/** Reconstructs the selected location from the URL, if a full/valid set of params is present. */
function locationFromSearchParams(params: URLSearchParams): LocationSuggestion | null {
  const displayName = params.get(QUERY_PARAM);
  const latitude = Number(params.get(LAT_PARAM));
  const longitude = Number(params.get(LNG_PARAM));

  if (!displayName || !Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  return {
    name: params.get(NAME_PARAM) ?? displayName,
    displayName,
    latitude,
    longitude,
    locationType: params.get(LOCATION_TYPE_PARAM) ?? '',
    country: params.get(COUNTRY_PARAM) ?? '',
  };
}

function locationToSearchParams(location: LocationSuggestion): URLSearchParams {
  const params = new URLSearchParams();
  params.set(QUERY_PARAM, location.displayName);
  params.set(LAT_PARAM, String(location.latitude));
  params.set(LNG_PARAM, String(location.longitude));
  params.set(NAME_PARAM, location.name);
  params.set(LOCATION_TYPE_PARAM, location.locationType);
  params.set(COUNTRY_PARAM, location.country);
  return params;
}

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
  const attractionsQuery = useAttractions(
    selectedLocation
      ? { latitude: selectedLocation.latitude, longitude: selectedLocation.longitude }
      : null,
  );

  const suggestions = useMemo(
    () => (locationsQuery.data?.items ?? []).slice(0, MAX_SUGGESTIONS),
    [locationsQuery.data],
  );

  // SearchPage stays mounted across same-route (search-param-only) history entries, so back/forward
  // navigation doesn't remount it the way leaving for the destination detail page and returning does.
  // Re-hydrate from the URL on those POP navigations to restore whatever was selected at that point.
  useEffect(() => {
    if (navigationType !== 'POP') return;
    const restored = locationFromSearchParams(searchParams);
    setSelectedLocation(restored);
    setQuery(restored?.displayName ?? '');
    setIsDropdownDismissed(Boolean(restored));
    setActiveIndex(-1);
  }, [navigationType, searchParams]);

  const discoverSearch = searchParams.toString() ? `?${searchParams.toString()}` : '';

  // Belt-and-suspenders restore path for BackToSearchButton (DestinationDetailPage): in-app
  // history/Link state can both be lost (e.g. a Vite-dev full reload while on the detail page
  // resets `location.key` to 'default' and drops any router state), so also keep the last
  // non-empty search string in sessionStorage as a history-independent fallback.
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
    // A genuinely new search — worth its own Back stop — so this pushes (default), dropping any
    // filter/sort params from a previous location (AttractionsGrid re-derives fresh ones below).
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

  return (
    <div className="space-y-section-gap">
      <section>
        <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 max-w-3xl mx-auto border border-outline-variant/30">
          <h1 className="text-display font-display mb-stack-lg text-primary">Discover destinations</h1>
          <div className="relative space-y-2">
            <label
              className="block text-label-md font-label-md text-on-surface-variant ml-1"
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
                  isListboxOpen && activeIndex >= 0 ? optionId(activeIndex) : undefined
                }
                value={query}
                onChange={(event) => handleQueryChange(event.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="e.g. Paris"
                autoComplete="off"
                className="w-full pl-12 pr-4 py-4 bg-surface border border-outline-variant rounded-xl focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary text-body-md transition-all"
              />
            </div>

            {showDropdown && (
              <LocationResults
                query={locationsQuery}
                suggestions={suggestions}
                activeIndex={activeIndex}
                onSelect={handleSelectLocation}
                onHover={setActiveIndex}
              />
            )}
          </div>

          {!isAuthenticated && (
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
        // Keyed by coordinates so switching locations remounts the grid — its filters/sort are
        // meant to be fresh per search, and a clean mount is what makes URL-hydration reliable.
        <AttractionsGrid
          key={`${selectedLocation.latitude},${selectedLocation.longitude}`}
          location={selectedLocation}
          query={attractionsQuery}
          discoverSearch={discoverSearch}
        />
      )}
    </div>
  );
}

interface LocationResultsProps {
  query: UseQueryResult<PagedResult<LocationSuggestion>>;
  suggestions: LocationSuggestion[];
  activeIndex: number;
  onSelect: (location: LocationSuggestion) => void;
  onHover: (index: number) => void;
}

function LocationResults({ query, suggestions, activeIndex, onSelect, onHover }: LocationResultsProps) {
  if (query.isLoading) {
    return (
      <p className={`${DROPDOWN_MESSAGE_CLASSES} text-on-surface-variant`} role="status">
        Searching…
      </p>
    );
  }

  if (query.isError) {
    return (
      <p className={`${DROPDOWN_MESSAGE_CLASSES} text-error`} role="alert">
        {getApiErrorMessage(query.error, 'Could not search locations.')}
      </p>
    );
  }

  if (suggestions.length === 0) {
    return (
      <p className={`${DROPDOWN_MESSAGE_CLASSES} text-on-surface-variant`}>
        No matching cities or countries.
      </p>
    );
  }

  return (
    <ul
      id={LOCATION_LISTBOX_ID}
      role="listbox"
      aria-label="Matching cities and countries"
      className="absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 overflow-hidden"
    >
      {suggestions.map((location, index) => (
        <li
          key={`${location.name}-${location.latitude}-${location.longitude}`}
          id={optionId(index)}
          role="option"
          aria-selected={index === activeIndex}
          onMouseDown={(event) => event.preventDefault()}
          onMouseEnter={() => onHover(index)}
          onClick={() => onSelect(location)}
          className={
            index === activeIndex
              ? 'w-full text-left px-4 py-3 text-body-md text-on-surface bg-surface-container transition-colors cursor-pointer'
              : 'w-full text-left px-4 py-3 text-body-md text-on-surface hover:bg-surface-container transition-colors cursor-pointer'
          }
        >
          <span>{location.displayName}</span>
          <span className="block text-label-sm text-on-surface-variant capitalize" aria-hidden="true">
            {location.locationType}
          </span>
        </li>
      ))}
    </ul>
  );
}

interface AttractionsGridProps {
  location: LocationSuggestion;
  query: UseQueryResult<PagedResult<AttractionSummary>>;
  /** Current Discover URL search string (e.g. `"?q=...&lat=..."`); threaded onto each card's
   * destination Link as router state so Back-navigation can restore this exact search even when
   * in-app history is unavailable (see `BackToSearchButton` in DestinationDetailPage.tsx). */
  discoverSearch: string;
}

type SortOrder = 'recommended' | 'rating';

const RATING_OPTIONS = [5, 7, 8, 9] as const;
/** How many category chips show before the "Show all" toggle. */
const VISIBLE_CATEGORY_LIMIT = 10;

const CATEGORY_PARAM = 'cat';
const RATING_PARAM = 'rating';
const SORT_PARAM = 'sort';

function categoryKeysFromSearchParams(params: URLSearchParams): string[] {
  const raw = params.get(CATEGORY_PARAM);
  return raw ? raw.split(',').filter(Boolean) : [];
}

function minRatingFromSearchParams(params: URLSearchParams): number | null {
  const raw = params.get(RATING_PARAM);
  if (raw === null) return null;
  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
}

function sortOrderFromSearchParams(params: URLSearchParams): SortOrder {
  return params.get(SORT_PARAM) === 'rating' ? 'rating' : 'recommended';
}

interface CategoryOption {
  /** Case-insensitive dedup key (see `kindKey`) — what filtering actually compares against. */
  key: string;
  label: string;
  count: number;
}

/** Raw category/tag values an attraction is associated with (category is usually the first tag, but not always — see Louvre fixture). */
function categoriesFor(attraction: AttractionSummary): string[] {
  const values = attraction.category ? [attraction.category, ...attraction.tags] : attraction.tags;
  return [...new Set(values)];
}

/**
 * Builds the category chip list from the loaded attractions: case-insensitive dedup (e.g.
 * "Art_galleries" / "art_galleries" collapse into one chip; "Bank" / "Banks" stay distinct
 * since they're genuinely different values), most-frequent first.
 */
function buildCategoryOptions(attractions: AttractionSummary[]): CategoryOption[] {
  const byKey = new Map<string, { label: string; count: number }>();

  for (const attraction of attractions) {
    for (const raw of categoriesFor(attraction)) {
      const key = kindKey(raw);
      if (!key) continue;
      const existing = byKey.get(key);
      if (existing) {
        existing.count += 1;
      } else {
        byKey.set(key, { label: humanizeKind(raw), count: 1 });
      }
    }
  }

  return [...byKey.entries()]
    .map(([key, { label, count }]) => ({ key, label, count }))
    .sort((a, b) => b.count - a.count || a.label.localeCompare(b.label));
}

function AttractionsGrid({ location, query, discoverSearch }: AttractionsGridProps) {
  const [searchParams, setSearchParams] = useSearchParams();
  const [selectedCategoryKeys, setSelectedCategoryKeys] = useState<string[]>(() =>
    categoryKeysFromSearchParams(searchParams),
  );
  const [minRating, setMinRating] = useState<number | null>(() => minRatingFromSearchParams(searchParams));
  const [sortOrder, setSortOrder] = useState<SortOrder>(() => sortOrderFromSearchParams(searchParams));
  const [showAllCategories, setShowAllCategories] = useState(false);

  // Mirrors filters/sort into the URL (replace — no history spam) so they survive detail-page
  // back navigation alongside the selected location. This component remounts per location (see
  // the `key` on <AttractionsGrid> in SearchPage), so the lazy initializers above stay accurate.
  // Skips its very first run: on mount, these values were only just *read* from the URL (or from
  // SearchPage's own location-select navigation, which is still landing in the same commit), so
  // writing them straight back here would race that other in-flight navigation.
  const isFirstFilterSyncRef = useRef(true);
  useEffect(() => {
    if (isFirstFilterSyncRef.current) {
      isFirstFilterSyncRef.current = false;
      return;
    }
    setSearchParams(
      (previous) => {
        const next = new URLSearchParams(previous);
        if (selectedCategoryKeys.length > 0) {
          next.set(CATEGORY_PARAM, selectedCategoryKeys.join(','));
        } else {
          next.delete(CATEGORY_PARAM);
        }
        if (minRating !== null) {
          next.set(RATING_PARAM, String(minRating));
        } else {
          next.delete(RATING_PARAM);
        }
        if (sortOrder !== 'recommended') {
          next.set(SORT_PARAM, sortOrder);
        } else {
          next.delete(SORT_PARAM);
        }
        return next;
      },
      { replace: true },
    );
  }, [selectedCategoryKeys, minRating, sortOrder, setSearchParams]);

  const attractions = query.data?.items ?? [];

  const categoryOptions = useMemo(() => buildCategoryOptions(attractions), [attractions]);
  const hasMoreCategories = categoryOptions.length > VISIBLE_CATEGORY_LIMIT;
  const visibleCategoryOptions = showAllCategories
    ? categoryOptions
    : categoryOptions.slice(0, VISIBLE_CATEGORY_LIMIT);

  const filteredAttractions = useMemo(() => {
    return attractions.filter((attraction) => {
      const matchesCategory =
        selectedCategoryKeys.length === 0 ||
        categoriesFor(attraction).some((raw) => selectedCategoryKeys.includes(kindKey(raw)));
      const matchesRating = minRating === null || (attraction.rating !== null && attraction.rating >= minRating);
      return matchesCategory && matchesRating;
    });
  }, [attractions, selectedCategoryKeys, minRating]);

  const sortedAttractions = useMemo(() => {
    if (sortOrder === 'recommended') {
      return filteredAttractions;
    }
    // Highest rating first; missing ratings sort last.
    return [...filteredAttractions].sort((a, b) => {
      if (a.rating === null && b.rating === null) return 0;
      if (a.rating === null) return 1;
      if (b.rating === null) return -1;
      return b.rating - a.rating;
    });
  }, [filteredAttractions, sortOrder]);

  const hasActiveFilters = selectedCategoryKeys.length > 0 || minRating !== null;
  const selectedCategoryLabels = categoryOptions
    .filter((option) => selectedCategoryKeys.includes(option.key))
    .map((option) => option.label);

  function toggleCategory(key: string) {
    setSelectedCategoryKeys((current) =>
      current.includes(key) ? current.filter((item) => item !== key) : [...current, key],
    );
  }

  function clearFilters() {
    setSelectedCategoryKeys([]);
    setMinRating(null);
  }

  return (
    <section className="space-y-stack-lg">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-headline-lg font-headline-lg text-on-surface">
          Attractions near {location.displayName}
        </h2>
        {query.data && attractions.length > 0 && (
          <p className="text-label-md font-label-md text-on-surface-variant">
            {sortedAttractions.length} of {attractions.length} attractions
          </p>
        )}
      </div>

      {query.isLoading && (
        <p className="text-on-surface-variant text-body-md">Loading attractions…</p>
      )}

      {query.isError && (
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(query.error, 'Could not load attractions.')}
        </p>
      )}

      {query.data && attractions.length === 0 && (
        <p className="text-on-surface-variant text-body-md">No attractions found.</p>
      )}

      {query.data && attractions.length > 0 && (
        <div className="bg-surface-container-lowest rounded-xl p-4 elevation-l1 border border-outline-variant/20 space-y-3">
          <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-3">
            {categoryOptions.length > 0 && (
              <fieldset className="min-w-0 flex-1">
                <legend className="text-label-sm font-label-sm text-on-surface-variant mb-2">
                  Category
                </legend>
                <div className="flex flex-wrap gap-2">
                  {visibleCategoryOptions.map((option) => {
                    const isSelected = selectedCategoryKeys.includes(option.key);
                    return (
                      <button
                        key={option.key}
                        type="button"
                        aria-pressed={isSelected}
                        onClick={() => toggleCategory(option.key)}
                        className={
                          isSelected
                            ? 'px-3 py-1.5 rounded-full text-label-sm font-label-sm bg-primary text-on-primary transition-colors'
                            : 'px-3 py-1.5 rounded-full text-label-sm font-label-sm bg-surface border border-outline-variant text-on-surface-variant hover:bg-surface-container transition-colors'
                        }
                      >
                        {option.label}
                      </button>
                    );
                  })}
                  {hasMoreCategories && (
                    <button
                      type="button"
                      onClick={() => setShowAllCategories((current) => !current)}
                      className="px-3 py-1.5 rounded-full text-label-sm font-label-sm text-primary hover:underline"
                    >
                      {showAllCategories ? 'Show less' : `Show all (${categoryOptions.length})`}
                    </button>
                  )}
                </div>
              </fieldset>
            )}

            <div className="flex flex-wrap gap-3 md:flex-shrink-0">
              <div className="space-y-1">
                <label
                  htmlFor="attractions-min-rating"
                  className="block text-label-sm font-label-sm text-on-surface-variant"
                >
                  Minimum rating
                </label>
                <select
                  id="attractions-min-rating"
                  value={minRating ?? ''}
                  onChange={(event) =>
                    setMinRating(event.target.value === '' ? null : Number(event.target.value))
                  }
                  className="border border-outline-variant rounded-lg px-3 py-2 text-body-md bg-surface"
                >
                  <option value="">Any rating</option>
                  {RATING_OPTIONS.map((rating) => (
                    <option key={rating} value={rating}>
                      {rating}+ rating
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-1">
                <label
                  htmlFor="attractions-sort-order"
                  className="block text-label-sm font-label-sm text-on-surface-variant"
                >
                  Sort by
                </label>
                <select
                  id="attractions-sort-order"
                  value={sortOrder}
                  onChange={(event) => setSortOrder(event.target.value as SortOrder)}
                  className="border border-outline-variant rounded-lg px-3 py-2 text-body-md bg-surface"
                >
                  <option value="recommended">Recommended</option>
                  <option value="rating">Highest rating</option>
                </select>
              </div>
            </div>
          </div>

          {hasActiveFilters && (
            <div className="flex flex-wrap items-center gap-2 pt-3 border-t border-outline-variant/20 text-label-sm font-label-sm text-on-surface-variant">
              <span>Active filters:</span>
              {selectedCategoryLabels.map((label) => (
                <span key={label} className="px-2 py-0.5 rounded-full bg-primary/10 text-primary">
                  {label}
                </span>
              ))}
              {minRating !== null && (
                <span className="px-2 py-0.5 rounded-full bg-primary/10 text-primary">
                  {minRating}+ rating
                </span>
              )}
              <button
                type="button"
                onClick={clearFilters}
                className="ml-auto text-primary font-label-sm underline hover:no-underline"
              >
                Clear all
              </button>
            </div>
          )}
        </div>
      )}

      {query.data && attractions.length > 0 && sortedAttractions.length === 0 && (
        <p className="text-on-surface-variant text-body-md">No attractions match the selected filters.</p>
      )}

      {sortedAttractions.length > 0 && (
        <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter items-stretch">
          {sortedAttractions.map((attraction) => (
            <AttractionCard
              key={attraction.providerPlaceId}
              attraction={attraction}
              discoverSearch={discoverSearch}
            />
          ))}
        </ul>
      )}
    </section>
  );
}
