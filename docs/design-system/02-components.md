# D2 · Design System — Component Library

> Liên quan: [D1 Tokens](01-foundations.md) · [D3 Patterns](03-patterns-content.md) · [P3 Features](../product-development/03-features-user-stories.md)

Đặc tả component tái dùng. Mỗi component: **mục đích · props/variants · states · a11y**. Hiện thực trên **shadcn/ui (Radix) + Tailwind**, dùng token từ [D1](01-foundations.md). Đặt trong `web/components/ui` (primitives) và `web/components/<domain>` (composed).

**Quy ước states chung:** `default · hover · active/pressed · focus-visible (ring primary) · disabled · loading`. Mọi interactive element có `focus-visible` ring (a11y).

---

## 1. Primitives (ui/)

### 1.1 `Button`
- **Variants:** `primary` (CTA), `secondary` (outline), `ghost` (nav/toolbar), `destructive` (xóa/thao tác nhạy cảm), `link`.
- **Sizes:** `sm(32) · md(40) · lg(48)`; `icon` (vuông).
- **Props:** `leftIcon, rightIcon, loading, fullWidth, disabled`.
- **A11y:** `<button>` thật; loading → `aria-busy`, giữ width; disabled không chỉ bằng màu.
- **Dùng:** "Xuất bản" = primary; "Xóa/Khóa tài khoản" = destructive + confirm.

### 1.2 `Input` / `Textarea` / `Select` / `Checkbox` / `Radio` / `Switch`
- States gồm `error` (viền danger + message), `success`. Label + helper + error text.
- `Switch` = toggle cài đặt xuất bản (Công khai, Học theo thứ tự…).
- `Checkbox` = filter đa danh mục (Library) — có indeterminate cho nhóm.
- **A11y:** label `htmlFor`; error qua `aria-describedby`; nhóm radio `role=radiogroup`.

### 1.3 `Badge` / `Pill` / `Tag`
- Variants: `neutral, primary-subtle, success, warning, danger, scope` (INTERNAL/PARTNER/GUEST màu riêng).
- Dùng: badge vai trò ("Admin tổng"), badge số lượng trên nhóm filter, scope tag.

### 1.4 `Avatar` / `AvatarGroup`
- Sizes `xs..lg`; fallback initials; `AvatarGroup` (chồng + "+N") cho "nhóm người học" trên course card.

### 1.5 `Tabs`
- Underline style. Dùng: Nội dung/Báo cáo (course), Xuất bản/Đào tạo (publishing/reports), Phòng ban/Chức vụ (leaderboard/members).
- **A11y:** Radix Tabs (roving tabindex, `aria-selected`).

### 1.6 `Dropdown` / `Menu` / `Popover` / `Tooltip`
- Dùng: profile dropdown, dropdown "Xuất bản đào tạo/nội dung", quality/speed menu player, action menu hàng bảng.
- **A11y:** keyboard nav, `esc` đóng, focus trap trong menu.

### 1.7 `Modal` / `Dialog` / `Sheet`
- `Dialog` center (form, confirm); `Sheet` trượt phải (chi tiết, filter mobile).
- **ConfirmDialog** chuyên cho **sensitive ops** (tạo TK, publish, xóa): tiêu đề rõ hậu quả, nút destructive, **không auto-submit** ([T4 §5](../technical-design/04-api-design.md)).
- **A11y:** focus trap, `aria-modal`, trả focus về trigger, `esc` đóng (trừ form dở → confirm).

### 1.8 `Table` / `DataGrid`
- Sticky header, sort, row hover, pagination (cursor), empty/loading state, row actions.
- Dùng: bảng thành viên, báo cáo theo nhân sự, leaderboard dạng list.
- **A11y:** `<table>` semantic; sort qua `aria-sort`; cột scope/trạng thái có text, không chỉ màu.

### 1.9 `ProgressBar` / `ProgressRing` / `Stat`
- `ProgressBar` (course %, khung giờ); `ProgressRing` (rank progress, pie hoàn thành nhỏ).
- `Stat`: số lớn + nhãn overline + delta (4 chỉ số dashboard, 6 thẻ báo cáo).

### 1.10 `Skeleton` / `Spinner` / `EmptyState` / `Toast`
- `Skeleton` cho SPA render trễ (nguồn §14). `EmptyState` (icon + text + CTA) — vd dashboard rỗng → "Khám phá thư viện". `Toast` cho kết quả async (job done) + lỗi.

### 1.11 `Breadcrumb` / `Pagination` / `SearchInput`
- `SearchInput` global (header) + trong DMS/members; debounce; gợi ý.

---

## 2. Navigation (layout/)

### 2.1 `TopNav`
- Logo + menu người học (Khám phá, Từ công ty, Thư viện, Xếp hạng, Báo cáo) + global search + profile.
- Active state = primary underline/bg. Responsive → hamburger drawer.

### 2.2 `AdminSidebar`
- Mục: Thành viên, Tài liệu, Xuất bản, QL đào tạo, AI LMS, Hướng dẫn. Icon + label; collapsible (240↔64px).
- **Ẩn mục theo permission** ([P2 §3](../product-development/02-personas-roles.md)); ẩn hoàn toàn với Learner thuần.

### 2.3 `ProfileMenu`
- Quản lý tài khoản, Quản lý hệ thống (admin), Ngôn ngữ (Tiếng Việt), Đổi mật khẩu, Đăng xuất.

### 2.4 `OrgTree`
- Cây tổ chức (TỔ CHỨC → phòng ban + KH-ĐỐI TÁC, KHÁCH), expand/collapse, chọn node → lọc. Dùng ở Members, DMS, Publishing target, Training mgmt.
- **A11y:** `role=tree`, keyboard (mũi tên, enter).

