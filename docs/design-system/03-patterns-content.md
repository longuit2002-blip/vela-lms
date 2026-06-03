# D3 · Design System — Patterns, Layouts & Content

> Liên quan: [D1 Tokens](01-foundations.md) · [D2 Components](02-components.md) · [P4 Sitemap](../product-development/04-roadmap-metrics.md)

Phần này gắn token + component ([D1](01-foundations.md), [D2](02-components.md)) thành **page templates** và quy định **microcopy/voice tiếng Việt**.

---

## 1. App shell (mọi trang)

```
┌────────────────────────────────────────────────────────────┐
│ TopNav: Logo │ Khám phá Từ-công-ty Thư-viện Xếp-hạng Báo-cáo │ 🔍  👤 │
├──────────┬─────────────────────────────────────────────────┤
│ Admin    │                                                  │
│ Sidebar  │   Page content (max-w 1280, gutter 24)           │
│ (240px,  │                                                  │
│ collapse)│                                                  │
└──────────┴─────────────────────────────────────────────────┘
```
- Sidebar chỉ hiện với role admin; người học thuần → chỉ TopNav, content full.
- SPA: chuyển trang giữ shell, chỉ thay content; **Skeleton** khi render trễ.
- Mobile: TopNav → hamburger; sidebar → drawer; bottom nav (mobile app) cho người học.

---

## 2. Page templates

### 2.1 Explore `/` (nguồn §2)
- **Hero kép** (2 CTA: "30 khóa miễn phí" + "Tạo LMS cho doanh nghiệp") + dải logo "550+ doanh nghiệp".
- **Section per skill-group** (6 nhóm): heading overline + badge số lượng + grid `CourseCard` (auto-fill 260px), nút "Xem tất cả".
- Card lộ trình phân biệt rõ. Lazy-load section khi cuộn.

### 2.2 Dashboard `/cua-ban` (nguồn §3)
- **2 cột:** trái = danh sách khóa được giao (hoặc EmptyState + CTA "Khám phá thư viện"); phải = **Profile widget** sticky.
- Profile widget (trên→dưới): vai trò · `RankProgress` (rank + điểm lên hạng) · `StatCardRow` 4 chỉ số · filter Năm+Quý · stickers · "Kế hoạch đào tạo tổ chức" · leaderboard mini.

### 2.3 Library `/thu-vien` (nguồn §4)
- **2 cột:** trái = `FilterPanel` (checkbox đa danh mục + badge số lượng); phải = grid `CourseCard`/`DocumentCard`. Mobile: filter trong Sheet.

### 2.4 Course detail `/noi-dung/<slug>` (nguồn §4.1)
- Header khóa (thumbnail, tên, danh mục, meta) + `JourneyCard` (x/y, %).
- **Tabs: Nội dung | Báo cáo.** Nội dung = `LessonList` theo chương + "Những gì bạn sẽ học" + CTA "Học bài đầu tiên". Báo cáo = bảng tiến độ HV (theo quyền).

### 2.5 Player `/noi-dung/<slug>/hoc/<lessonId>` (nguồn §4.2)
- `VideoPlayer` (16:9) + `WatermarkOverlay` · sidebar phải = `LessonList` (điều hướng, khóa nếu sequential) · nút trước/sau · auto-next.

### 2.6 Leaderboard `/xep-hang` (nguồn §5)
- `LeaderboardFilters` (tab Phòng ban/Chức vụ + scope + kỳ) · top 3 podium nổi bật · `LeaderboardRow` list · vị trí "tôi" highlight (kể cả ngoài top).

### 2.7 Reports `/bao-cao`, `/bao-cao-dao-tao` (nguồn §6)
- Tabs Xuất bản/Đào tạo · filter Năm/Quý (+phòng ban) · `StatCardRow` (6 thẻ) · `Chart` (pie hoàn thành; bar loại hình; ranking GV; % khung theo phòng) · nút Export.

### 2.8 Members `/thanh-vien` (nguồn §7)
- Tabs Phòng ban/Chức vụ · `OrgTree` trái + bảng `MemberRow` phải · nút "+ Thêm tài khoản" → `AddAccountModal`. Bulk actions (khóa, đổi phòng).

### 2.9 DMS `/tai-lieu` (nguồn §8)
- `OrgTree` + "CHIA SẺ VỚI TÔI" trái · breadcrumb + grid/list tài liệu phải · `SearchInput` (folder/tài liệu/câu hỏi) · actions (tạo thư mục, upload, chia sẻ).

### 2.10 Publishing `/xuat-ban` + creators (nguồn §9)
- Center: tabs Xuất bản/Đào tạo · toggle Grid/List · cây đích · 2 dropdown tạo (đào tạo 4 loại / nội dung 5 loại).
- **Course creator** `/tao-xuat-ban/xuat-ban-khoa-hoc`: `WizardSteps` (Nội dung | Chỉnh sửa | Cài đặt xuất bản). Chỉnh sửa = `RichTextEditor` + panel chọn tài liệu (Theo phòng ban/Được chia sẻ + Tạo thư mục). Cài đặt = `PublishSettingsForm`.

