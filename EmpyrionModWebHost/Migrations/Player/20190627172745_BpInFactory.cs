using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations
{
    public partial class BpInFactory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BpInFactory",
                table: "Players",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "BpRemainingTime",
                table: "Players",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BpInFactory",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "BpRemainingTime",
                table: "Players");
        }
    }
}
