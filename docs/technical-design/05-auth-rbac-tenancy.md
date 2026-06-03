# T5 · AuthN, RBAC/ABAC & Multi-tenancy

> Liên quan: [P2 Roles](../product-development/02-personas-roles.md) (nguồn nghiệp vụ) · [T1](01-architecture-stack.md) · [T3 §14 RLS](03-database-schema.md) · [T4 §5–6](04-api-design.md) · [T9 Security](09-infra-security-nfr.md)

Đây là tài liệu **bảo mật cốt lõi**. Nguyên tắc: phân quyền **enforce ở backend** (defense in depth); UI chỉ ẩn/disable. Quyền hiệu lực = **Role (RBAC) ∩ Audience Scope ∩ Nhánh cây tổ chức (ABAC) ∩ Tenant**.

---

## 1. Mô hình tổng thể / Overview

```
        Request (Bearer JWT)
              │
   ┌──────────▼───────────┐
   │ AuthN middleware      │  verify JWT → ClaimsPrincipal (sub, org, roles, scope, dept)
   └──────────┬───────────┘
   ┌──────────▼───────────┐
   │ Tenant middleware     │  SET LOCAL app.current_org = org   (RLS context)
   └──────────┬───────────┘
   ┌──────────▼───────────┐
   │ MediatR pipeline      │  AuthorizationBehavior → permission check (RBAC)
   │                       │  + ABAC guard inside handler/domain (scope, branch, ownership)
   └──────────┬───────────┘
   ┌──────────▼───────────┐
   │ EF Core global filter │  WHERE organization_id = current_org  (belt + RLS suspenders)
   └──────────────────────┘
```

3 lớp phòng thủ tenant: (1) middleware set org context, (2) EF Core **global query filter** theo `organization_id`, (3) Postgres **RLS** policy. Không lớp nào đủ một mình.

---

## 2. Authentication

### 2.1 Token model
- **Access token (JWT, ~15 phút):** ký RS256; claims: `sub` (userId), `org` (organizationId), `scope` (audience scope), `roles` (codes), `dept` (departmentId), `perms` (optional: tập permission đã resolve — hoặc resolve server-side mỗi request từ roles để tránh token phình & thu hồi tức thì).
- **Refresh token (~14 ngày, rotation):** lưu **hash** trong `refresh_tokens`; mỗi refresh cấp token mới + thu hồi cũ; phát hiện reuse → thu hồi cả chuỗi.
- Lưu client: access trong memory; refresh trong **httpOnly secure cookie** (web) / secure storage (mobile).

### 2.2 Flows
- **Login:** `POST /auth/login` (email + password) → verify Argon2id hash → cấp cặp token. Khóa tài khoản tạm sau N lần sai (brute-force).
- **First login / mật khẩu mặc định:** nếu `must_change_password` → buộc đổi trước khi vào.
- **Forgot/reset:** token 1 lần qua email, hết hạn ngắn.
- **Change password:** yêu cầu mật khẩu cũ; thu hồi mọi refresh token khác.
- **Logout:** thu hồi refresh token hiện tại.

### 2.3 Password policy
- Hash **Argon2id** (hoặc PBKDF2 nếu hạ tầng giới hạn). Min length, không cho mật khẩu phổ biến.
- Mật khẩu mặc định khi admin tạo TK = random hoặc do admin đặt, kèm `must_change_password=true`.

### 2.4 SSO (Phase 3)
OIDC/SAML cho doanh nghiệp lớn; map claim → user + department; JIT provisioning tùy chọn.

---

## 3. Authorization — RBAC

### 3.1 Permission codes (canonical)
Quyền là các **permission code** dạng `domain.action[.scope]`. Role = tập permission. Ví dụ:

```
users.read  users.create  users.update  users.lock  users.import  users.assign_role
org.manage  org.settings  roles.manage
categories.manage
courses.read  courses.create  courses.update  courses.delete
publications.read  publications.create  publications.update  publications.publish
learningpaths.manage  exams.manage  questions.manage
documents.read  documents.manage  documents.share
sessions.manage  attendance.manage
training.frameworks.manage  training.types.manage  assignments.create
enrollments.self  learning.consume  exams.attempt
leaderboard.read
reports.publishing.read  reports.training.read  reports.export
ai.generate  ai.lookup
audit.read
```

### 3.2 Role → permissions (seed)
| Role | Permissions (tóm tắt) |
|---|---|
| `OrgOwner` | `*` trong tổ chức (mọi code) |
| `OrgAdmin` | như OrgOwner trừ `org.manage` (xóa/billing) |
| `LndManager` | content/publishing/training/reports/ai + `users.*` (◐ nhánh) |
| `DeptManager` | `users.read/create/update/import/assign` (◐ nhánh), `publications.*` (◐), `assignments.create`, `reports.*.read` (◐) |
| `Instructor` | `courses.*` (◐ own), `publications.*` (◐ own), `sessions.manage` (◐ own), `attendance.manage` (◐ own), `ai.generate`, `documents.manage` (◐) |
| `Learner` | `learning.consume`, `enrollments.self`, `exams.attempt`, `leaderboard.read`, `ai.lookup` (nếu bật) |
| `Auditor` | `*.read`, `reports.*`, `audit.read` |

`◐` = permission có nhưng **giới hạn phạm vi** bởi ABAC (xem §4).

### 3.3 Enforce RBAC (Application layer)
Mỗi Command/Query khai báo permission yêu cầu; `AuthorizationBehavior` (MediatR) kiểm tra trước handler:

