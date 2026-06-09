import type { ReactNode } from "react";
import { AssetIcon } from "@/components/vela/assets";
import type { VisualIconName } from "@/lib/visual-assets";

export type Tone = "default" | "primary" | "success" | "warning" | "danger" | "warm" | "teal" | "gold" | "ink";

const textTone: Record<Tone, string> = {
  default: "text-foreground",
  primary: "text-primary",
  success: "text-success",
  warning: "text-warning",
  danger: "text-danger",
  warm: "text-learning-coral",
  teal: "text-teal",
  gold: "text-rank-gold",
  ink: "text-ink-rail",
};

const bgTone: Record<Tone, string> = {
  default: "bg-surface-muted text-muted",
  primary: "bg-primary-subtle text-primary",
  success: "bg-success-subtle text-success",
  warning: "bg-warning-subtle text-warning",
  danger: "bg-danger-subtle text-danger",
  warm: "bg-learning-apricot text-learning-coral",
  teal: "bg-teal-subtle text-teal",
  gold: "bg-warning-subtle text-rank-gold",
  ink: "bg-ink-rail text-white",
};

const fillTone: Record<Tone, string> = {
  default: "bg-border-strong",
  primary: "bg-primary",
  success: "bg-success",
  warning: "bg-warning",
  danger: "bg-danger",
  warm: "bg-learning-coral",
  teal: "bg-teal",
  gold: "bg-rank-gold",
  ink: "bg-ink-rail",
};

export function toneText(tone: Tone) {
  return textTone[tone];
}

