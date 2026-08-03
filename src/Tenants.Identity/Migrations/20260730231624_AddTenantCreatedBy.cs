using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acme.Pki.Tenants.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tenants");
        }
    }
}
