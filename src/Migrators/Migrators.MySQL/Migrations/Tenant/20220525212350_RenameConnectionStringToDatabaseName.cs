using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MySQL.Migrations.Tenant
{
    public partial class RenameConnectionStringToDatabaseName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Subscriptions",
                schema: "MultiTenancy",
                newName: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "ConnectionString",
                schema: "MultiTenancy",
                table: "Tenants",
                newName: "DatabaseName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "Subscriptions",
                newSchema: "MultiTenancy");

            migrationBuilder.RenameColumn(
                name: "DatabaseName",
                schema: "MultiTenancy",
                table: "Tenants",
                newName: "ConnectionString");
        }
    }
}
