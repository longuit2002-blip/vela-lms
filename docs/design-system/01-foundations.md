# D1 · Design System — Foundations & Tokens

> Liên quan: [D2 Components](02-components.md) · [D3 Patterns](03-patterns-content.md) · [T1 stack](../technical-design/01-architecture-stack.md) (Tailwind + shadcn/ui)

Đây là nền tảng UI Design System: **principles + design tokens**. Tokens là nguồn chân lý, hiện thực bằng **CSS variables + Tailwind theme** và component shadcn/ui. Tên token tiếng Anh; mô tả tiếng Việt.

---

## 1. Design Principles

1. **Rõ ràng hơn hoa mỹ (Clarity over decoration):** dày đặc dữ liệu (báo cáo, bảng) nhưng phải dễ quét. Ưu tiên thứ bậc thị giác.
2. **Hướng tiến độ (Progress-forward):** người học luôn thấy "đang ở đâu / còn bao nhiêu" (progress bar, rank, x/y).
3. **Tin cậy & chuyên nghiệp (Enterprise trust):** đây là công cụ doanh nghiệp — bảng màu điềm tĩnh, nhất quán; gamification thêm điểm nhấn nhưng không "trẻ con".
4. **Tiếng Việt-first:** typography & spacing tối ưu cho tiếng Việt (dấu phụ cao → cần line-height thoáng).
5. **Nhất quán đa nền tảng:** token dùng chung web/mobile.
6. **Tránh "AI slop":** không gradient tím vô hồn, không emoji thừa, không layout generic — bám brand & ngữ cảnh LMS.
7. **Accessible by default:** contrast AA, focus visible, keyboard nav, caption video.

---

## 2. Color tokens

### 2.1 Semantic (light theme — mặc định)
Dùng **semantic tokens** (không hardcode hex trong component). Giá trị tham chiếu (HSL/hex), tinh chỉnh theo brand BetterWork khi có.

| Token | Hex (gợi ý) | Dùng cho |
|---|---|---|
| `--color-bg` | `#F7F8FA` | Nền app |
| `--color-surface` | `#FFFFFF` | Card, panel, modal |
| `--color-surface-muted` | `#F1F3F6` | Nền phụ, hover row |
| `--color-border` | `#E3E7EE` | Viền, divider |
| `--color-text` | `#1A2230` | Chữ chính |
| `--color-text-muted` | `#5B6675` | Chữ phụ, caption |
| `--color-text-subtle` | `#8A94A6` | Placeholder, disabled |
| **Brand / Primary** |||
| `--color-primary` | `#2563EB` | Hành động chính, link, active nav |
| `--color-primary-hover` | `#1D4ED8` | Hover |
| `--color-primary-fg` | `#FFFFFF` | Chữ trên primary |
| `--color-primary-subtle` | `#EAF1FF` | Nền nhạt primary (badge, selected) |
| **Status** |||
| `--color-success` | `#16A34A` | Hoàn thành, đạt |
| `--color-warning` | `#D97706` | Sắp hết hạn, cảnh báo nhẹ |
| `--color-danger` | `#DC2626` | Lỗi, xóa, thao tác nhạy cảm |
| `--color-info` | `#0891B2` | Thông tin |

### 2.2 Rank palette (gamification — 9 bậc)
Màu nhận diện rank (badge/leaderboard); dùng cho `RankBadge` ([D2](02-components.md)):
| Rank | Token | Hex |
|---|---|---|
| Đồng/Bronze | `--rank-bronze` | `#A97142` |
| Bạc/Silver | `--rank-silver` | `#9AA4B2` |
| Vàng/Gold | `--rank-gold` | `#E0A21A` |
| Bạch Kim/Platinum | `--rank-platinum` | `#3FB0AC` |
| Kim Cương/Diamond | `--rank-diamond` | `#4FAEEB` |
| Tinh Anh/Emerald | `--rank-emerald` | `#19B36B` |
| Cao Thủ/Master | `--rank-master` | `#8B5CF6` |
| Chiến Tướng/Grandmaster | `--rank-grandmaster` | `#E0518A` |
| Thách Đấu/Challenger | `--rank-challenger` | `#E4503A` |

### 2.3 Data-viz palette (charts báo cáo)
Bộ màu phân biệt cao cho biểu đồ (loại hình đào tạo, pie hoàn thành): `#2563EB #16A34A #D97706 #8B5CF6 #0891B2 #E0518A #64748B`. Đảm bảo phân biệt với người mù màu (kiểm tra).

### 2.4 Dark theme
Định nghĩa token tương ứng (`--color-bg:#0F1623`, `--color-surface:#161E2E`, `--color-text:#E6EAF2`…). Component dùng semantic token → tự đổi theme. Dark theme = Phase 2.

---

## 3. Typography

- **Font chữ:** **Inter** (Latin + Vietnamese subset) hoặc **Be Vietnam Pro** (thuần Việt, dấu đẹp) cho UI; monospace `JetBrains Mono` cho code/số liệu kỹ thuật. Tải qua self-host (không phụ thuộc Google CDN cho privacy).
- **Vietnamese line-height** thoáng hơn (dấu phụ): base `1.6` cho body.

