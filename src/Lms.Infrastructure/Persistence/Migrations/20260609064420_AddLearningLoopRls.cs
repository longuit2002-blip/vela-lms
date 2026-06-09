using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningLoopRls : Migration
    {
        // Extends the tenant-isolation regime to the learning-loop aggregate-root tables. Only these
        // three carry organization_id; their child tables (modules, lessons, lesson_progress) have no
        // tenant column and are isolated transitively via their parent FK, so they get the app-role
        // grant (needed to read through the parent) but no row-level policy. The `lms_app` role already exists.
        private static readonly string[] TenantTables = ["courses", "publications", "enrollments"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "GRANT SELECT, INSERT, UPDATE, DELETE ON \"courses\", \"publications\", \"enrollments\", " +
                "\"modules\", \"lessons\", \"lesson_progress\" TO lms_app;");

            foreach (var table in TenantTables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" FORCE ROW LEVEL SECURITY;");
                // NULLIF maps the post-DISCARD-ALL '' back to NULL so an unscoped query matches no rows
                // (fail closed) — identical to the org-structure policy.
                migrationBuilder.Sql($@"
                    CREATE POLICY {table}_org_isolation ON ""{table}""
                        USING (organization_id = NULLIF(current_setting('app.current_org', true), '')::uuid)
                        WITH CHECK (organization_id = NULLIF(current_setting('app.current_org', true), '')::uuid);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantTables)
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS {table}_org_isolation ON \"{table}\";");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" NO FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
            }

            migrationBuilder.Sql(
                "REVOKE ALL ON \"courses\", \"publications\", \"enrollments\", " +
                "\"modules\", \"lessons\", \"lesson_progress\" FROM lms_app;");
        }
    }
}
