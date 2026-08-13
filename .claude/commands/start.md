---
description: Onboard into an existing, already-set-up project — detect the stack, summarize the architecture, generate docs, and build an initial TODO backlog. Additive only; never overwrites code.
argument-hint: "[optional notes about the project or focus area]"
---

Onboard into the EXISTING project in this repo. Treat any extra context as: $ARGUMENTS

Hard rule: do NOT modify, refactor, or overwrite existing source code. You may only ADD files under `docs/` and `.claude/`, and append to existing docs. If a doc already exists, append a clearly marked section rather than replacing it. Confirm before touching anything outside `docs/`.

Steps:
1. Detect the stack. Read, where present: `package.json`, `*.csproj`, `*.sln`, `Directory.Build.props`, `Dockerfile`, `docker-compose.yml`, `appsettings*.json`, `tsconfig.json`, `vite.config.*`, lockfiles. Identify backend (e.g. ASP.NET Core), frontend (e.g. React/TS), database, and build/run commands.
2. Map the architecture: top-level layout, projects/packages, entry points, API surface, data layer, key dependencies. Note how to build, test, and run locally.
3. Generate or append docs under `docs/` from templates, keeping them concise:
   - `docs/PRD.md` — product intent inferred from code/README (mark inferred items as assumptions).
   - `docs/ARCHITECTURE.md` — components, layers, data flow, build/run/test commands.
   - `docs/API.md` — endpoints/controllers and contracts found.
   - `docs/DATABASE.md` — entities, schema/migrations, relationships.
4. Build an initial TODO backlog (`docs/TODO.md` or the project tracker): gaps, missing tests, TODO/FIXME comments, obvious tech debt, undocumented config. Use task IDs and acceptance criteria per planning-and-task-breakdown.
5. Summarize findings to the user: detected stack, architecture overview, which docs were created/appended, and the top backlog items. List every file you added.

Verification: every doc generated is additive; no source file changed; the summary lists created files and how to build/test/run the project.
