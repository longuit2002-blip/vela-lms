# Vela Orange-Register Roll-out Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Roll the locked design-system decision (orange as the single dominant brand color, warm-charcoal chrome, semantic-only color, type/spacing discipline, one spine / two registers) from the `/design-poc` bake-off into the real Vela web app.

**Architecture:** Change foundation tokens once in `globals.css` (so every screen inherits orange primary + warm-charcoal rail), then relax the `font-black`-everywhere weights in the shared primitives (`ui.tsx`), then re-skin each screen by register — admin screens become data-forward (port the proven `variant-v0r` composition), learner screens stay warm but inherit the discipline. Finally delete the throwaway bake-off and verify every route by screenshot.

**Tech Stack:** Next.js 16 (App Router, Turbopack), React 19, Tailwind v4 (`@theme inline`), OKLCH tokens, Be Vietnam Pro + JetBrains Mono via `next/font`. Visual verification via Python Playwright (already installed) at 1440px and 390px.

**Source of truth:** `docs/vela-design-system-plan.md` → "Locked Register & Palette (2026-06-08)". Reference render: `web/app/design-poc/_bakeoff/variant-v0r.tsx` and `web/.qa-screenshots/bakeoff-V0R-*.png`.

**Verification convention (used by every task):**
- `cd web && npm run build` must succeed (compile + type check).
- Dev server for screenshots: `cd web && npm run dev -- --hostname 127.0.0.1 --port 3100`.
- Screenshot helper used throughout — save once as `web/.qa-screenshots/shot.py`:

```python
# web/.qa-screenshots/shot.py — usage: python shot.py <route> <name>
import sys, time, os
from playwright.sync_api import sync_playwright
route, name = sys.argv[1], sys.argv[2]
OUT = os.path.dirname(os.path.abspath(__file__))
with sync_playwright() as p:
    b = p.chromium.launch()
    for w, tag in [(1440, "desktop"), (390, "mobile")]:
        pg = b.new_page(viewport={"width": w, "height": 900}, device_scale_factor=2)
        pg.goto(f"http://127.0.0.1:3100{route}", wait_until="networkidle", timeout=90000)
        time.sleep(1.5)
        pg.screenshot(path=os.path.join(OUT, f"{name}-{tag}.png"), full_page=True)
    b.close()
print("shot", name)
```

- After each screenshot, **read the PNG and check against the locked criteria**: orange is the dominant chromatic color; the only amber/red is on warning/at-risk data; no indigo/navy anywhere (except the throwaway switcher until Task 7); titles wrap without colliding with chips; numbers are the visual heroes.

---

## File Structure

- `web/app/globals.css` — token values (palette, rail, focus ring) + `.shell-rail` gradient. **Foundation; touched once in Task 1.**
- `web/components/vela/ui.tsx` — shared primitives' type weights/spacing. **Touched once in Task 2.**
- `web/app/bao-cao/page.tsx` — admin register (port from `variant-v0r`). Task 3.
- `web/app/thanh-vien/page.tsx` — admin register. Task 4.
- `web/app/xuat-ban/page.tsx` — admin register for ops tables; learner-warm for the course canvas. Task 5.
- `web/app/cua-ban/page.tsx` — learner register; inherits tokens, gets discipline pass. Task 6.
- `web/app/design-poc/` — delete `_bakeoff/`, restore the route-map landing. Task 7.
- `web/.qa-screenshots/` — verification artifacts.

---

## Task 1: Recolor foundation tokens (orange primary + warm-charcoal chrome)

**Files:**
- Modify: `web/app/globals.css:3-46` (`:root` token block), `:30-34` (focus ring/shadow), `:170-174` (`.shell-rail`)

- [ ] **Step 1: Repoint the brand-primary and ink-rail tokens to the locked values**

In `web/app/globals.css`, inside `:root`, replace these four lines:

```css
  --ink-rail: oklch(0.16 0.052 258);
  --ink-rail-soft: oklch(0.22 0.058 257);
  --brand-primary: oklch(0.54 0.22 263);
  --brand-primary-hover: oklch(0.47 0.21 265);
  --brand-primary-subtle: oklch(0.92 0.048 263);
```

with:

```css
  --ink-rail: oklch(0.2 0.018 52);
  --ink-rail-soft: oklch(0.26 0.02 50);
  --brand-primary: oklch(0.56 0.2 34);
  --brand-primary-hover: oklch(0.5 0.19 33);
  --brand-primary-subtle: oklch(0.93 0.055 52);
```

