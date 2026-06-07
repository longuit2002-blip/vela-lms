---
date: 2026-06-03
topic: lms-identity-auth
---

# LMS Identity + Auth Slice (Phase 0)

## Summary

Add the `User` identity on top of the walking skeleton: a user belongs to one organization, logs in with email + password to receive a JWT access token plus a rotating refresh token, and tenant context (request middleware + EF Core global filter + a Postgres RLS scaffold) is threaded through every request. A seeded OrgOwner makes the flow demo-able, and real login + forced-change-password pages gate the web app.

---

## Problem Frame

The walking skeleton proved the Clean Architecture layers compose, but it has no notion of *who* is using the system: the `Organization` entity is a bare record, every endpoint is open, and the web app lists and creates organizations across all tenants with no login. Phase 0's foundation goal — "Admin creates org, imports user, logs in" — cannot begin until there is an authenticated identity tied to a tenant and a request pipeline that knows which organization a caller belongs to.

This slice is the smallest step that makes the product multi-tenant and login-gated. Everything downstream in Phase 0 (member management, the org tree, the full permission model) assumes a `User` that authenticates and a tenant context that scopes data — so that identity-and-tenant spine is built first, before breadth. Member create/import, the department/position tree, and the full RBAC/ABAC matrix are deliberately held back to their own slices; pulling them in now would re-introduce the breadth-before-proof risk the walking skeleton was meant to retire.

---

## Actors

- A1. Developer / seeder: runs the seed path that provisions the first Organization and its OrgOwner; runs and demos the stack.
- A2. OrgOwner (seeded user): the human who logs in, is forced to change the default password on first login, and sees their own organization.
- A3. API / system: authenticates requests, issues and rotates tokens, resolves and enforces tenant context.
- A4. Platform operator (deferred): future cross-tenant org provisioning lives here; this slice only leaves the seam, it builds no platform UI.

---

## Key Flows

- F1. Login → authenticated request → refresh → logout
  - **Trigger:** A2 opens the web app unauthenticated, or a client calls the auth API.
  - **Actors:** A2, A3
  - **Steps:** submit email + password → API verifies the password hash → issues access JWT (short-lived) + refresh token → an authenticated request carries the access token and resolves tenant context → when the access token nears expiry, the refresh token is exchanged for a new pair (old one revoked) → logout revokes the current refresh token.
  - **Outcome:** a session that starts, refreshes, and ends cleanly, scoped to the user's organization throughout.
  - **Covered by:** R2, R3, R4, R6, R8, R9

- F2. Forced first-login password change
  - **Trigger:** A2 logs in with the seeded default password (`mustChangePassword = true`).
  - **Actors:** A2, A3
  - **Steps:** login succeeds but the response signals must-change → the web app routes to the forced-change page → A2 supplies the current (default) password + a new one → on success the flag clears and other refresh tokens are revoked → normal app access is granted.
  - **Outcome:** no seeded account can operate on its default password.
  - **Covered by:** R7, R8, R14

- F3. Tenant isolation (cross-tenant access denied)
  - **Trigger:** a user authenticated to org A attempts to read data belonging to org B.
  - **Actors:** A3
  - **Steps:** request arrives with org A in the JWT → tenant context set to org A → EF global filter and RLS policy both constrain to org A → org B's rows are invisible.
  - **Outcome:** no cross-tenant data leaks, even if one isolation layer is bypassed.
  - **Covered by:** R9, R10, R11, R16

---

## Requirements

**Identity & tenant model**
- R1. Define a `User` aggregate in `Lms.Domain`: id (UUID v7), organization reference, email (unique within an organization), password hash, status (active/locked), `mustChangePassword`, role code(s), and timestamps — construction invariants only, no broader business rules. A user belongs to exactly one organization; no department/position this slice.

