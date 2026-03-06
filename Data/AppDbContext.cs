using Microsoft.EntityFrameworkCore;
using ScriblleDemo.Models;

namespace ScriblleDemo.Data
{
    public class AppDbContext : DbContext
    {
        // This constructor receives DB configuration from Program.cs
        // You don't call this yourself — .NET's dependency injection does
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Each DbSet = one table in MySQL
        // "Players" will be the table name
        // Initialize with null-forgiving operator to satisfy nullable reference checks
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<GameSession> GameSessions { get; set; } = null!;
        public DbSet<Round> Rounds { get; set; } = null!;
        public DbSet<WordList> WordList { get; set; } = null!;

        // OnModelCreating lets us give EF Core extra instructions
        // about how to build the tables
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RoomCode must be unique
            modelBuilder.Entity<GameSession>()
                .HasIndex(g => g.RoomCode)
                .IsUnique();

            // GameSession → Players (Cascade is fine here, only ONE path)
            modelBuilder.Entity<Player>()
                .HasOne(p => p.GameSession)
                .WithMany(g => g.Players)
                .HasForeignKey(p => p.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameSession → Rounds (Cascade is fine here)
            modelBuilder.Entity<Round>()
                .HasOne(r => r.GameSession)
                .WithMany(g => g.Rounds)
                .HasForeignKey(r => r.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ FIX: Round → Drawer(Player) must be NoAction
            // Because deleting a GameSession already cascades to Players
            // Having a second cascade path through Rounds → Players causes the cycle
            modelBuilder.Entity<Round>()
                .HasOne(r => r.Drawer)
                .WithMany()
                .HasForeignKey(r => r.DrawerPlayerId)
                .OnDelete(DeleteBehavior.NoAction);  // ← THIS is the fix

            // Seed words
            modelBuilder.Entity<WordList>().HasData(
                new WordList { Id = 1, Word = "Cat", Difficulty = "easy" },
                new WordList { Id = 2, Word = "House", Difficulty = "easy" },
                new WordList { Id = 3, Word = "Tree", Difficulty = "easy" },
                new WordList { Id = 4, Word = "Car", Difficulty = "easy" },
                new WordList { Id = 5, Word = "Sun", Difficulty = "easy" },
                new WordList { Id = 6, Word = "Guitar", Difficulty = "medium" },
                new WordList { Id = 7, Word = "Castle", Difficulty = "medium" },
                new WordList { Id = 8, Word = "Rainbow", Difficulty = "medium" },
                new WordList { Id = 9, Word = "Telescope", Difficulty = "hard" },
                new WordList { Id = 10, Word = "Submarine", Difficulty = "hard" }
            );
        }
    }
}
