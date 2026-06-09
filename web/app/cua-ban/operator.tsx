// Operator / L&D view of the role-aware /cua-ban home. Matches mock B (learning-operations-map):
// WARM register — flow-banner band, ops-canvas org matrix, risk lanes, publish gate, footer
// metrics. This is the home for an operator, NOT the /bao-cao reports page, so it is its own
// composition (not the shared OperationsBody). All visuals are CSS/SVG + data — no raster assets.
import { AssetIcon } from "@/components/vela/assets";
import { ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, IconBadge, ProgressBar, SectionFrame, StatusPill, type Tone } from "@/components/vela/ui";
import { branchMap, learningFlow, operationsMetrics, publishPipeline, riskLanes, trainingQueue } from "@/lib/mock-lms";
import type { VisualIconName } from "@/lib/visual-assets";
import { RoleLens } from "./role-lens";

export function OperatorHome() {
  return (
    <VelaAppShell active="Từ công ty" lens="L&D Admin" topbar={<OperatorTopbar />}>
      <div className="grid gap-4 px-4 py-5 sm:px-5 xl:px-6" style={{ background: "linear-gradient(180deg, oklch(0.96 0.045 62) 0%, var(--background) 360px)" }}>
        <FlowBanner />
        <section className="grid gap-4 xl:grid-cols-[300px_minmax(0,1fr)_320px]">
          <TrainingQueue />
          <OrganizationMap />
          <aside className="grid content-start gap-4">
            <RiskLanes />
            <PublishGate />
          </aside>
        </section>
        <FooterMetrics />
      </div>
    </VelaAppShell>
  );
}

function OperatorTopbar() {
  return (
    <div className="grid items-center gap-4 xl:grid-cols-[240px_minmax(320px,1fr)_auto]">
      <div>
        <h1 className="text-2xl font-extrabold leading-tight text-foreground">Từ công ty</h1>
        <p className="mt-1 text-sm font-medium text-muted">Command center vận hành đào tạo.</p>
      </div>
      <ShellSearch placeholder="Tìm khóa học, người học, báo cáo..." className="hidden md:flex" />
      <div className="flex flex-wrap justify-end gap-2">
        <RoleLens role="operator" />
        <button className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 font-mono text-sm font-semibold text-foreground">
          <AssetIcon name="data" className="size-4 text-subtle" />
          01/06–30/06/2026
        </button>
      </div>
    </div>
  );
}

function FlowBanner() {
  return (
    <section className="flow-banner overflow-hidden rounded-lg border border-learning-saffron/55 px-5 py-5 text-white">
      <div className="relative z-10 grid gap-5 lg:grid-cols-[minmax(170px,230px)_1fr_auto] lg:items-center">
        <div>
          <p className="text-base font-bold uppercase tracking-wide text-white">Nhịp độ học tập</p>
          <p className="mt-1 text-sm font-medium text-white/90">Dòng chảy học tập toàn tổ chức</p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {learningFlow.map((item) => (
            <div key={item.label} className="grid grid-cols-[44px_1fr] items-center gap-3">
              <span className="grid size-11 place-items-center rounded-full border-2 border-white/70 bg-white/20 text-white">
                <AssetIcon name={item.icon} className="size-5" />
              </span>
              <span className="min-w-0">
                <span className="block text-xs font-semibold text-white/90">{item.label}</span>
                <span className="font-mono text-2xl font-extrabold leading-none">{item.value}</span>
                <span className="block text-[11px] font-medium text-white/80">{item.detail}</span>
              </span>
            </div>
          ))}
        </div>
        <ActionButton secondary className="border-white/60 bg-white/95 text-danger hover:bg-white">Xem dòng chảy</ActionButton>
      </div>
    </section>
  );
}

function TrainingQueue() {
  return (
    <SectionFrame eyebrow="Ưu tiên theo deadline & rủi ro" title="Hàng đợi học tập" action={<ActionButton secondary>Xem tất cả</ActionButton>}>
      <div className="grid gap-2.5 p-3">
        {trainingQueue.map((item) => {
          const tone: Tone = item.progress < 30 ? "danger" : item.progress < 60 ? "warning" : "success";
          return (
            <article key={item.id} className="grid grid-cols-[34px_1fr] gap-3 rounded-lg border border-border bg-surface px-3 py-3">
              <span className="grid size-8 place-items-center rounded-lg bg-learning-coral font-mono text-xs font-bold text-white">{item.id}</span>
              <div className="min-w-0">
                <h3 className="text-sm font-semibold leading-snug text-foreground">{item.title}</h3>
                <div className="mt-2 flex items-center gap-2">
                  <span className="font-mono text-xs font-bold text-danger">{item.due}</span>
                  <StatusPill tone="warm" className="ml-auto">{item.scope}</StatusPill>
                  <span className="font-mono text-xs font-bold text-foreground">{item.progress}%</span>
                </div>
                <div className="mt-2"><ProgressBar value={item.progress} tone={tone} thin /></div>
              </div>
            </article>
          );
        })}
      </div>
      <button className="vela-focus w-full border-t border-border px-3 py-3 text-sm font-semibold text-primary hover:bg-primary-subtle">Xem tất cả hàng đợi</button>
    </SectionFrame>
  );
}

