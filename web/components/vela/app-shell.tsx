import Link from "next/link";
import type { ReactNode } from "react";
import { AssetIcon, VelaLogoMark } from "@/components/vela/assets";
import { currentUser } from "@/lib/mock-lms";
import type { VisualIconName } from "@/lib/visual-assets";

const primaryNav: Array<{ href: string; label: string; code: VisualIconName; role: string }> = [
  { href: "/cua-ban", label: "Từ công ty", code: "home", role: "Learner" },
  { href: "/bao-cao", label: "Báo cáo", code: "report", role: "L&D" },
  { href: "/thanh-vien", label: "Thành viên", code: "users", role: "L&D" },
  { href: "/xuat-ban", label: "Xuất bản", code: "publish", role: "L&D" },
  { href: "/design-poc", label: "PoC", code: "poc", role: "Design" },
];

const adminItems = [
  { label: "Khung giờ", icon: "data" },
  { label: "Danh mục", icon: "document" },
  { label: "Cấu hình", icon: "scope" },
  { label: "Nhật ký", icon: "branch" },
] as const;

const quickActions = [
  { label: "Giao khóa học", icon: "assigned" },
  { label: "Tạo báo cáo", icon: "report" },
  { label: "Xuất bản nhanh", icon: "publish" },
] as const;

