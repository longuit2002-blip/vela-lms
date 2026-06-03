# T4 · API Design (REST)

> Liên quan: [T1](01-architecture-stack.md) · [T3 Schema](03-database-schema.md) · [T5 Auth](05-auth-rbac-tenancy.md) · [T6 Media](06-media-video-scorm.md)

REST API là **hợp đồng chính** giữa clients (web/mobile) và backend, và là bề mặt cho **AI/agent** (ADR-012: agent-native parity — mọi action UI đều có endpoint). OpenAPI/Swagger sinh tự động từ controllers.

---

## 1. Conventions

- **Base:** `https://api.<org>.betterwork.vn/api/v1` (hoặc path-based `/api/v1`). Versioning qua path.
- **Định dạng:** JSON (`application/json`), UTF-8. Thời gian ISO-8601 UTC. ID là UUID.
- **Tài nguyên số nhiều, danh từ:** `/courses`, `/users`, `/publications`.
- **Phương thức:** `GET` (đọc), `POST` (tạo/hành động), `PATCH` (sửa một phần), `PUT` (thay toàn bộ - hiếm dùng), `DELETE` (xóa/archive).
- **Hành động không-CRUD** dùng sub-resource động từ: `POST /courses/{id}/publish`, `POST /lessons/{id}/complete`.
- **Idempotency:** mutation nhạy cảm nhận header `Idempotency-Key` (vd publish, import, payment-like). 
- **Tenant:** suy ra từ JWT (claim `org`), **không** nhận `organization_id` từ client cho dữ liệu thường.
- **Naming JSON:** `camelCase`.

---

## 2. Auth & headers

```
Authorization: Bearer <access_jwt>      # access token (~15 phút)
X-Request-Id: <uuid>                     # tracing (client optional, server generates if absent)
Idempotency-Key: <uuid>                  # for sensitive POST
Accept-Language: vi-VN                    # i18n messages
```
- Đăng nhập: `POST /auth/login` → `{ accessToken, refreshToken, user }`.
- Refresh: `POST /auth/refresh` (refresh token rotation). Chi tiết [T5 §2](05-auth-rbac-tenancy.md).

---

## 3. Phân trang, lọc, sắp xếp / Pagination, filtering, sorting

**Cursor-based** (mặc định, ổn định với dữ liệu lớn):
```
GET /courses?cursor=<opaque>&limit=20&sort=-createdAt&filter[categoryId]=...&q=...
```
Response envelope:
```json
{
  "data": [ /* items */ ],
  "page": { "nextCursor": "eyJ...", "hasMore": true, "limit": 20 }
}
```
- `q`: full-text search; `filter[field]`: lọc; `sort`: `field`/`-field` (giảm dần).
- List nhỏ/cố định có thể offset (`page`,`pageSize`).

---

## 4. Resource map (chính)

> `[M]/[S]/[C]` = phase. `◐` = phụ thuộc quyền (T5). Tất cả endpoint tenant-scoped.

### 4.1 Auth & account
| Method | Path | Mô tả |
|---|---|---|
| POST | `/auth/login` | Đăng nhập |
| POST | `/auth/refresh` | Làm mới token |
| POST | `/auth/logout` | Thu hồi refresh token |
| POST | `/auth/forgot-password` · `/auth/reset-password` | Quên/đặt lại MK |
| POST | `/account/change-password` | Đổi mật khẩu |
| GET | `/account/me` | Hồ sơ + permissions + rank |

### 4.2 Organization & members `[M]`
| Method | Path | Mô tả |
|---|---|---|
| GET/POST/PATCH | `/departments` , `/departments/{id}` | Cây tổ chức (CRUD) |
| GET | `/departments/tree` | Cây đầy đủ (closure) |
| GET/POST/PATCH | `/positions` | Chức vụ |
| GET | `/users?filter[departmentId]&filter[scope]&q` | Danh sách thành viên |
| POST | `/users` | Tạo 1 tài khoản (⚠️ sensitive, idempotent) |
| POST | `/users/bulk` | Tạo từ danh sách email |
| POST | `/users/import` | Import Excel (multipart) → trả `jobId` |
| GET | `/users/import/template` | Tải file mẫu |
| PATCH | `/users/{id}` | Sửa (phòng/chức vụ/role/trạng thái) |
| POST | `/users/{id}/lock` · `/unlock` | Khóa/mở |
| GET/POST | `/roles` | Vai trò & permissions ◐ |

