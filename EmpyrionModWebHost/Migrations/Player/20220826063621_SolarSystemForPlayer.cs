using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpyrionModWebHost.Migrations
{
    public partial class SolarSystemForPlayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SolarSystem",
                table: "Players",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolarSystem",
                table: "Players");
        }
    }
}
