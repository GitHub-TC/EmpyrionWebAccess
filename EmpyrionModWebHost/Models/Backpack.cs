using Eleon.Modding;
using EmpyrionNetAPITools;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class BackpackContext : DbContext
    {
        public BackpackContext(){}

        public BackpackContext(DbContextOptions<BackpackContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var ewaDbPath = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB");
            Directory.CreateDirectory(ewaDbPath);
            optionsBuilder.UseSqlite($"Filename={ewaDbPath}/Backpacks.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Backpack>()
                .HasKey(B => new { B.Id, B.Timestamp });
        }

        public DbSet<Backpack> Backpacks { get; set; }
    }

    public class Backpack
    {
        // Als ID wird die SteamID genutzt
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ToolbarContent { get; set; }
        public string BagContent { get; set; }
    }

    public class BackpackModel
    {
        public string SteamId;
        public ItemStack[] Toolbar;
        public ItemStack[] Bag;
    }

}
