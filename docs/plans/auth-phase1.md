# Auth Phase 1.1 — F4 US1–US4 Task Plan

> Scope: AUGMENT existing auth. Do NOT rebuild login, logout, refresh, or the User/UserToken model.
> Phase 1.0 delivered: EmailVerificationToken entity + migration, all trip schema.
> Target branch prefix: `feature/auth-us<N>-<slug>`

## State of the world (what already exists)

**Implemented and working:**
- `PUT /api/v1/auth/logout` — revokes `UserToken` row by composite key; AC T6 is satisfied (uses `PUT`).
- `POST /api/v1/auth/login` — uses `username` identifier, full device-tracking, lockout. Needs augmenting to accept email (US3).
- `PUT /api/v1/auth/refresh` — existing; no changes needed.
- `EmailVerificationToken` entity, EF configuration, migration — present and live.
- `IEmailService` / `EmailService` — SMTP template engine; used by `ForgotPassword` and new-device flows.
- `CommonHelper.IsValidEmail()`, `ValidatePasswordPolicy()` (regex: min 8 + upper + lower + digit + special), `GenerateBase64GuidToken()` — all reusable.
- `AuthControllerMsg`, `ResultRes<T>`, `BaseController`, `AuthMapper` — follow project conventions.

**Not yet implemented:**
- `POST /api/v1/auth/register` (F4-US1)
- `GET /api/v1/auth/verify-email?token=` (F4-US2)
- Login accepting email as identifier (F4-US3 gap)
- Email verification HTML template
- `appsettings` key `Security:Email:EmailVerificationNotification`

---

## FR Coverage Map

| FR | User Story | Tasks |
|----|-----------|-------|
| F4-US1 | Sign up with email and password | T-01, T-02, T-03, T-05, T-09 |
| F4-US2 | Verify email to activate account | T-01, T-02, T-04, T-06, T-09 |
| F4-US3 | Log in with email and password | T-07 |
| F4-US4 | Log out | T-08 |
| All | Integration verification + doc update | T-10 |

---

## Wave 1 — Shared foundations (all parallel-safe)

### T-01 [F4-US1, F4-US2] Add error-code constants for Register and VerifyEmail

**File:** `Domain/Messages/AuthControllerMsg.cs`

**Acceptance criteria:**
- Gains two new nested structs: `Register` and `VerifyEmail`.
- `Register` contains: `EmailRequired`, `PasswordRequired`, `FirstNameRequired`, `InvalidEmail`, `PasswordTooWeak`, `EmailTaken`, `RegistrationFailed`, `Exception`.
- `VerifyEmail` contains: `TokenRequired`, `TokenInvalid`, `TokenExpired`, `AlreadyVerified`, `Exception`.
- No other file changes. `dotnet build` passes.

**Depends on:** none  
**Parallel-safe:** yes

---

### T-02 [F4-US2] Add email verification template and appsettings key

**Files:**
- `WebApi/wwwroot/emails/Email_Verification_Template.html` — new file with placeholders `{{FirstName}}` and `{{Url}}`.
- `appsettings.json` — gains `Security:Email:EmailVerificationNotification` with sub-keys `Subject`, `Path`, `Url`.
- `Domain/Constants/ConfigKeys.cs` — `Security.Email` struct gains `const string EmailVerificationNotification`.

**Acceptance criteria:**
- Template file loads correctly when its path is read via `File.ReadAllText`.
- `dotnet build` passes.

**Depends on:** none  
**Parallel-safe:** yes

---

### T-07 [F4-US3] Augment LoginCommandHandler to accept email as identifier

**File:** `Application/Features/Auth/Commands/LoginCommand/LoginCommandHandler.cs`

**Acceptance criteria:**
- If `request.Username` contains `@`, call `userManager.FindByEmailAsync(request.Username)` first; if `null`, fall through to `FindByNameAsync`.
- Existing lockout, device-tracking, new-device email, token issuance logic unchanged.
- `LoginReq.cs` unchanged (field remains `Username string?`); callers may pass an email in the `username` field.
- All existing error codes remain; no new constants needed.
- No PII (email value) logged. `dotnet build` passes.

