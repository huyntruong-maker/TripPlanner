import { Link } from 'react-router-dom';
import type { AttractionSummary } from '../../types';
import { AddToTripControl } from '../trips/AddToTripControl';

interface AttractionCardProps {
  attraction: AttractionSummary;
}

/** F1/US3 — one attraction result: thumbnail/placeholder, category/tags, rating when available. */
export function AttractionCard({ attraction }: AttractionCardProps) {
  return (
    <li className="attraction-card">
      <Link to={`/destinations/${attraction.providerPlaceId}`} className="attraction-card-link">
        {attraction.thumbnailUrl ? (
          // Decorative: the heading right below already names the destination,
          // so an alt here would just duplicate it in the link's accessible name.
          <img className="attraction-thumbnail" src={attraction.thumbnailUrl} alt="" />
        ) : (
          <div className="attraction-thumbnail attraction-thumbnail--placeholder" aria-hidden="true">
            No photo
          </div>
        )}
        <div className="attraction-card-body">
          <h3>{attraction.name}</h3>
          {attraction.category && <p className="attraction-category">{attraction.category}</p>}
          {attraction.tags.length > 0 && (
            <ul className="attraction-tags">
              {attraction.tags.map((tag) => (
                <li key={tag}>{tag}</li>
              ))}
            </ul>
          )}
          {attraction.rating !== null && (
            <p className="attraction-rating">Rating {attraction.rating.toFixed(1)}</p>
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