### 3.1 Type scale
| Token | Size / Line-height | Weight | Dùng |
|---|---|---|---|
| `text-display` | 32 / 40 | 700 | Hero, tiêu đề trang lớn |
| `text-h1` | 24 / 32 | 700 | Tiêu đề trang |
| `text-h2` | 20 / 28 | 600 | Section |
| `text-h3` | 16 / 24 | 600 | Card title, sub-section |
| `text-body` | 14 / 22 | 400 | Mặc định |
| `text-body-strong` | 14 / 22 | 600 | Nhấn |
| `text-sm` | 13 / 20 | 400 | Phụ, bảng |
| `text-caption` | 12 / 18 | 400 | Caption, meta (lượt xem, last-seen) |
| `text-overline` | 11 / 16 | 600, uppercase, tracking+ | Nhãn nhóm ("TỔ CHỨC", "HẠNG") |

> "Overline uppercase" khớp nhãn trong nguồn ("HÀNH TRÌNH HỌC TẬP", "TỔ CHỨC", "HẠNG").

---

## 4. Spacing & layout

- **Spacing scale (4px base):** `0,4,8,12,16,20,24,32,40,48,64` → token `space-1..space-16` (Tailwind `1=4px`).
- **Container:** max-width `1280px`; gutter `24px`; nội dung admin có thể full-width.
- **Grid:** 12 cột; course grid responsive `repeat(auto-fill, minmax(260px, 1fr))`.
- **Layout chính:** Top nav (h `64px`) + Sidebar admin (w `240px`, collapsible `64px`) + content.

---

## 5. Radius, elevation, border

| Token | Giá trị | Dùng |
|---|---|---|
| `radius-sm` | 6px | input, badge |
| `radius-md` | 10px | button, card nhỏ |
| `radius-lg` | 14px | card, panel |
| `radius-xl` | 20px | modal, hero |
| `radius-full` | 9999px | avatar, pill, rank badge |
| `shadow-sm` | `0 1px 2px rgba(16,24,40,.06)` | card |
| `shadow-md` | `0 4px 12px rgba(16,24,40,.08)` | dropdown, popover |
| `shadow-lg` | `0 12px 32px rgba(16,24,40,.14)` | modal |
| `border-width` | 1px | mặc định |

---

## 6. Motion

- **Durations:** `fast 120ms`, `base 200ms`, `slow 320ms`. Easing `cubic-bezier(.2,.8,.2,1)`.
- Dùng cho: hover, mở dropdown/modal (fade+scale 98→100%), progress bar fill, rank-up celebration (Phase 2), skeleton shimmer khi SPA render trễ.
- **prefers-reduced-motion:** tôn trọng → tắt animation lớn.

---

## 7. Iconography
- Bộ icon line nhất quán (**Lucide**). Stroke `1.75px`, size `16/20/24`.
- Icon nav khớp chức năng (Explore, Library, Ranking, Reports, Members, Documents, Publish, Training, AI).
- Không emoji trong UI sản phẩm (trừ sticker gamification có thiết kế riêng).

---

## 8. Token implementation (Tailwind + CSS vars)

```css
/* globals.css */
:root {
  --color-bg:#F7F8FA; --color-surface:#FFF; --color-border:#E3E7EE;
  --color-text:#1A2230; --color-text-muted:#5B6675;
  --color-primary:#2563EB; --color-primary-hover:#1D4ED8; --color-primary-fg:#FFF;
  --color-success:#16A34A; --color-warning:#D97706; --color-danger:#DC2626;
  --radius-lg:14px; /* ... */
}
```
```js
// tailwind.config.js (trích)
theme: { extend: {
  colors: {
    bg:'var(--color-bg)', surface:'var(--color-surface)', border:'var(--color-border)',
    text:{DEFAULT:'var(--color-text)', muted:'var(--color-text-muted)'},
    primary:{DEFAULT:'var(--color-primary)', hover:'var(--color-primary-hover)', fg:'var(--color-primary-fg)'},
    success:'var(--color-success)', warning:'var(--color-warning)', danger:'var(--color-danger)',
  },
  borderRadius:{ lg:'var(--radius-lg)' },
}}
```
> Component (shadcn/ui) chỉ dùng class semantic (`bg-surface`, `text-muted`, `text-primary-fg`) → đổi token là đổi toàn hệ thống, không sửa component. Phù hợp đề xuất "design system bằng token" trong [T1](../technical-design/01-architecture-stack.md).

---

## 9. Naming & governance
- Token đặt tên **theo vai trò ngữ nghĩa**, không theo giá trị (`--color-danger` không `--color-red`).
- Component không hardcode hex/px ngoài token.
- Thêm token mới → cập nhật file này (nguồn chân lý) + Tailwind config + (option) Storybook.
- Versioning design system song hành với app; thay đổi breaking ghi changelog.
