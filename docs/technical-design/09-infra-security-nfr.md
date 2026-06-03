# T9 · Infrastructure, Security & Non-Functional Requirements

> Liên quan: [T1](01-architecture-stack.md) · [T5 Auth/RLS](05-auth-rbac-tenancy.md) · [T6 Media](06-media-video-scorm.md) · [P4 Metrics](../product-development/04-roadmap-metrics.md)

---

## 1. Môi trường & deployment / Environments

| Env | Mục đích | Ghi chú |
|---|---|---|
| `local` | Dev máy cá nhân | Docker Compose (PG, Redis, MinIO, Meilisearch) |
| `dev`/`staging` | Tích hợp, QA | Gần production, dữ liệu giả |
| `production` | Khách hàng thật | Multi-AZ, backup, monitoring |

### 1.1 Containers & orchestration
- Mỗi service đóng gói **Docker**. Triển khai **Kubernetes** (hoặc managed container như ECS/Azure Container Apps cho giai đoạn đầu — đơn giản hơn).
- Thành phần chạy: `Lms.Api` (n replicas, stateless), `Lms.Worker` (Hangfire), PostgreSQL (managed/HA), Redis (managed/HA), object storage (S3/MinIO), Meilisearch/OpenSearch, FFmpeg workers (scale theo hàng đợi transcode), CDN.
- **Stateless API** → scale ngang dễ; session/realtime state ở Redis.

### 1.2 Topology (production)
```
            ┌──────── CDN (CloudFront/Cloudflare) ────────┐
 users ───► │ static (web) · media (HLS, signed)          │
            └───────────────┬─────────────────────────────┘
                            ▼
                    ┌──────────────┐   WAF/Rate-limit
                    │  API Gateway │
                    └──────┬───────┘
              ┌────────────┼───────────────┐
              ▼            ▼                ▼
        Lms.Api(×N)   Lms.Worker(×M)   SignalR(×N, Redis backplane)
              │            │
        ┌─────┴────────────┴───────────────────────────┐
        ▼            ▼            ▼            ▼         ▼
   PostgreSQL    Redis        S3 storage   Meilisearch  Claude API
   (primary +    (cluster)
    read replica)
```

---

## 2. CI/CD
- **CI (GitHub Actions):** restore → build → unit tests → `Lms.Architecture.Tests` (NetArchTest dependency rule) → integration tests (**Testcontainers**: PG, Redis) → lint (web: eslint/prettier, dotnet format) → security scan (dependency, SAST) → build images.
- **CD:** push image → deploy staging (auto) → smoke tests → manual approve → production (blue/green hoặc rolling). EF Core migrations chạy có kiểm soát (migration job trước khi swap traffic; backward-compatible).
- **DB migration policy:** expand/contract (thêm cột nullable → backfill job → enforce sau) để zero-downtime; không sửa migration đã merge.
- IaC: Terraform cho hạ tầng; Helm/Kustomize cho k8s.

---

## 3. Observability
- **Logs:** Serilog → structured JSON → tập trung (Loki/ELK/Datadog). Mọi log gắn `traceId`, `organizationId`, `userId`.
- **Traces:** OpenTelemetry (API → DB → external) → Tempo/Jaeger.
- **Metrics:** Prometheus/OTel — RED (Rate/Errors/Duration) per endpoint; queue depth (Hangfire); transcode time; cache hit; leaderboard ops.
- **Dashboards & alerts:** error rate, p95 latency, job backlog, DB connections, disk, signed-URL 403 spike (leak attempt).
- **Health checks:** `/health/live`, `/health/ready` (DB/Redis/storage).

---

## 4. Reliability & resilience
- **SLO MVP:** uptime ≥ 99.5%; API p95 < 400ms (đọc), < 800ms (ghi).
- **Backups:** PostgreSQL PITR (daily full + WAL); S3 versioning/replication; test restore định kỳ.
- **Failure handling:** retry + backoff (Hangfire/HTTP), circuit breaker (Polly) cho external (Claude, email, storage); timeout rõ ràng; idempotency keys ([T4 §5](04-api-design.md)); **outbox** cho domain events (at-least-once).
- **Degradation:** AI/search/transcode down → tính năng cốt lõi (học video, báo cáo) vẫn chạy; hiển thị trạng thái.
- **DR:** RPO ≤ 1h, RTO ≤ 4h (mục tiêu); multi-AZ; runbook khôi phục.

---

## 5. Security (threat model)

