namespace Lms.Application.Authorization;

/// <summary>
/// Canonical permission codes (<c>domain.action[.scope]</c>) that this slice <em>enforces</em> via
/// <see cref="IRequirePermission"/>. The full role→permission matrix (T5 §3.1) is seeded as data on
/// the system roles (see the seeder); only the codes guarding endpoints that exist today are listed
/// here as constants, so a typo in an enforced code is a compile error rather than a silent denial.
/// </summary>
public static class Permissions
{
    public static class Departments
    {
        public const string Read = "departments.read";
        public const string Manage = "departments.manage";

        /// <summary>Full-scope org-tree management (skips the dept-branch ABAC check). Held only by OrgOwner/OrgAdmin.</summary>
        public const string ManageAll = "departments.manage.all";
    }

    public static class Positions
    {
        public const string Read = "positions.read";
        public const string Manage = "positions.manage";
    }

    public static class Courses
    {
        public const string Read = "courses.read";
        public const string Create = "courses.create";
        public const string Update = "courses.update";
    }

    public static class Publications
    {
        public const string Create = "publications.create";
        public const string Publish = "publications.publish";
    }

    public static class Assignments
    {
        public const string Create = "assignments.create";
    }

    public static class Learning
    {
        /// <summary>A learner acting on their own enrollments.</summary>
        public const string Self = "enrollments.self";

        /// <summary>A learner consuming assigned content (viewing detail, completing lessons).</summary>
        public const string Consume = "learning.consume";
    }
}
