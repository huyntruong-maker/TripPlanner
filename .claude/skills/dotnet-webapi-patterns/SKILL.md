---
name: dotnet-webapi-patterns
description: Guides agents through building ASP.NET Core Web APIs (.NET 8+). Use when creating endpoints, wiring DI, configuring EF Core, validation, auth, or error handling in a .NET backend.
---

# ASP.NET Core Web API Patterns

## Overview
Conventions for production ASP.NET Core (.NET 8+) Web APIs: project layout, endpoint style,
dependency injection, EF Core, validation, error semantics, versioning, and auth.

## When to Use
- Adding or refactoring API endpoints.
- Wiring services, options, DbContext, or middleware.
- Standardizing error responses or validation.
- Reviewing a .NET backend PR.

## Process

1. Use a layered project layout. Keep concerns separated:
   ```
   src/Api            (endpoints, DI, middleware, Program.cs)
   src/Application    (use cases, DTOs, validators, interfaces)
   src/Domain         (entities, value objects, domain rules)
   src/Infrastructure (EF Core, external clients, options impl)
   ```

2. Choose endpoint style deliberately.
   - Minimal APIs for small/focused services; group with `MapGroup`.
   - Controllers when you need filters, model binding conventions, or large surface area.
   ```csharp
   var group = app.MapGroup("/api/v1/orders").WithTags("Orders");
   group.MapGet("/{id:guid}", GetOrder).Produces<OrderDto>().Produces(404);
   ```

3. Register dependencies with correct lifetimes. Scoped for EF Core / per-request,
   Singleton for stateless clients, Transient for lightweight stateless helpers.
   ```csharp
   builder.Services.AddScoped<IOrderService, OrderService>();
   builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));
   ```

4. Bind configuration via the Options pattern, validate on start.
   ```csharp
   builder.Services.AddOptions<JwtOptions>()
       .Bind(builder.Configuration.GetSection("Jwt"))
       .ValidateDataAnnotations().ValidateOnStart();
   ```

5. Validate input with FluentValidation at the boundary; never trust client data.
   ```csharp
   public sealed class CreateOrderValidator : AbstractValidator<CreateOrderRequest> {
       public CreateOrderValidator() => RuleFor(x => x.Items).NotEmpty();
   }
   ```

6. Return RFC 7807 ProblemDetails for all errors. Add `AddProblemDetails()` and an
   exception handler; never leak stack traces.
   ```csharp
   builder.Services.AddProblemDetails();
   app.UseExceptionHandler();
   return Results.Problem(statusCode: 404, title: "Order not found");
   ```

7. Version the API explicitly with `Asp.Versioning`. Prefer URL segment `/api/v1`.
   Document each version in Swagger.

8. Secure with JWT bearer or OIDC. Authenticate, then authorize with policies.
   ```csharp
   builder.Services.AddAuthentication().AddJwtBearer();
   builder.Services.AddAuthorization(o =>
       o.AddPolicy("admin", p => p.RequireRole("Admin")));
   group.MapDelete("/{id}", DeleteOrder).RequireAuthorization("admin");
   ```

9. Async all the way. Accept `CancellationToken`, await DB/IO, never `.Result`/`.Wait()`.
   ```csharp
   await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
   ```

10. Keep `Program.cs` thin; push setup into extension methods
    (`AddApplication()`, `AddInfrastructure()`).

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Controllers are old, Minimal only." | Both are first-class; pick by surface area. |
| "Singleton DbContext is faster." | DbContext is not thread-safe; use Scoped. |
| "I'll validate in the service later." | Validate at the boundary; reject early. |
| "Returning 500 with the message is fine." | Use ProblemDetails; never leak internals. |
| "`.Result` is simpler than await." | It deadlocks and starves the thread pool. |

## Red Flags
- `DbContext` registered as Singleton or captured in a singleton.
- Endpoints returning raw exceptions or anonymous error objects.
- Business logic inside `Program.cs` or controllers.
- Sync-over-async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`).
- Secrets or connection strings in `appsettings.json` committed to git.
- No API version segment; breaking changes to existing routes.

## Verification
- `dotnet build` succeeds without warnings-as-errors failing.
- Swagger UI lists endpoints grouped and versioned.
- An invalid request returns a ProblemDetails body with correct status.
- DI lifetimes confirmed (no captive dependencies; build with `ValidateScopes`).
- All async paths accept and pass `CancellationToken`.
