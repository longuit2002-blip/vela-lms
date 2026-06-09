"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ApiError, login } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const session = await login(email, password);
      router.replace(session.mustChangePassword ? "/change-password" : "/organizations");
    } catch (err) {
      // Enumeration-safe: the API returns the same message for wrong password vs unknown email.
      setError(err instanceof ApiError ? err.message : "Không thể đăng nhập. Vui lòng thử lại.");
      setSubmitting(false);
    }
  }

  return (
    <main className="grid min-h-screen bg-background text-foreground lg:grid-cols-[1.05fr_.95fr]">
      <section className="flex min-h-[38vh] flex-col justify-between bg-[#17243a] px-6 py-8 text-white sm:px-10 lg:min-h-screen lg:px-14">
        <div className="flex items-center gap-3">
          <div className="grid size-10 place-items-center rounded-lg bg-white text-sm font-bold text-primary">V</div>
          <div>
            <p className="text-base font-semibold leading-5">Vela</p>
            <p className="text-xs text-blue-100">LMS doanh nghiệp</p>
          </div>
        </div>

        <div className="max-w-xl py-10">
          <p className="mb-4 inline-flex rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-blue-50">
            Multi-tenant learning operations
          </p>
          <h1 className="text-3xl font-bold leading-tight text-white sm:text-4xl">
            Theo dõi đào tạo, nội dung và quyền truy cập trong một bề mặt làm việc.
          </h1>
          <p className="mt-5 max-w-lg text-sm leading-6 text-blue-100">
            Đăng nhập để tiếp tục vận hành chương trình học, báo cáo tiến độ và quản lý dữ liệu theo tổ chức.
          </p>
        </div>

        <dl className="grid gap-3 text-sm sm:grid-cols-3">
          <div className="rounded-lg border border-white/15 bg-white/10 p-4">
            <dt className="text-blue-100">Phạm vi</dt>
            <dd className="mt-1 font-semibold text-white">Tổ chức</dd>
          </div>
          <div className="rounded-lg border border-white/15 bg-white/10 p-4">
            <dt className="text-blue-100">Bảo mật</dt>
            <dd className="mt-1 font-semibold text-white">RBAC + ABAC</dd>
          </div>
          <div className="rounded-lg border border-white/15 bg-white/10 p-4">
            <dt className="text-blue-100">Tiến độ</dt>
            <dd className="mt-1 font-semibold text-white">Rank + KPI</dd>
          </div>
        </dl>
      </section>

      <section className="flex items-center justify-center px-6 py-10 sm:px-10">
        <div className="w-full max-w-md">
          <header className="mb-7">
            <p className="text-sm font-medium text-primary">Tài khoản tổ chức</p>
            <h2 className="mt-2 text-2xl font-bold text-foreground">Đăng nhập vào Vela</h2>
            <p className="mt-2 text-sm text-muted">Dùng email và mật khẩu do tổ chức cấp.</p>
          </header>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-6 shadow-[var(--shadow-panel)]">
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-foreground">Email công việc</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="username"
                className="vela-focus rounded-lg border border-border bg-white px-3 py-2.5 text-foreground transition-colors placeholder:text-subtle hover:border-border-strong"
                placeholder="ban@congty.com"
              />
            </label>

            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-foreground">Mật khẩu</span>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
                className="vela-focus rounded-lg border border-border bg-white px-3 py-2.5 text-foreground transition-colors hover:border-border-strong"
              />
            </label>

            {error && (
              <p role="alert" className="rounded-lg border border-red-200 bg-danger-subtle px-3 py-2 text-sm text-danger">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="vela-focus mt-2 rounded-lg bg-primary px-4 py-2.5 font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting ? "Đang đăng nhập..." : "Đăng nhập"}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}
