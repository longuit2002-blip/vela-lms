# P1 · Tầm nhìn & Phạm vi sản phẩm / Vision & Scope

> Liên quan: [README](../README.md) · [P2 Personas](02-personas-roles.md) · [P3 Features](03-features-user-stories.md) · [P4 Roadmap](04-roadmap-metrics.md)

---

## 1. Tóm tắt sản phẩm / Product Summary

**GOS ACADEMY (BetterWork LMS)** là nền tảng **LMS doanh nghiệp đa tổ chức (multi-tenant enterprise LMS)** giúp các doanh nghiệp Việt Nam **đào tạo nội bộ, quản lý năng lực và đo lường hiệu quả đào tạo** ở quy mô lớn. Sản phẩm kết hợp:

- **Học tập số (E-Learning):** khóa học video, lộ trình, kỳ thi, tài liệu, podcast, SCORM.
- **Đào tạo có giảng viên (Instructor-led):** lớp học, sự kiện, workshop có lịch và điểm danh.
- **Quản trị đào tạo (L&D ops):** khung giờ đào tạo bắt buộc theo phòng ban/chức vụ, loại hình đào tạo, báo cáo KPI giảng viên & học viên.
- **Gamification:** hệ thống 9 bậc rank, điểm, sticker, leaderboard theo kỳ.
- **AI LMS:** trợ lý AI sinh bài học/video/khóa học và tra cứu kiến thức.
- **DMS:** quản lý tài liệu theo phòng ban + ngân hàng câu hỏi.

> **One-liner:** *"Nền tảng đào tạo doanh nghiệp all-in-one — học, thi, đo lường và tạo nội dung bằng AI — gamified, đa phòng ban, đa đối tượng."*

---

## 2. Bối cảnh & vấn đề / Context & Problem

Doanh nghiệp Việt (đặc biệt khối dịch vụ/bán lẻ/CSKH, ~550+ doanh nghiệp đang dùng theo nguồn) đối mặt:

| Vấn đề (Pain) | Hệ quả |
|---|---|
| Đào tạo phân mảnh (file, Zalo, email, lớp offline rời rạc) | Không truy vết được ai học gì, hoàn thành chưa |
| Không đo được hiệu quả đào tạo (ROI, compliance) | L&D khó báo cáo lên BGĐ |
| Nội dung dễ bị rò rỉ (video khóa học bị share) | Mất tài sản trí tuệ |
| Tạo nội dung đào tạo tốn thời gian | Tốc độ ra khóa học chậm |
| Động lực học thấp | Tỷ lệ hoàn thành thấp |
| Cơ cấu tổ chức phức tạp (phòng ban, chức vụ, đối tác, khách) | Phân quyền & giao học khó |

**Cách sản phẩm giải quyết:** một nền tảng tập trung + phân quyền theo cây tổ chức + chống leak video (watermark động) + gamification tạo động lực + AI tăng tốc tạo nội dung + báo cáo KPI tự động.

---

## 3. Tầm nhìn / Vision

> *Trở thành hệ điều hành đào tạo (Training OS) cho doanh nghiệp Việt Nam: nơi mọi nhân sự học đúng năng lực cần, mọi nhà quản lý thấy rõ bức tranh năng lực, và nội dung đào tạo được tạo ra nhanh gấp 10 lần nhờ AI.*

**North-star metric:** *Số giờ học hoàn thành có hiệu quả/tháng* (completed effective learning hours) — cân bằng giữa "lượng học" và "chất lượng" (gắn điểm KPI, không chỉ play video).

---

## 4. Đối tượng & giá trị / Audiences & Value Proposition

Sản phẩm phục vụ **3 phạm vi đối tượng (audience scopes)** — đây là khái niệm trung tâm chi phối phân quyền & hiển thị:

| Scope | Tiếng Việt | Đối tượng | Giá trị chính |
|---|---|---|---|
| `INTERNAL` | TỔ CHỨC | Nhân viên nội bộ | Đào tạo bắt buộc, lộ trình năng lực, gamification |
| `PARTNER` | KH-ĐỐI TÁC | Khách hàng/đối tác B2B | Đào tạo sản phẩm/quy trình cho đối tác |
| `GUEST` | KHÁCH | Người ngoài (marketing) | Khóa miễn phí, thu hút, demo năng lực |

**Value prop theo vai trò:**
- **Học viên (Learner):** học mọi lúc, thấy tiến độ & rank, được công nhận (sticker/leaderboard).
- **Giảng viên (Instructor):** tạo nội dung nhanh (AI), chấm/đánh giá, theo dõi lớp.
- **Trưởng phòng (Dept Manager):** giao khóa, theo dõi compliance khung giờ của phòng.
- **Admin đào tạo (L&D/Admin):** thiết kế chương trình, báo cáo toàn tổ chức, quản lý người dùng.
- **Ban giám đốc (Exec):** dashboard KPI đào tạo tổng thể.

---

## 5. Nguyên tắc sản phẩm / Product Principles

