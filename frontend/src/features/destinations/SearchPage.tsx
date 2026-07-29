import { useEffect, useMemo, useState } from 'react';
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

const QUERY_PARAM = 'q';
const LAT_PARAM = 'lat';
const LNG_PARAM = 'lng';
const NAME_PARAM = 'name';
const LOCATION_TYPE_PARAM = 'locationType';
const COUNTRY_PARAM = 'country';

function optionId(index: number): string {
  return `location-option-${index}`;
}

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

  // SearchPage stays mounted across same-route history entries, so re-hydrate from the URL on POP navigations.
  useEffect(() => {
    if (navigationType !== 'POP') return;
    const restored = locationFromSearchParams(searchParams);
    setSelectedLocation(restored);
    setQuery(restored?.displayName ?? '');
    setIsDropdownDismissed(Boolean(restored));
    setActiveIndex(-1);
  }, [navigationType, searchParams]);

  const discoverSearch = searchParams.toString() ? `?${searchParams.toString()}` : '';

  // fallback for BackToSearchButton when history/Link state is lost (e.g. a dev-server reload)
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
    // pushes (not replaces) so a new search gets its own Back stop, dropping stale filter/sort params
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

  // Once a location is picked the page is about browsing its attractions, so the hero collapses
  // to a slim bar pinned under the header: searching another city no longer needs a scroll back
  // to the top, and the reclaimed height brings the results above the fold.
  const isBrowsing = selectedLocation !== null;

  return (
    <div className="space-y-section-gap">
      <section
        className={
          isBrowsing
            ? 'sticky top-[var(--app-header-height)] z-40 py-3 bg-background'
            : undefined
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
                  isListboxOpen && activeIndex >= 0 ? optionId(activeIndex) : undefined
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
              <LocationResults
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
        // keyed by coordinates so switching locations remounts the grid with fresh filters/sort
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
  /** Current Discover URL search string; threaded onto each card's destination Link as router state so Back-navigation can restore it. */
  discoverSearch: string;
}

type SortOrder = 'recommended' | 'rating';

const RATING_OPTIONS = [5, 7, 8, 9] as const;
// Kept small: provider categories overlap heavily (e.g. religion/churches/cathedrals), and options are sorted most-frequent-first, so the long tail is mostly noise.
const VISIBLE_CATEGORY_LIMIT = 6;

const SELECT_CLASSES =
  'border border-outline-variant rounded-lg px-3 py-1.5 text-body-md bg-surface focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all';

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

// category is usually the first tag but not always, so both are collected separately
function categoriesFor(attraction: AttractionSummary): string[] {
  const values = attraction.category ? [attraction.category, ...attraction.tags] : attraction.tags;
  return [...new Set(values)];
}

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
  // Only drives the sub-lg collapse; from lg up the sidebar is always shown.
  const [isFilterOpen, setIsFilterOpen] = useState(false);

  // Writes only when the URL actually differs, so StrictMode's double-invoked effect can't wipe a freshly-pushed ?q/lat.
  useEffect(() => {
    const next = new URLSearchParams(searchParams);
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
    if (next.toString() !== searchParams.toString()) {
      setSearchParams(next, { replace: true });
    }
  }, [selectedCategoryKeys, minRating, sortOrder, searchParams, setSearchParams]);

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
    return [...filteredAttractions].sort((a, b) => {
      if (a.rating === null && b.rating === null) return 0;
      if (a.rating === null) return 1;
      if (b.rating === null) return -1;
      return b.rating - a.rating;
    });
  }, [filteredAttractions, sortOrder]);

  const hasActiveFilters = selectedCategoryKeys.length > 0 || minRating !== null;
  const activeFilterCount = selectedCategoryKeys.length + (minRating !== null ? 1 : 0);
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
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-headline-lg font-headline-lg text-on-surface">
          Attractions near {location.displayName}
        </h2>
        {query.data && attractions.length > 0 && (
          // Sort lives next to the result count — it describes the list, not the filter set.
          <div className="flex items-center gap-3">
            <p className="text-label-md font-label-md text-on-surface-variant">
              {hasActiveFilters
                ? `${sortedAttractions.length} of ${attractions.length} attractions`
                : `${attractions.length} attractions`}
            </p>
            <label
              htmlFor="attractions-sort-order"
              className="text-label-sm font-label-sm text-on-surface-variant whitespace-nowrap"
            >
              Sort by
            </label>
            <select
              id="attractions-sort-order"
              value={sortOrder}
              onChange={(event) => setSortOrder(event.target.value as SortOrder)}
              className={SELECT_CLASSES}
            >
              <option value="recommended">Recommended</option>
              <option value="rating">Highest rating</option>
            </select>
          </div>
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
        // Filters in their own column rather than a full-width bar: at 1200px of content the old
        // `justify-between` row left ~300px of dead space between the chips and the rating, and
        // everything scrolled out of reach. A sticky column also has room for more filters later.
        <div className="lg:grid lg:grid-cols-[260px_1fr] lg:gap-gutter lg:items-start">
          {/* Sticks below both the header and the slim search bar that is pinned under it
              (~5.5rem: the bar's card plus its wrapper padding), so filters stay in reach. */}
          <aside className="mb-stack-lg lg:mb-0 lg:sticky lg:top-[calc(var(--app-header-height)+5.5rem)]">
            <button
              type="button"
              onClick={() => setIsFilterOpen((open) => !open)}
              aria-expanded={isFilterOpen}
              aria-controls="attraction-filters"
              className="lg:hidden w-full flex items-center justify-between gap-2 bg-surface-container-lowest rounded-xl px-4 py-3 elevation-l1 border border-outline-variant/20 text-label-md font-label-md text-on-surface"
            >
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                  tune
                </span>
                Filters
                {activeFilterCount > 0 && (
                  <span className="px-2 py-0.5 rounded-full bg-primary text-on-primary text-label-sm">
                    {activeFilterCount}
                  </span>
                )}
              </span>
              <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                {isFilterOpen ? 'expand_less' : 'expand_more'}
              </span>
            </button>

            <div
              id="attraction-filters"
              className={`${isFilterOpen ? 'block' : 'hidden'} lg:block mt-2 lg:mt-0 bg-surface-container-lowest rounded-xl p-4 elevation-l1 border border-outline-variant/20 space-y-4`}
            >
              {categoryOptions.length > 0 && (
                // role=group rather than fieldset/legend so the "Show all" toggle can sit on the
                // label line instead of competing with the options for attention.
                <div role="group" aria-labelledby="category-filter-label">
                  <div className="flex items-center justify-between gap-3 mb-2">
                    <span
                      id="category-filter-label"
                      className="text-label-sm font-label-sm text-on-surface-variant"
                    >
                      Category
                    </span>
                    {hasMoreCategories && (
                      <button
                        type="button"
                        onClick={() => setShowAllCategories((current) => !current)}
                        className="text-label-sm font-label-sm text-primary hover:underline"
                      >
                        {showAllCategories ? 'Show less' : `Show all (${categoryOptions.length})`}
                      </button>
                    )}
                  </div>
                  {/* Full-width rows, not wrapped pills: in a 260px column pills wrap raggedly and
                      the counts no longer line up for scanning. */}
                  <div className="flex flex-col gap-1">
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
                              ? 'w-full flex items-center justify-between gap-2 px-3 py-1.5 rounded-lg text-label-sm font-label-sm bg-primary text-on-primary transition-colors text-left'
                              : 'w-full flex items-center justify-between gap-2 px-3 py-1.5 rounded-lg text-label-sm font-label-sm text-on-surface-variant hover:bg-surface-container transition-colors text-left'
                          }
                        >
                          <span className="min-w-0 truncate">{option.label}</span>
                          {/* Counts let users skip one-result categories without trying them. */}
                          <span
                            className={
                              isSelected ? 'opacity-80 flex-shrink-0' : 'text-outline flex-shrink-0'
                            }
                          >
                            {option.count}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}

              <div className="space-y-1 border-t border-outline-variant/20 pt-4">
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
                  className={`${SELECT_CLASSES} w-full`}
                >
                  <option value="">Any rating</option>
                  {RATING_OPTIONS.map((rating) => (
                    <option key={rating} value={rating}>
                      {rating}+ rating
                    </option>
                  ))}
                </select>
              </div>

              {hasActiveFilters && (
                <div className="flex flex-wrap items-center gap-2 border-t border-outline-variant/20 pt-4 text-label-sm font-label-sm text-on-surface-variant">
                  <span>Active filters:</span>
                  {selectedCategoryLabels.map((label) => (
                    <span
                      key={label}
                      className="px-2 py-0.5 rounded-full bg-primary/10 text-primary"
                    >
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
          </aside>

          <div className="space-y-stack-lg min-w-0">
            {sortedAttractions.length === 0 && (
              <p className="text-on-surface-variant text-body-md">
                No attractions match the selected filters.
              </p>
            )}

            {sortedAttractions.length > 0 && (
              // 2 columns beside the sidebar until xl, where the grid is wide enough for 3.
              <ul className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-gutter items-stretch">
                {sortedAttractions.map((attraction) => (
                  <AttractionCard
                    key={attraction.providerPlaceId}
                    attraction={attraction}
                    discoverSearch={discoverSearch}
                  />
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
