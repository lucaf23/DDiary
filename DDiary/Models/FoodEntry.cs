using System;

namespace DDiary.Models
{
    /// <summary>
    /// Voce alimento all'interno di una sezione pasto.
    /// </summary>
    public class FoodEntry
    {
        public int Id { get; set; }
        public int MealSectionId { get; set; }
        public TimeSpan MealTime { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public double PortionGrams { get; set; } = 0;
        public double ChoGrams { get; set; } = 0;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public MealSection? MealSection { get; set; }
    }
}
