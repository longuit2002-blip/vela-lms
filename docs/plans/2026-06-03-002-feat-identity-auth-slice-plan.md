---
date: 2026-06-03
type: feat
status: active
origin: docs/brainstorms/2026-06-03-lms-identity-auth-requirements.md
---

# feat: Identity + Auth Slice (Phase 0)

## Summary

Add a tenant-bound `User` identity on top of the walking skeleton: password login issuing an RS256 access JWT plus a rotating refresh token (with reuse detection), logout, authenticated change-password with a forced first-login change, and tenant context threaded through every request via request-resolved org claim → EF Core global query filter → a Postgres RLS scaffold. An idempotent dev seeder provisions one Organization + its OrgOwner so the flow is demo-able, and a same-origin Next.js proxy fronts real login + forced-change pages that gate the web app.

---

## Problem Frame

The walking skeleton proved the four Clean Architecture layers compose, but the system has no notion of *who* is calling: `Organization` is a bare record, every endpoint is open, and the web app lists/creates organizations across all tenants with no login. Phase 0's foundation goal — "admin creates org, imports user, logs in" — cannot start until there is an authenticated identity tied to a tenant and a request pipeline that knows which organization a caller belongs to.

This slice builds that identity-and-tenant spine and nothing wider. Member management, the department/position org tree, and the full RBAC/ABAC matrix are deliberately held to later slices (see origin: `docs/brainstorms/2026-06-03-lms-identity-auth-requirements.md`). Pulling them forward would reintroduce the breadth-before-proof risk the walking skeleton retired.

---

## Origin Document

This plan is sourced from `docs/brainstorms/2026-06-03-lms-identity-auth-requirements.md`. It carries forward that document's problem frame, actors (A1 developer/seeder, A2 OrgOwner, A3 API/system, A4 platform-operator-deferred), key flows (F1 login→authenticated→refresh→logout, F2 forced first-login change, F3 tenant isolation), all 20 requirements (R1–R20), acceptance examples (AE1–AE6), success criteria, scope boundaries, and key decisions. The brainstorm's seven "Deferred to Planning" technical questions are resolved in Key Technical Decisions below.

---

## Key Technical Decisions

- **Stack mirrors the walking skeleton exactly.** martinothamar **Mediator** (not MediatR) — handlers return `ValueTask<Result<T>>`, behaviors implement its `IPipelineBehavior` constrained to `IMessage`; **Ardalis.Result** → ProblemDetails; **FluentValidation** via the existing `ValidationBehavior` (throws → 422); UUID v7 via `Medo.Uuid7` behind `IIdGenerator`; **Minimal API** endpoint groups under `/api/v1`; snake_case EF mappings; problem `type` URIs under `https://errors.vela.app/...`; xUnit v3 with hand-written fakes (no Moq/FluentAssertions). All new auth crypto/JWT/EF types live in Infrastructure/Api — **never** in Application — or the NetArchTest dependency-rule tests go red.
- **Tenant context is the keystone abstraction.** Introduce `ICurrentUser` / `ITenantContext` (interfaces, no HTTP/EF types) in `Lms.Application/Abstractions`, implemented in `Lms.Api` against `IHttpContextAccessor` reading the `sub`/`org` claims. Both endpoints and the DbContext/interceptor depend on the abstraction, not `HttpContext`.
- **`organizations` is the tenant root, not a tenant-filtered table.** Only `users` and `refresh_tokens` carry the EF global query filter and RLS policy. The "my organization" view is a by-id lookup keyed on the JWT `org` claim. This resolves the brainstorm's open question (see origin Deferred-to-Planning, first item).
- **Tenant isolation = three coordinated layers, with `SET LOCAL` in a per-request transaction.** (1) A scoped `ITenantContext` resolves org from the JWT `org` claim only (never body/subdomain); (2) EF Core global query filter on `users`/`refresh_tokens` capturing the org id as a scalar on the DbContext (not a service call inside the lambda); (3) the tenant GUC is applied with `SET LOCAL app.current_org = <org>` **inside an explicit per-request transaction** (a unit-of-work opened once the principal is known) — *not* a connection-`SET` on open. `SET LOCAL` is transaction-scoped, so nothing leaks across pooled-connection reuse and there is no dependence on `DISCARD ALL` erasing/preserving a session value (the reviewers showed the session-`SET` approach both leaks and self-erases on multi-connection scopes, and violates R10's "within its transaction scope"). The migration emits `ENABLE` + `FORCE ROW LEVEL SECURITY` + policies keyed on `current_setting('app.current_org', true)::uuid` — the **two-arg `missing_ok=true` form** so an unset GUC yields NULL → matches no rows (fail-closed) instead of throwing.
- **Two DB roles.** Provision a **non-owner, non-superuser app role** (subject to RLS under `FORCE`) used by the API at runtime *and by the isolation tests*, plus a separate **`BYPASSRLS` maintenance role** used only for migrations, seeding, and the pre-auth login credential lookup. This resolves the chicken-and-egg the reviewers flagged: under `FORCE`, the owner/superuser is *not* exempt, so (a) the login email lookup runs before any tenant is known and would otherwise return zero rows, and (b) the seeder/migrations write rows with no tenant context. Both run as the bypass role; all ordinary request traffic uses the RLS-subject app role. `IgnoreQueryFilters` only bypasses the EF filter, never RLS — so the bypass role, not `IgnoreQueryFilters`, is what makes login's user lookup work.
- **Refresh rotation with reuse detection + grace window.** Store SHA-256 of the opaque refresh token (not Argon2 — high-entropy random token). Model a token family (`family_id`, `parent_id`, `replaced_by_id`, `used_at`, `revoked_at`, `revoked_reason`). Rotation consumes atomically via a guarded `UPDATE ... WHERE used_at IS NULL AND revoked_at IS NULL ... RETURNING <cols>` issued as **raw parameterized SQL** (`FromSqlInterpolated` / `Database.SqlQuery<T>`) — *not* `ExecuteUpdateAsync`, which returns only a row count and cannot project the consumed row needed for the grace branch, and *not* EF read-modify-write. On a consumed/revoked token within a **short grace window (~5–10s, not 30s)** whose child is still unused, **return the exact same access+refresh pair already minted at rotation time** (cached/looked-up via `replaced_by_id`) — do **not** mint a second independently-usable token; otherwise revoke the whole family. This narrows the reviewer-flagged replay window: a stolen-parent replay within grace yields only the already-issued pair, never a fresh divergent one. **Accepted residual risk:** the grace window is a deliberate, time-bounded softening of origin R5's "any reuse revokes the family" — mitigated by HttpOnly cookie (JS can't read it), ~15-min access TTL, and the short window. Documented in Risk Analysis. The whole consume → grace-check → replay-or-revoke sequence is owned by **one repository method** that manages its own transaction (Infrastructure owns `BeginTransaction`), so the Application handler never touches transaction boundaries.
- **Refresh re-sources roles from the user row.** On each rotation the new access token's `roles`/status are read fresh from the `users` row, never copied from the presented token — otherwise a 14-day refresh would carry stale roles and hand the future RBAC slice a multi-day authorization-lag bug. Cheap now, removes a latent landmine.
- **Password hashing: Argon2id via Konscious**, behind an `IPasswordHasher` port (so it's swappable). PHC string format (`$argon2id$v=19$m=19456,t=2,p=1$...`), 16-byte salt, 32-byte output; rehash-on-login when stored params are weaker than current policy. (Konscious's latest release predates .NET 10 but runs via netstandard; the port isolates the risk.)
- **JWT: RS256 via `JsonWebTokenHandler`**, single configured key pair (dev key from config/user-secrets; prod key from a secret store). JwtBearer validation with `ValidateIssuer/Audience/Lifetime/IssuerSigningKey = true`, `ClockSkew = TimeSpan.FromMinutes(1)`, `MapInboundClaims = false` (preserve raw `sub`/`org`/`roles`). Access ~15 min, refresh ~14 days. *Deferred:* JWKS endpoint + `kid` rotation.
- **Lockout: per-account only.** Attempt counter + lock-until timestamp on the user row; ~5 failed attempts → temporary lock. Per-IP throttling and its Redis dependency are deferred. Login responses are enumeration-safe (generic invalid-credentials, uniform timing).
- **Web↔API: same-origin via Next.js rewrites (BFF proxy).** Browser talks only to the Next.js origin, which proxies to the API, so the refresh cookie is `HttpOnly; Secure; SameSite=Lax`, **`Path` set to the exact refresh endpoint route (`/api/v1/auth/refresh`)** and **no `Domain` attribute** (origin-only; never `.vela.app`, which would share it across subdomains). CSRF defense for this slice is **`SameSite=Lax` + a Fetch-Metadata (`Sec-Fetch-Site`) check** on `/api/v1/auth/*` mutating endpoints — a single middleware condition, no new abstraction. A signed double-submit token is **deferred** (it was not in origin scope, and `SameSite=Lax` + Fetch-Metadata is sufficient for a same-origin BFF; revisit if a cross-origin model is ever adopted). Access token lives in memory; a single-flight interceptor refreshes on 401. API CORS gains `AllowCredentials()` + explicit `WithOrigins` (not `AllowAnyOrigin`, which CORS forbids with credentials) for direct (non-proxied) dev calls.
- **Audit logging is deferred with an explicit note.** T5 §7 mandates audit records for login/logout/password-change, which this slice implements. To avoid losing incident-response history, a minimal `audit_logs` write is a follow-up item (see Deferred to Follow-Up Work) — called out here rather than silently skipped so it is not mistaken for an oversight against T5.
- **Seeding: idempotent dev-only seeder**, config-driven (org name, owner email, default password), invoked by a guarded dev startup hook / command — not an EF migration. It sets tenant context to the new org before inserting the owner so the `users` `WITH CHECK` RLS policy passes under `FORCE`.
- **Logout / change-password revoke refresh tokens; no immediate access-token invalidation** — the ~15 min access TTL is accepted as the revocation lag (origin Deferred-to-Planning, last item).

