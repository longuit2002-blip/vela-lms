# Handover Context — Better Work LMS Design Research
> Tạo từ cuộc trò chuyện với Claude.ai · June 2026  
> Mục đích: đưa file này vào Claude Code để tiếp tục phân tích và implement design

---

## 1. PROJECT CONTEXT

**Tên sản phẩm:** Better Work  
**Loại:** LMS (Learning Management System) — nền tảng học trực tuyến cho doanh nghiệp/cá nhân  
**Mục tiêu design:** Có phong cách riêng (không generic AI slop), chuẩn UX, phù hợp cho cả marketing site lẫn app UI

**2 surfaces cần thiết kế riêng biệt:**

| Surface | Mục tiêu | Design register |
|---|---|---|
| Landing / Marketing page | Convert visitor → sign up | Brand: Distinctive, inspiring trust |
| App / Dashboard (LMS UI) | Học viên focus vào nội dung | Product: Clean, không phân tâm |

---

## 2. RESEARCH ĐÁNH GIÁ DESIGN SKILLS

### 2.1 Bối cảnh — vấn đề "AI slop" trong frontend
Hầu hết AI-generated UI đều converge về cùng một aesthetic: Inter font, purple-to-blue gradient, 4-card grid, Lucide icons, centered hero. Cộng đồng gọi đây là "AI slop". Các design skills dưới đây được tạo ra để giải quyết vấn đề này.

### 2.2 Skill đã nghiên cứu chi tiết

---

#### A. `pbakaus/impeccable` ⭐ (Recommended — cặp với Taste Skill)

**Tác giả:** Paul Bakaus (ex-Google DevRel, creator of jQuery UI)  
**GitHub:** https://github.com/pbakaus/impeccable  
**Stars:** ~34k  
**Website:** https://impeccable.style  
**License:** Free, MIT

**Đặc điểm nổi bật:**
- 1 skill, **23 sub-commands** nhóm theo lifecycle (Create → Evaluate → Refine → Simplify → Harden → System)
- **2 design registers** — đây là điểm khác biệt quan trọng nhất:
  - `Brand mode`: landing page, marketing, portfolio → **Distinctiveness là bar**
  - `Product mode`: dashboard, app, admin → **Earned familiarity là bar** (giống Linear/Notion/Figma)
- **46 slop rules** (41 deterministic + 5 LLM-based)
- CLI detector: `npx impeccable detect src/` — chạy trong PR check, exit code cho build gate
- Chrome extension: overlay slop detection trên bất kỳ page nào
- Live Mode (Beta): pick element trong browser → 3 variants → Accept → ghi thẳng vào source
- PRODUCT.md: context file — users, register, brand voice, anti-references (mọi command đều đọc trước khi design)
- DESIGN.md export: Google Stitch format, portable sang mọi AI tool

**23 Commands quan trọng:**

```
# Create
/impeccable init        → Tạo PRODUCT.md (chạy 1 lần đầu tiên)
/impeccable shape       → Discovery interview → brief trước khi build
/impeccable craft       → Design + build all-in-one

# Evaluate
/impeccable audit       → Technical quality check (P0→P3 severity)
/impeccable critique    → Design review + persona test + slop detection

# Refine
/impeccable bolder      → Đẩy lên impact không chaos
/impeccable quieter     → Tone xuống khi "đang hét"
/impeccable typeset     → Fix typography
/impeccable colorize    → Thêm màu chiến lược
/impeccable animate     → Motion có mục đích
/impeccable delight     → Micro-moments personality
/impeccable layout      → Fix spacing/visual rhythm

# Simplify
/impeccable distill     → Strip to essence
/impeccable clarify     → Fix UX copy

# Harden
/impeccable polish      → Final pass (good → great)
/impeccable harden      → Edge cases, i18n, error states
/impeccable optimize    → LCP đến bundle size

# System
/impeccable document    → Export DESIGN.md
/impeccable extract     → Pull tokens vào design system
/impeccable live        → Live Mode browser iteration
```

