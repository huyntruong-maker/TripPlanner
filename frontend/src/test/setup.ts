import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { server } from './msw/server';
import { resetTripsFixture } from './msw/handlers/trips';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

afterEach(() => resetTripsFixture());

// Explicit test API imports mean Testing Library's auto-cleanup doesn't self-register.
afterEach(() => cleanup());
