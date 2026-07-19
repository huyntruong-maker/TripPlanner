import { Link } from 'react-router-dom';
import type { AttractionSummary } from '../../types';
import { AddToTripControl } from '../trips/AddToTripControl';
import { QuickSaveControl } from '../trips/QuickSaveControl';
import type { DiscoverSearchLinkState } from './discoverSearchStorage';
import { humanizeKind } from './humanizeKind';

interface AttractionCardProps {
  attraction: AttractionSummary;
  /** Current Discover search string; threaded onto the destination Link as router state so
   * "Back to search" can restore this exact search (see `discoverSearchStorage.ts`). */
  discoverSearch: string;
}

const MAX_VISIBLE_TAGS = 3;

/**
 * One attraction result card: thumbnail, category/tags, rating, and two distinct save actions —
 * a hover/focus "Save place" icon (trip-only, straight to Saved Places) and the full "Add to
 * Trip" control in the footer (trip + day picker).
 */
export function AttractionCard({ attraction, discoverSearch }: AttractionCardProps) {
  // Category is already the first tag; exclude it to avoid showing it twice.
  const additionalTags = attraction.tags.filter((tag) => tag !== attraction.category);
  const visibleTags = additionalTags.slice(0, MAX_VISIBLE_TAGS);
  const hiddenTagCount = additionalTags.length - visibleTags.length;
  const destination = {
    providerPlaceId: attraction.providerPlaceId,
    name: attraction.name,
    category: attraction.category,
    thumbnailUrl: attraction.thumbnailUrl,
    lat: attraction.latitude,
    lng: attraction.longitude,
  };

  return (
    <li className="h-full">
      <article className="group relative h-full bg-white rounded-xl elevation-l1 hover:elevation-l2 transition-all duration-300 flex flex-col border border-outline-variant/20">
        {/* Sibling of the destination Link (not nested in it) so this button doesn't trigger navigation. */}
        <QuickSaveControl className="absolute top-3 left-3 z-20" destination={destination} />

        <Link
          to={`/destinations/${attraction.providerPlaceId}`}
          state={discoverSearch ? ({ discoverSearch } satisfies DiscoverSearchLinkState) : undefined}
          className="flex flex-col flex-grow"
        >
          <div className="relative aspect-[4/3] overflow-hidden rounded-t-xl">
            {attraction.thumbnailUrl ? (
              // Decorative: the heading below already names the destination.
              <img
                className="attraction-thumbnail w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                src={attraction.thumbnailUrl}
                alt=""
              />
            ) : (
              <div
                className="attraction-thumbnail w-full h-full flex flex-col items-center justify-center gap-1 bg-surface-container-high text-on-surface-variant"
                aria-hidden="true"
              >
                <span className="material-symbols-outlined text-3xl">image_not_supported</span>
                <span className="text-label-sm font-label-sm">No photo</span>
              </div>
            )}
            {attraction.rating !== null && (
              <p
                className="absolute top-3 right-3 rounded-full bg-on-surface/80 text-on-primary px-2.5 py-1 text-label-sm font-label-sm"
                aria-label={`Rating ${attraction.rating.toFixed(1)} out of 10`}
              >
                <span aria-hidden="true">★ {attraction.rating.toFixed(1)}</span>
              </p>
            )}
          </div>
          <div className="p-stack-lg flex-grow flex flex-col">
            <div className="mb-stack-md">
              {attraction.category && (
                <p className="text-label-sm font-label-sm text-secondary uppercase tracking-wider mb-1">
                  {humanizeKind(attraction.category)}
                </p>
              )}
              <h3 className="text-headline-md font-headline-md text-primary line-clamp-2 min-h-16">
                {attraction.name}
              </h3>
            </div>
            {visibleTags.length > 0 && (
              <ul className="flex flex-wrap gap-2 mb-stack-lg">
                {visibleTags.map((tag) => (
                  <li
                    key={tag}
                    className="bg-[#E0F2FE] text-primary px-3 py-1 rounded-full text-label-sm font-label-sm"
                  >
                    {humanizeKind(tag)}
                  </li>
                ))}
                {hiddenTagCount > 0 && (
                  <li className="bg-surface-container text-on-surface-variant px-3 py-1 rounded-full text-label-sm font-label-sm">
                    +{hiddenTagCount}
                  </li>
                )}
              </ul>
            )}
          </div>
        </Link>
        <div className="px-stack-lg pb-stack-lg mt-auto">
          <AddToTripControl destination={destination} />
        </div>
      </article>
    </li>
  );
}