### 4.3 Categories `[M]`
`GET/POST/PATCH /categories` — skill groups & domains.

### 4.4 Media `[M]`
| Method | Path | Mô tả |
|---|---|---|
| POST | `/media/upload-url` | Cấp presigned PUT URL (S3) |
| POST | `/media` | Đăng ký asset sau upload → enqueue transcode |
| GET | `/media/{id}` | Trạng thái (uploaded/transcoding/ready) |
| GET | `/media/{id}/playback` | Cấp **signed HLS URL** ngắn hạn (xem [T6](06-media-video-scorm.md)) |

### 4.5 Courses & content authoring `[M]`
| Method | Path | Mô tả |
|---|---|---|
| GET/POST/PATCH/DELETE | `/courses`, `/courses/{id}` | CRUD khóa (draft) ◐ |
| POST/PATCH/DELETE | `/courses/{id}/modules` , `/modules/{id}` | Chương |
| POST/PATCH/DELETE | `/modules/{id}/lessons`, `/lessons/{id}` | Bài học |
| POST | `/courses/{id}/reorder` | Sắp xếp module/lesson (kéo-thả) |
| GET/POST/PATCH | `/learning-paths` `[S]` | Lộ trình |
| GET/POST/PATCH | `/exams`, `/questions`, `/question-banks` `[S]` | Thi & ngân hàng câu hỏi |

### 4.6 Publishing `[M]`
| Method | Path | Mô tả |
|---|---|---|
| GET | `/publications?tab=training\|content&filter[kind]&view=grid\|list` | Trung tâm xuất bản |
| POST | `/publications` | Tạo publication (draft) cho content_id |
| PATCH | `/publications/{id}/settings` | Cài đặt xuất bản (audience, scope, sequential, points, hours, expiry…) |
| POST | `/publications/{id}/publish` | **Publish** (⚠️ sensitive, idempotent, ghi audit) ◐ |
| POST | `/publications/{id}/archive` | Lưu trữ |

**Ví dụ — Publish settings (PATCH body):**
```json
{
  "audienceScopes": ["INTERNAL"],
  "targets": [{ "type": "department", "id": "..." }, { "type": "user", "id": "..." }],
  "quickAdd": ["lan@cty.vn", "EMP123", "0900000000"],
  "isPublic": false,
  "sequential": true,
  "continueAfterExpiry": false,
  "trainingTypeId": "...",
  "skillDomainId": "...",
  "completionPoints": 100,
  "trainingHours": 2.5,
  "expiresAfterDays": 30
}
```

### 4.7 Learning (learner-facing) `[M]`
| Method | Path | Mô tả |
|---|---|---|
| GET | `/explore?filter[skillGroup]` | Trang Khám phá (gom nhóm) |
| GET | `/library?filter[categoryId][]&type=course\|document` | Thư viện (lọc đa danh mục) |
| GET | `/me/dashboard?year=&quarter=` | Dashboard cá nhân (4 chỉ số, rank, assigned) |
| GET | `/me/enrollments` | Khóa được giao/đang học |
| GET | `/courses/{slug}/detail` | Chi tiết khóa (hành trình, module/lesson, trạng thái) |
| POST | `/enrollments` | Tự ghi danh (self) |
| POST | `/lessons/{id}/progress` | Cập nhật watched/ratio (throttled) |
| POST | `/lessons/{id}/complete` | Hoàn thành lesson → điểm |
| POST | `/exams/{id}/attempts` · `PATCH .../submit` | Làm & nộp bài thi `[S]` |

**Ví dụ — `POST /lessons/{id}/progress`:**
```json
{ "watchedSeconds": 187, "watchRatio": 0.62, "position": 187 }
```

### 4.8 Gamification `[M]`
| Method | Path | Mô tả |
|---|---|---|
| GET | `/leaderboard?tab=department\|position&scope=INTERNAL&period=month\|quarter\|year\|all&groupId=` | Bảng xếp hạng |
| GET | `/me/rank` | Rank + điểm + tiến độ lên hạng |
| GET | `/ranks` | 9 bậc + ngưỡng |

