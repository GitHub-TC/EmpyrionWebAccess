using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations
{
    public partial class OnlineHours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnlineHours",
                table: "Players",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnlineHours",
                table: "Players");
        }
    }
}
