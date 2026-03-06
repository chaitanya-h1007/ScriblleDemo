using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScriblleDemo.Models
{
    public class Player
    {
        public int Id { get; set; }
        [Required]                  // NOT NULL in the database
        [MaxLength(50)]             // VARCHAR(50) in MySQL
        public string Nickname { get; set; } = string.Empty;

        // Which game session does this player belong to?
        // This is the Foreign Key — links to GameSession table
        public int GameSessionId { get; set; }

        // Score for this player in this session
        public int Score { get; set; } = 0;

        // Is this player the host (room creator)?
        public bool IsHost { get; set; } = false;

        // When did this player join?
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property — EF Core uses this to do JOINs automatically
        // "virtual" enables lazy loading (loads GameSession data only when needed)
        [ForeignKey("GameSessionId")]
        public virtual GameSession? GameSession { get; set; }
    }
}
