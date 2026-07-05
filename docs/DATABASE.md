# Database — Travel Trip Planner

Owner agent: database-expert. EF Core; SQL Server or PostgreSQL.

## Entities (MVP)
- **Users**(Id PK, Email unique, PasswordHash, EmailVerified bool, CreatedAt)
- **EmailVerificationTokens**(Id PK, UserId FK→Users, Token, ExpiresAt, ConsumedAt nullable)
- **RefreshTokens**(Id PK, UserId FK→Users, TokenHash, ExpiresAt, RevokedAt nullable)
- **Trips**(Id PK, UserId FK→Users, Name, StartDate nullable, EndDate nullable, CreatedAt, UpdatedAt)
- **ItineraryDays**(Id PK, TripId FK→Trips, Date, DayIndex)
- **TripDestinations**(Id PK, TripId FK→Trips, ItineraryDayId FK→ItineraryDays nullable,
  ProviderPlaceId, Name, Category nullable, ThumbnailUrl nullable, Lat, Lng, Position int, CreatedAt)
  - ItineraryDayId NULL = in "Saved Places" (not yet scheduled to a day).
- **DestinationCache**(ProviderPlaceId PK, PayloadJson, FetchedAt) — optional, for NFR-2/NFR-5.

## Relationships
User 1—* Trip; Trip 1—* ItineraryDay; Trip 1—* TripDestination; ItineraryDay 1—* TripDestination (optional).

## Key constraints & indexes
- Unique(Users.Email). Unique(Trips.UserId, Name) optional.
- Index TripDestinations(TripId, ItineraryDayId, Position) for board ordering.
- Check: Trips.StartDate ≤ EndDate (enforced in app + optional DB check).
- Prevent same ProviderPlaceId twice in the same ItineraryDay (app rule; unique filtered index optional).
- NFR-6: all Trip/TripDestination access filtered by Trips.UserId.

## Migrations workflow
`dotnet ef migrations add <Name>` → review SQL → `dotnet ef database update`. One migration per slice.

## Notes
SQL Server: use `datetime2`. PostgreSQL: `timestamptz`, `jsonb` for PayloadJson. Connection resiliency (EnableRetryOnFailure).

---

## Reconciliation against actual code (2026-06-17)

The section above is the planned schema. This section records what is **actually in the codebase**.
Status key: ✅ exists · ❌ not yet created.

### Database: PostgreSQL only
All Npgsql + PostgreSQL 16. SQL Server support is not implemented.

### Actual entities (EF Core, in `Domain/Entities/`)

**Identity entities (✅ exist, via ASP.NET Core Identity + custom extensions)**

| Entity | Table (EF default) | Key fields | Notes |
|--------|-------------------|------------|-------|
| `User` | `AspNetUsers` | Id (Guid), UserName, Email, FirstName, LastName, ResetPasswordToken, ResetPasswordExpiration, IsDeleted | Extends `IdentityUser<Guid>`. Implements `IBaseEntity`, `IIsDeletedEntity`. |
| `UserToken` | `AspNetUserTokens` | UserId, DeviceUuid, Token (JWT), RefreshToken, RefreshTokenExpiration, RememberMe, DeviceInfo, LocationInfo | Multi-device token storage. **Different from the planned `RefreshTokens` table.** |
| `UserClaim` | `AspNetUserClaims` | Id, UserId, ClaimType, ClaimValue | Standard Identity claims. |
| `UserLogin` | `AspNetUserLogins` | LoginProvider, ProviderKey, UserId | External login providers. |

There is no role/permission concept in this product — the PRD defines a single "Traveler" persona who only
manages their own data (NFR-6). A prior RBAC scaffold (`Role`/`UserRole`/`RoleClaim`, `AspNetRoles` /
`AspNetUserRoles` / `AspNetRoleClaims`) was unused leftover boilerplate and was removed, along with the
seeded `SuperAdmin`/`SystemAdmin` roles and the seeded `admin` user, via migration `RemoveRbacScaffold`
(2026-07-05). `BaseDbContext` now extends `IdentityUserContext<User, Guid, UserClaim, UserLogin, UserToken>`
(no role store).

**Base entity (✅ exists)**
`BaseEntity`: Id (Guid PK), CreatedBy (Guid?), CreatedAt (DateTimeOffset), UpdatedBy (Guid?), UpdatedAt (DateTimeOffset?).

**MVP entities (✅ created in migration `InitialSchema` — 2026-06-20)**

