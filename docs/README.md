# GOS ACADEMY — BetterWork LMS · Tài liệu Sản phẩm & Kỹ thuật

> **Mục đích / Purpose:** Bộ tài liệu này đặc tả đầy đủ (greenfield) để Claude Code và đội phát triển **xây dựng lại** nền tảng LMS doanh nghiệp "GOS ACADEMY (BetterWork LMS)" từ đầu. Tài liệu được tổng hợp và mở rộng từ bản hand-over khám phá ngày **03/06/2026** (URL gốc: `https://jacob.betterwork.vn/`).
>
> Đây **không** phải tài liệu vận hành sản phẩm hiện tại — mà là **spec để build mới**.

---

## 1. Bộ tài liệu (Document Map)

| # | File | Nội dung |
|---|------|----------|
| — | [README.md](README.md) | Index, quy ước, glossary song ngữ (file này) |
| **Product Development** ||
| P1 | [product-development/01-vision-scope.md](product-development/01-vision-scope.md) | Tầm nhìn, value proposition, đối tượng, phạm vi (MoSCoW), out-of-scope |
| P2 | [product-development/02-personas-roles.md](product-development/02-personas-roles.md) | Personas + ma trận vai trò/quyền (role × capability) |
| P3 | [product-development/03-features-user-stories.md](product-development/03-features-user-stories.md) | 14 module → epics → user stories + acceptance criteria |
| P4 | [product-development/04-roadmap-metrics.md](product-development/04-roadmap-metrics.md) | Sitemap/IA, lộ trình MVP→Phase, KPI & analytics events |
| **Technical Design — System** ||
| T1 | [technical-design/01-architecture-stack.md](technical-design/01-architecture-stack.md) | Kiến trúc C4, Clean Architecture (.NET), stack + ADR |
| T2 | [technical-design/02-domain-model-erd.md](technical-design/02-domain-model-erd.md) | Aggregates, entities, value objects, ERD |
| T3 | [technical-design/03-database-schema.md](technical-design/03-database-schema.md) | Schema PostgreSQL (DDL), index, constraint, cây tổ chức |
| T4 | [technical-design/04-api-design.md](technical-design/04-api-design.md) | REST contract, endpoints, conventions, error model |
| T5 | [technical-design/05-auth-rbac-tenancy.md](technical-design/05-auth-rbac-tenancy.md) | AuthN, RBAC + ABAC, multi-tenancy, permission matrix |
| T6 | [technical-design/06-media-video-scorm.md](technical-design/06-media-video-scorm.md) | Upload → HLS transcode → signed URL → watermark động, SCORM runtime |
| T7 | [technical-design/07-gamification-reporting.md](technical-design/07-gamification-reporting.md) | Điểm/9 rank, leaderboard (Redis), data pipeline báo cáo |
| T8 | [technical-design/08-ai-search-dms-jobs.md](technical-design/08-ai-search-dms-jobs.md) | AI LMS (LLM/RAG), global search, DMS, Excel import, jobs |
| T9 | [technical-design/09-infra-security-nfr.md](technical-design/09-infra-security-nfr.md) | Infra/CI-CD/CDN, threat model, audit, NFR, mobile |
| **Technical Design — UI Design System** ||
| D1 | [design-system/01-foundations.md](design-system/01-foundations.md) | Principles + design tokens (color/type/spacing/elevation/grid) |
| D2 | [design-system/02-components.md](design-system/02-components.md) | Component library (specs + states + a11y) |
| D3 | [design-system/03-patterns-content.md](design-system/03-patterns-content.md) | Page templates + microcopy/voice tiếng Việt |

**Thứ tự đọc gợi ý:** P1 → P2 → P3 (hiểu sản phẩm) → T1 → T2 → T3 → T4 (hiểu nền kỹ thuật) → các T còn lại theo nhu cầu → D1–D3 khi dựng UI.

---

## 2. Quy ước (Conventions)

- **Ngôn ngữ:** Văn xuôi nghiệp vụ bằng **tiếng Việt**; thuật ngữ kỹ thuật, schema, API, code, design token bằng **tiếng Anh**.
- **Định danh kỹ thuật:** entity/table/field/route/component **luôn tiếng Anh** (vd `LearningPath`, `enrollments`, `/api/v1/courses`).
- **Ngày tháng:** ISO `YYYY-MM-DD`; thời gian lưu **UTC**, hiển thị theo timezone tổ chức (mặc định `Asia/Ho_Chi_Minh`).
- **Tiền tệ/đơn vị:** giờ học = `hours` (decimal), điểm = `points` (integer).
- **ID:** dùng **UUID v7** (sortable theo thời gian) cho mọi public ID; khóa nội bộ có thể `bigint` identity.
- **Versioning API:** prefix `/api/v1`.
- **Mức ưu tiên (MoSCoW):** `[M]` Must · `[S]` Should · `[C]` Could · `[W]` Won't (this release).
- **ADR:** quyết định kiến trúc đánh số `ADR-001…` trong [T1](technical-design/01-architecture-stack.md).
- **Trạng thái spec:** mỗi mục tính năng gắn `MVP` / `Phase 2` / `Phase 3` (xem [P4](product-development/04-roadmap-metrics.md)).

