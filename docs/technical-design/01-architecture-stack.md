# T1 · Kiến trúc & Tech Stack / Architecture & Stack

> Liên quan: [README](../README.md) · [T2 Domain](02-domain-model-erd.md) · [T3 DB](03-database-schema.md) · [T4 API](04-api-design.md) · [T5 Auth](05-auth-rbac-tenancy.md) · [T9 Infra](09-infra-security-nfr.md)

---

## 1. Tổng quan / Overview

Hệ thống là **multi-tenant enterprise LMS**: web SPA (Next.js) + mobile app (React Native) gọi **REST API** của backend **ASP.NET Core (.NET 8 LTS)** xây theo **Clean Architecture**. Dữ liệu lưu PostgreSQL; Redis cho cache/leaderboard/realtime backplane; object storage cho media; pipeline FFmpeg cho video HLS; Claude API cho AI.

### 1.1 C4 — Level 1: System Context

```
        ┌────────────┐      ┌──────────────┐      ┌───────────────┐
        │  Learner   │      │ L&D / Admin  │      │ Instructor    │
        └─────┬──────┘      └──────┬───────┘      └──────┬────────┘
              │  HTTPS             │                     │
              ▼                    ▼                     ▼
        ┌───────────────────────────────────────────────────────┐
        │            GOS ACADEMY LMS (this system)              │
        │   Web (Next.js)  ·  Mobile (React Native)  ·  API     │
        └───┬─────────┬─────────┬──────────┬─────────┬──────────┘
            ▼         ▼         ▼          ▼         ▼
        ┌──────┐  ┌──────┐  ┌────────┐  ┌──────┐  ┌──────────┐
        │ S3   │  │Email │  │ Claude │  │ CDN  │  │ Identity │
        │store │  │/SMS  │  │  API   │  │      │  │ (SSO P3) │
        └──────┘  └──────┘  └────────┘  └──────┘  └──────────┘
```

### 1.2 C4 — Level 2: Containers

```
┌──────────────┐   ┌──────────────┐
│ Web (Next.js)│   │ Mobile (RN)  │
└──────┬───────┘   └──────┬───────┘
       └──────── REST /api/v1 (+ SignalR) ────────┐
                                                   ▼
                       ┌─────────────────────────────────────────┐
                       │        API Host (ASP.NET Core)          │
                       │  Controllers/Minimal API · Auth · SignalR│
                       └───┬──────────────┬───────────────┬──────┘
                           │ MediatR       │               │ enqueue
                           ▼               ▼               ▼
                  ┌────────────┐   ┌──────────────┐  ┌──────────────┐
                  │Application │   │ EF Core /     │  │ Hangfire     │
                  │(use cases) │   │ Repositories  │  │ Worker(s)    │
                  └─────┬──────┘   └──────┬───────┘  └──────┬───────┘
                        │                 ▼                 ▼ (transcode,
                        │          ┌─────────────┐    import, reports,
                        │          │ PostgreSQL  │    AI jobs, email)
                        │          │ + pgvector  │
                        ▼          └─────────────┘
                  ┌──────────┐   ┌──────────┐   ┌────────────┐   ┌─────────┐
                  │  Redis   │   │   S3     │   │ Search      │   │ FFmpeg  │
                  │cache/LB  │   │ storage  │   │(Meilisearch)│   │transcode│
                  └──────────┘   └──────────┘   └────────────┘   └─────────┘
```

---

## 2. Clean Architecture (.NET)

Phụ thuộc **hướng vào trong** (Dependency Rule): tầng ngoài biết tầng trong, không ngược lại. Domain ở lõi, không phụ thuộc gì.

```
┌──────────────────────────────────────────────────────────┐
│  Lms.Api  (Presentation)                                  │
│   Controllers/Minimal API · Middleware · Auth · SignalR    │
│   DI composition root · DTO mapping · Swagger              │
│      depends ▼                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Lms.Application                                       │ │
│  │   CQRS: Commands/Queries (MediatR) · Handlers          │ │
│  │   Ports (interfaces): IRepository, IFileStorage,       │ │
│  │     IVideoPipeline, IAiContentService, IEmailSender…   │ │
│  │   Validation (FluentValidation) · Pipeline Behaviors   │ │
│  │   DTOs · Mapping profiles · Domain event handlers      │ │
│  │      depends ▼                                          │ │
│  │  ┌────────────────────────────────────────────────┐   │ │
│  │  │  Lms.Domain  (core, no deps)                     │   │ │
│  │  │   Aggregates · Entities · Value Objects          │   │ │
│  │  │   Domain Events · Enums · Domain Services         │   │ │
│  │  │   Business rules & invariants · Specifications    │   │ │
│  │  └────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Lms.Infrastructure  (implements Application ports)    │ │
│  │   EF Core DbContext + configs + repositories           │ │
│  │   Redis · S3 client · FFmpeg runner · Claude client    │ │
│  │   Email/SMS · Hangfire jobs · Search indexer · Auth    │ │
│  └──────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

### 2.1 Project layout (solution)

```
src/
  Lms.Domain/                # entities, VOs, domain events, enums — zero deps
  Lms.Application/           # CQRS, ports, validators, behaviors, DTOs
  Lms.Infrastructure/        # EF Core, Redis, S3, FFmpeg, Claude, Hangfire, email
  Lms.Api/                   # ASP.NET Core host, controllers, middleware, DI
  Lms.Worker/                # (optional) dedicated Hangfire server host
  Lms.Contracts/             # shared DTOs/enums published to clients (optional)
