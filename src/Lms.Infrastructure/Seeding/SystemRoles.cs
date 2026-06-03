using Lms.Application.Authorization;

namespace Lms.Infrastructure.Seeding;

/// <summary>
/// The seven system roles and their permission sets (the P2 §3 / T5 §3.2 matrix), seeded as data.
/// The enforced codes (departments.*/positions.*) reference the <see cref="Permissions"/> constants so
/// the seed cannot drift from what the endpoints require; the rest are literals for capabilities that
/// arrive in later slices — seeded now so the matrix is complete, guarding nothing until their
/// features (and <see cref="IRequirePermission"/> declarations) exist.
/// <para>
/// Full-scope vs branch-scope org-tree management is encoded here: OrgOwner/OrgAdmin hold
/// <c>departments.manage.all</c> (the ABAC guard skips the branch check); LndManager holds only
/// <c>departments.manage</c> (branch-limited). DeptManager holds neither — its ◐ is over users.
/// </para>
/// </summary>
internal static class SystemRoles
{
    internal sealed record Definition(string Code, string Name, string[] Permissions);

    public static IReadOnlyList<Definition> All { get; } =
    [
        new("OrgOwner", "Quản trị tổng", [.. EveryPermission]),
        new("OrgAdmin", "Quản trị viên", [.. EveryPermission.Where(p => p != "org.manage")]),
        new("LndManager", "Quản lý đào tạo",
        [
            Permissions.Departments.Read, Permissions.Departments.Manage,
            Permissions.Positions.Read, Permissions.Positions.Manage,
            "users.read", "users.create", "users.update", "users.import", "users.assign_role",
            "categories.manage",
            "courses.read", "courses.create", "courses.update", "courses.delete",
            "publications.read", "publications.create", "publications.update", "publications.publish",
            "learningpaths.manage", "exams.manage", "questions.manage",
            "documents.read", "documents.manage", "documents.share",
            "sessions.manage", "attendance.manage",
            "training.frameworks.manage", "training.types.manage", "assignments.create",
            "reports.publishing.read", "reports.training.read", "reports.export",
            "ai.generate", "ai.lookup",
            "leaderboard.read", "learning.consume", "enrollments.self", "exams.attempt",
        ]),
        new("DeptManager", "Trưởng phòng",
        [
            Permissions.Departments.Read, Permissions.Positions.Read,
            "users.read", "users.create", "users.update", "users.import", "users.assign_role",
            "publications.read", "publications.create", "publications.update", "publications.publish",
            "assignments.create",
            "reports.publishing.read", "reports.training.read",
            "leaderboard.read", "learning.consume", "enrollments.self", "exams.attempt", "ai.lookup",
        ]),
        new("Instructor", "Giảng viên",
        [
            "courses.read", "courses.create", "courses.update",
            "publications.read", "publications.create", "publications.update", "publications.publish",
            "sessions.manage", "attendance.manage",
            "documents.read", "documents.manage",
            "questions.manage", "exams.manage",
            "ai.generate", "ai.lookup",
            "leaderboard.read", "learning.consume", "enrollments.self", "exams.attempt",
        ]),
        new("Learner", "Học viên",
        [
            "learning.consume", "enrollments.self", "exams.attempt", "leaderboard.read", "ai.lookup",
        ]),
        new("Auditor", "Kiểm toán",
        [
            Permissions.Departments.Read, Permissions.Positions.Read,
            "users.read", "courses.read", "publications.read", "documents.read",
            "reports.publishing.read", "reports.training.read", "reports.export",
            "leaderboard.read", "audit.read",
        ]),
    ];

    /// <summary>The full permission catalog (T5 §3.1) — OrgOwner gets all of it.</summary>
    private static readonly string[] EveryPermission =
    [
        Permissions.Departments.Read, Permissions.Departments.Manage, Permissions.Departments.ManageAll,
        Permissions.Positions.Read, Permissions.Positions.Manage,
        "users.read", "users.create", "users.update", "users.lock", "users.import", "users.assign_role",
        "org.manage", "org.settings", "roles.manage",
        "categories.manage",
        "courses.read", "courses.create", "courses.update", "courses.delete",
        "publications.read", "publications.create", "publications.update", "publications.publish",
        "learningpaths.manage", "exams.manage", "questions.manage",
        "documents.read", "documents.manage", "documents.share",
        "sessions.manage", "attendance.manage",
        "training.frameworks.manage", "training.types.manage", "assignments.create",
        "enrollments.self", "learning.consume", "exams.attempt",
        "leaderboard.read",
        "reports.publishing.read", "reports.training.read", "reports.export",
        "ai.generate", "ai.lookup",
        "audit.read",
    ];
}