---

## High-Level Technical Design

*This illustrates the intended request/tenant flow and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

```
Browser ──(same-origin)──> Next.js (rewrites /api/* ) ──> Lms.Api
                                                            │
   Request (Bearer access JWT, refresh cookie on /auth/*)   │
        │                                                    ▼
   UseAuthentication (JwtBearer RS256, MapInboundClaims=false)
        │  → ClaimsPrincipal { sub, org, roles }
        ▼
   Scoped ITenantContext/ICurrentUser  (reads sub/org via IHttpContextAccessor)
        │            │
        │            ├─> AppDbContext captures _orgId  ──> global query filter (users, refresh_tokens)
        │            └─> per-request transaction: SET LOCAL app.current_org = <org>
        ▼                                                   │   (RLS-subject app role)
   Mediator pipeline (ValidationBehavior → handler)          ▼
        │                                   Postgres RLS (FORCE) on users/refresh_tokens
        │                                   current_setting('app.current_org', true) → fail-closed
        ▼
   Ardalis.Result → ToMinimalApiResult / ProblemDetails
```

Refresh rotation (single transaction):

```
present refresh token
  → SHA-256 → guarded UPDATE ... WHERE used_at IS NULL AND revoked_at IS NULL RETURNING
       ├─ 1 row  → won: insert child (same family_id, parent_id), return new access+refresh
       └─ 0 rows → already consumed:
              ├─ child unused AND parent.used_at within grace (~30s) → replay child pair (benign)
              └─ else → revoke entire family (reuse_detected), reject 401
```

---

## Output Structure

New/changed source shape (per-unit `**Files:**` are authoritative):

```
src/
  Lms.Domain/
    Users/                  User.cs, UserStatus.cs
    Identity/               RefreshToken.cs (token-family aggregate)
  Lms.Application/
    Abstractions/           ICurrentUser.cs, ITenantContext.cs, IPasswordHasher.cs,
                            IJwtTokenIssuer.cs, IUserRepository.cs, IRefreshTokenRepository.cs
    Auth/
      Commands/             Login, RefreshToken, Logout, ChangePassword (+ validators)
      Queries/              GetMyOrganization
      Dtos/                 AuthTokensDto, MyOrganizationDto
  Lms.Infrastructure/
    Security/               Argon2idPasswordHasher.cs, JwtTokenIssuer.cs, RefreshTokenHasher.cs
    Persistence/
      Configurations/       UserConfiguration.cs, RefreshTokenConfiguration.cs
      Interceptors/         TenantConnectionInterceptor.cs
      UserRepository.cs, RefreshTokenRepository.cs
      Migrations/           (users + refresh_tokens; RLS enable/force/policies)
    Identity/               HttpTenantContext.cs  (or in Lms.Api)
    Seeding/                IdentitySeeder.cs
  Lms.Api/
    Endpoints/              AuthEndpoints.cs, (MyOrganization endpoint; retire org list/create)
    Auth/                   cookie + fetch-metadata/CSRF helpers
web/
  next.config (rewrites), app/(auth)/login, app/(auth)/change-password,
  lib/auth (in-memory token + single-flight refresh interceptor), org page gate
```

