using EmpyrionNetAPITools;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class FactoryItemsContext : DbContext
    {
        public FactoryItemsContext(){}

        public FactoryItemsContext(DbContextOptions<FactoryItemsContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var ewaDbPath = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB");
            Directory.CreateDirectory(ewaDbPath);
            optionsBuilder.UseSqlite($"Filename={ewaDbPath}/FactoryItems.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactoryItems>()
                .HasKey(B => new { B.Id, B.Timestamp });
        }

        public DbSet<FactoryItems> FactoryItems { get; set; }
    }

    public class FactoryItems
    {
        // Als ID wird die SteamID genutzt
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
        public string InProduction { get; set; }
        public string Produced { get; set; }
    }

}