**Authentication flows**
- R2. Login accepts email + password, verifies the stored password hash, and on success issues a short-lived access JWT (carrying at least subject, organization, and role-code claims) plus a longer-lived refresh token.
- R3. The account locks temporarily after N consecutive failed login attempts (brute-force guard).
- R4. Refresh tokens rotate: each refresh issues a new access + refresh pair and revokes the presented refresh token; refresh tokens are stored hashed, never in plaintext.
- R5. Refresh reuse detection: presenting an already-revoked refresh token revokes the entire token family/chain and the request is rejected.
- R6. Logout revokes the caller's current refresh token.
- R7. Authenticated change-password requires the current password; on success it revokes all of the user's *other* refresh tokens.
- R8. While `mustChangePassword` is true, the user must change their password before any other authenticated action succeeds.

**Tenant context enforcement**
- R9. Tenant organization is resolved from the JWT organization claim only — never from the request body or subdomain — to prevent tenant spoofing; a protected endpoint with no resolvable organization is treated as unauthenticated.
- R10. Each request sets the tenant context (`app.current_org`) within its transaction scope.
- R11. An EF Core global query filter scopes tenant-owned entities to the current organization.
- R12. RLS scaffold: enable Postgres row-level security plus a minimal organization-scoping policy on the organizations and users tables, driven by `app.current_org`, proving the DB-layer mechanism end-to-end without fanning policies across other tables yet.

**Bootstrap / seeding**
- R13. A seeding path provisions one Organization together with its OrgOwner user (random or configured default password, `mustChangePassword = true`). This becomes the only way organizations and users come into existence in this slice and replaces the skeleton's open org-create.

**Frontend (login + forced change)**
- R14. A login page authenticates against the auth API; the access token is held in memory and the refresh token in an httpOnly, secure cookie. When login signals must-change, the user is routed to the forced-change-password page before app access.
- R15. The org page is gated by authentication: unauthenticated visitors are redirected to login.
- R16. Post-login the org page renders the authenticated user's own organization ("my organization"); the skeleton's cross-tenant list and create-org form are removed.

**Tests**
- R17. Integration test exercising the F1 round trip (login → authenticated request → refresh → logout) against the running stack.
- R18. Tenant isolation test: two organizations seeded; a user authenticated to org A cannot read org B's data via either the EF filter or RLS.
- R19. Refresh-reuse test: replaying a revoked refresh token revokes the chain and the request is rejected.
- R20. Forced-change test: a `mustChangePassword` user is blocked from other authenticated actions until the password is changed.

---

## Acceptance Examples

- AE1. **Covers R2, R9.** Given a seeded OrgOwner, when they POST valid credentials to the login endpoint, the response returns an access token whose organization claim matches their organization and a refresh token is set; a subsequent authenticated call returns only that organization's data.
- AE2. **Covers R5.** Given a refresh token that has already been rotated (revoked), when it is presented again, the API rejects the request and the entire token family for that user is revoked (the still-valid newest token also stops working).
- AE3. **Covers R8.** Given a freshly seeded user with `mustChangePassword = true`, when they authenticate and call any non-change-password protected action, the API blocks it and directs them to change the password first.
- AE4. **Covers R3.** Given N consecutive failed login attempts for an account, when the next attempt is made within the lock window, it is rejected even if the password is now correct.
- AE5. **Covers R11, R12, R18.** Given two organizations each with one user, when the org-A user requests the organizations/users listing, only org-A rows are returned; bypassing the application filter in a direct query still returns no org-B rows because the RLS policy blocks them.
- AE6. **Covers R16.** Given an authenticated OrgOwner, when they open the web org page, only their own organization renders and no create-organization control is present.

---

## Success Criteria

- A developer can run the seed path, open the web app, log in as the seeded OrgOwner, be forced through a password change, and land on their own organization — end to end, locally.
- Cross-tenant isolation is provably real: an automated test demonstrates that org A cannot see org B's data through both the EF filter and the RLS policy.
- The refresh-token lifecycle (rotation, reuse detection, logout, change-password revocation) is exercised by tests, not just implemented.
- Clean handoff: the next Phase 0 slice (org tree + members + full RBAC) can add departments, member create/import, and permission enforcement on top of this `User` + tenant spine without reworking authentication or the tenant pipeline.

---

## Scope Boundaries

