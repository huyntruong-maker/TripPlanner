import { delay, http, HttpResponse } from 'msw';
import type { AttractionSummary, LocationSuggestion } from '../../../types';

const BASE_URL = 'http://localhost:5080/api/v1';

// Query strings the tests type into the search box, each exercising a distinct location-search state.
export const CITY_WITH_ATTRACTIONS_QUERY = 'Paris';
export const CITY_WITH_NO_ATTRACTIONS_QUERY = 'Nowhereville';
export const ATTRACTIONS_ERROR_CITY_QUERY = 'AttractionsErrorCity';
export const LOCATION_SEARCH_ERROR_QUERY = 'LocationSearchError';
export const NO_MATCHING_LOCATIONS_QUERY = 'Zzzzz';
export const SLOW_CITY_QUERY = 'SlowCity';
export const MANY_MATCHES_QUERY = 'ManyMatches';
export const FILTER_SORT_CITY_QUERY = 'FilterSortCity';

const PARIS: LocationSuggestion = {
  name: 'Paris',
  displayName: 'Paris, France',
  latitude: 48.8566,
  longitude: 2.3522,
  locationType: 'city',
  country: 'France',
};

const NOWHEREVILLE: LocationSuggestion = {
  name: 'Nowhereville',
  displayName: 'Nowhereville, Nowhere',
  latitude: 1,
  longitude: 1,
  locationType: 'city',
  country: 'Nowhere',
};

const ATTRACTIONS_ERROR_CITY: LocationSuggestion = {
  name: 'AttractionsErrorCity',
  displayName: 'AttractionsErrorCity, Testland',
  latitude: 2,
  longitude: 2,
  locationType: 'city',
  country: 'Testland',
};

const SLOW_CITY: LocationSuggestion = {
  name: 'SlowCity',
  displayName: 'SlowCity, Testland',
  latitude: 3,
  longitude: 3,
  locationType: 'city',
  country: 'Testland',
};

const EIFFEL_TOWER: AttractionSummary = {
  providerPlaceId: 'W214242',
  name: 'Eiffel Tower',
  category: 'cultural',
  tags: ['cultural', 'landmark'],
  rating: 9.5,
  thumbnailUrl: 'https://example.test/eiffel.jpg',
  latitude: 48.8584,
  longitude: 2.2945,
  address: 'Champ de Mars, Paris, France',
};

const LOUVRE: AttractionSummary = {
  providerPlaceId: 'W214999',
  name: 'Louvre Museum',
  category: 'museum',
  tags: [],
  rating: null,
  thumbnailUrl: null,
  latitude: 48.8606,
  longitude: 2.3376,
  address: null,
};

// F1-US1: a query with more matches than the 5-suggestion cap.
const MANY_MATCHES_LOCATIONS: LocationSuggestion[] = Array.from({ length: 7 }, (_, index) => ({
  name: `ManyMatches${index}`,
  displayName: `ManyMatches ${index}, Testland`,
  latitude: 10 + index,
  longitude: 10 + index,
  locationType: 'city',
  country: 'Testland',
}));

const FILTER_SORT_CITY: LocationSuggestion = {
  name: 'FilterSortCity',
  displayName: 'FilterSortCity, Testland',
  latitude: 20,
  longitude: 20,
  locationType: 'city',
  country: 'Testland',
};

// F1-US4/US5: distinct categories and a mix of ratings (including a missing one) to exercise filter/sort.
const MUSEUM_ALPHA: AttractionSummary = {
  providerPlaceId: 'F1',
  name: 'Museum Alpha',
  category: 'museum',
  tags: ['museum'],
  rating: 8.0,
  thumbnailUrl: null,
  latitude: 20.01,
  longitude: 20.01,
  address: null,
};

const PARK_BETA: AttractionSummary = {
  providerPlaceId: 'F2',
  name: 'Park Beta',
  category: 'park',
  tags: ['park', 'nature'],
  rating: 6.5,
  thumbnailUrl: null,
  latitude: 20.02,
  longitude: 20.02,
  address: null,
};

const LANDMARK_GAMMA: AttractionSummary = {
  providerPlaceId: 'F3',
  name: 'Landmark Gamma',
  category: 'landmark',
  tags: ['landmark'],
  rating: null,
  thumbnailUrl: null,
  latitude: 20.03,
  longitude: 20.03,
  address: null,
};

const MUSEUM_DELTA: AttractionSummary = {
  providerPlaceId: 'F4',
  name: 'Museum Delta',
  category: 'museum',
  tags: ['museum', 'historic'],
  rating: 9.2,
  thumbnailUrl: null,
  latitude: 20.04,
  longitude: 20.04,
  address: null,
};

export const destinationHandlers = [
  http.get(`${BASE_URL}/destinations/locations/search`, async ({ request }) => {
    const query = new URL(request.url).searchParams.get('query') ?? '';

    if (query === LOCATION_SEARCH_ERROR_QUERY) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Destination.SearchLocations.Exception',
          error: 'Could not search locations.',
          validates: [],
        },
        { status: 500 },
      );
    }

    if (query === CITY_WITH_ATTRACTIONS_QUERY) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [PARIS], totalCount: 1 },
      });
    }

    if (query === CITY_WITH_NO_ATTRACTIONS_QUERY) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [NOWHEREVILLE], totalCount: 1 },
      });
    }

    if (query === ATTRACTIONS_ERROR_CITY_QUERY) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [ATTRACTIONS_ERROR_CITY], totalCount: 1 },
      });
    }

    if (query === SLOW_CITY_QUERY) {
      await delay(150);
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [SLOW_CITY], totalCount: 1 },
      });
    }

    if (query === MANY_MATCHES_QUERY) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: MANY_MATCHES_LOCATIONS, totalCount: MANY_MATCHES_LOCATIONS.length },
      });
    }

    if (query === FILTER_SORT_CITY_QUERY) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [FILTER_SORT_CITY], totalCount: 1 },
      });
    }

    return HttpResponse.json({
      success: true,
      errorCode: null,
      error: null,
      validates: [],
      result: { items: [], totalCount: 0 },
    });
  }),

  http.get(`${BASE_URL}/destinations/attractions`, async ({ request }) => {
    const latitude = Number(new URL(request.url).searchParams.get('latitude'));

    if (latitude === NOWHEREVILLE.latitude) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [], totalCount: 0 },
      });
    }

    if (latitude === ATTRACTIONS_ERROR_CITY.latitude) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Destination.GetAttractions.Exception',
          error: 'Could not load attractions.',
          validates: [],
        },
        { status: 500 },
      );
    }

    if (latitude === SLOW_CITY.latitude) {
      await delay(150);
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: { items: [EIFFEL_TOWER], totalCount: 1 },
      });
    }

    if (latitude === FILTER_SORT_CITY.latitude) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: {
          items: [MUSEUM_ALPHA, PARK_BETA, LANDMARK_GAMMA, MUSEUM_DELTA],
          totalCount: 4,
        },
      });
    }

    return HttpResponse.json({
      success: true,
      errorCode: null,
      error: null,
      validates: [],
      result: { items: [EIFFEL_TOWER, LOUVRE], totalCount: 2 },
    });
  }),
];
