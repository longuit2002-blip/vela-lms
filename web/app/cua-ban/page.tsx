import { AssetIcon } from "@/components/vela/assets";
import { ShellIconButton, VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, DataTable, IconBadge, ProgressBar, RankEmblem, StatusPill, type Tone } from "@/components/vela/ui";
import { currentUser, leaderboard, learnerQueue } from "@/lib/mock-lms";

const checkpoints = [
  { icon: "assigned", label: "Được giao", detail: "3 nhiệm vụ", position: "left-[28%] top-[76%]" },
  { icon: "learning", label: "Đang học", detail: "Bài 3", position: "left-[50%] top-[58%]" },
  { icon: "rank", label: "Rank Vàng", detail: "Còn 180 điểm", position: "left-[72%] top-[43%]" },
  { icon: "complete", label: "Hoàn thành", detail: "Mục tiêu", position: "left-[92%] top-[28%]" },
] as const;

export default function LearnerDashboardPage() {
  return (
    <VelaAppShell active="Từ công ty" lens="Học viên" topbar={<LearnerTopbar />}>
      <div className="workspace-page grid xl:grid-cols-[minmax(0,1fr)_344px]">
        <div className="min-w-0">
          <LearnerHero />
          <MetricBand />
          <section className="workspace-pad">
            <LearningQueue />
          </section>
        </div>

        <aside className="grid content-start gap-4 border-l border-border bg-surface p-5">
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
    <div className="flex items-center justify-between gap-4">
      <p className="inline-flex items-center gap-3 text-sm font-semibold text-foreground">
        <span className="grid size-8 place-items-center rounded-full bg-learning-saffron text-lg">☼</span>
        Chào buổi sáng, {currentUser.name}
      </p>
      <div className="ml-auto flex items-center gap-3">
        <ShellIconButton icon="bell" label="Thông báo" badge="3" />
        <ShellIconButton icon="help" label="Trợ giúp" />
        <button className="vela-focus min-h-10 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground">{currentUser.quarter}</button>
      </div>
    </div>
  );
}

