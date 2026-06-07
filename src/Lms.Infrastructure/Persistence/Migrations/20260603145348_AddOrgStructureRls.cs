using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgStructureRls : Migration
    {
        // Extends the tenant-isolation regime (see AddRlsAndAppRole) to the new tenant-owned tables.
        // `roles` is intentionally excluded — system roles are an org-wide catalog (organization_id
        // NULL), so they get a read grant only, no RLS. The `lms_app` role already exists.
        private static readonly string[] TenantTables = ["departments", "department_closure", "positions"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "GRANT SELECT, INSERT, UPDATE, DELETE ON \"departments\", \"department_closure\", \"positions\" TO lms_app;");
            migrationBuilder.Sql("GRANT SELECT ON \"roles\" TO lms_app;");

            foreach (var table in TenantTables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" FORCE ROW LEVEL SECURITY;");
                // NULLIF maps the post-DISCARD-ALL '' back to NULL so an unscoped query matches no rows
                // (fail closed) instead of erroring on ''::uuid — identical to the users/refresh_tokens policy.
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

            migrationBuilder.Sql("REVOKE ALL ON \"departments\", \"department_closure\", \"positions\" FROM lms_app;");
            migrationBuilder.Sql("REVOKE SELECT ON \"roles\" FROM lms_app;");
        }
    }
}
