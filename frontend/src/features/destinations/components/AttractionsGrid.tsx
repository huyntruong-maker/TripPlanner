import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getApiErrorMessage } from '../../../api/errors';
import type { LocationSuggestion } from '../../../types';
import { ATTRACTIONS_PAGE_SIZE, useAttractions } from '../hooks/useAttractions';
import {
  applyFiltersToSearchParams,
  categoryKeysFromSearchParams,
  minRatingFromSearchParams,
  sortOrderFromSearchParams,
  type SortOrder,
} from '../lib/attractionFilterParams';
import { buildCategoryOptions, filterAttractions, sortByRating } from '../lib/categoryOptions';
import { AttractionCard } from './AttractionCard';
import { AttractionFilters, FILTER_SELECT_CLASSES } from './AttractionFilters';

interface AttractionsGridProps {
  location: LocationSuggestion;
  /** Current Discover URL search string, threaded onto each card's Link as router state so Back can restore it. */
  discoverSearch: string;
}

/** Results for the selected location: heading, sort, the filter column, the card grid and pagination. */
export function AttractionsGrid({ location, discoverSearch }: AttractionsGridProps) {
  const [searchParams, setSearchParams] = useSearchParams();
  const [page, setPage] = useState(1);
  const query = useAttractions({ latitude: location.latitude, longitude: location.longitude }, page);
  const [selectedCategoryKeys, setSelectedCategoryKeys] = useState<string[]>(() =>
    categoryKeysFromSearchParams(searchParams),
  );
  const [minRating, setMinRating] = useState<number | null>(() =>
    minRatingFromSearchParams(searchParams),
  );
  const [sortOrder, setSortOrder] = useState<SortOrder>(() =>
    sortOrderFromSearchParams(searchParams),
  );

  // Writes only when the URL actually differs, so StrictMode's double-invoked effect can't wipe a
  // freshly-pushed ?q/lat with a stale value.
  useEffect(() => {
    const next = applyFiltersToSearchParams(searchParams, {
      categoryKeys: selectedCategoryKeys,
      minRating,
      sortOrder,
    });
    if (next.toString() !== searchParams.toString()) {
      setSearchParams(next, { replace: true });
    }
  }, [selectedCategoryKeys, minRating, sortOrder, searchParams, setSearchParams]);

  const attractions = query.data?.items ?? [];
  const categoryOptions = useMemo(() => buildCategoryOptions(attractions), [attractions]);

  const filteredAttractions = useMemo(
    () => filterAttractions(attractions, selectedCategoryKeys, minRating),
    [attractions, selectedCategoryKeys, minRating],
  );

  const sortedAttractions = useMemo(
    () => (sortOrder === 'recommended' ? filteredAttractions : sortByRating(filteredAttractions)),
    [filteredAttractions, sortOrder],
  );

  const hasActiveFilters = selectedCategoryKeys.length > 0 || minRating !== null;

  // Providers report TotalCount as just this response's item count, not the true match count for
  // the search, so "next page exists" can only be inferred from a full page — not from totalCount.
  const hasPreviousPage = page > 1;
  const hasNextPage = attractions.length === ATTRACTIONS_PAGE_SIZE;
  const showPagination = hasPreviousPage || hasNextPage;

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
          // Sort sits with the result count — it describes the list, not the filter set.
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
              className={FILTER_SELECT_CLASSES}
            >
              <option value="recommended">Recommended</option>
              <option value="rating">Highest rating</option>
            </select>
          </div>
        )}
      </div>

      {query.isLoading && <p className="text-on-surface-variant text-body-md">Loading attractions…</p>}

      {query.isError && (
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(query.error, 'Could not load attractions.')}
        </p>
      )}

      {query.data && attractions.length === 0 && (
        <p className="text-on-surface-variant text-body-md">No attractions found.</p>
      )}

      {query.data && attractions.length > 0 && (
        <div className="lg:grid lg:grid-cols-[260px_1fr] lg:gap-gutter lg:items-start">
          <AttractionFilters
            categoryOptions={categoryOptions}
            selectedCategoryKeys={selectedCategoryKeys}
            minRating={minRating}
            onToggleCategory={toggleCategory}
            onMinRatingChange={setMinRating}
            onClear={clearFilters}
          />

          <div className="space-y-stack-lg min-w-0">
            {sortedAttractions.length === 0 && (
              <p className="text-on-surface-variant text-body-md">
                No attractions match the selected filters.
              </p>
            )}

            {sortedAttractions.length > 0 && (
              // 2 columns beside the filter column until xl, where there is room for 3.
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

      {query.data && showPagination && (
        <nav
          aria-label="Attractions pagination"
          className="flex items-center justify-center gap-4 pt-2"
        >
          <button
            type="button"
            onClick={() => setPage((current) => current - 1)}
            disabled={!hasPreviousPage}
            className="px-4 py-2 rounded-lg border border-outline-variant text-label-md font-label-md text-on-surface disabled:opacity-40 disabled:cursor-not-allowed hover:bg-surface-container-lowest"
          >
            Previous
          </button>
          <p className="text-label-md font-label-md text-on-surface-variant whitespace-nowrap">
            Page {page}
          </p>
          <button
            type="button"
            onClick={() => setPage((current) => current + 1)}
            disabled={!hasNextPage}
            className="px-4 py-2 rounded-lg border border-outline-variant text-label-md font-label-md text-on-surface disabled:opacity-40 disabled:cursor-not-allowed hover:bg-surface-container-lowest"
          >
            Next
          </button>
        </nav>
      )}
    </section>
  );
}
