using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScriblleDemo.Models
{
    public class Round
    {
        public int Id { get; set; }

        // Which game session does this round belong to?
        public int GameSessionId { get; set; }

        // Round number (1, 2, 3...)
        public int RoundNumber { get; set; }

        // The word that needs to be drawn this round
        [MaxLength(100)]
        public string WordToDraw { get; set; } = string.Empty;

        // Which player is drawing this round? (Foreign Key to Player)
        public int DrawerPlayerId { get; set; }

        // How many seconds for this round (default 80 seconds)
        public int TimeLimit { get; set; } = 80;

        // Has this round finished?
        public bool IsCompleted { get; set; } = false;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("GameSessionId")]
        public virtual GameSession? GameSession { get; set; }

        [ForeignKey("DrawerPlayerId")]
        public virtual Player? Drawer { get; set; }
    }
}