### 4.9 Reports `[M]/[S]`
| Method | Path | Mô tả |
|---|---|---|
| GET | `/reports/publishing?year=&quarter=` | 6 thẻ chỉ số + tỷ lệ hoàn thành |
| GET | `/reports/training?period=&departmentId=` | KPI đào tạo (GV/HV) `[S]` |
| GET | `/courses/{id}/report` | Báo cáo trong khóa (tiến độ HV) |
| POST | `/reports/{type}/export` | Export (Excel/PDF) → jobId |

### 4.10 DMS `[M]`
`GET/POST/PATCH/DELETE /folders`, `/documents`; `POST /documents/{id}/share`; `GET /shared-with-me`.

### 4.11 Training management `[S]`
`GET/POST/PATCH /training-types`; `GET/POST/PATCH /training-frameworks` (+ `POST /import`); `GET/POST/PATCH /training-sessions` + `POST /{id}/attendance`; `POST /assignments`.

### 4.12 AI LMS `[C]`
| Method | Path | Mô tả |
|---|---|---|
| POST | `/ai/sessions` | Tạo phiên |
| POST | `/ai/sessions/{id}/messages` | Gửi prompt (stream qua SignalR/SSE) |
| POST | `/ai/generate` | quick-action: `{ action: "lesson\|video\|course\|lookup", prompt, attachments[] }` → draft |
| POST | `/ai/drafts/{id}/accept` | Chuyển draft → nội dung (course/lesson) |

### 4.13 Search & realtime
- `GET /search?q=&types=course,document,user,question` — global search ([T8](08-ai-search-dms-jobs.md)).
- SignalR hub `/hubs/notifications` (rank changed, job done, AI stream).

---

## 5. Quy ước thao tác nhạy cảm (sensitive ops)

Các endpoint có **hệ quả ngoài** (tạo TK, đặt MK, publish, import hàng loạt, xóa):
1. Yêu cầu permission cụ thể (T5) + xác nhận client.
2. **Không auto-submit** (UI), idempotency key (API).
3. Ghi `audit_logs` (actor, before/after).
4. Trả `202 Accepted` + `jobId` nếu xử lý nền (import/transcode/export).

---

## 6. Error model (RFC 7807 ProblemDetails)

Mọi lỗi trả `application/problem+json`:
```json
{
  "type": "https://errors.betterwork.vn/validation",
  "title": "Validation failed",
  "status": 422,
  "detail": "Email không hợp lệ.",
  "instance": "/api/v1/users",
  "traceId": "00-abc...",
  "errors": { "email": ["Email đã tồn tại trong tổ chức."] }
}
```

| HTTP | Khi nào |
|---|---|
| 400 | Cú pháp/sai tham số |
| 401 | Thiếu/het hạn token |
| 403 | Không đủ quyền (RBAC/ABAC/scope) |
| 404 | Không thấy (hoặc ngoài tenant → cũng 404 để không lộ tồn tại) |
| 409 | Xung đột (trùng email, đã publish) |
| 422 | Validation nghiệp vụ |
| 429 | Rate limit |
| 500/503 | Lỗi server / phụ thuộc |

> Backend map `Result<T>`/exception → ProblemDetails ở middleware. **403 vs 404:** dữ liệu ngoài tenant trả 404 (ẩn tồn tại); trong tenant nhưng thiếu quyền trả 403.

---

## 7. Versioning, deprecation, rate limit
- Breaking change → `/api/v2`; v1 giữ tối thiểu 6 tháng, header `Deprecation`/`Sunset`.
- Rate limit theo user + IP (token bucket, Redis); trả `429` + `Retry-After`.
- CORS: chỉ origin web/mobile tin cậy.

---

## 8. Ví dụ end-to-end: tạo & publish khóa (agent-native)
```
POST /courses                      → {id} (draft)
POST /media/upload-url             → presigned PUT
PUT  <s3 url> (upload mp4)
POST /media                        → {mediaId} (enqueue transcode)
POST /courses/{id}/modules         → {moduleId}
POST /modules/{moduleId}/lessons   → {lessonId, mediaAssetId}
GET  /media/{mediaId}              → status: ready
POST /publications                 → {pubId} for course
PATCH /publications/{pubId}/settings (audience, points, hours…)
POST /publications/{pubId}/publish  (Idempotency-Key)  → 202 published
```
Chuỗi này dùng được bởi cả UI **và** AI agent → đảm bảo parity (ADR-012).
