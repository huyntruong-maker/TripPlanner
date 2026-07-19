import { describe, expect, it } from 'vitest';
import { buildFakeJwt } from '../test/buildFakeJwt';
import { decodeUserFromToken } from './jwt';

describe('decodeUserFromToken', () => {
  it('decodes id/email from the exact short claim names ASP.NET Core Identity issues', () => {
    // Backed by source analysis, not a live token: .NET's JwtSecurityTokenHandler maps NameIdentifier/Name to "nameid"/"unique_name".
    const token = buildFakeJwt({
      nameid: 'b3b1f5b0-1111-4a2b-9c3d-abcdef123456',
      unique_name: 'jane@example.com',
    });

    expect(decodeUserFromToken(token)).toEqual({
      id: 'b3b1f5b0-1111-4a2b-9c3d-abcdef123456',
      email: 'jane@example.com',
    });
  });

  it('falls back through alternate claim key spellings (sub / email)', () => {
    const token = buildFakeJwt({ sub: 'user-1', email: 'alt@example.com' });

    expect(decodeUserFromToken(token)).toEqual({ id: 'user-1', email: 'alt@example.com' });
  });

  it('returns null (not signed in) when the expected claims are missing', () => {
    const token = buildFakeJwt({ foo: 'bar' });

    expect(decodeUserFromToken(token)).toBeNull();
  });

  it('returns null for a malformed token instead of throwing', () => {
    expect(decodeUserFromToken('not-a-jwt')).toBeNull();
  });
});