### 2.11 Training mgmt `/quan-ly-dao-tao` (nguồn §10)
- 3 tab: Báo cáo theo nhân sự (bảng: Star priority, Tổng giờ, Tiến độ %, Khác) · Cài đặt khung giờ (theo phòng/chức vụ + Thêm Tag/Import Excel/Tải mẫu) · Loại hình đào tạo (CRUD 7 loại).

### 2.12 AI LMS `/ai-lms` (nguồn §11)
- `AiChatPanel` center · 4 quick-action chips · draft review.

---

## 3. Pattern chung

### 3.1 Empty / Loading / Error
- **Empty:** icon + 1 câu giải thích + CTA (vd "Bạn chưa được giao khóa nào. Khám phá thư viện →").
- **Loading:** Skeleton khớp layout (không spinner toàn trang khi SPA navigate).
- **Error:** thông báo tiếng Việt rõ + hành động ("Thử lại"); không lộ kỹ thuật.

### 3.2 Sensitive actions (tạo TK, publish, xóa, import)
- `ConfirmDialog`: tiêu đề nêu hậu quả · liệt kê ảnh hưởng (vd "Khóa sẽ hiển thị cho 320 người") · nút destructive/primary rõ · **không auto-submit** · toast kết quả. (Quy ước [T4 §5](../technical-design/04-api-design.md).)

### 3.3 Forms
- Label trên field · helper text · validate realtime + on-submit · error đỏ + message · disable nút khi submitting · giữ dữ liệu khi lỗi.

### 3.4 Tables/lists lớn
- Sticky header · cursor pagination · sort · filter · bulk select · empty state · cột trạng thái/scope có icon+text.

### 3.5 Responsive
- Breakpoints: `sm 640 · md 768 · lg 1024 · xl 1280`. 2 cột → 1 cột (md xuống); filter/panel → Sheet; grid giảm cột tự nhiên.

---

## 4. Microcopy & Voice (tiếng Việt)

### 4.1 Giọng điệu
- **Chuyên nghiệp, thân thiện, rõ ràng, ngắn.** Xưng hô: gọi người dùng là **"bạn"**; hệ thống trung tính.
- Tránh biệt ngữ kỹ thuật với người học; admin có thể chi tiết hơn.
- Tích cực & khích lệ ở ngữ cảnh học/gamification ("Tuyệt vời! Bạn vừa hoàn thành…", "Còn 120 điểm nữa để lên hạng Vàng").

### 4.2 Quy ước thuật ngữ (nhất quán toàn app — khớp [Glossary README](../README.md))
| Dùng | Không dùng |
|---|---|
| Khóa học | Lớp online (gây nhầm với lớp instructor-led) |
| Bài học | Video (khi nói đơn vị học) |
| Lộ trình | Path/Track (trong UI tiếng Việt) |
| Hoàn thành | Done/Xong |
| Xuất bản | Publish/Đăng |
| Hạng | Cấp/Level |
| Thành viên | Người dùng (trong khu quản trị tổ chức) |

### 4.3 Nhãn & nút (mẫu)
- Nút chính: "Xuất bản", "Lưu nháp", "Thêm tài khoản", "Bắt đầu học", "Học bài tiếp theo".
- Nhãn overline (in hoa): "HÀNH TRÌNH HỌC TẬP", "HẠNG", "TỔ CHỨC", "KH-ĐỐI TÁC", "KHÁCH".
- Trạng thái: "Hoàn thành", "Đang học", "Chưa bắt đầu", "Sắp hết hạn", "Đã hết hạn".

### 4.4 Thông báo (mẫu)
- Thành công: "Đã xuất bản khóa học. 320 thành viên có thể truy cập."
- Lỗi validate: "Email không hợp lệ.", "Email này đã tồn tại trong tổ chức."
- Cảnh báo sensitive: "Bạn sắp tạo 152 tài khoản và đặt mật khẩu mặc định. Hành động này không thể hoàn tác tự động. Tiếp tục?"
- Empty: "Chưa có dữ liệu cho kỳ này."

### 4.5 Ngày/giờ & số
- Ngày: `dd/MM/yyyy`; giờ: 24h. Số lớn: phân tách `.` (1.250 điểm). Giờ học: "12,5 giờ".
- Hiển thị theo timezone tổ chức; lưu UTC.

### 4.6 i18n-ready
- Mọi chuỗi qua **resource key** (next-intl), không hardcode trong component → sẵn sàng đa ngôn ngữ ([T9 §6](../technical-design/09-infra-security-nfr.md)).
- Tránh ghép chuỗi động khó dịch; dùng ICU message format cho số nhiều/biến.

---

## 5. Chống "AI slop" (kiểm tra trước khi ship UI)
- [ ] Bám brand & ngữ cảnh LMS doanh nghiệp — không gradient/illustration generic vô nghĩa.
- [ ] Thứ bậc thị giác rõ (1 hành động chính/màn hình).
- [ ] Khoảng trắng & canh lề nhất quán theo spacing scale.
- [ ] Microcopy cụ thể, tiếng Việt tự nhiên — không "Lorem"/máy móc.
- [ ] Trạng thái empty/loading/error đều được thiết kế.
- [ ] Dữ liệu thật (số lớp, người học) thay vì placeholder khi demo.
