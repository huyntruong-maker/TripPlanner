import { useState } from 'react';
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
const MIN_QUERY_LENGTH = 1;

const DROPDOWN_MESSAGE_CLASSES =
  'absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 px-4 py-3 text-body-md';

/**
 * F1/US2 — search a city or country by name, F1/US3 — browse its attractions.
 * Public page — users can browse before logging in (F3/US8).
 */
export function SearchPage() {
  const { isAuthenticated } = useAuth();
  const [query, setQuery] = useState('');
  const [selectedLocation, setSelectedLocation] = useState<LocationSuggestion | null>(null);
  const debouncedQuery = useDebouncedValue(query, SEARCH_DEBOUNCE_MS);

  const locationsQuery = useLocationSearch(debouncedQuery);
  const attractionsQuery = useAttractions(
    selectedLocation
      ? { latitude: selectedLocation.latitude, longitude: selectedLocation.longitude }
      : null,
  );

  function handleQueryChange(nextQuery: string) {
    setQuery(nextQuery);
    setSelectedLocation(null);
  }

  function handleSelectLocation(location: LocationSuggestion) {
    setSelectedLocation(location);
    setQuery(location.displayName);
  }

  const showLocationResults =
    !selectedLocation && debouncedQuery.trim().length >= MIN_QUERY_LENGTH;

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
                value={query}
                onChange={(event) => handleQueryChange(event.target.value)}
                placeholder="e.g. Paris"
                autoComplete="off"
                className="w-full pl-12 pr-4 py-4 bg-surface border border-outline-variant rounded-xl focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary text-body-md transition-all"
              />
            </div>

            {showLocationResults && (
              <LocationResults query={locationsQuery} onSelect={handleSelectLocation} />
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
  onSelect: (location: LocationSuggestion) => void;
}

function LocationResults({ query, onSelect }: LocationResultsProps) {
  if (query.isLoading) {
    return <p className={`${DROPDOWN_MESSAGE_CLASSES} text-on-surface-variant`}>Searching…</p>;
  }

  if (query.isError) {
    return (
      <p className={`${DROPDOWN_MESSAGE_CLASSES} text-error`} role="alert">
        {getApiErrorMessage(query.error, 'Could not search locations.')}
      </p>
    );
  }

  if (!query.data || query.data.items.length === 0) {
    return (
      <p className={`${DROPDOWN_MESSAGE_CLASSES} text-on-surface-variant`}>
        No matching cities or countries.
      </p>
    );
  }

  return (
    <ul className="absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 overflow-hidden">
      {query.data.items.map((location) => (
        <li key={`${location.name}-${location.latitude}-${location.longitude}`}>
          <button
            type="button"
            onClick={() => onSelect(location)}
            className="w-full text-left px-4 py-3 text-body-md text-on-surface hover:bg-surface-container transition-colors"
          >
            {location.displayName}
          </button>
        </li>
      ))}
    </ul>
  );
}

interface AttractionsGridProps {
  location: LocationSuggestion;
  query: UseQueryResult<PagedResult<AttractionSummary>>;
}

function AttractionsGrid({ location, query }: AttractionsGridProps) {
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

      {query.data && query.data.items.length === 0 && (
        <p className="text-on-surface-variant text-body-md">No attractions found.</p>
      )}

      {query.data && query.data.items.length > 0 && (
        <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter">
          {query.data.items.map((attraction) => (
            <AttractionCard key={attraction.providerPlaceId} attraction={attraction} />
          ))}
        </ul>
      )}
    </section>
  );
}
