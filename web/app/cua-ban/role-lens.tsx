"use client";

// Role lens for the role-aware /cua-ban home. Toggles ?role between learner and operator.
// (Mock-level switch — real role would come from auth/permissions.)
import { useRouter } from "next/navigation";
import { AssetIcon } from "@/components/vela/assets";

export function RoleLens({ role }: { role: "learner" | "operator" }) {
  const router = useRouter();
  const next = role === "learner" ? "operator" : "learner";
  const label = role === "learner" ? "Học viên" : "L&D Admin";

  return (
    <button
      onClick={() => router.replace(`/cua-ban?role=${next}`, { scroll: false })}
      className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground"
      title="Đổi vai trò xem (learner / vận hành)"
    >
      <AssetIcon name="users" className="size-4 text-subtle" />
      <span className="text-subtle">Lens</span> {label}
      <span className="text-subtle">⇄</span>
    </button>
  );
}
