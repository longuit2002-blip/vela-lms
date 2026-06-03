# T2 · Domain Model & ERD

> Liên quan: [T1 Architecture](01-architecture-stack.md) · [T3 DB Schema](03-database-schema.md) (hiện thực vật lý) · [T5 Auth](05-auth-rbac-tenancy.md)

Mở rộng từ "Domain model (gợi ý)" trong hand-over (§13). Đây là mô hình **khái niệm** (DDD); ánh xạ vật lý sang bảng ở [T3](03-database-schema.md).

---

## 1. Bounded Contexts

LMS chia thành các **bounded context**; mỗi context = một nhóm aggregate có ngôn ngữ & quy tắc riêng:

| Context | Trách nhiệm | Aggregates chính |
|---|---|---|
| **Identity & Org** | Tenant, người dùng, cây tổ chức, vai trò/quyền | `Organization`, `User`, `Department`, `Position`, `Role` |
| **Content & Publishing** | Tạo & phát hành nội dung học | `Course`, `LearningPath`, `Exam`, `Document`, `Publication` |
| **Learning** | Trải nghiệm học, tiến độ, enrollment | `Enrollment`, `LessonProgress`, `ExamAttempt` |
| **Instructor-led Training** | Lớp/sự kiện/workshop, điểm danh | `TrainingSession` (Class/Event/Workshop) |
| **Training Management** | Khung giờ, loại hình, compliance | `TrainingHoursFramework`, `TrainingType`, `Assignment` |
| **Gamification** | Điểm, rank, leaderboard, sticker | `LearnerProfile`, `PointTransaction`, `Rank`, `Sticker` |
| **DMS** | Tài liệu folder-based, chia sẻ, question bank | `Folder`, `Document`, `QuestionBank`, `Question` |
| **Reporting & Analytics** | Sự kiện, KPI (read models) | `LearningEvent`, report projections |
| **AI** | Phiên AI, draft nội dung | `AiSession`, `AiDraft` |

> Context giao tiếp qua **domain events** (vd `LessonCompleted` → Gamification cộng điểm + Reporting ghi event), tránh coupling trực tiếp.

---

## 2. Aggregates & invariants (Domain layer)

### 2.1 Identity & Org

**`Organization`** (aggregate root, = tenant)
- `Id, Name, Slug, Status(Active/Suspended), TimeZone, Locale, CreatedAt`
- Sở hữu cấu hình: rank thresholds, training types seed, skill groups.

**`Department`** (trong Organization)
- `Id, OrgId, ParentId?, Name, Path(closure)` — cây tổ chức.
- Invariant: không tạo chu trình (cycle) trong cây.

**`Position`** — `Id, OrgId, Name`.

**`User`** (aggregate root)
- `Id, OrgId, Email, Phone, FullName, EmployeeCode, Handle, AvatarUrl, Status, LastSeenAt, PasswordHash, MustChangePassword`
- `DepartmentId, PositionId`, `AudienceScope (INTERNAL|PARTNER|GUEST)`, `Roles[]`.
- Invariant: `Email` & `EmployeeCode` duy nhất trong Organization.

**`Role`** — `Id, OrgId?, Code, Name, Permissions[]` (xem [T5](05-auth-rbac-tenancy.md)). Một số role là system (seed), một số custom theo org.

### 2.2 Content & Publishing

**`Publication`** (aggregate root — khái niệm hợp nhất việc phát hành)
- Phân loại: `Training` (Course | LearningPath | Exam | TrainingSession) hoặc `Content` (Document(s) | Podcast | Video | SCORM).
- `Id, OrgId, Type, Title, Status(Draft/Published/Archived), AudienceScopes[], TargetAudience (departments/positions/users/groups), Settings, PublishedAt, PublishedBy, ExpiresAfterDays?, TrainingTypeId?, SkillDomain?, CompletionPoints, TrainingHours, IsPublic, ContinueAfterExpiry, Sequential`.
- Trỏ tới nội dung cụ thể (`Course`, `Document`…).
- Invariant: publish yêu cầu nội dung hợp lệ (Course ≥ 1 lesson), có ít nhất 1 audience target, người publish có quyền.