---

## Implementation Units

Grouped into four phases. Units are dependency-ordered; U-IDs are stable.

### Phase A — Foundation

### U1. Auth packages, configuration, and dev RSA key

**Goal:** Add the auth dependencies and configuration surface so later units compile and read settings consistently.
**Requirements:** Enables R2, R3, R14 (config substrate).
**Dependencies:** none.
**Files:**
- `Directory.Packages.props` (add `Microsoft.AspNetCore.Authentication.JwtBearer` + `Microsoft.IdentityModel.JsonWebTokens` at the .NET 10 rc band; **pin `Konscious.Security.Cryptography.Argon2` at its actual latest published version** — it tracks netstandard, not the SDK band, so "rc-band" is meaningless for it)
- `src/Lms.Api/appsettings.json`, `src/Lms.Api/appsettings.Development.json` (`Jwt` issuer/audience/lifetimes/key path, `Lockout` attempts/window, `Seed` org/owner/password sections, `RateLimit` login window, two `ConnectionStrings` — app role + `BYPASSRLS` maintenance role)
- `src/Lms.Api/Auth/AuthOptions.cs` (strongly-typed options records + validation)
- `.env.example` (JWT key material location, seed credentials), `docker-compose.yml` only if a key volume is needed
- `web/next.config.*` is **not** here (see U9)

**Approach:** Bind options with `AddOptions<T>().Bind().ValidateOnStart()`. Dev RSA private key loaded from a PEM file path or user-secrets; never commit a real key. Public key derived for validation. Keep all packages registered in DI only as later units need them.
**Patterns to follow:** existing `appsettings.json` `Section:Key` convention; `Program.cs` connection-string read-or-throw.
**Test scenarios:**
- Options validation fails fast when a required `Jwt`/`Seed` value is missing (ValidateOnStart throws at boot).
- Dev key loader produces a usable `RsaSecurityKey` from the configured PEM.
- `Test expectation: none` for the package-reference and `.env.example` changes (pure manifest).

### U2. User and RefreshToken domain aggregates

**Goal:** Model identity and the refresh-token family in the Domain layer with invariants and behavior, no infrastructure concerns.
**Requirements:** R1; supports R2–R8.
**Dependencies:** none (mirrors `Organization`).
**Files:**
- `src/Lms.Domain/Users/User.cs`, `src/Lms.Domain/Users/UserStatus.cs`
- `src/Lms.Domain/Identity/RefreshToken.cs`
- `tests/Lms.Domain.UnitTests/UserTests.cs`, `tests/Lms.Domain.UnitTests/RefreshTokenTests.cs`

**Approach:** `User` = `sealed partial class : Entity, IAggregateRoot`; private ctors; static `Create(Guid id, Guid orgId, string email, string passwordHash, IReadOnlyCollection<string> roleCodes, bool mustChangePassword)` validating email/org/hash invariants. Behavior methods: `VerifyCanLogin()` (status/lock checks), `RecordFailedLogin(maxAttempts, lockWindow, now)` (increments counter, sets lock-until), `RecordSuccessfulLogin(now)` (resets counter), `ChangePassword(newHash, now)` (clears `mustChangePassword`), enum-as-string `UserStatus { Active, Locked, Disabled }`. Lockout fields (`AccessFailedCount`, `LockoutEndsAt`) live on the aggregate. `RefreshToken` carries `FamilyId`, `TokenHash`, `UserId`, `OrganizationId`, `ParentId`, `ReplacedById`, `IssuedAt`, `ExpiresAt`, `UsedAt`, `RevokedAt`, `RevokedReason`; behavior `MarkRotated(childId, now)`, `Revoke(reason, now)`, `IsActive(now)`. Id supplied by caller (UUID v7 via `IIdGenerator`), per the Organization comment.
**Patterns to follow:** `src/Lms.Domain/Organizations/Organization.cs` (factory + private setters + `ArgumentException` on invariant violation); `src/Lms.Domain/SeedWork/Entity.cs`.
**Test scenarios:**
- Happy: `User.Create` with valid inputs sets timestamps, `Active`, supplied roles; equality by id.
- Edge/invariant: empty email, empty hash, empty org → `ArgumentException`.
- `RecordFailedLogin` reaching the threshold sets `LockoutEndsAt`/`Locked`; a successful login resets the counter.
- `Covers AE3.` `ChangePassword` clears `mustChangePassword`.
- `RefreshToken.IsActive` is false when expired, used, or revoked; `MarkRotated` sets `UsedAt`/`ReplacedById`; `Revoke` is idempotent on an already-revoked token.

### U3. Persistence for users and refresh_tokens