| Entity | Table | Key fields | Notes |
|--------|-------|------------|-------|
| `EmailVerificationToken` | `EmailVerificationTokens` | Id (Guid PK), UserId FK→AspNetUsers, Token (varchar 512, unique), ExpiresAt, ConsumedAt (nullable), CreatedAt | Cascade-delete on User. Unique index on Token; index on UserId. |
| `Trip` | `Trips` | Id (Guid PK), UserId FK→AspNetUsers, Name (varchar 200), StartDate (date, nullable), EndDate (date, nullable), CreatedBy/At/UpdatedBy/At | Implements `IBaseEntity`. Cascade-delete on User. Soft-delete filter `!IsDeleted`. Unique index on (UserId, Name). |
| `ItineraryDay` | `ItineraryDays` | Id (Guid PK), TripId FK→Trips, Date (date), DayIndex (int) | Cascade-delete on Trip. Soft-delete filter `!IsDeleted`. Unique index on (TripId, DayIndex). |
| `TripDestination` | `TripDestinations` | Id (Guid PK), TripId FK→Trips, ItineraryDayId FK→ItineraryDays (nullable), ProviderPlaceId (varchar 256), Name (varchar 300), Category (nullable), ThumbnailUrl (nullable), Lat/Lng (double), Position (int), CreatedAt | ItineraryDayId NULL = "Saved Places" (unscheduled). **SetNull** on ItineraryDay delete (destinations are retained, not removed); cascade-delete on Trip delete. Soft-delete filter `!IsDeleted`. Composite index on (TripId, ItineraryDayId, Position). |
| `DestinationCache` | `DestinationCache` | ProviderPlaceId (varchar 256, PK), PayloadJson (jsonb), FetchedAt | No FK; standalone cache table. PayloadJson stored as `jsonb`. |

Migration name: `InitialSchema` (file: `Infrastructure/Migrations/20260620133234_InitialSchema.cs`).
To apply: `dotnet ef database update --project Infrastructure --startup-project WebApi --context WriteDbContext` (requires PostgreSQL running).

**Phase 1.4 Trips API note (2026-07-05)**: No new migration was needed for the Trips API (F3-US1/US2/US3/US7/US8/US10). All `Trips`, `ItineraryDays`, and `TripDestinations` tables, indexes, and cascade rules were already present in `InitialSchema`. The SetNull cascade on `TripDestination.ItineraryDayId` is the mechanism that moves destinations to "Saved Places" when an `ItineraryDay` is soft-deleted during a date-range reduction (F3-US2 warn behavior).

### Deviation: UserToken vs. RefreshTokens
The design doc specifies a `RefreshTokens(Id, UserId, TokenHash, ExpiresAt, RevokedAt)` table.
The actual implementation uses `UserTokens` (ASP.NET Identity's `IdentityUserToken<Guid>` extended
with `DeviceUuid`, `RefreshToken`, `DeviceInfo`, `LocationInfo`). This supports multi-device sessions.
When building Phase 1.1 (register/verify), keep using `UserTokens` for token storage — do NOT
create a separate `RefreshTokens` table.

### Deviation: Users table
The design doc specifies `Users(Id, Email, PasswordHash, EmailVerified, CreatedAt)`.
Actual: ASP.NET Identity `AspNetUsers` which includes all Identity columns plus custom `FirstName`,
`LastName`, `IsDeleted`, `ResetPasswordToken`, `ResetPasswordExpiration`.
Add `EmailVerified` as `EmailConfirmed` — this **already exists** on `IdentityUser` (`EmailConfirmed bool`).
No separate `EmailVerificationTokens` table exists yet — build it in Phase 1.1.

### EF Core setup
- `WriteDbContext` (for mutations) and `ReadDbContext` (for queries) — both in `Infrastructure/DataAccess/DbContexts/`.
- Entity configurations via fluent API in `Infrastructure/DataAccess/Configurations/`.
- Migrations are output to `Infrastructure/Migrations/` (EF default; note the DataAccess/Migrations folder exists but is currently empty).
- Quartz has a **separate DbContext** (`QuartzContext`) and separate migrations in `Infrastructure/BackgroundHandler/`.
- `AutoMigration: true` in `appsettings.json` runs EF migrations on startup in dev.
- `BaseDbContext.OnModelCreating` now calls `base.OnModelCreating(builder)` first; this is required for .NET 10 Identity which adds `IdentityPasskeyData` (fixed in branch `feat/db-mvp-schema-phase1`).
