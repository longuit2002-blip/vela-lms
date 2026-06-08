// V0r — the ADMIN register: neutral, data-forward surfaces with ORANGE as the single
// dominant brand accent (primary actions, on-track state, links, active nav). Amber/red
// appear ONLY for problems, so the eye jumps to rows that need attention. Type + spacing
// discipline borrowed from V1. Same Vela token system; --color-primary is ORANGE globally.
import { ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { AssetIcon } from "@/components/vela/assets";
import {
  branchMap,
  learningFlow,
  operationsMetrics,
  publishPipeline,
  riskLanes,
  trainingQueue,
} from "@/lib/mock-lms";

// Orange = brand + on-track. Amber = warning. Red = at-risk. Nothing else gets a hue.
const sem = (tone: string) =>
  tone === "danger" || tone === "red"
    ? { text: "text-danger", fill: "bg-danger", dot: "bg-danger" }
    : tone === "warning" || tone === "amber" || tone === "gold"
      ? { text: "text-warning", fill: "bg-warning", dot: "bg-warning" }
      : { text: "text-primary", fill: "bg-primary", dot: "bg-primary" };

function Eyebrow({ children }: { children: React.ReactNode }) {
  return <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{children}</p>;
}

function Panel({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return <section className={`rounded-lg border border-border bg-surface ${className}`}>{children}</section>;
}

function PanelHeader({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="flex items-end justify-between gap-4 border-b border-border px-5 py-4">
      <div>
        {eyebrow ? <Eyebrow>{eyebrow}</Eyebrow> : null}
        <h2 className="mt-0.5 text-base font-bold leading-tight text-foreground">{title}</h2>
        {description ? <p className="mt-1 text-xs font-medium text-muted">{description}</p> : null}
      </div>
      {action}
    </div>
  );
}

export default function ReportsPage() {
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
}

function Topbar() {
  return (
    <div className="grid items-center gap-4 xl:grid-cols-[240px_minmax(320px,1fr)_auto]">
      <div>
        <h1 className="text-2xl font-extrabold leading-tight text-foreground">Từ công ty</h1>
        <p className="mt-1 text-sm font-medium text-muted">Số liệu & tracking đào tạo toàn tổ chức.</p>
      </div>
      <ShellSearch placeholder="Tìm khóa học, người học, báo cáo..." className="hidden md:flex" />
      <div className="flex flex-wrap justify-end gap-2">
        <button className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground">
          <AssetIcon name="users" className="size-4 text-subtle" />
          <span className="text-subtle">Lens</span> L&D Admin
        </button>
        <button className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 font-mono text-sm font-semibold text-foreground">
          <AssetIcon name="data" className="size-4 text-subtle" />
          01/06–30/06/2026
        </button>
      </div>
    </div>
  );
}

function FlowStrip() {
  return (
    <Panel className="overflow-hidden border-t-2 border-t-primary">
      <div className="flex items-center justify-between border-b border-border px-5 py-3">
        <Eyebrow>Nhịp học tập · toàn tổ chức</Eyebrow>
        <button className="vela-focus text-sm font-semibold text-primary hover:underline">Xem dòng chảy</button>
      </div>
      <div className="grid divide-x divide-border sm:grid-cols-2 lg:grid-cols-4">
        {learningFlow.map((item) => (
          <div key={item.label} className="px-5 py-6">
            <div className="flex items-center gap-2">
              <span className={`size-1.5 rounded-full ${sem(item.tone).dot}`} />
              <Eyebrow>{item.label}</Eyebrow>
            </div>
            <p className="mt-2.5 font-mono text-3xl font-extrabold leading-none text-foreground">{item.value}</p>
            <p className="mt-2 text-xs font-medium text-muted">{item.detail}</p>
          </div>
        ))}
      </div>
    </Panel>
  );
}

function Queue() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader eyebrow="Ưu tiên theo deadline & rủi ro" title="Hàng đợi học tập" />
      <div className="px-5">
        {trainingQueue.map((item) => {
          const urgent = item.due.includes("Quá hạn");
          const tone = item.progress < 30 ? "danger" : item.progress < 60 ? "warning" : "primary";
          return (
            <article key={item.id} className="flex gap-3 border-b border-border py-4 last:border-b-0">
              <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-primary font-mono text-xs font-bold text-white">
                {item.id}
              </span>
              <div className="min-w-0 flex-1">
                <h3 className="text-sm font-semibold leading-snug text-foreground">{item.title}</h3>
                <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1">
                  <span className={`text-xs font-medium ${urgent ? "text-danger" : "text-muted"}`}>{item.due}</span>
                  <span className="inline-flex items-center rounded-full bg-surface-muted px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-subtle">
                    {item.scope}
                  </span>
                  <span className="ml-auto font-mono text-xs font-bold text-foreground">{item.progress}%</span>
                </div>
                <div className="mt-2.5 h-1.5 overflow-hidden rounded-full bg-surface-muted">
                  <div className={`h-full rounded-full ${sem(tone).fill}`} style={{ width: `${item.progress}%` }} />
                </div>
              </div>
            </article>
          );
        })}
      </div>
    </Panel>
  );
}