function LearnerHero() {
  return (
    <section className="learner-hero-field relative min-h-[392px] overflow-hidden border-b border-border">
      <div className="relative z-10 max-w-[590px] px-5 py-10 sm:px-8 lg:px-10 lg:py-14">
        <h1 className="display-tight text-4xl font-black leading-[1.08] text-foreground sm:text-5xl">
          Hành trình học tiến mỗi ngày, <span className="text-learning-coral">vươn xa hơn.</span>
        </h1>
        <p className="mt-5 max-w-[460px] text-base font-semibold text-muted">Học đúng phần được giao · Đúng tiến độ · Đạt rank · Tạo giá trị</p>
        <div className="mt-7 flex flex-wrap gap-3">
          <ActionButton icon="learning">Mở bài tiếp theo</ActionButton>
          <ActionButton secondary>Xem lịch học</ActionButton>
        </div>
      </div>

      <div className="absolute inset-y-0 right-2 z-20 hidden w-[50%] lg:block">
        {checkpoints.map((checkpoint) => (
          <div key={checkpoint.label} className={`absolute grid -translate-x-1/2 -translate-y-1/2 justify-items-center gap-2 text-center ${checkpoint.position}`}>
            <span className="grid size-16 place-items-center rounded-full border-4 border-white/70 bg-surface text-primary shadow-[0_0_18px_oklch(1_0_0/.44)]">
              <AssetIcon name={checkpoint.icon} className="size-6" />
            </span>
            <span className="text-sm font-bold text-white drop-shadow">{checkpoint.label}</span>
            <span className="rounded-full bg-white/34 px-3 py-1 text-xs font-semibold text-white backdrop-blur">{checkpoint.detail}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function MetricBand() {
  return (
    <section className="grid grid-cols-2 border-b border-border bg-surface px-4 sm:grid-cols-5 sm:px-6">
      {[
        ["Tiến độ tổng", "78%", "+8% tuần này", "teal"],
        ["Giờ học YTD", "42,5", "+3,1 giờ", "teal"],
        ["Hoàn thành", "18", "khóa học", "success"],
        ["Đang học", "3", "khóa học", "primary"],
        ["Sắp đến hạn", "2", "nhiệm vụ", "warning"],
      ].map(([label, value, detail, tone]) => (
        <div key={label} className="border-b border-border py-4 sm:border-b-0 sm:border-r sm:px-4 last:border-r-0">
          <p className="text-xs font-semibold uppercase tracking-wide text-subtle">{label}</p>
          <p className={`mt-1 font-mono text-2xl font-black leading-none ${metricTone(tone)}`}>{value}</p>
          <p className="mt-1 text-xs font-medium text-muted">{detail}</p>
        </div>
      ))}
    </section>
  );
}

function RankProgressCard() {
  return (
    <section className="panel-raised p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-subtle">Rank Progress</p>
      <div className="mt-4 flex items-center gap-4">
        <RankEmblem />
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-2xl font-black text-foreground">{currentUser.rank}</p>
            <StatusPill tone="warning">Rank hiện tại</StatusPill>
          </div>
          <p className="mt-2 text-sm font-semibold text-muted">Còn {currentUser.pointsToNextRank} điểm lên {currentUser.nextRank}</p>
        </div>
      </div>
      <div className="mt-4">
        <div className="mb-2 flex justify-between text-sm font-medium">
          <span className="font-mono">{currentUser.points.toLocaleString("vi-VN")} / 1.600 điểm</span>
          <span className="text-muted">68%</span>
        </div>
        <ProgressBar value={68} tone="warning" />
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3">
        <div className="rounded-lg bg-learning-apricot px-3 py-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted">Điểm tích lũy</p>
          <p className="font-mono text-2xl font-black">{currentUser.points.toLocaleString("vi-VN")}</p>
        </div>
        <div className="rounded-lg bg-surface-raised px-3 py-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted">Hạng của bạn</p>
          <p className="font-mono text-2xl font-black">#38</p>
        </div>
      </div>
    </section>
  );
}

function MiniLeaderboard() {
  return (
    <section className="panel p-4">
      <h2 className="text-xs font-semibold uppercase tracking-wide text-subtle">Mini Leaderboard</h2>
      <div className="mt-3 grid gap-1">
        {leaderboard.map((row) => (
          <div key={row.rank} className={`grid grid-cols-[32px_1fr_auto] items-center gap-3 rounded-lg px-2 py-2 text-sm ${row.medal === "self" ? "bg-learning-apricot" : ""}`}>
            <span className="grid size-7 place-items-center rounded-full bg-surface-muted font-mono text-xs font-semibold text-muted">{row.rank}</span>
            <span>
              <span className="block font-bold text-foreground">{row.name}</span>
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
    <section className="panel border-danger/25 bg-danger-subtle p-4">
      <h2 className="text-xs font-semibold uppercase tracking-wide text-subtle">Rủi ro đào tạo</h2>
      <div className="mt-3 grid grid-cols-[44px_1fr] gap-3">
        <IconBadge icon="alert" tone="danger" />
        <div>
          <p className="font-bold text-danger">7 khóa học sắp quá hạn</p>
          <p className="mt-1 text-sm font-medium text-muted">Tỷ lệ hoàn thành trung bình: 62%</p>
        </div>
      </div>
      <ActionButton secondary tone="danger" className="mt-4 border-danger/30 text-danger">
        Xem chi tiết
      </ActionButton>
    </section>
  );
}

function LearningQueue() {
  return (
    <section className="panel overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border bg-surface-raised px-4 py-3">
        <div className="flex flex-wrap items-center gap-3">
          <h2 className="text-lg font-bold text-foreground">Learning queue</h2>
          <StatusPill tone="warm">Ưu tiên theo hạn gần</StatusPill>
        </div>
        <button className="vela-focus rounded-lg px-3 py-2 text-sm font-semibold text-primary hover:bg-primary-subtle">Xem tất cả khóa học</button>
      </div>
      <DataTable headers={["#", "Khóa học", "Scope", "Hạn nộp", "Bài đang mở", "Tiến độ", ""]} minWidth="min-w-[820px]">
        {learnerQueue.map((course) => (
          <tr key={course.id} className="transition-colors hover:bg-surface-raised">
            <td className="px-4 py-3">
              <span className="grid size-11 place-items-center rounded-lg bg-ink-rail font-mono text-sm font-black text-white">{course.id}</span>
            </td>
            <td className="px-4 py-3">
              <p className="font-bold leading-snug text-foreground">{course.title}</p>
              <p className="mt-1 text-xs font-medium text-muted">{course.reason}</p>
            </td>
            <td className="px-4 py-3">
              <StatusPill tone="teal">{course.scope}</StatusPill>
            </td>
            <td className="px-4 py-3">
              <StatusPill tone={course.tone as Tone}>{course.due}</StatusPill>
            </td>
            <td className="px-4 py-3 font-semibold text-muted">{course.module}</td>
            <td className="px-4 py-3">
              <div className="min-w-32">
                <div className="mb-2 flex justify-between text-xs font-medium">
                  <span className="text-muted">Tiến độ</span>
                  <span className={`font-semibold ${course.progress < 30 ? "text-danger" : "text-teal"}`}>{course.progress}%</span>
                </div>
                <ProgressBar value={course.progress} tone={course.tone as Tone} />
              </div>
            </td>
            <td className="px-4 py-3 text-right text-xl font-black text-muted">›</td>
          </tr>
        ))}
      </DataTable>
    </section>
  );
}

function metricTone(tone: string) {
  if (tone === "success") return "text-success";
  if (tone === "warning") return "text-warning";
  if (tone === "primary") return "text-primary";
  return "text-teal";
}