function OrganizationMap() {
  return (
    <SectionFrame
      title="Bản đồ tổ chức & đối tượng"
      description="Theo phòng ban, chi nhánh & scope"
      action={
        <div className="flex items-center gap-3">
          <span className="hidden items-center gap-3 text-[11px] font-semibold text-muted lg:flex">
            <span className="flex items-center gap-1"><span className="size-2 rounded-full bg-success" /> Hoạt động tốt</span>
            <span className="flex items-center gap-1"><span className="size-2 rounded-full bg-danger" /> Rủi ro cao</span>
          </span>
          <div className="flex rounded-lg border border-border bg-surface-muted p-1 text-xs font-semibold text-muted">
            <span className="rounded-md bg-surface px-3 py-1.5 text-foreground">Branches</span>
            <span className="px-3 py-1.5">Scopes</span>
          </div>
        </div>
      }
    >
      <div className="ops-canvas px-4 py-5">
        <div className="relative z-10 grid gap-4 lg:grid-cols-[170px_minmax(0,1fr)]">
          <div className="self-start rounded-lg bg-ink-rail p-4 text-white shadow-[0_8px_16px_oklch(0.2_0.02_52/.22)]">
            <p className="text-base font-bold leading-tight">BetterWork Vietnam</p>
            <p className="mt-1 text-xs font-medium text-white/70">Toàn tổ chức</p>
            <p className="mt-4 font-mono text-xl font-extrabold">1.248 / 1.840</p>
            <p className="text-xs font-medium text-white/65">68% hoàn thành</p>
            <div className="mt-3"><ProgressBar value={68} tone="warm" /></div>
          </div>

          <div className="grid gap-2.5">
            {branchMap.map((branch) => {
              const c = branchColor(branch.tone);
              return (
                <div key={branch.branch} className="grid gap-3 rounded-lg border border-border bg-surface/95 p-3 lg:grid-cols-[minmax(150px,1fr)_minmax(220px,300px)] lg:items-center">
                  <div className="grid grid-cols-[40px_1fr_auto] items-center gap-3">
                    <IconBadge icon={branchIcon(branch.tone)} tone={branchTone(branch.tone)} />
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-foreground">{branch.branch}</p>
                      <p className="text-xs font-medium text-muted">{branch.city}</p>
                      <p className="mt-0.5 font-mono text-xs font-bold text-foreground">{branch.assigned}</p>
                    </div>
                    <span className={`font-mono text-sm font-bold ${c}`}>{branch.percent}%</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2">
                    {branch.scopes.map((scope) => (
                      <div key={scope.label} className="rounded-lg border border-border bg-surface-raised px-3 py-2">
                        <p className="text-[11px] font-semibold uppercase tracking-wide text-subtle">{scope.label}</p>
                        <p className="font-mono text-xs font-bold text-foreground">{scope.value}</p>
                        <p className={`font-mono text-[11px] font-bold ${c}`}>{scope.percent}%</p>
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
        <p className="relative z-10 mt-4 flex items-center gap-2 border-t border-border pt-3 text-xs font-medium text-muted">
          <AssetIcon name="help" className="size-4 text-primary" />
          Dữ liệu cập nhật: 07/06/2026 10:30
          <span className="ml-auto hidden lg:inline">Click nhánh hoặc scope để xem chi tiết</span>
        </p>
      </div>
    </SectionFrame>
  );
}

function RiskLanes() {
  return (
    <SectionFrame title="Lane rủi ro phòng ban" description="Chỉ số tuân thủ & rủi ro chính">
      <div className="p-4">
        <div className="grid grid-cols-[1fr_64px_1fr] gap-3 border-b border-border pb-2 text-[11px] font-semibold uppercase tracking-wide text-subtle">
          <span>Phòng ban</span>
          <span>Tuân thủ</span>
          <span>Rủi ro chính</span>
        </div>
        <div className="grid gap-3 pt-3">
          {riskLanes.map((lane) => (
            <div key={lane.team} className="grid grid-cols-[1fr_64px_1fr] items-center gap-3 text-sm">
              <p className="font-semibold text-foreground">{lane.team}</p>
              <span className={`font-mono font-bold ${riskText(lane.tone)}`}>{lane.completion}%</span>
              <span className="flex items-center gap-1.5 text-xs font-medium text-muted">
                <span className={`size-2 shrink-0 rounded-full ${lane.tone === "danger" ? "bg-danger" : lane.tone === "warning" ? "bg-warning" : "bg-success"}`} />
                {lane.risk}
              </span>
              <div className="col-span-3"><ProgressBar value={lane.completion} tone={lane.tone as Tone} thin /></div>
            </div>
          ))}
        </div>
        <button className="vela-focus mt-4 w-full rounded-lg px-3 py-2 text-sm font-semibold text-primary hover:bg-primary-subtle">Xem báo cáo chi tiết</button>
      </div>
    </SectionFrame>
  );
}

function PublishGate() {
  return (
    <SectionFrame title="Cổng xuất bản" description="Quy trình 5 bước">
      <div className="grid gap-4 p-4">
        <div className="grid gap-2">
          {publishPipeline.map((item, index) => (
            <div key={item.label} className="grid grid-cols-[24px_1fr_auto] items-center gap-2 text-sm">
              <span className={`grid size-6 place-items-center rounded-full text-xs font-bold ${item.done ? "bg-teal text-white" : "border border-border text-subtle"}`}>{item.done ? "✓" : index + 1}</span>
              <span className="font-medium text-foreground">{item.label}</span>
              <span className="font-mono font-bold text-foreground">{item.value}</span>
            </div>
          ))}
        </div>
        <div className="flex items-center gap-4 border-t border-border pt-4">
          <span className="readiness-ring size-20 shrink-0">
            <span className="font-mono text-lg font-bold text-teal">74%</span>
          </span>
          <div className="grid gap-1.5 text-xs">
            <p className="font-semibold text-foreground">Xuất bản đúng hạn</p>
            <Stat label="Tổng nhóm học" value="154" />
            <Stat label="Sẵn sàng xuất bản" value="8" tone="text-success" />
            <Stat label="Quá hạn công bố" value="8" tone="text-danger" />
          </div>
        </div>
        <ActionButton className="w-full" icon="publish">Vào trung tâm xuất bản</ActionButton>
      </div>
    </SectionFrame>
  );
}

function Stat({ label, value, tone = "text-foreground" }: { label: string; value: string; tone?: string }) {
  return (
    <p className="flex items-center justify-between gap-3">
      <span className="font-medium text-muted">{label}</span>
      <span className={`font-mono font-bold ${tone}`}>{value}</span>
    </p>
  );
}

function FooterMetrics() {
  return (
    <SectionFrame title="Chỉ số vận hành học tập" description="Tổng hợp toàn tổ chức · kỳ Q2/2026">
      <div className="grid grid-cols-2 divide-x divide-y divide-border sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-7">
        {operationsMetrics.map((m) => (
          <div key={m.label} className="px-4 py-4">
            <p className="text-[11px] font-semibold uppercase tracking-wide text-subtle">{m.label}</p>
            <p className={`mt-1.5 font-mono text-2xl font-extrabold leading-none ${metricText(m.tone)}`}>{m.value}</p>
            <p className="mt-1.5 text-[11px] font-medium text-muted">{m.delta}</p>
          </div>
        ))}
        <div className="bg-warning-subtle px-4 py-4">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-warning">Điểm cần chú ý</p>
          <p className="mt-1.5 font-mono text-2xl font-extrabold leading-none text-warning">18</p>
          <p className="mt-1.5 text-[11px] font-medium text-muted">mục rủi ro</p>
        </div>
      </div>
    </SectionFrame>
  );
}

function branchTone(tone: string): Tone {
  if (tone === "red") return "danger";
  if (tone === "amber") return "warning";
  return "teal";
}
function branchIcon(tone: string): VisualIconName {
  return tone === "red" ? "alert" : "organization";
}
function branchColor(tone: string) {
  if (tone === "red") return "text-danger";
  if (tone === "amber") return "text-warning";
  return "text-success";
}
function riskText(tone: string) {
  if (tone === "danger") return "text-danger";
  if (tone === "warning") return "text-warning";
  return "text-success";
}
function metricText(tone: string) {
  if (tone === "success") return "text-success";
  if (tone === "gold") return "text-rank-gold";
  if (tone === "teal") return "text-teal";
  return "text-foreground";
}