### 5.1 Bề mặt tấn công & phòng thủ
| Mối đe dọa (STRIDE) | Phòng thủ |
|---|---|
| **Spoofing** | JWT RS256, refresh rotation + reuse detection, MFA (Phase 3), lockout brute-force |
| **Tampering** | HTTPS/TLS 1.2+, input validation (FluentValidation/Zod), không tin client cho quyền/tenant |
| **Repudiation** | `audit_logs` mọi sensitive op (actor, before/after, ip) |
| **Information disclosure** | Tenant isolation (filter+RLS), ABAC, 404 ẩn tồn tại, signed URL media, không lộ stack trace |
| **DoS** | Rate limit (Redis token bucket), WAF, body size limit, job budget, autoscale |
| **Elevation of privilege** | RBAC backend-enforced, IDOR test, deny-by-default, server resolve permission từ role |

### 5.2 Content protection (lõi sản phẩm)
- Video không expose file gốc; HLS + signed URL ngắn hạn + **watermark email động** ([T6](06-media-video-scorm.md)).
- Document: viewer + tải có kiểm soát.

### 5.3 App security hygiene
- OWASP Top 10 checklist mỗi release.
- Secrets trong vault (không hardcode; không vào git). Rotate định kỳ.
- Dependency scanning (Dependabot), container image scanning.
- CSP, secure/httpOnly/SameSite cookies, CSRF protection (refresh cookie + double-submit/SameSite).
- Sanitize rich-text (TipTap output) chống XSS lưu trữ.
- Sandbox SCORM iframe.

### 5.4 Privacy & compliance
- Dữ liệu cá nhân (email, sđt, mã NV, điểm) — phân loại & hạn chế truy cập.
- Hỗ trợ **export & xóa** dữ liệu cá nhân theo yêu cầu (GDPR-like / NĐ13 VN về bảo vệ dữ liệu cá nhân).
- Data residency: cấu hình region storage/AI theo yêu cầu tổ chức.
- Retention: `learning_events` archive sau N tháng; audit giữ ≥ 1 năm.

---

## 6. Non-Functional Requirements (NFR)

| Loại | Yêu cầu |
|---|---|
| **Performance** | Explore LCP < 2.5s (60 card); INP < 200ms; API p95 như §4; video startup < 3s; rebuffer < 1% |
| **Scalability** | 1 tổ chức: 100–50.000 user; hệ thống: hàng trăm tổ chức; leaderboard 50k user mượt (Redis ZSET); concurrent video viewers cao (CDN gánh) |
| **Availability** | ≥ 99.5% (MVP) → 99.9% (trưởng thành) |
| **Security** | Như §5; pen-test trước GA |
| **i18n/L10n** | Tiếng Việt-first; kiến trúc đa ngôn ngữ (next-intl + resource keys); format ngày/số theo locale; UTC lưu trữ |
| **Accessibility** | WCAG 2.1 AA mục tiêu (contrast, keyboard nav, ARIA, captions video) — xem [D2](../design-system/02-components.md) |
| **Compatibility** | Browser hiện đại (Chrome/Edge/Safari/Firefox 2 phiên bản gần nhất); responsive mobile web |
| **Maintainability** | Clean Architecture; test coverage mục tiêu (Domain/Application ≥ 80%); ADR cho quyết định lớn |
| **Observability** | Như §3; mọi request traceable |
| **Cost** | Transcode & AI theo job budget; CDN cache tối ưu chi phí egress |

---

## 7. Mobile strategy

- **Tech:** **React Native (Expo)** — app iOS/Android cho **người học** (nguồn xác nhận có app trên App Store/Google Play).
- **Phạm vi mobile (Phase 2):** đăng nhập, Explore/Library, học video (HLS + watermark), dashboard cá nhân, rank/leaderboard, notification (push). **Tạo nội dung/admin** ưu tiên web.
- **Chia sẻ:** dùng chung REST API + (option) chia sẻ logic TS (types, validation Zod) giữa web/mobile qua package chung.
- **Offline (Could):** tải bài để học offline với DRM/giới hạn — cân nhắc sau (rủi ro leak → thận trọng).
- **Push:** FCM/APNs qua `SendNotificationJob`.
- **Watermark mobile:** overlay email động trong player RN (tương tự web).

---

## 8. Định nghĩa "Production-ready" (checklist GA)
- [ ] Tenant isolation + ABAC pass pen-test (không rò rỉ chéo) — [T5 §8](05-auth-rbac-tenancy.md).
- [ ] Video signed URL + watermark hoạt động, không tải được file gốc — [T6](06-media-video-scorm.md).
- [ ] Backup/restore đã test; runbook DR.
- [ ] Observability + alerts bật; health checks xanh.
- [ ] Rate limit + WAF + secret management.
- [ ] CI xanh: unit + integration + architecture tests + security scan.
- [ ] NFR perf đạt ngưỡng §6 trên staging tải giả.
- [ ] Audit log đầy đủ cho sensitive ops.
