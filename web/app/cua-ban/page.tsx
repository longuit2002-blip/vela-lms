import { AssetIcon } from "@/components/vela/assets";
import { ShellIconButton, ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, DataTable, IconBadge, ProgressBar, RankEmblem, StatusPill, type Tone } from "@/components/vela/ui";
import { currentUser, leaderboard, learnerQueue } from "@/lib/mock-lms";
import { LearnerRunway, type Checkpoint } from "./runway";
import { OperationsBody } from "../_ops/ops-sections";
import { RoleLens } from "./role-lens";

// Data only — LearnerRunway samples the SVG path to place each node on the curve.
const checkpoints: Checkpoint[] = [
  { icon: "assigned", label: "Được giao", detail: "3 nhiệm vụ", tone: "warm" },
  { icon: "learning", label: "Đang học", detail: "Bài 3", tone: "primary" },
  { icon: "rank", label: "Rank Vàng", detail: "Còn 180 điểm", tone: "gold" },
  { icon: "complete", label: "Hoàn thành", detail: "Mục tiêu Q2", tone: "success" },
];

export default async function CuaBanPage({ searchParams }: { searchParams: Promise<{ role?: string }> }) {
  const sp = await searchParams;
  return sp.role === "operator" ? <OperatorHome /> : <LearnerHome />;
}

// Operator/admin view of the home: the shared operations composition (mock B), same body as
// /bao-cao. Switch back to the learner view via the role lens.
function OperatorHome() {
  return (
    <VelaAppShell active="Từ công ty" lens="L&D Admin" topbar={<OperatorTopbar />}>
      <OperationsBody />
    </VelaAppShell>
  );
}