**Depends on:** none  
**Parallel-safe:** yes

---

### T-08 [F4-US4] Verify logout acceptance criteria and reconcile doc

**Acceptance criteria:**
- Review `LogoutCommandHandler`: confirms it deletes the `UserToken` row — session ends, JWT refresh is blocked. ACs (1)–(4) of F4-US4 are met.
- If `docs/API.md` lists `POST /auth/logout`, update to `PUT /api/v1/auth/logout`.
- Confirm `[Authorize]` is applied (via `BaseController`) — no `[AllowAnonymous]` on the action.
- Confirm guard clauses for empty `token`/`refreshToken` are present in the controller.
- No handler code changes expected.

**Depends on:** none  
**Parallel-safe:** yes

---

## Wave 2 — Application layer: RegisterCommand (depends on T-01 + T-02)

### T-03 [F4-US1] Implement RegisterCommand + Handler

**File:** `Application/Features/Auth/Commands/RegisterCommand/RegisterCommand.cs`

**Acceptance criteria:**
- `RegisterCommand` is a `record` implementing `ICommand<string>` (returns error-code string; empty = success).
- Properties: `Email` (string?), `Password` (string?), `FirstName` (string?), `LastName` (string?).
- `RegisterCommandHandler` logic (in order):
  1. `userManager.FindByEmailAsync(email)` — if found → return `AuthControllerMsg.Register.EmailTaken` (generic; prevents enumeration).
  2. `new User { UserName = email, Email = email, FirstName = firstName, LastName = lastName ?? "" }`.
  3. `userManager.CreateAsync(user, password)` — on failure → return `AuthControllerMsg.Register.RegistrationFailed`.
  4. `CommonHelper.GenerateBase64GuidToken()` for verification token.
  5. Persist `EmailVerificationToken { UserId = user.Id, Token = token, ExpiresAt = UtcNow + 24h }` via `IWriteUnitOfWork.GetRepository<EmailVerificationToken>()`.
  6. `IEmailService.SendEmail(...)` using `ConfigKeys.Security.Email.EmailVerificationNotification` template.
  7. Return `string.Empty` on success.
- Handler does NOT log token values, passwords, or any PII.
- All dependencies are constructor-injected interfaces (unit-testable).

**Depends on:** T-01, T-02  
**Parallel-safe:** no (sequential after Wave 1)

---

## Wave 3 — Application layer: VerifyEmailCommand (depends on T-01, parallel with T-03)

### T-04 [F4-US2] Implement VerifyEmailCommand + Handler

**File:** `Application/Features/Auth/Commands/VerifyEmailCommand/VerifyEmailCommand.cs`

**Acceptance criteria:**
- `VerifyEmailCommand` is a `record` implementing `ICommand<string>`.
- Properties: `Token` (string?).
- `VerifyEmailCommandHandler` logic (in order):
  1. Query `EmailVerificationToken` where `Token == request.Token` (soft-delete filter applies).
  2. If null → return `AuthControllerMsg.VerifyEmail.TokenInvalid`.
  3. If `token.ConsumedAt != null` → return `AuthControllerMsg.VerifyEmail.AlreadyVerified`.
  4. If `token.ExpiresAt < DateTimeOffset.UtcNow` → return `AuthControllerMsg.VerifyEmail.TokenExpired`.
  5. `userManager.FindByIdAsync(token.UserId.ToString())`.
  6. `user.EmailConfirmed = true`; `userManager.UpdateAsync(user)`.
  7. `token.ConsumedAt = DateTimeOffset.UtcNow`; update token; `SaveChanges()`.
  8. Return `string.Empty`.
- Token value is never logged.

**Depends on:** T-01  
**Parallel-safe:** yes (can run in parallel with T-03 once T-01 is done)

---

## Wave 4 — API layer: controller actions (T-05 depends on T-03; T-06 depends on T-04)

### T-05 [F4-US1] Add RegisterReq model and Register action

