import { jwtDecode } from 'jwt-decode';
import type { AuthenticatedUser } from '../types';

// ASP.NET's JwtSecurityTokenHandler maps ClaimTypes to short names ("nameid"/"unique_name");
// try every known form since there's no `/me` endpoint to fall back on.
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

/** Decodes id/email from the token's claims; returns null (treat as signed out) if malformed or missing. */
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
