namespace DDiary.Models
{
    /// <summary>
    /// Sezione pasto all'interno del diario giornaliero.
    /// </summary>
    public class MealSection
    {
        public int Id { get; set; }
        public int DailyDiaryId { get; set; }
        public MealType MealType { get; set; }
        public double TotalCho { get; set; } = 0;
        public double InsulinCarbRatio { get; set; } = 0;
        public double? GlycemiaBefore { get; set; }
        public double? GlycemiaAfter { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Navigation
        public DailyDiary? DailyDiary { get; set; }
        public ICollection<FoodEntry> FoodEntries { get; set; } = new List<FoodEntry>();
    }
}
