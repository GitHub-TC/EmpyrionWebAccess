using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations.Faction
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactionId = table.Column<int>(nullable: false),
                    Origin = table.Column<byte>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Abbrev = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Factions");
        }
    }
}