---

## 3. Domain components

### 3.1 `CourseCard` ⭐ (khớp nguồn §2)
- **Hiển thị:** thumbnail (16:9), badge lượt xem, tên khóa (2 dòng truncate), **ProgressBar %**, số bài học, **AvatarGroup** nhóm người học + tổng số người.
- **Variants:** `course` | `path` (lộ trình — badge "Lộ trình") | `document` (Library).
- States: hover (elevate), `assigned` (badge), `completed` (✓), `expiring` (warning).

### 3.2 `LessonList` / `JourneyCard` (course detail — nguồn §4.1)
- `JourneyCard` "HÀNH TRÌNH HỌC TẬP": Hoàn thành x/y + ProgressRing %.
- `LessonList`: gom theo module/chương (accordion); mỗi lesson: icon video, tên, thời lượng, **trạng thái** (Hoàn thành ✓ / Chưa bắt đầu); khóa (lock) nếu `sequential` & lesson trước chưa xong.

### 3.3 `VideoPlayer` ⭐ (nguồn §4.2 — xem [T6](../technical-design/06-media-video-scorm.md))
- Controls: play/pause, seek (buffered+played), volume, time, **quality menu**, **speed**, **PiP**, fullscreen.
- **`WatermarkOverlay`** (sub-component): email/mã NV động, đổi vị trí định kỳ, opacity thấp, `pointer-events:none`, không tắt được.
- Resume vị trí; gửi progress throttle; ngưỡng hoàn thành.
- **A11y:** keyboard (space/←/→/F/M), captions (track), `aria-label` mọi nút.

### 3.4 `RankBadge` / `RankProgress` / `LeaderboardRow` (gamification — nguồn §5)
- `RankBadge`: huy hiệu theo 9 màu rank ([D1 §2.2](01-foundations.md)) + tên.
- `RankProgress`: rank hiện tại + điểm + "còn X điểm lên [rank kế]".
- `LeaderboardRow`: thứ hạng (top 3 nổi bật), avatar, tên, phòng/chức vụ, điểm; highlight "tôi".
- `LeaderboardFilters`: tab Phòng ban/Chức vụ + scope (TỔ CHỨC/KH-ĐỐI TÁC/KHÁCH) + kỳ (Tháng/Quý/Năm/Toàn thời gian).

### 3.5 `StatCardRow` (dashboard & reports)
- 4 chỉ số dashboard (Hoàn thành/Giờ học/Đang học/Chưa học) + 6 thẻ báo cáo xuất bản. Dùng `Stat`.

### 3.6 `FilterPanel` (Library — nguồn §4)
- Cột trái: nhóm checkbox đa danh mục, mỗi nhóm có badge số lượng; "Xóa lọc"; responsive → Sheet trên mobile.

### 3.7 `Chart` wrappers (reports)
- `PieChart` (tỷ lệ hoàn thành), `BarChart` (loại hình đào tạo, % khung theo phòng), `RankingBar` (xếp hạng GV). Dùng data-viz palette ([D1 §2.3](01-foundations.md)); legend, tooltip, empty state.

### 3.8 `RichTextEditor` (TipTap — nguồn §9.3)
- Toolbar đầy đủ: heading, bold/italic/underline, list, link, image, table, quote, code. Output HTML sanitized ([T9 §5.3](../technical-design/09-infra-security-nfr.md)).

### 3.9 `PublishSettingsForm` ⭐ (nguồn §9.3)
- Đối tượng truy cập (5 nhóm) + **quick-add** email/mã NV/SĐT (chip input) + `OrgTree` target.
- Toggles/fields: Công khai, Tiếp tục học khi hết hạn, Học theo thứ tự, Loại hình đào tạo (Select), Điểm xếp hạng hoàn thành (number), Lĩnh vực (Select), Hết hạn sau (ngày), Thời gian đào tạo (giờ). Nút **Xuất bản** (primary + confirm).

### 3.10 `MemberRow` / `AddAccountModal` (nguồn §7)
- `MemberRow`: tên + @handle + `Badge` vai trò + liên hệ + phòng/chức vụ + trạng thái + last-seen.
- `AddAccountModal`: 3 tab phương thức (Tạo đơn / Danh sách email / Từ Excel); Excel → upload + link file mẫu + bảng lỗi theo dòng; **ConfirmDialog** vì sensitive.

### 3.11 `AiChatPanel` (nguồn §11)
- Khung chat: message list (stream), input "Hỏi bất kỳ điều gì" + mic + đính kèm(+), 4 quick-action chips. Draft card có nút "Dùng/Chỉnh/Bỏ". Citations cho tra cứu.

### 3.12 `WizardSteps` (course creator 3 tab, onboarding)
- Stepper: Nội dung / Chỉnh sửa / Cài đặt xuất bản; trạng thái valid mỗi bước; lưu nháp.

---

## 4. Accessibility checklist (mọi component)
- [ ] Keyboard operable (tab/enter/esc/mũi tên); focus-visible ring.
- [ ] Contrast ≥ AA; thông tin không chỉ bằng màu (kèm icon/text — vd trạng thái, scope, rank).
- [ ] ARIA roles/labels đúng (Radix lo phần lớn).
- [ ] Form: label + error liên kết; thông báo lỗi rõ tiếng Việt.
- [ ] Video: captions + điều khiển bàn phím.
- [ ] Reduced-motion tôn trọng.

## 5. Tổ chức code & Storybook
- `web/components/ui/*` (primitives) · `web/components/<domain>/*` (composed).
- **Storybook** mỗi component: variants + states + a11y addon; là "living design system".
- Không tạo biến thể tùy hứng — thêm variant phải vào doc này.
