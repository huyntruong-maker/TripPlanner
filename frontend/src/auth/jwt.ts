import { jwtDecode } from 'jwt-decode';
import type { AuthenticatedUser } from '../types';

// ASP.NET Core Identity's default JwtSecurityTokenHandler serializes
// ClaimTypes.NameIdentifier / ClaimTypes.Name under short JWT claim names
// ("nameid" / "unique_name") rather than the long claim-type URIs — see
// Domain/Helpers/CommonHelper.cs which reads claims the same defensive way.
// We try every known form here since there is no `/me` endpoint to fall back on.
const ID_CLAIM_KEYS = [
  'nameid',
  'sub',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
];

const EMAIL_CLAIM_KEYS = [
  'email',
  'unique_name',
  'name',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
];

function firstStringClaim(claims: Record<string, unknown>, keys: string[]): string | null {
  for (const key of keys) {
    const value = claims[key];
    if (typeof value === 'string' && value.length > 0) {
      return value;
    }
  }
  return null;
}

/**
 * Decodes the current user's id/email from the access token's own claims.
 * Returns null if the token is malformed or missing the claims we expect —
 * callers must treat that as "not signed in" rather than throwing.
 */
export function decodeUserFromToken(token: string): AuthenticatedUser | null {
  try {
    const claims = jwtDecode<Record<string, unknown>>(token);
    const id = firstStringClaim(claims, ID_CLAIM_KEYS);
    const email = firstStringClaim(claims, EMAIL_CLAIM_KEYS);
    return id && email ? { id, email } : null;
  } catch {
    return null;
  }
}