tests/
  Lms.Domain.UnitTests/
  Lms.Application.UnitTests/
  Lms.Api.IntegrationTests/  # WebApplicationFactory + Testcontainers (PG, Redis)
  Lms.Architecture.Tests/    # NetArchTest: enforce dependency rule
web/                         # Next.js app (separate)
mobile/                      # React Native app (separate)
```

### 2.2 Quy ước Clean Architecture
- **Domain:** thuần POCO, business rules & invariants nằm trong aggregate; phát **domain events**. Không tham chiếu EF/HTTP.
- **Application:** mỗi use case = 1 `IRequest` (Command/Query) + `Handler`. Cross-cutting qua **MediatR pipeline behaviors**: `ValidationBehavior`, `LoggingBehavior`, `TransactionBehavior`, `AuthorizationBehavior`, `TenantBehavior`.
- **Infrastructure:** hiện thực **ports** khai báo ở Application (Dependency Inversion). EF Core repository + Unit of Work qua `DbContext`/`SaveChanges`.
- **Api:** mỏng — nhận request → map → `mediator.Send(...)` → trả `ProblemDetails`/DTO. Không chứa business logic.
- **Architecture tests** (NetArchTest) ép: Domain không tham chiếu Application/Infra; Api không tham chiếu trực tiếp Infra trừ composition root.

### 2.3 CQRS & ví dụ

```csharp
// Application/Courses/Commands/PublishCourse/PublishCourseCommand.cs
public sealed record PublishCourseCommand(Guid CourseId, PublishSettingsDto Settings)
    : IRequest<Result<PublicationDto>>;