function OrgMap() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader
        title="Bản đồ tổ chức & đối tượng"
        description="Theo phòng ban, chi nhánh & scope"
        action={
          <div className="flex rounded-lg border border-border p-0.5 text-xs font-semibold">
            <span className="rounded-md bg-surface-muted px-3 py-1.5 text-foreground">Branches</span>
            <span className="px-3 py-1.5 text-subtle">Scopes</span>
          </div>
        }
      />
      <div className="px-5 py-5">
        <div className="flex items-end justify-between gap-4 rounded-lg bg-ink-rail px-5 py-4 text-white">
          <div>
            <p className="text-lg font-bold leading-tight">BetterWork Vietnam</p>
            <p className="mt-0.5 text-xs font-medium text-white/60">Toàn tổ chức · 4 chi nhánh</p>
          </div>
          <div className="text-right">
            <p className="font-mono text-2xl font-extrabold leading-none">1.248 / 1.840</p>
            <p className="mt-1 text-xs font-medium text-white/60">68% hoàn thành</p>
          </div>
        </div>
        <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface-muted">
          <div className="h-full rounded-full bg-primary" style={{ width: "68%" }} />
        </div>

        <div className="mt-1">
          {branchMap.map((branch) => {
            const s = sem(branch.tone);
            return (
              <div key={branch.branch} className="grid gap-3 border-b border-border py-4 last:border-b-0 lg:grid-cols-[minmax(0,1fr)_230px] lg:items-center">
                <div>
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-foreground">
                      {branch.branch}
                      <span className="ml-2 text-xs font-medium text-muted">{branch.city}</span>
                    </p>
                    <span className={`font-mono text-sm font-bold ${s.text}`}>{branch.percent}%</span>
                  </div>
                  <p className="mt-1 font-mono text-xs font-medium text-muted">{branch.assigned}</p>
                  <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface-muted">
                    <div className={`h-full rounded-full ${s.fill}`} style={{ width: `${branch.percent}%` }} />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-2">
                  {branch.scopes.map((scope) => (
                    <div key={scope.label} className="rounded-lg border border-border bg-surface-raised px-3 py-2.5">
                      <p className="text-[11px] font-semibold uppercase tracking-wide text-subtle">{scope.label}</p>
                      <p className="mt-1 font-mono text-xs font-bold text-foreground">{scope.value}</p>
                      <p className={`font-mono text-[11px] font-bold ${s.text}`}>{scope.percent}%</p>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>

        <p className="mt-4 flex items-center gap-2 text-xs font-medium text-subtle">
          <AssetIcon name="help" className="size-4 text-subtle" />
          Dữ liệu cập nhật: 07/06/2026 10:30
        </p>
      </div>
    </Panel>
  );
}

function RiskLanes() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader title="Lane rủi ro phòng ban" description="Tuân thủ & rủi ro chính" />
      <div className="px-5 py-1">
        {riskLanes.map((lane) => {
          const s = sem(lane.tone);
          return (
            <div key={lane.team} className="border-b border-border py-3.5 last:border-b-0">
              <div className="flex items-center justify-between gap-3">
                <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
                  <span className={`size-1.5 rounded-full ${s.dot}`} />
                  {lane.team}
                </span>
                <span className={`font-mono text-sm font-bold ${s.text}`}>{lane.completion}%</span>
              </div>
              <div className="mt-2 flex items-center justify-between gap-3">
                <span className="text-xs font-medium text-muted">{lane.risk}</span>
              </div>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface-muted">
                <div className={`h-full rounded-full ${s.fill}`} style={{ width: `${lane.completion}%` }} />
              </div>
            </div>
          );
        })}
      </div>
    </Panel>
  );
}

function PublishGate() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader title="Cổng xuất bản" description="Quy trình 5 bước" />
      <div className="px-5 py-4">
        {publishPipeline.map((item, index) => (
          <div key={item.label} className="flex items-center gap-3 border-b border-border py-2.5 last:border-b-0">
            <span className={`grid size-6 place-items-center rounded-full font-mono text-[11px] font-bold ${item.done ? "bg-primary text-white" : "border border-border text-subtle"}`}>
              {item.done ? "✓" : index + 1}
            </span>
            <span className="flex-1 text-sm font-medium text-foreground">{item.label}</span>
            <span className="font-mono text-sm font-bold text-foreground">{item.value}</span>
          </div>
        ))}
        <div className="mt-4 flex items-center gap-4 border-t border-border pt-4">
          <span
            className="grid size-16 shrink-0 place-items-center rounded-full"
            style={{ background: "conic-gradient(var(--color-primary) 0 74%, var(--color-surface-muted) 74% 100%)" }}
          >
            <span className="grid size-12 place-items-center rounded-full bg-surface font-mono text-sm font-bold text-primary">74%</span>
          </span>
          <div>
            <p className="text-sm font-semibold text-foreground">Sẵn sàng xuất bản</p>
            <p className="mt-0.5 text-xs font-medium text-muted">36 khóa qua cổng soạn thảo & rà soát</p>
          </div>
        </div>
      </div>
    </Panel>
  );
}

function FooterMetrics() {
  return (
    <Panel className="overflow-hidden">
      <div className="grid divide-x divide-y divide-border sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7">
        {operationsMetrics.map((m) => (
          <div key={m.label} className="px-5 py-5">
            <Eyebrow>{m.label}</Eyebrow>
            <p className="mt-2 font-mono text-2xl font-extrabold leading-none text-foreground">{m.value}</p>
            <p className={`mt-2 text-xs font-medium ${m.tone === "success" || m.tone === "primary" ? "text-primary" : "text-muted"}`}>{m.delta}</p>
          </div>
        ))}
        <div className="px-5 py-5">
          <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-danger">Điểm cần chú ý</p>
          <p className="mt-2 font-mono text-2xl font-extrabold leading-none text-danger">18</p>
          <p className="mt-2 text-xs font-medium text-muted">mục rủi ro</p>
        </div>
      </div>
    </Panel>
  );
}
