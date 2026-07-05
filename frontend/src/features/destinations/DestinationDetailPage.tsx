import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getDestinationDetail } from '../../api/destinations';
import { getApiErrorMessage } from '../../api/errors';
import type { OpeningHours } from '../../types';
import { AddToTripControl } from '../trips/AddToTripControl';
import { PhotoCarousel } from './PhotoCarousel';

/**
 * F2/US1, US2, US4 — full destination detail. Must render even when every
 * optional field (description/photos/address/website/openingHours) is null
 * or empty (docs/API.md "graceful partial data" business rule).
 */
export function DestinationDetailPage() {
  const { providerPlaceId } = useParams<{ providerPlaceId: string }>();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['destinations', 'detail', providerPlaceId],
    queryFn: () => getDestinationDetail(providerPlaceId as string),
    enabled: Boolean(providerPlaceId),
  });

  if (isLoading) {
    return (
      <div className="card">
        <p className="state-message state-message--loading">Loading destination…</p>
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="card">
        <p className="error" role="alert">
          {getApiErrorMessage(error, 'Could not load this destination.')}
        </p>
        <p>
          <Link to="/" className="back-link">
            Back to search
          </Link>
        </p>
      </div>
    );
  }

  // The category is also the first tag from the provider; drop it from the tag
  // list so it isn't shown twice (once as the category label, once as a pill).
  const additionalTags = data.tags.filter((tag) => tag !== data.category);

  return (
    <div className="destination-detail card">
      <p>
        <Link to="/" className="back-link">
          Back to search
        </Link>
      </p>

      <PhotoCarousel photos={data.photos} destinationName={data.name} />

      <h1>{data.name}</h1>
      {data.category && <p className="attraction-category">{data.category}</p>}
      {additionalTags.length > 0 && (
        <ul className="attraction-tags">
          {additionalTags.map((tag) => (
            <li key={tag}>{tag}</li>
          ))}
        </ul>
      )}
      {data.rating !== null && <p className="attraction-rating">Rating {data.rating.toFixed(1)}</p>}

      {data.description && <p className="destination-description">{data.description}</p>}
      {data.address && <p className="destination-address">{data.address}</p>}
      {data.website && (
        <p>
          <a href={data.website} target="_blank" rel="noreferrer">
            Visit website
          </a>
        </p>
      )}

      <OpeningHoursSection openingHours={data.openingHours} />

      <AddToTripControl
        destination={{
          providerPlaceId: data.providerPlaceId,
          name: data.name,
          category: data.category,
          thumbnailUrl: data.photos[0] ?? null,
          lat: data.latitude,
          lng: data.longitude,
        }}
      />
    </div>
  );
}

function OpeningHoursSection({ openingHours }: { openingHours: OpeningHours | null }) {
  if (!openingHours) {
    return (
      <div className="opening-hours">
        <h2>Opening hours</h2>
        <p>Opening hours not available.</p>
      </div>
    );
  }

  return (
    <div className="opening-hours">
      <h2>Opening hours</h2>
      {openingHours.displayText && <p>{openingHours.displayText}</p>}
      {openingHours.isOpenNow !== null && (
        <p className={openingHours.isOpenNow ? 'status-open' : 'status-closed'}>
          {openingHours.isOpenNow ? 'Open now' : 'Closed now'}
        </p>
      )}
      {openingHours.weekdayText.length > 0 && (
        <ul>
          {openingHours.weekdayText.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
