import { describe, expect, it } from 'vitest';
import { buildFakeJwt } from '../test/buildFakeJwt';
import { decodeUserFromToken } from './jwt';

describe('decodeUserFromToken', () => {
  it('decodes id/email from the exact short claim names ASP.NET Core Identity issues', () => {
    // This locks in the Wave 0 risk flagged for verification. We could not spin
    // up the full backend (DB + Identity) in this environment, so this is
    // backed by direct source analysis rather than an observed live token:
    //
    // 1. Infrastructure/Identity/DbIdentity.cs registers `AddDefaultIdentity<User>`
    //    with no custom IUserClaimsPrincipalFactory and no IdentityOptions.ClaimsIdentity
    //    overrides -> the default UserClaimsPrincipalFactory<TUser> claim set applies:
    //    ClaimTypes.NameIdentifier (= user Id) and ClaimTypes.Name (= UserName).
    // 2. Application/Features/Auth/Commands/RegisterCommand/RegisterCommand.cs sets
    //    `UserName = request.Email`, so the Name claim's value is the user's email.
    // 3. Application/Features/Auth/Shared/AuthShareService.cs builds the JWT via
    //    plain `JwtSecurityTokenHandler.CreateToken`, which applies .NET's default
    //    outbound claim-type map, serializing ClaimTypes.NameIdentifier -> "nameid"
    //    and ClaimTypes.Name -> "unique_name" in the token payload.
    // 4. This is corroborated directly by Domain/Helpers/CommonHelper.cs, which reads
    //    raw (unmapped) JWT claims and explicitly checks for the short name "nameid"
    //    alongside the long-form ClaimTypes.NameIdentifier URI.
    //
    // Residual risk: not confirmed against a token issued by a running instance of
    // this backend. Re-verify once the API is reachable (e.g. in CI or locally) by
    // decoding a real login response's `token` and comparing claim keys.
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
