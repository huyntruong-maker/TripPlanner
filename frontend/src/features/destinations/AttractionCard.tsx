import { Link } from 'react-router-dom';
import type { AttractionSummary } from '../../types';
import { AddToTripControl } from '../trips/AddToTripControl';

interface AttractionCardProps {
  attraction: AttractionSummary;
}

/** F1/US3 — one attraction result: thumbnail/placeholder, category/tags, rating when available. */
export function AttractionCard({ attraction }: AttractionCardProps) {
  // The category is also the first tag from the provider; drop it from the tag
  // list so it isn't shown twice (once as the category label, once as a pill).
  const additionalTags = attraction.tags.filter((tag) => tag !== attraction.category);

  return (
    <li className="attraction-card">
      <Link to={`/destinations/${attraction.providerPlaceId}`} className="attraction-card-link">
        <div className="attraction-media">
          {attraction.thumbnailUrl ? (
            // Decorative: the heading right below already names the destination,
            // so an alt here would just duplicate it in the link's accessible name.
            <img className="attraction-thumbnail" src={attraction.thumbnailUrl} alt="" />
          ) : (
            <div className="attraction-thumbnail attraction-thumbnail--placeholder" aria-hidden="true">
              No photo
            </div>
          )}
          {attraction.rating !== null && (
            <p className="attraction-rating attraction-rating--badge">
              Rating {attraction.rating.toFixed(1)}
            </p>
          )}
        </div>
        <div className="attraction-card-body">
          <h3>{attraction.name}</h3>
          {attraction.category && <p className="attraction-category">{attraction.category}</p>}
          {additionalTags.length > 0 && (
            <ul className="attraction-tags">
              {additionalTags.map((tag) => (
                <li key={tag}>{tag}</li>
              ))}
            </ul>
          )}
        </div>
      </Link>
      <div className="attraction-card-actions">
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
    </li>
  );
}
