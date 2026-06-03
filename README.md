# Vela — Enterprise LMS

**Vela** is a multi-tenant, gamified corporate learning platform (e-learning + instructor-led training + AI content generation + anti-leak video) for the Vietnamese market. Greenfield build from the specs in [`docs/`](docs/README.md), starting with a **walking skeleton** (one `Organization` entity end-to-end through Clean Architecture → Next.js).

> Vela is its own brand — the specs reference a source product (GOS ACADEMY / BetterWork) only as provenance. See [docs/design-system/00-brand.md](docs/design-system/00-brand.md).

- **Plan:** [docs/plans/2026-06-03-001-feat-lms-walking-skeleton-plan.md](docs/plans/2026-06-03-001-feat-lms-walking-skeleton-plan.md)
- **Specs:** [docs/README.md](docs/README.md)

## Stack

- **Backend:** ASP.NET Core (**.NET 10 LTS**), Clean Architecture (`Lms.Domain` / `Lms.Application` / `Lms.Infrastructure` / `Lms.Api`), CQRS via martinothamar Mediator, EF Core + Npgsql, FluentValidation, Ardalis.Result.
- **Frontend:** Next.js (App Router) + TypeScript + Tailwind + shadcn/ui + TanStack Query (`web/`).
- **Data/infra (local):** PostgreSQL, Redis, MinIO via Docker Compose.

> The repo pins the SDK in [`global.json`](global.json). The dev machine currently has .NET `10.0.100-rc.1`; install the **.NET 10 GA SDK** before any real deployment (the RC builds the same `net10.0` output).

## Prerequisites

- .NET SDK 10.x · Node.js 22 LTS + pnpm · Docker Desktop (running)

## Quick start

```bash
# 1. Start local infrastructure (Postgres / Redis / MinIO)
cp .env.example .env          # local-only creds; .env is gitignored
docker compose up -d

# 2. Backend (from repo root)
dotnet build Lms.sln
dotnet run --project src/Lms.Api      # serves https://localhost:5xxx ; Swagger in Development

# 3. Frontend (separate terminal)
cd web && pnpm install && pnpm dev    # http://localhost:3000
```

## Tests

```bash
dotnet test Lms.sln                   # unit + architecture + integration (integration needs Docker)
cd web && pnpm lint && pnpm build
```

## Layout

```
src/      .NET solution (Domain / Application / Infrastructure / Api)
tests/    unit, architecture (dependency-rule), and integration (Testcontainers) tests
web/      Next.js app
docs/     product, technical-design, and design-system specs (+ plans, brainstorms)
```

## Security note (skeleton stage)

The `Organization` endpoints are **intentionally unauthenticated** for the walking skeleton, and Compose services use **local-only** credentials bound to `127.0.0.1`. Authentication, RBAC/ABAC, and multi-tenancy land in Phase 0 **before** any non-local deployment.