**Files:**
- `WebApi/Models/Requests/Auth/RegisterReq.cs` — new: `Email`, `Password`, `FirstName`, `LastName` (all `string?`; add `[MaxLength]` consistent with domain).
- `WebApi/Controllers/v1/AuthController.cs` — add `Register` action.

**Acceptance criteria:**
- `[HttpPost("register")]`, `[AllowAnonymous]`.
- `ISender` injected as **action-method parameter** (not constructor).
- Guard clauses (no FluentValidation):
  - `IsNullOrWhiteSpace(Email)` → `BadRequest` + `AuthControllerMsg.Register.EmailRequired`.
  - `!IsValidEmail(Email)` → `BadRequest` + `AuthControllerMsg.Register.InvalidEmail`.
  - `IsNullOrWhiteSpace(Password)` → `BadRequest` + `AuthControllerMsg.Register.PasswordRequired`.
  - `!ValidatePasswordPolicy(Password)` → `BadRequest` + `AuthControllerMsg.Register.PasswordTooWeak`.
  - `IsNullOrWhiteSpace(FirstName)` → `BadRequest` + `AuthControllerMsg.Register.FirstNameRequired`.
- Maps `RegisterReq → RegisterCommand` via `Mapper.Map<RegisterCommand>(request)`.
- Non-empty errorCode from handler → `BadRequest(response)`.
- Success → `StatusCode(201, response)` with `Success = true`.
- Catch block: log exception (no request body), return `InternalServerError` + `AuthControllerMsg.Register.Exception`.

**Depends on:** T-03  
**Parallel-safe:** no

---

### T-06 [F4-US2] Add VerifyEmail action to AuthController

**File:** `WebApi/Controllers/v1/AuthController.cs`

**Acceptance criteria:**
- `[HttpGet("verify-email")]`, `[AllowAnonymous]`.
- `[FromQuery] string? token` parameter.
- `ISender` injected as action-method parameter.
- Guard: `IsNullOrWhiteSpace(token)` → `BadRequest` + `AuthControllerMsg.VerifyEmail.TokenRequired`.
- Sends `new VerifyEmailCommand { Token = token }`.
- Non-empty errorCode → `BadRequest(response)`.
- Success → `Ok(response)` with `Success = true`.
- Catch block → `InternalServerError` + `AuthControllerMsg.VerifyEmail.Exception`.
- Token value is never logged.

**Depends on:** T-04  
**Parallel-safe:** yes (parallel to T-05 once T-04 done)

---

## Wave 5 — AutoMapper wiring (depends on T-05 + T-06)

### T-09 [F4-US1, F4-US2] Wire AutoMapper and verify DI

**File:** `WebApi/Mappers/AuthMapper.cs`

**Acceptance criteria:**
- Gains `CreateMap<RegisterReq, RegisterCommand>()`.
- MediatR handlers auto-registered via `AddMediatR` assembly scan for `Application` assembly (verify; no manual registration needed).
- Swagger/OpenAPI shows `POST /api/v1/auth/register` and `GET /api/v1/auth/verify-email` in the v1 group.
- `dotnet build` passes with zero warnings on new files.

**Depends on:** T-05, T-06  
**Parallel-safe:** no

---

## Wave 6 — Integration verification and doc update (depends on T-07, T-08, T-09)

### T-10 [All] End-to-end smoke test and docs update

**Acceptance criteria:**

Smoke test passes all steps:
1. `POST /api/v1/auth/register` (valid body) → 201, `success: true`.
2. `POST /api/v1/auth/register` (same email) → 400, `errorCode: "Auth.Register.EmailTaken"`.
3. `POST /api/v1/auth/register` (weak password) → 400, `errorCode: "Auth.Register.PasswordTooWeak"`.
4. `GET /api/v1/auth/verify-email?token=<valid>` → 200, `success: true`.
5. `GET /api/v1/auth/verify-email?token=<consumed>` → 400, `errorCode: "Auth.VerifyEmail.AlreadyVerified"`.
6. `GET /api/v1/auth/verify-email?token=<bogus>` → 400, `errorCode: "Auth.VerifyEmail.TokenInvalid"`.
7. `POST /api/v1/auth/login` (email in `username` field) → 200, tokens returned.
8. `PUT /api/v1/auth/logout` (valid tokens) → 200, `success: true`; subsequent refresh with revoked token → fails.