- No department/position org tree or closure table — later Phase 0 slice. A user belongs to one organization and nothing finer-grained here.
- No member create/update/lock UI or Excel import — the only user provisioning is the seed path.
- No RBAC/ABAC enforcement: roles ride along as JWT claims, but there is no permission-check pipeline and no scope/branch/ownership guards. This slice ships authentication without authorization.
- No full RLS policy fan-out across the schema — only the organizations and users tables get policies, as a mechanism proof.
- No forgot/reset password and no email-sending infrastructure — deliberately deferred (the walking skeleton avoided external email).
- No app shell, navigation, or design-system build-out beyond the login and forced-change pages.
- No SSO/SAML, no platform (cross-tenant) provisioning UI — only the seam is left for A4.

---

## Key Decisions

- Identity-and-tenant spine before breadth: build `User` + auth + tenant context as one slice, deferring members, org tree, and RBAC enforcement — consistent with the walking-skeleton "prove the spine first" principle.
- Seed an OrgOwner (org + owner provisioned together) instead of any member-create path, so member CRUD stays fully in a later slice while the slice remains demo-able.
- Tenant isolation lands as middleware + EF global filter + an RLS *scaffold* (orgs/users only): the full three-layer model from T5 is proven end-to-end now, with policy fan-out deferred to the slice that introduces real cross-tenant tables.
- Roles as claims only, no enforcement: the JWT carries role codes but the AuthorizationBehavior pipeline and the full permission/ABAC model wait for the RBAC slice. Accepted consequence: authenticated-but-not-authorized for this slice.
- The web org page becomes a single "my organization" view; the skeleton's cross-tenant list/create is retired and org creation moves entirely to the seed/platform path.
- Auth flow surface for this slice: login + refresh rotation + reuse detection + logout + authenticated change-password (with forced first-login change). Forgot/reset is out.
- Token placement: access token in memory, refresh token in an httpOnly secure cookie — per T5 §2.1.

---

## Dependencies / Assumptions

- Builds directly on the existing walking-skeleton solution (`Lms.Domain/Application/Infrastructure/Api`, the `Organization` entity, Docker Compose with PostgreSQL, and the CI architecture guardrail). *[verified in repo]*
- Specs T5 (`docs/technical-design/05-auth-rbac-tenancy.md`) and P2 (`docs/product-development/02-personas-roles.md`) are authoritative for the auth/tenant model and role catalog, but intentionally overview-level — unspecified detail is resolved by asking the user, not inferred (carries the walking skeleton's R14 working principle forward).
- The stack is .NET 10 (the spec was realigned from the original .NET 8 wording). *[per recent repo history — confirm against `global.json`/`Directory.Build.props` in planning]*
- No external services (email, SSO, CDN) are required for this slice.

---

## Outstanding Questions

### Resolve Before Planning

- (none — scope confirmed by the user)

### Deferred to Planning

- [Affects R1, R12][Technical] Which tenant-owned tables exist to filter in this slice is effectively just `users` (organizations is the tenant root) — confirm whether `organizations` itself is filtered to "my org" or treated as the tenant root that the filter keys off, and how that reconciles with the "my organization" page.
- [Affects R2][Technical] Password hashing algorithm (Argon2id per T5, or PBKDF2 fallback) and the JWT signing setup (RS256 key management) — choose in planning.
- [Affects R3][User decision / Technical] Concrete lockout policy: the value of N, the lock window length, and whether lockout is per-account or also IP-aware.
- [Affects R4, R5][Technical] Refresh-token store shape and how token families/chains are modeled to support reuse detection.
- [Affects R14][Technical] Web ↔ API origin model for the httpOnly refresh cookie (same-site vs cross-origin + CORS credentials), and how the access token is refreshed transparently on the client.
- [Affects R13][Technical] Seed mechanism (EF migration seed vs a dev-only command/endpoint) and whether default credentials are random-and-logged or configured via env.
- [Affects R6, R7][Technical] Whether logout/refresh-revocation needs immediate access-token invalidation (short access lifetime is assumed to make this unnecessary) — confirm in planning.
