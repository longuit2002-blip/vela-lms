# P4 · Sitemap, Lộ trình & Chỉ số / IA, Roadmap & Metrics

> Liên quan: [P1](01-vision-scope.md) · [P3 Features](03-features-user-stories.md) · [T9 NFR](../technical-design/09-infra-security-nfr.md)

---

## 1. Sitemap & Information Architecture

### 1.1 Bản đồ route / Route map

```
PUBLIC / LEARNER (Top Nav)
  /                         Explore (kho công khai theo 6 skill group)
  /cua-ban                  Dashboard cá nhân ("Từ công ty")
  /thu-vien                 Library (filter đa danh mục)
  /noi-dung/<slug>          Course detail (hành trình học) — tab Nội dung | Báo cáo
  /noi-dung/<slug>/hoc/<lessonId>   Trình học (video player)
  /xep-hang                 Leaderboard (tab Phòng ban|Chức vụ; scope; kỳ)
  /bao-cao                  Báo cáo Xuất bản (tab Xuất bản|Đào tạo)
  /bao-cao-dao-tao          Báo cáo Đào tạo (KPI)
  /huong-dan-su-dung        Help center (Phase 2)

ADMIN (Sidebar)
  /thanh-vien               Members (tab Phòng ban|Chức vụ) + modal tạo TK
  /tai-lieu                 DMS (cây tổ chức + Chia sẻ với tôi)
  /xuat-ban                 Publishing center (tab Xuất bản|Đào tạo; grid/list)
    /tao-xuat-ban/xuat-ban-khoa-hoc      Course creator (3 tab)
    /tao-xuat-ban/xuat-ban-lo-trinh      Path creator        [S]
    /tao-xuat-ban/xuat-ban-ky-thi        Exam creator        [S]
    /tao-xuat-ban/xuat-ban-lop-hoc       Class/Event/Workshop[S]
    /tao-xuat-ban/xuat-ban-tai-lieu      Document publish
    /tao-xuat-ban/xuat-ban-podcast       Podcast             [S]
    /tao-xuat-ban/xuat-ban-video         Video               [S]
    /tao-xuat-ban/xuat-ban-scorm         SCORM               [C]
  /quan-ly-dao-tao          Training mgmt (3 tab)            [S]
  /ai-lms                   AI assistant                     [C]
  /quan-ly-he-thong         Org system settings + roles + audit
  /quan-ly-tai-khoan        Account settings

AUTH
  /dang-nhap  /quen-mat-khau  /doi-mat-khau
PLATFORM (BetterWork ops)  — subdomain/área riêng
  /platform/to-chuc  ...                                     (xuyên tenant)
```

### 1.2 Navigation rules
- **Top nav** = trải nghiệm người học + báo cáo (mọi role thấy, mục lọc theo quyền).
- **Sidebar** = quản trị; ẩn hoàn toàn với `Learner` thuần.
- Mục hiển thị = `route.requiredPermissions ⊆ user.permissions` (xem [P2 §3](02-personas-roles.md)).

---

## 2. Lộ trình phát hành / Roadmap & Phasing

Nguyên tắc: **vertical slices** — mỗi phase ship được giá trị end-to-end cho 1 nhóm người dùng, không chỉ "tầng kỹ thuật".

### Phase 0 — Nền tảng (Foundation) · ~Sprint 1–2
Mục tiêu: bộ khung chạy được, tenant + auth + người dùng.
- Clean Architecture skeleton (.NET) + DB + CI/CD + auth ([T1](../technical-design/01-architecture-stack.md), [T5](../technical-design/05-auth-rbac-tenancy.md)).
- Tenant + cây tổ chức + RBAC/ABAC.
- Members: tạo đơn / Excel import (E8).
- App shell + nav (E1) + design system foundations ([D1](../design-system/01-foundations.md)).
- **Demo-able:** Admin tạo tổ chức, import user, đăng nhập.

### Phase 1 — MVP Học tập (Learn) · ~Sprint 3–6  → **First production release**
Mục tiêu: vòng đời học cơ bản end-to-end.
- Publishing: Course (creator 3 tab) + Document (E10 core).
- Media pipeline: upload → HLS → **watermark động** + player (E5, [T6](../technical-design/06-media-video-scorm.md)).
- Learning: Explore, Library, Course detail, hoàn thành lesson (E2–E4).
- Dashboard cá nhân (E3).
- Gamification cơ bản + leaderboard (E6, [T7](../technical-design/07-gamification-reporting.md)).
- Báo cáo Xuất bản (E7-S1) + DMS (E9) + Global search (E13-S1).
- **Demo-able:** tạo course → giao → học có watermark → lên rank → báo cáo.

### Phase 2 — Đào tạo & Vận hành (Train & Operate) · ~Sprint 7–10
- Learning Path, Exam + Question Bank (E10 nâng cao).
- Instructor-led: Lớp/Sự kiện/Workshop + điểm danh + chấm (E10-S2).
- Training mgmt: khung giờ + 7 loại hình + assign (E11).
- Báo cáo Đào tạo nâng cao (E7-S2).
- Podcast, Video publishing (E10-S3).
- Mobile app người học (React Native).
- Help center `/huong-dan-su-dung`.

