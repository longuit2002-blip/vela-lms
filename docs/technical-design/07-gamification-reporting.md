# T7 · Gamification Engine & Reporting

> Liên quan: [P3 E6/E7](../product-development/03-features-user-stories.md) · [P4 §3 Metrics](../product-development/04-roadmap-metrics.md) · [T2 §2.6](02-domain-model-erd.md) · [T3 §10/§13](03-database-schema.md)

Hai hệ thống dùng **chung nguồn sự kiện** (`learning_events` + `point_transactions`) để báo cáo và gamification không lệch số.

---

## PHẦN A — GAMIFICATION

## 1. Mô hình điểm / Points model

`point_transactions` là **append-only source of truth**. `LearnerProfile.total_points` và Redis ZSET là **projection** để đọc nhanh.

### 1.1 Quy tắc cộng điểm (cấu hình per-org trong `organizations.settings`)
| Hành động (reason) | Điểm mặc định | Ghi chú |
|---|---|---|
| `lesson_completed` | 10 | Chỉ khi `watch_ratio ≥ threshold` (chống cày) |
| `course_completed` | `publication.completion_points` | Cấu hình khi publish ([P3 E10](../product-development/03-features-user-stories.md)) |
| `path_completed` | 200 | |
| `exam_passed` | 50 + bonus theo score | |
| `framework_met` | 100 | Đạt khung giờ đào tạo trong kỳ |
| `attendance` | 20 | Tham dự lớp instructor-led |
| `streak_bonus` | 5/ngày | (option) học liên tục |

- Mỗi `lesson_completed` chỉ tính **một lần** / enrollment (idempotent theo `ref_type+ref_id+reason`).
- `department_id`, `position_id`, `audience_scope` **denormalized** vào transaction tại thời điểm sự kiện (đổi phòng giữa kỳ không lệch leaderboard lịch sử).

### 1.2 Luồng cộng điểm
```
LessonCompleted (domain event)
  → AwardPointsHandler:
      insert point_transaction(reason, amount, dept, pos, scope, occurred_at)
      profile.total_points += amount
      Redis: ZINCRBY on all relevant period/group keys
      check rank threshold → if changed: profile.current_rank_id = newRank; emit RankChanged
```

---

## 2. Ranks (9 bậc)

`ranks` seed mỗi org (ngưỡng cấu hình được):

| Order | Code | Tên | min_points (ví dụ) |
|---|---|---|---|
| 1 | bronze | Đồng | 0 |
| 2 | silver | Bạc | 500 |
| 3 | gold | Vàng | 1 500 |
| 4 | platinum | Bạch Kim | 3 500 |
| 5 | diamond | Kim Cương | 7 000 |
| 6 | emerald | Tinh Anh | 12 000 |
| 7 | master | Cao Thủ | 20 000 |
| 8 | grandmaster | Chiến Tướng | 32 000 |
| 9 | challenger | Thách Đấu | 50 000 |

- Rank tính theo **total_points all-time** (rank "trọn đời"); leaderboard kỳ tính riêng theo điểm trong kỳ.
- `GET /me/rank` trả `{ rank, points, nextRank, pointsToNext }`.
- `RankChanged` → notification + sticker (option).

---

## 3. Leaderboard (Redis sorted sets) — ADR-007

### 3.1 Key design
Leaderboard có **nhiều chiều**: tab (department|position) × scope (INTERNAL|PARTNER|GUEST) × period (month|quarter|year|all) × group (deptId|positionId|org). Dùng ZSET:

```
lb:{org}:{tab}:{scope}:{period}:{periodKey}:{groupId}
  member = userId, score = points-in-period

# ví dụ:
lb:org123:department:INTERNAL:month:2026-06:deptKinhDoanh
lb:org123:position:INTERNAL:quarter:2026-Q2:posTruongPhong
lb:org123:department:INTERNAL:all:all:org123          # toàn tổ chức
```

### 3.2 Thao tác
- **Cộng điểm:** `ZINCRBY` lên tất cả key liên quan của user (các period đang mở + group dept/position + org). Tính `periodKey` từ `occurred_at`.
- **Đọc top N:** `ZREVRANGE key 0 N-1 WITHSCORES` → join user info (cache).
- **Hạng của tôi:** `ZREVRANK key userId` (+1) & `ZSCORE`.
- **Highlight "tôi"** kể cả ngoài top: lấy rank của tôi riêng.

### 3.3 Period rollover & rebuild
- Period mới (tháng/quý/năm) → key mới tự nhiên theo `periodKey`; key cũ giữ làm lịch sử (TTL dài hoặc snapshot ra Postgres).
- **Rebuild/khôi phục:** vì `point_transactions` là nguồn chân lý, có job rebuild ZSET từ Postgres (khi Redis mất dữ liệu / đổi công thức): `SUM(amount) GROUP BY user WHERE occurred_at IN period`.
- Nhất quán: cộng điểm dùng **outbox** để đảm bảo Postgres + Redis không lệch (nếu Redis fail, replay từ outbox/transactions).

