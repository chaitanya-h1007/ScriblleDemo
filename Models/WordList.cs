using System.ComponentModel.DataAnnotations;

namespace ScriblleDemo.Models
{
    public class WordList
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Word { get; set; } = string.Empty;

        // Difficulty: "easy", "medium", "hard"
        [MaxLength(20)]
        public string Difficulty { get; set; } = "easy";
    }
}
