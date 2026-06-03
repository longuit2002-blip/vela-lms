# T8 · AI LMS, Search, DMS & Background Jobs

> Liên quan: [P3 E9/E12](../product-development/03-features-user-stories.md) · [T3 §11/§12](03-database-schema.md) · [T4 §4.10/§4.12](04-api-design.md) · [T6 import flow](06-media-video-scorm.md)

Gộp 4 hệ thống hỗ trợ: **AI LMS** (sinh nội dung), **Global Search**, **DMS**, **Background Jobs** (Hangfire) — vì chúng chia sẻ hạ tầng (jobs, indexing, storage).

---

## PHẦN A — AI LMS `[C]` (Phase 3)

## 1. Mục tiêu
Trợ lý AI giúp người tạo nội dung **nhanh gấp nhiều lần**: sinh bài học, kịch bản video, outline khóa học, và tra cứu kiến thức nội bộ. Nguyên tắc **human-in-the-loop** (ADR-009): AI luôn sinh **draft**, người dùng review rồi mới publish.

## 2. Kiến trúc

```
User (chat / quick-action)
   │  POST /ai/generate {action, prompt, attachments}
   ▼
AI Orchestration (Application)
   ├─ build context: scope-filtered org content + attachments
   ├─ (lookup) RAG: embed query → pgvector ANN → top-k chunks  [T3 doc_embeddings]
   ├─ call Claude API (system prompt theo action) — stream
   └─ persist AiSession/AiMessages/AiDraft
   ▼
Stream tokens → SignalR hub → UI
   ▼
User review draft → POST /ai/drafts/{id}/accept → tạo Course/Lesson (flow publish T4 §8)
```

## 3. Bốn quick-action (từ nguồn §11)
| Action | Input | Output (draft) | Kỹ thuật |
|---|---|---|---|
| **Tạo bài học/đọc bằng AI** | prompt + tài liệu | nội dung lesson / script đọc | Claude + (RAG nếu chọn tài liệu) |
| **Tạo video đào tạo bằng AI** | prompt | kịch bản + voice-over (TTS) → ghép slide/video | Claude (script) + TTS; video assembly job |
| **Tạo khóa học bằng AI** | chủ đề + mục tiêu | outline module → lesson + mô tả | Claude (structured output → course skeleton) |
| **Tra cứu thông tin** | câu hỏi | câu trả lời + **trích nguồn** | RAG (pgvector) + Claude, có citations |

- **Structured output:** ép Claude trả JSON schema (course outline, lesson list) để map thẳng vào `ai_drafts.content` rồi `accept` → tạo entity.
- **Voice/mic input:** speech-to-text (provider) → prompt.
- **Đính kèm (+):** tài liệu trong scope người dùng → thêm vào context/RAG.

## 4. RAG (tra cứu)
- Ingestion job: chunk tài liệu/khóa (theo org) → embed (model 1536-dim) → `doc_embeddings` (HNSW index).
- Query: embed câu hỏi → `ORDER BY embedding <=> :q LIMIT k` (cosine) → đưa top-k vào prompt → Claude trả lời + cite `source_refs`.
- **Scope-filtered:** chỉ embed/truy hồi nội dung user được phép (org + scope + quyền) → không lộ dữ liệu chéo.

## 5. An toàn AI
- Human-in-the-loop bắt buộc (không auto-publish).
- Giới hạn dữ liệu theo scope/quyền (như mọi truy vấn — [T5](05-auth-rbac-tenancy.md)).
- Rate limit + budget token theo org; log `ai.prompt_sent`/`ai.draft_generated` (P4 §3.2).
- Lọc nội dung nhạy cảm; prompt injection mitigation (tài liệu không ghi đè system prompt).
- PII: không gửi dữ liệu cá nhân ngoài mức cần; cấu hình provider region.

---

## PHẦN B — GLOBAL SEARCH

## 6. Mục tiêu (nguồn §8, §12)
Tìm xuyên **khóa học, tài liệu, người dùng, câu hỏi** (ngân hàng câu hỏi) — tôn trọng phân quyền, tiếng Việt tốt (dấu, dấu thanh).

