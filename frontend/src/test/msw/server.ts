import { setupServer } from 'msw/node';
import { handlers } from './handlers';

/** The shared MSW server instance used by every test file via src/test/setup.ts. */
export const server = setupServer(...handlers);
