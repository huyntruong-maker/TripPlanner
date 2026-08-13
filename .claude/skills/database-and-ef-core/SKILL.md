---
name: database-and-ef-core
description: Guides agents through EF Core modeling, migrations, indexing, query optimization, and resiliency for SQL Server and PostgreSQL. Use when designing schema, writing queries, or diagnosing data-layer performance.
---

# Database and EF Core

## Overview
Practical EF Core (.NET 8+) data-layer guidance: modeling, a safe migrations workflow,
indexing, query performance (especially avoiding N+1), transactions, and connection
resiliency, with SQL Server and PostgreSQL specifics.

## When to Use
- Designing or changing the schema / entity model.
- Writing or reviewing EF Core queries.
- Diagnosing slow queries, N+1, or transient failures.

## Process

1. Model explicitly with `IEntityTypeConfiguration<T>`; do not rely on convention guesses
   for keys, lengths, precision, or relationships.
   ```csharp
   builder.Property(o => o.Total).HasPrecision(18, 2);
   builder.HasIndex(o => o.CustomerId);
   ```

2. Manage schema with migrations only. Review the generated SQL before applying.
   ```bash
   dotnet ef migrations add AddOrderIndex
   dotnet ef migrations script --idempotent -o migrate.sql
   ```
   Apply via script in CI/deploy, not `EnsureCreated()` in production.

3. Index for your real query patterns: foreign keys, frequent filters, sort columns.
   Add covering/composite indexes where the workload justifies them. Do not over-index
   write-heavy tables.

4. Avoid N+1: load related data with `Include`/`ThenInclude` or projection, not lazy loops.
   ```csharp
   await db.Orders.Include(o => o.Items)
       .Where(o => o.CustomerId == id).ToListAsync(ct);
   ```

5. Project to DTOs with `Select` for read paths so you fetch only needed columns and skip
   change tracking.
   ```csharp
   .Select(o => new OrderDto(o.Id, o.Total)).AsNoTracking()
   ```

6. Use `AsNoTracking()` for read-only queries; keep tracking for updates.

7. Wrap multi-statement writes in a transaction; for multiple SaveChanges use an explicit
   transaction or the execution strategy.
   ```csharp
   await using var tx = await db.Database.BeginTransactionAsync(ct);
   ```

8. Enable connection resiliency for transient faults.
   ```csharp
   o.UseSqlServer(cs, s => s.EnableRetryOnFailure());
   o.UseNpgsql(cs, s => s.EnableRetryOnFailure());
   ```
   Note: retrying execution strategies require manual transactions via `ExecuteAsync`.

9. Provider notes:
   - SQL Server: `datetime2`, `decimal(18,2)`, clustered PK by default, watch deadlocks.
   - PostgreSQL: `timestamptz`, `numeric`, `jsonb` for documents, GIN indexes for JSON.

10. Profile real SQL (EF logging, `ToQueryString()`, query plans) before optimizing.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "EnsureCreated is fine for prod." | It skips migrations; use migration scripts. |
| "Lazy loading is convenient." | It hides N+1 query storms; load explicitly. |
| "Track everything, simpler." | Tracking read-only queries wastes memory/CPU. |
| "Indexes always help." | They slow writes; index for actual queries. |
| "Retries make transactions safe." | Retry strategies need manual transaction wiring. |

## Red Flags
- `EnsureCreated()` or raw schema edits instead of migrations.
- Loops issuing one query per item (classic N+1).
- Loading full entities to read two fields.
- No indexes on foreign keys or hot filter columns.
- No retry strategy against a managed/cloud database.

## Verification
- Migrations generate, script cleanly, and apply idempotently.
- Hot queries inspected via `ToQueryString()` / logs; no N+1.
- Read paths use `AsNoTracking` and DTO projection.
- Indexes exist for FKs and frequent filters/sorts.
- Retry-on-failure enabled; multi-write paths use transactions.
