---
date: 2026-06-03
topic: lms-authorization-org-structure
---

# LMS Authorization Spine + Org Structure (Phase 0)

## Summary

Build the authorization half the identity/auth slice deferred, plus the organization structure it depends on. Departments (full CRUD including reparent + delete, backed by a closure table) and a flat org-wide positions catalog become real; the auth slice's role *claims* become enforced *permissions* via an authorization step in the request pipeline (RBAC); a dept-branch ABAC guard confines managers to their own subtree. The `User` gains a department, a position, and real role links. A seeded sample tree + DeptManager prove RBAC + ABAC + tenant isolation end-to-end against the running stack. Backend + tests only — no new UI; members/import stay deferred.

---

## Problem Frame

The auth slice ships authentication *without* authorization: role codes ride in the JWT but nothing enforces them — an accepted "authenticated-but-not-authorized" gap. The product's highest-rated risk (`docs/product-development/04-roadmap-metrics.md` §4) is wrong authorization leaking data across departments/scopes. Before the sensitive member-provisioning surface is built (E8 flags account creation as a ⚠️ sensitive operation), the authorization spine must be real and proven.

The one ABAC rule that applies in Phase 0 — **dept-branch** (a manager manages only its own subtree) — needs the department tree + closure table, which doesn't exist yet either. Ownership, audience-scope, and self scoping all depend on Phase-1 content/learning entities, so they cannot be exercised now. So this slice builds the org structure and turns claims into enforced permissions, proving both against the running stack.

Members, import, and all org-management UI are deliberately held to later slices to keep this one a thin, provable spine — carrying the walking-skeleton "prove the spine before breadth" principle forward. `docs/` remains overview-level; unspecified detail is resolved by asking the user, not inferred (auth slice / walking skeleton R14).

---

## Actors