- [ ] **Step 2: Repoint the focus ring off the indigo hue**

Replace:

```css
  --focus-ring: oklch(0.62 0.2 263 / 0.34);
```

with:

```css
  --focus-ring: oklch(0.62 0.2 34 / 0.34);
```

- [ ] **Step 3: Recolor the shell rail gradient (literal navy → warm charcoal)**

Replace the `.shell-rail` rule:

```css
.shell-rail {
  background:
    linear-gradient(180deg, oklch(0.16 0.052 258), oklch(0.13 0.044 260)),
    var(--ink-rail);
}
```

with:

```css
.shell-rail {
  background:
    linear-gradient(180deg, oklch(0.2 0.02 52), oklch(0.15 0.016 46)),
    var(--ink-rail);
}
```

- [ ] **Step 4: Build**

Run: `cd web && npm run build`
Expected: build succeeds, no type errors.

- [ ] **Step 5: Screenshot a learner and an admin screen to confirm tokens propagated**

Run: `cd web && npm run dev -- --hostname 127.0.0.1 --port 3100` (background), then
`python web/.qa-screenshots/shot.py /cua-ban t1-cuaban` and `python web/.qa-screenshots/shot.py /bao-cao t1-baocao`.
Expected on reading both PNGs: rail is warm charcoal (not navy); any active-nav / primary button / primary progress is orange (not indigo). Learner warmth still reads.

- [ ] **Step 6: Commit**

```bash
git add web/app/globals.css web/.qa-screenshots/shot.py
git commit -m "feat(design): orange primary + warm-charcoal chrome tokens"
```

---

## Task 2: Relax type weights + spacing in shared primitives

**Files:**
- Modify: `web/components/vela/ui.tsx` (StatusPill, SectionFrame, MetricCard, PageHeader, ActionButton, DataTable, RunwaySteps)

Rationale: hierarchy must come from size/weight/color/space, not `font-black` everywhere. `900` stays only on hero numbers (MetricCard value) and page H1.

- [ ] **Step 1: StatusPill — drop from extrabold to semibold**

Replace `text-xs font-extrabold` in `StatusPill` with `text-xs font-semibold`:

```tsx
  return <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ${bgTone[tone]} ${className}`}>{children}</span>;
```

- [ ] **Step 2: SectionFrame — calmer header, more padding**

Replace the header block in `SectionFrame`:

```tsx
      <div className="grid gap-3 border-b border-border bg-surface-raised px-4 py-3 md:grid-cols-[1fr_auto] md:items-center">
        <div>
          {eyebrow ? <p className="text-xs font-extrabold text-learning-coral">{eyebrow}</p> : null}
          <h2 className="text-lg font-black leading-tight text-foreground">{title}</h2>
          {description ? <p className="mt-1 text-sm font-semibold text-muted">{description}</p> : null}
        </div>
        {action}
      </div>
```

with:

```tsx
      <div className="grid gap-3 border-b border-border bg-surface-raised px-5 py-4 md:grid-cols-[1fr_auto] md:items-center">
        <div>
          {eyebrow ? <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{eyebrow}</p> : null}
          <h2 className="text-base font-bold leading-tight text-foreground">{title}</h2>
          {description ? <p className="mt-1 text-xs font-medium text-muted">{description}</p> : null}
        </div>
        {action}
      </div>
```

- [ ] **Step 3: MetricCard — label as quiet eyebrow, detail medium, more padding (keep the number heavy)**

Replace the `MetricCard` body:

```tsx
    <article className="panel min-h-[90px] p-4">
      <p className="text-xs font-extrabold text-subtle">{label}</p>
      <p className={`mt-2 font-mono text-2xl font-black leading-none ${textTone[tone]}`}>{value}</p>
      <p className="mt-2 text-xs font-bold text-muted">{detail}</p>
    </article>
```

with:

```tsx
    <article className="panel min-h-[96px] p-5">
      <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{label}</p>
      <p className={`mt-2 font-mono text-2xl font-extrabold leading-none ${textTone[tone]}`}>{value}</p>
      <p className="mt-2 text-xs font-medium text-muted">{detail}</p>
    </article>
```

- [ ] **Step 4: PageHeader — keep H1 heavy, soften the description**

Replace `<p className="mt-1 max-w-[62ch] text-sm font-semibold text-muted">{description}</p>` with:

```tsx
        <p className="mt-1 max-w-[62ch] text-sm font-medium text-muted">{description}</p>
