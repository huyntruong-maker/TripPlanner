const RETURN_TO_PARAM = 'returnTo';
const DEFAULT_RETURN_TO = '/trips';

export function buildLoginUrl(currentPath: string): string {
  return `/login?${RETURN_TO_PARAM}=${encodeURIComponent(currentPath)}`;
}

/** Only accepts same-origin relative paths (must start with "/") to avoid an open redirect. */
export function readReturnTo(search: string, fallback: string = DEFAULT_RETURN_TO): string {
  const params = new URLSearchParams(search);
  const value = params.get(RETURN_TO_PARAM);
  return value && value.startsWith('/') && !value.startsWith('//') ? value : fallback;
}