**Slop rules nổi bật mà Impeccable bắt:**
- Typography: Inter/Geist overuse, hero eyebrow pill, italic serif display headline, flat type hierarchy
- Color: Purple-to-blue gradient, gradient text, cream/beige palette reflex, dark mode glow accent
- Layout: Nested cards, icon tile trên heading, 01/02/03 section numbering, identical card grids
- Motion: Bounce/elastic easing, image scale on hover
- Copy: Em-dash overuse, buzzwords (supercharge/empower), aphoristic cadence

**Install:**
```bash
# Claude Code (recommended)
/plugin marketplace add pbakaus/impeccable

# Hoặc universal
npx impeccable skills install

# Update
npx impeccable skills update
```

**Workflow cho Better Work LMS:**
```
# Landing page mới
shape → craft → critique → polish

# Cải thiện UI đã có
audit → bolder/quieter → polish

# Trước khi ship
audit landing → harden checkout → optimize
```

---

#### B. `Leonxlnx/taste-skill` ⭐ (Recommended — dùng cho generate phase)

**Tác giả:** Leon (Leonxlnx)  
**GitHub:** https://github.com/Leonxlnx/taste-skill  
**Website:** https://www.tasteskill.dev  
**Stars:** ~4.6k (growing fast, tạo Feb 2026)  
**License:** Free, MIT

**Đặc điểm nổi bật:**
- Thiên về **generate** với aesthetic nhất định ngay từ đầu (vs Impeccable thiên về audit/iterate)
- **3 tunable dials** (điều chỉnh trong prompt):
  - `DESIGN_VARIANCE`: 1=Perfect Symmetry → 10=Artsy Chaos (default: 8)
  - `MOTION_INTENSITY`: 1=Static → 10=Cinematic/Magic Physics (default: 6)
  - `VISUAL_DENSITY`: 1=Art Gallery → 10=Pilot Cockpit (default: 4)
- Framework-agnostic: React, Next.js, Vue, Svelte, plain HTML
- Anti-slop rules: ban Inter cho creative vibes, ban purple/blue AI palette, ban centered hero khi VARIANCE > 4
- Default stack: React/Next.js + Server Components + Tailwind CSS

**Variants quan trọng:**

| Variant | Install name | Dùng cho |
|---|---|---|
| Flagship v2 | `design-taste-frontend` | Landing/marketing page — đọc brief, tự suy luận direction |
| Minimalist | `minimalist-skill` | **LMS Dashboard** — "analogous to top-tier workspace platforms" (Notion, Linear) |
| Soft | `soft-skill` | App UI nếu target học viên phổ thông — calmer, friendlier |
| Redesign | `redesign-skill` | Audit + upgrade UI đã có (6-category scan) |
| Output | `output-skill` | Force complete non-lazy code |
| Brutalist | `brutalist-skill` | Không phù hợp LMS |

**Install:**
```bash
# Landing page
npx skills add https://github.com/Leonxlnx/taste-skill --skill "design-taste-frontend"

# LMS Dashboard
npx skills add https://github.com/Leonxlnx/taste-skill --skill "minimalist-skill"

# Nếu target học viên phổ thông
npx skills add https://github.com/Leonxlnx/taste-skill --skill "soft-skill"
```

**Minimalist-skill description (quan trọng với LMS):**
> "Premium Utilitarian Minimalism & Editorial UI — An advanced frontend engineering directive for generating highly refined, ultra-minimalist, document-style web interfaces analogous to top-tier workspace platforms. Enforces high-contrast warm monochrome palette, bespoke typographic hierarchies, meticulous structural macro-whitespace, bento-grid layouts, and ultra-flat component architecture with deliberate muted pastel accents. Actively rejects standard generic SaaS design trends."

**Bento 2.0 (dùng cho feature sections trên landing page):**
```
Aesthetic: High-end, minimal, functional
Palette: Background #f9fafb, cards white #ffffff, border slate-200/50
Surfaces: rounded-[2.5rem] cho major containers
Shadow: "diffusion shadow" — light wide-spreading, tạo depth không clutter
Typography: Geist / Satoshi / Cabinet Grotesk, tracking-tight headers
```

---

### 2.3 Skills khác đã research (không đi sâu)