export function StatusPill({
  children,
  tone = "primary",
  className = "",
}: {
  children: ReactNode;
  tone?: Tone;
  className?: string;
}) {
  return <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ${bgTone[tone]} ${className}`}>{children}</span>;
}

export function ProgressBar({
  value,
  tone = "primary",
  thin = false,
}: {
  value: number;
  tone?: Tone;
  thin?: boolean;
}) {
  return (
    <div className={`${thin ? "h-1.5" : "h-2"} overflow-hidden rounded-full bg-surface-muted`} aria-label={`Tiến độ ${value}%`}>
      <div className={`progress-fill h-full rounded-full ${fillTone[tone]}`} style={{ width: `${value}%` }} />
    </div>
  );
}

export function SectionFrame({
  eyebrow,
  title,
  description,
  action,
  children,
  className = "",
  padded = false,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
  padded?: boolean;
}) {
  return (
    <section className={`panel overflow-hidden ${className}`}>
      <div className="grid gap-3 border-b border-border bg-surface-raised px-5 py-4 md:grid-cols-[1fr_auto] md:items-center">
        <div>
          {eyebrow ? <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{eyebrow}</p> : null}
          <h2 className="text-base font-bold leading-tight text-foreground">{title}</h2>
          {description ? <p className="mt-1 text-xs font-medium text-muted">{description}</p> : null}
        </div>
        {action}
      </div>
      <div className={padded ? "p-4" : ""}>{children}</div>
    </section>
  );
}

export function MetricCard({
  label,
  value,
  detail,
  tone = "primary",
}: {
  label: string;
  value: string;
  detail: string;
  tone?: Tone;
}) {
  return (
    <article className="panel min-h-[96px] p-5">
      <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{label}</p>
      <p className={`mt-2 font-mono text-2xl font-extrabold leading-none ${textTone[tone]}`}>{value}</p>
      <p className="mt-2 text-xs font-medium text-muted">{detail}</p>
    </article>
  );
}

export function PageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  return (
    <header className="grid gap-3 lg:grid-cols-[minmax(220px,1fr)_auto] lg:items-start">
      <div>
        <h1 className="text-2xl font-black leading-tight text-foreground">{title}</h1>
        <p className="mt-1 max-w-[62ch] text-sm font-medium text-muted">{description}</p>
      </div>
      {actions ? <div className="flex flex-wrap justify-start gap-2 lg:justify-end">{actions}</div> : null}
    </header>
  );
}

export function RankEmblem({
  size = "md",
}: {
  size?: "sm" | "md" | "lg";
}) {
  const shell = size === "lg" ? "size-24" : size === "sm" ? "size-12" : "size-20";
  const text = size === "lg" ? "text-4xl" : size === "sm" ? "text-lg" : "text-3xl";

  return (
    <span
      aria-hidden="true"
      className={`grid ${shell} place-items-center rounded-lg border border-warning/30 bg-warning-subtle text-rank-gold shadow-[0_4px_8px_oklch(0.57_0.14_57/.14)]`}
    >
      <span className={`grid size-[72%] place-items-center rounded-md bg-gradient-to-b from-learning-saffron to-warning font-black ${text} text-white`}>
        G
      </span>
    </span>
  );
}

export function ActionButton({
  children,
  tone = "primary",
  icon,
  secondary = false,
  className = "",
}: {
  children: ReactNode;
  tone?: Tone;
  icon?: VisualIconName;
  secondary?: boolean;
  className?: string;
}) {
  const primaryClass =
    tone === "danger"
      ? "bg-danger text-white hover:bg-danger/90"
      : tone === "ink"
        ? "bg-ink-rail text-white hover:bg-ink-rail-soft"
        : "bg-primary text-white hover:bg-primary-hover";

  return (
    <button
      className={`vela-focus inline-flex min-h-10 items-center justify-center gap-2 rounded-lg px-4 text-sm font-semibold transition-colors ${
        secondary ? "border border-border bg-surface text-foreground hover:bg-surface-muted" : primaryClass
      } ${className}`}
    >
      {icon ? <AssetIcon name={icon} className="size-4" /> : null}
      {children}
    </button>
  );
}

export function IconBadge({
  icon,
  tone = "primary",
  size = "md",
}: {
  icon: VisualIconName;
  tone?: Tone;
  size?: "sm" | "md" | "lg";
}) {
  const sizeClass = size === "lg" ? "size-14" : size === "sm" ? "size-8" : "size-10";
  const iconSize = size === "lg" ? "size-6" : "size-4";

  return (
    <span className={`grid ${sizeClass} shrink-0 place-items-center rounded-lg ${bgTone[tone]}`}>
      <AssetIcon name={icon} className={iconSize} />
    </span>
  );
}

export function DataTable({
  headers,
  children,
  minWidth = "min-w-[900px]",
}: {
  headers: string[];
  children: ReactNode;
  minWidth?: string;
}) {
  return (
    <div className="overflow-x-auto">
      <table className={`w-full ${minWidth} border-separate border-spacing-0 text-left text-sm`}>
        <thead className="text-[11px] font-semibold uppercase tracking-wide text-subtle">
          <tr>
            {headers.map((header) => (
              <th key={header} className="border-b border-border bg-surface-raised px-4 py-3">
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">{children}</tbody>
      </table>
    </div>
  );
}

export function RunwaySteps({
  steps,
  active = 1,
}: {
  steps: ReadonlyArray<{ label: string; detail: string; icon: VisualIconName; tone: Tone }>;
  active?: number;
}) {
  return (
    <div className="relative grid grid-cols-5 gap-2 py-4">
      <div className="absolute left-10 right-10 top-[42px] h-0.5 bg-border" />
      {steps.map((step, index) => (
        <div key={step.label} className="relative z-10 grid justify-items-center gap-2 text-center">
          <span className={`grid size-10 place-items-center rounded-full border-2 border-surface ${index <= active ? fillTone[step.tone] : "bg-surface-muted"} text-white`}>
            <AssetIcon name={step.icon} className="size-4" />
          </span>
          <span className={`text-xs font-semibold ${index <= active ? textTone[step.tone] : "text-muted"}`}>{step.label}</span>
          <span className="text-[11px] font-semibold leading-tight text-muted">{step.detail}</span>
        </div>
      ))}
    </div>
  );
}
