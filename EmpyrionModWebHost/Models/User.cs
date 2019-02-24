using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace EmpyrionModWebHost.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string InGameSteamId { get; set; }
    }

    public enum Role
    {
        ServerAdmin = 0,
        InGameAdmin,
        Moderator,
        GameMaster,
        VIP,
        Player,
        None
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Username { get; set; }
        public Role Role { get; set; }
        public string InGameSteamId { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
    }

    public class UserContext : DbContext
    {
        public UserContext(){}

        public UserContext(DbContextOptions<UserContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));
            optionsBuilder.UseSqlite($"Filename={EmpyrionConfiguration.SaveGameModPath}/DB/Users.db");
        }

        public DbSet<User> Users { get; set; }
    }
}
