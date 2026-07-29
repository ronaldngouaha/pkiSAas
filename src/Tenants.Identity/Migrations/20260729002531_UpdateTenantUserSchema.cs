using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acme.Pki.Tenants.Identity.src.Tenants.Identity.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MfaMethods",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PiiConsentGivenAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocale",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ServiceAccount",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BillingAccountId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultAuthPolicy",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxCertificates",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerContactEmail",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryDomain",
                table: "Tenants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_NormalizedEmail",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PrimaryDomain",
                table: "Tenants",
                column: "PrimaryDomain");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId",
                table: "TenantDomains",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantDomains_Tenants_TenantId",
                table: "TenantDomains",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantDomains_Tenants_TenantId",
                table: "TenantDomains");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PrimaryDomain",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_TenantDomains_TenantId",
                table: "TenantDomains");

            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPhoneVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaMethods",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PiiConsentGivenAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredLocale",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ServiceAccount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAccountId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultAuthPolicy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxCertificates",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OwnerContactEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PrimaryDomain",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
