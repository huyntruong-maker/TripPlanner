---
name: security-and-hardening
description: Guides agents through OWASP Top 10 prevention across .NET APIs and React apps — auth, secrets, CORS, CSRF, validation, dependency auditing, and security headers. Use when securing or reviewing a full-stack feature.
---

# Security and Hardening

## Overview
OWASP Top 10 prevention mapped to an ASP.NET Core + React stack: authn/authz, secrets
management, CORS/CSRF, input validation, dependency auditing, and HTTP security headers.

## When to Use
- Building auth or handling sensitive data.
- Reviewing a feature for security issues.
- Configuring CORS, headers, or secret storage.

## Process

1. Broken access control (A01): enforce authorization on the server for every protected
   endpoint with policies; never trust the client. Check ownership, not just authentication.
   ```csharp
   group.MapDelete("/{id}", Delete).RequireAuthorization("owner");
   ```

2. Cryptographic / data exposure (A02): TLS everywhere; hash passwords with a strong KDF
   (ASP.NET Identity / Argon2). Never log tokens, passwords, or PII.

3. Injection (A03): use EF Core parameterized queries / LINQ; never string-concatenate SQL.
   On React, never `dangerouslySetInnerHTML` with untrusted input (XSS).

4. Manage secrets out of source control:
   - Dev: `dotnet user-secrets`.
   - Prod: Azure Key Vault / AWS Secrets Manager / env vars injected at runtime.
   - React: only `VITE_`/`NEXT_PUBLIC_` build-time public values; never ship API secrets.

5. Configure CORS to an explicit allow-list of origins; never `AllowAnyOrigin` with
   credentials.
   ```csharp
   o.AddPolicy("spa", p => p.WithOrigins("https://app.example.com")
       .AllowCredentials().AllowAnyHeader().AllowAnyMethod());
   ```

6. CSRF: prefer bearer tokens in the `Authorization` header (not auto-sent cookies). If
   using cookie auth, enable antiforgery tokens and `SameSite=Strict/Lax` cookies.

7. Validate and sanitize all input at the boundary (FluentValidation server-side, zod
   client-side). Treat client validation as UX only; server validation is authoritative.

8. Add security headers (CSP, HSTS, X-Content-Type-Options, Referrer-Policy,
   X-Frame-Options) via middleware or reverse proxy.

9. Audit dependencies regularly and in CI:
   ```bash
   dotnet list package --vulnerable --include-transitive
   npm audit --audit-level=high
   ```

10. Rate-limit auth and write endpoints; log security events; return generic auth errors
    (no "user not found" vs "wrong password" leak).

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Client validation is enough." | Attackers bypass the client; validate server-side. |
| "AllowAnyOrigin is easier." | With credentials it is a CSRF/data-leak hole. |
| ".env is gitignored, so it is safe." | Use a secret store; .env still leaks easily. |
| "We will audit deps before release." | Audit continuously in CI; vulns land daily. |
| "Detailed auth errors help users." | They help attackers enumerate accounts. |

## Red Flags
- Authorization checked only on the client / hidden UI as the only guard.
- Secrets or connection strings committed or in client bundles.
- `AllowAnyOrigin().AllowCredentials()` or wildcard CORS.
- Raw SQL string concatenation; `dangerouslySetInnerHTML` on user data.
- No dependency audit step; ignored high/critical advisories.

## Verification
- Every protected route enforces server-side authorization + ownership.
- No secrets in git history or the client bundle (grep/scan passes).
- CORS limited to known origins; security headers present.
- `dotnet list package --vulnerable` and `npm audit` clean (or triaged) in CI.
- Inputs validated server-side; XSS/injection vectors closed.
