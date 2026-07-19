/**
 * Turns a raw provider category/tag value (e.g. `"other_buildings_and_structures"`,
 * `"SKYSCRAPERS"`) into a human-readable label (`"Other buildings and structures"`,
 * `"Skyscrapers"`). Use this everywhere a category or tag is rendered — filter chips,
 * the attraction card's category eyebrow and tag chips, and the destination detail page —
 * so labels are consistent across the app instead of leaking raw provider slugs.
 */
export function humanizeKind(value: string): string {
  const normalized = value
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toLowerCase();

  if (!normalized) {
    return normalized;
  }

  return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

/** Case-insensitive dedup key for a raw category/tag value — distinct raw values (e.g. "Bank" vs "Banks") stay distinct; only exact case variants (e.g. "Art_galleries" vs "art_galleries") collapse. */
export function kindKey(value: string): string {
  return value.trim().toLowerCase();
}
