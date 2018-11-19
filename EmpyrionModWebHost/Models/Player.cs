using Eleon.Modding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Models
{
    public class PlayerContext : DbContext
    {
        public PlayerContext(){}

        public PlayerContext(DbContextOptions<PlayerContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=./Players.db");
        }

        public DbSet<Player> Players { get; set; }
    }

    public class Player
    {
        // Als ID wird die SteamID genutzt
        public string Id { get; set; }
        public int entityId { get; set; }
        public string steamId { get; set; }
        public int clientId { get; set; }
        public float radiation { get; set; }
        public float radiationMax { get; set; }
        public float bodyTemp { get; set; }
        public float bodyTempMax { get; set; }
        public int kills { get; set; }
        public int died { get; set; }
        public double credits { get; set; }
        public float foodMax { get; set; }
        public int exp { get; set; }
        public int upgrade { get; set; }
        public int ping { get; set; }
        public int permission { get; set; }
        public float food { get; set; }
        public float stamina { get; set; }
        public string steamOwnerId { get; set; }
        public string playerName { get; set; }
        public string playfield { get; set; }
        public string startPlayfield { get; set; }
        public float staminaMax { get; set; }
        public byte factionGroup { get; set; }
        public int factionId { get; set; }
        public byte factionRole { get; set; }
        public float health { get; set; }
        public float healthMax { get; set; }
        public float oxygen { get; set; }
        public float oxygenMax { get; set; }
        public byte origin { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public float posZ { get; set; }
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
    }
}