```

- [ ] **Step 5: ActionButton — bold, not black**

Replace `text-sm font-black transition-colors` with `text-sm font-semibold transition-colors` in `ActionButton`.

- [ ] **Step 6: DataTable — quiet header**

Replace `<thead className="text-xs font-extrabold text-subtle">` with:

```tsx
        <thead className="text-[11px] font-semibold uppercase tracking-wide text-subtle">
```

- [ ] **Step 7: RunwaySteps — label semibold**

Replace `<span className={`text-xs font-black ${index <= active ? textTone[step.tone] : "text-muted"}`}>{step.label}</span>` with:

```tsx
          <span className={`text-xs font-semibold ${index <= active ? textTone[step.tone] : "text-muted"}`}>{step.label}</span>
```

- [ ] **Step 8: Build + screenshot every route, confirm nothing regressed**

Run: `npm run build`; then `python web/.qa-screenshots/shot.py /cua-ban t2-cuaban`, `/bao-cao t2-baocao`, `/xuat-ban t2-xuatban`, `/thanh-vien t2-thanhvien`.
Expected on reading PNGs: clear type hierarchy (labels recede, numbers/titles lead); no layout breakage; weights no longer uniformly black.

- [ ] **Step 9: Commit**

```bash
git add web/components/vela/ui.tsx
git commit -m "refactor(design): type + spacing discipline in shared primitives"
```

---

## Task 3: Fold the V0r admin register into /bao-cao

**Files:**
- Modify: `web/app/bao-cao/page.tsx` (full rewrite to the admin register)
- Reference (do not import): `web/app/design-poc/_bakeoff/variant-v0r.tsx`

The proven admin composition already exists in `variant-v0r.tsx`. Port it, **dropping the prototype-only scaffolding** because the tokens are now global:

- Remove the `orangePrimary` style object, the `RAIL_OVERRIDE` `<style>`, and the `v0r-scope` wrapper `<div>` — these were per-subtree overrides; Task 1 made them global.
- Keep: `Topbar`, `FlowStrip` (neutral strip with the thin `border-t-2 border-t-primary` keyline), `Queue`, `OrgMap`, `RiskLanes`, `PublishGate`, `FooterMetrics`, the `sem()` helper, `Eyebrow`, `Panel`, `PanelHeader`.
- The page default export wraps the content in `VelaAppShell active="Báo cáo" lens="L&D Admin" topbar={<Topbar />}` and the same `grid gap-5 ... px-5 py-6 xl:px-7 xl:py-7` container with `FlowStrip`, then `grid xl:grid-cols-[300px_minmax(0,1fr)_312px]` of `Queue` / `OrgMap` / (`RiskLanes` + `PublishGate`), then `FooterMetrics`.

- [ ] **Step 1: Replace `web/app/bao-cao/page.tsx` with the ported admin composition**

Copy the body of `variant-v0r.tsx` into `bao-cao/page.tsx`, renaming `VariantV0r` to `export default function ReportsPage`, and delete the three scaffolding pieces named above so the return is:

```tsx
  return (
    <VelaAppShell active="Báo cáo" lens="L&D Admin" topbar={<Topbar />}>
      <div className="grid gap-5 bg-background px-5 py-6 xl:px-7 xl:py-7">
        <FlowStrip />
        <div className="grid gap-5 xl:grid-cols-[300px_minmax(0,1fr)_312px]">
          <Queue />
          <OrgMap />
          <aside className="grid content-start gap-5">
            <RiskLanes />
            <PublishGate />
          </aside>
        </div>
        <FooterMetrics />
      </div>
    </VelaAppShell>
  );