### 3.4 Hiệu năng
- ZSET O(log n) cho update/rank — chịu được tổ chức 50k user.
- User info (tên/avatar) cache Redis hash / TanStack Query phía client.

---

## 4. Sticker / Achievements
- `stickers.criteria` (jsonb) định nghĩa điều kiện (vd "hoàn thành 10 khóa", "đạt rank Vàng", "chuỗi 7 ngày").
- Đánh giá khi có domain event liên quan → `learner_stickers` (idempotent).
- Phase 3: seasons (mùa giải) — reset leaderboard kỳ + phần thưởng.

---

## PHẦN B — REPORTING & ANALYTICS

## 5. Kiến trúc báo cáo / Reporting architecture

```
Domain events ──► learning_events (append-only, partitioned by month)  [T3 §13]
                        │
        ┌───────────────┼───────────────────────┐
        ▼               ▼                         ▼
  Read models /     Scheduled rollups        Ad-hoc queries
  projections       (Hangfire nightly →      (filter Năm/Quý,
  (materialized)    report_daily_* tables)    phòng ban) 
        │
        ▼
   Report API (T4 §4.9) → charts (Recharts)
```

- **Nguồn dùng chung:** báo cáo và gamification đọc cùng `point_transactions`/`learning_events` → số khớp nhau.
- **Rollup nightly:** Hangfire job tổng hợp vào bảng `report_daily_*` (theo org/dept/period) để dashboard nhanh; query realtime cho số liệu nhỏ.
- Báo cáo lớn/export chạy **nền** (`POST /reports/{type}/export` → jobId → file S3 → notify).

## 6. Báo cáo Xuất bản (`/bao-cao`) `[M]`
6 thẻ chỉ số + biểu đồ tròn tỷ lệ hoàn thành, filter Năm/Quý:

| Thẻ | Định nghĩa | Nguồn |
|---|---|---|
| Tổng xuất bản | count(publications published trong kỳ) | publications |
| Sự kiện | count(kind=training_session) | publications |
| Kỳ thi | count(kind=exam) | publications |
| Khóa học | count(kind=course) | publications |
| Lộ trình | count(kind=learning_path) | publications |
| Tài liệu khác | count(kind in document/podcast/video/scorm) | publications |
| Tỷ lệ hoàn thành | completed_enrollments / total_enrollments | enrollments |

## 7. Báo cáo Đào tạo (`/bao-cao-dao-tao`) `[S]`
KPI (định nghĩa thống nhất — tránh mỗi nơi tính khác):

| KPI | Công thức |
|---|---|
| Tổng lớp | count(training_sessions trong kỳ) |
| Lượt HV | sum(participants) |
| Khung đào tạo YTD | sum(required_hours) các framework năm hiện tại |
| Điểm HV ≥ 8.5 | count(distinct user có avg score ≥ 8.5) |
| % tham dự | present / registered |
| Lượt GV | count(distinct instructor) |
| Tổng giờ dạy | sum(session_hours × instructors) |
| Điểm GV | avg(evaluation_score của lớp GV dạy) |
| Điểm đánh giá TB | avg(attendances.evaluation_score) |
| % hoàn thành khung theo phòng | tích lũy giờ / required_hours theo department |

Biểu đồ: phân bố **loại hình đào tạo** (7 loại), **xếp hạng giảng viên**, **% hoàn thành khung theo phòng ban**. Filter kỳ + phòng ban; export.

## 8. Báo cáo trong khóa (tab "Báo cáo") `[M]`
`GET /courses/{id}/report` → danh sách HV của khóa: tiến độ %, lessons hoàn thành, completed_at, điểm — tôn trọng ABAC (Instructor thấy khóa mình; DeptManager thấy phòng mình).

## 9. Báo cáo đào tạo theo nhân sự (QL đào tạo) `[S]`
Bảng từng nhân sự: **Điểm Star priority**, Tổng giờ, **Tiến độ %** (so khung giờ), Khác. So sánh `tích lũy giờ` vs `required_hours` (framework) → trạng thái compliance.

## 10. Data governance
- `learning_events` partition theo tháng; archive/cold-storage sau N tháng.
- Định nghĩa KPI versioned (đổi công thức → ghi rõ kỳ áp dụng).
- Số liệu nhạy cảm (điểm cá nhân) chỉ người có quyền xem; truy cập log audit ([T5 §7](05-auth-rbac-tenancy.md)).
