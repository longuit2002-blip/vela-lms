// /bao-cao (reports) — admin register. Renders the shared operations composition (mock B); the
// same body powers the operator view of the role-aware /cua-ban home.
import { ShellSearch, VelaAppShell } from "@/components/vela/app-shell";
import { AssetIcon } from "@/components/vela/assets";
import { OperationsBody } from "../_ops/ops-sections";

export default function ReportsPage() {
  return (
    <VelaAppShell active="Báo cáo" lens="L&D Admin" topbar={<Topbar />}>
      <OperationsBody />
    </VelaAppShell>
  );
}

function Topbar() {
  return (
    <div className="grid items-center gap-4 xl:grid-cols-[240px_minmax(320px,1fr)_auto]">
      <div>
        <h1 className="text-2xl font-extrabold leading-tight text-foreground">Báo cáo</h1>
        <p className="mt-1 text-sm font-medium text-muted">Số liệu & tracking đào tạo toàn tổ chức.</p>
      </div>
      <ShellSearch placeholder="Tìm khóa học, người học, báo cáo..." className="hidden md:flex" />
      <div className="flex flex-wrap justify-end gap-2">
        <button className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 text-sm font-semibold text-foreground">
          <AssetIcon name="users" className="size-4 text-subtle" />
          <span className="text-subtle">Lens</span> L&D Admin
        </button>
        <button className="vela-focus inline-flex min-h-10 items-center gap-2 rounded-lg border border-border bg-surface px-3 font-mono text-sm font-semibold text-foreground">
          <AssetIcon name="data" className="size-4 text-subtle" />
          01/06–30/06/2026
        </button>
      </div>
    </div>
  );
}