---

## 3. Glossary song ngữ (Bilingual Glossary)

| Tiếng Việt | English (canonical) | Ghi chú |
|---|---|---|
| Tổ chức | **Organization** | = tenant. BetterWork host nhiều tổ chức. |
| Phòng ban | **Department** | Node trong cây tổ chức. |
| Chức vụ | **Position** | Vị trí công việc (gắn user). |
| Vai trò | **Role** | Tập quyền (Admin tổng, Trưởng phòng…). |
| Phạm vi đối tượng | **Audience Scope** | `INTERNAL` (TỔ CHỨC), `PARTNER` (KH-ĐỐI TÁC), `GUEST` (KHÁCH). |
| Học viên | **Learner** | Người học. |
| Giảng viên | **Instructor** | Người dạy / chấm. |
| Khám phá | **Explore** | Trang `/` kho khóa công khai. |
| Thư viện | **Library** | Kho nội dung theo danh mục. |
| Khóa học | **Course** | Gồm Module → Lesson. |
| Lộ trình | **Learning Path** | Chuỗi khóa học có thứ tự. |
| Kỳ thi | **Exam** | Bài kiểm tra/đánh giá. |
| Lớp học / Sự kiện / Workshop | **Class / Event / Workshop** | Đào tạo có lịch (instructor-led). |
| Bài học | **Lesson** | Đơn vị học (chủ yếu VIDEO). 1 lesson hoàn thành = 1 task. |
| Module / Chương | **Module / Chapter** | Nhóm lesson trong course. |
| Xuất bản | **Publication / Publishing** | Hành vi tạo & phát hành nội dung. |
| Tài liệu | **Document** | File trong DMS / nội dung học. |
| Ngân hàng câu hỏi | **Question Bank** | Kho câu hỏi cho exam. |
| Xếp hạng | **Ranking / Leaderboard** | Bảng xếp hạng gamification. |
| Hạng | **Rank** | 9 bậc (xem dưới). |
| Điểm | **Points** | Điểm tích lũy → lên rank. |
| Sticker | **Sticker / Badge** | Huy hiệu thành tích. |
| Khung giờ đào tạo | **Training Hours Framework** | Số giờ yêu cầu theo phòng ban/chức vụ. |
| Loại hình đào tạo | **Training Type** | 7 loại (E-Learning…). |
| Báo cáo | **Report** | Báo cáo xuất bản & đào tạo. |
| Watermark email động | **Dynamic Email Watermark** | Chống chia sẻ video trái phép. |
| Trợ lý AI | **AI LMS / AI Assistant** | Sinh nội dung học bằng AI. |

### 9 bậc Rank / The 9 Ranks
`Đồng (Bronze) → Bạc (Silver) → Vàng (Gold) → Bạch Kim (Platinum) → Kim Cương (Diamond) → Tinh Anh (Emerald) → Cao Thủ (Master) → Chiến Tướng (Grandmaster) → Thách Đấu (Challenger)`

### 7 Loại hình đào tạo / 7 Training Types
1. E-Learning · 2. ĐT nội bộ trực tiếp (Internal in-person) · 3. ĐT bên ngoài (External) · 4. ĐT hãng (Vendor) · 5. ĐT outsource (Outsourced) · 6. Hội thảo (Seminar) · 7. ĐT khách hàng (Customer training)

### 6 Nhóm kỹ năng 2026 / Skill Groups (seed data)
1. Nền tảng phục vụ KH chuẩn mực (8) · 2. Ứng xử & xử lý tình huống KH (6) · 3. Phát triển năng lực cá nhân (6) · 4. Tăng trưởng hiệu suất & trải nghiệm KH (7) · 5. An toàn & sức khỏe lao động (4) · 6. Tài liệu L&D/HR/Trainer (1)

---

## 4. Tóm tắt stack (chi tiết ở [T1](technical-design/01-architecture-stack.md))

- **Frontend:** Next.js (React) + TypeScript · Tailwind + shadcn/ui · TanStack Query + Zustand · TipTap · Vidstack/HLS.js
- **Backend:** **ASP.NET Core (.NET 10 LTS)** — **Clean Architecture** (Domain/Application/Infrastructure/API) · CQRS + **martinothamar Mediator** (MIT; MediatR went commercial) · EF Core (Npgsql) · FluentValidation · Hangfire · SignalR
- **Data:** PostgreSQL · Redis (cache + leaderboard) · S3-compatible storage · pgvector (RAG) · Meilisearch/OpenSearch (global search)
- **Media:** FFmpeg → HLS · CDN · signed URLs · dynamic watermark
- **AI:** Claude API (Anthropic) + RAG
- **Mobile:** React Native (Expo)
- **Infra:** Docker · Kubernetes/managed containers · CI/CD (GitHub Actions) · OpenTelemetry

---

## 5. Nguồn gốc (Provenance)

Tài liệu nguồn: `GOS-ACADEMY-BetterWork-LMS-Documentation.html` (hand-over khám phá 03/06/2026, 25 ảnh màn hình). Các phần **suy luận/đề xuất** (schema, API, infra, design tokens) được đánh dấu khi cần để phân biệt với **quan sát thực tế** từ sản phẩm gốc.