**Goal:** EF configurations, repository ports + implementations, and the table migration — before any RLS is layered on.
**Requirements:** R1; substrate for R2–R8.
**Dependencies:** U2.
**Files:**
- `src/Lms.Application/Abstractions/IUserRepository.cs`, `src/Lms.Application/Abstractions/IRefreshTokenRepository.cs`
- `src/Lms.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `RefreshTokenConfiguration.cs`
- `src/Lms.Infrastructure/Persistence/UserRepository.cs`, `RefreshTokenRepository.cs`
- `src/Lms.Infrastructure/Persistence/AppDbContext.cs` (add `DbSet`s)
- `src/Lms.Infrastructure/DependencyInjection.cs` (register repositories)
- `src/Lms.Infrastructure/Persistence/Migrations/` (new migration: `users`, `refresh_tokens`)
- `tests/Lms.Api.IntegrationTests/` round-trip coverage (folded into U10)

**Approach:** Focused repository ports (no generic repository), each owning its `SaveChangesAsync`, mirroring `IOrganizationRepository`. `UserRepository` exposes `AddAsync`, **`FindByEmailForLoginAsync(email)`** (bypass-only — the single current consumer; org-scoped email lookup is added when member CRUD arrives, so the port stays exactly as wide as this slice needs), `FindByIdAsync`, `EmailExistsAsync`. `RefreshTokenRepository` exposes `AddAsync`, `FindByHashAsync`, a single transactional **`ConsumeAndRotateAsync`** method (raw guarded `UPDATE ... WHERE used_at IS NULL AND revoked_at IS NULL ... RETURNING` via `FromSqlInterpolated`/`SqlQuery` — **not** `ExecuteUpdateAsync`, which returns only a count) that internally handles the grace-replay vs family-revoke branch, plus `RevokeFamilyAsync(familyId, reason)`. snake_case tables/columns; `UserStatus` stored as string; unique index `(organization_id, email)` for users; unique index on `token_hash`, plus indexes on `family_id` and `(user_id, organization_id)` for refresh_tokens. FK `users.organization_id → organizations.id`.
**Patterns to follow:** `src/Lms.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs`, `src/Lms.Infrastructure/Persistence/OrganizationRepository.cs`, design-time `AppDbContextFactory`, `dotnet ef` via `.config/dotnet-tools.json`.
**Execution note:** Login email lookup runs *before* tenant context exists, and `users` is under `FORCE` RLS — so the lookup must run under the `BYPASSRLS` maintenance role/connection (see U5), **not** via `IgnoreQueryFilters`, which bypasses only the EF filter and leaves RLS blocking the row. Tenant-trapping the login path is the sharpest wall in this plan.
**Test scenarios:**
- User insert + read-back round-trips all columns; `(organization_id, email)` uniqueness rejects a duplicate.
- RefreshToken insert + `FindByHashAsync` returns the row; `token_hash` uniqueness enforced.
- Atomic-consume UPDATE returns the row on first call and zero rows on a second call against the same token (proves the guard).

### Phase B — Security services & tenancy

### U4. Argon2id password hasher and RS256 JWT issuer

**Goal:** Implement the two crypto services behind their Application ports.
**Requirements:** R2, R4, R7.
**Dependencies:** U1.
**Files:**
- `src/Lms.Application/Abstractions/IPasswordHasher.cs`, `IJwtTokenIssuer.cs`
- `src/Lms.Infrastructure/Security/Argon2idPasswordHasher.cs`, `JwtTokenIssuer.cs`, `RefreshTokenHasher.cs`
- `src/Lms.Infrastructure/DependencyInjection.cs` (register services + signing key)
- `tests/Lms.Application.UnitTests/Security/` or `tests/Lms.Infrastructure.UnitTests/` (new test project only if needed; otherwise Application unit tests with the real hasher)

**Approach:** `IPasswordHasher` = `Hash(string) → string (PHC)`, `Verify(hash, password) → (bool ok, bool needsRehash)`. Argon2id params m=19456/t=2/p=1, 16-byte salt, 32-byte output, PHC-encoded. `IJwtTokenIssuer.Issue(user, now) → (accessToken, expiresAt)` builds claims `sub`/`org`/`roles` via `JsonWebTokenHandler` + `SigningCredentials(RsaSecurityKey, RsaSha256)`. `RefreshTokenHasher` = SHA-256 of an opaque random token (generate 256-bit, return raw + hash). All three are Infrastructure; ports are Application.
**Patterns to follow:** `src/Lms.Infrastructure/DependencyInjection.cs` registration style; `Uuid7IdGenerator` as a singleton-service precedent.
**Test scenarios:**
- Hash→Verify round-trips; wrong password → false; tampered PHC → false.
- `needsRehash` true when stored params are below current policy.
- Issued JWT carries `sub`/`org`/`roles`, correct issuer/audience, ~15 min expiry; validates against the configured public key; an expired token fails validation with `ClockSkew=1m`.
- Refresh-token hasher: same input → same hash; raw token is high-entropy and not derivable from the hash.

### U5. Tenant context, EF global query filter, and RLS scaffold

**Goal:** Thread tenant identity from JWT claims into both the EF filter and a Postgres RLS policy on `users`/`refresh_tokens`. Highest-risk unit.
**Requirements:** R9, R10, R11, R12; advances F3.
**Dependencies:** U3.
**Files:**
- `src/Lms.Application/Abstractions/ICurrentUser.cs`, `ITenantContext.cs`
- `src/Lms.Api/Identity/HttpTenantContext.cs` (implements both via `IHttpContextAccessor`)
- `src/Lms.Infrastructure/Persistence/AppDbContext.cs` (constructor captures `_orgId`; `HasQueryFilter` on `User`/`RefreshToken`)
- `src/Lms.Infrastructure/Persistence/AppDbContextFactory.cs` (design-time path injects a **null-object `ITenantContext`** — no org — so migrations construct the context with no tenant)
- `src/Lms.Infrastructure/Persistence/TenantTransaction/` (per-request unit-of-work that opens a transaction and issues `SET LOCAL app.current_org` after the principal is known)
- `src/Lms.Infrastructure/Persistence/Migrations/` (new migration: `ENABLE`/`FORCE ROW LEVEL SECURITY` + policy on `users` and `refresh_tokens`; provision the RLS-subject app role + `BYPASSRLS` maintenance role / grants)
- `src/Lms.Api/Program.cs` + `src/Lms.Infrastructure/DependencyInjection.cs` (register `IHttpContextAccessor`, scoped tenant context, the tenant transaction; wire the two connection strings/roles)
- `tests/Lms.Api.IntegrationTests/WebAppFactory.cs` (its `ConfigureTestServices` DbContext re-registration and startup `MigrateAsync` must supply a null/disabled tenant context and run migrations/seed under the bypass role)
- `tests/Lms.Api.IntegrationTests/TenantIsolationTests.cs` (folded into U10)

**Approach:** `ITenantContext.OrganizationId` / `ICurrentUser.UserId,IsAuthenticated` read `org`/`sub` claims (raw names, since `MapInboundClaims=false`). `AppDbContext` takes `ITenantContext` in its constructor and captures `OrganizationId` into a nullable field referenced by `HasQueryFilter` (scalar capture, never a service call in the lambda; the field **must tolerate no-tenant** — null → filter matches nothing — for the design-time/test/login paths); use EF 10 named filters so a future soft-delete filter can coexist. Tenant GUC is applied with **`SET LOCAL app.current_org`** inside an explicit per-request transaction (opened once the principal is resolved), **not** a connection-open `SET`. Migration: for `users` and `refresh_tokens`, `ENABLE ROW LEVEL SECURITY`, `FORCE ROW LEVEL SECURITY`, and `CREATE POLICY ... USING (organization_id = current_setting('app.current_org', true)::uuid) WITH CHECK (organization_id = current_setting('app.current_org', true)::uuid)` — two-arg form so an unset GUC → NULL → no rows (fail-closed, no error). `organizations` gets **no** policy (tenant root; resolves origin Deferred-to-Planning Q1 — see Key Technical Decisions and the R12 traceability note).
**Execution note:** Start with a failing two-org integration test **that connects as the RLS-subject app role (never the superuser/owner, which `FORCE` does *not* exempt only if `BYPASSRLS` is absent — the default Testcontainers `postgres` superuser bypasses RLS and would false-green this test).** The test must also assert that a query with `app.current_org` unset returns **zero** rows (proving the policy is live and the connecting role is genuinely RLS-subject) before asserting cross-tenant invisibility. Wire this before the interceptor/policies.
**Patterns to follow:** EF multi-tenancy docs (scalar capture, DI lifetimes); existing `AppDbContext.OnModelCreating` `ApplyConfigurationsFromAssembly`.
**Technical design:** see High-Level Technical Design above (tenant flow). Keep the tenant transaction dependent on the abstraction, not `HttpContext`.
**Test scenarios:**
- `Covers F3 / AE5.` Connected as the RLS-subject app role: org-A user lists `users` → only org-A rows; a raw query with the EF filter bypassed still returns no org-B rows (RLS holds).
- **Role guard:** a `users` query with `app.current_org` unset returns zero rows (fails closed) — and fails loudly if run as a bypassing role (proves the test isn't false-greening).
- Unauthenticated request (no `org` claim) → tenant context reports no org; tenant-filtered queries return empty rather than leaking or throwing (two-arg `current_setting`).
- `SET LOCAL` value does not survive past its transaction (sequential operations in one scope each re-establish it; no stale-tenant carryover).
- A write for org A cannot insert a `users`/`refresh_tokens` row carrying org B's id (RLS `WITH CHECK` rejects).

### Phase C — Auth flows & API

### U6. Auth commands, endpoints, and middleware wiring

**Goal:** Implement login, refresh (rotation + reuse detection + grace), logout, and change-password, exposed as `/api/v1/auth/*`, with JwtBearer + tenant middleware wired in correct order and the forced-change gate enforced.
**Requirements:** R2, R3, R4, R5, R6, R7, R8, R9, R14 (server side); advances F1, F2.
**Dependencies:** U2, U4, U5.
**Files:**
- `src/Lms.Application/Auth/Commands/` (`LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `ChangePasswordCommand` + FluentValidation validators)
- `src/Lms.Application/Auth/Dtos/AuthTokensDto.cs`
- `src/Lms.Api/Endpoints/AuthEndpoints.cs`
- `src/Lms.Api/Auth/` (cookie writer, Fetch-Metadata + signed double-submit CSRF helper)
- `src/Lms.Api/Program.cs` (`AddAuthentication().AddJwtBearer(...)`, `AddAuthorization()`, `UseAuthentication`/`UseAuthorization` ordering after `UseCors`; CSRF/fetch-metadata filter on `/api/v1/auth/*`)
- `tests/Lms.Application.UnitTests/Auth/` handler tests; integration coverage folded into U10

**Approach:** Handlers return `Ardalis.Result`; validators throw via the existing `ValidationBehavior` → 422. **Login:** find user by email **via the `BYPASSRLS` maintenance path** (pre-auth, no tenant yet), check lock/status, verify password (rehash-on-login if needed), on failure `RecordFailedLogin` + generic 401, on success `RecordSuccessfulLogin`; **then establish tenant context for the looked-up user's org (open the per-request transaction and `SET LOCAL app.current_org`) before writing** — issue access JWT + mint refresh-token family root **inside that org-scoped transaction** (so the `refresh_tokens` `WITH CHECK` passes under `FORCE` RLS), set refresh cookie (`HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth/refresh`; **no `Domain`**), return access token + `mustChangePassword` flag. **Refresh:** the single-transaction `ConsumeAndRotateAsync` (atomic consume + short grace-window replay-of-the-same-pair + family-revoke; re-sources roles from the user row); sets a new cookie. **Logout:** revoke current refresh token, clear cookie. **ChangePassword:** `[Authorize]`, verify current password, set new hash, clear `mustChangePassword`, revoke all *other* refresh tokens of the user. **Forced-change gate:** an endpoint filter / fallback policy rejects authenticated calls (other than change-password and logout) with a `must-change-password` problem while the flag is set. **CSRF/abuse:** a Fetch-Metadata (`Sec-Fetch-Site`) check on `/api/v1/auth/*` mutating endpoints (no signed double-submit this slice), plus an interim coarse **ASP.NET Core built-in `RateLimiter`** fixed-window limit on `/api/v1/auth/login` (e.g. ~10/IP/min — no Redis) as a stopgap for credential-stuffing until the deferred per-IP throttler lands.
**Execution note:** Implement the refresh handler test-first against the concurrency/grace cases — it is the highest-risk logic. Test a cold login (no prior auth) actually persists a `refresh_tokens` row under `FORCE` RLS — the login-write-under-RLS path is the second-sharpest wall.
**Patterns to follow:** `src/Lms.Api/Endpoints/OrganizationEndpoints.cs` (`MapGroup` + `ISender` + `ToMinimalApiResult`); `src/Lms.Api/GlobalExceptionHandler.cs` problem-type scheme (`https://errors.vela.app/...`) — add `invalid-credentials`, `account-locked`, `must-change-password`, `refresh-reuse` types.
**Test scenarios:**
- `Covers AE1.` Valid login returns an access token whose `org` claim matches the user; refresh cookie is set.
- `Covers AE4.` After the configured failed-attempt threshold, the next attempt is rejected even with a now-correct password (locked).
- Login with a wrong password and login for a non-existent email return the **same** generic 401 (no enumeration).
- Refresh happy path rotates: old token consumed, new pair returned, family preserved.
- `Covers AE2.` Replaying a rotated token outside the grace window revokes the family and rejects; the previously-issued newest token then also fails.
- Grace window: replaying a just-rotated token within ~30s while its child is unused replays the same child pair (no family revoke).
- Logout revokes the current refresh token (subsequent refresh with it fails).
- ChangePassword with a wrong current password → rejected; success clears the flag and revokes other refresh tokens.
- `Covers AE3 / F2.` A `mustChangePassword` user calling any other protected endpoint is blocked until the change.
- Cold login (no prior auth) persists a `refresh_tokens` row under `FORCE` RLS (the post-auth `SET LOCAL` org-scoped write path works).
- Changing a user's role in the DB then refreshing yields a new access token reflecting the new role (refresh re-sources roles, not copies them).
- A login POST lacking `Sec-Fetch-Site: same-origin`/`same-site` is rejected; rapid repeated logins from one IP hit the interim rate limit.

### U7. Retire cross-tenant org endpoints → "my organization"

**Goal:** Replace the skeleton's open org list/create with an authenticated by-id "my organization" view.
**Requirements:** R15, R16; advances F3.
**Dependencies:** U5, U6.
**Files:**
- `src/Lms.Api/Endpoints/OrganizationEndpoints.cs` (remove list + create; add authenticated `GET /api/v1/organizations/me`)
- `src/Lms.Application/Auth/Queries/GetMyOrganizationQuery.cs` + `MyOrganizationDto`
- `src/Lms.Application/Organizations/` (remove/retire `CreateOrganization`/`ListOrganizations` or mark internal for seeding reuse)
- update/remove the integration tests covering the old endpoints (folded into U10)

**Approach:** `GET /api/v1/organizations/me` is `[Authorize]`, reads the org id from `ITenantContext`, fetches the org by id (organizations is unfiltered root), returns `MyOrganizationDto`. The create/list endpoints are removed from the public surface; the create logic, if reused, moves behind the seeder (U8). Keep `Result.NotFound` → 404 if the org id somehow has no row.
**Patterns to follow:** existing endpoint group + `ToMinimalApiResult`.
**Test scenarios:**
- `Covers AE6.` Authenticated OrgOwner GETs their org → only their own org returns; no create endpoint is reachable (404/405 on the old route).
- Unauthenticated GET of `/me` → 401.
- A user cannot retrieve another org by guessing an id (the `/me` route ignores any client-supplied id; only the JWT `org` is used).

### U8. Idempotent dev seeder (Organization + OrgOwner)

**Goal:** Provision one Organization and its OrgOwner so the slice is demo-able, replacing open org-create.
**Requirements:** R13.
**Dependencies:** U2, U3, U4, U5 (needs the `BYPASSRLS` maintenance role + RLS policies in place).
**Files:**
- `src/Lms.Infrastructure/Seeding/IdentitySeeder.cs`
- `src/Lms.Api/Program.cs` (guarded dev-only invocation, e.g. on startup in Development or via an explicit command/argument)
- `tests/Lms.Api.IntegrationTests/` seeder coverage (folded into U10)

**Approach:** Read org name, owner email, default password from the `Seed` config. Idempotent: no-op if the org/owner already exist. Hash the default password (Argon2id), create the `User` with `mustChangePassword = true` and the `OrgOwner` role code. The seeder runs under the **`BYPASSRLS` maintenance role** (it provisions the first org + owner before any tenant context exists); the `users` insert under `FORCE` RLS works because the bypass role is exempt — no per-row GUC juggling needed. **Password hygiene:** `AuthOptions` validation enforces a minimum seed-password length (≥12); `.env.example` ships a non-functional placeholder (`<change-me-min-12-chars>`), never a real default; a startup warning (not error) logs if the configured password is weak. Guard so it never runs in Production unless explicitly enabled.
**Patterns to follow:** `AppDbContextFactory` config-reading; existing DI composition in `Program.cs`.
**Test scenarios:**
- Fresh DB: seeder creates exactly one org + one OrgOwner with `mustChangePassword = true`; the owner can subsequently log in.
- Re-run: seeder is a no-op (no duplicate org/owner).
- The seeded owner row is created despite `FORCE` RLS (tenant context correctly set during seed).
- `Test expectation` for the Production guard: invoking with Production environment and the flag off does not seed.

### Phase D — Frontend & verification

### U9. Web: Next.js proxy, login + forced-change pages, token handling

**Goal:** Make the slice human-demo-able: same-origin proxy, login and forced-change pages, in-memory access token with transparent refresh, and an auth gate on the org page.
**Requirements:** R14, R15, R16; advances F1, F2.
**Dependencies:** U6, U7.
**Files:**
- `web/next.config.*` (rewrites `/api/*` → API origin)
- `web/app/(auth)/login/page.tsx`, `web/app/(auth)/change-password/page.tsx`
- `web/lib/auth/` (in-memory access-token store, single-flight 401-refresh fetch wrapper, CSRF double-submit header)
- `web/app/` org page (gate behind auth; render "my organization"; remove cross-tenant list/create UI)
- `web/` route-handler/server-action proxy bits if needed for cookie writes
- `web/__tests__/` or component tests per the web project's existing setup

**Approach:** Browser talks only to the Next.js origin; rewrites proxy to the API so the refresh cookie is first-party `SameSite=Lax`. Access token held in a module-scoped variable (not localStorage). A fetch wrapper retries once on 401 after calling `/api/v1/auth/refresh`, sharing a single in-flight refresh promise (no stampede). When login returns `mustChangePassword`, route to the forced-change page before app access. Org page redirects unauthenticated users to login and renders the `/me` org.

Interaction states (named so the implementer doesn't invent UX; copy is product-tunable):
- **Login error:** on 401, show one inline message below submit — `Incorrect email or password.` — identical for wrong-credentials vs unknown-email (mirrors the API's enumeration-safe behavior). The one exception: `account-locked` shows `Your account is temporarily locked. Try again later.` so the user stops retrying.
- **Login loading:** disable submit + show in-flight indicator (`Signing in…`) from submit until the response resolves/rejects; re-enable on error.
- **Forced-change form:** two fields (current + new password); on submit disable until resolved; API 422 → first validation message inline; API 401 (wrong current password) → `Current password is incorrect`; success → navigate to org page. No client-side strength rules — the API is the authority.
- **Transparent refresh:** success → silent retry (user sees nothing); failed refresh (401 on `/auth/refresh`) → clear in-memory token and redirect to `/login` (the redirect is the signal; no toast); pending calls reject silently.
- **Redirect after login:** always to `/` (the org page) — no `returnTo` stored/honored this slice (one gated destination); ignore any `?returnTo` if present.
- **Forced-change page access:** accessible to any authenticated user (functions as a normal change-password form); unauthenticated → login; no redirect-away for already-changed users.

**Patterns to follow:** existing `web/` App Router + TanStack Query setup from the walking skeleton; `web/AGENTS.md` / `web/CLAUDE.md` guidance.
**Test scenarios:**
- Login form posts credentials, stores the access token in memory, navigates to the org page on success.
- A `mustChangePassword` login routes to the forced-change page and blocks the org page until changed.
- Unauthenticated visit to the org page redirects to login.
- 401 on a data call triggers a single transparent refresh + retry; concurrent 401s share one refresh.
- Org page renders the user's own organization and shows no create-org control.

### U10. Cross-cutting integration tests + architecture rule updates

**Goal:** Prove the end-to-end flows and isolation that unit tests can't, and keep the dependency guardrail honest.
**Requirements:** R17, R18, R19, R20; verifies F1, F2, F3 and AE1–AE6.
**Dependencies:** U6, U7, U8.
**Files:**
- `tests/Lms.Api.IntegrationTests/AuthFlowTests.cs` (login→authenticated→refresh→logout)
- `tests/Lms.Api.IntegrationTests/TenantIsolationTests.cs`
- `tests/Lms.Api.IntegrationTests/RefreshReuseTests.cs`
- `tests/Lms.Api.IntegrationTests/ForcedChangeTests.cs`
- `tests/Lms.Architecture.Tests/DependencyRuleTests.cs` (assert new Application abstractions carry no EF/ASP.NET/Npgsql/crypto types)
- remove/replace integration tests asserting the old org list/create

**Approach:** Reuse `WebAppFactory<Program>` + Testcontainers PostgreSQL. Seed two orgs for isolation. Drive flows over HTTP with the cookie handling the proxy would do. Confirm the architecture tests fail if Application references a forbidden assembly (the guardrail is real).
**Execution note:** These are the AE-coverage backstop — each acceptance example from the origin must map to at least one assertion here or in a unit test.
**Test scenarios:**
- `Covers F1 / AE1.` Full login → authenticated `/me` → refresh → logout round trip succeeds end-to-end.
- `Covers F3 / AE5.` Two-org isolation on `users`/`refresh_tokens` across the EF filter **and** RLS, run as the RLS-subject app role; includes the role-guard assertion (a no-GUC query returns zero rows, proving the connecting role is genuinely RLS-subject and the test isn't false-greening via a bypassing role).
- `Covers AE2.` Refresh reuse outside the grace window revokes the family; a second presentation *within* grace returns the same already-issued pair (no new divergent token).
- `Covers F2 / AE3.` Forced-change blocks then unblocks.
- `Covers AE4.` Lockout after threshold.
- `Covers AE6.` "My organization" returns only the caller's org; old endpoints gone.
- Architecture test goes red on a deliberate Application→Infrastructure (or `Microsoft.AspNetCore`) reference.

---

## Requirements Traceability

| Requirement | Unit(s) |
|---|---|
| R1 User aggregate / one-org | U2, U3 |
| R2 Login → access+refresh | U4, U6 |
| R3 Lockout after N | U2, U6 |
| R4 Refresh rotation (hashed) | U2, U3, U6 |
| R5 Reuse detection → family revoke | U3, U6 |
| R6 Logout | U6 |
| R7 Change password | U6 |
| R8 Forced first-login change | U2, U6 |
| R9 Org from JWT claim only | U5, U6 |
| R10 SET app.current_org per request | U5 |
| R11 EF global query filter | U5 |
| R12 RLS scaffold — resolved as `users`/`refresh_tokens` (organizations is the unfiltered tenant root; resolves origin Q1) | U5 |
| R13 Dev seeder | U8 |
| R14 Login page + token handling | U6, U9 |
| R15 Auth gate on org page | U7, U9 |
| R16 "My organization" view | U7, U9 |
| R17 Login round-trip test | U10 |
| R18 Tenant isolation test | U5, U10 |
| R19 Refresh-reuse test | U10 |
| R20 Forced-change test | U10 |

---

## System-Wide Impact

- **`Program.cs` middleware pipeline** changes for every request: CORS (now with credentials) → `UseAuthentication` → `UseAuthorization` → tenant-aware DB access. Ordering errors here break auth globally — covered by U6 + U10.
- **`AppDbContext` becomes tenant-aware** (constructor dependency + nullable query-filter scalar + per-request `SET LOCAL` transaction). The design-time factory and test harness construct it with a null-object tenant. Any future entity must declare whether it is tenant-owned (gets the filter + RLS) or a root; document this expectation in the DbContext.
- **Two DB roles** are introduced: an RLS-subject app role (runtime + tests) and a `BYPASSRLS` maintenance role (migrations, seeding, pre-auth login lookup). Future raw data scripts must choose the right role; the RLS `FORCE` means the owner is not exempt.
- **Web app** shifts from direct API calls to same-origin proxied calls; the walking skeleton's org page behavior changes.
- **CI** continues to run unit + architecture + integration tests; integration tests now spin up auth + RLS paths (no new infra beyond the existing Testcontainers PostgreSQL).

---

## Scope Boundaries

Carried from the origin document; this plan does not exceed it.

- No department/position org tree or closure table.
- No member create/update/lock UI or Excel import — only the seed path provisions users.
- No RBAC/ABAC enforcement — roles ride as JWT claims; no permission-check pipeline, no scope/branch/ownership guards.
- No RLS policy fan-out beyond `users`/`refresh_tokens`.
- No forgot/reset password and no email infrastructure.
- No app shell, nav, or design-system build-out beyond the login + forced-change pages.
- No SSO/SAML; no platform cross-tenant provisioning UI.

### Deferred to Follow-Up Work

- **Audit logging** (`audit_logs` table + `IAuditLogger` port writing login/logout/refresh/password-change/lock events) — T5 §7 mandates it for these operations; deferred from this slice but called out so the gap is intentional and backfilled early (historical login data can't be recovered later).
- **Signed double-submit CSRF token** — this slice relies on `SameSite=Lax` + Fetch-Metadata (sufficient for the same-origin BFF); the HMAC double-submit becomes relevant only if a cross-origin model is adopted.
- **Robust per-IP / credential-stuffing throttling** and the **Redis** dependency it implies — this slice ships per-account lockout plus a coarse in-process rate limit as an interim stopgap.
- **JWKS endpoint + `kid` key rotation** (the slice ships a single configured RS256 key pair).
- **Single app DB role only (post-platform):** the two-role split (RLS-subject app role + `BYPASSRLS` maintenance role) is introduced here; revisit role topology when the platform-operator (A4) cross-tenant surface is built.
- **Capture this slice as the first `docs/solutions/` learning entry** once it lands (refresh-family modeling, `SET LOCAL` + FORCE-RLS + two-role wiring, login-write-under-RLS, martinothamar behavior translation, same-origin cookie wiring) — the learnings base does not exist yet.

---

## Dependencies / Prerequisites

- Builds on the walking skeleton (`Lms.Domain/Application/Infrastructure/Api`, `Organization`, Docker Compose PostgreSQL 17, CI architecture guardrail). Verified present.
- New NuGet packages added to `Directory.Packages.props` (U1): JwtBearer, `Microsoft.IdentityModel.JsonWebTokens`, Konscious Argon2 — versions consistent with the .NET 10 rc band.
- A dev RSA key pair (generated locally; not committed) and seed credentials in dev config.
- Stack confirmed .NET 10 (`global.json` SDK `10.0.100-rc.1`, all `.csproj` `net10.0`).
- No external services (email, SSO, CDN) required.

---

## Risk Analysis & Mitigation

| Risk | Impact | Mitigation |
|---|---|---|
| Refresh rotation race / network-retry false-positive logouts | Flaky prod logouts, hard to debug | Atomic guarded `UPDATE ... RETURNING` (raw SQL, not `ExecuteUpdate`) + short (~5–10s) grace-window replay of the *same* already-issued pair; refresh handler test-first against concurrency cases (U6) |
| Grace-window replay abused by a stolen-parent replay | Time-bounded reuse-detection bypass (softens R5) | Window returns only the already-minted pair (never a fresh divergent token); kept short; mitigated by HttpOnly cookie + ~15-min access TTL; **accepted residual risk, documented** |
| RLS silently bypassed because the connecting role owns/super-users the tables | Cross-tenant leak; **isolation test false-greens** | `FORCE ROW LEVEL SECURITY` + dedicated non-owner RLS-subject app role for runtime *and tests*; isolation test asserts a no-GUC query returns zero rows to prove the role is genuinely RLS-subject (U5/U10) |
| `SET LOCAL` not applied / applied before auth on the login write path | Login auths but can't persist refresh token under FORCE RLS | Login opens its org-scoped transaction *after* identifying the user and `SET LOCAL`s before the write; explicit cold-login-persists-token test (U6) |
| Unset GUC throws (`current_setting` single-arg) | Requests error instead of failing closed | Two-arg `current_setting('app.current_org', true)` → NULL → no rows (U5) |
| `AppDbContext` constructor change breaks design-time factory + test harness | Migrations / integration tests won't build/run | Null-object `ITenantContext` for design-time + WebAppFactory; nullable captured org id (U5) |
| EF query-filter scoped-service-in-lambda anti-pattern | Wrong/stale tenant, model-cache issues | Capture org id as a (nullable) scalar on the DbContext; never call a service inside the filter lambda (U5) |
| Stale roles on a 14-day refresh once RBAC lands | Multi-day authorization lag | Refresh re-sources roles/status from the user row, never copies token claims (U6) |
| Konscious Argon2 lacks a .NET 10 TFM / slow maintenance | Future incompatibility | Isolated behind `IPasswordHasher` port — swappable to libsodium/PBKDF2 without touching callers; pin the actual published version (U1/U4) |
| Credential stuffing across many accounts (per-account lockout blind to it) | Automated abuse of the login endpoint | Interim in-process `RateLimiter` on `/auth/login`; robust per-IP + Redis deferred (U6) |
| Forced-change gate missing an endpoint | Seeded default password usable | Gate as a fallback/endpoint filter, not per-endpoint opt-in; explicit test (U6/U10) |

---

## Verification Strategy

- Unit tests: domain invariants + lockout/password behavior (U2), crypto services (U4).
- Integration tests (Testcontainers PostgreSQL): F1 round-trip, F3 two-org isolation incl. RLS, refresh reuse + grace, forced-change, lockout (U10).
- Architecture tests: Application stays free of EF/ASP.NET/Npgsql/crypto types; deliberate violation goes red (U10).
- Manual demo: run the seeder, open the web app, log in as the seeded OrgOwner, complete the forced password change, land on "my organization" — end to end locally.
- Every origin Acceptance Example (AE1–AE6) maps to at least one named test scenario above.

---

## Outstanding Questions

### Resolve Before Implementation

- (none — the brainstorm's blocking questions were empty and all Deferred-to-Planning items are resolved in Key Technical Decisions.)

### Deferred to Implementation

- [Affects U5][Technical] Exact EF 10 named-filter API surface is preview-labeled at rc; if it shifts, fall back to a single AND-combined filter lambda for tenant + (future) soft-delete.
- [Affects U6][Technical] Final grace-window duration (~30s starting point) — tune against observed concurrent-refresh behavior during implementation.
- [Affects U4][Technical] Argon2id parameter tuning (m/t/p) to hit ~250–500ms on the target dev/prod hardware; raise memory before iterations.
- [Affects U9][Technical] Whether the Next.js cookie write needs a Route Handler/Server Action vs pure `rewrites` pass-through — confirm against the chosen proxy mechanism when wiring.
