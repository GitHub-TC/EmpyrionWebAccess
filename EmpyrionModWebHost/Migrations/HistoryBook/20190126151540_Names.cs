using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations.HistoryBook
{
    public partial class Names : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Structures",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Players",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Players");
        }
    }
}
