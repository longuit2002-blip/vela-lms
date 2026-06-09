// Admin register — neutral, data-forward surfaces with ORANGE as the single brand accent.
// Amber/red appear ONLY for warning/at-risk data. Color is semantic only.
import { VelaAppShell } from "@/components/vela/app-shell";
import { ActionButton, DataTable, MetricCard, StatusPill } from "@/components/vela/ui";
import { branchMap } from "@/lib/mock-lms";

// Orange = brand + on-track. Amber = warning. Red = at-risk. Nothing else gets a hue.
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
        <h2 className="mt-0.5 text-base font-bold leading-tight text-foreground">{title}</h2>
        {description ? <p className="mt-1 text-xs font-medium text-muted">{description}</p> : null}
      </div>
      {action}
    </div>
  );
}

const members = [
  { name: "Nguyễn Linh", handle: "@linh.nguyen", role: "Learner", team: "Phòng Nhân sự", status: "Đang hoạt động", lastSeen: "5 phút trước" },
  { name: "Trần Minh", handle: "@minh.tran", role: "Instructor", team: "Vận hành", status: "Đang hoạt động", lastSeen: "12 phút trước" },
  { name: "Phạm Hà", handle: "@ha.pham", role: "DeptManager", team: "Chất lượng", status: "Tạm khóa", lastSeen: "2 ngày trước" },
  { name: "Lê Hoàng", handle: "@hoang.le", role: "Partner", team: "Đối tác", status: "Chờ kích hoạt", lastSeen: "Chưa đăng nhập" },
];

export default function MembersPage() {
  return (
    <VelaAppShell active="Thành viên" lens="L&D Admin">
      <div className="grid gap-5 bg-background px-5 py-6 xl:px-7 xl:py-7">
        <PageHero />
        <MetricsStrip />
        <div className="grid gap-5 xl:grid-cols-[340px_minmax(0,1fr)]">
          <OrgScopeMap />
          <AccountLedger />
        </div>
      </div>
    </VelaAppShell>
  );
}

function PageHero() {
  return (
    <Panel className="overflow-hidden border-t-2 border-t-primary">
      <div className="flex items-end justify-between gap-4 px-5 py-5">
        <div>
          <Eyebrow>Quản lý truy cập · L&amp;D Admin</Eyebrow>
          <h1 className="mt-1.5 text-2xl font-extrabold leading-tight text-foreground">
            Role rõ, scope rõ, học đúng người
          </h1>
          <p className="mt-1.5 max-w-[62ch] text-sm font-medium text-muted">
            L&amp;D và admin cần thấy nhánh tổ chức, audience scope và vai trò trước khi tạo tài khoản
            hoặc mở thao tác nhạy cảm.
          </p>
        </div>
        <div className="hidden shrink-0 sm:block">
          <ActionButton icon="users">Thêm tài khoản</ActionButton>
        </div>
      </div>
      <div className="border-t border-border px-5 py-3 sm:hidden">
        <ActionButton icon="users">Thêm tài khoản</ActionButton>
      </div>
    </Panel>
  );
}

function MetricsStrip() {
  return (
    <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
      <MetricCard label="Nội bộ" value="1.840" detail="INTERNAL scope" tone="primary" />
      <MetricCard label="Đối tác" value="320" detail="PARTNER scope" tone="default" />
      <MetricCard label="Khách" value="92" detail="GUEST scope" tone="default" />
      <MetricCard label="Chờ kích hoạt" value="18" detail="cần nhắc đăng nhập" tone="warning" />
    </div>
  );
}

function OrgScopeMap() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader
        title="Org scope map"
        description="Chọn node để lọc learner, instructor và partner theo nhánh."
      />
      <div className="grid gap-3 p-5">
        {branchMap.map((branch) => {
          const s = sem(branch.tone);
          return (
            <button
              key={branch.branch}
              className="vela-focus rounded-lg border border-border bg-surface-raised px-4 py-3 text-left hover:bg-surface"
            >
              <div className="flex items-center justify-between gap-3">
                <span>
                  <span className="block text-sm font-semibold leading-snug text-foreground">
                    {branch.branch}
                  </span>
                  <span className="text-xs font-medium text-muted">{branch.city}</span>
                </span>
                <span className={`font-mono text-sm font-bold ${s.text}`}>{branch.percent}%</span>
              </div>
              <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-surface-muted">
                <div className={`h-full rounded-full ${s.fill}`} style={{ width: `${branch.percent}%` }} />
              </div>
            </button>
          );
        })}
      </div>
    </Panel>
  );
}

function AccountLedger() {
  return (
    <Panel className="overflow-hidden">
      <PanelHeader
        title="Account ledger"
        description="Mock cho import Excel, khóa tài khoản, đổi vai trò và audit trail."
        action={<ActionButton secondary>Import Excel</ActionButton>}
      />
      <DataTable headers={["Người dùng", "Vai trò", "Nhánh", "Trạng thái", "Lần cuối", ""]} minWidth="min-w-[840px]">
        {members.map((member) => (
          <tr key={member.handle} className="hover:bg-surface-raised">
            <td className="px-5 py-3">
              <p className="text-sm font-semibold text-foreground">{member.name}</p>
              <p className="font-mono text-xs font-medium text-muted">{member.handle}</p>
            </td>
            <td className="px-5 py-3">
              <StatusPill
                tone={
                  member.role === "Learner"
                    ? "primary"
                    : member.role === "Instructor"
                      ? "default"
                      : "default"
                }
              >
                {member.role}
              </StatusPill>
            </td>
            <td className="px-5 py-3 text-sm font-medium text-muted">{member.team}</td>
            <td className="px-5 py-3">
              <StatusPill
                tone={
                  member.status === "Tạm khóa"
                    ? "danger"
                    : member.status === "Chờ kích hoạt"
                      ? "warning"
                      : "default"
                }
              >
                {member.status}
              </StatusPill>
            </td>
            <td className="px-5 py-3 text-sm font-medium text-muted">{member.lastSeen}</td>
            <td className="px-5 py-3">
              <button className="vela-focus rounded-lg border border-border px-3 py-1.5 text-xs font-semibold hover:bg-surface-muted">
                Mở audit
              </button>
            </td>
          </tr>
        ))}
      </DataTable>
    </Panel>
  );
}
