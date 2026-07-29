import type { AttractionSummary } from '../../../types';
import { humanizeKind, kindKey } from './humanizeKind';

export interface CategoryOption {
  /** Case-insensitive dedup key (see `kindKey`) — what filtering actually compares against. */
  key: string;
  label: string;
  count: number;
}

/**
 * Raw category/tag values an attraction is associated with. `category` is usually the first tag
 * but not always (see the Louvre fixture), so both are collected and then de-duplicated.
 */
export function categoriesFor(attraction: AttractionSummary): string[] {
  const values = attraction.category ? [attraction.category, ...attraction.tags] : attraction.tags;
  return [...new Set(values)];
}

/** Builds the category filter list from the loaded attractions: case-insensitive dedup, most-frequent first. */
export function buildCategoryOptions(attractions: AttractionSummary[]): CategoryOption[] {
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

/** Applies the active category/rating filters. An empty category selection matches everything. */
export function filterAttractions(
  attractions: AttractionSummary[],
  selectedCategoryKeys: string[],
  minRating: number | null,
): AttractionSummary[] {
  return attractions.filter((attraction) => {
    const matchesCategory =
      selectedCategoryKeys.length === 0 ||
      categoriesFor(attraction).some((raw) => selectedCategoryKeys.includes(kindKey(raw)));
    const matchesRating =
      minRating === null || (attraction.rating !== null && attraction.rating >= minRating);
    return matchesCategory && matchesRating;
  });
}

/** Highest rating first; attractions with no rating sort last. */
export function sortByRating(attractions: AttractionSummary[]): AttractionSummary[] {
  return [...attractions].sort((a, b) => {
    if (a.rating === null && b.rating === null) return 0;
    if (a.rating === null) return 1;
    if (b.rating === null) return -1;
    return b.rating - a.rating;
  });
}