```

Keep the `import type { CSSProperties }` only if still used; since the override object is removed, delete that import. Imports needed: `ShellSearch, VelaAppShell` from app-shell; `AssetIcon` from assets; the six data exports from `@/lib/mock-lms`.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: succeeds; no unused-import errors (remove `CSSProperties` if flagged).

- [ ] **Step 3: Screenshot and compare to the V0r reference**

Run: `python web/.qa-screenshots/shot.py /bao-cao t3-baocao`.
Expected on reading `t3-baocao-desktop.png`: matches `bakeoff-V0R-full.png` — neutral surfaces, orange keyline + accents, amber on "Sản xuất", red on "Chất lượng", warm-charcoal org-total node, no warm gradient band, titles not clipped. Mobile (390) stacks cleanly.

- [ ] **Step 4: Commit**

```bash
git add web/app/bao-cao/page.tsx
git commit -m "feat(bao-cao): adopt admin register (orange accent on neutral, data-forward)"
```

---

## Task 4: Apply the admin register to /thanh-vien

**Files:**
- Modify: `web/app/thanh-vien/page.tsx`

- [ ] **Step 1: Read the current page**

Run: open `web/app/thanh-vien/page.tsx`. Identify every warm-decoration / heavy-color usage: `flow-banner`, `learning-field`, `bg-learning-coral|saffron|apricot` fills, and any `font-black` not on a hero number.

- [ ] **Step 2: Convert to admin register**

- Replace decorative warm fills with neutral surfaces (`panel`, `bg-surface`, `bg-surface-raised`).
- Use color only semantically: `text-primary`/`bg-primary` (orange) for brand + on-track, `warning`/`danger` for problems, neutral otherwise.
- Use the shared primitives (now disciplined) — `SectionFrame`, `MetricCard`, `DataTable`, `StatusPill` — for tables/metrics.
- Section padding `p-5`, gaps `gap-5`, list-row titles `text-sm font-semibold leading-snug`.

- [ ] **Step 3: Build + screenshot**

Run: `npm run build`; `python web/.qa-screenshots/shot.py /thanh-vien t4-thanhvien`.
Expected: reads as the same calm, orange-accented admin family as `/bao-cao`; no warm gradient washes; semantic-only color.

- [ ] **Step 4: Commit**

```bash
git add web/app/thanh-vien/page.tsx
git commit -m "feat(thanh-vien): admin register"
```

---

## Task 5: Split /xuat-ban — admin register for ops tables, warm for the course canvas

**Files:**
- Modify: `web/app/xuat-ban/page.tsx`

`/xuat-ban` is mixed: the course canvas is a learner-facing surface (keep warm), the publish runway + study/content/rank tables are admin ops (restrained).

- [ ] **Step 1: Read the current page** and label each section learner-warm vs admin-ops.

- [ ] **Step 2: Course canvas (learner-warm)** — keep `.course-thumb` and warm hero treatment; apply only the type/spacing discipline (drop stray `font-black`, give titles room). Primary CTA is now orange via tokens.

- [ ] **Step 3: Publish runway + tables (admin-ops)** — neutral surfaces; the runway/stepper uses `--color-primary` (orange) for done/active instead of teal; readiness ring uses orange (inline `conic-gradient(var(--color-primary) 0 N%, var(--color-surface-muted) N% 100%)`); study/content tables via `DataTable`; risk levels in `warning`/`danger`.

- [ ] **Step 4: Build + screenshot**

Run: `npm run build`; `python web/.qa-screenshots/shot.py /xuat-ban t5-xuatban`.
Expected: canvas reads warm/inviting; the publish/ops half reads calm and orange-accented; the two coexist without the page feeling like two designs (shared spine, type, spacing).

- [ ] **Step 5: Commit**

```bash
git add web/app/xuat-ban/page.tsx
git commit -m "feat(xuat-ban): warm canvas + admin-register publish/ops"
```

---

## Task 6: Learner discipline pass on /cua-ban

**Files:**
- Modify: `web/app/cua-ban/page.tsx`

Learner stays warm and colorful — only fix execution.

- [ ] **Step 1: Read the page.** Confirm it uses the warm hero (`learner-hero-field`), rank, leaderboard, queue.

- [ ] **Step 2: Apply discipline without removing warmth**

- Keep warm fields, coral, rank gold, motivational motifs.
- Remove uniform `font-black`: page H1 stays heavy; section titles `700`; labels `600` uppercase muted; body `500`.
- Give queue/list titles `leading-snug` and room to wrap (fix any "tràn text").
- Confirm primary CTAs/active states are orange (inherited) and look intentional against the warm field.

- [ ] **Step 3: Build + screenshot (desktop + mobile)**

Run: `npm run build`; `python web/.qa-screenshots/shot.py /cua-ban t6-cuaban`.
Expected: still warm and motivating (the learner pole), now with clean hierarchy and no overflow; visibly warmer/brighter than `/bao-cao` — proving the two-register split.

- [ ] **Step 4: Commit**

```bash
git add web/app/cua-ban/page.tsx
git commit -m "refactor(cua-ban): learner register discipline pass"
```

---

## Task 7: Remove the bake-off prototype, restore /design-poc landing

**Files:**
- Delete: `web/app/design-poc/_bakeoff/` (variant-v0, variant-v0r, variant-v1, variant-v2, prototype-switcher, NOTES.md)
- Modify: `web/app/design-poc/page.tsx` (back to a simple route-map landing)

The decision is locked in code and docs; the throwaway has served its purpose.

- [ ] **Step 1: Delete the prototype folder**

```bash
git rm -r web/app/design-poc/_bakeoff
```

- [ ] **Step 2: Restore `web/app/design-poc/page.tsx` to a simple server-component route map**

```tsx
import Link from "next/link";
import { VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, MetricCard, SectionFrame } from "@/components/vela/ui";

