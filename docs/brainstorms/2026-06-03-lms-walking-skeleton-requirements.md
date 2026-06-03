---
date: 2026-06-03
topic: lms-walking-skeleton
---

# LMS Walking Skeleton — Foundation Scaffold

## Summary

Stand up the thinnest end-to-end slice of the GOS ACADEMY / BetterWork LMS: one entity (`Organization`) round-tripping through all four Clean Architecture layers of a .NET 8 backend to a Next.js page, runnable locally via Docker Compose, gated by a CI pipeline that enforces the architecture dependency rule. This becomes the trunk that subsequent Phase 0 → Phase 1 slices grow on.

---

## Problem Frame

The product is fully specified at an *overall* level in `docs/` (product, technical, design-system), but no code exists yet — the project root contains only `docs/`. Jumping straight into breadth (auth, tenancy, video, gamification) risks discovering architecture, tooling, or dev-loop problems late, after a lot of code already assumes a shape. The team needs proof that the chosen stack and Clean Architecture layering actually compose, run, and test cleanly — on the smallest possible vertical — before investing in features.

A second pressure: `docs/` is a start-from-zero overview, not an exhaustive spec, so implementation *will* surface detail gaps. Those gaps must be resolved with the user, not silently guessed from the docs.

---

## Key Flows

F1. Organization round-trip (proves the layers compose)
- **Trigger:** developer/agent (or the web page) calls the API
- **Actors:** developer/agent, the running system
- **Steps:** `POST /api/v1/organizations` {name, slug} → Application command (MediatR) → Domain `Organization` aggregate → EF Core persists to PostgreSQL → `GET /api/v1/organizations` returns it → Next.js page renders the list
- **Outcome:** a created Organization is visible through every layer and on screen
- **Covered by:** R3, R4, R5, R6, R9

F2. Local dev + CI loop (proves the dev experience & guardrail)
- **Trigger:** developer runs the project locally / opens a PR
- **Actors:** developer, CI pipeline
- **Steps:** `docker compose up` starts PG/Redis/MinIO → backend + web run → tests pass locally → push → CI restores, builds, runs unit + architecture + integration tests, lints web → green
- **Outcome:** a reproducible local environment and a green CI gate that enforces the dependency rule
- **Covered by:** R10, R11, R12, R13

---

## Requirements

**Solution scaffold (backend)**
- R1. Create a .NET 8 solution under `src/` with Clean Architecture projects `Lms.Domain`, `Lms.Application`, `Lms.Infrastructure`, `Lms.Api` (per `docs/technical-design/01-architecture-stack.md` §2.1).
- R2. Wire the composition root in `Lms.Api`: DI registers Application (MediatR + validation behavior) and Infrastructure (EF Core, repositories).

**Backend vertical slice**
- R3. Define a minimal `Organization` aggregate in `Lms.Domain` (id UUID v7, name, slug, status, timestamps) — only construction invariants, no broader business rules.
- R4. Implement `CreateOrganization` (command) and `ListOrganizations` (query) as MediatR handlers in `Lms.Application`, with FluentValidation on the command and the `Result<T>` → ProblemDetails error pattern (`docs/technical-design/04-api-design.md` §6).
- R5. Implement the Organization repository + EF Core `DbContext` + initial migration in `Lms.Infrastructure` against PostgreSQL (Npgsql), aligned with the `organizations` table in `docs/technical-design/03-database-schema.md` §2.
- R6. Expose `POST /api/v1/organizations` and `GET /api/v1/organizations` in `Lms.Api` following API conventions (camelCase JSON, UUID ids, ProblemDetails on error).
- R7. Add health endpoints `GET /health/live` and `GET /health/ready` (ready checks PostgreSQL connectivity).

**Frontend slice**
- R8. Scaffold a Next.js (App Router) + TypeScript app under `web/` with Tailwind + shadcn/ui and the base design tokens from `docs/design-system/01-foundations.md` §8.
- R9. Build one page that calls `GET /api/v1/organizations` and renders the list, plus a minimal form to `POST` a new Organization — using TanStack Query for the data layer, no full page reload on submit.

**Local infra & dev loop**
- R10. Provide a `docker-compose.yml` at the project root starting PostgreSQL, Redis, and MinIO (S3-compatible), with a one-command local startup documented in a root `README`.

**CI & tests**
- R11. Add an architecture test (NetArchTest) that fails if `Lms.Domain` depends on Application/Infrastructure, or if `Lms.Api` depends directly on `Lms.Infrastructure` outside the composition root (the ADR-001/002 dependency rule).
- R12. Add one integration test (`WebApplicationFactory` + Testcontainers for PostgreSQL) exercising the Organization create→list round-trip, plus at least one Domain/Application unit test.
- R13. Add a CI pipeline (GitHub Actions) that restores, builds, runs unit + architecture + integration tests, and lints `web/`; the pipeline must be green.

