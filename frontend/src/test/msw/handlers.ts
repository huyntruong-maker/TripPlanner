import type { HttpHandler } from 'msw';
import { authHandlers } from './handlers/auth';
import { destinationHandlers } from './handlers/destinations';
import { destinationDetailHandlers } from './handlers/destinationDetail';
import { tripsHandlers } from './handlers/trips';

/**
 * Shared MSW request handlers, keyed by feature. Each feature wave appends its
 * own handlers here (or composes feature-local handlers into this array) so
 * every test run shares one mocked backend surface.
 *
 * Order matters: MSW resolves the first matching handler, and the generic
 * `/destinations/:providerPlaceId` detail route would otherwise shadow the
 * more specific `/destinations/attractions` and `/destinations/locations/search`
 * routes — so destinationHandlers must be registered first.
 */
export const handlers: HttpHandler[] = [
  ...authHandlers,
  ...destinationHandlers,
  ...destinationDetailHandlers,
  ...tripsHandlers,
];
