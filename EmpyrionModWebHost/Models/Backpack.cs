using Eleon.Modding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Models
{
    public class BackpackContext : DbContext
    {
        public BackpackContext(){}

        public BackpackContext(DbContextOptions<BackpackContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=./Backpacks.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Backpack>()
                .HasKey(B => new { B.Id, B.timestamp });
        }

        public DbSet<Backpack> Backpacks { get; set; }
    }

    public class Backpack
    {
        // Als ID wird die SteamID genutzt
        public string Id { get; set; }
        public DateTime timestamp { get; set; }
        public string content { get; set; }

    }
}