- A1. Developer / seeder: runs the extended seed (system roles + sample dept tree + positions + DeptManager) and the test/demo flow.
- A2. OrgOwner (seeded): full permissions within the organization; manages the whole department tree and positions.
- A3. Branch-scoped manager (seeded): a manager placed inside one branch; manages only its own subtree, cross-branch denied. The ABAC proof subject. *(Planning reconciliation: per [P2 §3](../product-development/02-personas-roles.md) the branch-limited (◐) org-tree-management role is **LndManager**, not DeptManager — DeptManager's ◐ is over users, deferred to the members slice. The 003 plan seeds an LndManager as this subject and treats DeptManager as an RBAC-negative case; "DeptManager" in F2/R14/R17/AE2/AE4 below should read "branch-scoped manager (LndManager)". Flagged for user confirmation.)*
- A4. API / system: resolves permissions from roles, enforces RBAC in the pipeline and ABAC in handlers, keeps tenant isolation.
- A5. Unprivileged authenticated user (e.g. a Learner): used in negative tests — authenticated but lacking org-management permissions → 403.

---

## Key Flows

- F1. Permission-enforced org-structure operation (RBAC)
  - **Trigger:** an authenticated user calls an org-structure endpoint (create/rename/move/delete department, or position CRUD).
  - **Actors:** A2/A3/A5, A4
  - **Steps:** request carries the access token → effective permissions resolved from the user's roles → the authorization step checks the operation's required permission → allowed → handler runs; lacking permission → 403 returned before the handler executes.
  - **Outcome:** only roles holding the permission perform the action; everyone else is denied uniformly.
  - **Covered by:** R7, R8, R9, R15

- F2. Dept-branch scoping (ABAC)
  - **Trigger:** a DeptManager attempts to manage a department.
  - **Actors:** A3, A4
  - **Steps:** RBAC passes (DeptManager holds the management permission, scope-limited) → the handler runs the dept-branch guard: `target_dept ∈ subtree(manager.dept)` via the closure table → in-branch allowed; out-of-branch → 403.
  - **Outcome:** a manager changes only its own subtree; sibling/parent branches are out of reach for its management actions.
  - **Covered by:** R10, R11, R12, R17

- F3. Reparent + delete a department (closure integrity)
  - **Trigger:** OrgOwner moves a department under a new parent, or deletes one.
  - **Actors:** A2, A4
  - **Steps:** move → cycle check (cannot move a department under its own descendant) → closure rows rebuilt for the moved subtree; delete → blocked if the department has child departments or assigned users, otherwise removed together with its closure rows.
  - **Outcome:** the closure table always reflects the tree; no orphaned users, no cycles.
  - **Covered by:** R3, R4, R18

- F4. Seed + end-to-end proof
  - **Trigger:** developer runs the extended seed, then the test suite / a manual API demo.
  - **Actors:** A1, A4
  - **Steps:** seed provisions the organization (auth slice) + system roles with permissions + a multi-level department tree + positions + a DeptManager placed in a branch → tests exercise RBAC (role × capability), ABAC (in/out of branch), tenant isolation, delete-block, reparent.
  - **Outcome:** enforcement is demonstrated against the running stack, not just unit-mocked.
  - **Covered by:** R14, R16, R17, R18, R19

---

## Requirements

**Org structure (departments + positions)**
- R1. Define a `Department` aggregate in the Identity & Org context: id (UUID v7), organization reference, optional parent, name, timestamps. Invariant: no cycles in the tree. (`docs/technical-design/02-domain-model-erd.md` §2.1, `docs/technical-design/03-database-schema.md` §2)
- R2. Maintain a department **closure table** (ancestor, descendant, depth) so subtree membership is a fast lookup for ABAC and listings. (`docs/technical-design/03-database-schema.md` §2, ADR-004)
- R3. Department CRUD: create (under a parent or as a root), rename, **reparent** (move), delete. Reparent rebuilds the closure for the moved subtree and rejects any move that would create a cycle.
- R4. Deleting a department is **rejected while it has child departments or any assigned users**; the caller must move/reassign first. Enforced in the domain/handler and returned as a clear error.
- R5. Define a `Position` aggregate: id, organization reference, name, **unique per organization**. Flat org-wide catalog (no hierarchy). CRUD: create, rename, list, delete. Delete is **rejected while any user holds the position**.

**Identity model extension**
- R6. Extend the `User` (from the auth slice) with an optional department reference, an optional position reference, and real role assignments. A user may hold multiple roles; effective permissions are the union. (`docs/technical-design/02-domain-model-erd.md` §2.1)
- R7. Define a `Role` with code, name, is-system flag, and a set of permission codes; seed the system roles (`OrgOwner`, `OrgAdmin`, `LndManager`, `DeptManager`, `Instructor`, `Learner`, `Auditor`) with the permission sets from the capability matrix. (`docs/product-development/02-personas-roles.md` §3, `docs/technical-design/05-auth-rbac-tenancy.md` §3.2)

**RBAC enforcement**
- R8. Establish the canonical permission-code catalog (`domain.action[.scope]`) per `docs/technical-design/05-auth-rbac-tenancy.md` §3.1, seeded as data on roles. The **full** matrix is seeded; only the codes guarding endpoints that exist this slice are actually enforced (the rest guard nothing yet).
- R9. A command/query declares the permission it requires; an authorization step in the request pipeline rejects the request with **403 before the handler** when the current user lacks that permission. (`docs/technical-design/05-auth-rbac-tenancy.md` §3.3)
- R10. The current user's effective permissions are resolved from their roles **server-side**, so role changes take effect without re-issuing tokens. (`docs/technical-design/05-auth-rbac-tenancy.md` §2.1)

**ABAC (dept-branch only)**
- R11. Provide a reusable dept-branch check — "is the target department within the subtree of the manager's department?" — via the closure table. (`docs/technical-design/05-auth-rbac-tenancy.md` §4.1)
- R12. Branch-limited management actions (a DeptManager managing departments) run the dept-branch guard inside the handler/domain **after** RBAC passes; out-of-branch targets return 403. Ownership / audience-scope / self ABAC are out of scope (no Phase-1 entities yet). (`docs/technical-design/05-auth-rbac-tenancy.md` §4, §4.3)

**Tenant consistency**
- R13. New tenant-owned tables (departments, department closure, positions, roles, user-role links) carry the `organization_id` discriminator and the EF Core global query filter, consistent with the auth slice's tenant pipeline. (`docs/technical-design/05-auth-rbac-tenancy.md` §5.2)

**Bootstrap / seeding**
- R14. Extend the auth slice's seed so a new organization is provisioned together with: its **system roles** (with permissions), a small **multi-level department tree**, a couple of **positions**, and a seeded **DeptManager** user placed inside one branch (with `mustChangePassword`, consistent with the auth seed). The OrgOwner seed is updated to link its role through the formal role model. Seed remains the only provisioning path — no member CRUD this slice.

**Enforcement surface**
- R15. Wire RBAC enforcement onto the org-structure endpoints (department + position CRUD) and **retrofit** the existing organization endpoints and the auth slice's protected endpoints to declare their required permissions. Authenticated users without the permission get 403.

**Tests**
- R16. RBAC test: for the seeded roles, each org-management capability is allowed for roles that hold it and 403 for roles that don't (role × capability per `docs/product-development/02-personas-roles.md` §3).
- R17. ABAC dept-branch test: the seeded DeptManager manages its own subtree but gets 403 managing a sibling/parent branch; OrgOwner manages the whole tree.
- R18. Closure-integrity tests: reparent rebuilds subtree closure correctly and rejects cycles; delete is blocked when the department has children or users and succeeds when empty.
- R19. Tenant isolation still holds for the new tables: an org-A user cannot see or manage org-B departments or positions (EF filter; RLS where applied).

---

## Acceptance Examples

- AE1. **Covers R9, R15.** Given a seeded Learner (no org-management permission), when they call create-department, the API returns 403 before any handler logic; given the OrgOwner, the same call succeeds.
- AE2. **Covers R11, R12, R17.** Given the seeded DeptManager in branch X, when they rename a department inside `subtree(X)` it succeeds, and when they rename a department outside X the API returns 403.
- AE3. **Covers R3, R18.** Given a department with two levels of children, when the OrgOwner moves it under a new parent, the closure table reflects the new ancestor paths for the whole moved subtree; a move that targets its own descendant is rejected.
- AE4. **Covers R4.** Given a department that still has an assigned user (the seeded DeptManager) or a child department, when delete is attempted it is rejected with a clear error; after the occupants are moved out, delete succeeds.
- AE5. **Covers R5.** Given a position held by a user, when delete-position is attempted it is rejected; an unused position deletes successfully; creating a position whose name already exists in the organization is rejected.
- AE6. **Covers R10.** Given a user whose DeptManager role is removed, when they next call a manager-only endpoint they are denied without needing to re-login (permissions resolved from roles server-side).
- AE7. **Covers R19.** Given two organizations each with a department tree, when an org-A user lists/manages departments only org-A departments are visible and org-B targets return not-found/forbidden.

---

## Success Criteria

- The "authenticated-but-not-authorized" gap from the auth slice is closed for the surfaces that exist: every org-structure and retrofitted endpoint enforces a required permission, proven by a test that goes red if the check is removed.
- Dept-branch ABAC is provably real end-to-end: an automated test shows the seeded DeptManager confined to its own subtree against the running stack, not just a mocked unit.
- The department tree maintains closure integrity through reparent and refuses unsafe deletes — no orphaned users, no cycles.
- Clean handoff: the members slice (E8) can add member create/import/lock/role-change/dept-change and the org-tree admin UI on top of this structure + enforced authz **without reworking** the permission pipeline, the closure table, or the `User` model.

---

## Scope Boundaries

- No member create / bulk / Excel-import / lock / role-change / dept-change (UI or API) — the only user & department provisioning is the seed; member management is the next slice (E8).
- No org-management **UI** of any kind (department tree view/editor, positions page) — it lands in the members page per E8-S1; app shell + nav (E1) and design-system foundations (D1) are separate slices.
- No **ownership, audience-scope, or self ABAC** — those need Phase-1 content/learning entities; only dept-branch is built.
- No **custom (org-defined) roles** or a role-editing surface — only the seeded system roles this slice.
- No RLS policy **fan-out** beyond the auth slice's orgs/users scaffold unless trivially included; new tables rely on the EF global filter (RLS extent decided in planning).
- No platform (cross-tenant) role enforcement — only the seam carried from the auth slice.
- No audit-log surface unless pulled in at planning (see Outstanding Questions).

---

## Key Decisions

- **Authorization spine before the sensitive member surface:** enforce RBAC + the one applicable ABAC rule (dept-branch) and build the org structure it needs, deferring members/import — closes the product's highest-rated risk (P4 §4) before account creation ships.
- **Full org structure now** (departments full CRUD incl. reparent/delete + positions catalog), chosen over a minimal create+list tree — the user opted to complete the structure module in this slice.
- **Delete = block-if-non-empty** for both departments and positions — safest, clearest invariant, no surprise data loss (deliberately stricter than the raw schema's cascade-children FK).
- **Prove ABAC by seed:** extend the auth seed with a sample tree + a DeptManager so dept-branch enforcement is demonstrated end-to-end without pulling member CRUD forward; seed stays the only provisioning path.
- **Seed the full role→permission matrix, wire enforcement only where endpoints exist** — the matrix is authoritative (P2/T5) and cheap to seed; unused codes guard nothing until their features arrive.
- **Permissions resolved from roles server-side** (not baked into the token) so revocation/role changes are immediate (T5 §2.1).
- **Backend + enforcement + seed + tests only; no new UI** — the org-tree UI belongs with members (E8-S1) and the app shell (E1) isn't built, so UI now would be reworked.
- This slice **formalizes the role model** the auth slice referenced loosely (string codes → a roles table + user-role links); the auth seed gets a minor back-touch to link through it.

---

## Dependencies / Assumptions

- Builds on the **already-built** identity/auth slice (`User` with role codes, `IdentitySeeder`, `HttpTenantContext` backing `ITenantContext`/`ICurrentUser`, the JWT issuer emitting `sub`/`org`/`roles`/`mcp`, `TenantConnectionInterceptor` + EF global query filter + the `AddRlsAndAppRole` RLS policy on `users`/`refresh_tokens`) and the walking skeleton (`Organization`, Clean Architecture, Docker Compose, CI guardrail). *[verified in `src/` and `docs/plans/2026-06-03-002-feat-identity-auth-slice-plan.md`; supersedes an earlier draft note that said auth was not yet built]*
- Specs T5 (auth/RBAC/tenancy), P2 (personas/roles matrix), T2 (domain model), and T3 §2 (org/identity schema, closure table) are authoritative for shape but overview-level; unspecified detail is resolved by asking, not inferred.
- Stack is .NET 10 + Clean Architecture (CQRS via martinothamar Mediator, EF Core/Npgsql, FluentValidation), PostgreSQL — per repo history and `global.json`. *[confirm in planning]*
- No external services (email, SSO, CDN) are needed for this slice.
- The closure table (`department_closure`) and the `roles` / `user_roles` tables from T3 §2 are introduced here; their EF mappings + migration are new in this slice.

---

## Outstanding Questions

### Resolve Before Planning

- (none — scope confirmed by the user)

### Deferred to Planning

- [Affects R8, R10][Technical] Whether effective permissions are resolved by re-reading user→roles→permissions per request or derived/cached from the JWT roles claim (T5 §2.1 allows either); reconcile with the immediate-revocation goal.
- [Affects R2, R3][Technical] Closure-table maintenance approach for reparent (delete+reinsert descendant paths vs incremental) and whether to use a DB trigger, application code, or recursive CTE.
- [Affects R13, R19][Technical] Whether the new tenant tables get Postgres RLS policies now or rely on the EF global filter only, given the auth slice deferred RLS fan-out.
- [Affects R3, R4][User/Technical] Whether reparent/delete of departments should write **audit logs** this slice, or audit starts with members (E8-S2 requires account-creation audit) — i.e. whether to stand up the `audit_logs` surface now.
- [Affects R7][Technical] Whether seeded system roles are per-organization rows or global (`roles.organization_id` NULL) referenced by all orgs (T3 allows NULL = system role).
- [Affects R14][Technical] Seed mechanism (EF migration seed vs dev-only command) and how it composes with the auth slice's seed — likely one shared seed path.
- [Affects R5][Product] Whether positions, though flat, should be seeded with any defaults or left empty until members need them.
