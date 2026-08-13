---
name: api-and-interface-design
description: Guides agents through contract-first REST API design — resource naming, status codes, pagination, errors, OpenAPI, and versioning. Use when designing or reviewing an HTTP API consumed by .NET services and React clients.
---

# API and Interface Design

## Overview
Contract-first REST conventions that keep an API predictable for its consumers. Stack-agnostic,
with concrete notes for an ASP.NET Core producer and a React/TanStack Query consumer.

## When to Use
- Designing new endpoints or a new API surface.
- Reviewing API shape, status codes, or error semantics.
- Defining the contract before implementation.

## Process

1. Design the contract first. Write the OpenAPI spec (or endpoint sketch) and agree on it
   before coding the .NET handlers or the React client.

2. Name resources as plural nouns; use HTTP verbs for actions.
   ```
   GET    /api/v1/orders
   POST   /api/v1/orders
   GET    /api/v1/orders/{id}
   DELETE /api/v1/orders/{id}/items/{itemId}
   ```
   No verbs in paths (`/getOrders` is wrong).

3. Use correct status codes:
   - 200 read ok, 201 created (+ `Location`), 204 no content.
   - 400 validation, 401 unauthenticated, 403 forbidden, 404 missing, 409 conflict.
   - 422 semantic validation, 429 rate-limited, 5xx server.

4. Standardize errors as RFC 7807 ProblemDetails so every consumer parses one shape.
   ```json
   { "type": "...", "title": "Validation failed", "status": 400,
     "errors": { "email": ["required"] } }
   ```

5. Paginate collections; never return unbounded lists. Prefer cursor pagination for large
   or live data; offset is fine for small/stable sets.
   ```
   GET /orders?cursor=eyJpZCI6MTB9&limit=20
   { "items": [...], "nextCursor": "..." }
   ```

6. Validate at the boundary. The .NET producer rejects bad input with 400/422 and
   ProblemDetails; the React consumer validates with zod before display/use.

7. Version explicitly (`/api/v1`). Add fields without breaking; bump the major version for
   breaking changes. Never silently change a field's meaning.

8. Keep payloads stable and typed. Generate TypeScript client types from OpenAPI
   (e.g., `openapi-typescript`) so the React side matches the contract.

9. Be consistent: camelCase JSON, ISO-8601 UTC timestamps, explicit currency/units,
   idempotency keys for unsafe retries.

10. Document every endpoint in Swagger/OpenAPI with examples and error responses.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Return 200 with an error flag." | Use real status codes; clients branch on them. |
| "Just return the whole list." | Unbounded responses break at scale; paginate. |
| "Each endpoint has its own error shape." | One ProblemDetails shape simplifies clients. |
| "We can change the field meaning quietly." | That is a breaking change; version it. |
| "Verbs in the URL are clearer." | REST uses nouns + HTTP methods. |

## Red Flags
- 200 responses carrying `success: false`.
- Action verbs in resource paths.
- Endpoints returning entire tables with no paging.
- Inconsistent casing, timestamp formats, or error shapes.
- Breaking changes shipped without a version bump.

## Verification
- OpenAPI spec exists and matches the implementation.
- Status codes correct for success, validation, auth, and not-found.
- Collections paginated with documented limits.
- All errors conform to ProblemDetails.
- Generated TS types compile against the React client.
