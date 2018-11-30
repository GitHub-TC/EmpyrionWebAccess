using Eleon.Modding;
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
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));
            optionsBuilder.UseSqlite($"Filename={EmpyrionConfiguration.SaveGameModPath}/DB/Backpacks.v2.db");
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
