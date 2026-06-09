# Asset prompts — /cua-ban learner hero (GPT Image 2 / ChatGPT image gen)

The learner hero needs **two raster assets only**. The runway/checkpoint path is **code-rendered**
(SVG in `web/app/cua-ban/runway.tsx`) — nodes are placed on the path in code, so there is no
runway image to align and no baked rings. Do NOT generate a runway image.

Config-style prompts (the premium GPT-Image-2 pattern). Paste each block into ChatGPT image gen,
then save into `web/public/vela-assets/learner/` with the exact filename (overwrites the current
asset). The page references these names; replacing the file swaps it in.

Fixes if it drifts: transparent came back with a backdrop → "Make the background fully
transparent, PNG with alpha." · too yellow → "more orange, less yellow." · stray marks →
"abstract only, remove all text/numbers/icons/UI."

Brand palette: off-white `#F8F1E9` · apricot `#F7D9BE` · saffron `#F2A93B` · coral `#EF6B3E` ·
gold `#D89A2E`.

---

## 1. `hero-runway-bg.png` — warm hero background (opaque, ~1600×640)

```text
/* HERO_BG_CONFIG: Vela Learner Runway — warm light field
   VERSION: 1.0.0 · AESTHETIC: premium enterprise-SaaS hero, abstract volumetric light */
{
  "GLOBAL_SETTINGS": { "aspect_ratio":"wide landscape 1600x640 (2.5:1)", "style":"abstract volumetric light, clean high-end motion-graphics still", "background":"opaque, single continuous field", "render_flags":["8K_UHD","soft_focus","smooth_gradient","no_banding","editorial_finish"] },
  "LAYOUT": { "energy":"diagonal flow from lower-left up to upper-right", "left_40_percent":"near-white warm glow, clean and uncluttered (dark headline text overlays here)", "right_60_percent":"warmer and more saturated; light streaks converge toward the upper-right" },
  "LIGHT_FIELD": { "streaks":"soft volumetric light ribbons sweeping lower-left to upper-right", "particles":"sparse fine sparkle dust, low density", "bloom":"gentle, no harsh hotspots" },
  "PALETTE": { "left_glow":"#F8F1E9","mid":"#F7D9BE","core":"#F2A93B","accent":"#EF6B3E","rule":"more orange than yellow; no red, pink, purple or blue" },
  "OUTPUT": { "mood":"optimistic, premium, forward-momentum, trustworthy", "avoid":["text","letters","numbers","icons","logos","UI","charts","people","objects","hard edges","dark vignette","neon","purple/blue tint","banding"] }
}
```

## 2. `rank-badge-glow.png` — gold aura behind the rank emblem (TRANSPARENT, ~240×240)

```text
/* GLOW_HALO_CONFIG: Vela rank aura · VERSION: 1.0.0 */
{
  "GLOBAL_SETTINGS": { "aspect_ratio":"square 240x240", "background":"FULLY TRANSPARENT (PNG with alpha)", "render_flags":["transparent_alpha","soft_radial_falloff","no_banding"] },
  "CORE_ASSETS": { "halo":"a single soft radial golden bloom, brightest at the center, fading smoothly to fully transparent at the edges" },
  "MATERIAL_LIGHTING": { "color":"#D89A2E warm gold", "falloff":"smooth gaussian, no visible ring, no hard edge" },
  "OUTPUT": { "mood":"premium, subtle", "avoid":["shape outline","badge","ring","text","icon","solid background","banding"] }
}
```

---

## Runway = code, not an image

The glowing checkpoint path is an SVG drawn in `web/app/cua-ban/runway.tsx`:
- A single themeable gradient path (gold→saffron) with an arrowhead, sweeping low-left → high-right.
- Checkpoint nodes are placed **on** the path via `getPointAtLength` (data-driven: pass any number
  of checkpoints; they distribute along the curve). Labels render above each node, detail below.
- To restyle: edit the `PATH` constant, the node-fraction spread, or `nodeBg()` in that file. No
  image regeneration, no alignment chasing.

When you regenerate the two raster assets above, drop them in `web/public/vela-assets/learner/`
and tell me — I'll re-screenshot `/cua-ban` and fine-tune the hero overlays.