### Phase 3 — AI & Mở rộng (Augment) · ~Sprint 11+
- AI LMS đầy đủ (E12) + RAG ([T8](../technical-design/08-ai-search-dms-jobs.md)).
- SCORM import/runtime (E10-S3, [T6](../technical-design/06-media-video-scorm.md)).
- Achievements/seasons nâng cao; org training plan tự động.
- SSO/SAML; API public cho HRIS export.

> Sprint chỉ mang tính tham chiếu, không cam kết thời lượng. Việc chia issue chi tiết: dùng quy trình tracer-bullet khi vào build.

---

## 3. Chỉ số & Analytics / Metrics & Events

### 3.1 North-star & KPI sản phẩm

| Loại | Metric | Định nghĩa | Mục tiêu MVP |
|---|---|---|---|
| North-star | Completed effective learning hours / tháng | Tổng giờ lesson hoàn thành đạt ngưỡng chất lượng | Tăng MoM |
| Activation | % user active trong 7 ngày sau tạo TK | login + ≥1 lesson start | ≥ 60% |
| Engagement | Course completion rate | hoàn thành / được giao | ≥ 50% |
| Engagement | DAU/MAU | | ≥ 25% |
| Compliance | % đạt khung giờ đào tạo theo phòng | giờ tích lũy / giờ yêu cầu | báo cáo được |
| Content velocity | Time-to-publish 1 course | tạo → publish | < 30 phút |
| Gamification | % user lên ≥1 rank/quý | | ≥ 40% |
| Quality | Điểm đánh giá khóa TB | rating sau hoàn thành | ≥ 4.0/5 |
| Reliability | Crash-free / error rate | (xem [T9](../technical-design/09-infra-security-nfr.md)) | ≥ 99.5% |

### 3.2 Analytics events (canonical event taxonomy)

Đặt tên `domain.action` snake/dot; mọi event kèm `organization_id, user_id, audience_scope, timestamp, session_id`.

| Event | Khi nào | Props chính |
|---|---|---|
| `auth.login` / `auth.logout` | Đăng nhập/xuất | method |
| `explore.viewed` | Mở Explore | skill_group_filter |
| `course.viewed` | Mở chi tiết khóa | course_id, source |
| `lesson.started` | Bắt đầu lesson | course_id, lesson_id |
| `lesson.progress` | Mốc xem (25/50/75/95%) | lesson_id, percent |
| `lesson.completed` | Hoàn thành lesson | lesson_id, points_awarded |
| `course.completed` | Hoàn thành khóa | course_id, duration_hours |
| `exam.submitted` | Nộp bài thi | exam_id, score, passed |
| `points.awarded` | Cộng điểm | reason, amount, new_total |
| `rank.changed` | Đổi rank | from_rank, to_rank |
| `publication.created` / `.published` | Tạo/xuất bản | type, audience_scopes |
| `assignment.created` | Giao khóa | target_type, target_id |
| `document.viewed` / `.shared` | DMS | document_id |
| `search.performed` | Global search | query_len, result_count |
| `ai.prompt_sent` / `ai.draft_generated` | AI LMS | action_type, tokens |
| `report.viewed` / `.exported` | Báo cáo | report_type, period |
| `member.created` / `.imported` | Tạo user | method, count |
| `class.attendance_marked` | Điểm danh | class_id, present_count |

> Events đẩy vào pipeline analytics (xem [T7 §5](../technical-design/07-gamification-reporting.md)); **dùng chung nguồn** cho cả báo cáo và gamification để tránh lệch số.

### 3.3 Guardrail metrics (chống tối ưu lệch)
- Watermark/anti-leak: số lần phát hiện chia sẻ trái phép (giảm).
- "Play-không-học": tỷ lệ lesson completed nhưng watch-ratio thấp (cảnh báo gian lận).
- Tải trang: LCP Explore < 2.5s, INP < 200ms.

---

## 4. Rủi ro sản phẩm / Product Risks

| Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|
| Phân quyền sai → lộ nội dung chéo phòng/scope | Cao | ABAC enforce ở backend + test phân quyền + pen-test ([T5](../technical-design/05-auth-rbac-tenancy.md)) |
| Watermark bị bypass | Cao | Signed URL ngắn hạn + watermark động + không expose file gốc ([T6](../technical-design/06-media-video-scorm.md)) |
| Gamification bị "cày" điểm giả (auto-play) | TB | Đo watch-ratio, chống seek-skip, điểm chỉ khi đạt ngưỡng chất lượng |
| AI sinh nội dung sai/nhạy cảm | TB | Human-in-the-loop (draft → review), giới hạn scope dữ liệu |
| Import Excel lỗi dữ liệu hàng loạt | TB | Validate theo dòng + preview + rollback |
| Quy mô leaderboard lớn chậm | TB | Redis sorted sets + tính theo kỳ ([T7](../technical-design/07-gamification-reporting.md)) |
