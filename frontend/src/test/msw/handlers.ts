import type { HttpHandler } from 'msw';
import { authHandlers } from './handlers/auth';
import { destinationHandlers } from './handlers/destinations';
import { destinationDetailHandlers } from './handlers/destinationDetail';
import { tripsHandlers } from './handlers/trips';

/** Shared MSW handlers, keyed by feature; order matters — destinationHandlers must precede the generic `/destinations/:providerPlaceId` route it would otherwise shadow. */
export const handlers: HttpHandler[] = [
  ...authHandlers,
  ...destinationHandlers,
  ...destinationDetailHandlers,
  ...tripsHandlers,
];
