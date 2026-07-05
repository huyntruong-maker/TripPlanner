import { useState } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { getApiErrorMessage } from '../../api/errors';
import { useLocationSearch } from './useLocationSearch';
import { useAttractions } from './useAttractions';
import { AttractionCard } from './AttractionCard';
import type { AttractionSummary, LocationSuggestion, PagedResult } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 1;

/**
 * F1/US2 — search a city or country by name, F1/US3 — browse its attractions.
 * Public page — users can browse before logging in (F3/US8).
 */
export function SearchPage() {
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
    <div className="search-page">
      <div className="card">
        <h1>Discover destinations</h1>
        <label htmlFor="destination-search">Search a city or country</label>
        <input
          id="destination-search"
          type="search"
          value={query}
          onChange={(event) => handleQueryChange(event.target.value)}
          placeholder="e.g. Paris"
          autoComplete="off"
        />

        {showLocationResults && (
          <LocationResults query={locationsQuery} onSelect={handleSelectLocation} />
        )}

        <p>
          <Link to="/login">Log in</Link> to start planning a trip.
        </p>
      </div>

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
    return <p>Searching…</p>;
  }

  if (query.isError) {
    return (
      <p className="error" role="alert">
        {getApiErrorMessage(query.error, 'Could not search locations.')}
      </p>
    );
  }

  if (!query.data || query.data.items.length === 0) {
    return <p>No matching cities or countries.</p>;
  }

  return (
    <ul className="location-results">
      {query.data.items.map((location) => (
        <li key={`${location.name}-${location.latitude}-${location.longitude}`}>
          <button type="button" onClick={() => onSelect(location)}>
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
    <div className="card">
      <h2>Attractions near {location.displayName}</h2>

      {query.isLoading && <p>Loading attractions…</p>}

      {query.isError && (
        <p className="error" role="alert">
          {getApiErrorMessage(query.error, 'Could not load attractions.')}
        </p>
      )}

      {query.data && query.data.items.length === 0 && <p>No attractions found.</p>}

      {query.data && query.data.items.length > 0 && (
        <ul className="attraction-grid">
          {query.data.items.map((attraction) => (
            <AttractionCard key={attraction.providerPlaceId} attraction={attraction} />
          ))}
        </ul>
      )}
    </div>
  );
}
