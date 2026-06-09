import Link from "next/link";
import { VelaAppShell } from "@/components/vela/app-shell";
import { MetricCard, SectionFrame } from "@/components/vela/ui";

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
