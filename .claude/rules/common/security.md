# Security (Common)

- Never commit secrets. Use `dotnet user-secrets` (dev) and a secret store / env (prod).
- Validate and sanitize all input server-side; client validation is UX only.
- Authorize on the server for every protected action; check ownership, not just login.
- Use parameterized queries / EF Core LINQ; never concatenate SQL.
- Restrict CORS to an explicit origin allow-list; never wildcard with credentials.
- Send security headers (CSP, HSTS, X-Content-Type-Options, X-Frame-Options).
- Keep dependencies patched; run `dotnet list package --vulnerable` and `npm audit` in CI.
- Do not log secrets, tokens, or PII; return generic authentication errors.
- Avoid `dangerouslySetInnerHTML` and any unescaped rendering of untrusted data.
- Use TLS everywhere; store passwords only as strong KDF hashes.
- Treat every external input as hostile until validated.

Security is a default, not a feature toggle.