export function VelaAppShell({
  children,
  active,
  lens = "L&D Admin",
  topbar,
  showTopbar = true,
}: {
  children: ReactNode;
  active: string;
  lens?: string;
  topbar?: ReactNode;
  showTopbar?: boolean;
}) {
  const activeRoute = primaryNav.find((item) => item.label === active);

  return (
    <main className="min-h-screen text-foreground">
      <div className="shell-rail text-white lg:hidden">
        <div className="flex items-center justify-between gap-4 px-4 py-3">
          <Link href="/cua-ban" className="vela-focus flex items-center gap-2 rounded-lg">
            <VelaLogoMark size={38} />
            <span className="grid leading-tight">
              <span className="text-lg font-black">Vela</span>
              <span className="text-xs font-semibold text-white/70">Training OS</span>
            </span>
          </Link>
          <div className="text-right">
            <p className="text-xs font-black text-white">{currentUser.organization}</p>
            <p className="text-[11px] font-semibold text-white/62">{activeRoute?.role ?? lens}</p>
          </div>
        </div>
        <nav className="flex gap-2 overflow-x-auto border-t border-white/10 px-3 py-3" aria-label="Điều hướng chính mobile">
          {primaryNav.map((item) => {
            const isActive = active === item.label;

            return (
              <Link
                key={item.href}
                href={item.href}
                className={`vela-focus inline-flex min-h-10 shrink-0 items-center gap-2 rounded-lg px-3 text-xs font-black ${
                  isActive ? "bg-white text-ink-rail" : "bg-white/8 text-white/76"
                }`}
              >
                <AssetIcon name={item.code} className="size-4" />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </div>

      <div className="min-h-screen lg:grid lg:grid-cols-[244px_minmax(0,1fr)]">
        <aside className="shell-rail hidden min-h-screen text-white lg:block">
          <div className="border-b border-white/10 px-4 py-5">
            <Link href="/cua-ban" className="vela-focus flex items-center gap-3 rounded-lg">
              <VelaLogoMark size={52} />
              <span className="grid leading-tight">
                <span className="text-2xl font-black">Vela</span>
                <span className="text-sm font-semibold text-white/72">Training OS</span>
              </span>
            </Link>

            <section className="mt-5 rounded-lg border border-white/12 bg-white/[0.055] p-3">
              <p className="text-[11px] font-extrabold uppercase text-white/48">Org lens</p>
              <div className="mt-3 grid gap-3">
                <button className="vela-focus flex min-h-12 items-center justify-between rounded-lg bg-white/[0.075] px-3 text-left">
                  <span>
                    <span className="block text-sm font-black">{currentUser.organization}</span>
                    <span className="text-xs font-semibold text-white/62">Tenant · betterwork.vn</span>
                  </span>
                  <span className="text-white/64">⌄</span>
                </button>
                <button className="vela-focus flex min-h-12 items-center justify-between rounded-lg bg-white/[0.075] px-3 text-left">
                  <span className="flex items-center gap-3">
                    <span className="grid size-9 place-items-center rounded-lg bg-learning-coral text-xs font-black text-white">NL</span>
                    <span>
                      <span className="block text-sm font-black">{currentUser.name}</span>
                      <span className="text-xs font-semibold text-white/62">{lens} · HRD</span>
                    </span>
                  </span>
                  <span className="text-white/64">⌄</span>
                </button>
              </div>
            </section>
          </div>

          <nav className="grid gap-1 px-3 py-4" aria-label="Điều hướng chính">
            <p className="px-2 pb-2 text-[11px] font-extrabold uppercase text-white/44">Điều hướng</p>
            {primaryNav.map((item) => {
              const isActive = active === item.label;

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`vela-focus grid min-h-11 grid-cols-[30px_1fr_auto] items-center gap-2 rounded-lg px-3 text-sm font-black transition-colors ${
                    isActive ? "bg-primary text-white" : "text-white/74 hover:bg-white/10 hover:text-white"
                  }`}
                >
                  <AssetIcon name={item.code} className="size-5" />
                  <span>{item.label}</span>
                  <span className="text-xs text-white/44">{isActive ? "›" : ""}</span>
                </Link>
              );
            })}
          </nav>

          <div className="px-3 pb-4">
            <p className="px-2 pb-2 text-[11px] font-extrabold uppercase text-white/44">Quản trị</p>
            <div className="grid gap-1">
              {adminItems.map((item) => (
                <button
                  key={item.label}
                  className="vela-focus grid min-h-10 grid-cols-[28px_1fr] items-center gap-2 rounded-lg px-3 text-left text-xs font-bold text-white/68 transition-colors hover:bg-white/10 hover:text-white"
                >
                  <AssetIcon name={item.icon} className="size-4" />
                  {item.label}
                </button>
              ))}
            </div>
          </div>

          <div className="mx-3 mt-1 rounded-lg border border-white/10 bg-white/[0.055] p-3">
            <p className="text-xs font-extrabold text-white/70">Quick actions</p>
            <div className="mt-3 grid grid-cols-3 gap-2">
              {quickActions.map((item) => (
                <button key={item.label} className="vela-focus grid min-h-[66px] place-items-center rounded-lg border border-white/10 px-1 text-center text-[11px] font-bold text-white/74">
                  <AssetIcon name={item.icon} className="size-4" />
                  <span>{item.label}</span>
                </button>
              ))}
            </div>
          </div>
        </aside>

        <section className="min-w-0">
          {showTopbar ? (
            <header className="sticky top-0 z-20 hidden border-b border-border bg-background/92 px-4 py-3 backdrop-blur sm:px-6 lg:block">
              {topbar ?? <DefaultTopbar />}
            </header>
          ) : null}

          <div className="route-enter">{children}</div>
        </section>
      </div>
    </main>
  );
}

export function ShellSearch({
  placeholder = "Tìm khóa học, người học, báo cáo, tài liệu...",
  className = "",
}: {
  placeholder?: string;
  className?: string;
}) {
  return (
    <div className={`min-w-0 items-center gap-3 rounded-lg border border-border bg-surface px-4 py-2.5 text-sm font-semibold text-muted md:flex ${className}`}>
      <AssetIcon name="scope" className="size-4 shrink-0 text-primary" />
      <span className="truncate">{placeholder}</span>
      <span className="ml-auto rounded-md bg-surface-muted px-2 py-0.5 font-mono text-xs text-subtle">⌘ K</span>
    </div>
  );
}

export function ShellIconButton({
  icon,
  label,
  badge,
}: {
  icon: VisualIconName;
  label: string;
  badge?: string;
}) {
  return (
    <button className="vela-focus relative grid size-10 place-items-center rounded-lg border border-border bg-surface text-foreground" aria-label={label}>
      <AssetIcon name={icon} className="size-4" />
      {badge ? <span className="absolute -right-1 -top-1 grid size-5 place-items-center rounded-full bg-danger text-[10px] font-black text-white">{badge}</span> : null}
    </button>
  );
}

function DefaultTopbar() {
  return (
    <div className="flex items-center justify-between gap-4">
      <ShellSearch className="hidden flex-1 md:flex" />
      <div className="ml-auto flex items-center gap-3">
        <ShellIconButton icon="bell" label="Thông báo" badge="3" />
        <ShellIconButton icon="help" label="Trợ giúp" />
        <button className="vela-focus min-h-10 rounded-lg border border-border bg-surface px-3 text-sm font-black text-foreground">{currentUser.quarter}</button>
        <div className="grid size-10 place-items-center rounded-full bg-teal-subtle text-sm font-black text-teal">{currentUser.initials}</div>
      </div>
    </div>
  );
}
