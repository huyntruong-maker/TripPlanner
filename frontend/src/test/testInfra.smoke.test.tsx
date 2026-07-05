import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from './msw/server';

/**
 * Wave 0 infra check only — proves Vitest + Testing Library + jest-dom + MSW
 * are wired together correctly. Feature suites (Waves 1-4) test real behavior;
 * this file is not a substitute for them.
 */
describe('test infrastructure', () => {
  it('renders with Testing Library and asserts via jest-dom matchers', () => {
    render(<p>hello test infra</p>);

    expect(screen.getByText('hello test infra')).toBeInTheDocument();
  });

  it('intercepts network calls through the shared MSW server', async () => {
    server.use(
      http.get('https://example.test/ping', () => HttpResponse.json({ ok: true })),
    );

    const response = await fetch('https://example.test/ping');
    const body = await response.json();

    expect(body).toEqual({ ok: true });
  });
});
