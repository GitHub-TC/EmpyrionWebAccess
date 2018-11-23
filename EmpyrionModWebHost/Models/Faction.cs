using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class FactionContext : DbContext
    {
        public FactionContext(){}

        public FactionContext(DbContextOptions<FactionContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));
            optionsBuilder.UseSqlite($"Filename={EmpyrionConfiguration.SaveGameModPath}/DB/Factions.db");
        }

        public DbSet<Faction> Factions { get; set; }
    }

    public class Faction
    {
        [Key]
        public int FactionId { get; set; }
        public byte Origin { get; set; }
        public string Name { get; set; }
        public string Abbrev { get; set; }
    }
}
