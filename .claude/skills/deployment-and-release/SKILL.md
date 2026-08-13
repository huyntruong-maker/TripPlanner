---
name: deployment-and-release
description: Guides agents through containerizing and releasing the .NET API and React app via Docker, GHCR, health checks, staged rollout, and semver GitHub Releases. Use when writing Dockerfiles, compose, or release workflows.
---

# Deployment and Release

## Overview
Container build and release practices for the stack: multi-stage Dockerfiles for the
ASP.NET Core API and the React app, docker-compose for local/integration, publishing to
GHCR, health checks, staged rollout with rollback, and semver releases.

## When to Use
- Writing or reviewing Dockerfiles / docker-compose.
- Setting up the release/publish pipeline.
- Defining health checks, rollout, or rollback.

## Process

1. Multi-stage Dockerfile for the .NET API; run as non-root, slim runtime image.
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   WORKDIR /src
   COPY . . && RUN dotnet publish src/Api -c Release -o /app
   FROM mcr.microsoft.com/dotnet/aspnet:8.0
   WORKDIR /app
   COPY --from=build /app .
   USER app
   ENTRYPOINT ["dotnet","Api.dll"]
   ```

2. Multi-stage Dockerfile for React; build static assets, serve via nginx (or a CDN).
   ```dockerfile
   FROM node:20 AS build
   WORKDIR /app
   COPY package*.json ./ && RUN npm ci
   COPY . . && RUN npm run build
   FROM nginx:alpine
   COPY --from=build /app/dist /usr/share/nginx/html
   ```

3. Compose the system for local/integration with API, web, and database.
   ```yaml
   services:
     api: { build: ., ports: ["8080:8080"], depends_on: [db] }
     db:  { image: postgres:16, environment: { POSTGRES_PASSWORD: dev } }
   ```

4. Inject config via environment variables at runtime; never bake secrets into images.
   The API reads connection strings/JWT keys from env; React only gets public build vars.

5. Expose health endpoints and wire container healthchecks.
   ```csharp
   builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
   app.MapHealthChecks("/health");
   ```
   ```dockerfile
   HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
   ```

6. Publish images to GHCR from CI, tagged by semver and git SHA.
   ```yaml
   - run: docker build -t ghcr.io/org/api:${{ github.ref_name }} .
   - run: docker push ghcr.io/org/api:${{ github.ref_name }}
   ```

7. Roll out in stages (canary or rolling); watch health/metrics before full traffic.
   Keep the previous image tag available for instant rollback.

8. Roll back by redeploying the prior known-good image tag; never hotfix in place.

9. Version with semver and cut GitHub Releases from tags with generated release notes.
   ```bash
   git tag v1.4.0 && git push --tags
   ```

10. Run database migrations as a controlled deploy step (idempotent script), not on app
    boot in production.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Single-stage image is fine." | Ships SDK + source; bloated and less secure. |
| "Bake the config into the image." | Couples image to env; inject at runtime. |
| "Run as root, simpler." | Violates least privilege; run as non-root. |
| "Migrate on app startup in prod." | Risky on multi-replica; run as a deploy step. |
| "Roll forward to fix a bad deploy." | Roll back to known-good first, then fix. |

## Red Flags
- Secrets or connection strings embedded in image layers.
- Single-stage builds shipping the SDK and source.
- Containers running as root.
- No health endpoint / no healthcheck.
- No rollback path; releases without tags or notes.

## Verification
- Images build multi-stage, run non-root, and start cleanly.
- `/health` returns healthy; container healthcheck passes.
- Images pushed to GHCR tagged by semver + SHA.
- Config and secrets injected via env, absent from layers.
- A prior image tag exists and a rollback is exercised.
