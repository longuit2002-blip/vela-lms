"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ApiError, AuthRequiredError, changePassword } from "@/lib/api";
import { useRequireAuth } from "@/lib/use-auth";

export default function ChangePasswordPage() {
  const router = useRouter();
  const status = useRequireAuth(); // accessible to any authenticated user; redirects if not signed in
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (status === "checking") {
    return <main className="mx-auto max-w-sm px-6 py-10 text-sm text-muted">Đang kiểm tra phiên đăng nhập...</main>;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await changePassword(currentPassword, newPassword);
      router.replace("/organizations");
    } catch (err) {
      if (err instanceof AuthRequiredError) {
        router.replace("/login");
        return;
      }
      // 401 means current password is wrong; 422 carries new-password validation details.
      setError(err instanceof ApiError ? err.message : "Không thể lưu mật khẩu. Vui lòng thử lại.");
      setSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 py-10">
      <section className="grid w-full max-w-5xl overflow-hidden rounded-xl border border-border bg-surface shadow-[var(--shadow-panel)] lg:grid-cols-[.9fr_1.1fr]">
        <aside className="bg-surface-muted p-6 sm:p-8">
          <div className="flex items-center gap-3">
            <div className="grid size-10 place-items-center rounded-lg bg-primary text-sm font-bold text-white">V</div>
            <div>
              <p className="font-semibold text-foreground">Vela</p>
              <p className="text-xs text-muted">Bảo vệ tài khoản</p>
            </div>
          </div>

          <div className="mt-10">
            <p className="text-sm font-medium text-primary">Yêu cầu bảo mật</p>
            <h1 className="mt-2 text-2xl font-bold leading-tight text-foreground">Đổi mật khẩu để tiếp tục</h1>
            <p className="mt-3 text-sm leading-6 text-muted">
              Tài khoản tổ chức có thể được cấp bằng mật khẩu mặc định. Cập nhật mật khẩu giúp giữ quyền truy cập và dữ liệu đào tạo an toàn.
            </p>
          </div>

          <ul className="mt-8 space-y-3 text-sm text-muted">
            <li className="rounded-lg border border-border bg-surface px-3 py-2">Ít nhất 8 ký tự theo chính sách tổ chức.</li>
            <li className="rounded-lg border border-border bg-surface px-3 py-2">Không dùng lại mật khẩu mặc định.</li>
            <li className="rounded-lg border border-border bg-surface px-3 py-2">Phiên đăng nhập sẽ được cập nhật sau khi lưu.</li>
          </ul>
        </aside>

        <div className="p-6 sm:p-8 lg:p-10">
          <header className="mb-7">
            <h2 className="text-2xl font-bold text-foreground">Thiết lập mật khẩu mới</h2>
            <p className="mt-2 text-sm text-muted">Nhập mật khẩu hiện tại và mật khẩu mới của bạn.</p>
          </header>

          <form onSubmit={handleSubmit} className="flex max-w-md flex-col gap-4">
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-foreground">Mật khẩu hiện tại</span>
              <input
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                required
                autoComplete="current-password"
                className="vela-focus rounded-lg border border-border bg-white px-3 py-2.5 text-foreground transition-colors hover:border-border-strong"
              />
            </label>

            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-foreground">Mật khẩu mới</span>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
                autoComplete="new-password"
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
              {submitting ? "Đang lưu..." : "Lưu mật khẩu mới"}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}