## 7. Kiến trúc
- **MVP:** PostgreSQL full-text (`tsvector` + `pg_trgm` cho fuzzy/typo) — đủ cho quy mô vừa; có sẵn cột `search_tsv` ([T3](03-database-schema.md)).
- **Scale:** **Meilisearch** (typo-tolerant, tiếng Việt, nhanh) hoặc OpenSearch; index cập nhật qua background job khi entity đổi (domain events).
- **Phân quyền trong search:** lọc theo `organization_id` + scope + quyền **trước khi** trả kết quả (post-filter hoặc index kèm ACL fields). Không bao giờ trả kết quả ngoài quyền.

```
GET /search?q=&types=course,document,user,question
  → query index (per type) với filter {org, scope, acl}
  → merge & rank → trả nhóm theo type
```

- Đề xuất/gợi ý (autocomplete) dùng prefix; highlight match.

---

## PHẦN C — DMS (Document Management) `[M]`

## 8. Mô hình (nguồn §8)
- **Folder-based** theo cây tổ chức + nhóm **"CHIA SẺ VỚI TÔI"** (shares).
- CRUD folder/document; mỗi document trỏ `media_asset` (file) hoặc `file_key`.
- **Phân quyền:** theo phòng ban/scope (visibility) + `shares` (grant cho user/department, read/edit).
- **Tích hợp:** document dùng trong Course (lesson type=document), Library (document card), Publishing ("Một/Nhiều tài liệu").
- **Question bank** thuộc DMS context — search trả cả câu hỏi.

## 9. Viewer & bảo mật
- Xem qua viewer (PDF.js); tải giới hạn theo policy; signed URL ([T6 §6](06-media-video-scorm.md)).
- Versioning tài liệu (option Phase 2).

---

## PHẦN D — BACKGROUND JOBS (Hangfire) — ADR-008

## 10. Vì sao Hangfire
Dashboard theo dõi, retry/backoff, scheduling (cron), lưu state trong PostgreSQL/Redis — phù hợp các tác vụ nền của LMS.

## 11. Danh mục jobs
| Job | Trigger | Mô tả | Retry |
|---|---|---|---|
| `TranscodeVideoJob` | sau upload media | FFmpeg → HLS, cập nhật `media_assets` | Có, idempotent |
| `ImportUsersJob` | `POST /users/import` | Parse Excel, validate theo dòng, tạo user, báo lỗi | Per-row, rollback batch lỗi |
| `ImportFrameworksJob` | import khung giờ | Parse Excel khung giờ đào tạo | Per-row |
| `IndexSearchJob` | entity changed | Cập nhật search index | Có |
| `EmbedContentJob` | tài liệu/khóa changed | Chunk + embed → pgvector (RAG) | Có |
| `GenerateReportExportJob` | `POST /reports/export` | Tổng hợp → file Excel/PDF → S3 → notify | Có |
| `NightlyRollupJob` | cron hằng đêm | Rollup `report_daily_*` ([T7 §5](07-gamification-reporting.md)) | Có |
| `RebuildLeaderboardJob` | thủ công/định kỳ | Rebuild Redis ZSET từ `point_transactions` | Idempotent |
| `ExpireEnrollmentsJob` | cron | Đánh dấu enrollment hết hạn | Idempotent |
| `SendNotificationJob` | domain events | Email/push (rank changed, assignment, hạn) | Có |
| `AiGenerateJob` | AI action nặng | Gọi Claude/TTS/video assembly | Có, budget-limited |
| `OutboxDispatchJob` | cron ngắn | Phát domain events từ `outbox_messages` (at-least-once) | Có |

## 12. Excel import (chi tiết — nguồn §7.1, §10)
```
POST /users/import (multipart xlsx) → 202 {jobId}
ImportUsersJob:
  - đọc template (cột: email, sđt, họ tên, mã NV, mật khẩu mặc định, phòng ban, chức vụ)
  - validate từng dòng (định dạng, trùng, phòng/chức vụ tồn tại)
  - tạo user hợp lệ; thu thập lỗi {row, field, message}
  - kết quả: {created, skipped, errors[]}  → client tải báo cáo lỗi
GET /users/import/template → file mẫu .xlsx
```
- Đọc xlsx bằng thư viện .NET (ClosedXML/EPPlus).
- **Preview** trước khi commit (option): import dry-run trả lỗi mà chưa tạo.
- Sensitive op → audit + không auto-submit ([T4 §5](04-api-design.md)).

## 13. Realtime (SignalR)
- Hub `/hubs/notifications`: job done (import/transcode/export), `RankChanged`, assignment mới, hạn sắp tới, AI stream.
- Redis backplane để scale nhiều instance.
