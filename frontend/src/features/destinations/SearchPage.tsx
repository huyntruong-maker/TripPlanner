import { useMemo, useState } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { getApiErrorMessage } from '../../api/errors';
import { useLocationSearch } from './useLocationSearch';
import { useAttractions } from './useAttractions';
import { AttractionCard } from './AttractionCard';
import type { AttractionSummary, LocationSuggestion, PagedResult } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 2;
const MAX_SUGGESTIONS = 5;
const LOCATION_LISTBOX_ID = 'location-suggestions-listbox';

const DROPDOWN_MESSAGE_CLASSES =
  'absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 px-4 py-3 text-body-md';

function optionId(index: number): string {
  return `location-option-${index}`;
}

/** Public page — users can browse destinations and attractions before logging in. */
export function SearchPage() {
  const { isAuthenticated } = useAuth();
  const [query, setQuery] = useState('');
  const [selectedLocation, setSelectedLocation] = useState<LocationSuggestion | null>(null);
  const [isDropdownDismissed, setIsDropdownDismissed] = useState(false);
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

  function handleQueryChange(nextQuery: string) {
    setQuery(nextQuery);
    setSelectedLocation(null);
    setIsDropdownDismissed(false);
    setActiveIndex(-1);
  }

  function handleSelectLocation(location: LocationSuggestion) {
    setSelectedLocation(location);
    setQuery(location.displayName);
    setIsDropdownDismissed(true);
    setActiveIndex(-1);
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

      {selectedLocation && <AttractionsGrid location={selectedLocation} query={attractionsQuery} />}
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
}

type SortOrder = 'recommended' | 'rating';

const RATING_OPTIONS = [5, 7, 8, 9] as const;

/** Distinct category/tag labels an attraction is associated with (category is usually the first tag, but not always — see Louvre fixture). */
function categoriesFor(attraction: AttractionSummary): string[] {
  const values = attraction.category ? [attraction.category, ...attraction.tags] : attraction.tags;
  return [...new Set(values)];
}

function AttractionsGrid({ location, query }: AttractionsGridProps) {
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [minRating, setMinRating] = useState<number | null>(null);
  const [sortOrder, setSortOrder] = useState<SortOrder>('recommended');

  const attractions = query.data?.items ?? [];

  const availableCategories = useMemo(() => {
    const all = attractions.flatMap(categoriesFor);
    return [...new Set(all)].sort((a, b) => a.localeCompare(b));
  }, [attractions]);

  const filteredAttractions = useMemo(() => {
    return attractions.filter((attraction) => {
      const matchesCategory =
        selectedCategories.length === 0 ||
        categoriesFor(attraction).some((category) => selectedCategories.includes(category));
      const matchesRating = minRating === null || (attraction.rating !== null && attraction.rating >= minRating);
      return matchesCategory && matchesRating;
    });
  }, [attractions, selectedCategories, minRating]);

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

  const hasActiveFilters = selectedCategories.length > 0 || minRating !== null;

  function toggleCategory(category: string) {
    setSelectedCategories((current) =>
      current.includes(category) ? current.filter((item) => item !== category) : [...current, category],
    );
  }

  function clearFilters() {
    setSelectedCategories([]);
    setMinRating(null);
  }

  return (
    <section className="space-y-stack-lg">
      <h2 className="text-headline-lg font-headline-lg text-on-surface">
        Attractions near {location.displayName}
      </h2>

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
        <div className="bg-surface-container-lowest rounded-xl p-6 elevation-l1 border border-outline-variant/20 flex flex-col md:flex-row md:items-start gap-stack-lg">
          {availableCategories.length > 0 && (
            <fieldset className="space-y-2">
              <legend className="text-label-md font-label-md text-on-surface-variant">Category</legend>
              <div className="flex flex-wrap gap-3">
                {availableCategories.map((category) => (
                  <label
                    key={category}
                    className="inline-flex items-center gap-2 text-body-md text-on-surface capitalize"
                  >
                    <input
                      type="checkbox"
                      checked={selectedCategories.includes(category)}
                      onChange={() => toggleCategory(category)}
                    />
                    {category}
                  </label>
                ))}
              </div>
            </fieldset>
          )}

          <div className="space-y-2">
            <label
              htmlFor="attractions-min-rating"
              className="block text-label-md font-label-md text-on-surface-variant"
            >
              Minimum rating
            </label>
            <select
              id="attractions-min-rating"
              value={minRating ?? ''}
              onChange={(event) =>
                setMinRating(event.target.value === '' ? null : Number(event.target.value))
              }
              className="border border-outline-variant rounded-lg px-3 py-2 text-body-md"
            >
              <option value="">Any rating</option>
              {RATING_OPTIONS.map((rating) => (
                <option key={rating} value={rating}>
                  {rating}+ rating
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <label
              htmlFor="attractions-sort-order"
              className="block text-label-md font-label-md text-on-surface-variant"
            >
              Sort by
            </label>
            <select
              id="attractions-sort-order"
              value={sortOrder}
              onChange={(event) => setSortOrder(event.target.value as SortOrder)}
              className="border border-outline-variant rounded-lg px-3 py-2 text-body-md"
            >
              <option value="recommended">Recommended</option>
              <option value="rating">Highest rating</option>
            </select>
          </div>

          {hasActiveFilters && (
            <button
              type="button"
              onClick={clearFilters}
              className="self-start md:self-end text-primary font-label-md hover:underline"
            >
              Clear filters
            </button>
          )}
        </div>
      )}

      {query.data && attractions.length > 0 && sortedAttractions.length === 0 && (
        <p className="text-on-surface-variant text-body-md">No attractions match the selected filters.</p>
      )}

      {sortedAttractions.length > 0 && (
        <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter">
          {sortedAttractions.map((attraction) => (
            <AttractionCard key={attraction.providerPlaceId} attraction={attraction} />
          ))}
        </ul>
      )}
    </section>
  );
}
