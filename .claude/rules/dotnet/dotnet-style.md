# .NET / C# Style

C# conventions for the ASP.NET Core backend (.NET 10).

- Enable `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>` where feasible.
  Avoid `!` null-forgiving except at proven-safe boundaries.
- Use file-scoped namespaces and `var` when the type is obvious.
- Commands (MediatR `ICommand<T>`) are `record` types; response DTOs and entity models are `class`.
  Do not wholesale switch between the two patterns within a layer — match what already exists.
- Naming: PascalCase for types/methods/properties, camelCase for locals/params,
  `_camelCase` for private fields, `I`-prefix for interfaces.
- Async all the way: suffix async methods `Async`, accept and pass `CancellationToken`,
  never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- One public type per file; keep `Program.cs` thin via DI extension methods (extension methods in
  `WebApi/Configurations/`).
- EF Core: dual DbContext pattern (ReadDbContext for queries, WriteDbContext for mutations via
  IReadUnitOfWork / IWriteUnitOfWork). Use `AsNoTracking()` for reads. Manage schema with
  migrations only — never `EnsureCreated` in prod. Avoid N+1 with `Include`/`Select`.
- **Validation & error response pattern**: validate manually in controllers (guard clauses at the
  top of each action). Return `ResultRes<T>` (`{ success, errorCode, error, validates }`) for all
  responses — not RFC 7807 ProblemDetails. Error codes are string constants in
  `Domain/Messages/*ControllerMsg.cs`. FluentValidation is NOT used.
- CQRS with MediatR: commands implement `ICommand<TResponse>`, queries implement `IQuery<TResponse>`.
  Inject `ISender` as an **action-method parameter** (not constructor) — ASP.NET endpoint DI injects it per-request.
- Bind config via the Options pattern / `IConfiguration.GetSection(ConfigKeys.*)` using the
  `ConfigKeys` constants class in Domain.
- Use `ILogger<T>` structured logging (Serilog backend); never log secrets or PII.
- Dispose / `await using` `IDisposable`/`IAsyncDisposable` resources.
- Run `dotnet format` before committing.

Prefer explicit, async, null-safe code.