function OperatorTopbar() {
  return (
    <div className="grid items-center gap-4 xl:grid-cols-[240px_minmax(320px,1fr)_auto]">
      <div>
        <h1 className="text-2xl font-extrabold leading-tight text-foreground">Từ công ty</h1>
        <p className="mt-1 text-sm font-medium text-muted">Vận hành đào tạo toàn tổ chức.</p>
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

function LearnerHome() {
  return (
    <VelaAppShell active="Từ công ty" lens="Học viên" topbar={<LearnerTopbar />}>
      <div className="workspace-page grid xl:grid-cols-[minmax(0,1fr)_356px]">
        <div className="min-w-0">
          <LearnerHero />
          <section className="workspace-pad">
            <LearningQueue />
          </section>
        </div>

        <aside className="grid content-start gap-4 border-t border-border bg-surface p-5 xl:border-l xl:border-t-0">
          <RankProgressCard />
          <MiniLeaderboard />
          <RiskCard />
        </aside>
      </div>
    </VelaAppShell>
  );
}

function LearnerTopbar() {
  return (
    <div className="flex items-center gap-4">
      <p className="inline-flex items-center gap-2.5 whitespace-nowrap text-sm font-semibold text-foreground">
        <span className="grid size-8 place-items-center rounded-full bg-learning-saffron text-base text-ink-rail">☀</span>
        Chào buổi sáng, {currentUser.name}
      </p>
      <ShellSearch placeholder="Tìm khóa học, người học, báo cáo..." className="hidden min-w-0 flex-1 md:flex" />
      <div className="ml-auto flex items-center gap-3">
        <RoleLens role="learner" />
        <ShellIconButton icon="bell" label="Thông báo" badge="3" />
        <ShellIconButton icon="help" label="Trợ giúp" />
        <button className="vela-focus min-h-10 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground">{currentUser.quarter}</button>
      </div>
    </div>
  );
}

function LearnerHero() {
  return (
    <section className="relative isolate min-h-[320px] overflow-hidden border-b border-border lg:min-h-[384px]">
      {/* warm light-field background */}
      <div
        className="absolute inset-0 -z-30 bg-cover bg-center"
        style={{ backgroundImage: "url('/vela-assets/learner/hero-runway-bg.png')" }}
        aria-hidden="true"
      />
      {/* saturation boost on the right so it reads vividly orange like the mock */}
      <div
        className="absolute inset-0 -z-20"
        style={{ background: "radial-gradient(125% 130% at 84% 32%, oklch(0.72 0.19 46 / 0.55), oklch(0.8 0.14 70 / 0.18) 42%, transparent 64%)" }}
        aria-hidden="true"
      />
      {/* warm-tinted left wash (keeps the headline legible without washing the field to white) */}
      <div
        className="absolute inset-0 -z-10"
        style={{ background: "linear-gradient(100deg, oklch(0.975 0.022 66 / 0.94) 0%, oklch(0.97 0.035 60 / 0.5) 34%, transparent 60%)" }}
        aria-hidden="true"
      />

      <div className="relative z-10 max-w-[38rem] px-6 py-9 sm:px-8 lg:px-10 lg:py-12">
        <h1 className="display-tight text-[1.95rem] font-black leading-[1.08] text-foreground sm:text-[2.2rem]">
          Hành trình học <br className="hidden sm:inline" />tiến mỗi ngày, <span className="text-learning-coral">vươn xa hơn.</span>
        </h1>
        <p className="mt-4 max-w-[34rem] text-sm font-medium leading-relaxed text-ink-rail/80">
          Học đúng phần được giao · Đúng tiến độ · Đạt rank · Tạo giá trị.
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          <ActionButton icon="learning">Mở bài tiếp theo</ActionButton>
          <ActionButton secondary>Xem lịch học</ActionButton>
        </div>
      </div>

      {/* runway: absolute right-side overlay on desktop, full-width strip below the text on mobile */}
      <LearnerRunway checkpoints={checkpoints} />
    </section>
  );
}

function RankProgressCard() {
  const rankBand = 1600;
  const inRank = rankBand - currentUser.pointsToNextRank; // progress within the current rank band
  const pct = Math.round((inRank / rankBand) * 100);
  return (
    <section className="panel-raised p-5">
      <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">Rank progress</p>
      <div className="mt-4 flex items-center gap-4">
        <span className="relative grid place-items-center">
          <img
            src="/vela-assets/learner/rank-badge-glow.png"
            alt=""
            aria-hidden="true"
            className="pointer-events-none absolute -z-10 w-[128%] max-w-none opacity-80"
          />
          <RankEmblem />
        </span>
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-2xl font-black text-foreground">{currentUser.rank}</p>
            <StatusPill tone="gold">Rank hiện tại</StatusPill>
          </div>
          <p className="mt-2 text-sm font-medium text-muted">Còn {currentUser.pointsToNextRank} điểm lên {currentUser.nextRank}</p>
        </div>
      </div>
      <div className="mt-5">
        <div className="mb-2 flex justify-between text-sm">
          <span className="font-mono font-semibold text-foreground">{inRank.toLocaleString("vi-VN")} / {rankBand.toLocaleString("vi-VN")} điểm</span>
          <span className="font-mono font-semibold text-primary">{pct}%</span>
        </div>
        <ProgressBar value={pct} tone="primary" />
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3">
        <div className="rounded-lg bg-learning-apricot px-3 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-ink-rail/70">Điểm tích lũy</p>
          <p className="mt-1 font-mono text-2xl font-extrabold text-ink-rail">{currentUser.points.toLocaleString("vi-VN")}</p>
        </div>
        <div className="rounded-lg border border-border bg-surface-raised px-3 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-subtle">Hạng của bạn</p>
          <p className="mt-1 font-mono text-2xl font-extrabold text-foreground">#38</p>
        </div>
      </div>
    </section>
  );
}

function MiniLeaderboard() {
  return (
    <section className="panel p-5">
      <h2 className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">Mini leaderboard</h2>
      <div className="mt-3 grid gap-1">
        {leaderboard.map((row) => (
          <div
            key={row.rank}
            className={`grid grid-cols-[30px_1fr_auto] items-center gap-3 rounded-lg px-2 py-2 text-sm ${row.medal === "self" ? "bg-learning-apricot" : ""}`}
          >
            <span className={`grid size-7 place-items-center rounded-full font-mono text-xs font-bold ${medalBg(row.medal)}`}>{row.rank}</span>
            <span className="min-w-0">
              <span className="block truncate font-semibold text-foreground">{row.name}</span>
              <span className="text-xs font-medium text-muted">{row.team}</span>
            </span>
            <span className="font-mono font-semibold text-foreground">{row.points}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function RiskCard() {
  return (
    <section className="panel border-danger/25 bg-danger-subtle p-5">
      <h2 className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">Rủi ro học tập</h2>
      <div className="mt-3 grid grid-cols-[44px_1fr] gap-3">
        <IconBadge icon="alert" tone="danger" />
        <div>
          <p className="font-bold leading-snug text-danger">7 khóa học sắp quá hạn</p>
          <p className="mt-1 text-sm font-medium text-muted">Tỷ lệ hoàn thành trung bình: 62%</p>
        </div>
      </div>
      <ActionButton secondary tone="danger" className="mt-4 w-full border-danger/30 text-danger">
        Xem chi tiết
      </ActionButton>
    </section>
  );
}

function LearningQueue() {
  return (
    <section className="panel overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border bg-surface-raised px-5 py-4">
        <div className="flex flex-wrap items-center gap-3">
          <h2 className="text-base font-bold text-foreground">Learning guide</h2>
          <StatusPill tone="warm">Ưu tiên theo hạn gần</StatusPill>
        </div>
        <button className="vela-focus rounded-lg px-3 py-2 text-sm font-semibold text-primary hover:bg-primary-subtle">Xem tất cả khóa học</button>
      </div>
      <DataTable headers={["#", "Khóa học", "Scope", "Hạn nộp", "Bài đang mở", "Tiến độ", ""]} minWidth="min-w-[820px]">
        {learnerQueue.map((course) => (
          <tr key={course.id} className="transition-colors hover:bg-surface-raised">
            <td className="px-4 py-3">
              <span className="grid size-10 place-items-center rounded-lg bg-primary font-mono text-sm font-bold text-white">{course.id}</span>
            </td>
            <td className="px-4 py-3">
              <p className="font-semibold leading-snug text-foreground">{course.title}</p>
              <p className="mt-1 text-xs font-medium text-muted">{course.reason}</p>
            </td>
            <td className="px-4 py-3">
              <StatusPill tone="warm">{course.scope}</StatusPill>
            </td>
            <td className="px-4 py-3">
              <StatusPill tone={course.tone as Tone}>{course.due}</StatusPill>
            </td>
            <td className="px-4 py-3 text-sm font-medium text-muted">{course.module}</td>
            <td className="px-4 py-3">
              <div className="min-w-32">
                <div className="mb-2 flex justify-between text-xs font-medium">
                  <span className="text-muted">Tiến độ</span>
                  <span className={`font-mono font-semibold ${course.progress < 30 ? "text-danger" : "text-primary"}`}>{course.progress}%</span>
                </div>
                <ProgressBar value={course.progress} tone={course.progress < 30 ? "danger" : "primary"} />
              </div>
            </td>
            <td className="px-4 py-3 text-right text-lg font-bold text-subtle">›</td>
          </tr>
        ))}
      </DataTable>
    </section>
  );
}

function medalBg(medal: string) {
  if (medal === "gold") return "bg-rank-gold text-white";
  if (medal === "silver") return "bg-border-strong text-white";
  if (medal === "bronze") return "bg-learning-coral text-white";
  if (medal === "self") return "bg-primary text-white";
  return "bg-surface-muted text-muted";
}