1. **Agent-native:** mọi hành động người dùng làm được trên UI thì cũng phải làm được qua API/AI agent (xem [T8](../technical-design/08-ai-search-dms-jobs.md)). AI là công dân hạng nhất.
2. **Phân quyền là mặc định:** dữ liệu luôn lọc theo tenant + audience scope + cây tổ chức. Không có "rò rỉ ngang".
3. **Bảo vệ nội dung:** video không tải trực tiếp; watermark động; signed URL ngắn hạn.
4. **Đo được mọi thứ:** mỗi hành động học tạo event → báo cáo & gamification dùng chung nguồn.
5. **Tiếng Việt-first, i18n-ready:** UI/microcopy tối ưu tiếng Việt, nhưng kiến trúc đa ngôn ngữ.
6. **Nội dung > tính năng:** tốc độ tạo & phát hành nội dung là lợi thế cạnh tranh → ưu tiên Publishing & AI.
7. **Mobile-parity cho người học:** trải nghiệm học trên mobile (app) ngang web.

---

## 6. Phạm vi phát hành / Scope (MoSCoW)

> Mức ưu tiên gắn với release; chi tiết phân kỳ ở [P4](04-roadmap-metrics.md).

### 6.1 MUST `[M]` — MVP
- [M] Multi-tenant + cây tổ chức (phòng ban, chức vụ) + 3 audience scope.
- [M] Quản lý người dùng: tạo đơn / danh sách email / import Excel; vai trò cơ bản.
- [M] AuthN (đăng nhập, đổi/đặt mật khẩu, refresh token) + RBAC/ABAC.
- [M] Publishing: **Course** (Module → Lesson video) + **Document**; tab Nội dung/Chỉnh sửa/Cài đặt xuất bản.
- [M] Trải nghiệm học: Explore, Library (filter danh mục), Course detail (hành trình học), **Video player + watermark email động**, đánh dấu hoàn thành.
- [M] Dashboard cá nhân `/cua-ban` (khóa được giao, tiến độ, 4 chỉ số, rank mini).
- [M] Gamification cơ bản: điểm + 9 rank + leaderboard (phòng ban/chức vụ × tháng/quý/năm/all-time).
- [M] Báo cáo cơ bản: báo cáo xuất bản + báo cáo đào tạo (KPI chính).
- [M] DMS folder-based theo phòng ban + chia sẻ.
- [M] Global search.

### 6.2 SHOULD `[S]` — Phase 2
- [S] **Learning Path** (lộ trình), **Exam** (kỳ thi) + Question Bank.
- [S] **Instructor-led**: Lớp/Sự kiện/Workshop + điểm danh + chấm điểm.
- [S] QL đào tạo: **khung giờ đào tạo** (required hours theo phòng/chức vụ) + import Excel + 7 loại hình đào tạo (CRUD).
- [S] Báo cáo đào tạo nâng cao (KPI giảng viên, xếp hạng GV, % hoàn thành khung theo phòng ban).
- [S] **Podcast**, **Video** publishing độc lập.
- [S] Mobile app (iOS/Android) cho người học.

### 6.3 COULD `[C]` — Phase 3
- [C] **AI LMS** đầy đủ: tạo bài học/video/khóa học bằng AI + tra cứu (RAG).
- [C] **SCORM** import/runtime.
- [C] Stickers/achievements nâng cao, mùa giải (seasons).
- [C] "Kế hoạch đào tạo tổ chức" (org training plan) tự động đề xuất.
- [C] SSO/SAML cho doanh nghiệp lớn.

### 6.4 WON'T (this release) `[W]`
- [W] Marketplace bán khóa học ra ngoài / thanh toán e-commerce.
- [W] Tạo video AI text-to-video sinh hình ảnh thật (chỉ làm script/voice ở Phase 3).
- [W] Mạng xã hội học tập (feed, comment threads phức tạp).
- [W] Tích hợp HRIS/payroll sâu (chỉ cung cấp API export).

---

## 7. Ngoài phạm vi & giả định / Out of Scope & Assumptions

**Out of scope (rõ ràng không làm trong tài liệu build này):** cổng thanh toán, kế toán, tuyển dụng, OKR/performance review (ngoài đào tạo).

**Giả định (Assumptions):**
- Mỗi doanh nghiệp = 1 tổ chức (tenant); BetterWork là nhà cung cấp SaaS host nhiều tổ chức.
- Người học có email (định danh chính) hoặc mã nhân viên.
- Nội dung video do tổ chức tự cung cấp (không CDN bên thứ ba kiểu YouTube).
- Quy mô mục tiêu mỗi tổ chức: 100 – 50.000 users; toàn hệ thống: hàng trăm tổ chức (xem NFR ở [T9](../technical-design/09-infra-security-nfr.md)).

---

## 8. Tiêu chí thành công / Success Criteria (định nghĩa "Done" của sản phẩm)

| Khía cạnh | Tiêu chí MVP |
|---|---|
| Onboarding | Admin tạo tổ chức + import 500 user qua Excel < 10 phút |
| Tạo nội dung | Tạo & xuất bản 1 course (5 lesson video) < 30 phút |
| Học tập | Học viên xem video có watermark, đánh dấu hoàn thành, thấy rank cập nhật |
| Phân quyền | Không user nào thấy nội dung ngoài scope/phòng ban được phép (kiểm thử pen-test) |
| Báo cáo | Trưởng phòng xem được % hoàn thành của phòng theo Năm/Quý |
| Hiệu năng | Trang Explore tải < 2.5s (LCP) với 60 course card |

Chi tiết KPI & analytics events: [P4 §3](04-roadmap-metrics.md).
