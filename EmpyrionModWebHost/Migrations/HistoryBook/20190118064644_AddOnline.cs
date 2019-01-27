using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations.HistoryBook
{
    public partial class AddOnline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Online",
                table: "Players",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Online",
                table: "Players");
        }
    }
}
