import { useState } from 'react';
import type { CategoryOption } from '../lib/categoryOptions';

const RATING_OPTIONS = [5, 7, 8, 9] as const;

/**
 * Kept small on purpose: provider categories overlap heavily (religion/churches/cathedrals all
 * describe the same places) and options arrive most-frequent-first, so the tail is mostly noise.
 */
const VISIBLE_CATEGORY_LIMIT = 6;

export const FILTER_SELECT_CLASSES =
  'border border-outline-variant rounded-lg px-3 py-1.5 text-body-md bg-surface focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all';

interface AttractionFiltersProps {
  categoryOptions: CategoryOption[];
  selectedCategoryKeys: string[];
  minRating: number | null;
  onToggleCategory: (key: string) => void;
  onMinRatingChange: (rating: number | null) => void;
  onClear: () => void;
}

/**
 * Filter column. Sticky beside the results from `lg`, collapsed behind a toggle below it.
 * Owns only presentation state (which rows are expanded); the active filters live in the page so
 * they can be mirrored into the URL.
 */
export function AttractionFilters({
  categoryOptions,
  selectedCategoryKeys,
  minRating,
  onToggleCategory,
  onMinRatingChange,
  onClear,
}: AttractionFiltersProps) {
  const [showAllCategories, setShowAllCategories] = useState(false);
  // Only drives the sub-lg collapse; from lg up the column is always shown.
  const [isOpen, setIsOpen] = useState(false);

  const hasMoreCategories = categoryOptions.length > VISIBLE_CATEGORY_LIMIT;
  const visibleCategoryOptions = showAllCategories
    ? categoryOptions
    : categoryOptions.slice(0, VISIBLE_CATEGORY_LIMIT);

  const hasActiveFilters = selectedCategoryKeys.length > 0 || minRating !== null;
  const activeFilterCount = selectedCategoryKeys.length + (minRating !== null ? 1 : 0);
  const selectedCategoryLabels = categoryOptions
    .filter((option) => selectedCategoryKeys.includes(option.key))
    .map((option) => option.label);

  return (
    // Sticks below both the header and the slim search bar pinned under it (~5.5rem: the bar's
    // card plus its wrapper padding), so the filters stay in reach while the results scroll.
    <aside className="mb-stack-lg lg:mb-0 lg:sticky lg:top-[calc(var(--app-header-height)+5.5rem)]">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-expanded={isOpen}
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
          {isOpen ? 'expand_less' : 'expand_more'}
        </span>
      </button>

      <div
        id="attraction-filters"
        className={`${isOpen ? 'block' : 'hidden'} lg:block mt-2 lg:mt-0 bg-surface-container-lowest rounded-xl p-4 elevation-l1 border border-outline-variant/20 space-y-4`}
      >
        {categoryOptions.length > 0 && (
          // role=group rather than fieldset/legend so the "Show all" toggle can sit on the label
          // line instead of competing with the options for attention.
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
            {/* Full-width rows, not wrapped pills: in a 260px column pills wrap raggedly and the
                counts stop lining up for scanning. */}
            <div className="flex flex-col gap-1">
              {visibleCategoryOptions.map((option) => {
                const isSelected = selectedCategoryKeys.includes(option.key);
                return (
                  <button
                    key={option.key}
                    type="button"
                    aria-pressed={isSelected}
                    onClick={() => onToggleCategory(option.key)}
                    className={
                      isSelected
                        ? 'w-full flex items-center justify-between gap-2 px-3 py-1.5 rounded-lg text-label-sm font-label-sm bg-primary text-on-primary transition-colors text-left'
                        : 'w-full flex items-center justify-between gap-2 px-3 py-1.5 rounded-lg text-label-sm font-label-sm text-on-surface-variant hover:bg-surface-container transition-colors text-left'
                    }
                  >
                    <span className="min-w-0 truncate">{option.label}</span>
                    {/* Counts let users skip one-result categories without trying them. */}
                    <span
                      className={isSelected ? 'opacity-80 flex-shrink-0' : 'text-outline flex-shrink-0'}
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
              onMinRatingChange(event.target.value === '' ? null : Number(event.target.value))
            }
            className={`${FILTER_SELECT_CLASSES} w-full`}
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
              onClick={onClear}
              className="ml-auto text-primary font-label-sm underline hover:no-underline"
            >
              Clear all
            </button>
          </div>
        )}
      </div>
    </aside>
  );
}
