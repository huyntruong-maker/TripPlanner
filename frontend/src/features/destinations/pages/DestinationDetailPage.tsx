import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { getDestinationDetail } from '../../../api/destinations';
import { getApiErrorMessage } from '../../../api/errors';
import type { OpeningHours } from '../../../types';
import { AddToTripControl } from '../../trips/components/AddToTripControl';
import { readLastDiscoverSearch, type DiscoverSearchLinkState } from '../lib/discoverSearchStorage';
import { humanizeKind } from '../lib/humanizeKind';
import { MapView } from '../components/MapView';
import { PhotoCarousel } from '../components/PhotoCarousel';

const BACK_LINK_CLASSES = 'inline-flex items-center gap-2 text-primary font-label-md hover:underline';

// falls back to router state, then sessionStorage, when there's no in-app history to pop
function BackToSearchButton() {
  const navigate = useNavigate();
  const location = useLocation();
  // react-router marks the very first history entry with key "default" — nothing to go back to.
  const canGoBack = location.key !== 'default';

  function goBackToSearch() {
    if (canGoBack) {
      navigate(-1);
      return;
    }
    const linkState = location.state as DiscoverSearchLinkState | null;
    const search = linkState?.discoverSearch ?? readLastDiscoverSearch() ?? '';
    navigate(`/${search}`);
  }

  return (
    <button
      type="button"
      onClick={goBackToSearch}
      className={BACK_LINK_CLASSES}
    >
      <span className="material-symbols-outlined" aria-hidden="true">
        arrow_back
      </span>
      Back to search
    </button>
  );
}

export function DestinationDetailPage() {
  const { providerPlaceId } = useParams<{ providerPlaceId: string }>();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['destinations', 'detail', providerPlaceId],
    queryFn: () => getDestinationDetail(providerPlaceId as string),
    enabled: Boolean(providerPlaceId),
  });

  if (isLoading) {
    return (
      <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 max-w-3xl mx-auto border border-outline-variant/30">
        <p className="text-on-surface-variant text-body-md">Loading destination…</p>
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="bg-surface-container-lowest rounded-xl p-8 elevation-l1 max-w-3xl mx-auto border border-outline-variant/30 space-y-stack-md">
        <p className="text-error text-body-md" role="alert">
          {getApiErrorMessage(error, 'Could not load this destination.')}
        </p>
        <BackToSearchButton />
      </div>
    );
  }

  // Category is already the first tag; exclude it to avoid showing it twice.
  const additionalTags = data.tags.filter((tag) => tag !== data.category);
  // Only render the map when the provider actually gave us usable coordinates.
  const hasCoordinates = Number.isFinite(data.latitude) && Number.isFinite(data.longitude);

  return (
    <div className="space-y-8">
      <BackToSearchButton />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-gutter">
        <div className="lg:col-span-8 space-y-8">
          <PhotoCarousel photos={data.photos} destinationName={data.name} />

          <div className="bg-white rounded-xl p-8 elevation-l1 border border-outline-variant/30 space-y-stack-lg">
            <div>
              {data.category && (
                <p className="text-label-sm font-label-sm text-secondary uppercase tracking-wider mb-1">
                  {humanizeKind(data.category)}
                </p>
              )}
              <h1 className="text-headline-lg font-headline-lg text-on-surface mb-2">{data.name}</h1>
              {data.rating !== null && (
                <p className="flex items-center gap-1 text-label-md font-label-md text-on-surface">
                  <span
                    className="material-symbols-outlined text-[18px] text-on-tertiary-container"
                    style={{ fontVariationSettings: "'FILL' 1" }}
                    aria-hidden="true"
                  >
                    star
                  </span>
                  Rating {data.rating.toFixed(1)}
                </p>
              )}
            </div>

            {additionalTags.length > 0 && (
              <ul className="flex flex-wrap gap-stack-sm">
                {additionalTags.map((tag) => (
                  <li
                    key={tag}
                    className="px-4 py-1.5 rounded-full bg-[#E0F2FE] text-primary font-label-md"
                  >
                    {humanizeKind(tag)}
                  </li>
                ))}
              </ul>
            )}

            {data.description && (
              <p className="text-body-lg font-body-lg text-on-surface leading-relaxed">
                {data.description}
              </p>
            )}

            {data.website && (
              <p>
                <a
                  href={data.website}
                  target="_blank"
                  rel="noreferrer"
                  className="text-primary font-label-md hover:underline"
                >
                  Visit website <span aria-hidden="true">↗</span>
                </a>
              </p>
            )}

            <div className="pt-stack-lg border-t border-outline-variant/30">
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
          </div>
        </div>

        <aside className="lg:col-span-4">
          <div className="bg-white rounded-xl p-6 elevation-l1 border border-outline-variant/30">
            <h2 className="font-headline-md text-headline-md text-on-surface mb-6">Visiting Info</h2>
            <ul className="space-y-4">
              <OpeningHoursSection openingHours={data.openingHours} />
              {data.address && (
                <li className="flex items-start gap-3">
                  <span className="material-symbols-outlined text-primary mt-1" aria-hidden="true">
                    location_on
                  </span>
                  <div>
                    <p className="font-label-md text-on-surface">Address</p>
                    <p className="text-on-surface-variant text-label-sm">{data.address}</p>
                  </div>
                </li>
              )}
              {data.address && (
                <li className="flex items-start gap-3">
                  <span className="material-symbols-outlined text-primary mt-1" aria-hidden="true">
                    map
                  </span>
                  <div>
                    <a
                      href={`https://www.google.com/maps/search/?api=1&query=${data.latitude},${data.longitude}`}
                      target="_blank"
                      rel="noreferrer"
                      className="font-label-md text-primary hover:underline"
                    >
                      View on map
                    </a>
                  </div>
                </li>
              )}
            </ul>
          </div>

          {hasCoordinates && (
            <div className="bg-white rounded-xl p-6 elevation-l1 border border-outline-variant/30 mt-6">
              <h2 className="font-headline-md text-headline-md text-on-surface mb-4">Location</h2>
              <MapView
                latitude={data.latitude}
                longitude={data.longitude}
                name={data.name}
                label={data.address}
              />
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}

function OpeningHoursSection({ openingHours }: { openingHours: OpeningHours | null }) {
  if (!openingHours) {
    return (
      <li className="flex items-start gap-3">
        <span className="material-symbols-outlined text-primary mt-1" aria-hidden="true">
          schedule
        </span>
        <div>
          <p className="font-label-md text-on-surface">Opening hours</p>
          <p className="text-on-surface-variant text-label-sm">Opening hours not available.</p>
        </div>
      </li>
    );
  }

  return (
    <li className="flex items-start gap-3">
      <span className="material-symbols-outlined text-primary mt-1" aria-hidden="true">
        schedule
      </span>
      <div>
        <p className="font-label-md text-on-surface">Opening hours</p>
        {openingHours.displayText && (
          <p className="text-on-surface-variant text-label-sm">{openingHours.displayText}</p>
        )}
        {openingHours.isOpenNow !== null && (
          <p
            className={
              openingHours.isOpenNow
                ? 'text-primary text-label-sm font-semibold'
                : 'text-error text-label-sm font-semibold'
            }
          >
            {openingHours.isOpenNow ? 'Open now' : 'Closed now'}
          </p>
        )}
        {openingHours.weekdayText.length > 0 && (
          <ul className="mt-1 space-y-0.5">
            {openingHours.weekdayText.map((line) => (
              <li key={line} className="text-on-surface-variant text-label-sm">
                {line}
              </li>
            ))}
          </ul>
        )}
      </div>
    </li>
  );
}
