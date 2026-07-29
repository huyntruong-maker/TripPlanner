import type { UseQueryResult } from '@tanstack/react-query';
import { getApiErrorMessage } from '../../../api/errors';
import type { LocationSuggestion, PagedResult } from '../../../types';

export const LOCATION_LISTBOX_ID = 'location-suggestions-listbox';

const MESSAGE_CLASSES =
  'absolute z-10 top-full left-0 right-0 mt-2 bg-surface-container-lowest border border-outline-variant rounded-lg elevation-l1 px-4 py-3 text-body-md';

/** `aria-activedescendant` on the combobox has to point at the highlighted option by id. */
export function locationOptionId(index: number): string {
  return `location-option-${index}`;
}

interface LocationSuggestionListProps {
  query: UseQueryResult<PagedResult<LocationSuggestion>>;
  suggestions: LocationSuggestion[];
  activeIndex: number;
  onSelect: (location: LocationSuggestion) => void;
  onHover: (index: number) => void;
}

/** The combobox popup: loading/error/empty messages, or the matching cities as a listbox. */
export function LocationSuggestionList({
  query,
  suggestions,
  activeIndex,
  onSelect,
  onHover,
}: LocationSuggestionListProps) {
  if (query.isLoading) {
    return (
      <p className={`${MESSAGE_CLASSES} text-on-surface-variant`} role="status">
        Searching…
      </p>
    );
  }

  if (query.isError) {
    return (
      <p className={`${MESSAGE_CLASSES} text-error`} role="alert">
        {getApiErrorMessage(query.error, 'Could not search locations.')}
      </p>
    );
  }

  if (suggestions.length === 0) {
    return (
      <p className={`${MESSAGE_CLASSES} text-on-surface-variant`}>
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
          id={locationOptionId(index)}
          role="option"
          aria-selected={index === activeIndex}
          // Keeps focus in the input so the combobox doesn't close before the click lands.
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
