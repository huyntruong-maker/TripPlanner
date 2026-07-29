import type { LocationSuggestion } from '../../../types';

// URL search-param keys — keep the selected location shareable and restorable across navigation.
const QUERY_PARAM = 'q';
const LAT_PARAM = 'lat';
const LNG_PARAM = 'lng';
const NAME_PARAM = 'name';
const LOCATION_TYPE_PARAM = 'locationType';
const COUNTRY_PARAM = 'country';

/** Reconstructs the selected location from the URL, if a full/valid set of params is present. */
export function locationFromSearchParams(params: URLSearchParams): LocationSuggestion | null {
  const displayName = params.get(QUERY_PARAM);
  const latitude = Number(params.get(LAT_PARAM));
  const longitude = Number(params.get(LNG_PARAM));

  if (!displayName || !Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  return {
    name: params.get(NAME_PARAM) ?? displayName,
    displayName,
    latitude,
    longitude,
    locationType: params.get(LOCATION_TYPE_PARAM) ?? '',
    country: params.get(COUNTRY_PARAM) ?? '',
  };
}

export function locationToSearchParams(location: LocationSuggestion): URLSearchParams {
  const params = new URLSearchParams();
  params.set(QUERY_PARAM, location.displayName);
  params.set(LAT_PARAM, String(location.latitude));
  params.set(LNG_PARAM, String(location.longitude));
  params.set(NAME_PARAM, location.name);
  params.set(LOCATION_TYPE_PARAM, location.locationType);
  params.set(COUNTRY_PARAM, location.country);
  return params;
}
