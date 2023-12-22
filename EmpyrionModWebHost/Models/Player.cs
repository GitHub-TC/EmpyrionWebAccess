using EmpyrionNetAPITools;
using Microsoft.EntityFrameworkCore;

namespace EmpyrionModWebHost.Models
{
    public class PlayerContext : DbContext
    {
        public string DBFile { get; set; }
        public PlayerContext(){}

        public PlayerContext(string aDBFile){
            DBFile = aDBFile;
        }

        public PlayerContext(DbContextOptions<PlayerContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var ewaDbPath = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB");
            Directory.CreateDirectory(ewaDbPath);
            optionsBuilder.UseSqlite(
                DBFile == null
                ? $"Filename={ewaDbPath}/Players.db"
                : $"Filename={DBFile}");
        }

        public DbSet<Player> Players { get; set; }
    }

    public class Player
    {
        // Als ID wird die SteamID genutzt
        public string Id { get; set; }
        public int EntityId { get; set; }
        public string SteamId { get; set; }
        public int ClientId { get; set; }
        public float Radiation { get; set; }
        public float RadiationMax { get; set; }
        public float BodyTemp { get; set; }
        public float BodyTempMax { get; set; }
        public int Kills { get; set; }
        public int Died { get; set; }
        public double Credits { get; set; }
        public float FoodMax { get; set; }
        public int Exp { get; set; }
        public int Upgrade { get; set; }
        public int Ping { get; set; }
        public int Permission { get; set; }
        public float Food { get; set; }
        public float Stamina { get; set; }
        public string SteamOwnerId { get; set; }
        public string PlayerName { get; set; }
        public string Playfield { get; set; }
        public string SolarSystem { get; set; }
        public string StartPlayfield { get; set; }
        public float StaminaMax { get; set; }
        public byte FactionGroup { get; set; }
        public int FactionId { get; set; }
        public byte FactionRole { get; set; }
        public float Health { get; set; }
        public float HealthMax { get; set; }
        public float Oxygen { get; set; }
        public float OxygenMax { get; set; }
        public byte Origin { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
        public bool Online { get; set; }
        public TimeSpan OnlineTime { get; set; }
        public DateTime LastOnline { get; set; }
        public string Note { get; set; }
        public int OnlineHours { get; set; }
        public string BpInFactory { get; set; }
        public float BpRemainingTime { get; set; }
        public long Filesize { get; set; }
    }
}
