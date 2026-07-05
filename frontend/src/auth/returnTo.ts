const RETURN_TO_PARAM = 'returnTo';
const DEFAULT_RETURN_TO = '/trips';

/**
 * Builds a /login URL that remembers where to send the user back to once
 * they sign in (F3-US8 AC2 — redirect-back only, best-effort per MVP scope).
 */
export function buildLoginUrl(currentPath: string): string {
  return `/login?${RETURN_TO_PARAM}=${encodeURIComponent(currentPath)}`;
}

/**
 * Reads the returnTo target from a /login URL's search string. Only accepts
 * same-origin relative paths (must start with "/") to avoid an open redirect
 * via a crafted query string — treat all input as hostile (security.md).
 */
export function readReturnTo(search: string, fallback: string = DEFAULT_RETURN_TO): string {
  const params = new URLSearchParams(search);
  const value = params.get(RETURN_TO_PARAM);
  return value && value.startsWith('/') && !value.startsWith('//') ? value : fallback;
}
