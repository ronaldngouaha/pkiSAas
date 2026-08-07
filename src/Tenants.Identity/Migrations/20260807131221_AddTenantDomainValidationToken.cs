using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acme.Pki.Tenants.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDomainValidationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantDomains_Domain",
                table: "TenantDomains");

            migrationBuilder.DropIndex(
                name: "IX_TenantDomains_TenantId",
                table: "TenantDomains");

            migrationBuilder.AlterColumn<string>(
                name: "Domain",
                table: "TenantDomains",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TenantDomains",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationToken",
                table: "TenantDomains",
                type: "varchar(512)",
                unicode: false,
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RoleCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RoleMap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleCatalogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId_Domain",
                table: "TenantDomains",
                columns: new[] { "TenantId", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleCatalogs_Scope_IsDefault",
                table: "RoleCatalogs",
                columns: new[] { "Scope", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleCatalogs_TenantId_NormalizedName",
                table: "RoleCatalogs",
                columns: new[] { "TenantId", "NormalizedName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_TenantDomains_TenantId_Domain",
                table: "TenantDomains");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TenantDomains");

            migrationBuilder.DropColumn(
                name: "ValidationToken",
                table: "TenantDomains");

            migrationBuilder.AlterColumn<string>(
                name: "Domain",
                table: "TenantDomains",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_Domain",
                table: "TenantDomains",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId",
                table: "TenantDomains",
                column: "TenantId");
        }
    }
}