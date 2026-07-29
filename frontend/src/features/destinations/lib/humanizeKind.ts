/** Turns a raw provider slug (e.g. `"SKYSCRAPERS"`) into a human label (`"Skyscrapers"`); use everywhere a category/tag renders, for consistency. */
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
