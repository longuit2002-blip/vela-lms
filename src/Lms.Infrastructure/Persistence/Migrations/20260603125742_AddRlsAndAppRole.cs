using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRlsAndAppRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Non-owner, non-superuser application role. It is SUBJECT to RLS (unlike the superuser the
            // app currently connects as, which bypasses RLS and relies on the EF global query filter).
            // Used by the isolation tests to prove the policy, and is the role the app is promoted to
            // when the non-owner-app-role hardening lands (see plan: Deferred to Follow-Up Work).
            // Dev-only credential.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lms_app') THEN
                        CREATE ROLE lms_app LOGIN PASSWORD 'lms_app_local_dev';
                    END IF;
                END
                $$;");

            migrationBuilder.Sql("GRANT USAGE ON SCHEMA public TO lms_app;");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON \"users\", \"refresh_tokens\" TO lms_app;");
            migrationBuilder.Sql("GRANT SELECT ON \"organizations\" TO lms_app;");

            // Tenant isolation policy keyed on the per-request session variable. The two-arg
            // current_setting(..., true) returns NULL when unset, so an unscoped query matches no rows
            // (fail closed) instead of erroring. FORCE so even a table owner is subject.
            foreach (var table in new[] { "users", "refresh_tokens" })
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" FORCE ROW LEVEL SECURITY;");
                // NULLIF(..., '') is essential: after a pooled connection's DISCARD ALL reset, a custom
                // GUC that was previously set returns '' (empty string), not NULL — and ''::uuid errors.
                // NULLIF maps '' back to NULL so an unscoped query matches no rows (fail closed).
                migrationBuilder.Sql($@"
                    CREATE POLICY {table}_org_isolation ON ""{table}""
                        USING (organization_id = NULLIF(current_setting('app.current_org', true), '')::uuid)
                        WITH CHECK (organization_id = NULLIF(current_setting('app.current_org', true), '')::uuid);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "users", "refresh_tokens" })
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS {table}_org_isolation ON \"{table}\";");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" NO FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
            }

            migrationBuilder.Sql("REVOKE ALL ON \"users\", \"refresh_tokens\", \"organizations\" FROM lms_app;");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA public FROM lms_app;");
            // Role is cluster-wide and may be shared; drop only if no longer used.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lms_app') THEN
                        DROP ROLE lms_app;
                    END IF;
                END
                $$;");
        }
    }
}
