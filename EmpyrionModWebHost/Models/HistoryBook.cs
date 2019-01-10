using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class HistoryBookContext : DbContext
    {
        public HistoryBookContext(){}

        public HistoryBookContext(DbContextOptions<HistoryBookContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));
            optionsBuilder.UseSqlite($"Filename={EmpyrionConfiguration.SaveGameModPath}/DB/HistoryBooks.db");
        }

        public DbSet<HistoryBookOfStructures> Structures { get; set; }
        public DbSet<HistoryBookOfPlayers> Players { get; set; }
    }

    public class HistoryBookOfStructures
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Playfield { get; set; }
        public int EntityId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int PosZ { get; set; }
        /// <summary>
        /// JSON mit einem vollständigen Datensatz den geänderten Daten zum letzten vollständigen
        /// </summary>
        public string Changed { get; set; }
    }

    public class HistoryBookOfPlayers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Playfield { get; set; }
        public string SteamId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int PosZ { get; set; }
        /// <summary>
        /// JSON mit einem vollständigen Datensatz den geänderten Daten zum letzten vollständigen
        /// </summary>
        public string Changed { get; set; }
    }

}
