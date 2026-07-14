import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from './msw/server';

/** Infra check only — proves the test stack is wired; feature suites test real behavior. */
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
