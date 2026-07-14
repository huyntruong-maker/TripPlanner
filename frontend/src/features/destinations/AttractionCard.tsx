import { Link } from 'react-router-dom';
import type { AttractionSummary } from '../../types';
import { AddToTripControl } from '../trips/AddToTripControl';

interface AttractionCardProps {
  attraction: AttractionSummary;
}

/** One attraction result card: thumbnail, category/tags, and rating when available. */
export function AttractionCard({ attraction }: AttractionCardProps) {
  // Category is already the first tag; exclude it to avoid showing it twice.
  const additionalTags = attraction.tags.filter((tag) => tag !== attraction.category);

  return (
    <li>
      <article className="h-full bg-white rounded-xl overflow-hidden elevation-l1 hover:elevation-l2 transition-all flex flex-col border border-outline-variant/20">
        <Link
          to={`/destinations/${attraction.providerPlaceId}`}
          className="flex flex-col flex-grow group"
        >
          <div className="relative aspect-[4/3] overflow-hidden">
            {attraction.thumbnailUrl ? (
              // Decorative: the heading below already names the destination.
              <img
                className="attraction-thumbnail w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                src={attraction.thumbnailUrl}
                alt=""
              />
            ) : (
              <div
                className="attraction-thumbnail w-full h-full flex items-center justify-center bg-surface-container-high text-on-surface-variant text-label-md"
                aria-hidden="true"
              >
                No photo
              </div>
            )}
            {attraction.rating !== null && (
              <p className="absolute top-4 right-4 glass-effect px-3 py-1 rounded-full flex items-center gap-1">
                <span
                  className="material-symbols-outlined text-[18px] text-on-tertiary-container"
                  style={{ fontVariationSettings: "'FILL' 1" }}
                  aria-hidden="true"
                >
                  star
                </span>
                <span className="text-label-md font-label-md text-on-surface">
                  Rating {attraction.rating.toFixed(1)}
                </span>
              </p>
            )}
          </div>
          <div className="p-stack-lg flex-grow flex flex-col">
            <div className="mb-stack-md">
              {attraction.category && (
                <p className="text-label-sm font-label-sm text-secondary uppercase tracking-wider mb-1">
                  {attraction.category}
                </p>
              )}
              <h3 className="text-headline-md font-headline-md text-primary">{attraction.name}</h3>
            </div>
            {additionalTags.length > 0 && (
              <ul className="flex flex-wrap gap-2 mb-stack-lg">
                {additionalTags.map((tag) => (
                  <li
                    key={tag}
                    className="bg-[#E0F2FE] text-primary px-3 py-1 rounded-full text-label-sm font-label-sm"
                  >
                    {tag}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </Link>
        <div className="px-stack-lg pb-stack-lg mt-auto">
          <AddToTripControl
            destination={{
              providerPlaceId: attraction.providerPlaceId,
              name: attraction.name,
              category: attraction.category,
              thumbnailUrl: attraction.thumbnailUrl,
              lat: attraction.latitude,
              lng: attraction.longitude,
            }}
          />
        </div>
      </article>
    </li>
  );
}
