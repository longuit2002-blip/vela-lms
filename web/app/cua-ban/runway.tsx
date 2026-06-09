"use client";

// Code-rendered learner runway: an SVG path with checkpoint nodes placed ON the path via
// getPointAtLength. Alignment is by construction (no chasing baked rings in a raster), themeable
// to the orange tokens, and data-driven. Per mock A: the path sweeps the full hero (tail low-left,
// below the text; rising to top-right) and the labelled nodes run center -> right, clear of the
// upper-left headline/CTAs. Responsive: full-hero overlay on desktop, strip below text on mobile.
import { useEffect, useRef, useState } from "react";
import { AssetIcon } from "@/components/vela/assets";
import type { Tone } from "@/components/vela/ui";
import type { VisualIconName } from "@/lib/visual-assets";

export type Checkpoint = { icon: VisualIconName; label: string; detail: string; tone: Tone };

// Sweep low-left -> high-right in a 1000x380 viewbox.
const PATH = "M 16 352 C 210 346, 344 320, 466 282 C 596 240, 662 166, 764 124 C 858 88, 908 60, 974 30";

function nodeBg(tone: Tone) {
  if (tone === "primary") return "bg-primary text-white";
  if (tone === "gold") return "bg-rank-gold text-white";
  if (tone === "success") return "bg-success text-white";
  return "bg-learning-coral text-white";
}

export function LearnerRunway({ checkpoints }: { checkpoints: Checkpoint[] }) {
  const pathRef = useRef<SVGPathElement>(null);
  const [pts, setPts] = useState<Array<{ x: number; y: number }>>([]);

  useEffect(() => {
    const path = pathRef.current;
    if (!path) return;
    const len = path.getTotalLength();
    const n = checkpoints.length;
    // Nodes live on the right portion of the sweep (the left tail runs under the hero text).
    setPts(
      checkpoints.map((_, i) => {
        const f = n <= 1 ? 0.7 : 0.46 + (i * 0.47) / (n - 1); // 0.46 -> 0.93
        const p = path.getPointAtLength(len * f);
        return { x: p.x, y: p.y };
      }),
    );
  }, [checkpoints]);

  return (
    <div className="pointer-events-none relative mx-5 mb-8 aspect-[1000/300] w-auto sm:mx-8 lg:absolute lg:inset-0 lg:m-0 lg:aspect-auto" aria-hidden="true">
      <svg viewBox="0 0 1000 380" preserveAspectRatio="none" className="absolute inset-0 size-full overflow-visible">
        <defs>
          <linearGradient id="rw-grad" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#D98A2E" />
            <stop offset="100%" stopColor="#F4B042" />
          </linearGradient>
          <filter id="rw-glow" x="-10%" y="-50%" width="120%" height="200%">
            <feGaussianBlur stdDeviation="5" />
          </filter>
          <marker id="rw-arrow" viewBox="0 0 12 12" refX="7" refY="6" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
            <path d="M1,1 L11,6 L1,11 Z" fill="#F4B042" />
          </marker>
        </defs>
        <path d={PATH} fill="none" stroke="url(#rw-grad)" strokeWidth="9" strokeLinecap="round" opacity="0.4" filter="url(#rw-glow)" />
        <path ref={pathRef} d={PATH} fill="none" stroke="url(#rw-grad)" strokeWidth="3.5" strokeLinecap="round" markerEnd="url(#rw-arrow)" />
      </svg>

      {pts.map((p, i) => {
        const cp = checkpoints[i];
        return (
          <span key={cp.label} className="absolute -translate-x-1/2 -translate-y-1/2" style={{ left: `${(p.x / 1000) * 100}%`, top: `${(p.y / 380) * 100}%` }}>
            <span className="relative grid place-items-center">
              <span className="absolute bottom-full mb-1.5 whitespace-nowrap text-[10px] font-bold uppercase tracking-wide text-ink-rail [text-shadow:0_1px_3px_oklch(1_0_0/.85)] lg:text-[11px]">
                {cp.label}
              </span>
              <span className={`grid size-9 place-items-center rounded-full ring-2 ring-white/90 shadow-[0_0_0_4px_oklch(1_0_0/.32),0_4px_14px_oklch(0.7_0.16_50/.4)] lg:size-11 ${nodeBg(cp.tone)}`}>
                <AssetIcon name={cp.icon} className="size-4 lg:size-5" />
              </span>
              <span className="absolute top-full mt-1.5 hidden whitespace-nowrap text-[10px] font-semibold text-ink-rail/70 [text-shadow:0_1px_2px_oklch(1_0_0/.7)] sm:block lg:text-[11px]">
                {cp.detail}
              </span>
            </span>
          </span>
        );
      })}
    </div>
  );
}
