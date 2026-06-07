using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgStructureAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "position_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_departments_departments_parent_id",
                        column: x => x.parent_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_departments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_positions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "department_closure",
                columns: table => new
                {
                    ancestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descendant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_closure", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "FK_department_closure_departments_ancestor_id",
                        column: x => x.ancestor_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_department_closure_departments_descendant_id",
                        column: x => x.descendant_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id_department_id",
                table: "users",
                columns: new[] { "organization_id", "department_id" });

            migrationBuilder.CreateIndex(
                name: "IX_users_position_id",
                table: "users",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_closure_descendant_id",
                table: "department_closure",
                column: "descendant_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_organization_id",
                table: "departments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_organization_id_parent_id",
                table: "departments",
                columns: new[] { "organization_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_parent_id",
                table: "departments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_positions_organization_id_name",
                table: "positions",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_organization_id",
                table: "roles",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_departments_department_id",
                table: "users",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_positions_position_id",
                table: "users",
                column: "position_id",
                principalTable: "positions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_departments_department_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_positions_position_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "department_closure");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropIndex(
                name: "IX_users_department_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_organization_id_department_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_position_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "position_id",
                table: "users");
        }
    }
}