const routes = [
  { href: "/cua-ban", label: "Learner runway", detail: "Warm register: hero, rank, queue." },
  { href: "/bao-cao", label: "Operations map", detail: "Admin register: queue, org map, risk, publish." },
  { href: "/xuat-ban", label: "Publish studio", detail: "Warm canvas + admin publish runway." },
];

export default function DesignPocPage() {
  return (
    <VelaAppShell active="PoC" lens="Design">
      <div className="grid gap-5 bg-background px-5 py-6 sm:px-7">
        <SectionFrame title="Route map" description="Một spine, hai register: learner ấm / admin điềm.">
          <div className="divide-y divide-border">
            {routes.map((route, index) => (
              <Link key={route.href} href={route.href} className="vela-focus grid gap-3 px-5 py-4 transition-colors hover:bg-surface-raised sm:grid-cols-[44px_1fr_auto] sm:items-center">
                <span className="grid size-10 place-items-center rounded-lg bg-ink-rail font-mono text-xs font-bold text-white">{String(index + 1).padStart(2, "0")}</span>
                <span>
                  <span className="block font-bold text-foreground">{route.label}</span>
                  <span className="text-sm font-medium text-muted">{route.detail}</span>
                </span>
                <span className="font-mono text-xs font-semibold text-primary">open</span>
              </Link>
            ))}
          </div>
        </SectionFrame>
        <section className="grid gap-3 sm:grid-cols-3">
          <MetricCard label="Learner" value="/cua-ban" detail="Warm register" tone="warm" />
          <MetricCard label="Admin" value="/bao-cao" detail="Data-forward register" tone="primary" />
          <MetricCard label="Publish" value="/xuat-ban" detail="Canvas + publish runway" tone="primary" />
        </section>
      </div>
    </VelaAppShell>
  );
}
```

- [ ] **Step 3: Build**

Run: `npm run build`
Expected: succeeds; `/design-poc` renders the landing; the `next/font` Newsreader import is gone with the deleted page (no dangling import).

- [ ] **Step 4: Commit**

```bash
git add web/app/design-poc
git commit -m "chore(design-poc): remove bake-off prototype, restore route map"
```

---

## Task 8: Final verification across all routes

**Files:** none (verification only)

- [ ] **Step 1: Build clean**

Run: `cd web && npm run build` — expected success. Then `npm run lint` — expected no new errors.

- [ ] **Step 2: Screenshot every route at desktop + mobile**

Run with the dev server up:
`python web/.qa-screenshots/shot.py /cua-ban final-cuaban`, `/bao-cao final-baocao`, `/thanh-vien final-thanhvien`, `/xuat-ban final-xuatban`, `/design-poc final-poc`.

- [ ] **Step 3: Read each PNG against the locked criteria**

For every route confirm: (a) orange is the dominant chromatic color; (b) zero indigo/navy anywhere; (c) amber/red appear only on warning/at-risk data; (d) admin routes read calm/data-forward, learner routes read warm/motivating, and they clearly share one spine; (e) no clipped or colliding Vietnamese text at 1440 or 390. Note any deviation and fix in the owning task before closing.

- [ ] **Step 4: Update the design-system doc's accepted-deviations if any visual compromise was made**, then commit any screenshot artifacts you want to keep.

```bash
git add web/.qa-screenshots docs/vela-design-system-plan.md
git commit -m "test(design): verify orange-register roll-out across routes"
```

---

## Self-Review notes (author)

- Spec coverage: orange primary (T1) ✓, warm-charcoal rail (T1) ✓, semantic-only color (T3–T5) ✓, type/spacing discipline (T2 primitives + per-screen) ✓, two registers learner/admin (T3 admin, T6 learner, T5 split) ✓, retire indigo/navy + focus ring (T1) ✓, fix "tràn text" (T2 + per-screen leading-snug) ✓, delete prototype (T7) ✓.
- The teal token is intentionally retained but demoted; T5 explicitly swaps teal-as-primary in publish to orange. Other incidental teal accents are acceptable as a minor accent per the locked doc.
- No automated unit tests exist for the web UI; verification is build + screenshot review, which is the honest test for a visual change.
