---
title: "Vela design system: locked register and palette (one spine, two registers)"
date: 2026-06-09
category: conventions
module: web
problem_type: convention
component: rails_view
severity: high
applies_when:
  - "Before any Vela web UI work on /cua-ban, /bao-cao, /thanh-vien, or /xuat-ban"
  - "Choosing or adding color tokens, brand color, or chrome in globals.css"
  - "Deciding learner (warm/motivating) vs admin (calm/data-forward) treatment for a surface"
  - "Setting font weight or type hierarchy in shared primitives"
  - "Tempted to reintroduce indigo or navy, or to add decorative gradient washes on admin"
related_components:
  - documentation
tags:
  - design-system
  - vela
  - oklch-tokens
  - color-palette
  - tailwind-v4
  - two-registers
  - typography
---

# Vela design system: locked register and palette (one spine, two registers)

## Context

The Vela web UI direction was the project's single hardest blocker. The design system "tổng thể" churned repeatedly across handoff docs without converging — it had failed to settle multiple times. Two problems compounded each other:

1. **Two competing blue hues with no clear winner.** The brand carried both an indigo `--brand-primary` (`oklch(0.54 0.22 263)`) and a navy `--ink-rail` (`oklch(0.16 0.052 258)`). Indigo at hue 263 also doubles as a known AI-slop / anti-reference hue, so it weakened rather than strengthened the brand.
2. **An identity question underneath the color question:** should the learner experience and the admin/L&D experience be *one product* or *two separate products*? Without an answer, every screen re-litigated its own look, and prose debate kept stalling.

The decision had to be settled durably so that all subsequent UI work (`/cua-ban`, `/bao-cao`, `/thanh-vien`, `/xuat-ban`) could proceed without re-opening the palette and register debate each time. It was resolved on **2026-06-08** via a structured `/design-poc` bake-off rather than more written argument, and locked into `docs/vela-design-system-plan.md`.

## Guidance

Durable rules. Before any Vela web UI work, read the "Locked Register & Palette (2026-06-08)" section of `docs/vela-design-system-plan.md` — it is the source of truth. Apply these:

**1. Orange is the single dominant brand color. The two blues are retired — do not reintroduce them.**

| Token | Was (blue) | Now |
|---|---|---|
| `--brand-primary` | `oklch(0.54 0.22 263)` indigo (also a slop/anti-reference hue) | `oklch(0.56 0.2 34)` deep orange, WCAG-AA for text and white-on-fill |
| `--ink-rail` | `oklch(0.16 0.052 258)` navy | `oklch(0.2 0.018 52)` warm charcoal |

Teal is demoted to a minor accent — it is **not** the publish/primary color. (Exact OKLCH for teal/gold/success/warning/danger: see `web/app/globals.css`.)

**2. One spine, two registers — one design system dialed by surface temperature.** Not one flat look, not two products.

- **Warm / learner surfaces** — `/cua-ban` and the `/xuat-ban` course canvas. Brighter, more motivating: coral `oklch(0.68 0.18 31)`, a wider warm range, and progress / rank / runway motifs. Goal: visual appeal and momentum.
- **Calm / admin (L&D) surfaces** — `/bao-cao`, `/thanh-vien`, the `/xuat-ban` ops side, and `/design-poc`. Neutral surfaces, deep-orange accent, image-free and CSS-only. Goal: numbers, tracking, fast scan, trust.

**3. Color is semantic, not decorative — strictly so on admin.** On admin surfaces: orange = brand + on-track; amber (`--warning`) = warning; red (`--danger`) = at-risk; everything else is neutral ink/surface. No large warm gradient washes or decorative canvas on admin — the eye must jump to the amber/red rows that need attention. Learner surfaces may use warm fields and a wider warm range, but stay anchored on the same orange.

**4. Type & spacing discipline — hierarchy from size, weight, color, and space, not weight alone.** Retire the `font-black`-everywhere pattern in primitives. Weight ladder:

- `900` (black): hero metrics and the page H1 **only**.
- `700` (bold): section titles.
- `600` (semibold): eyebrows/labels (uppercase, muted, small) and list-item titles.
- `500` (medium): body and secondary text.

Be Vietnam Pro is the UI family; JetBrains Mono (tabular figures) is reserved for metrics, IDs, dates, and aligned numbers. Letter spacing stays at `0`. Titles get room to wrap to two lines (`leading-snug`) so long Vietnamese strings never collide with adjacent chips (the "tràn text" fix). Panel padding and section gaps use `var(--space-5)`.

**5. This decision is LOCKED.** Do not reopen the register or color debate. Build on it.

## Why This Matters

