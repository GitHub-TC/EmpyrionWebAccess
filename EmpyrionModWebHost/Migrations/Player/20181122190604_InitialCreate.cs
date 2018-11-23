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
                    EntityId = table.Column<int>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    ClientId = table.Column<int>(nullable: false),
                    Radiation = table.Column<float>(nullable: false),
                    RadiationMax = table.Column<float>(nullable: false),
                    BodyTemp = table.Column<float>(nullable: false),
                    BodyTempMax = table.Column<float>(nullable: false),
                    Kills = table.Column<int>(nullable: false),
                    Died = table.Column<int>(nullable: false),
                    Credits = table.Column<double>(nullable: false),
                    FoodMax = table.Column<float>(nullable: false),
                    Exp = table.Column<int>(nullable: false),
                    Upgrade = table.Column<int>(nullable: false),
                    Ping = table.Column<int>(nullable: false),
                    Permission = table.Column<int>(nullable: false),
                    Food = table.Column<float>(nullable: false),
                    Stamina = table.Column<float>(nullable: false),
                    SteamOwnerId = table.Column<string>(nullable: true),
                    PlayerName = table.Column<string>(nullable: true),
                    Playfield = table.Column<string>(nullable: true),
                    StartPlayfield = table.Column<string>(nullable: true),
                    StaminaMax = table.Column<float>(nullable: false),
                    FactionGroup = table.Column<byte>(nullable: false),
                    FactionId = table.Column<int>(nullable: false),
                    FactionRole = table.Column<byte>(nullable: false),
                    Health = table.Column<float>(nullable: false),
                    HealthMax = table.Column<float>(nullable: false),
                    Oxygen = table.Column<float>(nullable: false),
                    OxygenMax = table.Column<float>(nullable: false),
                    Origin = table.Column<byte>(nullable: false),
                    PosX = table.Column<float>(nullable: false),
                    PosY = table.Column<float>(nullable: false),
                    PosZ = table.Column<float>(nullable: false),
                    RotX = table.Column<float>(nullable: false),
                    RotY = table.Column<float>(nullable: false),
                    RotZ = table.Column<float>(nullable: false),
                    Online = table.Column<bool>(nullable: false)
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