| Skill | Repo | Điểm nổi bật | Phù hợp LMS? |
|---|---|---|---|
| Anthropic frontend-design | `anthropics/skills` | Official baseline, bắt buộc commit aesthetic trước khi code | ✓ Dùng làm floor |
| Hallmark | `Nutlope/hallmark` | 21 macrostructures × 22 themes, 65-gate slop test | ✓ Nếu cần nhiều landing pages khác nhau |
| 2389-research/landing-page | `2389-research/landing-page-design` | Chỉ cho landing page, "Vibe Discovery" 4 câu hỏi | Tốt cho landing riêng |
| UI/UX Pro Max | `nextlevelbuilder/ui-ux-pro-max-skill` | 50+ styles, 161 palettes, 57 font pairings | ✓ Cho on-brand systematic |
| huashu-design | `alchaincyf/huashu-design` | 20 design philosophies, 5 schools | Non-commercial only |

---

## 3. RECOMMENDED STACK CHO BETTER WORK LMS

### 3.1 Theo surface

**Marketing/Landing page:**
```
Primary skill:    design-taste-frontend (Taste Skill v2)
Dials:            VARIANCE=7, MOTION=5, DENSITY=3
Review after:     /impeccable critique
Final pass:       /impeccable polish
```

**LMS App / Dashboard:**
```
Primary skill:    minimalist-skill (Taste Skill)
Aesthetic ref:    Notion / Linear / Craft.do
Review after:     /impeccable audit
Final pass:       /impeccable polish
```

### 3.2 Full workflow

```
1. SETUP (1 lần)
   → /impeccable init          # Tạo PRODUCT.md với context Better Work
   → Điền: users, register, brand voice, anti-references

2. LANDING PAGE
   → Activate: design-taste-frontend (VARIANCE=7, MOTION=5, DENSITY=3)
   → /impeccable shape          # Discovery brief
   → /impeccable craft landing  # Build
   → /impeccable critique       # Review
   → /impeccable polish         # Final pass

3. LMS DASHBOARD
   → Activate: minimalist-skill
   → Build course list, player, progress tracker
   → /impeccable audit          # Technical check
   → /impeccable harden         # Edge cases, i18n, error states
   → /impeccable optimize       # Performance

4. TRƯỚC KHI SHIP
   → npx impeccable detect src/ # Slop check trong CI
   → Chrome extension overlay   # Visual check staging
```

### 3.3 PRODUCT.md template cho Better Work

```markdown
# Better Work — Product Context

## Product
Better Work là LMS platform giúp doanh nghiệp và cá nhân phát triển kỹ năng nghề nghiệp.

## Users
- Học viên: nhân viên doanh nghiệp cần upskilling, individual learners
- Admin: L&D manager, HR, team lead
- Instructor: người tạo khóa học

## Register
- Landing page: BRAND (design IS the product)
- Dashboard/App: PRODUCT (design serves the learning experience)

## Brand voice
Calm, professional, empowering. Không "hype". Cụ thể, không buzzwords.

## Anti-references (những thứ KHÔNG được giống)
- Udemy (quá cluttered, cheap-feeling)
- Coursera (cũ, corporate boring)
- Generic SaaS purple gradient
- Card-heavy bloated layouts

## Reference direction (muốn gần giống)
- Notion (workspace feel — minimalist, focused)
- Linear (product UI quality)
- Craft.do (editorial, typography-first)

## Design constraints
- Typography-first: content là hero, UI là background
- Max 1 accent color
- Light mode chính, dark mode optional
- Mobile-first cho learner app
```

---

## 4. CÁC VẤN ĐỀ CÒN MỞ — Claude Code cần phân tích tiếp

### 4.1 Stack / Framework
- [ ] Better Work sẽ dùng framework gì? (Next.js App Router, Remix, Nuxt, SvelteKit?)
- [ ] State management cho progress tracking? (Zustand, Jotai, TanStack Query?)
- [ ] Component library base? (shadcn/ui, Radix, headless UI?)
- [ ] CSS approach? (Tailwind v4, CSS Modules, vanilla CSS?)

