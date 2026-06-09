// Two-register split:
//   • CourseCanvas (+ sub-strips + progress runway) = LEARNER-WARM: .learning-field, warm
//     accents, inviting hero. Type discipline applied; primary CTAs via orange tokens.
//   • PublishRunwayPanel + StudyRecordTable + ContentQueueTable + RankProgressCard =
//     ADMIN-RESTRAINED: neutral surfaces, orange = on-track/brand, amber/red = problems only.
//     No green on admin sections; no teal-as-primary; no decorative gradient washes.
import { AssetIcon } from "@/components/vela/assets";
import { ShellIconButton, ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, DataTable, IconBadge, ProgressBar, RankEmblem, RunwaySteps, SectionFrame, StatusPill } from "@/components/vela/ui";
import {
  aiAssist,
  contentQueue,
  currentUser,
  progressRunway,
  publishRunway,
  readinessRisks,
  sourceDocuments,
  studyRecords,
} from "@/lib/mock-lms";

// ── Admin-register primitives (same pattern as /bao-cao) ──────────────────────
// orange = brand / on-track   amber = warning   red = at-risk   neutral otherwise
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
        <h2 className="mt-0.5 text-base font-bold leading-snug text-foreground">{title}</h2>
        {description ? <p className="mt-1 text-xs font-medium text-muted">{description}</p> : null}
      </div>
      {action}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────
export default function PublishingPage() {
  return (
    <VelaAppShell active="Xuất bản" lens="CSKH - Learner" topbar={<PublishingTopbar />}>
      <div className="workspace-page">
        {/* Top split: warm course canvas (left) + admin publish runway (right) */}
        <section className="grid border-b border-border xl:grid-cols-[minmax(0,1fr)_420px]">
          <CourseCanvas />
          <PublishRunwayPanel />
        </section>

        {/* Bottom row: admin ops tables + rank card */}
        <section className="grid gap-4 px-4 py-4 sm:px-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_320px]">
          <StudyRecordTable />
          <ContentQueueTable />
          <RankProgressCard />
        </section>
      </div>
    </VelaAppShell>
  );
}