```csharp
public interface IRequirePermission { string Permission { get; } }

public sealed record LockUserCommand(Guid UserId) : IRequest<Result>, IRequirePermission
{ public string Permission => "users.lock"; }

public sealed class AuthorizationBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
{
    public async Task<TRes> Handle(TReq req, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        if (req is IRequirePermission p && !_currentUser.HasPermission(p.Permission))
            return (TRes)(object)Result.Forbidden();
        return await next();
    }
}
```

---

## 4. Authorization — ABAC (scope, branch, ownership)

RBAC trả lời *"được làm action này không?"*; ABAC giới hạn *"trên dữ liệu nào?"*. 5 quy tắc phạm vi (từ [P2 §4](../product-development/02-personas-roles.md)):

| Rule | Áp dụng cho | Cơ chế |
|---|---|---|
| **Tenant isolation** | Mọi user (trừ platform) | `organization_id` filter + RLS |
| **Department branch (◐)** | `DeptManager`, `LndManager` (giới hạn) | user chỉ thao tác trên nhánh mình: `target_dept ∈ subtree(user.dept)` qua `department_closure` |
| **Ownership (◐ own)** | `Instructor` | `resource.created_by = user.id` hoặc được gán dạy |
| **Audience scope** | Hiển thị nội dung | publication.audience_scopes ∩ user.scope ≠ ∅ **và** user khớp `publication_targets` (dept/position/user/group) |
| **Self** | `Learner` | chỉ dữ liệu học của chính mình (`enrollments.user_id = me`) |

### 4.1 Branch check (closure table)
```sql
-- user (DeptManager) quản được department X không?
SELECT 1 FROM department_closure
WHERE ancestor_id = :userDeptId AND descendant_id = :targetDeptId;
```

### 4.2 Content visibility (learner)
Một user thấy `Publication P` nếu:
```
P.status = 'published'
AND ( P.is_public OR user.scope = ANY(P.audience_scopes) )
AND ( P.is_public
      OR EXISTS target IN P.targets WHERE
           (target.type='user'       AND target.id = user.id)
        OR (target.type='department' AND target.id ∈ {user.dept ∪ ancestors(user.dept)})
        OR (target.type='position'   AND target.id = user.position)
        OR (target.type='group'      AND user ∈ group) )
```
> Truy vấn này gói trong **Specification** tái dùng (Domain/Application) cho mọi list học tập (Explore/Library/Dashboard) để không lặp logic.

### 4.3 ABAC trong handler/domain
ABAC kiểm tra cần dữ liệu → đặt trong handler hoặc domain guard, không ở behavior:
```csharp
// trong DeptManager update user
if (!await _orgTree.IsInBranch(current.DepartmentId, target.DepartmentId, ct))
    return Result.Forbidden("Ngoài phạm vi phòng ban quản lý.");
```

---

## 5. Multi-tenancy

### 5.1 Strategy (ADR-003)
**Shared database, shared schema, discriminator `organization_id`.** Đơn giản vận hành; cách ly bằng filter + RLS. Có thể tách DB-per-tenant cho khách lớn về sau (chiến lược lai).

### 5.2 Tenant context
- Resolve org từ JWT claim `org` (không từ body/subdomain cho dữ liệu thường — chống tenant spoofing).
- Đầu mỗi request (transaction): `SET LOCAL app.current_org = :org` → RLS dùng.
- EF Core **global query filter**:
```csharp
modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == _tenant.OrgId);
```
- Platform roles (`PlatformSuperAdmin`) bỏ filter qua cờ rõ ràng (`IgnoreQueryFilters()` + audit).

### 5.3 RLS policy (Postgres)
Bật RLS như [T3 §14](03-database-schema.md); policy `organization_id = current_setting('app.current_org')::uuid`. **Lợi ích:** kể cả lỗi quên filter ở app, DB vẫn chặn.

### 5.4 Edge cases
| Tình huống | Xử lý |
|---|---|
| Background job (Hangfire) không có HTTP context | Job mang theo `organizationId`; set tenant context thủ công khi mở scope |
| Cross-tenant platform op | Chỉ `PlatformSuperAdmin`; bỏ filter có chủ đích + ghi audit |
| User thuộc nhiều phòng? | MVP: 1 phòng chính. Nếu cần đa phòng → bảng `user_departments` (Phase sau) |
| Đổi phòng ban giữa kỳ | Điểm/báo cáo dùng `department_id` denormalized tại thời điểm sự kiện ([T3 point_transactions](03-database-schema.md)) |
| Guest scope | Chỉ thấy `is_public` publications; không vào sidebar admin |

---

## 6. Frontend enforcement (UI)
- Token decode → `permissions` + `scope` → ẩn menu/nút không có quyền.
- Route guard: chặn điều hướng tới route thiếu permission.
- **Luôn coi UI là gợi ý**; backend là nguồn chân lý. Không bao giờ tin client để quyết định quyền.

---

## 7. Audit & compliance
- Mọi sensitive op (T4 §5) ghi `audit_logs` (actor, action, before/after, ip).
- Đăng nhập/đổi quyền/đổi MK ghi audit.
- Truy cập báo cáo nhạy cảm log (ai xem gì).
- GDPR-like: hỗ trợ export & xóa dữ liệu cá nhân theo yêu cầu ([T9](09-infra-security-nfr.md)).

---

## 8. Test phân quyền (bắt buộc)
- **Unit:** mỗi role × capability theo ma trận [P2 §3](../product-development/02-personas-roles.md).
- **Integration:** giả lập 2 tổ chức → đảm bảo không rò rỉ chéo (tenant isolation).
- **ABAC:** DeptManager phòng A không sửa được user phòng B; Instructor không sửa khóa người khác; Learner không gọi được endpoint admin (403).
- **Negative/penetration:** thử IDOR (đổi id trong URL), token tenant khác, scope escalation. Phải trả 403/404 đúng quy ước ([T4 §6](04-api-design.md)).
