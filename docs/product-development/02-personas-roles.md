# P2 · Personas & Vai trò / Personas & Roles

> Liên quan: [P1 Vision](01-vision-scope.md) · [P3 Features](03-features-user-stories.md) · [T5 Auth & RBAC](../technical-design/05-auth-rbac-tenancy.md) (implementation kỹ thuật của phân quyền)

Tài liệu này định nghĩa **ai dùng sản phẩm** (personas) và **quyền của họ** (roles × capabilities). Phần này là nguồn chân lý cho thiết kế RBAC ở [T5](../technical-design/05-auth-rbac-tenancy.md).

---

## 1. Personas

### P-1 · Học viên nội bộ — "Lan" (Internal Learner)
- **Bối cảnh:** Nhân viên CSKH, phòng Kinh doanh. Bận, học trên cả điện thoại.
- **Mục tiêu:** Hoàn thành khóa được giao, đủ khung giờ đào tạo, lên rank.
- **Pain:** Không biết phải học gì trước; quên hạn; học rời rạc.
- **Cần:** Dashboard rõ "việc cần làm", tiến độ, nhắc hạn, học mobile, động lực (rank/sticker).

### P-2 · Giảng viên nội bộ — "Minh" (Instructor)
- **Bối cảnh:** Chuyên gia sản phẩm kiêm đào tạo, phòng Đào tạo.
- **Mục tiêu:** Ra nội dung nhanh, dạy lớp, chấm/đánh giá học viên.
- **Pain:** Soạn nội dung tốn thời gian; theo dõi lớp thủ công.
- **Cần:** Trình tạo khóa học (rich-text + chọn tài liệu), AI hỗ trợ soạn, danh sách lớp, chấm điểm, KPI giảng viên.

### P-3 · Trưởng phòng — "Hà" (Department Manager)
- **Bối cảnh:** Trưởng phòng Kinh doanh, chịu trách nhiệm compliance đào tạo của phòng.
- **Mục tiêu:** Phòng đạt đủ khung giờ; biết ai chưa hoàn thành.
- **Pain:** Không có cái nhìn tổng thể phòng; báo cáo thủ công.
- **Cần:** Giao khóa cho phòng, báo cáo % hoàn thành theo nhân sự, xếp hạng phòng.

### P-4 · Admin đào tạo / L&D — "Long Châu" (L&D Admin) — *tài khoản khám phá gốc*
- **Bối cảnh:** Trưởng phòng Đào tạo, **Admin tổng**. Thiết kế toàn bộ chương trình.
- **Mục tiêu:** Vận hành toàn bộ LMS: người dùng, nội dung, khung giờ, báo cáo.
- **Pain:** Cơ cấu tổ chức phức tạp; nhiều đối tượng (nội bộ/đối tác/khách).
- **Cần:** Quản lý người dùng & cây tổ chức, publishing center, QL đào tạo, AI LMS, báo cáo toàn tổ chức.

### P-5 · Quản trị hệ thống tổ chức — "IT Admin" (Org System Admin)
- **Bối cảnh:** IT của doanh nghiệp.
- **Mục tiêu:** Cấu hình tổ chức, phân quyền, tích hợp, bảo mật.
- **Cần:** "Quản lý hệ thống", quản lý vai trò, audit log, cấu hình SSO (Phase 3).

### P-6 · Học viên đối tác — "Đối tác" (Partner Learner)
- **Bối cảnh:** Nhân viên khách hàng B2B được cấp tài khoản scope `PARTNER`.
- **Cần:** Chỉ thấy nội dung dành cho đối tác; trải nghiệm học tương tự P-1 nhưng giới hạn scope.

### P-7 · Khách / Người học công khai — "Khách" (Guest)
- **Bối cảnh:** Người ngoài vào qua link công khai/marketing (scope `GUEST`).
- **Cần:** Xem & học khóa miễn phí công khai; không thấy dữ liệu nội bộ.

### P-8 · Ban giám đốc — "BGĐ" (Executive)
- **Cần:** Dashboard KPI tổng (chỉ đọc): tỷ lệ hoàn thành, giờ đào tạo, KPI giảng viên/học viên.

### P-9 · BetterWork Super Admin (Platform Operator) — *xuyên tenant*
- **Bối cảnh:** Đội vận hành nền tảng BetterWork.
- **Cần:** Tạo/đình chỉ tổ chức, hỗ trợ, giám sát hệ thống, billing (ngoài scope build học tập nhưng cần chỗ móc nối).

---

## 2. Mô hình vai trò / Role Model

Vai trò chia 2 cấp:

- **Platform roles** (xuyên tổ chức): `PlatformSuperAdmin`, `PlatformSupport`.
- **Organization roles** (trong 1 tổ chức): bảng dưới.

| Role (code) | Tiếng Việt | Mô tả | Persona |
|---|---|---|---|
| `OrgOwner` | Admin tổng | Toàn quyền trong tổ chức | P-4 |
| `OrgAdmin` | Quản trị viên | Quản trị vận hành (gần OrgOwner, trừ billing/xóa tổ chức) | P-4/P-5 |
| `LndManager` | QL Đào tạo | Quản lý chương trình, khung giờ, báo cáo toàn tổ chức | P-4 |
| `DeptManager` | Trưởng phòng | Quản lý phòng mình (+ phòng con) | P-3 |
| `Instructor` | Giảng viên | Tạo nội dung, dạy lớp, chấm | P-2 |
| `Learner` | Học viên | Học, thi, xem rank | P-1/P-6/P-7 |
| `Auditor` | Kiểm soát/BGĐ | Chỉ đọc báo cáo & dashboard | P-8 |

