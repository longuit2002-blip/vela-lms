"use client";

import { useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ShellIconButton, ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { DataTable, ProgressBar, StatusPill, type Tone } from "@/components/vela/ui";
import { ApiError, AuthRequiredError, getMyEnrollments, type EnrollmentSummary } from "@/lib/api";
import { currentUser } from "@/lib/mock-lms";
import { useRequireAuth } from "@/lib/use-auth";
import { LearnerRunway, type Checkpoint } from "./runway";
import { RoleLens } from "./role-lens";

// Static atmosphere data for the approved mock-A hero (decorative, not live metrics).
const checkpoints: Checkpoint[] = [
  { icon: "assigned", label: "Được giao", detail: "Khóa của bạn", tone: "warm" },
  { icon: "learning", label: "Đang học", detail: "Tiếp tục", tone: "primary" },
  { icon: "rank", label: "Rank", detail: "Sắp ra mắt", tone: "gold" },
  { icon: "complete", label: "Hoàn thành", detail: "Mục tiêu", tone: "success" },
];

function statusTone(status: string): Tone {
  if (status === "Completed") return "success";
  if (status === "InProgress") return "primary";
  return "warm";
}

function statusLabel(status: string): string {
  if (status === "Completed") return "Hoàn thành";
  if (status === "InProgress") return "Đang học";
  return "Chưa bắt đầu";
}

export function LearnerHome() {
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
          <ComingSoonCard title="Rank progress" note="Điểm và hạng sẽ xuất hiện khi tính năng gamification ra mắt." />
          <ComingSoonCard title="Mini leaderboard" note="Bảng xếp hạng theo phòng ban và chức vụ — sắp ra mắt." />
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
      <div
        className="absolute inset-0 -z-30 bg-cover bg-center"
        style={{ backgroundImage: "url('/vela-assets/learner/hero-runway-bg.png')" }}
        aria-hidden="true"
      />
      <div
        className="absolute inset-0 -z-20"
        style={{ background: "radial-gradient(125% 130% at 84% 32%, oklch(0.72 0.19 46 / 0.55), oklch(0.8 0.14 70 / 0.18) 42%, transparent 64%)" }}
        aria-hidden="true"
      />
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
      </div>

      <LearnerRunway checkpoints={checkpoints} />
    </section>
  );
}

function ComingSoonCard({ title, note }: { title: string; note: string }) {
  return (
    <section className="panel p-5 opacity-80">
      <div className="flex items-center justify-between">
        <h2 className="text-[11px] font-semibold uppercase tracking-[0.08em] text-subtle">{title}</h2>
        <StatusPill tone="warm">Sắp ra mắt</StatusPill>
      </div>
      <p className="mt-3 text-sm font-medium text-muted">{note}</p>
    </section>
  );
}

function LearningQueue() {
  const router = useRouter();
  const status = useRequireAuth();

  const { data: enrollments, isPending, isError, error } = useQuery({
    queryKey: ["my-enrollments"],
    queryFn: getMyEnrollments,
    enabled: status === "authenticated",
    retry: false,
  });

  useEffect(() => {
    if (isError && error instanceof AuthRequiredError) router.replace("/login");
    else if (isError && error instanceof ApiError && error.status === 403) router.replace("/change-password");
  }, [isError, error, router]);

  return (
    <section className="panel overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border bg-surface-raised px-5 py-4">
        <h2 className="text-base font-bold text-foreground">Khóa học của bạn</h2>
        <StatusPill tone="warm">Được giao</StatusPill>
      </div>

      {status === "checking" || isPending ? (
        <div className="grid gap-2 p-5" aria-label="Đang tải">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-14 animate-pulse rounded-lg bg-learning-apricot/50" />
          ))}
        </div>
      ) : isError ? (
        <div className="m-5 rounded-lg border border-danger/25 bg-danger-subtle p-4 text-sm font-medium text-danger" role="alert">
          Không tải được danh sách khóa học. Vui lòng thử lại.
        </div>
      ) : enrollments!.length === 0 ? (
        <div className="grid place-items-center gap-2 px-5 py-12 text-center">
          <p className="font-semibold text-foreground">Chưa có khóa học nào được giao</p>
          <p className="max-w-md text-sm font-medium text-muted">Khi quản trị đào tạo giao khóa học, khóa sẽ xuất hiện ở đây để bạn bắt đầu.</p>
        </div>
      ) : (
        <DataTable headers={["#", "Khóa học", "Trạng thái", "Tiến độ", ""]} minWidth="min-w-[640px]">
          {enrollments!.map((e: EnrollmentSummary, index: number) => (
            <tr key={e.enrollmentId} className="transition-colors hover:bg-surface-raised">
              <td className="px-4 py-3">
                <span className="grid size-10 place-items-center rounded-lg bg-primary font-mono text-sm font-bold text-white">{index + 1}</span>
              </td>
              <td className="px-4 py-3">
                <Link href={`/noi-dung/${e.enrollmentId}`} className="vela-focus font-semibold leading-snug text-foreground hover:text-primary">
                  {e.courseTitle}
                </Link>
              </td>
              <td className="px-4 py-3">
                <StatusPill tone={statusTone(e.status)}>{statusLabel(e.status)}</StatusPill>
              </td>
              <td className="px-4 py-3">
                <div className="min-w-32">
                  <div className="mb-2 flex justify-between text-xs font-medium">
                    <span className="text-muted">Tiến độ</span>
                    <span className="font-mono font-semibold text-primary">{e.progressPercent}%</span>
                  </div>
                  <ProgressBar value={e.progressPercent} tone="primary" />
                </div>
              </td>
              <td className="px-4 py-3 text-right">
                <Link
                  href={`/noi-dung/${e.enrollmentId}`}
                  className="vela-focus inline-flex rounded-lg px-3 py-2 text-sm font-semibold text-primary hover:bg-primary-subtle"
                >
                  Mở
                </Link>
              </td>
            </tr>
          ))}
        </DataTable>
      )}
    </section>
  );
}