// ── Topbar ────────────────────────────────────────────────────────────────────
function PublishingTopbar() {
  return (
    <div className="grid items-center gap-4 xl:grid-cols-[180px_minmax(320px,1fr)_auto]">
      {/* date chip — font-semibold per type ladder (labels/UI chrome) */}
      <button className="vela-focus inline-flex min-h-10 w-fit items-center gap-2 rounded-lg border border-learning-coral/35 bg-surface px-3 text-sm font-semibold text-foreground">
        <AssetIcon name="data" className="size-4 text-subtle" />
        Hôm nay, 07/06/2025
      </button>
      <ShellSearch placeholder="Tìm khóa học, tài liệu, người dùng..." className="hidden md:flex" />
      <div className="ml-auto flex items-center gap-3">
        <ShellIconButton icon="bell" label="Thông báo" badge="3" />
        <ShellIconButton icon="help" label="Trợ giúp" />
        {/* lens chip */}
        <button className="vela-focus min-h-10 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground">CSKH - Learner</button>
        {/* avatar: ink-rail, not teal */}
        <div className="grid size-10 place-items-center rounded-full bg-ink-rail text-sm font-bold text-white">C</div>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// LEARNER-WARM zone — .learning-field background, warm accents, inviting tone
// ══════════════════════════════════════════════════════════════════════════════
function CourseCanvas() {
  return (
    <section className="learning-field relative overflow-hidden px-4 py-5 sm:px-6 lg:px-7">
      <div className="relative z-10">
        {/* Eyebrow label — warm register uses learning-coral */}
        <p className="text-xs font-bold uppercase tracking-wide text-learning-coral">Course canvas</p>

        {/* Hero H1 — 900 weight is correct at this scale */}
        <h1 className="mt-2 text-4xl font-black leading-snug text-foreground sm:text-5xl">
          Hành trình học của bạn
        </h1>
        <p className="mt-3 text-base font-semibold text-muted">
          Học đúng phần được giao · Đúng tiến độ · Lên rank mỗi ngày
        </p>

        <div className="mt-5 grid gap-5 lg:grid-cols-[220px_minmax(0,1fr)]">
          <CourseThumbnail />

          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <StatusPill tone="warning">Ưu tiên cao</StatusPill>
              {/* "Nội bộ" tag — warm register: keep ink tone for internal badge */}
              <StatusPill tone="ink">Nội bộ</StatusPill>
            </div>
            {/* Course title — 700 weight for section heading under the H1 */}
            <h2 className="mt-2 text-xl font-bold leading-snug text-foreground">
              An toàn thông tin cho nhân sự tuyến đầu
            </h2>
            <p className="mt-2 text-sm font-semibold text-muted">Bài đang mở: Bảo mật dữ liệu khách hàng</p>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-xs font-semibold text-muted">
              <span>Module 3 / 6</span>
              <span>15 phút</span>
              <span>E-learning</span>
              <span>15 tài liệu</span>
            </div>
            <div className="mt-5">
              <div className="mb-2 flex items-center justify-between text-sm font-bold">
                <span className="text-muted">Tiến độ khóa học</span>
                {/* Progress % — orange (primary) in warm canvas */}
                <span className="font-mono text-primary">78%</span>
              </div>
              {/* Primary (orange) progress bar in learner canvas */}
              <ProgressBar value={78} tone="primary" />
            </div>
            <div className="mt-4 flex flex-wrap gap-3">
              <ActionButton icon="learning" className="px-3">Mở bài tiếp theo</ActionButton>
              <ActionButton secondary icon="complete" className="px-3">Xem mục tiêu</ActionButton>
              <ActionButton secondary icon="document" className="px-3">Tài liệu liên quan</ActionButton>
            </div>
          </div>
        </div>

        <div className="mt-5 grid gap-3 2xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <SourceStrip />
          <AiAssistStrip />
        </div>

        {/* Progress runway — still within the warm canvas */}
        <section className="panel mt-4 overflow-hidden bg-surface/92">
          <div className="border-b border-border px-4 py-3">
            <p className="text-xs font-bold text-learning-coral">Progress runway</p>
          </div>
          <RunwaySteps steps={progressRunway} active={1} />
        </section>
      </div>
    </section>
  );
}

function CourseThumbnail() {
  return (
    <div className="course-thumb relative aspect-[1.45] rounded-lg border border-border">
      <span className="windmast left-[18%]" />
      <span className="windmast left-[52%]" />
      <span className="windmast left-[82%]" />
      <span className="absolute inset-0 grid place-items-center">
        <span className="grid size-16 place-items-center rounded-full bg-ink-rail/76 text-white backdrop-blur">
          <AssetIcon name="learning" className="size-7" />
        </span>
      </span>
    </div>
  );
}

function SourceStrip() {
  return (
    <section className="panel bg-surface/94 p-3">
      <div className="flex items-center justify-between">
        <p className="text-xs font-bold text-foreground">Nguồn tài liệu (DMS)</p>
        <span className="source-stack-mark" aria-hidden="true">
          <span />
          <span />
          <span />
        </span>
      </div>
      <div className="mt-3 grid gap-2 sm:grid-cols-4">
        {sourceDocuments.map((source) => (
          <div key={source.title} className="rounded-lg border border-border bg-surface-raised px-3 py-2">
            <p className="truncate text-xs font-bold text-foreground">{source.title}</p>
            <p className="mt-1 font-mono text-[10px] font-semibold text-muted">{source.type}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

function AiAssistStrip() {
  return (
    <section className="panel bg-surface/94 p-3">
      <p className="text-xs font-bold text-foreground">AI LMS assist</p>
      <div className="mt-3 grid gap-2 sm:grid-cols-3">
        {aiAssist.map((item) => (
          <button key={item.title} className="vela-focus grid min-h-14 grid-cols-[28px_1fr] items-center gap-2 rounded-lg border border-border bg-surface-raised px-3 text-left">
            {/* orange accent in warm canvas for AI chip icon */}
            <AssetIcon name="aiSource" className="size-5 text-primary" />
            <span>
              <span className="block text-xs font-bold text-foreground">{item.title}</span>
              <span className="text-[11px] font-medium text-muted">{item.detail}</span>
            </span>
          </button>
        ))}
      </div>
    </section>
  );
}

// ══════════════════════════════════════════════════════════════════════════════
// ADMIN-RESTRAINED zone — neutral surfaces, orange = on-track, amber/red = problems
// ══════════════════════════════════════════════════════════════════════════════
function PublishRunwayPanel() {
  return (
    <aside className="bg-surface px-4 py-5 sm:px-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          {/* Admin eyebrow — neutral uppercase, no warm hue */}
          <Eyebrow>Publish runway</Eyebrow>
          <h2 className="mt-1 text-xl font-bold leading-snug text-foreground">Đường xuất bản khóa học</h2>
        </div>
        <ActionButton secondary>Tạo mới</ActionButton>
      </div>

      {/* Stepper: done/active = orange; pending = neutral border */}
      <section className="relative mt-5">
        <div className="absolute left-8 right-8 top-7 h-0.5 bg-border" />
        <div className="relative z-10 grid grid-cols-5 gap-2 pt-2">
          {publishRunway.map((step) => (
            <div key={step.label} className="grid justify-items-center gap-2 text-center">
              <span className={`grid size-11 place-items-center rounded-full border-2 border-surface ${adminStepFill(step.tone)} text-white`}>
                <AssetIcon name={step.icon} className="size-5" />
              </span>
              <span className="text-xs font-semibold text-foreground">{step.label}</span>
              <span className="text-[11px] font-medium leading-snug text-muted">{step.detail}</span>
              <span className="font-mono text-[11px] font-semibold text-muted">{step.progress}</span>
            </div>
          ))}
        </div>
      </section>

      {/* Readiness panel: orange conic ring, neutral surface */}
      <Panel className="mt-5 overflow-hidden">
        <div className="p-4">
          <div className="grid gap-4 sm:grid-cols-[130px_1fr]">
            <div>
              <Eyebrow>Điểm sẵn sàng</Eyebrow>
              {/* Readiness ring: orange conic gradient */}
              <div className="mt-3 flex items-center gap-3">
                <span
                  className="grid size-16 shrink-0 place-items-center rounded-full"
                  style={{ background: "conic-gradient(var(--color-primary) 0 82%, var(--color-surface-muted) 82% 100%)" }}
                >
                  <span className="grid size-12 place-items-center rounded-full bg-surface font-mono text-sm font-bold text-primary">82</span>
                </span>
                <div>
                  <p className="font-mono text-xl font-extrabold leading-none text-foreground">82<span className="text-sm font-medium text-muted">/100</span></p>
                  {/* Neutral label — NOT green; on-track just stays neutral */}
                  <p className="mt-1 text-xs font-medium text-muted">Sẵn sàng</p>
                </div>
              </div>
            </div>
            <div className="border-t border-border pt-4 sm:border-l sm:border-t-0 sm:pl-5 sm:pt-0">
              <Eyebrow>Phạm vi đối tượng</Eyebrow>
              <p className="mt-2 text-base font-bold text-foreground">Nội bộ</p>
              <p className="mt-1 text-sm font-medium text-muted">Phòng Vận hành, HR, CSKH</p>
              <p className="mt-3 font-mono text-sm font-bold text-foreground">126 người</p>
            </div>
          </div>
        </div>
      </Panel>

      {/* Risk checks: amber/red only — no green/teal */}
      <Panel className="mt-4 overflow-hidden">
        <PanelHeader eyebrow="Rủi ro &amp; cần xử lý" title="Readiness checks" />
        <div className="grid gap-2 p-3">
          {readinessRisks.map((risk) => {
            const riskTone = risk.level === "Cao" ? "danger" : "warning";
            return (
              <div key={risk.label} className="grid grid-cols-[34px_1fr_auto_22px] items-center gap-3 rounded-lg border border-border bg-surface-raised px-3 py-2 text-sm">
                <IconBadge icon="alert" tone={riskTone} size="sm" />
                <span>
                  <span className="block font-semibold text-foreground">{risk.label}</span>
                  <span className="text-xs font-medium text-muted">{risk.detail}</span>
                </span>
                <span className={`font-bold ${sem(riskTone).text}`}>{risk.level}</span>
                <span className="grid size-5 place-items-center rounded-full bg-warning text-xs font-bold text-white">!</span>
              </div>
            );
          })}
        </div>
      </Panel>

      <ActionButton className="mt-4 w-full" icon="publish">Tiếp tục xuất bản</ActionButton>
    </aside>
  );
}

// Admin step fill: danger/warning map to semantic; everything else = orange (on-track)
// No green — completed/active states stay orange
function adminStepFill(tone: string) {
  if (tone === "danger") return "bg-danger";
  if (tone === "warning") return "bg-warning";
  if (tone === "gold") return "bg-warning";
  // "success" or any other: orange (on-track, not green)
  return "bg-primary";
}

// ── Admin tables ──────────────────────────────────────────────────────────────
function StudyRecordTable() {
  return (
    <SectionFrame title="Hồ sơ học tập" action={<StatusPill tone="primary">Đang học</StatusPill>}>
      <DataTable headers={["#", "Khóa học", "Bài đang mở", "Tiến độ", "Hạn hoàn thành", "Điểm", "Trạng thái"]} minWidth="min-w-[760px]">
        {studyRecords.map((record, index) => {
          // Admin register: progress tone — danger for 0, warning for at-risk, primary (orange) otherwise
          const barTone = record.progress === 0 ? "danger" : record.progress < 50 ? "warning" : "primary";
          // Status pill: completed = neutral (not green); overdue = danger; active = primary
          const pillTone = record.status === "Quá hạn" ? "danger" : record.status === "Hoàn thành" ? "default" : "primary";
          return (
            <tr key={record.title} className="hover:bg-surface-raised">
              <td className="px-4 py-3 font-mono text-xs font-semibold">{index + 1}</td>
              <td className="px-4 py-3 font-semibold text-foreground">{record.title}</td>
              <td className="px-4 py-3 text-xs font-medium text-muted">{record.lesson}</td>
              <td className="px-4 py-3">
                <div className="min-w-20">
                  <ProgressBar value={record.progress} tone={barTone} thin />
                </div>
              </td>
              <td className={`px-4 py-3 text-xs font-semibold ${record.due.includes("Quá") ? "text-danger" : "text-muted"}`}>{record.due}</td>
              <td className="px-4 py-3 font-mono text-xs font-semibold">{record.score}</td>
              <td className="px-4 py-3">
                <StatusPill tone={pillTone}>{record.status}</StatusPill>
              </td>
            </tr>
          );
        })}
      </DataTable>
    </SectionFrame>
  );
}

function ContentQueueTable() {
  return (
    <SectionFrame title="Hàng chờ nội dung" action={<StatusPill tone="primary">Của tôi</StatusPill>}>
      <DataTable headers={["#", "Tiêu đề", "Loại", "Owner", "Giai đoạn", "Cập nhật", ""]} minWidth="min-w-[760px]">
        {contentQueue.map((item, index) => {
          // Stage tone: Legal = danger, Approve = warning, Draft/Review = primary (orange)
          const stageTone = item.stage === "Legal" ? "danger" : item.stage === "Approve" ? "warning" : "primary";
          return (
            <tr key={item.title} className="hover:bg-surface-raised">
              <td className="px-4 py-3 font-mono text-xs font-semibold">{index + 1}</td>
              <td className="px-4 py-3 font-semibold text-foreground">{item.title}</td>
              <td className="px-4 py-3 text-xs font-medium text-muted">{item.type}</td>
              <td className="px-4 py-3 text-xs font-semibold text-foreground">{item.owner}</td>
              <td className="px-4 py-3">
                <StatusPill tone={stageTone}>{item.stage}</StatusPill>
              </td>
              <td className="px-4 py-3 font-mono text-xs font-medium text-muted">{item.updated}</td>
              <td className="px-4 py-3">
                <button className="vela-focus rounded-lg border border-border px-3 py-1.5 text-xs font-semibold hover:bg-surface-muted">Mở</button>
              </td>
            </tr>
          );
        })}
      </DataTable>
    </SectionFrame>
  );
}

// RankProgressCard sits in the admin ops row but rank/gamification is learner-facing content.
// Treatment: use warm accent for the rank eyebrow/heading (learning-coral), but keep the
// stats block neutral (no green — warning bar is fine as it shows remaining gap, not success).
function RankProgressCard() {
  return (
    <Panel className="overflow-hidden p-4">
      <div className="flex items-center gap-4">
        <RankEmblem />
        <div>
          {/* Warm eyebrow — rank belongs to the learner's identity */}
          <p className="text-sm font-bold text-learning-coral">Rank progress</p>
          <h2 className="mt-1 text-2xl font-black text-foreground">Rank {currentUser.rank}</h2>
          <p className="text-sm font-medium text-muted">Còn 180 điểm lên Bạch Kim</p>
        </div>
      </div>
      {/* Warning bar = shows the gap to next rank (not yet achieved = caution) */}
      <div className="mt-4">
        <ProgressBar value={68} tone="warning" />
      </div>
      <div className="mt-4 grid grid-cols-3 divide-x divide-border rounded-lg border border-border bg-surface-raised text-center">
        <div className="px-2 py-3">
          <p className="text-xs font-medium text-muted">Điểm tích lũy</p>
          <p className="font-mono text-lg font-extrabold">2.840</p>
        </div>
        <div className="px-2 py-3">
          <p className="text-xs font-medium text-muted">Chuỗi học</p>
          <p className="font-mono text-lg font-extrabold">12 ngày</p>
        </div>
        <div className="px-2 py-3">
          <p className="text-xs font-medium text-muted">Top phòng ban</p>
          <p className="font-mono text-lg font-extrabold">#3</p>
        </div>
      </div>
    </Panel>
  );
}