> **Lưu ý:** Vai trò **độc lập** với audience scope. Một `Learner` có thể thuộc scope `INTERNAL`, `PARTNER` hoặc `GUEST` — scope quyết định *thấy nội dung nào*, role quyết định *làm được gì*. Quyền hiệu lực = **Role (RBAC) ∩ Scope + nhánh cây tổ chức (ABAC)**. Chi tiết: [T5](../technical-design/05-auth-rbac-tenancy.md).

Một user có thể có **nhiều role** (vd vừa `Instructor` vừa `DeptManager`). Quyền là hợp (union).

---

## 3. Ma trận quyền / Capability Matrix

`✓` = full · `◐` = giới hạn phạm vi (chỉ phòng ban/nhánh mình hoặc nội dung mình tạo) · `–` = không · `R` = read-only

| Capability | OrgOwner | OrgAdmin | LndManager | DeptManager | Instructor | Learner | Auditor |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| **Người dùng & tổ chức** |||||||
| Quản lý cây tổ chức (phòng/chức vụ) | ✓ | ✓ | ◐ | – | – | – | R |
| Tạo/sửa/khóa user | ✓ | ✓ | ◐ | ◐ | – | – | R |
| Import Excel user | ✓ | ✓ | ◐ | ◐ | – | – | – |
| Gán vai trò | ✓ | ✓ | ◐ | – | – | – | – |
| Quản lý hệ thống (cấu hình tổ chức) | ✓ | ✓ | – | – | – | – | – |
| **Nội dung & xuất bản** |||||||
| Tạo/sửa Course/Path/Exam | ✓ | ✓ | ✓ | ◐ | ◐(của mình) | – | – |
| Xuất bản (publish) | ✓ | ✓ | ✓ | ◐ | ◐ | – | – |
| Chọn audience scope khi publish | ✓ | ✓ | ✓ | ◐ | ◐ | – | – |
| Quản lý DMS (tài liệu) | ✓ | ✓ | ✓ | ◐ | ◐ | R(được chia sẻ) | R |
| Ngân hàng câu hỏi | ✓ | ✓ | ✓ | ◐ | ◐ | – | – |
| **Đào tạo (instructor-led)** |||||||
| Tạo Lớp/Sự kiện/Workshop | ✓ | ✓ | ✓ | ◐ | ◐ | – | – |
| Điểm danh / chấm điểm | ✓ | ✓ | ✓ | ◐ | ◐(lớp mình) | – | – |
| **QL đào tạo** |||||||
| Cài đặt khung giờ đào tạo | ✓ | ✓ | ✓ | – | – | – | R |
| Quản lý loại hình đào tạo | ✓ | ✓ | ✓ | – | – | – | R |
| Giao khóa cho user/phòng (assign) | ✓ | ✓ | ✓ | ◐ | – | – | – |
| **Học tập** |||||||
| Học khóa / xem video / hoàn thành | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| Làm bài thi | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| Xem rank/leaderboard cá nhân | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | R |
| **Báo cáo** |||||||
| Báo cáo xuất bản | ✓ | ✓ | ✓ | ◐ | R(của mình) | – | R |
| Báo cáo đào tạo (KPI) | ✓ | ✓ | ✓ | ◐(phòng) | R(lớp/HV mình) | R(của mình) | R |
| Xuất/Export báo cáo | ✓ | ✓ | ✓ | ◐ | – | – | ◐ |
| **AI LMS** |||||||
| Dùng AI tạo nội dung | ✓ | ✓ | ✓ | ◐ | ✓ | – | – |
| Dùng AI tra cứu | ✓ | ✓ | ✓ | ✓ | ✓ | ◐(cho phép) | – |

> Ma trận này được mã hóa thành **permission codes** (vd `users.create`, `publications.publish`, `reports.training.read`) trong [T5 §3](../technical-design/05-auth-rbac-tenancy.md). UI ẩn/disable theo permission; backend luôn enforce lại (defense in depth).

---

## 4. Quy tắc phạm vi (ABAC) tóm tắt

1. **Tenant isolation:** mọi truy vấn gắn `organization_id` của user; không bao giờ trả dữ liệu chéo tổ chức (trừ platform role).
2. **Department scope (◐):** `DeptManager`/`LndManager` (giới hạn) chỉ thao tác trên **nhánh cây** mình phụ trách (closure table — gồm phòng con).
3. **Ownership scope (◐ "của mình"):** `Instructor` sửa/báo cáo nội dung & lớp **mình tạo/được gán dạy**.
4. **Audience scope:** nội dung publish với scope nào thì chỉ user thuộc scope đó (và được nằm trong audience list/phòng ban đích) mới thấy.
5. **Self scope:** `Learner` chỉ xem dữ liệu học **của chính mình**.

Các trường hợp biên (edge cases) & cách enforce: [T5 §4–§5](../technical-design/05-auth-rbac-tenancy.md).