public sealed class PublishCourseHandler
    : IRequestHandler<PublishCourseCommand, Result<PublicationDto>>
{
    private readonly ICourseRepository _courses;
    private readonly ICurrentUser _user;          // port → Infra/Api supplies claims
    private readonly IUnitOfWork _uow;

    public async Task<Result<PublicationDto>> Handle(PublishCourseCommand c, CancellationToken ct)
    {
        var course = await _courses.GetWithModulesAsync(c.CourseId, ct);
        if (course is null) return Result.NotFound();
        // authorization & tenant enforced by behaviors + domain guard
        course.Publish(c.Settings.ToValueObject(), _user.UserId);   // invariant in aggregate
        await _uow.SaveChangesAsync(ct);          // domain events dispatched here
        return Result.Success(course.ToPublicationDto());
    }
}
```

> Validation pattern, error model (`Result<T>` → `ProblemDetails`) chi tiết ở [T4 §6](04-api-design.md).

---

## 3. Tech stack & lý do / Stack rationale

### 3.1 Backend
| Hạng mục | Chọn | Lý do |
|---|---|---|
| Runtime | .NET 8 (LTS) | Ổn định dài hạn, hiệu năng cao, hệ sinh thái enterprise |
| Web | ASP.NET Core Web API | Chuẩn, nhanh, middleware mạnh |
| Mediator/CQRS | MediatR | Tách use case, pipeline behaviors |
| ORM | EF Core 8 + Npgsql | Migrations, LINQ, mạnh với PostgreSQL |
| Validation | FluentValidation | Khai báo rõ ràng, test được |
| Mapping | Mapster (hoặc thủ công) | Nhanh, ít magic |
| Background jobs | Hangfire | Dashboard, retry, scheduling — transcode/import/report |
| Realtime | SignalR (Redis backplane) | AI streaming, notification, leaderboard live |
| AuthN | JWT (access + refresh), ASP.NET Identity (lõi) | Chuẩn, mở rộng SSO sau |
| Logging/observability | Serilog + OpenTelemetry | Structured logs, traces, metrics |
| Result/errors | Ardalis.Result hoặc custom `Result<T>` | Tránh exception cho control flow |

### 3.2 Frontend / Mobile
| Hạng mục | Chọn | Lý do |
|---|---|---|
| Web framework | Next.js (App Router) + TypeScript | SPA + SSR (SEO Explore) |
| UI | Tailwind CSS + shadcn/ui (Radix) | Nền design system token-based ([D1](../design-system/01-foundations.md)) |
| Server state | TanStack Query | Cache, retry, invalidation |
| Client state | Zustand | Nhẹ cho UI state |
| Forms | React Hook Form + Zod | Validation đồng bộ với API |
| Rich text | TipTap | Editor khóa học |
| Video | Vidstack (hoặc Video.js) + HLS.js | Player tùy biến + watermark overlay |
| Charts | Recharts/ECharts | Báo cáo |
| i18n | next-intl | Tiếng Việt-first, đa ngôn ngữ |
| Mobile | React Native (Expo) | App iOS/Android, share logic |

### 3.3 Data & hạ tầng
| Hạng mục | Chọn | Lý do |
|---|---|---|
| RDBMS | PostgreSQL 16 | Quan hệ, JSONB, full-text, pgvector, ltree |
| Cache/LB/queue | Redis 7 | Cache, leaderboard (ZSET), SignalR backplane, Hangfire |
| Object storage | S3-compatible (AWS S3 / MinIO) | Media, docs, SCORM, backups |
| Search | Meilisearch (MVP) / OpenSearch (scale) | Global search nhanh, tiếng Việt |
| Vector | pgvector | RAG cho AI tra cứu |
| Media transcode | FFmpeg | HLS multi-bitrate |
| CDN | CloudFront / Cloudflare | Phân phối media + static + signed URL |
| AI | Claude API (Anthropic) | Sinh nội dung + RAG |

Chi tiết infra/CI-CD/scaling: [T9](09-infra-security-nfr.md).

---

## 4. Luồng dữ liệu chính / Key data flows

**A. Học video (có watermark):** Client mở lesson → API kiểm quyền (scope/assign) → cấp **signed HLS playlist URL** (short-TTL) → player tải qua CDN → overlay watermark email động → gửi `lesson.progress`/`lesson.completed` → Application cộng điểm → cập nhật Redis leaderboard + ghi event. (Chi tiết [T6](06-media-video-scorm.md), [T7](07-gamification-reporting.md).)

**B. Xuất bản khóa:** Creator lưu nháp (Command) → upload video → Hangfire transcode HLS → khi xong cập nhật lesson `ready` → publish (chọn audience scope + settings) → tạo `Publication` + assignment → index search.

**C. AI tạo nội dung:** Prompt → Application enqueue AI job → Claude API (+RAG pgvector) → draft trả về stream qua SignalR → người dùng review → publish (flow B). (Chi tiết [T8](08-ai-search-dms-jobs.md).)

---

## 5. Architecture Decision Records (ADR)

| ADR | Quyết định | Lý do | Hệ quả |
|---|---|---|---|
| **ADR-001** | Backend **ASP.NET Core (.NET 8) + Clean Architecture** | Yêu cầu khách hàng; enterprise; testable; tách biệt rõ | Cần kỷ luật tầng; nhiều project |
| **ADR-002** | **CQRS với MediatR** (không event sourcing) | Tách read/write, pipeline cross-cutting; ES quá nặng cho LMS | Read model có thể tối ưu riêng |
| **ADR-003** | **PostgreSQL** đơn cơ sở, multi-tenant **shared DB + `organization_id`** | Đơn giản vận hành ở giai đoạn đầu; cách ly bằng query filter + RLS | Cần global query filter + test cách ly; tách DB/tenant để sau nếu cần |
| **ADR-004** | Cây tổ chức bằng **closure table** (+ option `ltree`) | Truy vấn cha/con & gán quyền theo nhánh nhanh | Bảng phụ `department_paths` |
| **ADR-005** | **RBAC + ABAC**; permission codes; enforce ở Application (behavior) + DB RLS | Phân quyền đa chiều (role × scope × nhánh) | Logic auth tập trung, test kỹ |
| **ADR-006** | Video **HLS + signed URL + watermark động**, không expose file gốc | Chống leak (yêu cầu lõi) | Cần transcode pipeline + CDN signed URL |
| **ADR-007** | Leaderboard bằng **Redis sorted sets** theo kỳ | O(log n) cập nhật/đọc, hợp realtime | Cần rebuild/snapshot khi đổi kỳ |
| **ADR-008** | **Hangfire** cho background jobs | Dashboard, retry, cron — đủ cho transcode/import/report | Cần Redis/SQL storage |
| **ADR-009** | **AI human-in-the-loop**: AI luôn sinh *draft*, không auto-publish | An toàn nội dung, đúng nguyên tắc sản phẩm | Thêm bước review |
| **ADR-010** | **Frontend tách** (Next.js) gọi REST, không Blazor/MVC | DX tốt, tái dùng cho mobile, SPA như nguồn | Cần CORS, BFF tùy chọn |
| **ADR-011** | **UUID v7** cho public IDs | Sortable, không lộ số lượng, an toàn hơn auto-int | Lưu `uuid` |
| **ADR-012** | **Agent-native parity**: mọi action UI có endpoint API tương ứng | Để AI/agent thao tác như người dùng | Thiết kế API đầy đủ, không "UI-only" |

> ADR mới được thêm khi có quyết định kiến trúc lớn; mỗi ADR ghi *Context → Decision → Consequences*.