Documentation:
- `docs/API.md` reconciliation table: Register and VerifyEmail change from ❌ to ✅; Login row notes email-in-username-field support; Logout row corrected to `PUT`.
- No secrets or PII appear in log output during the smoke test.

**Depends on:** T-09, T-07, T-08  
**Parallel-safe:** no

---

## Dependency graph (acyclic)

```
T-01 ──► T-03 ──► T-05 ──┐
     └──► T-04 ──► T-06 ──┤
T-02 ──► T-03             │
T-07 (independent) ───────┤
T-08 (independent) ───────┤
                          ▼
                         T-09 ──► T-10
```

## Wave summary for orchestrator

| Wave | Tasks | Parallelism |
|------|-------|-------------|
| 1 | T-01, T-02, T-07, T-08 | All four in parallel |
| 2 | T-03 | Sequential (needs T-01 + T-02) |
| 3 | T-04 | Sequential (needs T-01); overlaps with T-03 if T-01 is done |
| 4 | T-05, T-06 | Parallel pair (T-05 needs T-03; T-06 needs T-04) |
| 5 | T-09 | Sequential (needs T-05 + T-06) |
| 6 | T-10 | Sequential (needs T-09 + T-07 + T-08) |

---

## Key files for implementation

| File | Task | Action |
|------|------|--------|
| `Domain/Messages/AuthControllerMsg.cs` | T-01 | Add `Register` + `VerifyEmail` constant structs |
| `WebApi/wwwroot/emails/Email_Verification_Template.html` | T-02 | New template file |
| `appsettings.json` + `ConfigKeys.cs` | T-02 | New appsettings key + constant |
| `Application/Features/Auth/Commands/RegisterCommand/RegisterCommand.cs` | T-03 | New command + handler |
| `Application/Features/Auth/Commands/VerifyEmailCommand/VerifyEmailCommand.cs` | T-04 | New command + handler |
| `WebApi/Models/Requests/Auth/RegisterReq.cs` | T-05 | New request model |
| `WebApi/Controllers/v1/AuthController.cs` | T-05, T-06 | Add Register + VerifyEmail actions |
| `Application/Features/Auth/Commands/LoginCommand/LoginCommandHandler.cs` | T-07 | Add email fallback |
| `WebApi/Mappers/AuthMapper.cs` | T-09 | Add RegisterReq → RegisterCommand map |
| `docs/API.md` | T-10 | Update reconciliation table |

---

## Risks and decisions

1. **Password policy**: `ValidatePasswordPolicy()` requires uppercase + lowercase + digit + special + min 8 chars (stricter than "min 8" in PRD). Using the existing helper keeps internal consistency. Communicate to product owner — `"password1"` will return `PasswordTooWeak`.

2. **`User.FirstName` required**: Domain entity has `required string FirstName`. Register must collect it. If product owner removes this requirement, the entity must change (separate task, out of scope here).

3. **Token expiry**: Plan uses 24h hardcoded. Consider adding `Security:Jwt:EmailVerificationExpirationHours` to `ConfigKeys` + `appsettings.json` for parity with `ResetPasswordExpirationHours`.

4. **`EmailConfirmed` not yet enforced on login**: After US2, users with `EmailConfirmed = false` can still log in. The existing `LoginCommandHandler` does not check `EmailConfirmed`. If gating is required, add a check returning `AuthControllerMsg.Login.InActive` — but this is out of scope for the current US3 ACs.

5. **`UserName` set to email**: Setting `UserName = email` means `NormalizedUserName` and `NormalizedEmail` are the same normalized string. Safe with existing `UserConfiguration` unique index on `NormalizedUserName`.
