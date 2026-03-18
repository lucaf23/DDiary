using System;

namespace DDiary.Models
{
    /// <summary>
    /// Diario giornaliero del paziente diabetico.
    /// </summary>
    public class DailyDiary
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public string PhysicalActivityNotes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public UserProfile? UserProfile { get; set; }
        public ICollection<MealSection> MealSections { get; set; } = new List<MealSection>();
    }
}