**A structured visual bake-off converged what prose debate could not.** The decision came from `/design-poc` comparing four real renders — V0 (current) vs **V0r** (admin reference, the chosen render) vs V1 (minimalist-ui) vs V2 (cool-operational) — recorded in `web/app/design-poc/_bakeoff/NOTES.md`. Written back-and-forth never resolved; seeing the variants side by side did. That is *why the convergence is worth protecting*: it was expensive to reach and cheap to throw away.

**"One spine, two registers" beats both alternatives.** One flat look would force the motivating learner experience and the dense L&D console into the same visual register — either the admin reads as a toy or the learner reads as a spreadsheet. Two separate products would double the primitives, the maintenance, and the brand surface, and re-introduce exactly the "are these the same thing?" ambiguity that blocked the project. A single token set dialed by surface temperature gives each audience the right feel while keeping one brand, one component library, one source of truth.

**Semantic-only color on admin aids scan and trust.** When color carries meaning (orange on-track / amber warning / red at-risk) and nothing else, the operator's eye goes straight to the rows that need attention. Decorative gradient washes on a data console compete with that signal and erode trust in the numbers.

**The cost of re-deriving or reverting is real.** Re-litigating the register wastes the hard-won convergence and re-opens the project's worst blocker. Reintroducing indigo/navy weakens the brand (indigo 263 is a slop hue) and reverts the WCAG-AA contrast guarantees that orange `oklch(0.56 0.2 34)` was specifically chosen to satisfy.

## When to Apply

- **Before any Vela web UI work** — read the locked section first.
- **When building or altering learner vs admin surfaces** — confirm which register the route belongs to (`/cua-ban` and the `/xuat-ban` canvas = warm; `/bao-cao`, `/thanh-vien`, `/xuat-ban` ops, `/design-poc` = calm) and match its temperature.
- **When tempted to add a new accent color** — don't. Anchor on orange; use amber/red only for warning/at-risk meaning; teal stays minor.
- **When reaching for `font-black`** — stop and check the weight ladder; black is for hero metrics and the H1 only.
- **When onboarding someone new to the UI** — point them at the locked section and this doc so they inherit the decision instead of re-deriving it.

## Examples

**Token old → new (the retirement of blue):**

```css
/* before — two blues */
--brand-primary: oklch(0.54 0.22 263); /* indigo (slop hue) */
--ink-rail:      oklch(0.16 0.052 258); /* navy */

/* after — one orange brand + warm charcoal chrome */
--brand-primary: oklch(0.56 0.2 34);   /* deep orange, WCAG-AA */
--ink-rail:      oklch(0.2 0.018 52);   /* warm charcoal */
```

**Learner vs admin register contrast (same spine, different temperature):**

- *Learner* (`/cua-ban`): warm hero field, coral `oklch(0.68 0.18 31)`, rank gauge, runway/checkpoint motifs, raster atmosphere allowed on the hero (`hero-runway-bg`, `runway-path` — atmosphere only, no baked text/numbers/UI; all data stays code-rendered). Composition matches mock A.
- *Admin* (`/bao-cao`): neutral surfaces, deep-orange accent only, **image-free and CSS-only**, three-column operations map; color appears only as orange/amber/red status. This is the densest layout and the system benchmark.

**Weight-ladder before/after:**

```jsx
// before — font-black as the default; hierarchy collapses, everything shouts
<h1 className="font-black">Báo cáo</h1>
<h2 className="font-black">Bản đồ vận hành</h2>
<span className="font-black">Đúng tiến độ</span>

// after — hierarchy from the ladder (900 -> 700 -> 600 -> 500)
<h1 className="font-black">Báo cáo</h1>           {/* 900: page H1 only */}
<h2 className="font-bold">Bản đồ vận hành</h2>     {/* 700: section title */}
<span className="font-semibold uppercase ...">Đúng tiến độ</span> {/* 600: label */}
<p className="font-medium">…</p>                  {/* 500: body */}
```

## Related

- `docs/vela-design-system-plan.md` — the source-of-truth contract; "Locked Register & Palette (2026-06-08)" is the canonical statement of this decision.
- `docs/superpowers/plans/2026-06-08-vela-orange-register-rollout.md` — the roll-out plan (token changes in `globals.css` → primitive weight relaxation → per-register re-skin → bake-off cleanup).
- `docs/asset-handover-cua-ban-learner.md` — the learner-surface raster-asset workflow (hero atmosphere, recolored to the locked tokens).
- `web/app/globals.css` — where the OKLCH tokens actually live; the code source of truth for the palette.
- `web/app/design-poc/_bakeoff/NOTES.md` — the bake-off notes (V0 / V0r / V1 / V2); V0r is the admin reference render.
- `docs/design-system/00-brand.md`, `docs/design-system/01-foundations.md` — the pre-lock brand/foundations docs. **Note:** the locked contract in `vela-design-system-plan.md` *supersedes* `01-foundations.md` on color (it still describes the retired blues); treat the locked doc as current for palette.
