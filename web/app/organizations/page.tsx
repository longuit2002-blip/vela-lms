"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ApiError, AuthRequiredError, getMyOrganization, logout } from "@/lib/api";
import { useRequireAuth } from "@/lib/use-auth";

const adminItems = ["Tổng quan", "Cây tổ chức", "Vai trò & quyền", "Thiết lập bảo mật"];
const setupSteps = [
  "Tạo tenant và tài khoản quản trị",
  "Seed vai trò hệ thống",
  "Thiết lập cây phòng ban",
  "Import người dùng",
  "Xuất bản khóa học đầu tiên",
];
const scopes = [
  ["INTERNAL", "Nhân sự nội bộ", "bg-primary-subtle text-primary"],
  ["PARTNER", "Đối tác B2B", "bg-warning-subtle text-warning"],
  ["GUEST", "Khách học công khai", "bg-success-subtle text-success"],
];

export default function MyOrganizationPage() {
  const router = useRouter();
  const status = useRequireAuth();

  const { data: organization, isPending, isError, error } = useQuery({
    queryKey: ["my-organization"],
    queryFn: getMyOrganization,
    enabled: status === "authenticated",
    retry: false,
  });

  // Route based on auth/gate failures surfaced by the query.
  useEffect(() => {
    if (!isError) return;
    if (error instanceof AuthRequiredError) {
      router.replace("/login");
    } else if (error instanceof ApiError && error.status === 403) {
      // Forced first-login password change gate.
      router.replace("/change-password");
    }
  }, [isError, error, router]);

  async function handleSignOut() {
    await logout();
    router.replace("/login");
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <header className="sticky top-0 z-20 border-b border-border bg-surface/95 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="grid size-10 place-items-center rounded-lg bg-primary text-sm font-bold text-white">V</div>
            <div>
              <p className="font-semibold leading-5">Vela</p>
              <p className="text-xs text-muted">Training OS</p>
            </div>
          </div>

          <nav className="hidden items-center gap-1 rounded-lg bg-surface-muted p-1 text-sm font-medium text-muted md:flex" aria-label="Điều hướng chính">
            <a className="rounded-md bg-surface px-3 py-1.5 text-foreground shadow-sm" href="/organizations">Tổ chức</a>
            <span className="px-3 py-1.5">Nội dung</span>
            <span className="px-3 py-1.5">Báo cáo</span>
            <span className="px-3 py-1.5">Thành viên</span>
          </nav>

          <button
            type="button"
            onClick={handleSignOut}
            className="vela-focus rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold text-foreground transition-colors hover:border-border-strong hover:bg-surface-muted"
          >
            Đăng xuất
          </button>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[260px_1fr]">
        <aside className="hidden rounded-xl border border-border bg-surface p-3 shadow-sm lg:block">
          <p className="px-3 py-2 text-xs font-semibold uppercase tracking-wide text-subtle">Quản trị</p>
          {adminItems.map((item, index) => (
            <button
              key={item}
              type="button"
              className={`vela-focus mt-1 flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm font-medium transition-colors ${
                index === 0 ? "bg-primary-subtle text-primary" : "text-muted hover:bg-surface-muted hover:text-foreground"
              }`}
            >
              <span>{item}</span>
              {index === 0 ? <span className="size-2 rounded-full bg-primary" aria-hidden="true" /> : null}
            </button>
          ))}
        </aside>

        <section className="flex flex-col gap-6">
          <div className="grid gap-4 rounded-xl border border-border bg-surface p-5 shadow-sm lg:grid-cols-[1fr_auto] lg:items-center">
            <div>
              <p className="text-sm font-medium text-primary">Không gian tổ chức</p>
              <h1 className="mt-1 text-2xl font-bold tracking-normal text-foreground">Tổng quan tenant</h1>
              <p className="mt-2 max-w-2xl text-sm text-muted">
                Kiểm tra định danh tổ chức, trạng thái vận hành và các mốc cấu hình nền tảng trước khi mở rộng người dùng.
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <span className="rounded-full bg-success-subtle px-3 py-1 text-sm font-medium text-success">Tenant isolated</span>
              <span className="rounded-full bg-primary-subtle px-3 py-1 text-sm font-medium text-primary">RBAC ready</span>
            </div>
          </div>

          {status === "checking" || isPending ? (
            <section className="grid gap-4 md:grid-cols-3" aria-label="Đang tải">
              {[0, 1, 2].map((item) => (
                <div key={item} className="h-32 animate-pulse rounded-xl border border-border bg-surface" />
              ))}
            </section>
          ) : isError ? (
            <section className="rounded-xl border border-red-200 bg-danger-subtle p-5 text-sm text-danger" role="alert">
              Không tải được thông tin tổ chức. Vui lòng thử đăng nhập lại hoặc kiểm tra quyền truy cập.
            </section>
          ) : (
            <>
              <section className="grid gap-4 md:grid-cols-3">
                <article className="rounded-xl border border-border bg-surface p-5 shadow-sm">
                  <p className="text-sm font-medium text-muted">Tên tổ chức</p>
                  <p className="mt-2 text-xl font-bold text-foreground">{organization.name}</p>
                  <p className="mt-3 text-sm text-muted">Định danh hiển thị trong các luồng quản trị và báo cáo.</p>
                </article>
                <article className="rounded-xl border border-border bg-surface p-5 shadow-sm">
                  <p className="text-sm font-medium text-muted">Slug tenant</p>
                  <p className="mt-2 font-mono text-xl font-semibold text-foreground">{organization.slug}</p>
                  <p className="mt-3 text-sm text-muted">Dùng cho cấu hình route, tích hợp và hỗ trợ vận hành.</p>
                </article>
                <article className="rounded-xl border border-border bg-surface p-5 shadow-sm">
                  <p className="text-sm font-medium text-muted">Trạng thái</p>
                  <p className="mt-2 inline-flex rounded-full bg-success-subtle px-3 py-1 text-sm font-semibold text-success">
                    {organization.status}
                  </p>
                  <p className="mt-3 text-sm text-muted">Tenant đang sẵn sàng cho các tác vụ LMS cốt lõi.</p>
                </article>
              </section>

              <section className="grid gap-4 lg:grid-cols-[1.2fr_.8fr]">
                <article className="rounded-xl border border-border bg-surface p-5 shadow-sm">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <h2 className="text-lg font-bold text-foreground">Lộ trình cấu hình</h2>
                      <p className="mt-1 text-sm text-muted">Các bước nền tảng trước khi đưa đào tạo vào vận hành.</p>
                    </div>
                    <span className="rounded-full bg-warning-subtle px-3 py-1 text-sm font-medium text-warning">3/5 hoàn tất</span>
                  </div>
                  <div className="mt-5 h-2 overflow-hidden rounded-full bg-surface-muted">
                    <div className="progress-fill h-full w-3/5 rounded-full bg-primary" />
                  </div>
                  <ol className="mt-5 grid gap-3 text-sm">
                    {setupSteps.map((step, index) => (
                      <li key={step} className="flex items-center gap-3 rounded-lg border border-border bg-surface-raised px-3 py-2">
                        <span className={`grid size-6 place-items-center rounded-full text-xs font-bold ${
                          index < 3 ? "bg-primary text-white" : "bg-surface-muted text-muted"
                        }`}>
                          {index + 1}
                        </span>
                        <span className={index < 3 ? "font-medium text-foreground" : "text-muted"}>{step}</span>
                      </li>
                    ))}
                  </ol>
                </article>

                <article className="rounded-xl border border-border bg-surface p-5 shadow-sm">
                  <h2 className="text-lg font-bold text-foreground">Phạm vi truy cập</h2>
                  <p className="mt-1 text-sm text-muted">Vela phân quyền theo role, audience scope và nhánh phòng ban.</p>
                  <div className="mt-5 space-y-3">
                    {scopes.map(([code, label, tone]) => (
                      <div key={code} className="flex items-center justify-between rounded-lg border border-border px-3 py-2">
                        <div>
                          <p className="font-mono text-sm font-semibold text-foreground">{code}</p>
                          <p className="text-sm text-muted">{label}</p>
                        </div>
                        <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${tone}`}>Scope</span>
                      </div>
                    ))}
                  </div>
                </article>
              </section>
            </>
          )}
        </section>
      </div>
    </main>
  );
}
