# Vela Design System Contract

This document is the current source of truth for the Vela web UI direction. It supersedes the earlier asset-first experiment.

## North-Star Inputs

- `D:\lms\docs\design-handoff-for-new-session.md`
- `D:\lms\web\public\design-mocks\learner-runway-a.png`
- `D:\lms\web\public\design-mocks\learning-operations-map-b.png`
- `D:\lms\web\public\design-mocks\learning-studio-publish-c.png`

The approved mock language remains valid, but final implementation favors stable product layout and reusable primitives over fragile decorative SVG imports.

## Locked Register & Palette (2026-06-08)

Decided via the `/design-poc` bake-off (V0 current vs V0r admin vs V1 minimalist-ui vs V2 cool-operational; see `web/app/design-poc/_bakeoff/NOTES.md`). The reference render is variant **V0r**.

**One spine, two temperatures.** A single design system, dialed by surface:

- **Learner surfaces** (`/cua-ban`, the `/xuat-ban` course canvas): warm and motivating. Brighter orange (coral `oklch(0.68 0.18 31)`), more color, progress/rank/runway motifs. Goal: visual appeal and momentum.
- **Admin / L&D surfaces** (`/bao-cao`, `/thanh-vien`, `/xuat-ban` ops, `/design-poc`): data-forward and calm. Neutral surfaces, deep-orange accent, color used only for meaning. Goal: numbers and tracking, fast scan, trust.

**Orange is the single dominant brand color. The two pre-existing blues are retired:**

| Token | Was (blue) | Now |
|---|---|---|
| `--brand-primary` | `oklch(0.54 0.22 263)` indigo (also a slop/anti-reference hue) | `oklch(0.56 0.2 34)` deep orange, WCAG-AA for text and white-on-fill |
| `--ink-rail` | `oklch(0.16 0.052 258)` navy | `oklch(0.2 0.018 52)` warm charcoal |

**Color is semantic, not decorative.** On admin: orange = brand + on-track; amber (`--warning`) = warning; red (`--danger`) = at-risk; everything else is neutral ink/surface. No large warm gradient washes or decorative canvas on admin — the eye should jump to amber/red rows that need attention. Learner may use warm fields and a wider warm range, still anchored on the same orange.

**Type & spacing discipline** (the lesson from V1/minimalist-ui): hierarchy comes from **size, weight, color, and space — not weight alone**. Retire the `font-black`-everywhere pattern in the primitives. Weight ladder:

- `900` (black): reserved for hero metrics and the page H1 only.
- `700` (bold): section titles.
- `600` (semibold): eyebrows/labels (uppercase, muted, small) and list-item titles.
- `500` (medium): body and secondary text.

Spacing: panel padding `var(--space-5)`, section gaps `var(--space-5)`, and titles given room to wrap to two lines (`leading-snug`) so long Vietnamese strings never collide with adjacent chips (the "tràn text" fix). Numbers use JetBrains Mono with tabular figures.

## Design Principles

- Design serves enterprise LMS workflows: dense, readable, permission-aware, and action-oriented.
- B is the system north star: operations map, visible scope, branch topology, risk lanes, and publish gates.
- A informs learner motivation: warm learning field, visible next action, rank progress, and queue.
- C informs publish workflows: course canvas, readiness checks, source chips, and publish runway.

## Asset Policy

### Production Assets

- Brand mark and wordmark remain SVG assets in `web/public/vela-assets/brand`.
- Icons remain SVG mask assets in `web/public/vela-assets/icons`, consumed through `AssetIcon`.

### Code-Rendered Motifs

Decorative and structural motifs are now rendered by CSS and components, not SVG backgrounds:

- Learner orbit and checkpoint field: `.learner-hero-field`
- Operations topology canvas: `.ops-canvas`
- Publish/course thumbnail: `.course-thumb`
- Readiness ring: `.readiness-ring`
- Source document stack: `.source-stack-mark`
- Rank badge: `RankEmblem`

This avoids broken SVG rendering, keeps layout responsive, and makes these motifs themeable with design tokens.

### Learner Hero Imagery (revision 2026-06-08)

