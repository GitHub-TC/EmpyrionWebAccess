using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations.Backpack
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Backpacks",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    ToolbarContent = table.Column<string>(nullable: true),
                    BagContent = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backpacks", x => new { x.Id, x.Timestamp });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Backpacks");
        }
    }
}