**Working principle**
- R14. Treat `docs/` as start-from-zero context only: when an implementation detail is not clearly determined by `docs/` or this requirements doc, the implementer **pauses and asks the user** rather than inferring a default. This principle carries into `ce-plan` and `ce-work`.

---

## Acceptance Examples

- AE1. **Covers R3, R4, R5, R6.** Given the stack is running via docker compose, when a client POSTs `{name, slug}` to `/api/v1/organizations` and then GETs `/api/v1/organizations`, the newly created organization is returned with a UUID id.
- AE2. **Covers R4, R6.** Given an invalid create payload (e.g., missing name), when POSTed, the API responds `422` with an `application/problem+json` body listing the field error.
- AE3. **Covers R8, R9.** Given the backend has ≥1 organization, when the developer opens the web page, the organization list renders; submitting the form adds a row without a full page reload.
- AE4. **Covers R11.** Given a PR that makes `Lms.Domain` reference `Lms.Infrastructure`, when CI runs, the architecture test fails and the pipeline is red.
- AE5. **Covers R7.** Given PostgreSQL is down, when `GET /health/ready` is called it returns unhealthy, while `GET /health/live` still returns healthy.

---

## Success Criteria

- A developer can clone the repo, run one documented command, and reach a working Organization list/create page end-to-end within minutes.
- CI is green **and** the architecture test provably goes red on a deliberate dependency-rule violation (the guardrail is real, not theater).
- Every layer of the chosen stack is demonstrated composing correctly — architecture de-risked before feature breadth.
- Clean handoff for the next slice (Auth + Members, Phase 0): it can be added without re-scaffolding.

---

## Scope Boundaries

- No authentication, JWT/refresh tokens, or password handling — Phase 0 proper.
- No RBAC/ABAC, multi-tenancy enforcement, RLS, or org-tree closure table — later Phase 0 slices. `Organization` here is a bare entity, **not yet** a tenant boundary.
- No media/video pipeline, HLS, watermark, or SCORM — Phase 1+ (`docs/technical-design/06-media-video-scorm.md`).
- No gamification, reporting, AI LMS, DMS, publishing, or global search.
- No production deployment / IaC / Kubernetes — CI builds at most; deploy is later.
- No full seed data (9 ranks / 7 training types / 6 skill groups seeding is a Phase 0 task).
- Organization management beyond create/list (settings, suspend, slug rules) is later.

---

## Key Decisions

- **Walking-skeleton-first:** build the thinnest end-to-end vertical before breadth, to de-risk architecture + dev loop. Chosen over "Phase 0 Foundation (full)" and "vertical learning slice".
- **Stack locked:** .NET 8 + Clean Architecture (CQRS/MediatR, EF Core, FluentValidation) backend; Next.js + Tailwind/shadcn frontend; PostgreSQL/Redis/MinIO local — per `docs/technical-design/01-architecture-stack.md` (.NET chosen over NestJS).
- **Repo layout:** monorepo at the project root — `src/` (backend), `web/` (frontend), `docker-compose.yml`, existing `docs/`.
- **First entity = `Organization`:** the root tenant entity, so the skeleton's demo doubles as genuine foundation (not throwaway).
- **Tests + architecture guardrail are part of the skeleton, not deferred** — lock the dependency rule from the first commit.
- **Ask-don't-assume (R14):** docs are overview-level; unspecified detail is resolved by asking the user, not inferring.

---

## Dependencies / Assumptions

- Local toolchain available: .NET 8 SDK, Node.js (LTS), Docker Desktop. *[Assumption — verify on the dev machine]*
- The project root (currently `D:\lms` on this machine) contains only `docs/` (verified). It is **not yet a git repo** — CI assumes a git remote will be initialized.
- `docs/` specs are authoritative for overall shape but intentionally incomplete on detail (see R14).
- External services (Claude API, CDN, email, SSO) are **not** needed for the skeleton.

---

## Outstanding Questions

### Resolve Before Planning

- (none — scope confirmed by the user)

### Deferred to Planning

- [Affects R6][Technical] Minimal API vs Controllers for the skeleton endpoints — both fit Clean Architecture; pick in planning.
- [Affects R3][Technical] UUID v7 generation in .NET 8 (library vs hand-rolled) — decide in planning.
- [Affects R4][Technical] Mapster vs manual mapping for the single DTO — trivial; decide in planning.
- [Affects R13][Technical] CI runner specifics and whether to build/push container images at the skeleton stage.