The CSS-only rule above is relaxed for **the learner hero on `/cua-ban` only**, to pixel-match approved mock A. The learner hero may use generated raster atmosphere:

- `hero-runway-bg` (warm light field) and `runway-path` (glowing checkpoint path) live in `web/public/vela-assets/learner/` and are referenced by `app/cua-ban/page.tsx`.
- They carry **atmosphere only** — no baked text, icons, numbers, or UI. Checkpoint badges/labels, the rank gauge, and all data stay code-rendered and themeable.
- Generation is handed off to Codex + ChatGPT image gen via `docs/asset-handover-cua-ban-learner.md`, recolored to the locked tokens (orange/saffron/coral/gold). Admin surfaces remain CSS-only and image-free.

## Tokens

Tokens live in `web/app/globals.css` and are mapped to Tailwind v4 via `@theme inline`.

- Palette: OKLCH semantic colors for rail, surfaces, ink, primary, teal, gold, success, warning, danger. Per "Locked Register & Palette": `--brand-primary` is orange `oklch(0.56 0.2 34)` and `--ink-rail` is warm charcoal `oklch(0.2 0.018 52)` — no indigo/navy. Teal is demoted to a minor accent (it is not the publish/primary color).
- Spacing: 4px scale exposed as `--space-1` through `--space-12`.
- Shape: `--radius-ui` and `--radius-panel` are 8px. Pills are reserved for chips and status labels.
- Elevation: one panel shadow, one tight shadow, both restrained.
- Motion: 180ms to 420ms state/progress transitions with reduced-motion fallback.

## Typography

- Be Vietnam Pro is the UI family.
- JetBrains Mono is reserved for metrics, IDs, dates, and aligned numbers.
- Product UI uses fixed rem sizes, not viewport-scaled type.
- Letter spacing stays at `0`; hierarchy comes from size, weight, color, and space.
- Weight ladder (see "Locked Register & Palette"): `900` only for hero metrics and page H1; `700` section titles; `600` eyebrows/labels and list titles; `500` body. Do not set `font-black` as the default weight in primitives.

## Layout Primitives

Core classes:

- `.workspace-page`: page background and min-height after shell header
- `.workspace-pad`: route-level padding using spacing tokens
- `.workspace-stack`: vertical route rhythm
- `.workspace-two`: main + side rail layout
- `.workspace-three`: queue + operations map + risk/publish side rail
- `.panel` and `.panel-raised`: standard section containers

Core components:

- `VelaAppShell`: mobile rail, desktop rail, organization lens, route chrome, and an optional desktop `topbar` slot.
- `ShellSearch` and `ShellIconButton`: shared topbar primitives. Target routes own their topbar composition so A, B, and C keep the mock-specific command area without duplicating shell logic.
- `PageHeader`: title, description, and page actions for support routes that do not need a custom topbar.
- `SectionFrame`: section header with optional eyebrow and action slot.
- `MetricCard`, `StatusPill`, `ProgressBar`, `IconBadge`, `ActionButton`, `DataTable`.
- `RunwaySteps` and `RankEmblem`.

## Route Layout Contract

- `/cua-ban`: learner-focused two-column workspace. Main column is hero, metrics, learning queue. Side column is rank, leaderboard, risk.
- `/bao-cao`: operations-focused three-column workspace. Queue left, organization map center, risk/publish right. This is the densest layout and the system benchmark. The page title, search, role lens, and report period live in the shell topbar, not inside the scroll body.
- `/xuat-ban`: publish-focused split workspace. Course canvas left, publish runway right, then study/content/rank tables below. The date/search/support controls live in the shell topbar to preserve canvas height.
- `/thanh-vien` and `/design-poc`: support routes use the same shell, panels, metrics, and table primitives.

## Accepted Deviations From Mocks

- Decorative SVG mock motifs are replaced by CSS primitives for stability.
- Exact mock spacing is adjusted to prevent clipping at 1440px and 390px.
- The operations map is denser and less ornamental than mock B so branch/scope data remains readable.
- The publish thumbnail is an abstract code-rendered course visual rather than an imported image asset.
