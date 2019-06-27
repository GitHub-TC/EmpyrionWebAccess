using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations.FactoryItems
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FactoryItems",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    InProduction = table.Column<string>(nullable: true),
                    Produced = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryItems", x => new { x.Id, x.Timestamp });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactoryItems");
        }
    }
}
