using Microsoft.EntityFrameworkCore.Migrations;

namespace EmpyrionModWebHost.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    entityId = table.Column<int>(nullable: false),
                    steamId = table.Column<string>(nullable: true),
                    clientId = table.Column<int>(nullable: false),
                    radiation = table.Column<float>(nullable: false),
                    radiationMax = table.Column<float>(nullable: false),
                    bodyTemp = table.Column<float>(nullable: false),
                    bodyTempMax = table.Column<float>(nullable: false),
                    kills = table.Column<int>(nullable: false),
                    died = table.Column<int>(nullable: false),
                    credits = table.Column<double>(nullable: false),
                    foodMax = table.Column<float>(nullable: false),
                    exp = table.Column<int>(nullable: false),
                    upgrade = table.Column<int>(nullable: false),
                    ping = table.Column<int>(nullable: false),
                    permission = table.Column<int>(nullable: false),
                    food = table.Column<float>(nullable: false),
                    stamina = table.Column<float>(nullable: false),
                    steamOwnerId = table.Column<string>(nullable: true),
                    playerName = table.Column<string>(nullable: true),
                    playfield = table.Column<string>(nullable: true),
                    startPlayfield = table.Column<string>(nullable: true),
                    staminaMax = table.Column<float>(nullable: false),
                    factionGroup = table.Column<byte>(nullable: false),
                    factionId = table.Column<int>(nullable: false),
                    factionRole = table.Column<byte>(nullable: false),
                    health = table.Column<float>(nullable: false),
                    healthMax = table.Column<float>(nullable: false),
                    oxygen = table.Column<float>(nullable: false),
                    oxygenMax = table.Column<float>(nullable: false),
                    origin = table.Column<byte>(nullable: false),
                    posX = table.Column<float>(nullable: false),
                    posY = table.Column<float>(nullable: false),
                    posZ = table.Column<float>(nullable: false),
                    rotX = table.Column<float>(nullable: false),
                    rotY = table.Column<float>(nullable: false),
                    rotZ = table.Column<float>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