**`Course`** (aggregate root)
- `Id, OrgId, Title, Slug, CategoryId(skill group), ThumbnailUrl, DescriptionRichText, Status`.
- Chứa **`Module`** (Chương): `Id, Title, Order` → mỗi module chứa **`Lesson`**.
- **`Lesson`**: `Id, Title, Order, Type(VIDEO|DOCUMENT|SCORM…), MediaAssetId?, DocumentId?, DurationSeconds`.
- Invariant: lesson hoàn thành = 1 task; thứ tự module/lesson liên tục; nếu `Sequential` thì khóa lesson sau.

**`LearningPath`** — chuỗi `Course` có thứ tự + điều kiện hoàn thành.

**`Exam`** (aggregate) — `Questions[]` (từ Question Bank), `PassScore, Duration, AttemptsAllowed, Shuffle`.

**`MediaAsset`** (aggregate — quản lý video/file gốc & bản HLS) — `Id, OrgId, Kind(Video/Audio/File/Scorm), OriginalKey, HlsManifestKey?, Status(Uploaded/Transcoding/Ready/Failed), DurationSeconds, Renditions[]`. (Chi tiết [T6](06-media-video-scorm.md).)

### 2.3 Learning

**`Enrollment`** (aggregate root) — gắn `User` với `Publication`(course/path).
- `Id, OrgId, UserId, PublicationId, Source(Assigned/Self), Status(NotStarted/InProgress/Completed/Expired), ProgressPercent, StartedAt, CompletedAt, ExpiresAt`.
- Chứa **`LessonProgress`**: `LessonId, Status, WatchedSeconds, WatchRatio, CompletedAt`.
- Invariant: progress = completed_lessons / total_lessons; completed khi đạt 100% (hoặc theo policy); không vượt expiry trừ `ContinueAfterExpiry`.

**`ExamAttempt`** — `Id, ExamId, UserId, Answers[], Score, Passed, StartedAt, SubmittedAt`.

### 2.4 Instructor-led Training

**`TrainingSession`** (Class | Event | Workshop) — `Id, OrgId, Type, Title, InstructorIds[], Schedule(start/end), Location/Online, Capacity, Participants[], Attendance[], Evaluations[]`.
- Liên kết với `TrainingType` & sinh giờ đào tạo cho compliance.

### 2.5 Training Management

**`TrainingType`** — 7 loại seed; CRUD; `Id, OrgId, Name, IsSystem`.

**`TrainingHoursFramework`** — `Id, OrgId, Scope(Department|Position), TargetId, RequiredHours, Period(Year/Quarter), Tags[]`.

**`Assignment`** — giao `Publication` cho target (User/Department/Position/Group) + hạn.

### 2.6 Gamification

**`LearnerProfile`** (aggregate) — `UserId, TotalPoints, CurrentRankId, Stickers[]`, và điểm theo kỳ (cache ở Redis).
**`PointTransaction`** — `Id, UserId, Amount, Reason(LessonCompleted/CourseCompleted/ExamPassed/Framework…), RefId, OccurredAt`. (Append-only — nguồn chân lý điểm.)
**`Rank`** — 9 bậc: `Id, Order, Code, Name, MinPoints`.
**`Sticker`** — `Id, Code, Name, Criteria`.

### 2.7 DMS

**`Folder`** (cây) — `Id, OrgId, ParentId?, Name, OwnerScope(Department/User)`, sharing.
**`Document`** — `Id, OrgId, FolderId?, Title, MediaAssetId|FileKey, Type, Visibility`.
**`QuestionBank`** / **`Question`** — `Id, OrgId, Stem, Options[], Answer, Tags[]`.
**`Share`** — `ResourceType, ResourceId, GrantedToType(User/Department), GrantedToId, Permission(Read/Edit)`.

