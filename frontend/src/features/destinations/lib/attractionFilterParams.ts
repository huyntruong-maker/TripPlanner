export type SortOrder = 'recommended' | 'rating';

// URL search-param keys, so an active filter/sort survives navigation and can be shared.
const CATEGORY_PARAM = 'cat';
const RATING_PARAM = 'rating';
const SORT_PARAM = 'sort';

export function categoryKeysFromSearchParams(params: URLSearchParams): string[] {
  const raw = params.get(CATEGORY_PARAM);
  return raw ? raw.split(',').filter(Boolean) : [];
}

export function minRatingFromSearchParams(params: URLSearchParams): number | null {
  const raw = params.get(RATING_PARAM);
  if (raw === null) return null;
  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
}

export function sortOrderFromSearchParams(params: URLSearchParams): SortOrder {
  return params.get(SORT_PARAM) === 'rating' ? 'rating' : 'recommended';
}

/**
 * Mirrors the active filters/sort into `params`. Defaults are removed rather than written so the
 * URL stays clean, and the caller can compare the result against the current query string to skip
 * a redundant history write.
 */
export function applyFiltersToSearchParams(
  params: URLSearchParams,
  filters: { categoryKeys: string[]; minRating: number | null; sortOrder: SortOrder },
): URLSearchParams {
  const next = new URLSearchParams(params);

  if (filters.categoryKeys.length > 0) {
    next.set(CATEGORY_PARAM, filters.categoryKeys.join(','));
  } else {
    next.delete(CATEGORY_PARAM);
  }

  if (filters.minRating !== null) {
    next.set(RATING_PARAM, String(filters.minRating));
  } else {
    next.delete(RATING_PARAM);
  }

  if (filters.sortOrder !== 'recommended') {
    next.set(SORT_PARAM, filters.sortOrder);
  } else {
    next.delete(SORT_PARAM);
  }

  return next;
}