### 4.2 Design system
- [ ] Cần tạo DESIGN.md với brand tokens (màu, font, spacing, radius)
- [ ] Font chọn gì? (Minimalist-skill recommend warm monochrome — có thể: Geist + serif display cho marketing)
- [ ] Accent color? (Không purple — có thể: deep teal, emerald, slate blue)
- [ ] Spacing system? (4px / 8px base)

### 4.3 LMS-specific components cần design
Landing page:
- [ ] Hero section (không centered, không purple gradient)
- [ ] Feature bento grid
- [ ] Pricing table
- [ ] Social proof / testimonials
- [ ] CTA sections

Dashboard app:
- [ ] Sidebar navigation
- [ ] Course card (progress state, thumbnail, meta)
- [ ] Course player (video + transcript + notes)
- [ ] Progress tracker (visual hierarchy)
- [ ] Quiz/assessment UI
- [ ] Certificate display
- [ ] Notification center

### 4.4 Design decisions chưa chốt
- [ ] Dark mode có không? (Minimalist-skill support dual-mode)
- [ ] Motion level? (Taste Skill MOTION dial — 3–5 cho edu platform)
- [ ] Mobile app riêng hay responsive web?
- [ ] Brand name typography có custom typeface không?

---

## 5. INSTALL COMMANDS ĐẦY ĐỦ

```bash
# Cài Impeccable (recommended cho cả project)
/plugin marketplace add pbakaus/impeccable
# Hoặc:
npx impeccable skills install

# Cài Taste Skill variants
npx skills add https://github.com/Leonxlnx/taste-skill --skill "design-taste-frontend"
npx skills add https://github.com/Leonxlnx/taste-skill --skill "minimalist-skill"
npx skills add https://github.com/Leonxlnx/taste-skill --skill "soft-skill"
npx skills add https://github.com/Leonxlnx/taste-skill --skill "redesign-skill"

# Kiểm tra phiên bản Impeccable
npx impeccable skills check

# Slop detection trong CI
npx impeccable detect src/
```

---

## 6. NGUỒN THAM KHẢO

| Resource | URL |
|---|---|
| Impeccable website | https://impeccable.style |
| Impeccable docs | https://impeccable.style/docs |
| Impeccable slop catalog | https://impeccable.style/slop |
| Taste Skill GitHub | https://github.com/Leonxlnx/taste-skill |
| Taste Skill website | https://www.tasteskill.dev |
| Anthropic frontend-design | https://github.com/anthropics/skills/tree/main/skills/frontend-design |
| Hallmark | https://github.com/nutlope/hallmark |
| VoltAgent DESIGN.md library | https://github.com/VoltAgent/awesome-claude-design |
| Brand-design-md (62 brands) | https://github.com/zephyrwang6/brand-design-md |

---

## 7. CONTEXT CHO CLAUDE CODE

Khi đưa file này vào Claude Code, có thể bắt đầu với các prompt sau:

**Để setup project:**
```
Đây là context của dự án Better Work LMS. Đọc file HANDOVER này.
Bắt đầu bằng việc chạy /impeccable init để tạo PRODUCT.md cho project,
sau đó propose design system (font, color, spacing) phù hợp với LMS platform.
```

**Để build landing page:**
```
Dùng design-taste-frontend skill với dials VARIANCE=7, MOTION=5, DENSITY=3.
Build hero section cho Better Work LMS — không centered hero, không purple gradient.
Reference: Notion/Linear quality nhưng warmer cho education context.
```

**Để build dashboard:**
```
Dùng minimalist-skill. Build LMS dashboard layout với:
- Sidebar navigation (courses, progress, certificates, settings)
- Course list view (card với progress indicator)
- Typography-first approach — content là hero
Aesthetic gần với Notion workspace, không generic SaaS.
```

**Để audit sau khi build:**
```
Chạy /impeccable critique trên toàn bộ landing page.
Sau đó chạy /impeccable audit trên dashboard.
List tất cả issues theo priority P0→P3.
```

---

*File được tạo tự động từ research session trên Claude.ai*  
*Ngày tạo: June 2026*