### 2.8 AI

**`AiSession`** — `Id, OrgId, UserId, Messages[]`.
**`AiDraft`** — `Id, SessionId, Kind(Lesson/Video/Course/Lookup), Content, SourceRefs[], Status(Draft/Accepted/Discarded)`.

---

## 3. ERD (quan hệ cốt lõi)

```
Organization 1──* Department (ParentId self-ref: cây)
Organization 1──* Position
Organization 1──* User
User *──1 Department      User *──1 Position
User *──* Role            (UserRoles)
User 1──1 LearnerProfile
LearnerProfile *──1 Rank
LearnerProfile 1──* Sticker (LearnerStickers)
User 1──* PointTransaction

Organization 1──* Publication
Publication 1──1 Course | LearningPath | Exam | TrainingSession | DocumentSet   (polymorphic ref)
Course 1──* Module 1──* Lesson
Lesson *──1 MediaAsset (nullable)        Lesson *──1 Document (nullable)
LearningPath *──* Course (ordered: PathItems)
Exam 1──* Question        Question *──1 QuestionBank
TrainingSession *──1 TrainingType        TrainingSession 1──* Attendance

Publication 1──* Assignment ──> (User|Department|Position|Group)
User 1──* Enrollment *──1 Publication
Enrollment 1──* LessonProgress *──1 Lesson
User 1──* ExamAttempt *──1 Exam

Organization 1──* Folder (ParentId self-ref) 1──* Document
Share: (ResourceType, ResourceId) ──> (User|Department)

Organization 1──* TrainingHoursFramework ──> (Department|Position)
Organization 1──* TrainingType

User 1──* AiSession 1──* AiDraft
* ──* LearningEvent  (analytics, append-only; org_id,user_id,type,props,ts)
```

### 3.1 Ghi chú thiết kế quan hệ
- **Publication là "mặt tiền"** thống nhất cho mọi thứ phát hành; nội dung cụ thể tách aggregate riêng → tránh bảng "course" phình to và cho phép thêm loại mới (Podcast/SCORM) mà không phá schema.
- **Enrollment ↔ Publication** (không trỏ thẳng Course) để 1 cơ chế tiến độ dùng chung cho course/path/exam.
- **PointTransaction append-only** là nguồn chân lý; `LearnerProfile.TotalPoints` và Redis ZSET là projection để đọc nhanh ([T7](07-gamification-reporting.md)).
- **Polymorphic ref** (`Publication → Course|Path|…`) hiện thực bằng `content_type + content_id` (xem [T3 §5](03-database-schema.md)).

---

## 4. Domain Events (chính)

| Event | Phát từ | Hệ quả (subscriber) |
|---|---|---|
| `UserCreated` | User | Tạo LearnerProfile (rank Đồng, 0 điểm); index search |
| `PublicationPublished` | Publication | Tạo Assignment/Enrollment theo audience; index search; event `publication.published` |
| `LessonCompleted` | Enrollment | Gamification cộng điểm; cập nhật progress; analytics |
| `CourseCompleted` | Enrollment | Cộng `CompletionPoints`; cộng giờ đào tạo (framework); sticker; analytics |
| `ExamSubmitted` | ExamAttempt | Tính điểm, pass/fail; điểm gamification; báo cáo |
| `PointsAwarded` | LearnerProfile | Cập nhật Redis ZSET; kiểm tra `RankChanged` |
| `RankChanged` | LearnerProfile | Notification; sticker; analytics |
| `AttendanceMarked` | TrainingSession | Cộng giờ đào tạo; báo cáo đào tạo |
| `MediaTranscoded` | MediaAsset | Đổi Lesson sang `ready`; cho phép publish |

> Domain events dispatch sau `SaveChanges` (outbox pattern khi cần đảm bảo at-least-once — xem [T9](09-infra-security-nfr.md)).
