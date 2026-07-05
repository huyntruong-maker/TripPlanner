function base64UrlEncode(value: object): string {
  const base64 = btoa(JSON.stringify(value));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/**
 * Builds a syntactically valid (but unsigned) JWT for tests. jwt-decode never
 * verifies signatures, so this is enough to exercise decodeUserFromToken and
 * MSW-mocked auth flows without a live backend.
 */
export function buildFakeJwt(claims: Record<string, unknown>): string {
  const header = base64UrlEncode({ alg: 'HS512', typ: 'JWT' });
  const oneHourFromNowSeconds = Math.floor(Date.now() / 1000) + 3600;
  const payload = base64UrlEncode({ exp: oneHourFromNowSeconds, ...claims });
  return `${header}.${payload}.fake-signature`;
}
