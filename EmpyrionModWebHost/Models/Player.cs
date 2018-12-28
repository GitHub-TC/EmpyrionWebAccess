using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class PlayerContext : DbContext
    {
        public PlayerContext(){}

        public PlayerContext(DbContextOptions<PlayerContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));
            optionsBuilder.UseSqlite($"Filename={EmpyrionConfiguration.SaveGameModPath}/DB/Players.db");
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
    }
}
