using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ScriblleDemo.Models
{
    public enum GameStatus
    {
        Waiting,    // In lobby, waiting for players
        InProgress, // Game is running
        Finished    // Game over
    }
    public class GameSession
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(8)]
        // The short code players type to join e.g. "AB12XY"
        public string RoomCode { get; set; } = string.Empty;

        // Enum stored as int in DB (0=Waiting, 1=InProgress, 2=Finished)
        public GameStatus Status { get; set; } = GameStatus.Waiting;

        // Max 10 players as per your requirement
        public int MaxPlayers { get; set; } = 10;

        // How many rounds before game ends
        public int TotalRounds { get; set; } = 3;

        // Which round are we on right now?
        public int CurrentRound { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property — one GameSession has MANY Players
        // ICollection = "a list of" in EF Core terms
        public virtual ICollection<Player> Players { get; set; }
            = new List<Player>();

        // Navigation Property — one GameSession has MANY Rounds
        public virtual ICollection<Round> Rounds { get; set; }
            = new List<Round>();
    }
}
