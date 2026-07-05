import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { server } from './msw/server';
import { resetTripsFixture } from './msw/handlers/trips';

// Shared Vitest bootstrap for all feature test suites (Waves 1-4 add handlers,
// not setup). Fails the test if a request hits the network without a mock,
// per react-testing.md ("no real network").
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

// The trips handler mock keeps a mutable in-memory "database" so create/set-dates
// /add/remove tests can see the effects of prior calls; restore it between tests
// so trip mutations don't leak across test cases.
afterEach(() => resetTripsFixture());

// Testing Library's automatic cleanup only self-registers when Vitest's
// `globals: true` mode is on; we import test APIs explicitly instead, so
// unmount every rendered tree by hand after each test.
afterEach(() => cleanup());
