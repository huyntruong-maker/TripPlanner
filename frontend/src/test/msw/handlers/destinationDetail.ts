import { http, HttpResponse } from 'msw';
import type { DestinationDetail } from '../../../types';

const BASE_URL = 'http://localhost:5080/api/v1';

export const FULL_DETAIL_ID = 'W214242';
export const PARTIAL_DETAIL_ID = 'W000001';
export const NOT_FOUND_ID = 'DOES-NOT-EXIST';
export const SERVER_ERROR_ID = 'W_ERROR';

const FULL_DETAIL: DestinationDetail = {
  providerPlaceId: FULL_DETAIL_ID,
  name: 'Eiffel Tower',
  category: 'cultural',
  tags: ['cultural', 'landmark'],
  description: 'Famous iron lattice tower on the Champ de Mars in Paris.',
  photos: ['https://example.test/eiffel-1.jpg', 'https://example.test/eiffel-2.jpg'],
  address: 'Champ de Mars, Paris, France',
  website: 'https://toureiffel.paris',
  openingHours: {
    displayText: 'Daily 09:00–23:00',
    weekdayText: ['Monday: 09:00 – 23:00', 'Tuesday: 09:00 – 23:00'],
    isOpenNow: true,
  },
  rating: 9.5,
  latitude: 48.8584,
  longitude: 2.2945,
};

const PARTIAL_DETAIL: DestinationDetail = {
  providerPlaceId: PARTIAL_DETAIL_ID,
  name: 'Mystery Ruin',
  category: null,
  tags: [],
  description: null,
  photos: [],
  address: null,
  website: null,
  openingHours: null,
  rating: null,
  latitude: 10.0,
  longitude: 20.0,
};

export const destinationDetailHandlers = [
  http.get(`${BASE_URL}/destinations/:providerPlaceId`, ({ params }) => {
    const { providerPlaceId } = params;

    if (providerPlaceId === FULL_DETAIL_ID) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: FULL_DETAIL,
      });
    }

    if (providerPlaceId === PARTIAL_DETAIL_ID) {
      return HttpResponse.json({
        success: true,
        errorCode: null,
        error: null,
        validates: [],
        result: PARTIAL_DETAIL,
      });
    }

    if (providerPlaceId === SERVER_ERROR_ID) {
      return HttpResponse.json(
        {
          success: false,
          errorCode: 'Destination.GetDetail.Exception',
          error: 'Could not load this destination.',
          validates: [],
        },
        { status: 500 },
      );
    }

    return HttpResponse.json(
      {
        success: false,
        errorCode: 'Destination.GetDetail.NotFound',
        error: 'This destination could not be found.',
        validates: [],
      },
      { status: 404 },
    );
  }),
];
